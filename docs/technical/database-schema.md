# Database Schema

## Schema Ownership Model

StorageLens follows service-owned data boundaries. Each service has independent schema/database ownership and is the sole writer for its domain entities.

> Detailed reference: For entity-level fields, keys, and relationships, see [Data Dictionary](../data-dictionary.md).

## Domain Schemas

- `locations`
  - Storage location entities, lifecycle status, and synchronized totals (`TotalSizeBytes`, `FileCount`)
- `scanjobs`
  - Scan jobs, status, progress counters, correlation identifiers
- `fileinventory`
  - File records, hash state, duplicate markers, source path context
- `duplicates`
  - Duplicate groups and summary basis records
- `scanner`
  - Scan execution logs and scan errors
- `hashing`
  - Hash execution records and hash failure diagnostics
- `orchestration`
  - Workflow instances, step execution records, workflow errors

## Data Modeling Principles

- Strong keys (`Guid`) for distributed-safe identity
- Correlation IDs for cross-service traceability
- Explicit status fields to model state transitions
- Separate execution telemetry from primary domain records
- Cross-service synchronization via APIs only; no direct database writes across service boundaries

## Operational Guidance

- Keep schema changes backward compatible across service versions
- Use migrations for production schema evolution
- Preserve historical execution data for audit and support analytics
