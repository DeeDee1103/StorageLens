using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.ScanJobs.Controllers;
using StorageLens.Services.ScanJobs.Interfaces;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.ScanJobs.Tests;

public class ScanJobsControllerTests
{
    private static ScanJobDto MakeDto(string locationName = "Test Location") =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            locationName,
            ScanJobStatus.Queued.ToString(),
            DateTimeOffset.UtcNow,
            null,
            0,
            0,
            string.Empty,
            "Queued",
            Guid.NewGuid().ToString("N"));

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var stub = new StubScanJobsService { All = [MakeDto("A"), MakeDto("B")] };
        var controller = new ScanJobsController(stub);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<ScanJobDto>>(ok.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetAll_EmptyDb_ReturnsOkWithEmptyList()
    {
        var stub = new StubScanJobsService { All = [] };
        var controller = new ScanJobsController(stub);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<ScanJobDto>>(ok.Value);
        Assert.Empty(list);
    }

    // ── GetRecent ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecent_ReturnsOkWithList()
    {
        var stub = new StubScanJobsService { Recent = [MakeDto("X"), MakeDto("Y")] };
        var controller = new ScanJobsController(stub);

        var result = await controller.GetRecent(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<ScanJobDto>>(ok.Value);
        Assert.Equal(2, list.Count);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var dto = MakeDto();
        var stub = new StubScanJobsService { ById = dto };
        var controller = new ScanJobsController(stub);

        var result = await controller.GetById(dto.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var stub = new StubScanJobsService { ById = null };
        var controller = new ScanJobsController(stub);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAtAction()
    {
        var dto = MakeDto("New Location");
        var stub = new StubScanJobsService { Created = dto };
        var controller = new ScanJobsController(stub);

        var result = await controller.Create(
            new CreateScanJobRequest(Guid.NewGuid(), "Initial scan"),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
        Assert.Equal(nameof(controller.GetById), created.ActionName);
    }

    // ── UpdateStatus ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ExistingId_ReturnsOkWithDto()
    {
        var dto = MakeDto();
        var stub = new StubScanJobsService { UpdatedStatus = dto };
        var controller = new ScanJobsController(stub);

        var result = await controller.UpdateStatus(
            dto.Id,
            new UpdateScanJobStatusRequest(ScanJobStatus.Scanning, "Scanning"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task UpdateStatus_UnknownId_ReturnsNotFound()
    {
        var stub = new StubScanJobsService { UpdatedStatus = null };
        var controller = new ScanJobsController(stub);

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new UpdateScanJobStatusRequest(ScanJobStatus.Scanning, "Scanning"),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── UpdateProgress ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProgress_ExistingId_ReturnsOkWithDto()
    {
        var dto = MakeDto();
        var stub = new StubScanJobsService { UpdatedProgress = dto };
        var controller = new ScanJobsController(stub);

        var result = await controller.UpdateProgress(
            dto.Id,
            new UpdateScanJobProgressRequest(100, 50, 60, 0, 0, 0),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task UpdateProgress_UnknownId_ReturnsNotFound()
    {
        var stub = new StubScanJobsService { UpdatedProgress = null };
        var controller = new ScanJobsController(stub);

        var result = await controller.UpdateProgress(
            Guid.NewGuid(),
            new UpdateScanJobProgressRequest(100, 50, 60, 0, 0, 0),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── Stub ──────────────────────────────────────────────────────────────────

    private sealed class StubScanJobsService : IScanJobsService
    {
        public IReadOnlyList<ScanJobDto> All { get; init; } = [];
        public IReadOnlyList<ScanJobDto> Recent { get; init; } = [];
        public ScanJobDto? ById { get; init; }
        public ScanJobDto? Created { get; init; }
        public ScanJobDto? UpdatedStatus { get; init; }
        public ScanJobDto? UpdatedProgress { get; init; }

        public Task<IReadOnlyList<ScanJobDto>> GetAllAsync(CancellationToken ct) => Task.FromResult(All);
        public Task<IReadOnlyList<ScanJobDto>> GetRecentAsync(CancellationToken ct) => Task.FromResult(Recent);
        public Task<ScanJobDto?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(ById);
        public Task<ScanJobDto> CreateAsync(CreateScanJobRequest req, CancellationToken ct) =>
            Task.FromResult(Created ?? new ScanJobDto(Guid.NewGuid(), req.StorageLocationId, req.LocationName ?? "Default", ScanJobStatus.Queued.ToString(), DateTimeOffset.UtcNow, null, 0, 0, req.Notes));
        public Task<ScanJobDto?> UpdateStatusAsync(Guid id, UpdateScanJobStatusRequest req, CancellationToken ct) => Task.FromResult(UpdatedStatus);
        public Task<ScanJobDto?> UpdateProgressAsync(Guid id, UpdateScanJobProgressRequest req, CancellationToken ct) => Task.FromResult(UpdatedProgress);
    }
}
