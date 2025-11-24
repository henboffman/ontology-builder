# Cmd+Shift+Click to Add Concept Feature

## Date
November 2, 2025

## Overview
Added keyboard shortcut functionality to allow users to open the "Add Concept" dialog by holding Cmd+Shift (or Ctrl+Shift on Windows) and clicking anywhere on the graph background.

## Motivation
- Improves user workflow efficiency when working in the graph view
- Provides a contextual way to add concepts directly from the graph interface
- Complements the existing Ctrl+Click shortcut for adding relationships
- Reduces need to navigate to the toolbar to add concepts

## Implementation Details

### 1. JavaScript Event Handler (graphVisualization.js)
**File**: `/wwwroot/js/graphVisualization.js`

Added a click event handler on the Cytoscape instance that:
- Detects clicks on the background (not on nodes or edges)
- Checks for Cmd+Shift (Mac) or Ctrl+Shift (Windows/Linux) modifier keys
- Calls back to Blazor via JSInterop

```javascript
// Add click handler for background (Cmd+Shift+Click to add concept)
cy.on('click', function (event) {
    // Check if the click was on the background (not on a node or edge)
    if (event.target === cy) {
        // Check if Cmd+Shift (Mac) or Ctrl+Shift (Windows/Linux) was pressed
        if ((event.originalEvent.metaKey || event.originalEvent.ctrlKey) && event.originalEvent.shiftKey) {
            // Call back to Blazor to show add concept dialog
            dotNetHelper.invokeMethodAsync('OnBackgroundCmdShiftClick')
                .then(() => console.log('OnBackgroundCmdShiftClick succeeded'))
                .catch(err => console.error('OnBackgroundCmdShiftClick failed:', err));
        }
    }
});
```

**Key Design Decisions**:
- Used `event.target === cy` to ensure click is on background, not on nodes/edges
- Support both Mac (metaKey) and Windows/Linux (ctrlKey) modifier keys
- Require Shift key to differentiate from other shortcuts
- Added error handling for the async JSInterop call

### 2. Blazor Component Integration (GraphVisualization.razor)
**File**: `/Components/Pages/GraphVisualization.razor`

Added:
- New `EventCallback` parameter: `OnBackgroundCmdShiftClicked`
- JSInvokable method to handle the JavaScript callback

```csharp
[Parameter]
public EventCallback OnBackgroundCmdShiftClicked { get; set; }

[JSInvokable]
public async Task OnBackgroundCmdShiftClick()
{
    Console.WriteLine("[GraphVisualization] OnBackgroundCmdShiftClick called");
    await OnBackgroundCmdShiftClicked.InvokeAsync();
    Console.WriteLine("[GraphVisualization] OnBackgroundCmdShiftClicked callback invoked");
}
```

### 3. Parent Component Wiring (GraphView.razor)
**File**: `/Components/Ontology/GraphView.razor`

Connected the new event callback to the existing "Add Concept" functionality:

```razor
<GraphVisualization Ontology="@FilteredOntology" Height="700px" ColorMode="@ColorMode"
                  ShowIndividuals="@showIndividuals"
                  OnNodeCtrlClicked="@OnNodeCtrlClicked"
                  OnNodeClicked="@OnNodeClicked"
                  OnEdgeClicked="@OnEdgeClicked"
                  OnIndividualClicked="@OnIndividualClicked"
                  OnBackgroundCmdShiftClicked="@OnAddConceptClick"
                  @ref="graphVisualization" />
```

### 4. UI Documentation Updates
Updated help text and tooltips to document the new shortcut:

**Header subtitle**:
```
Drag nodes to rearrange • Scroll to zoom • Click to select • Ctrl+Click to add relationship • Cmd+Shift+Click to add concept
```

**Help panel**:
```html
<li><strong>Cmd+Shift+Click background</strong> (or Ctrl+Shift+Click on Windows) to add a new concept</li>
```

## Files Modified
1. `/wwwroot/js/graphVisualization.js` - Added background click handler
2. `/Components/Pages/GraphVisualization.razor` - Added event callback and JSInvokable method
3. `/Components/Ontology/GraphView.razor` - Wired up event callback and updated help text

## Testing
- ✅ Build succeeded with no errors (10 warnings, all pre-existing)
- ✅ Code compiles and runs successfully
- Manual testing required:
  - [ ] Verify Cmd+Shift+Click on Mac opens Add Concept dialog
  - [ ] Verify Ctrl+Shift+Click on Windows/Linux opens Add Concept dialog
  - [ ] Verify click must be on background (not nodes/edges)
  - [ ] Verify other keyboard shortcuts still work as expected

## User Experience Impact
- **Positive**: Faster workflow for users who prefer keyboard shortcuts
- **Positive**: More intuitive for users already familiar with Ctrl+Click for relationships
- **Neutral**: Completely optional - existing UI buttons still work
- **Low risk**: Non-intrusive addition that doesn't change existing behavior

## Code Quality
- Follows existing patterns for keyboard shortcuts in the application
- Consistent with Ctrl+Click pattern for adding relationships
- Properly handles cross-platform differences (Mac vs Windows/Linux)
- Includes console logging for debugging
- Updated documentation inline with implementation

## Future Enhancements
Potential improvements for future iterations:
- Add visual feedback when modifier keys are held (cursor change, overlay hint)
- Consider additional shortcuts for other common operations
- Add to global keyboard shortcuts help dialog
- Track usage analytics to measure adoption

## Related Features
- Ctrl+Click on node to add relationship (existing)
- Add Concept button in toolbar (existing)
- Keyboard shortcuts system (existing)

## Notes
This feature aligns with the project's focus on improving user efficiency and providing multiple ways to accomplish common tasks. The keyboard shortcut pattern is consistent with existing shortcuts and familiar to users of other graph editing tools.
