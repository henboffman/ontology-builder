# Workspace Creation Enhancement

**Feature**: Enhanced Workspace Creation Dialog
**Date**: November 22, 2025
**Status**: 📋 Planning - Awaiting Approval
**Estimated Effort**: 2-3 days

## Quick Summary

This feature enhances the workspace creation dialog to provide:
1. **Simplified UI**: Remove legacy ontology-specific fields (BFO, PROV-O checkboxes)
2. **Privacy Controls**: Set workspace visibility during creation (Private, Group, Public)
3. **Inline Group Creation**: Create and configure groups on-the-fly
4. **Direct User Access**: Grant specific users access without share links
5. **Better Defaults**: Version starts at 0.1 instead of 1.0

## Documentation Structure

This folder contains comprehensive planning documentation for the feature:

### 📄 [requirements.md](./requirements.md)
Complete functional and non-functional requirements including:
- User stories
- Acceptance criteria
- Success metrics
- Out of scope items

### 🎯 [design-decisions.md](./design-decisions.md)
Key architectural and UX decisions with rationale:
- **DD1**: Groups are globally reusable (not workspace-specific)
- **DD2**: Inline expandable UI (not modal-within-modal)
- **DD3**: Default permission level is ViewAddEdit
- **DD4**: Version default changed to 0.1
- **DD5**: User search with server-side autocomplete
- **DD6**: Large modal, single-column layout

### 🛠️ [implementation-plan.md](./implementation-plan.md)
Detailed 7-phase implementation plan:
1. Cleanup & remove legacy fields
2. Privacy selection UI
3. Group selection & creation
4. Direct user access
5. Service integration
6. Testing
7. Documentation

## Key Design Decisions

### Groups: Workspace-Specific or Reusable?

**Decision**: ✅ **Globally Reusable Groups**

**Rationale**:
- Matches existing group management in admin interface
- Users can create "Research Team" once, use for multiple workspaces
- No database schema changes needed
- Better long-term scalability
- Less data duplication

### User Access: Share Links vs Direct Grants?

**Decision**: ✅ **Move toward Direct User Grants**

**Rationale**:
- More explicit and secure
- Better access tracking
- No link expiration issues
- Easier permission management
- Share links still available for public workspaces

### UI Layout: Modal Size and Structure?

**Decision**: ✅ **Large Modal, Single Column**

**Removed**:
- "What is an Ontology?" sidebar (no longer relevant for workspaces)
- Ontology Frameworks section (BFO, PROV-O)
- Author field (auto-populated from current user)

**Structure**:
1. Name (required)
2. Description (optional)
3. Privacy Level (required, default: Private)
4. Group Settings (expandable if Group selected)
5. Direct User Access (expandable checkbox)
6. Action buttons

## Visual Mockup

```
┌────────────────────────────────────────────────────────┐
│  Create New Workspace                              [X] │
├────────────────────────────────────────────────────────┤
│                                                        │
│  Name *                                                │
│  ┌──────────────────────────────────────────────────┐ │
│  │ My Research Workspace                            │ │
│  └──────────────────────────────────────────────────┘ │
│                                                        │
│  Description                                           │
│  ┌──────────────────────────────────────────────────┐ │
│  │ Workspace for my research project on...          │ │
│  │                                                   │ │
│  └──────────────────────────────────────────────────┘ │
│                                                        │
│  Privacy Level *                                       │
│  ┌──────────────────────────────────────────────────┐ │
│  │ 👥 Group - Share with a team or group        ▼  │ │
│  └──────────────────────────────────────────────────┘ │
│  Only selected group members can access this          │
│  workspace.                                            │
│                                                        │
│  ┌─ Group Settings ─────────────────────────────────┐ │
│  │                                                   │ │
│  │ Select Group                                      │ │
│  │ ┌───────────────────────────────────────────────┐│ │
│  │ │ + Create New Group                          ▼││ │
│  │ └───────────────────────────────────────────────┘│ │
│  │                                                   │ │
│  │ New Group Name                                    │ │
│  │ ┌───────────────────────────────────────────────┐│ │
│  │ │ Research Team Alpha                           ││ │
│  │ └───────────────────────────────────────────────┘│ │
│  │                                                   │ │
│  │ Add Members                                       │ │
│  │ ┌───────────────────────────────────────────────┐│ │
│  │ │ Search users...                               ││ │
│  │ └───────────────────────────────────────────────┘│ │
│  │ [alice@example.com] [bob@example.com] [x]       │ │
│  └───────────────────────────────────────────────────┘ │
│                                                        │
│  ☐ Grant access to specific users                     │
│                                                        │
├────────────────────────────────────────────────────────┤
│              [Cancel]  [Create Workspace]              │
└────────────────────────────────────────────────────────┘
```

## Questions for User

Before implementing, please review and answer:

### Q1: Group Management Strategy

We've decided on **globally reusable groups** (can be used across multiple workspaces). Do you agree with this approach, or would you prefer workspace-specific groups?

**Pros of Reusable Groups**:
- ✅ Create once, use many times
- ✅ Centralized management
- ✅ No schema changes needed

**Cons**:
- ❌ Might be confusing (groups from different contexts)
- ❌ Need unique naming

### Q2: Permission Level Defaults

We've chosen **ViewAddEdit** as the default permission for group members. Is this the right level, or should it be:
- **View** (read-only)
- **ViewAndAdd** (can add, not edit)
- **ViewAddEdit** (can view, add, edit) ← Current choice
- **FullAccess** (can delete, manage)

### Q3: User Search Scope

When users search for people to grant access, should we:
- **Option A**: Show all users in the system
- **Option B**: Only show users in the same organization (if applicable)
- **Option C**: Only show users they've collaborated with before

### Q4: UI Complexity

Does the mockup look too complex? Should we:
- **Keep as is**: All options in one dialog
- **Simplify**: Remove some advanced options (direct user access)
- **Multi-step**: Break into wizard (Name/Description → Privacy → Access)

### Q5: Backwards Compatibility

Old workspaces don't have privacy settings configured. Should we:
- **Ignore**: Keep them as-is (privacy set on creation only)
- **Migrate**: Add UI to edit privacy level post-creation
- **Prompt**: Show reminder to configure privacy on old workspaces

## Next Steps

### Option 1: Proceed with Current Design
If you approve the current design:
1. I'll start implementation (Phase 1: Cleanup)
2. Estimated 2-3 days for full implementation
3. Will create PR for review when complete

### Option 2: Revise Design
If you have concerns:
1. Please answer the questions above
2. Suggest any changes to requirements or design
3. I'll update documentation and mockups
4. We'll review again before implementing

### Option 3: Pilot/MVP Approach
Start with minimal implementation:
1. Phase 1-2 only (cleanup + privacy selection)
2. Skip inline group creation (use existing groups only)
3. Skip direct user access (add later)
4. Get user feedback, then build Phase 3-4

## Files Modified

**Created**:
- `docs/development-ledger/2025-11-22-workspace-creation-enhancement/README.md` (this file)
- `docs/development-ledger/2025-11-22-workspace-creation-enhancement/requirements.md`
- `docs/development-ledger/2025-11-22-workspace-creation-enhancement/design-decisions.md`
- `docs/development-ledger/2025-11-22-workspace-creation-enhancement/implementation-plan.md`

**To Be Modified** (during implementation):
- `Components/Pages/Workspaces.razor` - Update create dialog
- `Services/WorkspaceService.cs` - Add privacy/access parameters
- `Services/UserGroupService.cs` - No changes needed (reuse existing)
- `Endpoints/UserSearchEndpoint.cs` - NEW: User search API
- `Components/Shared/UserPicker.razor` - NEW: User selection component
- `Components/Shared/UserGroupSelector.razor` - NEW: Group selection component
- `Eidos.Tests/` - Add comprehensive tests

## Timeline

**Planning**: ✅ Complete (November 22, 2025)
**Implementation**: 🔜 2-3 days (pending approval)
**Testing**: 🔜 Included in implementation
**Documentation**: 🔜 Included in implementation
**Deployment**: 🔜 After PR review and approval

## Contact

For questions or feedback on this feature, please:
1. Review the documentation files in this folder
2. Answer the questions in this README
3. Suggest changes or alternatives
4. Approve to proceed with implementation

---

**Last Updated**: November 22, 2025
**Status**: Awaiting user review and approval
