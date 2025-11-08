# Implementation Plan: Graph Layout Improvements

**Date**: November 6, 2025
**Status**: 🚧 In Progress

---

## Phase 1: Position Persistence (HIGH PRIORITY)

### Objective
Save and restore node positions across sessions using existing database fields.

### Tasks

#### 1.1 Add Service Methods for Position Updates

**File**: `Services/ConceptService.cs`

```csharp
/// <summary>
/// Updates the position of a concept node in the graph
/// </summary>
public async Task UpdatePositionAsync(int conceptId, double x, double y)
{
    using var context = await _contextFactory.CreateDbContextAsync();

    var concept = await context.Concepts.FindAsync(conceptId);
    if (concept == null)
        throw new InvalidOperationException($"Concept {conceptId} not found");

    concept.PositionX = x;
    concept.PositionY = y;

    await context.SaveChangesAsync();
}

/// <summary>
/// Batch update positions for multiple concepts
/// </summary>
public async Task UpdatePositionsBatchAsync(Dictionary<int, (double X, double Y)> positions)
{
    using var context = await _contextFactory.CreateDbContextAsync();

    var conceptIds = positions.Keys.ToList();
    var concepts = await context.Concepts
        .Where(c => conceptIds.Contains(c.Id))
        .ToListAsync();

    foreach (var concept in concepts)
    {
        if (positions.TryGetValue(concept.Id, out var pos))
        {
            concept.PositionX = pos.X;
            concept.PositionY = pos.Y;
        }
    }

    await context.SaveChangesAsync();
}
```

**File**: `Services/OntologyLinkService.cs`

```csharp
/// <summary>
/// Updates the position of an ontology link node in the graph
/// </summary>
public async Task UpdatePositionAsync(int linkId, double x, double y)
{
    using var context = await _contextFactory.CreateDbContextAsync();

    var link = await context.OntologyLinks.FindAsync(linkId);
    if (link == null)
        throw new InvalidOperationException($"OntologyLink {linkId} not found");

    link.PositionX = x;
    link.PositionY = y;

    await context.SaveChangesAsync();
}
```

#### 1.2 Add Interfaces

**File**: `Services/Interfaces/IConceptService.cs`

```csharp
Task UpdatePositionAsync(int conceptId, double x, double y);
Task UpdatePositionsBatchAsync(Dictionary<int, (double X, double Y)> positions);
```

**File**: `Services/Interfaces/IOntologyLinkService.cs`

```csharp
Task UpdatePositionAsync(int linkId, double x, double y);
```

#### 1.3 Add C# Methods to GraphVisualization Component

**File**: `Components/Pages/GraphVisualization.razor`

Add to `@code` block:

```csharp
[JSInvokable]
public async Task SaveNodePositionsBatch(List<NodePositionUpdate> updates)
{
    try
    {
        // Separate concepts and ontology links
        var conceptUpdates = updates
            .Where(u => u.Type == "concept")
            .ToDictionary(u => u.Id, u => (u.X, u.Y));

        var linkUpdates = updates.Where(u => u.Type == "ontologyLink").ToList();

        // Batch update concepts
        if (conceptUpdates.Any())
        {
            await ConceptService.UpdatePositionsBatchAsync(conceptUpdates);
        }

        // Update ontology links individually (usually fewer)
        foreach (var update in linkUpdates)
        {
            await OntologyLinkService.UpdatePositionAsync(update.Id, update.X, update.Y);
        }

        Logger.LogInformation("Saved positions for {Count} nodes", updates.Count);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error saving node positions");
    }
}

public class NodePositionUpdate
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // "concept" or "ontologyLink"
    public double X { get; set; }
    public double Y { get; set; }
}
```

#### 1.4 Update JavaScript to Capture Positions

**File**: `wwwroot/js/graphVisualization.js`

Add near top of file:

```javascript
// Position saving state
let positionUpdates = [];
let batchSaveTimeout = null;

/**
 * Saves node positions to the server in batches
 */
function scheduleBatchSave(dotNetHelper) {
    clearTimeout(batchSaveTimeout);
    batchSaveTimeout = setTimeout(async () => {
        if (positionUpdates.length > 0 && dotNetHelper) {
            try {
                await dotNetHelper.invokeMethodAsync(
                    'SaveNodePositionsBatch',
                    positionUpdates
                );
                console.log(`Saved ${positionUpdates.length} node positions`);
                positionUpdates = [];
            } catch (error) {
                console.error('Error saving positions:', error);
            }
        }
    }, 1000); // 1 second debounce
}
```

Update `renderOntologyGraph` function to add dragfree handler:

```javascript
// After cy initialization, add event listener
cy.on('dragfree', 'node', function(evt) {
    const node = evt.target;
    const nodeData = node.data();
    const pos = node.position();

    // Determine node type
    const nodeType = nodeData.isOntologyLink ? 'ontologyLink' : 'concept';
    const nodeId = nodeData.isOntologyLink ? nodeData.linkId : nodeData.id;

    // Add to batch
    positionUpdates.push({
        id: parseInt(nodeId),
        type: nodeType,
        x: pos.x,
        y: pos.y
    });

    // Schedule save
    scheduleBatchSave(dotNetHelper);
});
```

#### 1.5 Load Saved Positions on Initialization

**File**: `Components/Pages/GraphVisualization.razor`

Update `BuildGraphData` method to include positions:

```csharp
private object BuildGraphData()
{
    var nodes = new List<object>();
    var edges = new List<object>();

    // Existing concept node building
    foreach (var concept in Ontology.Concepts)
    {
        var nodeData = new
        {
            data = new
            {
                id = $"concept-{concept.Id}",
                label = concept.Name,
                type = "concept",
                nodeId = concept.Id,
                color = concept.Color ?? GetColorForCategory(concept.Category),
                category = concept.Category,
                isIndividual = false,
                isOntologyLink = false,
                isVirtualConcept = false
            },
            // Include position if saved
            position = concept.PositionX.HasValue && concept.PositionY.HasValue
                ? new { x = concept.PositionX.Value, y = concept.PositionY.Value }
                : null
        };

        // Only add node with position if not null
        if (nodeData.position != null)
        {
            nodes.Add(nodeData);
        }
        else
        {
            // Add without position property
            nodes.Add(new { data = nodeData.data });
        }
    }

    // Similar for OntologyLink nodes...
    foreach (var link in OntologyLinks)
    {
        var linkNodeData = new
        {
            data = new
            {
                id = $"ontologyLink-{link.Id}",
                label = link.TargetOntologyName,
                type = "ontologyLink",
                linkId = link.Id,
                isOntologyLink = true,
                color = link.Color ?? "#9B59B6"
            },
            position = link.PositionX.HasValue && link.PositionY.HasValue
                ? new { x = link.PositionX.Value, y = link.PositionY.Value }
                : null
        };

        if (linkNodeData.position != null)
        {
            nodes.Add(linkNodeData);
        }
        else
        {
            nodes.Add(new { data = linkNodeData.data });
        }
    }

    // ... existing edge building code ...

    return new { nodes, edges };
}
```

**File**: `wwwroot/js/graphVisualization.js`

Update layout configuration to use positions when available:

```javascript
// Check if any nodes have positions
const hasPositions = elements.nodes.some(n => n.position != null);

const layout = cy.layout({
    name: 'cose',
    animate: true,
    animationDuration: 500,
    randomize: !hasPositions,  // Don't randomize if we have saved positions
    // ... other config
});
```

---

## Phase 2: Edge Label Improvements (HIGH PRIORITY)

### Objective
Improve edge label readability and reduce overlaps.

### Tasks

#### 2.1 Update Edge Styling in JavaScript

**File**: `wwwroot/js/graphVisualization.js`

Replace edge selector style:

```javascript
{
    selector: 'edge',
    style: {
        'width': options.edgeThickness || 2,
        'line-color': '#999',
        'target-arrow-color': '#999',
        'target-arrow-shape': 'triangle',
        'curve-style': 'unbundled-bezier',  // Better for multi-edges
        'control-point-distances': [40, -40],
        'control-point-weights': [0.25, 0.75],

        // Label styling with outline
        'label': options.showEdgeLabels ? 'data(label)' : '',
        'font-size': edgeFontSize + 'px',
        'font-weight': '600',
        'text-rotation': 'autorotate',
        'text-margin-y': -10,
        'color': '#333',

        // White outline for readability
        'text-outline-color': '#ffffff',
        'text-outline-width': '2.5px',

        // Remove background (cleaner look)
        'text-background-opacity': 0,

        // Position labels along edge
        'edge-text-rotation': 'autorotate',
        'text-margin-x': 0,
        'text-wrap': 'wrap',
        'text-max-width': '100px'
    }
}
```

#### 2.2 Add Special Handling for Multi-Edges

```javascript
// After cy initialization, detect multi-edges
cy.ready(function() {
    // Group edges by source-target pair
    const edgeGroups = new Map();

    cy.edges().forEach(edge => {
        const source = edge.source().id();
        const target = edge.target().id();
        const key = [source, target].sort().join('-');

        if (!edgeGroups.has(key)) {
            edgeGroups.set(key, []);
        }
        edgeGroups.get(key).push(edge);
    });

    // Apply different label positions to multi-edges
    edgeGroups.forEach(edges => {
        if (edges.length > 1) {
            edges.forEach((edge, index) => {
                const offset = (index - (edges.length - 1) / 2) * 15;
                edge.style({
                    'text-margin-y': offset,
                    'control-point-distances': [40 * (1 + index * 0.3), -40 * (1 + index * 0.3)]
                });
            });
        }
    });
});
```

---

## Phase 3: Layout Algorithm Improvements (MEDIUM PRIORITY)

### Objective
Tune COSE parameters for better initial layouts.

### Tasks

#### 3.1 Update COSE Configuration

**File**: `wwwroot/js/graphVisualization.js`

Replace layout config:

```javascript
const layout = cy.layout({
    name: 'cose',
    animate: true,
    animationDuration: 500,

    // Improved spacing parameters
    nodeRepulsion: function(node) {
        // More repulsion for larger nodes
        return node.data('isOntologyLink') ? 15000 : 12000;
    },
    idealEdgeLength: function(edge) {
        // Longer edges for ontology link connections
        const source = edge.source();
        const target = edge.target();
        if (source.data('isOntologyLink') || target.data('isOntologyLink')) {
            return 150;
        }
        return 120;
    },
    edgeElasticity: 80,

    // Hierarchy settings
    nestingFactor: 3,
    gravity: 50,

    // Convergence settings
    numIter: 1500,
    initialTemp: 150,
    coolingFactor: 0.96,
    minTemp: 1.0,

    // Position settings
    randomize: !hasPositions,
    fit: true,
    padding: 50,

    // Component spacing
    componentSpacing: 150,

    // Node overlap prevention
    nodeOverlap: 20,

    // Use saved positions as starting point
    positions: hasPositions ? undefined : null
});
```

#### 3.2 Add Layout Quality Metrics

```javascript
/**
 * Calculates layout quality score
 */
function calculateLayoutQuality(cy) {
    let score = 100;

    // Check for overlapping nodes
    const nodes = cy.nodes();
    for (let i = 0; i < nodes.length; i++) {
        for (let j = i + 1; j < nodes.length; j++) {
            const dist = nodes[i].position().x - nodes[j].position().x;
            if (Math.abs(dist) < 60) {
                score -= 5;
            }
        }
    }

    // Check edge crossings (simplified)
    const edges = cy.edges().length;
    const crossingPenalty = Math.max(0, edges - 20) * 0.5;
    score -= crossingPenalty;

    return Math.max(0, score);
}

// Log quality after layout
layout.one('layoutstop', function() {
    const quality = calculateLayoutQuality(cy);
    console.log(`Layout quality score: ${quality}/100`);
});
```

---

## Phase 4: Layout Selector UI (LOW PRIORITY - OPTIONAL)

### Objective
Allow users to switch between layout algorithms and reset layout.

### Tasks

#### 4.1 Add Layout Dropdown to Control Bar

**File**: `Components/Ontology/OntologyControlBar.razor`

Add between existing buttons:

```razor
<!-- Layout Selector -->
<div class="btn-group" role="group">
    <button type="button" class="btn btn-sm btn-outline-secondary dropdown-toggle"
            data-bs-toggle="dropdown" aria-expanded="false"
            title="Change graph layout">
        <i class="bi bi-diagram-3"></i>
        @GetLayoutDisplayName(CurrentLayout)
    </button>
    <ul class="dropdown-menu">
        <li>
            <a class="dropdown-item @(CurrentLayout == GraphLayout.ForceDirected ? "active" : "")"
               @onclick="() => ChangeLayout(GraphLayout.ForceDirected)">
                <i class="bi bi-arrow-down-up"></i> Force-Directed
            </a>
        </li>
        <li>
            <a class="dropdown-item @(CurrentLayout == GraphLayout.Hierarchical ? "active" : "")"
               @onclick="() => ChangeLayout(GraphLayout.Hierarchical)">
                <i class="bi bi-diagram-2"></i> Hierarchical
            </a>
        </li>
        <li>
            <a class="dropdown-item @(CurrentLayout == GraphLayout.Circular ? "active" : "")"
               @onclick="() => ChangeLayout(GraphLayout.Circular)">
                <i class="bi bi-circle"></i> Circular
            </a>
        </li>
        <li><hr class="dropdown-divider"></li>
        <li>
            <a class="dropdown-item" @onclick="ResetLayout">
                <i class="bi bi-arrow-counterclockwise"></i> Reset Layout
            </a>
        </li>
    </ul>
</div>
```

Add code:

```csharp
@code {
    [Parameter] public GraphLayout CurrentLayout { get; set; }
    [Parameter] public EventCallback<GraphLayout> OnLayoutChange { get; set; }
    [Parameter] public EventCallback OnResetLayout { get; set; }

    private async Task ChangeLayout(GraphLayout layout)
    {
        await OnLayoutChange.InvokeAsync(layout);
    }

    private async Task ResetLayout()
    {
        await OnResetLayout.InvokeAsync();
    }

    private string GetLayoutDisplayName(GraphLayout layout)
    {
        return layout switch
        {
            GraphLayout.ForceDirected => "Force",
            GraphLayout.Hierarchical => "Hierarchical",
            GraphLayout.Circular => "Circular",
            GraphLayout.Grid => "Grid",
            GraphLayout.Concentric => "Concentric",
            GraphLayout.Breadth => "Breadth",
            _ => "Auto"
        };
    }
}
```

#### 4.2 Add Layout Switching to JavaScript

**File**: `wwwroot/js/graphVisualization.js`

Add function:

```javascript
/**
 * Changes the graph layout algorithm
 */
window.changeGraphLayout = function(containerId, layoutName) {
    const cy = window.cytoscapeInstances?.[containerId];
    if (!cy) {
        console.error('Cytoscape instance not found:', containerId);
        return;
    }

    const layoutConfig = getLayoutConfig(layoutName);
    const layout = cy.layout(layoutConfig);
    layout.run();
};

/**
 * Resets graph layout by clearing positions and re-running
 */
window.resetGraphLayout = function(containerId) {
    const cy = window.cytoscapeInstances?.[containerId];
    if (!cy) return;

    // Clear saved positions
    cy.nodes().forEach(node => {
        node.removeData('savedPosition');
    });

    // Re-run current layout
    const layout = cy.layout(getLayoutConfig('cose'));
    layout.run();
};

function getLayoutConfig(layoutName) {
    switch (layoutName) {
        case 'hierarchical':
            return {
                name: 'breadthfirst',
                directed: true,
                spacingFactor: 1.5,
                animate: true
            };
        case 'circular':
            return {
                name: 'circle',
                animate: true,
                animationDuration: 500
            };
        case 'grid':
            return {
                name: 'grid',
                animate: true,
                avoidOverlap: true
            };
        default:
            return {
                name: 'cose',
                // ... existing COSE config
            };
    }
}
```

---

## Testing Plan

### Unit Tests

**File**: `Eidos.Tests/Services/ConceptServiceTests.cs`

```csharp
[Fact]
public async Task UpdatePositionAsync_UpdatesConceptPosition()
{
    // Arrange
    var concept = new Concept { Name = "Test", OntologyId = 1 };
    _context.Concepts.Add(concept);
    await _context.SaveChangesAsync();

    // Act
    await _service.UpdatePositionAsync(concept.Id, 100.5, 200.7);

    // Assert
    var updated = await _context.Concepts.FindAsync(concept.Id);
    Assert.Equal(100.5, updated.PositionX);
    Assert.Equal(200.7, updated.PositionY);
}

[Fact]
public async Task UpdatePositionsBatchAsync_UpdatesMultiplePositions()
{
    // Arrange
    var concepts = new[]
    {
        new Concept { Name = "A", OntologyId = 1 },
        new Concept { Name = "B", OntologyId = 1 },
        new Concept { Name = "C", OntologyId = 1 }
    };
    _context.Concepts.AddRange(concepts);
    await _context.SaveChangesAsync();

    var positions = concepts.ToDictionary(
        c => c.Id,
        c => (X: c.Id * 100.0, Y: c.Id * 200.0)
    );

    // Act
    await _service.UpdatePositionsBatchAsync(positions);

    // Assert
    foreach (var concept in concepts)
    {
        var updated = await _context.Concepts.FindAsync(concept.Id);
        Assert.Equal(concept.Id * 100.0, updated.PositionX);
        Assert.Equal(concept.Id * 200.0, updated.PositionY);
    }
}
```

### Manual Testing Checklist

- [ ] Drag a concept node, verify position saves (check DB)
- [ ] Refresh page, verify position restored
- [ ] Drag multiple nodes, verify batch save
- [ ] Create new ontology, verify auto-layout works
- [ ] Test with 20+ node ontology
- [ ] Test with multiple edges between same nodes
- [ ] Verify edge labels readable at various zoom levels
- [ ] Test on mobile (touch drag)
- [ ] Test with ontology links (virtualized nodes)

---

## Deployment Steps

1. **Merge to main branch**
2. **Run database migrations** (none needed - fields exist)
3. **Deploy to staging**
4. **Run smoke tests**
5. **Monitor Application Insights for errors**
6. **Deploy to production**
7. **Announce feature in release notes**

---

## Rollback Plan

If issues arise:

1. **JavaScript changes**: Revert `graphVisualization.js` from git history
2. **Service changes**: Position updates are non-breaking (just won't save)
3. **Database**: No schema changes, rollback safe

---

## Performance Benchmarks

**Before**:
- Graph load time: ~500ms (50 nodes)
- Layout calculation: ~1000ms
- Position save: N/A (not implemented)

**After (Expected)**:
- Graph load time: ~550ms (+50ms for position loading)
- Layout calculation: ~800ms (fewer iterations with saved positions)
- Position save: <100ms (batched)
- Drag responsiveness: <50ms

---

## Future Enhancements

- [ ] Zoom/pan persistence
- [ ] Layout presets (save/load named layouts)
- [ ] Automatic layout optimization (genetic algorithm)
- [ ] Collaborative layout editing (real-time position sync)
- [ ] Export layout as image with preserved positions
- [ ] Minimap for large graphs
