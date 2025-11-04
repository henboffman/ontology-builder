# Project Phases Guide

## Overview

This guide covers all seven phases of agent-based application development, from initial requirements through production deployment and ongoing operations.

## Phase Overview

1. **Requirements Gathering** - Understanding what to build
2. **Architecture & Design** - Designing how to build it
3. **Development Setup & Infrastructure** - Preparing the environment
4. **Feature Development** - Building the application
5. **Quality Assurance & Testing** - Ensuring quality
6. **Production Readiness** - Preparing for launch
7. **Production Deployment & Hypercare** - Going live and stabilizing

---

## Phase 1: Requirements Gathering

**Primary Agent**: `requirements-architect`

**Secondary Agents**: `documentation-specialist`, `supportability-lifecycle-specialist`

### Objectives

- Understand the business problem and objectives
- Identify all stakeholders and their needs
- Document functional and non-functional requirements
- Establish success criteria and acceptance criteria
- Identify constraints, risks, and dependencies

### Process

#### 1.1 Initial Discovery

Use `requirements-architect` to conduct comprehensive discovery:

```markdown
Prompt Template:
"I'm starting a new [type of application] project. Help me gather comprehensive 
requirements by asking me all the necessary questions about:
- Business objectives and problem statement
- Users and stakeholders
- Functional requirements
- Non-functional requirements (performance, security, scalability)
- Technical constraints
- Integration needs
- Compliance requirements
- Timeline and budget constraints

Ask me questions one section at a time, and probe deeply based on my answers."
```

#### 1.2 Document Requirements

Use `documentation-specialist` to create formal requirement documents:

**Deliverables**:

- `/docs/requirements/BUSINESS_REQUIREMENTS.md`
- `/docs/requirements/FUNCTIONAL_REQUIREMENTS.md`
- `/docs/requirements/NON_FUNCTIONAL_REQUIREMENTS.md`
- `/docs/requirements/USER_STORIES.md`
- `/docs/requirements/CONSTRAINTS_AND_ASSUMPTIONS.md`

#### 1.3 Identify Supportability Requirements

Use `supportability-lifecycle-specialist` to ensure operational requirements are captured:

```markdown
Prompt Template:
"Based on these requirements [paste requirements], help me identify:
- SLA requirements and targets
- Monitoring and alerting needs
- Support team structure and escalation needs
- Backup and disaster recovery requirements
- Compliance and audit requirements
- Documentation needs for operations
- Training requirements for support teams"
```

#### 1.4 Update Development Ledger

```markdown
## Development Ledger Update - Requirements Phase

### Current Status
**Phase**: Requirements Gathering
**Last Updated**: [Date]
**Active Work**: Gathering and documenting requirements

### Decision Log
[Date] - Requirements Gathering Complete
- Context: Initial requirements gathering for [project]
- Decision: [Key scope and approach decisions]
- Stakeholders: [Who was involved]
- Status: Complete

### Feature Log
[List all identified features with "Planned" status]

Feature: [Feature Name]
- Status: Planned
- Priority: [Critical/High/Medium/Low]
- Requirements: [Link to requirements doc]

### Environment Log
[Document required environments based on requirements]

Environment: Production
- Purpose: Live production environment
- Status: To be provisioned
```

### Completion Criteria

- [ ] All stakeholder interviews completed
- [ ] Business requirements documented and approved
- [ ] Functional requirements documented with acceptance criteria
- [ ] Non-functional requirements quantified (performance targets, SLAs, etc.)
- [ ] User stories created and prioritized
- [ ] Constraints and assumptions documented
- [ ] Risks identified and mitigation strategies proposed
- [ ] Development ledger updated with requirements phase
- [ ] Requirements reviewed and signed off by stakeholders

---

## Phase 2: Architecture & Design

**Primary Agents**: `backend-architect`, `dotnet-data-specialist`, `documentation-specialist`

**Secondary Agents**: `dotnet-security-specialist`, `blazor-accessibility-performance-specialist`, `supportability-lifecycle-specialist`

### Objectives

- Design system architecture based on requirements
- Define service boundaries and component structure
- Design data models and persistence strategy
- Define API contracts and integration patterns
- Establish security architecture
- Plan observability and monitoring strategy
- Create architecture documentation

### Process

#### 2.1 High-Level Architecture Design

Use `backend-architect` to design the overall system:

```markdown
Prompt Template:
"Based on these requirements [paste requirements summary], design a backend 
architecture that includes:
- Service boundaries and microservices decomposition (if applicable)
- API design (REST/GraphQL/gRPC)
- Inter-service communication patterns
- Authentication and authorization strategy
- Resilience patterns (circuit breakers, retries, timeouts)
- Caching strategy
- Event-driven patterns (if applicable)
- Technology stack recommendations

Consider:
- Scale: [user count, transaction volume]
- Performance: [latency requirements]
- Consistency: [consistency requirements]
- Availability: [SLA targets]"
```

**Deliverables**:

- `/docs/architecture/SYSTEM_ARCHITECTURE.md`
- `/docs/architecture/diagrams/system-context.mmd` (C4 Context diagram)
- `/docs/architecture/diagrams/container-diagram.mmd` (C4 Container diagram)
- `/docs/architecture/API_DESIGN.md`
- `/docs/architecture/COMMUNICATION_PATTERNS.md`

#### 2.2 Data Architecture Design

Use `dotnet-data-specialist` to design data layer:

```markdown
Prompt Template:
"Based on this system architecture [paste architecture], design the data layer:
- Entity model design with Entity Framework Core
- Database schema considerations
- Repository pattern implementation
- Data access patterns (CQRS if applicable)
- Caching strategies for data
- Migration strategy
- Concurrency handling
- Multi-tenancy data isolation (if applicable)

Technical constraints:
- Database: SQL Server
- ORM: Entity Framework Core
- Expected data volume: [volume]
- Performance requirements: [requirements]"
```

**Deliverables**:

- `/docs/architecture/DATA_ARCHITECTURE.md`
- `/docs/architecture/diagrams/data-flow.mmd`
- `/docs/architecture/REPOSITORY_PATTERN.md`

#### 2.3 Security Architecture

Use `dotnet-security-specialist` to design security:

```markdown
Prompt Template:
"Design comprehensive security architecture for this system [paste architecture]:
- Authentication strategy (OAuth 2.0, JWT, etc.)
- Authorization model (RBAC, ABAC, etc.)
- API security (rate limiting, CORS, CSRF)
- Data protection (encryption at rest/in transit)
- Secrets management strategy (Azure Key Vault)
- Input validation and sanitization approach
- Security monitoring and audit logging
- Compliance requirements: [list requirements]"
```

**Deliverables**:

- `/docs/architecture/SECURITY_ARCHITECTURE.md`
- `/docs/architecture/diagrams/auth-flow.mmd`
- `/docs/security/THREAT_MODEL.md`

#### 2.4 Frontend Architecture (if applicable)

Use `blazor-developer` and `blazor-accessibility-performance-specialist`:

```markdown
Prompt Template:
"Design Blazor application architecture:
- Hosting model selection (Server/WebAssembly/Hybrid/Auto)
- Component architecture and organization
- State management strategy
- API integration patterns
- Authentication/authorization integration
- Performance optimization strategy
- Accessibility requirements (WCAG [level])
- Responsive design approach"
```

**Deliverables**:

- `/docs/architecture/FRONTEND_ARCHITECTURE.md`
- `/docs/architecture/COMPONENT_DESIGN.md`
- `/docs/architecture/STATE_MANAGEMENT.md`

#### 2.5 Observability & Supportability Architecture

Use `supportability-lifecycle-specialist`:

```markdown
Prompt Template:
"Design observability and supportability architecture:
- Application Insights configuration and custom metrics
- Logging strategy (structured logging, correlation IDs)
- Monitoring and alerting strategy
- Health check design
- Azure DevOps integration for CI/CD
- ServiceNow integration for incidents
- SharePoint structure for documentation
- Deployment strategy and environments
- Backup and disaster recovery approach
- Support handoff requirements"
```

**Deliverables**:

- `/docs/architecture/OBSERVABILITY_ARCHITECTURE.md`
- `/docs/architecture/DEPLOYMENT_ARCHITECTURE.md`
- `/docs/operations/MONITORING_STRATEGY.md`
- `/docs/operations/ALERT_DEFINITIONS.md`

#### 2.6 Create Architecture Decision Records

Use `documentation-specialist` to create ADRs (see [Documentation Standards](./DOCUMENTATION_STANDARDS.md) for ADR template):

```markdown
Prompt Template:
"Create Architecture Decision Records for these key decisions:
1. [Technology choice, e.g., 'Use Blazor Server over Blazor WebAssembly']
2. [Pattern choice, e.g., 'Implement CQRS pattern']
3. [Infrastructure choice, e.g., 'Deploy to Azure App Service']

For each ADR include:
- Context and problem statement
- Decision made
- Alternatives considered with pros/cons
- Consequences (positive and negative)
- Status"
```

**Deliverables**:

- `/docs/architecture/decisions/ADR-001-[decision-title].md` (for each major decision)

#### 2.7 Update Development Ledger

```markdown
## Development Ledger Update - Architecture Phase

### Current Status
**Phase**: Architecture & Design
**Last Updated**: [Date]
**Active Work**: Architecture design and documentation

### Decision Log
[Date] - Technology Stack Selected
- Context: [Why these technologies were considered]
- Decision: .NET 8, Blazor Server, SQL Server, Azure App Service
- Alternatives: [Other options considered]
- Consequences: [Impact on development, performance, cost]
- Status: Active
- Related ADR: ADR-001

[Date] - Architecture Pattern Selected
- Context: [Why this pattern was needed]
- Decision: [Pattern chosen, e.g., layered architecture, CQRS]
- Alternatives: [Other patterns considered]
- Consequences: [Complexity, scalability, team impact]
- Status: Active
- Related ADR: ADR-002

### Architecture Evolution
[Date] - Initial Architecture Defined
- Component: Overall system architecture
- Change: Initial architecture design completed
- Reason: Starting development with clear architectural foundation
- Impact: All components defined with clear boundaries

### Feature Log Updates
[Update each feature with architecture decisions]
Feature: [Feature Name]
- Architecture: Service boundaries defined, API contracts created
- Components: [List components involved]
```

### Completion Criteria

- [ ] System architecture documented with diagrams
- [ ] Service boundaries clearly defined
- [ ] API contracts specified (OpenAPI/GraphQL schemas)
- [ ] Data architecture designed
- [ ] Security architecture documented
- [ ] Frontend architecture defined (if applicable)
- [ ] Observability and monitoring strategy documented
- [ ] All major architecture decisions captured in ADRs
- [ ] Technology stack documented with versions
- [ ] Deployment architecture defined
- [ ] Architecture reviewed by team and stakeholders
- [ ] Development ledger updated with architecture decisions

---

## Phase 3: Development Setup & Infrastructure

**Primary Agents**: `documentation-specialist`, `supportability-lifecycle-specialist`, `dotnet-security-specialist`

### Objectives

- Set up repository structure and standards
- Configure development environment
- Set up CI/CD pipelines
- Configure infrastructure and environments
- Establish development workflows

### Process

#### 3.1 Repository Setup

Use `documentation-specialist` to create repository structure and documentation:

```markdown
Prompt Template:
"Create comprehensive repository documentation for a .NET/Blazor project:
- README.md with project overview, quick start, prerequisites
- CONTRIBUTING.md with development workflow, coding standards, PR process
- CHANGELOG.md structure
- CODE_OF_CONDUCT.md
- SECURITY.md with security policy
- .github/PULL_REQUEST_TEMPLATE.md
- .github/ISSUE_TEMPLATE/ (bug, feature request, documentation)
- Repository folder structure following best practices
- .gitignore for .NET projects
- .editorconfig with C# coding standards"
```

**Repository Structure**:

```
/
├── .github/
│   ├── workflows/          # GitHub Actions or reference to Azure DevOps
│   ├── PULL_REQUEST_TEMPLATE.md
│   └── ISSUE_TEMPLATE/
├── docs/
│   ├── requirements/
│   ├── architecture/
│   │   ├── decisions/     # ADRs
│   │   └── diagrams/
│   ├── api/
│   ├── operations/        # Runbooks, monitoring
│   ├── development/       # Development guides
│   └── DEVELOPMENT_LEDGER.md
├── src/
│   ├── [ProjectName].API/
│   ├── [ProjectName].Core/
│   ├── [ProjectName].Infrastructure/
│   └── [ProjectName].Web/  # Blazor project if applicable
├── tests/
│   ├── [ProjectName].UnitTests/
│   ├── [ProjectName].IntegrationTests/
│   └── [ProjectName].E2ETests/
├── scripts/               # Build, deployment, utility scripts
├── infrastructure/        # IaC - Bicep/ARM templates
├── .editorconfig
├── .gitignore
├── README.md
├── CONTRIBUTING.md
├── CHANGELOG.md
├── LICENSE
└── SECURITY.md
```

#### 3.2 Development Environment Setup

Use `documentation-specialist` to create setup guide:

**Deliverables**:

- `/docs/development/DEVELOPMENT_SETUP.md`

Content should include:

- Prerequisites (SDK versions, tools, services)
- Step-by-step setup instructions
- Configuration requirements
- Database setup (local development)
- Running the application locally
- Running tests
- Debugging tips
- Troubleshooting common issues

#### 3.3 CI/CD Pipeline Setup

Use `supportability-lifecycle-specialist`:

```markdown
Prompt Template:
"Create Azure DevOps CI/CD pipelines for a .NET application:
- Build pipeline with:
  - Build all projects
  - Run unit tests with coverage
  - Run integration tests
  - Security scanning (dependency check)
  - Code quality gates
  - Package artifacts
- Release pipeline with:
  - Multi-stage deployment (Dev -> Test -> Staging -> Production)
  - Approval gates for Staging and Production
  - Automated smoke tests post-deployment
  - ServiceNow change request creation for Production
  - Rollback procedures
  - Deployment notifications

Include pipeline YAML and documentation."
```

**Deliverables**:

- `/azure-pipelines/build-pipeline.yml`
- `/azure-pipelines/release-pipeline.yml`
- `/docs/operations/CICD_PIPELINE.md`

#### 3.4 Infrastructure Setup

Use `supportability-lifecycle-specialist`:

```markdown
Prompt Template:
"Create infrastructure setup for Azure:
- Infrastructure as Code (Bicep/ARM templates) for:
  - App Service / Container Apps
  - SQL Database
  - Application Insights
  - Key Vault
  - Storage accounts
  - Redis Cache (if applicable)
- Environment configurations (Dev, Test, Staging, Production)
- Networking and security groups
- Managed identities for Azure services
- Backup configurations
- Monitoring and alerting setup

Include deployment scripts and documentation."
```

**Deliverables**:

- `/infrastructure/` (Bicep/ARM templates)
- `/docs/operations/INFRASTRUCTURE_SETUP.md`
- `/docs/operations/ENVIRONMENT_CONFIGURATION.md`

#### 3.5 Secrets Management Setup

Use `dotnet-security-specialist`:

```markdown
Prompt Template:
"Set up secrets management strategy:
- Azure Key Vault configuration
- Managed Identity setup for applications
- User Secrets for local development
- Environment-specific secrets organization
- Secret rotation procedures
- Access policies and RBAC
- Documentation for developers on accessing secrets

Include configuration code and documentation."
```

**Deliverables**:

- `/docs/security/SECRETS_MANAGEMENT.md`
- Configuration code for Key Vault integration

#### 3.6 Monitoring & Observability Setup

Use `supportability-lifecycle-specialist`:

```markdown
Prompt Template:
"Set up comprehensive monitoring and observability:
- Application Insights configuration with custom metrics
- Structured logging configuration (Serilog)
- Correlation ID implementation
- Alert rules and action groups
- KQL queries for common scenarios
- Dashboards for operations team
- ServiceNow integration for alert-to-incident
- Health check endpoints

Provide implementation code and configuration."
```

**Deliverables**:

- `/src/[ProjectName].Infrastructure/Observability/` (implementation code)
- `/docs/operations/OBSERVABILITY_SETUP.md`
- `/docs/operations/ALERT_RUNBOOKS.md`

#### 3.7 Update Development Ledger

```markdown
## Development Ledger Update - Setup Phase

### Current Status
**Phase**: Development Setup & Infrastructure
**Last Updated**: [Date]
**Active Work**: Repository setup, infrastructure provisioning

### Environment Log
Environment: Development
- Purpose: Local development and testing
- URL: https://localhost:5001
- Configuration: User Secrets, local SQL Server
- Database: LocalDB / SQL Server Express
- Integrations: Mock external services
- Status: Active

Environment: Test
- Purpose: Automated testing and QA
- URL: https://app-test.azurewebsites.net
- Configuration: Azure App Configuration
- Database: Azure SQL Database (Test)
- Integrations: Test environment integrations
- Status: Provisioned

Environment: Staging
- Purpose: Pre-production validation
- URL: https://app-staging.azurewebsites.net
- Configuration: Azure App Configuration
- Database: Azure SQL Database (Staging)
- Integrations: Production integrations
- Status: Provisioned

Environment: Production
- Purpose: Live production environment
- URL: https://app.contoso.com
- Configuration: Azure App Configuration
- Database: Azure SQL Database (Production)
- Integrations: All production integrations
- Status: Provisioned, pending deployment

### Knowledge Base
Quick Links:
- Repository: [URL]
- Azure DevOps: [URL]
- Azure Portal: [URL]
- Application Insights: [URL]
- Key Vault: [URL]

Conventions:
- Branch Naming: feature/[ticket]-[description], bugfix/[ticket]-[description]
- Commit Messages: Conventional Commits (feat:, fix:, docs:, etc.)
- PR Process: Minimum 1 approval, all checks pass, linked work item
```

### Completion Criteria

- [ ] Repository created with proper structure
- [ ] README and contributing guidelines documented
- [ ] Development environment setup guide complete and tested
- [ ] CI/CD pipelines configured and tested
- [ ] All environments provisioned (Dev, Test, Staging, Production)
- [ ] Infrastructure as Code committed to repository
- [ ] Secrets management configured
- [ ] Monitoring and observability configured
- [ ] Health check endpoints implemented
- [ ] Development ledger updated with environment details
- [ ] Team trained on development workflow and tools

---

## Phase 4: Feature Development

**All Agents**: Used based on feature requirements

### Objectives

- Implement features iteratively
- Maintain code quality and test coverage
- Document code and features as developed
- Update development ledger continuously

### Process

#### 4.1 Feature Planning

For each feature, create a feature plan (see [Documentation Standards](./DOCUMENTATION_STANDARDS.md) for full template):

**Feature Plan Location**: `/docs/features/[FEATURE-NAME]-plan.md`

**Key Sections**:

- Status and Overview
- Requirements and Architecture
- Design Decisions
- Implementation Plan (Backend, Frontend, Testing, Documentation tasks)
- Technical Specifications
- Dependencies and Risks
- Testing Strategy
- Deployment Plan
- Success Criteria

#### 4.2 Feature Implementation Loop

For each feature, follow this loop:

**Step 1: Plan** → **Step 2: Implement** → **Step 3: Document** → **Step 4: Review** → **Step 5: Deploy** → **Step 6: Update Ledger**

##### Step 1: Plan

Create feature plan using appropriate agents:

- `requirements-architect` for clarifying requirements
- `backend-architect` for architectural decisions
- `documentation-specialist` for plan structure

##### Step 2: Implement

**Backend Implementation**:

```markdown
Use agents in order:
1. dotnet-data-specialist - for data layer changes
2. backend-architect - for API design and service logic
3. csharp-developer - for implementation details
4. dotnet-security-specialist - for security implementation
5. supportability-lifecycle-specialist - for observability

Prompt Template:
"Implement [feature name] based on this plan: [paste feature plan]
Focus on:
- [Your specialty area]
- Code quality and best practices
- Appropriate error handling
- Logging and observability
- Security considerations
- Test coverage

Provide implementation code with explanations."
```

**Frontend Implementation** (if applicable):

```markdown
Use agents in order:
1. blazor-developer - for component implementation
2. blazor-accessibility-performance-specialist - for accessibility and performance
3. documentation-specialist - for component documentation

Prompt Template:
"Implement [feature name] UI based on this plan: [paste feature plan]
Focus on:
- Component architecture and reusability
- State management
- API integration
- Accessibility (WCAG [level])
- Performance optimization
- Responsive design

Provide implementation code with explanations."
```

##### Step 3: Document

Use `documentation-specialist`:

```markdown
Prompt Template:
"Create comprehensive documentation for [feature name]:
1. Update API documentation (OpenAPI/GraphQL schema)
2. Create/update user guide sections
3. Document new configuration options
4. Create operations runbook if needed
5. Update architecture diagrams if needed
6. Create code examples for developers

Feature details: [paste feature implementation summary]"
```

**Documentation Deliverables**:

- `/docs/api/[endpoint-documentation].md`
- `/docs/user-guide/[feature-guide].md`
- `/docs/operations/runbooks/[feature-runbook].md` (if needed)
- Code comments (XML docs, inline comments)

##### Step 4: Review

Use the PR template from [Documentation Standards](./DOCUMENTATION_STANDARDS.md) which includes:

- Changes checklist
- Testing verification
- Security verification
- Quality checks
- Documentation updates
- Development ledger update

##### Step 5: Deploy

Follow deployment process:

1. **Deploy to Test Environment**
   - Run automated tests
   - Perform smoke tests
   - QA validation

2. **Deploy to Staging Environment**
   - Full regression testing
   - Performance testing
   - Security testing
   - UAT (User Acceptance Testing)

3. **Deploy to Production**
   - Create ServiceNow change request
   - Obtain approvals
   - Execute deployment during maintenance window
   - Monitor closely post-deployment
   - Execute smoke tests

##### Step 6: Update Ledger

```markdown
## Development Ledger Update - Feature Completion

### Feature Log
Feature: [Feature Name]
- Status: Completed
- Requirements: [links]
- Architecture: [links to ADRs, architecture docs]
- Implementation: 
  - PR: [PR link]
  - Commits: [commit range]
  - Code: [src/path/to/implementation]
- Testing:
  - Unit Test Coverage: [percentage]
  - Integration Tests: [count] tests passing
  - E2E Tests: [count] tests passing
- Documentation:
  - API Docs: [link]
  - User Guide: [link]
  - Runbook: [link if applicable]
- Deployment:
  - Test: Deployed [date]
  - Staging: Deployed [date]
  - Production: Deployed [date]
  - Version: v[version]
- Notes: [any important notes, known limitations]

### Decision Log (if new decisions made)
[Date] - [Decision Title]
- Context: [context]
- Decision: [decision]
- Feature: [Feature Name]
- Status: Active

### Technical Debt Log (if debt incurred)
[Date] - [Debt Description]
- Description: [what was compromised]
- Reason: [why - often time constraints]
- Feature: [Feature Name]
- Impact: [current limitations]
- Remediation Plan: [how to fix]
- Priority: [priority]
- Status: Open
```

#### 4.3 Continuous Documentation

Throughout development, maintain:

**Daily/Weekly Updates**:

- Update Development Ledger with progress
- Document blocking issues and resolutions
- Update feature statuses
- Log important decisions

**Sprint/Iteration Boundaries**:

- Update velocity metrics
- Review and update technical debt
- Update architecture documentation if needed
- Review and update runbooks
- Conduct retrospective and document findings

### Completion Criteria (Per Feature)

- [ ] Feature plan created and approved
- [ ] Implementation completed following plan
- [ ] All tests passing with required coverage
- [ ] Code review completed and approved
- [ ] Security review completed (if applicable)
- [ ] Performance validated (if applicable)
- [ ] Accessibility validated (if UI feature)
- [ ] Documentation completed (API, user, operations)
- [ ] Deployed to Test environment
- [ ] QA testing completed and signed off
- [ ] Deployed to Staging environment
- [ ] UAT completed and signed off
- [ ] Deployed to Production
- [ ] Post-deployment monitoring shows healthy metrics
- [ ] Development ledger updated

---

## Phase 5: Quality Assurance & Testing

**Primary Agents**: All development agents for fixing issues, `documentation-specialist` for test documentation

### Objectives

- Comprehensive testing across all levels
- Performance validation
- Security validation
- Accessibility validation
- Bug fixing and refinement

### Process

#### 5.1 Test Strategy Execution

**Unit Testing**:

- Target: 80%+ code coverage
- Focus: Business logic, validation, calculations
- Tools: xUnit, NUnit, Moq, FluentAssertions

**Integration Testing**:

- Target: All API endpoints, database interactions
- Focus: Service integration, database operations
- Tools: WebApplicationFactory, TestContainers

**E2E Testing**:

- Target: Critical user journeys
- Focus: Complete workflows, UI interactions
- Tools: Playwright, Selenium, bUnit (for Blazor components)

**Performance Testing**:

Use `blazor-accessibility-performance-specialist` and `dotnet-data-specialist`:

```markdown
Prompt Template:
"Create performance tests for:
- API endpoint response times (target: < [X]ms)
- Database query performance
- Concurrent user load (target: [N] users)
- Page load times (target: LCP < [X]s, FID < [X]ms)
- Memory usage under load

Provide test scripts and acceptance criteria."
```

**Security Testing**:

Use `dotnet-security-specialist`:

```markdown
Prompt Template:
"Conduct security testing:
- OWASP Top 10 vulnerability scanning
- Authentication and authorization testing
- Input validation testing
- SQL injection testing
- XSS vulnerability testing
- CSRF protection testing
- Dependency vulnerability scanning

Provide test cases and remediation for findings."
```

**Accessibility Testing**:

Use `blazor-accessibility-performance-specialist`:

```markdown
Prompt Template:
"Conduct accessibility testing for WCAG [level]:
- Automated testing with axe-core
- Keyboard navigation testing
- Screen reader testing (NVDA/JAWS)
- Color contrast validation
- Focus management verification
- ARIA implementation validation

Provide test results and remediation plan."
```

#### 5.2 Bug Tracking & Resolution

See [Documentation Standards](./DOCUMENTATION_STANDARDS.md) for bug report template.

**Bug Resolution Process**:

1. **Triage**: Assign severity and priority
2. **Investigation**: Use appropriate agent to analyze
3. **Fix**: Implement fix with appropriate agent
4. **Test**: Ensure fix works and no regression
5. **Document**: Update development ledger
6. **Deploy**: Follow deployment process

#### 5.3 Update Development Ledger

```markdown
## Development Ledger Update - QA Phase

### Metrics & KPIs
Development Metrics:
- Bug Rate: [bugs per feature]
- Test Coverage: [percentage]
- Code Quality: [SonarQube score if applicable]

Quality Metrics:
- Unit Tests: [count] tests, [percentage] coverage
- Integration Tests: [count] tests
- E2E Tests: [count] tests
- Performance Tests: All passing, [response time]ms average
- Security Tests: [vulnerabilities found/resolved]
- Accessibility Tests: WCAG [level] compliant

### Bug Log (if tracking in ledger)
[Date] - [Bug Title] - Severity: [level]
- Description: [brief description]
- Root Cause: [cause]
- Resolution: [how fixed]
- PR: [link]
- Status: Resolved

### Technical Debt Log (if identified during testing)
[Date] - Performance Optimization Needed
- Description: [Query X] is slow under load
- Reason: Discovered during performance testing
- Impact: Response time exceeds target under high load
- Remediation Plan: Add caching, optimize query
- Priority: Medium
- Status: Open
```

### Completion Criteria

- [ ] All unit tests passing with target coverage
- [ ] All integration tests passing
- [ ] All E2E tests for critical journeys passing
- [ ] Performance tests meeting targets
- [ ] Security scan completed with no critical/high vulnerabilities
- [ ] Accessibility tests meeting WCAG target level
- [ ] All critical and high severity bugs resolved
- [ ] Medium severity bugs addressed or documented as known issues
- [ ] Test documentation completed
- [ ] Development ledger updated with QA results

---

## Phase 6: Production Readiness

**Primary Agents**: `supportability-lifecycle-specialist`, `documentation-specialist`, `dotnet-security-specialist`

### Objectives

- Ensure production infrastructure is ready
- Complete operational documentation
- Train support teams
- Establish monitoring and alerting
- Plan production deployment
- Create incident response procedures

### Process

See [Operations & Support Guide](./OPERATIONS_SUPPORT_GUIDE.md) for:

- Complete Production Readiness Checklist
- Operations runbook templates
- Support team training plan
- Deployment plan template

#### 6.1 Production Readiness Activities

1. **Infrastructure Validation**
   - All resources provisioned and configured
   - Security hardening completed
   - Backup and DR tested

2. **Documentation Completion**
   - All runbooks complete
   - Architecture documentation current
   - User documentation ready
   - API documentation published

3. **Support Readiness**
   - Support team trained
   - Access provisioned
   - Escalation procedures documented
   - On-call schedule established

4. **Monitoring & Alerting**
   - All alerts configured
   - Dashboards created
   - ServiceNow integration tested
   - Health checks validated

#### 6.2 Update Development Ledger

```markdown
## Development Ledger Update - Production Ready

### Current Status
**Phase**: Production Readiness
**Last Updated**: [Date]
**Active Work**: Final preparations for production deployment
**Next Milestone**: Production deployment on [date]

### Production Readiness
- Production Readiness Checklist: 100% complete
- Support Team Training: Completed [date]
- Operations Documentation: Complete
- Monitoring & Alerting: Configured and validated
- Deployment Plan: Approved
- Change Request: CHG[number] - Approved

### Knowledge Base Update
Quick Links:
- Production URL: [Pending deployment]
- Production App Insights: [URL]
- Production Dashboards: [URL]
- ServiceNow: [URL]
- SharePoint Docs: [URL]
- Status Page: [URL]
- On-Call Schedule: [URL]

Support Contacts:
- L1 Support: [Contact info]
- L2 Support: [Contact info]
- L3 Support: [Contact info]
- On-Call: [On-call process]
```

### Completion Criteria

- [ ] Production readiness checklist 100% complete
- [ ] All infrastructure provisioned and validated
- [ ] Security hardening completed and validated
- [ ] Monitoring and alerting fully configured
- [ ] All operations runbooks completed and reviewed
- [ ] Support team trained and certified
- [ ] Deployment plan created and approved
- [ ] Rollback procedures tested
- [ ] Change request approved
- [ ] Communication plan executed
- [ ] Post-deployment support plan established
- [ ] Development ledger finalized for launch
- [ ] All stakeholder sign-offs obtained

---

## Phase 7: Production Deployment & Hypercare

**Primary Agents**: `supportability-lifecycle-specialist`, `documentation-specialist`

### Objectives

- Execute production deployment
- Monitor closely post-deployment
- Rapidly address any issues
- Collect feedback and metrics
- Stabilize production environment

### Process

See [Operations & Support Guide](./OPERATIONS_SUPPORT_GUIDE.md) for:

- Detailed deployment procedures
- Hypercare activities
- Post-deployment review template
- Transition to BAU process

#### 7.1 Deployment Execution

Follow the Production Deployment Plan created in Phase 6.

**Real-Time Deployment Tracking** (in Teams/Slack channel):

```
[Time] ✅ Pre-deployment backup completed
[Time] ⏳ Starting database migrations
[Time] ✅ Database migrations completed successfully
[Time] ⏳ Deploying application version [X.Y.Z]
[Time] ✅ Application deployed
[Time] ⏳ Running smoke tests
[Time] ✅ Smoke tests passed
[Time] ⏳ Enabling traffic
[Time] ✅ Production is live - monitoring closely
```

#### 7.2 Hypercare Period (First 2 Weeks)

**Hypercare Activities**:

1. **Intensive Monitoring** (24/7 for first 48 hours, then business hours)
   - Monitor Application Insights dashboards continuously
   - Watch for error rate spikes
   - Monitor performance metrics
   - Track user feedback
   - Quick response to any issues

2. **Daily Stand-ups**
   - Review overnight incidents
   - Review metrics and trends
   - Address any issues
   - Plan fixes for non-critical issues

3. **Rapid Issue Resolution**
   - Prioritize production issues over new features
   - Fast-track critical fixes
   - Document all issues and resolutions

4. **User Feedback Collection**
   - Monitor support tickets
   - Collect direct user feedback
   - Track feature usage
   - Identify usability issues

#### 7.3 Post-Deployment Review

One week after deployment, conduct review meeting using template from [Operations & Support Guide](./OPERATIONS_SUPPORT_GUIDE.md).

#### 7.4 Transition to BAU (Business As Usual)

After 2 weeks of hypercare:

1. **Transition to Normal Support Operations**
   - Reduce monitoring intensity
   - Transition to standard on-call rotation
   - Move from daily to weekly reviews

2. **Update Documentation**
   - Update runbooks with lessons learned
   - Document any workarounds or known issues
   - Update troubleshooting guides

3. **Final Development Ledger Update**

```markdown
## Development Ledger Update - Production Live

### Current Status
**Phase**: Production - Business As Usual
**Last Updated**: [Date]
**Active Work**: Monitoring production, planning next iteration

### Environment Log Update
Environment: Production
- Purpose: Live production environment
- URL: https://app.contoso.com
- Deployed Version: v1.0.0
- Last Deployment: [date]
- Status: Stable and operational

### Production Metrics (First Month)
- Uptime: [X]%
- Average Response Time: [X]ms
- Error Rate: [X]%
- Total Users: [count]
- Support Tickets: [count]

### Incident Log
[Date] - [Incident Title]
- Severity: [level]
- Impact: [description]
- Root Cause: [cause]
- Resolution: [resolution]
- Prevention: [prevention]
- Post-Mortem: [link]

### Lessons Learned
- [lesson 1]
- [lesson 2]

### Next Iteration Planning
- Backlog items prioritized
- Technical debt identified
- Performance optimizations planned
- New features in planning
```

### Completion Criteria

- [ ] Production deployment successful
- [ ] 2 weeks hypercare period completed
- [ ] All critical incidents resolved
- [ ] Post-deployment review conducted
- [ ] Lessons learned documented
- [ ] Action items from review assigned
- [ ] Transition to BAU complete
- [ ] Support team operating independently
- [ ] Development ledger updated for production
- [ ] Stakeholders satisfied with launch

---

## Ongoing Maintenance & Iteration

After successful production launch, continue using agents for regular activities.

### Regular Activities

**Weekly**:

- Review production metrics and alerts
- Deploy bug fixes and minor updates
- Update documentation as needed
- Update development ledger with changes

**Monthly**:

- Security updates and dependency updates
- Review technical debt and prioritize
- Update architecture documentation
- Conduct retrospective

**Quarterly**:

- Major feature releases
- Architecture reviews
- Performance optimization initiatives
- Support process improvements

### Continuous Improvement

Use agents to continuously improve:

```markdown
Use supportability-lifecycle-specialist:
"Review our production metrics from the last month: [paste metrics]
Identify:
- Performance optimization opportunities
- Monitoring gaps
- Alert improvements
- Runbook updates needed
- Support process improvements"
```

---

[← Back to Main Guide](./README.md) | [Next: Agent Usage Guide →](./AGENT_USAGE_GUIDE.md)
