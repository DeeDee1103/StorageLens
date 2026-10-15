using Microsoft.EntityFrameworkCore;
using StorageLens.Services.FileInventory.Data;
using StorageLens.Services.FileInventory.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.FileInventory.Repositories;

public class FileInventoryRepository(FileInventoryDbContext dbContext) : IFileInventoryRepository
{
    private const int ExistingPathQueryChunkSize = 1000;

    public async Task<IReadOnlyList<FileRecord>> GetPagedAsync(
        string? search, string? extension, int page, int pageSize, CancellationToken cancellationToken)
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

        return await query
            .OrderByDescending(x => x.SizeBytes)
            .Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
            .Take(Math.Clamp(pageSize, 1, 5000))
            .ToListAsync(cancellationToken);
    }

    public Task<FileRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.FileRecords
            .AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileRecord>> GetByLocationAsync(
        Guid locationId, int take, CancellationToken cancellationToken)
    {
        return await dbContext.FileRecords
            .AsNoTracking()
            .Where(x => x.StorageLocationId == locationId)
            .OrderByDescending(x => x.LastModifiedDate)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocationStorageStatsDto>> GetLocationStorageStatsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.FileRecords
            .AsNoTracking()
            .GroupBy(x => x.StorageLocationId)
            .Select(g => new LocationStorageStatsDto(
                g.Key,
                g.Sum(x => x.SizeBytes),
                g.LongCount()))
            .ToListAsync(cancellationToken);
    }

    public async Task<(int Inserted, int Updated)> UpsertBatchAsync(
        FileMetadataBatchIngestRequest request, CancellationToken cancellationToken)
    {
        var filePaths = request.Files.Select(x => x.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var existingByPath = await GetExistingByPathAsync(filePaths, cancellationToken);

        var inserted = 0;
        var updated = 0;

        foreach (var item in request.Files)
        {
            if (existingByPath.TryGetValue(item.FilePath, out var existing))
            {
                // A different size or modified time means the content may have changed,
                // so the stored hash and duplicate flags can no longer be trusted.
                var contentChanged = existing.SizeBytes != item.SizeBytes
                                     || existing.LastModifiedDate != item.LastModifiedDate;

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

                if (contentChanged)
                {
                    existing.HashValue = string.Empty;
                    existing.HashCalculatedDate = null;
                    existing.HashStatus = HashStatus.Pending.ToString();
                    existing.HashError = null;
                    existing.IsDuplicate = false;
                    existing.DuplicateGroupId = null;
                }
                else if (existing.HashStatus == HashStatus.Failed.ToString())
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
        return (inserted, updated);
    }

    public async Task<IReadOnlyList<PendingHashFileDto>> GetPendingHashFilesAsync(
        Guid? scanJobId, int take, bool includeFailed, CancellationToken cancellationToken)
    {
        var pending = HashStatus.Pending.ToString();
        var failed = HashStatus.Failed.ToString();
        var query = dbContext.FileRecords
            .AsNoTracking()
            .Where(x => x.HashStatus == pending || (includeFailed && x.HashStatus == failed));

        if (scanJobId.HasValue)
        {
            query = query.Where(x => x.ScanJobId == scanJobId.Value);
        }

        return await query
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
        Guid scanJobId, string correlationId, IReadOnlySet<string> duplicateHashes, CancellationToken cancellationToken)
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

    public async Task<int> DeleteByScanJobAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsRelational())
        {
            return await dbContext.FileRecords
                .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        var records = await dbContext.FileRecords
            .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
        {
            return 0;
        }

        dbContext.FileRecords.RemoveRange(records);
        await dbContext.SaveChangesAsync(cancellationToken);
        return records.Count;
    }

    private async Task<Dictionary<string, FileRecord>> GetExistingByPathAsync(
        IReadOnlyList<string> filePaths, CancellationToken cancellationToken)
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
}
