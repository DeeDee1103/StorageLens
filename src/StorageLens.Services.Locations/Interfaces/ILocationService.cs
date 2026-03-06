using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Locations.Interfaces;

public interface ILocationService
{
    Task<IReadOnlyList<StorageLocationDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<StorageLocationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<StorageLocationDto> CreateAsync(CreateStorageLocationRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Guid id, UpdateStorageLocationRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
