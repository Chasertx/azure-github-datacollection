using MetricsWorker.Models;

namespace MetricsWorker.Services;

public interface IAzureInsightsCollector
{
    Task<IEnumerable<AzureInsightsMetric>> CollectMetricsAsync(CancellationToken cancellationToken = default);
}