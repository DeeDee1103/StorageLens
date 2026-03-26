# Technical Specification

## Technology Stack

- Runtime: .NET 8
- Architecture: Microservices with HTTP integration
- Data: SQL Server (service-owned data stores)
- Hosting: Docker Compose (local and baseline deployment)
- UI: Razor Pages (`StorageLens.Web`)

## Primary Functional Scope

- Manage storage locations
- Create and track scan jobs
- Enumerate and persist file metadata
- Compute hashes and duplicate groups
- Expose analytics and dashboard aggregates
- Keep location-level storage totals synchronized from authoritative file inventory aggregates

## Key Non-Functional Requirements

- Reliability through service isolation and retry patterns
- Observability via health endpoints and logs
- Extensibility for event-driven messaging integration
- Maintainability via shared contracts and bounded contexts

## Core Workflow

1. User initiates scan.
2. Orchestrator creates workflow and scan job.
3. Scanner ingests file metadata into FileInventory.
4. Hashing processes pending files.
5. Duplicates recalculates groups.
6. Analytics refreshes projections.
7. Analytics enriches per-location totals from FileInventory grouped stats and synchronizes totals to Locations metadata.
8. Scan job is marked terminal.
