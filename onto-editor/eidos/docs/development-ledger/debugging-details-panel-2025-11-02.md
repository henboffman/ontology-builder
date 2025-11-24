# Debugging Details Panel Not Populating
## Date: November 2, 2025

## Issue
The details panel on the ontology page is not populating with information when clicking on concepts or relationships in the graph.

## Debug Logging Added

To diagnose this issue, we've added comprehensive console logging at three key points in the data flow:

### 1. OntologyView.SelectConcept (Line 1205)
```csharp
Console.WriteLine($"[OntologyView] SelectConcept called with: {concept?.Name ?? "null"}");
// ... state updates ...
Console.WriteLine($"[OntologyView] Selected concept set to: {selectedConcept?.Name ?? "null"}");
```

### 2. OntologyView.SelectRelationship (Line 1232)
```csharp
Console.WriteLine($"[OntologyView] SelectRelationship called with: {relationship?.RelationType ?? "null"}");
// ... state updates ...
Console.WriteLine($"[OntologyView] Selected relationship set to: {selectedRelationship?.RelationType ?? "null"}");
```

### 3. SelectedNodeDetailsPanel.OnParametersSet (Line 153)
```csharp
protected override void OnParametersSet()
{
    Console.WriteLine($"[SelectedNodeDetailsPanel] OnParametersSet - Concept: {SelectedConcept?.Name ?? "null"}, Relationship: {SelectedRelationship?.RelationType ?? "null"}, Individual: {SelectedIndividual?.Name ?? "null"}");
    base.OnParametersSet();
}
```

## How to Debug

1. **Open Browser Console**: Press F12, go to Console tab
2. **Click on a Concept in Graph**: Watch for console messages
3. **Analyze the Flow**:

### Expected Console Output When Clicking a Concept
```
[OntologyView] SelectConcept called with: ConceptName
[OntologyView] Selected concept set to: ConceptName
[SelectedNodeDetailsPanel] OnParametersSet - Concept: ConceptName, Relationship: null, Individual: null
```

### Expected Console Output When Clicking a Relationship
```
[OntologyView] SelectRelationship called with: RelationshipType
[OntologyView] Selected relationship set to: RelationshipType
[SelectedNodeDetailsPanel] OnParametersSet - Concept: null, Relationship: RelationshipType, Individual: null
```

## Possible Failure Scenarios

### Scenario 1: No Console Messages at All
**Problem**: Click events not reaching handlers
**Possible Causes**:
- Graph visualization JavaScript not calling back to Blazor
- JSInterop issues
- Event handlers not wired up correctly

**Check**:
- Graph visualization OnNodeClick/OnEdgeClick bindings
- JavaScript callback implementation

### Scenario 2: Only OntologyView Messages, No Panel Messages
**Problem**: State set but not propagating to panel
**Possible Causes**:
- Panel not in component tree
- Panel parameters not bound correctly
- StateHasChanged not triggering re-render

**Check**:
- Panel visibility (d-none d-md-block classes)
- Parameter bindings in markup
- Component hierarchy

### Scenario 3: All Messages Present But Panel Still Empty
**Problem**: Panel receiving data but not rendering
**Possible Causes**:
- Rendering logic issue in panel
- CSS hiding content
- Data structure mismatch

**Check**:
- Panel template conditions (@if statements)
- CSS for .ontology-details-sidebar
- Data property values

## Current State Changes

### Added Logging
- ✅ SelectConcept method
- ✅ SelectRelationship method
- ✅ SelectedNodeDetailsPanel.OnParametersSet

### Added selectedIndividual = null
Both SelectConcept and SelectRelationship now clear the individual selection to ensure only one item is selected at a time.

## Next Steps

Based on console output, we can determine:

1. **If no [OntologyView] messages**:
   - Check graph click event bindings
   - Verify HandleNodeClick/HandleEdgeClick are wired up

2. **If OntologyView messages but no [SelectedNodeDetailsPanel] messages**:
   - Check if panel is rendered (inspect element)
   - Verify parameter bindings
   - Check responsive display classes

3. **If all messages present**:
   - Check panel rendering logic
   - Inspect panel element in dev tools
   - Verify CSS not hiding content

## Files Modified for Debugging

1. **OntologyView.razor** (Lines 1205-1228, 1232-1249)
   - Added console logging to SelectConcept
   - Added console logging to SelectRelationship
   - Added selectedIndividual = null to both methods

2. **SelectedNodeDetailsPanel.razor** (Lines 153-157)
   - Added OnParametersSet override with logging

## Temporary Debug Code

**Note**: This logging code is temporary for debugging. Once issue is resolved, consider:
- Removing console logs, OR
- Converting to proper ILogger usage with appropriate log levels
- Wrapping in #if DEBUG directives

---
**Status**: Debugging in progress
**Next**: Analyze console output to determine root cause
