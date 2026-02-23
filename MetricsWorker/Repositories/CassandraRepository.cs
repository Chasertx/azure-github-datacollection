using Cassandra;
using MetricsWorker.Configuration;
using MetricsWorker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MetricsWorker.Interfaces;

namespace MetricsWorker.Repositories;

// Handles all writes to Cassandra/Astra DB for collected metrics.
public class CassandraRepository : ICassandraRepository, IDisposable
{
    private readonly ILogger<CassandraRepository> _logger;
    private readonly AstraDBConfig _config;
    private ICluster? _cluster;
    private ISession? _session;

    public CassandraRepository(
        ILogger<CassandraRepository> logger,
        IOptions<AstraDBConfig> config)
    {
        // Pull logger and DB settings from dependency injection.
        _logger = logger;
        _config = config.Value;
    }

    // Opens the Astra DB connection and selects the configured keyspace.
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Connecting to Astra DB: {DatabaseId}", _config.DatabaseId);

            // Basic config checks so failures happen early with clear errors.
            if (string.IsNullOrEmpty(_config.SecureConnectBundlePath))
            {
                throw new InvalidOperationException("SecureConnectBundlePath is not configured");
            }

            if (!File.Exists(_config.SecureConnectBundlePath))
            {
                throw new FileNotFoundException(
                    $"Secure Connect Bundle not found at: {_config.SecureConnectBundlePath}");
            }

            if (string.IsNullOrEmpty(_config.ApplicationToken))
            {
                throw new InvalidOperationException("ApplicationToken is not configured");
            }

            // Build Cassandra driver connection using Astra secure bundle + token auth.
            _cluster = Cluster.Builder()
                .WithCloudSecureConnectionBundle(_config.SecureConnectBundlePath)
                .WithCredentials("token", _config.ApplicationToken)
                .Build();

            _session = await _cluster.ConnectAsync(_config.Keyspace);

            _logger.LogInformation("Successfully connected to Astra DB keyspace: {Keyspace}",
                _config.Keyspace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Astra DB");
            throw;
        }
    }

    // Writes one Azure metric row into the azure_insights_metrics table.
    public async Task SaveAzureMetricAsync(AzureInsightsMetric metric)
    {
        var insert = @"
            INSERT INTO azure_insights_metrics 
            (id, timestamp, metric_name, resource_id, value, unit, dimensions)
            VALUES (?, ?, ?, ?, ?, ?, ?)";

        var statement = new SimpleStatement(insert,
            metric.Id,
            metric.Timestamp,
            metric.MetricName,
            metric.ResourceId,
            metric.Value,
            metric.Unit,
            metric.Dimensions);

        await _session!.ExecuteAsync(statement);

        _logger.LogDebug("Saved Azure metric: {MetricName} = {Value}",
            metric.MetricName, metric.Value);
    }

    // Writes one GitHub aggregate metric row into the github_metrics table.
    public async Task SaveGitHubMetricAsync(GitHubMetric metric)
    {
        var insert = @"
            INSERT INTO github_metrics 
            (id, timestamp, repository, metric_type, count, metadata)
            VALUES (?, ?, ?, ?, ?, ?)";

        var statement = new SimpleStatement(insert,
            metric.Id,
            metric.Timestamp,
            metric.Repository,
            metric.MetricType,
            metric.Count,
            metric.Metadata);

        await _session!.ExecuteAsync(statement);

        _logger.LogDebug("Saved GitHub metric: {Repository}/{MetricType} = {Count}",
            metric.Repository, metric.MetricType, metric.Count);
    }

    // Writes one commit-level metric row into the commit_metrics table.
    public async Task SaveCommitMetricAsync(CommitMetric metric)
    {
        var insert = @"
            INSERT INTO commit_metrics 
            (repository, commit_date, commit_sha, author, lines_added, lines_deleted, message)
            VALUES (?, ?, ?, ?, ?, ?, ?)";

        var statement = new SimpleStatement(insert,
            metric.Repository,
            metric.CommitDate,
            metric.CommitSha,
            metric.Author,
            metric.LinesAdded,
            metric.LinesDeleted,
            metric.Message);

        await _session!.ExecuteAsync(statement);
    }

    // Writes one pull-request-level metric row into the pull_request_metrics table.
    public async Task SavePullRequestMetricAsync(PullRequestMetric metric)
    {
        var insert = @"
            INSERT INTO pull_request_metrics 
            (repository, pull_request_number, state, author, created_at, 
             merged_at, closed_at, comments_count, reviews_count)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

        var statement = new SimpleStatement(insert,
            metric.Repository,
            metric.PullRequestNumber,
            metric.State,
            metric.Author,
            metric.CreatedAt,
            metric.MergedAt,
            metric.ClosedAt,
            metric.CommentsCount,
            metric.ReviewsCount);

        await _session!.ExecuteAsync(statement);
    }

    // Cleans up Cassandra resources when the repository is disposed.
    public void Dispose()
    {
        _session?.Dispose();
        _cluster?.Dispose();
    }
}
