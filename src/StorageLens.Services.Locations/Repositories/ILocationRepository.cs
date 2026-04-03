using StorageLens.Services.Locations.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Locations.Repositories;

/// <summary>
/// Abstracts all EF Core data-access operations for the Locations service,
/// allowing <see cref="Services.LocationService"/> to be tested without an EF Core dependency.
/// </summary>
public interface ILocationRepository
{
    Task<IReadOnlyList<StorageLocation>> GetAllVisibleAsync(CancellationToken cancellationToken);
    Task<StorageLocation?> GetByIdVisibleAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(StorageLocation entity, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Guid id, UpdateStorageLocationRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateStatsAsync(Guid id, UpdateStorageLocationStatsRequest request, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken);
}
