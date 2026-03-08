using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class ScannerSettingsModel(IConfiguration configuration, IWebHostEnvironment environment) : PageModel
{
    // ── Page state ───────────────────────────────────────────────────────────

    public bool IsDevelopmentEnvironment { get; private set; }
    public string EnvironmentName { get; private set; } = string.Empty;
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // ── Hashing worker ────────────────────────────────────────────────────────

    [BindProperty] public int HashingMaxDegreeOfParallelism { get; set; } = 4;
    [BindProperty] public int HashingBatchSize { get; set; } = 500;
    [BindProperty] public long HashingMaxFileSizeBytes { get; set; } = 2_147_483_648L;

    // ── Scanner path mappings ─────────────────────────────────────────────────

    [BindProperty] public bool ScannerEnableAutoMapping { get; set; } = true;
    [BindProperty] public bool ScannerAutoMapInContainerOnly { get; set; } = true;

    // ── Human-readable constants ──────────────────────────────────────────────

    public static readonly (long Bytes, string Label)[] CommonFileSizeLimits =
    [
        (536_870_912L,   "512 MB"),
        (1_073_741_824L, "1 GB"),
        (2_147_483_648L, "2 GB"),
        (4_294_967_296L, "4 GB"),
        (10_737_418_240L,"10 GB"),
        (0L,             "No limit"),
    ];

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public void OnGet()
    {
        SetPageDefaults();
        LoadCurrentSettings();
    }

    public IActionResult OnPostSave()
    {
        SetPageDefaults();

        if (!IsDevelopmentEnvironment)
        {
            ErrorMessage = "Configuration changes can only be saved in the Development environment.";
            return Page();
        }

        if (HashingMaxDegreeOfParallelism < 1 || HashingMaxDegreeOfParallelism > 64)
        {
            ModelState.AddModelError(nameof(HashingMaxDegreeOfParallelism), "Parallelism must be between 1 and 64.");
            ErrorMessage = "Please fix the validation errors below.";
            return Page();
        }

        if (HashingBatchSize < 10 || HashingBatchSize > 10_000)
        {
            ModelState.AddModelError(nameof(HashingBatchSize), "Batch size must be between 10 and 10,000.");
            ErrorMessage = "Please fix the validation errors below.";
            return Page();
        }

        MergeIntoDevSettings(ScannerPerformanceOptions.SectionName, new
        {
            HashingMaxDegreeOfParallelism,
            HashingBatchSize,
            HashingMaxFileSizeBytes,
            ScannerEnableAutoMapping,
            ScannerAutoMapInContainerOnly,
        });

        SuccessMessage = "Scanner & performance settings saved to appsettings.Development.json. " +
                         "Download the per-service snippets below and apply them to each service's appsettings.Development.json.";
        return Page();
    }

    // ── Snippet generators ────────────────────────────────────────────────────

    public string BuildHashingWorkerSnippet()
    {
        var obj = new
        {
            Database = new { UseInMemory = false },
            HashingWorker = new
            {
                MaxDegreeOfParallelism = HashingMaxDegreeOfParallelism,
                BatchSize = HashingBatchSize,
                MaxFileSizeBytes = HashingMaxFileSizeBytes,
            }
        };
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }

    public string BuildScannerMappingSnippet()
    {
        var obj = new
        {
            Database = new { UseInMemory = false },
            ScannerPathMappings = new
            {
                EnableAutoMapping = ScannerEnableAutoMapping,
                AutoMapInContainerOnly = ScannerAutoMapInContainerOnly,
                Mappings = Array.Empty<object>()
            }
        };
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "No limit";
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double val = bytes;
        int i = 0;
        while (val >= 1024 && i < units.Length - 1) { val /= 1024; i++; }
        return $"{val:0.##} {units[i]}";
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SetPageDefaults()
    {
        ViewData["Title"] = "Scanner & Performance";
        ViewData["Nav"] = "Settings";
        IsDevelopmentEnvironment = environment.IsDevelopment();
        EnvironmentName = environment.EnvironmentName;
    }

    private void LoadCurrentSettings()
    {
        var s = configuration.GetSection(ScannerPerformanceOptions.SectionName);
        HashingMaxDegreeOfParallelism = s.GetValue("HashingMaxDegreeOfParallelism", 4);
        HashingBatchSize = s.GetValue("HashingBatchSize", 500);
        HashingMaxFileSizeBytes = s.GetValue("HashingMaxFileSizeBytes", 2_147_483_648L);
        ScannerEnableAutoMapping = s.GetValue("ScannerEnableAutoMapping", true);
        ScannerAutoMapInContainerOnly = s.GetValue("ScannerAutoMapInContainerOnly", true);
    }

    private void MergeIntoDevSettings(string sectionName, object value)
    {
        var path = Path.Combine(environment.ContentRootPath, "appsettings.Development.json");
        var json = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : "{}";
        var doc = JsonNode.Parse(json) as JsonObject ?? [];
        var writeOpts = new JsonSerializerOptions { WriteIndented = true };
        doc[sectionName] = JsonSerializer.SerializeToNode(value, writeOpts);
        var tempPath = path + ".tmp";
        System.IO.File.WriteAllText(tempPath, doc.ToJsonString(writeOpts));
        System.IO.File.Move(tempPath, path, overwrite: true);
    }
}
