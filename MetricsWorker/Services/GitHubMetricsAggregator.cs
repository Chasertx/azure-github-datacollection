using MetricsWorker.Models;
using Microsoft.Extensions.Logging;

namespace MetricsWorker.Services;

// Defines aggregate calculations that convert raw GitHub events into summary metrics.
public interface IGitHubMetricsAggregator
{
    IEnumerable<GitHubMetric> CalculateCommitMetrics(IEnumerable<CommitMetric> commits, DateTime timestamp);
    IEnumerable<GitHubMetric> CalculatePullRequestMetrics(IEnumerable<PullRequestMetric> pullRequests, DateTime timestamp);
}

// Groups commit/PR data and computes higher-level GitHub metrics per repository.
public class GitHubMetricsAggregator : IGitHubMetricsAggregator
{
    private readonly ILogger<GitHubMetricsAggregator> _logger;

    public GitHubMetricsAggregator(ILogger<GitHubMetricsAggregator> logger)
    {
        _logger = logger;
    }

    public IEnumerable<GitHubMetric> CalculateCommitMetrics(
        IEnumerable<CommitMetric> commits,
        DateTime timestamp)
    {
        var metrics = new List<GitHubMetric>();

        // Materialize once so we can efficiently check and reuse the data.
        var commitsList = commits.ToList();
        if (!commitsList.Any())
            return metrics;

        // Compute commit aggregates separately for each repository.
        var byRepo = commitsList.GroupBy(c => c.Repository);

        foreach (var repoGroup in byRepo)
        {
            var repoCommits = repoGroup.ToList();

            // Total commits seen in the last 24-hour collection window.
            metrics.Add(new GitHubMetric
            {
                Id = Guid.NewGuid(),
                Timestamp = timestamp,
                Repository = repoGroup.Key,
                MetricType = "commits_24h",
                Count = repoCommits.Count,
                Metadata = new Dictionary<string, string>
                {
                    { "period", "24_hours" }
                }
            });

            // Total lines added/deleted across all commits.
            var totalAdded = repoCommits.Sum(c => c.LinesAdded);
            var totalDeleted = repoCommits.Sum(c => c.LinesDeleted);

            metrics.Add(new GitHubMetric
            {
                Id = Guid.NewGuid(),
                Timestamp = timestamp,
                Repository = repoGroup.Key,
                MetricType = "lines_added_24h",
                Count = totalAdded,
                Metadata = new Dictionary<string, string>
                {
                    { "period", "24_hours" }
                }
            });

            metrics.Add(new GitHubMetric
            {
                Id = Guid.NewGuid(),
                Timestamp = timestamp,
                Repository = repoGroup.Key,
                MetricType = "lines_deleted_24h",
                Count = totalDeleted,
                Metadata = new Dictionary<string, string>
                {
                    { "period", "24_hours" }
                }
            });

            // Number of unique commit authors.
            var uniqueAuthors = repoCommits.Select(c => c.Author).Distinct().Count();
            metrics.Add(new GitHubMetric
            {
                Id = Guid.NewGuid(),
                Timestamp = timestamp,
                Repository = repoGroup.Key,
                MetricType = "active_contributors_24h",
                Count = uniqueAuthors,
                Metadata = new Dictionary<string, string>
                {
                    { "period", "24_hours" }
                }
            });

            // Average commit size based on lines changed (added + deleted).
            var avgCommitSize = repoCommits.Any()
                ? (int)repoCommits.Average(c => c.LinesAdded + c.LinesDeleted)
                : 0;

            metrics.Add(new GitHubMetric
            {
                Id = Guid.NewGuid(),
                Timestamp = timestamp,
                Repository = repoGroup.Key,
                MetricType = "avg_commit_size_24h",
                Count = avgCommitSize,
                Metadata = new Dictionary<string, string>
                {
                    { "period", "24_hours" },
                    { "unit", "lines_changed" }
                }
            });
        }

        _logger.LogInformation("Calculated {Count} aggregated commit metrics", metrics.Count);
        return metrics;
    }

    public IEnumerable<GitHubMetric> CalculatePullRequestMetrics(
        IEnumerable<PullRequestMetric> pullRequests,
        DateTime timestamp)
    {
        var metrics = new List<GitHubMetric>();

        // Materialize once so we can efficiently check and reuse the data.
        var prList = pullRequests.ToList();
        if (!prList.Any())
            return metrics;

        // Compute PR aggregates separately for each repository.
        var byRepo = prList.GroupBy(pr => pr.Repository);

        foreach (var repoGroup in byRepo)
        {
            var repoPRs = repoGroup.ToList();

            // Current number of open pull requests.
            var openPRs = repoPRs.Count(pr => pr.State == "open");
            metrics.Add(new GitHubMetric
            {
                Id = Guid.NewGuid(),
                Timestamp = timestamp,
                Repository = repoGroup.Key,
                MetricType = "open_pull_requests",
                Count = openPRs,
                Metadata = new Dictionary<string, string>()
            });

            // Number of PRs merged in the last 7 days.
            var mergedPRs = repoPRs.Count(pr =>
                pr.MergedAt.HasValue &&
                pr.MergedAt.Value > DateTime.UtcNow.AddDays(-7));

            metrics.Add(new GitHubMetric
            {
                Id = Guid.NewGuid(),
                Timestamp = timestamp,
                Repository = repoGroup.Key,
                MetricType = "merged_prs_7d",
                Count = mergedPRs,
                Metadata = new Dictionary<string, string>
                {
                    { "period", "7_days" }
                }
            });

            // Average time-to-merge for PRs merged in the last 7 days.
            var recentMergedPRs = repoPRs
                .Where(pr => pr.MergedAt.HasValue &&
                            pr.MergedAt.Value > DateTime.UtcNow.AddDays(-7))
                .ToList();

            if (recentMergedPRs.Any())
            {
                var avgTimeToMerge = recentMergedPRs
                    .Average(pr => (pr.MergedAt!.Value - pr.CreatedAt).TotalHours);

                metrics.Add(new GitHubMetric
                {
                    Id = Guid.NewGuid(),
                    Timestamp = timestamp,
                    Repository = repoGroup.Key,
                    MetricType = "avg_time_to_merge_hours",
                    Count = (int)avgTimeToMerge,
                    Metadata = new Dictionary<string, string>
                    {
                        { "period", "7_days" },
                        { "unit", "hours" }
                    }
                });
            }

            // Average discussion/review activity across PRs.
            var avgComments = repoPRs.Any() ? (int)repoPRs.Average(pr => pr.CommentsCount) : 0;
            var avgReviews = repoPRs.Any() ? (int)repoPRs.Average(pr => pr.ReviewsCount) : 0;

            metrics.Add(new GitHubMetric
            {
                Id = Guid.NewGuid(),
                Timestamp = timestamp,
                Repository = repoGroup.Key,
                MetricType = "avg_pr_comments",
                Count = avgComments,
                Metadata = new Dictionary<string, string>
                {
                    { "period", "7_days" }
                }
            });

            metrics.Add(new GitHubMetric
            {
                Id = Guid.NewGuid(),
                Timestamp = timestamp,
                Repository = repoGroup.Key,
                MetricType = "avg_pr_reviews",
                Count = avgReviews,
                Metadata = new Dictionary<string, string>
                {
                    { "period", "7_days" }
                }
            });
        }

        _logger.LogInformation("Calculated {Count} aggregated PR metrics", metrics.Count);
        return metrics;
    }
}
