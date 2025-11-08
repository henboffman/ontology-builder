# Graph Layout Improvements & Position Persistence

**Date**: November 6, 2025
**Feature**: Enhanced Graph Visualization with Persistent Layouts
**Status**: 🚧 In Progress

## Overview

Comprehensive improvements to the graph visualization system to address layout quality issues and add position persistence across sessions. This feature enhances user experience by providing better automatic layouts and preserving manual adjustments.

## Problem Statement

### Current Issues

1. **Overlapping Edge Labels**
   - Multiple relationship labels stack on top of each other
   - Text becomes unreadable with 3+ relationships between concept clusters
   - No collision detection or label positioning logic

2. **Inconsistent Layouts**
   - COSE (force-directed) algorithm produces different results each run
   - Users must refresh multiple times to get acceptable layout
   - No way to preserve good layouts once achieved

3. **Lost Manual Adjustments**
   - Users can drag nodes to desired positions
   - Positions reset on page refresh or navigation
   - Database has `PositionX`/`PositionY` fields but they're not used

4. **Limited Layout Options**
   - Only COSE algorithm implemented despite 6 layouts in GraphViewState enum
   - No UI controls to switch between layout algorithms
   - Can't reset to auto-layout after manual adjustments

## Goals

### Primary Goals

✅ **Persist Node Positions**
- Save positions when users drag nodes
- Restore saved positions on graph load
- Support both Concept and OntologyLink positions

✅ **Improve Edge Label Rendering**
- Detect and prevent label overlaps
- Use bezier curve positioning for multi-edge scenarios
- Add text shadows/backgrounds for better readability

✅ **Enhanced COSE Layout**
- Tune parameters for better initial layouts
- Add deterministic seed for consistent results
- Reduce iterations needed to reach stable state

### Secondary Goals

🔲 **Layout Selector UI** (Optional)
- Dropdown in control bar to switch layouts
- Implement hierarchical and circular layouts
- Add "Reset Layout" button to re-run algorithm

🔲 **Zoom/Pan Persistence** (Optional)
- Save viewport state with positions
- Restore zoom level and center point

## Architecture Decisions

See [architecture-decisions.md](./architecture-decisions.md) for detailed ADRs.

**Key Decisions**:
1. Use existing `PositionX`/`PositionY` database fields
2. Save positions on `dragfree` event with debouncing
3. Improve COSE parameters before adding new layouts
4. Use Cytoscape edge bundling for overlap reduction
5. Add text-outline CSS for label readability

## Implementation Plan

See [implementation-plan.md](./implementation-plan.md) for detailed steps.

**Phase 1**: Position Persistence (Priority: High)
- Capture dragfree events in JavaScript
- Call C# method to update database
- Load positions on graph initialization

**Phase 2**: Edge Label Improvements (Priority: High)
- Add text-outline styling
- Implement edge-text-rotation options
- Use segment-distances for better positioning

**Phase 3**: Layout Enhancements (Priority: Medium)
- Tune COSE algorithm parameters
- Add deterministic random seed
- Test with complex ontologies

**Phase 4**: Layout Selector UI (Priority: Low)
- Add dropdown to OntologyControlBar
- Implement layout switching logic
- Add reset button

## Files to Modify

### JavaScript
- `wwwroot/js/graphVisualization.js` - Add position capture, improve layout config

### Razor Components
- `Components/Pages/GraphVisualization.razor` - Add SavePositions method
- `Components/Ontology/OntologyControlBar.razor` - Add layout selector (optional)

### C# Services
- `Services/ConceptService.cs` - Add UpdatePositionAsync method
- `Services/OntologyLinkService.cs` - Add UpdatePositionAsync method

### Models
- No changes needed (PositionX/PositionY already exist)

## Visual Design

See [visual-design.md](./visual-design.md) for mockups and styling details.

**Edge Label Styling**:
```css
text-outline-color: #ffffff
text-outline-width: 2px
text-background-opacity: 0.9
```

**Position Indicators**:
- Anchored nodes show lock icon
- Unsaved changes show orange border

## Testing Strategy

### Manual Testing
1. Drag nodes and verify positions save
2. Refresh page and verify positions restore
3. Create relationships and verify labels don't overlap
4. Test with 10+ concept ontology

### Integration Testing
- ConceptServiceTests: UpdatePositionAsync
- OntologyLinkServiceTests: UpdatePositionAsync
- GraphVisualizationTests: Position loading (bUnit)

## Success Metrics

- ✅ Positions persist across page refreshes (100% accuracy)
- ✅ No overlapping labels with <5 edges per node pair
- ✅ COSE layout produces acceptable result within 3 refreshes (vs 5+ currently)
- ✅ Drag-to-save latency <200ms
- ✅ Graph load time increase <100ms with position loading

## Rollout Plan

1. Deploy position persistence (low risk, high value)
2. Monitor performance metrics
3. Deploy edge label improvements
4. Gather user feedback
5. Decide on layout selector based on feedback

## Related Work

- Cytoscape.js documentation: https://js.cytoscape.org/
- Edge bundling: https://github.com/cytoscape/cytoscape.js/issues/1234
- COSE-Bilkent algorithm paper (better than default COSE)

---

**Status Legend**:
- ✅ Completed
- 🚧 In Progress
- 🔲 Not Started
- ❌ Blocked
