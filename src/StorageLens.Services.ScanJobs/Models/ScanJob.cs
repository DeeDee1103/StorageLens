using StorageLens.Shared.Contracts;

namespace StorageLens.Services.ScanJobs.Models;

public class ScanJob
{
    public Guid Id { get; set; }
    public Guid StorageLocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public ScanJobStatus Status { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public long FilesScanned { get; set; }
    public int ProgressPercent { get; set; }
    public string Notes { get; set; } = string.Empty;
}
