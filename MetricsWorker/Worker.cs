using MetricsWorker.Configuration;
using MetricsWorker.Repositories;
using MetricsWorker.Services;
using Microsoft.Extensions.Options;
using MetricsWorker.Interfaces;

namespace MetricsWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ICassandraRepository _repository;
    private readonly IAzureInsightsCollector _azureCollector;
    private readonly AzureInsightsConfig _azureConfig;

    public Worker(
        ILogger<Worker> logger,
        ICassandraRepository repository,
        IAzureInsightsCollector azureCollector,
        IOptions<AzureInsightsConfig> azureConfig)
    {
        _logger = logger;
        _repository = repository;
        _azureCollector = azureCollector;
        _azureConfig = azureConfig.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Azure Insights Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Collecting Azure Insights metrics...");

                var metrics = await _azureCollector.CollectMetricsAsync(stoppingToken);

                foreach (var metric in metrics)
                {
                    await _repository.SaveAzureMetricAsync(metric);
                }

                _logger.LogInformation(
                    "Saved {Count} Azure Insights metrics. Next collection in {Minutes} minutes",
                    metrics.Count(),
                    _azureConfig.CollectionIntervalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Azure Insights collection cycle");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(_azureConfig.CollectionIntervalMinutes),
                stoppingToken);
        }
    }
}