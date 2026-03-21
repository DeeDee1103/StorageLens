using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StorageLens.Services.Duplicates.Data;
using StorageLens.Services.Duplicates.Models;
using StorageLens.Services.Duplicates.Services;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Duplicates.Tests;

public class DuplicatesServiceTests
{
    private static DuplicatesDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<DuplicatesDbContext>()
            .UseInMemoryDatabase($"duplicates-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task GetSummaryAsync_UsesLatestRunAndReturnsExpectedTotals()
    {
        await using var db = CreateDbContext();
        var latestScanJobId = Guid.NewGuid();
        const string latestCorrelationId = "corr-latest";

        db.DuplicateGroups.AddRange(
            new DuplicateGroup
            {
                Id = Guid.NewGuid(),
                ScanJobId = latestScanJobId,
                CorrelationId = latestCorrelationId,
                HashValue = "h1",
                FileCount = 2,
                TotalSizeBytes = 300,
                WastedBytes = 100,
                ExampleFileName = "a.txt",
                LocationsCsv = "LocA,LocB",
                RecalculatedAtUtc = DateTimeOffset.UtcNow
            },
            new DuplicateGroup
            {
                Id = Guid.NewGuid(),
                ScanJobId = latestScanJobId,
                CorrelationId = latestCorrelationId,
                HashValue = "h2",
                FileCount = 3,
                TotalSizeBytes = 800,
                WastedBytes = 200,
                ExampleFileName = "b.txt",
                LocationsCsv = "LocA",
                RecalculatedAtUtc = DateTimeOffset.UtcNow
            },
            new DuplicateGroup
            {
                Id = Guid.NewGuid(),
                ScanJobId = Guid.NewGuid(),
                CorrelationId = "corr-old",
                HashValue = "old",
                FileCount = 99,
                TotalSizeBytes = 99,
                WastedBytes = 99,
                ExampleFileName = "old.txt",
                LocationsCsv = "Old",
                RecalculatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10)
            });
        await db.SaveChangesAsync();

        var filesResponse = new List<FileRecordDto>
        {
            MakeFile(scanJobId: latestScanJobId, correlationId: latestCorrelationId, filePath: @"C:\\a\\1.txt"),
            MakeFile(scanJobId: latestScanJobId, correlationId: latestCorrelationId, filePath: @"C:\\a\\2.txt"),
            MakeFile(scanJobId: latestScanJobId, correlationId: latestCorrelationId, filePath: @"C:\\a\\3.txt"),
            MakeFile(scanJobId: latestScanJobId, correlationId: latestCorrelationId, filePath: @"C:\\a\\4.txt")
        };

        var httpFactory = new StubHttpClientFactory(CreateHttpClient((request, _) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri is not null)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(filesResponse)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var sut = new DuplicatesService(db, httpFactory, NullLogger<DuplicatesService>.Instance);
        var result = await sut.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(2, result.DuplicateGroups);
        Assert.Equal(5, result.DuplicateFiles);
        Assert.Equal(300, result.WastedBytes);
        Assert.Equal(100, result.DuplicatePercent);
    }

    [Fact]
    public async Task RecalculateAsync_ReplacesStaleGroups_AndMarksHashes()
    {
        await using var db = CreateDbContext();
        var scanJobId = Guid.NewGuid();
        const string correlationId = "corr-recalc";

        db.DuplicateGroups.Add(new DuplicateGroup
        {
            Id = Guid.NewGuid(),
            ScanJobId = scanJobId,
            CorrelationId = correlationId,
            HashValue = "stale-hash",
            FileCount = 2,
            TotalSizeBytes = 20,
            WastedBytes = 10,
            ExampleFileName = "old.txt",
            LocationsCsv = "Old",
            RecalculatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();

        MarkDuplicateHashesRequest? capturedMarkRequest = null;

        var filesResponse = new List<FileRecordDto>
        {
            MakeFile(scanJobId, correlationId, @"C:\\a\\x1.txt", "hash-x", 100, "Completed", "LocA", "x1.txt"),
            MakeFile(scanJobId, correlationId, @"C:\\a\\x2.txt", "hash-x", 50, "Completed", "LocB", "x2.txt"),
            MakeFile(scanJobId, correlationId, @"C:\\a\\y1.txt", "hash-y", 75, "Completed", "LocA", "y1.txt"),
            MakeFile(scanJobId, correlationId, @"C:\\a\\y2.txt", "hash-y", 25, "Completed", "LocA", "y2.txt"),
            MakeFile(scanJobId, correlationId, @"C:\\a\\ignore.txt", "", 10, "Pending", "LocA", "ignore.txt")
        };

        var httpFactory = new StubHttpClientFactory(CreateHttpClient(async (request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(filesResponse)
                };
            }

            if (request.Method == HttpMethod.Post)
            {
                capturedMarkRequest = await request.Content!.ReadFromJsonAsync<MarkDuplicateHashesRequest>(cancellationToken: cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var sut = new DuplicatesService(db, httpFactory, NullLogger<DuplicatesService>.Instance);
        var result = await sut.RecalculateAsync(scanJobId, correlationId, CancellationToken.None);

        Assert.Equal(2, result.DuplicateGroups);
        Assert.Equal(4, result.DuplicateFiles);
        Assert.Equal(75, result.WastedBytes);

        var groups = await db.DuplicateGroups
            .AsNoTracking()
            .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
            .OrderBy(x => x.HashValue)
            .ToListAsync();

        Assert.Equal(2, groups.Count);
        Assert.DoesNotContain(groups, x => x.HashValue == "stale-hash");

        Assert.NotNull(capturedMarkRequest);
        Assert.Equal(scanJobId, capturedMarkRequest!.ScanJobId);
        Assert.Equal(correlationId, capturedMarkRequest.CorrelationId);
        Assert.Equal(2, capturedMarkRequest.Hashes.Count);
        Assert.Contains("hash-x", capturedMarkRequest.Hashes);
        Assert.Contains("hash-y", capturedMarkRequest.Hashes);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenFileInventoryIsPaginated_ReadsAllPages()
    {
        await using var db = CreateDbContext();
        var latestScanJobId = Guid.NewGuid();
        const string latestCorrelationId = "corr-paged-summary";

        db.DuplicateGroups.Add(new DuplicateGroup
        {
            Id = Guid.NewGuid(),
            ScanJobId = latestScanJobId,
            CorrelationId = latestCorrelationId,
            HashValue = "h1",
            FileCount = 3,
            TotalSizeBytes = 300,
            WastedBytes = 120,
            ExampleFileName = "largest.txt",
            LocationsCsv = "LocA,LocB",
            RecalculatedAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var page1 = Enumerable.Range(0, 2000)
            .Select(i => MakeFile(latestScanJobId, latestCorrelationId, $@"C:\\scan\\p1-{i}.txt"))
            .ToList();

        var page2 = new List<FileRecordDto>
        {
            MakeFile(latestScanJobId, latestCorrelationId, @"C:\\scan\\p2-last.txt")
        };

        var requestedPages = new List<int>();
        var httpFactory = new StubHttpClientFactory(CreateHttpClient((request, _) =>
        {
            if (request.Method != HttpMethod.Get || request.RequestUri is null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var page = GetQueryInt(request.RequestUri, "page", 1);
            requestedPages.Add(page);

            return page switch
            {
                1 => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(page1) },
                2 => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(page2) },
                _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new List<FileRecordDto>()) }
            };
        }));

        var sut = new DuplicatesService(db, httpFactory, NullLogger<DuplicatesService>.Instance);
        var result = await sut.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, requestedPages);
        Assert.Equal(3, result.DuplicateFiles);
        Assert.Equal(120, result.WastedBytes);
        Assert.Equal(Math.Round((double)3 / 2001 * 100, 2), result.DuplicatePercent);
    }

    [Fact]
    public async Task RecalculateAsync_WhenFileInventoryIsPaginated_AggregatesAcrossPages()
    {
        await using var db = CreateDbContext();
        var scanJobId = Guid.NewGuid();
        const string correlationId = "corr-paged-recalc";

        MarkDuplicateHashesRequest? capturedMarkRequest = null;
        var requestedPages = new List<int>();

        var page1 = Enumerable.Range(0, 1999)
            .Select(i => MakeFile(scanJobId, correlationId, $@"C:\\scan\\unique-{i}.txt", $"hash-{i}", 10 + i, "Completed", "LocA", $"u-{i}.txt"))
            .Append(MakeFile(scanJobId, correlationId, @"C:\\scan\\dup-a.txt", "shared-hash", 200, "Completed", "LocA", "dup-a.txt"))
            .ToList();

        var page2 = new List<FileRecordDto>
        {
            MakeFile(scanJobId, correlationId, @"C:\\scan\\dup-b.txt", "shared-hash", 50, "Completed", "LocB", "dup-b.txt")
        };

        var httpFactory = new StubHttpClientFactory(CreateHttpClient(async (request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri is not null)
            {
                var page = GetQueryInt(request.RequestUri, "page", 1);
                requestedPages.Add(page);

                return page switch
                {
                    1 => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(page1) },
                    2 => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(page2) },
                    _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new List<FileRecordDto>()) }
                };
            }

            if (request.Method == HttpMethod.Post)
            {
                capturedMarkRequest = await request.Content!.ReadFromJsonAsync<MarkDuplicateHashesRequest>(cancellationToken: cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }));

        var sut = new DuplicatesService(db, httpFactory, NullLogger<DuplicatesService>.Instance);
        var result = await sut.RecalculateAsync(scanJobId, correlationId, CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, requestedPages);
        Assert.Equal(1, result.DuplicateGroups);
        Assert.Equal(2, result.DuplicateFiles);
        Assert.Equal(50, result.WastedBytes);

        Assert.NotNull(capturedMarkRequest);
        Assert.Single(capturedMarkRequest!.Hashes);
        Assert.Equal("shared-hash", capturedMarkRequest.Hashes[0]);
    }

    private static FileRecordDto MakeFile(
        Guid? scanJobId,
        string correlationId,
        string filePath,
        string hashValue = "",
        long sizeBytes = 100,
        string hashStatus = "Completed",
        string locationName = "LocA",
        string fileName = "file.txt") =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            locationName,
            fileName,
            filePath,
            ".txt",
            sizeBytes,
            DateTimeOffset.UtcNow,
            hashValue,
            false,
            null,
            hashStatus,
            scanJobId,
            correlationId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            @"C:\\a");

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) =>
        CreateHttpClient((request, cancellationToken) => Task.FromResult(responder(request, cancellationToken)));

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            responder(request, cancellationToken);
    }

    private static int GetQueryInt(Uri uri, string key, int fallback)
    {
        var queryPairs = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var queryPair in queryPairs)
        {
            var kvp = queryPair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kvp.Length != 2)
            {
                continue;
            }

            if (!string.Equals(kvp[0], key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(Uri.UnescapeDataString(kvp[1]), out var parsed))
            {
                return parsed;
            }
        }

        return fallback;
    }
}
