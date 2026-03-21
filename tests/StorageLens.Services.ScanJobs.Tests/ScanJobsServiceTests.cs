using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using StorageLens.Services.ScanJobs.Data;
using StorageLens.Services.ScanJobs.Models;
using StorageLens.Services.ScanJobs.Services;
using StorageLens.Shared.Contracts;
using Xunit;

namespace StorageLens.Services.ScanJobs.Tests;

public class ScanJobsServiceTests
{
    private static ScanJobsDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ScanJobsDbContext>()
            .UseInMemoryDatabase($"scanjobs-{Guid.NewGuid()}")
            .Options);

    private static (ScanJobsDbContext Db, SqliteConnection Connection) CreateRelationalDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ScanJobsDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ScanJobsDbContext(options);
        db.Database.EnsureCreated();
        return (db, connection);
    }

    private static ScanJob MakeScanJob(DateTimeOffset? startTime = null) => new()
    {
        Id = Guid.NewGuid(),
        StorageLocationId = Guid.NewGuid(),
        LocationName = "Test Location",
        CorrelationId = Guid.NewGuid().ToString("N"),
        Status = ScanJobStatus.Queued,
        CurrentStage = "Queued",
        StartTime = startTime ?? DateTimeOffset.UtcNow,
        FilesScanned = 0,
        FilesHashed = 0,
        DuplicateGroupsFound = 0,
        WarningCount = 0,
        ErrorCount = 0,
        ProgressPercent = 0,
        Notes = string.Empty
    };

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        await using var db = CreateDb();
        var svc = new ScanJobsService(db);

        var result = await svc.GetAllAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByStartTimeDescending()
    {
        await using var db = CreateDb();
        var baseTime = DateTimeOffset.UtcNow;
        var oldJob = MakeScanJob(baseTime.AddHours(-2));
        oldJob.LocationName = "Old";
        var newJob = MakeScanJob(baseTime);
        newJob.LocationName = "New";

        db.ScanJobs.AddRange(
            oldJob,
            newJob
        );
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("New", result[0].LocationName);
        Assert.Equal("Old", result[1].LocationName);
    }

    // ── GetRecentAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecentAsync_ReturnsMostRecentSix()
    {
        await using var db = CreateDb();
        var baseTime = DateTimeOffset.UtcNow;
        for (var i = 0; i < 8; i++)
        {
            db.ScanJobs.Add(MakeScanJob(baseTime.AddMinutes(-i)));
        }
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.GetRecentAsync(CancellationToken.None);

        Assert.Equal(6, result.Count);
    }

    [Fact]
    public async Task GetRecentAsync_FewerThanSix_ReturnsAll()
    {
        await using var db = CreateDb();
        db.ScanJobs.AddRange(MakeScanJob(), MakeScanJob(), MakeScanJob());
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.GetRecentAsync(CancellationToken.None);

        Assert.Equal(3, result.Count);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        await using var db = CreateDb();
        var job = MakeScanJob();
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.GetByIdAsync(job.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(job.Id, result.Id);
        Assert.Equal(job.LocationName, result.LocationName);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using var db = CreateDb();
        var svc = new ScanJobsService(db);

        var result = await svc.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsJobWithQueuedStatus()
    {
        await using var db = CreateDb();
        var svc = new ScanJobsService(db);
        var locationId = Guid.NewGuid();
        var request = new CreateScanJobRequest(locationId, "Initial scan");

        var result = await svc.CreateAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(ScanJobStatus.Queued.ToString(), result.Status);
        Assert.Equal(1, await db.ScanJobs.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_GeneratesCorrelationIdWhenEmpty()
    {
        await using var db = CreateDb();
        var svc = new ScanJobsService(db);
        var request = new CreateScanJobRequest(Guid.NewGuid(), "test", CorrelationId: null);

        var result = await svc.CreateAsync(request, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.CorrelationId));
    }

    [Fact]
    public async Task CreateAsync_UsesProvidedCorrelationId()
    {
        await using var db = CreateDb();
        var svc = new ScanJobsService(db);
        const string correlationId = "my-custom-correlation-id";
        var request = new CreateScanJobRequest(Guid.NewGuid(), "test", CorrelationId: correlationId);

        var result = await svc.CreateAsync(request, CancellationToken.None);

        Assert.Equal(correlationId, result.CorrelationId);
    }

    [Fact]
    public async Task CreateAsync_UsesDefaultLocationNameWhenNotProvided()
    {
        await using var db = CreateDb();
        var svc = new ScanJobsService(db);
        var request = new CreateScanJobRequest(Guid.NewGuid(), "test", LocationName: null);

        var result = await svc.CreateAsync(request, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.LocationName));
    }

    // ── UpdateStatusAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ExistingId_UpdatesStatusAndReturnsDto()
    {
        await using var db = CreateDb();
        var job = MakeScanJob();
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.UpdateStatusAsync(
            job.Id,
            new UpdateScanJobStatusRequest(ScanJobStatus.Scanning, "Scanning files"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ScanJobStatus.Scanning.ToString(), result.Status);
        Assert.Equal("Scanning files", result.CurrentStage);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownId_ReturnsNull()
    {
        await using var db = CreateDb();
        var svc = new ScanJobsService(db);

        var result = await svc.UpdateStatusAsync(
            Guid.NewGuid(),
            new UpdateScanJobStatusRequest(ScanJobStatus.Scanning, "Scanning"),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateStatusAsync_TerminalStatus_SetsEndTime()
    {
        await using var db = CreateDb();
        var job = MakeScanJob();
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.UpdateStatusAsync(
            job.Id,
            new UpdateScanJobStatusRequest(ScanJobStatus.Failed, "Failed", IsTerminal: true),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.EndTime);
        var entity = await db.ScanJobs.FindAsync(job.Id);
        Assert.NotNull(entity!.EndTime);
    }

    [Fact]
    public async Task UpdateStatusAsync_CompletedWithTerminal_SetsProgressTo100()
    {
        await using var db = CreateDb();
        var job = MakeScanJob();
        job.ProgressPercent = 80;
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.UpdateStatusAsync(
            job.Id,
            new UpdateScanJobStatusRequest(ScanJobStatus.Completed, "Done", IsTerminal: true),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100, result.ProgressPercent);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonTerminalCompleted_DoesNotSetEndTime()
    {
        await using var db = CreateDb();
        var job = MakeScanJob();
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.UpdateStatusAsync(
            job.Id,
            new UpdateScanJobStatusRequest(ScanJobStatus.Scanning, "In progress", IsTerminal: false),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.EndTime);
    }

    // ── UpdateProgressAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProgressAsync_ExistingId_UpdatesProgressAndReturnsDto()
    {
        await using var db = CreateDb();
        var job = MakeScanJob();
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.UpdateProgressAsync(
            job.Id,
            new UpdateScanJobProgressRequest(500, 200, 50, 2, 0, 3),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(500, result.FilesScanned);
        Assert.Equal(50, result.ProgressPercent);
    }

    [Fact]
    public async Task UpdateProgressAsync_UnknownId_ReturnsNull()
    {
        await using var db = CreateDb();
        var svc = new ScanJobsService(db);

        var result = await svc.UpdateProgressAsync(
            Guid.NewGuid(),
            new UpdateScanJobProgressRequest(100, 50, 40, 0, 0, 0),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProgressAsync_ProgressAbove100_ClampsTo100()
    {
        await using var db = CreateDb();
        var job = MakeScanJob();
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.UpdateProgressAsync(
            job.Id,
            new UpdateScanJobProgressRequest(1000, 1000, 150, 0, 0, 0),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100, result.ProgressPercent);
    }

    [Fact]
    public async Task UpdateProgressAsync_ProgressBelow0_ClampsTo0()
    {
        await using var db = CreateDb();
        var job = MakeScanJob();
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        var result = await svc.UpdateProgressAsync(
            job.Id,
            new UpdateScanJobProgressRequest(0, 0, -10, 0, 0, 0),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.ProgressPercent);
    }

    [Fact]
    public async Task UpdateProgressAsync_NegativeWarningCount_ClampsToZero()
    {
        await using var db = CreateDb();
        var job = MakeScanJob();
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();
        var svc = new ScanJobsService(db);

        await svc.UpdateProgressAsync(
            job.Id,
            new UpdateScanJobProgressRequest(100, 50, 50, WarningCount: -5, ErrorCount: -3, DuplicateGroupsFound: -1),
            CancellationToken.None);

        var entity = await db.ScanJobs.FindAsync(job.Id);
        Assert.Equal(0, entity!.WarningCount);
        Assert.Equal(0, entity.ErrorCount);
        Assert.Equal(0, entity.DuplicateGroupsFound);
    }

    [Fact]
    public async Task UpdateStatusAsync_RelationalProvider_AppliesTerminalSemantics()
    {
        var (db, connection) = CreateRelationalDb();
        await using var _ = db;
        await using var __ = connection;

        var job = MakeScanJob();
        job.ProgressPercent = 50;
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();

        var svc = new ScanJobsService(db);
        var result = await svc.UpdateStatusAsync(
            job.Id,
            new UpdateScanJobStatusRequest(ScanJobStatus.Completed, "Done", "Completed", IsTerminal: true),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ScanJobStatus.Completed.ToString(), result!.Status);
        Assert.Equal(100, result.ProgressPercent);
        Assert.NotNull(result.EndTime);
    }

    [Fact]
    public async Task UpdateProgressAsync_RelationalProvider_ClampsValuesAndUpdates()
    {
        var (db, connection) = CreateRelationalDb();
        await using var _ = db;
        await using var __ = connection;

        var job = MakeScanJob();
        db.ScanJobs.Add(job);
        await db.SaveChangesAsync();

        var svc = new ScanJobsService(db);
        var result = await svc.UpdateProgressAsync(
            job.Id,
            new UpdateScanJobProgressRequest(
                FilesScanned: 100,
                FilesHashed: 80,
                ProgressPercent: 150,
                WarningCount: -2,
                ErrorCount: -1,
                DuplicateGroupsFound: -3,
                Notes: "updated"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100, result!.ProgressPercent);
        Assert.Equal(100, result.FilesScanned);
        Assert.Equal(80, result.FilesHashed);
        Assert.Equal(0, result.WarningCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(0, result.DuplicateGroupsFound);
        Assert.Equal("updated", result.Notes);
    }
}
