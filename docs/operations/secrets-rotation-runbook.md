# Secrets Rotation Runbook

## Purpose

This runbook standardizes secret rotation for StorageLens across local development and production-like environments.

## Rotation Scope

- Service API keys (`X-API-Key` / `ServiceApiKey`)
- Database credentials
- SMTP credentials
- Webhook signing secrets
- Any externally issued integration tokens

## Rotation Frequency

- High-sensitivity secrets: every 90 days.
- Standard secrets: every 180 days.
- Immediate rotation after suspected exposure.

## Pre-Rotation Checklist

1. Identify all consumers of the secret.
2. Confirm dual-secret or staged rollout capability.
3. Prepare rollback secret values and owner contacts.
4. Schedule maintenance window if required.

## Rotation Procedure

1. Create new secret value in secret store.
2. Update consuming service configuration.
3. Deploy configuration change.
4. Validate authentication-dependent flows.
5. Remove old secret after confirmation window.

## Validation Checklist

- Service startup succeeds.
- Health endpoints are green.
- Authenticated API calls succeed between services.
- Alerting and notification paths still function.

## Emergency Rotation

Use emergency flow when exposure is suspected:

1. Revoke old secret immediately.
2. Generate and apply replacement secret.
3. Restart affected services.
4. Validate critical paths.
5. Open incident and complete post-incident review.

## Audit and Documentation

For each rotation event, record:

- Rotated secret identifier (not value)
- Date/time and owner
- Services impacted
- Validation outcome
- Rollback actions (if any)

Also update:

- docs/operations/deployment-guide.md if process changed
- docs/technical/configuration-guide.md if config contract changed
- docs/CHANGELOG.md for operationally relevant changes
