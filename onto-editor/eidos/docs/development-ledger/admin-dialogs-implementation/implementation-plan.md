# Implementation Plan - Admin Dialogs
**Last Updated**: November 2, 2025

## Overview

Phased implementation approach:
1. **Phase 1**: Core infrastructure (dialog, list, basic display)
2. **Phase 2**: Search, filter, sort functionality
3. **Phase 3**: Inline editing
4. **Phase 4**: Delete functionality
5. **Phase 5**: Polish and optimization

## Phase 1: Core Infrastructure

### Task 1.1: Create Component Directory
**Estimated Time**: 5 minutes

```bash
mkdir -p Components/Ontology/Admin
```

**Files to create**:
- `Components/Ontology/Admin/` directory

### Task 1.2: Create AdminConceptDialog Component
**Estimated Time**: 30 minutes

**File**: `Components/Ontology/Admin/AdminConceptDialog.razor`

**Responsibilities**:
- Dialog visibility management
- Load concepts from service
- Display concept list
- Handle permissions

**Key Code**:
```razor
@using Eidos.Services
@using Eidos.Models
@inject ConceptService ConceptService
@inject OntologyPermissionService PermissionService
@inject ToastService ToastService

<FloatingPanel @ref="dialogPanel"
               Title="Manage Concepts"
               Size="FloatingPanelSize.Large"
               OnClose="Hide">
    <div class="admin-dialog-content">
        @if (isLoading)
        {
            <div class="text-center py-5">
                <div class="spinner-border"></div>
                <p class="text-muted mt-2">Loading concepts...</p>
            </div>
        }
        else if (concepts.Any())
        {
            <div class="entity-list">
                @foreach (var concept in concepts)
                {
                    <div class="entity-list-item" @key="concept.Id">
                        <div class="d-flex justify-content-between align-items-start">
                            <div>
                                <strong>@concept.Name</strong>
                                <div class="text-muted small">
                                    @(concept.Description ?? "No description")
                                </div>
                            </div>
                        </div>
                    </div>
                }
            </div>
        }
        else
        {
            <div class="text-center py-5 text-muted">
                <p>No concepts found</p>
            </div>
        }
    </div>
</FloatingPanel>

@code {
    [Parameter] public int OntologyId { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }

    private FloatingPanel? dialogPanel;
    private List<Concept> concepts = new();
    private bool isLoading = false;

    public async Task Show()
    {
        dialogPanel?.Show();
        await LoadConcepts();
    }

    public void Hide()
    {
        dialogPanel?.Hide();
    }

    private async Task LoadConcepts()
    {
        try
        {
            isLoading = true;
            concepts = (await ConceptService.GetByOntologyIdAsync(OntologyId)).ToList();
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Failed to load concepts");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}
```

### Task 1.3: Create CSS File
**Estimated Time**: 20 minutes

**File**: `wwwroot/css/admin-dialogs.css`

```css
/* Admin Dialog Container */
.admin-dialog-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    max-height: calc(100vh - 200px);
    overflow-y: auto;
}

/* Entity List */
.entity-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.entity-list-item {
    padding: 0.75rem 1rem;
    border: 1px solid var(--bs-border-color);
    border-radius: 0.375rem;
    background-color: var(--bs-body-bg);
    transition: background-color 150ms ease;
}

.entity-list-item:hover {
    background-color: rgba(0, 0, 0, 0.02);
}

[data-bs-theme="dark"] .entity-list-item:hover {
    background-color: rgba(255, 255, 255, 0.05);
}
```

### Task 1.4: Add Reference to OntologyView
**Estimated Time**: 15 minutes

**File**: `Components/Pages/OntologyView.razor`

**Add at top**:
```razor
<AdminConceptDialog @ref="adminConceptDialog"
                    OntologyId="@ontologyId"
                    OnChanged="HandleAdminDialogChanged" />
```

**Add in toolbar** (after existing buttons):
```razor
<button class="btn btn-sm btn-outline-secondary"
        @onclick="ShowAdminConceptDialog"
        title="Manage all concepts">
    <i class="bi bi-list-ul"></i> Manage Concepts
</button>
```

**Add in code section**:
```csharp
private AdminConceptDialog? adminConceptDialog;

private void ShowAdminConceptDialog()
{
    adminConceptDialog?.Show();
}

private async Task HandleAdminDialogChanged()
{
    // Reload ontology data
    await LoadOntologyAsync();
}
```

### Task 1.5: Add CSS Reference
**Estimated Time**: 5 minutes

**File**: `Components/App.razor` or `Pages/_Host.cshtml`

```html
<link rel="stylesheet" href="css/admin-dialogs.css" />
```

**Deliverables**:
- ✅ Dialog opens and closes
- ✅ Displays list of concepts
- ✅ Loading state works
- ✅ Empty state shows

---

## Phase 2: Search, Filter, Sort

### Task 2.1: Add Search Input
**Estimated Time**: 20 minutes

**File**: `Components/Ontology/Admin/AdminConceptDialog.razor`

**Add above entity list**:
```razor
<div class="admin-dialog-filters">
    <div class="input-group">
        <span class="input-group-text">
            <i class="bi bi-search"></i>
        </span>
        <input type="search"
               class="form-control"
               placeholder="Search concepts..."
               @bind-value="searchTerm"
               @bind-value:event="oninput"
               aria-label="Search concepts" />
        @if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            <button class="btn btn-outline-secondary"
                    @onclick="ClearSearch"
                    title="Clear search">
                <i class="bi bi-x"></i>
            </button>
        }
    </div>
</div>
```

**Add to code**:
```csharp
private string searchTerm = "";

private IEnumerable<Concept> FilteredConcepts =>
    concepts.Where(c =>
        string.IsNullOrWhiteSpace(searchTerm) ||
        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        (c.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
    );

private void ClearSearch()
{
    searchTerm = "";
}
```

**Update loop**:
```razor
@foreach (var concept in FilteredConcepts)
```

### Task 2.2: Add Filter Dropdown
**Estimated Time**: 25 minutes

**Add next to search**:
```razor
<select class="form-select" @bind="filterCategory" style="max-width: 200px;">
    <option value="all">All Categories</option>
    @foreach (var category in Categories)
    {
        <option value="@category">@category</option>
    }
</select>
```

**Add to code**:
```csharp
private string filterCategory = "all";

private IEnumerable<string> Categories =>
    concepts
        .Select(c => c.Category)
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .Distinct()
        .OrderBy(c => c);

private IEnumerable<Concept> FilteredConcepts =>
    concepts
        .Where(c => string.IsNullOrWhiteSpace(searchTerm) ||
                    c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (c.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
        .Where(c => filterCategory == "all" || c.Category == filterCategory);
```

### Task 2.3: Add Sort Dropdown
**Estimated Time**: 20 minutes

**Add next to filter**:
```razor
<select class="form-select" @bind="sortBy" style="max-width: 180px;">
    <option value="name">Sort: Name (A-Z)</option>
    <option value="created">Sort: Created (New)</option>
    <option value="modified">Sort: Modified (Recent)</option>
</select>
```

**Add to code**:
```csharp
private string sortBy = "name";

private IEnumerable<Concept> FilteredAndSortedConcepts =>
    FilteredConcepts.OrderBy(c => sortBy switch
    {
        "name" => c.Name,
        "created" => c.CreatedAt.ToString("yyyy-MM-dd"),
        "modified" => c.UpdatedAt?.ToString("yyyy-MM-dd") ?? c.CreatedAt.ToString("yyyy-MM-dd"),
        _ => c.Name
    });
```

### Task 2.4: Add Result Count
**Estimated Time**: 10 minutes

**Add at bottom**:
```razor
<div class="admin-dialog-footer text-muted small">
    Showing @FilteredAndSortedConcepts.Count() of @concepts.Count concepts
</div>
```

**Deliverables**:
- ✅ Search filters in real-time
- ✅ Category filter works
- ✅ Sort options work
- ✅ Result count displays
- ✅ Clear search button

---

## Phase 3: Inline Editing

### Task 3.1: Add Action Buttons
**Estimated Time**: 15 minutes

**Update entity list item**:
```razor
<div class="entity-list-item" @key="concept.Id">
    <div class="d-flex justify-content-between align-items-start">
        <div class="flex-grow-1">
            <strong>@concept.Name</strong>
            <div class="text-muted small">
                @(concept.Description ?? "No description")
            </div>
        </div>
        <div class="entity-actions d-flex gap-2">
            <button class="btn btn-sm btn-outline-primary"
                    @onclick="() => ToggleEdit(concept.Id)"
                    title="Edit @concept.Name">
                <i class="bi bi-pencil-square"></i>
            </button>
        </div>
    </div>
</div>
```

**Add to code**:
```csharp
private int? editingConceptId = null;

private void ToggleEdit(int conceptId)
{
    editingConceptId = editingConceptId == conceptId ? null : conceptId;
}
```

### Task 3.2: Add Inline Edit Form
**Estimated Time**: 35 minutes

**Update entity list item**:
```razor
<div class="entity-list-item @(editingConceptId == concept.Id ? "editing" : "")"
     @key="concept.Id">
    @if (editingConceptId == concept.Id)
    {
        <div class="entity-edit-form">
            <div class="mb-2">
                <label class="form-label small">Name</label>
                <input type="text"
                       class="form-control form-control-sm"
                       @bind="editingConcept.Name" />
            </div>
            <div class="mb-2">
                <label class="form-label small">Description</label>
                <textarea class="form-control form-control-sm"
                          rows="2"
                          @bind="editingConcept.Description"></textarea>
            </div>
            <div class="mb-2">
                <label class="form-label small">Category</label>
                <input type="text"
                       class="form-control form-control-sm"
                       @bind="editingConcept.Category" />
            </div>
            <div class="d-flex justify-content-end gap-2">
                <button class="btn btn-sm btn-secondary"
                        @onclick="CancelEdit">
                    Cancel
                </button>
                <button class="btn btn-sm btn-success"
                        @onclick="SaveEdit"
                        disabled="@isSaving">
                    @if (isSaving)
                    {
                        <span class="spinner-border spinner-border-sm me-1"></span>
                    }
                    Save
                </button>
            </div>
        </div>
    }
    else
    {
        <!-- Existing display code -->
    }
</div>
```

**Add to code**:
```csharp
private Concept editingConcept = new();
private bool isSaving = false;

private void ToggleEdit(int conceptId)
{
    if (editingConceptId == conceptId)
    {
        editingConceptId = null;
    }
    else
    {
        var concept = concepts.First(c => c.Id == conceptId);
        editingConcept = new Concept
        {
            Id = concept.Id,
            Name = concept.Name,
            Description = concept.Description,
            Category = concept.Category
        };
        editingConceptId = conceptId;
    }
}

private void CancelEdit()
{
    editingConceptId = null;
    editingConcept = new();
}

private async Task SaveEdit()
{
    try
    {
        isSaving = true;

        var command = new UpdateConceptCommand
        {
            Name = editingConcept.Name,
            Description = editingConcept.Description,
            Category = editingConcept.Category
        };

        await ConceptService.UpdateAsync(editingConcept.Id, command);

        ToastService.ShowSuccess($"Updated '{editingConcept.Name}'");

        await LoadConcepts();
        editingConceptId = null;
        await OnChanged.InvokeAsync();
    }
    catch (Exception ex)
    {
        ToastService.ShowError("Failed to save changes");
    }
    finally
    {
        isSaving = false;
    }
}
```

**Deliverables**:
- ✅ Click Edit to expand inline form
- ✅ Form shows current values
- ✅ Cancel collapses form
- ✅ Save updates concept
- ✅ Loading state during save

---

## Phase 4: Delete Functionality

### Task 4.1: Add Delete Button
**Estimated Time**: 10 minutes

**Add to action buttons**:
```razor
<button class="btn btn-sm btn-outline-danger"
        @onclick="() => DeleteConcept(concept.Id)"
        title="Delete @concept.Name">
    <i class="bi bi-trash"></i>
</button>
```

### Task 4.2: Implement Delete with Confirmation
**Estimated Time**: 25 minutes

**Add to code**:
```csharp
@inject ConfirmService ConfirmService

private async Task DeleteConcept(int conceptId)
{
    var concept = concepts.First(c => c.Id == conceptId);

    var confirmed = await ConfirmService.ShowAsync(
        title: "Delete Concept",
        message: $"Are you sure you want to delete '{concept.Name}'? This cannot be undone.",
        confirmText: "Delete",
        cancelText: "Cancel"
    );

    if (!confirmed) return;

    try
    {
        isLoading = true;

        await ConceptService.DeleteAsync(conceptId);

        ToastService.ShowSuccess($"Deleted '{concept.Name}'");

        await LoadConcepts();
        await OnChanged.InvokeAsync();
    }
    catch (InvalidOperationException ex)
    {
        // Concept has relationships
        ToastService.ShowError(ex.Message);
    }
    catch (Exception ex)
    {
        ToastService.ShowError("Failed to delete concept");
    }
    finally
    {
        isLoading = false;
    }
}
```

**Deliverables**:
- ✅ Delete button shows
- ✅ Confirmation dialog appears
- ✅ Concept deletes on confirm
- ✅ Error shown if concept has relationships
- ✅ List updates after delete

---

## Phase 5: Polish & Optimization

### Task 5.1: Add Status Indicators
**Estimated Time**: 20 minutes

**Add validation badges**:
```razor
<span class="badge bg-@GetValidationClass(concept) me-2">
    @GetValidationIcon(concept)
</span>
<strong>@concept.Name</strong>
```

**Add to code**:
```csharp
private string GetValidationClass(Concept concept)
{
    // Check if orphaned, has warnings, etc.
    var relationships = /* get from state */;
    if (!relationships.Any()) return "info";
    if (string.IsNullOrWhiteSpace(concept.Description)) return "warning";
    return "success";
}

private string GetValidationIcon(Concept concept)
{
    var cssClass = GetValidationClass(concept);
    return cssClass switch
    {
        "success" => "✓",
        "warning" => "!",
        "info" => "i",
        _ => "✓"
    };
}
```

### Task 5.2: Add Keyboard Shortcuts
**Estimated Time**: 15 minutes

```razor
<div @onkeydown="HandleKeyDown" tabindex="0">
    <!-- Content -->
</div>
```

```csharp
private void HandleKeyDown(KeyboardEventArgs e)
{
    if (e.Key == "Escape")
    {
        if (editingConceptId != null)
            CancelEdit();
        else
            Hide();
    }
}
```

### Task 5.3: Add Responsive Styles
**Estimated Time**: 20 minutes

**Update CSS**:
```css
@media (max-width: 767px) {
    .admin-dialog-filters {
        flex-direction: column;
        gap: 0.5rem;
    }

    .admin-dialog-filters .form-select,
    .admin-dialog-filters .input-group {
        max-width: 100% !important;
    }

    .entity-actions {
        flex-direction: column;
    }
}
```

### Task 5.4: Add Permission Checks
**Estimated Time**: 15 minutes

```csharp
protected override async Task OnParametersSetAsync()
{
    var canManage = await PermissionService.CanManageAsync(
        OntologyId,
        CurrentUserId
    );

    if (!canManage)
    {
        ToastService.ShowError("Admin access required");
        Hide();
    }
}
```

**Deliverables**:
- ✅ Status badges show
- ✅ Keyboard shortcuts work
- ✅ Mobile-responsive layout
- ✅ Permission checks in place
- ✅ Final polish complete

---

## Testing Checklist

- [ ] Dialog opens/closes correctly
- [ ] Concepts load and display
- [ ] Search filters in real-time
- [ ] Category filter works
- [ ] Sort options work correctly
- [ ] Edit mode expands/collapses
- [ ] Save updates concept successfully
- [ ] Cancel discards changes
- [ ] Delete shows confirmation
- [ ] Delete removes concept from list
- [ ] Error handling for failed operations
- [ ] Loading states display correctly
- [ ] Empty states show
- [ ] Permission checks work
- [ ] Responsive on mobile
- [ ] Keyboard navigation works
- [ ] Dark mode looks correct

## Future Enhancements

- [ ] Bulk operations (select multiple, delete all)
- [ ] Duplicate concept action
- [ ] Export filtered list to CSV
- [ ] Drag-and-drop reordering
- [ ] Undo/redo for edits
- [ ] Virtual scrolling for large lists (>1000 items)
- [ ] Relationship and Individual dialogs
- [ ] Advanced filters (created by, date range)

---
**Estimated Total Time**: 5-6 hours
**Complexity**: Medium
**Dependencies**: FloatingPanel, ConceptService, ToastService, ConfirmService
