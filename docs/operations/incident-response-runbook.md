# Incident Response Runbook

## Purpose

This runbook defines the standard incident response workflow for StorageLens service degradation, outages, and data-processing failures.

## Severity Levels

- Sev 1: Production unavailable, data loss risk, or active security event.
- Sev 2: Core workflow degraded (scan/orchestration failures) with workaround unavailable.
- Sev 3: Partial degradation with workaround available.
- Sev 4: Minor issue, low user impact.

## Roles

- Incident Commander (IC): Owns coordination and status decisions.
- Communications Lead: Publishes internal/external updates.
- Technical Lead: Owns diagnosis and remediation.
- Scribe: Maintains timeline and action log.

## First 15 Minutes Checklist

1. Declare incident and assign IC.
2. Capture impact scope: affected service(s), regions, user groups.
3. Freeze non-essential deployments.
4. Validate health endpoints for all services.
5. Check recent changes: deployments, config edits, infrastructure updates.
6. Start incident timeline in ticket/bridge notes.

## Triage Workflow

1. Confirm whether issue is service, data, dependency, or infrastructure related.
2. Validate database health and connection saturation.
3. Validate message broker health and queue depth (if used).
4. Validate orchestrator workflow error rates and stuck states.
5. Validate downstream call failures and timeout/circuit-breaker activity.

## Mitigation Patterns

- Roll back latest deployment if error spike correlates with release.
- Scale impacted service out when saturation is confirmed.
- Disable non-critical scheduled workload to reduce pressure.
- Pause and resume workflow processing after stabilization.
- Switch to documented fallback mode for dependent services.

## Recovery Validation

1. Error rate back to baseline.
2. Health checks green for all critical services.
3. Critical user flows verified:
   - Start scan workflow
   - Hashing execution
   - Duplicate recalculation
   - Analytics refresh
4. Backlog drains to expected level.

## Communication Cadence

- Sev 1: Every 15 minutes.
- Sev 2: Every 30 minutes.
- Sev 3-4: Hourly or at milestone updates.

## Post-Incident Review (within 5 business days)

- Incident summary and root cause.
- What worked and what failed.
- Corrective actions with owners and deadlines.
- Documentation updates (runbooks, deployment guide, monitoring guide).
- Add entry to docs/CHANGELOG.md when behavior/process changes.
