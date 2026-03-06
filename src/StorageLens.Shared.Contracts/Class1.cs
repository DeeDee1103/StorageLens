namespace StorageLens.Shared.Contracts;

public enum StorageLocationType
{
	Nas = 1,
	Local = 2,
	FileShare = 3
}

public enum LocationStatus
{
	Online = 1,
	Warning = 2,
	Offline = 3
}

public enum ScanJobStatus
{
	Pending = 1,
	Queued = 2,
	Scanning = 3,
	Hashing = 4,
	DetectingDuplicates = 5,
	RefreshingAnalytics = 6,
	Completed = 7,
	Failed = 8,
	PartiallyCompleted = 9,
	Running = 10
}

public enum HashStatus
{
	Pending = 1,
	InProgress = 2,
	Completed = 3,
	Failed = 4
}

public enum WorkflowStatus
{
	Pending = 1,
	Running = 2,
	Completed = 3,
	Failed = 4,
	RetryQueued = 5
}

public record StorageLocationDto(
	Guid Id,
	string Name,
	string Path,
	string Type,
	string Status,
	DateTimeOffset? LastScanDate,
	long TotalSizeBytes,
	long FileCount,
	DateTimeOffset CreatedDate);

public record CreateStorageLocationRequest(
	string Name,
	string Path,
	StorageLocationType Type);

public record UpdateStorageLocationRequest(
	string Name,
	string Path,
	StorageLocationType Type,
	LocationStatus Status);

public record ScanJobDto(
	Guid Id,
	Guid StorageLocationId,
	string LocationName,
	string Status,
	DateTimeOffset StartTime,
	DateTimeOffset? EndTime,
	long FilesScanned,
	int ProgressPercent,
	string Notes,
	string CurrentStage = "Pending",
	string CorrelationId = "",
	long FilesHashed = 0,
	int DuplicateGroupsFound = 0,
	int WarningCount = 0,
	int ErrorCount = 0);

public record CreateScanJobRequest(
	Guid StorageLocationId,
	string Notes,
	string? CorrelationId = null,
	string? LocationName = null);

public record UpdateScanJobStatusRequest(
	ScanJobStatus Status,
	string CurrentStage,
	string? Notes = null,
	bool IsTerminal = false);

public record UpdateScanJobProgressRequest(
	long FilesScanned,
	long FilesHashed,
	int ProgressPercent,
	int WarningCount,
	int ErrorCount,
	int DuplicateGroupsFound,
	string? Notes = null);

public record FileRecordDto(
	Guid Id,
	Guid StorageLocationId,
	string LocationName,
	string FileName,
	string FilePath,
	string Extension,
	long SizeBytes,
	DateTimeOffset LastModifiedDate,
	string HashValue,
	bool IsDuplicate,
	Guid? DuplicateGroupId,
	string HashStatus,
	Guid? ScanJobId = null,
	string CorrelationId = "",
	DateTimeOffset? HashCalculatedDate = null,
	DateTimeOffset? DiscoveryDate = null,
	string ScanSourcePath = "");

public record FileMetadataDto(
	Guid? Id,
	Guid StorageLocationId,
	Guid ScanJobId,
	string CorrelationId,
	string LocationName,
	string FileName,
	string FilePath,
	string Extension,
	long SizeBytes,
	DateTimeOffset LastModifiedDate,
	DateTimeOffset? CreatedDate,
	DateTimeOffset? LastAccessedDate,
	DateTimeOffset ScanTimestamp,
	string ScanSourcePath);

public record FileMetadataBatchIngestRequest(
	Guid StorageLocationId,
	Guid ScanJobId,
	string CorrelationId,
	string LocationName,
	string ScanSourcePath,
	IReadOnlyList<FileMetadataDto> Files);

public record FileBatchIngestResultDto(int ProcessedCount, int InsertedCount, int UpdatedCount, int ErrorCount);

public record PendingHashFileDto(
	Guid Id,
	Guid StorageLocationId,
	Guid ScanJobId,
	string CorrelationId,
	string FilePath,
	long SizeBytes,
	DateTimeOffset LastModifiedDate);

public record FileHashResultRequest(
	string CorrelationId,
	string HashStatus,
	string? HashValue,
	string? Error,
	DateTimeOffset ProcessedAtUtc);

public record DuplicateGroupDto(
	Guid Id,
	string HashValue,
	int FileCount,
	long TotalSizeBytes,
	long WastedBytes,
	string ExampleFileName,
	IReadOnlyList<string> Locations);

public record DuplicateSummaryDto(
	int DuplicateGroups,
	int DuplicateFiles,
	long WastedBytes,
	double DuplicatePercent);

public record DuplicateRecalculationResultDto(
	Guid ScanJobId,
	string CorrelationId,
	int DuplicateGroups,
	int DuplicateFiles,
	long WastedBytes,
	DateTimeOffset RecalculatedAtUtc);

public record MarkDuplicateHashesRequest(Guid ScanJobId, string CorrelationId, IReadOnlyList<string> Hashes);

public record ScannerExecuteRequest(
	Guid StorageLocationId,
	Guid ScanJobId,
	string CorrelationId,
	string LocationName,
	string RootPath,
	int BatchSize = 250,
	int ProgressUpdateInterval = 200,
	bool IncludeHidden = false);

public record ScannerExecutionDto(
	Guid Id,
	Guid StorageLocationId,
	Guid ScanJobId,
	string CorrelationId,
	string RootPath,
	string Status,
	DateTimeOffset StartedAtUtc,
	DateTimeOffset? CompletedAtUtc,
	long FilesDiscovered,
	int ErrorCount,
	string Notes);

public record HashingRunResultDto(
	Guid Id,
	Guid ScanJobId,
	string CorrelationId,
	int Attempted,
	int Succeeded,
	int Failed,
	DateTimeOffset StartedAtUtc,
	DateTimeOffset CompletedAtUtc);

public record WorkflowStepExecutionDto(
	string StepName,
	string Status,
	DateTimeOffset StartedAtUtc,
	DateTimeOffset? CompletedAtUtc,
	string Notes);

public record WorkflowInstanceDto(
	Guid Id,
	Guid StorageLocationId,
	Guid ScanJobId,
	string CorrelationId,
	string RootPath,
	string Status,
	DateTimeOffset CreatedAtUtc,
	DateTimeOffset? CompletedAtUtc,
	int RetryCount,
	string LastError,
	IReadOnlyList<WorkflowStepExecutionDto> Steps);

public record WorkflowStartResultDto(Guid WorkflowId, Guid ScanJobId, string CorrelationId, string Status);

public record DashboardKpiDto(
	long TotalStorageBytes,
	long TotalFiles,
	int DuplicatePercent,
	long PotentialSavingsBytes);

public record ChartPointDto(string Label, double Value);

public record DashboardDto(
	DashboardKpiDto Kpis,
	IReadOnlyList<ChartPointDto> StorageGrowth,
	IReadOnlyList<ChartPointDto> FileTypeDistribution,
	IReadOnlyList<ChartPointDto> LargestLocations,
	IReadOnlyList<ScanJobDto> RecentJobs,
	IReadOnlyList<DuplicateGroupDto> TopDuplicateGroups,
	IReadOnlyList<StorageLocationDto> Locations);

public record ReportsDto(
	IReadOnlyList<FileRecordDto> OldestFiles,
	IReadOnlyList<FileRecordDto> LargestFiles,
	IReadOnlyList<ChartPointDto> MostCommonFileTypes,
	IReadOnlyList<ChartPointDto> BiggestStorageConsumers,
	IReadOnlyList<ChartPointDto> DuplicateHeavyLocations);
