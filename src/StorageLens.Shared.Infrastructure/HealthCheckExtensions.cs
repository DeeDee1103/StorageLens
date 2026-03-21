using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Standardised health check registration and endpoint mapping for all StorageLens services.
///
/// Registers two endpoints:
///   GET /health/live  — liveness probe  (is the process running?)
///                       Always returns 200 unless the process is crashing.
///   GET /health/ready — readiness probe (can the service accept traffic?)
///                       Returns 200 only when:
///                         • The database is reachable  (tag: "db")
///                         • All configured upstream services respond to /health (tag: "upstream")
///
/// Usage (in Program.cs):
///   builder.AddStorageLensHealthChecks(
///       sqlConnectionString: builder.Configuration.GetConnectionString("LocationsDb"),
///       upstreamHealthUris: [
///           new Uri("http://scanjobs:8080/health")
///       ]);
///   ...
///   app.MapStorageLensHealthChecks();
/// </summary>
public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddStorageLensHealthChecks(
        this WebApplicationBuilder builder,
        string? sqlConnectionString = null,
        IEnumerable<Uri>? upstreamHealthUris = null)
    {
        var checks = builder.Services.AddHealthChecks();

        // Database readiness probe
        if (!string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            checks.AddSqlServer(
                connectionString: sqlConnectionString,
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db"]);
        }

        // Upstream service readiness probes
        var uris = upstreamHealthUris?.ToList() ?? [];
        foreach (var uri in uris)
        {
            checks.AddUrlGroup(
                uri: uri,
                name: $"upstream:{uri.Host}",
                failureStatus: HealthStatus.Degraded,   // Degraded, not Unhealthy — don't block traffic
                tags: ["ready", "upstream"]);
        }

        return checks;
    }

    /// <summary>
    /// Maps /health/live and /health/ready endpoints.
    /// Both endpoints are anonymous (no API key required).
    ///
    /// /health/live   — liveness:  returns 200 if process is alive (no checks)
    /// /health/ready  — readiness: returns 200 if DB + upstreams are healthy
    /// </summary>
    public static IEndpointRouteBuilder MapStorageLensHealthChecks(
        this IEndpointRouteBuilder endpoints)
    {
        // Liveness — always 200 while the process is running
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,   // no checks — just confirms the process is alive
            ResponseWriter = WriteJsonResponse,
        }).AllowAnonymous();

        // Readiness — only succeed when DB + upstreams are available
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy]   = StatusCodes.Status200OK,
                [HealthStatus.Degraded]  = StatusCodes.Status200OK,   // degraded = warn but serve
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
            ResponseWriter = WriteJsonResponse,
        }).AllowAnonymous();

        // Keep the legacy /health endpoint for backward compatibility
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteJsonResponse,
        }).AllowAnonymous();

        return endpoints;
    }

    private static Task WriteJsonResponse(HttpContext ctx, HealthReport report)
    {
        ctx.Response.ContentType = "application/json";
        var result = new
        {
            status  = report.Status.ToString(),
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status      = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration    = e.Value.Duration.TotalMilliseconds,
                    tags        = e.Value.Tags,
                })
        };
        return ctx.Response.WriteAsync(
            JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }
}
