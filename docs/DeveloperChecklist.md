# StorageLens Developer Rebuild Checklist

This comprehensive checklist covers all aspects needed to rebuild the StorageLens application from scratch. Use this when setting up a fresh development environment or reconstructing the application.

## Phase 1: Prerequisites & Environment Setup

### Development Machine Setup

- [ ] **Install .NET 8 SDK**
  - Download from https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify: `dotnet --version` (should show 8.x.x)
  - Verify: `dotnet workload list`

- [ ] **Install SQL Server**
  - SQL Server 2019 or later (or SQL Server 2022 recommended)
  - SQL Server Management Studio (SSMS) optional but recommended
  - Verify SQLEXPRESS or named instance is running

- [ ] **Install Docker Desktop**
  - Windows: Docker Desktop for Windows (with WSL 2)
  - Verify: `docker --version` and `docker-compose --version`
  - Configure: Memory allocation ≥ 4GB, CPU allocation ≥ 2 cores

- [ ] **Install Node.js (for frontend tooling)**
  - Node.js 18+ LTS recommended
  - Verify: `node --version` and `npm --version`

- [ ] **Install Git**
  - Git 2.30+ recommended
  - Verify: `git --version`

- [ ] **Install Visual Studio Code**
  - Latest version recommended
  - Install extensions:
    - C# Dev Kit
    - Entity Framework Core Power Tools (optional but helpful)
    - Docker extension
    - GitHub Copilot (if using AI assistance)

- [ ] **Clone Repository**
  ```bash
  git clone https://github.com/DeeDee1103/StorageLens.git
  cd StorageLens
  ```

- [ ] **Restore Dependencies**
  ```bash
  dotnet restore StorageLens.sln
  ```

---

## Phase 2: Solution Structure & Projects

### Validate Project Organization

- [ ] **Core Projects Present**
  - [ ] `src/StorageLens.Web/` - Razor Pages frontend
  - [ ] `src/StorageLens.Shared.Contracts/` - Shared DTOs and contracts
  - [ ] `src/StorageLens.Shared.Infrastructure/` - Shared utilities, logging, middleware

- [ ] **Service Projects Present** (9 total)
  - [ ] `src/StorageLens.Services.Locations/` (port 5101)
  - [ ] `src/StorageLens.Services.ScanJobs/` (port 5102)
  - [ ] `src/StorageLens.Services.FileInventory/` (port 5103)
  - [ ] `src/StorageLens.Services.Duplicates/` (port 5104)
  - [ ] `src/StorageLens.Services.Analytics/` (port 5105)
  - [ ] `src/StorageLens.Services.Scanner/` (port 5106)
  - [ ] `src/StorageLens.Services.Hashing/` (port 5107)
  - [ ] `src/StorageLens.Services.Orchestrator/` (port 5108)

- [ ] **Test Projects Present** (1 per service + shared)
  - [ ] `tests/StorageLens.Web.Tests/`
  - [ ] `tests/StorageLens.Services.Locations.Tests/`
  - [ ] `tests/StorageLens.Services.ScanJobs.Tests/`
  - [ ] `tests/StorageLens.Services.FileInventory.Tests/`
  - [ ] `tests/StorageLens.Services.Duplicates.Tests/`
  - [ ] `tests/StorageLens.Services.Analytics.Tests/`
  - [ ] `tests/StorageLens.Services.Scanner.Tests/`
  - [ ] `tests/StorageLens.Services.Hashing.Tests/`
  - [ ] `tests/StorageLens.Services.Orchestrator.Tests/`
  - [ ] `tests/StorageLens.Shared.Infrastructure.Tests/`

- [ ] **Solution Files Present**
  - [ ] `StorageLens.sln`
  - [ ] `StorageLens.slnx`
  - [ ] `Directory.Build.props` (shared build configuration)

---

## Phase 3: Database & Infrastructure

### SQL Server Setup

- [ ] **Create Local Databases**
  - [ ] Connect to SQL Server (local or SQLEXPRESS)
  - [ ] For **each service**, create database:
    - `locations` - for Locations service
    - `scanjobs` - for ScanJobs service
    - `fileinventory` - for FileInventory service
    - `duplicates` - for Duplicates service
    - `analytics` - for Analytics service
    - `scanner` - for Scanner service
    - `hashing` - for Hashing service
    - `orchestration` - for Orchestrator service
    - `clients` - for Web client management

- [ ] **Configure SQL Server User (Local Development)**
  - Create SQL user or use `sa` account
  - Grant `db_owner` role on all databases above
  - Note connection string for local development

- [ ] **Update Connection Strings**
  - [ ] `src/StorageLens.Web/appsettings.Development.json`
  - [ ] `src/StorageLens.Services.*/appsettings.Development.json` (all 8 services)
  - Example: `Server=localhost;Database=locations;User Id=sa;Password=YourPassword;Encrypt=false;`

- [ ] **Initialize Database Schemas**
  - For each service, apply Entity Framework migrations:
  ```bash
  # Example for Locations service
  dotnet ef database update \
    --project src/StorageLens.Services.Locations \
    --startup-project src/StorageLens.Services.Locations
  ```
  - Repeat for all 8 services

### Azure Infrastructure (Bicep)

- [ ] **Review Bicep Template**
  - [ ] `infra/main.bicep` - Review structure
  - [ ] `infra/main.parameters.json` - Review parameter values

- [ ] **Update Bicep Parameters for Your Environment**
  - [ ] Resource group name
  - [ ] Location (Azure region)
  - [ ] Environment name (dev/staging/prod)
  - [ ] SQL server admin credentials
  - [ ] KeyVault configuration
  - [ ] App Service configuration (size, scaling)

---

## Phase 4: Shared Infrastructure

### Entity Framework & Data Access

- [ ] **Verify Shared DbContext Configuration**
  - [ ] Each service has own `DbContext` in its project
  - [ ] No cross-service DbContext sharing
  - [ ] All DbContexts configured in `Program.cs`

- [ ] **Verify Database Migrations**
  - [ ] Each service has `Migrations/` folder
  - [ ] Initial migration created and applied
  - [ ] All schema objects created in appropriate database

### Shared Contracts

- [ ] **Verify Contract Files Present**
  - [ ] `src/StorageLens.Shared.Contracts/Models/` contains DTOs for:
    - LocationDto, LocationCreateRequest, LocationUpdateRequest
    - ScanJobDto, ScanJobCreateRequest, ScanJobUpdateRequest
    - FileInventoryDto, FileMetadataDto
    - DuplicateGroupDto, DuplicateSummaryDto
    - AnalyticsDto, DashboardMetricsDto
    - ScannerDto, HashingDto
    - OrchestratorDto, WorkflowStateDto

- [ ] **Verify Contract Immutability**
  - All DTOs are `record` types or immutable classes
  - No mutable properties in contracts

### Shared Infrastructure

- [ ] **Verify Infrastructure Components**
  - [ ] `src/StorageLens.Shared.Infrastructure/Logging/` - Correlation ID middleware, logging setup
  - [ ] `src/StorageLens.Shared.Infrastructure/Health/` - Health check endpoints
  - [ ] `src/StorageLens.Shared.Infrastructure/Middleware/` - Exception handling, correlation ID
  - [ ] `src/StorageLens.Shared.Infrastructure/Http/` - HttpClient factories, retry policies

- [ ] **Verify Dependency Injection Setup**
  - [ ] Extension methods in `IServiceCollection` for shared services
  - [ ] Correlation ID propagation configured
  - [ ] HttpClient factories registered for cross-service communication

---

## Phase 5: Web Frontend

### Razor Pages & Layout

- [ ] **Verify Page Structure**
  - [ ] `Pages/Index.cshtml` & `Pages/Index.cshtml.cs` - Landing page
  - [ ] `Pages/Dashboard.cshtml` & `Pages/Dashboard.cshtml.cs` - Main dashboard
  - [ ] `Pages/StorageLocations/` - Location management pages
  - [ ] `Pages/ScanJobs/` - Scan job pages
  - [ ] `Pages/FileInventory/` - File listing pages
  - [ ] `Pages/Duplicates/` - Duplicate analysis pages
  - [ ] `Pages/Reports/` - Reports and analytics pages
  - [ ] `Pages/Alerts/` - Alert configuration pages
  - [ ] `Pages/ClientManagement/` - Client management pages (owner only)
  - [ ] `Pages/Shared/` - Shared layout components

- [ ] **Verify Shared Layout**
  - [ ] `Pages/Shared/_Layout.cshtml` - Main layout
  - [ ] `Pages/Shared/_Navigation.cshtml` - Navigation component
  - [ ] Bootstrap 5 CSS included
  - [ ] Bootstrap Icons included

- [ ] **Verify Script Organization**
  - [ ] `wwwroot/js/site.js` - Global initialization
  - [ ] `wwwroot/js/profile.js` - Profile page specific
  - [ ] `wwwroot/js/charts.js` - Chart initialization (Chart.js)
  - [ ] Page-specific scripts loaded via `@section Scripts`

### Frontend Dependencies

- [ ] **Verify CSS Framework**
  - [ ] Bootstrap 5 (via NuGet or CDN)
  - [ ] Bootstrap Icons included

- [ ] **Verify JavaScript Libraries**
  - [ ] Chart.js for dashboard charts
  - [ ] Verify versions in `package.json` and `*.csproj`

- [ ] **Verify Static Assets**
  - [ ] `wwwroot/css/` folder with custom styles
  - [ ] `wwwroot/js/` folder with scripts
  - [ ] `wwwroot/images/` folder with application images
  - [ ] `wwwroot/lib/` folder with third-party libraries

### Authentication & Authorization

- [ ] **Verify Authentication Setup**
  - [ ] Authentication type configured (Cookie, AAD, etc.)
  - [ ] `Startup.cs` or `Program.cs` includes `AddAuthentication()`
  - [ ] Default policy set if needed

- [ ] **Verify Authorization**
  - [ ] Role-based access control (RBAC) configured
  - [ ] Owner mode protection for sensitive pages (ClientManagement, etc.)
  - [ ] HttpOnly cookie for `sl-owner-mode` if applicable

---

## Phase 6: Service Development

### For Each of 8 Services

- [ ] **Service Structure**
  - [ ] `Controllers/` folder with API controllers
  - [ ] `Models/` folder with entity models
  - [ ] `Data/` folder with DbContext
  - [ ] `Services/` folder with business logic
  - [ ] `Program.cs` with service configuration

- [ ] **API Configuration**
  - [ ] Service listening on correct port (5101-5108)
  - [ ] Controllers inherit from `[ApiController]`
  - [ ] Proper HTTP method attributes (`[HttpGet]`, `[HttpPost]`, etc.)
  - [ ] Route attributes configured

- [ ] **Database Setup**
  - [ ] DbContext configured for service's database
  - [ ] Entity models defined
  - [ ] Migrations created and applied

- [ ] **HTTP Client Registration**
  - [ ] If service calls other services, typed `HttpClient` registered in DI
  - [ ] Retry policies configured (Polly)
  - [ ] Timeout policies configured

- [ ] **Dependency Injection**
  - [ ] Service interfaces and implementations registered
  - [ ] Shared infrastructure services registered
  - [ ] Logging configured with correlation IDs

- [ ] **Health Check Endpoint**
  - [ ] `/health` endpoint implemented
  - [ ] Returns service status and dependencies health

- [ ] **Error Handling**
  - [ ] Global exception handler middleware configured
  - [ ] Explicit error responses (not generic 500s)
  - [ ] Error logging includes correlation IDs

### Specific Service Checklist

#### Locations Service (5101)
- [ ] CRUD endpoints for storage locations
- [ ] Location status management
- [ ] Location health checks
- [ ] Database: `locations` schema with `Locations` table
- [ ] Contracts: LocationDto, LocationCreateRequest, LocationUpdateRequest

#### ScanJobs Service (5102)
- [ ] Scan job lifecycle management
- [ ] Progress tracking endpoints
- [ ] State transition tracking
- [ ] Database: `scanjobs` schema with `ScanJobs` table
- [ ] Contracts: ScanJobDto, ScanJobCreateRequest, ScanJobStatusDto

#### FileInventory Service (5103)
- [ ] File metadata storage and retrieval
- [ ] Batch file ingest endpoint
- [ ] Hash update endpoints
- [ ] Search/filter capabilities
- [ ] Database: `fileinventory` schema with `FileRecords` table
- [ ] Contracts: FileInventoryDto, FileMetadataDto, BatchIngestRequest

#### Duplicates Service (5104)
- [ ] Duplicate group calculation
- [ ] Duplicate summary statistics
- [ ] Filtering by location/file type
- [ ] Database: `duplicates` schema with `DuplicateGroups` table
- [ ] Contracts: DuplicateGroupDto, DuplicateSummaryDto

#### Analytics Service (5105)
- [ ] Dashboard metrics aggregation
- [ ] Storage growth calculations
- [ ] Reports data generation
- [ ] Caches or calculates metrics from other services
- [ ] Database: `analytics` schema with `MetricsSnapshots` table
- [ ] Contracts: DashboardMetricsDto, AnalyticsReportDto

#### Scanner Service (5106)
- [ ] File system enumeration
- [ ] Local and UNC path support
- [ ] File metadata extraction (size, modified date, permissions)
- [ ] Batch file emission to FileInventory
- [ ] Database: `scanner` schema with execution logs
- [ ] Contracts: ScannerProgressDto, FileEnumerationDto

#### Hashing Service (5107)
- [ ] File enumeration for pending hashes
- [ ] SHA-256 hash computation
- [ ] Batch hash result posting
- [ ] Progress tracking
- [ ] Database: `hashing` schema with hashing job logs
- [ ] Contracts: HashingTaskDto, HashResultDto

#### Orchestrator Service (5108)
- [ ] Workflow coordination
- [ ] Service call sequencing (Scanner → FileInventory → Hashing → Duplicates)
- [ ] Retry and resume logic
- [ ] Correlation ID propagation across workflow
- [ ] Error handling and failure categorization
- [ ] Database: `orchestration` schema with workflow state
- [ ] Contracts: WorkflowStateDto, OrchestratorCommandDto

---

## Phase 7: Testing

### Unit Tests

- [ ] **For Each Service Test Project**
  - [ ] Controller tests covering:
    - Happy path scenarios
    - Error/exception handling
    - Validation failures
    - 404/403 responses
  - [ ] Service/business logic tests:
    - Core calculations
    - State transitions
    - Error conditions
  - [ ] Data access tests:
    - CRUD operations
    - Complex queries
    - Transaction handling

- [ ] **Shared Infrastructure Tests**
  - [ ] Correlation ID middleware tests
  - [ ] Logging tests
  - [ ] Exception handler tests

- [ ] **Web Frontend Tests**
  - [ ] Page model tests
  - [ ] API client integration
  - [ ] Error handling
  - [ ] ServiceUnavailable scenarios

### Integration Tests

- [ ] **Cross-Service Tests**
  - [ ] Orchestrator calling Scanner tests
  - [ ] FileInventory receiving batch ingest tests
  - [ ] Duplicates calculating from hashes tests
  - [ ] Analytics aggregating from services tests

- [ ] **End-to-End Workflow Tests**
  - [ ] Complete scan workflow (Orchestrator → Scanner → FileInventory → Hashing → Duplicates)
  - [ ] Error scenarios (service unavailable, timeout, etc.)

### Test Execution

- [ ] **Run All Tests Locally**
  ```bash
  dotnet test tests/ --configuration Release
  ```

- [ ] **Verify Test Coverage**
  - [ ] Business logic coverage > 80%
  - [ ] Controllers coverage > 70%
  - [ ] Data access coverage > 80%

- [ ] **Fix Any Failing Tests**
  - [ ] Debug test failures
  - [ ] Update tests or code as needed
  - [ ] Ensure all tests pass

---

## Phase 8: Local Development Environment

### Docker Compose Setup

- [ ] **Review docker-compose.yml**
  - [ ] 9 services configured (web + 8 services)
  - [ ] Each service mapped to correct port (5001, 5101-5108)
  - [ ] Environment variables configured
  - [ ] Volume mounts configured for development

- [ ] **Configure Local Environment**
  - [ ] Update `docker-compose.yml` with local settings
  - [ ] Configure SQL Server connection strings for container
  - [ ] Configure service discovery/service-to-service URLs

- [ ] **Start Docker Environment**
  ```bash
  docker-compose up --build
  ```

- [ ] **Verify All Services Running**
  - [ ] Web UI accessible at `http://localhost:5001`
  - [ ] Each service's `/health` endpoint accessible:
    - http://localhost:5101/health (Locations)
    - http://localhost:5102/health (ScanJobs)
    - http://localhost:5103/health (FileInventory)
    - http://localhost:5104/health (Duplicates)
    - http://localhost:5105/health (Analytics)
    - http://localhost:5106/health (Scanner)
    - http://localhost:5107/health (Hashing)
    - http://localhost:5108/health (Orchestrator)

### RabbitMQ Setup (if used)

- [ ] **RabbitMQ in Docker Compose or Local**
  - [ ] Service running on port 5672 (AMQP)
  - [ ] Management UI on port 15672 (if using image with management)
  - [ ] Connection strings configured in services

- [ ] **Verify RabbitMQ Connection**
  - [ ] Services can connect to RabbitMQ
  - [ ] Queues created as expected
  - [ ] Message publishing/consumption working

---

## Phase 9: Build & Compilation

### Full Solution Build

- [ ] **Clean Solution**
  ```bash
  dotnet clean StorageLens.sln
  ```

- [ ] **Restore NuGet Packages**
  ```bash
  dotnet restore StorageLens.sln
  ```

- [ ] **Build Solution**
  ```bash
  dotnet build StorageLens.sln --configuration Release
  ```

- [ ] **Verify No Compilation Errors**
  - [ ] All projects compile successfully
  - [ ] No warnings (or acceptable warnings documented)
  - [ ] Build output shows "Build succeeded"

### Console Application Builds (if applicable)

- [ ] **Test Console App Builds**
  - [ ] Build CLI tools or utilities
  - [ ] Verify executable generation

---

## Phase 10: Documentation

### Ongoing Documentation Maintenance (Every PR)

- [ ] **Documentation updated for this change set**
  - [ ] At least one impacted `docs/` file updated when source/config/workflow/test files changed
  - [ ] `docs/CHANGELOG.md` updated with clear Added/Changed/Fixed/Removed entry
  - [ ] `docs/developer/developer-guide.md` updated when development workflow or quality gates changed
  - [ ] `docs/DeveloperChecklist.md` updated when process/checklist expectations changed

- [ ] **Ownership and cadence alignment**
  - [ ] Affected docs reviewed against `docs/developer/docs-ownership-matrix.md`
  - [ ] Review cadence preserved (monthly/quarterly/release based on ownership matrix)

### Architecture & Design Documents

- [ ] **Verify Core Documentation**
  - [ ] `docs/architecture/microservices-architecture.md` - Service boundaries and interactions
  - [ ] `docs/architecture/data-architecture.md` - Database schema overview
  - [ ] `docs/architecture/infrastructure-architecture.md` - Azure deployment architecture
  - [ ] `docs/architecture/security-architecture.md` - Security and authentication design
  - [ ] `docs/architecture/system-overview.md` - System component overview

### API & Technical Documentation

- [ ] **Verify Technical Documentation**
  - [ ] `docs/technical/api-specification.md` - All API endpoints documented
  - [ ] `docs/technical/service-contracts.md` - Shared DTOs and contracts documented
  - [ ] `docs/technical/database-schema.md` - Data model documentation
  - [ ] `docs/technical/technology-stack.md` - Technology choices and versions
  - [ ] `docs/technical/technical-specification.md` - Overall technical design

### Developer Documentation

- [ ] **Developer Guides Present**
  - [ ] `docs/developer/developer-guide.md` - Codebase overview
  - [ ] `docs/developer/coding-standards.md` - Code style and standards
  - [ ] `docs/developer/contributing.md` - Contribution workflow
  - [ ] `docs/developer/local-development.md` - Local setup guide
  - [ ] `docs/developer/copilot-agents-guide.md` - Copilot agent usage
  - [ ] `docs/developer/architecture-review-process.md` - Architecture checkpoints and review flow
  - [ ] `docs/developer/onboarding-checklist.md` - New contributor onboarding path
  - [ ] `docs/developer/docs-ownership-matrix.md` - Documentation ownership and cadence

- [ ] **Development Practices Automation Docs**
  - [ ] `.github/workflows/code-quality.yml` documented in developer docs
  - [ ] ADR template available at `docs/architecture/adr/0000-template.md`
  - [ ] `docs/developer/copilot-config-reference.md` - Copilot config maintenance

### Operational Documentation

- [ ] **Operations & Deployment**
  - [ ] `docs/operations/deployment-guide.md` - Local and Azure deployment
  - [ ] `docs/operations/docker-deployment.md` - Docker deployment steps
  - [ ] `docs/operations/scaling-guide.md` - Scaling considerations
  - [ ] `docs/operations/monitoring-logging.md` - Observability setup
  - [ ] `docs/operations/disaster-recovery.md` - Backup and recovery procedures

### User & Product Documentation

- [ ] **User Guides**
  - [ ] `docs/user-guide/user-guide.md` - Main user guide
  - [ ] `docs/user-guide/dashboard-guide.md` - Dashboard usage
  - [ ] `docs/user-guide/scanning-guide.md` - How to create and run scans
  - [ ] `docs/user-guide/duplicate-analysis-guide.md` - Duplicate analysis features

- [ ] **Product Documentation**
  - [ ] `docs/product/product-overview.md` - Product description
  - [ ] `docs/product/roadmap.md` - Feature roadmap

### Diagram Documentation

- [ ] **Workflow & Architecture Diagrams**
  - [ ] `docs/diagrams/system-architecture.md` - High-level architecture diagram
  - [ ] `docs/diagrams/deployment-architecture.md` - Deployment overview
  - [ ] `docs/diagrams/service-interactions.md` - Service communication flow
  - [ ] `docs/diagrams/scan-workflow.md` - Scan process workflow
  - [ ] `docs/diagrams/retry-resume-workflow.md` - Retry and resume logic

### Copilot Configuration Documentation

- [ ] **Copilot Configuration**
  - [ ] `.instructions.md` - Project guidance file exists
  - [ ] `.prompt.md` - Quick reference file exists
  - [ ] `.agent.md` - Agent definitions file exists
  - [ ] `SKILL.md` - Skills definitions file exists
  - [ ] `copilot-instructions.md` - Configuration file exists
  - [ ] `AGENTS.md` - Agent registry file exists

### Changelog

- [ ] **Verify CHANGELOG**
  - [ ] `docs/CHANGELOG.md` exists
  - [ ] Changelog follows Keep a Changelog format
  - [ ] Semver versioning documented

---

## Phase 11: Git & Version Control

### Repository Initialization

- [ ] **Verify Git Setup**
  - [ ] `.git/` folder present
  - [ ] Remote `origin` points to GitHub
  - [ ] Default branch is `main`
  - [ ] Development branch is `develop`

- [ ] **Create/Verify Branches**
  - [ ] `main` branch exists
  - [ ] `develop` branch exists and is set as default for development

- [ ] **Verify .gitignore**
  - [ ] `.gitignore` includes:
    - `bin/`, `obj/` directories
    - `appsettings.*.local.json` (local settings)
    - `.vs/`, `.vscode/` local settings
    - `node_modules/`
    - `*.user` files
    - Environment files (`.env`, `.env.local`)
    - Secrets and sensitive data

### GitHub Configuration

- [ ] **Branch Protection Rules**
  - [ ] `main` branch protected (no direct pushes)
  - [ ] Require PR reviews before merge
  - [ ] Require status checks to pass (build, tests, docs guard)

- [ ] **Verify CI/CD Workflows**
  - [ ] `.github/workflows/` folder present
  - [ ] `docs-guard.yml` - Documentation guard CI workflow
  - [ ] Build workflow (if configured)
  - [ ] Test workflow (if configured)

---

## Phase 12: Configuration & Secrets

### Application Settings

- [ ] **For Each Service & Web**
  - [ ] `appsettings.json` - Production defaults
  - [ ] `appsettings.Development.json` - Local development overrides
  - [ ] `appsettings.Staging.json` - Staging environment (if needed)

- [ ] **Configuration Content**
  - [ ] Database connection strings
  - [ ] Service endpoints (for service-to-service communication)
  - [ ] Logging levels
  - [ ] CORS settings
  - [ ] Authentication configuration

### Secrets Management

- [ ] **Local Development Secrets**
  - [ ] `.env.local` file (not in version control) with:
    - SQL Server password
    - API keys
    - JWT secrets
    - SMTP credentials

- [ ] **Azure KeyVault Integration (if deploying)**
  - [ ] KeyVault created in Azure
  - [ ] Secrets stored in KeyVault
  - [ ] Managed identity configured
  - [ ] Services authenticate to KeyVault

- [ ] **Verify .gitignore for Secrets**
  - [ ] Ensure no sensitive files are committed
  - [ ] `.env`, `.env.local`, `secrets.json` ignored

---

## Phase 12.5: Security Best Practices

### Authorization & Authentication

- [ ] **API Endpoint Authorization**
  - [ ] All service controllers have `[Authorize]` attribute
  - [ ] Public endpoints explicitly marked with `[AllowAnonymous]` (with documentation)
  - [ ] Service-to-service calls include `X-API-Key` header in requests
  - [ ] Review `docs/developer/SECURITY-FIXES.md` for authorization patterns

- [ ] **Input Validation**
  - [ ] Email validation uses `EmailValidation.IsValidEmail()` utility (RFC 5322 compliant)
  - [ ] File paths validated with `FileSecurityValidation.IsPathSafe()` before file operations
  - [ ] File sizes validated BEFORE opening streams: `FileSecurityValidation.ValidateFileBeforeProcessing()`
  - [ ] User input sanitized before database queries

### Secrets & Configuration

- [ ] **Credential Management**
  - [ ] No hardcoded passwords, API keys, or credentials in source code
  - [ ] Production secrets stored in Azure Key Vault (not in `appsettings.json`)
  - [ ] Local development uses environment variables or `.env.local` (never committed)
  - [ ] Use `KeyVaultIntegration` class for Key Vault access with fallback
  - [ ] Verify `.gitignore` excludes `.env`, `.env.local`, `secrets.json`

- [ ] **Configuration Practices**
  - [ ] Sensitive values loaded via `IConfiguration` injected at runtime
  - [ ] No secrets in version control history (check git log if migrating from old .env)
  - [ ] `appsettings.Example.json` or `.env.example` shows structure without secrets

### Resource Management

- [ ] **HTTP Client Timeouts**
  - [ ] All HTTP clients have explicit `Timeout` set (30 seconds default, 4 hours for long-running)
  - [ ] No `Timeout.InfiniteTimeSpan` (prevents resource exhaustion)
  - [ ] Verify in each service `Program.cs`: `client.Timeout = TimeSpan.FromSeconds(30);`

- [ ] **Database Access**
  - [ ] Large queries paginated (no loading entire result sets into memory)
  - [ ] Use `.Take()` and `.Skip()` for cursor-based pagination
  - [ ] Async/await used for all I/O operations to prevent thread starvation

### Security Utilities

- [ ] **Verify Security Utilities Available**
  - [ ] `StorageLens.Shared.Infrastructure/Security/EmailValidation.cs` accessible
  - [ ] `StorageLens.Shared.Infrastructure/Security/FileSecurityValidation.cs` accessible
  - [ ] `StorageLens.Shared.Infrastructure/Security/KeyVaultIntegration.cs` accessible
  - [ ] `StorageLens.Shared.Infrastructure/Security/RateLimitingMiddleware.cs` accessible

### Logging & Monitoring

- [ ] **Sensitive Data Protection**
  - [ ] Credentials never logged (no API keys, passwords, or PII in logs)
  - [ ] Correlation IDs included in all service-to-service calls for tracing
  - [ ] Authorization successes logged at DEBUG level
  - [ ] Authorization failures logged at WARNING level

- [ ] **Review Logging Examples**
  - [ ] Good example: `logger.LogWarning("File validation failed for {FileName}", filename);`
  - [ ] Bad example: `logger.LogError("Auth failed with API key: {ApiKey}", apiKey);` ❌

### Security Documentation

- [ ] **Read Security Documentation**
  - [ ] Complete read of `docs/developer/SECURITY-FIXES.md`
  - [ ] Understand "Checklist for New Features" section
  - [ ] Understand "Pending Implementation" items (P2, P3 priority)
  - [ ] Bookmark for future reference when implementing new features

- [ ] **Security Training**
  - [ ] Reviewed external code review feedback (11 issues, 7/10 rating)
  - [ ] Understand which issues are FIXED (P0, P1, P2 timeouts) and which are PENDING (N+1, audit trail, rate limiting, symlinks, reflection)
  - [ ] Know how to find TODO comments: grep `TODO (P2 Security)` or `TODO (P3 Security)`

---

## Phase 13: Frontend Assets & Build

### Static Assets

- [ ] **Verify Asset Organization**
  - [ ] `wwwroot/css/` - Custom stylesheets
  - [ ] `wwwroot/js/` - JavaScript files
  - [ ] `wwwroot/images/` - Application images
  - [ ] `wwwroot/lib/` - Third-party libraries (Bootstrap, jQuery, Chart.js, etc.)

- [ ] **Build Static Assets**
  - [ ] If using build tools:
    ```bash
    npm install
    npm run build
    ```
  - [ ] Verify output in `wwwroot/`

### Frontend Initialization

- [ ] **Verify Script Initialization Patterns**
  - [ ] `site.js` loads on every page for global initialization
  - [ ] Page-specific scripts load via `@section Scripts { <script> }`
  - [ ] Chart.js initialized on dashboard/reports pages
  - [ ] No mixing of concerns (global vs. page-specific)

---

## Phase 14: Deployment Preparation

### Local Deployment Testing

- [ ] **Test Local Deployment**
  - [ ] Run all services locally (Docker or IIS Express)
  - [ ] Verify all endpoints functional
  - [ ] Test complete scan workflow end-to-end
  - [ ] Verify logging and correlation IDs working

### Pre-Production Checklist

- [ ] **Security Review**
  - [ ] No hardcoded credentials
  - [ ] HTTPS enabled for all endpoints
  - [ ] CORS properly configured
  - [ ] Authentication/authorization working
  - [ ] Input validation in place

- [ ] **Performance Validation**
  - [ ] Load testing if applicable
  - [ ] Database query optimization
  - [ ] Connection pooling configured

- [ ] **Database Backup**
  - [ ] Backup strategy documented
  - [ ] Backup/restore procedures tested
  - [ ] Disaster recovery plan ready

### Bicep Infrastructure Preparation

- [ ] **Prepare Bicep Templates**
  - [ ] `infra/main.bicep` reviewed and updated
  - [ ] `infra/main.parameters.json` configured for target environment
  - [ ] Resource naming conventions followed
  - [ ] All resources properly parameterized

- [ ] **Test Bicep Deployment**
  ```bash
  az bicep build --file infra/main.bicep
  # Validate template
  az deployment group validate \
    --resource-group <rg-name> \
    --template-file infra/main.bicep \
    --parameters infra/main.parameters.json
  ```

---

## Phase 15: Verification & Testing

### Build Verification

- [ ] **All Projects Build Successfully**
  ```bash
  dotnet build StorageLens.sln --configuration Release
  ```

- [ ] **Solution Compiles Without Errors**
  - [ ] No CS errors
  - [ ] No project reference issues
  - [ ] All NuGet packages resolved

### Test Verification

- [ ] **All Tests Pass**
  ```bash
  dotnet test tests/ --configuration Release
  ```

- [ ] **No Failing Tests**
  - [ ] Unit test results green
  - [ ] Integration test results green (if applicable)
  - [ ] Code coverage acceptable

### Local Runtime Verification

- [ ] **Run Locally & Verify**
  - [ ] Web UI loads at http://localhost:5001
  - [ ] Pages render correctly
  - [ ] Navigation works
  - [ ] API calls succeed
  - [ ] Errors handled gracefully

- [ ] **Service Health Checks**
  ```bash
  curl http://localhost:5101/health  # Locations
  curl http://localhost:5102/health  # ScanJobs
  curl http://localhost:5103/health  # FileInventory
  curl http://localhost:5104/health  # Duplicates
  curl http://localhost:5105/health  # Analytics
  curl http://localhost:5106/health  # Scanner
  curl http://localhost:5107/health  # Hashing
  curl http://localhost:5108/health  # Orchestrator
  ```

- [ ] **Database Connectivity**
  - [ ] All services can connect to their respective databases
  - [ ] Schema objects present
  - [ ] Sample queries execute successfully

---

## Phase 16: Documentation Verification

### Documentation Completeness

- [ ] **All Required Documentation Present**
  - [ ] Architecture docs (5 files)
  - [ ] Technical docs (5 files)
  - [ ] Developer docs (6 files)
  - [ ] Operations docs (5 files)
  - [ ] User docs (4 files)
  - [ ] Diagrams (5 files)
  - [ ] Project health docs (`docs/operations/sli-slo-targets.md`, `docs/developer/issue-management-and-release-process.md`)
  - [ ] Dashboard docs (`docs/operations/project-health-dashboard.md`)

- [ ] **Project Health Automation Present**
  - [ ] `.github/dependabot.yml` configured for NuGet and GitHub Actions updates
  - [ ] `.github/ISSUE_TEMPLATE/` contains bug and feature templates

- [ ] **CHANGELOG Updated**
  - [ ] Current version documented
  - [ ] All major features listed
  - [ ] Breaking changes highlighted

- [ ] **README & Contributing Guide Updated**
  - [ ] `README.md` includes quick start
  - [ ] `docs/developer/contributing.md` has PR checklist
  - [ ] `docs/developer/docs-ownership-matrix.md` is present and current
  - [ ] Links to documentation correct

### Documentation Freshness (Regular Basis)

- [ ] **Monthly docs review completed**
  - [ ] Developer/process docs reviewed (`docs/developer/*`)
  - [ ] Operational docs reviewed (`docs/operations/*`)
- [ ] **Monthly review logged**
  - [ ] Entry added to `docs/developer/docs-ownership-matrix.md` under "Docs Review Log"
- [ ] **Quarterly docs review completed**
  - [ ] Architecture docs reviewed (`docs/architecture/*`)
  - [ ] API/versioning policy reviewed (`docs/technical/api-specification.md`, `docs/technical/api-versioning-strategy.md`)
- [ ] **Quarterly review logged**
  - [ ] Entry added to `docs/developer/docs-ownership-matrix.md` under "Docs Review Log"

### Documentation Links

- [ ] **Internal Links Verified**
  - [ ] Cross-document links work
  - [ ] Diagrams referenced correctly
  - [ ] External links valid

---

## Phase 17: Copilot Setup

### Copilot Configuration

- [ ] **Copilot Files Present**
  - [ ] `.instructions.md` - Main guidance
  - [ ] `.prompt.md` - Quick reference
  - [ ] `.agent.md` - Agent definitions
  - [ ] `SKILL.md` - Skill definitions
  - [ ] `copilot-instructions.md` - Configuration
  - [ ] `AGENTS.md` - Agent registry

- [ ] **Copilot Documentation**
  - [ ] `docs/developer/copilot-agents-guide.md` - How to use agents
  - [ ] `docs/developer/copilot-config-reference.md` - How to maintain config

- [ ] **Test Copilot Integration**
  - [ ] Ask Copilot about a development task
  - [ ] Verify suggestions are relevant
  - [ ] Verify red flags mentioned

---

## Phase 18: Final Validation & Sign-Off

### Comprehensive System Check

- [ ] **Build System**
  - [ ] Builds complete without errors
  - [ ] All projects compile
  - [ ] No reference conflicts

- [ ] **Test System**
  - [ ] All tests pass
  - [ ] Coverage acceptable
  - [ ] No skipped or ignored tests

- [ ] **Runtime System**
  - [ ] Web UI functional
  - [ ] All services running
  - [ ] Cross-service communication working
  - [ ] Database operations working

- [ ] **Data System**
  - [ ] All databases created
  - [ ] Schemas correctly applied
  - [ ] Sample data can be inserted
  - [ ] Queries execute successfully

- [ ] **Documentation System**
  - [ ] All docs present and accurate
  - [ ] Links validated
  - [ ] diagrams render correctly
  - [ ] CHANGELOG updated

- [ ] **Infrastructure System**
  - [ ] Docker Compose works
  - [ ] All services port-mapped correctly
  - [ ] Environment variables configured
  - [ ] Health checks passing

- [ ] **Security Setup**
  - [ ] No secrets in version control
  - [ ] Authentication configured
  - [ ] Authorization roles defined
  - [ ] Input validation present

### Sign-Off Criteria

- [ ] **Ready for Development**
  - ✅ Full build succeeds
  - ✅ All tests pass
  - ✅ Local environment running
  - ✅ Documentation complete
  - ✅ Copilot configured
  - ✅ No critical issues remaining

- [ ] **Development Team Notified**
  - [ ] Environment ready for use
  - [ ] All team members have access
  - [ ] Documentation links shared
  - [ ] Copilot usage documented

---

## Additional Resources

### Quick Command Reference

```bash
# Build Solution
dotnet build StorageLens.sln

# Run Tests
dotnet test tests/

# Start Local Environment
docker-compose up --build

# Check Service Health
curl http://localhost:5001/health    # Web
curl http://localhost:5101/health    # Locations
curl http://localhost:5102/health    # ScanJobs
# ... (continue for all 8 services)

# Apply Database Migrations
dotnet ef database update \
  --project src/StorageLens.Services.Locations \
  --startup-project src/StorageLens.Services.Locations

```

### Troubleshooting Common Issues

| Issue | Solution |
|-------|----------|
| "Cannot find database" | Verify SQL Server is running; check connection strings |
| "Port already in use" | Change port in appsettings.json or docker-compose.yml |
| "NuGet restore fails" | Clear NuGet cache: `dotnet nuget locals all --clear` |
| "Tests fail with timeout" | Increase test timeout or check service dependencies |
| "Docker compose fails" | Verify Docker Desktop is running; sufficient memory allocated |
| "Service unavailable" | Check service logs; verify database connectivity |
| "Correlation ID missing" | Verify middleware registered in Program.cs |

### Key Documentation Files

- **For Developers**: Start with `docs/developer/developer-guide.md`
- **For Architecture**: See `docs/architecture/microservices-architecture.md`
- **For Deployment**: See `docs/operations/deployment-guide.md`
- **For API Reference**: See `docs/technical/api-specification.md`
- **For Copilot Help**: See `docs/developer/copilot-agents-guide.md`

---

**Last Updated**: March 19, 2026  
**Version**: 1.0  
**Audience**: Developers rebuilding StorageLens from scratch  
**Estimated Time**: 4-8 hours depending on experience and environment setup speed
