# StorageLens Data Dictionary

## Purpose
This document describes the persisted data model used by StorageLens services.
It focuses on database entities, table mappings, key fields, and cross-service identifiers.

## Storage Model Overview
- Storage engine: SQL Server
- Pattern: one persisted schema/database context per stateful service
- Main cross-service keys: CorrelationId, ScanJobId, StorageLocationId
- Transport contracts in StorageLens.Shared.Contracts are API DTOs and are not persisted tables

## Schemas and Contexts

| Schema | DbContext | Project |
| --- | --- | --- |
| orchestration | OrchestratorDbContext | src/StorageLens.Services.Orchestrator |
| fileinventory | FileInventoryDbContext | src/StorageLens.Services.FileInventory |
| locations | LocationsDbContext | src/StorageLens.Services.Locations |
| duplicates | DuplicatesDbContext | src/StorageLens.Services.Duplicates |
| hashing | HashingDbContext | src/StorageLens.Services.Hashing |
| scanner | ScannerDbContext | src/StorageLens.Services.Scanner |
| scanjobs | ScanJobsDbContext | src/StorageLens.Services.ScanJobs |
| clients | ClientsDbContext | src/StorageLens.Web |

Note: `src/StorageLens.Services.Analytics` is stateless and does not own a persisted schema or DbContext. It aggregates data from other services via HTTP clients.

## Orchestration Schema

### Table: WorkflowInstance
- Primary key: Id (Guid)
- Important columns:
  - StorageLocationId (Guid)
  - ScanJobId (Guid)
  - CorrelationId (string, indexed)
  - RootPath (string)
  - Status (string)
  - CreatedAtUtc, CompletedAtUtc
  - RetryCount
  - LastError
- Relationships:
  - One-to-many to WorkflowStepExecution
  - One-to-many to WorkflowError

### Table: WorkflowStepExecution
- Primary key: Id (Guid)
- Foreign key: WorkflowInstanceId -> WorkflowInstance.Id
- Important columns:
  - StepName
  - Status
  - StartedAtUtc, CompletedAtUtc
  - Notes

### Table: WorkflowError
- Primary key: Id (Guid)
- Foreign key: WorkflowInstanceId -> WorkflowInstance.Id
- Important columns:
  - StepName
  - Message
  - OccurredAtUtc

## FileInventory Schema

### Table: FileRecords
- Primary key: Id (Guid)
- Important columns:
  - StorageLocationId (Guid)
  - ScanJobId (Guid, indexed)
  - CorrelationId (string, indexed)
  - LocationName
  - FileName (indexed)
  - FilePath (unique)
  - Extension (indexed)
  - SizeBytes
  - CreatedDate
  - LastAccessedDate
  - LastModifiedDate (indexed)
  - DiscoveryDate
  - ScanSourcePath
  - HashValue (indexed)
  - HashStatus
  - HashCalculatedDate
  - HashError
  - IsDuplicate
  - DuplicateGroupId

## Locations Schema

### Table: StorageLocations
- Primary key: Id (Guid)
- Important columns:
  - Name (indexed)
  - Path
  - Type
  - Status
  - LastScanDate
  - TotalSizeBytes (indexed)
  - FileCount
  - CreatedDate

## Duplicates Schema

### Table: DuplicateGroups
- Primary key: Id (Guid)
- Important columns:
  - ScanJobId (Guid)
  - CorrelationId (string, indexed)
  - HashValue
  - FileCount
  - TotalSizeBytes
  - WastedBytes (indexed)
  - ExampleFileName
  - LocationsCsv
  - RecalculatedAtUtc
- Constraints:
  - Unique composite key on (ScanJobId, HashValue)

## Hashing Schema

### Table: HashExecution
- Primary key: Id (Guid)
- Important columns:
  - ScanJobId (Guid, indexed)
  - CorrelationId (string, indexed)
  - Status
  - Attempted
  - Succeeded
  - Failed
  - StartedAtUtc, CompletedAtUtc

### Table: HashFailure
- Primary key: Id (Guid)
- Foreign key: HashExecutionId -> HashExecution.Id
- Important columns:
  - FileRecordId
  - FilePath
  - Error
  - OccurredAtUtc

## Scanner Schema

### Table: ScanExecution
- Primary key: Id (Guid)
- Important columns:
  - StorageLocationId (Guid)
  - ScanJobId (Guid, indexed)
  - CorrelationId (string, indexed)
  - RootPath
  - Status
  - StartedAtUtc, CompletedAtUtc
  - FilesDiscovered
  - ErrorCount
  - Notes

### Table: ScanError
- Primary key: Id (Guid)
- Foreign key: ScanExecutionId -> ScanExecution.Id
- Important columns:
  - Path
  - ErrorType
  - Message
  - OccurredAtUtc

## ScanJobs Schema

### Table: ScanJobs
- Primary key: Id (Guid)
- Important columns:
  - StorageLocationId (Guid)
  - LocationName
  - CorrelationId (string, indexed)
  - Status
  - CurrentStage
  - StartTime, EndTime
  - FilesScanned
  - FilesHashed
  - Notes

## Clients Schema (Web)

### Table: Clients
- Primary key: Id (Guid)
- Important columns:
  - AccountNumber (unique)
  - CompanyName (indexed)
  - Industry
  - Website
  - ContactFirstName, ContactLastName
  - ContactEmail, ContactPhone, ContactTitle
  - BillingAddress, City, State, PostalCode, Country
  - AssignedTierId
  - AssignedServiceIdsJson
  - NotificationEmails
  - NotifyOnScanComplete
  - NotifyOnScanFailed
  - NotifyOnDuplicatesFound
  - NotifyOnStorageThreshold
  - StorageThresholdPercent
  - ContractStartDate, ContractEndDate
  - Status (indexed)
  - Notes
  - CreatedAt, UpdatedAt

## Important Enums Used in Persisted Fields
- StorageLocationType: Nas, Local, FileShare
- LocationStatus: Online, Warning, Offline
- ScanJobStatus: Pending, Queued, Scanning, Hashing, DetectingDuplicates, RefreshingAnalytics, Completed, Failed, PartiallyCompleted, Running
- HashStatus: Pending, InProgress, Completed, Failed
- WorkflowStatus: Pending, Running, Completed, Failed, RetryQueued
- WorkflowStepStatus: Pending, Running, Completed, Failed

## Cross-Service Data Flow Keys
- CorrelationId: primary trace key across orchestration, scanning, hashing, duplicates, and file inventory.
- ScanJobId: links scan lifecycle records across services.
- StorageLocationId: links physical or logical source location across services.

## Source References
- Entity and DbContext definitions under src/StorageLens.Services.* and src/StorageLens.Web
- EF Core migrations under each service project Migrations folder
- Shared transport contracts under src/StorageLens.Shared.Contracts
