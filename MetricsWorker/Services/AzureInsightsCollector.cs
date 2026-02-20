using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using MetricsWorker.Configuration;
using MetricsWorker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MetricsWorker.Services;

public class AzureInsightsCollector : IAzureInsightsCollector
{
    private readonly ILogger<AzureInsightsCollector> _logger;
    private readonly AzureInsightsConfig _config;
    private readonly LogsQueryClient _logsClient;

    public AzureInsightsCollector(
        ILogger<AzureInsightsCollector> logger,
        IOptions<AzureInsightsConfig> config)
    {
        _logger = logger;
        _config = config.Value;

        // Initialize credentials using service principal
        var credential = new ClientSecretCredential(
            _config.TenantId,
            _config.ClientId,
            _config.ClientSecret);

        _logsClient = new LogsQueryClient(credential);
    }

    public async Task<IEnumerable<AzureInsightsMetric>> CollectMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var metrics = new List<AzureInsightsMetric>();

        try
        {
            _logger.LogInformation("Starting Azure Insights metrics collection");

            // Aggregate metrics from all collectors
            metrics.AddRange(await CollectRequestMetricsAsync(cancellationToken));
            metrics.AddRange(await CollectDependencyMetricsAsync(cancellationToken));
            metrics.AddRange(await CollectExceptionMetricsAsync(cancellationToken));
            metrics.AddRange(await CollectCustomMetricsAsync(cancellationToken));

            _logger.LogInformation("Collected {Count} Azure Insights metrics", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting Azure Insights metrics");
        }

        return metrics;
    }

    private async Task<IEnumerable<AzureInsightsMetric>> CollectRequestMetricsAsync(
        CancellationToken cancellationToken)
    {
        var metrics = new List<AzureInsightsMetric>();
        try
        {
            // Querying AppRequests with PascalCase columns
            var query = @"
                AppRequests
                | where TimeGenerated > ago(700h)
                | summarize 
                    avg_duration = avg(DurationMs),
                    p95_duration = percentile(DurationMs, 95),
                    p99_duration = percentile(DurationMs, 99),
                    request_count = count(),
                    success_count = countif(Success == true),
                    failure_count = countif(Success == false)
                    by bin(TimeGenerated, 5m), Name, ResultCode
                | order by TimeGenerated desc";

            var response = await _logsClient.QueryWorkspaceAsync(
                _config.WorkspaceId,
                query,
                new QueryTimeRange(TimeSpan.FromHours(1)),
                cancellationToken: cancellationToken);

            var table = response.Value.Table;
            foreach (var row in table.Rows)
            {
                var timestamp = row.GetDateTimeOffset("TimeGenerated") ?? DateTimeOffset.UtcNow;
                var operationName = row.GetString("Name") ?? "unknown";
                var resultCode = row.GetString("ResultCode") ?? "200";

                metrics.Add(new AzureInsightsMetric
                {
                    Id = Guid.NewGuid(),
                    Timestamp = timestamp.UtcDateTime,
                    MetricName = "RequestDuration_Avg",
                    ResourceId = _config.WorkspaceId,
                    Value = row.GetDouble("avg_duration") ?? 0,
                    Unit = "milliseconds",
                    Dimensions = new Dictionary<string, string> { { "operation", operationName }, { "resultCode", resultCode } }
                });

                metrics.Add(new AzureInsightsMetric
                {
                    Id = Guid.NewGuid(),
                    Timestamp = timestamp.UtcDateTime,
                    MetricName = "RequestCount",
                    ResourceId = _config.WorkspaceId,
                    Value = row.GetInt64("request_count") ?? 0,
                    Unit = "count",
                    Dimensions = new Dictionary<string, string> { { "operation", operationName }, { "resultCode", resultCode } }
                });
            }
            _logger.LogInformation("Extracted {Count} request metrics from workspace logs", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting request metrics");
        }
        return metrics;
    }

    private async Task<IEnumerable<AzureInsightsMetric>> CollectDependencyMetricsAsync(
        CancellationToken cancellationToken)
    {
        var metrics = new List<AzureInsightsMetric>();
        try
        {
            // Querying AppDependencies using DurationMs and PascalCase
            var query = @"
                AppDependencies
                | where TimeGenerated > ago(700h)
                | summarize 
                    avg_duration = avg(DurationMs),
                    call_count = count(),
                    success_count = countif(Success == true),
                    failure_count = countif(Success == false)
                    by bin(TimeGenerated, 5m), Name, Type, Target
                | order by TimeGenerated desc";

            var response = await _logsClient.QueryWorkspaceAsync(
                _config.WorkspaceId,
                query,
                new QueryTimeRange(TimeSpan.FromHours(1)),
                cancellationToken: cancellationToken);

            foreach (var row in response.Value.Table.Rows)
            {
                var timestamp = row.GetDateTimeOffset("TimeGenerated") ?? DateTimeOffset.UtcNow;
                metrics.Add(new AzureInsightsMetric
                {
                    Id = Guid.NewGuid(),
                    Timestamp = timestamp.UtcDateTime,
                    MetricName = "DependencyDuration_Avg",
                    ResourceId = _config.WorkspaceId,
                    Value = row.GetDouble("avg_duration") ?? 0,
                    Unit = "milliseconds",
                    Dimensions = new Dictionary<string, string>
                    {
                        { "dependency", row.GetString("Name") ?? "unknown" },
                        { "type", row.GetString("Type") ?? "unknown" }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting dependency metrics");
        }
        return metrics;
    }

    private async Task<IEnumerable<AzureInsightsMetric>> CollectExceptionMetricsAsync(
        CancellationToken cancellationToken)
    {
        var metrics = new List<AzureInsightsMetric>();
        try
        {
            // AppExceptions uses ExceptionType and Message
            var query = @"
                AppExceptions
                | where TimeGenerated > ago(700h)
                | summarize 
                    exception_count = count()
                    by bin(TimeGenerated, 5m), ExceptionType, Message
                | order by TimeGenerated desc";

            var response = await _logsClient.QueryWorkspaceAsync(_config.WorkspaceId, query, new QueryTimeRange(TimeSpan.FromHours(1)), cancellationToken: cancellationToken);

            foreach (var row in response.Value.Table.Rows)
            {
                metrics.Add(new AzureInsightsMetric
                {
                    Id = Guid.NewGuid(),
                    Timestamp = (row.GetDateTimeOffset("TimeGenerated") ?? DateTimeOffset.UtcNow).UtcDateTime,
                    MetricName = "ExceptionCount",
                    ResourceId = _config.WorkspaceId,
                    Value = row.GetInt64("exception_count") ?? 0,
                    Unit = "count",
                    Dimensions = new Dictionary<string, string> { { "type", row.GetString("ExceptionType") ?? "unknown" } }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting exception metrics");
        }
        return metrics;
    }

    private async Task<IEnumerable<AzureInsightsMetric>> CollectCustomMetricsAsync(
        CancellationToken cancellationToken)
    {
        var metrics = new List<AzureInsightsMetric>();
        try
        {
            // AppMetrics uses Sum for the value column
            var query = @"
                AppMetrics
                | where TimeGenerated > ago(700h)
                | summarize 
                    avg_value = avg(Sum)
                    by bin(TimeGenerated, 5m), Name
                | order by TimeGenerated desc";

            var response = await _logsClient.QueryWorkspaceAsync(_config.WorkspaceId, query, new QueryTimeRange(TimeSpan.FromHours(1)), cancellationToken: cancellationToken);

            foreach (var row in response.Value.Table.Rows)
            {
                metrics.Add(new AzureInsightsMetric
                {
                    Id = Guid.NewGuid(),
                    Timestamp = (row.GetDateTimeOffset("TimeGenerated") ?? DateTimeOffset.UtcNow).UtcDateTime,
                    MetricName = $"Custom_{row.GetString("Name")}",
                    ResourceId = _config.WorkspaceId,
                    Value = row.GetDouble("avg_value") ?? 0,
                    Unit = "custom"
                });
            }
            _logger.LogInformation("Metric sync successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting custom metrics");
        }
        return metrics;
    }
}