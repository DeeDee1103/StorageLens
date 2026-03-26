using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Locations.Controllers;
using StorageLens.Services.Locations.Interfaces;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Locations.Tests;

public class LocationsControllerTests
{
    private static StorageLocationDto MakeDto(string name = "Test") =>
        new(Guid.NewGuid(), name, @"C:\test", "Local", "Online", null, 0, 0, DateTimeOffset.UtcNow);

    [Fact]
    public async Task GetAll_ReturnsOkWithList()
    {
        var stub = new StubLocationService { All = [MakeDto("A"), MakeDto("B")] };
        var controller = new LocationsController(stub);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<StorageLocationDto>>(ok.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var dto = MakeDto();
        var stub = new StubLocationService { ById = dto };
        var controller = new LocationsController(stub);

        var result = await controller.GetById(dto.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var stub = new StubLocationService { ById = null };
        var controller = new LocationsController(stub);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAtAction()
    {
        var dto = MakeDto("New");
        var stub = new StubLocationService { Created = dto };
        var controller = new LocationsController(stub);

        var result = await controller.Create(new CreateStorageLocationRequest("New", @"C:\new", StorageLocationType.Local), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
    }

    [Fact]
    public async Task Create_MissingName_ReturnsBadRequest()
    {
        var stub = new StubLocationService();
        var controller = new LocationsController(stub);

        var result = await controller.Create(new CreateStorageLocationRequest("", @"C:\path", StorageLocationType.Local), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_MissingPath_ReturnsBadRequest()
    {
        var stub = new StubLocationService();
        var controller = new LocationsController(stub);

        var result = await controller.Create(new CreateStorageLocationRequest("ValidName", "", StorageLocationType.Local), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ExistingId_ReturnsNoContent()
    {
        var stub = new StubLocationService { UpdateResult = true };
        var controller = new LocationsController(stub);

        var result = await controller.Update(Guid.NewGuid(), new UpdateStorageLocationRequest("N", @"C:\n", StorageLocationType.Local, LocationStatus.Online), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var stub = new StubLocationService { UpdateResult = false };
        var controller = new LocationsController(stub);

        var result = await controller.Update(Guid.NewGuid(), new UpdateStorageLocationRequest("N", @"C:\n", StorageLocationType.Local, LocationStatus.Online), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var stub = new StubLocationService { DeleteResult = true };
        var controller = new LocationsController(stub);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var stub = new StubLocationService { DeleteResult = false };
        var controller = new LocationsController(stub);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private sealed class StubLocationService : ILocationService
    {
        public IReadOnlyList<StorageLocationDto> All { get; init; } = [];
        public StorageLocationDto? ById { get; init; }
        public StorageLocationDto? Created { get; init; }
        public bool UpdateResult { get; init; }
        public bool DeleteResult { get; init; }

        public Task<IReadOnlyList<StorageLocationDto>> GetAllAsync(CancellationToken ct) => Task.FromResult(All);
        public Task<StorageLocationDto?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(ById);
        public Task<StorageLocationDto> CreateAsync(CreateStorageLocationRequest req, CancellationToken ct) =>
            Task.FromResult(Created ?? new StorageLocationDto(Guid.NewGuid(), req.Name, req.Path, req.Type.ToString(), "Online", null, 0, 0, DateTimeOffset.UtcNow));
        public Task<bool> UpdateAsync(Guid id, UpdateStorageLocationRequest req, CancellationToken ct) => Task.FromResult(UpdateResult);
        public Task<bool> UpdateStatsAsync(Guid id, UpdateStorageLocationStatsRequest req, CancellationToken ct) => Task.FromResult(UpdateResult);
        public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => Task.FromResult(DeleteResult);
    }
}
