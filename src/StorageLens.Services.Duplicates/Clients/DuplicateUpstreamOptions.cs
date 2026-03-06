namespace StorageLens.Services.Duplicates.Clients;

public class DuplicateUpstreamOptions
{
    public const string SectionName = "UpstreamServices";

    public string FileInventory { get; set; } = "http://localhost:5103";
}
