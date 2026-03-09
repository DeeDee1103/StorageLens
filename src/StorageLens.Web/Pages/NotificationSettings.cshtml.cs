using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class NotificationSettingsModel(IConfiguration configuration, IWebHostEnvironment environment, ITierAccessService tierAccess) : PageModel
{
    // ── Page state ───────────────────────────────────────────────────────────

    public bool IsDevelopmentEnvironment { get; private set; }
    public string EnvironmentName { get; private set; } = string.Empty;
    public string SelectedEditEnvironment { get; private set; } = EnvironmentSettingsModel.Development;
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public bool IsFeatureAvailable { get; private set; } = true;

    // ── Email settings ───────────────────────────────────────────────────────

    [BindProperty] public bool EmailEnabled { get; set; } = false;
    [BindProperty] public string SmtpHost { get; set; } = string.Empty;
    [BindProperty] public int SmtpPort { get; set; } = 587;
    [BindProperty] public bool SmtpUseSsl { get; set; } = true;
    [BindProperty] public string SmtpUsername { get; set; } = string.Empty;
    [BindProperty] public string FromAddress { get; set; } = string.Empty;
    [BindProperty] public string FromName { get; set; } = "StorageLens";
    [BindProperty] public string ToAddresses { get; set; } = string.Empty;

    // ── Webhook settings ─────────────────────────────────────────────────────

    [BindProperty] public bool WebhookEnabled { get; set; } = false;
    [BindProperty] public string WebhookUrl { get; set; } = string.Empty;
    [BindProperty] public string WebhookSecret { get; set; } = string.Empty;

    // ── Notification events ──────────────────────────────────────────────────

    [BindProperty] public bool NotifyOnScanComplete { get; set; } = true;
    [BindProperty] public bool NotifyOnScanFailed { get; set; } = true;
    [BindProperty] public bool NotifyOnDuplicatesFound { get; set; } = false;
    [BindProperty] public bool NotifyOnStorageThreshold { get; set; } = false;
    [BindProperty] public int StorageThresholdPercent { get; set; } = 90;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public void OnGet()
    {
        SetPageDefaults();
        IsFeatureAvailable = tierAccess.IsServiceAvailable(ServiceIds.NotificationAlerts);
        if (!IsFeatureAvailable) return;
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

        if (EmailEnabled && string.IsNullOrWhiteSpace(SmtpHost))
        {
            ModelState.AddModelError(nameof(SmtpHost), "SMTP host is required when email notifications are enabled.");
            ErrorMessage = "Please fix the validation errors below.";
            return Page();
        }

        if (WebhookEnabled && !Uri.TryCreate(WebhookUrl, UriKind.Absolute, out _))
        {
            ModelState.AddModelError(nameof(WebhookUrl), "Webhook URL must be a valid absolute URL.");
            ErrorMessage = "Please fix the validation errors below.";
            return Page();
        }

        if (StorageThresholdPercent is < 1 or > 100)
        {
            ModelState.AddModelError(nameof(StorageThresholdPercent), "Storage threshold must be between 1 and 100.");
            ErrorMessage = "Please fix the validation errors below.";
            return Page();
        }

        MergeIntoTargetSettings(NotificationOptions.SectionName, new
        {
            EmailEnabled,
            SmtpHost,
            SmtpPort,
            SmtpUseSsl,
            SmtpUsername,
            FromAddress,
            FromName,
            ToAddresses,
            WebhookEnabled,
            WebhookUrl,
            WebhookSecret,
            NotifyOnScanComplete,
            NotifyOnScanFailed,
            NotifyOnDuplicatesFound,
            NotifyOnStorageThreshold,
            StorageThresholdPercent,
        });

        SuccessMessage = $"Notification settings saved to appsettings.{SelectedEditEnvironment}.json.";
        return Page();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void SetPageDefaults()
    {
        ViewData["Title"] = "Notification Settings";
        ViewData["Nav"] = "Settings";
        IsDevelopmentEnvironment = environment.IsDevelopment();
        EnvironmentName = environment.EnvironmentName;
        SelectedEditEnvironment = EnvironmentSettingsModel.ResolveEditEnvironment(Request);
    }

    private void LoadCurrentSettings()
    {
        var s = configuration.GetSection(NotificationOptions.SectionName);
        EmailEnabled = s.GetValue<bool>("EmailEnabled");
        SmtpHost = s["SmtpHost"] ?? string.Empty;
        SmtpPort = s.GetValue("SmtpPort", 587);
        SmtpUseSsl = s.GetValue("SmtpUseSsl", true);
        SmtpUsername = s["SmtpUsername"] ?? string.Empty;
        FromAddress = s["FromAddress"] ?? string.Empty;
        FromName = s["FromName"] ?? "StorageLens";
        ToAddresses = s["ToAddresses"] ?? string.Empty;
        WebhookEnabled = s.GetValue<bool>("WebhookEnabled");
        WebhookUrl = s["WebhookUrl"] ?? string.Empty;
        WebhookSecret = s["WebhookSecret"] ?? string.Empty;
        NotifyOnScanComplete = s.GetValue("NotifyOnScanComplete", true);
        NotifyOnScanFailed = s.GetValue("NotifyOnScanFailed", true);
        NotifyOnDuplicatesFound = s.GetValue<bool>("NotifyOnDuplicatesFound");
        NotifyOnStorageThreshold = s.GetValue<bool>("NotifyOnStorageThreshold");
        StorageThresholdPercent = s.GetValue("StorageThresholdPercent", 90);
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
