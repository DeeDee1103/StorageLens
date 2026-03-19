using Microsoft.EntityFrameworkCore;
using StorageLens.Services.FileInventory.Data;
using StorageLens.Services.FileInventory.Models;
using StorageLens.Services.FileInventory.Services;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.FileInventory.Tests;

public class FileInventoryServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static FileInventoryDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<FileInventoryDbContext>()
            .UseInMemoryDatabase($"files-{Guid.NewGuid()}")
            .Options);

    private static FileRecord MakeRecord(
        string filePath = @"C:\data\file.txt",
        string fileName = "file.txt",
        string extension = ".txt",
        long sizeBytes = 1024,
        Guid? storageLocationId = null,
        Guid? scanJobId = null,
        string correlationId = "corr-1",
        string hashStatus = "Pending",
        string hashValue = "",
        bool isDuplicate = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            StorageLocationId = storageLocationId ?? Guid.NewGuid(),
            ScanJobId = scanJobId ?? Guid.NewGuid(),
            CorrelationId = correlationId,
            LocationName = "TestLocation",
            FileName = fileName,
            FilePath = filePath,
            Extension = extension,
            SizeBytes = sizeBytes,
            CreatedDate = DateTimeOffset.UtcNow,
            LastAccessedDate = DateTimeOffset.UtcNow,
            DiscoveryDate = DateTimeOffset.UtcNow,
            ScanSourcePath = @"C:\data",
            LastModifiedDate = DateTimeOffset.UtcNow,
            HashValue = hashValue,
            HashStatus = hashStatus,
            IsDuplicate = isDuplicate,
            DuplicateGroupId = null
        };

    private static FileMetadataDto MakeMetadata(
        string filePath = @"C:\data\file.txt",
        string fileName = "file.txt",
        string extension = ".txt",
        long sizeBytes = 1024,
        Guid? storageLocationId = null,
        Guid? scanJobId = null,
        string correlationId = "corr-1") =>
        new(
            Guid.NewGuid(),
            storageLocationId ?? Guid.NewGuid(),
            scanJobId ?? Guid.NewGuid(),
            correlationId,
            "TestLocation",
            fileName,
            filePath,
            extension,
            sizeBytes,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            @"C:\data");

    // -----------------------------------------------------------------------
    // GetAllAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_NoFilters_ReturnsAll()
    {
        await using var db = CreateDbContext();
        db.FileRecords.AddRange(
            MakeRecord(@"C:\a\one.txt", "one.txt"),
            MakeRecord(@"C:\a\two.txt", "two.txt"),
            MakeRecord(@"C:\a\three.txt", "three.txt"));
        await db.SaveChangesAsync();

        var sut = new FileInventoryService(db);
        var result = await sut.GetAllAsync(null, null, 1, 25, CancellationToken.None);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_SearchFilter_ReturnsMatchingFiles()
    {
        await using var db = CreateDbContext();
        db.FileRecords.AddRange(
            MakeRecord(@"C:\alpha\report.pdf", "report.pdf"),
            MakeRecord(@"C:\beta\invoice.txt", "invoice.txt"),
            MakeRecord(@"C:\alpha\summary.docx", "summary.docx"));
        await db.SaveChangesAsync();

        var sut = new FileInventoryService(db);
        var result = await sut.GetAllAsync("report", null, 1, 25, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("report.pdf", result[0].FileName);
    }

    [Fact]
    public async Task GetAllAsync_ExtensionFilter_ReturnsMatchingExtension()
    {
        await using var db = CreateDbContext();
        db.FileRecords.AddRange(
            MakeRecord(@"C:\a\file1.pdf", "file1.pdf", ".pdf"),
            MakeRecord(@"C:\a\file2.txt", "file2.txt", ".txt"),
            MakeRecord(@"C:\a\file3.pdf", "file3.pdf", ".pdf"));
        await db.SaveChangesAsync();

        var sut = new FileInventoryService(db);
        var result = await sut.GetAllAsync(null, ".pdf", 1, 25, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(".pdf", r.Extension));
    }

    [Fact]
    public async Task GetAllAsync_Pagination_ReturnsCorrectPage()
    {
        await using var db = CreateDbContext();
        // Insert 5 records with distinct sizes so ordering is deterministic
        for (var i = 1; i <= 5; i++)
        {
            db.FileRecords.Add(MakeRecord($@"C:\a\file{i}.txt", $"file{i}.txt", ".txt", sizeBytes: i * 100));
        }
        await db.SaveChangesAsync();

        var sut = new FileInventoryService(db);

        // page 1, pageSize 2 — largest two (500, 400)
        var page1 = await sut.GetAllAsync(null, null, 1, 2, CancellationToken.None);
        // page 2, pageSize 2 — next two (300, 200)
        var page2 = await sut.GetAllAsync(null, null, 2, 2, CancellationToken.None);

        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.True(page1[0].SizeBytes > page2[0].SizeBytes);
        // Ensure the pages don't overlap
        Assert.Empty(page1.Select(x => x.Id).Intersect(page2.Select(x => x.Id)));
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsDto()
    {
        await using var db = CreateDbContext();
        var record = MakeRecord();
        db.FileRecords.Add(record);
        await db.SaveChangesAsync();

        var sut = new FileInventoryService(db);
        var result = await sut.GetByIdAsync(record.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(record.Id, result.Id);
        Assert.Equal(record.FileName, result.FileName);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var sut = new FileInventoryService(db);

        var result = await sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // GetByLocationAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByLocationAsync_ReturnsFilesForLocation()
    {
        await using var db = CreateDbContext();
        var locationId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        db.FileRecords.AddRange(
            MakeRecord(@"C:\a\file1.txt", storageLocationId: locationId),
            MakeRecord(@"C:\a\file2.txt", storageLocationId: locationId),
            MakeRecord(@"C:\a\file3.txt", storageLocationId: otherId));
        await db.SaveChangesAsync();

        var sut = new FileInventoryService(db);
        var result = await sut.GetByLocationAsync(locationId, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(locationId, r.StorageLocationId));
    }

    // -----------------------------------------------------------------------
    // UpsertBatchAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpsertBatchAsync_EmptyRequest_ReturnsZeroCounts()
    {
        await using var db = CreateDbContext();
        var request = new FileMetadataBatchIngestRequest(
            Guid.NewGuid(), Guid.NewGuid(), "corr-1", "Loc", @"C:\", []);

        var sut = new FileInventoryService(db);
        var result = await sut.UpsertBatchAsync(request, CancellationToken.None);

        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(0, result.InsertedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public async Task UpsertBatchAsync_NewFiles_InsertsAll()
    {
        await using var db = CreateDbContext();
        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();

        var files = new List<FileMetadataDto>
        {
            MakeMetadata(@"C:\data\a.txt", "a.txt", storageLocationId: locationId, scanJobId: scanJobId),
            MakeMetadata(@"C:\data\b.txt", "b.txt", storageLocationId: locationId, scanJobId: scanJobId),
        };

        var request = new FileMetadataBatchIngestRequest(locationId, scanJobId, "corr-1", "Loc", @"C:\data", files);

        var sut = new FileInventoryService(db);
        var result = await sut.UpsertBatchAsync(request, CancellationToken.None);

        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(2, result.InsertedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(2, await db.FileRecords.CountAsync());
    }

    [Fact]
    public async Task UpsertBatchAsync_ExistingFiles_UpdatesAndDoesNotInsert()
    {
        await using var db = CreateDbContext();
        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();

        var existing = MakeRecord(@"C:\data\existing.txt", "existing.txt", storageLocationId: locationId);
        db.FileRecords.Add(existing);
        await db.SaveChangesAsync();

        var files = new List<FileMetadataDto>
        {
            MakeMetadata(@"C:\data\existing.txt", "existing.txt", storageLocationId: locationId, scanJobId: scanJobId),
        };

        var request = new FileMetadataBatchIngestRequest(locationId, scanJobId, "corr-1", "Loc", @"C:\data", files);

        var sut = new FileInventoryService(db);
        var result = await sut.UpsertBatchAsync(request, CancellationToken.None);

        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.InsertedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(1, await db.FileRecords.CountAsync());
    }

    [Fact]
    public async Task UpsertBatchAsync_FailedHashStatus_ResetsToP ending()
    {
        await using var db = CreateDbContext();
        var locationId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();

        var existing = MakeRecord(
            @"C:\data\failed.txt", "failed.txt",
            storageLocationId: locationId,
            hashStatus: HashStatus.Failed.ToString(),
            hashValue: "abc123");
        db.FileRecords.Add(existing);
        await db.SaveChangesAsync();

        var files = new List<FileMetadataDto>
        {
            MakeMetadata(@"C:\data\failed.txt", "failed.txt", storageLocationId: locationId, scanJobId: scanJobId),
        };

        var request = new FileMetadataBatchIngestRequest(locationId, scanJobId, "corr-1", "Loc", @"C:\data", files);

        var sut = new FileInventoryService(db);
        await sut.UpsertBatchAsync(request, CancellationToken.None);

        var updated = await db.FileRecords.FirstAsync(x => x.FilePath == @"C:\data\failed.txt");
        Assert.Equal(HashStatus.Pending.ToString(), updated.HashStatus);
        Assert.Null(updated.HashError);
    }

    // -----------------------------------------------------------------------
    // GetPendingHashFilesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPendingHashFiles_ReturnsOnlyPendingAndFailed()
    {
        await using var db = CreateDbContext();
        db.FileRecords.AddRange(
            MakeRecord(@"C:\a\pending.txt", hashStatus: "Pending"),
            MakeRecord(@"C:\a\failed.txt", hashStatus: "Failed"),
            MakeRecord(@"C:\a\completed.txt", hashStatus: "Completed"),
            MakeRecord(@"C:\a\inprogress.txt", hashStatus: "InProgress"));
        await db.SaveChangesAsync();

        var sut = new FileInventoryService(db);
        var result = await sut.GetPendingHashFilesAsync(null, 100, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Contains(r.FilePath, new[] { @"C:\a\pending.txt", @"C:\a\failed.txt" }));
    }

    [Fact]
    public async Task GetPendingHashFiles_FiltersByScanJobId()
    {
        await using var db = CreateDbContext();
        var targetScanJobId = Guid.NewGuid();
        var otherScanJobId = Guid.NewGuid();

        db.FileRecords.AddRange(
            MakeRecord(@"C:\a\file1.txt", hashStatus: "Pending", scanJobId: targetScanJobId),
            MakeRecord(@"C:\a\file2.txt", hashStatus: "Pending", scanJobId: otherScanJobId),
            MakeRecord(@"C:\a\file3.txt", hashStatus: "Failed", scanJobId: targetScanJobId));
        await db.SaveChangesAsync();

        var sut = new FileInventoryService(db);
        var result = await sut.GetPendingHashFilesAsync(targetScanJobId, 100, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(targetScanJobId, r.ScanJobId));
    }

    // -----------------------------------------------------------------------
    // UpdateHashResultAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateHashResultAsync_Found_UpdatesAndReturnsTrue()
    {
        await using var db = CreateDbContext();
        var record = MakeRecord(@"C:\a\file.txt", hashStatus: "Pending");
        db.FileRecords.Add(record);
        await db.SaveChangesAsync();

        var request = new FileHashResultRequest(
            "corr-updated",
            HashStatus.Completed.ToString(),
            "abc123def456",
            null,
            DateTimeOffset.UtcNow);

        var sut = new FileInventoryService(db);
        var success = await sut.UpdateHashResultAsync(record.Id, request, CancellationToken.None);

        Assert.True(success);

        var updated = await db.FileRecords.FirstAsync(x => x.Id == record.Id);
        Assert.Equal(HashStatus.Completed.ToString(), updated.HashStatus);
        Assert.Equal("abc123def456", updated.HashValue);
        Assert.Equal("corr-updated", updated.CorrelationId);
    }

    [Fact]
    public async Task UpdateHashResultAsync_NotFound_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        var request = new FileHashResultRequest(
            "corr-1",
            HashStatus.Completed.ToString(),
            "hash",
            null,
            DateTimeOffset.UtcNow);

        var sut = new FileInventoryService(db);
        var success = await sut.UpdateHashResultAsync(Guid.NewGuid(), request, CancellationToken.None);

        Assert.False(success);
    }

    // -----------------------------------------------------------------------
    // MarkDuplicateFlagsByHashAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkDuplicateFlagsByHashAsync_EmptySet_ReturnsZero()
    {
        await using var db = CreateDbContext();
        var scanJobId = Guid.NewGuid();
        db.FileRecords.Add(MakeRecord(@"C:\a\file.txt", scanJobId: scanJobId, hashValue: "aaa"));
        await db.SaveChangesAsync();

        var sut = new FileInventoryService(db);
        var count = await sut.MarkDuplicateFlagsByHashAsync(
            scanJobId, "corr-1", new HashSet<string>(), CancellationToken.None);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task MarkDuplicateFlagsByHashAsync_SetsIsDuplicateTrue_ForMatchingHashes()
    {
        await using var db = CreateDbContext();
        var scanJobId = Guid.NewGuid();
        const string correlationId = "corr-dup";
        const string dupHash = "deadbeef";

        db.FileRecords.AddRange(
            MakeRecord(@"C:\a\dup1.txt", scanJobId: scanJobId, correlationId: correlationId, hashValue: dupHash),
            MakeRecord(@"C:\a\dup2.txt", scanJobId: scanJobId, correlationId: correlationId, hashValue: dupHash),
            MakeRecord(@"C:\a\unique.txt", scanJobId: scanJobId, correlationId: correlationId, hashValue: "unique123"));
        await db.SaveChangesAsync();

        var sut = new FileInventoryService(db);
        var count = await sut.MarkDuplicateFlagsByHashAsync(
            scanJobId, correlationId, new HashSet<string> { dupHash }, CancellationToken.None);

        Assert.Equal(2, count);

        var dup1 = await db.FileRecords.FirstAsync(x => x.FilePath == @"C:\a\dup1.txt");
        var dup2 = await db.FileRecords.FirstAsync(x => x.FilePath == @"C:\a\dup2.txt");
        var unique = await db.FileRecords.FirstAsync(x => x.FilePath == @"C:\a\unique.txt");

        Assert.True(dup1.IsDuplicate);
        Assert.True(dup2.IsDuplicate);
        Assert.False(unique.IsDuplicate);
    }
}
