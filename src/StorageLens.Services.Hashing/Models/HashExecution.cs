namespace StorageLens.Services.Hashing.Models;

public class HashExecution
{
    public Guid Id { get; set; }
    public Guid ScanJobId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int Attempted { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
