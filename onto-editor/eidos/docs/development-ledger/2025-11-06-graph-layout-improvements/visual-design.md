# Visual Design: Graph Layout Improvements

**Date**: November 6, 2025
**Designer**: Claude (Blazor Developer Subagent)

---

## Edge Label Styling

### Current Design Issues

**Before**:
```
┌─────────────────┐
│    Concept A    │──────────────────┐
└─────────────────┘  ╭─────────╮    │
                     │  label  │    ▼
┌─────────────────┐  │background│ ┌─────────────────┐
│    Concept B    │  ╰─────────╯ │    Concept C    │
└─────────────────┘◀───────────────└─────────────────┘

Problems:
❌ White boxes create visual clutter
❌ Multiple labels stack and overlap
❌ Hard to read when boxes overlap
```

### Improved Design

**After**:
```
┌─────────────────┐
│    Concept A    │───── hasAuthor ─────┐
└─────────────────┘                     │
                                        ▼
┌─────────────────┐                  ┌─────────────────┐
│    Concept B    │◀──── references ──│    Concept C    │
└─────────────────┘                  └─────────────────┘

Features:
✅ Text outline (white halo) instead of boxes
✅ Bolder font weight (600) for readability
✅ Autorotate for natural reading angle
✅ Clean, minimal appearance
```

### CSS Specifications

```javascript
// Edge label styling
{
    'label': 'data(label)',
    'font-family': 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    'font-size': '12px',
    'font-weight': '600',
    'color': '#333333',

    // White outline for contrast
    'text-outline-color': '#ffffff',
    'text-outline-width': '2.5px',

    // Positioning
    'text-rotation': 'autorotate',
    'text-margin-y': -10,
    'text-margin-x': 0,

    // Readability
    'text-wrap': 'wrap',
    'text-max-width': '100px',

    // Remove background
    'text-background-opacity': 0
}
```

---

## Multi-Edge Scenarios

### Problem: Stacked Labels

When multiple edges connect same nodes:

```
         edge1.label
┌─────┐  edge2.label  ┌─────┐
│  A  │ ═════════════ │  B  │
└─────┘  edge3.label  └─────┘

❌ All labels at same position
❌ Illegible text
❌ User can't distinguish relationships
```

### Solution: Curved Edges with Offset Labels

```
          ╭── label1 ──╮
┌─────┐  ╱              ╲ ┌─────┐
│  A  │ ─────label2───── │  B  │
└─────┘  ╲              ╱ └─────┘
          ╰── label3 ──╯

✅ Each edge curves differently
✅ Labels positioned along curves
✅ Clear visual separation
```

**Implementation**:

```javascript
// Detect multi-edges after graph render
cy.ready(function() {
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

    // Apply offsets to multi-edges
    edgeGroups.forEach(edges => {
        if (edges.length > 1) {
            edges.forEach((edge, index) => {
                const offset = (index - (edges.length - 1) / 2) * 15;
                edge.style({
                    'text-margin-y': offset,
                    'control-point-distances': [
                        40 * (1 + index * 0.3),
                        -40 * (1 + index * 0.3)
                    ]
                });
            });
        }
    });
});
```

---

## Node Position Indicators

### Anchored Nodes (Position Saved)

Visual feedback when node has saved position:

```
┌───────────────────────┐
│      Concept Name     │ 🔒
│    (Position Saved)   │
└───────────────────────┘
```

**Subtle lock icon in corner**:

```javascript
{
    selector: 'node[?hasPosition]',
    style: {
        'background-opacity': 1,
        'border-width': 2,
        'border-color': '#28a745',  // Green border
        'overlay-opacity': 0.1,
        'overlay-color': '#28a745'
    }
}
```

### Unsaved Changes Indicator

When user drags node but batch save hasn't completed:

```
┌───────────────────────┐
│      Concept Name     │ ⏳
│   (Saving position)   │
└───────────────────────┘
     Orange pulse
```

**Temporary styling during save**:

```javascript
node.style({
    'border-color': '#ff8c00',  // Orange
    'border-width': 3
});

// Remove after save completes
setTimeout(() => {
    node.style({
        'border-color': '#28a745',
        'border-width': 2
    });
}, 1500);
```

---

## Layout Selector UI

### Control Bar Addition

**Current Control Bar**:
```
[Filters ▼] [Show Individuals] [Color: Concept ▼] [Export]
```

**Updated Control Bar**:
```
[Filters ▼] [Layout: Force ▼] [Show Individuals] [Color: Concept ▼] [Export]
```

### Layout Dropdown Menu

```razor
<!-- Mockup -->
<div class="btn-group">
    <button class="btn btn-sm btn-outline-secondary dropdown-toggle">
        <i class="bi bi-diagram-3"></i>
        Force-Directed
    </button>
    <ul class="dropdown-menu">
        <li>
            <a class="dropdown-item active">
                <i class="bi bi-arrow-down-up"></i>
                Force-Directed
                <span class="badge bg-success ms-2">Current</span>
            </a>
        </li>
        <li>
            <a class="dropdown-item">
                <i class="bi bi-diagram-2"></i>
                Hierarchical
            </a>
        </li>
        <li>
            <a class="dropdown-item">
                <i class="bi bi-circle"></i>
                Circular
            </a>
        </li>
        <li>
            <a class="dropdown-item">
                <i class="bi bi-grid-3x3"></i>
                Grid
            </a>
        </li>
        <li><hr class="dropdown-divider"></li>
        <li>
            <a class="dropdown-item">
                <i class="bi bi-arrow-counterclockwise"></i>
                Reset Layout
            </a>
        </li>
    </ul>
</div>
```

### Layout Icons

| Layout | Icon | Bootstrap Icon Class |
|--------|------|---------------------|
| Force-Directed | ↕️ | `bi-arrow-down-up` |
| Hierarchical | 🌳 | `bi-diagram-2` |
| Circular | ⭕ | `bi-circle` |
| Grid | ⊞ | `bi-grid-3x3` |
| Concentric | 🎯 | `bi-bullseye` |
| Breadth | 📊 | `bi-bar-chart-steps` |
| Reset | ↻ | `bi-arrow-counterclockwise` |

---

## Graph Quality Indicators

### Layout Quality Score

Display after auto-layout completes:

```
┌────────────────────────────────────┐
│  Layout Quality: ████████░░ 82/100 │
│  ✓ No overlapping nodes            │
│  ⚠ Some edge crossings             │
│  ℹ Click nodes to manually adjust  │
└────────────────────────────────────┘
```

**Toast notification**:

```javascript
function showLayoutQualityScore(score) {
    let message, type;

    if (score >= 80) {
        message = `Great layout! Quality: ${score}/100`;
        type = 'success';
    } else if (score >= 60) {
        message = `Good layout. Quality: ${score}/100. You can adjust nodes manually.`;
        type = 'info';
    } else {
        message = `Layout quality: ${score}/100. Try resetting or manually adjusting nodes.`;
        type = 'warning';
    }

    // Show toast (integrate with existing ToastService)
    console.log(message);
}
```

---

## Color Scheme

### Edge Labels

| Element | Color | Purpose |
|---------|-------|---------|
| Text | `#333333` | Main label text |
| Outline | `#ffffff` | White halo for contrast |
| Background | Transparent | Clean appearance |

### Position State Colors

| State | Color | Bootstrap Class | Purpose |
|-------|-------|-----------------|---------|
| Saved | `#28a745` | `success` | Position persisted |
| Saving | `#ff8c00` | `warning` | Save in progress |
| Error | `#dc3545` | `danger` | Save failed |

### Layout Quality

| Score | Color | Meaning |
|-------|-------|---------|
| 80-100 | Green | Excellent layout |
| 60-79 | Blue | Good layout |
| 40-59 | Yellow | Acceptable |
| 0-39 | Red | Poor layout |

---

## Responsive Design

### Desktop (>1200px)

```
┌─────────────────────────────────────────────────┐
│  [Control Bar with all buttons]                 │
├─────────────────────────────────────────────────┤
│                                                  │
│                 [Graph Canvas]                   │
│                  Full height                     │
│                                                  │
└─────────────────────────────────────────────────┘
```

### Tablet (768px - 1200px)

```
┌───────────────────────────────┐
│  [Control Bar - 2 rows]       │
├───────────────────────────────┤
│                                │
│      [Graph Canvas]            │
│                                │
└───────────────────────────────┘
```

### Mobile (<768px)

```
┌─────────────────┐
│  [⋮] More       │
├─────────────────┤
│                 │
│  [Graph]        │
│  Touch          │
│  Enabled        │
│                 │
└─────────────────┘
```

Mobile optimizations:
- Larger touch targets (60px min)
- Simplified control bar (hamburger menu)
- Pinch-to-zoom enabled
- Pan with single finger
- Long-press for context menu

---

## Animation & Transitions

### Position Save Animation

When user drags node:

```
1. User starts drag
   └─> Node border: solid → dashed

2. User releases (dragfree)
   └─> Node border: dashed → pulsing orange

3. Save completes (1s later)
   └─> Node border: orange → green fade

4. Resting state
   └─> Node border: green solid (subtle)
```

**CSS**:

```css
.cy-node-saving {
    animation: pulse-save 1s ease-in-out;
}

@keyframes pulse-save {
    0%, 100% {
        border-color: #ff8c00;
        border-width: 2px;
    }
    50% {
        border-color: #ffa500;
        border-width: 3px;
    }
}

.cy-node-saved {
    transition: border-color 0.5s ease-out;
    border-color: #28a745;
}
```

### Layout Transition

When switching layouts:

```
1. Fade out labels (200ms)
2. Animate nodes to new positions (500ms ease-in-out)
3. Fade in labels (200ms)
Total: 900ms
```

**JavaScript**:

```javascript
async function transitionLayout(cy, newLayoutConfig) {
    // Fade labels
    cy.elements().animate({
        style: { 'opacity': 0.3 }
    }, { duration: 200 });

    // Run layout
    const layout = cy.layout(newLayoutConfig);
    layout.run();

    // Wait for layout to complete
    layout.one('layoutstop', () => {
        cy.elements().animate({
            style: { 'opacity': 1 }
        }, { duration: 200 });
    });
}
```

---

## Accessibility

### Keyboard Navigation

| Key | Action |
|-----|--------|
| `Tab` | Focus next node |
| `Shift+Tab` | Focus previous node |
| `Arrow Keys` | Pan viewport |
| `+/-` | Zoom in/out |
| `Space` | Select focused node |
| `Esc` | Deselect all |

### Screen Reader Support

```html
<div id="cy"
     role="application"
     aria-label="Ontology graph visualization">
    <!-- Cytoscape container -->
</div>

<div class="sr-only" aria-live="polite" aria-atomic="true">
    <span id="graph-status">
        Graph loaded with {nodeCount} concepts and {edgeCount} relationships
    </span>
</div>
```

### High Contrast Mode

Detect and adjust:

```javascript
if (window.matchMedia('(prefers-contrast: high)').matches) {
    edgeStyle['text-outline-width'] = '3px';
    edgeStyle['font-weight'] = '700';
    nodeStyle['border-width'] = '3px';
}
```

---

## Performance Considerations

### Rendering Optimizations

**Large Graphs (>100 nodes)**:

```javascript
// Disable animations for large graphs
const animate = cy.nodes().length < 100;

layout: {
    name: 'cose',
    animate: animate,
    animationDuration: animate ? 500 : 0
}
```

**Edge Label LOD (Level of Detail)**:

```javascript
// Hide labels when zoomed out
cy.on('zoom', function() {
    const zoom = cy.zoom();

    if (zoom < 0.5) {
        // Hide all edge labels
        cy.edges().style('label', '');
    } else {
        // Show labels
        cy.edges().style('label', 'data(label)');
    }
});
```

---

## Future Enhancements

### Layout Presets

Allow users to save/load named layout configurations:

```
[Layout Presets ▼]
├─ Default (Force)
├─ My Research Layout ⭐
├─ Presentation Mode
└─ Save Current Layout...
```

### Collaborative Positioning

Real-time position sync when multiple users edit:

```
User A drags node
    ↓
SignalR broadcast
    ↓
User B sees animated position update
```

### Smart Auto-Layout

ML-based layout optimization:

```
1. Analyze ontology structure (tree, mesh, hierarchy)
2. Select optimal layout algorithm
3. Tune parameters for specific graph
4. Suggest manual adjustments
```

---

## Design System Compliance

### Color Palette

Uses existing Eidos design system:

| Purpose | Color Variable | Hex |
|---------|---------------|-----|
| Primary | `--bs-primary` | `#0d6efd` |
| Success | `--bs-success` | `#28a745` |
| Warning | `--bs-warning` | `#ffc107` |
| Danger | `--bs-danger` | `#dc3545` |
| Text | `--bs-body-color` | `#333333` |

### Typography

```css
font-family: 'Inter', -apple-system, BlinkMacSystemFont,
             "Segoe UI", Roboto, sans-serif;

/* Edge labels */
font-size: 12px;
font-weight: 600;
line-height: 1.4;

/* Control bar buttons */
font-size: 14px;
font-weight: 500;
```

### Spacing

Bootstrap spacing scale:

```
Padding: btn-sm = 0.25rem 0.5rem
Margin: ms-2 = 0.5rem
Gap: gap-2 = 0.5rem
```

---

**Last Updated**: November 6, 2025
**Status**: Ready for Implementation
