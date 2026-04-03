using StorageLens.Services.Duplicates.Models;

namespace StorageLens.Services.Duplicates.Repositories;

/// <summary>
/// Abstracts all EF Core data-access operations for the Duplicates service,
/// allowing <see cref="Services.DuplicatesService"/> to be tested without an EF Core dependency.
/// </summary>
public interface IDuplicateRepository
{
    Task<DuplicateGroup?> GetLatestGroupAsync(CancellationToken cancellationToken);
    Task<DuplicateGroupAggregate?> GetAggregateForRunAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DuplicateGroup>> GetTopByWastedBytesAsync(int take, CancellationToken cancellationToken);
    Task<DuplicateGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<int> DeleteByScanJobAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken);
    Task<int> DeleteAndReplaceAsync(Guid scanJobId, string correlationId, IEnumerable<DuplicateGroup> newGroups, CancellationToken cancellationToken);
}

/// <summary>Aggregated statistics for a single scan run's duplicate groups.</summary>
public record DuplicateGroupAggregate(int GroupCount, int DuplicateFiles, long WastedBytes);
