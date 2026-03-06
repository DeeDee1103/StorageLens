using Microsoft.AspNetCore.Mvc;
using StorageLens.Services.Analytics.Interfaces;
using StorageLens.Shared.Contracts;

namespace StorageLens.Services.Analytics.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await analyticsService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("reports")]
    public async Task<ActionResult<ReportsDto>> GetReports(CancellationToken cancellationToken)
    {
        var result = await analyticsService.GetReportsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("charts")]
    public async Task<ActionResult<IReadOnlyList<ChartPointDto>>> GetCharts(CancellationToken cancellationToken)
    {
        var result = await analyticsService.GetChartsAsync(cancellationToken);
        return Ok(result);
    }
}
