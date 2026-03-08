# Developer Guide

## Audience

Engineers implementing features across UI, service APIs, workflow stages, and shared contracts.

## Codebase Layout

- `src/StorageLens.Web`
- `src/StorageLens.Services.*`
- `src/StorageLens.Shared.Contracts`
- `src/StorageLens.Shared.Infrastructure`
- `tests/`

## Development Workflow

1. Pull latest `develop`
2. Build solution
3. Run service subset or full compose stack
4. Implement changes with tests
5. Update docs for architecture/API/contract changes

## Engineering Practices

- Keep service boundaries explicit
- Prefer additive contract changes
- Preserve correlation ID propagation
- Include health and operational considerations in feature design

## Frontend Script Organization

### Initialization Pattern

Frontend initialization is organized by feature/module:

- **`site.js`** - Common/global page initialization (theme, search, animations)
- **`profile.js`** - Profile page specific initialization and avatar management
- **Page-specific scripts** - Loaded via `@section Scripts { <script> }` in individual `.cshtml.cs` pages

Each script module is responsible for its own DOM initialization. Do not mix initialization concerns in `site.js`; keep page-specific or feature-specific logic in dedicated modules.
