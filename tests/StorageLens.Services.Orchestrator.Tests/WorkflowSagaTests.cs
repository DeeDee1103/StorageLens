using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StorageLens.Services.Orchestrator.Data;
using StorageLens.Services.Orchestrator.Models;
using StorageLens.Services.Orchestrator.Repositories;
using StorageLens.Services.Orchestrator.Sagas;
using StorageLens.Services.Orchestrator.Services;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Orchestrator.Tests;

/// <summary>
/// Tests for the Saga pattern: compensating transactions and step-resume (checkpoint) behaviour.
/// </summary>
public class WorkflowSagaTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static WorkflowOrchestratorService CreateService(
        OrchestratorDbContext dbContext,
        IHttpClientFactory clientFactory,
        IWorkflowCompensator? compensator = null) =>
        new(
            new WorkflowRepository(dbContext),
            new WorkflowQueue(),
            clientFactory,
            compensator ?? new NoOpWorkflowCompensator(),
            NullLogger<WorkflowOrchestratorService>.Instance);

    private static OrchestratorDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase($"saga-tests-{Guid.NewGuid()}")
            .Options);

    private static HttpClient CreateClient(string baseAddress, HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri(baseAddress) };

    private static HttpResponseMessage JsonResponse<T>(T body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = JsonContent.Create(body) };

    // -----------------------------------------------------------------------
    // Step-resume: skipping already-completed steps
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ProcessWorkflowAsync_WhenRetryQueued_SkipsAlreadyCompletedSteps()
    {
        await using var dbContext = CreateDbContext();

        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        // Create workflow that was previously attempted: Scanning already completed.
        dbContext.WorkflowInstances.Add(new WorkflowInstance
        {
            Id = workflowId,
            StorageLocationId = locationId,
            ScanJobId = scanJobId,
            CorrelationId = "corr-resume",
            RootPath = @"A:\\data",
            Status = WorkflowStatus.RetryQueued.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 1
        });
        await dbContext.SaveChangesAsync();

        // Simulate completed Scanning step from previous run.
        dbContext.WorkflowStepExecutions.Add(new WorkflowStepExecution
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            StepName = "Scanning",
            Status = WorkflowStepStatus.Completed.ToString(),
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            CompletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-4),
            Notes = "Completed"
        });
        await dbContext.SaveChangesAsync();

        var locationsHandler = new StubHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                var dto = new StorageLocationDto(
                    locationId, "Primary", @"A:\\data",
                    StorageLocationType.Local.ToString(),
                    LocationStatus.Online.ToString(),
                    null, 0, 0, DateTimeOffset.UtcNow,
                    "Site A", null, null);
                return Task.FromResult(JsonResponse(dto));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var scanJobsHandler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var scannerHandler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var hashingHandler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var duplicatesHandler = new StubHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                var result = new DuplicateRecalculationResultDto(scanJobId, "corr-resume", 0, 0, 0, DateTimeOffset.UtcNow);
                return Task.FromResult(JsonResponse(result));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var analyticsHandler = new StubHttpMessageHandler((_, _) =>
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

        var service = CreateService(dbContext, clientFactory);
        await service.ProcessWorkflowAsync(workflowId, CancellationToken.None);

        var updated = await dbContext.WorkflowInstances
            .Include(x => x.Steps)
            .FirstAsync(x => x.Id == workflowId);

        // Workflow should complete.
        Assert.Equal(WorkflowStatus.Completed.ToString(), updated.Status);

        // Scanner should NOT have been called again (Scanning was already completed).
        Assert.Equal(0, scannerHandler.RequestCount);

        // Hashing, Duplicates, Analytics should have run.
        Assert.True(hashingHandler.RequestCount >= 1);
        Assert.True(duplicatesHandler.RequestCount >= 1);
        Assert.True(analyticsHandler.RequestCount >= 1);
    }

    // -----------------------------------------------------------------------
    // WorkflowCompensator: compensation calls on failure
    // -----------------------------------------------------------------------

    [Fact]
    public async Task WorkflowCompensator_WhenScanningCompleted_CallsFileInventoryDelete()
    {
        // Arrange a workflow that completed Scanning.
        var workflowId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var correlationId = "corr-compensate";

        var fileInventoryDeleteCalled = false;
        string? capturedUrl = null;

        var fileInventoryHandler = new StubHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Delete)
            {
                fileInventoryDeleteCalled = true;
                capturedUrl = request.RequestUri?.ToString();
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var clientFactory = new StubHttpClientFactory(new Dictionary<string, HttpClient>
        {
            ["fileinventory"] = CreateClient("http://fileinventory", fileInventoryHandler),
            ["duplicates"] = CreateClient("http://duplicates", new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))))
        });

        var compensator = new WorkflowCompensator(clientFactory, NullLogger<WorkflowCompensator>.Instance);

        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ScanJobId = scanJobId,
            CorrelationId = correlationId,
            StorageLocationId = Guid.NewGuid(),
            RootPath = @"A:\\data",
            Status = WorkflowStatus.Failed.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Steps = new List<WorkflowStepExecution>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = workflowId,
                    StepName = "Scanning",
                    Status = WorkflowStepStatus.Completed.ToString(),
                    StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-3),
                    CompletedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = workflowId,
                    StepName = "Hashing",
                    Status = WorkflowStepStatus.Failed.ToString(),
                    StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2)
                }
            }
        };

        // Act
        await compensator.CompensateAsync(workflow, CancellationToken.None);

        // Assert
        Assert.True(fileInventoryDeleteCalled, "FileInventory DELETE should have been called to compensate the Scanning step.");
        Assert.NotNull(capturedUrl);
        Assert.Contains(scanJobId.ToString(), capturedUrl);
        Assert.Contains(Uri.EscapeDataString(correlationId), capturedUrl);
    }

    [Fact]
    public async Task WorkflowCompensator_WhenDuplicatesCompleted_CallsDuplicatesDelete()
    {
        var workflowId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var correlationId = "corr-dups-compensate";

        var duplicatesDeleteCalled = false;

        var clientFactory = new StubHttpClientFactory(new Dictionary<string, HttpClient>
        {
            ["fileinventory"] = CreateClient("http://fileinventory", new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))),
            ["duplicates"] = CreateClient("http://duplicates", new StubHttpMessageHandler((request, _) =>
            {
                if (request.Method == HttpMethod.Delete)
                {
                    duplicatesDeleteCalled = true;
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }))
        });

        var compensator = new WorkflowCompensator(clientFactory, NullLogger<WorkflowCompensator>.Instance);

        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ScanJobId = scanJobId,
            CorrelationId = correlationId,
            StorageLocationId = Guid.NewGuid(),
            RootPath = @"A:\\data",
            Status = WorkflowStatus.Failed.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Steps = new List<WorkflowStepExecution>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = workflowId,
                    StepName = "Scanning",
                    Status = WorkflowStepStatus.Completed.ToString(),
                    StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = workflowId,
                    StepName = "Hashing",
                    Status = WorkflowStepStatus.Completed.ToString(),
                    StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-8)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = workflowId,
                    StepName = "DetectingDuplicates",
                    Status = WorkflowStepStatus.Completed.ToString(),
                    StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-6)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = workflowId,
                    StepName = "RefreshingAnalytics",
                    Status = WorkflowStepStatus.Failed.ToString(),
                    StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-4)
                }
            }
        };

        await compensator.CompensateAsync(workflow, CancellationToken.None);

        Assert.True(duplicatesDeleteCalled, "Duplicates DELETE should have been called to compensate the DetectingDuplicates step.");
    }

    [Fact]
    public async Task WorkflowCompensator_WhenNoStepsCompleted_MakesNoCalls()
    {
        var callCount = 0;
        var allHandler = new StubHttpMessageHandler((_, _) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var clientFactory = new StubHttpClientFactory(new Dictionary<string, HttpClient>
        {
            ["fileinventory"] = CreateClient("http://fileinventory", allHandler),
            ["duplicates"] = CreateClient("http://duplicates", allHandler)
        });

        var compensator = new WorkflowCompensator(clientFactory, NullLogger<WorkflowCompensator>.Instance);

        var workflow = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            ScanJobId = Guid.NewGuid(),
            CorrelationId = "corr-none",
            StorageLocationId = Guid.NewGuid(),
            RootPath = @"A:\\data",
            Status = WorkflowStatus.Failed.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Steps = new List<WorkflowStepExecution>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = Guid.NewGuid(),
                    StepName = "Scanning",
                    Status = WorkflowStepStatus.Failed.ToString(),
                    StartedAtUtc = DateTimeOffset.UtcNow
                }
            }
        };

        await compensator.CompensateAsync(workflow, CancellationToken.None);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public async Task ProcessWorkflowAsync_WhenHashingFails_TriggesCompensation()
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
            CorrelationId = "corr-comp-trigger",
            RootPath = @"A:\\data",
            Status = WorkflowStatus.Pending.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var compensationCalled = false;
        var capturedWorkflowId = Guid.Empty;

        var trackingCompensator = new TrackingWorkflowCompensator(wf =>
        {
            compensationCalled = true;
            capturedWorkflowId = wf.Id;
            return Task.CompletedTask;
        });

        var locationsHandler = new StubHttpMessageHandler((req, _) =>
        {
            if (req.Method == HttpMethod.Get)
            {
                var dto = new StorageLocationDto(
                    locationId, "Primary", @"A:\\data",
                    StorageLocationType.Local.ToString(),
                    LocationStatus.Online.ToString(),
                    null, 0, 0, DateTimeOffset.UtcNow,
                    "Site A", null, null);
                return Task.FromResult(JsonResponse(dto));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var scanJobsHandler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var scannerHandler = new StubHttpMessageHandler((req, _) =>
        {
            if (req.Method == HttpMethod.Post)
            {
                var result = new ScannerExecutionDto(
                    Guid.NewGuid(), locationId, scanJobId, "corr-comp-trigger",
                    @"A:\\data", "Completed", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 5, 0, string.Empty);
                return Task.FromResult(JsonResponse(result));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var hashingHandler = new StubHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Simulated hashing failure."));

        var clientFactory = new StubHttpClientFactory(new Dictionary<string, HttpClient>
        {
            ["locations"] = CreateClient("http://locations", locationsHandler),
            ["scanjobs"] = CreateClient("http://scanjobs", scanJobsHandler),
            ["scanner"] = CreateClient("http://scanner", scannerHandler),
            ["hashing"] = CreateClient("http://hashing", hashingHandler),
            ["duplicates"] = CreateClient("http://duplicates", new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))),
            ["analytics"] = CreateClient("http://analytics", new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))))
        });

        var service = CreateService(dbContext, clientFactory, trackingCompensator);
        await service.ProcessWorkflowAsync(workflowId, CancellationToken.None);

        var updated = await dbContext.WorkflowInstances
            .Include(x => x.Steps)
            .FirstAsync(x => x.Id == workflowId);

        Assert.Equal(WorkflowStatus.Failed.ToString(), updated.Status);
        Assert.True(compensationCalled, "Compensator should have been called when workflow fails.");
        Assert.Equal(workflowId, capturedWorkflowId);
    }

    // -----------------------------------------------------------------------
    // Stub helpers
    // -----------------------------------------------------------------------

    private sealed class StubHttpClientFactory(Dictionary<string, HttpClient> clients) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            if (clients.TryGetValue(name, out var client))
            {
                return client;
            }

            return new HttpClient(new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))))
            { BaseAddress = new Uri("http://localhost") };
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

    private sealed class NoOpWorkflowCompensator : IWorkflowCompensator
    {
        public Task CompensateAsync(WorkflowInstance workflow, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class TrackingWorkflowCompensator(Func<WorkflowInstance, Task> onCompensate) : IWorkflowCompensator
    {
        public Task CompensateAsync(WorkflowInstance workflow, CancellationToken cancellationToken) =>
            onCompensate(workflow);
    }
}
