# Documentation Ownership Matrix

## Purpose

This matrix defines who owns each documentation area, what changes trigger updates, and how often each area should be reviewed for accuracy.

## Ownership Model

- Primary Owner: accountable for correctness and completeness.
- Secondary Owner: backup reviewer and escalation contact.
- Update Trigger: code/process changes that require doc updates in the same PR.
- Review Cadence: periodic validation even when no major change occurred.

## Matrix

| Documentation Area | Primary Owner | Secondary Owner | Update Trigger | Review Cadence |
|---|---|---|---|---|
| docs/architecture/* | Architecture Reviewer / Platform Lead | Cross-Service Integrator | Service boundary changes, workflow redesign, new components | Quarterly |
| docs/technical/api-specification.md | Contract Steward / API Lead | Service Owner of changed endpoint | Endpoint, DTO, status code, or behavior changes | Every release |
| docs/technical/api-versioning-strategy.md | Contract Steward | Engineering Manager | Breaking change policy updates, new version rollout | Quarterly |
| docs/technical/configuration-guide.md | Backend Lead | DevOps Lead | New settings, renamed keys, default value changes | Every release |
| docs/operations/deployment-guide.md | DevOps Lead | Platform Engineer | Deployment workflow, infra, environment variable, or startup changes | Monthly |
| docs/operations/ci-cd-pipeline.md | DevOps Lead | QA Lead | Workflow/check updates, quality gate changes, release gate changes | Monthly |
| docs/operations/incident-response-runbook.md | Operations Lead | Security Lead | Incident process changes, role changes, new mitigation patterns | Quarterly |
| docs/operations/secrets-rotation-runbook.md | Security Lead | DevOps Lead | Secret scope/cadence/process changes, key management updates | Quarterly |
| docs/operations/monitoring-logging.md | SRE/Observability Owner | Service Leads | New telemetry, alerting thresholds, dashboard changes | Monthly |
| docs/developer/developer-guide.md | Developer Experience Lead | Engineering Manager | Workflow/process/gate changes for contributors | Monthly |
| docs/developer/contributing.md | Developer Experience Lead | Repository Maintainer | PR policy, required checks, merge rules, branch strategy changes | Monthly |
| docs/CHANGELOG.md | PR Author (required) | Reviewer / Maintainer | Any user-visible, operational, API, workflow, or guardrail-impacting change | Every PR |

## PR Review Enforcement

Use this matrix during PR review:

1. Identify changed code area and mapped documentation owner.
2. Confirm impacted docs are updated in the same PR.
3. Confirm docs/CHANGELOG.md contains a clear entry when required.
4. Request review from primary or secondary owner when high-impact docs are touched.

## Escalation Path

If ownership is unclear or unavailable:

1. Default to service owner and repository maintainer.
2. Mark PR with docs-owner-needed label.
3. Resolve owner assignment before merge.

## Maintenance Rule

When teams or ownership roles change, this matrix must be updated in the same PR as role/process updates.

## Docs Review Log

Use this table to record recurring documentation reviews. Add one row for each completed review cycle.

| Review Date | Cadence Type | Scope Reviewed | Reviewer | Findings Summary | Follow-Up PR/Issue |
|---|---|---|---|---|---|
| YYYY-MM-DD | Monthly | Developer + Operations docs | Name | No major drift / minor updates identified | Link |
| YYYY-MM-DD | Quarterly | Architecture + API versioning docs | Name | Versioning/process updates required | Link |

### Logging Rule

- Monthly reviews should include `docs/developer/*` and `docs/operations/*`.
- Quarterly reviews should include `docs/architecture/*`, `docs/technical/api-specification.md`, and `docs/technical/api-versioning-strategy.md`.
- If updates are required, open a PR in the same sprint and reference it in the log.
