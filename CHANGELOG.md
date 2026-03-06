# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and this project uses semantic versioning.

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
