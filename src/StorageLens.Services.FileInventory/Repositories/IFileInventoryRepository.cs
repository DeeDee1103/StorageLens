using StorageLens.Services.FileInventory.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.FileInventory.Repositories;

/// <summary>
/// Abstracts all EF Core data-access operations for the FileInventory service,
/// allowing <see cref="Services.FileInventoryService"/> to be tested without an EF Core dependency.
/// </summary>
public interface IFileInventoryRepository
{
    Task<IReadOnlyList<FileRecord>> GetPagedAsync(string? search, string? extension, int page, int pageSize, CancellationToken cancellationToken);
    Task<FileRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<FileRecord>> GetByLocationAsync(Guid locationId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<LocationStorageStatsDto>> GetLocationStorageStatsAsync(CancellationToken cancellationToken);
    Task<(int Inserted, int Updated)> UpsertBatchAsync(FileMetadataBatchIngestRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PendingHashFileDto>> GetPendingHashFilesAsync(Guid? scanJobId, int take, bool includeFailed, CancellationToken cancellationToken);
    Task<bool> UpdateHashResultAsync(Guid id, FileHashResultRequest request, CancellationToken cancellationToken);
    Task<int> MarkDuplicateFlagsByHashAsync(Guid scanJobId, string correlationId, IReadOnlySet<string> duplicateHashes, CancellationToken cancellationToken);
    Task<int> DeleteByScanJobAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken);
}
