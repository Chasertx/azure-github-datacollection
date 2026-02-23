namespace MetricsWorker.Models;

// Generic metric record collected from Azure Insights.
public class AzureInsightsMetric
{
    // Unique record ID for storage/tracking.
    public Guid Id { get; set; }
    // When this metric snapshot was collected.
    public DateTime Timestamp { get; set; }
    // Metric name (for example CPUPercentage).
    public string MetricName { get; set; } = string.Empty;
    // Azure resource that produced the metric.
    public string ResourceId { get; set; } = string.Empty;
    // Numeric metric value.
    public double Value { get; set; }
    // Unit for the value (percent, bytes, etc.).
    public string Unit { get; set; } = string.Empty;
    // Extra labels/dimensions that describe this metric.
    public Dictionary<string, string> Dimensions { get; set; } = new();
}

// High-level GitHub metric entry (counts by type).
public class GitHubMetric
{
    // Unique record ID for storage/tracking.
    public Guid Id { get; set; }
    // When this metric snapshot was collected.
    public DateTime Timestamp { get; set; }
    // Repository name this metric belongs to.
    public string Repository { get; set; } = string.Empty;
    // Metric category/type (commits, issues, PRs, etc.).
    public string MetricType { get; set; } = string.Empty;
    // Count value for this metric type.
    public long Count { get; set; }
    // Additional context for this metric.
    public Dictionary<string, string> Metadata { get; set; } = new();
}

// Commit-level details collected from GitHub.
public class CommitMetric
{
    // Repository where the commit was made.
    public string Repository { get; set; } = string.Empty;
    // Full SHA for the commit.
    public string CommitSha { get; set; } = string.Empty;
    // Commit author name/login.
    public string Author { get; set; } = string.Empty;
    // Date/time when the commit was created.
    public DateTime CommitDate { get; set; }
    // Number of lines added in the commit.
    public int LinesAdded { get; set; }
    // Number of lines deleted in the commit.
    public int LinesDeleted { get; set; }
    // Commit message text.
    public string Message { get; set; } = string.Empty;
}

// Pull request-level details collected from GitHub.
public class PullRequestMetric
{
    // Repository where the PR exists.
    public string Repository { get; set; } = string.Empty;
    // PR number within the repository.
    public int PullRequestNumber { get; set; }
    // Current PR state (open, closed, merged).
    public string State { get; set; } = string.Empty;
    // User who opened the PR.
    public string Author { get; set; } = string.Empty;
    // When the PR was created.
    public DateTime CreatedAt { get; set; }
    // When the PR was merged (if merged).
    public DateTime? MergedAt { get; set; }
    // When the PR was closed (if closed).
    public DateTime? ClosedAt { get; set; }
    // Total number of comments on the PR.
    public int CommentsCount { get; set; }
    // Total number of review events on the PR.
    public int ReviewsCount { get; set; }
}
