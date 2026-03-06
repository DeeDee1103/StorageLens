using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Duplicates.Interfaces;

public interface IDuplicatesService
{
    Task<DuplicateSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DuplicateGroupDto>> GetGroupsAsync(CancellationToken cancellationToken);
    Task<DuplicateGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DuplicateRecalculationResultDto> RecalculateAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken);
}
