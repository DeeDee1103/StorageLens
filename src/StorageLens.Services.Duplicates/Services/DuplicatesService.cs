using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using StorageLens.Services.Duplicates.Data;
using StorageLens.Services.Duplicates.Interfaces;
using StorageLens.Services.Duplicates.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Duplicates.Services;

public class DuplicatesService(
    DuplicatesDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<DuplicatesService> logger) : IDuplicatesService
{
    public async Task<DuplicateSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var latestGroup = await dbContext.DuplicateGroups
            .AsNoTracking()
            .OrderByDescending(x => x.RecalculatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestGroup is null)
        {
            return new DuplicateSummaryDto(0, 0, 0, 0);
        }

        var aggregate = await dbContext.DuplicateGroups
            .AsNoTracking()
            .Where(x => x.ScanJobId == latestGroup.ScanJobId && x.CorrelationId == latestGroup.CorrelationId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                GroupCount = g.Count(),
                DuplicateFiles = g.Sum(x => x.FileCount),
                WastedBytes = g.Sum(x => x.WastedBytes)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var groupCount = aggregate?.GroupCount ?? 0;
        var duplicateFiles = aggregate?.DuplicateFiles ?? 0;
        var wastedBytes = aggregate?.WastedBytes ?? 0L;
        var fileInventoryClient = httpClientFactory.CreateClient("fileinventory");
        var filesForRunCount = 0;
        await foreach (var file in EnumerateFilesForRunAsync(fileInventoryClient, latestGroup.ScanJobId, latestGroup.CorrelationId, cancellationToken))
        {
            if (file.ScanJobId == latestGroup.ScanJobId && file.CorrelationId == latestGroup.CorrelationId)
            {
                filesForRunCount += 1;
            }
        }

        var duplicatePercent = filesForRunCount == 0
            ? 0
            : Math.Round((double)duplicateFiles / filesForRunCount * 100, 2);

        return new DuplicateSummaryDto(
            groupCount,
            duplicateFiles,
            wastedBytes,
            Math.Clamp(duplicatePercent, 0, 100));
    }

    public async Task<IReadOnlyList<DuplicateGroupDto>> GetGroupsAsync(CancellationToken cancellationToken)
    {
        var groups = await dbContext.DuplicateGroups
            .AsNoTracking()
            .OrderByDescending(x => x.WastedBytes)
            .Take(50)
            .ToListAsync(cancellationToken);

        return groups.Select(Map).ToList();
    }

    public async Task<DuplicateGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var group = await dbContext.DuplicateGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return group is null ? null : Map(group);
    }

    public async Task<DuplicateRecalculationResultDto> RecalculateAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken)
    {
        var fileInventoryClient = httpClientFactory.CreateClient("fileinventory");
        var fileHashAggregates = new Dictionary<string, DuplicateHashAggregate>(StringComparer.OrdinalIgnoreCase);

        await foreach (var file in EnumerateFilesForRunAsync(fileInventoryClient, scanJobId, correlationId, cancellationToken))
        {
            if (file.ScanJobId != scanJobId || file.CorrelationId != correlationId)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(file.HashValue) || file.HashStatus != HashStatus.Completed.ToString())
            {
                continue;
            }

            if (!fileHashAggregates.TryGetValue(file.HashValue, out var aggregate))
            {
                aggregate = new DuplicateHashAggregate(file.HashValue);
                fileHashAggregates[file.HashValue] = aggregate;
            }

            aggregate.Add(file);
        }

        var duplicateAggregates = fileHashAggregates.Values
            .Where(x => x.FileCount > 1)
            .ToList();

        if (dbContext.Database.IsRelational())
        {
            await dbContext.DuplicateGroups
                .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            var staleGroups = await dbContext.DuplicateGroups
                .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
                .ToListAsync(cancellationToken);

            if (staleGroups.Count > 0)
            {
                dbContext.DuplicateGroups.RemoveRange(staleGroups);
            }
        }

        var recalculatedAtUtc = DateTimeOffset.UtcNow;

        foreach (var duplicateAggregate in duplicateAggregates)
        {
            dbContext.DuplicateGroups.Add(new DuplicateGroup
            {
                Id = Guid.NewGuid(),
                ScanJobId = scanJobId,
                CorrelationId = correlationId,
                HashValue = duplicateAggregate.HashValue,
                FileCount = duplicateAggregate.FileCount,
                TotalSizeBytes = duplicateAggregate.TotalSizeBytes,
                WastedBytes = duplicateAggregate.WastedBytes,
                ExampleFileName = duplicateAggregate.ExampleFileName,
                LocationsCsv = duplicateAggregate.GetLocationsCsv(),
                RecalculatedAtUtc = recalculatedAtUtc
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var duplicateHashValues = duplicateAggregates.Select(x => x.HashValue).ToList();
        var markRequest = new MarkDuplicateHashesRequest(scanJobId, correlationId, duplicateHashValues);

        using var markResponse = await fileInventoryClient.PostAsJsonAsync(
            $"api/files/mark-duplicates/{scanJobId}",
            markRequest,
            cancellationToken);

        if (!markResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to mark duplicate flags in file inventory for scan job {ScanJobId}", scanJobId);
        }

        var duplicateFileCount = duplicateAggregates.Sum(x => x.FileCount);
        var wastedBytes = duplicateAggregates.Sum(x => x.WastedBytes);

        return new DuplicateRecalculationResultDto(
            scanJobId,
            correlationId,
            duplicateAggregates.Count,
            duplicateFileCount,
            wastedBytes,
            recalculatedAtUtc);
    }

    private static async IAsyncEnumerable<FileRecordDto> EnumerateFilesForRunAsync(
        HttpClient fileInventoryClient,
        Guid scanJobId,
        string correlationId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int pageSize = 2000;
        var page = 1;

        while (true)
        {
            var files = await fileInventoryClient.GetFromJsonAsync<List<FileRecordDto>>(
                $"api/files?page={page}&pageSize={pageSize}&scanJobId={scanJobId}&correlationId={Uri.EscapeDataString(correlationId)}",
                cancellationToken) ?? [];

            if (files.Count == 0)
            {
                yield break;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            if (files.Count < pageSize)
            {
                yield break;
            }

            page += 1;
        }
    }

    private sealed class DuplicateHashAggregate(string hashValue)
    {
        private readonly HashSet<string> locations = new(StringComparer.OrdinalIgnoreCase);

        public string HashValue { get; } = hashValue;
        public int FileCount { get; private set; }
        public long TotalSizeBytes { get; private set; }
        public long LargestFileSize { get; private set; }
        public string ExampleFileName { get; private set; } = string.Empty;
        public long WastedBytes => Math.Max(0, TotalSizeBytes - LargestFileSize);

        public void Add(FileRecordDto file)
        {
            FileCount += 1;
            TotalSizeBytes += file.SizeBytes;

            if (file.SizeBytes >= LargestFileSize)
            {
                LargestFileSize = file.SizeBytes;
                ExampleFileName = file.FileName;
            }

            if (!string.IsNullOrWhiteSpace(file.LocationName))
            {
                locations.Add(file.LocationName);
            }
        }

        public string GetLocationsCsv() => string.Join(",", locations);
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
