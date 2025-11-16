# Workspace-First Architecture - Context

**Created:** 2025-11-16
**Last Updated:** 2025-11-16

## Current State Analysis

### Database Schema

#### Ontologies Table
```csharp
public class Ontology
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Visibility and permissions
    public OntologyVisibility Visibility { get; set; }
    public PermissionLevel? PublicPermissionLevel { get; set; }

    // Optional workspace relationship
    public int? WorkspaceId { get; set; }  // NULLABLE - needs to be NOT NULL
    public Workspace? Workspace { get; set; }

    // Relationships
    public ICollection<Concept> Concepts { get; set; }
    public ICollection<OntologyTag> Tags { get; set; }
    public ICollection<UserShareAccess> SharedAccesses { get; set; }
    public ICollection<OntologyGroupPermission> GroupPermissions { get; set; }
}
```

#### Workspaces Table
```csharp
public class Workspace
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string OwnerId { get; set; }
    public ApplicationUser Owner { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Visibility
    public string Visibility { get; set; } = "private"; // private, group, public

    // Relationships
    public ICollection<Note> Notes { get; set; }
    public ICollection<Tag> Tags { get; set; }
    public Ontology? Ontology { get; set; }  // 1:1 relationship (optional currently)
}
```

### Key Files to Modify

#### Services
- **`Services/OntologyService.cs`** - Main ontology business logic (150+ lines)
  - Currently manages ontology permissions independently
  - Needs refactoring to delegate to WorkspacePermissionService

- **`Services/OntologyPermissionService.cs`** - Ontology-specific permissions (200+ lines)
  - To be replaced/refactored into WorkspacePermissionService
  - Complex permission checks for view, edit, manage

- **`Services/WorkspaceService.cs`** - Workspace business logic (100+ lines)
  - Needs enhancement to handle workspace creation with ontology
  - Currently separate from ontology operations

- **NEW: `Services/WorkspacePermissionService.cs`**
  - Consolidate all permission logic here
  - Used by both workspace and ontology operations

#### Repositories
- **`Data/Repositories/OntologyRepository.cs`**
  - Update queries to include workspace context
  - Add workspace-based filtering

- **`Data/Repositories/WorkspaceRepository.cs`**
  - Enhance to handle workspace + ontology operations
  - Add eager loading of ontology when needed

#### Components (Pages)
- **`Components/Pages/OntologyView.razor`** - Main ontology view (500+ lines)
  - Contains Graph, List, Hierarchy, TTL tabs
  - Has Share, Settings, Import, Export buttons
  - Needs consolidation with WorkspaceView

- **`Components/Pages/WorkspaceView.razor`** - Main workspace/notes view (400+ lines)
  - Currently only shows notes
  - Missing Share, Settings, Import, Export buttons
  - Needs toolbar integration

- **`Components/Pages/Dashboard.razor`** - Main dashboard (300+ lines)
  - Shows both ontologies and workspaces
  - Needs simplification to workspace-only

- **NEW: `Components/Shared/WorkspaceToolbar.razor`**
  - Unified toolbar for all workspace views
  - Share, Settings, Import, Export functionality

#### Settings Components
- **`Components/Settings/ShareOntologyDialog.razor`**
  - Currently ontology-specific
  - Needs to become workspace-aware

- **`Components/Settings/OntologySettings.razor`**
  - Ontology settings dialog
  - Should become WorkspaceSettings with ontology section

### Current Permission Flow

#### Ontology Permissions
```csharp
// OntologyPermissionService.CanViewAsync()
1. Check if user is owner → Full access
2. Check OntologyVisibility:
   - Private: Only owner + shared users/groups
   - Public: Everyone can view (with PublicPermissionLevel)
3. Check UserShareAccess table for direct shares
4. Check OntologyGroupPermissions for group access
5. Return aggregated permission level
```

#### Workspace Permissions
```csharp
// Currently in WorkspaceService (not dedicated service)
1. Check if user is owner → Full access
2. Check workspace visibility:
   - private: Only owner
   - group: Check group membership
   - public: Everyone
3. Basic permission checks (view, edit)
```

### Permission Tables

#### Current State
- `UserShareAccesses` - Direct user shares for ontologies
- `OntologyGroupPermissions` - Group permissions for ontologies
- `UserGroups` - User groups
- `UserGroupMembers` - Group membership

#### Proposed State
- `WorkspaceUserPermissions` - Direct user shares for workspaces
- `WorkspaceGroupPermissions` - Group permissions for workspaces
- `UserGroups` - User groups (unchanged)
- `UserGroupMembers` - Group membership (unchanged)

### Routing

#### Current Routes (Program.cs)
```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Page routes defined in components:
// @page "/ontology/{id:int}"           - OntologyView.razor
// @page "/workspace/{id:int}"          - WorkspaceView.razor
// @page "/dashboard"                   - Dashboard.razor
```

#### Proposed Routes
```csharp
// Unified workspace routes:
// @page "/workspace/{id:int}"                    - Default view (based on preferences)
// @page "/workspace/{id:int}/graph"              - Graph view
// @page "/workspace/{id:int}/list"               - List view
// @page "/workspace/{id:int}/hierarchy"          - Hierarchy view
// @page "/workspace/{id:int}/ttl"                - TTL view
// @page "/workspace/{id:int}/notes"              - Notes view

// Backward compatibility redirects:
// /ontology/{ontologyId} → /workspace/{workspaceId}/graph
```

### Data Relationships

#### Current State
```
User
├── Ontologies (Owner) - 1:Many
├── Workspaces (Owner) - 1:Many
├── UserShareAccesses - Many:Many (User ↔ Ontology)
└── UserGroupMembers - Many:Many (User ↔ Group)

Ontology
├── Workspace - Many:1 (OPTIONAL)
├── Concepts - 1:Many
├── Relationships - 1:Many
├── Tags - Many:Many
├── UserShareAccesses - 1:Many
└── GroupPermissions - 1:Many

Workspace
├── Ontology - 1:1 (OPTIONAL)
├── Notes - 1:Many
├── Tags - 1:Many
└── Attachments - 1:Many (via Notes)
```

#### Proposed State
```
User
├── Workspaces (Owner) - 1:Many
├── WorkspaceUserPermissions - Many:Many (User ↔ Workspace)
└── UserGroupMembers - Many:Many (User ↔ Group)

Workspace (PRIMARY)
├── Ontology - 1:1 (REQUIRED)
├── Notes - 1:Many
├── Tags - 1:Many
├── Attachments - 1:Many (via Notes)
├── WorkspaceUserPermissions - 1:Many
└── WorkspaceGroupPermissions - 1:Many

Ontology (CHILD OF WORKSPACE)
├── Workspace - 1:1 (REQUIRED)
├── Concepts - 1:Many
├── Relationships - 1:Many
└── Individuals - 1:Many
```

### Critical Files for Initial Investigation

Before starting implementation, analyze these files thoroughly:

1. **Database Context**
   - `Data/OntologyDbContext.cs` - Entity configurations

2. **Current Services**
   - `Services/OntologyService.cs` - Ontology CRUD
   - `Services/OntologyPermissionService.cs` - Permission logic
   - `Services/WorkspaceService.cs` - Workspace CRUD
   - `Services/ConceptService.cs` - Uses ontology permissions
   - `Services/RelationshipService.cs` - Uses ontology permissions

3. **Current Components**
   - `Components/Pages/OntologyView.razor` - Main ontology UI
   - `Components/Pages/WorkspaceView.razor` - Main workspace UI
   - `Components/Pages/Dashboard.razor` - Entry point
   - `Components/Settings/ShareOntologyDialog.razor` - Sharing UI
   - `Components/Settings/OntologySettings.razor` - Settings UI

4. **Hubs (Real-time)**
   - `Hubs/OntologyHub.cs` - Real-time collaboration hub
   - Uses OntologyPermissionService for access checks

### Collaboration Board Impact

Current state:
```csharp
public class CollaborationPost
{
    public int Id { get; set; }
    public int OntologyId { get; set; }  // References Ontology
    public Ontology Ontology { get; set; }
    // ...
}
```

Proposed state:
```csharp
public class CollaborationPost
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }  // References Workspace instead
    public Workspace Workspace { get; set; }
    // ...
}
```

### Feature Toggles

Should implement feature toggle for gradual rollout:
```csharp
public class FeatureFlags
{
    public bool UseWorkspaceFirstArchitecture { get; set; } = false;
}
```

Enable in stages:
1. Backend changes with feature flag
2. UI changes with feature flag
3. Test with select users
4. Enable for all users
5. Remove old code paths

### Testing Considerations

#### Current Test Coverage
- `OntologyPermissionServiceTests.cs` - 30+ tests
- `OntologyServiceTests.cs` - 20+ tests
- `WorkspaceServiceTests.cs` - 15+ tests
- Component tests for OntologyView, WorkspaceView

#### New Tests Needed
- `WorkspacePermissionServiceTests.cs` - Port and enhance existing tests
- `WorkspaceService` atomic creation tests (workspace + ontology)
- Migration tests (ensure all ontologies get workspaces)
- Backward compatibility routing tests
- UI integration tests for unified toolbar

### Performance Considerations

#### Query Optimization
Current N+1 query risks:
```csharp
// Loading workspaces without ontology
var workspaces = await _context.Workspaces.ToListAsync();
// Then for each workspace, loading ontology separately
```

Solution - Eager loading:
```csharp
var workspaces = await _context.Workspaces
    .Include(w => w.Ontology)
        .ThenInclude(o => o.Concepts)
    .Include(w => w.Notes)
    .Include(w => w.Tags)
    .ToListAsync();
```

#### Caching Strategy
- Cache workspace metadata (name, description, visibility)
- Cache permission results (user can view workspace X)
- Invalidate on permission changes

### Migration Complexity Areas

1. **Orphaned Ontologies**
   - Ontologies without WorkspaceId
   - Need to create workspaces for them

2. **Permission Migration**
   - Map OntologyGroupPermissions → WorkspaceGroupPermissions
   - Map UserShareAccesses → WorkspaceUserPermissions

3. **Collaboration Board**
   - Update CollaborationPost.OntologyId → WorkspaceId
   - Maintain foreign key integrity

4. **Activity Tracking**
   - OntologyActivity table references OntologyId
   - May need WorkspaceActivity table or update to include WorkspaceId

### UI/UX Impact Areas

1. **Dashboard**
   - Remove ontology cards
   - Show workspace cards with concept count

2. **Navigation**
   - Consolidate tabs into single view
   - Update breadcrumbs

3. **Settings**
   - Merge ontology and workspace settings
   - Update permission dialogs

4. **Import/Export**
   - Clarify context (importing to workspace's ontology)
   - Support both ontology and notes import

### Dependencies to Review

- **SignalR Hub**: OntologyHub uses OntologyPermissionService
- **Activity Tracking**: OntologyActivity references ontologies
- **Version Control**: Version history tied to ontologies
- **Templates**: Ontology templates need workspace context
- **Forking**: Fork operation creates new ontology, needs workspace

### Known Constraints

1. **Database Size**: Production database size may make migrations slow
2. **Active Users**: Migration must not interrupt active sessions
3. **URL Changes**: Existing bookmarks/links will break without redirects
4. **Third-party Integrations**: Any external links to ontologies need updates

### Success Criteria

- [ ] All existing ontologies have workspaces
- [ ] All permissions migrated successfully
- [ ] No permission escalation or data access issues
- [ ] All tests passing
- [ ] No performance degradation
- [ ] Backward compatible URL redirects working
- [ ] User documentation updated
- [ ] Zero data loss

## Decision Log

### Decision 1: Workspace as Primary Entity
**Date:** 2025-11-16
**Decision:** Make Workspace the primary container, Ontology becomes a required child
**Rationale:**
- Simplifies mental model for users
- Unifies permission system
- Enables future features (workspace-level collaboration, analytics)

### Decision 2: 1:1 Relationship
**Date:** 2025-11-16
**Decision:** Enforce 1:1 relationship between Workspace and Ontology
**Rationale:**
- Simpler to implement and understand
- Matches current usage patterns
- Can be relaxed later if needed (1:Many) without breaking changes

### Decision 3: Permission Migration
**Date:** 2025-11-16
**Decision:** Migrate all ontology permissions to workspace level
**Rationale:**
- Single source of truth for access control
- Eliminates permission sync issues
- Simplifies permission checking logic

### Decision 4: Backward Compatible Routes
**Date:** 2025-11-16
**Decision:** Keep `/ontology/{id}` routes with redirects
**Rationale:**
- Existing bookmarks and external links continue to work
- Gradual migration path for users
- Can deprecate old routes later

## Questions for Review

1. Should we support multiple ontologies per workspace in the future?
   - Current decision: No, enforce 1:1 for simplicity

2. What happens to ontology templates?
   - Proposal: Become workspace templates (includes ontology structure)

3. Should forking create a new workspace or just clone the ontology?
   - Proposal: Fork creates new workspace with all data (ontology + notes)

4. How to handle workspace deletion?
   - Proposal: Cascade delete ontology, notes, tags, attachments (with confirmation)

5. Should we keep separate import for ontology vs notes?
   - Proposal: Yes, keep separate for clarity (Import Ontology, Import Notes)

## Resources

- EF Core Migrations Documentation: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- ASP.NET Core Routing: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing
- Feature Flags Pattern: https://martinfowler.com/articles/feature-toggles.html
