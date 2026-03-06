namespace StorageLens.Services.Duplicates.Models;

public class DuplicateGroup
{
    public Guid Id { get; set; }
    public string HashValue { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public long WastedBytes { get; set; }
    public string ExampleFileName { get; set; } = string.Empty;
    public string LocationsCsv { get; set; } = string.Empty;
}
