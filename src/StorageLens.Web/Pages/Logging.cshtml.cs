using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;

namespace StorageLens.Web.Pages;

public class LoggingModel(ScanJobsApiClient scanJobsApiClient) : PageModel
{
    public IReadOnlyList<LogInsightEntry> Entries { get; private set; } = [];
    public bool UsingSyntheticData { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Logging";
        ViewData["Nav"] = "Insights";

        var now = DateTimeOffset.UtcNow;

        var liveEntries = await BuildLiveEntriesAsync(cancellationToken);
        if (liveEntries.Count > 0)
        {
            Entries = liveEntries;
            return;
        }

        UsingSyntheticData = true;
        Entries =
        [
            new LogInsightEntry(
                now.AddMinutes(-2),
                "Error",
                "StorageLens.Services.FileInventory",
                "FILEINV-BATCH-500",
                "c2eaf5de-b17d-46ff-b8d7-4f06b6f5d001",
                "/api/files/batch",
                "System",
                "Batch ingest failed while writing file metadata.",
                "SqlException",
                "Timeout expired. The timeout period elapsed prior to completion of the operation.",
                "The file inventory service could not save records in time. This is often temporary.",
                "Wait 1-2 minutes and retry the scan job. If repeated, reduce batch size or check SQL resource usage.",
                false),
            new LogInsightEntry(
                now.AddMinutes(-7),
                "Warning",
                "StorageLens.Services.Scanner",
                "SCANNER-PATH-404",
                "c2eaf5de-b17d-46ff-b8d7-4f06b6f5d002",
                "/api/scanner/execute",
                "System",
                "Scanner skipped inaccessible path during enumeration.",
                "DirectoryNotFoundException",
                "Could not find part of the path '/scan-root/dept-z'.",
                "A configured folder path no longer exists or is not mounted.",
                "Verify the storage path exists and Docker volume mappings are correct. Update location path if needed.",
                false),
            new LogInsightEntry(
                now.AddMinutes(-11),
                "Information",
                "StorageLens.Services.Orchestrator",
                "ORCH-WORKFLOW-START",
                "c2eaf5de-b17d-46ff-b8d7-4f06b6f5d003",
                "/api/orchestrator/scans/start/6fca4de4-9146-4f28-8f4b-a99d2adf8d41",
                "admin@storagelens.local",
                "Workflow started successfully.",
                null,
                null,
                "Workflow started normally.",
                "No action required.",
                false),
            new LogInsightEntry(
                now.AddMinutes(-18),
                "Error",
                "StorageLens.Services.Hashing",
                "HASH-IO-403",
                "c2eaf5de-b17d-46ff-b8d7-4f06b6f5d004",
                "/api/hashing/run/ca10432b-02e6-4cf0-a9c3-a595b38f1d7e",
                "System",
                "Hashing failed while opening one or more files.",
                "UnauthorizedAccessException",
                "Access to the path is denied.",
                "The service account does not have permission to read one or more files.",
                "Check file/folder ACL permissions for scanner and hashing service runtime identities.",
                true),
            new LogInsightEntry(
                now.AddMinutes(-26),
                "Warning",
                "StorageLens.Services.Analytics",
                "ANALYTICS-UPSTREAM-503",
                "c2eaf5de-b17d-46ff-b8d7-4f06b6f5d005",
                "/api/analytics/dashboard",
                "admin@storagelens.local",
                "Analytics fallback triggered due to upstream timeout.",
                "HttpRequestException",
                "Response status code does not indicate success: 503 (Service Unavailable).",
                "Analytics could not read one upstream service and returned fallback values.",
                "Retry after service recovery. Check dependent services health endpoints and recent deployments.",
                false),
            new LogInsightEntry(
                now.AddMinutes(-39),
                "Error",
                "StorageLens.Services.Locations",
                "LOC-AUTH-401",
                "c2eaf5de-b17d-46ff-b8d7-4f06b6f5d006",
                "/api/locations",
                "Unknown",
                "Request rejected due to invalid service credential.",
                "AuthenticationException",
                "Invalid API key.",
                "The caller sent an invalid or missing API key.",
                "Verify X-API-Key configuration and secret rotation status for the calling service.",
                true)
        ];
    }

    private async Task<IReadOnlyList<LogInsightEntry>> BuildLiveEntriesAsync(CancellationToken cancellationToken)
    {
        var jobs = await scanJobsApiClient.GetScanJobsAsync(cancellationToken) ?? [];

        return jobs
            .OrderByDescending(job => job.EndTime ?? job.StartTime)
            .Take(50)
            .Select(MapScanJobToLogEntry)
            .ToList();
    }

    private static LogInsightEntry MapScanJobToLogEntry(ScanJobDto job)
    {
        var note = string.IsNullOrWhiteSpace(job.Notes) ? "No job details recorded." : job.Notes;
        var status = job.Status ?? string.Empty;
        var severity = GetSeverity(job);
        var eventId = $"SCANJOB-{status.ToUpperInvariant()}";
        var requestPath = $"/api/scanjobs/{job.Id}";
        var message = BuildMessage(job);
        var userFriendly = BuildUserFriendlyText(job);
        var resolution = BuildResolutionSteps(job, note);
        var exceptionType = severity switch
        {
            "Error" => "ScanJobFailure",
            "Warning" => "ScanJobWarning",
            _ => "ScanJobStatus"
        };

        return new LogInsightEntry(
            job.EndTime ?? job.StartTime,
            severity,
            "StorageLens.ScanPipeline",
            eventId,
            string.IsNullOrWhiteSpace(job.CorrelationId) ? job.Id.ToString("N") : job.CorrelationId,
            requestPath,
            "System",
            message,
            exceptionType,
            note,
            userFriendly,
            resolution,
            string.Equals(severity, "Error", StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildResolutionSteps(ScanJobDto job, string note)
    {
        if (note.Contains("Scan root path not found", StringComparison.OrdinalIgnoreCase)
            || note.Contains("DirectoryNotFoundException", StringComparison.OrdinalIgnoreCase))
        {
            return "Verify the location path exists on the host, confirm Docker Desktop file sharing allows the drive, and make sure the folder is available offline if it is under OneDrive.";
        }

        if (note.Contains("Only rooted local paths and UNC paths are supported", StringComparison.OrdinalIgnoreCase))
        {
            return "Use a full local path such as C:/folder or a UNC path such as \\server\\share when defining the storage location.";
        }

        if (string.Equals(job.Status, "Queued", StringComparison.OrdinalIgnoreCase))
        {
            return "Queued jobs should move to Scanning quickly. If a job remains queued after a restart, verify the orchestrator background worker is running and the workflow was re-enqueued.";
        }

        if (string.Equals(job.Status, "Running", StringComparison.OrdinalIgnoreCase)
            || string.Equals(job.Status, "Scanning", StringComparison.OrdinalIgnoreCase))
        {
            return "Check whether files scanned and progress percent continue to increase. If not, inspect orchestrator and scanner logs using the correlation ID.";
        }

        if (job.ErrorCount > 0)
        {
            return "Open the failed job on the Scan Jobs page, review the stage and correlation ID, then inspect the scanner and orchestrator logs for the same correlation.";
        }

        return "No action required unless this status remains unchanged longer than expected for the target path size.";
    }

    private static string GetSeverity(ScanJobDto job)
    {
        if (string.Equals(job.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Error";
        }

        if (job.ErrorCount > 0 || job.WarningCount > 0 || string.Equals(job.Status, "PartiallyCompleted", StringComparison.OrdinalIgnoreCase))
        {
            return "Warning";
        }

        return "Information";
    }

    private static string BuildMessage(ScanJobDto job)
    {
        if (string.Equals(job.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return $"Scan failed for {job.LocationName} during {job.CurrentStage}.";
        }

        if (string.Equals(job.Status, "Queued", StringComparison.OrdinalIgnoreCase))
        {
            return $"Scan is queued for {job.LocationName} and waiting for workflow execution.";
        }

        if (string.Equals(job.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return $"Scan completed for {job.LocationName}.";
        }

        return $"Scan is {job.Status.ToLowerInvariant()} for {job.LocationName} during {job.CurrentStage}.";
    }

    private static string BuildUserFriendlyText(ScanJobDto job)
    {
        if (string.Equals(job.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return "The scan pipeline stopped before it could complete successfully.";
        }

        if (string.Equals(job.Status, "Queued", StringComparison.OrdinalIgnoreCase))
        {
            return "The scan request was accepted but has not started processing yet.";
        }

        if (string.Equals(job.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return "The scan finished successfully.";
        }

        return "The scan is active or completed with non-fatal issues.";
    }

    public sealed record LogInsightEntry(
        DateTimeOffset TimestampUtc,
        string Severity,
        string Service,
        string EventId,
        string CorrelationId,
        string RequestPath,
        string User,
        string Message,
        string? ExceptionType,
        string? Exception,
        string UserFriendlyException,
        string ResolutionSteps,
        bool EscalateToIt);
}
