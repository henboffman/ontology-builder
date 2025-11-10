# Merge Request / Approval Workflow - Business & User Requirements

**Date**: November 8, 2025
**Feature**: Enterprise Governance with Merge Request Approval Workflow
**Status**: Planning Phase

---

## Executive Summary

Add an "Approval Mode" to ontologies that transforms the editing workflow from direct changes to a review-based approval process. When enabled, users with Edit permissions create Merge Requests (MRs) instead of making direct changes. Admins/Owners review aggregated diffs and approve or reject changes as a batch, providing enterprise-grade governance for mature ontologies.

**Key Benefits**:
- Quality control for production ontologies
- Audit trail for all changes
- Prevent unauthorized or accidental modifications
- Enable collaborative review process
- Maintain ontology integrity in multi-user environments

---

## User Stories & Acceptance Criteria

### Epic 1: Approval Mode Configuration

#### Story 1.1: Enable Approval Mode
**As an** ontology owner or admin
**I want to** enable "Approval Mode" for my ontology
**So that** all edits require review before being applied

**Acceptance Criteria**:
- Settings dialog includes "Approval Mode" toggle
- Toggle is only visible to users with FullAccess permission
- When enabled, a confirmation dialog warns about workflow change
- Existing edits in progress are preserved
- Setting is persisted in database
- Activity log records when approval mode is toggled
- Notification sent to all Edit-level users explaining new workflow

**Edge Cases**:
- Cannot disable if there are pending MRs (must resolve first)
- Users without FullAccess cannot see or change this setting
- Setting change requires ontology UpdatedAt timestamp update

---

#### Story 1.2: View Approval Mode Status
**As a** user with Edit permission
**I want to** clearly see when an ontology is in Approval Mode
**So that** I understand why I cannot make direct edits

**Acceptance Criteria**:
- Banner appears at top of ontology view when Approval Mode is enabled
- Banner text: "This ontology requires approval for changes. Your edits will create merge requests."
- Edit buttons show "Create Merge Request" instead of "Save"
- Settings dialog shows approval mode status prominently
- Help text explains the workflow

**Edge Cases**:
- Banner does not appear for View-only users
- Banner is dismissible but reappears on page reload
- Mobile-responsive banner design

---

### Epic 2: Creating Merge Requests

#### Story 2.1: Create Merge Request from Edits
**As a** user with Edit permission in Approval Mode
**I want to** create a merge request with my proposed changes
**So that** an admin can review and approve them

**Acceptance Criteria**:
- "Create Merge Request" button replaces direct save actions
- Dialog prompts for:
  - Title (required, max 200 chars)
  - Description (optional, markdown supported)
  - Automatically detected changes summary (read-only)
- Changes are captured as structured diff:
  - Added concepts (count + details)
  - Modified concepts (count + field-level changes)
  - Deleted concepts (count + names)
  - Added relationships (count + details)
  - Modified relationships (count + changes)
  - Deleted relationships (count + names)
- MR is created with status "Pending"
- User is notified of successful creation
- User can continue editing and create additional MRs

**Edge Cases**:
- Cannot create empty MR (must have at least one change)
- If another user made conflicting changes, show warning
- MR captures user making the request, timestamp, and IP address
- Duplicate MR prevention: warn if similar pending MR exists
- Handle command undo/redo state (changes must be committed)

---

#### Story 2.2: Save MR as Draft
**As a** user creating a merge request
**I want to** save my work as a draft
**So that** I can finish it later without submitting for review

**Acceptance Criteria**:
- "Save as Draft" button available in MR creation dialog
- Draft MRs have status "Draft"
- Draft MRs visible only to creator
- Creator can edit draft MR (title, description, add more changes)
- Creator can submit draft for review (changes to "Pending")
- Draft MRs do not notify reviewers

**Edge Cases**:
- Limit drafts to 10 per user per ontology
- Warn if changes become stale (base ontology modified)
- Auto-save draft every 2 minutes while editing

---

#### Story 2.3: Batch Multiple Changes into One MR
**As a** user with Edit permission
**I want to** make multiple edits and submit them as one merge request
**So that** reviewers see logically grouped changes

**Acceptance Criteria**:
- All uncommitted changes since last MR submission are included
- User can review all changes before creating MR
- Change summary shows:
  - Total concepts added/modified/deleted
  - Total relationships added/modified/deleted
  - Total properties changed
- User can discard specific changes from the batch
- Command invoker cleared after MR creation

**Edge Cases**:
- If base ontology changes during editing, detect conflicts
- Maximum 500 changes per MR (split into multiple if needed)
- Warn user if MR is very large (>100 changes)

---

### Epic 3: Reviewing Merge Requests

#### Story 3.1: View Pending Merge Requests
**As an** admin or owner
**I want to** see all pending merge requests
**So that** I can review and act on them

**Acceptance Criteria**:
- "Merge Requests" tab/view in ontology interface
- List shows all MRs with status "Pending"
- Each MR shows:
  - Title
  - Submitter name and avatar
  - Submission date (relative time: "2 hours ago")
  - Change summary: "+3 concepts, -1 relationship, 4 definitions changed"
  - Status badge
- Sortable by: submission date, submitter, change count
- Filterable by: status, submitter, date range
- Pagination (25 per page)
- Click to view details

**Edge Cases**:
- Empty state: "No pending merge requests"
- Show count in navigation badge
- Real-time updates via SignalR when new MR created
- Mobile-responsive table/card view

---

#### Story 3.2: Review Merge Request Details
**As an** admin or owner
**I want to** view detailed changes in a merge request
**So that** I can make an informed approval decision

**Acceptance Criteria**:
- Detail view shows:
  - MR metadata (title, description, submitter, date)
  - Visual diff of all changes
  - Comments/discussion (future enhancement)
- Diff view displays:
  - Added items: green background, "+" prefix
  - Modified items: yellow background, "~" prefix, before/after comparison
  - Deleted items: red background, "-" prefix
- For concepts: show name, definition, properties changes
- For relationships: show source, target, type changes
- For properties: show field-by-field diff
- Navigation between changes (Next/Previous buttons)
- "Approve" and "Reject" buttons prominently displayed
- "Request Changes" option (with comment requirement)

**Edge Cases**:
- Show warning if base ontology changed since MR creation
- Highlight conflicting changes in red
- Show JSON diff for complex property changes
- Allow side-by-side comparison view
- Export diff as PDF or HTML report

---

#### Story 3.3: Approve Merge Request
**As an** admin or owner
**I want to** approve a merge request
**So that** the changes are applied to the ontology

**Acceptance Criteria**:
- "Approve" button requires confirmation dialog
- Confirmation shows summary of changes being applied
- Optional approval comment field
- Upon approval:
  - All changes are applied atomically (all or nothing)
  - MR status changes to "Approved"
  - MR approval date and approver recorded
  - Activity log records approval with full audit trail
  - Submitter notified of approval
  - Ontology UpdatedAt timestamp updated
  - Ontology counts updated (ConceptCount, RelationshipCount)
- Success message shown to approver
- MR moves to "Approved" list

**Edge Cases**:
- If base ontology changed, detect conflicts and prevent approval
- Conflict resolution: show diff and require manual merge or rejection
- If approval fails (database error), MR remains pending
- Transaction rollback on any error during application
- Re-validation of all changes before applying
- Cannot approve own MR (requires different admin)
- Approve button disabled if user lacks FullAccess

---

#### Story 3.4: Reject Merge Request
**As an** admin or owner
**I want to** reject a merge request with a reason
**So that** the submitter understands why their changes were not accepted

**Acceptance Criteria**:
- "Reject" button requires reason (mandatory text field)
- Confirmation dialog shows reason and warns changes will not be applied
- Upon rejection:
  - MR status changes to "Rejected"
  - MR rejection date, rejector, and reason recorded
  - Submitter notified with rejection reason
  - Changes are NOT applied to ontology
- Rejected MR moved to "Rejected" list
- Submitter can view rejection reason in MR details

**Edge Cases**:
- Reason must be at least 10 characters
- Rejection is permanent (cannot undo)
- Submitter can create new MR with revised changes
- Activity log records rejection
- Cannot reject already approved or rejected MR

---

### Epic 4: Merge Request Lifecycle

#### Story 4.1: View My Merge Requests
**As a** user who submitted merge requests
**I want to** see the status of my submissions
**So that** I know what has been reviewed

**Acceptance Criteria**:
- "My Merge Requests" filter/tab
- Shows all MRs created by current user
- Grouped by status: Draft, Pending, Approved, Rejected
- Each MR shows status, submission date, reviewer (if reviewed)
- Click to view details
- Can edit or delete draft MRs
- Can view approval/rejection reason for completed MRs

**Edge Cases**:
- Empty state for each status
- Cannot edit pending, approved, or rejected MRs
- Show notification badge for state changes (approved/rejected)

---

#### Story 4.2: Receive Notifications
**As a** user involved in merge request workflow
**I want to** receive notifications for relevant events
**So that** I can take timely action

**Acceptance Criteria**:
- Submitter receives notification when:
  - MR is approved (with approver name)
  - MR is rejected (with reason)
  - MR has conflict (ontology changed)
- Reviewers receive notification when:
  - New MR submitted for review
  - MR count shown in badge
- In-app toast notifications
- Optional email notifications (future)
- Notification center shows history

**Edge Cases**:
- Do not notify for draft MRs
- Batch notifications if multiple MRs submitted quickly
- Dismissible notifications
- Mark as read functionality

---

#### Story 4.3: Handle Stale Merge Requests
**As an** admin or owner
**I want to** identify and handle stale merge requests
**So that** the MR queue remains manageable

**Acceptance Criteria**:
- MR is "stale" if base ontology changed after MR creation
- Stale indicator shown on MR list and detail view
- Warning message: "Base ontology has changed. Conflicts may exist."
- Reviewer options for stale MR:
  - Attempt auto-merge (if no conflicts)
  - Review conflicts and manually resolve
  - Reject as stale and request resubmission
- Submitter can update stale draft MR to latest base

**Edge Cases**:
- Detect conflicts: same concept/relationship modified in base and MR
- Show conflict resolution UI with three-way diff
- Auto-merge only if no conflicts detected
- Cannot auto-merge if structural changes (e.g., deleted concept used in relationship)

---

#### Story 4.4: Cancel Merge Request
**As a** submitter
**I want to** cancel my pending merge request
**So that** I can withdraw changes I no longer want

**Acceptance Criteria**:
- "Cancel" button visible to MR creator
- Confirmation dialog required
- Upon cancellation:
  - MR status changes to "Cancelled"
  - Reviewers notified
  - Activity log records cancellation
- Cancelled MRs moved to "Cancelled" list
- Cannot cancel approved or rejected MRs

**Edge Cases**:
- Only creator can cancel (not reviewers)
- Cannot cancel if currently being reviewed (prevent race condition)
- Cancellation is permanent (cannot undo)

---

### Epic 5: Advanced Features

#### Story 5.1: Assign Reviewer
**As an** ontology owner
**I want to** assign specific reviewers to merge requests
**So that** the right expert reviews the right changes

**Acceptance Criteria**:
- Owner can assign user with FullAccess as reviewer
- Assigned reviewer receives notification
- MR shows assigned reviewer name
- Only assigned reviewer can approve/reject
- Unassigned MRs can be reviewed by any admin/owner
- Can reassign reviewer

**Edge Cases**:
- Cannot assign user without FullAccess
- Can assign multiple reviewers (require all approvals)
- Self-assignment not allowed
- Email notification to assigned reviewer

---

#### Story 5.2: Bulk Approve/Reject
**As an** admin with many pending MRs
**I want to** approve or reject multiple MRs at once
**So that** I can process reviews efficiently

**Acceptance Criteria**:
- Checkbox selection in MR list
- "Bulk Approve" and "Bulk Reject" buttons
- Confirmation dialog shows all selected MRs
- Optional comment applies to all
- All operations atomic (all succeed or all fail)
- Progress indicator for bulk operations
- Success/failure summary displayed

**Edge Cases**:
- Maximum 50 MRs per bulk operation
- Skip MRs with conflicts (show warning)
- Cannot bulk approve own MRs
- Activity log records each MR individually

---

#### Story 5.3: MR Comments/Discussion
**As a** reviewer or submitter
**I want to** add comments to a merge request
**So that** we can discuss changes before approval

**Acceptance Criteria**:
- Comment thread in MR detail view
- Any user with View access can comment
- Comments show author, timestamp, avatar
- Markdown support for formatting
- Notifications when new comment added
- Comment history preserved
- Can edit/delete own comments (within 5 minutes)

**Edge Cases**:
- Comment character limit: 2000 chars
- Spam prevention: rate limit comments
- Comments preserved after approval/rejection
- Can attach screenshots (future enhancement)

---

#### Story 5.4: MR Analytics
**As an** ontology owner
**I want to** see statistics about merge requests
**So that** I can understand workflow patterns

**Acceptance Criteria**:
- Analytics dashboard shows:
  - Total MRs: pending, approved, rejected, cancelled
  - Average time to approval
  - Top contributors (by MR count)
  - Approval rate by reviewer
  - Change types distribution (concepts vs relationships)
- Date range filter
- Export as CSV

**Edge Cases**:
- Empty state if no MRs exist
- Privacy: only show analytics to FullAccess users
- Real-time updates via SignalR

---

## Permission Matrix

| Role/Permission | View MRs | Create MR | Edit Draft | Submit for Review | Review MR | Approve MR | Reject MR | Cancel MR | Enable Approval Mode |
|----------------|----------|-----------|------------|-------------------|-----------|------------|-----------|-----------|----------------------|
| **View** | Yes | No | No | No | No | No | No | No | No |
| **ViewAndAdd** | Yes | Yes | Yes (own) | Yes (own) | No | No | No | Yes (own) | No |
| **ViewAddEdit** | Yes | Yes | Yes (own) | Yes (own) | No | No | No | Yes (own) | No |
| **FullAccess** | Yes | Yes | Yes (own) | Yes (own) | Yes | Yes* | Yes* | Yes (own) | Yes |
| **Owner** | Yes | Yes | Yes (own) | Yes (own) | Yes | Yes | Yes | Yes (own) | Yes |

*Cannot approve or reject own MR (requires different admin)

---

## Workflow States

```
┌─────────┐
│  Draft  │ ← User saves work in progress
└────┬────┘
     │
     │ User clicks "Submit for Review"
     ▼
┌─────────┐
│ Pending │ ← Awaiting admin review
└────┬────┘
     │
     ├──────────────┬──────────────┬────────────────┐
     │              │              │                │
     │ Admin        │ Admin        │ User           │ Ontology
     │ Approves     │ Rejects      │ Cancels        │ Changes (Stale)
     │              │              │                │
     ▼              ▼              ▼                ▼
┌─────────┐    ┌──────────┐   ┌───────────┐   ┌───────┐
│Approved │    │ Rejected │   │ Cancelled │   │ Stale │
└─────────┘    └──────────┘   └───────────┘   └───┬───┘
                                                    │
                                                    │ Admin
                                                    │ Resolves
                                                    ▼
                                               ┌─────────┐
                                               │Approved │
                                               │   or    │
                                               │Rejected │
                                               └─────────┘
```

### State Definitions

1. **Draft**: User is still working on changes. Not visible to reviewers.
2. **Pending**: Submitted for review. Awaiting admin/owner action.
3. **Stale**: Base ontology changed after MR creation. May have conflicts.
4. **Approved**: Admin approved and changes applied to ontology.
5. **Rejected**: Admin rejected with reason. Changes not applied.
6. **Cancelled**: Submitter withdrew the MR before review.

### State Transitions

- Draft → Pending: User submits for review
- Draft → Cancelled: User cancels draft
- Pending → Approved: Admin approves (no conflicts)
- Pending → Rejected: Admin rejects with reason
- Pending → Cancelled: User cancels before review
- Pending → Stale: Base ontology modified (automatic detection)
- Stale → Approved: Admin resolves conflicts and approves
- Stale → Rejected: Admin rejects due to conflicts

### Terminal States
- **Approved**: Final, no further changes
- **Rejected**: Final, no further changes
- **Cancelled**: Final, no further changes

---

## Edge Cases & Special Scenarios

### 1. Conflicting Changes

**Scenario**: User A creates MR to edit Concept "Person". Before approval, User B (with FullAccess) directly edits "Person".

**Handling**:
- MR automatically marked as "Stale"
- Reviewer sees conflict warning with diff
- Options:
  - Auto-merge if changes to different fields
  - Manual merge: reviewer chooses User A, User B, or custom resolution
  - Reject as stale: User A must create new MR with latest base

**Detection Logic**:
- Compare MR base version (OntologyActivity.VersionNumber) with current version
- Field-level diff to identify specific conflicts
- Structural conflicts (e.g., concept deleted) = auto-reject

---

### 2. Large Merge Requests

**Scenario**: User creates MR with 500+ changes.

**Handling**:
- Warning shown: "This MR is very large (500 changes). Consider splitting into smaller logical groups."
- UI may be slow rendering large diffs
- Pagination in diff view (50 changes per page)
- Performance optimization: lazy-load change details
- Hard limit: 1000 changes per MR

---

### 3. Cascade Deletions

**Scenario**: MR deletes Concept "Animal" which has child concepts and relationships.

**Handling**:
- Diff shows all cascading deletions clearly
- Warning: "Deleting this concept will also delete 5 relationships and 2 child concepts."
- Reviewer must explicitly acknowledge cascade
- All cascades included in change summary

---

### 4. Duplicate Merge Requests

**Scenario**: User creates MR editing "Person". Then creates another MR editing "Person" again.

**Handling**:
- System detects duplicate (editing same entities)
- Warning: "A pending MR already modifies Concept 'Person'. Do you want to continue?"
- Options:
  - Cancel and edit existing draft
  - Continue anyway (may conflict later)
  - Merge into existing draft

---

### 5. Reviewer Permissions Revoked

**Scenario**: Admin is reviewing MR. Owner revokes admin's FullAccess before approval.

**Handling**:
- MR becomes unassigned (if assigned to that admin)
- Admin can no longer approve/reject
- UI shows: "You no longer have permission to review this MR."
- Another admin must take over review

---

### 6. Approval Mode Disabled with Pending MRs

**Scenario**: Owner wants to disable Approval Mode but 10 MRs are pending.

**Handling**:
- System prevents disabling
- Error message: "Cannot disable Approval Mode. 10 pending merge requests must be resolved first."
- Options:
  - Bulk approve all
  - Bulk reject all
  - Resolve individually

---

### 7. Concurrent MR Approvals

**Scenario**: Two admins attempt to approve different MRs simultaneously.

**Handling**:
- Database row-level locking on MR status updates
- Second approval attempts refresh and shows success for both
- Activity log shows both approvals with timestamps
- Potential conflict detection runs for each MR independently

---

### 8. MR on Forked Ontology

**Scenario**: User forks ontology (with Approval Mode), makes changes, creates MR.

**Handling**:
- Fork has independent Approval Mode setting (default: inherited)
- MR applies to fork, not parent
- If fork is later merged back to parent, MR history is preserved
- Parent MRs do not affect fork

---

### 9. Undo/Redo with MR

**Scenario**: User makes edits, undoes some, creates MR.

**Handling**:
- MR captures final state of command stack
- Undone changes are NOT included in MR
- Clear indication: "Current changes: 3 concepts added (2 undone actions not included)"
- Command stack cleared after MR submission

---

### 10. MR Submission During System Maintenance

**Scenario**: User clicks "Create MR" during database maintenance or network outage.

**Handling**:
- Retry logic: attempt submission 3 times with exponential backoff
- If fails, save MR data to local storage (browser)
- Toast notification: "Unable to submit MR. Changes saved locally. Retry when online."
- Auto-retry on reconnection

---

## UI/UX Requirements

### 1. Approval Mode Toggle (Settings Dialog)

**Location**: Ontology Settings > Advanced

**UI Elements**:
- Toggle switch: "Require approval for all edits"
- Help text: "When enabled, users with Edit permission cannot make direct changes. All edits create merge requests that require admin approval."
- Visible only to FullAccess users
- Confirmation dialog on enable
- Shows pending MR count if any exist

**Visual Treatment**:
- Standard Bootstrap toggle switch
- Warning icon if pending MRs exist
- Badge showing "Approval Mode Active" in settings

---

### 2. Approval Mode Banner

**Location**: Top of OntologyView, below navigation

**UI Elements**:
- Info banner (Bootstrap alert-info)
- Icon: shield or checkmark
- Text: "This ontology requires approval for changes. Your edits will create merge requests."
- "Learn More" link to help docs
- Dismissible but persists on reload
- Only shown to users with Edit permission

**Mobile**: Full-width, collapsible

---

### 3. Create Merge Request Dialog

**Location**: Modal dialog triggered by "Create Merge Request" button

**UI Elements**:
- Title input (required, 200 char max)
- Description textarea (optional, markdown supported, 2000 char max)
- Change summary (read-only, collapsible):
  - "+3 concepts: Person, Organization, Event"
  - "~2 concepts modified: Animal, Product"
  - "-1 relationship: is-a"
  - "+5 properties added"
- Buttons:
  - "Submit for Review" (primary)
  - "Save as Draft" (secondary)
  - "Cancel"

**Validation**:
- Title required
- At least one change required
- Duplicate MR warning if applicable

**Visual Treatment**:
- Large modal (80% viewport width on desktop)
- Green "+", yellow "~", red "-" for changes
- Markdown preview for description
- Character counters for text inputs

---

### 4. Merge Request List View

**Location**: New tab in OntologyView ("Merge Requests")

**UI Elements**:
- Filter bar:
  - Status dropdown: All, Pending, Approved, Rejected, Draft, Cancelled, Stale
  - Submitter search
  - Date range picker
- Sort options: Date (newest first), Submitter, Change count
- Table/card view toggle
- Table columns:
  - Title (with link to detail)
  - Submitter (avatar + name)
  - Status (badge)
  - Change summary
  - Submitted date (relative)
  - Actions (View, Approve*, Reject*, Cancel*)
- Pagination (25 per page)
- Bulk selection checkboxes (for FullAccess users)

**Badge Colors**:
- Draft: gray
- Pending: blue
- Stale: orange
- Approved: green
- Rejected: red
- Cancelled: gray strikethrough

**Mobile**: Card view by default

---

### 5. Merge Request Detail View

**Location**: Dedicated page (e.g., /ontology/{id}/merge-requests/{mrId})

**UI Elements**:
- Header:
  - Title (editable for drafts)
  - Status badge
  - Submitter info (avatar, name, submission date)
  - Reviewer info (if approved/rejected: avatar, name, decision date)
- Tabs:
  - "Changes" (default)
  - "Discussion" (comments)
  - "History" (activity log)
- Changes tab:
  - Visual diff viewer
  - Navigation: Previous/Next change
  - Expand/collapse details
  - Search within changes
- Action buttons (contextual):
  - "Approve" (green, requires confirmation)
  - "Reject" (red, requires reason)
  - "Cancel" (for submitter)
  - "Edit" (for draft)
  - "Request Changes" (orange, requires comment)
- Conflict warning banner (if stale)

**Diff Visualization**:
- Added: Green left border, "+" icon
- Modified: Yellow left border, "~" icon, before/after columns
- Deleted: Red left border, "-" icon, strikethrough text
- Field-level diffs: highlight changed words/values

**Mobile**: Stacked layout, swipe to navigate changes

---

### 6. Notification System

**Location**: Top-right corner (toast), Notification center (dropdown)

**Notification Types**:
- MR submitted: "John Doe submitted a merge request: 'Add product concepts'"
- MR approved: "Your merge request 'Add product concepts' was approved by Jane Admin"
- MR rejected: "Your merge request 'Add product concepts' was rejected. Reason: Duplicates existing concepts."
- New MR for review: "New merge request awaiting your review"
- MR cancelled: "John Doe cancelled merge request 'Add product concepts'"

**UI Elements**:
- Toast notification: Auto-dismiss after 5 seconds
- Notification badge: Count of unread
- Notification center: List of all notifications
- Click notification to navigate to MR detail
- Mark as read/unread
- Clear all

**Visual Treatment**:
- Green: approval
- Red: rejection
- Blue: new MR
- Gray: cancellation

---

### 7. Approval Confirmation Dialog

**Location**: Modal dialog on "Approve" button click

**UI Elements**:
- Title: "Approve Merge Request?"
- Change summary: "+3 concepts, -1 relationship, 4 definitions changed"
- Warning: "These changes will be applied immediately and cannot be undone."
- Optional comment field
- Buttons:
  - "Confirm Approval" (green)
  - "Cancel"

**Validation**:
- Re-check for conflicts before allowing confirmation
- If conflict detected, show error and prevent approval

---

### 8. Rejection Dialog

**Location**: Modal dialog on "Reject" button click

**UI Elements**:
- Title: "Reject Merge Request"
- Reason textarea (required, min 10 chars, max 500 chars)
- Character counter
- Buttons:
  - "Confirm Rejection" (red)
  - "Cancel"

**Validation**:
- Reason required
- Minimum length enforced

---

## Non-Functional Requirements

### Performance
- MR list loads in <2 seconds for 1000 MRs
- Diff rendering for <100 changes in <1 second
- Approval/rejection applies in <3 seconds
- Conflict detection runs in <1 second
- Notification delivery <500ms via SignalR

### Scalability
- Support up to 10,000 MRs per ontology
- Handle 100 concurrent reviewers
- Bulk operations support up to 50 MRs
- Pagination prevents memory issues

### Security
- All MR operations require authentication
- Permission checks at service layer
- SQL injection prevention (parameterized queries)
- XSS prevention in markdown rendering
- Audit trail for all approval/rejection actions
- Cannot approve own MR (prevent self-approval abuse)

### Accessibility
- WCAG 2.1 AA compliance
- Keyboard navigation for all MR actions
- Screen reader support (ARIA labels)
- Color-blind friendly diff visualization
- Focus management in dialogs

### Usability
- Responsive design: mobile, tablet, desktop
- Touch-friendly on mobile (swipe to navigate changes)
- Loading indicators for async operations
- Clear error messages with recovery actions
- Undo warning before destructive actions

### Reliability
- Transaction integrity: all or nothing approval
- Automatic retry on network failure
- Local storage fallback for draft MRs
- Graceful degradation if SignalR fails
- Database backup before bulk approvals

---

## Success Metrics

- 90% of MRs reviewed within 24 hours
- <5% MR rejection rate (indicates good quality submissions)
- Zero data loss incidents
- 100% audit trail coverage
- User satisfaction: 4.5/5 stars for approval workflow
- <1% stale MR rate (indicates timely reviews)

---

## Open Questions

1. **Email Notifications**: Should we send email in addition to in-app notifications?
   - Recommendation: Phase 2 enhancement

2. **Reviewer Assignment**: Automatic assignment based on change type?
   - Recommendation: Manual assignment for MVP, auto-assign in Phase 2

3. **MR Templates**: Pre-defined templates for common change types?
   - Recommendation: Phase 3 enhancement

4. **Integration with CI/CD**: Auto-validation of MRs?
   - Recommendation: Phase 4 enhancement

5. **MR Dependencies**: Can one MR depend on another?
   - Recommendation: Not in MVP, consider for future

6. **Squash and Merge**: Combine multiple MRs into one before applying?
   - Recommendation: Phase 3 enhancement

---

## Related Documents

- [Technical Design](./technical-design.md)
- [Implementation Plan](./implementation-plan.md)
- [UI Mockups](./ui-mockups.md)
- [Data Model](./data-model.md)

---

**Last Updated**: November 8, 2025
**Status**: Draft for Review
**Next Step**: Technical Design Document
