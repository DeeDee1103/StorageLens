using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Locations.Models;

public class StorageLocation
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public StorageLocationType Type { get; set; }
    public LocationStatus Status { get; set; }
    public DateTimeOffset? LastScanDate { get; set; }
    public long TotalSizeBytes { get; set; }
    public long FileCount { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
}
