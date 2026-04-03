using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Duplicates.Interfaces;

public interface IDuplicatesService
{
    Task<DuplicateSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DuplicateGroupDto>> GetGroupsAsync(CancellationToken cancellationToken);
    Task<DuplicateGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DuplicateRecalculationResultDto> RecalculateAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken);
    /// <summary>
    /// Deletes all duplicate groups for the specified scan job and correlation ID.
    /// Used as a compensating transaction when a workflow fails after the DetectingDuplicates step completes.
    /// </summary>
    Task<int> DeleteByScanJobAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken);
}
