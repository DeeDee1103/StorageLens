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
