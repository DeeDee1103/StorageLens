using StorageLens.Shared.Contracts;

namespace StorageLens.Services.ScanJobs.Models;

public class ScanJob
{
    public Guid Id { get; set; }
    public Guid StorageLocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public ScanJobStatus Status { get; set; }
    public string CurrentStage { get; set; } = "Pending";
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public long FilesScanned { get; set; }
    public long FilesHashed { get; set; }
    public int DuplicateGroupsFound { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public int ProgressPercent { get; set; }
    public string Notes { get; set; } = string.Empty;
}
