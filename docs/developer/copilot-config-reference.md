# Copilot Configuration Files Reference

This document explains each Copilot configuration file and what it does.

## Configuration Files Overview

StorageLens uses 6 configuration files in the root directory to customize Copilot behavior:

| File | Format | Purpose | When to Update |
|------|--------|---------|-----------------|
| `.instructions.md` | Markdown | Detailed project guidance, development practices | Project structure or practices change |
| `.prompt.md` | Markdown | Concise context and red flags | Architecture decisions or common mistakes change |
| `.agent.md` | Markdown | Agent persona definitions | Add/remove agents or agent scope changes |
| `SKILL.md` | Markdown | Domain-specific skill definitions | New domain-specific capabilities needed |
| `copilot-instructions.md` | Markdown | Configuration, behavioral rules, communication patterns | Core project rules or scenarios change |
| `AGENTS.md` | Markdown | Agent registry with tool capabilities matrix | Agent capabilities or invocation patterns change |

---

## Detailed File Descriptions

### 1. `.instructions.md` – Project Foundation Guide

**Purpose**: Comprehensive reference for developers new to StorageLens.

**Contains**:
- Project overview and technology stack
- Service registry with responsibilities and ports
- Core principles (service boundaries, data ownership, communication)
- Development guidelines (before you code checklist)
- Coding standards (style, testing, documentation)
- Common tasks (adding services, endpoints, cross-service communication)
- Build & run commands
- Code review checklist
- Key files reference

**When to update**:
- ✏️ Project structure fundamentally changes
- ✏️ New core principle or architectural rule
- ✏️ Service ports or technology stack changes
- ✏️ New common development task patterns emerge

**Who should read**: Every developer working on StorageLens (start here for onboarding)

---

### 2. `.prompt.md` – Quick Reference & Context

**Purpose**: Concise architecture context for every Copilot interaction.

**Contains**:
- Condensed architecture (services, data ownership)
- Before-every-change checklist
- Coding conventions
- Documentation guard rules
- Common patterns with code snippets
- Red flags to avoid

**When to update**:
- ✏️ Red flags or anti-patterns change
- ✏️ Coding conventions become stricter
- ✏️ Common patterns evolve
- ✏️ Documentation requirements shift

**When Copilot uses it**: Automatically in every interaction to provide context

---

### 3. `.agent.md` – Agent Personas

**Purpose**: Define the 6 specialized agents and their responsibilities.

**Contains**:
- Agent 1: Microservice-Developer
- Agent 2: Cross-Service-Integrator
- Agent 3: Contract-Steward
- Agent 4: Test-and-Docs-Guardian
- Agent 5: Architecture-Reviewer
- Agent 6: DevOps-and-Deployment

Each agent has: Description, Invocation Examples, Responsibilities, Tools Access, Output

**When to update**:
- ✏️ Create new agent for new development workflow
- ✏️ Change agent responsibilities or scope
- ✏️ Add/remove tools available to agent
- ✏️ Modify agent collaboration patterns

**When Copilot uses it**: When routing tasks to specialized agents

---

### 4. `SKILL.md` – Domain-Specific Capabilities

**Purpose**: Define reusable domain skills across all agents.

**Contains**:
- 9 skills with "When to use", "Provides", and "Example"
- Skills include: Service Boundaries, Contract Design, Cross-Service Communication, Documentation Guard, Testing Patterns, Anti-Pattern Detection, Local Development & Docker, Deployment & Infrastructure, Workflow Orchestration

**When to update**:
- ✏️ New domain expertise needed
- ✏️ Skill scope or examples become outdated
- ✏️ Better patterns emerge in the project
- ✏️ Team learns new best practices

**When Copilot uses it**: Agents invoke relevant skills; Copilot suggests skills for questions

---

### 5. `copilot-instructions.md` – Configuration & Behavioral Rules

**Purpose**: Core configuration, rules, and communication guidance.

**Contains**:
- Project identity (repo, branch, type)
- Core development mode (microservices enforcer)
- Key file mapping
- Service port registry (5001–5108)
- Behavioral rules:
  - Service boundary enforcement
  - Contract immutability
  - Logging with correlation IDs
  - Test requirements
  - Documentation guard
- Common scenarios with step-by-step workflows
- Build & test commands
- Anti-patterns to avoid
- Review checklist
- Related docs links

**When to update**:
- ✏️ Service ports change
- ✏️ Behavioral rules become stricter/looser
- ✏️ New common scenario emerges
- ✏️ Build/test commands change

**Who reads it**: Developers and Copilot (loaded automatically)

---

### 6. `AGENTS.md` – Agent Registry

**Purpose**: Complete agent reference with invocation patterns and capabilities matrix.

**Contains**:
- Same 6 agents as `.agent.md`, but with expanded detail
- Invocation methods (direct request, explicit mention, implicit)
- Agent collaboration patterns (which agents call which)
- Tool capabilities matrix (shows what each agent can do)
- Fallback & escalation strategy
- Related documentation links

**When to update**:
- ✏️ Agent tool access changes
- ✏️ Collaboration patterns between agents change
- ✏️ New invocation method discovered
- ✏️ Escalation strategy evolves

**How it differs from `.agent.md`**: More detailed, registry-style; includes tool matrix and invocation patterns

---

## Update Workflow

When you need to update configuration files:

### Step 1: Identify Which File(s) Need Updates

| Change Type | File(s) to Update |
|-------------|-------------------|
| New service or port | `copilot-instructions.md` (port registry) |
| New architectural rule | `.instructions.md`, `.prompt.md`, `copilot-instructions.md` |
| New agent needed | `.agent.md`, `AGENTS.md` |
| New development pattern | `SKILL.md`, `.instructions.md` |
| Anti-pattern discovered | `.prompt.md`, `SKILL.md` |
| Documentation rules change | `copilot-instructions.md` |

### Step 2: Update the File(s)

Use clear, concise language. Include examples where helpful.

### Step 3: Update docs/CHANGELOG.md

Document the configuration change:
```markdown
### Added
- New agent: StorageLens-SecurityReview for security best practices

### Changed
- Updated Service port registry: Scanner moved from 5106 to 5107
```

### Step 4: Important: Documentation Guard

**Remember**: Changes to `.instructions.md`, `.prompt.md`, `.agent.md`, `SKILL.md`, `copilot-instructions.md`, or `AGENTS.md` trigger the CI documentation guard.

You MUST:
1. ✅ Update at least one file in `docs/`
2. ✅ Update `docs/CHANGELOG.md`

The documentation guard workflow (`.github/workflows/docs-guard.yml`) will block your PR if:
- You change a config file
- But don't update documentation
- Or don't update CHANGELOG.md

---

## File Interactions

Here's how the files work together:

```
┌─────────────────────────────────────────────────────┐
│        .instructions.md (Foundation)                 │
│  - Project overview                                  │
│  - All principles & practices                        │
│  - Read first for learning                           │
└──────────────────┬──────────────────────────────────┘
                   │
        ┌──────────┴──────────┐
        ▼                     ▼
┌──────────────────┐  ┌──────────────────────┐
│ .prompt.md       │  │ copilot-instructions │
│ (Quick ref)      │  │ (Config & behavior)  │
│ - Context        │  │ - Rules              │
│ - Red flags      │  │ - Scenarios          │
│ - Patterns       │  │ - Port registry      │
└────────┬─────────┘  └──────────┬───────────┘
         │                       │
         └───────────┬───────────┘
                     ▼
        ┌────────────────────────┐
        │  .agent.md & AGENTS.md │
        │  (Agent definitions)   │
        │  - Personas            │
        │  - Responsibilities    │
        │  - Tools/capabilities  │
        └────────────┬───────────┘
                     │
                     ▼
        ┌────────────────────────┐
        │  SKILL.md              │
        │  (Reusable skills)     │
        │  - Service boundaries  │
        │  - Testing patterns    │
        │  - etc. (9 total)      │
        └────────────────────────┘
```

---

## Maintenance Best Practices

### ✅ Keep Files Synchronized

When updating one file, check if others need updates:
- New rule in `.instructions.md`? → Add to `.prompt.md` red flags
- New scenario in `copilot-instructions.md`? → Add to `.instructions.md` common tasks
- New agent responsibility? → Update both `.agent.md` and `AGENTS.md`

### ✅ Include Examples

Concrete examples help developers understand quickly:
```markdown
## Example

User: "Add a new validation to the Locations API"
Copilot (Microservice-Developer):
> Checking Locations service structure...
> Found LocationController and LocationValidator
> [provides implementation]
```

### ✅ Link to Related Resources

Always link to relevant documentation:
```markdown
See [`docs/architecture/microservices-architecture.md`](../../docs/architecture/microservices-architecture.md) for service details.
```

### ✅ Version Your Changes

Include "Last Updated" dates in files:
```markdown
**Last Updated**: March 19, 2026
**Next Review**: When new services are added
```

### ✅ Audit Tool Access

Before changing what tools an agent can access, verify:
- ✓ The tool is necessary for the agent's role
- ✓ The tool doesn't violate project principles
- ✓ Update both `.agent.md` and `AGENTS.md` tool matrix

---

## Testing Configuration Changes

After updating configuration files:

1. **Manual Test**: Open a new Copilot conversation and verify:
   - Agents are suggested appropriately
   - Red flags are mentioned
   - Common patterns are accurate

2. **Documentation Guard**: Ensure:
   - At least one `docs/` file is updated
   - `docs/CHANGELOG.md` entry exists
   - CI workflow passes

3. **Team Review**: Before merging:
   - Have teammates review configuration changes
   - Verify examples are realistic and accurate
   - Check for contradictions with existing docs

---

## Related Documentation

- [`docs/developer/copilot-agents-guide.md`](../../docs/developer/copilot-agents-guide.md) — How to USE the agents (for developers)
- [`docs/developer/developer-guide.md`](../../docs/developer/developer-guide.md) — General StorageLens development guidance
- [`docs/developer/contributing.md`](../../docs/developer/contributing.md) — Contribution workflow and PR requirements

---

**Last Updated**: March 19, 2026  
**Audience**: Project maintainers updating Copilot configuration  
**Next Review**: When new development workflows emerge or project structure changes
