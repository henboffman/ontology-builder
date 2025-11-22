# Design Decisions - Workspace Creation Enhancement

**Date**: November 22, 2025
**Feature**: Enhanced Workspace Creation Dialog

## Decision Log

### DD1: Group Scope - Workspace-Specific vs Globally Reusable

**Date**: 2025-11-22
**Status**: ✅ DECIDED - Globally Reusable Groups
**Decider**: Product Team

#### Context

When users create groups during workspace creation, should these groups be:
- **Option A**: Scoped only to that workspace (workspace-specific)
- **Option B**: Available globally for reuse across workspaces

#### Decision

**Selected Option**: B - Globally Reusable Groups

#### Rationale

**Pros of Globally Reusable Groups**:
1. **Consistency**: Matches existing group management in admin interface
2. **Reusability**: Users can create "Research Team" group once, use it for multiple workspaces
3. **Centralized Management**: All groups visible in one place
4. **Less Duplication**: Avoids creating "Research Team 1", "Research Team 2", etc.
5. **Future-Proof**: Enables organization-level features (team workspaces, etc.)
6. **Database Design**: Current schema supports this naturally (UserGroup table has no WorkspaceId)

**Cons of Globally Reusable Groups**:
1. **Complexity**: Users might be confused seeing groups from other workspaces
2. **Naming Conflicts**: Must ensure unique group names
3. **Permissions Complexity**: A group can have different permissions on different workspaces

**Why We Rejected Workspace-Specific**:
1. Would require schema changes (add WorkspaceId to UserGroup)
2. Creates data duplication (same team, multiple group records)
3. Harder to manage groups across workspaces
4. Contradicts existing admin group management

#### Implementation Notes

- Use existing UserGroupService.CreateGroupAsync
- Display existing groups in a dropdown/selector
- Show "Create New Group" as a prominent option
- Consider adding a "suggested groups" feature based on user's memberships
- Add visual indicator showing which groups are already in use

#### Consequences

**Positive**:
- Leverages existing infrastructure
- No database migration needed
- Simpler implementation
- Better long-term scalability

**Negative**:
- Need to handle group naming conflicts
- Must provide clear UI for existing vs new groups
- Users need education on group reusability

---

### DD2: User Access Grant UI - Modal vs Inline

**Date**: 2025-11-22
**Status**: ✅ DECIDED - Inline with Expandable Section
**Decider**: UX Team

#### Context

How should we present the "Grant Access to Specific Users" feature in the create dialog?

**Options**:
- **Option A**: Separate modal dialog (click "Add Users" → modal opens)
- **Option B**: Inline expandable section in create dialog
- **Option C**: Separate step in multi-step wizard

#### Decision

**Selected Option**: B - Inline Expandable Section

#### Rationale

**Why Inline Expandable**:
1. **Single Context**: All creation settings in one view
2. **No Modal Stack**: Avoids modal-within-modal complexity
3. **Progressive Disclosure**: Show section only when relevant
4. **Mobile Friendly**: Better than nested modals on mobile
5. **Quick Access**: No extra clicks to add users

**Why Not Modal**:
- Modal-within-modal is confusing
- Breaks flow and context
- Harder to implement in Blazor

**Why Not Wizard**:
- Overkill for this flow
- More clicks required
- Users want "quick create"

#### Implementation

```html
<!-- Privacy Selection -->
<select @bind="privacy">
  <option value="private">Private</option>
  <option value="group">Group</option>
  <option value="public">Public</option>
</select>

<!-- Expandable Section for Group -->
@if (privacy == "group")
{
  <div class="group-settings">
    <!-- Group selector or create new -->
  </div>
}

<!-- Expandable Section for Direct User Access -->
<div class="user-access-section">
  <label>
    <input type="checkbox" @bind="showUserAccess" />
    Grant access to specific users
  </label>

  @if (showUserAccess)
  {
    <!-- User picker component -->
  }
</div>
```

---

### DD3: Permission Level Default for Group Members

**Date**: 2025-11-22
**Status**: ✅ DECIDED - ViewAddEdit (Can view and edit)
**Decider**: Product Team

#### Context

When a user creates a group during workspace creation, what default permission should group members have?

**Options**:
- **View**: Read-only access
- **ViewAndAdd**: Can view and add new concepts/notes
- **ViewAddEdit**: Can view, add, and edit (default for collaboration)
- **FullAccess**: Admin rights

#### Decision

**Selected Option**: ViewAddEdit (Can view, add, and edit)

#### Rationale

**Why ViewAddEdit**:
1. **Collaboration Intent**: If creating a group, user wants to collaborate
2. **Least Surprise**: Most users expect "group members can edit"
3. **Matches Common Use Cases**: Research teams, project groups need edit access
4. **Can Be Overridden**: User can change per-group permission level later

**Why Not View**:
- Too restrictive for collaboration
- Not useful for most group scenarios

**Why Not FullAccess**:
- Too permissive (members can delete workspace)
- Security risk

#### Implementation

- Set `PermissionLevel.ViewAddEdit` when creating WorkspaceGroupPermission
- Allow user to override in UI (advanced option)
- Document this default in user guide

---

### DD4: Version Number Default Change

**Date**: 2025-11-22
**Status**: ✅ DECIDED - Change to 0.1
**Decider**: Product Team

#### Context

Currently, new workspaces default to version "1.0". Should we change this?

#### Decision

**Selected Option**: Change default to "0.1"

#### Rationale

**Why 0.1**:
1. **Semantic Versioning**: 0.x indicates pre-1.0 development
2. **User Expectation**: "1.0" implies completeness, most new workspaces are drafts
3. **Flexibility**: Leaves room for 0.2, 0.3, etc. before "1.0 release"
4. **Convention**: Many software projects start at 0.1

**Impact**:
- Low risk change (just a default value)
- Users can still enter "1.0" manually if desired
- Update WorkspaceService.CreateWorkspaceAsync (line 73)

---

### DD5: User Lookup/Search Implementation

**Date**: 2025-11-22
**Status**: 🔄 PROPOSED - Autocomplete with Server-Side Search
**Decider**: TBD

#### Context

How should we implement user lookup for granting access?

**Options**:
- **Option A**: Autocomplete with server-side search (search as you type)
- **Option B**: Dropdown with client-side filtering (load all users)
- **Option C**: Tag input with autocomplete (like email fields)

#### Proposed Decision

**Option A**: Autocomplete with Server-Side Search

#### Rationale

**Why Server-Side Search**:
1. **Scalability**: Works with thousands of users
2. **Performance**: Only loads matching results
3. **Privacy**: Doesn't expose full user list
4. **UX**: Common pattern (familiar to users)

**Implementation Approach**:
```csharp
// New API endpoint
[HttpGet("/api/users/search")]
public async Task<List<UserSearchResult>> SearchUsers(string query)
{
    // Search by username, email, display name
    // Limit to 10 results
    // Return: Id, Email, DisplayName, PhotoUrl
}
```

Component:
- Use Blazored.Typeahead or similar component
- Debounce search (300ms)
- Show user avatar + name + email
- Multi-select support

**Open Questions**:
- Should we restrict search to organization members only?
- How to handle users with similar names?
- What if user has no display name?

---

### DD6: UI Layout - Dialog Size and Structure

**Date**: 2025-11-22
**Status**: ✅ DECIDED - Large Modal (lg), Single Column
**Decider**: UX Team

#### Context

The create dialog will have more content with privacy controls and user selection.

#### Decision

**Layout**:
- Modal size: `modal-lg` (Bootstrap large modal)
- Structure: Single column, vertically stacked sections
- Sections (in order):
  1. Name (required)
  2. Description (optional)
  3. Privacy Level (required, default: Private)
  4. Group Settings (if Group selected)
  5. Direct User Access (expandable, optional)
  6. Action buttons (Create / Cancel)

**Why Single Column**:
- More vertical space for expanding sections
- Clearer reading flow
- Better mobile experience
- Removes need for 2-column layout from old design

**Removed**:
- "What is an Ontology?" sidebar (no longer needed)
- Author field (auto-populated from current user)
- Version field (auto-populated to 0.1, can be edited later)

---

## Future Considerations

### Workspace Templates

Consider adding workspace templates in the future:
- "Personal Knowledge Base" (Private, empty)
- "Research Project" (Group, with standard structure)
- "Public Documentation" (Public, view-only)

### Bulk Operations

For future enhancement:
- Import users from CSV
- Copy group from another workspace
- Workspace duplication with permissions

### Advanced Permissions

Consider more granular permissions:
- Read-only on concepts, edit on notes
- Per-concept or per-relationship permissions
- Time-limited access grants

---

## Questions for User Feedback

1. Is the privacy selector clear enough? Do we need better explanations?
2. Should we show a "Who will have access?" preview before creating?
3. Is inline group creation intuitive, or should it be a separate step?
4. Do users want to set permission levels per user, or is a single level OK?

---

**Last Updated**: November 22, 2025
**Status**: Active - decisions being implemented
