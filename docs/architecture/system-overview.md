# System Overview

## Platform Overview

StorageLens is a microservices-based .NET 8 platform that discovers, inventories, and analyzes enterprise file storage. It supports scan orchestration, metadata normalization, duplicate detection, and KPI reporting.

## Problem Statement

Organizations with large NAS and shared storage estates commonly face:
- low visibility into storage composition
- untracked duplicate data growth
- slow and manual scan operations
- fragmented ownership across operations and governance teams

StorageLens addresses this through a workflow-based architecture that captures scan progress, centralizes inventory metadata, and exposes analyzable APIs.

## Core Capabilities

- Storage location registration and lifecycle state tracking
- Scan job creation, stage progression, and progress updates
- File metadata ingestion in batches
- Hashing and duplicate classification workflows
- Aggregated analytics for dashboards and reports
- Live global storage distribution backed by FileInventory grouped location statistics
- Automatic synchronization of location totals into Locations metadata for consistent cross-page reporting
- Operational health endpoints for runtime diagnostics

## System Design Principles

- API-first service boundaries
- Independent service deployability
- Explicit data ownership per bounded context
- Observable workflow progression via correlation IDs
- Production-safe startup and retry behavior for infrastructure dependencies
- Structured logging with JSON output (Serilog) enriched with `ServiceName`, `Environment`, and `CorrelationId` on every log line
- Distributed tracing via OpenTelemetry with OTLP export (Jaeger in Docker, configurable OTLP endpoint in production)

## Observability Architecture

Every service in the platform shares a common observability stack configured via `StorageLens.Shared.Infrastructure`:

| Layer | Implementation | Notes |
|---|---|---|
| **Structured Logging** | Serilog (JSON to stdout) | `CorrelationId` pushed to every log scope automatically |
| **Distributed Tracing** | OpenTelemetry (OTLP exporter) | ASP.NET Core + HttpClient instrumented; `/health` excluded |
| **Correlation Propagation** | `X-Correlation-ID` header | Generated at ingress; forwarded on all upstream HTTP calls |
| **Local Trace UI** | Jaeger all-in-one (Docker) | Available at `http://localhost:16686` when running `docker-compose up` |
| **Production Exporter** | OTLP endpoint (configurable) | Set `Observability:OtlpEndpoint` to point at Jaeger/Tempo/ADOT |

## Microservices Approach

StorageLens decomposes capabilities by domain and process stage:
- Domain records are owned by dedicated services
- Workflow sequencing is coordinated by Orchestrator
- Data exchange occurs through HTTP contracts in `StorageLens.Shared.Contracts`
- Services remain independently scalable and operable
