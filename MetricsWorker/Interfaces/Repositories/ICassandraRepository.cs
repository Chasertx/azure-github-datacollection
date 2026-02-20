using MetricsWorker.Models;

namespace MetricsWorker.Interfaces;

public interface ICassandraRepository
{
    Task InitializeAsync();
    Task SaveAzureMetricAsync(AzureInsightsMetric metric);
    Task SaveGitHubMetricAsync(GitHubMetric metric);
    Task SaveCommitMetricAsync(CommitMetric metric);
    Task SavePullRequestMetricAsync(PullRequestMetric metric);
}