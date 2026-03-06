namespace StorageLens.Services.Scanner.Clients;

public class ScannerUpstreamOptions
{
    public const string SectionName = "UpstreamServices";

    public string FileInventory { get; set; } = "http://localhost:5103";
    public string ScanJobs { get; set; } = "http://localhost:5102";
}
