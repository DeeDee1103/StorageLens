using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StorageLens.Web.Pages;

public class LoggingModel : PageModel
{
    public IReadOnlyList<LogInsightEntry> Entries { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Logging";
        ViewData["Nav"] = "Insights";

        var now = DateTimeOffset.UtcNow;

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
