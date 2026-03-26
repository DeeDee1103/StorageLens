using Microsoft.EntityFrameworkCore;
using StorageLens.Services.FileInventory.Data;
using StorageLens.Services.FileInventory.Interfaces;
using StorageLens.Services.FileInventory.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.FileInventory.Services;

public class FileInventoryService(FileInventoryDbContext dbContext) : IFileInventoryService
{
    private const int ExistingPathQueryChunkSize = 1000;

    public async Task<IReadOnlyList<FileRecordDto>> GetAllAsync(string? search, string? extension, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.FileRecords.AsNoTracking().AsQueryable();

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
            .Select(x => new FileRecordDto(
                x.Id,
                x.StorageLocationId,
                x.LocationName,
                x.FileName,
                x.FilePath,
                x.Extension,
                x.SizeBytes,
                x.LastModifiedDate,
                x.HashValue,
                x.IsDuplicate,
                x.DuplicateGroupId,
                x.HashStatus,
                x.ScanJobId,
                x.CorrelationId,
                x.HashCalculatedDate,
                x.DiscoveryDate,
                x.ScanSourcePath))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<FileRecordDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.FileRecords
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new FileRecordDto(
                x.Id,
                x.StorageLocationId,
                x.LocationName,
                x.FileName,
                x.FilePath,
                x.Extension,
                x.SizeBytes,
                x.LastModifiedDate,
                x.HashValue,
                x.IsDuplicate,
                x.DuplicateGroupId,
                x.HashStatus,
                x.ScanJobId,
                x.CorrelationId,
                x.HashCalculatedDate,
                x.DiscoveryDate,
                x.ScanSourcePath))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileRecordDto>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var items = await dbContext.FileRecords
            .AsNoTracking()
            .Where(x => x.StorageLocationId == locationId)
            .OrderByDescending(x => x.LastModifiedDate)
            .Take(150)
            .Select(x => new FileRecordDto(
                x.Id,
                x.StorageLocationId,
                x.LocationName,
                x.FileName,
                x.FilePath,
                x.Extension,
                x.SizeBytes,
                x.LastModifiedDate,
                x.HashValue,
                x.IsDuplicate,
                x.DuplicateGroupId,
                x.HashStatus,
                x.ScanJobId,
                x.CorrelationId,
                x.HashCalculatedDate,
                x.DiscoveryDate,
                x.ScanSourcePath))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<IReadOnlyList<LocationStorageStatsDto>> GetLocationStorageStatsAsync(CancellationToken cancellationToken)
    {
        var stats = await dbContext.FileRecords
            .AsNoTracking()
            .GroupBy(x => x.StorageLocationId)
            .Select(g => new LocationStorageStatsDto(
                g.Key,
                g.Sum(x => x.SizeBytes),
                g.LongCount()))
            .ToListAsync(cancellationToken);

        return stats;
    }

    public async Task<FileBatchIngestResultDto> UpsertBatchAsync(FileMetadataBatchIngestRequest request, CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
        {
            return new FileBatchIngestResultDto(0, 0, 0, 0);
        }

        var filePaths = request.Files.Select(x => x.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var existingByPath = await GetExistingByPathAsync(filePaths, cancellationToken);

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
            .AsNoTracking()
            .Where(x => x.HashStatus == HashStatus.Pending.ToString() || x.HashStatus == HashStatus.Failed.ToString());

        if (scanJobId.HasValue)
        {
            query = query.Where(x => x.ScanJobId == scanJobId.Value);
        }

        var items = await query
            .OrderBy(x => x.DiscoveryDate)
            .Take(Math.Clamp(take, 1, 5000))
            .Select(x => new PendingHashFileDto(
                x.Id,
                x.StorageLocationId,
                x.ScanJobId,
                x.CorrelationId,
                x.FilePath,
                x.SizeBytes,
                x.LastModifiedDate))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<bool> UpdateHashResultAsync(Guid id, FileHashResultRequest request, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsRelational())
        {
            var rows = await dbContext.FileRecords
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(v => v.CorrelationId, request.CorrelationId)
                    .SetProperty(v => v.HashStatus, request.HashStatus)
                    .SetProperty(v => v.HashValue, request.HashValue ?? string.Empty)
                    .SetProperty(v => v.HashCalculatedDate, request.ProcessedAtUtc)
                    .SetProperty(v => v.HashError, request.Error), cancellationToken);

            return rows > 0;
        }

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

        var duplicateHashValues = duplicateHashes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (duplicateHashValues.Count == 0)
        {
            return 0;
        }

        if (dbContext.Database.IsRelational())
        {
            var shouldBeDuplicate = dbContext.FileRecords.Where(x =>
                x.ScanJobId == scanJobId &&
                x.CorrelationId == correlationId &&
                !string.IsNullOrWhiteSpace(x.HashValue) &&
                duplicateHashValues.Contains(x.HashValue));

            var shouldBeUnique = dbContext.FileRecords.Where(x =>
                x.ScanJobId == scanJobId &&
                x.CorrelationId == correlationId &&
                (string.IsNullOrWhiteSpace(x.HashValue) || !duplicateHashValues.Contains(x.HashValue)) &&
                x.IsDuplicate);

            var setTrueCount = await shouldBeDuplicate
                .Where(x => !x.IsDuplicate)
                .ExecuteUpdateAsync(x => x.SetProperty(v => v.IsDuplicate, true), cancellationToken);

            var setFalseCount = await shouldBeUnique
                .ExecuteUpdateAsync(x => x.SetProperty(v => v.IsDuplicate, false), cancellationToken);

            return setTrueCount + setFalseCount;
        }

        var records = await dbContext.FileRecords
            .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
            .ToListAsync(cancellationToken);

        var modifiedCount = 0;

        foreach (var record in records)
        {
            var isDuplicate = !string.IsNullOrWhiteSpace(record.HashValue) && duplicateHashes.Contains(record.HashValue);
            if (record.IsDuplicate != isDuplicate)
            {
                record.IsDuplicate = isDuplicate;
                modifiedCount++;
            }
        }

        if (modifiedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return modifiedCount;
    }

    private async Task<Dictionary<string, FileRecord>> GetExistingByPathAsync(
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken)
    {
        var existingByPath = new Dictionary<string, FileRecord>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < filePaths.Count; index += ExistingPathQueryChunkSize)
        {
            var chunk = filePaths
                .Skip(index)
                .Take(ExistingPathQueryChunkSize)
                .ToList();

            var matches = await dbContext.FileRecords
                .Where(x => chunk.Contains(x.FilePath))
                .ToListAsync(cancellationToken);

            foreach (var match in matches)
            {
                existingByPath[match.FilePath] = match;
            }
        }

        return existingByPath;
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
