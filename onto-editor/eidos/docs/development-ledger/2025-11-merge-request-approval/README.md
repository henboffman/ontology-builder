# Merge Request / Approval Workflow - Requirements Documentation

**Date**: November 8, 2025
**Feature**: Enterprise Governance with Merge Request Approval System
**Status**: Planning Phase - Ready for Review
**Estimated Effort**: 80-100 hours (2-2.5 weeks full-time)

---

## Overview

This directory contains comprehensive requirements documentation for implementing a GitHub-style Merge Request / Pull Request approval workflow in the Eidos ontology builder. The feature enables enterprise-grade governance for mature ontologies by requiring admin approval before changes are applied.

---

## Documentation Structure

### 1. [requirements.md](./requirements.md)
**Business & User Requirements**

Complete user stories, acceptance criteria, and functional requirements.

**Key Sections**:
- 15+ User Stories with acceptance criteria
- Permission matrix (who can create/review/approve)
- Workflow state machine (Draft → Pending → Approved/Rejected)
- 10+ Edge case scenarios
- UI/UX requirements
- Success metrics

**Target Audience**: Product managers, stakeholders, UX designers

---

### 2. [technical-design.md](./technical-design.md)
**Technical Architecture & Implementation**

System architecture, database design, and service layer specifications.

**Key Sections**:
- Architecture overview with diagrams
- Complete database schema (3 new tables, 1 extension)
- Domain models with full C# code
- DTOs for API layer
- Service interfaces and implementation outlines
- Repository pattern integration
- SignalR hub design
- Integration with existing systems:
  - Command pattern (undo/redo)
  - Permission system
  - Version history/activity tracking
- Performance considerations (indexes, caching, pagination)
- Security (authorization, SQL injection prevention, XSS)
- Error handling strategy
- Testing approach

**Target Audience**: Software architects, senior developers

---

### 3. [implementation-plan.md](./implementation-plan.md)
**Development Roadmap & Timeline**

Phased implementation plan with tasks, estimates, and dependencies.

**Key Sections**:
- **Phase 1** (16-20h): Core data model & basic CRUD
- **Phase 2** (20-24h): Change detection & diff visualization
- **Phase 3** (18-22h): Review UI & approval workflow
- **Phase 4** (16-18h): Notifications & polish
- **Phase 5** (10-16h): Testing & documentation
- Risk assessment for each phase
- Rollout strategy
- Success criteria
- Development checklist

**Target Audience**: Project managers, development team

---

### 4. [ui-mockups.md](./ui-mockups.md)
**User Interface Design**

ASCII mockups and design specifications for all user-facing screens.

**Key Sections**:
- 13 detailed screen mockups:
  - Approval mode toggle
  - Approval mode banner
  - Create merge request dialog
  - Merge request list view
  - Merge request detail view
  - Approval/rejection dialogs
  - My merge requests page
  - Notification toasts
  - Discussion tab (future)
  - Bulk actions
  - Conflict resolution (future)
  - Mobile responsive designs
- Design tokens (colors, typography, spacing, icons)
- Accessibility considerations (keyboard nav, screen readers, WCAG)

**Target Audience**: UX designers, frontend developers

---

### 5. [data-model.md](./data-model.md)
**Database Schema & Queries**

Complete database design with tables, indexes, and sample queries.

**Key Sections**:
- Entity relationship diagram
- 3 new tables with full SQL definitions:
  - `MergeRequests` (primary entity)
  - `MergeRequestChanges` (individual changes)
  - `MergeRequestComments` (discussion)
- Extension to `Ontologies` table (approval mode)
- Computed columns & views
- 7 sample queries with performance notes
- Data integrity constraints
- Indexing strategy (primary, composite, filtered, covering)
- Data retention & cleanup policies
- Complete EF Core migrations (C# code)

**Target Audience**: Database architects, backend developers

---

## Quick Start

### For Stakeholders & Product Team
1. Start with **requirements.md** for user stories and acceptance criteria
2. Review **ui-mockups.md** for visual design
3. Check **implementation-plan.md** for timeline and milestones

### For Development Team
1. Read **technical-design.md** for architecture and integration points
2. Review **data-model.md** for database schema
3. Follow **implementation-plan.md** for phased development
4. Reference **ui-mockups.md** for UI implementation

### For QA Team
1. Review **requirements.md** for acceptance criteria
2. Check **edge cases** section in requirements
3. Follow **Phase 5** in implementation-plan for testing strategy

---

## Key Features

### Approval Mode
- **Toggle Setting**: Ontology owners can enable "Approval Mode" in settings
- **Workflow Change**: When enabled, edits create merge requests instead of direct changes
- **Backwards Compatible**: Existing ontologies continue working as before

### Merge Requests
- **Creation**: Users with Edit permission create MRs with proposed changes
- **Drafts**: Save work in progress without submitting for review
- **Change Summary**: Automatic detection of added/modified/deleted concepts and relationships
- **Visual Diffs**: Green/yellow/red highlighting for add/modify/delete changes

### Review & Approval
- **Admin Review**: Users with FullAccess can review pending MRs
- **Visual Diff View**: See exactly what will change with before/after comparison
- **Approve/Reject**: Batch approval or rejection with comments
- **Conflict Detection**: Automatic detection if ontology changed since MR creation

### Notifications
- **Real-time**: SignalR-based notifications for MR events
- **In-app Toasts**: Immediate feedback for approvals, rejections, new MRs
- **Notification Center**: View history of all MR-related notifications

### Audit Trail
- **Full History**: Every MR action recorded in OntologyActivity
- **Accountability**: Who approved/rejected, when, and why
- **Compliance**: Audit trail for enterprise governance requirements

---

## Integration with Existing Systems

### Leverages Existing Infrastructure
- **Permission System**: Uses `OntologyPermissionService` for access control
- **Version History**: Integrates with `OntologyActivityService` for audit trail
- **Command Pattern**: Extends existing undo/redo commands to capture changes
- **SignalR**: Uses existing real-time communication for notifications
- **Blazor Components**: Follows established component patterns

### No Breaking Changes
- Existing functionality continues to work
- Approval mode is optional (off by default)
- Backwards compatible with all existing data

---

## Architecture Highlights

### Database Design
- **3 New Tables**: MergeRequests, MergeRequestChanges, MergeRequestComments
- **Optimized Indexes**: Composite and filtered indexes for performance
- **JSON Snapshots**: Store before/after state for diff visualization
- **Cascade Deletes**: Proper referential integrity

### Service Layer
- **MergeRequestService**: Core business logic for MR CRUD
- **ChangeDetectionService**: Captures changes from command stack
- **NotificationService**: Manages real-time and in-app notifications
- **Integration Services**: Permission, activity, ontology services

### Frontend Components
- **MergeRequestList.razor**: List view with filtering and sorting
- **MergeRequestDetail.razor**: Detail view with diff visualization
- **DiffViewer.razor**: Visual diff component (reusable)
- **ApprovalDialog.razor**: Confirmation dialog for approval
- **RejectionDialog.razor**: Rejection with required reason

---

## Success Criteria

### Technical
- [ ] Zero breaking changes to existing features
- [ ] All unit tests pass (150+ new tests)
- [ ] Integration tests cover main workflows (5+ scenarios)
- [ ] Performance: MR creation <2s, Approval <3s, List load <2s
- [ ] Code coverage >80% for new code

### Business
- [ ] 90% of MRs reviewed within 24 hours
- [ ] <5% MR rejection rate (quality indicator)
- [ ] Zero data loss incidents
- [ ] User satisfaction: 4+/5 stars
- [ ] Feature adoption: 30%+ of ontologies in 3 months

---

## Timeline

### Recommended Schedule (4 weeks, conservative)
- **Week 1**: Phase 1 - Core data model & basic CRUD
- **Week 2**: Phase 2 - Change detection & diff visualization
- **Week 3**: Phase 3 - Review UI & approval workflow
- **Week 4**: Phase 4 & 5 - Notifications, testing, documentation

### Aggressive Schedule (2 weeks, full-time)
- **Week 1**: Phase 1-2
- **Week 2**: Phase 3-5

---

## Risks & Mitigation

### High-Risk Areas
1. **Command Pattern Extension** (Phase 2.1)
   - Risk: Breaking undo/redo
   - Mitigation: Comprehensive tests, feature flag, rollback plan

2. **Transaction Integrity** (Phase 3.3)
   - Risk: Partial change application on error
   - Mitigation: Database transactions, rollback logic, extensive error testing

3. **Conflict Detection** (Phase 2.2)
   - Risk: Missing conflicts or false positives
   - Mitigation: Conservative detection, manual review option, clear warnings

---

## Next Steps

1. **Review Phase**: Stakeholder review of all requirements documents
2. **Approval**: Get go/no-go decision from product team
3. **Kickoff**: Create feature branch, set up task board
4. **Phase 1**: Begin implementation with database schema
5. **Iteration**: Develop incrementally, test continuously

---

## Questions & Answers

### Q: Will this slow down normal editing?
**A**: No. Approval mode is optional and only affects ontologies where it's enabled. Performance impact is minimal (<10ms overhead for permission checks).

### Q: Can users still see changes before approval?
**A**: Yes. MRs are visible to all users with View permission. Changes are just not applied to the ontology until approved.

### Q: What happens if two admins approve different MRs that conflict?
**A**: The second approval will detect conflicts and either auto-merge (if no field-level conflicts) or require manual resolution.

### Q: Can a user approve their own merge request?
**A**: No. The system prevents self-approval. A different admin/owner must review and approve.

### Q: How do conflicts get resolved?
**A**: Phase 6 (future) includes a conflict resolution UI with three-way diff. For MVP, conflicting MRs are flagged and must be rejected or manually merged in the base ontology first.

### Q: Can this be disabled if users don't like it?
**A**: Yes. Ontology owners can disable approval mode at any time (after resolving pending MRs).

---

## Resources

### External References
- [GitHub Pull Requests](https://docs.github.com/en/pull-requests) - Inspiration for MR workflow
- [GitLab Merge Requests](https://docs.gitlab.com/ee/user/project/merge_requests/) - Feature reference
- [OWL Versioning](https://www.w3.org/TR/owl2-syntax/#Ontology_Versioning) - Ontology version tracking

### Internal References
- [CLAUDE.md](../../CLAUDE.md) - Project context and architecture
- [Development Ledger](../) - Other feature implementations
- [Collaboration Board](../2025-10-31-collaboration-board/) - Related governance feature

---

## Contributing

When implementing this feature:
1. Follow existing code style and patterns
2. Write tests for all new functionality
3. Update documentation as you go
4. Keep this README updated with progress
5. Ask questions in team chat

---

## Status Updates

### November 8, 2025
- ✅ Requirements documentation complete
- ✅ Technical design complete
- ✅ Implementation plan complete
- ✅ UI mockups complete
- ✅ Data model complete
- ⏳ Awaiting stakeholder review

---

## Contact

**Feature Owner**: [To be assigned]
**Tech Lead**: [To be assigned]
**Questions**: [Team channel or email]

---

**Last Updated**: November 8, 2025
**Status**: Requirements Complete - Ready for Review
