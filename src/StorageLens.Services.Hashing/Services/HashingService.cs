using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using StorageLens.Services.Hashing.Clients;
using StorageLens.Services.Hashing.Data;
using StorageLens.Services.Hashing.Interfaces;
using StorageLens.Services.Hashing.Models;
using StorageLens.Shared.Contracts;
using StorageLens.Shared.Infrastructure;

namespace StorageLens.Services.Hashing.Services;

public class HashingService(
    HashingDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IOptions<HashingWorkerOptions> workerOptions,
    ILogger<HashingService> logger) : IHashingService
{
    public async Task<HashingRunResultDto> RunAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken)
    {
        var execution = new HashExecution
        {
            Id = Guid.NewGuid(),
            ScanJobId = scanJobId,
            CorrelationId = correlationId,
            Status = "Running",
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.HashExecutions.Add(execution);
        await dbContext.SaveChangesAsync(cancellationToken);

        var options = workerOptions.Value;
        var fileInventoryClient = httpClientFactory.CreateClient("fileinventory");
        var scanJobsClient = httpClientFactory.CreateClient("scanjobs");

        await scanJobsClient.PutAsJsonAsync(
            $"api/scanjobs/{scanJobId}/status",
            new UpdateScanJobStatusRequest(ScanJobStatus.Hashing, "Hashing", "Calculating SHA-256 hashes"),
            cancellationToken);

        var pendingFiles = await fileInventoryClient.GetFromJsonAsync<List<PendingHashFileDto>>(
                          $"api/files/pending-hash?scanJobId={scanJobId}&take={Math.Clamp(options.BatchSize, 50, 5000)}",
                          cancellationToken) ?? [];

        execution.Attempted = pendingFiles.Count;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (pendingFiles.Count == 0)
        {
            execution.Status = "Completed";
            execution.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(execution);
        }

        using var semaphore = new SemaphoreSlim(Math.Clamp(options.MaxDegreeOfParallelism, 1, 16));
        var tasks = pendingFiles.Select(async file =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await ProcessFileAsync(file, execution.Id, options.MaxFileSizeBytes, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        execution.Failed = await dbContext.Set<HashFailure>()
            .Where(x => x.HashExecutionId == execution.Id)
            .CountAsync(cancellationToken);

        execution.Succeeded = Math.Max(0, execution.Attempted - execution.Failed);
        execution.Status = execution.Failed == 0 ? "Completed" : "PartiallyCompleted";
        execution.CompletedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var currentJob = await scanJobsClient.GetFromJsonAsync<ScanJobDto>(
            $"api/scanjobs/{scanJobId}",
            cancellationToken);

        var progress = new UpdateScanJobProgressRequest(
            FilesScanned: currentJob?.FilesScanned ?? 0,
            FilesHashed: execution.Succeeded,
            ProgressPercent: 90,
            WarningCount: currentJob?.WarningCount ?? 0,
            ErrorCount: Math.Max(currentJob?.ErrorCount ?? 0, execution.Failed),
            DuplicateGroupsFound: currentJob?.DuplicateGroupsFound ?? 0,
            Notes: execution.Failed == 0 ? "Hashing completed" : $"Hashing completed with {execution.Failed} failures");

        await scanJobsClient.PutAsJsonAsync($"api/scanjobs/{scanJobId}/progress", progress, cancellationToken);

        return Map(execution);
    }

    public async Task<HashingRunResultDto?> GetExecutionAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.HashExecutions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    private async Task ProcessFileAsync(PendingHashFileDto file, Guid executionId, long maxFileSizeBytes, CancellationToken cancellationToken)
    {
        var fileInventoryClient = httpClientFactory.CreateClient("fileinventory");

        try
        {
            // SECURITY: Validate file existence and size BEFORE opening stream to prevent memory DoS attacks
            // File size check must happen before stream creation to prevent resource exhaustion
            FileSecurityValidation.ValidateFileBeforeProcessing(file.FilePath, maxFileSizeBytes);

            await fileInventoryClient.PostAsJsonAsync(
                $"api/files/{file.Id}/hash-result",
                new FileHashResultRequest(file.CorrelationId, HashStatus.InProgress.ToString(), null, null, DateTimeOffset.UtcNow),
                cancellationToken);

            await using var stream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            var hashValue = Convert.ToHexString(hashBytes).ToLowerInvariant();

            await fileInventoryClient.PostAsJsonAsync(
                $"api/files/{file.Id}/hash-result",
                new FileHashResultRequest(file.CorrelationId, HashStatus.Completed.ToString(), hashValue, null, DateTimeOffset.UtcNow),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to hash file {FilePath}", file.FilePath);

            dbContext.HashFailures.Add(new HashFailure
            {
                Id = Guid.NewGuid(),
                HashExecutionId = executionId,
                FileRecordId = file.Id,
                FilePath = file.FilePath,
                Error = ex.Message,
                OccurredAtUtc = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            await fileInventoryClient.PostAsJsonAsync(
                $"api/files/{file.Id}/hash-result",
                new FileHashResultRequest(file.CorrelationId, HashStatus.Failed.ToString(), null, ex.Message, DateTimeOffset.UtcNow),
                cancellationToken);
        }
    }

    private static HashingRunResultDto Map(HashExecution execution) =>
        new(
            execution.Id,
            execution.ScanJobId,
            execution.CorrelationId,
            execution.Attempted,
            execution.Succeeded,
            execution.Failed,
            execution.StartedAtUtc,
            execution.CompletedAtUtc ?? DateTimeOffset.UtcNow);
}
