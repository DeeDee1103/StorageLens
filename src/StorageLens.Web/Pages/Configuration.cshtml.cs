using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class ConfigurationModel(IConfiguration configuration, IWebHostEnvironment environment) : PageModel
{
    // ── Page state ───────────────────────────────────────────────────────────

    public bool IsDevelopmentEnvironment { get; private set; }
    public string EnvironmentName { get; private set; } = string.Empty;
    public string SelectedEditEnvironment { get; private set; } = EnvironmentSettingsModel.Development;
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // ── Database provider ────────────────────────────────────────────────────

    [BindProperty] public string DbProvider { get; set; } = "LocalDb";
    [BindProperty] public bool UseInMemory { get; set; } = false;

    // LocalDB
    [BindProperty] public string LocalDbInstance { get; set; } = "MSSQLLocalDB";
    [BindProperty] public string LocalDbDatabase { get; set; } = "StorageLensMvp";

    // Azure SQL
    [BindProperty] public string AzureSqlServer { get; set; } = string.Empty;
    [BindProperty] public string AzureSqlDatabase { get; set; } = "StorageLens";
    [BindProperty] public string AzureSqlUserId { get; set; } = string.Empty;
    [BindProperty] public bool AzureSqlEncrypt { get; set; } = true;
    [BindProperty] public bool AzureSqlTrustServerCertificate { get; set; } = false;
    [BindProperty] public int ConnectionTimeout { get; set; } = 30;

    // Additional options
    [BindProperty] public int MinPoolSize { get; set; } = 0;
    [BindProperty] public int MaxPoolSize { get; set; } = 100;
    [BindProperty] public string ApplicationName { get; set; } = "StorageLens";

    // ── Service endpoints ────────────────────────────────────────────────────

    [BindProperty] public string LocationsEndpoint { get; set; } = "http://localhost:5101/";
    [BindProperty] public string ScanJobsEndpoint { get; set; } = "http://localhost:5102/";
    [BindProperty] public string FileInventoryEndpoint { get; set; } = "http://localhost:5103/";
    [BindProperty] public string DuplicatesEndpoint { get; set; } = "http://localhost:5104/";
    [BindProperty] public string AnalyticsEndpoint { get; set; } = "http://localhost:5105/";
    [BindProperty] public string OrchestratorEndpoint { get; set; } = "http://localhost:5108/";

    // ── Static service metadata ──────────────────────────────────────────────

    /// <summary>All backend services that use a SQL database, along with their connection string key.</summary>
    public static readonly (string Label, string ConnectionStringKey)[] ServiceDbKeys =
    [
        ("Locations",      "LocationsDb"),
        ("Scan Jobs",      "ScanJobsDb"),
        ("File Inventory", "FileInventoryDb"),
        ("Duplicates",     "DuplicatesDb"),
        ("Scanner",        "ScannerDb"),
        ("Hashing",        "HashingDb"),
        ("Orchestrator",   "OrchestratorDb"),
    ];

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public void OnGet()
    {
        SetPageDefaults();
        LoadCurrentSettings();
    }

    public IActionResult OnPostSaveDbConfig()
    {
        SetPageDefaults();

        if (!IsDevelopmentEnvironment)
        {
            ErrorMessage = "Configuration changes can only be saved in the Development environment.";
            return Page();
        }

        if (DbProvider == "AzureSql" && string.IsNullOrWhiteSpace(AzureSqlServer))
        {
            ModelState.AddModelError(nameof(AzureSqlServer), "Azure SQL server name is required.");
            ErrorMessage = "Please fix the validation errors below.";
            return Page();
        }

        MergeIntoTargetSettings(DatabaseConfigOptions.SectionName, new
        {
            Provider = DbProvider,
            UseInMemory,
            LocalDbInstance,
            LocalDbDatabase,
            AzureSqlServer,
            AzureSqlDatabase,
            AzureSqlUserId,
            AzureSqlEncrypt,
            AzureSqlTrustServerCertificate,
            ConnectionTimeout,
            MinPoolSize,
            MaxPoolSize,
            ApplicationName,
        });

        SuccessMessage = $"Database configuration saved to appsettings.{SelectedEditEnvironment}.json. Restart each service for changes to take effect.";
        return Page();
    }

    public IActionResult OnPostSaveEndpoints()
    {
        SetPageDefaults();

        if (!IsDevelopmentEnvironment)
        {
            ErrorMessage = "Configuration changes can only be saved in the Development environment.";
            return Page();
        }

        if (!ValidateEndpoints())
        {
            ErrorMessage = "All service endpoint URLs must be valid (e.g. http://localhost:5101/).";
            return Page();
        }

        MergeIntoTargetSettings(ServiceEndpointsOptions.SectionName, new
        {
            Locations = LocationsEndpoint,
            ScanJobs = ScanJobsEndpoint,
            FileInventory = FileInventoryEndpoint,
            Duplicates = DuplicatesEndpoint,
            Analytics = AnalyticsEndpoint,
            Orchestrator = OrchestratorEndpoint,
        });

        SuccessMessage = $"Service endpoints saved to appsettings.{SelectedEditEnvironment}.json. Restart the web app for changes to take effect.";
        return Page();
    }

    // ── Connection string generators ─────────────────────────────────────────

    public string BuildLocalDbConnectionString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Server=(localdb)\\{LocalDbInstance};");
        sb.Append($"Database={LocalDbDatabase};");
        sb.Append("Trusted_Connection=True;");
        sb.Append("TrustServerCertificate=True;");
        if (!string.IsNullOrWhiteSpace(ApplicationName))
            sb.Append($"Application Name={ApplicationName};");
        if (MinPoolSize > 0)
            sb.Append($"Min Pool Size={MinPoolSize};");
        if (MaxPoolSize != 100)
            sb.Append($"Max Pool Size={MaxPoolSize};");
        return sb.ToString();
    }

    public string BuildAzureSqlConnectionString()
    {
        var server = string.IsNullOrWhiteSpace(AzureSqlServer)
            ? "<your-server>"
            : AzureSqlServer.Trim();
        var userId = string.IsNullOrWhiteSpace(AzureSqlUserId)
            ? "<your-sql-user>"
            : AzureSqlUserId.Trim();

        var sb = new System.Text.StringBuilder();
        sb.Append($"Server=tcp:{server}.database.windows.net,1433;");
        sb.Append($"Initial Catalog={AzureSqlDatabase};");
        sb.Append($"Encrypt={AzureSqlEncrypt};");
        sb.Append($"TrustServerCertificate={AzureSqlTrustServerCertificate};");
        sb.Append($"Connection Timeout={ConnectionTimeout};");
        sb.Append($"User ID={userId};");
        sb.Append("Password=<store-in-user-secrets>;");
        if (!string.IsNullOrWhiteSpace(ApplicationName))
            sb.Append($"Application Name={ApplicationName};");
        if (MinPoolSize > 0)
            sb.Append($"Min Pool Size={MinPoolSize};");
        if (MaxPoolSize != 100)
            sb.Append($"Max Pool Size={MaxPoolSize};");
        return sb.ToString();
    }

    public string GetConnectionString() =>
        DbProvider == "AzureSql" ? BuildAzureSqlConnectionString() : BuildLocalDbConnectionString();

    // ── Private helpers ──────────────────────────────────────────────────────

    private void SetPageDefaults()
    {
        ViewData["Title"] = "Database Configuration";
        ViewData["Nav"] = "Settings";
        IsDevelopmentEnvironment = environment.IsDevelopment();
        EnvironmentName = environment.EnvironmentName;
        SelectedEditEnvironment = EnvironmentSettingsModel.ResolveEditEnvironment(Request);
    }

    private void LoadCurrentSettings()
    {
        var db = configuration.GetSection(DatabaseConfigOptions.SectionName);
        DbProvider = db["Provider"] ?? "LocalDb";
        UseInMemory = db.GetValue<bool>("UseInMemory");
        LocalDbInstance = db["LocalDbInstance"] ?? "MSSQLLocalDB";
        LocalDbDatabase = db["LocalDbDatabase"] ?? "StorageLensMvp";
        AzureSqlServer = db["AzureSqlServer"] ?? string.Empty;
        AzureSqlDatabase = db["AzureSqlDatabase"] ?? "StorageLens";
        AzureSqlUserId = db["AzureSqlUserId"] ?? string.Empty;
        AzureSqlEncrypt = db.GetValue("AzureSqlEncrypt", true);
        AzureSqlTrustServerCertificate = db.GetValue<bool>("AzureSqlTrustServerCertificate");
        ConnectionTimeout = db.GetValue("ConnectionTimeout", 30);
        MinPoolSize = db.GetValue<int>("MinPoolSize");
        MaxPoolSize = db.GetValue("MaxPoolSize", 100);
        ApplicationName = db["ApplicationName"] ?? "StorageLens";

        var ep = configuration.GetSection(ServiceEndpointsOptions.SectionName);
        LocationsEndpoint = ep["Locations"] ?? "http://localhost:5101/";
        ScanJobsEndpoint = ep["ScanJobs"] ?? "http://localhost:5102/";
        FileInventoryEndpoint = ep["FileInventory"] ?? "http://localhost:5103/";
        DuplicatesEndpoint = ep["Duplicates"] ?? "http://localhost:5104/";
        AnalyticsEndpoint = ep["Analytics"] ?? "http://localhost:5105/";
        OrchestratorEndpoint = ep["Orchestrator"] ?? "http://localhost:5108/";
    }

    private bool ValidateEndpoints()
    {
        static bool IsValid(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        return IsValid(LocationsEndpoint) && IsValid(ScanJobsEndpoint) &&
               IsValid(FileInventoryEndpoint) && IsValid(DuplicatesEndpoint) &&
               IsValid(AnalyticsEndpoint) && IsValid(OrchestratorEndpoint);
    }

    private void MergeIntoTargetSettings(string sectionName, object value)
    {
        var fileName = $"appsettings.{SelectedEditEnvironment}.json";
        var path = Path.Combine(environment.ContentRootPath, fileName);
        var json = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : "{}";
        var doc = JsonNode.Parse(json) as JsonObject ?? [];

        var writeOpts = new JsonSerializerOptions { WriteIndented = true };
        doc[sectionName] = JsonSerializer.SerializeToNode(value, writeOpts);
        var tempPath = path + ".tmp";
        System.IO.File.WriteAllText(tempPath, doc.ToJsonString(writeOpts));
        System.IO.File.Move(tempPath, path, overwrite: true);
    }
}
