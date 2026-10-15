# Deployment Guide

## Deployment Models

- Local/dev: Docker Compose
- Enterprise: Container platform (AKS/EKS/GKE or equivalent)

## Pre-Deployment Checklist

- Validate service images build successfully
- Validate SQL connectivity and credentials
- Confirm service endpoint routing and DNS
- Confirm health endpoints and startup ordering
- Confirm observability resources are provisioned (Log Analytics + Application Insights)
- Set the required secrets (see below) before provisioning

## Required Secrets

Services fail closed: outside the `Development` environment, any request is rejected with `401` when `ServiceApiKey` is missing. Every service, including Web, must get the same key.

| Setting | Azure (azd) | Docker Compose (`.env`) | Notes |
|---|---|---|---|
| `ServiceApiKey` | `azd env set STORAGELENS_SERVICE_API_KEY <random, 32+ chars>` | `STORAGELENS_SERVICE_API_KEY` | Required. Stored as a Container Apps secret. |
| `Pricing:OwnerPin` | `azd env set STORAGELENS_OWNER_PIN <pin>` | `STORAGELENS_OWNER_PIN` | Optional. Unlocks Pricing Admin and Client Management. If empty, those pages stay locked. |

For local `dotnet run`, use user-secrets (for example `dotnet user-secrets set "Pricing:OwnerPin" "<pin>" --project src/StorageLens.Web`). No PIN or key is committed to `appsettings.json`.

Owner mode is a Data Protection-encrypted cookie that expires after 8 hours. Data Protection keys are per instance by default, so with more than one Web replica a user may be asked for the PIN again. The Orchestrator is pinned to one replica because its queue lives in memory and it recovers interrupted workflows at startup.

## Deployment Sequence

1. Provision data infrastructure (SQL, messaging)
2. Deploy domain services (`Locations`, `ScanJobs`, `FileInventory`)
3. Deploy workflow services (`Scanner`, `Hashing`, `Orchestrator`)
4. Deploy analytical and support services (`Duplicates`, `Analytics`)
5. Deploy `StorageLens.Web`
6. Run health and smoke checks

## Post-Deployment Validation

- All `/health` endpoints return `200`
- Scan workflow completes in target environment
- Dashboard and duplicate summary return expected data
- FileInventory grouped stats endpoint returns data: `GET /api/files/stats/by-location`
- Locations stats update endpoint accepts updates from analytics: `PUT /api/locations/{id}/stats`
- Global View storage tier values match authoritative location totals (no persistent `0 B` values for active scanned locations)
