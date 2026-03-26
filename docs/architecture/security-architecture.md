# Security Architecture

## Security Objectives

- Protect storage metadata and operational telemetry
- Enforce least privilege across services and infrastructure
- Preserve auditability of scan workflow actions

## Current Controls

- Service-to-service isolation by container boundary
- Configuration-based endpoint management
- Health endpoints separated from business APIs
- Read-only scanning behavior (no file mutation)
- Service-to-service API key authentication via `X-API-Key`
- Correlation ID propagation for request traceability and audit context
- Rate limiting on service endpoints to reduce abuse and saturation risk
- Structured audit logging for sensitive and workflow actions

## Recommended Enhancements

- Add end-user authentication and authorization for interactive access (OIDC/JWT)
- Introduce API gateway and service identity
- Encrypt in transit (TLS everywhere)
- Encrypt SQL data at rest and backup sets
- Store secrets in vault-managed providers, not plain env vars
- Extend centralized audit trail with tamper-evident logging

## Threat Considerations

- Path traversal and unsafe root path handling
- Unauthorized API invocation and replay attempts
- Sensitive metadata exposure in logs
- Resource exhaustion during large scans
