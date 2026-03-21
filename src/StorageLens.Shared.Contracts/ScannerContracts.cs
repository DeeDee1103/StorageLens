namespace StorageLens.Shared.Contracts;

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
