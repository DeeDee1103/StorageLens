# StorageLens Copilot Agents & Skills Guide

This guide explains how to use the specialized Copilot agents and skills configured for StorageLens development.

## What Are Agents & Skills?

**Skills** are domain-specific capabilities that enhance Copilot's understanding of StorageLens patterns.  
**Agents** are specialized AI personas, each expert in a particular development workflow.

Together, they provide context-aware assistance that understands:
- Service boundaries and microservices architecture
- API contract design and versioning
- Cross-service integration patterns
- Testing and documentation requirements
- Deployment and infrastructure concerns

## Configuration Files

The following files in the root directory configure Copilot for StorageLens:

| File | Purpose |
|------|---------|
| `.instructions.md` | Detailed project guidance and development practices |
| `.prompt.md` | Concise architecture context and red flags |
| `.agent.md` | Definitions of 6 specialized agents |
| `SKILL.md` | 9 reusable domain-specific skills |
| `copilot-instructions.md` | Configuration, behavioral rules, and scenarios |
| `AGENTS.md` | Agent registry with invocation patterns |

These files are **automatically loaded by Copilot** when you work in the StorageLens workspace.

---

## The 6 Specialized Agents

### 1. StorageLens-Microservice-Developer

**When to use**: Adding features, fixing bugs, or refactoring **within a single service**.

**Examples**:
- "Add a new endpoint to the Locations service to export location data as CSV"
- "Fix the bug in Duplicates where hash comparison fails with null values"
- "Refactor the Scanner to support async file enumeration"

**What it does**:
- ✅ Reviews service DbContext and existing contracts
- ✅ Examines test patterns to match your coding style
- ✅ Implements changes respecting service boundaries
- ✅ Writes comprehensive unit tests
- ✅ Reminds you to update CHANGELOG.md

**You'll get**: Code implementation, tests, and documentation checklist.

---

### 2. StorageLens-Cross-Service-Integrator

**When to use**: Adding features that **span multiple services** or designing **orchestration logic**.

**Examples**:
- "Design a new workflow where the Orchestrator coordinates scanning and hashing across services"
- "Add profiling data collection that's shared between Analytics, Duplicates, and Scanner"
- "How should Duplicates call FileInventory to retrieve hash data?"

**What it does**:
- ✅ Maps affected services and data ownership
- ✅ Reviews contracts in `StorageLens.Shared.Contracts`
- ✅ Designs immutable DTOs for cross-service communication
- ✅ Creates typed `HttpClient` integration patterns
- ✅ Implements correlation ID propagation
- ✅ Writes integration tests
- ✅ Updates architecture diagrams

**You'll get**: Cross-service implementation, integration tests, architecture documentation.

---

### 3. StorageLens-Contract-Steward

**When to use**: **Adding, modifying, or versioning API contracts and shared DTOs**.

**Examples**:
- "I need to add a new field to the ScanJobDto. Is this backward compatible?"
- "How should I handle a breaking change to the FileMetada contract?"
- "What services depend on the LocationDto? Will my change break them?"

**What it does**:
- ✅ Reviews existing contracts in `StorageLens.Shared.Contracts`
- ✅ Searches across services to identify contract usage
- ✅ Assesses backward compatibility impact
- ✅ Recommends versioning strategies for breaking changes
- ✅ Ensures immutability and serialization correctness
- ✅ Updates API documentation

**You'll get**: Versioning strategy, migration documentation, backward-compatible design.

---

### 4. StorageLens-Test-and-Docs-Guardian

**When to use**: **Ensuring adequate test coverage and documentation compliance**.

**Examples**:
- "Generate comprehensive tests for this Orchestrator workflow logic"
- "I just changed the scan stages. What documentation should I update?"
- "Is my test coverage adequate for this cross-service feature?"

**What it does**:
- ✅ Analyzes code for test coverage gaps
- ✅ Identifies files in `docs/` that need updating
- ✅ Verifies CHANGELOG update requirements
- ✅ Suggests unit and integration test cases
- ✅ Checks documentation guard compliance (CI rule)
- ✅ Links to relevant CI workflows

**You'll get**: Test recommendations, documentation checklist, compliance verification.

---

### 5. StorageLens-Architecture-Reviewer

**When to use**: **Validating changes against microservices principles** and **architectural design**.

**Examples**:
- "Should the profiling feature go in Analytics or Duplicates service?"
- "Is my design for cross-service communication respecting service boundaries?"
- "I'm seeing tight coupling between Scanner and Hashing. How should I refactor?"

**What it does**:
- ✅ Reviews microservices architecture and bounded contexts
- ✅ Identifies anti-patterns (cross-DB access, tight coupling)
- ✅ Validates HTTP contract and retry semantics
- ✅ Checks error handling and logging approach
- ✅ Suggests resilience patterns (timeouts, retries, circuit breakers)
- ✅ Recommends architecture documentation updates

**You'll get**: Architecture validation, anti-pattern detection, design recommendations.

---

### 6. StorageLens-DevOps-and-Deployment

**When to use**: **Docker, infrastructure, and deployment changes**.

**Examples**:
- "I created a new Profiling service. How do I add it to docker-compose?"
- "Update the Bicep templates for the new service and its database"
- "What environment variables and secrets do I need to configure?"

**What it does**:
- ✅ Reviews `docker-compose.yml` and `infra/main.bicep`
- ✅ Suggests Docker image, port, and volume configurations
- ✅ Updates Bicep for App Services, networking, and databases
- ✅ Manages managed identities and RBAC
- ✅ Provides secrets management patterns
- ✅ Updates deployment and operations documentation

**You'll get**: Docker configurations, Bicep templates, deployment scripts, operations guides.

---

## 9 Domain-Specific Skills

These skills are automatically invoked by agents or available when you ask:

| Skill | What It Does |
|-------|-------------|
| **Service Boundaries** | Identifies service ownership, cross-service interaction points, flags anti-patterns |
| **Contract Design** | Manages API contracts, immutability, backward compatibility, versioning |
| **Cross-Service Communication** | Typed HttpClient patterns, correlation IDs, retry logic, integration tests |
| **Docs Guard Compliance** | Documentation checklist, CHANGELOG requirements, CI validation |
| **Testing Patterns** | Unit test structures, mocking, integration tests, coverage analysis |
| **Anti-Pattern Detection** | Identifies cross-DB queries, tight coupling, missing logging |
| **Local Development & Docker** | Docker Compose setup, port mapping, health checks, volumes |
| **Deployment & Infrastructure** | Bicep templates, Azure resources, networking, RBAC, secrets |
| **Workflow Orchestration** | State machines, retry/backoff, resume logic, correlation IDs |

---

## How to Invoke Agents

### Method 1: Direct Ask
```
"I need to add a new endpoint to the Locations service."
→ Copilot suggests Microservice-Developer agent automatically
```

### Method 2: Explicit Request
```
"@StorageLens-Cross-Service-Integrator: Design the profiling feature"
→ Activates Cross-Service-Integrator agent for this task
```

### Method 3: Question-Based
```
"Is my design respecting service boundaries?"
→ Copilot invokes Architecture-Reviewer agent
```

---

## Common Development Scenarios

### Scenario 1: Adding an API Endpoint to a Service

**Use this agent sequence**:
1. **Microservice-Developer** - Implement endpoint with tests
2. **Test-and-Docs-Guardian** - Verify coverage and doc updates
3. **Contract-Steward** - Review if contracts changed

**Example conversation**:
```
User: "Add a new GET endpoint to Locations service that returns usage statistics"

Copilot (Microservice-Developer):
> Reviewing Locations service structure...
> Located LocationsController
> Found LocationDto and related contracts
> Suggesting test patterns from existing test project
> [Provides implementation + tests]

User: "Make sure documentation is updated"

Copilot (Test-and-Docs-Guardian):
> Checking test coverage...
> Identified files to update:
>   - docs/technical/api-specification.md (new endpoint)
>   - docs/CHANGELOG.md (user-visible change)
> Test coverage looks good. ✓
```

---

### Scenario 2: Cross-Service Feature

**Use this agent sequence**:
1. **Architecture-Reviewer** - Validate design
2. **Cross-Service-Integrator** - Implement integration
3. **Test-and-Docs-Guardian** - Verify tests and docs
4. **Contract-Steward** - Review contract changes

**Example conversation**:
```
User: "I need Duplicates service to retrieve file hashes from FileInventory"

Copilot (Cross-Service-Integrator):
> Mapping service boundaries...
> Designing IFileInventoryClient interface
> Implementing typed HttpClient with correlation ID propagation
> [Provides full integration code]

User: "Is this design sound?"

Copilot (Architecture-Reviewer):
> Service boundaries: ✓ Respected
> HTTP communication: ✓ Proper pattern
> Error handling: ✓ Explicit failure cases
> Recommendation: Add circuit breaker for resilience
```

---

### Scenario 3: API Breaking Change

**Use this agent sequence**:
1. **Contract-Steward** - Assess impact
2. **Cross-Service-Integrator** - Coordinate updates
3. **Test-and-Docs-Guardian** - Migration docs

**Example conversation**:
```
User: "I need to add a required field to ScanJobDto"

Copilot (Contract-Steward):
> Analyzing ScanJobDto usage...
> Services affected:
>   - Web (reads ScanJobDto)
>   - Orchestrator (creates ScanJobDto)
>   - ScanJobs service (owns ScanJobDto)
> This is a BREAKING change
> 
> Options:
> 1. Make field optional with migration period
> 2. Deprecate old contract, create v2
> 3. Add default value in DbContext
```

---

### Scenario 4: New Microservice

**Use this agent sequence**:
1. **Architecture-Reviewer** - Validate bounded context
2. **Microservice-Developer** - Create service
3. **Cross-Service-Integrator** - Design integrations
4. **DevOps-and-Deployment** - Docker & Bicep
5. **Test-and-Docs-Guardian** - Final compliance check

**Example conversation**:
```
User: "I need to create a new Profiling service"

Copilot (Architecture-Reviewer):
> Profiling data ownership validated
> Suggested service boundaries and responsibilities
> [Architecture recommendations]

Copilot (Microservice-Developer):
> Creating project structure...
> Setting up DbContext and health endpoint...
> [Implementation scaffolding]

Copilot (DevOps-and-Deployment):
> Adding to docker-compose.yml with port 5109
> Creating Bicep resource definitions
> [Deploy configurations]

Copilot (Test-and-Docs-Guardian):
> Document updates required:
>   - docs/architecture/microservices-architecture.md (new service)
>   - docs/technical/service-contracts.md (Profiling contracts)
>   - docs/operations/deployment-guide.md (new port/config)
>   - docs/CHANGELOG.md (new service launch)
```

---

## Best Practices

### ✅ DO

- **Ask clarifying questions to agents** - They understand context
- **Let agents guide documentation updates** - They know requirements
- **Use cross-service agent for feature spanning services** - Avoids integration issues
- **Verify design with Architecture-Reviewer before coding** - Prevents rework
- **Trust the agents' red flags** - They're based on project principles

### ❌ DON'T

- **Ignore agent recommendations** - They're based on project architecture
- **Skip documentation guardrail checks** - CI will catch it
- **Code cross-DB queries** - Violates microservices principles
- **Forget correlation IDs in HTTP calls** - Breaks distributed tracing
- **Bypass test requirements** - Coverage ensures reliability

---

## Documentation Guard Integration

The CI workflow `.github/workflows/docs-guard.yml` enforces:

1. **Source/Config changes** → Must update at least one file in `docs/`
2. **Any code change** → Must update `docs/CHANGELOG.md`
3. **Agent/Skills changes** → Copilot configuration file updates

When you work with agents and make changes:
- Copilot reminds you to update `docs/`
- CI validates compliance
- PRs are blocked until documentation is updated

This ensures knowledge is captured and team learning is preserved.

---

## Key Files Reference

| Path | Purpose |
|------|---------|
| `.instructions.md` | Detailed guidance (load first when learning) |
| `.prompt.md` | Quick reference and red flags |
| `SKILL.md` | All 9 skills with examples |
| `AGENTS.md` | Agent definitions and capabilities |
| `copilot-instructions.md` | Configuration and behavioral rules |
| `.agent.md` | Agent persona definitions |
| `docs/developer/copilot-agents-guide.md` | This file - how to use agents |

---

## Questions & Support

**Q: How do I know which agent to use?**  
A: Copilot will suggest the most relevant agent based on your question. You can also:
- Check the scenario table above
- Ask "What agent should I use for...?"
- Use explicit mentions like `@StorageLens-Microservice-Developer`

**Q: Can agents work together?**  
A: Yes! Agents collaborate automatically:
- Microservice-Developer invokes Test-and-Docs-Guardian for compliance
- Cross-Service-Integrator calls Architecture-Reviewer for validation
- All agents respect the shared principles in `.instructions.md`

**Q: What if I need help outside agent scope?**  
A: Agents will escalate to more appropriate peers or provide guidance for manual steps.

**Q: How are agents and skills updated?**  
A: They're defined in root-level `.md` files. Changes are automatically picked up by Copilot.

---

**Last Updated**: March 19, 2026  
**Related**: See all configuration files in repository root (`.*` files and `AGENTS.md`, `SKILL.md`)
