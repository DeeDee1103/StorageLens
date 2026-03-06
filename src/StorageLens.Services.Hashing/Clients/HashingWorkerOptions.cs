namespace StorageLens.Services.Hashing.Clients;

public class HashingWorkerOptions
{
    public const string SectionName = "HashingWorker";

    public int MaxDegreeOfParallelism { get; set; } = 4;
    public int BatchSize { get; set; } = 500;
    public long MaxFileSizeBytes { get; set; } = 2L * 1024 * 1024 * 1024;
}
