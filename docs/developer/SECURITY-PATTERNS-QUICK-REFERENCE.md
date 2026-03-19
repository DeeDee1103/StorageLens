# Security Patterns Quick Reference

**For Quick Copy-Paste Security Code**

Use this file when implementing new features. Copy patterns and adapt to your use case.

---

## Authorization Pattern

**Add to ALL controller classes:**

```csharp
using Microsoft.AspNetCore.Authorization;

[Authorize]  // ← ADD THIS LINE
[ApiController]
[Route("api/[controller]")]
public class YourController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<YourDto>> GetById(Guid id)
    {
        // All requests must have X-API-Key header
        return Ok(result);
    }
}
```

---

## Email Validation Pattern

**When validating email input:**

```csharp
using StorageLens.Shared.Infrastructure.Security;

// In your page model or service
public bool ValidateEmail(string email)
{
    if (!EmailValidation.IsValidEmail(email))
    {
        ModelState.AddModelError(nameof(RecipientEmail), 
            "Please enter a valid email address.");
        return false;
    }
    return true;
}

// For multiple emails (comma or semicolon separated)
var emails = EmailValidation.TryExtractValidEmails(emailString);
foreach (var email in emails)
{
    // send to each email
}
```

**Configuration (appsettings.json):**

```json
{
  "AlertThresholdOptions": {
    "RecipientEmail": "alerts@company.com"
  }
}
```

---

## File Validation Pattern

**Before opening any file stream:**

```csharp
using StorageLens.Shared.Infrastructure.Security;

// ✅ CORRECT: Validate BEFORE opening stream
try
{
    FileSecurityValidation.ValidateFileBeforeProcessing(
        filePath: userProvidedPath,
        maxFileSizeBytes: 5_000_000_000); // 5GB

    // Now safe to open the file
    await using var stream = new FileStream(
        userProvidedPath,
        FileMode.Open,
        FileAccess.Read);
    
    var content = await stream.ReadAsync(...);
}
catch (FileNotFoundException ex)
{
    logger.LogWarning(ex, "File not found");
}
catch (InvalidOperationException ex)
{
    logger.LogWarning(ex, "File too large");
}
```

**For path safety checks with traversal prevention:**

```csharp
// Ensure path is within allowed directory
string rootPath = "/allowed/uploads";
string userPath = userInput; // e.g., "/allowed/uploads/file.txt"

if (!FileSecurityValidation.IsPathSafe(userPath, rootPath))
{
    throw new InvalidOperationException("Path not allowed");
}
```

---

## HTTP Client Configuration Pattern

**In Program.cs for each service:**

```csharp
var builder = WebApplication.CreateBuilder(args);

var serviceApiKey = builder.Configuration["ServiceApiKey"] ?? string.Empty;

// ✅ For quick API calls (default 30 seconds):
builder.Services.AddHttpClient("other-service", client =>
{
    client.BaseAddress = new Uri("https://other-service.example.com");
    client.Timeout = TimeSpan.FromSeconds(30); // ← ADD THIS
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-API-Key", serviceApiKey);
});

// ✅ For long-running operations (4 hours):
builder.Services.AddHttpClient("scanner", client =>
{
    client.BaseAddress = new Uri("https://scanner-service.example.com");
    client.Timeout = TimeSpan.FromHours(4); // ← LONG OPERATIONS
    if (!string.IsNullOrWhiteSpace(serviceApiKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-API-Key", serviceApiKey);
});
```

---

## Key Vault Integration Pattern

**For accessing secrets in Program.cs:**

```csharp
using StorageLens.Shared.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Option 1: Auto-detect Key Vault from environment
builder.Configuration.AddAzureKeyVaultIfAvailable();

// Option 2: Explicit Key Vault URL
var keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URL");
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}

// Validate required secrets at startup
var requiredSecrets = new[] { "SqlPassword", "ServiceApiKey" };
KeyVaultIntegration.ValidateRequiredSecrets(
    builder.Configuration, 
    requiredSecrets);

// Now access secrets normally
var sqlPassword = builder.Configuration["SqlPassword"];
var apiKey = builder.Configuration["ServiceApiKey"];
```

**Then in your service, just use IConfiguration:**

```csharp
public MyService(IConfiguration configuration)
{
    var apiKey = configuration["ServiceApiKey"];
    // Use the key for API calls
}
```

---

## Rate Limiting Pattern (Pending Integration)

**When adding to a service (implementation coming):**

```csharp
// In Program.cs:

var builder = WebApplication.CreateBuilder(args);

// Register rate limiting
builder.Services.AddRateLimitingMiddleware(new RateLimitOptions
{
    MaxRequestsPerWindow = 100,
    WindowDuration = TimeSpan.FromMinutes(1),
    ExcludedPaths = new[] { "/health", "/metrics" }
});

var app = builder.Build();

// Apply rate limiting middleware (before authentication)
app.UseRateLimitingMiddleware();
app.UseAuthentication();
app.UseAuthorization();
```

**When rate limit exceeded, client receives:**

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 45
Content-Type: application/json

{"error":"Rate limit exceeded. Retry after 45 seconds."}
```

---

## Best Practices Checklist

For each new feature, verify:

- [ ] **Authorization**: All endpoints have `[Authorize]` attribute
- [ ] **Input Validation**: 
  - [ ] Emails validated with `EmailValidation.IsValidEmail()`
  - [ ] File paths validated with `FileSecurityValidation.IsPathSafe()`
  - [ ] File sizes checked BEFORE stream open
- [ ] **Resource Management**:
  - [ ] HTTP clients have 30s timeout (or 4h for long-running)
  - [ ] Large queries use pagination (`.Skip()` / `.Take()`)
- [ ] **Secrets**:
  - [ ] No hardcoded credentials
  - [ ] Use `IConfiguration` for secrets (from Key Vault or env vars)
  - [ ] `.env` files not committed
- [ ] **Logging**:
  - [ ] No credentials logged
  - [ ] Authorization failures logged at WARNING level
  - [ ] Include correlation IDs in cross-service calls
- [ ] **Testing**:
  - [ ] Unit tests for validation logic
  - [ ] Integration tests for authorization

---

## Common Mistakes to Avoid

| ❌ DON'T | ✅ DO |
|----------|------|
| Hardcode email: `SendEmail("admin@company.com")` | Load from config: `config["AlertEmail"]` |
| Check file size after stream open | Check file size BEFORE stream open |
| Use `.Contains('@')` for email validation | Use `EmailValidation.IsValidEmail()` |
| Set `Timeout = Timeout.InfiniteTimeSpan` | Set `Timeout = TimeSpan.FromSeconds(30)` |
| No authorization on controller | Add `[Authorize]` class attribute |
| Log credentials: `logger.LogError("Key: {Key}", apiKey)` | Log action: `logger.LogError("Auth failed for endpoint")` |
| Load all DB records into memory | Use pagination: `.Skip(page).Take(size)` |
| No email validation on user input | Always use `EmailValidation.IsValidEmail()` |

---

## Contacting the Team

**Questions?** Refer to:
- `docs/developer/SECURITY-FIXES.md` - Comprehensive security documentation
- `docs/DeveloperChecklist.md` - Phase 12.5 for full security setup
- Code examples in existing services (Hashing, Orchestrator, etc.)

**Reporting Security Issues:**
- Open a GitHub issue with `security` label (keep private)
- Include the code context and potential impact
- Don't include actual credentials or PII

---

**Last Updated**: March 19, 2026  
**Version**: 1.0
