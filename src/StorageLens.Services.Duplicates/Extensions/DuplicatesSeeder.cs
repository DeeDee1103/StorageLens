using StorageLens.Services.Duplicates.Data;
using StorageLens.Services.Duplicates.Models;

namespace StorageLens.Services.Duplicates.Extensions;

public static class DuplicatesSeeder
{
    public static async Task SeedAsync(DuplicatesDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (dbContext.DuplicateGroups.Any())
        {
            return;
        }

        var random = new Random(100);
        var locations = new[]
        {
            "Legal NAS Archive",
            "Shared Finance Drive",
            "Engineering File Share",
            "Records Staging"
        };

        var groups = new List<DuplicateGroup>();
        for (var i = 1; i <= 15; i++)
        {
            var fileCount = random.Next(3, 18);
            var total = random.NextInt64(600_000_000, 12_000_000_000);
            var wasted = (long)(total * 0.7);
            var selected = locations.OrderBy(_ => random.Next()).Take(2 + i % 2);

            groups.Add(new DuplicateGroup
            {
                Id = Guid.Parse($"d4000000-0000-0000-0000-{i.ToString("D12")}"),
                HashValue = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant() + i,
                FileCount = fileCount,
                TotalSizeBytes = total,
                WastedBytes = wasted,
                ExampleFileName = $"archived_case_bundle_{i:D2}.zip",
                LocationsCsv = string.Join(", ", selected)
            });
        }

        dbContext.DuplicateGroups.AddRange(groups);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
