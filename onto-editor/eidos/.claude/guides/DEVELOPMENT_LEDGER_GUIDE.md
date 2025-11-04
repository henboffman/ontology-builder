# Development Ledger Guide

## Overview

The **Development Ledger** is the single source of truth for your project's evolution. It must be actively maintained throughout development, serving as a living history of your project's decisions, features, and evolution.

## Ledger Location

```
/docs/DEVELOPMENT_LEDGER.md
```

Store this in your repository's docs folder, version controlled alongside your code.

## Why Maintain a Development Ledger?

- **Single Source of Truth**: One place to understand project status and history
- **Onboarding**: New team members can quickly understand project evolution
- **Decision Tracking**: Remember why decisions were made months later
- **Audit Trail**: Compliance and governance requirements
- **Knowledge Preservation**: Prevents knowledge loss when team members leave
- **Context for AI Agents**: Provides critical context when using agents for development

## Ledger Structure

### Current Status Section

Always keep this at the top and update it frequently:

```markdown
## Current Status
**Phase**: [Requirements/Architecture/Development/Testing/Deployment/Production]
**Sprint**: [Current sprint number if applicable]
**Last Updated**: [Date]
**Active Work**: [What's currently in progress]
**Blockers**: [Any current blockers]
**Next Milestone**: [Next major milestone and date]
```

**Update Frequency**: Daily or whenever status changes significantly

### Decision Log

Track all major decisions, alternatives considered, and rationale.

```markdown
## Decision Log

### [Date] - Decision Title
- **Context**: Why this decision was needed
- **Decision**: What was decided
- **Alternatives**: What else was considered (with pros/cons)
- **Consequences**: Impact of this decision (positive and negative)
- **Status**: Active/Superseded/Deprecated
- **Related ADR**: [Link to formal ADR if created]
- **Participants**: Who was involved in the decision
```

**When to Add**:

- Major technology choices
- Architecture pattern selections
- Significant design decisions
- Process or workflow changes
- Tool or framework selections
- Scope changes

**Examples**:

- "Use Blazor Server instead of WebAssembly"
- "Implement CQRS pattern for order processing"
- "Deploy to Azure App Service instead of AKS"
- "Use Redis for distributed caching"

### Feature Log

Track features from conception through completion.

```markdown
## Feature Log

### Feature: [Feature Name]
- **Status**: Planned/In Progress/Completed/Deferred/Cancelled
- **Priority**: Critical/High/Medium/Low
- **Started**: [Date]
- **Completed**: [Date]
- **Requirements**: [Links to requirement documents]
- **Architecture**: [Links to architecture decisions/diagrams/ADRs]
- **Implementation**: 
  - PRs: [Links to pull requests]
  - Commits: [Commit range or key commits]
  - Code Location: [src/path/to/implementation]
- **Testing**: 
  - Unit Test Coverage: [percentage]
  - Integration Tests: [count/status]
  - E2E Tests: [count/status]
  - Performance: [results]
- **Documentation**: 
  - API Docs: [link]
  - User Guide: [link]
  - Runbook: [link if applicable]
- **Deployment**: 
  - Test: Deployed [date]
  - Staging: Deployed [date]
  - Production: Deployed [date]
  - Version: v[version]
- **Dependencies**: [Other features or systems this depends on]
- **Blocked By**: [Anything blocking this feature]
- **Blocking**: [What features this blocks]
- **Notes**: [Important considerations, known limitations, workarounds]
```

**Update Frequency**: At each feature milestone (start, architecture complete, implementation complete, testing complete, deployed)

**Statuses Explained**:

- **Planned**: In backlog, not started
- **In Progress**: Active development
- **Completed**: Deployed to production and stable
- **Deferred**: Postponed to future release
- **Cancelled**: Will not be implemented

### Technical Debt Log

Track intentional compromises and future improvements.

```markdown
## Technical Debt Log

### [Date] - Debt Item Title
- **ID**: TD-[number] (for tracking)
- **Description**: What was compromised or what needs improvement
- **Reason**: Why this compromise was made (time constraints, lack of information, etc.)
- **Impact**: Current impact/limitations (performance, maintainability, user experience)
- **Area**: [Frontend/Backend/Database/Infrastructure/etc.]
- **Remediation Plan**: How to address this later
- **Estimated Effort**: [Story points or time estimate]
- **Priority**: High/Medium/Low
- **Status**: Open/In Progress/Resolved
- **Related Feature**: [Feature that introduced this debt]
- **Added By**: [Team member name]
```

**When to Add**:

- Shortcut taken to meet deadline
- Known performance issue deferred
- Incomplete error handling
- Missing tests
- Hardcoded values that should be configurable
- Copy-pasted code that should be refactored
- Incomplete documentation

**Update Frequency**: Add immediately when debt is incurred; review and update monthly

### Architecture Evolution

Track how architecture changes over time.

```markdown
## Architecture Evolution

### [Date] - Architecture Change Title
- **Component**: What part of the system changed
- **Change**: Description of the change
- **Reason**: Why the change was made
- **Migration**: Migration strategy if applicable
- **Impact**: Services/features affected
- **Effort**: Actual effort required
- **Completed**: [Date]
- **Related ADR**: [Link to ADR]
```

**When to Add**:

- Service boundary changes
- New architectural patterns introduced
- Technology stack changes
- Infrastructure changes
- Integration pattern changes

### Integration Log

Track external integrations and their status.

```markdown
## Integration Log

### Integration: [System Name]
- **Purpose**: Why we integrate with this system
- **Type**: REST API/gRPC/Message Queue/Database/File Transfer/etc.
- **Direction**: Inbound/Outbound/Bidirectional
- **Authentication**: Auth method (OAuth, API Key, mTLS, etc.)
- **Credentials Location**: [Key Vault secret name, no actual secrets]
- **Endpoints/Operations**: Key endpoints or operations used
- **Data Exchanged**: Brief description of data flow
- **Frequency**: Real-time/Batch/On-demand
- **SLA**: Response time requirements, uptime requirements
- **Dependencies**: What in our system depends on this integration
- **Status**: Active/Deprecated/Planned/Testing
- **Health**: Current status, known issues
- **Documentation**: [Link to integration docs]
- **Contact**: [External system contact information]
- **Monitoring**: [Link to dashboard or health check]
```

**Update Frequency**: Add when integration is added; update when status changes or issues occur

### Environment Log

Track environment configurations and differences.

```markdown
## Environment Log

### Environment: [Environment Name]
- **Purpose**: Development/Testing/Staging/Production
- **URL**: [Environment URL]
- **Infrastructure**: [Azure App Service, AKS, etc.]
- **Configuration Source**: [Azure App Configuration, Key Vault, etc.]
- **Database**: [Database server, database name - no connection strings]
- **Cache**: [Redis instance details]
- **Message Queue**: [Service Bus, RabbitMQ details]
- **Integrations Active**: [Which integrations are enabled]
- **Deployed Version**: [Current version deployed]
- **Last Deployment**: [Date and result]
- **Auto-Deploy**: Yes/No - [from which branch]
- **Access**: [Who has access, how to request access]
- **Known Issues**: [Current known issues in this environment]
- **Configuration Differences**: [Key differences from other environments]
```

**Update Frequency**: Update after each deployment; review monthly

### Incident Log

Track production incidents and resolutions.

```markdown
## Incident Log

### [Date Time] - Incident Title
- **Incident ID**: INC-[number] (ServiceNow or other tracking system)
- **Severity**: Critical/High/Medium/Low
- **Status**: Open/Investigating/Resolved/Closed
- **Detected By**: [Monitoring/User Report/Automated Alert]
- **Impact**: 
  - Users Affected: [count or percentage]
  - Services Affected: [list]
  - Business Impact: [description]
- **Timeline**:
  - Detected: [date time]
  - Response Started: [date time]
  - Resolved: [date time]
  - Duration: [duration]
- **Root Cause**: [What caused the incident]
- **Contributing Factors**: [What else contributed]
- **Resolution**: [How it was resolved]
- **Temporary Workaround**: [If applicable]
- **Permanent Fix**: [If different from resolution]
- **Prevention**: [Steps to prevent recurrence]
- **Action Items**: 
  - [ ] [Action 1] - Assigned: [name] - Due: [date]
  - [ ] [Action 2] - Assigned: [name] - Due: [date]
- **Post-Mortem**: [Link to detailed post-mortem document]
- **Related**: [Related incidents, features, changes]
```

**Update Frequency**: Add immediately when incident occurs; update throughout incident lifecycle; final update after post-mortem

### Meeting Notes & Key Discussions

Track important meetings and decisions.

```markdown
## Meeting Notes & Key Discussions

### [Date] - Meeting Title
- **Type**: Planning/Review/Architecture/Incident/Stakeholder
- **Attendees**: [List of attendees]
- **Purpose**: Why the meeting was held
- **Key Decisions**: 
  - [Decision 1]
  - [Decision 2]
- **Discussion Points**:
  - [Topic 1]: [Summary]
  - [Topic 2]: [Summary]
- **Action Items**: 
  - [ ] [Action 1] - Assigned: [name] - Due: [date]
  - [ ] [Action 2] - Assigned: [name] - Due: [date]
- **Follow-up**: [Next steps, next meeting date]
- **Recording**: [Link if recorded]
- **Notes**: [Link to detailed notes if separate document]
```

**When to Add**: After important meetings that result in decisions or action items

### Metrics & KPIs

Track important project metrics over time.

```markdown
## Metrics & KPIs

### Development Metrics (Updated [Date])
- **Sprint Velocity**: [Story points completed per sprint, trending]
- **Cycle Time**: [Average time from start to deployment]
- **Lead Time**: [Average time from request to deployment]
- **Deployment Frequency**: [Deployments per week/month]
- **Bug Rate**: [Bugs per feature/sprint]
- **Bug Resolution Time**: [Average time to resolve]
- **Code Coverage**: [Test coverage percentage]
- **Code Quality**: [SonarQube score, tech debt ratio]
- **PR Review Time**: [Average time for PR approval]

### Production Metrics (Updated [Date])
- **Uptime**: [Current month uptime percentage]
- **Availability SLA**: [Target vs actual]
- **Response Time**: 
  - Average: [X]ms
  - P95: [X]ms
  - P99: [X]ms
- **Error Rate**: [Errors per request, trending]
- **Throughput**: [Requests per second/minute]
- **Active Users**: [Daily/Monthly active users]
- **API Usage**: [API calls per day, by endpoint]
- **Database Performance**: [Query times, connection pool usage]
- **Cache Hit Rate**: [Percentage]
- **Resource Utilization**: [CPU, Memory, Disk usage]

### Business Metrics (Updated [Date])
- **User Adoption**: [User growth, activation rate]
- **Feature Usage**: [Most/least used features]
- **User Satisfaction**: [NPS, CSAT scores]
- **Support Tickets**: [Ticket volume, resolution time]
- **Cost Metrics**: [Infrastructure costs, cost per user]
```

**Update Frequency**: Weekly for development metrics, daily for production metrics during hypercare, weekly thereafter

### Knowledge Base

Quick reference for team members.

```markdown
## Knowledge Base

### Key Contacts
- **Product Owner**: [Name] - [Email] - [Phone]
- **Technical Lead**: [Name] - [Email] - [Phone]
- **DevOps Lead**: [Name] - [Email] - [Phone]
- **Security Lead**: [Name] - [Email] - [Phone]
- **Support Lead**: [Name] - [Email] - [Phone]
- **Database Administrator**: [Name] - [Email] - [Phone]
- **On-Call**: [How to find on-call person]

### Quick Links
- **Repository**: [GitHub/Azure DevOps URL]
- **CI/CD Pipelines**: [URL]
- **Production Environment**: [URL]
- **Staging Environment**: [URL]
- **Test Environment**: [URL]
- **Monitoring Dashboard**: [Application Insights/Grafana URL]
- **Logs**: [Log Analytics URL]
- **Backlog**: [Azure DevOps/Jira URL]
- **Documentation**: [Wiki/SharePoint URL]
- **API Documentation**: [Swagger/GraphQL Playground URL]
- **Architecture Diagrams**: [Lucidchart/Draw.io URL]

### Development Conventions
- **Branch Naming**: [Convention, e.g., feature/TICKET-description]
- **Commit Messages**: [Convention, e.g., Conventional Commits]
- **PR Process**: 
  - Minimum [N] approvals required
  - All checks must pass
  - Link to work item required
  - Update tests required
- **Code Review**: [Guidelines, checklist]
- **Definition of Done**: [Criteria for feature completion]

### Access & Permissions
- **How to Request Access**: [Process]
- **Azure Subscription**: [Subscription name, how to request]
- **Database Access**: [How to request, who approves]
- **Production Access**: [Limited to specific roles, approval process]
- **VPN Required**: Yes/No - [Setup instructions]

### Useful Commands
```bash
# Run locally
dotnet run --project src/ProjectName

# Run tests
dotnet test

# Run migrations
dotnet ef database update

# Build for production
dotnet publish -c Release
```

### Common Troubleshooting

- **Issue**: Can't connect to database locally
  - **Solution**: [Solution steps]
- **Issue**: Build fails with [error]
  - **Solution**: [Solution steps]

```

**Update Frequency**: Update as information changes; review quarterly

## Ledger Maintenance Schedule

### Daily
- Update **Current Status** if work changes significantly
- Add **Decision Log** entries for any decisions made
- Update **Feature Log** for features in active development

### After Each Significant Event
- **Sprint Planning**: Update feature priorities and statuses
- **Architecture Decision**: Add to Decision Log and Architecture Evolution
- **Deployment**: Update Environment Log
- **Incident**: Add to Incident Log
- **Integration Added**: Add to Integration Log
- **Technical Debt Incurred**: Add to Technical Debt Log

### Weekly
- Update **Metrics & KPIs** - Development section
- Review and update feature statuses
- Review open action items from meetings

### Monthly
- Update **Metrics & KPIs** - Production section (or weekly during hypercare)
- Review **Technical Debt Log** and prioritize items
- Review **Knowledge Base** for accuracy
- Archive old completed items if ledger getting too long

### Quarterly
- Comprehensive review of entire ledger
- Archive old sections to separate archive document
- Update conventions and processes based on learnings
- Review and update key contacts

## Tips for Maintaining the Ledger

### Make It a Habit
- Include ledger updates in your definition of done
- Set reminders for regular updates
- Make it part of sprint ceremonies
- Assign ownership (rotate responsibility)

### Keep It Concise
- Be brief but complete
- Link to detailed documents rather than duplicating
- Use consistent formatting
- Remove or archive outdated information

### Make It Searchable
- Use consistent terminology
- Tag items with relevant keywords
- Use markdown headers for easy navigation
- Consider using IDs for cross-referencing (ADR-001, TD-042, INC-123)

### Integrate with Tools
- Link to work items in your tracking system
- Link to PRs and commits
- Link to monitoring dashboards
- Link to ServiceNow incidents
- Use automation where possible (e.g., auto-update from deployments)

### Use It Actively
- Reference it in meetings
- Use it for onboarding
- Use it when making decisions
- Share it with stakeholders
- Provide it to AI agents for context

## Example Ledger Snippets

### Good Decision Log Entry

```markdown
### 2024-01-15 - Use Blazor Server Over Blazor WebAssembly

- **Context**: Needed to choose Blazor hosting model. Primary users are on corporate network with reliable connectivity. Performance requirements are moderate (page loads <2s). Security requirement for no client-side code.

- **Decision**: Use Blazor Server hosting model.

- **Alternatives**: 
  - Blazor WebAssembly: Rejected because of large initial download size (10MB+) and requirement to not expose business logic on client
  - Blazor Auto (.NET 8): Rejected due to complexity and team unfamiliarity with new pattern

- **Consequences**: 
  - Positive: Simpler deployment, smaller initial load, server-side code execution, easier debugging
  - Negative: Requires persistent SignalR connection, higher server resource usage, potential latency for remote users

- **Status**: Active

- **Related ADR**: ADR-003-blazor-hosting-model.md

- **Participants**: Technical Lead, Backend Architect, 3 Developers
```

### Good Feature Log Entry

```markdown
### Feature: User Authentication with Azure AD

- **Status**: Completed
- **Priority**: Critical
- **Started**: 2024-01-10
- **Completed**: 2024-01-25

- **Requirements**: 
  - User Stories: docs/requirements/USER_STORIES.md#auth-001
  - Functional Requirements: docs/requirements/FUNCTIONAL_REQUIREMENTS.md#authentication

- **Architecture**: 
  - ADR-005: Use Azure AD B2C for authentication
  - Authentication flow diagram: docs/architecture/diagrams/auth-flow.mmd
  - Security model: docs/architecture/SECURITY_ARCHITECTURE.md

- **Implementation**: 
  - PRs: #42, #45, #47
  - Commits: a1b2c3d..x9y8z7w
  - Code Location: src/ProjectName.Web/Authentication/, src/ProjectName.API/Security/

- **Testing**: 
  - Unit Test Coverage: 92%
  - Integration Tests: 15 tests passing (auth flow, token validation, role checks)
  - E2E Tests: 8 tests passing (login, logout, session timeout, role-based access)
  - Security: Passed penetration test on 2024-01-20

- **Documentation**: 
  - API Docs: docs/api/authentication.md
  - User Guide: docs/user-guide/logging-in.md
  - Runbook: docs/operations/runbooks/auth-troubleshooting.md

- **Deployment**: 
  - Test: Deployed 2024-01-18
  - Staging: Deployed 2024-01-22
  - Production: Deployed 2024-01-25
  - Version: v1.1.0

- **Dependencies**: Azure AD B2C tenant configured

- **Notes**: 
  - Remember to rotate client secret annually (expires 2025-01-25)
  - Monitor SignalR connection issues after authentication changes
```

### Good Technical Debt Entry

```markdown
### 2024-01-20 - Order Processing Uses Synchronous Calls

- **ID**: TD-007
- **Description**: Order processing service makes synchronous HTTP calls to payment gateway and inventory service, blocking threads while waiting for responses.

- **Reason**: Implemented synchronously to meet sprint deadline. Async messaging infrastructure not yet in place.

- **Impact**: 
  - Under high load (>100 orders/min), thread pool exhaustion can occur
  - Slower response times during peak hours
  - Cascading failures if downstream services slow down
  - Current max throughput: ~80 orders/min vs target of 200 orders/min

- **Area**: Backend - Order Service

- **Remediation Plan**: 
  1. Implement Azure Service Bus messaging for payment and inventory operations
  2. Convert to async message-based processing with Outbox pattern
  3. Add circuit breaker for resilience
  4. Estimated: 13 story points (1.5 sprints)

- **Estimated Effort**: 13 story points

- **Priority**: High (will become blocker at scale)

- **Status**: Open - Planned for Sprint 15

- **Related Feature**: Order Processing (Feature-12)

- **Added By**: Jane Smith
```

## Common Mistakes to Avoid

### ❌ Don't: Write Novel-Length Entries

Keep entries concise. Link to detailed documents instead.

### ✅ Do: Be Brief with Links

**Bad**:

```
Decision: After extensive research into various authentication providers including Auth0, Okta, Azure AD, AWS Cognito, and Firebase Auth, evaluating each on the criteria of cost, features, integration complexity, team expertise, compliance requirements, and support for our use cases... [500 more words]
```

**Good**:

```
Decision: Use Azure AD B2C for authentication. See docs/architecture/decisions/ADR-005-authentication-provider.md for detailed analysis.
```

### ❌ Don't: Let It Get Stale

A ledger that's not updated is worse than no ledger.

### ✅ Do: Update Frequently

- Set reminders
- Make it part of process
- Assign ownership
- Review in stand-ups

### ❌ Don't: Include Secrets

Never put passwords, API keys, connection strings, or other secrets in the ledger.

### ✅ Do: Reference Secret Locations

**Bad**:

```
Connection String: Server=prod-sql.database.windows.net;Database=mydb;User=admin;Password=MyP@ssw0rd
```

**Good**:

```
Connection String: Stored in Azure Key Vault secret "prod-sql-connection-string"
```

### ❌ Don't: Make It Someone Else's Problem

"The tech lead will update it" → Nobody updates it

### ✅ Do: Shared Responsibility

- Rotate responsibility
- Make it part of everyone's workflow
- Include in definition of done

## When to Reference the Ledger

### Development

- Before starting new feature → Check feature log, related decisions
- Making architecture decision → Check decision log, architecture evolution
- Implementing integration → Check integration log
- Encountering technical debt → Check tech debt log

### Meetings

- Sprint planning → Review feature log, tech debt log
- Architecture review → Reference decision log, architecture evolution
- Incident review → Reference incident log
- Retrospective → Review entire ledger for patterns

### Using AI Agents

- Provide relevant ledger sections to agents for context
- Reference decision log when asking about architecture
- Include feature log entry when implementing features
- Share incident log when troubleshooting similar issues

### Onboarding

- New team members should read entire ledger
- Helps understand project history and context
- Shows decision rationale
- Identifies current priorities and issues

## Ledger Anti-Patterns

### The Ignored Ledger

**Symptom**: Last updated 3 months ago
**Fix**: Make updates part of definition of done, assign ownership, set reminders

### The Novel

**Symptom**: Each entry is 500+ words
**Fix**: Be concise, link to detailed documents, use bullet points

### The Junk Drawer

**Symptom**: No organization, everything in one section
**Fix**: Use the defined structure, proper sections, headers

### The Secret Keeper

**Symptom**: Contains passwords, API keys, connection strings
**Fix**: Remove immediately, reference secret locations only

### The Time Machine

**Symptom**: Only historical information, no current status
**Fix**: Always keep "Current Status" up to date

### The Zombie Ledger

**Symptom**: Contains outdated, incorrect information
**Fix**: Regular reviews, archive old information, mark superseded entries

## Integration with AI Agents

When using AI agents for development tasks, provide relevant sections of your Development Ledger:

```markdown
Example prompt for backend-architect:
"Design an API endpoint for [feature]. Here is the context from our development ledger:

**Current Architecture** (from Decision Log):
[Paste relevant architecture decisions]

**Existing Integrations** (from Integration Log):
[Paste relevant integrations]

**Related Features** (from Feature Log):
[Paste related feature implementations]

Please design an endpoint that aligns with our architecture and integrates properly."
```

The ledger provides agents with:

- Historical context for decisions
- Existing patterns to follow
- Integration points to consider
- Technical constraints to respect
- Known issues to avoid

## Conclusion

The Development Ledger is your project's memory. Maintain it diligently and it will:

- Save hours in meetings explaining history
- Prevent repeated mistakes
- Speed up onboarding
- Provide context for decisions
- Enable better AI agent assistance
- Serve as audit trail
- Preserve institutional knowledge

**Key Takeaway**: Update little and often. Five minutes after each significant event is better than trying to reconstruct history later.

---

[← Back to Main Guide](./README.md) | [Next: Project Phases Guide →](./PROJECT_PHASES_GUIDE.md)
