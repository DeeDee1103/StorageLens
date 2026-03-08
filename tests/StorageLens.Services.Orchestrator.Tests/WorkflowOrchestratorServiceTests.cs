using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StorageLens.Services.Orchestrator.Data;
using StorageLens.Services.Orchestrator.Models;
using StorageLens.Services.Orchestrator.Services;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Orchestrator.Tests;

public class WorkflowOrchestratorServiceTests
{
    [Fact]
    public async Task ProcessWorkflowAsync_WhenScannerFails_MarksWorkflowFailed_AndStopsNextSteps()
    {
        await using var dbContext = CreateDbContext();

        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        dbContext.WorkflowInstances.Add(new WorkflowInstance
        {
            Id = workflowId,
            StorageLocationId = locationId,
            ScanJobId = scanJobId,
            CorrelationId = "corr-123",
            RootPath = @"A:\\data",
            Status = WorkflowStatus.Pending.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var locationsHandler = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == $"/api/locations/{locationId}")
            {
                var dto = new StorageLocationDto(
                    locationId,
                    "Primary",
                    @"A:\\data",
                    StorageLocationType.Local.ToString(),
                    LocationStatus.Online.ToString(),
                    null,
                    0,
                    0,
                    DateTimeOffset.UtcNow);
                return Task.FromResult(JsonResponse(dto));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var scanJobsHandler = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Put && request.RequestUri?.AbsolutePath == $"/api/scanjobs/{scanJobId}/status")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var scannerHandler = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/api/scanner/execute")
            {
                var failed = new ScannerExecutionDto(
                    Guid.NewGuid(),
                    locationId,
                    scanJobId,
                    "corr-123",
                    @"A:\\data",
                    "Failed",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    0,
                    1,
                    "boom");

                return Task.FromResult(JsonResponse(failed, HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var hashingHandler = new StubHttpMessageHandler((request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var duplicatesHandler = new StubHttpMessageHandler((request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var analyticsHandler = new StubHttpMessageHandler((request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var clientFactory = new StubHttpClientFactory(new Dictionary<string, HttpClient>
        {
            ["locations"] = CreateClient("http://locations", locationsHandler),
            ["scanjobs"] = CreateClient("http://scanjobs", scanJobsHandler),
            ["scanner"] = CreateClient("http://scanner", scannerHandler),
            ["hashing"] = CreateClient("http://hashing", hashingHandler),
            ["duplicates"] = CreateClient("http://duplicates", duplicatesHandler),
            ["analytics"] = CreateClient("http://analytics", analyticsHandler)
        });

        var service = new WorkflowOrchestratorService(
            dbContext,
            new WorkflowQueue(),
            clientFactory,
            NullLogger<WorkflowOrchestratorService>.Instance);

        await service.ProcessWorkflowAsync(workflowId, CancellationToken.None);

        var updated = await dbContext.WorkflowInstances
            .Include(x => x.Steps)
            .FirstAsync(x => x.Id == workflowId);

        Assert.Equal(WorkflowStatus.Failed.ToString(), updated.Status);
        Assert.Contains(updated.Steps, x => x.StepName == "Scanning" && x.Status == "Failed");
        Assert.DoesNotContain(updated.Steps, x => x.StepName == "Hashing");

        Assert.Equal(1, scannerHandler.RequestCount);
        Assert.Equal(0, hashingHandler.RequestCount);

        var errors = await dbContext.WorkflowErrors.Where(x => x.WorkflowInstanceId == workflowId).ToListAsync();
        Assert.NotEmpty(errors);
    }

    private static OrchestratorDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase($"orchestrator-tests-{Guid.NewGuid()}")
            .Options;

        return new OrchestratorDbContext(options);
    }

    private static HttpClient CreateClient(string baseAddress, HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri(baseAddress) };

    private static HttpResponseMessage JsonResponse<T>(T body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(body)
        };

        return response;
    }

    private sealed class StubHttpClientFactory(Dictionary<string, HttpClient> clients) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            if (!clients.TryGetValue(name, out var client))
            {
                throw new InvalidOperationException($"No client configured for '{name}'.");
            }

            return client;
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount += 1;
            return await handler(request, cancellationToken);
        }
    }
}
