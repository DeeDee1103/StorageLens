# CI/CD Pipeline

## Overview

StorageLens CI currently enforces two quality controls on pull requests and branch updates:

- Documentation Guard: requires docs updates and CHANGELOG alignment for source/config changes.
- Quality Gates: runs build, tests, and coverage threshold validation.

## Active Workflows

## Documentation Guard

- Workflow: `.github/workflows/docs-guard.yml`
- Check name: `Require docs for code changes`
- Trigger: pull request events
- Fails when:
  - Source/config/workflow files changed without docs changes
  - Source/config/workflow files changed without docs/CHANGELOG.md update

## Quality Gates

- Workflow: `.github/workflows/tests-and-coverage.yml`
- Workflow name: `quality-gates`
- Check name: `Build Test Coverage`
- Trigger: push and pull_request on `main` and `develop`
- Steps:
  1. Restore
  2. Build
  3. Test with `XPlat Code Coverage`
  4. Enforce minimum line coverage threshold (80%)
  5. Upload TestResults artifacts

## Infrastructure Validation

- Workflow: `.github/workflows/infra-validation.yml`
- Check name: `Validate Bicep`
- Trigger: infra and deployment manifest changes on `main` and `develop`
- Steps:
  1. Setup Azure CLI
  2. Compile `infra/main.bicep`
  3. Fail fast on template syntax/compile errors

## Branch Protection (when plan supports it)

Configure required status checks on `main` and `develop`:

- `Build Test Coverage`
- `Require docs for code changes`

Also enable:

- Require branches to be up to date before merging
- Require conversation resolution before merge

## Branch Protection Plan-Limit Fallback

When branch protection cannot be enforced due repository plan limits:

- Use `.github/CODEOWNERS` to enforce review visibility on infra, workflows, and shared-security paths.
- Keep CI checks green and treat failing checks as merge blockers in team process.
- Require PR checklist completion and documented reviewer sign-off before merge.

## Release Promotion Guidance

1. Merge to develop after passing quality checks.
2. Validate environment smoke tests.
3. Promote to main with release notes and changelog review.
4. Tag release version and publish release notes.

## Failure Triage

- Docs Guard failure: update impacted docs and docs/CHANGELOG.md.
- Coverage gate failure: add/fix tests or adjust implementation to meet threshold.
- Test failure: isolate failed project and run targeted `dotnet test` locally.
