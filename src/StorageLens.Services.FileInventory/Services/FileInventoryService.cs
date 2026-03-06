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
            .Take(Math.Clamp(pageSize, 1, 200))
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
            entity.HashValue.Length > 10 ? "Hashed" : "Pending");
}
