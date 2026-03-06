using StorageLens.Services.ScanJobs.Data;
using StorageLens.Services.ScanJobs.Models;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.ScanJobs.Extensions;

public static class ScanJobsSeeder
{
    public static async Task SeedAsync(ScanJobsDbContext dbContext)
    {
        if (dbContext.ScanJobs.Any())
        {
            return;
        }

        var baseTime = DateTimeOffset.UtcNow.AddDays(-2);

        var jobs = new List<ScanJob>
        {
            new()
            {
                Id = Guid.Parse("b2000000-0000-0000-0000-000000000001"),
                StorageLocationId = Guid.Parse("a1000000-0000-0000-0000-000000000001"),
                LocationName = "Legal NAS Archive",
                Status = ScanJobStatus.Completed,
                StartTime = baseTime.AddHours(1),
                EndTime = baseTime.AddHours(2),
                FilesScanned = 1_650_000,
                ProgressPercent = 100,
                Notes = "Scheduled nightly scan"
            },
            new()
            {
                Id = Guid.Parse("b2000000-0000-0000-0000-000000000002"),
                StorageLocationId = Guid.Parse("a1000000-0000-0000-0000-000000000002"),
                LocationName = "Shared Finance Drive",
                Status = ScanJobStatus.Completed,
                StartTime = baseTime.AddHours(5),
                EndTime = baseTime.AddHours(6),
                FilesScanned = 820_000,
                ProgressPercent = 100,
                Notes = "Monthly reconciliation scan"
            },
            new()
            {
                Id = Guid.Parse("b2000000-0000-0000-0000-000000000003"),
                StorageLocationId = Guid.Parse("a1000000-0000-0000-0000-000000000003"),
                LocationName = "Engineering File Share",
                Status = ScanJobStatus.Running,
                StartTime = DateTimeOffset.UtcNow.AddMinutes(-40),
                EndTime = null,
                FilesScanned = 3_200_000,
                ProgressPercent = 74,
                Notes = "Deep metadata refresh"
            },
            new()
            {
                Id = Guid.Parse("b2000000-0000-0000-0000-000000000004"),
                StorageLocationId = Guid.Parse("a1000000-0000-0000-0000-000000000004"),
                LocationName = "Records Staging",
                Status = ScanJobStatus.Pending,
                StartTime = DateTimeOffset.UtcNow.AddMinutes(20),
                EndTime = null,
                FilesScanned = 0,
                ProgressPercent = 0,
                Notes = "Queued after maintenance"
            },
            new()
            {
                Id = Guid.Parse("b2000000-0000-0000-0000-000000000005"),
                StorageLocationId = Guid.Parse("a1000000-0000-0000-0000-000000000003"),
                LocationName = "Engineering File Share",
                Status = ScanJobStatus.Failed,
                StartTime = baseTime.AddHours(16),
                EndTime = baseTime.AddHours(16).AddMinutes(18),
                FilesScanned = 170_000,
                ProgressPercent = 12,
                Notes = "Permission denied on project archive"
            }
        };

        dbContext.ScanJobs.AddRange(jobs);
        await dbContext.SaveChangesAsync();
    }
}
