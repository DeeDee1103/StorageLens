using StorageLens.Services.FileInventory.Interfaces;
using StorageLens.Services.FileInventory.Models;
using StorageLens.Services.FileInventory.Repositories;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.FileInventory.Services;

public class FileInventoryService(IFileInventoryRepository repository) : IFileInventoryService
{
    public async Task<IReadOnlyList<FileRecordDto>> GetAllAsync(
        string? search, string? extension, int page, int pageSize, CancellationToken cancellationToken)
    {
        var records = await repository.GetPagedAsync(search, extension, page, pageSize, cancellationToken);
        return records.Select(Map).ToList();
    }

    public async Task<FileRecordDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(id, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<IReadOnlyList<FileRecordDto>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var records = await repository.GetByLocationAsync(locationId, 150, cancellationToken);
        return records.Select(Map).ToList();
    }

    public Task<IReadOnlyList<LocationStorageStatsDto>> GetLocationStorageStatsAsync(CancellationToken cancellationToken) =>
        repository.GetLocationStorageStatsAsync(cancellationToken);

    public async Task<FileBatchIngestResultDto> UpsertBatchAsync(
        FileMetadataBatchIngestRequest request, CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
        {
            return new FileBatchIngestResultDto(0, 0, 0, 0);
        }

        var (inserted, updated) = await repository.UpsertBatchAsync(request, cancellationToken);
        return new FileBatchIngestResultDto(request.Files.Count, inserted, updated, 0);
    }

    public Task<IReadOnlyList<PendingHashFileDto>> GetPendingHashFilesAsync(
        Guid? scanJobId, int take, CancellationToken cancellationToken) =>
        repository.GetPendingHashFilesAsync(scanJobId, take, cancellationToken);

    public Task<bool> UpdateHashResultAsync(Guid id, FileHashResultRequest request, CancellationToken cancellationToken) =>
        repository.UpdateHashResultAsync(id, request, cancellationToken);

    public Task<int> MarkDuplicateFlagsByHashAsync(
        Guid scanJobId, string correlationId, IReadOnlySet<string> duplicateHashes, CancellationToken cancellationToken) =>
        repository.MarkDuplicateFlagsByHashAsync(scanJobId, correlationId, duplicateHashes, cancellationToken);

    public Task<int> DeleteByScanJobAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken) =>
        repository.DeleteByScanJobAsync(scanJobId, correlationId, cancellationToken);

    private static FileRecordDto Map(FileRecord entity) =>
        new(
            entity.Id,
            entity.StorageLocationId,
            entity.LocationName,
            entity.FileName,
            entity.FilePath,
            entity.Extension,
            entity.SizeBytes,
            entity.LastModifiedDate,
            entity.HashValue,
            entity.IsDuplicate,
            entity.DuplicateGroupId,
            entity.HashStatus,
            entity.ScanJobId,
            entity.CorrelationId,
            entity.HashCalculatedDate,
            entity.DiscoveryDate,
            entity.ScanSourcePath);
}
