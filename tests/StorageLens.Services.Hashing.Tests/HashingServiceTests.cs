using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StorageLens.Services.Hashing.Clients;
using StorageLens.Services.Hashing.Data;
using StorageLens.Services.Hashing.Services;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Hashing.Tests;

public sealed class HashingServiceTests : IDisposable
{
    private readonly string _workDir = Directory.CreateTempSubdirectory("storagelens-hashing-tests-").FullName;

    public void Dispose() => Directory.Delete(_workDir, recursive: true);

    [Fact]
    public async Task RunAsync_WhenPendingFilesSpanSeveralBatches_HashesEveryFile()
    {
        var scanJobId = Guid.NewGuid();
        var inventory = new FakeFileInventory();
        for (var i = 0; i < 130; i++)
        {
            inventory.Add(scanJobId, CreateFile($"file-{i}.txt", $"content {i}"));
        }

        await using var db = CreateDbContext();
        var service = CreateService(db, inventory, batchSize: 50, parallelism: 4);

        var result = await service.RunAsync(scanJobId, "corr-batches", CancellationToken.None);

        Assert.Equal(130, result.Attempted);
        Assert.Equal(130, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.All(inventory.Files.Values, f => Assert.Equal(HashStatus.Completed.ToString(), f.Status));
        Assert.True(inventory.PendingRequests >= 3, "130 files at a batch size of 50 need at least three fetches.");
    }

    [Fact]
    public async Task RunAsync_WhenManyFilesFailInParallel_RecordsEveryFailure()
    {
        var scanJobId = Guid.NewGuid();
        var inventory = new FakeFileInventory();
        for (var i = 0; i < 10; i++)
        {
            inventory.Add(scanJobId, CreateFile($"ok-{i}.txt", $"ok {i}"));
        }
        for (var i = 0; i < 60; i++)
        {
            inventory.Add(scanJobId, Path.Combine(_workDir, $"missing-{i}.txt"));
        }

        await using var db = CreateDbContext();
        var service = CreateService(db, inventory, batchSize: 50, parallelism: 16);

        var result = await service.RunAsync(scanJobId, "corr-failures", CancellationToken.None);

        Assert.Equal(70, result.Attempted);
        Assert.Equal(60, result.Failed);
        Assert.Equal(10, result.Succeeded);
        Assert.Equal(60, await db.HashFailures.CountAsync(x => x.HashExecutionId == result.Id));
    }

    [Fact]
    public async Task RunAsync_WhenResultCannotBeRecorded_DoesNotLoopForever()
    {
        var scanJobId = Guid.NewGuid();
        var inventory = new FakeFileInventory { RejectHashResults = true };
        inventory.Add(scanJobId, CreateFile("stuck.txt", "stuck"));

        await using var db = CreateDbContext();
        var service = CreateService(db, inventory, batchSize: 50, parallelism: 1);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await service.RunAsync(scanJobId, "corr-stuck", timeout.Token);

        Assert.Equal(1, result.Attempted);
    }

    [Fact]
    public async Task RunAsync_RetriesFilesThatFailedInAnEarlierRun()
    {
        var scanJobId = Guid.NewGuid();
        var inventory = new FakeFileInventory();
        var id = inventory.Add(scanJobId, CreateFile("retry.txt", "retry"));
        inventory.Files[id].Status = HashStatus.Failed.ToString();

        await using var db = CreateDbContext();
        var service = CreateService(db, inventory, batchSize: 50, parallelism: 1);

        var result = await service.RunAsync(scanJobId, "corr-retry", CancellationToken.None);

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(HashStatus.Completed.ToString(), inventory.Files[id].Status);
    }

    private string CreateFile(string name, string content)
    {
        var path = Path.Combine(_workDir, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    private static HashingDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<HashingDbContext>()
            .UseInMemoryDatabase($"hashing-tests-{Guid.NewGuid()}")
            .Options);

    private static HashingService CreateService(HashingDbContext db, FakeFileInventory inventory, int batchSize, int parallelism)
    {
        var scanJobs = new StubHandler((req, _) => Task.FromResult(
            req.Method == HttpMethod.Get
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.OK)));

        var factory = new StubHttpClientFactory(new Dictionary<string, HttpClient>
        {
            ["fileinventory"] = new(inventory) { BaseAddress = new Uri("http://fileinventory") },
            ["scanjobs"] = new(scanJobs) { BaseAddress = new Uri("http://scanjobs") },
        });

        return new HashingService(
            db,
            factory,
            Options.Create(new HashingWorkerOptions { BatchSize = batchSize, MaxDegreeOfParallelism = parallelism }),
            NullLogger<HashingService>.Instance);
    }

    /// <summary>Minimal in-memory FileInventory: serves pending-hash pages and records hash results.</summary>
    private sealed class FakeFileInventory : HttpMessageHandler
    {
        public sealed class Entry(Guid scanJobId, string path)
        {
            public Guid ScanJobId { get; } = scanJobId;
            public string Path { get; } = path;
            public string Status { get; set; } = HashStatus.Pending.ToString();
        }

        public ConcurrentDictionary<Guid, Entry> Files { get; } = new();
        public bool RejectHashResults { get; init; }
        public int PendingRequests;

        public Guid Add(Guid scanJobId, string path)
        {
            var id = Guid.NewGuid();
            Files[id] = new Entry(scanJobId, path);
            return id;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;

            if (request.Method == HttpMethod.Get && path == "/api/files/pending-hash")
            {
                Interlocked.Increment(ref PendingRequests);
                var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
                var scanJobId = Guid.Parse(query["scanJobId"]!);
                var take = int.Parse(query["take"]!);
                var includeFailed = !string.Equals(query["includeFailed"], "false", StringComparison.OrdinalIgnoreCase);

                var page = Files
                    .Where(kv => kv.Value.ScanJobId == scanJobId
                                 && (kv.Value.Status == HashStatus.Pending.ToString()
                                     || (includeFailed && kv.Value.Status == HashStatus.Failed.ToString())))
                    .OrderBy(kv => kv.Value.Path, StringComparer.Ordinal)
                    .Take(take)
                    .Select(kv => new PendingHashFileDto(kv.Key, Guid.Empty, scanJobId, "corr", kv.Value.Path, 0, DateTimeOffset.UtcNow))
                    .ToList();

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(page) };
            }

            if (request.Method == HttpMethod.Post && path.EndsWith("/hash-result", StringComparison.Ordinal))
            {
                if (RejectHashResults)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                var id = Guid.Parse(path.Split('/')[3]);
                var body = await request.Content!.ReadFromJsonAsync<FileHashResultRequest>(cancellationToken);
                Files[id].Status = body!.HashStatus;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }

    private sealed class StubHttpClientFactory(Dictionary<string, HttpClient> clients) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => clients[name];
    }
}
