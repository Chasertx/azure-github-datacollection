using MetricsWorker;
using MetricsWorker.Configuration;
using MetricsWorker.Repositories;
using Serilog;
using MetricsWorker.Interfaces;
using MetricsWorker.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/metrics-worker-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Metrics Worker Service");

    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog
    builder.Services.AddSerilog();

    // Bind configuration
    builder.Services.Configure<AzureInsightsConfig>(
        builder.Configuration.GetSection("AzureInsights"));
    builder.Services.Configure<GitHubConfig>(
        builder.Configuration.GetSection("GitHub"));
    builder.Services.Configure<AstraDBConfig>(
        builder.Configuration.GetSection("AstraDB"));
    builder.Services.AddSingleton<IAzureInsightsCollector, AzureInsightsCollector>();
    // Register services
    builder.Services.AddSingleton<ICassandraRepository, CassandraRepository>();
    builder.Services.AddSingleton<IGitHubCollector, GitHubCollector>();
    builder.Services.AddSingleton<IGitHubMetricsAggregator, GitHubMetricsAggregator>();

    // Register the worker
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<GitHubWorker>();

    var host = builder.Build();

    // Initialize Cassandra connection
    var cassandraRepo = host.Services.GetRequiredService<ICassandraRepository>();
    await cassandraRepo.InitializeAsync();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}