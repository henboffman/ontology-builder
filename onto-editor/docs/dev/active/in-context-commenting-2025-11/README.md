# In-Context Commenting & Discussion Threads

**Feature Status**: Planning Complete ✅
**Implementation Start Date**: TBD
**Estimated Duration**: 3-4 weeks
**Priority**: High

---

## 📋 Project Overview

This directory contains comprehensive planning documents for implementing **In-Context Commenting & Discussion Threads** - a Figma/Google Docs-style commenting system for the Eidos Ontology Builder.

### Value Proposition

Enable users to right-click any concept or relationship and attach threaded discussions directly to that entity, with @mention support and real-time notifications via SignalR.

**Key Benefits**:
- Contextual collaboration (discussions stay with entities)
- @Mention notifications for quick feedback
- Real-time awareness via existing SignalR infrastructure
- Knowledge retention (comments become searchable history)
- Reduced communication overhead (no need for Slack/email)

---

## 📚 Documentation Structure

### 1. [implementation-plan.md](implementation-plan.md)
**Purpose**: High-level architecture and implementation roadmap
**Contents**:
- Executive summary and value proposition
- Current architecture analysis (SignalR, permissions, existing models)
- Proposed data models (EntityComment, CommentMention, EntityCommentCounts)
- Service layer design (IEntityCommentService)
- SignalR integration strategy
- UI component specifications (sliding panel, comment threads, @mention editor)
- Visual design (minimal sci-fi aesthetic)
- 4-phase implementation plan (Database → SignalR → UI → Notifications)
- Performance optimization strategies
- Security considerations
- Testing strategy
- Success metrics

**When to read**: Start here for big-picture understanding

---

### 2. [architecture-context.md](architecture-context.md)
**Purpose**: Technical decisions and rationale (the "why")
**Contents**:
- Existing infrastructure analysis (what we're reusing)
- Key architectural decisions:
  - Decision 1: Polymorphic entity reference (EntityType + EntityId)
  - Decision 2: Threaded comments via ParentCommentId
  - Decision 3: Denormalized comment counts
  - Decision 4: @Mention parsing on creation
  - Decision 5: Ontology-scoped SignalR broadcasting
  - Decision 6-8: UI/UX patterns (context menu, sliding panel, resolution)
- Performance optimization strategies (lazy loading, batch queries)
- Security considerations (XSS prevention, rate limiting, mention privacy)
- Testing strategy with examples
- Future extensibility points

**When to read**: Before making technical decisions or when questioning design choices

---

### 3. [implementation-tasks.md](implementation-tasks.md)
**Purpose**: Detailed task checklist for developers
**Contents**:
- Phase 1: Database & Core Services (Week 1)
  - 1.1 Database schema (3 tables, indexes)
  - 1.2 Model classes (EntityComment, CommentMention, EntityCommentCount)
  - 1.3 Repository layer (IEntityCommentRepository)
  - 1.4 Service layer (IEntityCommentService, MentionParser)
- Phase 2: SignalR Integration (Week 1-2)
  - 2.1 Hub methods (AddComment, UpdateComment, DeleteComment, ResolveThread)
  - 2.2 Client-side handlers (JavaScript interop)
- Phase 3: UI Components (Week 2-3)
  - 3.1 Context menu integration (graph, list, mobile)
  - 3.2 Comment panel component
  - 3.3 Comment thread component
  - 3.4 Comment editor with @mention autocomplete
  - 3.5 Comment count badges
- Phase 4: @Mention Notifications (Week 3)
  - 4.1 In-app notifications (bell icon, dropdown)
  - 4.2 Email notifications (optional)
- Phase 5: Polish & Testing (Week 4)
  - 5.1 Performance optimization
  - 5.2 Accessibility (ARIA labels, keyboard navigation)
  - 5.3 Testing (unit, integration, E2E, load tests)
  - 5.4 Documentation
- Deployment checklist
- Success metrics
- Risk mitigation

**When to read**: Daily during implementation to track progress

---

## 🚀 Quick Start Guide

### For Project Managers
1. Read **implementation-plan.md** (sections: Executive Summary, Proposed Architecture, Implementation Phases)
2. Review **implementation-tasks.md** for timeline and resource estimates
3. Track progress using the checkboxes in implementation-tasks.md

### For Developers Starting Implementation
1. Read **implementation-plan.md** (entire document)
2. Read **architecture-context.md** (sections: Existing Infrastructure, Key Architectural Decisions)
3. Use **implementation-tasks.md** as your daily checklist
4. Start with Phase 1: Database & Core Services

### For Developers Joining Mid-Implementation
1. Skim **implementation-plan.md** (Executive Summary, Proposed Architecture)
2. Read **architecture-context.md** thoroughly (understand the "why")
3. Find current phase in **implementation-tasks.md** and continue from there

### For Code Reviewers
1. Reference **architecture-context.md** to understand design decisions
2. Verify implementations match specifications in **implementation-plan.md**
3. Check off completed tasks in **implementation-tasks.md**

---

## 🔑 Key Technical Concepts

### Polymorphic Entity Reference
Comments target concepts, relationships, or individuals using `EntityType` + `EntityId` instead of separate foreign keys. This allows a single comment table for all entity types.

### Threaded Discussions
Self-referential `ParentCommentId` enables infinite nesting (UI limits to 5 levels for UX).

### Denormalized Counts
`EntityCommentCounts` table pre-computes comment counts for fast badge display. Updated transactionally with comment CRUD.

### @Mention Parsing
Mentions extracted on comment creation (not display) to enable immediate notifications and efficient querying.

### Real-Time Broadcasting
SignalR broadcasts comments to ontology-scoped groups (existing infrastructure). Targeted `Clients.User()` calls for @mention notifications.

---

## 📊 Success Metrics (Track After Launch)

| Metric | Target | How to Measure |
|--------|--------|----------------|
| Adoption Rate | 30% of active ontologies | `SELECT COUNT(DISTINCT OntologyId) FROM EntityComments` |
| Average Comments | 5 per ontology | `SELECT AVG(TotalComments) FROM EntityCommentCounts` |
| @Mention Usage | 20% of comments | `SELECT (COUNT(DISTINCT CommentId) / COUNT(*)) FROM CommentMentions` |
| Reply Rate | 40% of comments | `SELECT COUNT(*) WHERE ParentCommentId IS NOT NULL` |
| Performance | < 200ms load time | Application Insights query latency |
| SignalR Latency | < 100ms | Monitor hub method execution time |

---

## 🛠️ Files to Create

### Models (Phase 1)
- `/Models/EntityComment.cs`
- `/Models/CommentMention.cs`
- `/Models/EntityCommentCount.cs`

### Repositories (Phase 1)
- `/Data/Repositories/IEntityCommentRepository.cs`
- `/Data/Repositories/EntityCommentRepository.cs`

### Services (Phase 1)
- `/Services/Interfaces/IEntityCommentService.cs`
- `/Services/EntityCommentService.cs`
- `/Services/MentionParser.cs`

### Components (Phase 3)
- `/Components/Shared/EntityCommentPanel.razor` + `.razor.cs` + `.razor.css`
- `/Components/Shared/CommentThread.razor` + `.razor.cs` + `.razor.css`
- `/Components/Shared/CommentEditor.razor` + `.razor.cs` + `.razor.css`
- `/Components/Shared/MentionDropdown.razor` + `.razor.css`
- `/Components/Shared/CommentBadge.razor` + `.razor.css`
- `/Components/Ontology/GraphContextMenu.razor`
- `/Components/Shared/FloatingCommentButton.razor`

### JavaScript (Phase 2)
- `/wwwroot/js/comment-interop.js`

### Notifications (Phase 4)
- `/Services/Interfaces/INotificationService.cs`
- `/Services/NotificationService.cs`
- `/Components/Shared/NotificationDropdown.razor` + `.razor.css`

### Tests (Phase 5)
- `/Eidos.Tests/Repositories/EntityCommentRepositoryTests.cs`
- `/Eidos.Tests/Services/EntityCommentServiceTests.cs`
- `/Eidos.Tests/Hubs/OntologyHubCommentTests.cs`
- `/Eidos.Tests/Integration/EntityCommentIntegrationTests.cs`
- `/Eidos.Tests/E2E/CommentWorkflowTests.cs`

---

## 🔒 Security Checklist

- [ ] Sanitize Markdown output (prevent XSS)
- [ ] Rate limit comment creation (10/min per user)
- [ ] Validate @mentions against ontology access
- [ ] Permission checks in hub methods
- [ ] SQL injection prevention (use parameterized queries)
- [ ] HTTPS-only communication

---

## 🧪 Testing Checklist

### Unit Tests (Minimum 30)
- [ ] EntityCommentRepository CRUD operations
- [ ] EntityCommentService comment creation
- [ ] MentionParser edge cases (invalid users, duplicates)
- [ ] Permission checks (CanAddComment, CanEditComment)
- [ ] Denormalized count updates

### Integration Tests
- [ ] SignalR hub broadcasts
- [ ] Real-time updates reach clients
- [ ] Permission denied throws HubException

### End-to-End Tests
- [ ] User adds comment to concept
- [ ] User @mentions colleague
- [ ] Mentioned user receives notification
- [ ] User resolves comment thread
- [ ] Real-time updates appear for other users

### Load Tests
- [ ] 10 concurrent users adding comments
- [ ] SignalR message latency < 100ms
- [ ] Database query performance < 50ms

---

## 📅 Timeline Estimate

| Phase | Duration | Tasks |
|-------|----------|-------|
| Phase 1: Database & Core Services | 5 days | Schema, models, repositories, services |
| Phase 2: SignalR Integration | 3 days | Hub methods, client handlers |
| Phase 3: UI Components | 7 days | Panel, threads, editor, badges |
| Phase 4: Notifications | 3 days | In-app, email (optional) |
| Phase 5: Polish & Testing | 4 days | Performance, accessibility, tests |
| **Total** | **22 days (~4 weeks)** | |

---

## 🤝 Team Roles

- **Backend Developer**: Phase 1 (Database & Services), Phase 2 (SignalR)
- **Frontend Developer**: Phase 3 (UI Components)
- **Full-Stack Developer**: Phase 4 (Notifications), Phase 5 (Integration)
- **QA Engineer**: Phase 5 (Testing)
- **UX Designer**: Phase 3 (Visual design review)

---

## 📞 Questions or Issues?

- **Architecture questions**: Reference architecture-context.md
- **Implementation details**: Reference implementation-plan.md
- **Task status**: Update checkboxes in implementation-tasks.md
- **Blockers**: Document in this README

---

## ✅ Current Status

**Phase**: Planning Complete
**Next Step**: Review with team, assign tasks, begin Phase 1
**Blockers**: None

---

**Last Updated**: November 9, 2025
**Document Owner**: Requirements Architect
**Reviewers**: [To be assigned]
