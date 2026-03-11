namespace StorageLens.Web.Tests;

/// <summary>
/// Stub HTTP message handler for unit testing HTTP clients without real network calls.
/// </summary>
internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    public int RequestCount { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount += 1;
        return await handler(request, cancellationToken);
    }
}
