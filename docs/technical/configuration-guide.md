# Configuration Guide

## Configuration Sources

- `appsettings.json`
- `appsettings.Development.json`
- Environment variables (container/runtime overrides)
- .NET user-secrets (`secrets.json` in the user profile, for local development)

## Standard Configuration Areas

- `ConnectionStrings`
- `Database:UseInMemory`
- `UpstreamServices`
- Service-specific workers/options (for example `HashingWorker`)
- Logging levels

## Docker Compose Variables

Compose injects critical values for:
- SQL connection strings per service database
- Inter-service HTTP endpoint base URLs
- ASP.NET Core URL bindings

Compose supports an optional global Azure SQL override variable:

- `STORAGELENS_AZURE_SQL_CONNECTION_STRING`

When this variable is set in `.env`, all service `ConnectionStrings__*Db` values use that Azure SQL connection string.
When it is not set, services fall back to local container SQL connection strings (`Server=sqlserver,1433;...`).

## Security Recommendations

- Do not commit credentials to `appsettings*.json` or source-controlled files
- Store local development secrets in .NET user-secrets
- Use `.env` for Docker Compose secrets and keep `.env` out of source control
- Use per-environment configuration sets
- Restrict admin credentials and rotate regularly
