using StorageLens.Shared.Contracts;

namespace StorageLens.Integration.Tests.Infrastructure;

/// <summary>
/// Fluent test data builders for all request DTOs.
/// Eliminates repetitive object construction across test classes and ensures
/// every test uses valid baseline data by default.
///
/// Pattern:
///   var req = new CreateLocationBuilder().WithName("NAS-01").Build();
/// </summary>

// ── Locations ─────────────────────────────────────────────────────────────────

public sealed class CreateLocationBuilder
{
    private string _name = "Test Location";
    private string _path = @"C:\TestShare";
    private StorageLocationType _type = StorageLocationType.Local;
    private string _siteLocation = "Test Site";
    private LocationStatus _status = LocationStatus.Online;
    private double? _latitude;
    private double? _longitude;

    public CreateLocationBuilder WithName(string name) { _name = name; return this; }
    public CreateLocationBuilder WithPath(string path) { _path = path; return this; }
    public CreateLocationBuilder WithType(StorageLocationType type) { _type = type; return this; }
    public CreateLocationBuilder WithSiteLocation(string siteLocation) { _siteLocation = siteLocation; return this; }
    public CreateLocationBuilder WithStatus(LocationStatus status) { _status = status; return this; }
    public CreateLocationBuilder WithCoordinates(double? latitude, double? longitude) { _latitude = latitude; _longitude = longitude; return this; }

    public CreateStorageLocationRequest Build() =>
        new(_name, _path, _type, _siteLocation, _status, _latitude, _longitude);
}

public sealed class UpdateLocationBuilder
{
    private string _name = "Updated Location";
    private string _path = @"C:\UpdatedShare";
    private StorageLocationType _type = StorageLocationType.Nas;
    private LocationStatus _status = LocationStatus.Online;
    private string _siteLocation = "Test Site";
    private double? _latitude;
    private double? _longitude;

    public UpdateLocationBuilder WithName(string name) { _name = name; return this; }
    public UpdateLocationBuilder WithPath(string path) { _path = path; return this; }
    public UpdateLocationBuilder WithStatus(LocationStatus status) { _status = status; return this; }
    public UpdateLocationBuilder WithSiteLocation(string siteLocation) { _siteLocation = siteLocation; return this; }
    public UpdateLocationBuilder WithCoordinates(double? latitude, double? longitude) { _latitude = latitude; _longitude = longitude; return this; }

    public UpdateStorageLocationRequest Build() =>
        new(_name, _path, _type, _status, _siteLocation, _latitude, _longitude);
}

// ── ScanJobs ──────────────────────────────────────────────────────────────────

public sealed class CreateScanJobBuilder
{
    private Guid _locationId = Guid.NewGuid();
    private string _notes = "Integration test scan";
    private string? _correlationId = null;

    public CreateScanJobBuilder ForLocation(Guid id) { _locationId = id; return this; }
    public CreateScanJobBuilder WithNotes(string notes) { _notes = notes; return this; }
    public CreateScanJobBuilder WithCorrelationId(string id) { _correlationId = id; return this; }

    public CreateScanJobRequest Build() =>
        new(_locationId, _notes, _correlationId ?? Guid.NewGuid().ToString("N"));
}

// ── Scanner ───────────────────────────────────────────────────────────────────

public sealed class ScannerExecuteRequestBuilder
{
    private Guid _locationId = Guid.NewGuid();
    private Guid _scanJobId = Guid.NewGuid();
    private string _correlationId = Guid.NewGuid().ToString("N");
    private string _locationName = "Test Location";
    private string _rootPath = @"C:\TestShare";
    private int _batchSize = 250;

    public ScannerExecuteRequestBuilder ForLocation(Guid id) { _locationId = id; return this; }
    public ScannerExecuteRequestBuilder ForScanJob(Guid id) { _scanJobId = id; return this; }
    public ScannerExecuteRequestBuilder WithCorrelationId(string id) { _correlationId = id; return this; }
    public ScannerExecuteRequestBuilder WithRootPath(string path) { _rootPath = path; return this; }
    public ScannerExecuteRequestBuilder WithBatchSize(int size) { _batchSize = size; return this; }

    public ScannerExecuteRequest Build() =>
        new(_locationId, _scanJobId, _correlationId, _locationName, _rootPath, _batchSize);
}

// ── FileInventory ─────────────────────────────────────────────────────────────

public sealed class FileMetadataBatchBuilder
{
    private Guid _locationId = Guid.NewGuid();
    private Guid _scanJobId = Guid.NewGuid();
    private string _correlationId = Guid.NewGuid().ToString("N");
    private string _locationName = "Test Location";
    private string _scanSourcePath = @"C:\TestShare";
    private List<FileMetadataDto> _files = [];

    public FileMetadataBatchBuilder ForLocation(Guid id) { _locationId = id; return this; }
    public FileMetadataBatchBuilder ForScanJob(Guid id) { _scanJobId = id; return this; }
    public FileMetadataBatchBuilder WithFile(string fileName, string path, long sizeBytes = 1024)
    {
        _files.Add(new FileMetadataDto(
            null, _locationId, _scanJobId, _correlationId,
            _locationName, fileName, path, System.IO.Path.GetExtension(fileName),
            sizeBytes, DateTimeOffset.UtcNow.AddDays(-1), null, null,
            DateTimeOffset.UtcNow, _scanSourcePath));
        return this;
    }

    public FileMetadataBatchIngestRequest Build() =>
        new(_locationId, _scanJobId, _correlationId, _locationName, _scanSourcePath, _files);
}
