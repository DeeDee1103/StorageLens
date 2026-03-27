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
        return await dbContext.StorageLocations
            .AsNoTracking()
            .Where(x => x.IsVisibleInUI)
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new StorageLocationDto(
                x.Id,
                x.Name,
                x.Path,
                x.Type.ToString(),
                x.Status.ToString(),
                x.LastScanDate,
                x.TotalSizeBytes,
                x.FileCount,
                x.CreatedDate,
                x.SiteLocation,
                x.Latitude,
                x.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<StorageLocationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.StorageLocations
            .AsNoTracking()
            .Where(x => x.Id == id && x.IsVisibleInUI)
            .Select(x => new StorageLocationDto(
                x.Id,
                x.Name,
                x.Path,
                x.Type.ToString(),
                x.Status.ToString(),
                x.LastScanDate,
                x.TotalSizeBytes,
                x.FileCount,
                x.CreatedDate,
                x.SiteLocation,
                x.Latitude,
                x.Longitude))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StorageLocationDto> CreateAsync(CreateStorageLocationRequest request, CancellationToken cancellationToken)
    {
        var entity = new StorageLocation
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Path = request.Path,
            SiteLocation = request.SiteLocation,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Type = request.Type,
            Status = request.Status,
            CreatedDate = DateTimeOffset.UtcNow,
            LastScanDate = DateTimeOffset.UtcNow.AddDays(-1),
            TotalSizeBytes = 0,
            FileCount = 0,
            IsVisibleInUI = true
        };

        dbContext.StorageLocations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(entity);
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

        var entity = await dbContext.StorageLocations.FirstOrDefaultAsync(x => x.Id == id && x.IsVisibleInUI, cancellationToken);
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

        var entity = await dbContext.StorageLocations.FirstOrDefaultAsync(x => x.Id == id && x.IsVisibleInUI, cancellationToken);
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

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsRelational())
        {
            var rows = await dbContext.StorageLocations
                .Where(x => x.Id == id && x.IsVisibleInUI)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(v => v.IsVisibleInUI, false), cancellationToken);

            return rows > 0;
        }

        var entity = await dbContext.StorageLocations.FirstOrDefaultAsync(x => x.Id == id && x.IsVisibleInUI, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsVisibleInUI = false;
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
            entity.CreatedDate,
            entity.SiteLocation,
            entity.Latitude,
            entity.Longitude);
}
