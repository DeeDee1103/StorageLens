# Issue Management and Release Process

## Purpose

Improve project health visibility by standardizing issue intake, prioritization, and release planning.

## Issue Intake

Use GitHub issue templates:

- Bug report: `.github/ISSUE_TEMPLATE/bug_report.yml`
- Feature request: `.github/ISSUE_TEMPLATE/feature_request.yml`

Issue requirements:

- Reproducible summary and impact
- Affected services/contracts/workflows
- Testing and documentation impact

## Labels

Recommended baseline labels:

- `triage-needed`
- `bug`
- `enhancement`
- `dependencies`
- `security`
- `good first issue`
- `help wanted`
- `priority:p1`
- `priority:p2`
- `priority:p3`

## Triage Process

1. New issues start as `triage-needed`.
2. Assign priority (`p1/p2/p3`) and owner.
3. Tag architecture-impacting issues for architecture review.
4. Split oversized issues into smaller deliverables.

## Release Planning

- Planning cadence: every sprint
- Release branch target: `main` via `develop`
- Required inputs:
  - passing quality gates
  - changelog updates
  - docs updates for impacted areas
  - ADR/reference updates for major design changes

## Release Notes Checklist

- User-visible features and fixes summarized
- Breaking changes called out with migration notes
- Operational changes highlighted
- Security/dependency updates summarized

## New Contributor Path

Tag starter-friendly tasks with:

- `good first issue`
- `help wanted`

Link newcomers to onboarding docs:

- `docs/developer/onboarding-checklist.md`
- `docs/developer/developer-guide.md`
