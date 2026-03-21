# Documentation Guardrail System

This document explains StorageLens's automated documentation guardrail system, which ensures code changes are always accompanied by documentation updates.

## Overview

The **Documentation Guardrail** is a CI/CD workflow (`.github/workflows/docs-guard.yml`) that automatically validates every pull request to ensure that code, configuration, and infrastructure changes are accompanied by corresponding documentation updates.

**Goal**: Keep documentation synchronized with code changes and prevent knowledge silos.

---

## How It Works

### 1. File Monitoring

The guardrail monitors changes to specific file categories:

#### 📝 **Watched Files (Trigger Documentation Requirement)**

```
Source Code:
  - src/**
  - tests/**

Infrastructure & Deployment:
  - infra/**
  - scripts/**
  - docker-compose.yml
  - azure.yaml
  - StorageLens.sln / StorageLens.slnx

Workflows:
  - .github/workflows/**

Configuration:
  - README.md

Copilot Configuration & Agents:
  - .instructions.md        (custom instructions)
  - .prompt.md              (custom prompts)
  - .agent.md               (agent definitions)
  - SKILL.md                (skill definitions)
  - copilot-instructions.md (project-level instructions)
  - AGENTS.md               (agent registry)
```

#### 📚 **Documentation Files (Satisfy Requirement)**

```
General Docs:
  - docs/**
  - docs/CHANGELOG.md (ALWAYS required when source files change)
```

### 2. Enforcement Rules

**Rule 1: Source/Config Changes Require Documentation Update**

```
IF (any source_or_config file changes)
THEN (at least one file under docs/ must also change)
```

**Example Violation**:
```
❌ Changed: src/StorageLens.Services.Locations/Controllers/LocationController.cs
❌ Did NOT change: Any files in docs/
→ PR blocked: "Documentation update required"
```

**Example Fix**:
```
✅ Changed: src/StorageLens.Services.Locations/Controllers/LocationController.cs
✅ Also changed: docs/technical/api-specification.md
→ PR passes: Documentation requirement satisfied
```

---

**Rule 2: Any Code Change Requires CHANGELOG Update**

```
IF (any source_or_config file changes)
THEN (docs/CHANGELOG.md must be updated in the same PR)
```

**Example Violation**:
```
❌ Changed: src/StorageLens.Services.Duplicates/DuplicateCalculator.cs
❌ Did NOT change: docs/CHANGELOG.md
→ PR blocked: "CHANGELOG update required"
```

**Example Fix**:
```
✅ Changed: src/StorageLens.Services.Duplicates/DuplicateCalculator.cs
✅ Also changed: docs/CHANGELOG.md with entry:
   ### Fixed
   - **Duplicates service** — Fixed incorrecte hash comparison logic
→ PR passes: CHANGELOG requirement satisfied
```

---

**Rule 3: Copilot Configuration Changes Require Documentation**

```
IF (any .instructions.md, .prompt.md, .agent.md, SKILL.md, 
    copilot-instructions.md, or AGENTS.md file changes)
THEN (docs/CHANGELOG.md must reflect the update)
    AND (docs/developer/copilot-agents-guide.md may need updating if new agent added)
```

This ensures Copilot configuration changes are tracked and teams stay aware of new agents/skills/specialized workflows.

---

## Requirement Categories

### **User-Visible Changes**

Changes that impact end-users must be documented:

| Change Type | Required Documentation |
|---|---|
| New UI feature/component | `docs/user-guide/` |
| UI component accessibility | `docs/user-guide/accessibility-guide.md` |
| UI/UX design change | `docs/user-guide/design-patterns.md` |
| Dashboard modification | `docs/user-guide/dashboard-guide.md` |
| New API endpoint | `docs/technical/api-specification.md` |
| Breaking API change | `docs/technical/service-contracts.md` + migration guide |
| New service | `docs/architecture/microservices-architecture.md` |

**ALWAYS**: `docs/CHANGELOG.md` with entry summarizing the change

### **Developer-Facing Changes**

Changes that impact developers must be documented:

| Change Type | Required Documentation |
|---|---|
| New service or API | `docs/technical/service-contracts.md` |
| Database schema change | `docs/technical/database-schema.md` |
| Error handling pattern | `docs/technical/api-specification.md` |
| Data model change | `docs/technical/data-architecture.md` |
| Testing patterns | `docs/developer/coding-standards.md` |

**ALWAYS**: `docs/CHANGELOG.md` with entry

### **Operational Changes**

Changes affecting deployment, scaling, or infrastructure must be documented:

| Change Type | Required Documentation |
|---|---|
| Service port change | `docs/operations/deployment-guide.md` |
| New environment variable | `docs/operations/configuration-guide.md` |
| Bicep infrastructure change | `docs/operations/deployment-guide.md` |
| Docker Compose change | `docs/operations/docker-deployment.md` |
| Scaling requirements | `docs/operations/scaling-guide.md` |

**ALWAYS**: `docs/CHANGELOG.md` with entry

### **Workflow & Architecture Changes**

Changes to system workflows or service interactions must be documented:

| Change Type | Required Documentation |
|---|---|
| Orchestration flow change | `docs/diagrams/scan-workflow.md` |
| Service interaction change | `docs/diagrams/service-interactions.md` |
| New microservice | `docs/architecture/microservices-architecture.md` |
| Retry/resume logic change | `docs/diagrams/retry-resume-workflow.md` |

**ALWAYS**: `docs/CHANGELOG.md` with entry

---

## Documentation Update Guide by File Type

### When You Change `.src/` Files

**Minimum Documentation Updates**:
1. ✅ At least one file in `docs/technical/`, `docs/architecture/`, or `docs/user-guide/`
2. ✅ `docs/CHANGELOG.md` with clear entry

**Example for New API Endpoint**:
```markdown
# docs/technical/api-specification.md
## New Endpoint: GET /api/locations/{id}/metrics

Returns usage metrics for a storage location...

# docs/CHANGELOG.md
### Added
- **Locations API** — New `GET /api/locations/{id}/metrics` endpoint for location usage metrics
```

### When You Change `tests/` Files

**Minimum Documentation Updates**:
1. ✅ If introducing new test patterns: `docs/developer/coding-standards.md`
2. ✅ `docs/CHANGELOG.md` if test infrastructure significantly changes

### When You Change `infra/` Files

**Minimum Documentation Updates**:
1. ✅ `docs/operations/deployment-guide.md` and/or `docs/operations/scaling-guide.md`
2. ✅ `docs/CHANGELOG.md` with deployment impact

**Example for New Bicep Resource**:
```markdown
# docs/operations/deployment-guide.md
## New Resource: Application Insights Instance

The Profiling service now includes integrated monitoring...

# docs/CHANGELOG.md
### Added
- **Infrastructure** — New Application Insights resource for Profiling service telemetry
```

### When You Change `.github/workflows/` Files

**Minimum Documentation Updates**:
1. ✅ `docs/operations/deployment-guide.md` if CI/CD changes deployment behavior
2. ✅ `docs/CHANGELOG.md` with workflow change

### When You Change Docker Compose or Configuration

**Minimum Documentation Updates**:
1. ✅ `docs/operations/docker-deployment.md` or `docs/operations/configuration-guide.md`
2. ✅ `docs/CHANGELOG.md` with configuration change

### When You Change Copilot Configuration Files

**Minimum Documentation Updates**:
1. ✅ Update affected documentation (e.g., if adding new agent, update `docs/developer/copilot-agents-guide.md`)
2. ✅ Update `docs/developer/developer-guide.md` if workflow changes
3. ✅ `docs/CHANGELOG.md` with agent/skill/configuration changes

**Example for New Agent (e.g., UI/UX Designer)**:
```markdown
# docs/developer/copilot-agents-guide.md
## Agent: UI/UX Designer [NEW]

Specialized for building and enhancing user interface components,
improving user experience, and ensuring design consistency across StorageLens.Web...

# docs/CHANGELOG.md
### Added
- **Copilot Agents** — New UI/UX Designer agent for component development and design consistency
```

**Example for New Skill**:
```markdown
# docs/developer/copilot-agents-guide.md or SKILL.md
## Skill: [Skill Name] [NEW]

Description of skill purpose and when to invoke...

# docs/CHANGELOG.md
### Added
- **Copilot Skills** — New [Skill Name] skill for [purpose]
```

---

## Typical PR Workflow

### Step 1: Implement Your Feature

```bash
git checkout develop
git pull
git checkout -b feature/my-feature

# Make code changes
# src/StorageLens.Services.X/MyFeature.cs

# Write tests
# tests/StorageLens.Services.X.Tests/MyFeatureTests.cs
```

### Step 2: Update Documentation

**Before creating PR**, update relevant docs:

```bash
# 1. API change? → Update docs/technical/api-specification.md
# 2. New service? → Update docs/architecture/microservices-architecture.md  
# 3. Infrastructure change? → Update docs/operations/deployment-guide.md
# 4. User-facing change? → Update relevant docs/user-guide/ file
# 5. ALWAYS → Update docs/CHANGELOG.md
```

### Step 3: Create Pull Request

```bash
git add .
git commit -m "Feature: MyFeature with documentation"
git push origin feature/my-feature
# Create PR on GitHub
```

### Step 4: CI/CD Validation

**Guard Checklist** (automated):
- ✅ Did you change source/config files? → Did you update docs files?
- ✅ Did you change code? → Did you update docs/CHANGELOG.md?
- ✅ Did you change Copilot config? → Did you update docs/CHANGELOG.md?

**If any check fails**:
```
❌ PR Blocked: Documentation update required

Documentation update required: source or configuration changed, 
but no files under docs/ were updated.

Please update relevant documentation in docs/ before merging.
```

**Fix**: 
- Update missing documentation
- Commit and push to PR branch
- Guard re-runs automatically

### Step 5: Merge

Once all checks pass (including guardrail):
```bash
✅ Build passing
✅ Tests passing
✅ Documentation guard passing
✅ Code review approved
→ Ready to merge!
```

---

## Special Cases

### Documentation-Only Changes

If you're updating ONLY documentation (no code changes):
- ✅ Allowed without guardrail restrictions
- ✅ Update docs/CHANGELOG.md if docs significantly improve/change

**Example**:
```
Changed files:
  - docs/technical/api-specification.md (added clarification)
  - docs/CHANGELOG.md (documented the improvement)
  
Result: No guardrail check triggered (no source/config files changed)
```

### Refactoring Without Behavior Changes

If you refactor code without changing behavior:
- ✅ Still requires documentation update if internal patterns change
- ✅ Still requires CHANGELOG.md entry if architecture/patterns affected
- ✅ May skip if truly internal-only refactor with no visible impact

**Example**:
```
Changed:
  - src/StorageLens.Services.Locations/Controllers/LocationController.cs
    (refactored to async/await pattern)
  
Required:
  - docs/developer/coding-standards.md (to document the async/await approach)
  - docs/CHANGELOG.md (internal refactoring entry)
```

### Emergency/Hotfix Branches

Hotfixes to `main` must still:
1. ✅ Update relevant documentation
2. ✅ Update docs/CHANGELOG.md with [HOTFIX] tag
3. ✅ Be cherry-picked back to `develop` with same docs updates

---

## Documentation Guard Workflow File

The guardrail is implemented in `.github/workflows/docs-guard.yml`:

```yaml
name: Documentation Guard
on:
  pull_request:
    types: [opened, synchronize, reopened, ready_for_review]

jobs:
  docs-guard:
    name: Require docs for code changes
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        
      - name: Detect changed file groups
        id: changes
        uses: dorny/paths-filter@v3
        with:
          filters: |
            docs:
              - 'docs/**'
            docs_changelog:
              - 'docs/CHANGELOG.md'
            source_or_config:
              - 'src/**'    # Source code
              - 'tests/**'  # Tests
              - 'infra/**'  # Infrastructure
              - 'scripts/**'
              - '.github/workflows/**'
              - 'docker-compose.yml'
              - 'azure.yaml'
              - 'README.md'
              - 'StorageLens.sln'
              - 'StorageLens.slnx'
              - '.instructions.md'   # Copilot config
              - '.prompt.md'
              - '.agent.md'
              - 'SKILL.md'
              - 'copilot-instructions.md'
              - 'AGENTS.md'
      
      - name: Enforce docs updates
        run: |
          if [[ source_or_config changed && docs NOT changed ]]; then
            echo "❌ Documentation update required"
            exit 1
          fi
          
          if [[ source_or_config changed && CHANGELOG NOT changed ]]; then
            echo "❌ CHANGELOG update required"
            exit 1
          fi
          
          echo "✅ Documentation guard passed"
```

---

## FAQ

### Q: What if my change doesn't impact documentation?

**A**: All source/config changes should have some documentation impact. Either:
- Document the new feature/behavior for users/developers
- Update architecture docs if design changed
- Add CHANGELOG entry for visibility
- At minimum: Brief CHANGELOG entry

### Q: Can I skip documentation for refactoring?

**A**: No. Refactoring changes coding patterns, which should be documented:
- Update `docs/developer/coding-standards.md`
- Add entry to `docs/CHANGELOG.md` so team learns about pattern shift

### Q: What if documentation doesn't need updating?

**A**: If code genuinely requires no documentation update (rare), escalate to team:
- Ping PR reviewers with explanation
- They may approve exception (but still blocked by CI)
- Use case: Changing internal variable names with no external impact

### Q: How do I know which documentation files to update?

**A**: Follow the "Requirement Categories" table above, or:
- Think: "Would another developer need to know about this change?" → Update `docs/developer/`
- Think: "Would a user care about this?" → Update `docs/user-guide/`
- Think: "Would a designer need to know?" (UI/UX changes) → Update `docs/user-guide/design-patterns.md`
- Think: "Does this affect operations/deployment?" → Update `docs/operations/`  
- Think: "Is this an agent/skill addition?" → Update `docs/developer/copilot-agents-guide.md`
- **Always**: Update `docs/CHANGELOG.md`

### Q: What if I updated documentation but guardrail still fails?

**A**: Ensure:
1. ✅ You changed at least one file in `docs/` folder (not just `docs/CHANGELOG.md`)
2. ✅ You updated `docs/CHANGELOG.md` with a meaningful entry
3. ✅ The files you changed are actually in the PR (use `git diff`)
4. ✅ Wait for CI to re-run after your push (1-2 minutes)

### Q: Can multiple people contribute documentation to the same PR?

**A**: Yes! Push commits with documentation updates—guardrail will pass once requirements met.

---

## Best Practices

### ✅ DO

- **Update documentation in the same commit** as code changes
- **Be specific in CHANGELOG entries** — "Fixed duplicate calculation" is better than "Bug fixes"
- **Link related documentation** — If you update API docs, link to data model docs
- **Use clear language** — Assume readers aren't familiar with this change
- **Include examples** for complex features
- **Keep CHANGELOG.md chronological** — Newest entries at top

### ❌ DON'T

- **Skip documentation "for now"** — Future PRs often forget
- **Write vague CHANGELOG entries** — "Updated code" tells no one anything useful
- **Document in separate PR** — Do it with the code change
- **Break documentation links** — Test links if you rename/move files
- **Update old/outdated documentation** — Refresh first, then update

---

## Continuous Improvement

The guardrail system itself is documented in `docs/developer/documentation-guardrail.md` (this file).

**To improve the guardrail**:
1. Update `.github/workflows/docs-guard.yml` if watch patterns need changing
2. Update `docs/developer/documentation-guardrail.md` to reflect changes
3. Update `docs/CHANGELOG.md` with guardrail improvements
4. Team reviews and approves

---

**Last Updated**: March 19, 2026  
**Related Files**:
- `.github/workflows/docs-guard.yml` — The guardrail implementation
- `docs/developer/contributing.md` — PR workflow and checklist
- `docs/CHANGELOG.md` — Log of all changes
- `docs/DeveloperChecklist.md` — Rebuild guide (includes docs verification phase)
