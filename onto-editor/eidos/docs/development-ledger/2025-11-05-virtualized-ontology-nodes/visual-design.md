# Visual Design: Virtualized Ontology Nodes

**Created**: November 5, 2025

---

## Design Principles

Following the existing Eidos visual language:
- **Clarity**: Immediately obvious what's virtualized vs native
- **Consistency**: Matches existing graph node styles
- **Accessibility**: Color-blind friendly, high contrast
- **Responsive**: Works on mobile and desktop

---

## Virtualized Ontology Node

### Normal State

```
┌─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─┐
│  🔗                          │
│                              │
│    Library Ontology          │
│                              │
│  ┌──────────────────────┐   │
│  │   42 concepts        │   │
│  └──────────────────────┘   │
└─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─┘
```

**Visual Properties**:
- Shape: Rounded rectangle (border-radius: 12px)
- Size: 140px x 90px (larger than concepts)
- Background: Purple gradient (#9333ea → #7c3aed)
- Border: 3px dashed, #6b21a8
- Icon: 🔗 Link icon in top-left
- Label: Ontology name (bold, white, 14px)
- Badge: Concept count in rounded container
- Shadow: 0 4px 8px rgba(147, 51, 234, 0.3)

### Selected State

```
┌━━━━━━━━━━━━━━━━━━━━━━━━━━━┐
│  🔗                        │
│                            │
│    Library Ontology        │
│                            │
│  ┌──────────────────────┐ │
│  │   42 concepts        │ │
│  └──────────────────────┘ │
└━━━━━━━━━━━━━━━━━━━━━━━━━━━┘
    (golden border)
```

**Changes from normal**:
- Border: 4px solid, #fbbf24 (golden yellow)
- Border style: solid (not dashed)
- Shadow: 0 6px 12px rgba(251, 191, 36, 0.4)

### Update Available State

```
┌─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─┐
│  🔗                    [↻]  │  ← Update badge
│                              │
│    Library Ontology          │
│                              │
│  ┌──────────────────────┐   │
│  │   42 concepts        │   │
│  └──────────────────────┘   │
└─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─┘
```

**Changes from normal**:
- Badge: Orange refresh icon in top-right
- Border: Animated pulsing (#f59e0b)
- Animation: 2s pulse cycle

### Expanded State

```
┌─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─┐
│  🔗                               [−]     │
│                                           │
│    Library Ontology                       │
│                                           │
│  ┌─────────────────────────────────────┐ │
│  │   ○ Book                            │ │ ← Virtualized
│  │   ○ Author                          │ │   concepts
│  │   ○ Publisher                       │ │
│  └─────────────────────────────────────┘ │
└─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─┘
```

**Changes from normal**:
- Height: Expands to fit contained concepts
- Background: Semi-transparent (#9333ea with 20% opacity)
- Collapse button: [−] in top-right
- Children: Virtualized concept nodes inside

### Permission Denied State

```
┌─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─┐
│  🔒                          │  ← Lock icon
│                              │
│    Library Ontology          │
│                              │
│  ┌──────────────────────┐   │
│  │  Access Revoked      │   │
│  └──────────────────────┘   │
└─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─┘
```

**Changes from normal**:
- Icon: 🔒 instead of 🔗
- Background: Grayscale (#6b7280 → #4b5563)
- Badge: "Access Revoked" in red
- Opacity: 0.6

---

## Virtualized Concept Nodes

### Normal State

```
    ┆┄┄┄┄┄┄┄┄┄┄┆
    ┆          ┆
    ┆   Book   ┆  🔗
    ┆          ┆
    ┆┄┄┄┄┄┄┄┄┄┄┆
```

**Visual Properties**:
- Shape: Circle (same as regular concepts)
- Size: 60px diameter
- Background: Lighter version of concept color (opacity: 0.7)
- Border: 2px dotted (not solid)
- Icon: Small 🔗 in top-right corner
- Label: Concept name (14px)
- Cursor: pointer (clickable)

### Hover State

```
    ┆┄┄┄┄┄┄┄┄┄┄┆
    ┆          ┆
    ┆   Book   ┆  🔗
    ┆          ┆
    ┆┄┄┄┄┄┄┄┄┄┄┆
   (shows tooltip)
```

**Tooltip Content**:
```
Book
From: Library Ontology
Click to view details
[View in source ontology →]
```

---

## Context Menu

### For Ontology Link Node

```
┌─────────────────────────────┐
│ ○ Expand / Collapse         │
├─────────────────────────────┤
│ ↻ Refresh                   │
│ 👁 View Source Ontology      │
├─────────────────────────────┤
│ ✂ Unlink                    │
└─────────────────────────────┘
```

### For Virtualized Concept

```
┌─────────────────────────────┐
│ 👁 View Details (Read-only)  │
│ 🔗 View in Source Ontology   │
├─────────────────────────────┤
│ ➕ Add Relationship          │
└─────────────────────────────┘
```

---

## Link Ontology Dialog

```
┌───────────────────────────────────────────────────────┐
│ Link Ontology                                    [×]  │
├───────────────────────────────────────────────────────┤
│                                                       │
│  🔍 [Search ontologies...                        ]   │
│                                                       │
│  ┌─────────────────────────────────────────────────┐ │
│  │  ┌─────────────────────────────────────────┐   │ │
│  │  │ Library Ontology                  ✓Can│   │ │
│  │  │ A system for organizing books          │   │ │
│  │  │ 42 concepts • Updated 2 days ago        │   │ │
│  │  └─────────────────────────────────────────┘   │ │
│  │                                                 │ │
│  │  ┌─────────────────────────────────────────┐   │ │
│  │  │ Music Ontology                    ✓Can│   │ │
│  │  │ Concepts for music theory              │   │ │
│  │  │ 58 concepts • Updated 1 week ago        │   │ │
│  │  └─────────────────────────────────────────┘   │ │
│  │                                                 │ │
│  │  ┌─────────────────────────────────────────┐   │ │
│  │  │ Chemistry Ontology                🔒    │   │ │
│  │  │ Chemical compounds and reactions        │   │ │
│  │  │ 127 concepts • No access                │   │ │
│  │  └─────────────────────────────────────────┘   │ │
│  └─────────────────────────────────────────────────┘ │
│                                                       │
├───────────────────────────────────────────────────────┤
│                             [Cancel]  [Link Ontology] │
└───────────────────────────────────────────────────────┘
```

**Design Notes**:
- Search bar at top with instant filtering
- Scrollable list of accessible ontologies
- Card-based layout with hover effects
- Permission indicator: ✓ Can view / 🔒 No access
- Metadata: concept count, last updated
- Selected card has blue border
- Link button disabled if no access or no selection

---

## Concept Details (Read-Only Mode)

When clicking a virtualized concept:

```
┌───────────────────────────────────────────────────────┐
│ Book (Virtualized)                               [×]  │
├───────────────────────────────────────────────────────┤
│                                                       │
│  🔗 From: Library Ontology                            │
│  ┌─────────────────────────────────────────────────┐ │
│  │ This concept is from a linked ontology and      │ │
│  │ cannot be edited here.                          │ │
│  │ [View in Source Ontology →]                     │ │
│  └─────────────────────────────────────────────────┘ │
│                                                       │
│  Definition:                                          │
│  A written or printed work consisting of pages...    │
│                                                       │
│  Properties:                                          │
│  • title: string (required)                           │
│  • author: Person (object property)                   │
│  • publicationYear: integer                           │
│                                                       │
│  Relationships:                                       │
│  • isPartOf → Collection                              │
│  • writtenBy → Author                                 │
│                                                       │
├───────────────────────────────────────────────────────┤
│                                           [Close]     │
└───────────────────────────────────────────────────────┘
```

**Design Notes**:
- Banner at top indicating it's virtualized
- Blue info box explaining read-only nature
- Link to view in source ontology
- All fields grayed out (disabled state)
- No edit/delete buttons
- Close button only (no save)

---

## Toolbar Button

Add to ontology toolbar:

```
[+ Concept] [+ Relationship] [+ Individual] [🔗 Link Ontology]
                                              ^^^^^^^^^^^^^^^^
                                              (new button)
```

**Button Style**:
- Icon: 🔗 or diagram-3 Bootstrap icon
- Label: "Link Ontology"
- Style: outline-primary (matches other toolbar buttons)
- Tooltip: "Insert another ontology as a node"

---

## Notification Toasts

### Success

```
┌─────────────────────────────────────┐
│ ✓ Linked Library Ontology           │
│   Added to graph successfully       │
└─────────────────────────────────────┘
```

### Update Available

```
┌─────────────────────────────────────┐
│ ↻ Update Available                  │
│   Library Ontology has changed      │
│   [Refresh Now]                     │
└─────────────────────────────────────┘
```

### Error

```
┌─────────────────────────────────────┐
│ ✗ Cannot Link Ontology              │
│   Would create circular dependency  │
└─────────────────────────────────────┘
```

---

## Color Palette

### Primary Colors (Virtualized Nodes)

- **Purple Primary**: #9333ea (main background start)
- **Purple Secondary**: #7c3aed (gradient end)
- **Purple Dark**: #6b21a8 (border)
- **Purple Darker**: #4c1d95 (text outline)

### Accent Colors

- **Golden Yellow**: #fbbf24 (selected state)
- **Orange**: #f59e0b (update indicator)
- **Red**: #ef4444 (error, no access)
- **Green**: #10b981 (success)

### Neutral Colors

- **Gray Light**: #f3f4f6 (disabled backgrounds)
- **Gray**: #6b7280 (disabled borders)
- **Gray Dark**: #374151 (disabled text)

---

## Animations

### Pulse (Update Available)

```css
@keyframes pulse {
  0%, 100% {
    opacity: 1;
    border-color: #f59e0b;
  }
  50% {
    opacity: 0.7;
    border-color: #fb923c;
  }
}
```

Duration: 2s
Iteration: infinite
Easing: ease-in-out

### Expand

```css
@keyframes expand {
  from {
    height: 90px;
    opacity: 1;
  }
  to {
    height: auto;
    opacity: 0.2;
  }
}
```

Duration: 300ms
Easing: ease-out

### Hover Lift

```css
@keyframes lift {
  from {
    transform: translateY(0);
    box-shadow: 0 4px 8px rgba(0,0,0,0.1);
  }
  to {
    transform: translateY(-2px);
    box-shadow: 0 6px 12px rgba(0,0,0,0.15);
  }
}
```

Duration: 150ms
Easing: ease-out

---

## Responsive Behavior

### Desktop (>768px)

- Ontology nodes: 140px x 90px
- Concept nodes: 60px diameter
- Dialog: 600px width, centered
- Context menu: Full options

### Mobile (<768px)

- Ontology nodes: 120px x 75px (smaller)
- Concept nodes: 50px diameter
- Dialog: Full screen width with padding
- Context menu: Bottom sheet instead of dropdown
- Touch-friendly tap targets (min 44px)

---

## Accessibility

### Color Blind Considerations

- Don't rely solely on color for state
- Use icons + color: 🔗 for linked, 🔒 for denied
- Patterns: dashed border for linked, dotted for virtualized

### Keyboard Navigation

- Tab through dialog options
- Enter to select
- Escape to close
- Arrow keys to navigate list

### Screen Readers

- ARIA labels: "Virtualized ontology node: Library Ontology, 42 concepts"
- ARIA live regions for update notifications
- Alt text for all icons

---

## Dark Mode Support

All colors have dark mode variants:

| Element | Light Mode | Dark Mode |
|---------|------------|-----------|
| Purple background | #9333ea | #7c3aed |
| Border | #6b21a8 | #9333ea |
| Text | #ffffff | #f3f4f6 |
| Dialog background | #ffffff | #1f2937 |
| Card background | #f9fafb | #111827 |

Use CSS custom properties for easy switching:

```css
:root {
  --virtualized-bg: #9333ea;
  --virtualized-border: #6b21a8;
}

[data-theme="dark"] {
  --virtualized-bg: #7c3aed;
  --virtualized-border: #9333ea;
}
```
