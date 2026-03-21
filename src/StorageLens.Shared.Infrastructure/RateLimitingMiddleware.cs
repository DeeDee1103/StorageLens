using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Simple in-memory rate limiting middleware.
/// Prevents API abuse by limiting requests per IP address.
/// NOTE: For production with multiple instances, use distributed rate limiting (Redis, Azure App Configuration, etc.)
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;
    private readonly ConcurrentDictionary<string, ClientRateLimitInfo> _clientInfo;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
        _clientInfo = new ConcurrentDictionary<string, ClientRateLimitInfo>();

        // Periodic cleanup of expired entries every 5 minutes
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                CleanupExpiredEntries();
            }
        });
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);

        if (!IsRateLimitExceeded(clientId))
        {
            await _next(context);
            return;
        }

        _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.Append("Retry-After", _options.WindowDuration.TotalSeconds.ToString("F0"));
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded",
            retryAfterSeconds = _options.WindowDuration.TotalSeconds
        });
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get real IP from X-Forwarded-For header (common in proxies)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            return forwardedFor.ToString().Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
    }

    private bool IsRateLimitExceeded(string clientId)
    {
        var now = DateTime.UtcNow;

        var info = _clientInfo.AddOrUpdate(
            clientId,
            new ClientRateLimitInfo { FirstRequestTime = now, RequestCount = 1, WindowStart = now },
            (key, existing) =>
            {
                // If window has expired, reset
                if (now - existing.WindowStart > _options.WindowDuration)
                {
                    return new ClientRateLimitInfo { FirstRequestTime = now, RequestCount = 1, WindowStart = now };
                }

                // Increment count
                existing.RequestCount++;
                return existing;
            });

        return info.RequestCount > _options.MaxRequestsPerWindow;
    }

    private void CleanupExpiredEntries()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = _clientInfo
            .Where(kvp => now - kvp.Value.WindowStart > _options.WindowDuration.Add(TimeSpan.FromMinutes(1)))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _clientInfo.TryRemove(key, out _);
        }
    }

    private class ClientRateLimitInfo
    {
        public DateTime FirstRequestTime { get; set; }
        public int RequestCount { get; set; }
        public DateTime WindowStart { get; set; }
    }
}

/// <summary>
/// Rate limiting configuration options.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Maximum number of requests allowed per window (default: 100 requests).
    /// </summary>
    public int MaxRequestsPerWindow { get; set; } = 100;

    /// <summary>
    /// Time window for rate limiting (default: 1 minute).
    /// </summary>
    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Paths that should be excluded from rate limiting (e.g., /health, /metrics).
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/health/ready",
        "/health/live",
        "/metrics"
    };
}

/// <summary>
/// Extension methods for rate limiting middleware configuration.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds rate limiting middleware to the pipeline with default configuration.
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseRateLimiting(new RateLimitOptions());
    }

    /// <summary>
    /// Adds rate limiting middleware to the pipeline with custom configuration.
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app, RateLimitOptions options)
    {
        return app.UseMiddleware<RateLimitingMiddleware>(options);
    }

    /// <summary>
    /// Adds rate limiting services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, RateLimitOptions? options = null)
    {
        services.AddSingleton(options ?? new RateLimitOptions());
        return services;
    }
}
