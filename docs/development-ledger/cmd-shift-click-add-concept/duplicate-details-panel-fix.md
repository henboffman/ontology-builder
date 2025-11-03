# Fix: Duplicate Details Panel on Desktop

## Date
November 2, 2025

## Issue
A duplicate `SelectedNodeDetailsPanel` was appearing on desktop screens, showing the same details panel twice:
1. Inside the `.ontology-view-nav` sidebar (within the tabs container)
2. In the separate `.ontology-sidebar` div (outside the tabs container)

## Root Cause
The layout has two different responsive sections for the details panel:
- **Mobile/Tablet**: Details panel should appear in the `.ontology-view-nav` sidebar (within tabs container) because the grid layout stacks vertically
- **Desktop**: Details panel should appear in the separate `.ontology-sidebar` column on the right

However, the details panel inside `.ontology-view-nav` was showing on all screen sizes, causing duplication on desktop.

## Solution
Added the Bootstrap `d-md-none` class to hide the details panel inside `.ontology-view-nav` on desktop screens (medium and up).

### Changed Code
**File**: `/Components/Pages/OntologyView.razor` (lines 282-288)

```razor
<!-- Selected Node Details Panel (Mobile/Tablet only - hidden on desktop) -->
<div class="mt-3 d-md-none">
    <SelectedNodeDetailsPanel SelectedConcept="@selectedConcept"
                             SelectedRelationship="@selectedRelationship"
                             SelectedIndividual="@selectedIndividual"
                             ConnectedConcepts="@GetConnectedConcepts()" />
</div>
```

### Layout Behavior
**Mobile/Tablet (< 768px)**:
- Tabs container uses single-column grid
- Details panel shows in `.ontology-view-nav` (below view selector)
- `.ontology-sidebar` is hidden (`d-none d-md-block`)

**Desktop (>= 768px)**:
- Tabs container uses two-column grid (content + view nav)
- Details panel inside `.ontology-view-nav` is hidden (`d-md-none`)
- Details panel shows in separate `.ontology-sidebar` column

## Testing
- ✅ Build succeeded with no new errors
- Manual testing required:
  - [ ] Verify details panel shows once on desktop
  - [ ] Verify details panel shows on mobile/tablet
  - [ ] Verify responsive breakpoint transitions correctly

## Related Files
- `/Components/Pages/OntologyView.razor` - Main ontology view layout
- `/wwwroot/css/ontology-tabs-layout.css` - Grid layout and responsive styles

## Impact
- Fixes visual duplication on desktop
- Improves user experience by removing redundant information
- No functionality changes, only visibility adjustments
