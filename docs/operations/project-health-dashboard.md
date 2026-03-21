# Project Health Dashboard

## Purpose

Provide a single operational view of release readiness, reliability posture, quality gates, and active technical debt.

## Update Cadence

- Weekly: quality gate and dependency status
- Monthly: SLI/SLO status, risk register, and debt trend
- Per release: release readiness and sign-off results

## Health Summary

| Area | Current Status | Trend | Last Updated | Owner |
|---|---|---|---|---|
| Build/Test/Coverage Gates | Green / Amber / Red | Up / Flat / Down | YYYY-MM-DD | DevEx/QA |
| Code Quality Checks | Green / Amber / Red | Up / Flat / Down | YYYY-MM-DD | DevEx |
| Dependency Security (Dependabot) | Green / Amber / Red | Up / Flat / Down | YYYY-MM-DD | Security/Platform |
| SLI/SLO Compliance | Green / Amber / Red | Up / Flat / Down | YYYY-MM-DD | SRE/Operations |
| Documentation Freshness | Green / Amber / Red | Up / Flat / Down | YYYY-MM-DD | Docs Owners |
| Technical Debt Burn-down | Green / Amber / Red | Up / Flat / Down | YYYY-MM-DD | Engineering Leads |

## Quality Gates

| Gate | Required Check | Current Result | Last Run | Notes |
|---|---|---|---|---|
| Documentation Guard | Require docs for code changes | Pass / Fail | YYYY-MM-DD | |
| Test and Coverage | Build Test Coverage | Pass / Fail | YYYY-MM-DD | Coverage >= 80% line rate required |
| Code Quality | Code Quality Checks | Pass / Fail | YYYY-MM-DD | Format + vulnerable package checks + build |

## Reliability (SLI/SLO)

Reference targets: `docs/operations/sli-slo-targets.md`

| SLI | Target (SLO) | Current | Status | Action Needed |
|---|---|---|---|---|
| API Availability | >= 99.5% monthly | TBD | Green / Amber / Red | |
| API p95 Latency (read) | <= 500 ms | TBD | Green / Amber / Red | |
| API p95 Latency (write) | <= 1000 ms | TBD | Green / Amber / Red | |
| Workflow Success Rate | >= 98% monthly | TBD | Green / Amber / Red | |
| Workflow Retry Recovery Success | >= 95% | TBD | Green / Amber / Red | |

## Technical Debt Register (Active)

| Item | Priority | Status | Owner | Target Sprint/Date | Reference |
|---|---|---|---|---|---|
| Duplicates N+1 / memory optimization | P2 | In Progress | Backend | 2026-03 sprint close | docs/CHANGELOG.md |
| Configuration audit logging | P2 | Done | Security/Backend | 2026-03-20 | docs/CHANGELOG.md |
| Rate limiting integration completion | P2 | Done | Security | 2026-03-20 | docs/CHANGELOG.md |
| Symlink traversal hardening | P3 | Done | Security | 2026-03-20 | docs/CHANGELOG.md |
| EF reflection refactor | P3 | Done | Backend | 2026-03-20 | docs/CHANGELOG.md |

## Release Readiness Snapshot

| Checklist Area | Status | Notes |
|---|---|---|
| CI checks required for merge | Complete / Incomplete | |
| Documentation and changelog updates | Complete / Incomplete | |
| Architecture review/ADR requirements | Complete / Incomplete | |
| Security and dependency checks | Complete / Incomplete | |
| SLI/SLO review completed | Complete / Incomplete | |

## Ownership and Review

- Primary owner: Platform/Operations lead
- Secondary owner: Engineering manager
- Inputs from: QA, Security, DevEx, Service leads

Update this dashboard in the same PR when quality gate definitions, SLO targets, or release policy changes.
