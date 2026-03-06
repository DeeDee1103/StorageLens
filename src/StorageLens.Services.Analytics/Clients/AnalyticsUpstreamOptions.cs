namespace StorageLens.Services.Analytics.Clients;

public class AnalyticsUpstreamOptions
{
    public const string SectionName = "UpstreamServices";

    public string Locations { get; set; } = "http://localhost:5101";
    public string ScanJobs { get; set; } = "http://localhost:5102";
    public string FileInventory { get; set; } = "http://localhost:5103";
    public string Duplicates { get; set; } = "http://localhost:5104";
}
