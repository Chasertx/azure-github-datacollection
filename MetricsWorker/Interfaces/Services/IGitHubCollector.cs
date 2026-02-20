using MetricsWorker.Models;

namespace MetricsWorker.Interfaces;

public interface IGitHubCollector
{
    Task<IEnumerable<GitHubMetric>> CollectRepositoryMetricsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CommitMetric>> CollectCommitMetricsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PullRequestMetric>> CollectPullRequestMetricsAsync(CancellationToken cancellationToken = default);
}