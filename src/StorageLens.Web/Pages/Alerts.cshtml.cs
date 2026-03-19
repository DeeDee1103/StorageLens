using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Clients;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class AlertsModel(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    AnalyticsApiClient analyticsApiClient,
    IAlertEmailService alertEmailService,
    ITierAccessService tierAccess) : PageModel
{
    private const string DefaultRecipient = "dddevacct16783@gmail.com";

    public bool IsDevelopmentEnvironment { get; private set; }
    public string EnvironmentName { get; private set; } = string.Empty;
    public string SelectedEditEnvironment { get; private set; } = EnvironmentSettingsModel.Development;
    public bool IsFeatureAvailable { get; private set; } = true;

    [BindProperty] public int MinThresholdPercent { get; set; } = 70;
    [BindProperty] public int MaxThresholdPercent { get; set; } = 90;
    [BindProperty] public string RecipientEmail { get; set; } = DefaultRecipient;

    public int CurrentUsagePercent { get; private set; }
    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? AutoAlertMessage { get; private set; }
    public bool ServiceUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SetPageDefaults();
        IsFeatureAvailable = tierAccess.IsServiceAvailable(ServiceIds.NotificationAlerts);
        if (!IsFeatureAvailable)
        {
            return;
        }

        LoadCurrentSettings();
        await RefreshCurrentUsageAsync(cancellationToken);
        await TrySendAutoAlertAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        SetPageDefaults();
        IsFeatureAvailable = tierAccess.IsServiceAvailable(ServiceIds.NotificationAlerts);
        if (!IsFeatureAvailable)
        {
            return Page();
        }

        RecipientEmail = string.IsNullOrWhiteSpace(RecipientEmail)
            ? DefaultRecipient
            : RecipientEmail.Trim();

        ValidateThresholdInputs();
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please fix the validation errors below.";
            await RefreshCurrentUsageAsync(cancellationToken);
            return Page();
        }

        if (!IsDevelopmentEnvironment)
        {
            ErrorMessage = "Configuration changes can only be saved in the Development environment.";
            await RefreshCurrentUsageAsync(cancellationToken);
            return Page();
        }

        MergeIntoTargetSettings(AlertThresholdOptions.SectionName, new
        {
            MinThresholdPercent,
            MaxThresholdPercent,
            RecipientEmail
        });

        SuccessMessage = $"Alert threshold settings saved to appsettings.{SelectedEditEnvironment}.json.";

        await RefreshCurrentUsageAsync(cancellationToken);
        await TrySendAutoAlertAsync(cancellationToken);
        return Page();
    }

    private void SetPageDefaults()
    {
        ViewData["Title"] = "Alerts";
        ViewData["Nav"] = "Alerts";
        IsDevelopmentEnvironment = environment.IsDevelopment();
        EnvironmentName = environment.EnvironmentName;
        SelectedEditEnvironment = EnvironmentSettingsModel.ResolveEditEnvironment(Request);
    }

    private void ValidateThresholdInputs()
    {
        if (MinThresholdPercent is < 1 or > 99)
        {
            ModelState.AddModelError(nameof(MinThresholdPercent), "Minimum threshold must be between 1 and 99.");
        }

        if (MaxThresholdPercent is < 2 or > 100)
        {
            ModelState.AddModelError(nameof(MaxThresholdPercent), "Maximum threshold must be between 2 and 100.");
        }

        if (MinThresholdPercent >= MaxThresholdPercent)
        {
            ModelState.AddModelError(nameof(MaxThresholdPercent), "Maximum threshold must be greater than minimum threshold.");
        }

        if (string.IsNullOrWhiteSpace(RecipientEmail) || !RecipientEmail.Contains('@'))
        {
            ModelState.AddModelError(nameof(RecipientEmail), "A valid recipient email is required.");
        }
    }

    private void LoadCurrentSettings()
    {
        var section = configuration.GetSection(AlertThresholdOptions.SectionName);
        MinThresholdPercent = section.GetValue("MinThresholdPercent", 70);
        MaxThresholdPercent = section.GetValue("MaxThresholdPercent", 90);
        RecipientEmail = string.IsNullOrWhiteSpace(section["RecipientEmail"])
            ? DefaultRecipient
            : section["RecipientEmail"]!.Trim();
    }

    private async Task RefreshCurrentUsageAsync(CancellationToken cancellationToken)
    {
        var dashboard = await analyticsApiClient.GetDashboardAsync(cancellationToken);
        if (dashboard is null)
        {
            ServiceUnavailable = true;
            CurrentUsagePercent = 0;
            ErrorMessage ??= "Analytics service is temporarily unavailable. Alerts are using 0% until data is restored.";
            return;
        }

        ServiceUnavailable = false;
        CurrentUsagePercent = (int)Math.Clamp(dashboard.Kpis.DuplicatePercent, 0, 100);
    }

    private async Task TrySendAutoAlertAsync(CancellationToken cancellationToken)
    {
        if (CurrentUsagePercent < MaxThresholdPercent)
        {
            AutoAlertMessage = "Current usage is below the maximum threshold. No alert sent.";
            return;
        }

        var result = await alertEmailService.SendThresholdReachedEmailAsync(
            CurrentUsagePercent,
            MaxThresholdPercent,
            RecipientEmail,
            cancellationToken);

        AutoAlertMessage = result.Message;
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
