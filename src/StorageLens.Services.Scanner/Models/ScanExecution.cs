namespace StorageLens.Services.Scanner.Models;

public class ScanExecution
{
    public Guid Id { get; set; }
    public Guid StorageLocationId { get; set; }
    public Guid ScanJobId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public long FilesDiscovered { get; set; }
    public int ErrorCount { get; set; }
    public string Notes { get; set; } = string.Empty;
}
