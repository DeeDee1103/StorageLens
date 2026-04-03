using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;
using StorageLens.Web.Services;

namespace StorageLens.Web.Pages;

public class ScanJobsModel(
    ScanJobsApiClient scanJobsApiClient,
    LocationsApiClient locationsApiClient,
    OrchestratorApiClient orchestratorApiClient,
    IOptions<AppearanceOptions> appearanceOptions) : PageModel
{
    [BindProperty]
    public Guid SelectedLocationId { get; set; }

    [BindProperty]
    public string Notes { get; set; } = "Manual scan request";

    [BindProperty]
    public DateTime? ScheduledStartLocal { get; set; }

    public IReadOnlyList<ScanJobDto> Jobs { get; private set; } = [];
    public IReadOnlyList<StorageLocationDto> Locations { get; private set; } = [];

    public bool ServiceUnavailable { get; private set; }
    public int ScanAutoRefreshSeconds { get; private set; } = Math.Max(0, appearanceOptions.Value.ScanAutoRefreshSeconds);

    private static readonly HashSet<string> ActiveStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Running", "Scanning", "Hashing", "DetectingDuplicates", "RefreshingAnalytics", "InProgress", "Queued"
    };

    public bool HasActiveJobs => Jobs.Any(j => ActiveStatuses.Contains(j.Status));

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Scan Jobs";
        ViewData["Nav"] = "ScanJobs";

        var jobs = await scanJobsApiClient.GetScanJobsAsync(cancellationToken);
        var locations = await locationsApiClient.GetLocationsAsync(cancellationToken);

        if (jobs is null || locations is null)
        {
            ServiceUnavailable = true;
            return;
        }

        Jobs = jobs;
        Locations = locations;
        if (SelectedLocationId == Guid.Empty)
        {
            SelectedLocationId = Locations.FirstOrDefault()?.Id ?? Guid.Empty;
        }
    }

    public async Task<IActionResult> OnGetSnapshotAsync(CancellationToken cancellationToken)
    {
        var jobs = await scanJobsApiClient.GetScanJobsAsync(cancellationToken);
        if (jobs is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Scan jobs are currently unavailable." });
        }

        var activeJobCount = jobs.Count(job => ActiveStatuses.Contains(job.Status));
        var failedJobCount = jobs.Count(job => string.Equals(job.Status, "Failed", StringComparison.OrdinalIgnoreCase));
        var averageProgress = jobs.Count > 0 ? (int)Math.Round(jobs.Average(job => job.ProgressPercent)) : 0;
        var totalFilesScanned = jobs.Sum(job => job.FilesScanned);

        return new JsonResult(new ScanJobsSnapshotDto(
            jobs,
            activeJobCount > 0,
            activeJobCount,
            failedJobCount,
            averageProgress,
            totalFilesScanned,
            DateTimeOffset.UtcNow));
    }

    public async Task<IActionResult> OnPostStartAsync(CancellationToken cancellationToken)
    {
        if (SelectedLocationId == Guid.Empty)
        {
            return RedirectToPage();
        }

        DateTimeOffset? scheduledStartUtc = null;
        if (ScheduledStartLocal.HasValue)
        {
            var localDateTime = DateTime.SpecifyKind(ScheduledStartLocal.Value, DateTimeKind.Local);
            scheduledStartUtc = new DateTimeOffset(localDateTime).ToUniversalTime();
        }

        await orchestratorApiClient.StartScanAsync(SelectedLocationId, scheduledStartUtc, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId != Guid.Empty)
        {
            await orchestratorApiClient.CancelJobAsync(jobId, cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResumeAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId != Guid.Empty)
        {
            await orchestratorApiClient.ResumeJobAsync(jobId, cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId != Guid.Empty)
        {
            await scanJobsApiClient.DeleteAsync(jobId, cancellationToken);
        }

        return RedirectToPage();
    }

    public sealed record ScanJobsSnapshotDto(
        IReadOnlyList<ScanJobDto> Jobs,
        bool HasActiveJobs,
        int ActiveJobCount,
        int FailedJobCount,
        int AverageProgress,
        long TotalFilesScanned,
        DateTimeOffset GeneratedAtUtc);
}
