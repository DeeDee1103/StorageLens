using StorageLens.Shared.Contracts;

namespace StorageLens.Services.ScanJobs.Interfaces;

public interface IScanJobsService
{
    Task<IReadOnlyList<ScanJobDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ScanJobDto>> GetRecentAsync(CancellationToken cancellationToken);
    Task<ScanJobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ScanJobDto> CreateAsync(CreateScanJobRequest request, CancellationToken cancellationToken);
    Task<ScanJobDto?> UpdateStatusAsync(Guid id, UpdateScanJobStatusRequest request, CancellationToken cancellationToken);
    Task<ScanJobDto?> UpdateProgressAsync(Guid id, UpdateScanJobProgressRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
