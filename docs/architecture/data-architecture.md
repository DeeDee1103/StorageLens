# Data Architecture

## Data Domains

- Location catalog (`Locations`)
- Workflow and execution state (`Orchestrator`, `ScanJobs`, `Scanner`, `Hashing`)
- File inventory metadata (`FileInventory`)
- Duplicate group intelligence (`Duplicates`)
- Aggregated analytical projections (`Analytics`)

## Data Flow Summary

1. A scan workflow starts with a location and correlation ID.
2. Scanner enumerates files and writes metadata via FileInventory API.
3. ScanJobs receives progress and status updates.
4. Hashing enriches inventory with deterministic hash values.
5. Duplicates computes grouped duplicate sets and optimization signals.
6. Analytics composes read models for dashboards and reports.

## Data Integrity Rules

- Service-level ownership and encapsulation
- API contracts enforce DTO consistency
- Correlation IDs associate records across services
- Workflow transitions define progression constraints

## Data Retention Considerations

- Retain scan job history for audit and trend reporting
- Archive old file snapshots based on business retention policies
- Snapshot key KPI outputs for product analytics continuity
