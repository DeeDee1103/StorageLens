# Developer Documentation Guide

**Welcome to StorageLens Development!**

This directory contains all documentation needed to develop, maintain, and extend StorageLens. Start here to find the information you need.

---

## 📚 Quick Navigation

### For New Developers (First Time)

1. **Start Here**: [Developer Guide](developer-guide.md)
   - Project overview and architecture
   - Technology stack
   - Development environment setup
   - Common development workflows

2. **Set Up Your Environment**: [Local Development](local-development.md)
   - Step-by-step local setup
   - Database configuration
   - Service startup
   - Troubleshooting

3. **Understand Our Code**: [Coding Standards](coding-standards.md)
   - Naming conventions
   - Code organization
   - Testing patterns
   - Comment style

4. **Security First**: [SECURITY-FIXES.md](SECURITY-FIXES.md) ⭐ **NEW**
   - Understanding security vulnerabilities we fixed
   - Security patterns used in StorageLens
   - How to write secure code
   - Migration guide for credentials

### For Code Review Preparation

1. **Before Opening a PR**: [Contributing Guide](contributing.md)
   - PR requirements and checklist
   - Code review process
   - Commit message format
   - What reviewers are looking for

2. **During Code Review**: [SECURITY-PATTERNS-QUICK-REFERENCE.md](SECURITY-PATTERNS-QUICK-REFERENCE.md) ⭐ **NEW**
   - Copy-paste security code patterns
   - Authorization, email validation, file handling
   - HTTP client configuration
   - Common mistakes checklist

### For Specific Tasks

**Adding a new feature?**
- Read: Coding Standards + SECURITY-PATTERNS-QUICK-REFERENCE.md
- Follow: Phase 12.5 in [Developer Checklist](../DeveloperChecklist.md#phase-125-security-best-practices)
- Check: "Checklist for New Features" in SECURITY-FIXES.md

**Fixing a bug?**
- Read: Coding Standards
- Check: Does your fix match existing patterns?
- Follow: Contributing guide for PR format

**Deploying to production?**
- Read: [Deployment Guide](../operations/deployment-guide.md)
- Check: SECURITY-FIXES.md §Credentials Management
- Verify: All security checklist items complete

**Questions about security?**
- Read: SECURITY-FIXES.md (comprehensive reference)
- See: SECURITY-PATTERNS-QUICK-REFERENCE.md (code examples)
- Check: FAQ at bottom of SECURITY-FIXES.md

---

## 📋 Documentation Files

### Core Development Documentation

| File | Purpose | For Whom |
|------|---------|----------|
| **developer-guide.md** | Project overview, tech stack, workflows | All developers |
| **local-development.md** | Environment setup and troubleshooting | New developers |
| **coding-standards.md** | Code style, naming, organization | All developers |
| **contributing.md** | PR process, review checklist, CI/CD | Contributors |

### Security Documentation ⭐ NEW - MUST READ

| File | Purpose | For Whom |
|------|---------|----------|
| **SECURITY-FIXES.md** | Comprehensive security guide with all 11 issues, fixes, and patterns | All developers |
| **SECURITY-PATTERNS-QUICK-REFERENCE.md** | Copy-paste security code patterns for common tasks | During feature development |

### Architecture & Technical

| File | Purpose | For Whom |
|------|---------|----------|
| **../architecture/microservices-architecture.md** | Service design, boundaries, interaction patterns | Architects, senior developers |
| **../architecture/data-architecture.md** | Database schema, entities, relationships | Backend developers |
| **../technical/api-specification.md** | API endpoints, request/response formats | Integration developers |
| **../technical/service-contracts.md** | Shared DTOs and models | All service developers |
| **../technical/database-schema.md** | Table definitions and relationships | Database developers |

### Operations & Deployment

| File | Purpose | For Whom |
|------|---------|----------|
| **../operations/deployment-guide.md** | Production deployment procedures | DevOps, release team |
| **../operations/docker-deployment.md** | Docker configuration and setup | DevOps, container specialists |
| **../operations/monitoring-logging.md** | Observability and logging patterns | DevOps, support engineers |

### Planning & Process

| File | Purpose | For Whom |
|------|---------|----------|
| **../DeveloperChecklist.md** | 19-phase rebuild checklist (Phase 12.5 is security!) | New developers, rebuild team |
| **../developer/documentation-guardrail.md** | Documentation requirements and CI/CD checks | Tech leads, documentation team |

---

## 🔐 Security Highlights

### Critical Security Patterns (ALL Must Know)

1. **Authorization**: Every controller needs `[Authorize]` attribute
2. **Email Validation**: Use `EmailValidation.IsValidEmail()` (RFC 5322)
3. **File Validation**: Check file size BEFORE opening stream
4. **HTTP Clients**: Set explicit timeout (30s default, 4h for long-running)
5. **Credentials**: Never hardcode - use Key Vault or environment variables

### For More Details

- **Complete Security Guide**: See [SECURITY-FIXES.md](SECURITY-FIXES.md)
- **Security Phase Checklist**: See [Phase 12.5](../DeveloperChecklist.md#phase-125-security-best-practices) in DeveloperChecklist
- **Code Examples**: See [SECURITY-PATTERNS-QUICK-REFERENCE.md](SECURITY-PATTERNS-QUICK-REFERENCE.md)

---

## 🚀 Common Workflows

### "I'm new and just got onboarded"

```
1. Read: developer-guide.md
2. Follow: local-development.md to set up environment
3. Read: coding-standards.md
4. Read: SECURITY-FIXES.md (must read for security awareness)
5. Do: Run the build and tests (`dotnet build`, `dotnet test`)
6. Pick: First issue and follow contributing.md
```

### "I'm adding a new feature"

```
1. Read: SECURITY-PATTERNS-QUICK-REFERENCE.md
2. Check: Phase 12.5 security checklist in DeveloperChecklist
3. Code: Follow existing patterns in similar services
4. Test: Write unit tests
5. Submit: Follow PR checklist in contributing.md
```

### "I'm deploying to production"

```
1. Verify: All code passes security checklist (phase 12.5)
2. Check: Credentials are in Key Vault (SECURITY-FIXES.md §P0)
3. Verify: HTTP clients have timeouts (SECURITY-FIXES.md §P2)
4. Follow: deployment-guide.md procedures
5. Monitor: After deployment using monitoring-logging.md
```

### "There's a security question"

```
1. First: Search SECURITY-FIXES.md (comprehensive reference)
2. Then: Check SECURITY-PATTERNS-QUICK-REFERENCE.md for examples
3. FAQ: Check SECURITY-FIXES.md §FAQ section
4. Stuck?: Open issue with `security` label on GitHub
```

---

## 🛠️ Development Tools & Setup

### Required Tools

- **.NET 8 SDK** - [Install](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** - Local instance or Azure SQL
- **Docker Desktop** - For local development environment
- **Git** - For version control
- **Visual Studio Code** - Recommended editor with C# extensions

### VS Code Extensions

- **C# Dev Kit** - Required for .NET development
- **Docker** - Container management
- **GitHub Copilot** - Optional but recommended
- **Gitlens** - Git integration
- **SQL Server (mssql)** - Database exploration

### Setup Quick Links

- **Full Setup**: See [local-development.md](local-development.md)
- **Docker Setup**: See [../operations/docker-deployment.md](../operations/docker-deployment.md)
- **Database Setup**: See [../technical/database-schema.md](../technical/database-schema.md)

---

## 📖 Frequently Needed References

### Code Organization

**Where are the security utilities?**
→ `src/StorageLens.Shared.Infrastructure/Security/`

**Where are the service implementations?**
→ `src/StorageLens.Services.{ServiceName}/`

**Where are the unit tests?**
→ `tests/StorageLens.Services.{ServiceName}.Tests/`

**Where are the database models?**
→ `src/StorageLens.Services.{ServiceName}/Data/`

### Documentation Organization

**Architecture decisions?**
→ `docs/architecture/`

**How to deploy?**
→ `docs/operations/`

**API specifications?**
→ `docs/technical/`

**Build/setup process?**
→ `docs/developer/` or `docs/DeveloperChecklist.md`

---

## 🎓 Learning Paths

### For Backend Developers

1. Developer Guide
2. Coding Standards
3. Microservices Architecture
4. Data Architecture
5. API Specification
6. **[NEW] SECURITY-FIXES.md**
7. Pick a service and explore its code

### For Frontend Developers

1. Developer Guide
2. Local Development
3. Coding Standards
4. Frontend page in dashboard-guide.md
5. **[NEW] SECURITY-PATTERNS-QUICK-REFERENCE.md** (for API calls)
6. Contributing Guide

### For DevOps/Infrastructure

1. Developer Guide
2. Deployment Guide
3. Docker Deployment
4. Infrastructure Architecture
5. Monitoring & Logging
6. **[NEW] SECURITY-FIXES.md §P0 Credentials**

### For New Tech Lead/Reviewer

1. Microservices Architecture
2. Developer Guide
3. Contributing Guide
4. **[NEW] SECURITY-FIXES.md** (complete reference)
5. **[NEW] Phase 12.5** in DeveloperChecklist
6. Documentation Guardrail
7. All other architecture documents

---

## ✅ Quality Checklist

Before submitting a PR, verify:

- [ ] Code follows coding-standards.md
- [ ] All security items in Phase 12.5 checklist are met
- [ ] Unit tests written and passing
- [ ] PR follows contributing.md format
- [ ] CHANGELOG.md updated if needed
- [ ] Documentation updated if API changed

---

## 🆘 Getting Help

### Issues or Questions?

1. **Search** existing documentation (this directory)
2. **Check** SECURITY-FIXES.md FAQ for security questions
3. **Look** at similar implementations in the codebase
4. **Ask** in team chat (include relevant files/line numbers)
5. **Open** GitHub issue if it's reproducible bug

### Security Concerns?

→ Open **private** GitHub issue with `security` label  
→ Include code context and potential impact  
→ Don't include credentials or PII

### Documentation Issues?

→ File issue with `documentation` label  
→ Suggest improvements in contributing.md discussion thread

---

## 📝 Document Change Log

| Date | Change | Document |
|------|--------|----------|
| 2026-03-19 | Created SECURITY-FIXES.md | ⭐ |
| 2026-03-19 | Created SECURITY-PATTERNS-QUICK-REFERENCE.md | ⭐ |
| 2026-03-19 | Added Phase 12.5 to DeveloperChecklist | ⭐ |
| 2026-03-19 | Updated CHANGELOG.md with security section | ⭐ |
| 2026-03-19 | Created this README.md | ⭐ |

**Last Updated**: March 19, 2026

---

## 🎯 Next Steps

1. **New Developers**: Start with [Developer Guide](developer-guide.md)
2. **All Developers**: Must read [SECURITY-FIXES.md](SECURITY-FIXES.md)
3. **Before Coding**: Reference [SECURITY-PATTERNS-QUICK-REFERENCE.md](SECURITY-PATTERNS-QUICK-REFERENCE.md)
4. **Before PR**: Review [Phase 12.5](../DeveloperChecklist.md#phase-125-security-best-practices) security checklist

**Questions?** Check this README, then the specific document, then ask in team chat.

---

**Welcome to the team! 🚀**
