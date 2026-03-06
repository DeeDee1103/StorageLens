namespace StorageLens.Services.Scanner.Models;

public class ScanError
{
    public Guid Id { get; set; }
    public Guid ScanExecutionId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}
