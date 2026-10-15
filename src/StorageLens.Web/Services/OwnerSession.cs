using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace StorageLens.Web.Services;

/// <summary>
/// Owner-mode session for the admin pages (Pricing Admin, Client Management).
/// The cookie carries a Data Protection-encrypted, time-limited payload, so it
/// cannot be forged or replayed past its lifetime by editing the cookie value.
/// </summary>
public sealed class OwnerSession(IDataProtectionProvider dataProtectionProvider, IWebHostEnvironment environment)
{
    public const string CookieName = "sl-owner-mode";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    private const string Purpose = "StorageLens.Web.OwnerSession.v1";
    private const string Payload = "owner";

    private ITimeLimitedDataProtector Protector =>
        dataProtectionProvider.CreateProtector(Purpose).ToTimeLimitedDataProtector();

    public bool IsAuthenticated(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(CookieName, out var value) || string.IsNullOrEmpty(value))
            return false;

        try
        {
            return Protector.Unprotect(value, out _) == Payload;
        }
        catch (CryptographicException)
        {
            // Tampered, expired, or protected with a key this instance does not have.
            return false;
        }
    }

    public void SignIn(HttpResponse response)
    {
        response.Cookies.Append(CookieName, Protector.Protect(Payload, Lifetime), new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            MaxAge = Lifetime,
        });
    }

    public static void SignOut(HttpResponse response) => response.Cookies.Delete(CookieName);

    /// <summary>
    /// Constant-time PIN check. An unconfigured (empty) PIN never matches,
    /// so owner mode stays locked until a PIN is supplied via secrets.
    /// </summary>
    public static bool PinMatches(string? configuredPin, string? enteredPin)
    {
        var storedBytes = Encoding.UTF8.GetBytes(configuredPin ?? string.Empty);
        var enteredBytes = Encoding.UTF8.GetBytes(enteredPin ?? string.Empty);
        return storedBytes.Length > 0 && CryptographicOperations.FixedTimeEquals(storedBytes, enteredBytes);
    }
}
