# Configuration Guide

## Configuration Sources

- `appsettings.json`
- `appsettings.Development.json`
- Environment variables (container/runtime overrides)

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

- Replace plain-text secrets with secret manager integration
- Use per-environment configuration sets
- Restrict admin credentials and rotate regularly
