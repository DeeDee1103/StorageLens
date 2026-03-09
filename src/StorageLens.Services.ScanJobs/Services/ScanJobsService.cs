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
            .AsNoTracking()
            .OrderByDescending(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ScanJobDto>> GetRecentAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.ScanJobs
            .AsNoTracking()
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
            LocationName = string.IsNullOrWhiteSpace(request.LocationName) ? "Scheduled Location" : request.LocationName,
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId,
            Status = ScanJobStatus.Queued,
            CurrentStage = "Queued",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = null,
            FilesScanned = 0,
            FilesHashed = 0,
            DuplicateGroupsFound = 0,
            WarningCount = 0,
            ErrorCount = 0,
            ProgressPercent = 0,
            Notes = request.Notes
        };

        dbContext.ScanJobs.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ScanJobDto?> UpdateStatusAsync(Guid id, UpdateScanJobStatusRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ScanJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = request.Status;
        entity.CurrentStage = request.CurrentStage;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            entity.Notes = request.Notes;
        }

        if (request.IsTerminal)
        {
            entity.EndTime = DateTimeOffset.UtcNow;
            if (request.Status == ScanJobStatus.Completed)
            {
                entity.ProgressPercent = 100;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ScanJobDto?> UpdateProgressAsync(Guid id, UpdateScanJobProgressRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ScanJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.FilesScanned = request.FilesScanned;
        entity.FilesHashed = request.FilesHashed;
        entity.ProgressPercent = Math.Clamp(request.ProgressPercent, 0, 100);
        entity.WarningCount = Math.Max(0, request.WarningCount);
        entity.ErrorCount = Math.Max(0, request.ErrorCount);
        entity.DuplicateGroupsFound = Math.Max(0, request.DuplicateGroupsFound);

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            entity.Notes = request.Notes;
        }

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
            entity.Notes,
            entity.CurrentStage,
            entity.CorrelationId,
            entity.FilesHashed,
            entity.DuplicateGroupsFound,
            entity.WarningCount,
            entity.ErrorCount);
}
