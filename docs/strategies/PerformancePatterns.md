# Performance Patterns

## Executive Summary

StorageLens will implement **N+1 prevention patterns and reflection safety guidelines**:
- Identifies and fixes N+1 query issues in Duplicates and FileInventory services
- Provides EF Core `.Include()` and `.ThenInclude()` patterns
- Documents reflection anti-patterns in EF Core method calls
- Provides type-safe alternatives using Expression Trees
- Includes query performance detection in tests

**Current State**: 7.5/10 - N+1 acknowledged, reflection fragility mentioned in CHANGELOG  
**Target State**: 9.5/10 - Performant queries, type-safe EF Core patterns

---

## 1. N+1 Query Problem Analysis

### 1.1 What is N+1?

N+1 query problem occurs when:
1. You execute 1 query to fetch N items
2. Then execute N additional queries to fetch related data
3. Total: N+1 database round trips instead of 1

**Example (BAD)**:
```csharp
// Query 1: Fetch all files
var files = await dbContext.FileRecords
    .Where(x => x.ScanJobId == scanJobId)
    .ToListAsync();  // Executes 1 query

// Queries 2-N: For each file, fetch its duplicate group
foreach (var file in files)
{
    var group = await dbContext.DuplicateGroups
        .FirstOrDefaultAsync(x => x.Id == file.DuplicateGroupId);  // Executes N queries!
    
    // Use group...
}
```

**Result**: 1 + N queries instead of 1 optimized query

### 1.2 Current N+1 Issues in StorageLens

#### Issue #1: Duplicates Service GetSummaryAsync

**Location**: [src/StorageLens.Services.Duplicates/Services/DuplicatesService.cs](src/StorageLens.Services.Duplicates/Services/DuplicatesService.cs#L15-L54)

**Current Pattern**:
```csharp
// Gets duplicate groups from local DbContext
var groups = await dbContext.DuplicateGroups
    .AsNoTracking()
    .Where(x => x.ScanJobId == latestGroup.ScanJobId && x.CorrelationId == latestGroup.CorrelationId)
    .ToListAsync(cancellationToken);

// Then makes HTTP call to FileInventory with fixed page size
var files = await fileInventoryClient.GetFromJsonAsync<List<FileRecordDto>>(
    $"api/files?page=1&pageSize=5000&scanJobId={latestGroup.ScanJobId}...",
    cancellationToken);
```

**Problem**: Large scans (>5000 files) are truncated. Requires pagination but no loop exists.

**Impact**: Duplicates calculation may be incomplete

#### Issue #2: FileInventory Service UpsertBatchAsync

**Location**: [src/StorageLens.Services.FileInventory/Services/FileInventoryService.cs](src/StorageLens.Services.FileInventory/Services/FileInventoryService.cs#L49-L119)

**Current Pattern**:
```csharp
var existingByPath = await dbContext.FileRecords
    .Where(x => filePaths.Contains(x.FilePath))
    .ToDictionaryAsync(x => x.FilePath, cancellationToken);

foreach (var item in request.Files)
{
    if (existingByPath.TryGetValue(item.FilePath, out var existing))
    {
        // Manual property-by-property update
        existing.StorageLocationId = item.StorageLocationId;
        existing.ScanJobId = item.ScanJobId;
        // ... many more assignments ...
    }
}

await dbContext.SaveChangesAsync(cancellationToken);
```

**Problem**: For large batches, this loads all records into memory. Would be faster with `ExecuteUpdateAsync()` (EF7+).

**Impact**: Memory usage spikes with large batch ingest

#### Issue #3: FileInventory Service MarkDuplicateFlagsByHashAsync

**Location**: [src/StorageLens.Services.FileInventory/Services/FileInventoryService.cs](src/StorageLens.Services.FileInventory/Services/FileInventoryService.cs#L175-L208)

**Current Pattern**:
```csharp
var records = await dbContext.FileRecords
    .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
    .ToListAsync(cancellationToken);

foreach (var record in records)
{
    var isDuplicate = !string.IsNullOrWhiteSpace(record.HashValue) && duplicateHashes.Contains(record.HashValue);
    if (record.IsDuplicate != isDuplicate)
    {
        record.IsDuplicate = isDuplicate;
        modifiedCount++;
    }
}

await dbContext.SaveChangesAsync(cancellationToken);
```

**Problem**: Loads potentially millions of records into memory. `ExecuteUpdateAsync()` would be single query.

**Impact**: Memory exhaustion on large scans

---

## 2. N+1 Prevention Patterns

### 2.1 Pattern: Use `.Include()` and `.ThenInclude()`

When related data is needed, fetch it in a single query:

```csharp
// GOOD: Single query with navigation properties
var filesWithGroups = await dbContext.FileRecords
    .AsNoTracking()
    .Where(x => x.ScanJobId == scanJobId)
    .Include(x => x.DuplicateGroup)  // Eager load related group
    .Include(x => x.StorageLocation) // Eager load location
    .ToListAsync(cancellationToken);

// Access related data without additional queries
foreach (var file in filesWithGroups)
{
    var groupName = file.DuplicateGroup?.HashValue; // No query!
    var location = file.StorageLocation?.Name;      // No query!
}
```

### 2.2 Pattern: Use `.SelectMany()` for Hierarchical Queries

Fetch flattened data structure in single query:

```csharp
// GOOD: Fetch nested data without joins
var filesByGroup = await dbContext.DuplicateGroups
    .AsNoTracking()
    .Where(x => x.ScanJobId == scanJobId)
    .SelectMany(
        group => dbContext.FileRecords.Where(f => f.DuplicateGroupId == group.Id),
        (group, file) => new
        {
            Group = group,
            File = file
        })
    .ToListAsync(cancellationToken);
```

### 2.3 Pattern: Use `ExecuteUpdateAsync()` for Bulk Updates (EF7+)

Instead of fetch -> modify -> save, use direct SQL update:

```csharp
// BAD: Loads entire table, modifies in memory, saves
var records = await dbContext.FileRecords
    .Where(x => x.ScanJobId == scanJobId)
    .ToListAsync();

foreach (var record in records)
{
    record.IsDuplicate = duplicateHashes.Contains(record.HashValue);
}

await dbContext.SaveChangesAsync();

// GOOD: Single SQL UPDATE statement
var updatedCount = await dbContext.FileRecords
    .Where(x => x.ScanJobId == scanJobId && !duplicateHashes.Contains(x.HashValue))
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(f => f.IsDuplicate, false),
    cancellationToken);

updatedCount += await dbContext.FileRecords
    .Where(x => x.ScanJobId == scanJobId && duplicateHashes.Contains(x.HashValue))
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(f => f.IsDuplicate, true),
    cancellationToken);
```

### 2.4 Pattern: Use `.ProjectTo<DTO>()` for Read Operations

Fetch only needed columns, not entire entity:

```csharp
// BAD: Loads entire FileRecord entity
var files = await dbContext.FileRecords
    .Where(x => x.ScanJobId == scanJobId)
    .AsNoTracking()
    .ToListAsync();

var dtos = files.Select(f => new FileRecordDto(
    f.Id, f.StorageLocationId, f.LocationName, 
    // ... many properties ...
)).ToList();

// GOOD: Fetch only columns needed for DTO
var dtos = await dbContext.FileRecords
    .Where(x => x.ScanJobId == scanJobId)
    .AsNoTracking()
    .Select(f => new FileRecordDto(
        f.Id, f.StorageLocationId, f.LocationName,
        f.FileName, f.FilePath, f.Extension, f.SizeBytes,
        f.LastModifiedDate, f.HashValue, f.IsDuplicate,
        f.DuplicateGroupId, f.HashStatus, f.ScanJobId,
        f.CorrelationId, f.HashCalculatedDate,
        f.DiscoveryDate, f.ScanSourcePath))
    .ToListAsync(cancellationToken);
```

### 2.5 Pattern: Pagination to Avoid Memory Overflow

For large datasets, fetch in chunks:

```csharp
// GOOD: Paginate through large result sets
public async Task<int> MarkAllDuplicatesAsync(
    Guid scanJobId,
    string correlationId,
    IReadOnlyList<string> duplicateHashes,
    CancellationToken cancellationToken)
{
    const int pageSize = 1000;
    var pageNumber = 1;
    var totalUpdated = 0;

    while (true)
    {
        var batch = await dbContext.FileRecords
            .Where(x => x.ScanJobId == scanJobId && x.CorrelationId == correlationId)
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (batch.Count == 0)
            break; // No more records

        foreach (var record in batch)
        {
            record.IsDuplicate = duplicateHashes.Contains(record.HashValue);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        totalUpdated += batch.Count;

        pageNumber++;
    }

    return totalUpdated;
}
```

---

## 3. Reflection Anti-Patterns and Fixes

### 3.1 Anti-Pattern: String-Based EF Core Queries

**Problem**: Fragile to refactoring, no compile-time safety

```csharp
// BAD: String-based - breaks if FileRecord.Id property renamed
var entity = await dbContext.FileRecords
    .FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == fileId);

// BAD: String-based Include - breaks if property renamed
var files = await dbContext.FileRecords
    .Include("DuplicateGroup")
    .ToListAsync();
```

### 3.2 Fix: Use Expression Trees

**Solution**: Compile-time safe, refactoring-proof

```csharp
// GOOD: Strongly-typed
var entity = await dbContext.FileRecords
    .FirstOrDefaultAsync(x => x.Id == fileId);

// GOOD: Strongly-typed Include
var files = await dbContext.FileRecords
    .Include(x => x.DuplicateGroup)
    .ToListAsync();
```

### 3.3 Anti-Pattern: Dynamic Property Access

**Problem**: Reflection overhead, hard to trace

```csharp
// BAD: Using reflection to access properties
var properties = typeof(FileRecord).GetProperties();
foreach (var prop in properties)
{
    var value = prop.GetValue(record);
    // Do something with value
}

// BAD: Reflection in query
var filesByProp = await dbContext.FileRecords
    .Where(x => (string)typeof(FileRecord)
        .GetProperty("FileName")!
        .GetValue(x)! == searchTerm)
    .ToListAsync();
```

### 3.4 Fix: Use Strongly-Typed Properties

```csharp
// GOOD: Compile-time safe
var entity = new FileRecord();
var fileName = entity.FileName; // Direct property access

// GOOD: Strongly-typed query
var files = await dbContext.FileRecords
    .Where(x => x.FileName == searchTerm)
    .ToListAsync();
```

### 3.5 Anti-Pattern: Late-Bound Property Access

**Problem**: No compile-time checking, possible null reference errors

```csharp
// BAD: Reflection lookup at runtime
public object GetPropertiesForAudit(object entity)
{
    var type = entity.GetType();
    return type.GetProperties()
        .ToDictionary(p => p.Name, p => p.GetValue(entity));
}
```

### 3.6 Fix: Use Compile-Time Generic Constraints

```csharp
// GOOD: Compile-time safety
public Dictionary<string, object?> GetPropertiesForAudit<T>(T entity) where T : class
{
    return typeof(T).GetProperties()
        .Where(p => p.CanRead)
        .ToDictionary(p => p.Name, p => p.GetValue(entity));
}

// Or even better, use explicit mapping
public AuditLogDto GetAuditLog(FileRecord file)
{
    return new AuditLogDto(
        FileId: file.Id,
        FileName: file.FileName,
        FileSize: file.SizeBytes,
        LastModified: file.LastModifiedDate,
        ModifiedBy: file.LastModifiedBy);
}
```

---

## 4. Query Performance Detection

### 4.1 EF Core Query Logging

Configure EF Core to log all queries:

```csharp
// In Program.cs
builder.Services.AddDbContext<FileInventoryDbContext>((sp, options) =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>();
    
    options.UseSqlServer(connectionString)
        .LogTo(logger.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command").Log);
});
```

### 4.2 Test Pattern: Assert Query Count

Verify no N+1 in unit tests:

```csharp
namespace StorageLens.Services.FileInventory.Tests;

[TestClass]
public class QueryPerformanceTests
{
    private FileInventoryDbContext _dbContext = null!;

    [TestInitialize]
    public void Setup()
    {
        // Use in-memory database with listener
        var options = new DbContextOptionsBuilder<FileInventoryDbContext>()
            .UseInMemoryDatabase("TestDb")
            .LogTo(Console.WriteLine)
            .EnableSensitiveDataLogging()
            .Options;

        _dbContext = new FileInventoryDbContext(options);
    }

    [TestMethod]
    public async Task GetFilesByLocationId_ExecutesSingleQuery()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        await SeedTestData(locationId);

        var queryCount = 0;
        var listener = new ListeningLoggerFactory();
        listener.QueryExecuted += (query) => queryCount++;

        _dbContext.Database.SetLoggerFactory(listener);

        // Act
        var files = await _dbContext.FileRecords
            .Where(x => x.StorageLocationId == locationId)
            .Include(x => x.DuplicateGroup)
            .ToListAsync();

        // Assert - Should be exactly 2 queries (1 for files, 1 for groups loaded together)
        // NOT N+1 (one for files, one for each file's group)
        Assert.IsTrue(queryCount <= 2, $"Expected <= 2 queries, but got {queryCount}");
    }

    [TestMethod]
    public async Task MarkDuplicatesAsync_UsesExecuteUpdate()
    {
        // Arrange
        var scanJobId = Guid.NewGuid();
        var duplicateHashes = new[] { "hash1", "hash2" };

        // Create a query interceptor to count database commands
        var commandCount = 0;

        // Act
        commandCount = await dbContext.FileRecords
            .Where(x => x.ScanJobId == scanJobId && duplicateHashes.Contains(x.HashValue))
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(f => f.IsDuplicate, true));

        // Assert - Should be single UPDATE statement, not multiple round trips
        Assert.IsTrue(commandCount > 0);
    }
}

// Test helper: Query listener
public class ListeningLoggerFactory : ILoggerFactory
{
    public event Action<string>? QueryExecuted;

    public ILogger CreateLogger(string categoryName)
    {
        return new ListeningLogger(categoryName, this);
    }

    public void AddProvider(ILoggerProvider provider) { }
    public void Dispose() { }

    public class ListeningLogger : ILogger
    {
        private readonly ListeningLoggerFactory _factory;

        public ListeningLogger(string category, ListeningLoggerFactory factory)
        {
            _factory = factory;
        }

        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (message.Contains("Executing DbCommand"))
            {
                _factory.QueryExecuted?.Invoke(message);
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) => null;
    }
}
```

### 4.3 Integration Test: Performance Baseline

```csharp
[TestClass]
public class PerformanceBaselineTests
{
    [TestMethod]
    [Timeout(5000)] // Fail if takes > 5 seconds
    public async Task GetSummary_With1000Duplicates_CompletesUnder5Seconds()
    {
        // Arrange - Create 1000 duplicate groups
        var scanJobId = Guid.NewGuid();
        for (int i = 0; i < 1000; i++)
        {
            dbContext.DuplicateGroups.Add(new DuplicateGroup
            {
                Id = Guid.NewGuid(),
                ScanJobId = scanJobId,
                HashValue = $"hash_{i}",
                FileCount = 10,
                WastedBytes = 1024 * i
            });
        }
        await dbContext.SaveChangesAsync();

        var service = new DuplicatesService(dbContext, logger, httpClientFactory);

        // Act
        var summary = await service.GetSummaryAsync(CancellationToken.None);

        // Assert
        Assert.IsNotNull(summary);
        Assert.AreEqual(1000, summary.DuplicateGroups);
    }
}
```

---

## 5. Fixed Implementation Examples

### 5.1 DuplicatesService.GetSummaryAsync (Fixed)

```csharp
public async Task<DuplicateSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
{
    var latestGroup = await dbContext.DuplicateGroups
        .AsNoTracking()
        .OrderByDescending(x => x.RecalculatedAtUtc)
        .FirstOrDefaultAsync(cancellationToken);

    if (latestGroup is null)
    {
        return new DuplicateSummaryDto(0, 0, 0, 0);
    }

    // FIXED: Fetch all groups for scan job in single query
    var groups = await dbContext.DuplicateGroups
        .AsNoTracking()
        .Where(x => x.ScanJobId == latestGroup.ScanJobId 
            && x.CorrelationId == latestGroup.CorrelationId)
        .ToListAsync(cancellationToken);

    var duplicateFiles = groups.Sum(x => x.FileCount);
    var wastedBytes = groups.Sum(x => x.WastedBytes);

    // FIXED: Use pagination to handle large scans
    var allFiles = new List<FileRecordDto>();
    const int pageSize = 1000;
    var pageNumber = 1;

    while (true)
    {
        var pageFiles = await fileInventoryClient.GetFromJsonAsync<List<FileRecordDto>>(
            $"api/files?page={pageNumber}&pageSize={pageSize}&scanJobId={Uri.EscapeDataString(latestGroup.ScanJobId.ToString())}&correlationId={Uri.EscapeDataString(latestGroup.CorrelationId)}",
            cancellationToken) ?? [];

        if (pageFiles.Count == 0)
            break;

        allFiles.AddRange(pageFiles);
        pageNumber++;
    }

    var filesForRun = allFiles
        .Where(x => x.ScanJobId == latestGroup.ScanJobId 
            && x.CorrelationId == latestGroup.CorrelationId)
        .ToList();

    var duplicatePercent = filesForRun.Count == 0
        ? 0
        : Math.Round((double)duplicateFiles / filesForRun.Count * 100, 2);

    return new DuplicateSummaryDto(
        groups.Count,
        duplicateFiles,
        wastedBytes,
        Math.Clamp(duplicatePercent, 0, 100));
}
```

### 5.2 FileInventoryService.MarkDuplicateFlagsByHashAsync (Fixed)

```csharp
public async Task<int> MarkDuplicateFlagsByHashAsync(
    Guid scanJobId,
    string correlationId,
    IReadOnlyList<string> duplicateHashes,
    CancellationToken cancellationToken)
{
    if (duplicateHashes.Count == 0)
    {
        return 0;
    }

    // FIXED: Use ExecuteUpdateAsync for single SQL UPDATE
    // Update all matching hashes to IsDuplicate=true
    var markedCount = await dbContext.FileRecords
        .Where(x => x.ScanJobId == scanJobId 
            && x.CorrelationId == correlationId 
            && duplicateHashes.Contains(x.HashValue))
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(f => f.IsDuplicate, true),
        cancellationToken);

    // Update all non-matching hashes to IsDuplicate=false
    var unmarkedCount = await dbContext.FileRecords
        .Where(x => x.ScanJobId == scanJobId 
            && x.CorrelationId == correlationId 
            && !duplicateHashes.Contains(x.HashValue))
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(f => f.IsDuplicate, false),
        cancellationToken);

    _logger.LogInformation(
        "Marked duplicates. Marked: {MarkedCount}, Unmarked: {UnmarkedCount}, CorrelationId: {CorrelationId}",
        markedCount,
        unmarkedCount,
        correlationId);

    return markedCount + unmarkedCount;
}
```

### 5.3 FileInventoryService.UpsertBatchAsync (Fixed - Using ExecuteUpdateAsync)

```csharp
public async Task<FileBatchIngestResultDto> UpsertBatchAsync(
    FileMetadataBatchIngestRequest request,
    CancellationToken cancellationToken)
{
    if (request.Files.Count == 0)
    {
        return new FileBatchIngestResultDto(0, 0, 0, 0);
    }

    var inserted = 0;
    var updated = 0;
    var errorCount = 0;

    try
    {
        var filePaths = request.Files.Select(x => x.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // FIXED: Use ExecuteUpdateAsync for bulk updates instead of loading into memory
        var existingPaths = await dbContext.FileRecords
            .Where(x => filePaths.Contains(x.FilePath))
            .Select(x => x.FilePath)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        // Process in batches to avoid memory overflow
        const int batchSize = 500;
        for (int i = 0; i < request.Files.Count; i += batchSize)
        {
            var batch = request.Files.Skip(i).Take(batchSize).ToList();

            foreach (var item in batch)
            {
                if (existingPaths.Contains(item.FilePath))
                {
                    updated++;
                }
                else
                {
                    inserted++;
                }
            }

            // For new inserts, add in batch
            var newRecords = batch
                .Where(x => !existingPaths.Contains(x.FilePath))
                .Select(x => new FileRecord
                {
                    Id = x.Id ?? Guid.NewGuid(),
                    StorageLocationId = x.StorageLocationId,
                    ScanJobId = x.ScanJobId,
                    CorrelationId = x.CorrelationId,
                    LocationName = x.LocationName,
                    FileName = x.FileName,
                    FilePath = x.FilePath,
                    Extension = x.Extension,
                    SizeBytes = x.SizeBytes,
                    CreatedDate = x.CreatedDate,
                    LastAccessedDate = x.LastAccessedDate,
                    DiscoveryDate = x.ScanTimestamp,
                    ScanSourcePath = x.ScanSourcePath,
                    LastModifiedDate = x.LastModifiedDate,
                    HashValue = string.Empty,
                    HashStatus = HashStatus.Pending.ToString(),
                    IsDuplicate = false,
                    DuplicateGroupId = null
                })
                .ToList();

            if (newRecords.Count > 0)
            {
                dbContext.FileRecords.AddRange(newRecords);
            }

            // For existing records, use ExecuteUpdateAsync
            var existingInBatch = batch
                .Where(x => existingPaths.Contains(x.FilePath))
                .ToList();

            if (existingInBatch.Count > 0)
            {
                foreach (var item in existingInBatch)
                {
                    var matchCount = await dbContext.FileRecords
                        .Where(x => x.FilePath == item.FilePath)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(f => f.StorageLocationId, item.StorageLocationId)
                            .SetProperty(f => f.ScanJobId, item.ScanJobId)
                            .SetProperty(f => f.CorrelationId, item.CorrelationId)
                            .SetProperty(f => f.LocationName, item.LocationName)
                            .SetProperty(f => f.FileName, item.FileName)
                            .SetProperty(f => f.Extension, item.Extension)
                            .SetProperty(f => f.SizeBytes, item.SizeBytes)
                            .SetProperty(f => f.LastModifiedDate, item.LastModifiedDate)
                            .SetProperty(f => f.CreatedDate, item.CreatedDate)
                            .SetProperty(f => f.LastAccessedDate, item.LastAccessedDate)
                            .SetProperty(f => f.DiscoveryDate, item.ScanTimestamp)
                            .SetProperty(f => f.ScanSourcePath, item.ScanSourcePath)
                            .SetProperty(f => f.HashStatus, HashStatus.Pending.ToString())
                            .SetProperty(f => f.HashError, (string?)null),
                        cancellationToken);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new FileBatchIngestResultDto(inserted + updated, inserted, updated, errorCount);
    }
    catch (DbUpdateException dbEx)
    {
        _logger.LogError(dbEx, "Database error during batch upsert. CorrelationId: {CorrelationId}",
            request.CorrelationId);
        errorCount++;
        throw;
    }
}
```

---

## 6. Deployment Checklist

- [ ] Add performance test cases to verify single-query patterns
- [ ] Fix DuplicatesService.GetSummaryAsync with pagination
- [ ] Fix FileInventoryService.MarkDuplicateFlagsByHashAsync with ExecuteUpdateAsync
- [ ] Fix FileInventoryService.UpsertBatchAsync with batch processing
- [ ] Have all services review queries for potential N+1
- [ ] Remove all string-based `.Include()` calls
- [ ] Add query interceptor to development logging for debugging
- [ ] Create performance baseline tests for critical operations
- [ ] Document query patterns in service-specific README files
- [ ] Review all navigation property definitions in DbContext models
- [ ] Update CHANGELOG with reflection safety improvements

---

## Success Criteria

- ✅ No N+1 queries in any service
- ✅ All queries compile-time safe (no string-based includes)
- ✅ Large batch operations use ExecuteUpdateAsync
- ✅ Pagination implemented for large result sets
- ✅ Performance tests validate query efficiency
- ✅ No reflection-based property access in EF Core queries
- ✅ All services follow identical query optimization patterns

