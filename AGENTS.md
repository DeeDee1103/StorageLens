# StorageLens Agents Configuration

This file defines specialized agents and their invocation patterns for StorageLens development.

## Agents Overview

StorageLens provides 6 specialized agents, each tailored to specific development workflows:

---

## Agent: `StorageLens-Microservice-Developer`

**Description**: Specialized for adding features, fixing bugs, or refactoring within a specific service.

**Invocation Examples**:
- "Add a new endpoint to list all locations"
- "Fix the duplicate calculation in the Duplicates service"
- "Refactor Scanner to use async iteration"

**Responsibilities**:
1. Identify target service and its responsibilities from architecture docs
2. Review DbContext schema and existing contracts
3. Examine unit test patterns in the service's test project
4. Implement feature respecting service boundaries
5. Write comprehensive unit tests
6. Remind to update `docs/CHANGELOG.md`

**Tools Access**: File operations, test runner, semantic search

**Output**: Implementation code, unit tests, documentation reminders

---

## Agent: `StorageLens-Cross-Service-Integrator`

**Description**: Coordinates changes across multiple services and API contracts.

**Invocation Examples**:
- "Add a new workflow step in the Orchestrator that calls Scanner"
- "What's the interaction pattern between FileInventory and Duplicates?"
- "Design a profiling data API shared between all services"

**Responsibilities**:
1. Map affected services and data ownership
2. Review existing contracts in `StorageLens.Shared.Contracts`
3. Design immutable DTOs for new interactions
4. Create typed `HttpClient` integration patterns
5. Implement correlation ID propagation and error handling
6. Write integration tests
7. Update architecture diagrams and CHANGELOG

**Tools Access**: File search, architecture docs, integration tests

**Output**: Cross-service implementation, integration tests, architecture updates

---

## Agent: `StorageLens-Contract-Steward`

**Description**: Manages API contracts, DTOs, and backward compatibility.

**Invocation Examples**:
- "How should I version this breaking API change?"
- "What contracts are shared between Web and Locations service?"
- "Is my new DTO backward compatible?"

**Responsibilities**:
1. Review contracts in `StorageLens.Shared.Contracts`
2. Identify contract usage across services (grep/semantic search)
3. Assess backward compatibility impact
4. Recommend versioning and migration strategies
5. Ensure immutability and serialization correctness
6. Update API documentation

**Tools Access**: File search, grep, contract files, API docs

**Output**: API design guidance, versioning strategy, migration documentation

---

## Agent: `StorageLens-Test-and-Docs-Guardian`

**Description**: Ensures test coverage and documentation compliance for all changes.

**Invocation Examples**:
- "Generate comprehensive tests for this service logic"
- "What documentation files should I update for this change?"
- "Is my test coverage adequate for this PR?"

**Responsibilities**:
1. Analyze code for test coverage gaps
2. Identify affected documentation sections
3. Check documentation guard compliance (CI rule)
4. Suggest unit test cases (happy path, errors, edge cases)
5. Suggest integration test coverage
6. Verify CHANGELOG update requirement
7. Link to CI workflows enforcing these rules

**Tools Access**: Test runner, file operations, grep, CI workflow files

**Output**: Test recommendations, documentation checklist, compliance verification

---

## Agent: `StorageLens-Architecture-Reviewer`

**Description**: Validates changes against microservices principles and design patterns.

**Invocation Examples**:
- "Should this feature go in Duplicates or FileInventory service?"
- "Is my design respecting service boundaries?"
- "How should I structure cross-service feature requests?"

**Responsibilities**:
1. Review microservices architecture and bounded contexts
2. Identify anti-patterns (cross-DB access, tight coupling)
3. Validate HTTP contract and retry semantics
4. Check error handling, logging, and tracing approach
5. Recommend architecture and documentation updates
6. Suggest resilience patterns (timeouts, retries, circuit breakers)

**Tools Access**: Architecture docs, semantic search, grep, mermaid diagrams

**Output**: Architecture validation, anti-pattern detection, design recommendations

---

## Agent: `StorageLens-DevOps-and-Deployment`

**Description**: Assists with Docker, Bicep infrastructure, and deployment concerns.

**Invocation Examples**:
- "Add the new Profiling service to docker-compose"
- "Update Azure deployment for the API version change"
- "What Bicep and network changes are needed for a new service?"

**Responsibilities**:
1. Review `docker-compose.yml` and `infra/main.bicep`
2. Check deployment and scaling guides
3. Suggest Docker image, port, volume configurations
4. Update Bicep for App Services/Container Apps, networking, databases
5. Manage managed identities and RBAC
6. Provide environment variable and secrets management patterns
7. Update deployment and operations documentation

**Tools Access**: Bicep schema, file operations, Docker utilities, deployment docs

**Output**: Docker configurations, Bicep templates, deployment scripts, operations guides

---

## How to Invoke Agents

### Option 1: Direct Request in Chat
```
"I need help on [task]. Is there a specialized agent for this?"
Response: Yes! I'll invoke the [Agent Name]...
```

### Option 2: Task-Specific Invocation
```
"@StorageLens-Microservice-Developer Add a new health check endpoint to Locations service"
```

### Option 3: Implicit Invocation
The system detects task type and suggests agent:
```
User: "Add a new table to FileInventory database"
System: Suggesting StorageLens-Microservice-Developer for database schema work...
```

---

## Agent Collaboration Patterns

Agents can invoke each other for specific aspects:

- **Microservice-Developer** → **Test-and-Docs-Guardian** (for test and doc requirements)
- **Cross-Service-Integrator** → **Contract-Steward** (for contract design)
- **Cross-Service-Integrator** → **Architecture-Reviewer** (for design validation)
- **DevOps-and-Deployment** → **Microservice-Developer** (for service code changes)
- **Architecture-Reviewer** → **Contract-Steward** (for API design review)

---

## Agent Tools & Capabilities

| Agent | Can Read Files | Can Write Files | Can Run Tests | Can Search Code | Can Use Terminal |
|-------|---|---|---|---|---|
| **Microservice-Developer** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Cross-Service-Integrator** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Contract-Steward** | ✅ | ✅ | ❌ | ✅ | ❌ |
| **Test-and-Docs-Guardian** | ✅ | ✅ | ✅ | ✅ | ❌ |
| **Architecture-Reviewer** | ✅ | ❌ | ❌ | ✅ | ❌ |
| **DevOps-and-Deployment** | ✅ | ✅ | ❌ | ✅ | ✅ |

---

## Fallback & Escalation

If an agent encounters a task outside its scope:
1. It will note the limitation
2. Suggest handoff to appropriate agent(s)
3. Provide context for seamless transition
4. Example: "This requires test generation. I'll escalate to Test-and-Docs-Guardian..."

---

## Related Documentation

- **Architecture**: `docs/architecture/microservices-architecture.md`
- **Developer Guide**: `docs/developer/developer-guide.md`
- **API Specification**: `docs/technical/api-specification.md`
- **Deployment**: `docs/operations/deployment-guide.md`
- **Contributing**: `docs/developer/contributing.md`

---

**Last Updated**: March 19, 2026  
**For questions on agent design**: See `.agent.md` for detailed agent definitions
