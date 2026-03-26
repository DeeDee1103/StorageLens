namespace StorageLens.Shared.Contracts;

public enum HashStatus
{
	Pending = 1,
	InProgress = 2,
	Completed = 3,
	Failed = 4
}

public record FileRecordDto(
	Guid Id,
	Guid StorageLocationId,
	string LocationName,
	string FileName,
	string FilePath,
	string Extension,
	long SizeBytes,
	DateTimeOffset LastModifiedDate,
	string HashValue,
	bool IsDuplicate,
	Guid? DuplicateGroupId,
	string HashStatus,
	Guid? ScanJobId = null,
	string CorrelationId = "",
	DateTimeOffset? HashCalculatedDate = null,
	DateTimeOffset? DiscoveryDate = null,
	string ScanSourcePath = "");

public record FileMetadataDto(
	Guid? Id,
	Guid StorageLocationId,
	Guid ScanJobId,
	string CorrelationId,
	string LocationName,
	string FileName,
	string FilePath,
	string Extension,
	long SizeBytes,
	DateTimeOffset LastModifiedDate,
	DateTimeOffset? CreatedDate,
	DateTimeOffset? LastAccessedDate,
	DateTimeOffset ScanTimestamp,
	string ScanSourcePath);

public record FileMetadataBatchIngestRequest(
	Guid StorageLocationId,
	Guid ScanJobId,
	string CorrelationId,
	string LocationName,
	string ScanSourcePath,
	IReadOnlyList<FileMetadataDto> Files);

public record FileBatchIngestResultDto(int ProcessedCount, int InsertedCount, int UpdatedCount, int ErrorCount);

public record PendingHashFileDto(
	Guid Id,
	Guid StorageLocationId,
	Guid ScanJobId,
	string CorrelationId,
	string FilePath,
	long SizeBytes,
	DateTimeOffset LastModifiedDate);

public record FileHashResultRequest(
	string CorrelationId,
	string HashStatus,
	string? HashValue,
	string? Error,
	DateTimeOffset ProcessedAtUtc);

public record MarkDuplicateHashesRequest(Guid ScanJobId, string CorrelationId, IReadOnlyList<string> Hashes);

public record LocationStorageStatsDto(
	Guid StorageLocationId,
	long TotalSizeBytes,
	long FileCount);
