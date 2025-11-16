# Workspace-First Architecture - Task Breakdown

**Created:** 2025-11-16
**Last Updated:** 2025-11-16

## Task Checklist

### Phase 1: Foundation & Data Migration (Week 1)

#### Database Schema Analysis
- [ ] Audit current Ontology-Workspace relationships
  - [ ] Count ontologies without workspaces
  - [ ] Identify orphaned ontologies
  - [ ] Verify permission table structure
- [ ] Design new schema
  - [ ] WorkspaceUserPermissions table design
  - [ ] WorkspaceGroupPermissions table design
  - [ ] Update Ontology foreign key constraints

#### Migration Scripts
- [ ] Create migration: `AddWorkspaceToOrphanedOntologies`
  - [ ] For each ontology without WorkspaceId
  - [ ] Create workspace with matching name/description
  - [ ] Set workspace owner to ontology creator
  - [ ] Link ontology to new workspace
- [ ] Create migration: `MakeOntologyWorkspaceIdRequired`
  - [ ] Add NOT NULL constraint to Ontologies.WorkspaceId
  - [ ] Add unique constraint (one ontology per workspace)
- [ ] Create migration: `MigratePermissionsToWorkspace`
  - [ ] Create WorkspaceUserPermissions table
  - [ ] Create WorkspaceGroupPermissions table
  - [ ] Copy data from OntologyGroupPermissions to WorkspaceGroupPermissions
  - [ ] Copy data from UserShareAccesses to WorkspaceUserPermissions
  - [ ] Verify data integrity
- [ ] Create migration: `UpdateCollaborationBoardWorkspaceRef`
  - [ ] Add WorkspaceId to CollaborationPosts
  - [ ] Populate from OntologyId → Ontology.WorkspaceId
  - [ ] Make WorkspaceId required
  - [ ] Remove OntologyId column

#### Database Indexes
- [ ] Add index on Ontologies.WorkspaceId
- [ ] Add index on WorkspaceUserPermissions.WorkspaceId
- [ ] Add index on WorkspaceGroupPermissions.WorkspaceId
- [ ] Add composite index on WorkspaceUserPermissions (WorkspaceId, UserId)

#### Testing Migration Scripts
- [ ] Create test database with sample data
- [ ] Run migrations on test database
- [ ] Verify all ontologies have workspaces
- [ ] Verify all permissions migrated correctly
- [ ] Test rollback scripts

### Phase 2: Backend Implementation (Week 1-2)

#### Models & Entities
- [ ] Create `WorkspaceUserPermission.cs` model
- [ ] Create `WorkspaceGroupPermission.cs` model
- [ ] Update `Ontology.cs` - Make WorkspaceId required (NOT NULL)
- [ ] Update `Workspace.cs` - Add navigation properties
- [ ] Update `CollaborationPost.cs` - Change to WorkspaceId
- [ ] Update `OntologyDbContext.cs` - Configure new relationships

#### WorkspacePermissionService
- [ ] Create `Services/Interfaces/IWorkspacePermissionService.cs`
- [ ] Create `Services/WorkspacePermissionService.cs`
  - [ ] Implement `CanViewAsync(workspaceId, userId)`
  - [ ] Implement `CanEditAsync(workspaceId, userId)`
  - [ ] Implement `CanManageAsync(workspaceId, userId)`
  - [ ] Implement `GetPermissionLevelAsync(workspaceId, userId)`
  - [ ] Implement `GetWorkspaceUsersAsync(workspaceId)`
  - [ ] Implement `GetWorkspaceGroupsAsync(workspaceId)`
  - [ ] Implement `ShareWithUserAsync(workspaceId, userId, permissionLevel)`
  - [ ] Implement `ShareWithGroupAsync(workspaceId, groupId, permissionLevel)`
  - [ ] Implement `RemoveUserAccessAsync(workspaceId, userId)`
  - [ ] Implement `RemoveGroupAccessAsync(workspaceId, groupId)`
- [ ] Add caching for permission results (5-minute sliding expiration)
- [ ] Add comprehensive logging

#### Repository Updates
- [ ] Update `WorkspaceRepository.cs`
  - [ ] Add `CreateWorkspaceWithOntologyAsync(workspace, ontology, userId)`
  - [ ] Add eager loading of Ontology in queries
  - [ ] Add permission-filtered queries using WorkspacePermissionService
- [ ] Update `OntologyRepository.cs`
  - [ ] Update all queries to include workspace context
  - [ ] Add workspace-based filtering
  - [ ] Remove direct permission checks (delegate to WorkspacePermissionService)
- [ ] Create `WorkspacePermissionRepository.cs`
  - [ ] CRUD operations for WorkspaceUserPermissions
  - [ ] CRUD operations for WorkspaceGroupPermissions
  - [ ] Query methods for permission checks

#### Service Refactoring
- [ ] Update `WorkspaceService.cs`
  - [ ] Refactor `CreateWorkspaceAsync` to create workspace + ontology atomically
  - [ ] Add transaction support for atomic operations
  - [ ] Update `DeleteWorkspaceAsync` to cascade delete ontology
  - [ ] Integrate WorkspacePermissionService for all permission checks
- [ ] Update `OntologyService.cs`
  - [ ] Replace OntologyPermissionService with WorkspacePermissionService
  - [ ] Update all methods to use workspace-based permissions
  - [ ] Add workspace context to all operations
  - [ ] Update `ForkOntologyAsync` to create new workspace
- [ ] Update `ConceptService.cs`
  - [ ] Use WorkspacePermissionService instead of OntologyPermissionService
- [ ] Update `RelationshipService.cs`
  - [ ] Use WorkspacePermissionService instead of OntologyPermissionService
- [ ] Update `CollaborationBoardService.cs`
  - [ ] Update to reference WorkspaceId instead of OntologyId
  - [ ] Update auto-permission granting to workspace level

#### Hub Updates
- [ ] Update `OntologyHub.cs`
  - [ ] Replace OntologyPermissionService with WorkspacePermissionService
  - [ ] Update presence tracking to use workspace context
  - [ ] Update all permission checks

#### Endpoint Updates
- [ ] Update all API endpoints to use workspace-based permissions
- [ ] Add workspace context to endpoint parameters where needed
- [ ] Update documentation/Swagger comments

### Phase 3: UI/UX Implementation (Week 2-3)

#### New Components
- [ ] Create `Components/Shared/WorkspaceToolbar.razor`
  - [ ] Share button with dropdown
  - [ ] Settings button
  - [ ] Import button with dropdown (Ontology TTL, Notes Markdown)
  - [ ] Export button with dropdown (Ontology TTL/JSON, Notes Markdown/ZIP)
  - [ ] Responsive design for mobile
- [ ] Create `Components/Shared/WorkspaceTabNavigation.razor`
  - [ ] Graph tab
  - [ ] List tab
  - [ ] Hierarchy tab
  - [ ] TTL tab
  - [ ] Notes tab
  - [ ] Active tab indicator
  - [ ] Mobile-friendly tab switcher

#### Update Existing Components
- [ ] Update `Components/Pages/WorkspaceView.razor`
  - [ ] Add WorkspaceToolbar component
  - [ ] Add WorkspaceTabNavigation component
  - [ ] Integrate graph view (move from OntologyView)
  - [ ] Integrate list view (move from OntologyView)
  - [ ] Integrate hierarchy view (move from OntologyView)
  - [ ] Integrate TTL view (move from OntologyView)
  - [ ] Keep existing notes view
  - [ ] Add route parameters for tab selection
  - [ ] Add default view based on user preferences
- [ ] Update `Components/Pages/Dashboard.razor`
  - [ ] Remove "Create Ontology" button
  - [ ] Keep only "Create Workspace" button
  - [ ] Remove ontology cards section
  - [ ] Update workspace cards to show concept count
  - [ ] Add "Open Workspace" instead of "Open Ontology"
- [ ] Update `Components/Settings/ShareOntologyDialog.razor`
  - [ ] Rename to `ShareWorkspaceDialog.razor`
  - [ ] Update to use WorkspacePermissionService
  - [ ] Update UI text (Ontology → Workspace)
- [ ] Update `Components/Settings/OntologySettings.razor`
  - [ ] Rename to `WorkspaceSettings.razor`
  - [ ] Add workspace settings section
  - [ ] Keep ontology settings in a sub-section
  - [ ] Update permission management

#### Routing Updates
- [ ] Update `WorkspaceView.razor` routes
  - [ ] Add `@page "/workspace/{id:int}"` (default view)
  - [ ] Add `@page "/workspace/{id:int}/graph"`
  - [ ] Add `@page "/workspace/{id:int}/list"`
  - [ ] Add `@page "/workspace/{id:int}/hierarchy"`
  - [ ] Add `@page "/workspace/{id:int}/ttl"`
  - [ ] Add `@page "/workspace/{id:int}/notes"`
- [ ] Create redirect handler in `Program.cs`
  - [ ] Detect `/ontology/{ontologyId}` requests
  - [ ] Look up workspace ID from ontology ID
  - [ ] Redirect to `/workspace/{workspaceId}/graph`
- [ ] Update all internal navigation links
  - [ ] Dashboard → Workspace links
  - [ ] Breadcrumb navigation
  - [ ] Sidebar navigation

#### User Preferences
- [ ] Add `DefaultWorkspaceView` to UserPreferences model
- [ ] Update `Components/Settings/PreferencesSettings.razor`
  - [ ] Add dropdown for default view selection (Graph, List, Hierarchy, TTL, Notes)
- [ ] Update WorkspaceView to use default view preference

#### Import/Export UI
- [ ] Update Import dialog
  - [ ] Clarify "Import to Ontology" vs "Import Notes"
  - [ ] Update to work with workspace context
- [ ] Update Export dialog
  - [ ] Add "Export Ontology (TTL/JSON)" option
  - [ ] Add "Export Notes (Markdown/ZIP)" option
  - [ ] Support exporting both simultaneously

### Phase 4: Testing & Quality Assurance (Week 3-4)

#### Unit Tests
- [ ] Create `WorkspacePermissionServiceTests.cs`
  - [ ] Test CanViewAsync for all permission scenarios
  - [ ] Test CanEditAsync for all permission scenarios
  - [ ] Test CanManageAsync for all permission scenarios
  - [ ] Test permission caching
  - [ ] Test group-based permissions
  - [ ] Test user-based permissions
- [ ] Update `OntologyServiceTests.cs`
  - [ ] Update to use workspace context
  - [ ] Test workspace-ontology atomic creation
  - [ ] Test fork creates new workspace
- [ ] Update `WorkspaceServiceTests.cs`
  - [ ] Test CreateWorkspaceWithOntology atomic operation
  - [ ] Test cascade delete
  - [ ] Test permission inheritance
- [ ] Create `WorkspaceRepositoryTests.cs`
  - [ ] Test eager loading of ontology
  - [ ] Test permission-filtered queries
- [ ] Update all existing service tests to use WorkspacePermissionService

#### Integration Tests
- [ ] Test complete workspace creation flow (UI → Service → Database)
- [ ] Test permission sharing flow (share workspace with user/group)
- [ ] Test workspace deletion with cascade
- [ ] Test import/export with workspace context
- [ ] Test fork operation creates new workspace
- [ ] Test collaboration board with workspace references

#### Component Tests
- [ ] Test WorkspaceToolbar component rendering
- [ ] Test WorkspaceTabNavigation component
- [ ] Test Dashboard workspace cards
- [ ] Test WorkspaceView tab switching
- [ ] Test ShareWorkspaceDialog
- [ ] Test WorkspaceSettings

#### Migration Testing
- [ ] Test migration on copy of production database
- [ ] Verify all ontologies have workspaces after migration
- [ ] Verify all permissions migrated correctly
- [ ] Verify no data loss
- [ ] Test rollback procedure
- [ ] Performance test migration on large datasets

#### Manual QA Testing
- [ ] Test workspace creation flow
- [ ] Test opening workspace (default view)
- [ ] Test switching between all tabs (Graph, List, Hierarchy, TTL, Notes)
- [ ] Test Share functionality
- [ ] Test Settings functionality
- [ ] Test Import (both ontology and notes)
- [ ] Test Export (both ontology and notes)
- [ ] Test permission enforcement (view, edit, manage)
- [ ] Test collaboration board with workspace
- [ ] Test backward compatible redirects (`/ontology/{id}` → `/workspace/{id}/graph`)
- [ ] Test fork operation
- [ ] Test deletion with confirmation
- [ ] Test real-time collaboration (SignalR)
- [ ] Test on mobile devices

### Phase 5: Documentation & Deployment (Week 4)

#### Developer Documentation
- [ ] Update `CLAUDE.md` with new architecture
  - [ ] Update project structure section
  - [ ] Update database schema section
  - [ ] Update services section
  - [ ] Update routing section
- [ ] Update code comments
  - [ ] WorkspacePermissionService XML documentation
  - [ ] Service method documentation
  - [ ] Component documentation
- [ ] Create architecture diagram
  - [ ] Workspace-centric data model
  - [ ] Permission flow diagram
  - [ ] UI navigation flow

#### User Documentation
- [ ] Update `/docs/user-guides/GETTING_STARTED.md`
  - [ ] Update workspace creation instructions
  - [ ] Remove ontology-specific creation steps
- [ ] Update `/docs/user-guides/WORKSPACES_AND_NOTES.md`
  - [ ] Document unified workspace view
  - [ ] Document tab navigation
  - [ ] Document toolbar functionality
- [ ] Update `/docs/user-guides/PERMISSIONS.md`
  - [ ] Update to workspace-level permissions
  - [ ] Clarify permission inheritance
- [ ] Create migration guide
  - [ ] What's changing for users
  - [ ] How to adapt workflows
  - [ ] FAQ section

#### Release Notes
- [ ] Create `/docs/release-notes/WORKSPACE_FIRST_ARCHITECTURE.md`
  - [ ] Feature summary
  - [ ] Benefits for users
  - [ ] Migration notes
  - [ ] Breaking changes
  - [ ] Backward compatibility notes

#### Deployment Preparation
- [ ] Create deployment checklist
- [ ] Create rollback plan
- [ ] Database backup procedure
- [ ] Feature flag configuration
- [ ] Monitoring and alerting setup
- [ ] Create deployment runbook

#### Post-Deployment
- [ ] Monitor error logs for issues
- [ ] Monitor performance metrics
- [ ] Collect user feedback
- [ ] Address any critical bugs
- [ ] Plan for old code cleanup (remove OntologyPermissionService after stable period)

## Task Dependencies

```
Phase 1 (Database)
└─→ Phase 2 (Backend)
    └─→ Phase 3 (UI)
        └─→ Phase 4 (Testing)
            └─→ Phase 5 (Documentation & Deployment)

Critical Path:
1. Database migrations must complete before backend changes
2. WorkspacePermissionService must be implemented before service refactoring
3. Backend must be stable before UI changes
4. All testing must pass before deployment
```

## Priority Levels

### P0 (Critical - Must Complete)
- Database migrations
- WorkspacePermissionService implementation
- Service refactoring (Ontology, Workspace, Concept, Relationship)
- WorkspaceToolbar component
- Dashboard updates
- Routing updates
- Permission migration testing
- Backward compatible redirects

### P1 (High - Should Complete)
- WorkspaceTabNavigation component
- User preferences for default view
- Import/Export UI updates
- Comprehensive unit tests
- Integration tests
- Migration testing on production copy
- User documentation updates

### P2 (Medium - Nice to Have)
- Component tests
- Architecture diagrams
- Code comment improvements
- Feature flag implementation
- Advanced caching strategies

### P3 (Low - Future Enhancement)
- Performance optimizations beyond basic indexing
- Advanced analytics
- Workspace templates system
- Multiple ontologies per workspace support (future)

## Time Estimates

| Task Category | Estimated Time |
|--------------|---------------|
| Database Migrations | 8 hours |
| WorkspacePermissionService | 12 hours |
| Repository Updates | 8 hours |
| Service Refactoring | 16 hours |
| Hub Updates | 4 hours |
| WorkspaceToolbar Component | 8 hours |
| WorkspaceView Integration | 12 hours |
| Dashboard Updates | 6 hours |
| Routing & Redirects | 6 hours |
| Settings Components | 8 hours |
| Unit Tests | 16 hours |
| Integration Tests | 12 hours |
| Manual QA | 8 hours |
| Documentation | 12 hours |
| **Total** | **136 hours (~3.4 weeks)** |

## Risk Items

High-risk tasks requiring extra attention:
1. ⚠️ Database migrations on production data
2. ⚠️ Permission migration (must not lose or escalate permissions)
3. ⚠️ Backward compatible routing (broken links impact users)
4. ⚠️ Cascade delete (accidental data loss risk)
5. ⚠️ Real-time hub updates (SignalR must continue working)

## Completion Criteria

Phase complete when:
- [ ] All tasks marked complete
- [ ] All tests passing (100%)
- [ ] No critical bugs
- [ ] Documentation updated
- [ ] Code reviewed
- [ ] Deployment approved

## Notes

- Use feature flags for gradual rollout
- Keep OntologyPermissionService temporarily for rollback capability
- Monitor production closely for first week after deployment
- Plan for 1-2 week stabilization period before removing old code
