# Workspace Permissions Bug - Comprehensive Requirements Document

## Executive Summary

**Bug Reported**: A user shared a workspace via share link, and the recipient could access the ontology but not the workspace notes. When the owner changed the workspace visibility to "public", the recipient still couldn't access notes - the system appeared to prioritize the share link permission level over the public visibility setting.

**Root Cause Analysis**: The permission system has an architectural inconsistency where:
1. Share links (`OntologyShare`, `UserShareAccess`) are **ontology-level** permissions
2. Workspaces have their own **workspace-level** permissions (`WorkspaceUserAccess`, `WorkspaceGroupPermission`)
3. The two permission systems are not synchronized or integrated

**Impact**: Critical - breaks the fundamental workspace collaboration model where ontology and notes should share unified permissions.

---

## 1. Current Behavior Analysis

### 1.1 Expected Behavior (Intended Design)

**When sharing a workspace via share link:**
- User should have consistent access to **both** ontology AND workspace notes
- The share link should grant permission to the entire workspace (ontology + notes)
- Permissions should be unified - one permission grants access to all workspace components

**When changing workspace visibility to public:**
- The workspace (including ontology and notes) should become publicly accessible
- Public visibility should override restrictive share link permissions
- Any authenticated user should be able to view the workspace and notes

**Permission hierarchy (intended):**
1. **Owner** - Full access to everything
2. **Public visibility** - If workspace is public, everyone can view (highest priority for access grants)
3. **Group permissions** - Members of groups with workspace access
4. **Direct user access** - Users specifically granted access via WorkspaceUserAccess
5. **Share links** - Users who accessed via share link (lowest priority)

### 1.2 Actual Broken Behavior (Current State)

**Share link scenario:**
- User clicks ontology share link → Creates `UserShareAccess` record
- `UserShareAccess` grants permission to **ontology only**
- No corresponding `WorkspaceUserAccess` record is created
- Result: User can view ontology but **not workspace notes**

**Public visibility scenario:**
- Owner changes workspace to "public"
- `OntologyPermissionService.CanViewWorkspaceAsync()` checks workspace visibility
- However, `NoteService.GetWorkspaceNotesAsync()` likely checks different permissions
- Result: User with share link access still **cannot view notes** despite public visibility

**Why this happens:**
1. **Dual permission systems**: Ontologies have their own permission model separate from workspaces
2. **No synchronization**: Share links create `UserShareAccess` (ontology) but not `WorkspaceUserAccess` (workspace)
3. **Inconsistent checks**: Different services check different permission sources
4. **No permission hierarchy**: Public visibility doesn't override share link restrictions

### 1.3 Code Evidence

**OntologyPermissionService.CanViewWorkspaceAsync()** (Lines 478-522):
```csharp
// Checks:
// 1. Owner check
// 2. Public visibility check ✓
// 3. Group permissions check ✓
// 4. Direct user access (WorkspaceUserAccess) check ✓
// MISSING: No check for OntologyShare/UserShareAccess
```

**OntologyPermissionService.CanEditWorkspaceAsync()** (Lines 527-578):
```csharp
// Checks:
// 1. Owner check
// 2. Public edit check ✓
// 3. Group permissions check ✓
// 4. Direct user access (WorkspaceUserAccess) check ✓
// MISSING: No check for OntologyShare/UserShareAccess
```

**WorkspaceRepository.UserHasAccessAsync()** (Lines 222-253):
```csharp
// Checks:
// 1. Owner check
// 2. Public visibility check ✓
// 3. Direct user access (WorkspaceUserAccess) check ✓
// 4. Group permissions check ✓
// MISSING: No check for OntologyShare/UserShareAccess
```

**Critical Gap**: None of the workspace permission checks look at `OntologyShare` or `UserShareAccess` tables, which are used by share links.

---

## 2. Permission Hierarchy Requirements

### 2.1 Unified Permission Model

**REQUIREMENT**: Implement a single, unified permission hierarchy for workspaces that considers ALL permission sources.

**Priority Order (most permissive to least permissive):**

1. **Owner** (UserId == workspace.UserId)
   - Always has full access (view, edit, manage, delete)
   - Cannot be overridden

2. **Public Visibility** (workspace.Visibility == "public")
   - If public, ALL authenticated users can view
   - If public AND workspace.AllowPublicEdit == true, ALL can edit
   - Overrides all other permission sources for view/edit access
   - Most permissive setting wins

3. **Group Permissions** (WorkspaceGroupPermission)
   - Users in groups with granted permissions
   - Permission level defined by PermissionLevel enum
   - View, ViewAndAdd, ViewAddEdit, or FullAccess

4. **Direct User Access** (WorkspaceUserAccess)
   - Users explicitly granted access to this specific workspace
   - Permission level defined by PermissionLevel enum
   - Created manually by owner or automatically via collaboration features

5. **Share Link Access** (OntologyShare → UserShareAccess)
   - Users who accessed via share link
   - Permission level defined by share link's PermissionLevel
   - Should grant access to BOTH ontology and workspace notes
   - Lowest priority but must be honored

**Rule**: If ANY permission source grants access, the user has access. Use the MOST PERMISSIVE permission level from all sources.

### 2.2 Permission Level Mapping

**PermissionLevel Enum Values:**
- `View` (0) - Read-only access
- `ViewAndAdd` (1) - Can view and add new items
- `ViewAddEdit` (2) - Can view, add, and edit
- `FullAccess` (3) - Full admin access

**Permission Resolution Logic:**
```
For each user + workspace combination:
1. If user is owner → FullAccess
2. If workspace is public:
   - If AllowPublicEdit == true → ViewAddEdit
   - Else → View
3. Check all group permissions → Take MAX(PermissionLevel)
4. Check direct user access → Take MAX(PermissionLevel)
5. Check share link access → Convert to PermissionLevel, take MAX
6. Return MAX of all permission levels found
7. If no permissions found → "none" (no access)
```

### 2.3 Share Link Integration

**REQUIREMENT**: Share links must grant access to the entire workspace, not just the ontology.

**Implementation Options:**

**Option A: Automatic WorkspaceUserAccess Creation (RECOMMENDED)**
- When user accesses share link and `UserShareAccess` is created
- Automatically create corresponding `WorkspaceUserAccess` record
- Mirror the permission level from the share link
- Keep both records in sync

**Pros:**
- Clean separation of concerns
- Existing workspace permission checks work without modification
- Easy to understand and maintain
- Share links become a "gateway" to workspace access

**Cons:**
- Data duplication
- Need to handle synchronization when share link is revoked

**Option B: Unified Permission Check**
- Modify all workspace permission methods to check BOTH:
  - WorkspaceUserAccess (direct access)
  - UserShareAccess (via share link)
- Combine results using MAX permission level

**Pros:**
- No data duplication
- Single source of truth for each permission type

**Cons:**
- More complex permission checks
- Need to modify multiple methods
- Performance impact (more joins)

**RECOMMENDED**: **Option A** - Automatic WorkspaceUserAccess creation for simplicity and performance.

---

## 3. Functional Requirements

### 3.1 Share Link Behavior

**FR-1: Share Link Access Grant**
- **Given**: User clicks on ontology share link with PermissionLevel.View
- **When**: They authenticate and access the workspace
- **Then**: They should have View access to BOTH:
  - The ontology (concepts, relationships, individuals)
  - The workspace notes section

**FR-2: Share Link Permission Levels**
- Share link with `PermissionLevel.View` → View-only access to ontology + notes
- Share link with `PermissionLevel.ViewAndAdd` → Can view and add concepts + notes
- Share link with `PermissionLevel.ViewAddEdit` → Can view, add, edit concepts + notes
- Share link with `PermissionLevel.FullAccess` → Full admin access to workspace

**FR-3: Share Link Revocation**
- **Given**: User has accessed workspace via share link
- **When**: Owner deactivates the share link (IsActive = false)
- **Then**:
  - User's `UserShareAccess` should be marked inactive or deleted
  - User's auto-created `WorkspaceUserAccess` should be removed
  - User should lose access to both ontology and notes
  - Exception: If user has OTHER permission sources (group, direct), they retain access

**FR-4: Share Link Expiration**
- **Given**: Share link has ExpiresAt date in the past
- **When**: User attempts to access workspace
- **Then**: Access should be denied with appropriate error message
- **And**: Any auto-created WorkspaceUserAccess should be removed on next access attempt

### 3.2 Visibility Change Behavior

**FR-5: Change to Public Visibility**
- **Given**: Workspace is private with some users having share link access
- **When**: Owner changes workspace.Visibility to "public"
- **Then**:
  - All authenticated users can view the workspace and notes
  - Users with share link access retain their permission level (if higher than View)
  - Public visibility takes precedence for granting access (most permissive)

**FR-6: Change from Public to Private**
- **Given**: Workspace is public
- **When**: Owner changes workspace.Visibility to "private"
- **Then**:
  - Only users with explicit permissions (group, direct, share link) can access
  - Users without explicit permissions lose access immediately

**FR-7: Public Edit Toggle**
- **Given**: Workspace is public
- **When**: Owner toggles workspace.AllowPublicEdit
- **Then**:
  - If true: All authenticated users can edit ontology and notes
  - If false: Only users with explicit edit permissions can edit

### 3.3 Permission Consistency

**FR-8: Unified Access Control**
- **Requirement**: A user's permission level must be IDENTICAL for:
  - Viewing/editing the ontology
  - Viewing/editing workspace notes
  - Uploading attachments
  - Exporting notes
  - All workspace operations

**FR-9: Permission Inheritance**
- **Given**: Workspace contains an ontology
- **When**: User is granted workspace permission
- **Then**: They automatically have equivalent permission to the ontology
- **Note**: Workspace is the parent entity; ontology is a child component

**FR-10: Legacy Ontology Compatibility**
- **Given**: Legacy ontology exists without a workspace
- **When**: User accesses the ontology
- **Then**: Auto-create workspace (existing behavior)
- **And**: Migrate all ontology permissions to workspace permissions
- **And**: Create WorkspaceUserAccess records for any UserShareAccess records

### 3.4 Permission Checking Performance

**FR-11: Optimized Permission Queries**
- Permission checks must complete in < 100ms for typical workspaces
- Use single-query permission checks (no N+1 queries)
- Cache permission results where appropriate (with 1-minute expiration)
- Use database indexes on foreign keys

**FR-12: Permission Caching (Optional Enhancement)**
- Consider caching user permissions for workspace in memory
- Cache key: `workspace_{workspaceId}_user_{userId}_permissions`
- TTL: 60 seconds (to balance freshness vs performance)
- Invalidate cache when permissions change (owner updates sharing settings)

---

## 4. User Experience Requirements

### 4.1 Real-Time Permission Updates

**UX-1: Immediate Access Grant**
- **When**: Owner grants user access (share link, group, direct)
- **Then**: User sees workspace in their list immediately (no refresh required)
- **Implementation**: Use SignalR to notify user of permission grant

**UX-2: Immediate Access Revocation**
- **When**: Owner revokes user access
- **Then**:
  - User sees error message if currently viewing workspace
  - Workspace disappears from user's list
  - User is redirected to home page
- **Implementation**: Use SignalR to notify user of permission revocation

**UX-3: Permission Level Changes**
- **When**: Owner changes user's permission level (e.g., View → Edit)
- **Then**:
  - UI updates to reflect new capabilities (edit buttons appear/disappear)
  - No page refresh required
  - Toast notification: "Your permissions for this workspace have been updated"

### 4.2 Visual Feedback

**UX-4: Permission Indicators**
- Show user's current permission level in workspace header
- Badge: "View Only", "Can Edit", "Admin", "Owner"
- Color-coded for clarity (gray, blue, green, gold)

**UX-5: Access Denied Messages**
- **Scenario 1**: User tries to access workspace without permission
  - Message: "You don't have access to this workspace. Contact the owner to request access."
  - Show workspace name and owner name (if public metadata)

- **Scenario 2**: User had access but it was revoked
  - Message: "Your access to this workspace has been revoked."
  - Redirect to home page after 3 seconds

- **Scenario 3**: Share link expired
  - Message: "This share link has expired. Contact the owner for a new link."

**UX-6: Loading States**
- Show loading spinner while checking permissions
- Don't flash "Access Denied" during loading
- Graceful degradation if permission check times out

### 4.3 Owner Experience

**UX-7: Share Link Creation Clarity**
- When creating share link, show clear message:
  - "This link grants access to the entire workspace, including ontology and notes."
  - Show permission level selector with descriptions
  - Preview what shared users will be able to do

**UX-8: Visibility Change Warnings**
- **When**: Owner changes from Public to Private
  - Warning: "Making this workspace private will restrict access. Users without explicit permissions will lose access."
  - Show count of users who currently have access

- **When**: Owner changes from Private to Public
  - Warning: "Making this workspace public will allow anyone to view it."
  - Option to also enable public editing

**UX-9: Permission Management UI**
- Show all users with access (via share links, groups, direct)
- Indicate permission source (Share Link, Group: "Team A", Direct)
- Allow owner to revoke individual access
- Show permission level for each user

---

## 5. Data Consistency Requirements

### 5.1 Database Constraints

**DC-1: Foreign Key Integrity**
- All permission tables must have proper foreign key constraints
- CASCADE delete when workspace is deleted
- NO ACTION on user deletion (preserve audit trail)

**DC-2: Permission Level Validation**
- `WorkspaceUserAccess.PermissionLevel` must be valid enum value (0-3)
- `WorkspaceGroupPermission.PermissionLevel` must be valid enum value (0-3)
- Check constraints at database level

**DC-3: Unique Constraints**
- `WorkspaceUserAccess`: UNIQUE(WorkspaceId, SharedWithUserId)
  - One user can only have ONE direct access record per workspace
- `WorkspaceGroupPermission`: UNIQUE(WorkspaceId, UserGroupId)
  - One group can only have ONE permission record per workspace

**DC-4: Visibility Values**
- `Workspace.Visibility` must be one of: "private", "group", "public"
- Database check constraint or application-level validation

### 5.2 Synchronization Requirements

**DC-5: Share Link to Workspace Access Sync**
- **Given**: User accesses via share link (UserShareAccess created)
- **Then**: Automatically create WorkspaceUserAccess with matching permission level
- **Mapping**:
  - OntologyShare.PermissionLevel → WorkspaceUserAccess.PermissionLevel
  - OntologyShare.OntologyId → Workspace (via Ontology.WorkspaceId)
  - UserShareAccess.UserId → WorkspaceUserAccess.SharedWithUserId

**DC-6: Share Link Deactivation Cleanup**
- **Given**: OntologyShare.IsActive changed to false
- **Then**: Delete corresponding WorkspaceUserAccess records
- **Exception**: Only delete if user has NO OTHER permission sources

**DC-7: Workspace Deletion Cascade**
- **When**: Workspace is deleted
- **Then**: CASCADE delete:
  - All WorkspaceUserAccess records
  - All WorkspaceGroupPermission records
  - All Notes and NoteContent
  - All Tags and NoteTagAssignments
  - The associated Ontology (or set WorkspaceId to null)

### 5.3 Audit Logging

**DC-8: Permission Change Logging**
- Log all permission grants/revocations to OntologyActivity or new PermissionAuditLog table
- Capture:
  - Timestamp
  - UserId who made the change
  - UserId affected
  - Permission level before/after
  - Permission source (share link, group, direct)
  - Action (granted, revoked, modified)

**DC-9: Access Attempt Logging**
- Log failed access attempts for security monitoring
- Capture:
  - Timestamp
  - UserId who attempted access
  - WorkspaceId
  - Reason for denial
  - Permission sources checked

---

## 6. Testing Requirements

### 6.1 Unit Test Scenarios

**Test Suite: Permission Resolution**

**T-1: Owner Always Has Access**
```csharp
[Fact]
public async Task CanViewWorkspace_OwnerAlwaysHasAccess()
{
    // Given: Workspace owned by User A
    // When: Check if User A can view
    // Then: Returns true, regardless of visibility or other settings
}

[Fact]
public async Task CanEditWorkspace_OwnerAlwaysCanEdit()
{
    // Given: Workspace owned by User A
    // When: Check if User A can edit
    // Then: Returns true, regardless of AllowPublicEdit or other settings
}
```

**T-2: Public Visibility Tests**
```csharp
[Fact]
public async Task CanViewWorkspace_PublicWorkspaceAllowsAnyUser()
{
    // Given: Workspace with Visibility = "public"
    // When: Check if User B (non-owner, no other permissions) can view
    // Then: Returns true
}

[Fact]
public async Task CanEditWorkspace_PublicWithEditAllowsAnyUser()
{
    // Given: Workspace with Visibility = "public" AND AllowPublicEdit = true
    // When: Check if User B can edit
    // Then: Returns true
}

[Fact]
public async Task CanEditWorkspace_PublicWithoutEditDeniesNonOwner()
{
    // Given: Workspace with Visibility = "public" AND AllowPublicEdit = false
    // When: Check if User B (with no other permissions) can edit
    // Then: Returns false
}
```

**T-3: Share Link Access Tests**
```csharp
[Fact]
public async Task CanViewWorkspace_ShareLinkGrantsAccess()
{
    // Given: Private workspace, User B has UserShareAccess with PermissionLevel.View
    // When: Check if User B can view workspace
    // Then: Returns true
}

[Fact]
public async Task CanViewWorkspace_ShareLinkGrantsNoteAccess()
{
    // Given: User B accessed via share link
    // When: Call NoteService.GetWorkspaceNotesAsync()
    // Then: Returns notes (not empty), no UnauthorizedAccessException
}

[Fact]
public async Task CanEditWorkspace_ShareLinkWithEditPermission()
{
    // Given: User B has UserShareAccess with PermissionLevel.ViewAddEdit
    // When: Check if User B can edit workspace
    // Then: Returns true
}

[Fact]
public async Task CanEditWorkspace_ShareLinkViewOnlyDeniesEdit()
{
    // Given: User B has UserShareAccess with PermissionLevel.View
    // When: Check if User B can edit
    // Then: Returns false
}
```

**T-4: Permission Hierarchy Tests**
```csharp
[Fact]
public async Task CanViewWorkspace_PublicOverridesNoPermissions()
{
    // Given: Public workspace, User B has no explicit permissions
    // When: Check if User B can view
    // Then: Returns true (public visibility grants access)
}

[Fact]
public async Task GetPermissionLevel_ReturnsMaxPermissionLevel()
{
    // Given:
    //   - User B has share link with PermissionLevel.View
    //   - User B is in group with PermissionLevel.ViewAddEdit
    // When: Call GetPermissionLevelAsync()
    // Then: Returns "edit" (ViewAddEdit is higher than View)
}

[Fact]
public async Task CanEditWorkspace_GroupPermissionGrantsEdit()
{
    // Given: Private workspace, User B in group with ViewAddEdit permission
    // When: Check if User B can edit
    // Then: Returns true
}
```

**T-5: Share Link Synchronization Tests**
```csharp
[Fact]
public async Task AccessShareLink_CreatesWorkspaceUserAccess()
{
    // Given: User B accesses share link for workspace
    // When: UserShareAccess record is created
    // Then: WorkspaceUserAccess record is automatically created with same permission level
}

[Fact]
public async Task DeactivateShareLink_RemovesWorkspaceUserAccess()
{
    // Given: User B has access via share link (both UserShareAccess and WorkspaceUserAccess)
    // When: Share link is deactivated (IsActive = false)
    // Then: User B's WorkspaceUserAccess record is deleted
    // And: User B can no longer access workspace
}

[Fact]
public async Task DeactivateShareLink_PreservesOtherPermissions()
{
    // Given:
    //   - User B has share link access
    //   - User B also in group with workspace permission
    // When: Share link is deactivated
    // Then: WorkspaceUserAccess from share link is removed
    // But: User B still has access via group permission
}
```

### 6.2 Integration Test Scenarios

**T-6: End-to-End Share Link Flow**
```csharp
[Fact]
public async Task ShareLinkFlow_UserCanAccessOntologyAndNotes()
{
    // 1. Owner creates workspace with ontology and notes
    // 2. Owner creates share link with PermissionLevel.View
    // 3. User B accesses share link
    // 4. User B navigates to workspace
    // 5. Assert: User B can view ontology (OntologyView page loads)
    // 6. Assert: User B can view notes (notes list populated)
    // 7. Assert: User B cannot edit (edit buttons disabled)
}
```

**T-7: Visibility Change Impact Test**
```csharp
[Fact]
public async Task VisibilityChange_PrivateToPublic_GrantsAccess()
{
    // 1. Owner creates private workspace
    // 2. User B has no permissions (cannot access)
    // 3. Owner changes workspace.Visibility to "public"
    // 4. Assert: User B can now access workspace
    // 5. Assert: User B can view notes
}

[Fact]
public async Task VisibilityChange_PublicToPrivate_RevokesAccess()
{
    // 1. Owner creates public workspace
    // 2. User B can access (no explicit permissions)
    // 3. Owner changes workspace.Visibility to "private"
    // 4. Assert: User B can no longer access workspace
    // 5. Assert: Access denied error when trying to load notes
}
```

**T-8: Permission Caching Test (if implemented)**
```csharp
[Fact]
public async Task PermissionCache_InvalidatedOnChange()
{
    // 1. User B accesses workspace (permission cached)
    // 2. Owner revokes User B's access
    // 3. User B tries to access workspace again
    // 4. Assert: Permission check returns false (cache invalidated)
    // 5. Assert: Access denied despite recent successful access
}
```

### 6.3 Edge Case Tests

**T-9: Expired Share Link**
```csharp
[Fact]
public async Task ExpiredShareLink_DeniesAccess()
{
    // Given: Share link with ExpiresAt in the past
    // When: User tries to access workspace
    // Then: Access denied, error message about expiration
}
```

**T-10: Concurrent Permission Changes**
```csharp
[Fact]
public async Task ConcurrentPermissionUpdate_NoDataLoss()
{
    // 1. Owner grants User B access
    // 2. Simultaneously, Owner grants User C access
    // 3. Assert: Both UserShareAccess records created successfully
    // 4. Assert: Both WorkspaceUserAccess records created successfully
}
```

**T-11: Guest User Access**
```csharp
[Fact]
public async Task GuestUser_CannotAccessWorkspace()
{
    // Given: Public workspace
    // When: Unauthenticated user (guest) tries to access
    // Then: Redirected to login page (WorkspaceView requires authentication)
}
```

**T-12: Deleted User Permissions**
```csharp
[Fact]
public async Task DeletedUser_PermissionsPreservedForAudit()
{
    // Given: User B has workspace access
    // When: User B's account is deleted
    // Then: WorkspaceUserAccess record remains (for audit trail)
    // But: User B cannot access (authentication fails)
}
```

### 6.4 Performance Tests

**T-13: Permission Check Performance**
```csharp
[Fact]
public async Task PermissionCheck_CompletesInUnder100ms()
{
    // Given: Workspace with 10 groups, 50 users, 20 share links
    // When: Check if User B can view workspace
    // Then: Permission check completes in < 100ms
    // And: Single database query (no N+1)
}
```

**T-14: Bulk Permission Resolution**
```csharp
[Fact]
public async Task GetAccessibleWorkspaces_HandlesLargeDataset()
{
    // Given: User B has access to 100+ workspaces via various sources
    // When: Call GetAccessibleWorkspacesAsync()
    // Then: Completes in < 500ms
    // And: Returns all 100+ workspaces
}
```

---

## 7. Security Requirements

### 7.1 Authorization Security

**SEC-1: Permission Bypass Prevention**
- All workspace access must go through permission checks
- No direct database access that bypasses permission service
- Repository methods should NOT enforce permissions (service layer responsibility)
- UI components should check permissions before rendering edit controls

**SEC-2: Horizontal Privilege Escalation Prevention**
- User cannot grant themselves higher permissions
- Only workspace owner (or admin group members with FullAccess) can modify permissions
- Permission level cannot be set higher than grantor's level

**SEC-3: IDOR (Insecure Direct Object Reference) Prevention**
- WorkspaceId in URL must be validated against user's permissions
- Return 403 Forbidden (not 404) if workspace exists but user lacks access
- Don't leak workspace existence to unauthorized users

**SEC-4: Share Link Security**
- Share tokens must be cryptographically secure (min 32 bytes, Base64URL encoded)
- Share links should be one-time-use or have usage limits (optional)
- Expired share links must not grant access (check ExpiresAt)
- Deactivated share links (IsActive = false) must not grant access

### 7.2 Input Validation

**SEC-5: Permission Level Validation**
- Validate PermissionLevel enum values (0-3)
- Reject invalid values at API layer
- Use [EnumDataType] validation attribute

**SEC-6: Visibility Validation**
- Only accept "private", "group", "public"
- Case-insensitive comparison
- Reject invalid values with 400 Bad Request

**SEC-7: UserId Validation**
- Validate UserId exists in ApplicationUsers table
- Prevent granting permissions to non-existent users
- Handle deleted users gracefully

### 7.3 Audit & Monitoring

**SEC-8: Failed Access Attempts Logging**
- Log all failed permission checks with:
  - UserId
  - WorkspaceId
  - Timestamp
  - IP address (optional)
  - User agent (optional)
- Alert on suspicious patterns (e.g., 10+ failed attempts in 1 minute)

**SEC-9: Permission Change Auditing**
- Log all permission grants/revocations
- Include: who, what, when, before/after values
- Immutable audit log (append-only)
- Retain for compliance (e.g., 1 year)

**SEC-10: Share Link Usage Tracking**
- Track share link access count (already in OntologyShare.AccessCount)
- Track last accessed timestamp
- Track unique users who accessed link
- Allow owner to see usage statistics

---

## 8. Migration & Backwards Compatibility

### 8.1 Data Migration Plan

**MIG-1: Legacy Ontology Workspace Creation**
- **Already implemented** in `WorkspaceService.EnsureWorkspaceForOntologyAsync()`
- Maintains compatibility with ontologies created before workspace-first architecture

**MIG-2: Share Link Permission Migration**
- **NEW REQUIREMENT**: For all existing UserShareAccess records:
  1. Find associated OntologyShare
  2. Find associated Ontology
  3. Find associated Workspace (via Ontology.WorkspaceId)
  4. Create WorkspaceUserAccess with matching permission level
  5. Mark migration as complete in database

**Migration Script**:
```sql
-- Create WorkspaceUserAccess for all UserShareAccess records
INSERT INTO WorkspaceUserAccesses (WorkspaceId, SharedWithUserId, PermissionLevel, CreatedAt)
SELECT
    o.WorkspaceId,
    usa.UserId,
    os.PermissionLevel,
    usa.FirstAccessedAt
FROM UserShareAccesses usa
INNER JOIN OntologyShares os ON usa.OntologyShareId = os.Id
INNER JOIN Ontologies o ON os.OntologyId = o.Id
WHERE
    os.IsActive = 1
    AND (os.ExpiresAt IS NULL OR os.ExpiresAt > GETUTCDATE())
    AND o.WorkspaceId IS NOT NULL
    AND NOT EXISTS (
        -- Don't create duplicate if already exists
        SELECT 1 FROM WorkspaceUserAccesses wua
        WHERE wua.WorkspaceId = o.WorkspaceId
        AND wua.SharedWithUserId = usa.UserId
    );
```

**MIG-3: Visibility Synchronization**
- Ensure Workspace.Visibility matches Ontology.Visibility for all existing records
- One-time sync script:
```sql
UPDATE Workspaces
SET Visibility = o.Visibility,
    AllowPublicEdit = o.AllowPublicEdit
FROM Workspaces w
INNER JOIN Ontologies o ON w.Id = o.WorkspaceId
WHERE w.Visibility != o.Visibility
   OR w.AllowPublicEdit != o.AllowPublicEdit;
```

### 8.2 Compatibility Requirements

**COMPAT-1: Existing Share Links Continue Working**
- After migration, users with existing share links should:
  - Continue to access ontology (no regression)
  - **NEW**: Also be able to access workspace notes
- No user-facing changes required

**COMPAT-2: API Backwards Compatibility**
- OntologyPermissionService methods continue to work
- No breaking changes to method signatures
- Internal implementation changes only

**COMPAT-3: UI Compatibility**
- Existing UI components (ShareModal, OntologySettings) continue working
- Graceful degradation if new features not available
- No forced UI updates for users

---

## 9. Implementation Phases

### Phase 1: Core Permission Fix (CRITICAL - Week 1)

**Tasks**:
1. Update `OntologyPermissionService.CanViewWorkspaceAsync()` to check UserShareAccess
2. Update `OntologyPermissionService.CanEditWorkspaceAsync()` to check UserShareAccess
3. Update `WorkspaceRepository.UserHasAccessAsync()` to check UserShareAccess
4. Write unit tests for share link permission checks
5. Test end-to-end: share link → workspace → notes access

**Deliverables**:
- Users with share links can access workspace notes
- All 20+ permission unit tests passing

### Phase 2: Data Migration (CRITICAL - Week 1)

**Tasks**:
1. Write EF Core migration to add database constraints
2. Write data migration script (MIG-2) for existing UserShareAccess
3. Test migration on copy of production database
4. Execute migration in production during maintenance window
5. Verify all existing share link users have WorkspaceUserAccess

**Deliverables**:
- All existing share link users have workspace access
- No data loss or corruption

### Phase 3: Automatic Sync (HIGH PRIORITY - Week 2)

**Tasks**:
1. Update `OntologyShareService.AcceptShareAsync()` (or equivalent) to create WorkspaceUserAccess
2. Update share link deactivation to remove WorkspaceUserAccess
3. Add background job to sync deactivated/expired share links
4. Write integration tests for sync behavior
5. Test concurrent access scenarios

**Deliverables**:
- New share links automatically grant workspace access
- Deactivating share link revokes workspace access

### Phase 4: Permission Hierarchy (MEDIUM PRIORITY - Week 2-3)

**Tasks**:
1. Implement GetPermissionLevelAsync() to aggregate all permission sources
2. Ensure public visibility takes precedence
3. Add permission resolution caching (optional)
4. Write performance tests
5. Optimize database queries (minimize joins)

**Deliverables**:
- Permission checks complete in < 100ms
- Correct permission hierarchy enforced

### Phase 5: UX Enhancements (LOW PRIORITY - Week 3-4)

**Tasks**:
1. Add permission level badge to workspace header
2. Improve access denied error messages
3. Add real-time permission change notifications (SignalR)
4. Add permission management UI for owners
5. Add share link creation clarity improvements

**Deliverables**:
- Better user experience for shared workspaces
- Clear communication of permission levels

### Phase 6: Audit & Security (ONGOING)

**Tasks**:
1. Add permission change audit logging
2. Add failed access attempt logging
3. Set up monitoring/alerting for suspicious patterns
4. Security review of permission implementation
5. Penetration testing for privilege escalation

**Deliverables**:
- Complete audit trail of permission changes
- Security vulnerabilities identified and fixed

---

## 10. Acceptance Criteria

### AC-1: Share Link Access (CRITICAL)
- [ ] User can access workspace via share link
- [ ] User can view ontology via share link
- [ ] User can view workspace notes via share link
- [ ] Permission level from share link is honored (View vs Edit)

### AC-2: Public Visibility (CRITICAL)
- [ ] Public workspace allows any user to view ontology
- [ ] Public workspace allows any user to view notes
- [ ] Public workspace with AllowPublicEdit allows any user to edit
- [ ] Changing to public immediately grants access

### AC-3: Permission Hierarchy (HIGH)
- [ ] Owner always has full access
- [ ] Public visibility grants access to all
- [ ] Group permissions work correctly
- [ ] Direct user access works correctly
- [ ] Share link access works correctly
- [ ] Most permissive permission wins

### AC-4: Data Consistency (HIGH)
- [ ] UserShareAccess automatically creates WorkspaceUserAccess
- [ ] Deactivating share link removes WorkspaceUserAccess
- [ ] Workspace and Ontology visibility stay in sync
- [ ] No orphaned permission records

### AC-5: Performance (MEDIUM)
- [ ] Permission checks complete in < 100ms
- [ ] No N+1 query problems
- [ ] GetAccessibleWorkspaces scales to 100+ workspaces
- [ ] Database indexes improve query performance

### AC-6: User Experience (MEDIUM)
- [ ] Clear error messages for access denied
- [ ] Permission level visible to user
- [ ] Real-time updates when permissions change
- [ ] Share link creation shows what will be shared

### AC-7: Security (HIGH)
- [ ] No permission bypass vulnerabilities
- [ ] No horizontal privilege escalation
- [ ] Share links are cryptographically secure
- [ ] Failed access attempts are logged
- [ ] Permission changes are audited

### AC-8: Testing (HIGH)
- [ ] All 20+ unit tests passing
- [ ] All integration tests passing
- [ ] Edge cases covered
- [ ] Performance tests passing

### AC-9: Migration (CRITICAL)
- [ ] All existing UserShareAccess have WorkspaceUserAccess
- [ ] No data loss during migration
- [ ] Existing share links continue working
- [ ] No user-facing breaking changes

### AC-10: Documentation (MEDIUM)
- [ ] Permission system documented
- [ ] API documentation updated
- [ ] User guide updated
- [ ] Migration guide for administrators

---

## 11. Risks & Mitigation

### Risk 1: Data Migration Failure
**Probability**: Low | **Impact**: Critical
- **Mitigation**:
  - Test migration on copy of production database
  - Have rollback plan ready
  - Execute during low-traffic maintenance window
  - Monitor error logs during migration

### Risk 2: Performance Degradation
**Probability**: Medium | **Impact**: Medium
- **Mitigation**:
  - Add database indexes before migration
  - Test with production-scale data
  - Implement permission caching if needed
  - Monitor query performance after deployment

### Risk 3: Permission Bypass Vulnerability
**Probability**: Low | **Impact**: Critical
- **Mitigation**:
  - Security code review before deployment
  - Penetration testing of permission system
  - Add comprehensive logging for failed access attempts
  - Monitor for suspicious access patterns

### Risk 4: Breaking Existing Functionality
**Probability**: Medium | **Impact**: High
- **Mitigation**:
  - Comprehensive integration tests
  - Manual testing of all share link scenarios
  - Phased rollout (internal users first)
  - Feature flag to rollback if needed

### Risk 5: User Confusion
**Probability**: Medium | **Impact**: Low
- **Mitigation**:
  - Clear error messages
  - Permission level indicators in UI
  - User documentation
  - Support team prepared for questions

---

## 12. Success Metrics

### Primary Metrics
1. **Share Link Success Rate**: % of share link accesses that successfully grant workspace access
   - Target: 100% (up from current ~50% for notes access)

2. **Permission Check Latency**: Average time to complete permission check
   - Target: < 100ms (p95)

3. **Access Denied Error Rate**: % of legitimate users seeing access denied errors
   - Target: < 1% (down from current ~50% for share link users)

### Secondary Metrics
4. **User Support Tickets**: Number of tickets related to workspace access issues
   - Target: Reduce by 80%

5. **Share Link Usage**: Number of share links created and actively used
   - Track: Monitor for increase after fix (better UX → more sharing)

6. **Workspace Collaboration**: Number of workspaces with 2+ active collaborators
   - Track: Monitor for increase after fix

### Technical Metrics
7. **Database Query Count**: Queries per permission check
   - Target: 1 query (no N+1)

8. **Test Coverage**: Permission-related code coverage
   - Target: > 90%

9. **Migration Success Rate**: % of UserShareAccess records successfully migrated
   - Target: 100%

---

## 13. Appendix: Database Schema Changes

### New Indexes (Performance)

```sql
-- Improve WorkspaceUserAccess queries
CREATE INDEX IX_WorkspaceUserAccesses_WorkspaceId
ON WorkspaceUserAccesses(WorkspaceId);

CREATE INDEX IX_WorkspaceUserAccesses_SharedWithUserId
ON WorkspaceUserAccesses(SharedWithUserId);

CREATE UNIQUE INDEX IX_WorkspaceUserAccesses_WorkspaceId_SharedWithUserId
ON WorkspaceUserAccesses(WorkspaceId, SharedWithUserId);

-- Improve WorkspaceGroupPermissions queries
CREATE INDEX IX_WorkspaceGroupPermissions_WorkspaceId
ON WorkspaceGroupPermissions(WorkspaceId);

CREATE INDEX IX_WorkspaceGroupPermissions_UserGroupId
ON WorkspaceGroupPermissions(UserGroupId);

CREATE UNIQUE INDEX IX_WorkspaceGroupPermissions_WorkspaceId_UserGroupId
ON WorkspaceGroupPermissions(WorkspaceId, UserGroupId);

-- Improve UserShareAccess queries
CREATE INDEX IX_UserShareAccesses_UserId
ON UserShareAccesses(UserId);

CREATE INDEX IX_UserShareAccesses_OntologyShareId
ON UserShareAccesses(OntologyShareId);
```

### New Check Constraints (Data Integrity)

```sql
-- Ensure Visibility is valid
ALTER TABLE Workspaces
ADD CONSTRAINT CK_Workspaces_Visibility
CHECK (Visibility IN ('private', 'group', 'public'));

-- Ensure PermissionLevel is valid (0-3)
ALTER TABLE WorkspaceUserAccesses
ADD CONSTRAINT CK_WorkspaceUserAccesses_PermissionLevel
CHECK (PermissionLevel BETWEEN 0 AND 3);

ALTER TABLE WorkspaceGroupPermissions
ADD CONSTRAINT CK_WorkspaceGroupPermissions_PermissionLevel
CHECK (PermissionLevel BETWEEN 0 AND 3);
```

---

## 14. Contact & Ownership

**Document Owner**: Requirements Architect (Claude Code)
**Primary Developer**: TBD
**Reviewer**: Technical Lead
**Approver**: Product Owner

**Created**: 2025-11-18
**Last Updated**: 2025-11-18
**Status**: Draft - Pending Review

---

**END OF REQUIREMENTS DOCUMENT**
