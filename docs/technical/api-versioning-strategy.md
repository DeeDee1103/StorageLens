# API Versioning Strategy

## Purpose

This document defines how StorageLens introduces API changes without breaking existing consumers.

## Versioning Model

StorageLens uses URI versioning for breaking changes:

- Current major version endpoints: `/api/...` (treated as V1)
- Next breaking version endpoints: `/api/v2/...`

For non-breaking additions, remain in the current major version.

## What Is a Breaking Change

Any change that can break existing clients, including:

- Removing fields from response DTOs
- Renaming fields or changing field types
- Changing endpoint behavior or status code semantics incompatibly
- Tightening validation in ways that reject previously valid requests

## Change Categories

- Additive (non-breaking): new optional fields, new endpoints, new optional query params
- Breaking: incompatible contract changes requiring a new major API version

## Deprecation Policy

1. Announce deprecation in docs/CHANGELOG.md and API docs.
2. Keep deprecated version active for minimum 90 days.
3. Provide migration guidance and examples.
4. Remove deprecated version only after communicated retirement date.

## Implementation Rules

- Shared DTOs in `StorageLens.Shared.Contracts` must prefer additive evolution.
- When introducing V2, keep V1 controllers/routes active during migration window.
- Track version ownership and migration tasks in sprint planning.

## Documentation Requirements for API Changes

For each API contract change, update all of:

- docs/technical/api-specification.md
- docs/technical/api-versioning-strategy.md (if policy/process changed)
- docs/CHANGELOG.md

## PR Checklist for Breaking Changes

- New versioned endpoint path added.
- Existing clients remain functional on prior version.
- Migration notes included.
- Deprecation timeline documented.
