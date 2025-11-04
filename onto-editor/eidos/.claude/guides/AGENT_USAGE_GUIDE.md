# Agent Usage Guide

## Overview

This guide provides detailed information on how to effectively use specialized AI agents throughout the development process, including when to use each agent, prompt templates, and best practices.

## Available Agents

### requirements-architect

**Specialization**: Requirements gathering, stakeholder analysis, business analysis

**When to Use**:

- Starting a new project
- Clarifying vague requirements
- Identifying missing requirements
- Validating completeness of requirements
- Prioritizing features
- Identifying constraints and risks

**Best For**:

- Initial discovery sessions
- Requirement documentation
- Stakeholder interviews
- User story creation
- Acceptance criteria definition

### backend-architect

**Specialization**: System architecture, API design, microservices, distributed systems

**When to Use**:

- Designing overall system architecture
- Defining service boundaries
- Designing API contracts (REST/GraphQL/gRPC)
- Planning inter-service communication
- Choosing architectural patterns
- Technology stack selection

**Best For**:

- High-level architecture design
- API design and documentation
- Integration patterns
- Resilience patterns
- Service decomposition

### dotnet-data-specialist

**Specialization**: Entity Framework Core, SQL Server, data access patterns, database optimization

**When to Use**:

- Designing entity models
- Implementing data access layer
- Optimizing database queries
- Designing repository patterns
- Configuring EF Core
- Database migration strategies

**Best For**:

- Entity configuration
- LINQ query optimization
- Repository implementation
- Migration creation
- Concurrency handling
- Performance tuning data access

### csharp-developer

**Specialization**: C# coding, .NET development, clean code practices

**When to Use**:

- Implementing business logic
- Writing service classes
- Creating utility functions
- Code refactoring
- Implementing design patterns
- General C# coding tasks

**Best For**:

- Service implementation
- Business logic
- Helper classes
- Code quality improvements
- Unit test creation

### blazor-developer

**Specialization**: Blazor components, state management, JavaScript interop

**When to Use**:

- Building Blazor components
- Implementing UI features
- State management
- Component architecture
- JavaScript interop
- Form implementation

**Best For**:

- Component development
- UI implementation
- Client-side logic
- Real-time features with SignalR
- Component library creation

### blazor-accessibility-performance-specialist

**Specialization**: WCAG compliance, web performance, Core Web Vitals, accessibility testing

**When to Use**:

- Ensuring WCAG compliance
- Optimizing performance
- Accessibility testing
- Performance testing
- Bundle optimization (WebAssembly)
- Rendering optimization

**Best For**:

- Accessibility implementation
- Performance optimization
- WCAG compliance verification
- Core Web Vitals improvement
- Screen reader support

### dotnet-security-specialist

**Specialization**: Security best practices, authentication, authorization, vulnerability prevention

**When to Use**:

- Implementing authentication
- Implementing authorization
- Security architecture design
- Vulnerability assessment
- Input validation
- Secrets management

**Best For**:

- Security implementation
- Authentication flows
- Authorization policies
- Security testing
- Vulnerability remediation
- Secure coding practices

### documentation-specialist

**Specialization**: Technical writing, documentation standards, API documentation, code documentation

**When to Use**:

- Creating any documentation
- Writing README files
- API documentation
- Architecture documentation
- User guides
- Runbook creation

**Best For**:

- All documentation tasks
- Documentation structure
- Template creation
- Documentation standards
- Code comments

### supportability-lifecycle-specialist

**Specialization**: Observability, monitoring, Azure DevOps, ServiceNow, operational excellence

**When to Use**:

- Setting up monitoring
- Creating runbooks
- Configuring CI/CD pipelines
- Planning deployments
- Incident response planning
- Infrastructure setup

**Best For**:

- Observability implementation
- Alert configuration
- Runbook creation
- CI/CD pipeline setup
- Production readiness
- Support team enablement

## Agent Selection Decision Tree

### Starting a New Project?

→ **requirements-architect** (gather requirements)
→ **backend-architect** (design architecture)
→ **documentation-specialist** (create initial docs)

### Working on Backend Services?

→ **dotnet-data-specialist** (data layer first)
→ **backend-architect** (API and service logic)
→ **csharp-developer** (implementation details)
→ **dotnet-security-specialist** (security implementation)

### Working on Frontend (Blazor)?

→ **blazor-developer** (component implementation)
→ **blazor-accessibility-performance-specialist** (accessibility and performance)
→ **documentation-specialist** (component documentation)

### Working on Security?

→ **dotnet-security-specialist** (security implementation)
→ **backend-architect** (security architecture)

### Working on Operations/Deployment?

→ **supportability-lifecycle-specialist** (all operational concerns)
→ **documentation-specialist** (runbook creation)

### Need Documentation?

→ **documentation-specialist** (always)

## Prompt Templates by Task

### Requirements Gathering

**Initial Project Discovery**:

```markdown
Agent: requirements-architect

"I'm starting a new [type] project for [business domain]. The high-level goal is [goal].

Please help me gather comprehensive requirements by asking questions about:
1. Business objectives and success metrics
2. Users and stakeholders
3. Functional requirements
4. Non-functional requirements
5. Technical constraints
6. Integration needs
7. Compliance requirements

Ask questions one section at a time, probing deeply based on my answers."
```

**Clarifying Vague Requirements**:

```markdown
Agent: requirements-architect

"I have this requirement: '[vague requirement]'

Help me clarify this by asking questions to uncover:
- What problem this solves
- Who the users are
- What success looks like
- What constraints exist
- What acceptance criteria should be"
```

### Architecture Design

**System Architecture Design**:

```markdown
Agent: backend-architect

"Design a backend architecture for [application type] with these requirements:

**Functional Requirements**:
[paste key functional requirements]

**Non-Functional Requirements**:
- Scale: [user count, transaction volume]
- Performance: [latency requirements]
- Availability: [SLA targets]
- Consistency: [consistency requirements]

**Technical Constraints**:
[any constraints]

Please provide:
1. High-level architecture with service boundaries
2. API design approach (REST/GraphQL/gRPC)
3. Inter-service communication patterns
4. Authentication/authorization strategy
5. Resilience patterns
6. Caching strategy
7. Technology recommendations
8. Architecture diagrams in Mermaid format"
```

**API Design**:

```markdown
Agent: backend-architect

"Design a RESTful API for [feature/domain] with these requirements:
[paste requirements]

Include:
1. Resource modeling
2. Endpoint definitions with HTTP methods
3. Request/response formats
4. Authentication/authorization approach
5. Error handling strategy
6. Versioning strategy
7. OpenAPI/Swagger schema
8. Example requests and responses"
```

### Data Layer Design

**Entity Model Design**:

```markdown
Agent: dotnet-data-specialist

"Design Entity Framework Core entities for [domain] based on these requirements:
[paste requirements]

**Data Requirements**:
- Entities: [list main entities]
- Relationships: [describe relationships]
- Data volume: [expected volume]
- Query patterns: [common queries]

Please provide:
1. Entity class definitions with EF Core configuration
2. Fluent API configuration
3. Relationship configuration
4. Index recommendations
5. Migration strategy
6. Repository pattern implementation example"
```

**Query Optimization**:

```markdown
Agent: dotnet-data-specialist

"This LINQ query is slow:
[paste query code]

**Context**:
- Data volume: [count]
- Current performance: [time]
- Target performance: [time]
- Execution frequency: [frequency]

Please:
1. Analyze the query and identify issues
2. Suggest optimizations
3. Provide optimized query code
4. Recommend indexes if needed
5. Suggest caching strategy if applicable"
```

### Implementation

**Backend Feature Implementation**:

```markdown
Agent: csharp-developer

"Implement [feature name] based on this specification:
[paste feature specification]

**Technical Context**:
- Architecture: [brief architecture context]
- Dependencies: [existing services/components]
- Constraints: [any constraints]

Please provide:
1. Service class implementation
2. Appropriate error handling
3. Logging statements
4. Input validation
5. Unit tests
6. XML documentation comments"
```

**Blazor Component Implementation**:

```markdown
Agent: blazor-developer

"Implement a Blazor component for [feature] with these requirements:
[paste requirements]

**Technical Details**:
- Hosting model: [Server/WebAssembly/Auto]
- State management: [approach]
- API integration: [endpoints]

Please provide:
1. Component code with proper lifecycle methods
2. State management implementation
3. Event handling
4. API integration
5. Error handling
6. Loading states
7. Component documentation"
```

### Security Implementation

**Authentication Implementation**:

```markdown
Agent: dotnet-security-specialist

"Implement authentication for [application] using [OAuth/JWT/etc.] with these requirements:
[paste requirements]

**Context**:
- Identity provider: [Azure AD/Auth0/etc.]
- User roles: [list roles]
- Authorization needs: [requirements]

Please provide:
1. Authentication configuration
2. Token handling
3. Authorization policies
4. Role/claim management
5. Security best practices
6. Configuration code
7. Testing approach"
```

**Security Review**:

```markdown
Agent: dotnet-security-specialist

"Review this code for security vulnerabilities:
[paste code]

**Context**:
- Purpose: [what the code does]
- User input: [what comes from users]
- Sensitive data: [any sensitive data handled]

Please identify:
1. Security vulnerabilities
2. Input validation issues
3. SQL injection risks
4. XSS vulnerabilities
5. Authentication/authorization issues
6. Remediation recommendations"
```

### Accessibility & Performance

**Accessibility Review**:

```markdown
Agent: blazor-accessibility-performance-specialist

"Review this Blazor component for WCAG [A/AA/AAA] compliance:
[paste component code]

Please check:
1. Semantic HTML usage
2. ARIA attributes
3. Keyboard navigation
4. Screen reader support
5. Color contrast
6. Focus management
7. Provide remediation recommendations with code examples"
```

**Performance Optimization**:

```markdown
Agent: blazor-accessibility-performance-specialist

"Optimize the performance of this [component/page]:
[paste code]

**Current Metrics**:
- LCP: [time]
- FID: [time]
- CLS: [score]
- Bundle size (if WASM): [size]

**Target Metrics**:
- LCP: < [time]
- FID: < [time]
- CLS: < [score]

Please provide:
1. Performance analysis
2. Optimization recommendations
3. Code changes needed
4. Before/after comparison
5. Testing approach"
```

### Documentation

**README Creation**:

```markdown
Agent: documentation-specialist

"Create a comprehensive README.md for [project name]:

**Project Details**:
- Type: [Web API/Blazor App/etc.]
- Technology: [.NET 8, Blazor Server, etc.]
- Purpose: [brief purpose]

Include:
1. Project overview
2. Features list
3. Prerequisites
4. Installation instructions
5. Configuration steps
6. Usage examples
7. Documentation links
8. Contributing guidelines
9. License information"
```

**API Documentation**:

```markdown
Agent: documentation-specialist

"Create API documentation for this endpoint:
[paste endpoint code or OpenAPI spec]

Include:
1. Endpoint description
2. HTTP method and path
3. Request parameters
4. Request body schema
5. Response schemas (success and errors)
6. Authentication requirements
7. Code examples in C# and curl
8. Error codes and meanings"
```

**Architecture Decision Record**:

```markdown
Agent: documentation-specialist

"Create an ADR for this decision:

**Decision**: [what was decided]
**Context**: [why the decision was needed]
**Alternatives**: [what else was considered]
**Consequences**: [impact of the decision]

Please format this as a complete Architecture Decision Record following the standard template."
```

### Operations & Support

**Monitoring Setup**:

```markdown
Agent: supportability-lifecycle-specialist

"Set up comprehensive monitoring for [application] with these requirements:

**Application Details**:
- Technology: [stack]
- Architecture: [brief architecture]
- SLA targets: [targets]

Please provide:
1. Application Insights configuration
2. Custom metrics to track
3. Structured logging implementation
4. Correlation ID implementation
5. Alert rules with thresholds
6. KQL queries for common scenarios
7. Dashboard configuration
8. ServiceNow integration for incidents"
```

**Runbook Creation**:

```markdown
Agent: supportability-lifecycle-specialist

"Create a runbook for [scenario/incident type]:

**Context**:
- System: [system description]
- Common triggers: [what causes this]
- Impact: [impact when it occurs]

Include:
1. Purpose and scope
2. Prerequisites
3. Detection/symptoms
4. Step-by-step diagnostic procedures
5. Resolution steps
6. Validation steps
7. Escalation procedures
8. Related KQL queries or dashboard links"
```

**CI/CD Pipeline**:

```markdown
Agent: supportability-lifecycle-specialist

"Create Azure DevOps CI/CD pipelines for [application]:

**Requirements**:
- Build: [build requirements]
- Test: [testing requirements]
- Environments: [Dev, Test, Staging, Production]
- Approval gates: [where approvals needed]

Please provide:
1. Build pipeline YAML
2. Release pipeline configuration
3. Multi-stage deployment setup
4. Automated testing integration
5. Approval gate configuration
6. ServiceNow integration for production
7. Rollback procedures
8. Pipeline documentation"
```

## Agent Combination Strategies

### Strategy 1: Sequential Specialization

Use agents in sequence, each building on the previous agent's work.

**Example - Feature Implementation**:

1. **requirements-architect**: Clarify feature requirements
2. **backend-architect**: Design API and service architecture
3. **dotnet-data-specialist**: Design data layer
4. **csharp-developer**: Implement service logic
5. **dotnet-security-specialist**: Add security
6. **documentation-specialist**: Document the feature

### Strategy 2: Parallel Consultation

Use multiple agents simultaneously for different aspects of the same problem.

**Example - Architecture Design**:

- **backend-architect**: Overall system architecture
- **dotnet-security-specialist**: Security architecture (parallel)
- **supportability-lifecycle-specialist**: Observability architecture (parallel)
- **documentation-specialist**: Document all architecture decisions

### Strategy 3: Iterative Refinement

Use the same agent multiple times to refine the output.

**Example - API Design**:

1. **backend-architect**: Initial API design
2. Review and provide feedback
3. **backend-architect**: Refined design based on feedback
4. Review again
5. **backend-architect**: Final design with any remaining adjustments

### Strategy 4: Review and Validate

Use one agent to create, another to review.

**Example - Security Implementation**:

1. **csharp-developer**: Implement feature
2. **dotnet-security-specialist**: Review for security vulnerabilities
3. **csharp-developer**: Fix identified issues
4. **dotnet-security-specialist**: Final security validation

### Strategy 5: Specialist Consultation

Use a specialist agent to enhance work from a generalist.

**Example - Blazor Component**:

1. **blazor-developer**: Build component with basic functionality
2. **blazor-accessibility-performance-specialist**: Enhance with accessibility and optimize performance
3. **documentation-specialist**: Document component usage

## Best Practices for Using Agents

### 1. Provide Sufficient Context

**❌ Poor**:

```
"Create an API for orders"
```

**✅ Good**:

```
"Create a RESTful API for order management in an e-commerce system.

Context:
- Orders can have multiple line items
- Support for order status workflow (Pending -> Processing -> Shipped -> Delivered)
- Integration with payment gateway required
- Expected volume: 1000 orders/day
- Performance requirement: < 200ms response time
- Authentication: JWT tokens
- Users: Customers (place orders) and Admins (manage all orders)

Requirements:
[detailed requirements]"
```

### 2. Be Specific About Constraints

Always include:

- Technology stack constraints
- Performance requirements
- Security requirements
- Existing architecture/patterns to follow
- Team skill level
- Timeline constraints

### 3. Request Examples and Explanations

Ask for:

- Working code examples
- Explanation of decisions made
- Trade-offs considered
- Alternative approaches
- Testing strategies

**Example**:

```
"Provide implementation code with:
1. Inline comments explaining complex logic
2. XML documentation for public APIs
3. Example usage
4. Unit test examples
5. Explanation of design decisions"
```

### 4. Iterate Based on Feedback

Don't expect perfect output on first try:

1. Get initial output
2. Review and identify issues
3. Provide specific feedback
4. Get refined output
5. Repeat as needed

### 5. Cross-Reference with Development Ledger

Always provide relevant ledger context:

```
"Implement [feature] following our existing patterns.

From our Development Ledger:

**Architecture Decisions** (relevant ADRs):
[paste relevant decisions]

**Existing Patterns**:
[paste existing implementation patterns]

**Integrations**:
[paste relevant integrations]

Please design consistently with these existing patterns."
```

### 6. Ask for Multiple Options When Unsure

When facing a decision:

```
"We need to [solve problem]. Please provide 2-3 solution approaches with:
1. Description of each approach
2. Pros and cons
3. Complexity assessment
4. Performance implications
5. Your recommendation with rationale"
```

### 7. Request Documentation Alongside Code

Always ask for:

- Code comments
- XML documentation
- Usage examples
- Testing approach
- Operational considerations

### 8. Validate Against Requirements

After receiving output:

1. Check against original requirements
2. Verify all edge cases covered
3. Ensure security considerations addressed
4. Confirm performance requirements met
5. Validate accessibility if UI component

## Common Prompt Patterns

### Pattern: "Analyze and Recommend"

```markdown
"Analyze [code/architecture/design] and recommend improvements for:
- [concern 1]
- [concern 2]
- [concern 3]

Current state:
[paste current state]

Target state:
[describe desired state]

Provide specific recommendations with code examples."
```

### Pattern: "Review and Critique"

```markdown
"Review this [code/design/document] for:
- Correctness
- Best practices
- Security issues
- Performance concerns
- Maintainability

[paste content]

Provide:
1. Issues found with severity (Critical/High/Medium/Low)
2. Specific remediation for each issue
3. Best practice recommendations"
```

### Pattern: "Implement with Constraints"

```markdown
"Implement [feature] with these constraints:

**Must Have**:
- [constraint 1]
- [constraint 2]

**Must Not**:
- [anti-constraint 1]
- [anti-constraint 2]

**Preferences**:
- [preference 1]
- [preference 2]

Provide implementation that respects all constraints with explanation."
```

### Pattern: "Migrate/Refactor"

```markdown
"Refactor this code from [current approach] to [target approach]:

**Current Code**:
[paste code]

**Target Approach**:
[describe target]

**Reasons for Refactoring**:
- [reason 1]
- [reason 2]

Provide:
1. Refactored code
2. Migration strategy
3. Testing approach
4. Risks and mitigation"
```

### Pattern: "Design with Trade-offs"

```markdown
"Design [component/system] optimizing for [primary concern] while balancing:
- [concern 1]
- [concern 2]
- [concern 3]

Explicitly state trade-offs made and why."
```

## Agent Usage Anti-Patterns

### ❌ Anti-Pattern 1: Vague Prompts

**Problem**: "Make this better"
**Solution**: Be specific about what "better" means (faster, more maintainable, more secure, etc.)

### ❌ Anti-Pattern 2: No Context

**Problem**: Asking for code without providing any context about the system
**Solution**: Always provide system context, constraints, and requirements

### ❌ Anti-Pattern 3: Accepting First Output Without Review

**Problem**: Using agent output without reviewing or testing
**Solution**: Always review, validate, and test agent output

### ❌ Anti-Pattern 4: Wrong Agent for Task

**Problem**: Using backend-architect for detailed C# implementation
**Solution**: Use agent specializations appropriately

### ❌ Anti-Pattern 5: No Iterative Refinement

**Problem**: Giving up if first output isn't perfect
**Solution**: Provide feedback and iterate to improve

### ❌ Anti-Pattern 6: Ignoring Security/Performance

**Problem**: Not considering security or performance in prompts
**Solution**: Always include security and performance requirements

### ❌ Anti-Pattern 7: Not Documenting Agent Usage

**Problem**: Not tracking which agents were used for what
**Solution**: Document agent usage in Development Ledger

## Quick Reference Table

| Task | Primary Agent | Secondary Agents | Key Outputs |
|------|---------------|------------------|-------------|
| Requirements gathering | requirements-architect | documentation-specialist | Requirements docs |
| System architecture | backend-architect | dotnet-data-specialist, dotnet-security-specialist | Architecture docs, ADRs |
| API design | backend-architect | documentation-specialist | OpenAPI spec, API docs |
| Data layer | dotnet-data-specialist | backend-architect | Entity models, repositories |
| Backend implementation | csharp-developer | dotnet-security-specialist | Service code, tests |
| Frontend implementation | blazor-developer | blazor-accessibility-performance-specialist | Components, pages |
| Security | dotnet-security-specialist | backend-architect | Security implementation |
| Accessibility | blazor-accessibility-performance-specialist | blazor-developer | Accessible components |
| Performance optimization | blazor-accessibility-performance-specialist | dotnet-data-specialist | Optimized code |
| Documentation | documentation-specialist | All specialists | All documentation |
| Monitoring/Observability | supportability-lifecycle-specialist | documentation-specialist | Monitoring config, runbooks |
| CI/CD | supportability-lifecycle-specialist | documentation-specialist | Pipeline configs |
| Deployment | supportability-lifecycle-specialist | documentation-specialist | Deployment plans, runbooks |

## Measuring Agent Effectiveness

Track these metrics to improve agent usage:

### Quality Metrics

- **First-time acceptance rate**: How often agent output is accepted without modification
- **Iteration count**: Average iterations needed to get acceptable output
- **Defect rate**: Bugs found in agent-generated code

### Efficiency Metrics

- **Time saved**: Compare time with agents vs without
- **Rework reduction**: Less rework due to better initial design
- **Documentation completeness**: Better documentation coverage

### Improvement Actions

- **Low acceptance rate**: Improve prompt quality, add more context
- **High iteration count**: Provide better initial requirements
- **High defect rate**: Add more validation, use review agents

## Conclusion

Effective agent usage requires:

1. **Right agent for right task**: Use specializations appropriately
2. **Sufficient context**: Provide all necessary information
3. **Iterative refinement**: Expect to refine outputs
4. **Validation**: Always review and test
5. **Documentation**: Track usage in Development Ledger
6. **Continuous improvement**: Learn and improve prompt patterns

---

[← Back to Main Guide](./README.md) | [Next: Documentation Standards →](./DOCUMENTATION_STANDARDS.md)
