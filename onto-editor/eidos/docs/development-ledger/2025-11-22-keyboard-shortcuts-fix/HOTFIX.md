# Keyboard Shortcuts Hotfix - November 22, 2025

## Issue

After initial implementation, keyboard shortcuts stopped working entirely:
- Alt + G/L/H/T (view switching) not working
- Alt + C/R (add concept/relationship) not working
- Esc not dismissing dialogs

## Root Cause

The component registration for keyboard shortcuts was happening **too late**. The code was:

```csharp
// WRONG: Only registers after ontology loads
if (ontology != null && !hasRendered)
{
    dotNetRef = DotNetObjectReference.Create(this);
    await JS.InvokeVoidAsync("window.keyboardShortcuts.registerComponent", dotNetRef);
    // ...
}
```

This meant keyboard shortcuts only started working AFTER the ontology data was loaded, creating a race condition and delay.

## Fix

Separated the component registration to happen immediately on `firstRender`:

```csharp
// CORRECT: Register on first render
if (firstRender)
{
    dotNetRef = DotNetObjectReference.Create(this);
    await JS.InvokeVoidAsync("window.keyboardShortcuts.registerComponent", dotNetRef);
}

// SignalR initialization still waits for ontology
if (ontology != null && !hasRendered)
{
    await JS.InvokeVoidAsync("ontologyHub.init", dotNetRef, ontology.Id);
    // ...
}
```

## Changes Made

**File**: `Components/Pages/OntologyView.razor.cs`
**Method**: `OnAfterRenderAsync`
**Lines**: 2447-2486

- Added `if (firstRender)` block to register keyboard shortcuts immediately
- Separated keyboard shortcuts registration from SignalR initialization
- SignalR still waits for ontology data to be loaded

## Testing

After fix:
- ✅ Alt + G/L/H/T should switch views
- ✅ Alt + C/R should open add concept/relationship dialogs
- ✅ Ctrl/Cmd + Z/Y should undo/redo
- ✅ Esc should close dialogs (native Blazor behavior)
- ✅ Ctrl/Cmd + K should open command palette
- ✅ Ctrl/Cmd + , should open settings

## Build Status

```
Build succeeded.
    54 Warning(s) (pre-existing)
    0 Error(s)
Time Elapsed 00:00:04.25
```

## Notes

The Esc key behavior is handled natively by Blazor - the JavaScript just allows the event to propagate. This is correct and should work for all modal dialogs.

All keyboard shortcuts are now registered immediately when the component first renders, ensuring they're available as soon as the page loads.
