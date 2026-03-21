namespace StorageLens.Shared.Contracts;

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
