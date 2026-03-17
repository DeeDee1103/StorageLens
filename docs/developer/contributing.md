# Contributing

## Branching and PRs

- Branch from `develop`
- Keep PRs focused and reviewable
- Include tests and documentation updates

## Pull Request Checklist

- Solution builds and tests pass
- API or contract changes documented
- Operational impact documented (config, deploy, scale)
- Backward compatibility assessed
- `docs/CHANGELOG.md` updated for user-visible, API, or operational changes

## Documentation Update Requirement

Any change to services, contracts, workflows, or deployment behavior must update relevant files under `docs/` before merge.

Guard enforcement rules:
- If source/config/workflow/test files change, at least one file under `docs/` must also change.
- If source/config/workflow/test files change, `docs/CHANGELOG.md` must be updated in the same PR.

CI enforces this rule through:
- `.github/workflows/docs-guard.yml`
