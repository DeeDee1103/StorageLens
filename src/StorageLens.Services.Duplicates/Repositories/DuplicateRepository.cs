using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Duplicates.Data;
using StorageLens.Services.Duplicates.Models;

namespace StorageLens.Services.Duplicates.Repositories;

public class DuplicateRepository(DuplicatesDbContext dbContext) : IDuplicateRepository
{
    public Task<DuplicateGroup?> GetLatestGroupAsync(CancellationToken cancellationToken)
    {
        return dbContext.DuplicateGroups
            .AsNoTracking()
            .OrderByDescending(x => x.RecalculatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DuplicateGroupAggregate?> GetAggregateForRunAsync(
        Guid scanJobId, string correlationId, CancellationToken cancellationToken)
    {
        return await dbContext.DuplicateGroups
            .AsNoTracking()
            .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
            .GroupBy(_ => 1)
            .Select(g => new DuplicateGroupAggregate(
                g.Count(),
                g.Sum(x => x.FileCount),
                g.Sum(x => x.WastedBytes)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DuplicateGroup>> GetTopByWastedBytesAsync(
        int take, CancellationToken cancellationToken)
    {
        return await dbContext.DuplicateGroups
            .AsNoTracking()
            .OrderByDescending(x => x.WastedBytes)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<DuplicateGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.DuplicateGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<int> DeleteByScanJobAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsRelational())
        {
            return await dbContext.DuplicateGroups
                .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        var staleGroups = await dbContext.DuplicateGroups
            .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
            .ToListAsync(cancellationToken);

        if (staleGroups.Count > 0)
        {
            dbContext.DuplicateGroups.RemoveRange(staleGroups);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return staleGroups.Count;
    }

    public async Task<int> DeleteAndReplaceAsync(
        Guid scanJobId, string correlationId, IEnumerable<DuplicateGroup> newGroups, CancellationToken cancellationToken)
    {
        await DeleteByScanJobAsync(scanJobId, correlationId, cancellationToken);

        var groupList = newGroups.ToList();
        if (groupList.Count > 0)
        {
            dbContext.DuplicateGroups.AddRange(groupList);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return groupList.Count;
    }
}
