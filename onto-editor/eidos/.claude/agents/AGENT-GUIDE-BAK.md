# Agent-Based Application Development Guide

## Overview

This guide provides a structured approach to building applications using specialized AI agents, from initial requirements gathering through production deployment. Each phase leverages specific agents to ensure comprehensive coverage of all aspects of application development.

## Core Principles

1. **Requirements First**: Never start coding without clear, documented requirements
2. **Architecture Before Implementation**: Design the system before building it
3. **Document As You Go**: Maintain the development ledger continuously
4. **Agent Specialization**: Use the right agent for each task
5. **Iterative Refinement**: Refine requirements and architecture based on discoveries
6. **Quality Gates**: Each phase has clear completion criteria
7. **Traceability**: Every implementation decision traces back to a requirement

## Development Ledger

The **Development Ledger** is the single source of truth for your project's evolution. It must be actively maintained throughout development.

### Ledger Location

```
/docs/DEVELOPMENT_LEDGER.md
```

### Ledger Structure

```markdown
# Development Ledger

## Project Overview
[Brief project description, last updated date]

## Current Status
**Phase**: [Requirements/Architecture/Development/Testing/Deployment]
**Sprint**: [Current sprint number if applicable]
**Last Updated**: [Date]
**Active Work**: [What's currently in progress]

## Decision Log
Track all major decisions, alternatives considered, and rationale.

### [Date] - Decision Title
- **Context**: Why this decision was needed
- **Decision**: What was decided
- **Alternatives**: What else was considered
- **Consequences**: Impact of this decision
- **Status**: Active/Superseded/Deprecated
- **Related ADR**: Link to formal ADR if created

## Feature Log
Track features from conception through completion.

### Feature: [Feature Name]
- **Status**: Planned/In Progress/Completed/Deferred
- **Requirements**: Links to requirement documents
- **Architecture**: Links to architecture decisions/diagrams
- **Implementation**: Links to PRs, commits, code locations
- **Testing**: Test coverage, test results
- **Documentation**: Links to user docs, API docs, runbooks
- **Deployment**: Deployment status, environments
- **Notes**: Important considerations, blockers, dependencies

## Technical Debt Log
Track intentional compromises and future improvements.

### [Date] - Debt Item
- **Description**: What was compromised
- **Reason**: Why this compromise was made
- **Impact**: Current impact/limitations
- **Remediation Plan**: How to address this later
- **Priority**: High/Medium/Low
- **Status**: Open/In Progress/Resolved

## Architecture Evolution
Track how architecture changes over time.

### [Date] - Architecture Change
- **Component**: What changed
- **Change**: Description of the change
- **Reason**: Why the change was made
- **Migration**: Migration strategy if applicable
- **Impact**: Services/features affected

## Integration Log
Track external integrations and their status.

### Integration: [System Name]
- **Purpose**: Why we integrate with this system
- **Type**: REST API/gRPC/Message Queue/Database/etc.
- **Authentication**: Auth method and credential location
- **Endpoints**: Key endpoints/operations used
- **Dependencies**: What depends on this integration
- **Status**: Active/Deprecated/Planned
- **Health**: Current status, known issues
- **Documentation**: Link to integration docs

## Environment Log
Track environment configurations and differences.

### Environment: [Environment Name]
- **Purpose**: Development/Testing/Staging/Production
- **URL**: Environment URL
- **Configuration**: Key configuration differences
- **Database**: Database details (non-sensitive)
- **Integrations**: Which integrations are active
- **Deployed Version**: Current version deployed
- **Last Deployment**: Date and result
- **Known Issues**: Current known issues in this environment

## Incident Log
Track production incidents and resolutions.

### [Date] - Incident Title
- **Severity**: Critical/High/Medium/Low
- **Impact**: What was affected
- **Root Cause**: What caused the incident
- **Resolution**: How it was resolved
- **Prevention**: Steps to prevent recurrence
- **Post-Mortem**: Link to detailed post-mortem

## Meeting Notes & Key Discussions
Track important meetings and decisions.

### [Date] - Meeting Title
- **Attendees**: Who was present
- **Purpose**: Why the meeting was held
- **Key Decisions**: What was decided
- **Action Items**: Who is doing what by when
- **Follow-up**: Next steps

## Metrics & KPIs
Track important project metrics over time.

### Development Metrics
- **Velocity**: Story points completed per sprint
- **Cycle Time**: Average time from start to deployment
- **Bug Rate**: Bugs per feature/sprint
- **Code Coverage**: Test coverage percentage

### Production Metrics
- **Uptime**: Current month uptime percentage
- **Performance**: Key performance indicators
- **Error Rate**: Error rate trends
- **User Adoption**: Usage metrics if applicable

## Knowledge Base
Quick reference for team members.

### Key Contacts
- **Product Owner**: [Name, contact]
- **Tech Lead**: [Name, contact]
- **DevOps**: [Name, contact]
- **Security**: [Name, contact]

### Quick Links
- **Repository**: [URL]
- **CI/CD**: [URL]
- **Monitoring**: [URL]
- **Documentation**: [URL]
- **Backlog**: [URL]

### Conventions
- **Branch Naming**: [Convention]
- **Commit Messages**: [Convention]
- **PR Process**: [Brief description]
- **Code Review**: [Guidelines]
```

## Phase-Based Development Process

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

### Decision Log
[Date] - Requirements Gathering Complete
- Context: Initial requirements gathering for [project]
- Decision: [Key decisions made during requirements]
- Stakeholders: [Who was involved]
- Status: Complete

### Feature Log
[List all identified features with "Planned" status]

### Environment Log
[Document required environments based on requirements]
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
- `/docs/architecture/diagrams/system-context.mmd` (C4 Context diagram in Mermaid)
- `/docs/architecture/diagrams/container-diagram.mmd` (C4 Container diagram)
- `/docs/architecture/API_DESIGN.md`
- `/docs/architecture/COMMUNICATION_PATTERNS.md`

#### 2.2 Data Architecture Design

Use `dotnet-data-specialist` to design data layer:

```markdown
Prompt Template:
"Based on this system architecture [paste architecture], design the data layer:
- Entity model design with Entity Framework Core
- Database schema considerations (defer detailed schema to database-architect)
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

Use `documentation-specialist` to create ADRs:

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

### Decision Log
[Date] - Technology Stack Selected
- Context: [Why these technologies were considered]
- Decision: [Technologies chosen with versions]
- Alternatives: [Other options considered]
- Consequences: [Impact on development, performance, cost]
- Status: Active
- Related ADR: ADR-001

[Date] - Architecture Pattern Selected
- Context: [Why this pattern was needed]
- Decision: [Pattern chosen, e.g., microservices, CQRS]
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
"Create comprehensive repository documentation for a [.NET/Blazor] project:
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

```markdown
Content should include:
- Prerequisites (SDK versions, tools, services)
- Step-by-step setup instructions
- Configuration requirements
- Database setup (local development)
- Running the application locally
- Running tests
- Debugging tips
- Troubleshooting common issues
```

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
  - Code quality gates (SonarQube if available)
  - Package artifacts
- Release pipeline with:
  - Multi-stage deployment (Dev -> Test -> Staging -> Production)
  - Approval gates for Staging and Production
  - Automated smoke tests post-deployment
  - ServiceNow change request creation for Production
  - Rollback procedures
  - Deployment notifications (Teams/email)

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
- Status: Active

Environment: Staging
- Purpose: Pre-production validation
- URL: https://app-staging.azurewebsites.net
- Configuration: Azure App Configuration
- Database: Azure SQL Database (Staging)
- Integrations: Production integrations
- Status: Active

Environment: Production
- Purpose: Live production environment
- URL: https://app.contoso.com
- Configuration: Azure App Configuration
- Database: Azure SQL Database (Production)
- Integrations: All production integrations
- Status: Pending deployment

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

For each feature, create a feature plan:

**Feature Plan Template** (`/docs/features/[FEATURE-NAME]-plan.md`):

```markdown
# Feature: [Feature Name]

## Status
Planned | In Progress | In Review | Completed

## Overview
[Brief description of the feature]

## Requirements
- Link to user stories: [link]
- Link to acceptance criteria: [link]
- Non-functional requirements: [performance, security, accessibility]

## Architecture
- Services involved: [list]
- API endpoints: [list new/modified endpoints]
- Database changes: [list new/modified entities]
- Integration points: [external systems]
- Related ADRs: [links]

## Design Decisions
### Decision 1: [Title]
- Context: [why decision needed]
- Decision: [what was decided]
- Alternatives: [what else considered]
- Rationale: [why this choice]

## Implementation Plan
### Backend Tasks
- [ ] Task 1: [description] - Assigned to [name] - Estimate: [hours]
- [ ] Task 2: [description] - Assigned to [name] - Estimate: [hours]

### Frontend Tasks (if applicable)
- [ ] Task 1: [description] - Assigned to [name] - Estimate: [hours]

### Testing Tasks
- [ ] Unit tests for [component]
- [ ] Integration tests for [workflow]
- [ ] E2E tests for [user journey]

### Documentation Tasks
- [ ] API documentation
- [ ] User documentation
- [ ] Operations runbook (if needed)

## Technical Specifications
### API Endpoints
[Detailed endpoint specifications or links to OpenAPI]

### Data Model Changes
[Entity changes, migrations]

### UI Components (if applicable)
[Component descriptions, mockups]

## Dependencies
- External: [external dependencies]
- Internal: [other features, services]
- Blockers: [anything blocking progress]

## Testing Strategy
- Unit test coverage target: [percentage]
- Integration test scenarios: [list]
- E2E test scenarios: [list]
- Performance testing: [requirements]
- Security testing: [requirements]
- Accessibility testing: [requirements]

## Deployment Plan
- Database migrations: [migration strategy]
- Configuration changes: [list]
- Feature flags: [flag strategy]
- Rollout strategy: [gradual, all at once, etc.]
- Rollback plan: [how to rollback]

## Documentation Requirements
- [ ] API documentation updated
- [ ] User guide updated
- [ ] Operations runbook created/updated
- [ ] Architecture documentation updated
- [ ] Development ledger updated

## Risks & Mitigation
### Risk 1: [Title]
- Probability: High/Medium/Low
- Impact: High/Medium/Low
- Mitigation: [mitigation strategy]

## Success Criteria
- [ ] All acceptance criteria met
- [ ] All tests passing with required coverage
- [ ] Code review completed and approved
- [ ] Documentation completed
- [ ] Deployed to Test environment
- [ ] QA sign-off obtained
- [ ] Operations team trained (if needed)

## Timeline
- Planned Start: [date]
- Planned Completion: [date]
- Actual Start: [date]
- Actual Completion: [date]
```

#### 4.2 Feature Implementation Loop

For each feature, follow this loop:

**Step 1: Plan** → **Step 2: Implement** → **Step 3: Document** → **Step 4: Review** → **Step 5: Deploy** → **Step 6: Update Ledger**

##### Step 1: Plan

Create feature plan (above) using appropriate agents:

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

Prompt Template for each:
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

**Code Review Checklist** (`/.github/PULL_REQUEST_TEMPLATE.md` should include):

```markdown
## Feature: [Feature Name]

## Changes
- [ ] Backend changes
- [ ] Frontend changes
- [ ] Database changes (migration included)
- [ ] Configuration changes
- [ ] Documentation updates

## Testing
- [ ] Unit tests added/updated (coverage: __%)
- [ ] Integration tests added/updated
- [ ] E2E tests added/updated (if applicable)
- [ ] Manual testing completed
- [ ] Accessibility tested (if UI changes)
- [ ] Performance tested (if applicable)

## Security
- [ ] Input validation implemented
- [ ] Authentication/authorization verified
- [ ] Sensitive data protected
- [ ] Security testing completed
- [ ] No secrets in code

## Quality
- [ ] Code follows project conventions
- [ ] No compiler warnings
- [ ] Static analysis passed
- [ ] Code review completed
- [ ] All feedback addressed

## Documentation
- [ ] Code documented (XML docs, comments)
- [ ] API documentation updated
- [ ] User documentation updated
- [ ] Operations documentation updated (if needed)
- [ ] Architecture documentation updated (if needed)
- [ ] Feature plan updated with completion status

## Deployment
- [ ] Database migration tested
- [ ] Configuration documented
- [ ] Deployment plan reviewed
- [ ] Rollback plan documented
- [ ] Feature flag configured (if applicable)

## Development Ledger
- [ ] Development ledger updated with feature completion

## Related Items
- User Story: [link]
- Feature Plan: [link]
- ADRs: [links if applicable]

## Reviewers
@[reviewer1] @[reviewer2]
```

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
   - Monitor alerts and metrics

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

### Integration Log (if new integrations added)
Integration: [System Name]
- Purpose: [why integrated]
- Type: [API type]
- Authentication: [auth method]
- Feature: [Feature Name]
- Documentation: [link]
- Status: Active

### Metrics Update
Development Metrics:
- Features Completed: [count]
- Average Feature Cycle Time: [days]
- Current Sprint Velocity: [points]
- Test Coverage: [percentage]
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
- Tools: WebApplicationFactory, TestContainers, integration test projects

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

**Bug Report Template** (in issue tracker):

```markdown
## Bug Report

### Environment
- [ ] Development
- [ ] Test
- [ ] Staging
- [ ] Production

### Severity
- [ ] Critical (Production down, data loss)
- [ ] High (Major feature broken)
- [ ] Medium (Feature partially broken)
- [ ] Low (Minor issue, cosmetic)

### Description
[Clear description of the bug]

### Steps to Reproduce
1. [Step 1]
2. [Step 2]
3. [Step 3]

### Expected Behavior
[What should happen]

### Actual Behavior
[What actually happens]

### Screenshots/Logs
[Attach relevant screenshots or log snippets]

### Impact
- Users affected: [All/Specific role/Single user]
- Business impact: [Description]
- Workaround available: [Yes/No - description]

### Technical Details
- Browser/Device: [if applicable]
- Error logs: [link to Application Insights query]
- Correlation ID: [if available]

### Related Items
- Feature: [feature name]
- PR: [PR link if recently deployed]
```

**Bug Resolution Process**:

1. **Triage**: Assign severity and priority
2. **Investigation**: Use appropriate agent to analyze

   ```markdown
   "Investigate this bug: [paste bug report]
   Analyze the code in [component/service] and identify:
   - Root cause
   - Affected areas
   - Potential fix approaches
   - Risk of fix
   - Test cases to prevent regression"
   ```

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

### Quality Metrics
- Unit Tests: [count] tests, [percentage] coverage
- Integration Tests: [count] tests
- E2E Tests: [count] tests
- Performance Tests: All passing, [response time]ms average
- Security Tests: [vulnerabilities found/resolved]
- Accessibility Tests: WCAG [level] compliant

### Bug Log (sample, track separately too)
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

#### 6.1 Production Readiness Checklist

Use `supportability-lifecycle-specialist`:

```markdown
Prompt Template:
"Create a comprehensive production readiness checklist for:
- Infrastructure validation
- Security hardening
- Monitoring and alerting configuration
- Backup and DR procedures
- Performance baseline establishment
- Capacity planning validation
- Support team readiness
- Documentation completeness
- Incident response procedures
- Rollback procedures
- Communication plans"
```

**Production Readiness Checklist** (`/docs/operations/PRODUCTION_READINESS_CHECKLIST.md`):

```markdown
# Production Readiness Checklist

## Infrastructure
- [ ] Production environment provisioned per architecture
- [ ] All Azure resources created and configured
- [ ] Networking and security groups configured
- [ ] SSL/TLS certificates installed and valid
- [ ] DNS configured and tested
- [ ] CDN configured (if applicable)
- [ ] Load balancer configured
- [ ] Auto-scaling rules configured and tested
- [ ] Backup configuration validated
- [ ] DR site configured (if applicable)

## Database
- [ ] Production database provisioned with appropriate tier
- [ ] All migrations tested and ready
- [ ] Backup configuration validated
- [ ] Point-in-time restore tested
- [ ] Connection strings configured in Key Vault
- [ ] Database performance baseline established
- [ ] Indexes reviewed and optimized
- [ ] Database monitoring configured

## Security
- [ ] All secrets in Key Vault, none in code/config
- [ ] Managed identities configured for Azure services
- [ ] RBAC configured for all resources
- [ ] Security scanning completed (no critical/high issues)
- [ ] Penetration testing completed (if required)
- [ ] WAF configured (if applicable)
- [ ] DDoS protection enabled
- [ ] Security headers configured
- [ ] Authentication tested with production identity provider
- [ ] Authorization tested for all roles
- [ ] Compliance requirements validated

## Monitoring & Observability
- [ ] Application Insights configured for production
- [ ] Custom metrics and events instrumented
- [ ] Correlation IDs implemented
- [ ] Structured logging configured
- [ ] Log Analytics workspace configured
- [ ] All alert rules created and tested
- [ ] Action groups configured with correct contacts
- [ ] Dashboards created for operations team
- [ ] ServiceNow integration tested
- [ ] Health check endpoints validated
- [ ] Availability tests configured

## Performance
- [ ] Performance testing completed
- [ ] Load testing completed with production-level load
- [ ] Performance baselines documented
- [ ] Caching configured and tested
- [ ] CDN configured for static assets (if applicable)
- [ ] Database query performance validated
- [ ] API response times meeting targets
- [ ] Page load times meeting targets (if web UI)

## Documentation
- [ ] Architecture documentation complete and reviewed
- [ ] API documentation complete and published
- [ ] User documentation complete and accessible
- [ ] Operations runbooks complete
- [ ] Deployment procedures documented
- [ ] Rollback procedures documented
- [ ] Incident response procedures documented
- [ ] Disaster recovery procedures documented
- [ ] Support escalation paths documented
- [ ] Configuration documentation complete
- [ ] Development ledger up to date

## Deployment
- [ ] Deployment pipeline tested in staging
- [ ] Database migration strategy validated
- [ ] Configuration management validated
- [ ] Feature flags configured (if applicable)
- [ ] Rollback plan documented and tested
- [ ] Deployment window scheduled
- [ ] Change request created (ServiceNow)
- [ ] Stakeholder approvals obtained
- [ ] Communication plan prepared
- [ ] Post-deployment smoke tests prepared

## Support Readiness
- [ ] Support team trained on application
- [ ] Runbooks provided to support team
- [ ] Support team access provisioned
- [ ] Support dashboards configured
- [ ] Escalation procedures documented
- [ ] On-call schedule established
- [ ] Incident response procedures reviewed
- [ ] Support contact information documented
- [ ] Knowledge base articles created

## Business Continuity
- [ ] Backup procedures documented and tested
- [ ] Restore procedures documented and tested
- [ ] Disaster recovery plan documented
- [ ] RTO/RPO targets validated
- [ ] DR site failover tested (if applicable)
- [ ] Business continuity plan reviewed
- [ ] Stakeholders informed of BC procedures

## Compliance & Legal
- [ ] Privacy policy reviewed and published
- [ ] Terms of service reviewed and published
- [ ] Cookie consent implemented (if applicable)
- [ ] GDPR compliance validated (if applicable)
- [ ] Data retention policies configured
- [ ] Compliance audit completed (if required)
- [ ] Legal review completed

## Communication
- [ ] Internal stakeholders notified of launch
- [ ] Support teams notified and prepared
- [ ] User communication prepared (if applicable)
- [ ] Status page configured (if applicable)
- [ ] Incident communication templates prepared

## Sign-offs
- [ ] Technical Lead: _________________ Date: _______
- [ ] Security Team: _________________ Date: _______
- [ ] Operations Team: _______________ Date: _______
- [ ] Product Owner: _________________ Date: _______
- [ ] Executive Sponsor: _____________ Date: _______
```

#### 6.2 Operations Documentation

Use `supportability-lifecycle-specialist` and `documentation-specialist`:

**Create Operations Runbooks**:

```markdown
Prompt Template:
"Create detailed runbooks for:
1. Application deployment procedure
2. Database migration procedure
3. Scaling procedures (up and down)
4. Certificate renewal procedure
5. Common troubleshooting scenarios
6. Incident response procedures
7. Backup and restore procedures
8. Disaster recovery procedures
9. Performance investigation procedures
10. Security incident response

For each runbook include:
- Purpose
- Prerequisites
- Step-by-step procedures
- Decision points
- Validation steps
- Rollback procedures
- Escalation paths
- Related monitoring queries
- Contact information"
```

**Deliverables**:

- `/docs/operations/runbooks/DEPLOYMENT_RUNBOOK.md`
- `/docs/operations/runbooks/DATABASE_MIGRATION_RUNBOOK.md`
- `/docs/operations/runbooks/INCIDENT_RESPONSE_RUNBOOK.md`
- `/docs/operations/runbooks/TROUBLESHOOTING_GUIDE.md`
- `/docs/operations/runbooks/BACKUP_RESTORE_RUNBOOK.md`
- `/docs/operations/runbooks/DISASTER_RECOVERY_RUNBOOK.md`
- `/docs/operations/ALERT_RESPONSE_GUIDE.md`
- `/docs/operations/ESCALATION_PROCEDURES.md`

**Create SharePoint Documentation Structure**:

```markdown
Prompt Template:
"Design SharePoint site structure for application lifecycle documentation:
- Document library organization
- Folder structure for different document types
- Metadata schema for searchability
- Retention policies
- Access permissions by role
- Document templates
- Approval workflows

Include:
- Runbooks library
- Architecture documentation library
- Incident post-mortems library
- Release notes library
- Training materials library
- Contact and on-call schedules library"
```

#### 6.3 Support Team Training

**Training Plan** (`/docs/operations/SUPPORT_TRAINING_PLAN.md`):

```markdown
# Support Team Training Plan

## Training Objectives
- Understand application architecture and functionality
- Navigate monitoring dashboards and logs
- Execute runbooks for common scenarios
- Respond to incidents effectively
- Escalate appropriately

## Training Modules

### Module 1: Application Overview (2 hours)
- Business purpose and key features
- User workflows and use cases
- Architecture overview
- Technology stack
- Integration points

### Module 2: Technical Deep Dive (3 hours)
- Application components
- Data model overview
- API structure
- Authentication and authorization
- Key technical concepts

### Module 3: Monitoring & Diagnostics (3 hours)
- Application Insights navigation
- Key metrics and dashboards
- Log querying with KQL
- Correlation ID usage
- Performance investigation
- Health check interpretation

### Module 4: Incident Response (4 hours)
- Incident classification and severity
- Triage procedures
- Common issues and resolutions
- Runbook execution
- Escalation procedures
- Communication protocols
- ServiceNow workflow

### Module 5: Hands-On Scenarios (4 hours)
- Scenario 1: High error rate alert
- Scenario 2: Performance degradation
- Scenario 3: Authentication issues
- Scenario 4: Database connectivity issues
- Scenario 5: Deployment rollback

### Module 6: Tools & Access (1 hour)
- Azure Portal navigation
- Application Insights access
- ServiceNow access and usage
- SharePoint documentation access
- On-call schedule and escalation
- Contact information

## Training Materials
- Presentation slides: [link]
- Hands-on exercises: [link]
- Runbook documentation: [link]
- Video recordings: [link]
- Practice environment access

## Certification
- [ ] Completed all modules
- [ ] Passed hands-on scenarios
- [ ] Reviewed all runbooks
- [ ] Access provisioned
- [ ] Added to on-call rotation

## Trainer: [Name]
## Training Dates: [Dates]
```

#### 6.4 Final Pre-Production Activities

**Production Deployment Plan** (`/docs/operations/PRODUCTION_DEPLOYMENT_PLAN.md`):

```markdown
# Production Deployment Plan

## Deployment Overview
- **Application**: [Application Name]
- **Version**: [Version Number]
- **Deployment Date**: [Date]
- **Deployment Window**: [Start] - [End] (Timezone)
- **Expected Downtime**: [None/Duration]

## Pre-Deployment
### 1 Week Before
- [ ] Change request created in ServiceNow
- [ ] Stakeholder approvals obtained
- [ ] Deployment communications sent
- [ ] Training completed for support teams
- [ ] Production readiness checklist completed

### 1 Day Before
- [ ] Staging environment validated
- [ ] Database migration tested in staging
- [ ] Rollback procedures validated
- [ ] On-call team notified and ready
- [ ] Monitoring dashboards reviewed
- [ ] Communication templates prepared

### Day of Deployment - 2 Hours Before
- [ ] Team assembled (deployment team, support team)
- [ ] Communication channels established (Teams/Bridge)
- [ ] Final staging validation
- [ ] Backup verification
- [ ] Go/No-Go decision point

## Deployment Steps
### Step 1: Pre-Deployment Backup (15 min)
- [ ] Database backup triggered
- [ ] Backup verified
- [ ] Configuration backup taken

### Step 2: Maintenance Mode (if applicable) (5 min)
- [ ] Maintenance page activated
- [ ] User notification displayed
- [ ] Status page updated

### Step 3: Database Migration (30 min)
- [ ] Migrations executed
- [ ] Migration verification
- [ ] Data integrity checks

### Step 4: Application Deployment (20 min)
- [ ] New version deployed
- [ ] Health checks validated
- [ ] Smoke tests executed

### Step 5: Post-Deployment Validation (30 min)
- [ ] Critical workflows tested
- [ ] API endpoints validated
- [ ] Authentication validated
- [ ] Integration points tested
- [ ] Performance validated
- [ ] Error rates monitored

### Step 6: Release (10 min)
- [ ] Maintenance mode deactivated
- [ ] Traffic enabled
- [ ] Monitoring dashboards watching
- [ ] Status page updated

## Post-Deployment
### First Hour
- [ ] Monitor error rates closely
- [ ] Monitor performance metrics
- [ ] Monitor user feedback
- [ ] Support team monitoring incidents
- [ ] Hypercare mode active

### First 24 Hours
- [ ] Continued monitoring
- [ ] Review incident reports
- [ ] Address any issues immediately
- [ ] Daily status updates

### First Week
- [ ] Review production metrics
- [ ] Address any performance issues
- [ ] Collect user feedback
- [ ] Post-deployment review meeting

## Rollback Procedure
If critical issues are encountered:
1. Assess severity and impact
2. Decision to rollback (specify decision maker)
3. Execute rollback runbook
4. Restore database backup if needed
5. Validate rollback
6. Communicate to stakeholders
7. Incident post-mortem

## Rollback Criteria
- [ ] Critical functionality broken
- [ ] Data integrity compromised
- [ ] Security vulnerability introduced
- [ ] Performance degradation >50%
- [ ] Error rate >10%

## Communication Plan
### Internal Communication
- **Before**: Email to all stakeholders 1 week, 1 day, 2 hours before
- **During**: Real-time updates in Teams channel
- **After**: Deployment summary email

### External Communication (if applicable)
- **Before**: User notification 1 week before
- **During**: Status page updates
- **After**: Release notes published

## Contacts
- **Deployment Lead**: [Name, Phone, Email]
- **Technical Lead**: [Name, Phone, Email]
- **Database Administrator**: [Name, Phone, Email]
- **Support Team Lead**: [Name, Phone, Email]
- **Product Owner**: [Name, Phone, Email]
- **Executive Sponsor**: [Name, Phone, Email]

## ServiceNow Change Request
- **Change Request Number**: CHG[number]
- **Status**: [Approved]
- **Link**: [URL]

## Success Criteria
- [ ] Deployment completed within window
- [ ] All smoke tests passed
- [ ] No critical incidents in first hour
- [ ] Error rate <1%
- [ ] Performance within targets
- [ ] No rollback required

## Lessons Learned
[To be completed after deployment]
```

#### 6.5 Update Development Ledger - Final

```markdown
## Development Ledger Update - Production Ready

### Current Status
**Phase**: Production Deployment
**Last Updated**: [Date]
**Active Work**: Production deployment scheduled for [date]

### Environment Log Update
Environment: Production
- Purpose: Live production environment
- URL: https://app.contoso.com
- Configuration: Azure App Configuration
- Database: Azure SQL Database (Production)
- Integrations: All production integrations active
- Deployed Version: [pending]
- Last Deployment: [pending]
- Status: Ready for deployment

### Production Readiness
- Production Readiness Checklist: 100% complete
- Support Team Training: Completed [date]
- Operations Documentation: Complete
- Monitoring & Alerting: Configured and validated
- Deployment Plan: Approved
- Change Request: CHG[number] - Approved

### Knowledge Base Update
Quick Links:
- Production URL: [URL]
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

#### 7.1 Deployment Execution

Follow the Production Deployment Plan created in Phase 6.

**Real-Time Deployment Tracking** (in Teams/Slack channel):

```markdown
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

**One Week After Deployment** - Conduct review meeting:

```markdown
Prompt for documentation-specialist:
"Create a post-deployment review document covering:
- Deployment execution (what went well, what didn't)
- Production metrics (uptime, performance, errors)
- Incidents and resolutions
- User feedback summary
- Lessons learned
- Action items for improvement
- Next steps"
```

**Post-Deployment Review** (`/docs/operations/POST_DEPLOYMENT_REVIEW_[DATE].md`):

```markdown
# Post-Deployment Review - [Date]

## Deployment Summary
- **Deployment Date**: [Date]
- **Version Deployed**: [Version]
- **Downtime**: [None/Duration]
- **Issues During Deployment**: [None/List]
- **Rollback Required**: No

## Production Metrics (First Week)
### Availability
- **Uptime**: 99.9%
- **Downtime Incidents**: [count]
- **Impact**: [description]

### Performance
- **Average Response Time**: [X]ms (target: [Y]ms)
- **P95 Response Time**: [X]ms
- **P99 Response Time**: [X]ms
- **Page Load Time (LCP)**: [X]s (target: [Y]s)
- **Database Query Time**: [X]ms average

### Errors
- **Error Rate**: [X]% (target: <1%)
- **Total Errors**: [count]
- **Critical Errors**: [count]
- **Common Errors**: [description]

### Usage
- **Total Users**: [count]
- **Active Users**: [count]
- **API Requests**: [count]
- **Most Used Features**: [list]

## Incidents
### Incident 1: [Title]
- **Severity**: [level]
- **Occurred**: [date/time]
- **Resolved**: [date/time]
- **Duration**: [duration]
- **Root Cause**: [cause]
- **Resolution**: [resolution]
- **Prevention**: [preventive measures]

## User Feedback
### Positive Feedback
- [feedback 1]
- [feedback 2]

### Issues Reported
- [issue 1]
- [issue 2]

### Feature Requests
- [request 1]
- [request 2]

## What Went Well
- [success 1]
- [success 2]

## What Could Be Improved
- [improvement 1]
- [improvement 2]

## Lessons Learned
- [lesson 1]
- [lesson 2]

## Action Items
- [ ] [Action 1] - Assigned: [name] - Due: [date]
- [ ] [Action 2] - Assigned: [name] - Due: [date]

## Next Steps
- Continue monitoring for [timeframe]
- Address action items
- Plan for [next release/phase]
- Transition to normal support operations
```

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

### Maintenance Schedule
- Weekly: Review metrics, deploy minor updates
- Monthly: Security updates, dependency updates
- Quarterly: Major feature releases, architecture reviews
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

After successful production launch, continue using agents for:

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

**Use agents to continuously improve**:

```markdown
Use supportability-lifecycle-specialist:
"Review our production metrics from the last month: [paste metrics]
Identify:
- Performance optimization opportunities
- Monitoring gaps
- Alert improvements
- Runbook updates needed
- Support process improvements"

Use documentation-specialist:
"Review our documentation and identify:
- Outdated content
- Missing documentation
- Documentation gaps based on support tickets
- Documentation improvement opportunities"

Use dotnet-data-specialist:
"Review our database performance metrics: [paste metrics]
Identify:
- Slow queries to optimize
- Missing indexes
- Query patterns to improve
- Caching opportunities"
```

---

## Agent Usage Quick Reference

### When to Use Which Agent

| Task | Primary Agent | Secondary Agents |
|------|---------------|------------------|
| Requirements gathering | requirements-architect | documentation-specialist, supportability-lifecycle-specialist |
| System architecture design | backend-architect | dotnet-data-specialist, dotnet-security-specialist |
| API design | backend-architect | documentation-specialist |
| Data layer design | dotnet-data-specialist | backend-architect |
| Security implementation | dotnet-security-specialist | backend-architect |
| Frontend implementation | blazor-developer | blazor-accessibility-performance-specialist |
| Accessibility implementation | blazor-accessibility-performance-specialist | blazor-developer |
| Performance optimization | blazor-accessibility-performance-specialist | dotnet-data-specialist, backend-architect |
| Documentation creation | documentation-specialist | All agents for their specialty |
| Operations setup | supportability-lifecycle-specialist | documentation-specialist, dotnet-security-specialist |
| Incident response | supportability-lifecycle-specialist | Relevant technical agents |
| Code implementation | csharp-developer | dotnet-data-specialist, dotnet-security-specialist |

---

## Best Practices

### Documentation Best Practices

1. **Keep Development Ledger Current**: Update after every significant change
2. **Document Decisions**: Create ADRs for all major architectural decisions
3. **Feature Plans**: Never start implementation without a feature plan
4. **Update as You Go**: Don't defer documentation to end of sprint
5. **Link Everything**: Create traceability from requirements → architecture → code → docs
6. **Review Regularly**: Schedule regular documentation reviews
7. **Version with Code**: Keep docs in same repo as code

### Agent Usage Best Practices

1. **Right Agent for Right Task**: Use specialized agents for their expertise
2. **Provide Context**: Always provide relevant context when using agents
3. **Iterative Refinement**: Don't expect perfect output first time - refine
4. **Validate Output**: Review and validate agent suggestions
5. **Combine Agents**: Use multiple agents for complex tasks
6. **Learn Patterns**: Build a library of effective prompts
7. **Document Agent Usage**: Note which agents were used for what in ledger

### Quality Best Practices

1. **Quality Gates**: Establish clear completion criteria for each phase
2. **Test Early, Test Often**: Don't defer testing to end
3. **Code Reviews**: Every change should be reviewed
4. **Security First**: Consider security in every decision
5. **Accessibility First**: Build accessibility in, don't bolt on
6. **Performance First**: Design for performance, don't optimize later
7. **Monitor Everything**: If you can't measure it, you can't improve it

---

## Troubleshooting Common Issues

### Issue: Requirements Keep Changing

**Solution**:

- Use `requirements-architect` to identify root cause of changes
- Establish change control process
- Prioritize changes and defer non-critical
- Update development ledger with change rationale
- Create ADR for significant requirement changes

### Issue: Architecture Needs Significant Changes Mid-Project

**Solution**:

- Use `backend-architect` to assess impact
- Create ADR documenting change and rationale
- Update architecture documentation
- Assess impact on implemented features
- Update development ledger with architecture evolution
- Plan refactoring in phases if large change

### Issue: Technical Debt Accumulating

**Solution**:

- Document all technical debt in development ledger
- Prioritize debt items by impact
- Allocate percentage of each sprint to debt reduction
- Use appropriate agents to plan debt remediation
- Track debt metrics over time

### Issue: Documentation Falling Behind

**Solution**:

- Make documentation part of definition of done
- Use `documentation-specialist` to catch up
- Automate what can be automated (API docs, diagrams)
- Schedule regular documentation review sessions
- Assign documentation ownership

### Issue: Support Team Overwhelmed

**Solution**:

- Use `supportability-lifecycle-specialist` to review support processes
- Analyze common incidents for patterns
- Improve runbooks and documentation
- Add automated remediation where possible
- Improve monitoring to catch issues earlier
- Consider additional training or staffing

---

## Conclusion

This guide provides a structured, agent-based approach to application development from requirements through production. The key to success is:

1. **Following the process**: Don't skip phases or cut corners
2. **Using the right agents**: Leverage agent specialization
3. **Maintaining the development ledger**: It's your single source of truth
4. **Documenting continuously**: Document as you go, not at the end
5. **Iterating and improving**: Learn from each phase and improve

Remember: The development ledger is living documentation. Update it religiously, and it will become your project's most valuable artifact.

Good luck with your project! 🚀
