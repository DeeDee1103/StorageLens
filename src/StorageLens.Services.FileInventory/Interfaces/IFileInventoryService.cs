using StorageLens.Shared.Contracts;

namespace StorageLens.Services.FileInventory.Interfaces;

public interface IFileInventoryService
{
    Task<IReadOnlyList<FileRecordDto>> GetAllAsync(string? search, string? extension, int page, int pageSize, CancellationToken cancellationToken);
    Task<FileRecordDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<FileRecordDto>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LocationStorageStatsDto>> GetLocationStorageStatsAsync(CancellationToken cancellationToken);
    Task<FileBatchIngestResultDto> UpsertBatchAsync(FileMetadataBatchIngestRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PendingHashFileDto>> GetPendingHashFilesAsync(Guid? scanJobId, int take, CancellationToken cancellationToken);
    Task<bool> UpdateHashResultAsync(Guid id, FileHashResultRequest request, CancellationToken cancellationToken);
    Task<int> MarkDuplicateFlagsByHashAsync(Guid scanJobId, string correlationId, IReadOnlySet<string> duplicateHashes, CancellationToken cancellationToken);
}
