using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Locations.Data;
using StorageLens.Services.Locations.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Locations.Repositories;

public class LocationRepository(LocationsDbContext dbContext) : ILocationRepository
{
    public async Task<IReadOnlyList<StorageLocation>> GetAllVisibleAsync(CancellationToken cancellationToken)
    {
        return await dbContext.StorageLocations
            .AsNoTracking()
            .Where(x => x.IsVisibleInUI)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public Task<StorageLocation?> GetByIdVisibleAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.StorageLocations
            .AsNoTracking()
            .Where(x => x.Id == id && x.IsVisibleInUI)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(StorageLocation entity, CancellationToken cancellationToken)
    {
        dbContext.StorageLocations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateStorageLocationRequest request, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsRelational())
        {
            var rows = await dbContext.StorageLocations
                .Where(x => x.Id == id && x.IsVisibleInUI)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(v => v.Name, request.Name)
                    .SetProperty(v => v.Path, request.Path)
                    .SetProperty(v => v.SiteLocation, request.SiteLocation)
                    .SetProperty(v => v.Latitude, request.Latitude)
                    .SetProperty(v => v.Longitude, request.Longitude)
                    .SetProperty(v => v.Type, request.Type)
                    .SetProperty(v => v.Status, request.Status), cancellationToken);

            return rows > 0;
        }

        var entity = await dbContext.StorageLocations
            .FirstOrDefaultAsync(x => x.Id == id && x.IsVisibleInUI, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.Name = request.Name;
        entity.Path = request.Path;
        entity.SiteLocation = request.SiteLocation;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.Type = request.Type;
        entity.Status = request.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateStatsAsync(Guid id, UpdateStorageLocationStatsRequest request, CancellationToken cancellationToken)
    {
        var scanDate = request.LastScanDate ?? DateTimeOffset.UtcNow;

        if (dbContext.Database.IsRelational())
        {
            var rows = await dbContext.StorageLocations
                .Where(x => x.Id == id && x.IsVisibleInUI)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(v => v.TotalSizeBytes, request.TotalSizeBytes)
                    .SetProperty(v => v.FileCount, request.FileCount)
                    .SetProperty(v => v.LastScanDate, scanDate), cancellationToken);

            return rows > 0;
        }

        var entity = await dbContext.StorageLocations
            .FirstOrDefaultAsync(x => x.Id == id && x.IsVisibleInUI, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.TotalSizeBytes = request.TotalSizeBytes;
        entity.FileCount = request.FileCount;
        entity.LastScanDate = scanDate;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsRelational())
        {
            var rows = await dbContext.StorageLocations
                .Where(x => x.Id == id && x.IsVisibleInUI)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(v => v.IsVisibleInUI, false), cancellationToken);

            return rows > 0;
        }

        var entity = await dbContext.StorageLocations
            .FirstOrDefaultAsync(x => x.Id == id && x.IsVisibleInUI, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsVisibleInUI = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
