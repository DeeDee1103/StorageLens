# Changelog

All notable changes to StorageLens are documented in this file.

The format is inspired by Keep a Changelog, and this project follows Semantic Versioning principles.

## [Unreleased]

### Changed
- **Landing page is now the default home route** (`/`) and the dashboard remains available at `/Index`.
- **App navigation updated**: a new **Home** entry is shown above **Dashboard** in both desktop sidebar and mobile offcanvas nav, linking users back to the landing page.
- **Dashboard chart UX refresh**:
  - **Storage Growth Trend** upgraded with pseudo-3D rendering, animated load, on-point value labels, and click-to-open detail modal interactions.
  - **File Type Distribution** upgraded with pseudo-3D doughnut rendering, animated load, percentage labels, and click-to-open detail modal interactions.
  - **Largest Locations** upgraded with pseudo-3D bars, animated load, TB value labels, and click-to-open detail modal interactions.
- **Reports page chart UX refresh**:
  - **Most Common File Types** bar chart upgraded with pseudo-3D rendering, animated load, numeric labels, and click-to-open detail modal interactions.
  - **Duplicate-Heavy Locations** panel redesigned from a plain list to a ranked visual impact view with rank badges, share percentages, and proportional impact bars.

### Added
- **`StorageLens.Shared.Infrastructure.Tests` project** — new xUnit test project covering the HTTP client policy pipeline behaviour:
  - `HttpClientPolicyPipelineIntegrationTests` with three integration tests verifying that `CorrelationIdDelegatingHandler` is correctly resolved from DI and properly adds the `X-Correlation-Id` header to outgoing requests, does not overwrite an already-present header, and generates a unique ID per request.
- **`CorrelationIdDelegatingHandler`** class added to `StorageLens.Shared.Infrastructure` — a `DelegatingHandler` that automatically attaches a `X-Correlation-Id` header to every outgoing HTTP request when the header is not already present, enabling end-to-end correlation tracing across services.
- **Chart details modal patterns** on Dashboard and Reports pages for chart click-drill interactions.
- **Interaction hints** on chart headers (e.g., "Click bars for details", "Click points for details") to make interactivity discoverable.
- **Alerts page** (`/Alerts`) with adjustable min/max threshold sliders, test recipient support, and automatic threshold-check email dispatch when usage reaches the max threshold.
- **SMTP alert service** (`SmtpAlertEmailService`) with cooldown protection to avoid repeated email sends on page refresh.
- **`AlertsModelTests`** unit test validating that threshold crossing triggers an email dispatch call.

### Fixed
- **`CorrelationIdDelegatingHandler` DI registration** — added `services.AddSingleton<CorrelationIdDelegatingHandler>()` in `HttpClientPolicyPipelineIntegrationTests` so the handler is resolvable when `AddHttpMessageHandler<T>()` activates it through the DI container. Previously tests threw `System.InvalidOperationException: No service for type 'StorageLens.Shared.Infrastructure.CorrelationIdDelegatingHandler' has been registered.`
- **Service-unavailable error handling** — all API client methods (`LocationsApiClient`, `ScanJobsApiClient`, `FileInventoryApiClient`, `DuplicatesApiClient`, `AnalyticsApiClient`, `OrchestratorApiClient`) now catch `HttpRequestException` and log a warning instead of propagating the exception to the error page. Pages that fail to load backend data now display a clear yellow alert banner rather than crashing.
- **`DuplicatesService` build error** — `GroupBy` result was projected to `List<FileRecordDto>` via `.Select(g => g.OrderByDescending(...).ToList())`, causing the group key to be lost. The select now projects to a named tuple `(Key, Files)` so `HashValue` and the final `duplicateHashValues` collection are correctly populated.

### Added
- **`StorageLens.Web.Tests` project** — new xUnit test project covering page model behaviour for `IndexModel`, `StorageLocationsModel`, `ScanJobsModel`, and `DuplicatesModel`:
  - Happy-path tests verifying that data returned by API clients is correctly exposed via page model properties.
  - Service-unavailable tests verifying that `ServiceUnavailable = true` is set when a downstream service throws `HttpRequestException`, ensuring no unhandled exception propagates to the user.
  - Validation tests verifying that required-field errors are raised before any API call is made.
- **`ServiceUnavailable` property** added to all key page models (`IndexModel`, `StorageLocationsModel`, `ScanJobsModel`, `FileInventoryModel`, `DuplicatesModel`, `ReportsModel`). When set, each page renders a Bootstrap warning alert banner informing the user that a backend service is temporarily unavailable.
- **Form-submit loading state** (`site.js`) — POST forms now disable their submit button and display a `spinner-border` spinner with a "Working…" label on click, preventing double-submits and giving clear feedback during server round-trips.
- **Client Management page** (`/ClientManagement`) — owner-only interface for adding and managing client companies and granting them service access:
  - Protected by the same `sl-owner-mode` HttpOnly cookie used by Pricing Admin; enter the owner PIN to unlock.
  - Summary KPI cards: Total Clients, Active Clients, New This Month.
  - Client list table with: Account Number, Company, Primary Contact, Email, Tier, Status, Created date, and Edit/Delete actions.
  - Add / Edit modal form with sections for:
    - **Company Information**: company name, industry, website.
    - **Primary Contact**: first name, last name, title, phone, email.
    - **Billing Address**: street, city, state, postal code, country.
    - **Service Access**: subscription tier selector (pulled from Pricing config), contract start/end dates, and per-service checkboxes for additional grants beyond the tier defaults.
    - **Notification Settings**: comma-separated recipient email addresses; toggles for scan-complete, scan-failed, duplicates-detected, and storage-threshold alerts; configurable threshold percentage.
    - **Account Status & Notes**: Active / Inactive / Suspended status selector and free-text notes field.
  - Client records are persisted to the `clients.Clients` SQL Server table using EF Core (same database server as back-end services, configured via `DatabaseConfig`).
  - Auto-generated account numbers in `SL-NNNNN` format.
  - Client Management card added to the Settings page (`/Settings`) under the owner-only section.
- **`ClientRecord` model** (`src/StorageLens.Web/Data/ClientRecord.cs`) — domain entity capturing all company, contact, billing, subscription, notification, and audit fields.
- **`ClientsDbContext`** (`src/StorageLens.Web/Data/ClientsDbContext.cs`) — EF Core DbContext scoped to the `clients` SQL schema; registered in `Program.cs` with startup table initialisation via `StartupDatabaseInitializer`.
- **`ClientsDatabaseConnectionBuilder`** (`src/StorageLens.Web/Services/ClientsDatabaseConnectionBuilder.cs`) — builds the SQL connection string for the Web project from the existing `DatabaseConfig` appsettings section; supports both LocalDB and Azure SQL providers.
- **`clients` SQL schema constant** added to `StorageLens.Shared.Infrastructure.SqlSchemas`.
- **`StorageLens.Web.csproj`** — added `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.InMemory` (v8.0.12) package references and a project reference to `StorageLens.Shared.Infrastructure`.
- **UI screenshots** (`docs/screenshots/README.md`) — full-page captures of all 17 application screens taken from a live running instance, with descriptions of each page's purpose and content.
- **Active subscription tier enforcement** — the application now tracks which pricing tier the current installation is subscribed to, and gates features accordingly:
  - `Pricing:ActiveTierId` configuration key added to `appsettings.json` (default: `"professional"`); leave blank to disable tier enforcement and unlock all features.
  - `ActiveTierId` property added to `PricingOptions` model.
  - `ITierAccessService` / `TierAccessService` (`Services/TierAccessService.cs`) — scoped service that resolves the active tier from configuration and exposes `IsServiceAvailable(string serviceId)` for tier-based feature gating.
  - `TierAccessService` registered in `Program.cs`.
- **Duplicate Detection tier gate** (`/Duplicates`) — shows an upgrade banner with a link to `/Pricing` when the active tier does not include the `duplicate-detection` service (requires Professional plan or higher). The API is not called when the feature is unavailable.
- **Notification Alerts tier gate** (`/NotificationSettings`) — shows an upgrade banner when the active tier does not include `notification-alerts` (requires Professional plan or higher). Settings form is hidden until the plan is upgraded.
- **Subscription Plan card** on the Profile page (`/Profile`) — displays the active tier name, description, highlight tagline, and monthly price (or "Custom" for Enterprise). Includes a "View Plans" link to `/Pricing` and a "Choose a Plan" CTA when no tier is configured.
- **Active Tier management** in Pricing Admin (`/PricingAdmin`) — new "Active Subscription Tier" section at the top of the owner-authenticated view:
  - Dropdown selector showing all configured tiers with their prices.
  - "Save Active Tier" button persists the selection to the target `appsettings.{Environment}.json`.
  - Visual summary of services included in the currently selected tier.

- **Pricing page screenshot** added to `docs/screenshots/pricing-page.png` — full-page capture of the `/Pricing` route showing all four subscription tiers (Starter, Professional, Business, Enterprise) and the Service Details reference panel.
- **About page** (`/About`) — professional About Us page for StorageLens:
  - Hero banner with product logo, description, version badge, and capability tags.
  - KPI highlight cards: storage locations, files supported, scan engine, microservices count.
  - Mission statement and Core Values sections.
  - Key Features grid covering Dashboard, Scan Jobs, Duplicate Detection, File Inventory, Reports, and Configuration.
  - Technology Stack panel (backend, frontend, database, deployment).
  - Support & Contact section and Release Notes panel.
  - **About** nav link added to sidebar navigation (desktop and mobile offcanvas).
  - About page CSS styles added to `site.css` (hero, version box, badges, feature cards, stack items, contact list, release list, dark-mode variants).
- **Pricing page** (`/Pricing`) — a public-facing pricing catalogue showing four configurable subscription tiers:
  - **Starter** ($29/month): File Inventory, Scan Jobs, Reports & Analytics, Email Support.
  - **Professional** ($79/month, *Most Popular*): Starter + Duplicate Detection, Notification Alerts.
  - **Business** ($149/month): Professional + API Access, Scheduled Scans, Retention Policies, Priority Support.
  - **Enterprise** (Contact Us): All services including AI Insights and Custom Integrations.
  - Each tier card shows its monthly price (or "Custom" for Contact Us tiers), a highlight tagline, and a full feature checklist indicating which services are included or excluded.
  - A *Service Details* reference panel beneath the cards explains every service and its individual add-on price.
- **Pricing Admin page** (`/PricingAdmin`) — owner-only configuration interface for managing pricing:
  - Protected by a PIN stored in `appsettings.json` (`Pricing:OwnerPin`); the correct PIN sets an HttpOnly `sl-owner-mode` cookie that grants the owner session without requiring server-side state.
  - Tier editor: name, description, highlight tagline, Most-Popular / Contact-Us flags, calculated vs. manual price toggle, and a per-tier service checklist.
  - Service editor: edit any existing service's name, description, Bootstrap-Icons icon class, and monthly price via an inline modal.
  - Add-Service form: creates a new service (ID, name, description, icon, price) that immediately appears in all tier checklists — designed to accommodate future service additions without code changes.
  - All changes are persisted to the target `appsettings.{Environment}.json` file, consistent with other settings pages.
  - Lock button ends the owner session by clearing the `sl-owner-mode` cookie.
- **Pricing Management card** added to the Settings hub (`/Settings`), linking directly to `/PricingAdmin` with an "Owner only" badge.
- **Pricing link** added to the sidebar navigation (desktop and offcanvas mobile) between Reports and Settings.
- `PricingOptions`, `PricingTier`, and `PricingService` classes (`Services/PricingOptions.cs`) — strongly-typed model for the `Pricing` configuration section.
- `Pricing` configuration section added to `appsettings.json` with four default tiers and twelve configurable services (File Inventory, Scan Jobs, Reports & Analytics, Email Support, Duplicate Detection, Notification Alerts, API Access, Scheduled Scans, Retention Policies, Priority Support, AI Insights, Custom Integrations).
- Pricing card CSS styles added to `site.css`: responsive card grid, popular-badge, price typography, feature list, and dark-mode adjustments.

### Changed
- **Footer** — removed "MVP" label; footer now reads "StorageLens | Built on .NET 8 microservices".
- **API client constructors** — all six HTTP API clients now accept `ILogger<T>` alongside `HttpClient`. The ILogger is automatically injected by the existing `AddHttpClient<T>` registrations and is used to log warnings when downstream services are unreachable.
- `_Layout.cshtml` — both the desktop sidebar nav and the offcanvas mobile nav now include a Pricing link (`/Pricing`, `bi-tags` icon).


  - `ScannerExecutionService`: renamed `info`→`fileInfo`, `errors`→`errorCount`, `batch`→`fileBatch`, `current`→`currentDirectory`, `files`→`filePaths`, `directories`→`subdirectories`, `progress`→`progressPercent`.
  - `ScannerPathResolver`: renamed `current`→`currentOptions`, `fromRule`→`ruleResolvedPath`, `driveMapped`→`windowsDriveMappedPath`, `uncMapped`→`uncMappedPath`, `normalizedInput`→`normalizedInputPath`, `mode`→`mappingMode`, `replaced`→`regexReplacedPath`, `normalizedFrom`→`normalizedFromPattern`, `suffix`→`pathSuffix`, `mapped`→`resolvedReplacementPath`, and `rest`→`remainingPath` in both auto-map helpers.
  - `HashingService`: renamed `sha`→`sha256` and `pending`→`pendingFiles`.
  - `DuplicatesService`: renamed `candidates`→`filesWithCompletedHashes`, `stale`→`staleGroups`, `bucket`→`duplicateBucket`, `ordered`→`orderedGroupFiles`, `largest`→`largestFile`, and `hashes`→`duplicateHashValues`.
  - `FileInventoryService`: renamed `paths`→`filePaths`, `changed`→`modifiedCount`, and `duplicate`→`isDuplicate`.
  - `StartupDatabaseInitializer`: renamed `createTablesAsync`→`createTablesMethod`, `task`→`createTablesTask`, `current`→`currentException` (both exception-traversal helpers), and `number`→`sqlErrorNumber`.
  - `ScannerSettingsModel.FormatBytes`: renamed single-letter `val`→`sizeValue` and `i`→`unitIndex`; renamed `s`→`settingsSection` in `LoadCurrentSettings`.
  - `site.js`: renamed `openLink`→`pathAnchor` in `openStoragePath`.

### Added
- **Environment Settings page** (`/EnvironmentSettings`) — lets engineers switch the active edit environment between **Development** and **Production**:
  - Displays the current runtime environment (from `ASPNETCORE_ENVIRONMENT`) with a badge.
  - Two-button switcher persists the chosen edit target in a server-side cookie (`sl-edit-env`).
  - Contextual warning banner appears when Production edit mode is active.
  - Reference table explaining the ASP.NET Core configuration-file hierarchy (base → Development / Production → User Secrets → Environment Variables).
- **Environment switcher banner** on the Settings hub page — shows the current runtime environment and active edit environment at a glance, with a direct link to the Environment Settings page.
- **Environment indicator** on Database Configuration, Notifications, and Scanner & Performance settings pages — a small badge displays which environment's file will be written, with a quick "Switch" link.
- `appsettings.Production.json` — production configuration scaffold with Azure SQL defaults, higher connection-pool limits, production-appropriate service endpoints, and reduced logging verbosity.

### Changed
- Database Configuration, Notification Settings, and Scanner & Performance **Save** buttons now indicate the target environment (blue for Development, red for Production) and include the environment name in the button label.
- `MergeIntoDevSettings` helper renamed to `MergeIntoTargetSettings` in all three settings page models; it now derives the target file name dynamically from the active edit environment cookie instead of always writing to `appsettings.Development.json`.
- `SettingsModel` now accepts `IWebHostEnvironment` to expose the runtime environment and selected edit environment to the Settings hub view.


- **Appearance & Display settings page** (`/AppearanceSettings`) — fully functional browser-side preference manager:
  - Visual theme picker cards (Light, Dark, System default) with miniature UI previews; applies the theme immediately and persists to `localStorage`.
  - Default landing page dropdown (Dashboard, Storage Locations, Scan Jobs, File Inventory, Duplicates, Reports).
  - Sidebar collapse-by-default toggle.
  - Tables & Data Display section: rows per page (10/25/50/100), Scan Jobs auto-refresh interval (disabled/5 s/10 s/30 s/60 s), dashboard date range (7/14/30 days).
  - Animations & Accessibility section: KPI counter animation, reduce motion, compact mode toggles.
  - Reset to defaults button clears all `localStorage` preference keys and re-applies system theme.
  - Live "Current stored preferences" display showing all active `localStorage` keys.
- **Notification Settings page** (`/NotificationSettings`) — configures alerting channels and saves to `appsettings.Development.json`:
  - Email / SMTP section: host, port, SSL/TLS toggle, username, read-only password field with `dotnet user-secrets` reminder, from address, from name, comma-separated recipient list.
  - Webhook section: URL, HMAC-SHA256 signing secret (sent as `X-StorageLens-Signature` header).
  - Notification Events section: scan completed, scan failed, duplicates detected, storage threshold exceeded (with configurable % threshold).
  - Show/hide collapsible panels for email and webhook fields based on their enable toggles.
  - In development mode: **Save** button persists all settings to `appsettings.Development.json`.
- **Scanner & Performance settings page** (`/ScannerSettings`) — generates per-service configuration snippets:
  - Hashing Worker section: Max Degree of Parallelism (1–64), Batch Size (10–10,000), Max File Size to Hash dropdown (512 MB / 1 GB / 2 GB / 4 GB / 10 GB / No limit).
  - Live-updating JSON snippet for `StorageLens.Services.Hashing` `appsettings.Development.json` with copy-to-clipboard button.
  - Scanner Path Mappings section: Enable auto-mapping toggle, Auto-map in container only toggle.
  - Example custom path mappings reference table (Prefix and Regex examples for Docker/UNC mounts).
  - Live-updating JSON snippet for `StorageLens.Services.Scanner` `appsettings.Development.json` with copy-to-clipboard button.
  - Performance Tuning Guide table with recommended values for developer laptops and Azure VMs (4/8+ vCPU).
  - In development mode: **Save** button persists settings to the web app's `appsettings.Development.json`.
- `AppearanceOptions`, `NotificationOptions`, `ScannerPerformanceOptions` — three new strongly-typed options classes registered via DI.
- `appsettings.json` — `Appearance`, `Notifications`, and `ScannerPerformance` configuration sections with production defaults.
- All `MergeIntoDevSettings` file writes now use an atomic write pattern (write to `.tmp` then `File.Move` with overwrite) to prevent corrupt JSON if the process is interrupted mid-write.

### Changed
- `Settings.cshtml`: Reorganised into two rows — four active **Configure** cards (Database Configuration, Appearance & Display, Notifications, Scanner & Performance) followed by five **Coming soon** placeholders (Authentication, Cloud Connectors, Scheduled Scans, Retention Policies, AI Features). Removed the "Orchestration" placeholder (superseded by Scanner & Performance).

  - Database provider toggle: **Local SQL Server (LocalDB)** or **Azure SQL Database**.
  - LocalDB fields: instance name and database name.
  - Azure SQL fields: server name, database name, user ID, connection timeout, Encrypt, Trust Server Certificate, plus a read-only password field with a reminder to use `dotnet user-secrets`.
  - Advanced connection options (expandable): Min/Max Pool Size and Application Name.
  - Live-updating **Generated Connection String Preview** with a copy-to-clipboard button; the preview updates as form fields are typed.
  - **Per-Service Connection Strings** panel listing all seven backend services (Locations, ScanJobs, FileInventory, Duplicates, Scanner, Hashing, Orchestrator) with individual copy buttons and a **Download appsettings snippet (JSON)** button.
  - **Service Endpoints** form to configure the base URLs used by the web front-end to reach each backend microservice.
  - In development mode: **Save** buttons persist database config and service endpoints to `appsettings.Development.json`.
  - In non-development environments: read-only display with an informational banner.
  - In-memory database toggle (per-service, dev only) with a clear label.
- `DatabaseConfigOptions` class (`Services/DatabaseConfigOptions.cs`) — strongly-typed options class bound from the `DatabaseConfig` configuration section, covering all provider, LocalDB, Azure SQL, and advanced pooling settings.
- `appsettings.json` — new `DatabaseConfig` section with LocalDB defaults.
- `appsettings.Development.json` — new `DatabaseConfig` section with Azure SQL template values for local Azure development.

### Changed
- `Settings.cshtml`: Added a functional **Database Configuration** card at the top of the Settings page, linked to the new `/Configuration` page, visually distinguished from the placeholder "Coming soon" cards below it.

- `AnalyticsService.GetDashboardAsync`: Total Files KPI now always uses `locations.Sum(x => x.FileCount)` (accurate total from location metadata) instead of the API-capped `files.Count` value which was limited to the first 2,000 file records.
- `AnalyticsService.GetDashboardAsync`: Storage Growth Trend chart now correctly plots storage in TB (sum of `TotalSizeBytes` for distinct locations scanned each day, divided to TB) instead of the raw `FilesScanned` count that was previously used.
- Dashboard `Index.cshtml`: Chart.js text colour (`Chart.defaults.color`) is now set to the muted dark-mode palette (`#9eb0cd`) when dark mode is active, so axis labels and tick marks remain readable against the dark card backgrounds.
- Reports `Reports.cshtml`: Same Chart.js dark-mode colour fix applied to the Most Common File Types chart.

### Fixed
- Dark-mode flash on page load: an inline `<script>` immediately after `<body>` now reads `localStorage` and applies the `dark-mode` class before the first browser paint, eliminating the white-flash-then-dark flicker.
- CSS `body.preload` / `body.preload *` rule added to suppress all CSS transitions during the initial page load so the theme is applied instantaneously; transitions are re-enabled once the `load` event fires.
- `Index.cshtml`: removed a stray `</div>` closing tag that appeared after the `@section Scripts {}` block; this orphaned tag could cause the browser HTML parser to close an ancestor `<div>` prematurely and break the page layout.
### Fixed
- `docker-compose.yml`: removed Windows-only volume mounts (`C:/` and UNC network share) from the `scanner` and `hashing` services so the stack starts on Linux and macOS hosts without modification.
- `docker-compose.yml`: removed obsolete top-level `version` attribute that produced deprecation warnings with Docker Compose v2.
- `StartupDatabaseInitializer`: expanded startup retry logic to treat SQL Server pre-login handshake failures (`SocketException`, network `IOException`) as retryable errors, preventing services from crashing when SQL Server is still initialising inside the Docker Compose stack.

### Added
- **User Profile page** (`/Profile`) — a full-featured profile dashboard including:
  - Profile picture with automatic Microsoft Graph SSO avatar support; falls back to initials when the Graph endpoint is unavailable.
  - Custom photo upload (max 2 MB) with client-side preview and localStorage persistence.
  - Editable profile fields: display name, email, job title, department, and location, managed via an Edit Profile modal.
  - Activity stats panel showing scans run, duplicates found, storage locations, and days active.
  - Preferences panel: theme selection (Light / Dark / System), default page view, and per-event notification toggles — saved to localStorage.
  - Security panel: last sign-in timestamp, Microsoft SSO authentication status, 2FA status, Sign Out All Sessions action, and one-click profile data export.
  - Recent Activity feed showing the latest user-initiated events (scans, duplicates, locations, reports).
- Sidebar profile footer showing the user's avatar, display name, and Admin role badge with a direct link to the Profile page.
- Top-bar avatar button now opens a Bootstrap dropdown showing the user's name, email, and quick links to Profile and Settings.
- `profile.js` — profile data persistence layer using localStorage for offline-first profile management across all pages.

### Changed
- `site.js` — calls `slProfileInit()` on every page load so the sidebar and top-bar avatar/name always reflect the current profile state.
- `_Layout.cshtml` — offcanvas mobile navigation now includes a Profile link for parity with the desktop sidebar.
- `site.css` — added sidebar flex-column layout so the profile footer always sits at the bottom of the sidebar.
- Quick Actions panel buttons now include matching Bootstrap Icons for visual clarity.
- All data tables (Dashboard, Scan Jobs, File Inventory, Duplicates, Storage Locations) now show a friendly empty-state message with an icon when no records are present.
- Scan Jobs page now displays a percentage label next to each progress bar so exact progress is readable without hovering.
- Scan Jobs page now auto-refreshes every 10 seconds while any job is in an active state (Running, Scanning, Hashing, etc.) and shows a spinning "Auto-refreshing" badge.
- File Inventory pagination now disables the Previous button on page 1 and the Next button when the current page returns no results, preventing dead-end navigation.
- Settings page replaced with six placeholder cards (Authentication, Cloud Connectors, Notifications, Scheduled Scans, Retention Policies, Orchestration) giving a clear roadmap for future features.
- Settings page now includes an **AI Features** card (Coming Soon) describing planned machine-learning capabilities: anomaly detection, predictive capacity planning, and AI-driven duplicate classification.
- Global search box now supports the Ctrl+K / Cmd+K keyboard shortcut to focus it from anywhere on the page, with a visible `Ctrl K` hint badge in the search bar.
- Mobile offcanvas navigation now correctly highlights the active page link, matching the behaviour of the desktop sidebar.

### Changed
- Sidebar navigation links now animate smoothly on hover/active via a CSS transition.
- Dark mode toggle now transitions body background, card, and border colours smoothly instead of switching instantly.
- `--sl-transition` CSS custom property introduced to apply consistent timing across the layout for dark/light mode switching.
- Focus-visible outlines added for keyboard-navigable controls (links, buttons, inputs, selects) to meet basic WCAG 2.1 AA focus-indicator requirements.
- `AnalyticsService.GetDashboardAsync`: replaced `.Result` property accesses after `await Task.WhenAll` with idiomatic `await` calls so that individual task exceptions propagate as the original exception type rather than `AggregateException`.
- `FileInventoryService.GetAllAsync`: raised the per-request page-size ceiling from 200 to 5,000 so that internal service calls (Analytics, Duplicates) which request up to 2,000 records per page are no longer silently truncated to 200.
- Orchestrator `Program.cs`: set `Timeout = Timeout.InfiniteTimeSpan` on the `scanner` and `hashing` HTTP clients so that long-running scan and hash operations are not aborted by the default 100-second HttpClient timeout.

### Fixed
- `DuplicatesService.RecalculateAsync`: removed spurious `&` from the FileInventory query string (`?&page=1` → `?page=1`) that produced a malformed URL.

### Removed
- Deleted leftover ASP.NET scaffolding files `WeatherForecastController.cs` and `WeatherForecast.cs` from `StorageLens.Services.Locations`.

### Added
- Improved readability for the Storage Locations "Start Scan" action with a blue background and black text.
- Added `docs/technical/technology-stack.md` for a centralized, service-by-service technology inventory.
- Added a global dark mode toggle in the web header that switches all pages and persists user preference.
- Storage location paths are now clickable and open as `file://` links for both local Windows paths and UNC shares.
- Storage Locations now displays `Total Used` and `% Space Remaining` columns; remaining space is calculated from the underlying drive/share when available.
- Storage Locations now includes a Copy Path icon button next to each path with clipboard support and visual confirmation.
- Added sidebar logo branding integration (logo asset, sidebar brand header, and favicon) with responsive sizing.

### Changed
- Documentation index now links to the changelog and technology stack reference.
- Documentation maintenance policy now explicitly requires updating `docs/CHANGELOG.md` for user-visible and operational changes.
- Updated the Scan Jobs page Start Scan action to use the same high-contrast blue style as Storage Locations.
- Improved dark mode toggle discoverability on mobile by adding a visible "Dark Mode" label next to the icon.
- Improved Storage Location path links with a Windows-focused open handler that tries multiple UNC/local path URL formats on click.
- Replaced generated logo assets with the provided official StorageLens logo image.
- Updated sidebar branding to transparent styling and changed label text to "Storage Intelligence".
- Cleaned up Storage Locations table grid spacing and updated Start Scan action to an icon button so Start Scan/Delete remain aligned on one row.
- Replaced logo assets again with the newer transparent StorageLens image provided by user.
- Updated browser tab favicon to use the transparent icon asset with cache-busting versioning.
- Updated browser tab title branding text from "StorageLens" to "Storage Lens".
- Restored explicit `Total Used` column on Storage Locations and kept `Total Size` as capacity when available.
- Added `Job ID` column to Scan Jobs table (compact display with full GUID tooltip).
- Scanner UNC handling improved: built-in auto path remapping now defaults to container-only, and scans with 0 files plus access errors now fail explicitly instead of silently appearing successful.

## [0.1.0] - 2026-03-06

### Added
- Initial StorageLens multi-service documentation package under `docs/` (architecture, diagrams, technical, operations, developer, user-guide, and product sections).

[Unreleased]: https://github.com/DeeDee1103/StorageLens/compare/main...develop
[0.1.0]: https://github.com/DeeDee1103/StorageLens/releases/tag/v0.1.0
