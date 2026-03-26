# Terminology Consistency Matrix

## Purpose
This matrix defines canonical term usage across StorageLens documentation.
Use it to keep wording consistent between business-facing docs, technical docs, code entities, and HTTP headers.

## Canonical Rules
- Business docs prefer human-readable forms (for example, Scan Job, Workflow Status, Storage Location).
- Technical docs can include code aliases when needed (for example, Scan Job (code: ScanJob)).
- Code and data model docs use exact symbol forms (for example, ScanJobStatus, CorrelationId).
- Compound modifiers are hyphenated (for example, service-to-service).

## Core Matrix

| Canonical Term | Business Form | Technical Form | Code/Data Form | Header/Param Form |
| --- | --- | --- | --- | --- |
| Scan Job | Scan Job | Scan Job | ScanJob | scanJobId |
| Scan Job Status | Scan Job Status | Scan Job Status | ScanJobStatus | n/a |
| Workflow Instance | Workflow | Workflow Instance | WorkflowInstance | workflowId |
| Workflow Status | Workflow Status | Workflow Status | WorkflowStatus | n/a |
| Workflow Step Execution | Workflow Step | Workflow Step Execution | WorkflowStepExecution | n/a |
| Correlation ID | Correlation ID | Correlation ID | CorrelationId | X-Correlation-ID, correlationId |
| API Key | API Key | API Key | ServiceApiKey | X-API-Key |
| Storage Location | Storage Location | Storage Location | StorageLocation, StorageLocationId | locationId |
| File Inventory | File Inventory | File Inventory | FileInventory, FileRecord | n/a |
| Duplicate Group | Duplicate Group | Duplicate Group | DuplicateGroup | n/a |
| Hash Status | Hash Status | Hash Status | HashStatus | n/a |
| Service-to-Service Call | n/a | Service-to-Service Call | n/a | n/a |

## Style Guidance by Document Type

### Business Documentation
- Use plain-language terms and title case in headings.
- Prefer Scan Job, Workflow, Storage Location, File Inventory.
- Avoid code-style symbols unless absolutely necessary.

### Technical Documentation
- Use canonical human-readable term first.
- Add code alias when precision matters: (code: SymbolName).
- Keep hyphenated modifiers (service-to-service, cross-service).

### Code and Data Documentation
- Use exact symbol and field names (ScanJobStatus, CorrelationId, StorageLocationId).
- Keep HTTP header names exact (X-API-Key, X-Correlation-ID).
- Keep route/query parameters camelCase when applicable.

## Frequently Misused Forms

| Avoid | Use Instead |
| --- | --- |
| ScanJob (in business prose) | Scan Job |
| ScanJobStatus (as a heading without context) | Scan Job Status (code: ScanJobStatus) |
| workflowstatus | Workflow Status |
| service to service | service-to-service |
| correlation id | Correlation ID |

## Maintenance Checklist
- Add new high-value terms here when introduced.
- Update both glossary files when canonical wording changes.
- Keep links in docs index current.
- Verify at least once per release cycle.

## Related Docs
- [Glossary of Terms - Business Users](../glossary-terms-business-users.md)
- [Glossary of Terms - Technical](../glossary-terms-technical.md)
- [Data Dictionary](../data-dictionary.md)
- [Service Contracts](service-contracts.md)
