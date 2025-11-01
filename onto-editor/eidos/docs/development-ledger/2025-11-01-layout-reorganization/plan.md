# OntologyView Layout Reorganization - Plan

**Date**: November 1, 2025
**Feature**: Maximize screen real estate with persistent validation panel and improved navigation
**Status**: 📋 Planning Phase

## Executive Summary

Reorganize the OntologyView layout to better utilize screen space by:
1. Moving view mode selector to a right sidebar (vertical tabs/navigation)
2. Integrating Concepts/Relationships tabs within List view
3. Making validation panel persistent at bottom 30% of viewport
4. Maintaining all ontology controls (Import, Export, Settings, Share, etc.)
5. Improving detail panel visibility for selected concepts/relationships

## Problem Statement

**Current Issues**:
- Validation panel competes for horizontal space when showing issues
- Concepts and Relationships sections in List view occupy full height, requiring scrolling
- Selected concept/relationship details panel is in right sidebar (col-md-3), limiting detail visibility
- User must close/minimize validation panel to see more content
- Limited vertical screen real estate utilization

**User Feedback**:
> "The problem panel works great. I feel like we aren't making the best use of our screen real estate... [we should have] the problems panel always visible as the bottom 30% of the screen real estate, and the selected concepts or relationships panel occupies the remaining viewport."

## Current Layout Analysis

### Structure Overview

```
OntologyView.razor (current)
├── Container-fluid
│   ├── Permission Banner (conditional)
│   ├── Keyboard Shortcuts Banner (conditional)
│   ├── OntologyHeader (name, description, action buttons)
│   ├── ViewModeSelector (horizontal tabs: Graph, List, Hierarchy, etc.)
│   ├── [Dialogs: Import, Export, Settings, BulkCreate, etc.]
│   └── Row ontology-layout
│       ├── Col-12 col-md-9 (Main Content)
│       │   ├── ValidationPanel (when issues exist)
│       │   └── [Current View based on viewMode]
│       │       ├── GraphView
│       │       ├── ListView (Concepts + Relationships)
│       │       ├── HierarchyView
│       │       ├── InstancesView
│       │       ├── TtlView
│       │       ├── NotesView
│       │       ├── TemplatesView
│       │       ├── LinksView
│       │       ├── CollaboratorsView
│       │       ├── VersionHistoryView
│       │       └── HelpView
│       └── Col-12 col-md-3 (Sidebar - conditional)
│           ├── Selected Concept Details
│           ├── Selected Relationship Details
│           ├── Add Concept Form
│           ├── Add Relationship Form
│           └── Add Individual Form
```

**Key Components**:
- **OntologyHeader.razor**: Ontology metadata, action buttons (Import, Export, Share, Settings, Fork, Clone, Lineage)
- **ViewModeSelector.razor**: Horizontal tabs for switching views, includes Undo/Redo, Presence Users
- **ValidationPanel.razor**: Collapsible panel showing validation issues
- **ListView.razor**: Shows Concepts card + Relationships card stacked vertically

### Current Responsive Behavior

- **Desktop (md+)**: 9-column main content, 3-column sidebar
- **Mobile/Tablet**: Full-width main content, sidebar appears below
- **OntologyHeader**: Desktop shows all buttons, mobile shows dropdown menu

## Proposed Layout

### Visual Structure

```
┌─────────────────────────────────────────────────────────────────┐
│ OntologyHeader (ontology name, description, action buttons)    │
│ [Import] [Export] [Share] [Settings] [Fork] [Clone] [Lineage]  │
└─────────────────────────────────────────────────────────────────┘
┌──────────────────────────────────────────┬──────────────────────┐
│                                          │  View Selector       │
│                                          │  ┌────────────────┐  │
│                                          │  │ 📊 Graph       │  │
│                                          │  │ 📋 List        │  │
│  Main Content Area (70% height)         │  │ 🌳 Hierarchy   │  │
│  ────────────────────────────────────    │  │ 👤 Instances   │  │
│                                          │  │ 📝 TTL         │  │
│  When viewMode = List:                  │  │ 📄 Notes       │  │
│    ┌─────────────────────────────────┐  │  │ 📑 Templates   │  │
│    │ Tabs: [Concepts] [Relationships]│  │  │ 🔗 Links       │  │
│    ├─────────────────────────────────┤  │  │ 👥 Collaborator│  │
│    │ - Concept 1 (with validation)   │  │  │ ⏱️  History     │  │
│    │ - Concept 2                      │  │  │ ❓ Help        │  │
│    │ - Concept 3 (with validation)   │  │  └────────────────┘  │
│    │ [Selected Concept Details →]    │  │                      │
│    └─────────────────────────────────┘  │  Undo/Redo           │
│                                          │  ┌────────────────┐  │
│  Other views (Graph, Hierarchy, etc.)   │  │ ↶ Undo         │  │
│  render in full main content area       │  │ ↷ Redo         │  │
│                                          │  └────────────────┘  │
│                                          │                      │
│                                          │  Presence Users      │
│                                          │  ┌────────────────┐  │
│                                          │  │ 👤 User 1      │  │
│                                          │  │ 👤 User 2      │  │
│                                          │  └────────────────┘  │
├──────────────────────────────────────────┴──────────────────────┤
│ Validation Panel (30% height, always visible, collapsible)      │
│ ┌──────────────────────────────────────────────────────────────┐│
│ │ [v] Problems  2 errors  3 warnings  [Refresh]  5m ago       ││
│ │ ├─ [X] Duplicate concept: 'Person' → Concept #42            ││
│ │ ├─ [!] Orphaned concept: 'Animal' → Concept #15             ││
│ │ └─ [i] Missing description: 'Product' → Concept #28         ││
│ └──────────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────────┘
```

### Key Changes

1. **Right Sidebar for View Selection** (200-250px width)
   - Vertical list/stack of view mode buttons
   - Undo/Redo controls
   - Presence users section
   - Always visible on desktop, collapsible on mobile

2. **Bottom Persistent Validation Panel** (30% viewport height)
   - Always rendered (not conditional)
   - Shows "No issues" when validation passes
   - Collapsible to give more space when needed
   - Resizable divider (optional enhancement)

3. **Enhanced List View with Tabs**
   - Internal tabs for Concepts vs Relationships
   - Each tab shows its respective items + selected item details
   - Validation indicators on list items (already implemented)
   - Full vertical space utilization

4. **Main Content Area** (70% viewport height, ~80% width)
   - More horizontal space (no longer sharing with sidebar)
   - Height allocated above validation panel
   - Current view renders here based on viewMode

## Architectural Approach

### Component Hierarchy Changes

```
OntologyView.razor (modified)
├── OntologyHeader.razor (unchanged)
├── Main Layout Grid
│   ├── Left: Main Content Area (80% width, 70% height)
│   │   └── ViewRenderer Component (new or inline)
│   │       ├── GraphView
│   │       ├── ListViewTabs (new - wraps ListView)
│   │       │   ├── Tab: Concepts
│   │       │   │   ├── Concept List (from ListView)
│   │       │   │   └── Selected Concept Details
│   │       │   └── Tab: Relationships
│   │       │       ├── Relationship List (from ListView)
│   │       │       └── Selected Relationship Details
│   │       ├── HierarchyView
│   │       └── [Other views...]
│   ├── Right: Vertical Navigation Sidebar (20% width, 70% height)
│   │   ├── ViewModeNav (new - vertical buttons)
│   │   ├── UndoRedoControls (extracted from ViewModeSelector)
│   │   └── PresenceSection (extracted from ViewModeSelector)
│   └── Bottom: ValidationPanel (100% width, 30% height)
│       └── ValidationPanel.razor (modified for persistent rendering)
```

### New Components to Create

1. **`Components/Ontology/ViewModeNav.razor`**
   - Vertical navigation for view modes
   - Icon + label buttons
   - Active state styling
   - Mobile: Collapsible/hamburger menu

2. **`Components/Ontology/ListViewTabs.razor`**
   - Wraps ListView component
   - Provides internal tabs for Concepts/Relationships
   - Manages selected item details display
   - Responsive behavior

3. **`Components/Ontology/UndoRedoControls.razor`** (optional extraction)
   - Undo/Redo buttons
   - Reusable across layouts

### Components to Modify

1. **`Components/Pages/OntologyView.razor`**
   - Change from row/col Bootstrap grid to custom layout
   - Remove conditional sidebar (col-md-3)
   - Make ValidationPanel always rendered
   - Integrate new ViewModeNav

2. **`Components/Ontology/ViewModeSelector.razor`**
   - Deprecate (functionality split into ViewModeNav + UndoRedoControls)
   - OR refactor to be the new ViewModeNav

3. **`Components/Ontology/ValidationPanel.razor`**
   - Add "No issues" state when ValidationResult has no issues
   - Make always-visible friendly (not just conditional rendering)
   - Consider resizable functionality

4. **`Components/Ontology/ListView.razor`**
   - Extract concepts rendering into separate method/section
   - Extract relationships rendering into separate method/section
   - Will be wrapped by ListViewTabs component

### CSS/Styling Strategy

**New CSS Classes** (OntologyView.razor.css or app.css):

```css
/* Main Layout */
.ontology-layout-redesign {
    display: grid;
    grid-template-columns: 1fr 250px; /* Main + Sidebar */
    grid-template-rows: 70vh 30vh; /* Content + Validation */
    gap: 1rem;
    height: calc(100vh - 200px); /* Account for header */
}

.ontology-main-content-area {
    grid-column: 1;
    grid-row: 1;
    overflow-y: auto;
}

.ontology-sidebar-nav {
    grid-column: 2;
    grid-row: 1;
    overflow-y: auto;
    border-left: 1px solid var(--bs-border-color);
    padding: 1rem;
}

.ontology-validation-panel-persistent {
    grid-column: 1 / 3; /* Span both columns */
    grid-row: 2;
    overflow-y: auto;
    border-top: 2px solid var(--bs-border-color);
}

/* Vertical Navigation */
.view-mode-nav-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem;
    border-radius: 0.375rem;
    cursor: pointer;
    transition: background-color 0.2s;
}

.view-mode-nav-item:hover {
    background-color: var(--bs-light);
}

.view-mode-nav-item.active {
    background-color: var(--bs-primary);
    color: white;
}

/* Responsive: Mobile */
@media (max-width: 768px) {
    .ontology-layout-redesign {
        grid-template-columns: 1fr; /* Single column */
        grid-template-rows: auto 30vh; /* Content auto-sized, validation fixed */
    }

    .ontology-sidebar-nav {
        grid-column: 1;
        grid-row: 1;
        border-left: none;
        border-bottom: 1px solid var(--bs-border-color);
        /* Show as horizontal scrollable buttons on mobile */
        display: flex;
        overflow-x: auto;
        padding: 0.5rem;
    }

    .ontology-validation-panel-persistent {
        grid-column: 1;
    }
}
```

## Implementation Plan

### Phase 1: Preparation & Component Extraction (2-3 hours)

**Goal**: Extract reusable components without changing layout

**Tasks**:
1. ✅ Create subdirectory: `docs/development-ledger/2025-11-01-layout-reorganization/`
2. ✅ Document current architecture (this file)
3. Create `ViewModeNav.razor` component
   - Extract view mode list from ViewModeSelector
   - Create vertical button layout
   - Wire up OnViewModeChanged event
4. Create `UndoRedoControls.razor` component (optional)
   - Extract undo/redo buttons
   - Wire up callbacks
5. Update ValidationPanel.razor
   - Add "No issues" state rendering
   - Ensure works when always rendered

**Testing**: Verify components work in isolation, no layout changes yet

### Phase 2: Layout Grid Implementation (3-4 hours)

**Goal**: Implement new CSS Grid layout structure

**Tasks**:
1. Create `OntologyView.razor.css` with new grid classes
2. Modify `OntologyView.razor` layout structure
   - Replace `<div class="row ontology-layout">` with grid layout
   - Add main content area div
   - Add sidebar nav div
   - Add bottom validation panel div
3. Integrate ViewModeNav into sidebar
4. Make ValidationPanel always rendered at bottom
5. Test responsive behavior

**Testing**: Verify all views render correctly, sidebar navigation works, validation panel always visible

### Phase 3: List View Tabs Integration (2-3 hours)

**Goal**: Add internal tabs to List view for Concepts/Relationships

**Tasks**:
1. Create `ListViewTabs.razor` component
   - Bootstrap tabs for Concepts/Relationships
   - Pass through ListView component with filtering
   - Integrate selected item details
2. Modify ListView.razor (if needed)
   - Separate rendering logic for concepts vs relationships
   - Expose as parameters or methods
3. Wire up ListViewTabs in OntologyView
4. Test tab switching, selection, validation indicators

**Testing**: Verify tabs work, selection persists, validation indicators show correctly

### Phase 4: Responsive & Polish (2-3 hours)

**Goal**: Mobile experience, edge cases, polish

**Tasks**:
1. Implement mobile sidebar collapse/hamburger
2. Test all view modes in new layout
3. Adjust spacing, padding, borders
4. Verify keyboard navigation
5. Test with validation issues present
6. Verify presence users rendering
7. Check accessibility (ARIA labels, keyboard nav)

**Testing**: Full regression testing, mobile testing, accessibility audit

### Phase 5: Documentation & Deployment (1 hour)

**Goal**: Document changes, update ledger, deploy

**Tasks**:
1. Update `docs/development-ledger/2025-11-01-layout-reorganization/implementation.md`
2. Add screenshots/diagrams
3. Update CLAUDE.md with new layout patterns
4. Create migration notes for other developers
5. Deploy to staging/production

**Total Estimated Time**: 10-14 hours

## Design Decisions & Rationale

### Decision 1: Right Sidebar for View Navigation

**Options Considered**:
- A) Right sidebar (vertical tabs)
- B) Left sidebar (vertical tabs)
- C) Keep horizontal tabs at top
- D) Bottom sidebar

**Choice**: A) Right sidebar

**Rationale**:
- Western reading pattern: eyes finish on right, natural to look for navigation
- Keeps ontology header and main content left-aligned
- Consistent with "details on right" pattern (selected concepts currently on right)
- Mobile can collapse to hamburger menu
- Vertical space better utilized than horizontal tabs

**Trade-offs**:
- Less conventional than left sidebar
- Slightly awkward initially (as user noted)
- BUT: User mentioned right side, shows willingness to try

### Decision 2: 30% Bottom for Validation Panel

**Options Considered**:
- A) 30% bottom (user suggestion)
- B) 20% bottom (less space)
- C) 40% bottom (more space for issues)
- D) Resizable divider

**Choice**: A) 30% bottom (with future consideration for D)

**Rationale**:
- User-specified requirement
- Provides adequate space for 5-10 visible issues without scrolling
- Maintains 70% for main content (good balance)
- Can add resizable divider later as enhancement

**Trade-offs**:
- Fixed percentage may not suit all screen sizes
- Could implement CSS `resize: vertical` or JS drag handle later

### Decision 3: CSS Grid vs Flexbox

**Options Considered**:
- A) CSS Grid
- B) Flexbox
- C) Bootstrap grid system
- D) CSS Grid with Bootstrap as fallback

**Choice**: A) CSS Grid with responsive fallbacks

**Rationale**:
- Grid perfect for 2D layout (columns AND rows)
- Cleaner code than nested flexbox
- Better browser support now (2025)
- Can fall back to flexbox on mobile
- More maintainable for this specific layout

**Trade-offs**:
- Slightly less familiar to some developers
- Need careful responsive design

### Decision 4: List View Tabs (Concepts/Relationships)

**Options Considered**:
- A) Bootstrap nav tabs
- B) Button group toggle
- C) Dropdown selector
- D) Keep stacked cards

**Choice**: A) Bootstrap nav tabs

**Rationale**:
- Consistent with existing app patterns
- Familiar UX (tabs for switching content)
- Clearly shows which section is active
- Space-efficient
- Accessible

**Trade-offs**:
- Adds one extra click to switch views
- User must choose which to view (can't see both simultaneously)
- BUT: Full height for selected tab compensates

## Open Questions & Considerations

### Question 1: Resizable Validation Panel?

**Options**:
- Implement now with JS drag handle
- Implement later as enhancement
- Don't implement (fixed 30%)

**Recommendation**: Implement later as Phase 6 enhancement
- Adds complexity
- User didn't request it
- Can add after validating basic layout

### Question 2: ViewModeSelector - Refactor or Replace?

**Options**:
- Refactor existing component to be vertical
- Create new ViewModeNav component
- Use both (horizontal for mobile, vertical for desktop)

**Recommendation**: Create new ViewModeNav, deprecate ViewModeSelector
- Cleaner separation of concerns
- Easier to maintain
- Can reference old component if needed

### Question 3: Selected Item Details Placement

**Current**: Right sidebar (col-md-3)
**Issue**: Limited space for details

**Options**:
- A) Within tab content (below list)
- B) Slide-out panel from right
- C) Modal dialog
- D) Expand list item inline

**Recommendation**: A) Within tab content below list
- Most space-efficient
- Consistent with "selected item gets focus" pattern
- Can scroll to see full details
- Natural flow: select item → details appear below

### Question 4: Mobile Experience

**Concern**: Complex layout may be difficult on mobile

**Options**:
- Simplify mobile layout significantly
- Keep similar layout with adjustments
- Mobile-first redesign

**Recommendation**: Simplify mobile with horizontal scrollable view selector
- Validation panel stays at bottom (but shorter, 20vh)
- Main content takes full width
- View selector becomes horizontal scrollable buttons at top
- Keep detail panels in tabs

## Success Criteria

### Functional Requirements
- ✅ All ontology controls remain accessible (Import, Export, Settings, Share, etc.)
- ✅ User can select different view modes (Graph, List, Hierarchy, etc.)
- ✅ Validation panel always visible at bottom 30% of viewport
- ✅ List view has separate tabs for Concepts and Relationships
- ✅ Selected concept/relationship details visible with adequate space
- ✅ Validation indicators persist on list items
- ✅ Undo/Redo controls remain accessible
- ✅ Presence users remain visible

### Non-Functional Requirements
- ✅ Responsive on mobile, tablet, desktop
- ✅ Performance: No noticeable lag in rendering
- ✅ Accessibility: Keyboard navigation works, ARIA labels present
- ✅ Visual consistency with existing app design
- ✅ Code maintainability: Clear component structure

### User Experience Goals
- ✅ More vertical space for viewing concepts/relationships
- ✅ Validation issues always visible without closing/opening panel
- ✅ Less scrolling required to see selected item details
- ✅ Intuitive navigation between views
- ✅ No loss of functionality from current layout

## Risks & Mitigation

### Risk 1: User Resistance to Right Sidebar Navigation

**Likelihood**: Medium
**Impact**: Medium

**Mitigation**:
- Provide clear visual affordances (icons + labels)
- Add tooltips
- Include in keyboard shortcuts banner
- Monitor user feedback, be ready to move sidebar if needed

### Risk 2: Mobile Experience Degradation

**Likelihood**: Medium
**Impact**: High

**Mitigation**:
- Test extensively on mobile devices
- Simplify mobile layout appropriately
- Consider progressive disclosure
- Ensure all functions accessible on mobile

### Risk 3: Validation Panel Always Visible May Be Distracting

**Likelihood**: Low
**Impact**: Medium

**Mitigation**:
- Make it collapsible with clear toggle
- Show "No issues" state with green checkmark (positive reinforcement)
- Use subtle colors when collapsed
- Allow user to minimize to thin bar

### Risk 4: Breaking Existing Functionality

**Likelihood**: Medium
**Impact**: High

**Mitigation**:
- Implement incrementally with feature flag (optional)
- Thorough testing of all view modes
- Keep old layout code initially (can switch back if needed)
- Beta test with user before full rollout

## Rollout Strategy

### Option A: Direct Replacement (Recommended)
1. Implement new layout on feature branch
2. Thorough testing
3. Deploy to production
4. Monitor for issues

**Pros**: Clean, no technical debt
**Cons**: Risky if issues found

### Option B: Feature Flag
1. Implement with feature toggle
2. Default to new layout for current user
3. Allow fallback to old layout if issues
4. Remove old layout after 2 weeks

**Pros**: Safe, easy rollback
**Cons**: Maintenance overhead, more code

**Recommendation**: Option A (Direct Replacement) given single-user context during development, with manual Git rollback as safety net

## Next Steps

1. **User Approval**: Review this plan with user, confirm direction
2. **Start Phase 1**: Create ViewModeNav component
3. **Iterate**: Build incrementally, test frequently
4. **Document**: Keep implementation log as we progress

---

**Plan Status**: ✅ Complete, awaiting user approval
**Created By**: Claude Code
**Last Updated**: November 1, 2025
