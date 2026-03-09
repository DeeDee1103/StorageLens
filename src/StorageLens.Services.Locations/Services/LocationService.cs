using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Locations.Data;
using StorageLens.Services.Locations.Interfaces;
using StorageLens.Services.Locations.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Locations.Services;

public class LocationService(LocationsDbContext dbContext) : ILocationService
{
    public async Task<IReadOnlyList<StorageLocationDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.StorageLocations
            .AsNoTracking()
            .OrderByDescending(x => x.TotalSizeBytes)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToList();
    }

    public async Task<StorageLocationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.StorageLocations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<StorageLocationDto> CreateAsync(CreateStorageLocationRequest request, CancellationToken cancellationToken)
    {
        var entity = new StorageLocation
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Path = request.Path,
            Type = request.Type,
            Status = LocationStatus.Online,
            CreatedDate = DateTimeOffset.UtcNow,
            LastScanDate = DateTimeOffset.UtcNow.AddDays(-1),
            TotalSizeBytes = 0,
            FileCount = 0
        };

        dbContext.StorageLocations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateStorageLocationRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.StorageLocations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.Name = request.Name;
        entity.Path = request.Path;
        entity.Type = request.Type;
        entity.Status = request.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.StorageLocations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.StorageLocations.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static StorageLocationDto Map(StorageLocation entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Path,
            entity.Type.ToString(),
            entity.Status.ToString(),
            entity.LastScanDate,
            entity.TotalSizeBytes,
            entity.FileCount,
            entity.CreatedDate);
}
