// Contracts have been split into domain-specific files:
//   LocationContracts.cs    — StorageLocationDto, CreateStorageLocationRequest, UpdateStorageLocationRequest, StorageLocationType, LocationStatus
//   ScanJobContracts.cs     — ScanJobDto, CreateScanJobRequest, UpdateScanJobStatusRequest, UpdateScanJobProgressRequest, ScanJobStatus
//   FileInventoryContracts.cs — FileRecordDto, FileMetadataDto, FileMetadataBatchIngestRequest, FileBatchIngestResultDto, PendingHashFileDto, FileHashResultRequest, MarkDuplicateHashesRequest, HashStatus
//   DuplicatesContracts.cs  — DuplicateGroupDto, DuplicateSummaryDto, DuplicateRecalculationResultDto
//   ScannerContracts.cs     — ScannerExecuteRequest, ScannerExecutionDto
//   HashingContracts.cs     — HashingRunResultDto
//   OrchestratorContracts.cs — WorkflowInstanceDto, WorkflowStepExecutionDto, WorkflowStartResultDto, WorkflowStatus
//   AnalyticsContracts.cs   — DashboardDto, DashboardKpiDto, ChartPointDto, ReportsDto
