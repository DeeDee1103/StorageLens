using StorageLens.Services.FileInventory.Data;
using StorageLens.Services.FileInventory.Models;

namespace StorageLens.Services.FileInventory.Extensions;

public static class FileInventorySeeder
{
    public static async Task SeedAsync(FileInventoryDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (dbContext.FileRecords.Any())
        {
            return;
        }

        var random = new Random(42);
        var locations = new[]
        {
            (Guid.Parse("a1000000-0000-0000-0000-000000000001"), "Legal NAS Archive", "\\\\nas01\\legal\\archive"),
            (Guid.Parse("a1000000-0000-0000-0000-000000000002"), "Shared Finance Drive", "\\\\nas02\\finance\\shared"),
            (Guid.Parse("a1000000-0000-0000-0000-000000000003"), "Engineering File Share", "D:\\EngShare"),
            (Guid.Parse("a1000000-0000-0000-0000-000000000004"), "Records Staging", "\\\\nas03\\records\\staging")
        };

        var extensions = new[] { ".pdf", ".docx", ".xlsx", ".pptx", ".zip", ".jpg", ".png", ".msg", ".csv", ".txt" };

        var records = new List<FileRecord>();
        for (var i = 1; i <= 120; i++)
        {
            var location = locations[i % locations.Length];
            var ext = extensions[i % extensions.Length];
            Guid? duplicateGroup = i % 8 == 0
                ? Guid.Parse($"d4000000-0000-0000-0000-{(i % 15 + 1).ToString("D12")}")
                : null;
            var isDuplicate = duplicateGroup.HasValue;

            records.Add(new FileRecord
            {
                Id = Guid.NewGuid(),
                StorageLocationId = location.Item1,
                LocationName = location.Item2,
                FileName = $"file_{i:D4}{ext}",
                FilePath = $"{location.Item3}\\dept{(i % 9) + 1}\\file_{i:D4}{ext}",
                Extension = ext,
                SizeBytes = random.NextInt64(40_000, 2_800_000_000),
                LastModifiedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 1500)),
                HashValue = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant(),
                IsDuplicate = isDuplicate,
                DuplicateGroupId = duplicateGroup
            });
        }

        dbContext.FileRecords.AddRange(records);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
