namespace StorageLens.Services.FileInventory.Models;

public class FileRecord
{
    public Guid Id { get; set; }
    public Guid StorageLocationId { get; set; }
    public Guid ScanJobId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset? CreatedDate { get; set; }
    public DateTimeOffset? LastAccessedDate { get; set; }
    public DateTimeOffset DiscoveryDate { get; set; }
    public string ScanSourcePath { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedDate { get; set; }
    public string HashValue { get; set; } = string.Empty;
    public string HashStatus { get; set; } = "Pending";
    public DateTimeOffset? HashCalculatedDate { get; set; }
    public string? HashError { get; set; }
    public bool IsDuplicate { get; set; }
    public Guid? DuplicateGroupId { get; set; }
}
