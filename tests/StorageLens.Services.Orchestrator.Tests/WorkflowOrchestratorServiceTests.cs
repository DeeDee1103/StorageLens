using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StorageLens.Services.Orchestrator.Data;
using StorageLens.Services.Orchestrator.Models;
using StorageLens.Services.Orchestrator.Services;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Orchestrator.Tests;

public class WorkflowOrchestratorServiceTests
{
    [Fact]
    public async Task ProcessWorkflowAsync_WhenAllStepsSucceed_CompletesWorkflow_AndRecordsAllSteps()
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
            CorrelationId = "corr-success",
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
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == $"/api/scanjobs/{scanJobId}")
            {
                var dto = new ScanJobDto(
                    scanJobId,
                    locationId,
                    "Primary",
                    "Completed",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    10,
                    100,
                    "Workflow completed",
                    CurrentStage: "Completed",
                    CorrelationId: "corr-success",
                    FilesHashed: 10,
                    DuplicateGroupsFound: 1,
                    WarningCount: 0,
                    ErrorCount: 0);

                return Task.FromResult(JsonResponse(dto));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var scannerHandler = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/api/scanner/execute")
            {
                var success = new ScannerExecutionDto(
                    Guid.NewGuid(),
                    locationId,
                    scanJobId,
                    "corr-success",
                    @"A:\\data",
                    "Completed",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    10,
                    0,
                    string.Empty);
                return Task.FromResult(JsonResponse(success));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var hashingHandler = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == $"/api/hashing/run/{scanJobId}")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var duplicatesHandler = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == $"/api/duplicates/recalculate/{scanJobId}")
            {
                var dto = new DuplicateRecalculationResultDto(scanJobId, "corr-success", 1, 2, 3, DateTimeOffset.UtcNow);
                return Task.FromResult(JsonResponse(dto));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var analyticsHandler = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/api/analytics/dashboard")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

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

        Assert.Equal(WorkflowStatus.Completed.ToString(), updated.Status);
        Assert.NotNull(updated.CompletedAtUtc);
        Assert.Equal(string.Empty, updated.LastError);

        var stepNames = updated.Steps.OrderBy(x => x.StartedAtUtc).Select(x => x.StepName).ToList();
        Assert.Equal(["Scanning", "Hashing", "DetectingDuplicates", "RefreshingAnalytics"], stepNames);
        Assert.All(updated.Steps, step => Assert.Equal("Completed", step.Status));

        Assert.Equal(1, scannerHandler.RequestCount);
        Assert.Equal(1, hashingHandler.RequestCount);
        Assert.Equal(1, duplicatesHandler.RequestCount);
        Assert.Equal(1, analyticsHandler.RequestCount);
    }

    [Fact]
    public async Task ProcessWorkflowAsync_WhenWorkflowCompleted_SkipsWithoutCallingUpstreams()
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
            CorrelationId = "corr-skip",
            RootPath = @"A:\\data",
            Status = WorkflowStatus.Completed.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var scannerHandler = new StubHttpMessageHandler((request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var clientFactory = new StubHttpClientFactory(new Dictionary<string, HttpClient>
        {
            ["scanjobs"] = CreateClient("http://scanjobs", new StubHttpMessageHandler((request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))),
            ["locations"] = CreateClient("http://locations", new StubHttpMessageHandler((request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))),
            ["scanner"] = CreateClient("http://scanner", scannerHandler),
            ["hashing"] = CreateClient("http://hashing", new StubHttpMessageHandler((request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))),
            ["duplicates"] = CreateClient("http://duplicates", new StubHttpMessageHandler((request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))),
            ["analytics"] = CreateClient("http://analytics", new StubHttpMessageHandler((request, cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))))
        });

        var service = new WorkflowOrchestratorService(
            dbContext,
            new WorkflowQueue(),
            clientFactory,
            NullLogger<WorkflowOrchestratorService>.Instance);

        await service.ProcessWorkflowAsync(workflowId, CancellationToken.None);

        var unchanged = await dbContext.WorkflowInstances.FirstAsync(x => x.Id == workflowId);
        Assert.Equal(WorkflowStatus.Completed.ToString(), unchanged.Status);
        Assert.Equal(0, scannerHandler.RequestCount);
    }

    [Fact]
    public async Task ProcessWorkflowAsync_WhenHashingThrowsHttpRequestException_MarksWorkflowFailed_AndSkipsRemainingSteps()
    {
        await using var dbContext = CreateDbContext();

        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var statusEndpoints = new List<string>();

        dbContext.WorkflowInstances.Add(new WorkflowInstance
        {
            Id = workflowId,
            StorageLocationId = locationId,
            ScanJobId = scanJobId,
            CorrelationId = "corr-hashing-fault",
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
            if (request.Method == HttpMethod.Put && request.RequestUri is not null && request.RequestUri.AbsolutePath == $"/api/scanjobs/{scanJobId}/status")
            {
                statusEndpoints.Add(request.RequestUri.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var scannerHandler = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == "/api/scanner/execute")
            {
                var success = new ScannerExecutionDto(
                    Guid.NewGuid(),
                    locationId,
                    scanJobId,
                    "corr-hashing-fault",
                    @"A:\\data",
                    "Completed",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    5,
                    0,
                    string.Empty);

                return Task.FromResult(JsonResponse(success));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var hashingHandler = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath == $"/api/hashing/run/{scanJobId}")
            {
                throw new HttpRequestException("Simulated transient network fault during hashing call.");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

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
        Assert.Contains(updated.Steps, x => x.StepName == "Scanning" && x.Status == "Completed");
        Assert.Contains(updated.Steps, x => x.StepName == "Hashing" && x.Status == "Failed");
        Assert.DoesNotContain(updated.Steps, x => x.StepName == "DetectingDuplicates");
        Assert.DoesNotContain(updated.Steps, x => x.StepName == "RefreshingAnalytics");

        Assert.Equal(1, scannerHandler.RequestCount);
        Assert.Equal(1, hashingHandler.RequestCount);
        Assert.Equal(0, duplicatesHandler.RequestCount);
        Assert.Equal(0, analyticsHandler.RequestCount);

        var errors = await dbContext.WorkflowErrors
            .Where(x => x.WorkflowInstanceId == workflowId)
            .ToListAsync();

        Assert.Contains(errors, x => x.StepName == "Hashing");
        Assert.Contains(errors, x => x.StepName == "Workflow");
        Assert.Contains(errors, x => x.Message.Contains("network fault", StringComparison.OrdinalIgnoreCase));

        Assert.True(statusEndpoints.Count >= 3);
    }

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
