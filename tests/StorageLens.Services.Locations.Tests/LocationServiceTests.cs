using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Locations.Data;
using StorageLens.Services.Locations.Models;
using StorageLens.Services.Locations.Services;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.Locations.Tests;

public class LocationServiceTests
{
    private static LocationsDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<LocationsDbContext>()
            .UseInMemoryDatabase($"locations-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        await using var db = CreateDb();
        var svc = new LocationService(db);

        var result = await svc.GetAllAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByTotalSizeBytesDescending()
    {
        await using var db = CreateDb();
        db.StorageLocations.AddRange(
            new StorageLocation { Id = Guid.NewGuid(), Name = "Small", Path = @"C:\small", TotalSizeBytes = 100, FileCount = 1, CreatedDate = DateTimeOffset.UtcNow, LastScanDate = DateTimeOffset.UtcNow },
            new StorageLocation { Id = Guid.NewGuid(), Name = "Large", Path = @"C:\large", TotalSizeBytes = 9999, FileCount = 10, CreatedDate = DateTimeOffset.UtcNow, LastScanDate = DateTimeOffset.UtcNow }
        );
        await db.SaveChangesAsync();
        var svc = new LocationService(db);

        var result = await svc.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Large", result[0].Name);
        Assert.Equal("Small", result[1].Name);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.StorageLocations.Add(new StorageLocation { Id = id, Name = "My Loc", Path = @"C:\data", TotalSizeBytes = 0, FileCount = 0, CreatedDate = DateTimeOffset.UtcNow, LastScanDate = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var svc = new LocationService(db);

        var result = await svc.GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("My Loc", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using var db = CreateDb();
        var svc = new LocationService(db);

        var result = await svc.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_PersistsLocationAndReturnsDto()
    {
        await using var db = CreateDb();
        var svc = new LocationService(db);
        var request = new CreateStorageLocationRequest("Work NAS", @"\\server\share", StorageLocationType.Nas);

        var result = await svc.CreateAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Work NAS", result.Name);
        Assert.Equal(@"\\server\share", result.Path);
        Assert.Equal(LocationStatus.Online.ToString(), result.Status);
        Assert.Equal(1, await db.StorageLocations.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_SetsZeroSizeAndFileCount()
    {
        await using var db = CreateDb();
        var svc = new LocationService(db);
        var request = new CreateStorageLocationRequest("New Loc", @"C:\new", StorageLocationType.Local);

        var result = await svc.CreateAsync(request, CancellationToken.None);

        Assert.Equal(0, result.TotalSizeBytes);
        Assert.Equal(0, result.FileCount);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesAndReturnsTrue()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.StorageLocations.Add(new StorageLocation { Id = id, Name = "Old", Path = @"C:\old", TotalSizeBytes = 0, FileCount = 0, CreatedDate = DateTimeOffset.UtcNow, LastScanDate = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var svc = new LocationService(db);

        var updated = await svc.UpdateAsync(id, new UpdateStorageLocationRequest("New", @"C:\new", StorageLocationType.Local, LocationStatus.Online), CancellationToken.None);

        Assert.True(updated);
        var entity = await db.StorageLocations.FindAsync(id);
        Assert.Equal("New", entity!.Name);
        Assert.Equal(@"C:\new", entity.Path);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ReturnsFalse()
    {
        await using var db = CreateDb();
        var svc = new LocationService(db);

        var updated = await svc.UpdateAsync(Guid.NewGuid(), new UpdateStorageLocationRequest("X", @"C:\x", StorageLocationType.Local, LocationStatus.Online), CancellationToken.None);

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesAndReturnsTrue()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.StorageLocations.Add(new StorageLocation { Id = id, Name = "Temp", Path = @"C:\temp", TotalSizeBytes = 0, FileCount = 0, CreatedDate = DateTimeOffset.UtcNow, LastScanDate = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var svc = new LocationService(db);

        var deleted = await svc.DeleteAsync(id, CancellationToken.None);

        Assert.True(deleted);
        Assert.Equal(0, await db.StorageLocations.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_ReturnsFalse()
    {
        await using var db = CreateDb();
        var svc = new LocationService(db);

        var deleted = await svc.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(deleted);
    }
}
