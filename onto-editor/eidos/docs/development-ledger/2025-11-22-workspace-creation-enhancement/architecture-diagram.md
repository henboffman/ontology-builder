# Architecture Diagram - Workspace Creation Enhancement

**Date**: November 22, 2025
**Feature**: Enhanced Workspace Creation Dialog

## System Architecture

### Component Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│                     Workspaces.razor                         │
│  (or Home.razor - parent page)                               │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Shows/Hides
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              CreateWorkspaceDialog.razor                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  • Name input                                         │  │
│  │  • Description textarea                               │  │
│  │  • Privacy selector (Private/Group/Public)            │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                              │
│  @if (privacy == "group")                                    │
│  ┌───────────────────────────────────────────────────────┐  │
│  │         UserGroupSelector.razor                       │  │
│  │   ┌─────────────────────────────────────────────────┐ │  │
│  │   │  • Existing group dropdown                      │ │  │
│  │   │  • "Create New Group" option                    │ │  │
│  │   └─────────────────────────────────────────────────┘ │  │
│  │                                                         │  │
│  │   @if (selectedGroupId == -1)                          │  │
│  │   ┌─────────────────────────────────────────────────┐ │  │
│  │   │  • Group name input                             │ │  │
│  │   │  • Group description                            │ │  │
│  │   │  • UserPicker for members                       │ │  │
│  │   └─────────────────────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                              │
│  @if (enableDirectAccess)                                    │
│  ┌───────────────────────────────────────────────────────┐  │
│  │              UserPicker.razor                         │  │
│  │   ┌─────────────────────────────────────────────────┐ │  │
│  │   │  • Search input (autocomplete)                  │ │  │
│  │   │  • Search results dropdown                      │ │  │
│  │   │  • Selected users (badges)                      │ │  │
│  │   └─────────────────────────────────────────────────┘ │  │
│  │                                                         │  │
│  │   ┌─────────────────────────────────────────────────┐ │  │
│  │   │  • Permission level selector                    │ │  │
│  │   └─────────────────────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                              │
│  [Cancel Button]  [Create Button]                           │
└─────────────────────────────────────────────────────────────┘
```

## Data Flow

### Creation Flow (Happy Path)

```
┌─────────────┐
│    User     │
│ clicks      │
│ "Create"    │
└──────┬──────┘
       │
       ▼
┌──────────────────────────────────────────────────┐
│  CreateWorkspaceDialog.CreateWorkspace()         │
│  • Validates input                               │
│  • Collects all form data                        │
└────────────────┬─────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────┐
│  WorkspaceService.CreateWorkspaceWithAccessAsync │
│  • Starts database transaction                   │
└────────────────┬─────────────────────────────────┘
                 │
                 ├─────────────────────────────────┐
                 │                                 │
                 ▼                                 ▼
┌────────────────────────────┐  ┌────────────────────────────┐
│ Create Workspace           │  │ Create Ontology            │
│ • Name: "Research WS"      │  │ • Name: "Research WS       │
│ • Visibility: "group"      │  │   Ontology"                │
│ • AllowPublicEdit: false   │  │ • Visibility: "group"      │
│                            │  │ • Version: "0.1"           │
└─────────────┬──────────────┘  └────────────┬───────────────┘
              │                              │
              └──────────────┬───────────────┘
                             │
                             ▼
              ┌──────────────────────────────┐
              │ IF new group needed:         │
              │ UserGroupService.            │
              │   CreateGroupAsync()         │
              │ • Name: "Research Team"      │
              │ • CreatedBy: currentUserId   │
              └──────────────┬───────────────┘
                             │
                             ▼
              ┌──────────────────────────────┐
              │ Add members to group:        │
              │ UserGroupService.            │
              │   AddUserToGroupAsync()      │
              │ (foreach member)             │
              └──────────────┬───────────────┘
                             │
                             ▼
              ┌──────────────────────────────┐
              │ Grant group permission:      │
              │ PermissionService.           │
              │   GrantGroupPermissionAsync()│
              │ • WorkspaceId                │
              │ • GroupId                    │
              │ • Level: ViewAddEdit         │
              └──────────────┬───────────────┘
                             │
                             ▼
              ┌──────────────────────────────┐
              │ IF direct user access:       │
              │ PermissionService.           │
              │   GrantUserAccessAsync()     │
              │ (foreach user)               │
              │ • WorkspaceId                │
              │ • UserId                     │
              │ • Level: ViewAddEdit         │
              └──────────────┬───────────────┘
                             │
                             ▼
              ┌──────────────────────────────┐
              │ Commit Transaction           │
              └──────────────┬───────────────┘
                             │
                             ▼
              ┌──────────────────────────────┐
              │ Return created Workspace     │
              └──────────────┬───────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────┐
│  CreateWorkspaceDialog                           │
│  • Shows success toast                           │
│  • Closes dialog                                 │
│  • Navigates to /workspace/{id}                  │
└──────────────────────────────────────────────────┘
```

## Database Schema

### Tables Involved

```sql
-- Core workspace data
┌─────────────────────────┐
│      Workspaces         │
├─────────────────────────┤
│ • Id (PK)               │
│ • Name                  │
│ • Description           │
│ • UserId (FK)           │
│ • Visibility            │  ← NEW: Set during creation
│ • AllowPublicEdit       │  ← NEW: Set during creation
│ • CreatedAt             │
│ • UpdatedAt             │
└─────────────────────────┘

-- Associated ontology (1:1)
┌─────────────────────────┐
│      Ontologies         │
├─────────────────────────┤
│ • Id (PK)               │
│ • WorkspaceId (FK)      │
│ • Name                  │
│ • Visibility            │  ← Matches workspace
│ • AllowPublicEdit       │  ← Matches workspace
│ • Version               │  ← NEW DEFAULT: "0.1"
│ • ...                   │
└─────────────────────────┘

-- Groups (reusable across workspaces)
┌─────────────────────────┐
│      UserGroups         │
├─────────────────────────┤
│ • Id (PK)               │
│ • Name                  │  ← NEW: Created inline
│ • Description           │
│ • CreatedByUserId (FK)  │
│ • Color                 │
│ • IsActive              │
└─────────────────────────┘

-- Group membership
┌─────────────────────────┐
│   UserGroupMembers      │
├─────────────────────────┤
│ • Id (PK)               │
│ • UserGroupId (FK)      │
│ • UserId (FK)           │  ← NEW: Added inline
│ • IsGroupAdmin          │
│ • JoinedAt              │
└─────────────────────────┘

-- Group permissions on workspace
┌──────────────────────────────┐
│ WorkspaceGroupPermissions    │
├──────────────────────────────┤
│ • Id (PK)                    │
│ • WorkspaceId (FK)           │  ← NEW: Set at creation
│ • UserGroupId (FK)           │  ← NEW: Set at creation
│ • PermissionLevel            │  ← NEW: ViewAddEdit default
│ • CreatedAt                  │
└──────────────────────────────┘

-- Direct user access to workspace
┌──────────────────────────────┐
│   WorkspaceUserAccesses      │
├──────────────────────────────┤
│ • Id (PK)                    │
│ • WorkspaceId (FK)           │  ← NEW: Set at creation
│ • SharedWithUserId (FK)      │  ← NEW: Set at creation
│ • PermissionLevel            │  ← NEW: User-selected
│ • CreatedAt                  │
└──────────────────────────────┘
```

## API Endpoints

### New Endpoint: User Search

```
GET /api/users/search?query={searchTerm}

Response:
[
  {
    "id": "user-guid",
    "email": "alice@example.com",
    "displayName": "Alice Smith",
    "photoUrl": "https://..."
  },
  ...
]

• Max 10 results
• Searches: email, display name, username
• Debounced client-side (300ms)
• Requires authentication
```

### Modified Service Method

```csharp
// WorkspaceService.cs - Enhanced creation method
Task<Workspace> CreateWorkspaceWithAccessAsync(
    string userId,                      // Owner
    string name,                        // Workspace name
    string? description,                // Optional description
    string visibility,                  // "private", "group", "public"
    bool allowPublicEdit,               // If public, allow editing?
    int? groupId = null,                // Existing group ID
    string? newGroupName = null,        // Or create new group
    List<string>? newGroupMemberIds = null,  // Members for new group
    List<string>? directAccessUserIds = null, // Direct user access
    string directAccessPermissionLevel = "ViewAddEdit" // Permission level
)
```

## Privacy Levels

```
┌────────────┬─────────────────────────────────────────────────┐
│  Level     │  Access Rules                                   │
├────────────┼─────────────────────────────────────────────────┤
│  Private   │  • Owner only                                   │
│            │  • Plus users in WorkspaceUserAccesses          │
│            │  • NOT visible in public listings              │
├────────────┼─────────────────────────────────────────────────┤
│  Group     │  • Owner                                        │
│            │  • Members of groups in WorkspaceGroupPermissions
│            │  • Plus users in WorkspaceUserAccesses          │
│            │  • NOT visible in public listings              │
├────────────┼─────────────────────────────────────────────────┤
│  Public    │  • Anyone can view                              │
│            │  • Appears in public workspace discovery        │
│            │  • If AllowPublicEdit=true, anyone can edit     │
│            │  • If AllowPublicEdit=false, view-only for non- │
│            │    members                                      │
└────────────┴─────────────────────────────────────────────────┘
```

## Permission Levels

```
┌──────────────┬─────────┬─────────┬─────────┬─────────────┐
│   Level      │  View   │   Add   │  Edit   │   Manage    │
├──────────────┼─────────┼─────────┼─────────┼─────────────┤
│  View        │    ✓    │    ✗    │    ✗    │     ✗       │
├──────────────┼─────────┼─────────┼─────────┼─────────────┤
│  ViewAndAdd  │    ✓    │    ✓    │    ✗    │     ✗       │
├──────────────┼─────────┼─────────┼─────────┼─────────────┤
│  ViewAddEdit │    ✓    │    ✓    │    ✓    │     ✗       │
│  (default)   │         │         │         │             │
├──────────────┼─────────┼─────────┼─────────┼─────────────┤
│  FullAccess  │    ✓    │    ✓    │    ✓    │     ✓       │
│              │         │         │         │  (Delete,   │
│              │         │         │         │   Settings) │
└──────────────┴─────────┴─────────┴─────────┴─────────────┘
```

## Error Handling

```
┌───────────────────────────────────────────────────────────┐
│  Error Scenario                │  Handling                │
├────────────────────────────────┼──────────────────────────┤
│  Empty workspace name          │  Show validation error   │
├────────────────────────────────┼──────────────────────────┤
│  Group creation fails          │  Rollback transaction    │
│                                │  Show error toast        │
├────────────────────────────────┼──────────────────────────┤
│  User search fails             │  Show "No results"       │
│                                │  Retry on next keystroke │
├────────────────────────────────┼──────────────────────────┤
│  User not found in direct      │  Show warning            │
│  access                        │  Skip that user          │
├────────────────────────────────┼──────────────────────────┤
│  Duplicate group name          │  Show error              │
│                                │  Suggest alternative     │
├────────────────────────────────┼──────────────────────────┤
│  Network timeout               │  Show retry option       │
│                                │  Don't lose form data    │
└────────────────────────────────┴──────────────────────────┘
```

## Security Considerations

### Authorization Checks

```csharp
// 1. User must be authenticated
if (!isAuthenticated) return Unauthorized();

// 2. User can only create groups they'll be member of
// (automatically added as creator)

// 3. User can only grant permissions they themselves have
// (owner always has FullAccess)

// 4. Input validation
if (name.Length > 200) throw ValidationException;
if (description?.Length > 1000) throw ValidationException;

// 5. XSS protection
name = HtmlEncoder.Encode(name);
description = HtmlEncoder.Encode(description);

// 6. Rate limiting (future)
// Limit workspace creation to N per hour per user
```

### Data Privacy

```
• User search limited to 10 results (prevent data scraping)
• Email addresses only shown to authenticated users
• User photos respect privacy settings
• No PII in error messages or logs
```

---

**Last Updated**: November 22, 2025
**Status**: Architecture documented, ready for implementation
