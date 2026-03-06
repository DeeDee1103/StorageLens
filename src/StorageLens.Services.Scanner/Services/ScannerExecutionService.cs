using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using StorageLens.Services.Scanner.Data;
using StorageLens.Services.Scanner.Interfaces;
using StorageLens.Services.Scanner.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Scanner.Services;

public class ScannerExecutionService(
    ScannerDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IScannerPathResolver pathResolver,
    ILogger<ScannerExecutionService> logger) : IScannerExecutionService
{
    public async Task<ScannerExecutionDto> ExecuteAsync(ScannerExecuteRequest request, CancellationToken cancellationToken)
    {
        var resolvedRootPath = pathResolver.Resolve(request.RootPath);

        if (!PathSafety.IsPathSupported(resolvedRootPath))
        {
            throw new InvalidOperationException("Only rooted local paths and UNC paths are supported.");
        }

        if (!Directory.Exists(resolvedRootPath))
        {
            throw new DirectoryNotFoundException($"Scan root path not found: {request.RootPath} (resolved: {resolvedRootPath})");
        }

        var execution = new ScanExecution
        {
            Id = Guid.NewGuid(),
            StorageLocationId = request.StorageLocationId,
            ScanJobId = request.ScanJobId,
            CorrelationId = request.CorrelationId,
            RootPath = resolvedRootPath,
            Status = "Running",
            StartedAtUtc = DateTimeOffset.UtcNow,
            Notes = "Scanning in progress"
        };

        dbContext.ScanExecutions.Add(execution);
        await dbContext.SaveChangesAsync(cancellationToken);

        await UpdateScanJobStatusAsync(
            request.ScanJobId,
            new UpdateScanJobStatusRequest(ScanJobStatus.Scanning, "Scanning", "Enumerating files"),
            cancellationToken);

        var errors = 0;
        var filesDiscovered = 0L;
        var batch = new List<FileMetadataDto>(Math.Max(50, request.BatchSize));

        try
        {
            var pendingDirectories = new Stack<string>();
            pendingDirectories.Push(resolvedRootPath);

            while (pendingDirectories.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pendingDirectories.Pop();

                IEnumerable<string> directories;
                try
                {
                    directories = Directory.EnumerateDirectories(current);
                }
                catch (Exception ex)
                {
                    await RecordErrorAsync(execution.Id, current, ex, cancellationToken);
                    errors++;
                    continue;
                }

                foreach (var directory in directories)
                {
                    pendingDirectories.Push(directory);
                }

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(current);
                }
                catch (Exception ex)
                {
                    await RecordErrorAsync(execution.Id, current, ex, cancellationToken);
                    errors++;
                    continue;
                }

                foreach (var filePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var info = new FileInfo(filePath);
                        if (!info.Exists)
                        {
                            continue;
                        }

                        var metadata = new FileMetadataDto(
                            null,
                            request.StorageLocationId,
                            request.ScanJobId,
                            request.CorrelationId,
                            request.LocationName,
                            info.Name,
                            info.FullName,
                            NormalizeExtension(info.Extension),
                            info.Length,
                            info.LastWriteTimeUtc,
                            info.CreationTimeUtc == DateTime.MinValue ? null : info.CreationTimeUtc,
                            info.LastAccessTimeUtc == DateTime.MinValue ? null : info.LastAccessTimeUtc,
                            DateTimeOffset.UtcNow,
                            resolvedRootPath);

                        batch.Add(metadata);
                        filesDiscovered++;

                        if (batch.Count >= request.BatchSize)
                        {
                            await FlushBatchAsync(request, batch, cancellationToken);
                            batch.Clear();
                        }

                        if (filesDiscovered % Math.Max(50, request.ProgressUpdateInterval) == 0)
                        {
                            var progress = Math.Min(70, 10 + (int)(filesDiscovered / 500));
                            await UpdateScanJobProgressAsync(request.ScanJobId, filesDiscovered, progress, errors, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        await RecordErrorAsync(execution.Id, filePath, ex, cancellationToken);
                        errors++;
                    }
                }
            }

            if (batch.Count > 0)
            {
                await FlushBatchAsync(request, batch, cancellationToken);
            }

            execution.Status = errors == 0 ? "Completed" : "PartiallyCompleted";
            execution.Notes = errors == 0 ? "Scan completed" : "Scan completed with some errors";
        }
        catch (OperationCanceledException)
        {
            execution.Status = "Canceled";
            execution.Notes = "Scan canceled";
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal scanner failure for job {ScanJobId}", request.ScanJobId);
            execution.Status = "Failed";
            execution.Notes = ex.Message;

            await UpdateScanJobStatusAsync(
                request.ScanJobId,
                new UpdateScanJobStatusRequest(ScanJobStatus.Failed, "Scanning", ex.Message, true),
                cancellationToken);
        }

        execution.FilesDiscovered = filesDiscovered;
        execution.ErrorCount = errors;
        execution.CompletedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await UpdateScanJobProgressAsync(request.ScanJobId, filesDiscovered, 75, errors, cancellationToken);

        return Map(execution);
    }

    public async Task<ScannerExecutionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ScanExecutions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    private async Task FlushBatchAsync(ScannerExecuteRequest request, IReadOnlyList<FileMetadataDto> batch, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("fileinventory");
        var resolvedRootPath = pathResolver.Resolve(request.RootPath);
        var payload = new FileMetadataBatchIngestRequest(
            request.StorageLocationId,
            request.ScanJobId,
            request.CorrelationId,
            request.LocationName,
            resolvedRootPath,
            batch);

        using var response = await client.PostAsJsonAsync("api/files/batch", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task UpdateScanJobStatusAsync(Guid scanJobId, UpdateScanJobStatusRequest request, CancellationToken cancellationToken)
    {
        var scanJobsClient = httpClientFactory.CreateClient("scanjobs");
        await scanJobsClient.PutAsJsonAsync($"api/scanjobs/{scanJobId}/status", request, cancellationToken);
    }

    private async Task UpdateScanJobProgressAsync(
        Guid scanJobId,
        long filesScanned,
        int progress,
        int errors,
        CancellationToken cancellationToken)
    {
        var scanJobsClient = httpClientFactory.CreateClient("scanjobs");
        var request = new UpdateScanJobProgressRequest(
            filesScanned,
            0,
            progress,
            0,
            errors,
            0,
            errors == 0 ? "Scanning" : $"Scanning with {errors} errors");

        await scanJobsClient.PutAsJsonAsync($"api/scanjobs/{scanJobId}/progress", request, cancellationToken);
    }

    private async Task RecordErrorAsync(Guid executionId, string path, Exception ex, CancellationToken cancellationToken)
    {
        dbContext.ScanErrors.Add(new ScanError
        {
            Id = Guid.NewGuid(),
            ScanExecutionId = executionId,
            Path = path,
            ErrorType = ex.GetType().Name,
            Message = ex.Message,
            OccurredAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ScannerExecutionDto Map(ScanExecution entity) =>
        new(
            entity.Id,
            entity.StorageLocationId,
            entity.ScanJobId,
            entity.CorrelationId,
            entity.RootPath,
            entity.Status,
            entity.StartedAtUtc,
            entity.CompletedAtUtc,
            entity.FilesDiscovered,
            entity.ErrorCount,
            entity.Notes);

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "";
        }

        // FileInventory currently stores extension with a constrained column length.
        // Keep payload compatible with deployed schema to prevent batch ingest failures.
        var trimmed = extension.Trim();
        return trimmed.Length <= 20 ? trimmed : trimmed[..20];
    }
}
