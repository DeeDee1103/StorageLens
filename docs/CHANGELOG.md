# Changelog

All notable changes to StorageLens are documented in this file.

The format is inspired by Keep a Changelog, and this project follows Semantic Versioning principles.

## [Unreleased]

### Added
- Dashboard KPI cards now display contextual icons (storage, files, duplicates, savings) for faster at-a-glance recognition.
- Quick Actions panel buttons now include matching Bootstrap Icons for visual clarity.
- All data tables (Dashboard, Scan Jobs, File Inventory, Duplicates, Storage Locations) now show a friendly empty-state message with an icon when no records are present.
- Scan Jobs page now displays a percentage label next to each progress bar so exact progress is readable without hovering.
- Scan Jobs page now auto-refreshes every 10 seconds while any job is in an active state (Running, Scanning, Hashing, etc.) and shows a spinning "Auto-refreshing" badge.
- File Inventory pagination now disables the Previous button on page 1 and the Next button when the current page returns no results, preventing dead-end navigation.
- Settings page replaced with six placeholder cards (Authentication, Cloud Connectors, Notifications, Scheduled Scans, Retention Policies, Orchestration) giving a clear roadmap for future features.
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
