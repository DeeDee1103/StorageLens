using Microsoft.EntityFrameworkCore;
using StorageLens.Services.Locations.Data;
using StorageLens.Services.Locations.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Locations.Extensions;

public static class LocationsSeeder
{
    public static async Task SeedAsync(LocationsDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.StorageLocations.AnyAsync())
        {
            return;
        }

        dbContext.StorageLocations.AddRange(new List<StorageLocation>
        {
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"),
                Name = "Legal NAS Archive",
                Path = "\\\\nas01\\legal\\archive",
                SiteLocation = "New York, US",
                Latitude = 40.7128,
                Longitude = -74.0060,
                Type = StorageLocationType.Nas,
                Status = LocationStatus.Online,
                LastScanDate = DateTimeOffset.UtcNow.AddHours(-6),
                TotalSizeBytes = 58L * 1024 * 1024 * 1024 * 1024,
                FileCount = 11_500_000,
                CreatedDate = DateTimeOffset.UtcNow.AddMonths(-14)
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"),
                Name = "Shared Finance Drive",
                Path = "\\\\nas02\\finance\\shared",
                SiteLocation = "London, UK",
                Latitude = 51.5074,
                Longitude = -0.1278,
                Type = StorageLocationType.FileShare,
                Status = LocationStatus.Online,
                LastScanDate = DateTimeOffset.UtcNow.AddHours(-8),
                TotalSizeBytes = 31L * 1024 * 1024 * 1024 * 1024,
                FileCount = 8_200_000,
                CreatedDate = DateTimeOffset.UtcNow.AddMonths(-11)
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000003"),
                Name = "Engineering File Share",
                Path = "D:\\EngShare",
                SiteLocation = "San Francisco, US",
                Latitude = 37.7749,
                Longitude = -122.4194,
                Type = StorageLocationType.Local,
                Status = LocationStatus.Warning,
                LastScanDate = DateTimeOffset.UtcNow.AddDays(-1),
                TotalSizeBytes = 73L * 1024 * 1024 * 1024 * 1024,
                FileCount = 17_900_000,
                CreatedDate = DateTimeOffset.UtcNow.AddMonths(-9)
            },
            new()
            {
                Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"),
                Name = "Records Staging",
                Path = "\\\\nas03\\records\\staging",
                SiteLocation = "Singapore",
                Latitude = 1.3521,
                Longitude = 103.8198,
                Type = StorageLocationType.Nas,
                Status = LocationStatus.Online,
                LastScanDate = DateTimeOffset.UtcNow.AddHours(-12),
                TotalSizeBytes = 20L * 1024 * 1024 * 1024 * 1024,
                FileCount = 5_100_000,
                CreatedDate = DateTimeOffset.UtcNow.AddMonths(-5)
            }
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
