# Service Contracts

## Contract Source

All cross-service DTOs are defined in:
- `src/StorageLens.Shared.Contracts/Class1.cs`

## Primary Contract Families

- Location contracts
  - `StorageLocationDto`, `CreateStorageLocationRequest`, `UpdateStorageLocationRequest`
- Scan job contracts
  - `ScanJobDto`, `CreateScanJobRequest`, `UpdateScanJobStatusRequest`, `UpdateScanJobProgressRequest`
- File inventory contracts
  - `FileRecordDto`, `FileMetadataDto`, `FileMetadataBatchIngestRequest`, `PendingHashFileDto`, `FileHashResultRequest`
- Duplicate contracts
  - `DuplicateGroupDto`, `DuplicateSummaryDto`, `DuplicateRecalculationResultDto`, `MarkDuplicateHashesRequest`
- Workflow contracts
  - `ScannerExecuteRequest`, `ScannerExecutionDto`, `HashingRunResultDto`, `WorkflowInstanceDto`, `WorkflowStartResultDto`
- Analytics contracts
  - `DashboardDto`, `ReportsDto`, `DashboardKpiDto`, `ChartPointDto`

## Contract Governance

- Additive changes preferred for compatibility
- Contract versioning required for breaking changes
- Update docs and consumers in the same change set
