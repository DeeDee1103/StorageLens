using Microsoft.EntityFrameworkCore;
using StorageLens.Services.FileInventory.Data;
using StorageLens.Services.FileInventory.Interfaces;
using StorageLens.Services.FileInventory.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.FileInventory.Services;

public class FileInventoryService(FileInventoryDbContext dbContext) : IFileInventoryService
{
    public async Task<IReadOnlyList<FileRecordDto>> GetAllAsync(string? search, string? extension, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.FileRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.FileName.Contains(search) || x.FilePath.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(extension))
        {
            query = query.Where(x => x.Extension == extension);
        }

        var items = await query
            .OrderByDescending(x => x.SizeBytes)
            .Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
            .Take(Math.Clamp(pageSize, 1, 5000))
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<FileRecordDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.FileRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<FileRecordDto>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var items = await dbContext.FileRecords
            .Where(x => x.StorageLocationId == locationId)
            .OrderByDescending(x => x.LastModifiedDate)
            .Take(150)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<FileBatchIngestResultDto> UpsertBatchAsync(FileMetadataBatchIngestRequest request, CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
        {
            return new FileBatchIngestResultDto(0, 0, 0, 0);
        }

        var paths = request.Files.Select(x => x.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var existingByPath = await dbContext.FileRecords
            .Where(x => paths.Contains(x.FilePath))
            .ToDictionaryAsync(x => x.FilePath, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var inserted = 0;
        var updated = 0;

        foreach (var item in request.Files)
        {
            if (existingByPath.TryGetValue(item.FilePath, out var existing))
            {
                existing.StorageLocationId = item.StorageLocationId;
                existing.ScanJobId = item.ScanJobId;
                existing.CorrelationId = item.CorrelationId;
                existing.LocationName = item.LocationName;
                existing.FileName = item.FileName;
                existing.Extension = item.Extension;
                existing.SizeBytes = item.SizeBytes;
                existing.LastModifiedDate = item.LastModifiedDate;
                existing.CreatedDate = item.CreatedDate;
                existing.LastAccessedDate = item.LastAccessedDate;
                existing.DiscoveryDate = item.ScanTimestamp;
                existing.ScanSourcePath = item.ScanSourcePath;

                if (existing.HashStatus == HashStatus.Failed.ToString())
                {
                    existing.HashStatus = HashStatus.Pending.ToString();
                    existing.HashError = null;
                }

                updated++;
                continue;
            }

            var entity = new FileRecord
            {
                Id = item.Id ?? Guid.NewGuid(),
                StorageLocationId = item.StorageLocationId,
                ScanJobId = item.ScanJobId,
                CorrelationId = item.CorrelationId,
                LocationName = item.LocationName,
                FileName = item.FileName,
                FilePath = item.FilePath,
                Extension = item.Extension,
                SizeBytes = item.SizeBytes,
                CreatedDate = item.CreatedDate,
                LastAccessedDate = item.LastAccessedDate,
                DiscoveryDate = item.ScanTimestamp,
                ScanSourcePath = item.ScanSourcePath,
                LastModifiedDate = item.LastModifiedDate,
                HashValue = string.Empty,
                HashStatus = HashStatus.Pending.ToString(),
                IsDuplicate = false,
                DuplicateGroupId = null
            };

            dbContext.FileRecords.Add(entity);
            inserted++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new FileBatchIngestResultDto(request.Files.Count, inserted, updated, 0);
    }

    public async Task<IReadOnlyList<PendingHashFileDto>> GetPendingHashFilesAsync(Guid? scanJobId, int take, CancellationToken cancellationToken)
    {
        var query = dbContext.FileRecords
            .Where(x => x.HashStatus == HashStatus.Pending.ToString() || x.HashStatus == HashStatus.Failed.ToString());

        if (scanJobId.HasValue)
        {
            query = query.Where(x => x.ScanJobId == scanJobId.Value);
        }

        var items = await query
            .OrderBy(x => x.DiscoveryDate)
            .Take(Math.Clamp(take, 1, 5000))
            .ToListAsync(cancellationToken);

        return items.Select(x => new PendingHashFileDto(
            x.Id,
            x.StorageLocationId,
            x.ScanJobId,
            x.CorrelationId,
            x.FilePath,
            x.SizeBytes,
            x.LastModifiedDate)).ToList();
    }

    public async Task<bool> UpdateHashResultAsync(Guid id, FileHashResultRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.FileRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.CorrelationId = request.CorrelationId;
        entity.HashStatus = request.HashStatus;
        entity.HashValue = request.HashValue ?? string.Empty;
        entity.HashCalculatedDate = request.ProcessedAtUtc;
        entity.HashError = request.Error;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> MarkDuplicateFlagsByHashAsync(
        Guid scanJobId,
        string correlationId,
        IReadOnlySet<string> duplicateHashes,
        CancellationToken cancellationToken)
    {
        if (duplicateHashes.Count == 0)
        {
            return 0;
        }

        var records = await dbContext.FileRecords
            .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
            .ToListAsync(cancellationToken);

        var changed = 0;

        foreach (var record in records)
        {
            var duplicate = !string.IsNullOrWhiteSpace(record.HashValue) && duplicateHashes.Contains(record.HashValue);
            if (record.IsDuplicate != duplicate)
            {
                record.IsDuplicate = duplicate;
                changed++;
            }
        }

        if (changed > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }

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
