# OntologyView Refactoring Plan - Detailed Component Specifications

**Last Updated**: February 2025

---

## Overview

This document details the 23 components and classes that will be extracted from the 3,184-line OntologyView.razor file. Each component is specified with its responsibilities, parameters, dependencies, and size estimate.

---

## Phase 1: State Management Classes

### 1. OntologyViewState.cs (~200 lines)

**Purpose**: Centralized state management for the entire OntologyView component.

**Responsibilities**:
- Manage ontology data (Concepts, Relationships, Individuals)
- Track current view mode (Graph, List, Hierarchy, TTL)
- Handle loading states and error messages
- Manage selected entities (concept, relationship, individual)
- Track UI state (sidebar visibility, panel states)

**Key Properties**:
```csharp
public class OntologyViewState
{
    // Ontology Data
    public int OntologyId { get; set; }
    public Ontology? Ontology { get; set; }
    public List<Concept> Concepts { get; set; } = new();
    public List<Relationship> Relationships { get; set; } = new();
    public List<Individual> Individuals { get; set; } = new();

    // View State
    public ViewMode CurrentView { get; set; } = ViewMode.Graph;
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }

    // Selection State
    public Concept? SelectedConcept { get; set; }
    public Relationship? SelectedRelationship { get; set; }
    public Individual? SelectedIndividual { get; set; }

    // UI State
    public bool IsSidebarVisible { get; set; } = true;
    public bool IsDetailsPanelVisible { get; set; }
    public bool IsFloatingPanelVisible { get; set; }

    // Permission State
    public bool CanEdit { get; set; }
    public bool CanManage { get; set; }

    // Events
    public event Action? OnStateChanged;

    // Methods
    public void SetSelectedConcept(Concept? concept);
    public void SetSelectedRelationship(Relationship? relationship);
    public void SetSelectedIndividual(Individual? individual);
    public void ClearSelection();
    public void NotifyStateChanged();
}
```

**Dependencies**: Eidos.Models namespace

**Testing**: Unit tests for state transitions and event firing

---

### 2. GraphViewState.cs (~150 lines)

**Purpose**: Specialized state management for graph visualization.

**Responsibilities**:
- Manage Cytoscape.js graph instance
- Track graph layout settings
- Handle node/edge visibility toggles
- Manage graph interaction state (dragging, zooming)

**Key Properties**:
```csharp
public class GraphViewState
{
    // Graph Instance
    public string GraphElementId { get; set; } = "ontology-graph";
    public bool IsGraphInitialized { get; set; }

    // Layout Settings
    public GraphLayout CurrentLayout { get; set; } = GraphLayout.Hierarchical;
    public bool ShowIndividuals { get; set; } = false;
    public bool ShowLabels { get; set; } = true;

    // Interaction State
    public bool IsNodeDragging { get; set; }
    public string? HoveredNodeId { get; set; }

    // Filtering
    public string? CategoryFilter { get; set; }
    public HashSet<string> HiddenConceptIds { get; set; } = new();

    // Events
    public event Action? OnGraphStateChanged;

    // Methods
    public void ToggleIndividuals();
    public void SetLayout(GraphLayout layout);
    public void FilterByCategory(string? category);
    public void NotifyStateChanged();
}

public enum GraphLayout
{
    Hierarchical,
    Force,
    Circular,
    Grid
}
```

**Dependencies**: None (pure state)

**Testing**: Unit tests for layout transitions and filter logic

---

## Phase 2: UI Components

### 3. OntologyHeader.razor (~100 lines)

**Purpose**: Display ontology title, description, and metadata.

**Markup**:
```razor
<div class="ontology-header">
    <h2>@Ontology.Name</h2>
    @if (!string.IsNullOrEmpty(Ontology.Description))
    {
        <p class="text-muted">@Ontology.Description</p>
    }
    <div class="ontology-stats">
        <span class="badge bg-primary">@Ontology.ConceptCount Concepts</span>
        <span class="badge bg-info">@Ontology.RelationshipCount Relationships</span>
        @if (ShowPresence)
        {
            <PresenceIndicator OntologyId="@Ontology.Id" />
        }
    </div>
</div>
```

**Parameters**:
- `Ontology` (required): The ontology to display
- `ShowPresence` (optional, default: true): Whether to show presence indicator

**Dependencies**: Eidos.Models, PresenceIndicator component

---

### 4. OntologyToolbar.razor (~150 lines)

**Purpose**: Toolbar with actions (add concept, add relationship, export, settings).

**Markup**:
```razor
<div class="ontology-toolbar">
    <div class="btn-group">
        @if (CanEdit)
        {
            <button class="btn btn-primary" @onclick="OnAddConceptClick">
                <i class="bi bi-plus-circle"></i> Add Concept
            </button>
            <button class="btn btn-primary" @onclick="OnAddRelationshipClick">
                <i class="bi bi-arrow-left-right"></i> Add Relationship
            </button>
            <button class="btn btn-primary" @onclick="OnAddIndividualClick">
                <i class="bi bi-person-plus"></i> Add Individual
            </button>
        }
        <button class="btn btn-secondary" @onclick="OnExportClick">
            <i class="bi bi-download"></i> Export
        </button>
        @if (CanManage)
        {
            <button class="btn btn-secondary" @onclick="OnSettingsClick">
                <i class="bi bi-gear"></i> Settings
            </button>
        }
    </div>

    <div class="search-box">
        <input type="text"
               class="form-control"
               placeholder="Search concepts..."
               @bind="SearchTerm"
               @bind:event="oninput" />
    </div>
</div>
```

**Parameters**:
- `CanEdit` (required): Whether user can edit
- `CanManage` (required): Whether user can manage settings
- `SearchTerm` (two-way binding): Current search term
- Event callbacks: `OnAddConceptClick`, `OnAddRelationshipClick`, `OnAddIndividualClick`, `OnExportClick`, `OnSettingsClick`

**Dependencies**: None (pure UI)

---

### 5. ViewTabs.razor (~80 lines)

**Purpose**: Tab navigation for switching between views.

**Markup**:
```razor
<ul class="nav nav-tabs">
    <li class="nav-item">
        <a class="nav-link @(CurrentView == ViewMode.Graph ? "active" : "")"
           @onclick="() => OnViewChanged.InvokeAsync(ViewMode.Graph)">
            <i class="bi bi-diagram-3"></i> Graph
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link @(CurrentView == ViewMode.List ? "active" : "")"
           @onclick="() => OnViewChanged.InvokeAsync(ViewMode.List)">
            <i class="bi bi-list-ul"></i> List
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link @(CurrentView == ViewMode.Hierarchy ? "active" : "")"
           @onclick="() => OnViewChanged.InvokeAsync(ViewMode.Hierarchy)">
            <i class="bi bi-diagram-2"></i> Hierarchy
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link @(CurrentView == ViewMode.Ttl ? "active" : "")"
           @onclick="() => OnViewChanged.InvokeAsync(ViewMode.Ttl)">
            <i class="bi bi-code-slash"></i> TTL
        </a>
    </li>
</ul>
```

**Parameters**:
- `CurrentView` (required): Currently selected view
- `OnViewChanged` (required): EventCallback<ViewMode> for view changes

**Dependencies**: Eidos.Models.Enums (ViewMode enum)

---

### 6. GraphViewPanel.razor (~300 lines)

**Purpose**: Interactive Cytoscape.js graph visualization panel.

**Responsibilities**:
- Initialize Cytoscape.js graph
- Render concepts as nodes and relationships as edges
- Handle node selection, dragging, zooming
- Provide graph controls (layout, show/hide individuals)
- Interact with graphVisualization.js via JS interop

**Parameters**:
- `Concepts` (required): List of concepts to visualize
- `Relationships` (required): List of relationships to visualize
- `Individuals` (optional): List of individuals to visualize
- `GraphState` (required): GraphViewState for configuration
- `OnNodeSelected` (required): EventCallback<string> when node clicked
- `OnEdgeSelected` (required): EventCallback<string> when edge clicked

**Dependencies**:
- JS Interop (graphVisualization.js)
- Eidos.Models

**JavaScript Interop**:
```javascript
// graphVisualization.js methods used:
window.initializeGraph(elementId, concepts, relationships, individuals, options)
window.updateGraph(elementId, concepts, relationships, individuals)
window.setLayout(elementId, layoutName)
window.centerGraph(elementId)
window.fitGraph(elementId)
```

---

### 7. ListViewPanel.razor (~250 lines)

**Purpose**: Tabular list view of concepts and relationships.

**Markup**:
```razor
<div class="list-view">
    <h4>Concepts (@FilteredConcepts.Count)</h4>
    <table class="table table-hover">
        <thead>
            <tr>
                <th>Name</th>
                <th>Category</th>
                <th>Definition</th>
                @if (CanEdit)
                {
                    <th>Actions</th>
                }
            </tr>
        </thead>
        <tbody>
            @foreach (var concept in FilteredConcepts)
            {
                <tr @onclick="() => OnConceptSelected.InvokeAsync(concept.Id)">
                    <td>
                        <div class="d-flex align-items-center">
                            <div class="concept-color" style="background-color: @concept.Color"></div>
                            @concept.Name
                        </div>
                    </td>
                    <td>@concept.Category</td>
                    <td>@(concept.Definition?.Truncate(100))</td>
                    @if (CanEdit)
                    {
                        <td>
                            <button class="btn btn-sm btn-outline-primary"
                                    @onclick="() => OnEditClick.InvokeAsync(concept)">
                                Edit
                            </button>
                            <button class="btn btn-sm btn-outline-danger"
                                    @onclick="() => OnDeleteClick.InvokeAsync(concept)">
                                Delete
                            </button>
                        </td>
                    }
                </tr>
            }
        </tbody>
    </table>
</div>
```

**Parameters**:
- `Concepts` (required): List of concepts
- `Relationships` (required): List of relationships
- `SearchTerm` (optional): Filter by search term
- `CanEdit` (required): Whether user can edit
- Event callbacks: `OnConceptSelected`, `OnRelationshipSelected`, `OnEditClick`, `OnDeleteClick`

**Dependencies**: Eidos.Models

---

### 8. HierarchyViewPanel.razor (~200 lines)

**Purpose**: Tree view showing parent-child concept relationships.

**Markup**:
```razor
<div class="hierarchy-view">
    @foreach (var rootConcept in RootConcepts)
    {
        <ConceptTreeNode Concept="@rootConcept"
                        AllRelationships="@Relationships"
                        AllConcepts="@Concepts"
                        OnConceptSelected="@OnConceptSelected"
                        Level="0" />
    }
</div>

@code {
    // Helper component for recursive tree rendering
}
```

**Parameters**:
- `Concepts` (required): List of all concepts
- `Relationships` (required): List of all relationships
- `OnConceptSelected` (required): EventCallback<int> when concept clicked

**Child Component**: `ConceptTreeNode.razor` (internal, ~50 lines) for recursive rendering

**Dependencies**: Eidos.Models

---

### 9. TtlViewPanel.razor (~150 lines)

**Purpose**: Display and edit raw Turtle (TTL) format.

**Markup**:
```razor
<div class="ttl-view">
    @if (CanEdit)
    {
        <textarea class="form-control ttl-editor"
                  rows="30"
                  @bind="TtlContent"
                  @bind:event="oninput"
                  spellcheck="false">
        </textarea>
        <button class="btn btn-primary mt-2" @onclick="OnSaveClick">
            <i class="bi bi-save"></i> Save TTL
        </button>
    }
    else
    {
        <pre class="ttl-readonly">@TtlContent</pre>
    }

    <button class="btn btn-secondary mt-2" @onclick="OnDownloadClick">
        <i class="bi bi-download"></i> Download TTL
    </button>
</div>
```

**Parameters**:
- `OntologyId` (required): ID of ontology to display
- `CanEdit` (required): Whether user can edit TTL
- Event callbacks: `OnSaveClick`, `OnDownloadClick`

**Dependencies**:
- OntologyService (for loading/saving TTL)
- JS Interop (for download)

---

### 10. AddConceptDialog.razor (~200 lines)

**Purpose**: Modal dialog for adding/editing concepts.

**Markup**:
```razor
<Modal IsVisible="@IsVisible" Title="@Title" Size="Modal.ModalSize.Large">
    <EditForm Model="@Model" OnValidSubmit="@HandleSubmit">
        <DataAnnotationsValidator />
        <ValidationSummary />

        <div class="mb-3">
            <label class="form-label">Name *</label>
            <InputText class="form-control" @bind-Value="Model.Name" />
        </div>

        <div class="mb-3">
            <label class="form-label">Category</label>
            <InputText class="form-control" @bind-Value="Model.Category" />
        </div>

        <div class="mb-3">
            <label class="form-label">Definition</label>
            <InputTextArea class="form-control" rows="3" @bind-Value="Model.Definition" />
        </div>

        <div class="mb-3">
            <label class="form-label">Color</label>
            <InputText type="color" class="form-control" @bind-Value="Model.Color" />
        </div>

        <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @onclick="OnCancel">Cancel</button>
            <button type="submit" class="btn btn-primary">Save</button>
            @if (EnableSaveAndAddAnother)
            {
                <button type="button" class="btn btn-success" @onclick="HandleSaveAndAddAnother">
                    Save & Add Another
                </button>
            }
        </div>
    </EditForm>
</Modal>
```

**Parameters**:
- `IsVisible` (required): Whether dialog is visible
- `Concept` (optional): Concept to edit (null for new)
- `OntologyId` (required): ID of parent ontology
- `EnableSaveAndAddAnother` (optional, default: true)
- Event callbacks: `OnSave`, `OnCancel`

**Dependencies**:
- Eidos.Models
- Eidos.Components.Shared (Modal)
- ConceptService

---

### 11. AddRelationshipDialog.razor (~180 lines)

**Purpose**: Modal dialog for adding/editing relationships.

**Markup**:
```razor
<Modal IsVisible="@IsVisible" Title="@Title">
    <EditForm Model="@Model" OnValidSubmit="@HandleSubmit">
        <DataAnnotationsValidator />
        <ValidationSummary />

        <div class="mb-3">
            <label class="form-label">Source Concept *</label>
            <select class="form-select" @bind="Model.SourceConceptId">
                <option value="">-- Select Source --</option>
                @foreach (var concept in Concepts)
                {
                    <option value="@concept.Id">@concept.Name</option>
                }
            </select>
        </div>

        <div class="mb-3">
            <label class="form-label">Relationship Type *</label>
            <select class="form-select" @bind="Model.RelationType">
                <option value="is-a">is a</option>
                <option value="part-of">part of</option>
                <option value="related-to">related to</option>
                <option value="custom">Custom</option>
            </select>
            @if (Model.RelationType == "custom")
            {
                <InputText class="form-control mt-2"
                          placeholder="Custom relationship type"
                          @bind-Value="Model.CustomRelationType" />
            }
        </div>

        <div class="mb-3">
            <label class="form-label">Target Concept *</label>
            <select class="form-select" @bind="Model.TargetConceptId">
                <option value="">-- Select Target --</option>
                @foreach (var concept in Concepts)
                {
                    <option value="@concept.Id">@concept.Name</option>
                }
            </select>
        </div>

        <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @onclick="OnCancel">Cancel</button>
            <button type="submit" class="btn btn-primary">Save</button>
        </div>
    </EditForm>
</Modal>
```

**Parameters**:
- `IsVisible` (required): Whether dialog is visible
- `Relationship` (optional): Relationship to edit (null for new)
- `Concepts` (required): List of available concepts
- `OntologyId` (required): ID of parent ontology
- Event callbacks: `OnSave`, `OnCancel`

**Dependencies**:
- Eidos.Models
- Eidos.Components.Shared (Modal)
- RelationshipService

---

### 12. AddIndividualDialog.razor (~150 lines)

**Purpose**: Modal dialog for adding/editing individuals (instances).

**Parameters**:
- `IsVisible` (required): Whether dialog is visible
- `Individual` (optional): Individual to edit (null for new)
- `Concepts` (required): List of available concepts (for type selection)
- `OntologyId` (required): ID of parent ontology
- Event callbacks: `OnSave`, `OnCancel`

**Dependencies**:
- Eidos.Models
- Eidos.Components.Shared (Modal)
- IndividualService

---

### 13. ConfirmDeleteDialog.razor (~80 lines)

**Purpose**: Reusable confirmation dialog for delete operations.

**Markup**:
```razor
<Modal IsVisible="@IsVisible" Title="Confirm Deletion" Size="Modal.ModalSize.Small">
    <p>Are you sure you want to delete <strong>@EntityName</strong>?</p>
    @if (!string.IsNullOrEmpty(WarningMessage))
    {
        <div class="alert alert-warning">
            <i class="bi bi-exclamation-triangle"></i> @WarningMessage
        </div>
    }
    <div class="modal-footer">
        <button type="button" class="btn btn-secondary" @onclick="OnCancel">Cancel</button>
        <button type="button" class="btn btn-danger" @onclick="OnConfirm">Delete</button>
    </div>
</Modal>
```

**Parameters**:
- `IsVisible` (required): Whether dialog is visible
- `EntityName` (required): Name of entity being deleted
- `WarningMessage` (optional): Additional warning text
- Event callbacks: `OnConfirm`, `OnCancel`

**Dependencies**: Eidos.Components.Shared (Modal)

---

## Phase 3: Code-Behind Partial Classes

### 14. OntologyView.Lifecycle.cs (~150 lines)

**Purpose**: Component lifecycle methods (initialization, disposal).

**Methods**:
```csharp
public partial class OntologyView
{
    protected override async Task OnInitializedAsync()
    {
        // Load ontology data
        // Check permissions
        // Initialize state
        // Subscribe to SignalR hub
    }

    protected override async Task OnParametersSetAsync()
    {
        // Handle parameter changes (e.g., OntologyId changed)
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize JavaScript interop
            // Initialize graph
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Unsubscribe from SignalR
        // Clean up JS resources
        // Dispose services
    }
}
```

**Dependencies**: All services, JS interop

---

### 15. OntologyView.Permissions.cs (~100 lines)

**Purpose**: Permission checking logic.

**Methods**:
```csharp
public partial class OntologyView
{
    private async Task LoadPermissionsAsync()
    {
        State.CanEdit = await PermissionService.CanEditAsync(OntologyId, CurrentUserId);
        State.CanManage = await PermissionService.CanManageAsync(OntologyId, CurrentUserId);
        State.NotifyStateChanged();
    }

    private async Task<bool> EnsureCanEditAsync()
    {
        if (!State.CanEdit)
        {
            await ToastService.ShowError("You don't have permission to edit this ontology.");
            return false;
        }
        return true;
    }

    private async Task<bool> EnsureCanManageAsync()
    {
        if (!State.CanManage)
        {
            await ToastService.ShowError("You don't have permission to manage this ontology.");
            return false;
        }
        return true;
    }
}
```

**Dependencies**: OntologyPermissionService, ToastService

---

### 16. OntologyView.SignalR.cs (~200 lines)

**Purpose**: SignalR hub connection and real-time collaboration.

**Methods**:
```csharp
public partial class OntologyView
{
    private HubConnection? _hubConnection;

    private async Task InitializeSignalRAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/hubs/ontology"))
            .WithAutomaticReconnect()
            .Build();

        // Subscribe to hub events
        _hubConnection.On<int>("ConceptAdded", OnConceptAddedAsync);
        _hubConnection.On<int>("ConceptUpdated", OnConceptUpdatedAsync);
        _hubConnection.On<int>("ConceptDeleted", OnConceptDeletedAsync);
        _hubConnection.On<int>("RelationshipAdded", OnRelationshipAddedAsync);
        // ... more event handlers

        await _hubConnection.StartAsync();
        await _hubConnection.SendAsync("JoinOntology", OntologyId);
    }

    private async Task OnConceptAddedAsync(int conceptId)
    {
        // Reload concept from service
        // Add to state
        // Update graph
        StateHasChanged();
    }

    // ... more event handlers
}
```

**Dependencies**: SignalR, OntologyHub, OntologyService

---

### 17. OntologyView.GraphOperations.cs (~250 lines)

**Purpose**: Graph-specific operations (layout, filtering, interaction).

**Methods**:
```csharp
public partial class OntologyView
{
    private async Task InitializeGraphAsync()
    {
        await JSRuntime.InvokeVoidAsync(
            "initializeGraph",
            GraphState.GraphElementId,
            State.Concepts,
            State.Relationships,
            State.Individuals,
            new { layout = GraphState.CurrentLayout.ToString().ToLower() }
        );
        GraphState.IsGraphInitialized = true;
    }

    private async Task UpdateGraphAsync()
    {
        if (!GraphState.IsGraphInitialized) return;

        await JSRuntime.InvokeVoidAsync(
            "updateGraph",
            GraphState.GraphElementId,
            State.Concepts,
            State.Relationships,
            GraphState.ShowIndividuals ? State.Individuals : new List<Individual>()
        );
    }

    private async Task OnNodeClickedAsync(string nodeId)
    {
        // Parse nodeId (format: "concept-123" or "individual-456")
        // Load entity details
        // Update selected entity in state
        // Show details panel
    }

    private async Task OnEdgeClickedAsync(string edgeId)
    {
        // Parse edgeId (format: "rel-123")
        // Load relationship details
        // Update selected relationship in state
    }

    private async Task ChangeLayoutAsync(GraphLayout layout)
    {
        GraphState.SetLayout(layout);
        await JSRuntime.InvokeVoidAsync("setLayout", GraphState.GraphElementId, layout.ToString().ToLower());
    }

    private async Task ToggleIndividualsAsync()
    {
        GraphState.ToggleIndividuals();
        await UpdateGraphAsync();
    }
}
```

**Dependencies**: JS interop, OntologyViewState, GraphViewState

---

### 18. OntologyView.Dialogs.cs (~150 lines)

**Purpose**: Dialog state management and event handlers.

**Methods**:
```csharp
public partial class OntologyView
{
    // Dialog visibility state
    private bool _showAddConceptDialog;
    private bool _showAddRelationshipDialog;
    private bool _showAddIndividualDialog;
    private bool _showDeleteDialog;

    // Current entity being edited
    private Concept? _editingConcept;
    private Relationship? _editingRelationship;
    private Individual? _editingIndividual;

    // Dialog open methods
    private void OpenAddConceptDialog()
    {
        _editingConcept = null;
        _showAddConceptDialog = true;
    }

    private void OpenEditConceptDialog(Concept concept)
    {
        _editingConcept = concept;
        _showAddConceptDialog = true;
    }

    // Dialog save handlers
    private async Task HandleConceptSavedAsync(Concept concept)
    {
        if (!await EnsureCanEditAsync()) return;

        if (concept.Id == 0)
        {
            // Add new concept
            await ConceptService.AddConceptAsync(concept);
            await ToastService.ShowSuccess($"Concept '{concept.Name}' added.");
        }
        else
        {
            // Update existing concept
            await ConceptService.UpdateConceptAsync(concept);
            await ToastService.ShowSuccess($"Concept '{concept.Name}' updated.");
        }

        _showAddConceptDialog = false;
        await ReloadDataAsync();
    }

    // Dialog cancel handlers
    private void HandleConceptDialogCanceled()
    {
        _showAddConceptDialog = false;
        _editingConcept = null;
    }

    // ... similar methods for relationships and individuals
}
```

**Dependencies**: Services, ToastService

---

### 19. OntologyView.Export.cs (~150 lines)

**Purpose**: Export functionality (TTL, PDF, image).

**Methods**:
```csharp
public partial class OntologyView
{
    private async Task ExportAsTtlAsync()
    {
        var ttl = await OntologyService.ExportToTtlAsync(OntologyId);
        var fileName = $"{State.Ontology?.Name ?? "ontology"}.ttl";

        await JSRuntime.InvokeVoidAsync(
            "downloadFile",
            fileName,
            "text/turtle",
            ttl
        );

        await ToastService.ShowSuccess("Exported as TTL");
    }

    private async Task ExportGraphAsImageAsync()
    {
        var imageData = await JSRuntime.InvokeAsync<string>(
            "exportGraphAsImage",
            GraphState.GraphElementId
        );

        var fileName = $"{State.Ontology?.Name ?? "ontology"}-graph.png";

        await JSRuntime.InvokeVoidAsync(
            "downloadFile",
            fileName,
            "image/png",
            imageData
        );

        await ToastService.ShowSuccess("Graph exported as image");
    }

    private async Task ExportAsPdfAsync()
    {
        // Generate PDF report with ontology details
        // Use a PDF library or server-side generation
        await ToastService.ShowInfo("PDF export coming soon");
    }
}
```

**Dependencies**: JS interop, OntologyService, ToastService

---

### 20. OntologyView.Search.cs (~100 lines)

**Purpose**: Search and filtering logic.

**Methods**:
```csharp
public partial class OntologyView
{
    private string _searchTerm = string.Empty;

    private string SearchTerm
    {
        get => _searchTerm;
        set
        {
            _searchTerm = value;
            ApplySearchFilter();
        }
    }

    private List<Concept> FilteredConcepts =>
        string.IsNullOrWhiteSpace(_searchTerm)
            ? State.Concepts
            : State.Concepts.Where(c =>
                c.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (c.Definition?.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Category?.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();

    private List<Relationship> FilteredRelationships =>
        string.IsNullOrWhiteSpace(_searchTerm)
            ? State.Relationships
            : State.Relationships.Where(r =>
                r.RelationType.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
                r.SourceConcept?.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                r.TargetConcept?.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) == true
            ).ToList();

    private async Task ApplySearchFilter()
    {
        if (State.CurrentView == ViewMode.Graph)
        {
            // Update graph with filtered data
            await UpdateGraphAsync();
        }
        StateHasChanged();
    }
}
```

**Dependencies**: OntologyViewState

---

### 21. OntologyView.Keyboard.cs (~100 lines)

**Purpose**: Keyboard shortcut handling.

**Methods**:
```csharp
public partial class OntologyView
{
    private DotNetObjectReference<OntologyView>? _objRef;

    private async Task RegisterKeyboardShortcutsAsync()
    {
        _objRef = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("registerKeyboardShortcuts", _objRef);
    }

    [JSInvokable]
    public async Task HandleKeyboardShortcut(string shortcut)
    {
        switch (shortcut)
        {
            case "ctrl+k": // Quick search
                // Focus search box
                await JSRuntime.InvokeVoidAsync("focusElement", "search-box");
                break;

            case "ctrl+n": // New concept
                if (State.CanEdit)
                    OpenAddConceptDialog();
                break;

            case "ctrl+r": // New relationship
                if (State.CanEdit)
                    OpenAddRelationshipDialog();
                break;

            case "ctrl+1": // Switch to graph view
                State.CurrentView = ViewMode.Graph;
                StateHasChanged();
                break;

            case "ctrl+2": // Switch to list view
                State.CurrentView = ViewMode.List;
                StateHasChanged();
                break;

            // ... more shortcuts
        }
    }
}
```

**Dependencies**: JS interop (keyboardShortcuts.js)

---

## Phase 4: Refactored Main Component

### 22. OntologyView.razor (~400 lines)

**Purpose**: Main orchestration component that composes all extracted components.

**Structure**:
```razor
@page "/ontology/{OntologyId:int}"
@using Eidos.Components.Ontology
@using Eidos.Services
@inject OntologyService OntologyService
@inject ConceptService ConceptService
@inject RelationshipService RelationshipService
@inject OntologyPermissionService PermissionService
@inject ToastService ToastService
@inject IJSRuntime JSRuntime
@inject NavigationManager NavigationManager
@implements IAsyncDisposable

@* Loading State *@
@if (State.IsLoading)
{
    <div class="text-center my-5">
        <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
    </div>
}
else if (!string.IsNullOrEmpty(State.ErrorMessage))
{
    <div class="alert alert-danger">@State.ErrorMessage</div>
}
else if (State.Ontology != null)
{
    <div class="ontology-view-container">
        @* Header *@
        <OntologyHeader Ontology="@State.Ontology" ShowPresence="true" />

        @* Toolbar *@
        <OntologyToolbar CanEdit="@State.CanEdit"
                        CanManage="@State.CanManage"
                        @bind-SearchTerm="@SearchTerm"
                        OnAddConceptClick="@OpenAddConceptDialog"
                        OnAddRelationshipClick="@OpenAddRelationshipDialog"
                        OnAddIndividualClick="@OpenAddIndividualDialog"
                        OnExportClick="@ExportAsTtlAsync"
                        OnSettingsClick="@OpenSettingsDialog" />

        @* View Tabs *@
        <ViewTabs CurrentView="@State.CurrentView"
                 OnViewChanged="@HandleViewChanged" />

        @* View Content *@
        <div class="view-content">
            @switch (State.CurrentView)
            {
                case ViewMode.Graph:
                    <GraphViewPanel Concepts="@FilteredConcepts"
                                  Relationships="@FilteredRelationships"
                                  Individuals="@State.Individuals"
                                  GraphState="@GraphState"
                                  OnNodeSelected="@OnNodeClickedAsync"
                                  OnEdgeSelected="@OnEdgeClickedAsync" />
                    break;

                case ViewMode.List:
                    <ListViewPanel Concepts="@FilteredConcepts"
                                 Relationships="@FilteredRelationships"
                                 SearchTerm="@SearchTerm"
                                 CanEdit="@State.CanEdit"
                                 OnConceptSelected="@HandleConceptSelected"
                                 OnRelationshipSelected="@HandleRelationshipSelected"
                                 OnEditClick="@OpenEditConceptDialog"
                                 OnDeleteClick="@HandleDeleteClick" />
                    break;

                case ViewMode.Hierarchy:
                    <HierarchyViewPanel Concepts="@State.Concepts"
                                      Relationships="@State.Relationships"
                                      OnConceptSelected="@HandleConceptSelected" />
                    break;

                case ViewMode.Ttl:
                    <TtlViewPanel OntologyId="@OntologyId"
                                CanEdit="@State.CanEdit"
                                OnSaveClick="@HandleTtlSave"
                                OnDownloadClick="@ExportAsTtlAsync" />
                    break;
            }
        </div>

        @* Details Panel (Sidebar) *@
        @if (State.IsDetailsPanelVisible)
        {
            <SelectedNodeDetailsPanel SelectedConcept="@State.SelectedConcept"
                                     SelectedRelationship="@State.SelectedRelationship"
                                     SelectedIndividual="@State.SelectedIndividual"
                                     ConnectedConcepts="@GetConnectedConcepts()" />
        }

        @* Floating Panel (for mobile/alternate view) *@
        <ConceptDetailsFloatingPanel IsVisible="@State.IsFloatingPanelVisible"
                                    IsVisibleChanged="@((bool visible) => State.IsFloatingPanelVisible = visible)"
                                    Concept="@State.SelectedConcept"
                                    ConnectedConcepts="@GetConnectedConcepts()"
                                    Restrictions="@GetConceptRestrictions()"
                                    OnAddRestrictionClick="@HandleAddRestriction"
                                    OnEditRestrictionClick="@HandleEditRestriction"
                                    OnDeleteRestrictionClick="@HandleDeleteRestriction"
                                    RestrictionsReadOnly="@(!State.CanEdit)" />
    </div>

    @* Dialogs *@
    <AddConceptDialog IsVisible="@_showAddConceptDialog"
                     Concept="@_editingConcept"
                     OntologyId="@OntologyId"
                     OnSave="@HandleConceptSavedAsync"
                     OnCancel="@HandleConceptDialogCanceled" />

    <AddRelationshipDialog IsVisible="@_showAddRelationshipDialog"
                          Relationship="@_editingRelationship"
                          Concepts="@State.Concepts"
                          OntologyId="@OntologyId"
                          OnSave="@HandleRelationshipSavedAsync"
                          OnCancel="@HandleRelationshipDialogCanceled" />

    <AddIndividualDialog IsVisible="@_showAddIndividualDialog"
                        Individual="@_editingIndividual"
                        Concepts="@State.Concepts"
                        OntologyId="@OntologyId"
                        OnSave="@HandleIndividualSavedAsync"
                        OnCancel="@HandleIndividualDialogCanceled" />

    <ConfirmDeleteDialog IsVisible="@_showDeleteDialog"
                        EntityName="@_deletingEntityName"
                        WarningMessage="@_deleteWarningMessage"
                        OnConfirm="@HandleDeleteConfirmed"
                        OnCancel="@HandleDeleteCanceled" />
}

@code {
    [Parameter] public int OntologyId { get; set; }

    private OntologyViewState State { get; set; } = new();
    private GraphViewState GraphState { get; set; } = new();

    // Partial class implementations:
    // - OntologyView.Lifecycle.cs
    // - OntologyView.Permissions.cs
    // - OntologyView.SignalR.cs
    // - OntologyView.GraphOperations.cs
    // - OntologyView.Dialogs.cs
    // - OntologyView.Export.cs
    // - OntologyView.Search.cs
    // - OntologyView.Keyboard.cs
}
```

**Responsibilities**:
- Component lifecycle (delegated to Lifecycle partial class)
- State initialization
- Compose UI from extracted components
- Route events to appropriate handlers
- Coordinate between state and child components

**Dependencies**: All extracted components and services

---

## Summary Statistics

| Component Type | Count | Avg Lines | Total Lines |
|---------------|-------|-----------|-------------|
| State Classes | 2 | 175 | 350 |
| UI Components | 11 | 165 | 1,815 |
| Dialogs | 4 | 152 | 610 |
| Partial Classes | 8 | 131 | 1,050 |
| Main Component | 1 | 400 | 400 |
| **TOTAL** | **26** | **165** | **4,225** |

**Size Comparison**:
- **Before**: 3,184 lines (1 monolithic file)
- **After**: 4,225 lines (26 focused files, avg 162 lines each)
- **Main Component**: 400 lines (87% reduction)

**Note**: Total line count increases due to component boilerplate, but maintainability, testability, and readability dramatically improve.

---

**Next Steps**: See `implementation-timeline.md` for week-by-week execution plan.
