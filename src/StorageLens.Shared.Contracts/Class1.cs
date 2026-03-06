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
	Running = 2,
	Completed = 3,
	Failed = 4
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
	string Notes);

public record CreateScanJobRequest(Guid StorageLocationId, string Notes);

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
	string HashStatus);

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
