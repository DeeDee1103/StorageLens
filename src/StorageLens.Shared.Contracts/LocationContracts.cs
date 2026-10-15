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
    DateTimeOffset CreatedDate,
    string SiteLocation,
    double? Latitude,
    double? Longitude);

public record CreateStorageLocationRequest(
    string Name,
    string Path,
    StorageLocationType Type,
    string SiteLocation,
    LocationStatus Status,
    double? Latitude = null,
    double? Longitude = null);

public record UpdateStorageLocationRequest(
    string Name,
    string Path,
    StorageLocationType Type,
    LocationStatus Status,
    string SiteLocation,
    double? Latitude,
    double? Longitude);

public record UpdateStorageLocationStatsRequest(
    long TotalSizeBytes,
    long FileCount,
    DateTimeOffset? LastScanDate = null);
