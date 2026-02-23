using MetricsWorker.Models;

namespace MetricsWorker.Interfaces;

// Defines how GitHub data is collected before being saved to storage.
public interface IGitHubCollector
{
    // Collects repository-level metrics (for example stars, forks, open issues).
    Task<IEnumerable<GitHubMetric>> CollectRepositoryMetricsAsync(CancellationToken cancellationToken = default);
    // Collects commit-level metrics across the configured repositories.
    Task<IEnumerable<CommitMetric>> CollectCommitMetricsAsync(CancellationToken cancellationToken = default);
    // Collects pull request metrics across the configured repositories.
    Task<IEnumerable<PullRequestMetric>> CollectPullRequestMetricsAsync(CancellationToken cancellationToken = default);
}
