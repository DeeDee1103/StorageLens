using StorageLens.Shared.Contracts;

namespace StorageLens.Services.FileInventory.Interfaces;

public interface IFileInventoryService
{
    Task<IReadOnlyList<FileRecordDto>> GetAllAsync(string? search, string? extension, int page, int pageSize, CancellationToken cancellationToken);
    Task<FileRecordDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<FileRecordDto>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken);
}
