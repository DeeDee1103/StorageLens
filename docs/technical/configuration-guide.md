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

## Security Recommendations

- Do not commit credentials to `appsettings*.json` or source-controlled files
- Store local development secrets in .NET user-secrets
- Use `.env` for Docker Compose secrets and keep `.env` out of source control
- Use per-environment configuration sets
- Restrict admin credentials and rotate regularly
