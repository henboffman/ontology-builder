# Reference Patterns - Admin Dialogs
**Last Updated**: November 2, 2025

This document points to existing code patterns to follow when implementing admin dialogs.

## Component Patterns

### FloatingPanel Pattern
**File**: `/Components/Shared/FloatingPanel.razor`
**Use for**: Main dialog container

Key features to adopt:
- Size variants (small: 400px, medium: 600px, large: 800px)
- Draggable header
- Fixed positioning with z-index: 1050
- Close button with `OnClose` callback
- Smooth slide-in animation (200ms)

**Example Usage**:
```razor
<FloatingPanel @ref="dialogPanel"
               Title="Manage Concepts"
               Size="FloatingPanelSize.Large"
               OnClose="HandleClose">
    <!-- Dialog content here -->
</FloatingPanel>
```

### ConceptManagementPanel Pattern
**File**: `/Components/Ontology/ConceptManagementPanel.razor`
**Use for**: Dialog lifecycle management

Key methods to implement:
- `public void ShowAdd()` - Opens dialog in add mode
- `public void ShowEdit(Concept concept)` - Opens in edit mode
- `public void Hide()` - Closes dialog
- `public void FocusInput()` - Focus first input on open

Key features:
- `@ref` for programmatic control
- EventCallback for `OnConceptChanged`
- Permission checks before opening
- Toast notifications on success/error

### ConceptListView Pattern
**File**: `/Components/Ontology/ConceptListView.razor`
**Use for**: List rendering and filtering

Key features to adopt:
- Real-time search filtering
- Sort dropdown (name, category, created)
- Responsive layout (desktop vs mobile)
- Empty state handling
- Validation status indicators

**Search Pattern**:
```csharp
private string searchTerm = "";

private IEnumerable<Concept> FilteredConcepts =>
    viewState.Concepts.Where(c =>
        string.IsNullOrWhiteSpace(searchTerm) ||
        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        (c.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
    );
```

## Service Patterns

### ConceptService CRUD Pattern
**File**: `/Services/ConceptService.cs`

**Update Method**:
```csharp
public async Task<Concept> UpdateAsync(int id, UpdateConceptCommand command)
{
    // 1. Get existing entity
    var concept = await _repository.GetByIdAsync(id);
    if (concept == null) throw new NotFoundException();

    // 2. Update properties
    concept.Name = command.Name;
    concept.Description = command.Description;
    // ...

    // 3. Save
    await _repository.UpdateAsync(concept);

    // 4. Record activity
    await _activityService.RecordUpdateAsync(concept, beforeSnapshot, userId);

    // 5. Broadcast change
    await _hub.Clients.All.SendAsync("ConceptUpdated", concept);

    return concept;
}
```

**Delete Method**:
```csharp
public async Task DeleteAsync(int id, string userId)
{
    // 1. Permission check (done in controller/component)

    // 2. Get entity
    var concept = await _repository.GetByIdAsync(id);
    if (concept == null) throw new NotFoundException();

    // 3. Check dependencies
    var relationships = await _relationshipRepo.GetByConceptIdAsync(id);
    if (relationships.Any())
    {
        throw new InvalidOperationException(
            $"Cannot delete concept with {relationships.Count()} relationships"
        );
    }

    // 4. Delete
    await _repository.DeleteAsync(id);

    // 5. Record activity
    await _activityService.RecordDeleteAsync(concept, userId);

    // 6. Broadcast
    await _hub.Clients.All.SendAsync("ConceptDeleted", id);
}
```

### Permission Check Pattern
**File**: `/Services/OntologyPermissionService.cs`

```csharp
// Check if user can manage (edit/delete) entities
var canManage = await _permissionService.CanManageAsync(
    ontologyId,
    userId
);

if (!canManage)
{
    _toastService.ShowError("Admin access required");
    return;
}
```

### Toast Notification Pattern
**File**: `/Services/ToastService.cs`

```csharp
// Success
_toastService.ShowSuccess($"Concept '{concept.Name}' updated");

// Error
_toastService.ShowError("Failed to delete concept");

// Warning
_toastService.ShowWarning("This concept has 5 relationships");

// Info
_toastService.ShowInfo("Changes saved automatically");
```

### Confirm Dialog Pattern
**File**: `/Services/ConfirmService.cs`

```csharp
var confirmed = await _confirmService.ShowAsync(
    title: "Delete Concept",
    message: $"Are you sure you want to delete '{concept.Name}'? This cannot be undone.",
    confirmText: "Delete",
    cancelText: "Cancel"
);

if (confirmed)
{
    await DeleteConcept(concept.Id);
}
```

## Styling Patterns

### CSS File Organization
**File**: `/wwwroot/css/floating-panel.css`

Structure to follow:
1. Block-level styles (`.admin-dialog`)
2. Element styles (`.admin-dialog__header`)
3. Modifier styles (`.admin-dialog--loading`)
4. Responsive overrides (`@media`)
5. Dark mode overrides (`[data-bs-theme="dark"]`)

### Bootstrap Class Usage
**Pattern**: Prefer Bootstrap classes, add custom only when needed

Common patterns:
- `list-group` / `list-group-item` for lists
- `form-control` / `form-select` for inputs
- `btn btn-sm btn-outline-*` for action buttons
- `badge bg-*` for status indicators
- `text-muted` for secondary text
- `d-flex justify-content-between` for layouts

### Dark Mode Pattern
**File**: Any CSS file in project

```css
/* Light mode (default) */
.my-component {
    background: #ffffff;
    color: #212529;
}

/* Dark mode */
[data-bs-theme="dark"] .my-component {
    background: #212529;
    color: #ffffff;
}
```

## State Management Patterns

### Component State Pattern
**File**: `/Components/Pages/OntologyView.razor`

```csharp
@code {
    // Services
    [Inject] private ConceptService ConceptService { get; set; }
    [Inject] private ToastService ToastService { get; set; }
    [Inject] private ConfirmService ConfirmService { get; set; }

    // Component refs
    private AdminConceptDialog? adminDialog;

    // State
    private List<Concept> concepts = new();
    private bool isLoading = false;
    private string? errorMessage = null;

    // Lifecycle
    protected override async Task OnInitializedAsync()
    {
        await LoadConcepts();
    }

    // Methods
    private async Task LoadConcepts()
    {
        try
        {
            isLoading = true;
            concepts = await ConceptService.GetByOntologyIdAsync(ontologyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load concepts");
            errorMessage = "Failed to load concepts";
        }
        finally
        {
            isLoading = false;
        }
    }

    // Public methods for child components
    public void ShowAdminDialog()
    {
        adminDialog?.Show();
    }
}
```

## Error Handling Patterns

### Try-Catch-Finally Pattern
```csharp
private async Task SaveEntity()
{
    try
    {
        isLoading = true;
        errorMessage = null;

        await EntityService.UpdateAsync(entity);

        ToastService.ShowSuccess("Saved successfully");
        await Reload();
    }
    catch (NotFoundException)
    {
        ToastService.ShowError("Entity not found");
    }
    catch (ValidationException ex)
    {
        ToastService.ShowError(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to save entity");
        ToastService.ShowError("An error occurred");
    }
    finally
    {
        isLoading = false;
        StateHasChanged();
    }
}
```

## Responsive Design Patterns

### Mobile-First Approach
**File**: `/Components/Ontology/ConceptListView.razor`

```razor
<!-- Mobile: Vertical stack -->
<div class="d-md-none">
    <div class="d-grid gap-2">
        <button class="btn btn-primary">Edit</button>
        <button class="btn btn-danger">Delete</button>
    </div>
</div>

<!-- Desktop: Horizontal buttons -->
<div class="d-none d-md-flex gap-2">
    <button class="btn btn-sm btn-outline-primary">Edit</button>
    <button class="btn btn-sm btn-outline-danger">Delete</button>
</div>
```

### Responsive Dialog Sizing
```css
/* Desktop */
@media (min-width: 768px) {
    .admin-dialog {
        width: 800px;
        right: 20px;
    }
}

/* Tablet */
@media (max-width: 767px) and (min-width: 576px) {
    .admin-dialog {
        width: 90vw;
        right: 5vw;
    }
}

/* Mobile */
@media (max-width: 575px) {
    .admin-dialog {
        width: 100vw;
        right: 0;
        left: 0;
    }
}
```

## Performance Patterns

### Debounced Search
```csharp
private System.Timers.Timer? searchDebounceTimer;

private void OnSearchInput(ChangeEventArgs e)
{
    searchDebounceTimer?.Stop();
    searchDebounceTimer = new System.Timers.Timer(300);
    searchDebounceTimer.Elapsed += (s, args) =>
    {
        InvokeAsync(() =>
        {
            searchTerm = e.Value?.ToString() ?? "";
            StateHasChanged();
        });
    };
    searchDebounceTimer.AutoReset = false;
    searchDebounceTimer.Start();
}
```

### Use @key for List Items
```razor
@foreach (var concept in FilteredConcepts)
{
    <EntityListItem @key="concept.Id"
                    Entity="concept"
                    OnEdit="HandleEdit"
                    OnDelete="HandleDelete" />
}
```

## Accessibility Patterns

### Focus Management
```csharp
private ElementReference searchInput;

public async Task Show()
{
    isVisible = true;
    StateHasChanged();
    await Task.Delay(100); // Wait for render
    await searchInput.FocusAsync();
}
```

### Keyboard Navigation
```razor
<div @onkeydown="HandleKeyDown">
    <!-- Content -->
</div>

@code {
    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            Hide();
        }
        else if (e.Key == "Enter" && selectedEntityId != null)
        {
            ToggleExpanded(selectedEntityId.Value);
        }
    }
}
```

## File Paths Quick Reference

### Components
- FloatingPanel: `/Components/Shared/FloatingPanel.razor`
- ConceptManagementPanel: `/Components/Ontology/ConceptManagementPanel.razor`
- ConceptListView: `/Components/Ontology/ConceptListView.razor`
- OntologyView: `/Components/Pages/OntologyView.razor`

### Services
- ConceptService: `/Services/ConceptService.cs`
- OntologyPermissionService: `/Services/OntologyPermissionService.cs`
- ToastService: `/Services/ToastService.cs`
- ConfirmService: `/Services/ConfirmService.cs`

### Styling
- Floating Panel CSS: `/wwwroot/css/floating-panel.css`
- Bootstrap: `/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css`

### Models
- Concept: `/Models/Concept.cs`
- Relationship: `/Models/Relationship.cs`
- Individual: `/Models/Individual.cs`

---
**Next**: See [implementation-plan.md](./implementation-plan.md) to begin coding
