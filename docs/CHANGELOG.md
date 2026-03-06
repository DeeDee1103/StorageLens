# Changelog

All notable changes to StorageLens are documented in this file.

The format is inspired by Keep a Changelog, and this project follows Semantic Versioning principles.

## [Unreleased]

### Added
- Storage Locations page now supports deleting a location from the UI using a trash action button.
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

## [0.1.0] - 2026-03-06

### Added
- Initial StorageLens multi-service documentation package under `docs/` (architecture, diagrams, technical, operations, developer, user-guide, and product sections).

[Unreleased]: https://github.com/DeeDee1103/StorageLens/compare/main...develop
[0.1.0]: https://github.com/DeeDee1103/StorageLens/releases/tag/v0.1.0
