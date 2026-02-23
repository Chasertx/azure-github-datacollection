namespace MetricsWorker.Configuration;

// Settings used to read metrics from Azure Monitor / Log Analytics.
public class AzureInsightsConfig
{
    // Azure AD tenant that owns the app registration.
    public string TenantId { get; set; } = string.Empty;
    // Client/application ID from the app registration.
    public string ClientId { get; set; } = string.Empty;
    // Client secret used to authenticate the app.
    public string ClientSecret { get; set; } = string.Empty;
    // Log Analytics workspace to query for metrics.
    public string WorkspaceId { get; set; } = string.Empty;
    // How often Azure data should be collected (in minutes).
    public int CollectionIntervalMinutes { get; set; } = 15;
}

// Settings for pulling repository activity from GitHub.
public class GitHubConfig
{
    // Personal access token or GitHub app token.
    public string Token { get; set; } = string.Empty;
    // GitHub organization name to scope repo lookups.
    public string Organization { get; set; } = string.Empty;
    // Repositories to include when collecting metrics.
    public List<string> Repositories { get; set; } = new();
    // How often GitHub data should be collected (in minutes).
    public int CollectionIntervalMinutes { get; set; } = 30;
}

// Settings for writing collected metrics into Astra DB.
public class AstraDBConfig
{
    // Astra database ID.
    public string DatabaseId { get; set; } = string.Empty;
    // Region where the Astra database is hosted.
    public string DatabaseRegion { get; set; } = string.Empty;
    // Keyspace used to store metrics tables.
    public string Keyspace { get; set; } = "metrics_data";
    // Astra application token with DB access.
    public string ApplicationToken { get; set; } = string.Empty;
    // Path to the secure connect bundle zip file.
    public string SecureConnectBundlePath { get; set; } = string.Empty;
}
