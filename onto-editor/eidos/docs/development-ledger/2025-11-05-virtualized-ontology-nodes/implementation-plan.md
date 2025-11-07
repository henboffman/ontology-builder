# Implementation Plan: Virtualized Ontology Nodes

**Created**: November 5, 2025
**Last Updated**: November 5, 2025

---

## Development Approach

This feature will be built iteratively using the proven patterns from the OntologyView refactor:
- **Incremental development** with working software at each phase
- **Test-driven** with unit tests before implementation
- **Documentation-first** for complex decisions
- **Maintain 100% test pass** rate throughout

---

## Phase 1: Database Schema & Models (Days 1-2)

### Goals
- Extend `OntologyLink` to support internal references
- Create migration with proper constraints
- Ensure backwards compatibility

### Tasks

#### 1.1 Update OntologyLink Model
**File**: `Models/OntologyModels.cs`

```csharp
public class OntologyLink
{
    public int Id { get; set; }

    // Parent ontology that contains this link
    public int OntologyId { get; set; }
    public Ontology Ontology { get; set; } = null!;

    // Type of link (new)
    [Required]
    public LinkType LinkType { get; set; } = LinkType.External;

    // For external ontologies (existing)
    [StringLength(500)]
    public string? Uri { get; set; }

    [StringLength(50)]
    public string? Prefix { get; set; }

    // For internal ontology references (new)
    public int? LinkedOntologyId { get; set; }
    public Ontology? LinkedOntology { get; set; }

    // Display properties
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Graph position (new)
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }

    [StringLength(20)]
    public string? Color { get; set; }

    // Metadata
    public bool ConceptsImported { get; set; }
    public int ImportedConceptCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Sync tracking (new)
    public DateTime? LastSyncedAt { get; set; }
    public bool UpdateAvailable { get; set; }
}

public enum LinkType
{
    External = 0,  // URI-based external ontology
    Internal = 1   // Internal ontology reference (virtualized)
}
```

#### 1.2 Update DbContext Configuration
**File**: `Data/OntologyDbContext.cs`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configuration ...

    // OntologyLink configuration
    modelBuilder.Entity<OntologyLink>(entity =>
    {
        entity.HasKey(e => e.Id);

        // Parent ontology relationship
        entity.HasOne(ol => ol.Ontology)
            .WithMany(o => o.LinkedOntologies)
            .HasForeignKey(ol => ol.OntologyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Linked ontology relationship (new)
        entity.HasOne(ol => ol.LinkedOntology)
            .WithMany()  // Don't create inverse navigation
            .HasForeignKey(ol => ol.LinkedOntologyId)
            .OnDelete(DeleteBehavior.SetNull);  // If linked ontology deleted, set to null

        // Validation: must have either Uri or LinkedOntologyId
        entity.HasCheckConstraint(
            "CK_OntologyLink_HasTarget",
            "(LinkType = 0 AND Uri IS NOT NULL) OR (LinkType = 1 AND LinkedOntologyId IS NOT NULL)"
        );

        // Prevent linking ontology to itself
        entity.HasCheckConstraint(
            "CK_OntologyLink_NoSelfReference",
            "OntologyId <> LinkedOntologyId OR LinkedOntologyId IS NULL"
        );

        // Indexes for performance
        entity.HasIndex(ol => ol.OntologyId);
        entity.HasIndex(ol => ol.LinkedOntologyId);
        entity.HasIndex(ol => new { ol.OntologyId, ol.LinkType });
    });
}
```

#### 1.3 Create Migration
**Command**: `dotnet ef migrations add AddVirtualizedOntologyLinks`

**Migration File**: `Migrations/YYYYMMDD_AddVirtualizedOntologyLinks.cs`

Key operations:
1. Add `LinkType` column (default 0 = External)
2. Add `LinkedOntologyId` nullable FK
3. Add position columns (PositionX, PositionY, Color)
4. Add sync tracking columns (LastSyncedAt, UpdateAvailable)
5. Create check constraints
6. Create indexes
7. Update existing records to have LinkType = External

#### 1.4 Testing

**Test File**: `Tests/Models/OntologyLinkTests.cs`

Test cases:
- ✅ Can create external link with URI
- ✅ Can create internal link with LinkedOntologyId
- ✅ Cannot create link without target
- ✅ Cannot create self-referencing link
- ✅ Cascade delete removes links when parent deleted
- ✅ Set null when linked ontology deleted

---

## Phase 2: Service Layer (Days 3-5)

### Goals
- Implement link management service
- Add permission checking
- Prevent circular dependencies
- Handle synchronization logic

### Tasks

#### 2.1 Create IOntologyLinkService Interface
**File**: `Services/Interfaces/IOntologyLinkService.cs`

```csharp
public interface IOntologyLinkService
{
    // Link creation and management
    Task<OntologyLink> CreateInternalLinkAsync(
        int parentOntologyId,
        int linkedOntologyId,
        string userId,
        double? positionX = null,
        double? positionY = null);

    Task<OntologyLink> CreateExternalLinkAsync(
        int ontologyId,
        string uri,
        string name,
        string userId);

    Task UpdateLinkPositionAsync(int linkId, double x, double y, string userId);

    Task UpdateLinkMetadataAsync(int linkId, string name, string? description, string userId);

    Task RemoveLinkAsync(int linkId, string userId);

    // Querying
    Task<List<OntologyLink>> GetLinksForOntologyAsync(int ontologyId, string userId);

    Task<OntologyLink?> GetLinkByIdAsync(int linkId, string userId);

    Task<Ontology?> GetLinkedOntologyAsync(int linkId, string userId);

    Task<List<Concept>> GetLinkedConceptsAsync(int linkId, string userId);

    // Validation
    Task<bool> CanLinkOntologyAsync(int parentOntologyId, int targetOntologyId, string userId);

    Task<bool> HasCircularDependencyAsync(int parentOntologyId, int targetOntologyId);

    Task<List<int>> GetDependencyChainAsync(int ontologyId);

    // Synchronization
    Task CheckForUpdatesAsync(int linkId);

    Task SyncLinkAsync(int linkId, string userId);

    Task<bool> IsLinkStaleAsync(int linkId);
}
```

#### 2.2 Implement OntologyLinkService
**File**: `Services/OntologyLinkService.cs`

Key methods:

**CreateInternalLinkAsync**:
```csharp
public async Task<OntologyLink> CreateInternalLinkAsync(
    int parentOntologyId,
    int linkedOntologyId,
    string userId,
    double? positionX = null,
    double? positionY = null)
{
    // 1. Validate parent ontology exists and user can edit
    var canEdit = await _permissionService.CanEditAsync(parentOntologyId, userId);
    if (!canEdit)
        throw new UnauthorizedAccessException("You cannot link ontologies to this ontology");

    // 2. Validate target ontology exists and user can view
    var canView = await _permissionService.CanViewAsync(linkedOntologyId, userId);
    if (!canView)
        throw new UnauthorizedAccessException("You cannot view the target ontology");

    // 3. Check for circular dependency
    var hasCircular = await HasCircularDependencyAsync(parentOntologyId, linkedOntologyId);
    if (hasCircular)
        throw new InvalidOperationException("Linking would create a circular dependency");

    // 4. Get linked ontology metadata
    var linkedOntology = await _ontologyRepository.GetByIdAsync(linkedOntologyId);
    if (linkedOntology == null)
        throw new NotFoundException("Target ontology not found");

    // 5. Create link
    var link = new OntologyLink
    {
        OntologyId = parentOntologyId,
        LinkedOntologyId = linkedOntologyId,
        LinkType = LinkType.Internal,
        Name = linkedOntology.Name,
        Description = linkedOntology.Description,
        PositionX = positionX ?? 0,
        PositionY = positionY ?? 0,
        ImportedConceptCount = linkedOntology.ConceptCount,
        LastSyncedAt = DateTime.UtcNow,
        UpdateAvailable = false
    };

    var created = await _ontologyLinkRepository.AddAsync(link);

    // 6. Log activity
    await _activityService.LogActivityAsync(
        parentOntologyId,
        userId,
        "linked_ontology",
        $"Linked ontology: {linkedOntology.Name}");

    return created;
}
```

**HasCircularDependencyAsync**:
```csharp
public async Task<bool> HasCircularDependencyAsync(int parentOntologyId, int targetOntologyId)
{
    // If parent == target, circular
    if (parentOntologyId == targetOntologyId)
        return true;

    // Get all ontologies that target depends on
    var targetDependencies = await GetDependencyChainAsync(targetOntologyId);

    // If target depends on parent, linking would create cycle
    return targetDependencies.Contains(parentOntologyId);
}

private async Task<List<int>> GetDependencyChainAsync(int ontologyId, HashSet<int>? visited = null)
{
    visited ??= new HashSet<int>();

    if (visited.Contains(ontologyId))
        return new List<int>(); // Already visited, avoid infinite loop

    visited.Add(ontologyId);

    var links = await _ontologyLinkRepository.GetByOntologyIdAsync(ontologyId);
    var internalLinks = links.Where(l => l.LinkType == LinkType.Internal && l.LinkedOntologyId.HasValue);

    var dependencies = new List<int>();

    foreach (var link in internalLinks)
    {
        dependencies.Add(link.LinkedOntologyId!.Value);

        // Recursive: get dependencies of dependencies
        var childDeps = await GetDependencyChainAsync(link.LinkedOntologyId.Value, visited);
        dependencies.AddRange(childDeps);
    }

    return dependencies.Distinct().ToList();
}
```

**CheckForUpdatesAsync**:
```csharp
public async Task CheckForUpdatesAsync(int linkId)
{
    var link = await _ontologyLinkRepository.GetByIdAsync(linkId);
    if (link == null || link.LinkType != LinkType.Internal || !link.LinkedOntologyId.HasValue)
        return;

    var linkedOntology = await _ontologyRepository.GetByIdAsync(link.LinkedOntologyId.Value);
    if (linkedOntology == null)
        return;

    // Check if linked ontology updated since last sync
    if (linkedOntology.UpdatedAt > link.LastSyncedAt)
    {
        link.UpdateAvailable = true;
        await _ontologyLinkRepository.UpdateAsync(link);
    }
}
```

#### 2.3 Create Repository
**File**: `Data/Repositories/OntologyLinkRepository.cs`

```csharp
public class OntologyLinkRepository : IOntologyLinkRepository
{
    private readonly OntologyDbContext _context;

    public async Task<List<OntologyLink>> GetByOntologyIdAsync(int ontologyId)
    {
        return await _context.OntologyLinks
            .Include(ol => ol.LinkedOntology)
            .Where(ol => ol.OntologyId == ontologyId)
            .OrderBy(ol => ol.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<OntologyLink?> GetByIdAsync(int id)
    {
        return await _context.OntologyLinks
            .Include(ol => ol.Ontology)
            .Include(ol => ol.LinkedOntology)
            .FirstOrDefaultAsync(ol => ol.Id == id);
    }

    // ... other CRUD methods
}
```

#### 2.4 Register Services
**File**: `Program.cs`

```csharp
builder.Services.AddScoped<IOntologyLinkService, OntologyLinkService>();
builder.Services.AddScoped<IOntologyLinkRepository, OntologyLinkRepository>();
```

#### 2.5 Testing

**Test File**: `Tests/Services/OntologyLinkServiceTests.cs`

Test cases:
- ✅ Can create link with valid permissions
- ✅ Cannot link without view permission on target
- ✅ Cannot link without edit permission on parent
- ✅ Detects circular dependency (A → B → A)
- ✅ Detects transitive circular dependency (A → B → C → A)
- ✅ Can remove link successfully
- ✅ Sync updates concept count
- ✅ Check for updates marks stale links

---

## Phase 3: Graph Visualization (Days 6-8)

### Goals
- Render virtualized ontology nodes in Cytoscape
- Implement expand/collapse behavior
- Add visual distinction and styling
- Handle user interactions

### Tasks

#### 3.1 Extend Graph Data Model
**File**: `wwwroot/js/graphVisualization.js`

Add node type for ontology links:

```javascript
function buildGraphData(ontology, links) {
    const nodes = [];
    const edges = [];

    // Add concept nodes
    ontology.concepts.forEach(concept => {
        nodes.push({
            data: {
                id: `concept_${concept.id}`,
                label: concept.name,
                type: 'concept',
                category: concept.category,
                color: concept.color
            },
            position: { x: concept.positionX, y: concept.positionY },
            classes: 'concept-node'
        });
    });

    // Add ontology link nodes (NEW)
    links.forEach(link => {
        if (link.linkType === 1) { // Internal
            nodes.push({
                data: {
                    id: `link_${link.id}`,
                    label: link.name,
                    type: 'ontology-link',
                    linkId: link.id,
                    linkedOntologyId: link.linkedOntologyId,
                    conceptCount: link.importedConceptCount,
                    updateAvailable: link.updateAvailable,
                    color: link.color || '#9333ea'
                },
                position: { x: link.positionX, y: link.positionY },
                classes: 'ontology-link-node'
            });
        }
    });

    // ... edges ...

    return { nodes, edges };
}
```

#### 3.2 Add Styling
**File**: `wwwroot/css/graph-styles.css`

```css
/* Virtualized ontology node styles */
.ontology-link-node {
    background-color: #9333ea; /* Purple */
    background-image: linear-gradient(135deg, #9333ea 0%, #7c3aed 100%);
    shape: round-rectangle;
    width: 140px;
    height: 90px;
    border-width: 3px;
    border-style: dashed;
    border-color: #6b21a8;
    text-valign: center;
    text-halign: center;
    font-size: 14px;
    font-weight: bold;
    color: #ffffff;
    text-outline-color: #4c1d95;
    text-outline-width: 2px;
}

.ontology-link-node:selected {
    border-color: #fbbf24;
    border-width: 4px;
}

.ontology-link-node.update-available {
    border-color: #f59e0b;
    animation: pulse 2s infinite;
}

@keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.7; }
}

/* Expanded state - show contained concepts */
.ontology-link-node.expanded {
    background-color: rgba(147, 51, 234, 0.2);
}

/* Virtualized concept nodes (shown when expanded) */
.virtualized-concept-node {
    opacity: 0.8;
    border-style: dotted;
}

.virtualized-concept-node::after {
    content: '🔗';
    position: absolute;
    top: -10px;
    right: -10px;
    font-size: 16px;
}
```

#### 3.3 Implement Expand/Collapse
**File**: `wwwroot/js/graphVisualization.js`

```javascript
let expandedLinks = new Set();

function handleLinkNodeDoubleClick(nodeId, linkId, linkedOntologyId) {
    if (expandedLinks.has(linkId)) {
        collapseLinkNode(linkId);
    } else {
        expandLinkNode(linkId, linkedOntologyId);
    }
}

async function expandLinkNode(linkId, linkedOntologyId) {
    // Call server to get linked concepts
    const response = await fetch(`/api/ontology-links/${linkId}/concepts`);
    const concepts = await response.json();

    // Add virtualized concept nodes as children
    const newNodes = concepts.map(concept => ({
        data: {
            id: `virtual_${linkId}_${concept.id}`,
            label: concept.name,
            type: 'virtualized-concept',
            parent: `link_${linkId}`,  // Parent is the link node
            originalConceptId: concept.id,
            linkId: linkId,
            color: concept.color
        },
        classes: 'virtualized-concept-node'
    }));

    cy.add(newNodes);

    // Run layout on new nodes
    cy.layout({ name: 'cose', animate: true }).run();

    // Mark as expanded
    expandedLinks.add(linkId);
    cy.$id(`link_${linkId}`).addClass('expanded');
}

function collapseLinkNode(linkId) {
    // Remove all virtualized concepts for this link
    cy.nodes(`[linkId = "${linkId}"]`).remove();

    // Mark as collapsed
    expandedLinks.delete(linkId);
    cy.$id(`link_${linkId}`).removeClass('expanded');
}
```

#### 3.4 Context Menu for Link Nodes
**File**: `wwwroot/js/graphVisualization.js`

```javascript
function showLinkNodeContextMenu(linkId, position) {
    const menu = [
        {
            label: 'View Source Ontology',
            action: () => window.open(`/ontology/${linkedOntologyId}`, '_blank')
        },
        {
            label: 'Refresh',
            action: () => DotNet.invokeMethodAsync('Eidos', 'SyncOntologyLink', linkId)
        },
        {
            label: 'Unlink',
            action: () => DotNet.invokeMethodAsync('Eidos', 'RemoveOntologyLink', linkId),
            className: 'danger'
        }
    ];

    displayContextMenu(position, menu);
}
```

#### 3.5 Update Graph Component
**File**: `Components/Pages/OntologyView.GraphOperations.cs`

```csharp
private async Task LoadGraphDataAsync()
{
    // Load ontology with concepts and relationships
    var ontology = await _ontologyService.GetByIdAsync(OntologyId);

    // Load ontology links (NEW)
    var links = await _ontologyLinkService.GetLinksForOntologyAsync(OntologyId, UserId);

    // Pass to JavaScript
    await _jsRuntime.InvokeVoidAsync("initializeGraph", ontology, links);
}

[JSInvokable]
public async Task SyncOntologyLink(int linkId)
{
    await _ontologyLinkService.SyncLinkAsync(linkId, UserId);
    await LoadGraphDataAsync();
    StateHasChanged();
}

[JSInvokable]
public async Task RemoveOntologyLink(int linkId)
{
    await _ontologyLinkService.RemoveLinkAsync(linkId, UserId);
    await LoadGraphDataAsync();
    StateHasChanged();
}
```

---

## Phase 4: UI Components (Days 9-12)

### Goals
- Create link ontology dialog
- Add toolbar button
- Implement concept details with read-only mode
- Handle drag-and-drop positioning

### Tasks

#### 4.1 Create LinkOntologyDialog Component
**File**: `Components/Ontology/LinkOntologyDialog.razor`

```razor
@inject IOntologyService OntologyService
@inject IOntologyPermissionService PermissionService
@inject IToastService ToastService

<Modal @ref="_modal" Title="Link Ontology">
    <Body>
        @if (_loading)
        {
            <div class="spinner-border" role="status"></div>
        }
        else
        {
            <div class="mb-3">
                <input type="text"
                       class="form-control"
                       placeholder="Search ontologies..."
                       @bind="_searchQuery"
                       @bind:event="oninput"
                       @onkeyup="OnSearchChanged" />
            </div>

            <div class="ontology-list" style="max-height: 400px; overflow-y: auto;">
                @foreach (var ontology in _filteredOntologies)
                {
                    <div class="card mb-2 ontology-card @(IsSelected(ontology) ? "selected" : "")"
                         @onclick="() => SelectOntology(ontology)">
                        <div class="card-body">
                            <h6 class="card-title">@ontology.Name</h6>
                            <p class="card-text text-muted small">@ontology.Description</p>
                            <div class="d-flex justify-content-between align-items-center">
                                <span class="badge bg-secondary">
                                    @ontology.ConceptCount concepts
                                </span>
                                @if (CanLink(ontology))
                                {
                                    <span class="badge bg-success">
                                        <i class="bi bi-check-circle"></i> Can view
                                    </span>
                                }
                                else
                                {
                                    <span class="badge bg-danger">
                                        <i class="bi bi-lock"></i> No access
                                    </span>
                                }
                            </div>
                        </div>
                    </div>
                }
            </div>
        }
    </Body>
    <Footer>
        <button class="btn btn-secondary" @onclick="Cancel">Cancel</button>
        <button class="btn btn-primary"
                @onclick="ConfirmLink"
                disabled="@(_selectedOntology == null || !CanLink(_selectedOntology))">
            Link Ontology
        </button>
    </Footer>
</Modal>

@code {
    [Parameter] public int ParentOntologyId { get; set; }
    [Parameter] public string UserId { get; set; } = string.Empty;
    [Parameter] public EventCallback<int> OnOntologyLinked { get; set; }

    private Modal _modal = null!;
    private bool _loading = true;
    private string _searchQuery = string.Empty;
    private List<Ontology> _availableOntologies = new();
    private List<Ontology> _filteredOntologies = new();
    private Ontology? _selectedOntology;

    public async Task ShowAsync()
    {
        _modal.Show();
        await LoadAvailableOntologiesAsync();
    }

    private async Task LoadAvailableOntologiesAsync()
    {
        _loading = true;
        StateHasChanged();

        // Get all ontologies user can view (excluding current)
        _availableOntologies = await OntologyService.GetUserAccessibleOntologiesAsync(UserId);
        _availableOntologies = _availableOntologies
            .Where(o => o.Id != ParentOntologyId)
            .ToList();

        _filteredOntologies = _availableOntologies;
        _loading = false;
        StateHasChanged();
    }

    private void OnSearchChanged()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            _filteredOntologies = _availableOntologies;
        }
        else
        {
            var query = _searchQuery.ToLower();
            _filteredOntologies = _availableOntologies
                .Where(o => o.Name.ToLower().Contains(query) ||
                            (o.Description?.ToLower().Contains(query) ?? false))
                .ToList();
        }
    }

    private void SelectOntology(Ontology ontology)
    {
        _selectedOntology = ontology;
    }

    private bool IsSelected(Ontology ontology)
    {
        return _selectedOntology?.Id == ontology.Id;
    }

    private bool CanLink(Ontology? ontology)
    {
        return ontology != null;
        // Additional permission check happens server-side
    }

    private async Task ConfirmLink()
    {
        if (_selectedOntology == null) return;

        try
        {
            await OnOntologyLinked.InvokeAsync(_selectedOntology.Id);
            _modal.Hide();
            ToastService.ShowSuccess($"Linked {_selectedOntology.Name} successfully");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to link ontology: {ex.Message}");
        }
    }

    private void Cancel()
    {
        _modal.Hide();
    }
}
```

**Styles** (`wwwroot/css/components/link-ontology-dialog.css`):
```css
.ontology-card {
    cursor: pointer;
    transition: all 0.2s ease;
}

.ontology-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 8px rgba(0,0,0,0.1);
}

.ontology-card.selected {
    border-color: #0d6efd;
    background-color: #e7f3ff;
}
```

#### 4.2 Add Toolbar Button
**File**: `Components/Ontology/OntologyToolbar.razor`

```razor
<!-- Add after "Add Concept" button -->
<button class="btn btn-outline-primary"
        @onclick="OnLinkOntologyClicked"
        title="Link another ontology">
    <i class="bi bi-diagram-3"></i> Link Ontology
</button>

@code {
    [Parameter] public EventCallback OnLinkOntologyClicked { get; set; }
}
```

#### 4.3 Integrate into OntologyView
**File**: `Components/Pages/OntologyView.razor`

```razor
<LinkOntologyDialog @ref="_linkDialog"
                   ParentOntologyId="@OntologyId"
                   UserId="@UserId"
                   OnOntologyLinked="HandleOntologyLinked" />

@code {
    private LinkOntologyDialog _linkDialog = null!;

    private async Task ShowLinkOntologyDialog()
    {
        await _linkDialog.ShowAsync();
    }

    private async Task HandleOntologyLinked(int linkedOntologyId)
    {
        try
        {
            var link = await _ontologyLinkService.CreateInternalLinkAsync(
                OntologyId,
                linkedOntologyId,
                UserId);

            await LoadGraphDataAsync();
            _toastService.ShowSuccess("Ontology linked successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _toastService.ShowError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _toastService.ShowError(ex.Message);
        }
    }
}
```

---

## Phase 5: SignalR Synchronization (Days 13-14)

### Goals
- Detect when source ontology changes
- Notify parent ontologies
- Update availability indicator
- Handle permission revocation

### Tasks

#### 5.1 Extend SignalR Hub
**File**: `Hubs/OntologyHub.cs`

```csharp
public class OntologyHub : Hub
{
    private readonly IOntologyLinkService _linkService;

    // Existing methods...

    public async Task JoinOntologyGroup(int ontologyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ontology_{ontologyId}");

        // Also join as observer for any ontologies this one links to
        var links = await _linkService.GetLinksForOntologyAsync(ontologyId, Context.UserIdentifier);
        foreach (var link in links.Where(l => l.LinkType == LinkType.Internal))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"ontology_{link.LinkedOntologyId}_observers");
        }
    }

    // NEW: Broadcast updates to observers
    public async Task NotifyOntologyUpdated(int ontologyId)
    {
        await Clients.Group($"ontology_{ontologyId}_observers")
            .SendAsync("LinkedOntologyUpdated", ontologyId);
    }
}
```

#### 5.2 Handle Update Notifications
**File**: `Components/Pages/OntologyView.SignalR.cs`

```csharp
private async Task InitializeSignalRAsync()
{
    _hubConnection = new HubConnectionBuilder()
        .WithUrl(NavigationManager.ToAbsoluteUri("/hubs/ontology"))
        .Build();

    // Existing handlers...

    // NEW: Handle linked ontology updates
    _hubConnection.On<int>("LinkedOntologyUpdated", async (linkedOntologyId) =>
    {
        await InvokeAsync(async () =>
        {
            await HandleLinkedOntologyUpdatedAsync(linkedOntologyId);
        });
    });

    await _hubConnection.StartAsync();
    await _hubConnection.InvokeAsync("JoinOntologyGroup", OntologyId);
}

private async Task HandleLinkedOntologyUpdatedAsync(int linkedOntologyId)
{
    // Find which links point to this ontology
    var links = await _ontologyLinkService.GetLinksForOntologyAsync(OntologyId, UserId);
    var affectedLinks = links.Where(l => l.LinkedOntologyId == linkedOntologyId);

    foreach (var link in affectedLinks)
    {
        await _ontologyLinkService.CheckForUpdatesAsync(link.Id);
    }

    // Refresh graph to show update indicators
    await LoadGraphDataAsync();
    StateHasChanged();

    _toastService.ShowInfo($"Updates available for linked ontology: {links.First().Name}");
}
```

#### 5.3 Trigger Notifications on Changes
**File**: `Services/ConceptService.cs` and `Services/RelationshipService.cs`

```csharp
public async Task<Concept> CreateConceptAsync(Concept concept, string userId)
{
    // ... existing logic ...

    // NEW: Notify observers that this ontology was updated
    await _hubContext.Clients.Group($"ontology_{concept.OntologyId}_observers")
        .SendAsync("LinkedOntologyUpdated", concept.OntologyId);

    return created;
}
```

---

## Phase 6: Export & Import (Days 15-16)

### Goals
- Include linked ontologies in TTL export (owl:imports)
- Export link metadata in JSON
- Handle import of linked references

### Tasks

#### 6.1 Update TTL Export
**File**: `Services/TtlExportService.cs`

```csharp
private IGraph BuildRdfGraph(Ontology ontology)
{
    var graph = new Graph();

    // ... existing setup ...

    // NEW: Add owl:imports for linked ontologies
    var owlImports = graph.CreateUriNode("owl:imports");
    var links = await _ontologyLinkRepository.GetByOntologyIdAsync(ontology.Id);

    foreach (var link in links.Where(l => l.LinkType == LinkType.Internal && l.LinkedOntology != null))
    {
        var linkedUri = !string.IsNullOrWhiteSpace(link.LinkedOntology.Namespace)
            ? link.LinkedOntology.Namespace
            : OntologyNamespaces.CreateDefaultNamespace(link.LinkedOntology.Name);

        var importNode = graph.CreateUriNode(UriFactory.Create(linkedUri));
        graph.Assert(ontologyNode, owlImports, importNode);
    }

    // ... rest of export ...
}
```

#### 6.2 Update JSON Export
**File**: `Services/Export/JsonExportStrategy.cs`

```csharp
LinkedOntologies = ontology.LinkedOntologies.Select(lo => new LinkedOntologyExportModel
{
    Name = lo.Name,
    Uri = lo.Uri,
    LinkedOntologyId = lo.LinkedOntologyId,  // NEW
    LinkType = lo.LinkType.ToString(),        // NEW
    Prefix = lo.Prefix,
    Description = lo.Description,
    PositionX = lo.PositionX,                 // NEW
    PositionY = lo.PositionY,                 // NEW
    ConceptsImported = lo.ConceptsImported,
    ImportedConceptCount = lo.ImportedConceptCount,
    CreatedAt = lo.CreatedAt
}).ToList()
```

---

## Phase 7: Testing & Documentation (Days 17-20)

### Testing Strategy

#### Unit Tests
- `OntologyLinkServiceTests.cs`: Service logic, permissions, circular detection
- `OntologyLinkRepositoryTests.cs`: Data access
- `CircularDependencyDetectionTests.cs`: Graph traversal algorithms

#### Integration Tests
- `LinkOntologyWorkflowTests.cs`: End-to-end workflow
- `PermissionEnforcementTests.cs`: Access control
- `SyncTests.cs`: SignalR synchronization

#### UI Tests (bUnit)
- `LinkOntologyDialogTests.cs`: Dialog behavior
- `GraphRenderingTests.cs`: Virtualized node rendering

#### Manual Testing Checklist
- [ ] Create link to accessible ontology
- [ ] Cannot link to ontology without permission
- [ ] Cannot create circular dependency
- [ ] Graph renders virtualized node correctly
- [ ] Expand shows virtualized concepts
- [ ] Update indicator appears when source changes
- [ ] Sync refreshes data
- [ ] Unlink removes node
- [ ] Export includes owl:imports
- [ ] Permission revocation handled gracefully

### Documentation

#### User-Facing
- User Guide: "Linking Ontologies" section
- Release Notes: Feature announcement
- Video tutorial: Creating and managing links

#### Developer
- API documentation for `IOntologyLinkService`
- Architecture decision records
- Database schema documentation

---

## Rollout Plan

### Week 1-2: Alpha (Internal Testing)
- Deploy to dev environment
- Team testing with sample ontologies
- Fix critical bugs

### Week 3: Beta (Limited Users)
- Deploy to staging
- Invite 10 power users
- Collect feedback
- Performance tuning

### Week 4: General Availability
- Deploy to production
- Announce in release notes
- Monitor usage metrics
- Address user feedback

---

## Success Criteria

**Technical**:
- ✅ 100% test pass rate
- ✅ No circular dependency bugs
- ✅ <100ms link creation time
- ✅ <2s graph render with 5 links
- ✅ Zero data loss incidents

**User**:
- ✅ 30% of active users create at least one link in first month
- ✅ <5% error rate in link creation
- ✅ 4.5/5 average user satisfaction score

---

## Next Steps

After this document is reviewed and approved:

1. Create Phase 1 task branch: `feature/virtualized-nodes-phase1`
2. Create GitHub Project board for tracking
3. Begin database migration implementation
4. Schedule daily stand-ups for duration of project
