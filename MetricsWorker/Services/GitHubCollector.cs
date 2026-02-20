using MetricsWorker.Configuration;
using MetricsWorker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using MetricsWorker.Interfaces;

namespace MetricsWorker.Services;

public class GitHubCollector : IGitHubCollector
{
    private readonly ILogger<GitHubCollector> _logger;
    private readonly GitHubConfig _config;
    private readonly GitHubClient _client;

    public GitHubCollector(
        ILogger<GitHubCollector> logger,
        IOptions<GitHubConfig> config)
    {
        _logger = logger;
        _config = config.Value;

        // Initialize GitHub client with personal access token
        _client = new GitHubClient(new ProductHeaderValue("MetricsWorker"))
        {
            Credentials = new Credentials(_config.Token)
        };
    }

    public async Task<IEnumerable<GitHubMetric>> CollectRepositoryMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var metrics = new List<GitHubMetric>();
        var timestamp = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Collecting GitHub repository metrics");

            foreach (var repoName in _config.Repositories)
            {
                try
                {
                    var repository = await _client.Repository.Get(_config.Organization, repoName);

                    metrics.Add(new GitHubMetric
                    {
                        Id = Guid.NewGuid(),
                        Timestamp = timestamp,
                        Repository = $"{_config.Organization}/{repoName}",
                        MetricType = "repository_size",
                        Count = repository.Size,
                        Metadata = new Dictionary<string, string>
                        {
                            { "unit", "KB" },
                            { "language", repository.Language ?? "unknown" }
                        }
                    });

                    metrics.Add(new GitHubMetric
                    {
                        Id = Guid.NewGuid(),
                        Timestamp = timestamp,
                        Repository = $"{_config.Organization}/{repoName}",
                        MetricType = "stars",
                        Count = repository.StargazersCount,
                        Metadata = new Dictionary<string, string>
                        {
                            { "visibility", repository.Private ? "private" : "public" }
                        }
                    });

                    // Octokit uses AllowFork instead of AllowForking
                    metrics.Add(new GitHubMetric
                    {
                        Id = Guid.NewGuid(),
                        Timestamp = timestamp,
                        Repository = $"{_config.Organization}/{repoName}",
                        MetricType = "forks",
                        Count = repository.ForksCount,
                        Metadata = new Dictionary<string, string>
                        {
                            { "is_private", repository.Private.ToString() }
                        }
                    });

                    metrics.Add(new GitHubMetric
                    {
                        Id = Guid.NewGuid(),
                        Timestamp = timestamp,
                        Repository = $"{_config.Organization}/{repoName}",
                        MetricType = "open_issues",
                        Count = repository.OpenIssuesCount,
                        Metadata = new Dictionary<string, string>()
                    });

                    _logger.LogInformation("Collected metrics for repository: {Repository}", repoName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error collecting metrics for repository: {Repository}", repoName);
                }

                await Task.Delay(100, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in repository metrics collection");
        }

        return metrics;
    }

    public async Task<IEnumerable<CommitMetric>> CollectCommitMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var commits = new List<CommitMetric>();

        try
        {
            _logger.LogInformation("Collecting GitHub commit metrics");
            var since = DateTimeOffset.UtcNow.AddHours(-24);

            foreach (var repoName in _config.Repositories)
            {
                try
                {
                    var request = new CommitRequest { Since = since };
                    var repoCommits = await _client.Repository.Commit.GetAll(_config.Organization, repoName, request);

                    foreach (var commit in repoCommits)
                    {
                        // Fetch individual commit details to extract file stats
                        var detailedCommit = await _client.Repository.Commit.Get(_config.Organization, repoName, commit.Sha);

                        commits.Add(new CommitMetric
                        {
                            Repository = $"{_config.Organization}/{repoName}",
                            CommitSha = commit.Sha,
                            Author = commit.Commit.Author.Name,
                            CommitDate = commit.Commit.Author.Date.UtcDateTime,
                            LinesAdded = detailedCommit.Stats.Additions,
                            LinesDeleted = detailedCommit.Stats.Deletions,
                            Message = commit.Commit.Message.Length > 500
                                ? commit.Commit.Message.Substring(0, 500)
                                : commit.Commit.Message
                        });
                    }
                    _logger.LogInformation("Buffered {Count} commits from {Repo}", repoCommits.Count, repoName);
                    await Task.Delay(100, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error collecting commits for repository: {Repository}", repoName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in commit metrics collection");
        }

        return commits;
    }

    public async Task<IEnumerable<PullRequestMetric>> CollectPullRequestMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var pullRequests = new List<PullRequestMetric>();

        try
        {
            _logger.LogInformation("Initiating PR sync sequence");

            foreach (var repoName in _config.Repositories)
            {
                try
                {
                    var request = new PullRequestRequest
                    {
                        State = ItemStateFilter.All,
                        SortProperty = PullRequestSort.Updated,
                        SortDirection = SortDirection.Descending
                    };

                    var prs = await _client.PullRequest.GetAllForRepository(_config.Organization, repoName, request);
                    var recentPrs = prs.Where(pr => pr.UpdatedAt > DateTimeOffset.UtcNow.AddDays(-7)).ToList();

                    foreach (var pr in recentPrs)
                    {
                        // Aggregate reviews and comments for PR activity tracking
                        var reviews = await _client.PullRequest.Review.GetAll(_config.Organization, repoName, pr.Number);
                        var issueComments = await _client.Issue.Comment.GetAllForIssue(_config.Organization, repoName, pr.Number);
                        var reviewComments = await _client.PullRequest.ReviewComment.GetAll(_config.Organization, repoName, pr.Number);

                        pullRequests.Add(new PullRequestMetric
                        {
                            Repository = $"{_config.Organization}/{repoName}",
                            PullRequestNumber = pr.Number,
                            State = pr.State.StringValue,
                            Author = pr.User.Login,
                            CreatedAt = pr.CreatedAt.UtcDateTime,
                            MergedAt = pr.MergedAt?.UtcDateTime,
                            CommentsCount = issueComments.Count + reviewComments.Count,
                            ReviewsCount = reviews.Count
                        });

                        await Task.Delay(50, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error collecting PRs for repository: {Repository}", repoName);
                }
            }
            _logger.LogInformation("Pull request data harvest complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in pull request metrics collection");
        }

        return pullRequests;
    }
}