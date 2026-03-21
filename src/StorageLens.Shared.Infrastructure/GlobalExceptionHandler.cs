using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Centralised exception handler for all StorageLens services.
///
/// Registered via builder.Services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;() in
/// ObservabilityExtensions.  Converts every unhandled exception into an RFC 7807
/// ProblemDetails response so callers always receive a consistent error shape:
///
///   {
///     "status": 500,
///     "title": "An unexpected error occurred",
///     "detail": "...",  // safe message — never exposes internal paths or stack traces
///     "correlationId": "abc123"
///   }
///
/// File paths and raw exception details are never forwarded to the caller;
/// they are logged server-side with the correlation ID for diagnosis.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[CorrelationIdMiddleware.HttpContextKey]?.ToString()
                            ?? httpContext.TraceIdentifier;

        var statusCode = MapStatusCode(exception);

        // Log full details server-side (never sent to client)
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception [{ExceptionType}] — CorrelationId: {CorrelationId}",
                exception.GetType().Name,
                correlationId);
        }
        else
        {
            _logger.LogWarning(
                "Client error [{ExceptionType}]: {SafeMessage} — CorrelationId: {CorrelationId}",
                exception.GetType().Name,
                GetSafeMessage(exception),
                correlationId);
        }

        httpContext.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Status  = statusCode,
            Title   = GetTitle(statusCode),
            Detail  = GetSafeDetail(exception, statusCode),
        };
        problem.Extensions["correlationId"] = correlationId;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static int MapStatusCode(Exception ex) => ex switch
    {
        ArgumentException              => StatusCodes.Status400BadRequest,
        ArgumentNullException          => StatusCodes.Status400BadRequest,
        KeyNotFoundException           => StatusCodes.Status404NotFound,
        FileNotFoundException          => StatusCodes.Status404NotFound,
        UnauthorizedAccessException    => StatusCodes.Status403Forbidden,
        NotImplementedException        => StatusCodes.Status501NotImplemented,
        OperationCanceledException     => StatusCodes.Status499ClientClosedRequest,
        _                              => StatusCodes.Status500InternalServerError,
    };

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        403 => "Forbidden",
        404 => "Not Found",
        499 => "Request Cancelled",
        501 => "Not Implemented",
        _   => "An unexpected error occurred",
    };

    /// <summary>
    /// Returns a caller-safe message.
    /// For 4xx (client errors) we forward the exception message — it is intentional
    /// and contains no internal state.
    /// For 5xx we return a generic message to prevent information disclosure.
    /// File paths are always stripped before returning.
    /// </summary>
    private static string GetSafeDetail(Exception ex, int statusCode)
    {
        if (statusCode is >= 500 or StatusCodes.Status499ClientClosedRequest)
            return "An internal error occurred. Please contact support and quote the correlation ID.";

        return GetSafeMessage(ex);
    }

    /// <summary>
    /// Returns the exception message with any filesystem paths redacted.
    /// Prevents path disclosure in 4xx responses.
    /// </summary>
    private static string GetSafeMessage(Exception ex)
    {
        var msg = ex.Message;
        // Redact anything that looks like a Windows or UNC path
        msg = System.Text.RegularExpressions.Regex.Replace(
            msg,
            @"[A-Za-z]:\\[^\s""'<>|?*\r\n]*|\\\\[^\s""'<>|?*\r\n]+",
            "[path]");
        return msg;
    }
}
