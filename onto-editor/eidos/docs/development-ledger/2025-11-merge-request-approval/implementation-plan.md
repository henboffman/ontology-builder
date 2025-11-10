# Merge Request / Approval Workflow - Implementation Plan

**Date**: November 8, 2025
**Feature**: Development Roadmap for Merge Request System
**Status**: Planning Phase

---

## Overview

This document outlines a phased approach to implementing the Merge Request / Approval Workflow system. The implementation is structured to deliver incremental value while maintaining code quality and system stability.

**Total Estimated Effort**: 80-100 hours (2-2.5 weeks full-time)

---

## Implementation Phases

### Phase 1: Core Data Model & Basic CRUD
**Goal**: Establish database foundation and basic service layer
**Estimated Time**: 16-20 hours

#### Tasks

##### 1.1 Database Schema (4-5 hours)

**Subtasks**:
- [ ] Create migration for `MergeRequests` table
  - All columns as per technical design
  - Foreign key constraints
  - Indexes for performance
- [ ] Create migration for `MergeRequestChanges` table
  - Relationship to `MergeRequests`
  - Sequence number ordering
  - Indexes on MergeRequestId
- [ ] Create migration for `MergeRequestComments` table
  - Relationship to `MergeRequests`
  - Optional: Parent comment support for threading
- [ ] Extend `Ontologies` table with approval mode fields
  - `RequiresApproval` bit column
  - `ApprovalModeEnabledAt` timestamp
  - `ApprovalModeEnabledBy` user reference
- [ ] Run migrations and verify schema
- [ ] Seed test data for development

**Deliverables**:
- Migration files in `/Migrations/`
- Updated `OntologyDbContext.cs` with new DbSets
- Database schema diagram (optional)

**Testing**:
- Manual verification of schema in database
- Verify foreign key constraints
- Test cascade delete behavior

**Risk Assessment**: Low
- Well-defined schema
- Standard EF Core patterns

---

##### 1.2 Domain Models (3-4 hours)

**Subtasks**:
- [ ] Create `/Models/MergeRequest.cs`
  - All properties from technical design
  - Navigation properties
  - Computed properties (IsStale, CanBeReviewed, etc.)
- [ ] Create `/Models/MergeRequestChange.cs`
  - Change type, entity type enums
  - JSON snapshot properties
- [ ] Create `/Models/MergeRequestComment.cs`
  - Author, content, timestamps
  - Parent comment relationship
- [ ] Create `/Models/Enums/MergeRequestStatus.cs`
  - Draft, Pending, Stale, Approved, Rejected, Cancelled
- [ ] Create `/Models/Enums/MergeRequestChangeType.cs`
  - Create, Update, Delete
- [ ] Update `Ontology.cs` model
  - Add `RequiresApproval` property
  - Add navigation property to `MergeRequests`

**Deliverables**:
- 5 new model files
- 2 enum files
- Updated Ontology model

**Testing**:
- Compile-time verification
- Unit tests for computed properties

**Risk Assessment**: Low

---

##### 1.3 Repository Layer (4-5 hours)

**Subtasks**:
- [ ] Create `/Data/Repositories/IMergeRequestRepository.cs` interface
  - CRUD method signatures
  - Query methods (by ontology, by status, by submitter)
- [ ] Implement `/Data/Repositories/MergeRequestRepository.cs`
  - AddAsync, UpdateAsync, DeleteAsync
  - GetByIdAsync, GetWithChangesAsync, GetWithCommentsAsync
  - GetByOntologyAsync, GetByStatusAsync
  - GetPendingCountAsync
  - Optimized queries with AsNoTracking
- [ ] Create `/Data/Repositories/IMergeRequestChangeRepository.cs` (if needed)
- [ ] Implement MergeRequestChangeRepository (if needed)
- [ ] Update `OntologyRepository` to include RequiresApproval in queries

**Deliverables**:
- Repository interface and implementation
- Repository registered in DI container

**Testing**:
- Repository unit tests with in-memory database
- Test all query methods
- Verify eager loading includes

**Risk Assessment**: Low

---

##### 1.4 Basic Service Layer (5-6 hours)

**Subtasks**:
- [ ] Create `/Services/Interfaces/IMergeRequestService.cs`
  - Method signatures from technical design
- [ ] Create `/Services/MergeRequestService.cs` skeleton
  - Constructor with dependencies
  - CreateMergeRequestAsync (basic implementation)
  - GetMergeRequestAsync
  - GetMergeRequestsAsync (with pagination)
  - UpdateMergeRequestAsync
  - DeleteAsync / CancelMergeRequestAsync
- [ ] Implement permission checks
  - Use existing `OntologyPermissionService`
  - CanUserReviewAsync
  - CanUserCancelAsync
- [ ] Implement basic DTO mapping
  - MergeRequest → MergeRequestDto
  - MergeRequestChange → MergeRequestChangeDto
- [ ] Register services in `Program.cs`

**Deliverables**:
- Service interface and implementation
- Basic CRUD operations working
- Permission checks integrated

**Testing**:
- Service layer unit tests
- Mock repository dependencies
- Test permission checks

**Risk Assessment**: Medium
- Depends on clear understanding of existing permission system

---

#### Phase 1 Acceptance Criteria
- [ ] Database tables created and migrated
- [ ] Domain models compile without errors
- [ ] Repository layer can perform CRUD operations
- [ ] Service layer can create and retrieve MRs
- [ ] Permission checks prevent unauthorized access
- [ ] All unit tests pass (target: 20+ tests)

---

### Phase 2: Change Detection & Diff Visualization
**Goal**: Capture changes from command stack and display diffs
**Estimated Time**: 20-24 hours

#### Tasks

##### 2.1 Extend Command Pattern (6-8 hours)

**Subtasks**:
- [ ] Extend `ICommand` interface
  - Add `string GetBeforeSnapshot()` method
  - Add `string GetAfterSnapshot()` method
  - Add `string EntityType { get; }` property
  - Add `int? EntityId { get; }` property
- [ ] Update all existing command implementations
  - `CreateConceptCommand.cs`: capture after snapshot
  - `UpdateConceptCommand.cs`: capture before and after
  - `DeleteConceptCommand.cs`: capture before snapshot
  - `CreateRelationshipCommand.cs`: similar
  - `UpdateRelationshipCommand.cs`: similar
  - `DeleteRelationshipCommand.cs`: similar
- [ ] Extend `CommandInvoker.cs`
  - Add `GetExecutedCommands(int ontologyId)` method
  - Add `ClearForOntology(int ontologyId)` method
  - Maintain executed commands list (in addition to undo stack)
- [ ] Create snapshot serialization utilities
  - `SnapshotSerializer.Serialize<T>(T entity)` method
  - Handle circular references
  - Consistent JSON format

**Deliverables**:
- Updated command pattern with snapshot support
- All 6 command classes updated
- CommandInvoker can export command history

**Testing**:
- Test snapshot capture in each command
- Verify JSON serialization/deserialization
- Test GetExecutedCommands returns correct list

**Risk Assessment**: Medium-High
- Requires changes to core command pattern
- Must not break undo/redo functionality
- Circular reference handling in serialization

---

##### 2.2 Change Detection Service (6-7 hours)

**Subtasks**:
- [ ] Create `/Services/Interfaces/IChangeDetectionService.cs`
- [ ] Implement `/Services/ChangeDetectionService.cs`
  - `CaptureChangesAsync(int ontologyId, string userId)`
    - Get commands from invoker
    - Convert to MergeRequestChange entities
    - Calculate field-level diffs
  - `DetectConflictsAsync(MergeRequest mr)`
    - Compare MR base version with current ontology
    - Identify field-level conflicts
    - Return list of Conflict objects
  - `GenerateDiff(string beforeJson, string afterJson)`
    - Field-by-field comparison
    - Return FieldChange list
- [ ] Create `Conflict.cs` model
  - ChangeId, EntityType, FieldName
  - MergeRequestValue, CurrentValue
  - ConflictType (Modified, Deleted, Structural)
- [ ] Integrate with MergeRequestService
  - Call CaptureChangesAsync in CreateMergeRequestAsync
  - Call DetectConflictsAsync before approval

**Deliverables**:
- Change detection service implemented
- Conflict model defined
- Integration with MR service

**Testing**:
- Test change capture from command stack
- Test conflict detection scenarios:
  - Same field modified in both
  - Entity deleted in ontology but MR modifies it
  - No conflicts (happy path)
- Test field-level diff generation

**Risk Assessment**: High
- Complex logic for conflict detection
- Edge cases in diff generation
- Performance with large change sets

---

##### 2.3 Diff Visualization Component (8-9 hours)

**Subtasks**:
- [ ] Create `/Components/MergeRequest/DiffViewer.razor`
  - Props: List<MergeRequestChangeDto> changes
  - Visual diff display (+ green, ~ yellow, - red)
  - Expand/collapse each change
  - Before/after side-by-side view for updates
- [ ] Create `/Components/MergeRequest/ChangeCard.razor`
  - Single change display
  - Entity type icon (concept, relationship, property)
  - Field-level changes highlighted
  - Conflict indicator
- [ ] Create CSS for diff visualization
  - Color coding for add/modify/delete
  - Strikethrough for deleted text
  - Highlight changed words in modified fields
- [ ] Implement pagination for large change sets
  - Show 50 changes per page
  - Next/Previous navigation
  - Jump to change number
- [ ] Add search/filter within changes
  - Filter by entity type
  - Search by entity name

**Deliverables**:
- DiffViewer component
- ChangeCard component
- Diff CSS styles

**Testing**:
- Manual testing with various change types
- Test large change sets (100+ changes)
- Test conflict highlighting
- Test search/filter

**Risk Assessment**: Medium
- UI complexity for diff rendering
- Performance with large diffs

---

#### Phase 2 Acceptance Criteria
- [ ] Commands capture before/after snapshots
- [ ] CommandInvoker can export executed commands
- [ ] ChangeDetectionService captures changes into MR
- [ ] Conflict detection identifies field-level conflicts
- [ ] DiffViewer component renders changes visually
- [ ] User can paginate and filter changes
- [ ] All unit tests pass (target: 30+ additional tests)

---

### Phase 3: Review UI & Approval Workflow
**Goal**: Enable admins to review and approve/reject MRs
**Estimated Time**: 18-22 hours

#### Tasks

##### 3.1 Merge Request List View (6-7 hours)

**Subtasks**:
- [ ] Create `/Components/Pages/MergeRequestList.razor`
  - Route: `/ontology/{id}/merge-requests`
  - Filter bar (status, submitter, date range)
  - Sort options (date, submitter, change count)
  - Table view with columns:
    - Title, Submitter, Status, Change Summary, Date, Actions
  - Pagination (25 per page)
  - Bulk selection checkboxes (for FullAccess users)
- [ ] Create `/Components/MergeRequest/MergeRequestListItem.razor`
  - Single row component
  - Status badge with color coding
  - Change summary with icons
  - Quick actions (View, Approve, Reject, Cancel)
- [ ] Implement filtering and sorting
  - Client-side filtering for small lists (<100 items)
  - Server-side filtering for large lists
  - URL query parameters for filter state
- [ ] Add empty state
  - "No merge requests" message
  - Helpful tips for first-time users

**Deliverables**:
- MergeRequestList page component
- List item component
- Filtering and sorting logic

**Testing**:
- Test with 0, 1, 10, 100, 1000 MRs
- Test each filter option
- Test sorting by each column
- Test pagination

**Risk Assessment**: Low

---

##### 3.2 Merge Request Detail View (7-8 hours)

**Subtasks**:
- [ ] Create `/Components/Pages/MergeRequestDetail.razor`
  - Route: `/ontology/{id}/merge-requests/{mrId}`
  - Header: Title, Status, Submitter info, Dates
  - Tabs: Changes, Discussion, History
  - Action buttons: Approve, Reject, Cancel, Edit (contextual)
- [ ] Changes tab
  - Integrate DiffViewer component from Phase 2
  - Navigation: Previous/Next change
  - Search within changes
- [ ] Discussion tab (basic, no comments yet)
  - Placeholder: "Comments coming soon"
  - Show MR description
- [ ] History tab
  - Timeline of MR events (created, submitted, approved, etc.)
  - Use OntologyActivity data
- [ ] Stale/Conflict warning banner
  - Prominent display if MR is stale
  - List conflicts if any
  - "Resolve Conflicts" button (future)

**Deliverables**:
- MergeRequestDetail page component
- Tab navigation
- Integration with DiffViewer

**Testing**:
- Navigate to MR detail from list
- Switch between tabs
- View changes for various MR types
- Display stale warning correctly

**Risk Assessment**: Low

---

##### 3.3 Approval/Rejection Dialogs (5-7 hours)

**Subtasks**:
- [ ] Create `/Components/MergeRequest/ApprovalDialog.razor`
  - Confirm approval
  - Show change summary
  - Optional comment field
  - Warning: "Changes will be applied immediately"
  - Confirm/Cancel buttons
- [ ] Create `/Components/MergeRequest/RejectionDialog.razor`
  - Require rejection reason (textarea, min 10 chars)
  - Character counter
  - Confirm/Cancel buttons
- [ ] Implement ApprovalLogic in MergeRequestService
  - `ApproveMergeRequestAsync` method
  - Transaction wrapping
  - Apply each change sequentially
  - Rollback on error
  - Update MR status
  - Record activity
- [ ] Implement RejectionLogic
  - `RejectMergeRequestAsync` method
  - Validate reason provided
  - Update MR status
  - Record activity
  - Notify submitter
- [ ] Add toast notifications
  - Success: "Merge request approved successfully"
  - Error: "Failed to approve: [reason]"

**Deliverables**:
- ApprovalDialog component
- RejectionDialog component
- Approval/rejection logic in service
- Toast notifications

**Testing**:
- Approve valid MR (no conflicts)
- Reject MR with reason
- Test transaction rollback on error
- Verify activity recording
- Verify notifications

**Risk Assessment**: High
- Transaction integrity critical
- Rollback must work correctly
- Apply changes logic complex

---

#### Phase 3 Acceptance Criteria
- [ ] MergeRequestList page displays all MRs with filters
- [ ] User can navigate to MR detail
- [ ] MR detail shows all changes with diff visualization
- [ ] Admin can approve MR and changes are applied
- [ ] Admin can reject MR with reason
- [ ] Submitter sees approval/rejection status
- [ ] All unit and integration tests pass (target: 40+ additional tests)

---

### Phase 4: Notifications & Polish
**Goal**: Real-time notifications, UI polish, edge case handling
**Estimated Time**: 16-18 hours

#### Tasks

##### 4.1 SignalR Notifications (6-7 hours)

**Subtasks**:
- [ ] Create `/Hubs/MergeRequestHub.cs`
  - JoinOntologyGroup, LeaveOntologyGroup methods
  - Broadcast methods: NotifyMRCreated, NotifyMRApproved, etc.
- [ ] Integrate with MergeRequestService
  - Call hub methods after MR state changes
  - Send notifications to ontology group
- [ ] Create `/Services/Interfaces/INotificationService.cs`
  - NotifyMergeRequestCreatedAsync
  - NotifyMergeRequestApprovedAsync
  - NotifyMergeRequestRejectedAsync
  - NotifyCommentAddedAsync (for Phase 5)
- [ ] Implement NotificationService
  - Use HubContext to send SignalR messages
  - Use ToastService for in-app toasts
  - Future: Email notifications
- [ ] Update Blazor components to listen for SignalR
  - Subscribe to "MergeRequestCreated" event
  - Update MR list in real-time
  - Show toast notification

**Deliverables**:
- MergeRequestHub
- NotificationService
- SignalR integration in components

**Testing**:
- Test real-time notification delivery
- Test with multiple users connected
- Verify only relevant users notified

**Risk Assessment**: Medium
- SignalR connection management
- Ensure notifications don't spam

---

##### 4.2 Approval Mode Settings (4-5 hours)

**Subtasks**:
- [ ] Update `/Components/Ontology/OntologySettingsDialog.razor`
  - Add "Advanced" tab
  - Add "Require Approval for Edits" toggle
  - Help text explaining approval mode
  - Confirmation dialog on enable
  - Show pending MR count if any exist
  - Prevent disable if pending MRs exist
- [ ] Implement SetApprovalModeAsync in OntologyService
  - Update Ontology.RequiresApproval
  - Record ApprovalModeEnabledAt, EnabledBy
  - Record activity
- [ ] Update OntologyViewState
  - Add RequiresApproval property
  - Add CanCreateMergeRequest computed property
  - Add CanDirectEdit computed property
- [ ] Update OntologyView to check approval mode
  - Show banner if RequiresApproval = true
  - Change "Save" button to "Create Merge Request"
  - Disable direct edit actions

**Deliverables**:
- Approval mode toggle in settings
- Banner in OntologyView
- Updated edit workflow

**Testing**:
- Enable approval mode
- Verify banner appears
- Verify edit buttons change
- Verify cannot disable with pending MRs
- Verify activity recorded

**Risk Assessment**: Low

---

##### 4.3 My Merge Requests View (3-4 hours)

**Subtasks**:
- [ ] Create `/Components/Pages/MyMergeRequests.razor`
  - Route: `/my-merge-requests`
  - Show all MRs created by current user
  - Group by status: Draft, Pending, Approved, Rejected
  - Each group collapsible
  - Link to ontology and MR detail
- [ ] Add navigation link
  - In main navigation menu
  - Badge showing pending count
- [ ] Implement GetMyMergeRequestsAsync in service
  - Filter by SubmitterId = currentUserId
  - Order by CreatedAt DESC

**Deliverables**:
- MyMergeRequests page
- Navigation link with badge

**Testing**:
- View as user with MRs
- View as user with no MRs
- Verify badge count

**Risk Assessment**: Low

---

##### 4.4 Draft MR Support (3-4 hours)

**Subtasks**:
- [ ] Add "Save as Draft" button in create MR dialog
  - Set Status = Draft
  - Don't set SubmittedAt
  - Don't notify reviewers
- [ ] Add "Submit for Review" button in MR detail
  - Only visible for Draft MRs by owner
  - Change Status to Pending
  - Set SubmittedAt
  - Notify reviewers
- [ ] Implement SubmitForReviewAsync in service
  - Update status
  - Detect conflicts (ontology may have changed)
  - Record activity
- [ ] Show draft MRs in "My Merge Requests"
  - Separate section or filter
  - Edit/Delete actions available

**Deliverables**:
- Draft save functionality
- Submit for review functionality
- Draft MR management

**Testing**:
- Save MR as draft
- Edit draft MR
- Submit draft for review
- Verify reviewers not notified for draft

**Risk Assessment**: Low

---

#### Phase 4 Acceptance Criteria
- [ ] Real-time notifications via SignalR
- [ ] Approval mode can be enabled in settings
- [ ] Banner shows when approval mode active
- [ ] "My Merge Requests" page works
- [ ] Draft MRs can be saved and submitted later
- [ ] All unit and integration tests pass (target: 20+ additional tests)

---

### Phase 5: Testing & Documentation
**Goal**: Comprehensive testing, documentation, bug fixes
**Estimated Time**: 10-16 hours

#### Tasks

##### 5.1 Integration Testing (4-5 hours)

**Subtasks**:
- [ ] Create end-to-end test scenarios
  - Test 1: Create MR → Approve → Verify changes applied
  - Test 2: Create MR → Reject → Verify changes NOT applied
  - Test 3: Create MR → Ontology changes → Detect stale
  - Test 4: Bulk approve multiple MRs
  - Test 5: Cancel MR before review
- [ ] Test with multiple concurrent users
  - User A creates MR while User B edits ontology
  - Two admins approve different MRs simultaneously
- [ ] Performance testing
  - Create MR with 100+ changes
  - List page with 1000+ MRs
  - Approval of large MR
- [ ] Error scenario testing
  - Database connection failure during approval
  - SignalR disconnection
  - Transaction rollback

**Deliverables**:
- Integration test suite
- Performance benchmarks
- Error handling verification

**Testing**:
- All scenarios pass
- Performance meets targets (<3s for approval)
- Errors handled gracefully

**Risk Assessment**: Medium

---

##### 5.2 User Acceptance Testing (3-4 hours)

**Subtasks**:
- [ ] Manual testing of complete workflows
  - As Editor: Create MR, save draft, submit for review
  - As Admin: Review MR, approve, verify changes
  - As Admin: Review MR, reject, verify reason shown
  - As Editor: View rejection, create new MR
- [ ] UI/UX testing
  - Test on desktop (Chrome, Firefox, Safari)
  - Test on tablet (iPad)
  - Test on mobile (iPhone, Android)
  - Test dark mode
  - Test accessibility (screen reader, keyboard nav)
- [ ] Edge case testing
  - Empty states (no MRs, no changes)
  - Large data sets (1000+ MRs, 500+ changes)
  - Conflict resolution

**Deliverables**:
- UAT test plan
- Bug list
- UI/UX feedback

**Risk Assessment**: Low

---

##### 5.3 Documentation (3-4 hours)

**Subtasks**:
- [ ] Update `/docs/user-guide/merge-requests.md`
  - What are merge requests?
  - When to use approval mode
  - How to create a merge request
  - How to review and approve
  - How to handle conflicts
- [ ] Update `/docs/technical/merge-request-architecture.md`
  - System architecture overview
  - Database schema reference
  - Service layer documentation
  - Integration points
- [ ] Update CLAUDE.md
  - Add merge request feature to feature list
  - Document approval mode setting
  - Note key services and patterns
- [ ] Create CHANGELOG entry
  - Feature: Merge Request / Approval Workflow
  - Breaking changes: None
  - Migration: Run migration

**Deliverables**:
- User documentation
- Technical documentation
- Updated CLAUDE.md
- Changelog entry

**Risk Assessment**: Low

---

##### 5.4 Bug Fixes & Polish (3-5 hours)

**Subtasks**:
- [ ] Fix bugs identified in UAT
- [ ] Polish UI based on feedback
  - Improve color contrasts
  - Better error messages
  - Loading indicators
  - Empty states
- [ ] Code review and refactoring
  - Remove duplicate code
  - Improve naming
  - Add missing comments
- [ ] Final performance optimization
  - Add database indexes if needed
  - Optimize queries
  - Add caching where appropriate

**Deliverables**:
- Bug fixes
- Polished UI
- Optimized code

**Risk Assessment**: Low

---

#### Phase 5 Acceptance Criteria
- [ ] All integration tests pass
- [ ] UAT scenarios pass
- [ ] Documentation complete
- [ ] All critical bugs fixed
- [ ] Code reviewed and polished
- [ ] Performance targets met

---

## Phase 6: Advanced Features (Future)
**Goal**: Optional enhancements for future releases
**Estimated Time**: 30-40 hours (separate release)

### Features to Consider

#### 6.1 MR Comments/Discussion (8-10 hours)
- Full comment thread support
- Markdown comments
- Notifications on new comments
- Edit/delete own comments

#### 6.2 Reviewer Assignment (4-5 hours)
- Assign specific reviewers to MRs
- Notification to assigned reviewer
- Reassignment capability
- Multiple reviewers requiring all approvals

#### 6.3 Conflict Resolution UI (10-12 hours)
- Three-way diff view
- Choose MR version, current version, or manual resolution
- Apply resolved conflicts
- Test and validate resolution

#### 6.4 MR Templates (4-5 hours)
- Pre-defined MR templates for common changes
- Template selection on MR creation
- Custom templates per ontology

#### 6.5 Bulk Operations Enhancements (4-5 hours)
- Bulk approve with individual review
- Bulk reject with individual reasons
- Progress tracking for bulk operations

#### 6.6 Email Notifications (6-8 hours)
- Configure SMTP settings
- Email templates for MR events
- Opt-in/opt-out preferences
- Email digest (daily summary)

#### 6.7 MR Analytics Dashboard (8-10 hours)
- Metrics: approval rate, time to approval, contributor stats
- Charts and graphs
- Export analytics to CSV/PDF
- Trend analysis

---

## Dependencies

### External Dependencies
- No new external packages required
- Uses existing EF Core, SignalR, Blazor Server

### Internal Dependencies
- Must not break existing undo/redo (Phase 2 risk)
- Must integrate with OntologyPermissionService
- Must integrate with OntologyActivityService
- Must work with existing command pattern

---

## Risk Mitigation

### High-Risk Areas

#### 1. Command Pattern Extension (Phase 2.1)
**Risk**: Breaking undo/redo functionality
**Mitigation**:
- Comprehensive unit tests for command pattern
- Manual testing of undo/redo before and after changes
- Feature flag to disable MR if issues found
- Rollback plan: Revert Phase 2.1 changes

#### 2. Transaction Integrity (Phase 3.3)
**Risk**: Partial application of changes on error
**Mitigation**:
- Use database transactions
- Test rollback scenarios extensively
- Log all operations for debugging
- Dry-run mode for testing

#### 3. Conflict Detection (Phase 2.2)
**Risk**: Missing conflicts or false positives
**Mitigation**:
- Conservative conflict detection (err on side of caution)
- Manual review step for admins
- Clear conflict indicators in UI
- Ability to force-approve if needed (with warning)

---

## Rollout Strategy

### Option A: Feature Branch with Soft Launch
1. Develop on `feature/merge-requests` branch
2. Deploy to staging environment
3. Internal testing with development team
4. Soft launch: Enable for 1-2 pilot ontologies
5. Gather feedback, fix bugs
6. Full launch: Enable for all ontologies (optional setting)

### Option B: Phased User Rollout
1. Phase 1-3: Core functionality deployed
2. Enable for internal ontologies only
3. Phase 4: Notifications and polish
4. Enable for beta users
5. Phase 5: Testing and documentation
6. Full release to all users

**Recommendation**: Option A (Feature Branch with Soft Launch)

---

## Success Metrics

### Technical Metrics
- [ ] Zero breaking changes to existing features
- [ ] All unit tests pass (target: 150+ new tests)
- [ ] Integration tests cover main workflows (5+ scenarios)
- [ ] Performance: MR creation <2s, Approval <3s, List load <2s
- [ ] Code coverage >80% for new code

### User Metrics
- [ ] 90% of MRs reviewed within 24 hours (target)
- [ ] <5% MR rejection rate (indicates quality submissions)
- [ ] Zero data loss incidents
- [ ] User satisfaction: 4+/5 stars
- [ ] Feature adoption: 30%+ of ontologies enable approval mode (3 months)

---

## Timeline

### Aggressive Schedule (2 weeks full-time)
- Week 1:
  - Days 1-2: Phase 1 (Core Data Model)
  - Days 3-5: Phase 2 (Change Detection)
- Week 2:
  - Days 1-3: Phase 3 (Review UI & Approval)
  - Days 4-5: Phase 4 (Notifications & Polish)
  - Weekend: Phase 5 (Testing & Documentation)

### Realistic Schedule (3 weeks part-time)
- Week 1: Phase 1 + Phase 2.1
- Week 2: Phase 2.2-2.3 + Phase 3.1-3.2
- Week 3: Phase 3.3 + Phase 4 + Phase 5

### Conservative Schedule (4 weeks)
- Week 1: Phase 1
- Week 2: Phase 2
- Week 3: Phase 3
- Week 4: Phase 4 + Phase 5

**Recommendation**: Conservative Schedule (allows for unknowns and polish)

---

## Development Checklist

### Before Starting
- [ ] Review all requirements documents
- [ ] Understand existing permission system
- [ ] Understand existing command pattern
- [ ] Set up feature branch
- [ ] Create task tracking board (GitHub Projects, Jira, etc.)

### During Development
- [ ] Follow existing code style and patterns
- [ ] Write unit tests for all new services
- [ ] Update documentation as you go
- [ ] Commit frequently with descriptive messages
- [ ] Test on multiple browsers regularly
- [ ] Keep CHANGELOG.md updated

### Before Each Phase Completion
- [ ] All tests pass
- [ ] Code reviewed (self-review minimum)
- [ ] Manual testing of new features
- [ ] Documentation updated
- [ ] No console errors
- [ ] Accessible (keyboard nav, screen reader)

### Before Final Release
- [ ] All phases complete
- [ ] All acceptance criteria met
- [ ] Performance benchmarks met
- [ ] UAT passed
- [ ] Documentation complete
- [ ] Deployment plan reviewed
- [ ] Rollback plan ready
- [ ] Stakeholder approval

---

## Deployment Plan

### Pre-Deployment
1. Merge feature branch to `develop`
2. Run all tests on CI/CD pipeline
3. Deploy to staging environment
4. Run smoke tests on staging
5. Performance testing on staging
6. Final UAT on staging

### Deployment Steps
1. Schedule maintenance window (optional, no downtime expected)
2. Backup production database
3. Deploy code to production
4. Run database migrations
5. Verify migration success
6. Smoke test production
7. Monitor logs and metrics for 1 hour
8. Enable feature for pilot ontologies
9. Monitor for 24 hours
10. Enable feature for all ontologies

### Post-Deployment
1. Announce feature in release notes
2. Email users about new capability
3. Monitor usage metrics
4. Monitor error logs
5. Gather user feedback
6. Prioritize bug fixes
7. Plan Phase 6 enhancements

### Rollback Plan
If critical issues found:
1. Disable approval mode feature flag (if implemented)
2. Revert code deployment
3. Revert database migration (if safe)
4. Communicate issue to users
5. Debug and fix
6. Re-deploy when ready

---

## Related Documents

- [Business Requirements](./requirements.md)
- [Technical Design](./technical-design.md)
- [UI Mockups](./ui-mockups.md)
- [Data Model](./data-model.md)

---

**Last Updated**: November 8, 2025
**Status**: Draft for Review
**Next Step**: UI Mockups Document
