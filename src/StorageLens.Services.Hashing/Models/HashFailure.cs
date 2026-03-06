namespace StorageLens.Services.Hashing.Models;

public class HashFailure
{
    public Guid Id { get; set; }
    public Guid HashExecutionId { get; set; }
    public Guid FileRecordId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}
