using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace StorageLens.Shared.Infrastructure;

public static class ApiKeyAuthentication
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-API-Key";
    public const string ConfigurationKey = "ServiceApiKey";
}

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions { }

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = _configuration[ApiKeyAuthentication.ConfigurationKey];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            // If no key is configured, allow through (development/in-memory mode)
            var claims = new[] { new Claim(ClaimTypes.Name, "internal-service") };
            var identity = new ClaimsIdentity(claims, ApiKeyAuthentication.SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        if (!Request.Headers.TryGetValue(ApiKeyAuthentication.HeaderName, out var extractedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-API-Key header."));
        }

        if (!string.Equals(extractedApiKey, configuredKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var authClaims = new[] { new Claim(ClaimTypes.Name, "internal-service") };
        var authIdentity = new ClaimsIdentity(authClaims, ApiKeyAuthentication.SchemeName);
        var authTicket = new AuthenticationTicket(new ClaimsPrincipal(authIdentity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(authTicket));
    }
}
