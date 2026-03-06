namespace StorageLens.Services.FileInventory.Models;

public class FileRecord
{
    public Guid Id { get; set; }
    public Guid StorageLocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset LastModifiedDate { get; set; }
    public string HashValue { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public Guid? DuplicateGroupId { get; set; }
}
