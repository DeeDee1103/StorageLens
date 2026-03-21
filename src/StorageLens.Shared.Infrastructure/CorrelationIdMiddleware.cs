using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Middleware that ensures every inbound HTTP request carries a correlation ID.
/// If the client provides an X-Correlation-ID header it is reused; otherwise a
/// new GUID is generated.  The value is:
///   - stored in HttpContext.Items for downstream use within the same process
///   - echoed back in the response header so callers can correlate responses
///   - pushed into the Serilog LogContext so every log line emitted during the
///     request automatically includes the correlation ID
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string HttpContextKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        context.Items[HttpContextKey] = correlationId;
        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension method to add the correlation ID middleware to the pipeline.
/// Should be registered before UseAuthentication so auth failures are also correlated.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
