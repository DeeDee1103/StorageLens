namespace StorageLens.Web.Services;

public class ServiceEndpointsOptions
{
    public const string SectionName = "ServiceEndpoints";

    public string Locations { get; set; } = "http://localhost:5101/";
    public string ScanJobs { get; set; } = "http://localhost:5102/";
    public string FileInventory { get; set; } = "http://localhost:5103/";
    public string Duplicates { get; set; } = "http://localhost:5104/";
    public string Analytics { get; set; } = "http://localhost:5105/";
}
