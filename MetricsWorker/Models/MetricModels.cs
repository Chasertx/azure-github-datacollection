namespace MetricsWorker.Models;

public class AzureInsightsMetric
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public Dictionary<string, string> Dimensions { get; set; } = new();
}

public class GitHubMetric
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Repository { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public long Count { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class CommitMetric
{
    public string Repository { get; set; } = string.Empty;
    public string CommitSha { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CommitDate { get; set; }
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PullRequestMetric
{
    public string Repository { get; set; } = string.Empty;
    public int PullRequestNumber { get; set; }
    public string State { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? MergedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int CommentsCount { get; set; }
    public int ReviewsCount { get; set; }
}