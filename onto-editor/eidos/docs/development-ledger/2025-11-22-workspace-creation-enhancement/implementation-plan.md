# Implementation Plan - Workspace Creation Enhancement

**Date**: November 22, 2025
**Feature**: Enhanced Workspace Creation Dialog
**Estimated Effort**: 2-3 days

## Phase 1: Preparation & Cleanup (2-3 hours)

### Task 1.1: Remove Legacy Fields

**File**: `Components/Pages/Home.razor` (or new CreateWorkspaceDialog.razor)
**Changes**:

- [x] Remove "Ontology Frameworks" section (lines 344-361)
  - Remove UsesBFO checkbox
  - Remove UsesProvO checkbox
  - Remove explanatory text
- [x] Remove "What is an Ontology?" sidebar (lines 373-392)
- [x] Update default version from "1.0" to "0.1"

**Testing**:

- Verify create dialog renders without removed sections
- Verify workspace created has version "0.1"
- Check mobile responsiveness

### Task 1.2: Extract Dialog to Separate Component (Optional but Recommended)

**New File**: `Components/Workspace/CreateWorkspaceDialog.razor`
**Rationale**: Separate concerns, reusability, easier testing

**Approach**:

- Create new component
- Move dialog HTML from Home.razor to new component
- Add parameters for callbacks (OnCreated, OnCancelled)
- Update Home.razor to use new component

---

## Phase 2: Privacy Selection UI (3-4 hours)

### Task 2.1: Add Privacy Selector

**File**: `Components/Workspace/CreateWorkspaceDialog.razor`
**Changes**:

```html
<div class="mb-3">
  <label class="form-label">Privacy Level</label>
  <select class="form-select" @bind="selectedPrivacy">
    <option value="private">🔒 Private - Only you and invited users</option>
    <option value="group">👥 Group - Share with a team or group</option>
    <option value="public">🌍 Public - Anyone can view</option>
  </select>
  <small class="text-muted">@GetPrivacyDescription()</small>
</div>

@if (selectedPrivacy == "public")
{
  <div class="mb-3">
    <div class="form-check">
      <input class="form-check-input" type="checkbox" @bind="allowPublicEdit" />
      <label class="form-check-label">
        Allow anyone to edit (not just view)
      </label>
    </div>
  </div>
}
```

**Code-Behind**:

```csharp
private string selectedPrivacy = "private";
private bool allowPublicEdit = false;

private string GetPrivacyDescription()
{
    return selectedPrivacy switch
    {
        "private" => "Only you can access this workspace. You can invite specific users later.",
        "group" => "This workspace will be shared with a group of users you select.",
        "public" => "Anyone with the link can view this workspace.",
        _ => ""
    };
}
```

### Task 2.2: Update WorkspaceService

**File**: `Services/WorkspaceService.cs`
**Changes**:
Update `CreateWorkspaceAsync` signature:

```csharp
public async Task<Workspace> CreateWorkspaceAsync(
    string userId,
    string name,
    string? description = null,
    string visibility = "private",  // NEW
    bool allowPublicEdit = false)   // NEW
{
    // ... existing validation ...

    var workspace = new Workspace
    {
        Name = name.Trim(),
        Description = description?.Trim(),
        UserId = userId,
        Visibility = visibility,        // Use parameter
        AllowPublicEdit = allowPublicEdit, // Use parameter
        NoteCount = 0,
        ConceptNoteCount = 0,
        UserNoteCount = 0
    };

    // ... rest of method ...

    // Update ontology creation to match workspace visibility
    var ontology = new Ontology
    {
        Name = $"{name} Ontology",
        Description = $"Auto-generated ontology for workspace '{name}'",
        UserId = userId,
        WorkspaceId = created.Id,
        Visibility = visibility,        // Match workspace
        AllowPublicEdit = allowPublicEdit, // Match workspace
        Version = "0.1"  // CHANGED from "1.0"
    };

    // ... rest of method ...
}
```

---

## Phase 3: Group Selection & Creation (5-6 hours)

### Task 3.1: Create UserGroupSelector Component

**New File**: `Components/Shared/UserGroupSelector.razor`
**Purpose**: Reusable component for selecting/creating groups

**Features**:

- Show existing groups (user is member of OR user created)
- "Create New Group" option
- Inline group creation form
- Member selection for new groups

**Interface**:

```html
<UserGroupSelector
    @bind-SelectedGroupId="selectedGroupId"
    @bind-NewGroupName="newGroupName"
    @bind-NewGroupMembers="newGroupMembers"
    OnGroupChanged="HandleGroupChanged" />
```

**Implementation Outline**:

```html
<div class="group-selector">
  @if (showExistingGroups)
  {
    <select class="form-select" @bind="selectedGroupId">
      <option value="0">-- Select a group --</option>
      <option value="-1">+ Create New Group</option>
      @foreach (var group in availableGroups)
      {
        <option value="@group.Id">@group.Name (@group.Members.Count members)</option>
      }
    </select>
  }

  @if (selectedGroupId == -1)
  {
    <!-- Inline group creation form -->
    <div class="create-group-form mt-3">
      <input type="text" class="form-control mb-2"
             placeholder="Group name"
             @bind="newGroupName" />

      <textarea class="form-control mb-2"
                placeholder="Description (optional)"
                @bind="newGroupDescription"></textarea>

      <label>Group Members</label>
      <UserPicker @bind-SelectedUsers="newGroupMembers" />

      <button class="btn btn-sm btn-outline-secondary mt-2"
              @onclick="CancelNewGroup">
        Cancel
      </button>
    </div>
  }
</div>
```

**Code-Behind**:

```csharp
@code {
    [Parameter] public int SelectedGroupId { get; set; }
    [Parameter] public EventCallback<int> SelectedGroupIdChanged { get; set; }

    [Parameter] public string? NewGroupName { get; set; }
    [Parameter] public EventCallback<string?> NewGroupNameChanged { get; set; }

    private List<UserGroup> availableGroups = new();
    private List<string> newGroupMembers = new();
    private string? newGroupDescription;
    private bool showExistingGroups = true;

    protected override async Task OnInitializedAsync()
    {
        // Load groups user has created or is member of
        availableGroups = await UserGroupService.GetUserGroupsAsync(currentUserId);
    }

    private void CancelNewGroup()
    {
        selectedGroupId = 0;
        newGroupName = null;
        newGroupDescription = null;
        newGroupMembers.Clear();
    }
}
```

### Task 3.2: Create UserPicker Component

**New File**: `Components/Shared/UserPicker.razor`
**Purpose**: Multi-select user picker with autocomplete

**Features**:

- Search users by name/email
- Server-side search (API endpoint)
- Display selected users as chips/badges
- Remove selected users

**API Endpoint Required**:

```csharp
// File: Endpoints/UserSearchEndpoint.cs (NEW)
[HttpGet("/api/users/search")]
public async Task<IResult> SearchUsers(
    [FromQuery] string query,
    [FromServices] IUserService userService)
{
    if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
    {
        return Results.Ok(new List<UserSearchResult>());
    }

    var results = await userService.SearchUsersAsync(query, limit: 10);
    return Results.Ok(results);
}

public record UserSearchResult(
    string Id,
    string Email,
    string DisplayName,
    string? PhotoUrl);
```

**Component Implementation**:

```html
<div class="user-picker">
  <!-- Search input -->
  <input type="text"
         class="form-control"
         placeholder="Search users by name or email..."
         @bind="searchQuery"
         @bind:event="oninput"
         @onkeyup="HandleSearchInput" />

  <!-- Search results dropdown -->
  @if (showResults && searchResults.Any())
  {
    <div class="search-results">
      @foreach (var user in searchResults)
      {
        <div class="search-result-item" @onclick="() => SelectUser(user)">
          <img src="@user.PhotoUrl" alt="@user.DisplayName" />
          <div>
            <div>@user.DisplayName</div>
            <small>@user.Email</small>
          </div>
        </div>
      }
    </div>
  }

  <!-- Selected users -->
  <div class="selected-users mt-2">
    @foreach (var user in selectedUsers)
    {
      <span class="badge bg-primary me-1">
        @user.DisplayName
        <button type="button"
                class="btn-close btn-close-white btn-sm"
                @onclick="() => RemoveUser(user.Id)">
        </button>
      </span>
    }
  </div>
</div>
```

**Code-Behind**:

```csharp
@code {
    [Parameter] public List<string> SelectedUsers { get; set; } = new();
    [Parameter] public EventCallback<List<string>> SelectedUsersChanged { get; set; }

    private string searchQuery = "";
    private List<UserSearchResult> searchResults = new();
    private List<UserSearchResult> selectedUserObjects = new();
    private bool showResults = false;
    private System.Timers.Timer? debounceTimer;

    private async Task HandleSearchInput()
    {
        // Debounce: wait 300ms after last keystroke
        debounceTimer?.Stop();
        debounceTimer = new System.Timers.Timer(300);
        debounceTimer.Elapsed += async (s, e) => await SearchUsers();
        debounceTimer.AutoReset = false;
        debounceTimer.Start();
    }

    private async Task SearchUsers()
    {
        if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length < 2)
        {
            searchResults.Clear();
            showResults = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        var response = await Http.GetFromJsonAsync<List<UserSearchResult>>(
            $"/api/users/search?query={Uri.EscapeDataString(searchQuery)}");

        searchResults = response ?? new List<UserSearchResult>();
        showResults = true;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectUser(UserSearchResult user)
    {
        if (!SelectedUsers.Contains(user.Id))
        {
            SelectedUsers.Add(user.Id);
            selectedUserObjects.Add(user);
            await SelectedUsersChanged.InvokeAsync(SelectedUsers);
        }

        searchQuery = "";
        searchResults.Clear();
        showResults = false;
    }

    private async Task RemoveUser(string userId)
    {
        SelectedUsers.Remove(userId);
        selectedUserObjects.RemoveAll(u => u.Id == userId);
        await SelectedUsersChanged.InvokeAsync(SelectedUsers);
    }
}
```

### Task 3.3: Integrate Group Selection in Create Dialog

**File**: `Components/Workspace/CreateWorkspaceDialog.razor`
**Changes**:

```html
@if (selectedPrivacy == "group")
{
  <div class="mb-3">
    <label class="form-label">Select Group</label>
    <UserGroupSelector
        @bind-SelectedGroupId="selectedGroupId"
        @bind-NewGroupName="newGroupName"
        @bind-NewGroupMembers="newGroupMembers" />
  </div>
}
```

---

## Phase 4: Direct User Access (3-4 hours)

### Task 4.1: Add User Access Section

**File**: `Components/Workspace/CreateWorkspaceDialog.razor`
**Changes**:

```html
<div class="mb-3">
  <div class="form-check">
    <input class="form-check-input"
           type="checkbox"
           id="grantUserAccess"
           @bind="enableUserAccess" />
    <label class="form-check-label" for="grantUserAccess">
      Grant access to specific users
    </label>
  </div>
</div>

@if (enableUserAccess)
{
  <div class="user-access-section mb-3">
    <label class="form-label">Select Users</label>
    <UserPicker @bind-SelectedUsers="directAccessUserIds" />

    <div class="mt-2">
      <label class="form-label">Permission Level</label>
      <select class="form-select" @bind="directAccessPermissionLevel">
        <option value="View">View Only</option>
        <option value="ViewAndAdd">View & Add</option>
        <option value="ViewAddEdit">View, Add & Edit</option>
        <option value="FullAccess">Full Access</option>
      </select>
    </div>
  </div>
}
```

**Code-Behind**:

```csharp
private bool enableUserAccess = false;
private List<string> directAccessUserIds = new();
private string directAccessPermissionLevel = "ViewAddEdit";
```

---

## Phase 5: Service Integration (4-5 hours)

### Task 5.1: Update WorkspaceService for Group & User Access

**File**: `Services/WorkspaceService.cs`
**New Method**:

```csharp
/// <summary>
/// Create workspace with full privacy and access configuration
/// </summary>
public async Task<Workspace> CreateWorkspaceWithAccessAsync(
    string userId,
    string name,
    string? description,
    string visibility,
    bool allowPublicEdit,
    int? groupId = null,
    string? newGroupName = null,
    List<string>? newGroupMemberIds = null,
    List<string>? directAccessUserIds = null,
    string directAccessPermissionLevel = "ViewAddEdit")
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 1. Create the workspace
        var workspace = await CreateWorkspaceAsync(
            userId, name, description, visibility, allowPublicEdit);

        // 2. Handle group creation if needed
        if (visibility == "group")
        {
            int targetGroupId = groupId ?? 0;

            // Create new group if requested
            if (targetGroupId == -1 && !string.IsNullOrEmpty(newGroupName))
            {
                var newGroup = await _userGroupService.CreateGroupAsync(
                    newGroupName,
                    $"Group for workspace '{name}'",
                    userId);

                targetGroupId = newGroup.Id;

                // Add members to new group
                if (newGroupMemberIds?.Any() == true)
                {
                    foreach (var memberId in newGroupMemberIds)
                    {
                        await _userGroupService.AddUserToGroupAsync(
                            targetGroupId, memberId, userId);
                    }
                }
            }

            // Grant group permission to workspace
            if (targetGroupId > 0)
            {
                await _permissionService.GrantGroupPermissionAsync(
                    workspace.Id,
                    targetGroupId,
                    PermissionLevel.ViewAddEdit,
                    userId);
            }
        }

        // 3. Grant direct user access
        if (directAccessUserIds?.Any() == true)
        {
            foreach (var accessUserId in directAccessUserIds)
            {
                await _permissionService.GrantUserAccessAsync(
                    workspace.Id,
                    accessUserId,
                    Enum.Parse<PermissionLevel>(directAccessPermissionLevel));
            }
        }

        await transaction.CommitAsync();

        _logger.LogInformation(
            "Created workspace {WorkspaceId} with {GroupCount} groups and {UserCount} direct access grants",
            workspace.Id,
            groupId.HasValue ? 1 : 0,
            directAccessUserIds?.Count ?? 0);

        return workspace;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Failed to create workspace with access configuration");
        throw;
    }
}
```

### Task 5.2: Create Permission Service Methods

**File**: `Services/WorkspacePermissionService.cs` (NEW or add to existing)

```csharp
public async Task GrantGroupPermissionAsync(
    int workspaceId,
    int groupId,
    PermissionLevel permissionLevel,
    string grantedByUserId)
{
    using var context = await _contextFactory.CreateDbContextAsync();

    var permission = new WorkspaceGroupPermission
    {
        WorkspaceId = workspaceId,
        UserGroupId = groupId,
        PermissionLevel = permissionLevel,
        CreatedAt = DateTime.UtcNow
    };

    context.WorkspaceGroupPermissions.Add(permission);
    await context.SaveChangesAsync();
}

public async Task GrantUserAccessAsync(
    int workspaceId,
    string userId,
    PermissionLevel permissionLevel)
{
    using var context = await _contextFactory.CreateDbContextAsync();

    var access = new WorkspaceUserAccess
    {
        WorkspaceId = workspaceId,
        SharedWithUserId = userId,
        PermissionLevel = permissionLevel,
        CreatedAt = DateTime.UtcNow
    };

    context.WorkspaceUserAccesses.Add(access);
    await context.SaveChangesAsync();
}
```

### Task 5.3: Wire Up Create Dialog

**File**: `Components/Workspace/CreateWorkspaceDialog.razor`
**Update CreateWorkspace method**:

```csharp
private async Task CreateWorkspace()
{
    if (string.IsNullOrWhiteSpace(newWorkspaceName))
    {
        ToastService.ShowWarning("Please enter a workspace name");
        return;
    }

    try
    {
        creating = true;

        var workspace = await WorkspaceService.CreateWorkspaceWithAccessAsync(
            currentUserId!,
            newWorkspaceName,
            newWorkspaceDescription,
            selectedPrivacy,
            allowPublicEdit,
            groupId: selectedGroupId,
            newGroupName: newGroupName,
            newGroupMemberIds: newGroupMembers,
            directAccessUserIds: directAccessUserIds,
            directAccessPermissionLevel: directAccessPermissionLevel
        );

        ToastService.ShowSuccess($"Created workspace '{newWorkspaceName}'");
        await OnWorkspaceCreated.InvokeAsync(workspace);
        HideCreateDialog();

        // Navigate to the new workspace
        Navigation.NavigateTo($"workspace/{workspace.Id}");
    }
    catch (Exception ex)
    {
        ToastService.ShowError($"Error creating workspace: {ex.Message}");
        _logger.LogError(ex, "Failed to create workspace");
    }
    finally
    {
        creating = false;
    }
}
```

---

## Phase 6: Testing (4-5 hours)

### Task 6.1: Unit Tests

**New File**: `Eidos.Tests/Services/WorkspaceCreationWithAccessTests.cs`

**Test Cases**:

```csharp
[Fact]
public async Task CreateWorkspace_Private_CreatesWithPrivateVisibility()
{
    // Test private workspace creation
}

[Fact]
public async Task CreateWorkspace_WithGroup_CreatesGroupPermission()
{
    // Test group-based workspace
}

[Fact]
public async Task CreateWorkspace_WithNewGroup_CreatesGroupAndMembers()
{
    // Test inline group creation
}

[Fact]
public async Task CreateWorkspace_WithDirectAccess_CreatesUserAccessRecords()
{
    // Test direct user access grants
}

[Fact]
public async Task CreateWorkspace_DefaultVersion_Is_0_1()
{
    // Test default version is 0.1
}

[Fact]
public async Task CreateWorkspace_Public_WithEditEnabled_AllowsPublicEdit()
{
    // Test public workspace with edit permissions
}
```

### Task 6.2: Component Tests

**New File**: `Eidos.Tests/Components/CreateWorkspaceDialogTests.cs`

**Test Cases**:

- Privacy selector changes visibility
- Group section shows/hides based on privacy
- User access section expands/collapses
- Validation works correctly
- Error handling displays messages

### Task 6.3: Integration Tests

**Manual Testing Checklist**:

- [ ] Create private workspace (default)
- [ ] Create workspace with existing group
- [ ] Create workspace with new group (inline)
- [ ] Create workspace with direct user access
- [ ] Create public workspace with edit enabled
- [ ] Verify permissions are correctly applied
- [ ] Verify workspace AND ontology have matching visibility
- [ ] Test mobile responsiveness
- [ ] Test error handling (network errors, validation)
- [ ] Test with long user lists (performance)

---

## Phase 7: Documentation (2 hours)

### Task 7.1: Update User Documentation

**File**: `docs/user-guides/WORKSPACES_AND_NOTES.md`
**Add Section**: "Creating a Workspace with Privacy Controls"

### Task 7.2: Update CLAUDE.md

**File**: `CLAUDE.md`
**Add Section**: Document new workspace creation flow

### Task 7.3: Create Migration Guide

**File**: `docs/migration/workspace-creation-v2.md`
**Content**: How to use new creation flow, changes from old flow

---

## Implementation Order

1. ✅ Phase 1: Cleanup (remove old fields) - **Start Here**
2. Phase 2: Privacy selection
3. Phase 3: User picker component (needed for both groups and direct access)
4. Phase 3: Group selector component
5. Phase 4: Direct user access
6. Phase 5: Service integration
7. Phase 6: Testing
8. Phase 7: Documentation

## Risk Mitigation

**Risk**: Group creation fails mid-transaction
**Mitigation**: Use database transactions, rollback on error

**Risk**: User search endpoint performance issues
**Mitigation**: Implement search debouncing, limit results to 10

**Risk**: UI becomes too complex
**Mitigation**: Use progressive disclosure, hide advanced options

**Risk**: Breaking changes to existing workspace creation
**Mitigation**: Keep backward compatibility, make new features optional

---

## Success Criteria

- [ ] All legacy fields removed
- [ ] Privacy selector functional
- [ ] Group creation works inline
- [ ] Direct user access works
- [ ] All tests pass (unit + integration)
- [ ] Documentation updated
- [ ] No regressions in existing workspace features
- [ ] Mobile-responsive design
- [ ] Performance: < 2s workspace creation time

---

**Last Updated**: November 22, 2025
**Status**: Ready for implementation
**Estimated Completion**: 2-3 days (16-24 hours)
