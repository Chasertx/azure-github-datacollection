using MetricsWorker.Configuration;
using MetricsWorker.Repositories;
using MetricsWorker.Services;
using Microsoft.Extensions.Options;
using MetricsWorker.Interfaces;

namespace MetricsWorker;

public class GitHubWorker : BackgroundService
{
    private readonly ILogger<GitHubWorker> _logger;
    private readonly ICassandraRepository _repository;
    private readonly IGitHubCollector _githubCollector;
    private readonly IGitHubMetricsAggregator _aggregator;
    private readonly GitHubConfig _config;

    public GitHubWorker(
        ILogger<GitHubWorker> logger,
        ICassandraRepository repository,
        IGitHubCollector githubCollector,
        IGitHubMetricsAggregator aggregator,
        IOptions<GitHubConfig> config)
    {
        _logger = logger;
        _repository = repository;
        _githubCollector = githubCollector;
        _aggregator = aggregator;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GitHub Metrics Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                _logger.LogInformation("Collecting GitHub metrics...");

                // Collect repository-level metrics
                var repoMetrics = await _githubCollector.CollectRepositoryMetricsAsync(stoppingToken);
                foreach (var metric in repoMetrics)
                {
                    await _repository.SaveGitHubMetricAsync(metric);
                }

                // Collect commit metrics
                var commits = await _githubCollector.CollectCommitMetricsAsync(stoppingToken);
                foreach (var commit in commits)
                {
                    await _repository.SaveCommitMetricAsync(commit);
                }

                // Calculate and save aggregated commit metrics
                var commitAggMetrics = _aggregator.CalculateCommitMetrics(commits, timestamp);
                foreach (var metric in commitAggMetrics)
                {
                    await _repository.SaveGitHubMetricAsync(metric);
                }

                // Collect pull request metrics
                var pullRequests = await _githubCollector.CollectPullRequestMetricsAsync(stoppingToken);
                foreach (var pr in pullRequests)
                {
                    await _repository.SavePullRequestMetricAsync(pr);
                }

                // Calculate and save aggregated PR metrics
                var prAggMetrics = _aggregator.CalculatePullRequestMetrics(pullRequests, timestamp);
                foreach (var metric in prAggMetrics)
                {
                    await _repository.SaveGitHubMetricAsync(metric);
                }

                var totalMetrics = repoMetrics.Count() + commitAggMetrics.Count() + prAggMetrics.Count();
                _logger.LogInformation(
                    "Saved {Count} GitHub metrics. Next collection in {Minutes} minutes",
                    totalMetrics,
                    _config.CollectionIntervalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GitHub collection cycle");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(_config.CollectionIntervalMinutes),
                stoppingToken);
        }
    }
}