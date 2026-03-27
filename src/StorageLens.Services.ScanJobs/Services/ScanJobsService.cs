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
        return await dbContext.ScanJobs
            .AsNoTracking()
            .OrderByDescending(x => x.StartTime)
            .Select(x => new ScanJobDto(
                x.Id,
                x.StorageLocationId,
                x.LocationName,
                x.Status.ToString(),
                x.StartTime,
                x.EndTime,
                x.FilesScanned,
                x.ProgressPercent,
                x.Notes,
                x.CurrentStage,
                x.CorrelationId,
                x.FilesHashed,
                x.DuplicateGroupsFound,
                x.WarningCount,
                x.ErrorCount))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScanJobDto>> GetRecentAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ScanJobs
            .AsNoTracking()
            .OrderByDescending(x => x.StartTime)
            .Take(6)
            .Select(x => new ScanJobDto(
                x.Id,
                x.StorageLocationId,
                x.LocationName,
                x.Status.ToString(),
                x.StartTime,
                x.EndTime,
                x.FilesScanned,
                x.ProgressPercent,
                x.Notes,
                x.CurrentStage,
                x.CorrelationId,
                x.FilesHashed,
                x.DuplicateGroupsFound,
                x.WarningCount,
                x.ErrorCount))
            .ToListAsync(cancellationToken);
    }

    public async Task<ScanJobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ScanJobs
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ScanJobDto(
                x.Id,
                x.StorageLocationId,
                x.LocationName,
                x.Status.ToString(),
                x.StartTime,
                x.EndTime,
                x.FilesScanned,
                x.ProgressPercent,
                x.Notes,
                x.CurrentStage,
                x.CorrelationId,
                x.FilesHashed,
                x.DuplicateGroupsFound,
                x.WarningCount,
                x.ErrorCount))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ScanJobDto> CreateAsync(CreateScanJobRequest request, CancellationToken cancellationToken)
    {
        var initialStatus = request.InitialStatus ?? ScanJobStatus.Queued;
        var initialStage = string.IsNullOrWhiteSpace(request.InitialStage)
            ? initialStatus.ToString()
            : request.InitialStage;

        var entity = new ScanJob
        {
            Id = Guid.NewGuid(),
            StorageLocationId = request.StorageLocationId,
            LocationName = string.IsNullOrWhiteSpace(request.LocationName) ? "Scheduled Location" : request.LocationName,
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId,
            Status = initialStatus,
            CurrentStage = initialStage,
            StartTime = request.ScheduledStartUtc?.ToUniversalTime() ?? DateTimeOffset.UtcNow,
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
        if (dbContext.Database.IsRelational())
        {
            var terminalTimestamp = DateTimeOffset.UtcNow;
            var keepNotes = string.IsNullOrWhiteSpace(request.Notes);
            var rows = await dbContext.ScanJobs
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(v => v.Status, request.Status)
                    .SetProperty(v => v.CurrentStage, request.CurrentStage)
                    .SetProperty(v => v.Notes, v => keepNotes ? v.Notes : request.Notes!)
                    .SetProperty(v => v.EndTime, v => request.IsTerminal ? terminalTimestamp : v.EndTime)
                    .SetProperty(v => v.ProgressPercent, v => request.IsTerminal && request.Status == ScanJobStatus.Completed ? 100 : v.ProgressPercent), cancellationToken);

            if (rows == 0)
            {
                return null;
            }

            return await GetByIdAsync(id, cancellationToken);
        }

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
        if (dbContext.Database.IsRelational())
        {
            var keepNotes = string.IsNullOrWhiteSpace(request.Notes);
            var rows = await dbContext.ScanJobs
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(v => v.FilesScanned, request.FilesScanned)
                    .SetProperty(v => v.FilesHashed, request.FilesHashed)
                    .SetProperty(v => v.ProgressPercent, Math.Clamp(request.ProgressPercent, 0, 100))
                    .SetProperty(v => v.WarningCount, Math.Max(0, request.WarningCount))
                    .SetProperty(v => v.ErrorCount, Math.Max(0, request.ErrorCount))
                    .SetProperty(v => v.DuplicateGroupsFound, Math.Max(0, request.DuplicateGroupsFound))
                    .SetProperty(v => v.Notes, v => keepNotes ? v.Notes : request.Notes!), cancellationToken);

            if (rows == 0)
            {
                return null;
            }

            return await GetByIdAsync(id, cancellationToken);
        }

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

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsRelational())
        {
            var rows = await dbContext.ScanJobs
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            return rows > 0;
        }

        var entity = await dbContext.ScanJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.ScanJobs.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
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
