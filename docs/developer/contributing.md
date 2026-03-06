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

## Documentation Update Requirement

Any change to services, contracts, workflows, or deployment behavior must update relevant files under `docs/` before merge.

CI enforces this rule through:
- `.github/workflows/docs-guard.yml`
