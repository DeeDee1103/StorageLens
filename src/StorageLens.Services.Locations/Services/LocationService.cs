using StorageLens.Services.Locations.Interfaces;
using StorageLens.Services.Locations.Models;
using StorageLens.Services.Locations.Repositories;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Locations.Services;

public class LocationService(ILocationRepository repository) : ILocationService
{
    public async Task<IReadOnlyList<StorageLocationDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await repository.GetAllVisibleAsync(cancellationToken);
        return entities.Select(Map).ToList();
    }

    public async Task<StorageLocationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdVisibleAsync(id, cancellationToken);
        return entity is null ? null : Map(entity);
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

        await repository.AddAsync(entity, cancellationToken);
        return Map(entity);
    }

    public Task<bool> UpdateAsync(Guid id, UpdateStorageLocationRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(id, request, cancellationToken);

    public Task<bool> UpdateStatsAsync(Guid id, UpdateStorageLocationStatsRequest request, CancellationToken cancellationToken) =>
        repository.UpdateStatsAsync(id, request, cancellationToken);

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        repository.SoftDeleteAsync(id, cancellationToken);

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
