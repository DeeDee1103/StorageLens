using StorageLens.Services.ScanJobs.Interfaces;
using StorageLens.Services.ScanJobs.Models;
using StorageLens.Services.ScanJobs.Repositories;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.ScanJobs.Services;

public class ScanJobsService(IScanJobRepository repository) : IScanJobsService
{
    public async Task<IReadOnlyList<ScanJobDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await repository.GetAllAsync(cancellationToken);
        return entities.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ScanJobDto>> GetRecentAsync(CancellationToken cancellationToken)
    {
        var entities = await repository.GetRecentAsync(6, cancellationToken);
        return entities.Select(Map).ToList();
    }

    public async Task<ScanJobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : Map(entity);
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

        await repository.AddAsync(entity, cancellationToken);
        return Map(entity);
    }

    public async Task<ScanJobDto?> UpdateStatusAsync(Guid id, UpdateScanJobStatusRequest request, CancellationToken cancellationToken)
    {
        var entity = await repository.UpdateStatusAsync(id, request, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<ScanJobDto?> UpdateProgressAsync(Guid id, UpdateScanJobProgressRequest request, CancellationToken cancellationToken)
    {
        var entity = await repository.UpdateProgressAsync(id, request, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);

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
