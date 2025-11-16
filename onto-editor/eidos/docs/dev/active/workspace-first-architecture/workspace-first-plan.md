# Workspace-First Architecture Plan

**Created:** 2025-11-16
**Last Updated:** 2025-11-16
**Status:** Planning

## Overview

This plan details a significant architectural shift in Eidos to make **Workspaces** the primary container for both ontologies and notes, with unified permission management and consistent UI/UX across views.

## Current Architecture Issues

### 1. Dual Entry Points
- Users can create ontologies OR workspaces independently
- Confusing mental model: "Do I create a workspace or an ontology?"
- Permissions managed separately for ontologies and workspaces
- Inconsistent UI between ontology view and workspace view

### 2. Inconsistent Permissions
- Ontology permissions managed via `OntologyPermissionService`
- Workspace permissions exist but may not be fully synchronized
- Notes inherit workspace permissions, but ontologies have separate permission system
- Risk of permission mismatches between ontology and its workspace notes

### 3. UI Inconsistencies
- Share, Settings, Import, Export buttons only visible in ontology view
- Workspace view lacks these critical functions
- Users must switch contexts to perform administrative tasks

## Proposed Architecture

### Core Concept: Workspace as Primary Container

```
Workspace (Primary Container)
├── Ontology (1:1 relationship)
│   ├── Concepts
│   ├── Relationships
│   └── Individuals
├── Notes
│   ├── Workspace Notes
│   └── Concept Notes
├── Tags
└── Attachments

Permissions managed at Workspace level
```

### Key Principles

1. **Workspace-First Creation**: Users always create workspaces, which automatically include an ontology
2. **Unified Permissions**: All permission checks happen at workspace level
3. **Consistent Toolbar**: Share, Settings, Import, Export available in all views
4. **1:1 Relationship**: Every workspace has exactly one ontology; every ontology belongs to exactly one workspace

## Architecture Changes

### 1. Database Schema Changes

#### Current State
- `Ontologies` table with separate permissions
- `Workspaces` table with separate permissions
- Loose relationship between them

#### Proposed State
- `Workspaces` table becomes the primary entity
- `Ontologies` table has required `WorkspaceId` foreign key (NOT NULL)
- All permission tables reference `WorkspaceId` instead of `OntologyId`

#### Migration Strategy
```sql
-- Step 1: Ensure all ontologies have workspaces
-- Create workspace for any ontology without one

-- Step 2: Add NOT NULL constraint to Ontologies.WorkspaceId
ALTER TABLE Ontologies
ALTER COLUMN WorkspaceId int NOT NULL;

-- Step 3: Migrate permissions
-- Move OntologyGroupPermissions to WorkspaceGroupPermissions
-- Update all permission foreign keys

-- Step 4: Add unique constraint
ALTER TABLE Ontologies
ADD CONSTRAINT UC_Ontology_Workspace UNIQUE (WorkspaceId);
```

### 2. Service Layer Refactoring

#### WorkspaceService (Enhanced)
- Becomes the primary service for workspace operations
- Creates workspace + ontology atomically
- Handles all permission operations
- Manages workspace metadata (name, description, visibility)

#### OntologyService (Refactored)
- Delegates permission checks to WorkspaceService
- Focuses on ontology-specific operations (concepts, relationships)
- Always operates within workspace context

#### New: WorkspacePermissionService
- Consolidates all permission logic
- Replaces `OntologyPermissionService` for permission checks
- Methods:
  - `CanViewAsync(workspaceId, userId)`
  - `CanEditAsync(workspaceId, userId)`
  - `CanManageAsync(workspaceId, userId)`
  - `GetWorkspaceUsersAsync(workspaceId)`
  - `ShareWithUserAsync(workspaceId, userId, permissionLevel)`
  - `ShareWithGroupAsync(workspaceId, groupId, permissionLevel)`

### 3. UI/UX Changes

#### Dashboard Changes
- Remove "Create Ontology" option
- Single "Create Workspace" button
- Workspace cards show:
  - Workspace name and description
  - Concept count (from ontology)
  - Note count
  - Last modified date
  - Visibility status

#### Unified Toolbar Component
Create `WorkspaceToolbar.razor` component with:
- Share button (with permission modal)
- Settings button (workspace + ontology settings)
- Import button (TTL/RDF for ontology, Markdown for notes)
- Export button (TTL/JSON for ontology, Markdown/ZIP for notes)

This toolbar appears identically in:
- Ontology Graph View
- Ontology List View
- Ontology Hierarchy View
- Workspace Notes View

#### Navigation Changes
```
Before:
Dashboard → Ontology View (with tabs: Graph, List, Hierarchy, TTL)
Dashboard → Workspace View (with notes)

After:
Dashboard → Workspace View (with tabs: Graph, List, Hierarchy, TTL, Notes)
```

Single unified view with all tabs accessible from one place.

### 4. Routing Changes

#### Current Routes
- `/ontology/{id}` - Ontology view
- `/workspace/{id}` - Workspace view

#### Proposed Routes
- `/workspace/{id}` - Main workspace view (default to user preference)
  - `/workspace/{id}/graph` - Graph view
  - `/workspace/{id}/list` - List view
  - `/workspace/{id}/hierarchy` - Hierarchy view
  - `/workspace/{id}/ttl` - TTL view
  - `/workspace/{id}/notes` - Notes view

#### Backward Compatibility
- Redirect `/ontology/{ontologyId}` → `/workspace/{workspaceId}/graph`
- Maintain old links for existing users

### 5. User Preferences Enhancement

Add to `UserPreferences` table:
- `DefaultWorkspaceView` - Which view to load first (Graph, List, Hierarchy, Notes)
- Currently defaults to Graph (ontology), can be set to Notes

## Migration Path

### Phase 1: Data Migration (Week 1)
1. Create migration to ensure all ontologies have workspaces
2. Create workspace for any orphaned ontologies
3. Add WorkspaceId NOT NULL constraint
4. Create WorkspacePermissionService
5. Migrate existing permissions to workspace-level

### Phase 2: Backend Refactoring (Week 1-2)
1. Implement WorkspacePermissionService
2. Refactor OntologyService to use WorkspacePermissionService
3. Update all repositories to use workspace-based permissions
4. Add workspace context to all operations
5. Comprehensive testing

### Phase 3: UI Refactoring (Week 2-3)
1. Create unified WorkspaceToolbar component
2. Consolidate views into single workspace route
3. Update dashboard to workspace-first approach
4. Implement tab-based navigation
5. Add routing redirects for backward compatibility

### Phase 4: Testing & Documentation (Week 3-4)
1. Update all tests for new architecture
2. Manual QA testing
3. Update user documentation
4. Update developer documentation (CLAUDE.md)
5. Create migration guide for users

## User-Facing Changes

### What Users Will Notice

#### Positive Changes
1. **Simplified Creation**: One "Create Workspace" button instead of choosing between workspace/ontology
2. **Persistent Toolbar**: Share, Settings, Import, Export always available regardless of view
3. **Unified Navigation**: Single place to access graph, notes, and all other views
4. **Consistent Permissions**: No confusion about who can access what

#### Behavior Changes
1. **Dashboard**: Now shows workspaces only (each contains an ontology)
2. **URL Structure**: Links will change from `/ontology/{id}` to `/workspace/{id}/graph`
3. **Default View**: Can be customized in preferences (Graph or Notes)

### Migration for Existing Users
- All existing ontologies automatically get associated workspaces
- Permissions preserved and migrated
- Old links redirect to new structure
- No data loss

## Technical Benefits

1. **Single Source of Truth**: Workspace is the container for all related data
2. **Simplified Permission Model**: One permission system instead of two
3. **Better Data Integrity**: Enforced 1:1 relationship prevents orphaned data
4. **Cleaner Code**: Less conditional logic about "am I in workspace or ontology context?"
5. **Easier Feature Development**: New features built at workspace level automatically available everywhere

## Risks & Mitigations

### Risk 1: Breaking Changes
**Mitigation**:
- Implement routing redirects
- Comprehensive testing
- Staged rollout with feature flag

### Risk 2: Data Migration Failures
**Mitigation**:
- Database backup before migration
- Migration scripts thoroughly tested
- Rollback plan documented

### Risk 3: User Confusion
**Mitigation**:
- In-app notifications about changes
- Updated documentation
- Onboarding tooltips for new structure

### Risk 4: Performance Impact
**Mitigation**:
- Optimize workspace queries with proper indexes
- Eager loading of related data
- Caching strategy for workspace metadata

## Success Metrics

1. **Code Quality**: Reduced conditional logic in permission checks
2. **User Experience**: Single creation flow, consistent toolbar across views
3. **Data Integrity**: No orphaned ontologies or workspaces
4. **Performance**: No degradation in load times
5. **Test Coverage**: Maintain 100% passing tests with new architecture

## Open Questions

1. **Workspace Naming**: Should workspace name default to ontology name or vice versa?
   - **Proposal**: Workspace name is primary, ontology inherits it

2. **Templates**: How do ontology templates work in workspace-first model?
   - **Proposal**: Workspace templates that include ontology template

3. **Forking**: When forking, do we fork workspace or ontology?
   - **Proposal**: Fork creates new workspace with cloned ontology and optionally cloned notes

4. **Import/Export**: Separate imports for ontology vs notes or unified?
   - **Proposal**: Keep separate for now (Import Ontology, Import Notes), consider unified later

5. **Collaboration Board**: Does it reference workspaces now instead of ontologies?
   - **Proposal**: Yes, collaboration posts reference workspaces

## Dependencies

- Database migration scripts
- Entity Framework Core migrations
- WorkspacePermissionService implementation
- WorkspaceToolbar component
- Route consolidation
- Backward compatibility redirects

## Timeline

- **Week 1**: Planning, database schema design, migration scripts
- **Week 2**: Backend implementation, permission service refactoring
- **Week 3**: UI implementation, toolbar consolidation, routing
- **Week 4**: Testing, documentation, deployment preparation

**Total Estimated Effort**: 3-4 weeks

## Next Steps

1. Review and approve this plan
2. Create detailed task breakdown
3. Set up feature flag for gradual rollout
4. Begin Phase 1: Data migration planning
