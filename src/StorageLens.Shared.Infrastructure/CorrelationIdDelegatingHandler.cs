using Microsoft.AspNetCore.Http;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// A delegating handler that propagates the current request's correlation ID to
/// every outbound HTTP call made by registered HttpClient instances.
///
/// If the handler is used outside an active HTTP request (e.g. background services)
/// a fresh GUID is generated so downstream services always receive a traceable ID.
/// </summary>
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext
                                ?.Items[CorrelationIdMiddleware.HttpContextKey]
                                ?.ToString()
                            ?? Guid.NewGuid().ToString("N");

        if (!request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
