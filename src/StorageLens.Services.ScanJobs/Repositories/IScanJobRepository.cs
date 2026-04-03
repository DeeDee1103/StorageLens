using StorageLens.Services.ScanJobs.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.ScanJobs.Repositories;

/// <summary>
/// Abstracts all EF Core data-access operations for the ScanJobs service,
/// allowing <see cref="Services.ScanJobsService"/> to be tested without an EF Core dependency.
/// </summary>
public interface IScanJobRepository
{
    Task<IReadOnlyList<ScanJob>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ScanJob>> GetRecentAsync(int take, CancellationToken cancellationToken);
    Task<ScanJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(ScanJob entity, CancellationToken cancellationToken);
    Task<ScanJob?> UpdateStatusAsync(Guid id, UpdateScanJobStatusRequest request, CancellationToken cancellationToken);
    Task<ScanJob?> UpdateProgressAsync(Guid id, UpdateScanJobProgressRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
