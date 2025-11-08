# Architecture Decisions: Graph Layout Improvements

**Created**: November 6, 2025
**Status**: In Progress

---

## ADR-GL-001: Use Existing Position Fields

**Status**: Accepted

**Context**:
Need to persist node positions across sessions. Database already has `PositionX` and `PositionY` fields on Concept and OntologyLink models from earlier migrations.

**Decision**:
Use existing nullable double fields. Store Cytoscape.js position coordinates directly without transformation.

**Alternatives Considered**:
1. Create new LayoutState table
   - ❌ Over-engineering
   - ❌ Requires migration and new models
   - ❌ More complex queries

2. Store in browser localStorage
   - ❌ Not synced across devices
   - ❌ Lost on cache clear
   - ❌ No server-side access

3. Use existing fields (CHOSEN)
   - ✅ Already in database schema
   - ✅ No migration needed
   - ✅ Works with export/import
   - ✅ Survives browser changes

**Rationale**:
- Fields were added in 20251025222257_AddOntologyProvenance migration
- Already used in JSON import/export
- Simple and efficient

---

## ADR-GL-002: Save on Dragfree Event with Debouncing

**Status**: Accepted

**Context**:
Need to capture when user finishes dragging node and save position. Cytoscape emits many events during drag operation.

**Decision**:
Listen to `dragfree` event (fires when mouse released after drag). Debounce with 500ms delay to batch rapid drags.

**Alternatives Considered**:
1. Save on every drag event
   - ❌ Hundreds of DB writes per drag
   - ❌ Performance impact
   - ❌ Unnecessary server load

2. Save on graph unload only
   - ❌ Lost if page crashes
   - ❌ Lost on navigation
   - ❌ No confirmation to user

3. Dragfree + debounce (CHOSEN)
   - ✅ Fires once per drag completion
   - ✅ Batches rapid adjustments
   - ✅ Immediate feedback
   - ✅ Reliable save points

**Rationale**:
- `dragfree` is semantic event for "user finished dragging"
- Debouncing prevents save spam while node hunting
- 500ms balances responsiveness with efficiency

**Implementation**:
```javascript
let savePositionTimeout;
cy.on('dragfree', 'node', function(evt) {
    clearTimeout(savePositionTimeout);
    savePositionTimeout = setTimeout(() => {
        const node = evt.target;
        const pos = node.position();
        dotNetHelper.invokeMethodAsync('SaveNodePosition',
            node.id(), pos.x, pos.y);
    }, 500);
});
```

---

## ADR-GL-003: Improve COSE Before Adding New Layouts

**Status**: Accepted

**Context**:
GraphViewState defines 6 layout algorithms but only COSE is implemented. Current COSE produces inconsistent results.

**Decision**:
Optimize COSE parameters first. Add other layouts only if user feedback requests them.

**Alternatives Considered**:
1. Implement all 6 layouts immediately
   - ❌ Significant development time
   - ❌ Unknown if users want alternatives
   - ❌ More UI complexity

2. Keep only COSE (CHOSEN for now)
   - ✅ Focus on quality over quantity
   - ✅ COSE works for most ontologies
   - ✅ Can add later if needed

3. Add hierarchical only
   - ⚠️ Good for tree-like ontologies
   - ❌ Doesn't help with label overlap

**Rationale**:
- 80% of ontologies are general graphs (not pure hierarchies)
- Better COSE parameters solve most complaints
- Position persistence reduces need for re-layout
- Can incrementally add layouts based on user requests

**COSE Parameter Tuning**:
```javascript
layout: {
    name: 'cose',
    animate: true,
    animationDuration: 500,
    nodeRepulsion: 12000,        // ↑ from 8000 (more space)
    idealEdgeLength: 120,        // ↑ from 100 (longer edges)
    edgeElasticity: 80,          // ↓ from 100 (less bounce)
    nestingFactor: 3,            // ↓ from 5 (flatter hierarchy)
    gravity: 50,                 // ↓ from 80 (less centering)
    numIter: 1500,               // ↑ from 1000 (better convergence)
    initialTemp: 150,            // ↓ from 200 (slower start)
    coolingFactor: 0.96,         // ↑ from 0.95 (slower cooling)
    minTemp: 1.0,
    randomize: false,            // Use positions if available
    componentSpacing: 150        // Space between disconnected components
}
```

---

## ADR-GL-004: Use Text Outline for Label Readability

**Status**: Accepted

**Context**:
Edge labels overlap when multiple relationships exist between concept clusters. White background boxes help but create visual clutter.

**Decision**:
Use CSS text-outline (white halo) instead of background boxes. Increase font weight for better visibility.

**Alternatives Considered**:
1. Background boxes (CURRENT)
   - ❌ Creates visual clutter
   - ❌ Boxes overlap each other
   - ❌ Harder to read when stacked

2. Edge bundling/curved edges
   - ⚠️ Helps separation
   - ❌ Doesn't solve label overlap
   - ✅ Use in combination

3. Text outline (CHOSEN)
   - ✅ Clean appearance
   - ✅ Labels readable over any background
   - ✅ Less visual weight
   - ✅ Better for overlapping scenarios

**Rationale**:
- Outline creates separation without boxes
- Industry standard (maps, diagrams)
- Works with Cytoscape's text-outline-* properties

**Implementation**:
```javascript
{
    selector: 'edge',
    style: {
        'label': 'data(label)',
        'font-size': '12px',
        'font-weight': '600',           // Bolder for readability
        'text-outline-color': '#ffffff',
        'text-outline-width': '2px',
        'text-rotation': 'autorotate',
        'text-margin-y': -10,
        'color': '#333',
        'text-background-opacity': 0    // Remove background
    }
}
```

---

## ADR-GL-005: Deterministic Layout with Saved Positions

**Status**: Accepted

**Context**:
COSE uses random initial positions, causing different layouts each time. Users lose manual adjustments.

**Decision**:
When positions exist in database, use them as starting points (`randomize: false`). When no positions, use deterministic random seed.

**Alternatives Considered**:
1. Always randomize
   - ❌ Loses manual adjustments
   - ❌ Different every refresh
   - ❌ Frustrating UX

2. Grid initial positions
   - ❌ Poor starting point for COSE
   - ❌ Doesn't preserve user intent

3. Use saved positions + seed (CHOSEN)
   - ✅ Preserves manual adjustments
   - ✅ Consistent initial layouts
   - ✅ Predictable behavior

**Rationale**:
- Position persistence makes this possible
- Best of both: auto-layout and manual control
- Users can drag nodes knowing positions will stick

**Implementation**:
```javascript
// In buildGraphData, check if positions exist
const hasPositions = elements.nodes.some(n => n.position);

layout: {
    name: 'cose',
    randomize: !hasPositions,  // Use positions if available
    // ... other params
}
```

---

## ADR-GL-006: Edge Segment Distance for Multi-Edge Scenarios

**Status**: Accepted

**Context**:
When multiple edges connect same two nodes (bidirectional or different relationship types), labels stack.

**Decision**:
Use `segment-distances` and `segment-weights` to position labels at different points along multi-edges.

**Alternatives Considered**:
1. Single label for all edges
   - ❌ Loses information
   - ❌ Can't distinguish relationship types

2. Stack labels vertically
   - ❌ Still overlap
   - ❌ Hard to read

3. Position along edge (CHOSEN)
   - ✅ Spreads labels out
   - ✅ Clear which label belongs to which edge
   - ✅ Cytoscape built-in support

**Rationale**:
- Cytoscape handles multi-edge layout automatically
- segment-distances distributes labels evenly
- Combines well with curve-style: 'bezier'

**Implementation**:
```javascript
{
    selector: 'edge',
    style: {
        'curve-style': 'unbundled-bezier',  // Better for multi-edges
        'control-point-distances': [40, -40],
        'control-point-weights': [0.25, 0.75],
        'text-margin-y': -10,
        'segment-distances': [20],           // Distance from node
        'segment-weights': [0.5],            // Middle of edge
        'edge-text-rotation': 'autorotate'
    }
}
```

---

## ADR-GL-007: Batch Position Updates with Bulk Save

**Status**: Accepted

**Context**:
Users often drag multiple nodes to adjust layout. Individual saves create many DB roundtrips.

**Decision**:
Collect position changes in JavaScript, send as batch after 1 second of inactivity.

**Alternatives Considered**:
1. Save each node individually
   - ❌ Many DB calls
   - ❌ Slow for bulk adjustments

2. Save all on "Save" button click
   - ❌ User forgets to save
   - ❌ Lost on navigation

3. Auto-batch with timeout (CHOSEN)
   - ✅ Efficient DB usage
   - ✅ Transparent to user
   - ✅ Saved automatically

**Rationale**:
- User often drags 3-5 nodes in quick succession
- 1-second delay catches batch without feeling slow
- Reduces DB load while maintaining auto-save UX

**Implementation**:
```javascript
let positionUpdates = [];
let batchSaveTimeout;

cy.on('dragfree', 'node', function(evt) {
    const node = evt.target;
    positionUpdates.push({
        id: node.data('nodeId'),
        type: node.data('type'), // 'concept' or 'ontologyLink'
        x: node.position().x,
        y: node.position().y
    });

    clearTimeout(batchSaveTimeout);
    batchSaveTimeout = setTimeout(async () => {
        if (positionUpdates.length > 0) {
            await dotNetHelper.invokeMethodAsync(
                'SaveNodePositionsBatch',
                positionUpdates
            );
            positionUpdates = [];
        }
    }, 1000);
});
```

---

## ADR-GL-008: Service Layer for Position Updates

**Status**: Accepted

**Context**:
Position updates need to go through proper service layer for logging, error handling, and testability.

**Decision**:
Add `UpdatePositionAsync` methods to ConceptService and OntologyLinkService.

**Alternatives Considered**:
1. Update directly in component
   - ❌ Violates architecture
   - ❌ Hard to test
   - ❌ No error handling

2. Create PositionService
   - ❌ Over-engineering
   - ❌ Positions are entity properties

3. Add to existing services (CHOSEN)
   - ✅ Follows existing patterns
   - ✅ Position is concept/link property
   - ✅ Consistent with UpdateConceptAsync

**Rationale**:
- Position is a property of Concept/OntologyLink
- ConceptService already handles updates
- Keeps architecture clean

**Method Signature**:
```csharp
Task UpdatePositionAsync(int conceptId, double x, double y);
Task UpdatePositionsBatchAsync(List<PositionUpdate> updates);

public class PositionUpdate
{
    public int EntityId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}
```

---

## Summary of Key Decisions

1. **Use existing PositionX/PositionY fields** in database
2. **Save on dragfree event** with 500ms debounce
3. **Optimize COSE parameters** before adding new layouts
4. **Text outline styling** for better label readability
5. **Deterministic layouts** using saved positions
6. **Segment distances** for multi-edge label separation
7. **Batch position updates** to reduce DB calls
8. **Service layer methods** for position persistence

These decisions prioritize:
- **User experience**: Positions stick, labels readable
- **Performance**: Batching, debouncing, efficient queries
- **Architecture**: Service layer, existing patterns
- **Maintainability**: Minimal new code, leverage existing features
