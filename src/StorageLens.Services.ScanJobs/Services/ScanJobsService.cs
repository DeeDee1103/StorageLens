using Microsoft.EntityFrameworkCore;
using StorageLens.Services.ScanJobs.Data;
using StorageLens.Services.ScanJobs.Interfaces;
using StorageLens.Services.ScanJobs.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.ScanJobs.Services;

public class ScanJobsService(ScanJobsDbContext dbContext) : IScanJobsService
{
    public async Task<IReadOnlyList<ScanJobDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.ScanJobs
            .OrderByDescending(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ScanJobDto>> GetRecentAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.ScanJobs
            .OrderByDescending(x => x.StartTime)
            .Take(6)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToList();
    }

    public async Task<ScanJobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ScanJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<ScanJobDto> CreateAsync(CreateScanJobRequest request, CancellationToken cancellationToken)
    {
        var entity = new ScanJob
        {
            Id = Guid.NewGuid(),
            StorageLocationId = request.StorageLocationId,
            LocationName = "Scheduled Location",
            Status = ScanJobStatus.Pending,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = null,
            FilesScanned = 0,
            ProgressPercent = 0,
            Notes = request.Notes
        };

        dbContext.ScanJobs.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static ScanJobDto Map(ScanJob entity) =>
        new(
            entity.Id,
            entity.StorageLocationId,
            entity.LocationName,
            entity.Status.ToString(),
            entity.StartTime,
            entity.EndTime,
            entity.FilesScanned,
            entity.ProgressPercent,
            entity.Notes);
}
