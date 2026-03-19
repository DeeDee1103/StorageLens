# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and this project uses semantic versioning.

## [Unreleased]

### Security
**Critical (P0)**
- Added `[Authorize]` attribute to all 8 microservice controllers (Locations, ScanJobs, FileInventory, Duplicates, Analytics, Hashing, Scanner, Orchestrator) to enforce API key authentication. Service-to-service calls must include `X-API-Key` header.
- Documented credential management best practices in `docs/developer/SECURITY-FIXES.md`. Credentials must be stored in Azure Key Vault (production) or environment variables (local development). Remove `.env` files from commits.

**High Priority (P1)**
- `HashingService.ProcessFileAsync`: Refactored to validate file existence and size BEFORE opening FileStream, preventing memory DoS attacks. Uses new `FileSecurityValidation.ValidateFileBeforeProcessing()` utility.
- `Alerts.cshtml.cs`: Migrated from hardcoded email recipient to configuration-based model. Email validation upgraded to RFC 5322 compliant pattern using `EmailValidation.IsValidEmail()` utility. Moved email validation to shared `StorageLens.Shared.Infrastructure.Security` namespace.

**Medium Priority (P2)**
- All HTTP client configurations updated with explicit timeouts: 30-second default for regular API calls, 4-hour timeout for long-running operations (Scanner, Hashing services). Prevents hung connections and resource exhaustion. Applied to Orchestrator, Analytics, Scanner, Duplicates, Hashing services.
- Added rate limiting infrastructure via `RateLimitingMiddleware.cs` (created but pending integration to services). Configured for 100 requests per minute per IP, excludes health endpoints, returns HTTP 429 with Retry-After header.

### Added
- New security utility classes in `StorageLens.Shared.Infrastructure/Security/`:
  - `EmailValidation.cs`: RFC 5322 compliant email validation; supports comma/semicolon-separated email lists
  - `FileSecurityValidation.cs`: Pre-stream file validation (existence, size, path traversal checks)
  - `KeyVaultIntegration.cs`: Azure Key Vault configuration with fallback to environment variables; startup secret validation
  - `RateLimitingMiddleware.cs`: Per-IP rate limiting with configurable windows and request limits (pending integration)
  - `HttpClientConfigurationExtensions.cs`: Secure HTTP client timeout configuration
- Comprehensive security documentation: `docs/developer/SECURITY-FIXES.md` - covers all 11 security issues identified in external code review, implementation status, and usage patterns for new developers
- Security checklist in `docs/developer/SECURITY-FIXES.md` for developers to follow when adding new features

### Changed
- `StorageLens.Services.Hashing\Program.cs`: Added 30-second timeout to fileinventory and scanjobs HTTP clients
- `StorageLens.Services.Orchestrator\Program.cs`: Added 30-second timeout to locations, scanjobs, duplicates, and analytics HTTP clients (kept 4-hour timeout for scanner and hashing long-running operations)
- `StorageLens.Services.Analytics\Program.cs`: Added 30-second timeout to all HTTP client registrations (locations, scanjobs, fileinventory, duplicates)
- `StorageLens.Services.Scanner\Program.cs`: Added 30-second timeout to fileinventory and scanjobs HTTP clients
- `StorageLens.Services.Duplicates\Program.cs`: Added 30-second timeout to fileinventory HTTP client
- `StorageLens.Web\Pages\Alerts.cshtml.cs`: Integrated `EmailValidation.IsValidEmail()` for RFC 5322 compliant validation; added TODO for config write audit trail

### Fixed
- **P1 Security**: Fixed potential memory DoS vulnerability in HashingService where file size was checked after stream open
- **P1 Security**: Fixed insufficient email validation (now uses RFC 5322 pattern instead of simple `.Contains('@')`)
- **P2 Security**: Fixed HTTP client resource exhaustion risk by adding explicit timeouts to all service-to-service calls

### To Do (Pending Implementation)
- **P2**: Implement N+1/memory optimization in Duplicates service - refactor to batch processing with cursor-based pagination
- **P2**: Add audit logging for configuration file writes (appsettings.json modifications)
- **P2**: Integrate `RateLimitingMiddleware` into all service pipelines
- **P3**: Enhance symlink traversal detection in `PathSafety.cs` with .NET 9+ symlink APIs
- **P3**: Replace reflection-based EF Core method calls with public APIs where available

### Added
- `WorkflowProcessingOptions` configuration class for `StorageLens.Services.Orchestrator` to control concurrent scan workflow processing via `MaxConcurrentWorkflows` setting (defaults to `Environment.ProcessorCount`; `appsettings.json` default: 4).
- `WorkflowBackgroundService` updated to process workflows concurrently using a `SemaphoreSlim`, with graceful draining on shutdown.
- Two new unit tests in `WorkflowBackgroundServiceTests` covering concurrent dispatch and cancellation behaviour.

### Changed
- Frontend script organization improved: profile initialization moved from `site.js` to dedicated `profile.js` module, keeping page-specific initialization concerns separate from global initialization.

## [0.5.0] - 2026-03-06

### Changed
- `AnalyticsService.GetDashboardAsync`: replaced `.Result` property accesses after `await Task.WhenAll` with idiomatic `await` calls so that individual task exceptions propagate correctly.
- `FileInventoryService.GetAllAsync`: raised the per-request page-size ceiling from 200 to 5,000 so that internal service calls requesting up to 2,000 records are no longer silently truncated.
- Orchestrator `Program.cs`: set `Timeout = Timeout.InfiniteTimeSpan` on the `scanner` and `hashing` HTTP clients to prevent the 100-second default HttpClient timeout from aborting long-running scan and hash operations.

### Fixed
- `DuplicatesService.RecalculateAsync`: removed spurious `&` from the FileInventory query string (`?&page=1` → `?page=1`).

### Removed
- Leftover ASP.NET scaffolding files `WeatherForecastController.cs` and `WeatherForecast.cs` deleted from `StorageLens.Services.Locations`.


### Added
- Dynamic scanner path resolver supporting:
  - configurable prefix and regex rules
  - automatic Windows drive translation (`C:\...` -> `/host/drives/c/...`)
  - automatic UNC translation (`\\server\share\...` -> `/host/unc/server/share/...`)
- Repository-wide version metadata via `Directory.Build.props`.
- `VERSION` file for quick release reference.
- This changelog for version history tracking.

### Changed
- Scanner path-mapping config model upgraded from static `From/To` pairs to dynamic rule-based mappings.
- Docker compose mounts generalized to support dynamic local-user paths and UNC path resolution.

## [0.3.0] - 2026-03-06

### Added
- End-to-end scan workflow stabilization and documentation package.
- CI docs guard workflow for documentation update enforcement.
