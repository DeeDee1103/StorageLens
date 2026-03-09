using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StorageLens.Web.Data;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class ClientManagementModel(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ClientsDbContext db) : PageModel
{
    // ── Cookie constants (shared with PricingAdmin) ──────────────────────────

    private const string OwnerCookieValue = "granted";

    // ── Page state ───────────────────────────────────────────────────────────

    public bool IsOwnerAuthenticated { get; private set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public List<ClientRecord> Clients { get; private set; } = [];
    public List<PricingTier> AvailableTiers { get; private set; } = [];
    public List<PricingService> AvailableServices { get; private set; } = [];

    // Summary stats
    public int TotalClients { get; private set; }
    public int ActiveClients { get; private set; }
    public int NewThisMonth { get; private set; }

    // ── Bind properties (owner PIN form) ─────────────────────────────────────

    [BindProperty] public string OwnerPin { get; set; } = string.Empty;

    // ── Bind properties (add / edit client form) ──────────────────────────────

    [BindProperty] public string FormClientId { get; set; } = string.Empty;
    [BindProperty] public string FormCompanyName { get; set; } = string.Empty;
    [BindProperty] public string FormIndustry { get; set; } = string.Empty;
    [BindProperty] public string FormWebsite { get; set; } = string.Empty;
    [BindProperty] public string FormContactFirstName { get; set; } = string.Empty;
    [BindProperty] public string FormContactLastName { get; set; } = string.Empty;
    [BindProperty] public string FormContactEmail { get; set; } = string.Empty;
    [BindProperty] public string FormContactPhone { get; set; } = string.Empty;
    [BindProperty] public string FormContactTitle { get; set; } = string.Empty;
    [BindProperty] public string FormBillingAddress { get; set; } = string.Empty;
    [BindProperty] public string FormCity { get; set; } = string.Empty;
    [BindProperty] public string FormState { get; set; } = string.Empty;
    [BindProperty] public string FormPostalCode { get; set; } = string.Empty;
    [BindProperty] public string FormCountry { get; set; } = string.Empty;
    [BindProperty] public string FormAssignedTierId { get; set; } = string.Empty;
    [BindProperty] public string[] FormAssignedServiceIds { get; set; } = [];
    [BindProperty] public string FormNotificationEmails { get; set; } = string.Empty;
    [BindProperty] public bool FormNotifyOnScanComplete { get; set; } = true;
    [BindProperty] public bool FormNotifyOnScanFailed { get; set; } = true;
    [BindProperty] public bool FormNotifyOnDuplicatesFound { get; set; } = false;
    [BindProperty] public bool FormNotifyOnStorageThreshold { get; set; } = false;
    [BindProperty] public int FormStorageThresholdPercent { get; set; } = 90;
    [BindProperty] public string FormContractStartDate { get; set; } = string.Empty;
    [BindProperty] public string FormContractEndDate { get; set; } = string.Empty;
    [BindProperty] public string FormStatus { get; set; } = ClientStatus.Active;
    [BindProperty] public string FormNotes { get; set; } = string.Empty;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public async Task OnGetAsync()
    {
        SetPageDefaults();
        LoadPricingConfig();
        if (IsOwnerAuthenticated)
            await LoadClientsAsync();
    }

    // ── Owner authentication ─────────────────────────────────────────────────

    public async Task<IActionResult> OnPostUnlockAsync()
    {
        SetPageDefaults();
        LoadPricingConfig();

        var storedPin = configuration[$"{PricingOptions.SectionName}:OwnerPin"] ?? string.Empty;

        var storedBytes = System.Text.Encoding.UTF8.GetBytes(storedPin);
        var enteredBytes = System.Text.Encoding.UTF8.GetBytes(OwnerPin ?? string.Empty);
        var pinsMatch = storedBytes.Length > 0
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(storedBytes, enteredBytes);

        if (!pinsMatch)
        {
            ErrorMessage = "Incorrect owner PIN. Please try again.";
            return Page();
        }

        Response.Cookies.Append(PricingAdminModel.OwnerCookieName, OwnerCookieValue, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
        });

        IsOwnerAuthenticated = true;
        SuccessMessage = "Owner mode unlocked. You can now manage clients.";
        await LoadClientsAsync();
        return Page();
    }

    public IActionResult OnPostLock()
    {
        Response.Cookies.Delete(PricingAdminModel.OwnerCookieName);
        return RedirectToPage();
    }

    // ── Add / Edit client ─────────────────────────────────────────────────────

    public async Task<IActionResult> OnPostSaveClientAsync()
    {
        SetPageDefaults();
        LoadPricingConfig();

        if (!IsOwnerAuthenticated)
        {
            ErrorMessage = "Owner authentication required.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(FormCompanyName))
        {
            ErrorMessage = "Company name is required.";
            await LoadClientsAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(FormContactEmail))
        {
            ErrorMessage = "Contact email is required.";
            await LoadClientsAsync();
            return Page();
        }

        var serviceIdsJson = JsonSerializer.Serialize(FormAssignedServiceIds ?? []);

        if (Guid.TryParse(FormClientId, out var existingId))
        {
            // Update existing
            var existing = await db.Clients.FindAsync(existingId);
            if (existing is null)
            {
                ErrorMessage = "Client not found.";
                await LoadClientsAsync();
                return Page();
            }

            ApplyFormToRecord(existing, serviceIdsJson);
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            SuccessMessage = $"Client \"{existing.CompanyName}\" updated successfully.";
        }
        else
        {
            // Create new
            var record = new ClientRecord
            {
                AccountNumber = await GenerateAccountNumberAsync(),
            };
            ApplyFormToRecord(record, serviceIdsJson);
            record.CreatedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            db.Clients.Add(record);
            await db.SaveChangesAsync();
            SuccessMessage = $"Client \"{record.CompanyName}\" added successfully (Account: {record.AccountNumber}).";
        }

        await LoadClientsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteClientAsync(Guid clientId)
    {
        SetPageDefaults();
        LoadPricingConfig();

        if (!IsOwnerAuthenticated)
        {
            ErrorMessage = "Owner authentication required.";
            await LoadClientsAsync();
            return Page();
        }

        var record = await db.Clients.FindAsync(clientId);
        if (record is null)
        {
            ErrorMessage = "Client not found.";
            await LoadClientsAsync();
            return Page();
        }

        var name = record.CompanyName;
        db.Clients.Remove(record);
        await db.SaveChangesAsync();
        SuccessMessage = $"Client \"{name}\" deleted.";
        await LoadClientsAsync();
        return Page();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public static List<string> ParseServiceIds(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SetPageDefaults()
    {
        ViewData["Title"] = "Client Management";
        ViewData["Nav"] = "Settings";
        IsOwnerAuthenticated = Request.Cookies.TryGetValue(PricingAdminModel.OwnerCookieName, out var val)
                               && val == OwnerCookieValue;
    }

    private void LoadPricingConfig()
    {
        var section = configuration.GetSection(PricingOptions.SectionName);
        AvailableTiers = section.GetSection("Tiers").Get<List<PricingTier>>() ?? [];
        AvailableServices = section.GetSection("AvailableServices").Get<List<PricingService>>() ?? [];
    }

    private async Task LoadClientsAsync()
    {
        Clients = await db.Clients.OrderByDescending(c => c.CreatedAt).ToListAsync();
        TotalClients = Clients.Count;
        ActiveClients = Clients.Count(c => c.Status == ClientStatus.Active);
        var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        NewThisMonth = Clients.Count(c => c.CreatedAt >= thisMonthStart);
    }

    private void ApplyFormToRecord(ClientRecord record, string serviceIdsJson)
    {
        record.CompanyName = (FormCompanyName ?? string.Empty).Trim();
        record.Industry = (FormIndustry ?? string.Empty).Trim();
        record.Website = (FormWebsite ?? string.Empty).Trim();
        record.ContactFirstName = (FormContactFirstName ?? string.Empty).Trim();
        record.ContactLastName = (FormContactLastName ?? string.Empty).Trim();
        record.ContactEmail = (FormContactEmail ?? string.Empty).Trim();
        record.ContactPhone = (FormContactPhone ?? string.Empty).Trim();
        record.ContactTitle = (FormContactTitle ?? string.Empty).Trim();
        record.BillingAddress = (FormBillingAddress ?? string.Empty).Trim();
        record.City = (FormCity ?? string.Empty).Trim();
        record.State = (FormState ?? string.Empty).Trim();
        record.PostalCode = (FormPostalCode ?? string.Empty).Trim();
        record.Country = (FormCountry ?? string.Empty).Trim();
        record.AssignedTierId = (FormAssignedTierId ?? string.Empty).Trim();
        record.AssignedServiceIdsJson = serviceIdsJson;
        record.NotificationEmails = (FormNotificationEmails ?? string.Empty).Trim();
        record.NotifyOnScanComplete = FormNotifyOnScanComplete;
        record.NotifyOnScanFailed = FormNotifyOnScanFailed;
        record.NotifyOnDuplicatesFound = FormNotifyOnDuplicatesFound;
        record.NotifyOnStorageThreshold = FormNotifyOnStorageThreshold;
        record.StorageThresholdPercent = FormStorageThresholdPercent;
        record.ContractStartDate = DateTime.TryParse(FormContractStartDate, out var sd) ? (DateTime?)sd : null;
        record.ContractEndDate = DateTime.TryParse(FormContractEndDate, out var ed) ? (DateTime?)ed : null;
        record.Status = ClientStatus.All.Contains(FormStatus) ? FormStatus : ClientStatus.Active;
        record.Notes = (FormNotes ?? string.Empty).Trim();
    }

    private async Task<string> GenerateAccountNumberAsync()
    {
        // Query the highest existing sequence number to avoid gaps from deletions.
        // The unique index on AccountNumber ensures the database rejects duplicates
        // if two requests race; callers should retry on a uniqueness violation.
        var accounts = await db.Clients.Select(c => c.AccountNumber).ToListAsync();
        var maxSeq = accounts
            .Select(a =>
            {
                var part = a.StartsWith("SL-", StringComparison.OrdinalIgnoreCase)
                    ? a[3..] : a;
                return int.TryParse(part, out var n) ? n : 0;
            })
            .DefaultIfEmpty(0)
            .Max();
        return $"SL-{(maxSeq + 1):D5}";
    }
}
