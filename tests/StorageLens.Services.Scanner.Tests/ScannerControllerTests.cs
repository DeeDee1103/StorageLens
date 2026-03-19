using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Scanner.Controllers;
using StorageLens.Services.Scanner.Interfaces;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Scanner.Tests;

public class ScannerControllerTests
{
    // -----------------------------------------------------------------------
    // POST /api/scanner/execute
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Execute_ReturnsOk_WhenScanSucceeds()
    {
        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var request = BuildRequest(locationId, scanJobId);
        var expected = BuildExecution(locationId, scanJobId, "Completed");
        var stub = new StubScannerExecutionService { ExecuteResult = expected };
        var controller = new ScannerController(stub);

        var result = await controller.Execute(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ScannerExecutionDto>(ok.Value);
        Assert.Equal(expected.Id, dto.Id);
        Assert.Equal(expected.ScanJobId, dto.ScanJobId);
        Assert.Equal(expected.StorageLocationId, dto.StorageLocationId);
        Assert.Equal("Completed", dto.Status);
        Assert.Equal(expected.FilesDiscovered, dto.FilesDiscovered);
    }

    [Fact]
    public async Task Execute_Returns500_WhenScanFails()
    {
        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var request = BuildRequest(locationId, scanJobId);
        var failed = BuildExecution(locationId, scanJobId, "Failed");
        var stub = new StubScannerExecutionService { ExecuteResult = failed };
        var controller = new ScannerController(stub);

        var result = await controller.Execute(request, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
        var dto = Assert.IsType<ScannerExecutionDto>(statusResult.Value);
        Assert.Equal("Failed", dto.Status);
    }

    [Fact]
    public async Task Execute_PassesRequest_ToService()
    {
        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var request = BuildRequest(locationId, scanJobId);
        var stub = new StubScannerExecutionService { ExecuteResult = BuildExecution(locationId, scanJobId, "Completed") };
        var controller = new ScannerController(stub);

        await controller.Execute(request, CancellationToken.None);

        Assert.Same(request, stub.LastRequest);
    }

    [Fact]
    public async Task Execute_PassesCancellationToken_ToService()
    {
        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var stub = new StubScannerExecutionService { ExecuteResult = BuildExecution(locationId, scanJobId, "Completed") };
        var controller = new ScannerController(stub);
        using var cts = new CancellationTokenSource();

        await controller.Execute(BuildRequest(locationId, scanJobId), cts.Token);

        Assert.Equal(cts.Token, stub.LastCancellationToken);
    }

    [Fact]
    public async Task Execute_ReturnsOk_WithZeroFiles_WhenDirectoryIsEmpty()
    {
        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var empty = BuildExecution(locationId, scanJobId, "Completed", filesDiscovered: 0);
        var stub = new StubScannerExecutionService { ExecuteResult = empty };
        var controller = new ScannerController(stub);

        var result = await controller.Execute(BuildRequest(locationId, scanJobId), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ScannerExecutionDto>(ok.Value);
        Assert.Equal(0L, dto.FilesDiscovered);
    }

    // -----------------------------------------------------------------------
    // GET /api/scanner/executions/{id}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetExecution_ReturnsOk_WhenExecutionExists()
    {
        var id = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();
        var expected = BuildExecution(locationId, scanJobId, "Completed", id: id);
        var stub = new StubScannerExecutionService { GetByIdResult = expected };
        var controller = new ScannerController(stub);

        var result = await controller.GetExecution(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ScannerExecutionDto>(ok.Value);
        Assert.Equal(id, dto.Id);
        Assert.Equal("Completed", dto.Status);
    }

    [Fact]
    public async Task GetExecution_ReturnsNotFound_WhenExecutionDoesNotExist()
    {
        var stub = new StubScannerExecutionService { GetByIdResult = null };
        var controller = new ScannerController(stub);

        var result = await controller.GetExecution(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetExecution_PassesCorrectId_ToService()
    {
        var id = Guid.NewGuid();
        var expected = BuildExecution(Guid.NewGuid(), Guid.NewGuid(), "Completed", id: id);
        var stub = new StubScannerExecutionService { GetByIdResult = expected };
        var controller = new ScannerController(stub);

        await controller.GetExecution(id, CancellationToken.None);

        Assert.Equal(id, stub.LastGetByIdId);
    }

    [Fact]
    public async Task GetExecution_PassesCancellationToken_ToService()
    {
        var id = Guid.NewGuid();
        var expected = BuildExecution(Guid.NewGuid(), Guid.NewGuid(), "Completed", id: id);
        var stub = new StubScannerExecutionService { GetByIdResult = expected };
        var controller = new ScannerController(stub);
        using var cts = new CancellationTokenSource();

        await controller.GetExecution(id, cts.Token);

        Assert.Equal(cts.Token, stub.LastCancellationToken);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static ScannerExecuteRequest BuildRequest(Guid locationId, Guid scanJobId) =>
        new(
            StorageLocationId: locationId,
            ScanJobId: scanJobId,
            CorrelationId: "corr-test-001",
            LocationName: "Test Location",
            RootPath: @"C:\data\test");

    private static ScannerExecutionDto BuildExecution(
        Guid locationId,
        Guid scanJobId,
        string status,
        long filesDiscovered = 12_500L,
        Guid? id = null) =>
        new(
            Id: id ?? Guid.NewGuid(),
            StorageLocationId: locationId,
            ScanJobId: scanJobId,
            CorrelationId: "corr-test-001",
            RootPath: @"C:\data\test",
            Status: status,
            StartedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
            CompletedAtUtc: DateTimeOffset.UtcNow,
            FilesDiscovered: filesDiscovered,
            ErrorCount: 0,
            Notes: status == "Failed" ? "Scan error" : "OK");

    // -----------------------------------------------------------------------
    // Stub
    // -----------------------------------------------------------------------

    private sealed class StubScannerExecutionService : IScannerExecutionService
    {
        public ScannerExecutionDto ExecuteResult { get; set; } = null!;
        public ScannerExecutionDto? GetByIdResult { get; set; }

        public ScannerExecuteRequest? LastRequest { get; private set; }
        public Guid LastGetByIdId { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<ScannerExecutionDto> ExecuteAsync(ScannerExecuteRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(ExecuteResult);
        }

        public Task<ScannerExecutionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            LastGetByIdId = id;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(GetByIdResult);
        }
    }
}
