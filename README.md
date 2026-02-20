# Azure-GitHub Metrics Collector

A robust **.NET 10** background service designed to aggregate infrastructure performance from **Azure Insights** and developer activity from **GitHub**, persisting all data into a unified **Astra DB (Cassandra)** keyspace.

## 🚀 Architecture Overview

This worker service bridges the gap between DevOps and infrastructure monitoring. By polling disparate APIs and normalizing the data into a time-series database, it enables real-time dashboarding of both code velocity and application health.

-   **Runtime:** .NET 10 Worker Service
    
-   **Database:** DataStax Astra DB (Cassandra Query Language - CQL)
    
-   **Authentication:** Azure Service Principal (RBAC) & GitHub Personal Access Tokens (PAT)
    

## ✨ Key Features

-   **Unified Data Model:** Stores GitHub commits, PRs, and repository stats alongside Azure request durations and status codes.
    
-   **Power State Tracking:** Implements a `Status` field to track whether Azure resources are "Running" or "Stopped."
    
-   **Cassandra Optimized:** Uses a schema designed for high-volume time-series ingestion with partition-aware primary keys.
    
-   **Resilient Polling:** Built-in rate limiting and staggered delays to respect GitHub's API quotas.
    

## 🛠 Configuration

The service is configured via `appsettings.json`. Ensure the following blocks are populated:

JSON

```
{
  "GitHubConfig": {
    "Organization": "chasertx",
    "Token": "YOUR_GITHUB_PAT",
    "Repositories": [
      "SuperSecretProject",
      "AStatePlumbing",
      "pulse-backend"
    ]
  },
  "AzureConfig": {
    "WorkspaceId": "YOUR_LOG_ANALYTICS_WORKSPACE_ID",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  },
  "AstraDbConfig": {
    "SecureConnectBundlePath": "secure-connect-metrics.zip",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "Keyspace": "metrics_data"
  }
}

```

## 📊 Database Schema

To support efficient sorting by time and filtering by resource, the following schema is used in Astra DB:

SQL

```
-- Main metrics table with clustering order
CREATE TABLE metrics_data.azure_insights_metrics (
    resource_id text,
    timestamp timestamp,
    id uuid,
    metric_name text,
    value double,
    unit text,
    status text,
    dimensions map<text, text>,
    PRIMARY KEY (resource_id, timestamp)
) WITH CLUSTERING ORDER BY (timestamp DESC);