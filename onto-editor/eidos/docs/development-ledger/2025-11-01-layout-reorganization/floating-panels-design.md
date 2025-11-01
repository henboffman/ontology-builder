# Floating Panels Design - Add/Edit Concept & Relationship

**Date**: November 1, 2025
**Feature**: Advanced floating panels for add/edit operations
**Status**: 📋 Design Phase

## Problem Statement

**Current Implementation**:
- Add/Edit Concept panel appears in right sidebar (col-md-3)
- Add/Edit Relationship panel appears in right sidebar (col-md-3)
- Limited space (25% of screen width)
- Blocks view of selected item details
- User must scroll to see all form fields
- Feels cramped on complex forms

**User Vision**:
> "What if they popped up, not quite as a dialog or modal, but as an overlay - almost like a very advanced context menu - and that way they could float over information, maybe even be freely moveable? I want the user experience to be top-tier."

## Design Inspiration

### Modern Examples
- **VS Code Command Palette**: Floating, centered, keyboard-focused
- **Notion Quick Add**: Appears near cursor, context-aware
- **Figma Properties Panel**: Detached, moveable, stays on top
- **Chrome DevTools**: Dockable, resizable, remembers position
- **Linear Issue Create**: Smooth slide-in, keyboard shortcuts, instant

### Key Characteristics for "Top-Tier UX"
✅ **Non-blocking**: Can see content behind panel
✅ **Contextual**: Appears near relevant content
✅ **Moveable**: Drag to reposition
✅ **Resizable**: Adjust height/width as needed (optional)
✅ **Keyboard-first**: Tab navigation, shortcuts, quick actions
✅ **Smart positioning**: Avoids covering important info
✅ **Persistent state**: Remembers size/position
✅ **Smooth animations**: Slide in, fade in, no jarring transitions
✅ **Escape to close**: Intuitive dismissal
✅ **Auto-focus**: First input field focused on open

## Proposed Design: Floating Action Panel

### Visual Concept

```
┌─────────────────────────────────────────────────────────────┐
│ OntologyView - Normal content visible                       │
│                                                              │
│   Concept List                                              │
│   ├─ Person                  ┌──────────────────────────┐   │
│   ├─ Animal                  │ ✨ Add New Concept       │   │
│   └─ Product                 ├──────────────────────────┤   │
│                              │ Name: [____________]     │   │
│   Relationship List          │                          │   │
│   ├─ Person → knows → Animal │ Definition:              │   │
│                              │ [___________________]    │   │
│                              │ [___________________]    │   │
│                              │                          │   │
│                              │ Category: [_________]    │   │
│                              │                          │   │
│                              │ Color: 🎨               │   │
│                              │                          │   │
│                              │ [Cancel]  [Save & Close] │   │
│                              │          [Save & Add ⏎]  │   │
│                              └──────────────────────────┘   │
│                                     ↑ Draggable              │
│                                     ↑ Appears with animation │
└─────────────────────────────────────────────────────────────┘
```

### Features

**1. Floating Overlay**
- Position: Starts centered or near triggering element
- Z-index: Above content, below modals
- Backdrop: Semi-transparent (10% black), allows interaction with background
- Shadow: Prominent drop shadow for depth

**2. Draggable**
- Drag handle: Top bar with title
- Cursor changes to `grab` on hover, `grabbing` when dragging
- Snaps to edges when near (optional)
- Constrained within viewport (doesn't go offscreen)

**3. Intelligent Positioning**
- **Default**: Centered vertically, 60% from left
- **Near context**: If triggered from list item, appears near that item
- **Avoid overlap**: Doesn't cover validation panel or selected item
- **Remembers position**: localStorage saves last position per panel type

**4. Smooth Animations**
- **Open**: Fade in + subtle scale (0.95 → 1.0) over 200ms
- **Close**: Fade out + scale (1.0 → 0.95) over 150ms
- **Drag**: Smooth CSS transitions
- **Respects `prefers-reduced-motion`**

**5. Keyboard Interactions**
- `Escape`: Close panel
- `Ctrl/Cmd + Enter`: Save & Close
- `Ctrl/Cmd + Shift + Enter`: Save & Add Another
- `Tab`: Natural tab order through fields
- `Ctrl/Cmd + K`: Quick search for existing concepts (when adding relationship)

**6. Size Options**
- **Compact**: 350px width, auto height (default for simple forms)
- **Standard**: 450px width, auto height (default for concept/relationship)
- **Expanded**: 600px width, auto height (for bulk create or complex forms)
- **Resizable**: Drag bottom-right corner (optional enhancement)

**7. Smart Field Behaviors**
- Auto-focus first field on open
- Live validation (red borders, error messages)
- Autocomplete for categories, relationship types
- Color picker with recent colors
- Template selection dropdown
- "Save & Add Another" keeps panel open, clears fields

**8. Visual Design**
- **Border**: 1px solid border with theme color
- **Background**: Solid white (light mode) or dark gray (dark mode)
- **Header**: Colored accent bar (primary color)
- **Buttons**: Right-aligned, clear hierarchy (Cancel secondary, Save primary)
- **Fields**: Material Design style with floating labels (optional)

## Implementation Architecture

### Component Structure

```
FloatingPanel.razor (New - Reusable container)
├── Props:
│   ├── Title: string
│   ├── IsVisible: bool
│   ├── OnClose: EventCallback
│   ├── DefaultPosition: { x, y }
│   ├── Size: enum (Compact, Standard, Expanded)
│   ├── IsDraggable: bool = true
│   ├── ShowBackdrop: bool = true
│   └── ChildContent: RenderFragment
├── State:
│   ├── currentPosition: { x, y }
│   ├── isDragging: bool
│   └── dragOffset: { x, y }
└── Behaviors:
    ├── HandleDragStart()
    ├── HandleDrag()
    ├── HandleDragEnd()
    ├── HandleEscapeKey()
    ├── SavePosition() → localStorage
    └── LoadPosition() ← localStorage

AddConceptFloatingPanel.razor (New - Specific to concepts)
├── Wraps: FloatingPanel
├── Contains: AddConceptForm (extracted from current sidebar)
└── Handles: Concept-specific logic

EditConceptFloatingPanel.razor (New)
├── Similar to Add, but for editing

AddRelationshipFloatingPanel.razor (New)
├── Wraps: FloatingPanel
├── Contains: AddRelationshipForm
└── Handles: Relationship-specific logic

EditRelationshipFloatingPanel.razor (New)
├── Similar to Add, but for editing
```

### JavaScript Interop (wwwroot/js/floating-panel.js)

```javascript
/**
 * Floating Panel Helper Functions
 */

class FloatingPanelManager {
    constructor() {
        this.panels = new Map();
    }

    /**
     * Initialize a draggable floating panel
     * @param {string} panelId - Unique panel identifier
     * @param {object} options - Configuration options
     */
    initializePanel(panelId, options = {}) {
        const panel = document.getElementById(panelId);
        if (!panel) return;

        const header = panel.querySelector('.floating-panel-header');
        if (!header) return;

        let isDragging = false;
        let currentX, currentY, initialX, initialY;
        let xOffset = 0, yOffset = 0;

        // Load saved position
        const savedPosition = this.loadPosition(panelId);
        if (savedPosition) {
            panel.style.transform = `translate3d(${savedPosition.x}px, ${savedPosition.y}px, 0)`;
            xOffset = savedPosition.x;
            yOffset = savedPosition.y;
        }

        header.addEventListener('mousedown', dragStart);
        document.addEventListener('mousemove', drag);
        document.addEventListener('mouseup', dragEnd);

        function dragStart(e) {
            initialX = e.clientX - xOffset;
            initialY = e.clientY - yOffset;

            if (e.target === header || header.contains(e.target)) {
                isDragging = true;
                panel.classList.add('dragging');
            }
        }

        function drag(e) {
            if (!isDragging) return;

            e.preventDefault();
            currentX = e.clientX - initialX;
            currentY = e.clientY - initialY;

            xOffset = currentX;
            yOffset = currentY;

            // Constrain within viewport
            const bounds = panel.getBoundingClientRect();
            const maxX = window.innerWidth - bounds.width;
            const maxY = window.innerHeight - bounds.height;

            xOffset = Math.max(0, Math.min(xOffset, maxX));
            yOffset = Math.max(0, Math.min(yOffset, maxY));

            setTranslate(xOffset, yOffset, panel);
        }

        function dragEnd() {
            if (!isDragging) return;

            initialX = currentX;
            initialY = currentY;
            isDragging = false;

            panel.classList.remove('dragging');

            // Save position
            window.floatingPanelManager.savePosition(panelId, {
                x: xOffset,
                y: yOffset
            });
        }

        function setTranslate(xPos, yPos, el) {
            el.style.transform = `translate3d(${xPos}px, ${yPos}px, 0)`;
        }

        this.panels.set(panelId, { panel, header, cleanup: () => {
            header.removeEventListener('mousedown', dragStart);
            document.removeEventListener('mousemove', drag);
            document.removeEventListener('mouseup', dragEnd);
        }});
    }

    /**
     * Save panel position to localStorage
     */
    savePosition(panelId, position) {
        localStorage.setItem(`floating-panel-${panelId}`, JSON.stringify(position));
    }

    /**
     * Load panel position from localStorage
     */
    loadPosition(panelId) {
        const saved = localStorage.getItem(`floating-panel-${panelId}`);
        return saved ? JSON.parse(saved) : null;
    }

    /**
     * Destroy panel instance
     */
    destroyPanel(panelId) {
        const panelData = this.panels.get(panelId);
        if (panelData) {
            panelData.cleanup();
            this.panels.delete(panelId);
        }
    }

    /**
     * Center panel in viewport
     */
    centerPanel(panelId) {
        const panel = document.getElementById(panelId);
        if (!panel) return;

        const bounds = panel.getBoundingClientRect();
        const x = (window.innerWidth - bounds.width) / 2;
        const y = (window.innerHeight - bounds.height) / 2;

        panel.style.transform = `translate3d(${x}px, ${y}px, 0)`;
        this.savePosition(panelId, { x, y });
    }
}

// Global instance
window.floatingPanelManager = new FloatingPanelManager();

/**
 * Blazor interop methods
 */
window.initializeFloatingPanel = function(panelId, options) {
    window.floatingPanelManager.initializePanel(panelId, options);
};

window.destroyFloatingPanel = function(panelId) {
    window.floatingPanelManager.destroyPanel(panelId);
};

window.centerFloatingPanel = function(panelId) {
    window.floatingPanelManager.centerPanel(panelId);
};
```

### CSS Styling (wwwroot/css/floating-panel.css)

```css
/* Floating Panel Container */
.floating-panel {
    position: fixed;
    z-index: 1040; /* Below modals (1050), above content */
    background: var(--bs-body-bg);
    border: 1px solid var(--bs-border-color);
    border-radius: 8px;
    box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15),
                0 2px 8px rgba(0, 0, 0, 0.1);
    overflow: hidden;
    opacity: 0;
    transform: translate3d(-50%, -50%, 0) scale(0.95);
    transition: opacity 200ms ease-out, transform 200ms ease-out;
    max-height: 90vh;
    display: flex;
    flex-direction: column;
}

/* Visible state */
.floating-panel.visible {
    opacity: 1;
    transform: translate3d(-50%, -50%, 0) scale(1);
}

/* Dragging state */
.floating-panel.dragging {
    cursor: grabbing;
    transition: none; /* Disable transitions while dragging */
}

/* Backdrop */
.floating-panel-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    z-index: 1039;
    background: rgba(0, 0, 0, 0.1);
    opacity: 0;
    transition: opacity 200ms ease-out;
    pointer-events: none; /* Allow clicks through backdrop */
}

.floating-panel-backdrop.visible {
    opacity: 1;
}

/* Header (Drag Handle) */
.floating-panel-header {
    padding: 1rem;
    background: linear-gradient(135deg, var(--bs-primary), var(--bs-primary-dark, var(--bs-primary)));
    color: white;
    cursor: grab;
    user-select: none;
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.floating-panel-header:active {
    cursor: grabbing;
}

.floating-panel-header h5 {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.floating-panel-header .close-btn {
    background: rgba(255, 255, 255, 0.2);
    border: none;
    color: white;
    width: 28px;
    height: 28px;
    border-radius: 4px;
    cursor: pointer;
    transition: background 150ms;
    display: flex;
    align-items: center;
    justify-content: center;
}

.floating-panel-header .close-btn:hover {
    background: rgba(255, 255, 255, 0.3);
}

/* Body */
.floating-panel-body {
    padding: 1.5rem;
    overflow-y: auto;
    flex: 1;
}

/* Footer */
.floating-panel-footer {
    padding: 1rem 1.5rem;
    border-top: 1px solid var(--bs-border-color);
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
    background: var(--bs-light);
}

/* Size Variants */
.floating-panel.size-compact {
    width: 350px;
}

.floating-panel.size-standard {
    width: 450px;
}

.floating-panel.size-expanded {
    width: 600px;
}

/* Responsive: Mobile */
@media (max-width: 768px) {
    .floating-panel {
        width: 90vw !important;
        max-width: 500px;
        left: 50% !important;
        top: 50% !important;
        transform: translate(-50%, -50%) !important;
    }

    .floating-panel-header {
        cursor: default; /* No dragging on mobile */
    }
}

/* Reduced Motion */
@media (prefers-reduced-motion: reduce) {
    .floating-panel,
    .floating-panel-backdrop {
        transition: none;
    }
}

/* Dark Mode */
[data-bs-theme="dark"] .floating-panel {
    box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5),
                0 2px 8px rgba(0, 0, 0, 0.3);
}

[data-bs-theme="dark"] .floating-panel-footer {
    background: var(--bs-dark);
}
```

## Usage Example

### FloatingPanel.razor Component

```razor
@inject IJSRuntime JS

@if (IsVisible)
{
    <div class="floating-panel-backdrop @(IsVisible ? "visible" : "")"
         @onclick="HandleBackdropClick"></div>

    <div id="@PanelId"
         class="floating-panel size-@Size.ToString().ToLower() @(IsVisible ? "visible" : "")"
         @ref="panelElement"
         @onkeydown="HandleKeyDown">

        <div class="floating-panel-header">
            <h5>
                @if (!string.IsNullOrEmpty(Icon))
                {
                    <i class="bi bi-@Icon"></i>
                }
                @Title
            </h5>
            <button class="close-btn" @onclick="Close" title="Close (Esc)">
                <i class="bi bi-x-lg"></i>
            </button>
        </div>

        <div class="floating-panel-body">
            @ChildContent
        </div>

        @if (ShowFooter)
        {
            <div class="floating-panel-footer">
                @FooterContent
            </div>
        }
    </div>
}

@code {
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string Icon { get; set; } = "";
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public PanelSize Size { get; set; } = PanelSize.Standard;
    [Parameter] public bool IsDraggable { get; set; } = true;
    [Parameter] public bool ShowBackdrop { get; set; } = true;
    [Parameter] public bool CloseOnBackdropClick { get; set; } = false;
    [Parameter] public bool ShowFooter { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? FooterContent { get; set; }

    private ElementReference panelElement;
    private string PanelId => $"floating-panel-{Guid.NewGuid()}";

    public enum PanelSize { Compact, Standard, Expanded }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsVisible && IsDraggable)
        {
            await JS.InvokeVoidAsync("initializeFloatingPanel", PanelId, new { });
        }
    }

    private async Task Close()
    {
        await OnClose.InvokeAsync();
        await IsVisibleChanged.InvokeAsync(false);
    }

    private async Task HandleBackdropClick()
    {
        if (CloseOnBackdropClick)
        {
            await Close();
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            await Close();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (IsDraggable)
        {
            await JS.InvokeVoidAsync("destroyFloatingPanel", PanelId);
        }
    }
}
```

### Usage in OntologyView.razor

```razor
<!-- Replace current sidebar add concept panel with floating panel -->
<AddConceptFloatingPanel IsVisible="@showAddConcept"
                        OntologyId="@Id"
                        OnClose="@(() => showAddConcept = false)"
                        OnConceptAdded="@HandleConceptAdded" />

<AddRelationshipFloatingPanel IsVisible="@showAddRelationship"
                             OntologyId="@Id"
                             Concepts="@ontology.Concepts"
                             OnClose="@(() => showAddRelationship = false)"
                             OnRelationshipAdded="@HandleRelationshipAdded" />
```

## User Experience Flow

### Scenario 1: Adding a Concept

1. **User clicks "Add Concept" button** in List view
2. **Panel slides in** from center with fade animation (200ms)
3. **First field auto-focuses** (Name field)
4. **User types** concept name
5. **User tabs** through Definition, Category, Color fields
6. **Live validation** shows errors in real-time
7. **User presses `Ctrl+Enter`** to save & close
   - OR clicks "Save & Add Another" to keep panel open
8. **Panel closes** with fade-out animation (150ms)
9. **New concept appears** in list with highlight
10. **Toast notification** confirms creation

### Scenario 2: Editing a Relationship (Drag & Reposition)

1. **User clicks edit icon** on relationship in list
2. **Panel appears** near the relationship item
3. **User drags panel** by header to better position
4. **Panel snaps** to avoid covering selected item
5. **User makes edits** while seeing context behind panel
6. **User presses `Escape`** to cancel changes
7. **Panel closes** without saving

### Scenario 3: Bulk Adding (Save & Add Another)

1. **User clicks "Add Concept"**
2. **Panel opens**, user fills form
3. **User clicks "Save & Add Another"** (or `Ctrl+Shift+Enter`)
4. **Panel stays open**, fields clear, focus returns to Name
5. **User adds second concept** quickly
6. **Repeat** for third, fourth concept
7. **User presses `Escape`** to finish
8. **Panel closes**, all concepts visible in list

## Benefits Over Current Design

### Current (Sidebar Panel)
- ❌ Fixed position (right sidebar)
- ❌ Limited width (25% screen)
- ❌ Blocks selected item details
- ❌ Requires scrolling for long forms
- ❌ Can't see context while editing
- ❌ Feels constrained

### Proposed (Floating Panel)
- ✅ Flexible positioning (drag anywhere)
- ✅ Optimal width (450px default, adjustable)
- ✅ Non-blocking (see content behind)
- ✅ Compact height (auto, scrollable body)
- ✅ Context visible while working
- ✅ Feels modern and powerful

## Advanced Features (Phase 2 Enhancements)

### 1. Resizable Panels
- Drag corner to resize width/height
- Remember size preference per panel type
- Snap to predefined sizes (Compact, Standard, Expanded)

### 2. Multi-Panel Support
- Multiple panels open simultaneously
- Z-index management (click to bring to front)
- Cascade positioning when opening multiple

### 3. Smart Positioning
- Appear near triggering element (e.g., clicked list item)
- Avoid covering important UI (validation panel, selected item)
- Magnetic edges (snap to viewport edges)

### 4. Keyboard Shortcuts
- `Ctrl+N`: New Concept
- `Ctrl+Shift+N`: New Relationship
- `Ctrl+E`: Edit selected item
- `Ctrl+D`: Duplicate selected item

### 5. Templates & Quick Actions
- Template dropdown in panel header
- Quick-apply common patterns
- "Duplicate & Edit" opens panel with pre-filled fields

### 6. Minimization
- Minimize panel to tab at bottom of screen
- Click tab to restore
- Multiple minimized panels stack horizontally

## Accessibility Considerations

✅ **Keyboard Navigation**: Full keyboard support (Tab, Escape, Enter)
✅ **Focus Management**: Auto-focus first field, trap focus in panel
✅ **ARIA Labels**: `role="dialog"`, `aria-labelledby`, `aria-modal="true"`
✅ **Screen Readers**: Announce panel open/close, field errors
✅ **Reduced Motion**: Respects user preference, instant show/hide
✅ **Color Contrast**: Meets WCAG AA standards

## Implementation Checklist

### Phase 1: Basic Floating Panel
- [ ] Create `FloatingPanel.razor` base component
- [ ] Create `wwwroot/js/floating-panel.js` with drag logic
- [ ] Create `wwwroot/css/floating-panel.css` with animations
- [ ] Add to `App.razor` scripts
- [ ] Test drag functionality
- [ ] Test keyboard interactions (Escape to close)
- [ ] Test responsive behavior (mobile centered, not draggable)

### Phase 2: Concept & Relationship Panels
- [ ] Create `AddConceptFloatingPanel.razor`
- [ ] Create `EditConceptFloatingPanel.razor`
- [ ] Create `AddRelationshipFloatingPanel.razor`
- [ ] Create `EditRelationshipFloatingPanel.razor`
- [ ] Extract form content from current sidebar components
- [ ] Wire up save/cancel/close events
- [ ] Test "Save & Add Another" workflow

### Phase 3: Integration & Polish
- [ ] Replace sidebar panels with floating panels in `OntologyView.razor`
- [ ] Remove old sidebar panel code
- [ ] Test all add/edit workflows
- [ ] Test validation error display
- [ ] Add smooth animations
- [ ] Save/load position from localStorage
- [ ] Add keyboard shortcuts
- [ ] Toast notifications for actions

### Phase 4: Advanced Features (Optional)
- [ ] Resizable panels
- [ ] Multi-panel support
- [ ] Smart positioning near clicked item
- [ ] Panel minimization
- [ ] Template quick-apply

## Testing Plan

### Unit Tests
- Drag positioning logic
- Viewport constraint logic
- localStorage save/load

### Integration Tests
- Panel opens/closes correctly
- Form submission works
- Validation displays properly
- Keyboard shortcuts function

### User Testing
- Drag panel to various positions
- Test on different screen sizes
- Mobile experience (no dragging)
- Dark mode appearance
- Accessibility with screen reader

## Success Metrics

✅ **Usability**: Users can add/edit without scrolling or losing context
✅ **Efficiency**: Faster concept/relationship creation (less clicks)
✅ **Satisfaction**: Users prefer floating panels over sidebar (survey)
✅ **Accessibility**: Passes WCAG AA compliance
✅ **Performance**: No lag in drag interactions (<16ms frame time)

---

**Design Status**: ✅ Complete, ready for implementation
**Created By**: Claude Code
**Last Updated**: November 1, 2025
