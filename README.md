# MetricsWorker

`MetricsWorker` is a .NET worker service that:
- Collects Azure Application Insights metrics from Log Analytics
- Collects GitHub repository, commit, and pull request metrics
- Stores everything in Astra DB (Cassandra)

The app starts two hosted workers:
- `Worker` for Azure Insights metrics
- `GitHubWorker` for GitHub metrics

## What This Project Does

Every cycle, the service:
1. Reads telemetry from Azure Log Analytics (`AppRequests`, `AppDependencies`, `AppExceptions`, `AppMetrics`)
2. Reads GitHub repository/commit/PR data from configured repos
3. Builds aggregate GitHub metrics (for example commits in last 24h, merged PRs in last 7d)
4. Writes raw + aggregated records into Cassandra tables

## Prerequisites

Install:
- .NET SDK `10.0` (project targets `net10.0`)
- Access to an Astra DB (Cassandra) database
- Azure AD app registration with access to the target Log Analytics workspace
- GitHub token with repo read permissions

Have these ready:
- Astra Secure Connect Bundle zip path
- Astra application token
- Azure `TenantId`, `ClientId`, `ClientSecret`, `WorkspaceId`
- GitHub `Organization`, token, and repository list

## Project Structure

- `MetricsWorker/Program.cs`: DI, config binding, hosted-service startup
- `MetricsWorker/Worker.cs`: Azure collection loop
- `MetricsWorker/GitHubWorker.cs`: GitHub collection + aggregation loop
- `MetricsWorker/Services/AzureInsightsCollector.cs`: Azure queries + mapping
- `MetricsWorker/Services/GitHubCollector.cs`: GitHub API collection
- `MetricsWorker/Services/GitHubMetricsAggregator.cs`: Derived GitHub metrics
- `MetricsWorker/Repositories/CassandraRepository.cs`: Cassandra connection + inserts

## Configuration

Update `MetricsWorker/appsettings.json`.

Important:
- Code binds GitHub config from section name `GitHub` (not `GitHubConfig`).
- If your file currently uses `GitHubConfig`, rename it to `GitHub` or update `Program.cs`.

Use this shape:

```json
{
  "AzureInsights": {
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "WorkspaceId": "YOUR_WORKSPACE_ID",
    "CollectionIntervalMinutes": 15
  },
  "GitHub": {
    "Token": "YOUR_GITHUB_TOKEN",
    "Organization": "YOUR_ORG",
    "Repositories": [
      "repo-one",
      "repo-two"
    ],
    "CollectionIntervalMinutes": 30
  },
  "AstraDB": {
    "DatabaseId": "YOUR_ASTRA_DB_ID",
    "DatabaseRegion": "YOUR_ASTRA_REGION",
    "Keyspace": "metrics_data",
    "ApplicationToken": "YOUR_ASTRA_APPLICATION_TOKEN",
    "SecureConnectBundlePath": "C:\\path\\to\\secure-connect-xxxx.zip"
  }
}
```

## Cassandra Schema

Create the keyspace and tables before running the service.

```sql
CREATE KEYSPACE IF NOT EXISTS metrics_data
WITH replication = {'class': 'NetworkTopologyStrategy', 'replication_factor': 3};

CREATE TABLE IF NOT EXISTS metrics_data.azure_insights_metrics (
  id uuid,
  timestamp timestamp,
  metric_name text,
  resource_id text,
  value double,
  unit text,
  dimensions map<text, text>,
  PRIMARY KEY ((resource_id), timestamp, id)
) WITH CLUSTERING ORDER BY (timestamp DESC, id DESC);

CREATE TABLE IF NOT EXISTS metrics_data.github_metrics (
  id uuid,
  timestamp timestamp,
  repository text,
  metric_type text,
  count bigint,
  metadata map<text, text>,
  PRIMARY KEY ((repository), timestamp, metric_type, id)
) WITH CLUSTERING ORDER BY (timestamp DESC, metric_type ASC, id DESC);

CREATE TABLE IF NOT EXISTS metrics_data.commit_metrics (
  repository text,
  commit_date timestamp,
  commit_sha text,
  author text,
  lines_added int,
  lines_deleted int,
  message text,
  PRIMARY KEY ((repository), commit_date, commit_sha)
) WITH CLUSTERING ORDER BY (commit_date DESC, commit_sha ASC);

CREATE TABLE IF NOT EXISTS metrics_data.pull_request_metrics (
  repository text,
  pull_request_number int,
  state text,
  author text,
  created_at timestamp,
  merged_at timestamp,
  closed_at timestamp,
  comments_count int,
  reviews_count int,
  PRIMARY KEY ((repository), pull_request_number)
);
```

Notes:
- These tables match the insert columns used in `MetricsWorker/Repositories/CassandraRepository.cs`.
- Adjust primary keys to your read/query patterns if needed.

## Run Locally

From the repo root:

```powershell
dotnet restore
dotnet build
dotnet run --project .\MetricsWorker\MetricsWorker.csproj
```

## What You Should See

On startup:
- Cassandra connection logs
- Azure and GitHub worker startup logs
- Periodic logs each collection cycle with saved counts

Log output:
- Console
- Rolling file logs at `MetricsWorker/logs/metrics-worker-YYYYMMDD.txt`

## Data Collection Behavior

Azure worker:
- Runs every `AzureInsights.CollectionIntervalMinutes`
- Queries App Insights tables and writes `azure_insights_metrics`

GitHub worker:
- Runs every `GitHub.CollectionIntervalMinutes`
- Writes:
  - Raw repository metrics -> `github_metrics`
  - Raw commit data -> `commit_metrics`
  - Raw PR data -> `pull_request_metrics`
  - Aggregated commit/PR metrics -> `github_metrics`

## Troubleshooting

`GitHub metrics are not being collected`:
- Verify appsettings section is `GitHub` (or align `Program.cs`)
- Check token scopes and org/repo names

`Failed to connect to Astra DB`:
- Verify `SecureConnectBundlePath` exists and is readable
- Verify `ApplicationToken` is valid
- Verify `Keyspace` exists

`Azure query/auth failures`:
- Verify `TenantId`, `ClientId`, `ClientSecret`
- Ensure service principal has workspace query permissions
- Verify `WorkspaceId`

`No rows being inserted`:
- Confirm Cassandra tables exist with expected column names/types
- Check worker logs for exceptions during collection or save

## Security Recommendations

- Do not commit secrets into `appsettings.json`
- Use environment variables or `dotnet user-secrets` for local development
- Rotate GitHub and Astra tokens regularly

## Quick Start Checklist

1. Install .NET 10 SDK
2. Create Cassandra keyspace/tables
3. Fill Azure/GitHub/Astra config
4. Confirm `GitHub` section name matches code binding
5. Run the worker and verify logs
