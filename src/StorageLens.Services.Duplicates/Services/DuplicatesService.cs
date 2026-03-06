using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Duplicates.Data;
using StorageLens.Services.Duplicates.Interfaces;
using StorageLens.Services.Duplicates.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Duplicates.Services;

public class DuplicatesService(DuplicatesDbContext dbContext) : IDuplicatesService
{
    public async Task<DuplicateSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var groups = await dbContext.DuplicateGroups.ToListAsync(cancellationToken);
        var duplicateFiles = groups.Sum(x => x.FileCount);
        var wastedBytes = groups.Sum(x => x.WastedBytes);

        return new DuplicateSummaryDto(
            groups.Count,
            duplicateFiles,
            wastedBytes,
            duplicateFiles == 0 ? 0 : 27);
    }

    public async Task<IReadOnlyList<DuplicateGroupDto>> GetGroupsAsync(CancellationToken cancellationToken)
    {
        var groups = await dbContext.DuplicateGroups
            .OrderByDescending(x => x.WastedBytes)
            .Take(50)
            .ToListAsync(cancellationToken);

        return groups.Select(Map).ToList();
    }

    public async Task<DuplicateGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var group = await dbContext.DuplicateGroups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return group is null ? null : Map(group);
    }

    private static DuplicateGroupDto Map(DuplicateGroup group) =>
        new(
            group.Id,
            group.HashValue,
            group.FileCount,
            group.TotalSizeBytes,
            group.WastedBytes,
            group.ExampleFileName,
            group.LocationsCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
}
