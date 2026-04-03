using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Orchestrator.Controllers;
using StorageLens.Services.Orchestrator.Interfaces;
using StorageLens.Services.Orchestrator.Models;
using StorageLens.Services.Orchestrator.Sagas;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Orchestrator.Tests;

public class OrchestratorControllerTests
{
    // -----------------------------------------------------------------------
    // POST /api/orchestrator/scans/start/{locationId}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Start_ReturnsAccepted_WithWorkflowStartResultDto()
    {
        var locationId = Guid.NewGuid();
        var expected = BuildStartResult();
        var stub = new StubWorkflowOrchestratorService { StartResult = expected };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        var result = await controller.Start(locationId, null, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result.Result);
        var dto = Assert.IsType<WorkflowStartResultDto>(accepted.Value);
        Assert.Equal(expected.WorkflowId, dto.WorkflowId);
        Assert.Equal(expected.ScanJobId, dto.ScanJobId);
        Assert.Equal(expected.CorrelationId, dto.CorrelationId);
        Assert.Equal(expected.Status, dto.Status);
    }

    [Fact]
    public async Task Start_PassesLocationId_ToService()
    {
        var locationId = Guid.NewGuid();
        var stub = new StubWorkflowOrchestratorService { StartResult = BuildStartResult() };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        await controller.Start(locationId, null, CancellationToken.None);

        Assert.Equal(locationId, stub.LastStartLocationId);
    }

    [Fact]
    public async Task Start_PassesCancellationToken_ToService()
    {
        var stub = new StubWorkflowOrchestratorService { StartResult = BuildStartResult() };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());
        using var cts = new CancellationTokenSource();

        await controller.Start(Guid.NewGuid(), null, cts.Token);

        Assert.Equal(cts.Token, stub.LastCancellationToken);
    }

    // -----------------------------------------------------------------------
    // GET /api/orchestrator/workflows/{id}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetWorkflow_ReturnsOk_WhenWorkflowExists()
    {
        var id = Guid.NewGuid();
        var expected = BuildWorkflowInstance(id);
        var stub = new StubWorkflowOrchestratorService { WorkflowInstance = expected };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        var result = await controller.GetWorkflow(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<WorkflowInstanceDto>(ok.Value);
        Assert.Equal(expected.Id, dto.Id);
        Assert.Equal(expected.Status, dto.Status);
        Assert.Equal(expected.CorrelationId, dto.CorrelationId);
        Assert.Equal(expected.Steps.Count, dto.Steps.Count);
    }

    [Fact]
    public async Task GetWorkflow_ReturnsNotFound_WhenWorkflowDoesNotExist()
    {
        var stub = new StubWorkflowOrchestratorService { WorkflowInstance = null };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        var result = await controller.GetWorkflow(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetWorkflow_PassesCorrectId_ToService()
    {
        var id = Guid.NewGuid();
        var stub = new StubWorkflowOrchestratorService { WorkflowInstance = BuildWorkflowInstance(id) };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        await controller.GetWorkflow(id, CancellationToken.None);

        Assert.Equal(id, stub.LastGetWorkflowId);
    }

    [Fact]
    public async Task GetWorkflow_PassesCancellationToken_ToService()
    {
        var id = Guid.NewGuid();
        var stub = new StubWorkflowOrchestratorService { WorkflowInstance = BuildWorkflowInstance(id) };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());
        using var cts = new CancellationTokenSource();

        await controller.GetWorkflow(id, cts.Token);

        Assert.Equal(cts.Token, stub.LastCancellationToken);
    }

    // -----------------------------------------------------------------------
    // POST /api/orchestrator/workflows/{id}/resume
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Resume_ReturnsAccepted_WhenWorkflowExists()
    {
        var id = Guid.NewGuid();
        var expected = BuildStartResult();
        var stub = new StubWorkflowOrchestratorService { ResumeResult = expected };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        var result = await controller.Resume(id, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result.Result);
        var dto = Assert.IsType<WorkflowStartResultDto>(accepted.Value);
        Assert.Equal(expected.WorkflowId, dto.WorkflowId);
    }

    [Fact]
    public async Task Resume_ReturnsNotFound_WhenWorkflowDoesNotExist()
    {
        var stub = new StubWorkflowOrchestratorService { ResumeResult = null };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        var result = await controller.Resume(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Resume_PassesCorrectId_ToService()
    {
        var id = Guid.NewGuid();
        var stub = new StubWorkflowOrchestratorService { ResumeResult = BuildStartResult() };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        await controller.Resume(id, CancellationToken.None);

        Assert.Equal(id, stub.LastResumeId);
    }

    // -----------------------------------------------------------------------
    // POST /api/orchestrator/workflows/{id}/retry
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Retry_ReturnsAccepted_WhenWorkflowExists()
    {
        var id = Guid.NewGuid();
        var expected = BuildStartResult();
        var stub = new StubWorkflowOrchestratorService { RetryResult = expected };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        var result = await controller.Retry(id, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result.Result);
        var dto = Assert.IsType<WorkflowStartResultDto>(accepted.Value);
        Assert.Equal(expected.WorkflowId, dto.WorkflowId);
    }

    [Fact]
    public async Task Retry_ReturnsNotFound_WhenWorkflowDoesNotExist()
    {
        var stub = new StubWorkflowOrchestratorService { RetryResult = null };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        var result = await controller.Retry(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Retry_PassesCorrectId_ToService()
    {
        var id = Guid.NewGuid();
        var stub = new StubWorkflowOrchestratorService { RetryResult = BuildStartResult() };
        var controller = new OrchestratorController(stub, new NoOpWorkflowCompensator());

        await controller.Retry(id, CancellationToken.None);

        Assert.Equal(id, stub.LastRetryId);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static WorkflowStartResultDto BuildStartResult() =>
        new(
            WorkflowId: Guid.NewGuid(),
            ScanJobId: Guid.NewGuid(),
            CorrelationId: "corr-orch-001",
            Status: WorkflowStatus.Running.ToString());

    private static WorkflowInstanceDto BuildWorkflowInstance(Guid id)
    {
        var steps = new List<WorkflowStepExecutionDto>
        {
            new("Scanning", WorkflowStatus.Completed.ToString(), DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddMinutes(-7), "OK"),
            new("Hashing",  WorkflowStatus.Completed.ToString(), DateTimeOffset.UtcNow.AddMinutes(-7),  DateTimeOffset.UtcNow.AddMinutes(-4), "OK")
        };

        return new WorkflowInstanceDto(
            Id: id,
            StorageLocationId: Guid.NewGuid(),
            ScanJobId: Guid.NewGuid(),
            CorrelationId: "corr-orch-001",
            RootPath: @"C:\data\test",
            Status: WorkflowStatus.Completed.ToString(),
            CreatedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-15),
            CompletedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-4),
            RetryCount: 0,
            LastError: string.Empty,
            Steps: steps);
    }

    // -----------------------------------------------------------------------
    // Stub
    // -----------------------------------------------------------------------

    private sealed class StubWorkflowOrchestratorService : IWorkflowOrchestratorService
    {
        public WorkflowStartResultDto StartResult { get; set; } = null!;
        public WorkflowInstanceDto? WorkflowInstance { get; set; }
        public WorkflowStartResultDto? ResumeResult { get; set; }
        public WorkflowStartResultDto? RetryResult { get; set; }

        public Guid LastStartLocationId { get; private set; }
        public Guid LastGetWorkflowId { get; private set; }
        public Guid LastResumeId { get; private set; }
        public Guid LastRetryId { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<WorkflowStartResultDto> StartScanAsync(Guid locationId, DateTimeOffset? scheduledStartUtc, CancellationToken cancellationToken)
        {
            LastStartLocationId = locationId;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(StartResult);
        }

        public Task<WorkflowInstanceDto?> GetWorkflowAsync(Guid id, CancellationToken cancellationToken)
        {
            LastGetWorkflowId = id;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(WorkflowInstance);
        }

        public Task<WorkflowStartResultDto?> ResumeAsync(Guid id, CancellationToken cancellationToken)
        {
            LastResumeId = id;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(ResumeResult);
        }

        public Task<WorkflowStartResultDto?> RetryAsync(Guid id, CancellationToken cancellationToken)
        {
            LastRetryId = id;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(RetryResult);
        }

        public Task<bool> CancelByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken)
        {
            LastCancellationToken = cancellationToken;
            return Task.FromResult(true);
        }

        public Task<WorkflowStartResultDto?> ResumeByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken)
        {
            LastCancellationToken = cancellationToken;
            return Task.FromResult(ResumeResult);
        }

        public ValueTask EnqueueAsync(Guid workflowId, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public Task ProcessWorkflowAsync(Guid workflowId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class NoOpWorkflowCompensator : IWorkflowCompensator
    {
        public Task CompensateAsync(WorkflowInstance workflow, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
