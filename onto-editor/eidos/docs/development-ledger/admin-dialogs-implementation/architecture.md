# Architecture - Admin Dialogs
**Last Updated**: November 2, 2025

## Component Hierarchy

```
OntologyView.razor
└── AdminEntityDialog.razor (new)
    ├── FloatingPanel.razor (existing)
    └── Dialog Content:
        ├── EntityListHeader (new)
        │   ├── SearchBox (new)
        │   ├── FilterDropdown (new)
        │   └── SortDropdown (new)
        ├── EntityList (new)
        │   └── EntityListItem (new) × N
        │       ├── EntityDisplay (collapsed state)
        │       └── EntityEditForm (expanded state)
        └── EntityListFooter (new)
            └── Result count
```

## Component Responsibilities

### AdminEntityDialog.razor
**Purpose**: Main container for entity management
**Responsibilities**:
- Manage dialog visibility
- Load entity data from services
- Handle search/filter/sort state
- Coordinate child components
- Handle permission checks

**Key Properties**:
```csharp
[Parameter] public EntityType Type { get; set; } // Concept, Relationship, Individual
[Parameter] public int OntologyId { get; set; }
[Parameter] public EventCallback OnEntityChanged { get; set; }
```

**Key Methods**:
```csharp
public void Show()
public void Hide()
private async Task LoadEntities()
private async Task DeleteEntity(int id)
private async Task SaveEntity(EntityDto entity)
```

### EntityListHeader.razor
**Purpose**: Search, filter, and sort controls
**Responsibilities**:
- Capture search input
- Provide filter dropdowns
- Provide sort options
- Emit filter/sort events to parent

**Key Properties**:
```csharp
[Parameter] public string SearchTerm { get; set; }
[Parameter] public EventCallback<string> OnSearchChanged { get; set; }
[Parameter] public EventCallback<string> OnSortChanged { get; set; }
[Parameter] public EventCallback<string> OnFilterChanged { get; set; }
```

### EntityList.razor
**Purpose**: Render filtered/sorted entity list
**Responsibilities**:
- Display entity items
- Handle empty states
- Manage list virtualization (future)
- Track expanded item

**Key Properties**:
```csharp
[Parameter] public IEnumerable<EntityDto> Entities { get; set; }
[Parameter] public EventCallback<int> OnEdit { get; set; }
[Parameter] public EventCallback<int> OnDelete { get; set; }
```

### EntityListItem.razor
**Purpose**: Single entity row with inline edit
**Responsibilities**:
- Display entity in collapsed state
- Toggle to edit mode
- Validate and save changes
- Show delete confirmation

**Key Properties**:
```csharp
[Parameter] public EntityDto Entity { get; set; }
[Parameter] public bool IsExpanded { get; set; }
[Parameter] public EventCallback OnSave { get; set; }
[Parameter] public EventCallback OnDelete { get; set; }
[Parameter] public EventCallback OnCancel { get; set; }
```

## Data Flow

### Opening Dialog
```
User clicks "Manage Concepts" button
→ OntologyView.ShowAdminDialog(EntityType.Concept)
→ AdminEntityDialog.Show()
→ AdminEntityDialog.LoadEntities()
→ ConceptService.GetByOntologyIdAsync()
→ State updated, UI renders
```

### Searching
```
User types in search box
→ EntityListHeader.OnSearchChanged
→ AdminEntityDialog.HandleSearchChanged(searchTerm)
→ Filter entities in-memory (LINQ)
→ EntityList re-renders with filtered items
```

### Editing Entity
```
User clicks entity row
→ EntityListItem.ToggleExpanded()
→ Show edit form inline
→ User changes fields
→ User clicks Save
→ EntityListItem.OnSave()
→ AdminEntityDialog.SaveEntity()
→ ConceptService.UpdateAsync()
→ ToastService.ShowSuccess()
→ Reload entities, collapse item
```

### Deleting Entity
```
User clicks delete icon
→ EntityListItem.OnDelete()
→ AdminEntityDialog.DeleteEntity(id)
→ ConfirmService.ShowAsync("Delete {name}?")
→ If confirmed: ConceptService.DeleteAsync()
→ ToastService.ShowSuccess()
→ Remove from list, update UI
```

## Service Layer

Uses existing services - no new services needed:

- **ConceptService** - `GetByOntologyIdAsync`, `UpdateAsync`, `DeleteAsync`
- **RelationshipService** - Same pattern
- **IndividualService** - Same pattern
- **OntologyPermissionService** - `CanManageAsync(ontologyId, userId)`
- **ToastService** - `ShowSuccess`, `ShowError`
- **ConfirmService** - `ShowAsync`

## State Management

### Local Component State
```csharp
// AdminEntityDialog.razor
private List<EntityDto> allEntities = new();
private List<EntityDto> filteredEntities = new();
private string searchTerm = "";
private string filterCategory = "all";
private string sortBy = "name";
private int? expandedEntityId = null;
private bool isLoading = false;
```

### Computed Properties
```csharp
private IEnumerable<EntityDto> FilteredAndSortedEntities =>
    allEntities
        .Where(e => MatchesSearch(e))
        .Where(e => MatchesFilter(e))
        .OrderBy(e => GetSortValue(e));
```

## Styling Approach

Follow existing patterns:

**CSS File**: `/wwwroot/css/admin-dialogs.css`

**Key Classes**:
```css
.admin-dialog-content { /* Dialog body styles */ }
.entity-list { /* List container */ }
.entity-list-item { /* Single row */ }
.entity-list-item.expanded { /* Expanded state */ }
.entity-edit-form { /* Inline edit form */ }
.entity-actions { /* Edit/delete buttons */ }
```

**Bootstrap Classes**:
- `list-group` / `list-group-item` for entity list
- `form-control` for search/filter inputs
- `btn-sm` for action buttons
- `badge` for status indicators

## Permission Handling

```csharp
protected override async Task OnInitializedAsync()
{
    // Check permission on load
    var hasAccess = await PermissionService.CanManageAsync(
        OntologyId,
        CurrentUserId
    );

    if (!hasAccess)
    {
        ToastService.ShowError("Admin access required");
        Hide();
        return;
    }

    await LoadEntities();
}
```

## Error Handling

```csharp
private async Task SaveEntity(EntityDto entity)
{
    try
    {
        isLoading = true;
        await ConceptService.UpdateAsync(entity);
        ToastService.ShowSuccess($"{entity.Name} updated");
        await LoadEntities();
        expandedEntityId = null;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to update entity {Id}", entity.Id);
        ToastService.ShowError("Failed to save changes");
    }
    finally
    {
        isLoading = false;
        StateHasChanged();
    }
}
```

## Performance Considerations

### Initial Load
- Load all entities upfront (assuming <1000 items)
- Filter/sort in-memory (fast for small datasets)
- Future: Implement server-side paging if needed

### Search Performance
- Debounce search input (300ms delay)
- Use LINQ for filtering (compiled at runtime)
- Avoid re-fetching from server on every keystroke

### Rendering Optimization
- Use `@key` directive on list items
- Only render visible items (future: virtual scrolling)
- Avoid unnecessary re-renders with `ShouldRender()`

## File Structure

New files to create:
```
/Components/Ontology/Admin/
  ├── AdminEntityDialog.razor
  ├── AdminEntityDialog.razor.cs
  ├── EntityListHeader.razor
  ├── EntityList.razor
  ├── EntityListItem.razor
  └── EntityListItem.razor.cs

/wwwroot/css/
  └── admin-dialogs.css

/Models/DTOs/
  └── EntityDto.cs (if needed)
```

Modified files:
```
/Components/Pages/OntologyView.razor
  - Add "Manage Entities" button
  - Add @ref to AdminEntityDialog
  - Add ShowAdminDialog method
```

## Integration with Existing Features

### With ConceptManagementPanel
- Admin dialog for quick edits
- Full ConceptManagementPanel for detailed editing
- Both coexist, serve different purposes

### With Floating Modal
- Admin dialog uses FloatingPanel component
- Follows same positioning/animation patterns
- Can be open alongside other panels

### With Real-time Updates (SignalR)
- Optional: Broadcast entity changes
- Other users see updates in real-time
- Use existing OntologyHub methods

---
**Next**: See [ui-design.md](./ui-design.md) for visual specifications
