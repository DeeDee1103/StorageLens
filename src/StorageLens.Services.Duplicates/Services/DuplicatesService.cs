using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
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
            .OrderByDescending(x => x.RecalculatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestGroup is null)
        {
            return new DuplicateSummaryDto(0, 0, 0, 0);
        }

        var groups = await dbContext.DuplicateGroups
            .Where(x => x.ScanJobId == latestGroup.ScanJobId && x.CorrelationId == latestGroup.CorrelationId)
            .ToListAsync(cancellationToken);

        var duplicateFiles = groups.Sum(x => x.FileCount);
        var wastedBytes = groups.Sum(x => x.WastedBytes);
        var fileInventoryClient = httpClientFactory.CreateClient("fileinventory");
        var files = await fileInventoryClient.GetFromJsonAsync<List<FileRecordDto>>(
            "api/files?page=1&pageSize=2000",
            cancellationToken) ?? [];

        var filesForRun = files
            .Where(x => x.ScanJobId == latestGroup.ScanJobId && x.CorrelationId == latestGroup.CorrelationId)
            .ToList();

        var duplicatePercent = filesForRun.Count == 0
            ? 0
            : Math.Round((double)duplicateFiles / filesForRun.Count * 100, 2);

        return new DuplicateSummaryDto(
            groups.Count,
            duplicateFiles,
            wastedBytes,
            Math.Clamp(duplicatePercent, 0, 100));
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

    public async Task<DuplicateRecalculationResultDto> RecalculateAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken)
    {
        var fileInventoryClient = httpClientFactory.CreateClient("fileinventory");

        var files = await fileInventoryClient.GetFromJsonAsync<List<FileRecordDto>>(
            $"api/files?page=1&pageSize=2000",
            cancellationToken) ?? [];

        var candidates = files
            .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
            .Where(x => !string.IsNullOrWhiteSpace(x.HashValue) && x.HashStatus == HashStatus.Completed.ToString())
            .ToList();

        var duplicateBuckets = candidates
            .GroupBy(x => x.HashValue, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        var stale = await dbContext.DuplicateGroups
            .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
            .ToListAsync(cancellationToken);

        if (stale.Count > 0)
        {
            dbContext.DuplicateGroups.RemoveRange(stale);
        }

        var recalculatedAtUtc = DateTimeOffset.UtcNow;

        foreach (var bucket in duplicateBuckets)
        {
            var ordered = bucket.OrderByDescending(x => x.SizeBytes).ToList();
            var largest = ordered.First();

            dbContext.DuplicateGroups.Add(new DuplicateGroup
            {
                Id = Guid.NewGuid(),
                ScanJobId = scanJobId,
                CorrelationId = correlationId,
                HashValue = bucket.Key,
                FileCount = ordered.Count,
                TotalSizeBytes = ordered.Sum(x => x.SizeBytes),
                WastedBytes = ordered.Skip(1).Sum(x => x.SizeBytes),
                ExampleFileName = largest.FileName,
                LocationsCsv = string.Join(",", ordered.Select(x => x.LocationName).Distinct(StringComparer.OrdinalIgnoreCase)),
                RecalculatedAtUtc = recalculatedAtUtc
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var hashes = duplicateBuckets.Select(x => x.Key).ToList();
        var markRequest = new MarkDuplicateHashesRequest(scanJobId, correlationId, hashes);

        using var markResponse = await fileInventoryClient.PostAsJsonAsync(
            $"api/files/mark-duplicates/{scanJobId}",
            markRequest,
            cancellationToken);

        if (!markResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to mark duplicate flags in file inventory for scan job {ScanJobId}", scanJobId);
        }

        var duplicateFileCount = duplicateBuckets.Sum(x => x.Count());
        var wastedBytes = duplicateBuckets.Sum(x => x.OrderByDescending(v => v.SizeBytes).Skip(1).Sum(v => v.SizeBytes));

        return new DuplicateRecalculationResultDto(
            scanJobId,
            correlationId,
            duplicateBuckets.Count,
            duplicateFileCount,
            wastedBytes,
            recalculatedAtUtc);
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
