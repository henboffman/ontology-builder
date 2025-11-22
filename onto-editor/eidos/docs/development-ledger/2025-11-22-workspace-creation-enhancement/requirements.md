# Workspace Creation Enhancement - Requirements

**Date**: November 22, 2025
**Feature**: Enhanced Workspace Creation Dialog
**Status**: Planning

## Overview

This document outlines requirements for enhancing the workspace creation dialog to provide streamlined privacy controls, inline group management, and direct user access grants.

## Business Goals

1. **Simplified Privacy Management**: Allow users to set privacy settings when creating a workspace instead of requiring post-creation configuration
2. **Reduced Friction**: Enable group creation and member selection directly in the create flow
3. **Better Access Control**: Move away from share links toward explicit user-based access grants
4. **Improved UX**: Streamline the creation experience by removing unnecessary fields and adding relevant controls

## Functional Requirements

### FR1: Privacy Selection in Create Dialog

**Description**: Users must be able to select workspace privacy level during creation.

**Acceptance Criteria**:
- Privacy selector with three options: Private (default), Group, Public
- Clear explanation of each privacy level
- Private: Only owner and explicitly granted users can access
- Group: Accessible to selected user groups
- Public: Anyone can view (with optional edit permissions)
- Selected privacy level is applied to both the workspace AND its associated ontology

### FR2: Inline Group Creation

**Description**: When "Group" privacy is selected, users can create a new group on-the-fly.

**Acceptance Criteria**:
- "Create New Group" button appears when Group privacy is selected
- Inline group creation form with:
  - Group name (required)
  - Group description (optional)
  - Member selection (multi-select user picker)
  - Color picker for group badge (optional)
- Ability to select existing groups OR create new group
- New group is created when workspace is saved (not before)
- Group creator is automatically added as group admin

### FR3: Direct User Access Grant

**Description**: Users can grant access to specific individuals directly (without using share links).

**Acceptance Criteria**:
- User lookup/autocomplete component
- Search by username, email, or display name
- Multi-select to grant access to multiple users
- Permission level selector per user (View, ViewAndAdd, ViewAddEdit, FullAccess)
- Creates WorkspaceUserAccess entries for each selected user
- Works for all privacy levels (Private, Group, Public)

### FR4: Remove Legacy Fields

**Description**: Clean up the create dialog by removing deprecated ontology-specific fields.

**Acceptance Criteria**:
- Remove "Ontology Frameworks" section entirely
  - Remove "Use Basic Formal Ontology (BFO)" checkbox
  - Remove "Use PROV-O for provenance tracking" checkbox
- Remove "What is an Ontology?" sidebar panel
- Keep essential fields: Name, Description
- Change default version from "1.0" to "0.1"

### FR5: Group Management Strategy

**Description**: Determine whether groups created via this flow are workspace-specific or reusable.

**Decision Points**:
- **Option A**: Workspace-Specific Groups
  - Groups created here are scoped to this workspace only
  - Not visible in admin group management
  - Automatically deleted when workspace is deleted
  - Simpler mental model for users

- **Option B**: Reusable Groups
  - Groups created here appear in global group list
  - Can be reused for other workspaces
  - Persist even if the workspace is deleted
  - More flexible but potentially confusing

**Recommendation**: Option B (Reusable Groups) - See design-decisions.md for rationale

## Non-Functional Requirements

### NFR1: Performance
- Group creation and permission assignment must complete within 2 seconds
- User search autocomplete must respond within 200ms
- No noticeable lag when switching between privacy levels

### NFR2: Security
- Only authenticated users can create workspaces
- Users can only grant permissions they themselves have
- Input validation on all fields (max lengths, required fields)
- XSS protection on user-generated content (group names, descriptions)

### NFR3: Usability
- Privacy selection is clear and unambiguous
- Group creation flow is intuitive (no modal-within-modal)
- User search provides helpful feedback (no results, loading states)
- Error messages are actionable and specific

### NFR4: Compatibility
- Works with existing WorkspaceService
- Works with existing UserGroupService
- Maintains backwards compatibility with existing workspaces
- Mobile-responsive design

## User Stories

### US1: Private Workspace for Personal Use
**As a** user creating a personal knowledge base
**I want to** create a private workspace with one click
**So that** I can keep my notes and ontology private without additional configuration

### US2: Collaborative Workspace with Team
**As a** team lead
**I want to** create a workspace and add my team members during creation
**So that** they have immediate access without needing to manage share links

### US3: Research Group Workspace
**As a** researcher
**I want to** create a workspace for my research group and define the group inline
**So that** I can organize my collaborators and grant them appropriate permissions

### US4: Public Knowledge Base
**As a** knowledge curator
**I want to** create a public workspace that anyone can view
**So that** I can share my structured knowledge with the world

## Out of Scope

The following are explicitly **not** included in this initial implementation:

1. Editing workspace privacy after creation (separate feature)
2. Group management UI overhaul (separate feature)
3. Advanced permission levels (concept-level, relationship-level)
4. Workspace templates or presets
5. Bulk user import (CSV, etc.)
6. Team/organization-level workspace management
7. Email invitations to non-registered users

## Dependencies

- Existing WorkspaceService (Services/WorkspaceService.cs)
- Existing UserGroupService (Services/UserGroupService.cs)
- Existing permission models (WorkspaceGroupPermission, WorkspaceUserAccess)
- User lookup/search API (may need new endpoint)
- Blazor component library (Bootstrap 5)

## Success Metrics

1. **Adoption**: 80%+ of new workspaces created with privacy settings configured
2. **Group Usage**: 30%+ of new workspaces use group-based permissions
3. **User Satisfaction**: Positive feedback on simplified creation flow
4. **Time to Create**: Median workspace creation time reduced by 40%
5. **Share Link Reduction**: 50% reduction in share link creation (in favor of direct grants)

## Open Questions

1. ✅ Should groups be workspace-specific or globally reusable? **Decision: Globally reusable**
2. Should we allow editing privacy level after creation in this same feature?
3. What permission level should be default for group members?
4. Should we show a preview of who will have access before creating?
5. How do we handle very large user lists (500+ users)?

## References

- [CLAUDE.md](../../../CLAUDE.md) - Project context
- [Workspace Architecture](../../architecture/WORKSPACE_ARCHITECTURE.md)
- [Group Management](../../) - Related feature documentation

---

**Last Updated**: November 22, 2025
**Status**: Planning - awaiting design decisions and user feedback
