# Service Contracts

## Contract Source

Cross-service DTOs are defined in domain-specific files under:
- `src/StorageLens.Shared.Contracts/`

> Note: For persisted entity mappings, table/column context, and cross-service data keys, see [Data Dictionary](../data-dictionary.md).

## Primary Contract Families

- Location contracts
  - `StorageLocationDto`, `CreateStorageLocationRequest`, `UpdateStorageLocationRequest`, `UpdateStorageLocationStatsRequest`
- Scan job contracts
  - `ScanJobDto`, `CreateScanJobRequest`, `UpdateScanJobStatusRequest`, `UpdateScanJobProgressRequest`
- File inventory contracts
  - `FileRecordDto`, `FileMetadataDto`, `FileMetadataBatchIngestRequest`, `PendingHashFileDto`, `FileHashResultRequest`, `LocationStorageStatsDto`
- Duplicate contracts
  - `DuplicateGroupDto`, `DuplicateSummaryDto`, `DuplicateRecalculationResultDto`, `MarkDuplicateHashesRequest`
- Workflow contracts
  - `ScannerExecuteRequest`, `ScannerExecutionDto`, `HashingRunResultDto`, `WorkflowInstanceDto`, `WorkflowStartResultDto`, `StartScanRequest`
- Analytics contracts
  - `DashboardDto`, `ReportsDto`, `DashboardKpiDto`, `ChartPointDto`

## Contract Governance

- Additive changes preferred for compatibility
- Contract versioning required for breaking changes
- Update docs and consumers in the same change set
