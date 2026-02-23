using MetricsWorker.Models;

namespace MetricsWorker.Interfaces;

// Contract for saving collected metrics into Cassandra/Astra DB.
// The worker uses this interface so storage logic stays in one place.
public interface ICassandraRepository
{
    // Prepares the repository (for example: connect, create tables, verify keyspace).
    Task InitializeAsync();
    // Persists one Azure Insights metric record.
    Task SaveAzureMetricAsync(AzureInsightsMetric metric);
    // Persists one high-level GitHub metric record.
    Task SaveGitHubMetricAsync(GitHubMetric metric);
    // Persists one commit-level metric record.
    Task SaveCommitMetricAsync(CommitMetric metric);
    // Persists one pull-request-level metric record.
    Task SavePullRequestMetricAsync(PullRequestMetric metric);
}
