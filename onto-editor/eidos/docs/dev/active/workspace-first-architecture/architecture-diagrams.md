# Workspace-First Architecture - Diagrams

**Created:** 2025-11-16
**Last Updated:** 2025-11-16

## Current Architecture (Before)

### Data Model - Current State

```
┌─────────────────┐
│      User       │
└────────┬────────┘
         │
         │ owns (1:Many)
         ├────────────────────────┐
         │                        │
         ▼                        ▼
┌─────────────────┐      ┌─────────────────┐
│    Ontology     │      │    Workspace    │
│                 │      │                 │
│ - Id            │      │ - Id            │
│ - Name          │◄─┐   │ - Name          │
│ - Description   │  │   │ - Description   │
│ - Visibility    │  │   │ - Visibility    │
│ - WorkspaceId?  │  │   │ - OwnerId       │
│   (NULLABLE)    │  │   └─────────────────┘
└────────┬────────┘  │            │
         │           │            │
         │           │            ├─► Notes (1:Many)
         │           └────────────┤
         │          (Many:1)      ├─► Tags (1:Many)
         │        (OPTIONAL)      │
         ├─► Concepts (1:Many)    └─► Attachments (via Notes)
         ├─► Relationships
         ├─► Individuals
         └─► OntologyGroupPermissions (1:Many)

┌─────────────────────────────────────────┐
│         Permission Tables               │
├─────────────────────────────────────────┤
│ - UserShareAccesses                     │
│   (User ↔ Ontology direct shares)       │
│                                         │
│ - OntologyGroupPermissions              │
│   (Group ↔ Ontology permissions)        │
└─────────────────────────────────────────┘
```

### Permission Flow - Current State

```
User requests access to Ontology
         │
         ▼
┌─────────────────────────────────┐
│  OntologyPermissionService      │
│                                 │
│  1. Check if user is owner      │
│  2. Check visibility            │
│     - Private                   │
│     - Public                    │
│  3. Check UserShareAccesses     │
│  4. Check OntologyGroupPerms    │
│  5. Aggregate permissions       │
└────────────┬────────────────────┘
             │
             ▼
      Return: View/Edit/Manage

Workspace has separate permission checks (not unified)
```

### UI Navigation - Current State

```
Dashboard
├─► Create Ontology ────► Ontology View
│                         ├─► Graph Tab
│                         ├─► List Tab
│                         ├─► Hierarchy Tab
│                         ├─► TTL Tab
│                         └─► [Share, Settings, Import, Export buttons]
│
└─► Create Workspace ───► Workspace View
                          ├─► Notes List
                          └─► Note Editor
                              [NO Share/Settings/Import/Export]
```

## Proposed Architecture (After)

### Data Model - Proposed State

```
┌─────────────────┐
│      User       │
└────────┬────────┘
         │
         │ owns (1:Many)
         │
         ▼
┌─────────────────────────────────────────┐
│           Workspace (PRIMARY)           │
│                                         │
│ - Id                                    │
│ - Name                                  │
│ - Description                           │
│ - Visibility (private/group/public)     │
│ - OwnerId                               │
└────────┬────────────────────────────────┘
         │
         │ has (1:1 REQUIRED)
         │
         ├───────────────────────────────┐
         │                               │
         ▼                               ▼
┌─────────────────┐            ┌─────────────────┐
│    Ontology     │            │   Other Data    │
│   (CHILD)       │            │                 │
│                 │            │ - Notes         │
│ - Id            │            │ - Tags          │
│ - Name          │            │ - Attachments   │
│ - WorkspaceId   │            │                 │
│   (REQUIRED)    │            └─────────────────┘
│                 │
├─► Concepts      │
├─► Relationships │
└─► Individuals   │

┌───────────────────────────────────────────┐
│      Permission Tables (WORKSPACE)        │
├───────────────────────────────────────────┤
│ - WorkspaceUserPermissions                │
│   (User ↔ Workspace direct shares)        │
│                                           │
│ - WorkspaceGroupPermissions               │
│   (Group ↔ Workspace permissions)         │
└───────────────────────────────────────────┘

All permissions managed at Workspace level
Ontology, Notes, Tags inherit workspace permissions
```

### Permission Flow - Proposed State

```
User requests access to Workspace (or any child resource)
         │
         ▼
┌─────────────────────────────────┐
│  WorkspacePermissionService     │
│  (SINGLE SOURCE OF TRUTH)       │
│                                 │
│  1. Check if user is owner      │
│  2. Check visibility            │
│     - private                   │
│     - group                     │
│     - public                    │
│  3. Check WorkspaceUserPerms    │
│  4. Check WorkspaceGroupPerms   │
│  5. Cache result (5 min)        │
│  6. Return permission level     │
└────────────┬────────────────────┘
             │
             ▼
      Return: View/Edit/Manage

All services use WorkspacePermissionService:
- OntologyService
- ConceptService
- RelationshipService
- NoteService
- TagService
```

### UI Navigation - Proposed State

```
Dashboard
└─► Create Workspace ───► Workspace View (Unified)
                          │
                          ├─► Graph Tab (Ontology)
                          ├─► List Tab (Ontology)
                          ├─► Hierarchy Tab (Ontology)
                          ├─► TTL Tab (Ontology)
                          └─► Notes Tab

                          [Persistent Toolbar on ALL tabs]
                          ┌────────────────────────────────┐
                          │ Share │ Settings │ Import │ Export │
                          └────────────────────────────────┘

Routes:
/workspace/{id}           → Default view (user preference)
/workspace/{id}/graph     → Graph view
/workspace/{id}/list      → List view
/workspace/{id}/hierarchy → Hierarchy view
/workspace/{id}/ttl       → TTL view
/workspace/{id}/notes     → Notes view

Backward Compatibility:
/ontology/{id} → Redirect to /workspace/{workspaceId}/graph
```

## Service Architecture

### Current Service Dependencies

```
┌──────────────────────┐
│  OntologyService     │
│                      │
│  Depends on:         │
│  - OntologyPermServ  │───┐
│  - OntologyRepo      │   │
└──────────────────────┘   │
                           │
┌──────────────────────┐   │
│  ConceptService      │   │
│                      │   │
│  Depends on:         │   │
│  - OntologyPermServ  │───┤
│  - ConceptRepo       │   │
└──────────────────────┘   │
                           │
┌──────────────────────┐   │
│  WorkspaceService    │   │
│                      │   │
│  Depends on:         │   │
│  - WorkspaceRepo     │   │ Different permission systems!
│  - (Own perms)       │   │
└──────────────────────┘   │
                           │
                           ▼
              ┌──────────────────────────┐
              │ OntologyPermissionService │
              │ (Ontology-specific)       │
              └──────────────────────────┘
```

### Proposed Service Dependencies

```
┌──────────────────────┐
│  WorkspaceService    │
│                      │
│  Depends on:         │
│  - WorkspacePermServ │───┐
│  - WorkspaceRepo     │   │
│  - OntologyRepo      │   │
└──────────────────────┘   │
                           │
┌──────────────────────┐   │
│  OntologyService     │   │
│                      │   │
│  Depends on:         │   │
│  - WorkspacePermServ │───┤
│  - OntologyRepo      │   │
└──────────────────────┘   │
                           │
┌──────────────────────┐   │
│  ConceptService      │   │
│                      │   │
│  Depends on:         │   │
│  - WorkspacePermServ │───┤
│  - ConceptRepo       │   │
└──────────────────────┘   │
                           │
┌──────────────────────┐   │
│  NoteService         │   │
│                      │   │
│  Depends on:         │   │
│  - WorkspacePermServ │───┤
│  - NoteRepo          │   │
└──────────────────────┘   │
                           │
                           ▼
              ┌──────────────────────────┐
              │ WorkspacePermissionService│
              │ (SINGLE SOURCE OF TRUTH)  │
              │                           │
              │ - CanViewAsync            │
              │ - CanEditAsync            │
              │ - CanManageAsync          │
              │ - ShareWithUserAsync      │
              │ - ShareWithGroupAsync     │
              │ - Caching layer           │
              └──────────────────────────┘
```

## Database Migration Flow

### Step 1: Create Workspaces for Orphaned Ontologies

```
Before:
Ontologies Table
├─ Ontology 1 (WorkspaceId = 5)  ✓ Has workspace
├─ Ontology 2 (WorkspaceId = NULL) ✗ Orphaned
└─ Ontology 3 (WorkspaceId = 7)  ✓ Has workspace

Workspaces Table
├─ Workspace 5
└─ Workspace 7

Migration Action:
1. Find Ontology 2 (WorkspaceId = NULL)
2. Create Workspace 8:
   - Name = Ontology 2's name
   - Description = Ontology 2's description
   - OwnerId = Ontology 2's CreatedBy
3. Set Ontology 2.WorkspaceId = 8

After:
Ontologies Table
├─ Ontology 1 (WorkspaceId = 5)  ✓
├─ Ontology 2 (WorkspaceId = 8)  ✓ Now has workspace
└─ Ontology 3 (WorkspaceId = 7)  ✓

Workspaces Table
├─ Workspace 5
├─ Workspace 7
└─ Workspace 8 (NEW - auto-created)
```

### Step 2: Make WorkspaceId Required

```sql
-- After Step 1, all ontologies have workspaces
ALTER TABLE Ontologies
ALTER COLUMN WorkspaceId int NOT NULL;

ALTER TABLE Ontologies
ADD CONSTRAINT UC_Ontology_Workspace UNIQUE (WorkspaceId);

-- Now enforced: Every ontology MUST have a workspace
--                Every workspace can have only ONE ontology
```

### Step 3: Migrate Permissions

```
Before:
OntologyGroupPermissions
├─ OntologyId=1, GroupId=100, Permission=Edit
├─ OntologyId=2, GroupId=101, Permission=View
└─ OntologyId=3, GroupId=100, Permission=Manage

UserShareAccesses
├─ OntologyId=1, UserId=200, Permission=Edit
└─ OntologyId=2, UserId=201, Permission=View

Migration Action:
1. For each OntologyGroupPermission:
   - Look up Ontology.WorkspaceId
   - Create WorkspaceGroupPermission with WorkspaceId

2. For each UserShareAccess:
   - Look up Ontology.WorkspaceId
   - Create WorkspaceUserPermission with WorkspaceId

After:
WorkspaceGroupPermissions
├─ WorkspaceId=5, GroupId=100, Permission=Edit    (from Ont 1)
├─ WorkspaceId=8, GroupId=101, Permission=View    (from Ont 2)
└─ WorkspaceId=7, GroupId=100, Permission=Manage  (from Ont 3)

WorkspaceUserPermissions
├─ WorkspaceId=5, UserId=200, Permission=Edit  (from Ont 1)
└─ WorkspaceId=8, UserId=201, Permission=View  (from Ont 2)

Old tables can be dropped after verification
```

## User Experience Flow

### Current User Flow (Before)

```
User logs in
    │
    ▼
Dashboard
    │
    ├─► Click "Create Ontology"
    │   ├─► Enter name, description
    │   ├─► Choose visibility
    │   ├─► Choose template (optional)
    │   └─► Create
    │       └─► Opens Ontology View
    │           ├─► Can add concepts
    │           ├─► Can share
    │           └─► Can export
    │
    └─► Click "Create Workspace"
        ├─► Enter name, description
        └─► Create
            └─► Opens Workspace View
                ├─► Can add notes
                └─► Cannot share/export (UI missing)

Problem: Two separate creation paths, inconsistent features
```

### Proposed User Flow (After)

```
User logs in
    │
    ▼
Dashboard
    │
    └─► Click "Create Workspace"
        ├─► Enter name, description
        ├─► Choose visibility
        ├─► Choose template (optional - includes ontology structure)
        └─► Create (atomically creates workspace + ontology)
            │
            ▼
        Workspace View (Unified)
            │
            ├─► Default tab based on preference (Graph or Notes)
            │
            ├─► Can switch tabs:
            │   ├─► Graph (ontology visualization)
            │   ├─► List (concept list)
            │   ├─► Hierarchy (concept tree)
            │   ├─► TTL (raw ontology)
            │   └─► Notes (workspace notes)
            │
            └─► Toolbar (available on ALL tabs):
                ├─► Share (with users/groups)
                ├─► Settings (workspace + ontology)
                ├─► Import (TTL for ontology, MD for notes)
                └─► Export (TTL/JSON for ontology, MD/ZIP for notes)

Benefit: Single creation path, consistent UI, all features always available
```

## Backward Compatibility

### URL Redirect Strategy

```
User has old bookmark: /ontology/123
         │
         ▼
┌─────────────────────────────────┐
│  ASP.NET Core Middleware        │
│                                 │
│  1. Detect /ontology/{id} route │
│  2. Query: Ontology 123         │
│     → WorkspaceId = 456         │
│  3. Redirect to:                │
│     /workspace/456/graph        │
└─────────────────────────────────┘
         │
         ▼
User sees Workspace View (Graph tab)
- Same content as before
- New UI with unified toolbar
- Bookmark should be updated

Redirect logic:
- 301 Permanent Redirect (for SEO/bookmarks)
- Preserves query parameters
- Logs redirect for analytics
```

## Component Architecture

### Current Component Structure (Before)

```
OntologyView.razor (500+ lines)
├─► Graph rendering
├─► List rendering
├─► Hierarchy rendering
├─► TTL rendering
├─► Share button → ShareOntologyDialog
├─► Settings button → OntologySettings
├─► Import button → Import logic
└─► Export button → Export logic

WorkspaceView.razor (400+ lines)
├─► Notes list
├─► Note editor
└─► (No share/settings/import/export)

Problem: Duplicate logic, inconsistent features
```

### Proposed Component Structure (After)

```
WorkspaceView.razor (Main container)
├─► WorkspaceToolbar.razor (Shared component)
│   ├─► Share button → ShareWorkspaceDialog
│   ├─► Settings button → WorkspaceSettings
│   ├─► Import button → Import menu (Ontology/Notes)
│   └─► Export button → Export menu (Ontology/Notes)
│
├─► WorkspaceTabNavigation.razor
│   ├─► Graph tab
│   ├─► List tab
│   ├─► Hierarchy tab
│   ├─► TTL tab
│   └─► Notes tab
│
└─► Tab Content (based on active tab)
    ├─► OntologyGraphView.razor (extracted from old OntologyView)
    ├─► OntologyListView.razor (extracted)
    ├─► OntologyHierarchyView.razor (extracted)
    ├─► OntologyTtlView.razor (extracted)
    └─► NotesView.razor (existing workspace notes)

Benefits:
- Single toolbar component used everywhere
- Reusable tab views
- Consistent UI/UX
- Easier to maintain
```

## Timeline Visualization

```
Week 1: Foundation
┌─────────────────────────────────────┐
│ Day 1-2: Database schema design     │
│ Day 3-4: Migration script creation  │
│ Day 5:   Migration testing          │
└─────────────────────────────────────┘
         │
         ▼
Week 2: Backend Implementation
┌─────────────────────────────────────┐
│ Day 1-2: WorkspacePermissionService │
│ Day 3-4: Service refactoring        │
│ Day 5:   Repository updates         │
└─────────────────────────────────────┘
         │
         ▼
Week 3: UI Implementation
┌─────────────────────────────────────┐
│ Day 1-2: WorkspaceToolbar component │
│ Day 3-4: WorkspaceView integration  │
│ Day 5:   Routing & redirects        │
└─────────────────────────────────────┘
         │
         ▼
Week 4: Testing & Documentation
┌─────────────────────────────────────┐
│ Day 1-2: Unit & integration tests   │
│ Day 3:   Manual QA                  │
│ Day 4:   Documentation              │
│ Day 5:   Deployment preparation     │
└─────────────────────────────────────┘
         │
         ▼
    Deployment
    └─► Monitor for 1-2 weeks
        └─► Remove old code
```

## Success Metrics

```
Before Migration:
┌─────────────────────────────────────┐
│ Codebase Metrics                    │
├─────────────────────────────────────┤
│ - 2 permission services             │
│ - 2 permission table sets           │
│ - Inconsistent UI (2 entry points)  │
│ - Complex permission logic          │
└─────────────────────────────────────┘

After Migration:
┌─────────────────────────────────────┐
│ Codebase Metrics                    │
├─────────────────────────────────────┤
│ - 1 permission service              │
│ - 1 permission table set            │
│ - Unified UI (1 entry point)        │
│ - Simplified permission logic       │
│ - Better user experience            │
│ - Easier maintenance                │
└─────────────────────────────────────┘
```
