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
