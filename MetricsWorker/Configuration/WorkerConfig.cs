namespace MetricsWorker.Configuration;

public class AzureInsightsConfig
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public int CollectionIntervalMinutes { get; set; } = 15;
}

public class GitHubConfig
{
    public string Token { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public List<string> Repositories { get; set; } = new();
    public int CollectionIntervalMinutes { get; set; } = 30;
}

public class AstraDBConfig
{
    public string DatabaseId { get; set; } = string.Empty;
    public string DatabaseRegion { get; set; } = string.Empty;
    public string Keyspace { get; set; } = "metrics_data";
    public string ApplicationToken { get; set; } = string.Empty;
    public string SecureConnectBundlePath { get; set; } = string.Empty;
}