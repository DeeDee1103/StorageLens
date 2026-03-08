namespace StorageLens.Web.Services;

public class ScannerPerformanceOptions
{
    public const string SectionName = "ScannerPerformance";

    // ── Hashing worker ────────────────────────────────────────────────────────

    /// <summary>Maximum number of files hashed concurrently.</summary>
    public int HashingMaxDegreeOfParallelism { get; set; } = 4;

    /// <summary>Number of files dequeued and hashed per worker iteration.</summary>
    public int HashingBatchSize { get; set; } = 500;

    /// <summary>Files larger than this value (bytes) are skipped during hashing. Default 2 GB.</summary>
    public long HashingMaxFileSizeBytes { get; set; } = 2_147_483_648L;

    // ── Scanner path mappings ─────────────────────────────────────────────────

    /// <summary>
    /// When true the scanner automatically maps container-internal paths to the
    /// host-mounted equivalents (e.g. /host/drives/c → C:\).
    /// </summary>
    public bool ScannerEnableAutoMapping { get; set; } = true;

    /// <summary>
    /// When true, auto-mapping only applies when the scanner is running inside a container.
    /// </summary>
    public bool ScannerAutoMapInContainerOnly { get; set; } = true;
}
