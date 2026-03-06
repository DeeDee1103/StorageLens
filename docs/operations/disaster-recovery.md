# Disaster Recovery

## Recovery Objectives

- Define target RPO and RTO per environment
- Prioritize scan job history and file inventory recovery

## Backup Strategy

- Scheduled SQL backups with retention policies
- Backup verification and periodic restore drills
- Store backups in geographically redundant storage

## Recovery Runbook

1. Restore infrastructure baseline
2. Restore SQL databases in service dependency order
3. Redeploy services from versioned images
4. Validate health endpoints and smoke workflow
5. Resume paused workflows where appropriate

## Continuity Recommendations

- Keep immutable deployment artifacts
- Automate environment recreation using infrastructure-as-code
- Maintain documented incident communication workflows
