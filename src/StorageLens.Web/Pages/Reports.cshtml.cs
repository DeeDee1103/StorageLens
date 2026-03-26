using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StorageLens.Shared.Contracts;
using StorageLens.Web.Clients;

namespace StorageLens.Web.Pages;

public class ReportsModel(AnalyticsApiClient analyticsApiClient) : PageModel
{
    public ReportsDto Reports { get; private set; } = new([], [], [], [], []);

    public bool ServiceUnavailable { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Reports";
        ViewData["Nav"] = "Reports";

        var payload = await analyticsApiClient.GetReportsAsync(cancellationToken);
        if (payload is not null)
        {
            Reports = payload;
            return;
        }

        ServiceUnavailable = true;
    }

    public async Task<IActionResult> OnGetExportCsvAsync(string report, CancellationToken cancellationToken)
    {
        var payload = await analyticsApiClient.GetReportsAsync(cancellationToken);
        if (payload is null)
        {
            TempData["ReportExportError"] = "Reports service is unavailable. Unable to export right now.";
            return RedirectToPage();
        }

        var normalized = (report ?? string.Empty).Trim().ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        string csv;
        string fileName;

        switch (normalized)
        {
            case "oldest-files":
                csv = BuildFileRecordsCsv(payload.OldestFiles);
                fileName = $"storagelens-oldest-files-{timestamp}.csv";
                break;
            case "largest-files":
                csv = BuildFileRecordsCsv(payload.LargestFiles);
                fileName = $"storagelens-largest-files-{timestamp}.csv";
                break;
            case "common-types":
                csv = BuildChartPointsCsv(payload.MostCommonFileTypes, "FileType", "Count");
                fileName = $"storagelens-common-file-types-{timestamp}.csv";
                break;
            case "duplicate-heavy":
                csv = BuildChartPointsCsv(payload.DuplicateHeavyLocations, "Location", "DuplicateLoad");
                fileName = $"storagelens-duplicate-heavy-locations-{timestamp}.csv";
                break;
            default:
                TempData["ReportExportError"] = "Unknown report export option.";
                return RedirectToPage();
        }

        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    private static string BuildFileRecordsCsv(IEnumerable<FileRecordDto> files)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FileName,Path,Extension,SizeBytes,LocationName,LastModifiedUtc");

        foreach (var file in files)
        {
            sb.AppendLine(string.Join(",",
                Csv(file.FileName),
                Csv(file.FilePath),
                Csv(file.Extension),
                file.SizeBytes.ToString(CultureInfo.InvariantCulture),
                Csv(file.LocationName),
                file.LastModifiedDate.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildChartPointsCsv(IEnumerable<ChartPointDto> points, string labelHeader, string valueHeader)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{labelHeader},{valueHeader}");

        foreach (var point in points)
        {
            sb.AppendLine($"{Csv(point.Label)},{point.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        var input = value ?? string.Empty;
        return $"\"{input.Replace("\"", "\"\"")}\"";
    }
}
