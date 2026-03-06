namespace StorageLens.Services.Scanner.Clients;

public class ScannerPathMappingOptions
{
    public const string SectionName = "ScannerPathMappings";

    // When true, scanner may auto-map Windows/UNC paths into /host/* conventions.
    public bool EnableAutoMapping { get; set; } = true;

    // When true, built-in auto-mapping only applies when running inside a container.
    public bool AutoMapInContainerOnly { get; set; } = true;

    public List<ScannerPathMappingRule> Mappings { get; set; } = [];
}

public class ScannerPathMappingRule
{
    // Supported modes: Prefix, Regex
    public string Mode { get; set; } = "Prefix";

    // Prefix mode uses StartsWith(pattern), Regex mode uses Regex.Replace(pattern, replacement).
    public string Pattern { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
    public string Description { get; set; } = string.Empty;
}
