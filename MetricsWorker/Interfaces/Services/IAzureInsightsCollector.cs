using MetricsWorker.Models;

namespace MetricsWorker.Services;

// Defines how Azure Insights metrics are collected from the source system.
public interface IAzureInsightsCollector
{
    // Collects the latest Azure metrics and returns them for downstream storage.
    // CancellationToken lets the worker stop gracefully during shutdown.
    Task<IEnumerable<AzureInsightsMetric>> CollectMetricsAsync(CancellationToken cancellationToken = default);
}
