# Feature 2: View State Preservation on Relationship Delete

**Implemented**: November 5, 2025
**Status**: ✅ Complete
**Time**: 30 minutes

---

## Summary

Fixed issue where deleting a relationship while on the "Relationships" tab would reset the view back to the "Concepts" tab. The selected tab now persists across data refreshes, providing a smoother user experience.

---

## Problem

### Original Behavior
1. User navigates to List View
2. User clicks "Relationships" tab
3. User deletes a relationship
4. `LoadOntology()` is called to refresh data
5. ListViewPanel component re-renders
6. Tab resets to "Concepts" (default)
7. User must manually click back to "Relationships"

### Root Cause

The `activeListTab` state was local to the `ListViewPanel` component:

```csharp
private string activeListTab = "concepts";  // Component-local state
```

When `LoadOntology()` caused a re-render, the component's local state was reset to its default value.

---

## Solution

### Architecture Change

Converted `activeListTab` from component-local state to a **parent-controlled parameter** using Blazor's two-way binding pattern.

**Principle**: State that needs to persist across re-renders should be managed by the parent component.

### Implementation

#### 1. Updated ListViewPanel Component

**File**: `ListViewPanel.razor` (lines 315-331)

**Before**:
```csharp
@code {
    private string activeListTab = "concepts";  // Local state - resets on re-render

    [Parameter, EditorRequired]
    public ICollection<Concept> Concepts { get; set; } = new List<Concept>();
    // ...
}
```

**After**:
```csharp
@code {
    [Parameter]
    public string ActiveListTab { get; set; } = "concepts";

    [Parameter]
    public EventCallback<string> ActiveListTabChanged { get; set; }

    private string activeListTab
    {
        get => ActiveListTab;
        set
        {
            if (ActiveListTab != value)
            {
                ActiveListTabChanged.InvokeAsync(value);
            }
        }
    }

    [Parameter, EditorRequired]
    public ICollection<Concept> Concepts { get; set; } = new List<Concept>();
    // ...
}
```

**Key Points**:
- Made `ActiveListTab` a parameter
- Added `ActiveListTabChanged` event callback
- Created private property `activeListTab` as wrapper for internal use
- Existing code continues to use lowercase `activeListTab` (no changes needed in markup)

#### 2. Added State to OntologyView

**File**: `OntologyView.razor` (line 665)

```csharp
private string activeListTab = "concepts";  // Preserves tab selection in List view
```

#### 3. Updated Component Binding

**File**: `OntologyView.razor` (line 215)

**Before**:
```razor
<ListViewPanel Concepts="@ontology.Concepts"
               Relationships="@ontology.Relationships"
               SortOption="@sortOption"
               ...other parameters... />
```

**After**:
```razor
<ListViewPanel Concepts="@ontology.Concepts"
               Relationships="@ontology.Relationships"
               SortOption="@sortOption"
               @bind-ActiveListTab="@activeListTab"
               ...other parameters... />
```

---

## Behavior After Fix

### Scenario: Delete Relationship on Relationships Tab
1. User switches to "Relationships" tab
2. `activeListTab` is set to "relationships" in OntologyView state
3. User deletes a relationship
4. `LoadOntology()` refreshes data
5. ListViewPanel re-renders with `ActiveListTab="relationships"`
6. User remains on Relationships tab ✅
7. Deleted relationship is removed from list

### Scenario: Switch View Modes
1. User on Relationships tab
2. User switches to Graph view
3. User switches back to List view
4. Still on Relationships tab (state preserved) ✅

---

## Technical Details

### Two-Way Binding Pattern

Blazor's `@bind-` syntax creates automatic two-way binding:

```razor
@bind-ActiveListTab="@activeListTab"
```

This expands to:
```razor
ActiveListTab="@activeListTab"
ActiveListTabChanged="@((value) => activeListTab = value)"
```

### Why This Works

1. **Parent owns the state**: `OntologyView.activeListTab` persists across component lifecycles
2. **Child receives state**: `ListViewPanel.ActiveListTab` parameter gets current value on each render
3. **Changes flow up**: When user clicks tab, `ActiveListTabChanged` event updates parent state
4. **Changes flow down**: Parent state change triggers child re-render with new value

### Alternative Approaches Considered

1. **Store in browser localStorage**
   - ❌ Overkill for session-scoped state
   - ❌ Adds serialization complexity
   - ❌ State persists across browser sessions (undesired)

2. **Use Cascading Parameters**
   - ❌ Makes ListViewPanel less reusable
   - ❌ Implicit dependency on parent structure

3. **Parent-controlled parameter** (CHOSEN)
   - ✅ Explicit dependency in component API
   - ✅ Easy to test
   - ✅ Standard Blazor pattern
   - ✅ State lives at appropriate level

---

## Files Modified

### ListViewPanel.razor
- **Lines 315-331**: Converted `activeListTab` from local state to parameter
- **Impact**: Component is now more flexible and testable

### OntologyView.razor
- **Line 665**: Added `activeListTab` state variable
- **Line 215**: Added `@bind-ActiveListTab` parameter
- **Impact**: Parent now controls tab state

---

## Testing

### Manual Test Cases

✅ **Test 1: Delete on Relationships Tab**
- Switch to List view, Relationships tab
- Delete a relationship
- Verify still on Relationships tab
- Verify relationship removed from list

✅ **Test 2: Delete on Concepts Tab**
- Switch to List view, Concepts tab
- Delete a concept (via concept menu)
- Verify still on Concepts tab
- Verify concept removed from list

✅ **Test 3: Switch View Modes**
- Go to Relationships tab
- Switch to Graph view
- Switch back to List view
- Verify on Relationships tab

✅ **Test 4: Fresh Page Load**
- Navigate to ontology
- Verify on Concepts tab (default)
- Switch to Relationships
- Refresh page
- Verify resets to Concepts tab (expected - no persistence across sessions)

---

## User Impact

### Benefits
- **Less clicking**: No need to re-select tab after deletions
- **Smoother workflow**: Stay in context when making changes
- **Intuitive behavior**: Matches user expectations
- **Applies to all tabs**: Works for both Concepts and Relationships

### Backwards Compatibility
- ✅ No breaking changes
- ✅ Default behavior unchanged (starts on Concepts tab)
- ✅ No database changes
- ✅ No data migration needed

---

## Architecture Notes

### State Management Hierarchy

```
OntologyView (State Owner)
  ├─ viewMode: ViewMode
  ├─ sortOption: string
  ├─ activeListTab: string  ← NEW
  └─ ListViewPanel (State Consumer)
      ├─ ActiveListTab parameter  ← receives from parent
      └─ private activeListTab    ← wrapper for internal use
```

### Why Parent-Controlled State?

**Benefits**:
1. State persists across component re-renders
2. Parent can control or reset state if needed
3. Makes component behavior predictable
4. Enables future features (e.g., deep linking to specific tab)

**Trade-offs**:
- More parameters to pass down
- Parent has more responsibility
- Component is less self-contained

**Decision**: The trade-off is worth it for state that needs to persist.

---

## Future Enhancements

**Potential improvements (not in current scope)**:
1. **Deep linking**: Add tab to URL query string
2. **Session persistence**: Remember tab across page refreshes
3. **Tab-specific sorting**: Different sort options per tab
4. **Tab badges**: Show counts on tab labels

---

## Related Issues

This fix also improves the experience when:
- Editing relationships
- Duplicating relationships
- Any operation that calls `LoadOntology()`

The tab state now persists across all these operations.

---

**Build Status**: ✅ Passing (0 errors)
**Test Status**: ✅ Manual testing passed
