using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Hashing.Controllers;
using StorageLens.Services.Hashing.Interfaces;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Hashing.Tests;

public class HashingControllerTests
{
    // -----------------------------------------------------------------------
    // POST /api/hashing/run/{scanJobId}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Run_ReturnsOk_WithHashingRunResultDto()
    {
        var scanJobId = Guid.NewGuid();
        var correlationId = "corr-abc-001";
        var expected = BuildRunResult(scanJobId, correlationId);
        var stub = new StubHashingService { RunResult = expected };
        var controller = new HashingController(stub);

        var result = await controller.Run(scanJobId, correlationId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HashingRunResultDto>(ok.Value);
        Assert.Equal(expected.Id, dto.Id);
        Assert.Equal(expected.ScanJobId, dto.ScanJobId);
        Assert.Equal(expected.CorrelationId, dto.CorrelationId);
        Assert.Equal(expected.Attempted, dto.Attempted);
        Assert.Equal(expected.Succeeded, dto.Succeeded);
        Assert.Equal(expected.Failed, dto.Failed);
    }

    [Fact]
    public async Task Run_PassesCorrectArguments_ToService()
    {
        var scanJobId = Guid.NewGuid();
        var correlationId = "corr-xyz-999";
        var stub = new StubHashingService { RunResult = BuildRunResult(scanJobId, correlationId) };
        var controller = new HashingController(stub);

        await controller.Run(scanJobId, correlationId, CancellationToken.None);

        Assert.Equal(scanJobId, stub.LastRunScanJobId);
        Assert.Equal(correlationId, stub.LastRunCorrelationId);
    }

    [Fact]
    public async Task Run_PassesCancellationToken_ToService()
    {
        var scanJobId = Guid.NewGuid();
        var stub = new StubHashingService { RunResult = BuildRunResult(scanJobId, "c") };
        var controller = new HashingController(stub);
        using var cts = new CancellationTokenSource();

        await controller.Run(scanJobId, "c", cts.Token);

        Assert.Equal(cts.Token, stub.LastCancellationToken);
    }

    [Fact]
    public async Task Run_ReturnsOk_WhenNoFilesWereAttempted()
    {
        var scanJobId = Guid.NewGuid();
        var result = new HashingRunResultDto(
            Guid.NewGuid(),
            scanJobId,
            "corr-empty",
            Attempted: 0,
            Succeeded: 0,
            Failed: 0,
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow);

        var stub = new StubHashingService { RunResult = result };
        var controller = new HashingController(stub);

        var actionResult = await controller.Run(scanJobId, "corr-empty", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<HashingRunResultDto>(ok.Value);
        Assert.Equal(0, dto.Attempted);
        Assert.Equal(0, dto.Failed);
    }

    // -----------------------------------------------------------------------
    // GET /api/hashing/executions/{id}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetExecution_ReturnsOk_WhenExecutionExists()
    {
        var id = Guid.NewGuid();
        var expected = BuildRunResult(Guid.NewGuid(), "corr-get-001", id);
        var stub = new StubHashingService { Execution = expected };
        var controller = new HashingController(stub);

        var result = await controller.GetExecution(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<HashingRunResultDto>(ok.Value);
        Assert.Equal(expected.Id, dto.Id);
        Assert.Equal(expected.CorrelationId, dto.CorrelationId);
    }

    [Fact]
    public async Task GetExecution_ReturnsNotFound_WhenExecutionDoesNotExist()
    {
        var stub = new StubHashingService { Execution = null };
        var controller = new HashingController(stub);

        var result = await controller.GetExecution(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetExecution_PassesCorrectId_ToService()
    {
        var id = Guid.NewGuid();
        var stub = new StubHashingService { Execution = BuildRunResult(Guid.NewGuid(), "c", id) };
        var controller = new HashingController(stub);

        await controller.GetExecution(id, CancellationToken.None);

        Assert.Equal(id, stub.LastGetExecutionId);
    }

    [Fact]
    public async Task GetExecution_PassesCancellationToken_ToService()
    {
        var id = Guid.NewGuid();
        var stub = new StubHashingService { Execution = BuildRunResult(Guid.NewGuid(), "c", id) };
        var controller = new HashingController(stub);
        using var cts = new CancellationTokenSource();

        await controller.GetExecution(id, cts.Token);

        Assert.Equal(cts.Token, stub.LastCancellationToken);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static HashingRunResultDto BuildRunResult(
        Guid scanJobId,
        string correlationId,
        Guid? id = null)
    {
        var started = DateTimeOffset.UtcNow.AddMinutes(-2);
        return new HashingRunResultDto(
            id ?? Guid.NewGuid(),
            scanJobId,
            correlationId,
            Attempted: 100,
            Succeeded: 97,
            Failed: 3,
            StartedAtUtc: started,
            CompletedAtUtc: started.AddMinutes(2));
    }

    // -----------------------------------------------------------------------
    // Stub
    // -----------------------------------------------------------------------

    private sealed class StubHashingService : IHashingService
    {
        public HashingRunResultDto RunResult { get; set; } = null!;
        public HashingRunResultDto? Execution { get; set; }

        public Guid LastRunScanJobId { get; private set; }
        public string LastRunCorrelationId { get; private set; } = string.Empty;
        public Guid LastGetExecutionId { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<HashingRunResultDto> RunAsync(Guid scanJobId, string correlationId, CancellationToken cancellationToken)
        {
            LastRunScanJobId = scanJobId;
            LastRunCorrelationId = correlationId;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(RunResult);
        }

        public Task<HashingRunResultDto?> GetExecutionAsync(Guid id, CancellationToken cancellationToken)
        {
            LastGetExecutionId = id;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Execution);
        }
    }
}
