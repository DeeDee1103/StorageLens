# Glossary of Terms - Technical

## Purpose
This glossary defines technical terminology used across StorageLens services, data flows, and operations.

## A

### Aggregation
A read pattern where one service composes results from multiple upstream services into a unified response.

### API Contract
A shared request/response definition (DTOs and enums) used by services and clients.

### API Key
A shared secret value sent in `X-API-Key` for service authorization.

### Audit Log
A structured log event that records a security-relevant or workflow-significant action with context.

## B

### Bounded Context
A service-owned domain boundary with isolated data model, behavior, and persistence ownership.

## C

### Correlation ID
A request-trace identifier propagated across service boundaries, commonly through `X-Correlation-ID`.

### Circuit Breaker
A resilience pattern that temporarily blocks outbound calls to an unhealthy upstream dependency.

## D

### DashboardDto
Analytics contract type that aggregates KPI and chart/report payloads for UI rendering.

### DbContext
Entity Framework Core data context that maps entities to tables and controls persistence behavior.

### Domain Entity
A persistence model class representing business state in a service-owned schema.

### DuplicateGroup
A persisted aggregation of files sharing the same hash value for a specific scan context.

## E

### Entity Relationship
A foreign key or navigational association between persisted entities.

### Endpoint
A routable HTTP API path exposed by a service (for example `/api/locations` or `/health`).

## F

### FileRecord
Primary persisted file-metadata entity in FileInventory.

### Foreign Key (FK)
A relational column constraint referencing another table's primary key.

## H

### HashExecution
Persisted record of a hashing run with counters and execution status.

### HashFailure
Persisted error record describing a failed hash operation for a file.

### HashStatus
Enum-like state marker for file hashing progress (Pending, InProgress, Completed, Failed).

### Health Endpoint
Operational endpoint used for liveness/readiness checks.

## I

### Idempotency
Property where repeated execution of a request produces the same effective result.

### Ingest
Process of submitting discovered file metadata into FileInventory.

## K

### KPI
A computed metric surfaced through analytics responses and UI dashboards.

## L

### Locations Service
Service responsible for storage source registration, classification, and health status metadata.

## M

### Migration
Versioned EF Core schema change artifact used to evolve database structure safely.

### Microservice
Independently deployable service with a focused domain responsibility and owned persistence boundary.

## O

### Observability
Operational visibility across logs, traces, metrics, and health data.

### Orchestrator Service
Workflow coordinator that sequences scanner, hashing, duplicates, and analytics refresh stages.

## P

### Primary Key (PK)
A unique table identifier column, typically a Guid in StorageLens schemas.

### Projection
A shaped subset of data returned by queries or DTO mapping logic.

## R

### Rate Limiting
Traffic-control middleware that caps request rates over a time window.

### RetryQueued
Workflow status indicating deferred retry scheduling for a failed workflow path.

## S

### Scan Execution (code: ScanExecution)
Persisted scanner-run entity capturing discovery counts, timing, and error metrics.

### Scan Job (code: ScanJob)
Persisted lifecycle aggregate representing a single location scan run.

### Scan Job Status (code: ScanJobStatus)
Status model for scan jobs (Pending, Queued, Scanning, Hashing, DetectingDuplicates, RefreshingAnalytics, Completed, Failed, PartiallyCompleted, Running).

### Schema Ownership
Principle that each service is the sole writer to its own schema/database context.

### Service-to-Service Call
HTTP call between backend services using shared contracts and security headers.

### Stateless Service
Service without owned persisted schema; computes responses from upstream systems.

### Structured Logging
Machine-parsable logging with stable fields for filtering and analysis.

## T

### Trace
A distributed record of request flow across multiple services.

### Transport DTO
Non-persisted contract object used to transfer data across service boundaries.

## U

### Upstream Dependency
External or internal service that a component calls to fulfill a request.

## W

### Workflow Error (code: WorkflowError)
Persisted orchestrator error record tied to workflow instance and step context.

### Workflow Instance (code: WorkflowInstance)
Persisted orchestrator aggregate tracking end-to-end execution state for a run.

### Workflow Step Execution (code: WorkflowStepExecution)
Persisted step-level execution record for each workflow stage.

### Workflow Status (code: WorkflowStatus)
State model for orchestrator workflows (Pending, Running, Completed, Failed, RetryQueued).

## Related References
- Data model reference: [Data Dictionary](data-dictionary.md)
- API and contract overview: [API Specification](technical/api-specification.md)
- Shared DTOs: [Service Contracts](technical/service-contracts.md)
- Naming standards: [Terminology Consistency Matrix](technical/terminology-consistency-matrix.md)
