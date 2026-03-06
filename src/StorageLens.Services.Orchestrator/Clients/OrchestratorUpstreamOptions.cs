namespace StorageLens.Services.Orchestrator.Clients;

public class OrchestratorUpstreamOptions
{
    public const string SectionName = "UpstreamServices";

    public string Locations { get; set; } = "http://localhost:5101";
    public string ScanJobs { get; set; } = "http://localhost:5102";
    public string Scanner { get; set; } = "http://localhost:5106";
    public string Hashing { get; set; } = "http://localhost:5107";
    public string Duplicates { get; set; } = "http://localhost:5104";
    public string Analytics { get; set; } = "http://localhost:5105";
}
