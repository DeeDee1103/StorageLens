using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Web.Clients;
using StorageLens.Shared.Contracts;

namespace StorageLens.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AnalyticsApiClient _analyticsApiClient;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AnalyticsApiClient analyticsApiClient, ILogger<IndexModel> logger)
    {
        _analyticsApiClient = analyticsApiClient;
        _logger = logger;
    }

    public DashboardDto Dashboard { get; private set; } = new(
        new DashboardKpiDto(0, 0, 0, 0),
        [],
        [],
        [],
        [],
        [],
        []);

    public bool ServiceUnavailable { get; private set; }

    [BindProperty]
    public DemoRequestInput DemoRequest { get; set; } = new();

    [TempData]
    public string? DemoRequestStatus { get; set; }

    [TempData]
    public string? DemoRequestError { get; set; }

    public bool ShowDemoModal { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dashboard";
        ViewData["Nav"] = "Dashboard";

        var dashboard = await _analyticsApiClient.GetDashboardAsync(cancellationToken);
        if (dashboard is not null)
        {
            Dashboard = dashboard;
            return;
        }

        ServiceUnavailable = true;
        _logger.LogWarning("Analytics service returned no dashboard payload.");
    }

    public IActionResult OnPostRequestDemo()
    {
        if (!ModelState.IsValid)
        {
            ShowDemoModal = true;
            DemoRequestError = "Please complete the required fields before submitting your request.";
            return Page();
        }

        _logger.LogInformation(
            "Demo request submitted: Name={Name}, Email={Email}, Company={Company}, TeamSize={TeamSize}, InterestedInPilot={InterestedInPilot}",
            DemoRequest.Name,
            DemoRequest.WorkEmail,
            DemoRequest.Company,
            DemoRequest.TeamSize,
            DemoRequest.InterestedInPilot);

        DemoRequestStatus = "Thanks. Your demo request has been received. Our team will contact you shortly.";
        return RedirectToPage("/Index", new { contact = "success" });
    }

    public sealed class DemoRequestInput
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Work email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [StringLength(180)]
        public string WorkEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company is required")]
        [StringLength(180)]
        public string Company { get; set; } = string.Empty;

        [Required(ErrorMessage = "Team size is required")]
        [StringLength(60)]
        public string TeamSize { get; set; } = string.Empty;

        [StringLength(1500)]
        public string? Notes { get; set; }

        public bool InterestedInPilot { get; set; }
    }
}
