# API Specification

## API Style

- RESTful HTTP APIs
- JSON request/response payloads
- Shared DTO contracts from `StorageLens.Shared.Contracts`

## Authentication and Headers

- Service endpoints require `X-API-Key` for authorized access.
- `X-Correlation-ID` is propagated across service calls for traceability.
- `Content-Type: application/json` is used for JSON payloads.

## Core Endpoint Catalog

> Related: See [Service Contracts](service-contracts.md) for DTO definitions and [Data Dictionary](../data-dictionary.md) for persisted entity reference.

## Orchestrator
- `POST /api/orchestrator/scans/start/{locationId}`
- `GET /api/orchestrator/workflows/{id}`
- `POST /api/orchestrator/workflows/{id}/resume`
- `POST /api/orchestrator/workflows/{id}/retry`

## Locations
- `GET /api/locations`
- `GET /api/locations/{id}`
- `POST /api/locations`
- `PUT /api/locations/{id}`

## ScanJobs
- `GET /api/scanjobs/recent`
- `GET /api/scanjobs/{id}`
- `POST /api/scanjobs`
- `PUT /api/scanjobs/{id}/status`
- `PUT /api/scanjobs/{id}/progress`

## FileInventory
- `GET /api/files`
- `GET /api/files/{id}`
- `GET /api/files/by-location/{locationId}`
- `POST /api/files/batch`
- `GET /api/files/pending-hash`
- `POST /api/files/{id}/hash-result`
- `POST /api/files/mark-duplicates/{scanJobId}`

## Scanner
- `POST /api/scanner/execute`
- `GET /api/scanner/executions/{id}`

## Hashing
- `POST /api/hashing/run/{scanJobId}?correlationId={id}`
- `GET /api/hashing/executions/{id}`

## Duplicates
- `GET /api/duplicates/summary`
- `GET /api/duplicates/groups`
- `GET /api/duplicates/groups/{id}`
- `POST /api/duplicates/recalculate/{scanJobId}?correlationId={id}`

## Analytics
- `GET /api/analytics/dashboard`
- `GET /api/analytics/reports`
- `GET /api/analytics/charts`

## Health
- `/health` is exposed by backend services.
