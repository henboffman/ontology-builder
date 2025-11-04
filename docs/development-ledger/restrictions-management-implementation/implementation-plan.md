# Restrictions Management - Implementation Plan
**Created**: November 2, 2025
**Status**: Planning

## Overview

Implement user-friendly restriction management within the AdminConceptDialog, allowing administrators to add, edit, and delete restrictions on concepts in an intuitive way that non-technical users can understand.

## Background

### Current State
- **Backend**: Complete and robust
  - 7 restriction types supported (Cardinality, ValueType, Range, Required, Enumeration, Pattern, ConceptType)
  - Full service layer (`IRestrictionService`) with validation
  - Repository layer with efficient queries
  - OWL/TTL export for semantic web compliance
  - Permission-based access control

- **Frontend**: Display-only
  - `ConceptRestrictionsEditor.razor` shows restrictions with readable descriptions
  - Delete functionality works perfectly
  - Add/Edit show placeholder toasts ("will be implemented")

### The Gap
**No UI for creating or editing restrictions**. Users cannot:
- Add new restrictions to concepts
- Edit existing restriction values
- Understand what each restriction type does (need better guidance)

## Goals

1. **User-Friendly**: Design for non-technical users
   - Clear labels and descriptions
   - Examples for each restriction type
   - Inline help text
   - Validation with helpful error messages

2. **Integrated**: Add to AdminConceptDialog
   - Keep all concept management in one place
   - Expandable "Restrictions" section within concept editing
   - Consistent with existing inline editing patterns

3. **Comprehensive**: Support all 7 restriction types
   - Dynamic form fields based on type
   - Proper validation for each type
   - Type-specific help text

4. **Safe**: Prevent user errors
   - Validate before saving
   - Confirmation for destructive actions
   - Clear feedback on success/failure

## Approach: Expandable Restrictions Section

We'll enhance the AdminConceptDialog by adding a restrictions section when a concept is in edit mode.

### UI Structure

```
AdminConceptDialog
└── Concept Edit Form
    ├── Name (existing)
    ├── Simple Explanation (existing)
    ├── Definition (existing)
    ├── Category (existing)
    └── Restrictions (NEW)
        ├── Restrictions List
        │   ├── Restriction Item 1
        │   │   ├── Display View
        │   │   └── Edit View (inline toggle)
        │   ├── Restriction Item 2
        │   └── ...
        └── Add Restriction Button
            └── Add Restriction Form (expandable)
```

### User Flow

#### Viewing Restrictions
1. User opens concept in edit mode
2. Sees expandable "Restrictions" section
3. Click to expand
4. Lists all restrictions with badges and descriptions
5. Each restriction has Edit/Delete buttons

#### Adding a Restriction
1. User clicks "Add Restriction" button
2. Form expands below
3. Step 1: Select Restriction Type (dropdown with descriptions)
4. Step 2: Form fields appear based on type
5. User fills in required fields
6. Help text and examples guide the user
7. User clicks "Add"
8. Validation runs
9. Success: Restriction added to list, form clears
10. Error: Helpful error message shown

#### Editing a Restriction
1. User clicks Edit button on a restriction
2. Restriction switches to inline edit mode
3. Form pre-populated with current values
4. User modifies fields
5. User clicks Save or Cancel
6. Success: Restriction updates, switches back to display
7. Error: Error message shown, stays in edit mode

#### Deleting a Restriction
1. User clicks Delete button
2. Confirmation dialog appears (already implemented)
3. User confirms
4. Restriction removed from list
5. Success toast shown

## Phased Implementation

### Phase 1: Infrastructure & Simple Types
**Estimated Time**: 2-3 hours

**Tasks**:
1. Add restrictions loading to AdminConceptDialog
2. Create expandable Restrictions section in concept edit view
3. Implement Add Restriction form infrastructure
4. Support 3 simple restriction types:
   - **Required**: Single checkbox
   - **ValueType**: Dropdown (string, integer, decimal, boolean, date, uri, concept)
   - **Cardinality**: Two number inputs (min/max)

**Deliverables**:
- ✅ Restrictions load when concept is edited
- ✅ Restrictions section expandable
- ✅ Add restriction form with type selector
- ✅ Dynamic form fields based on type
- ✅ Create restriction via RestrictionService
- ✅ Success/error feedback

### Phase 2: Complex Types & Validation
**Estimated Time**: 2-3 hours

**Tasks**:
1. Add 4 complex restriction types:
   - **Range**: Min/max value inputs (with type awareness)
   - **Enumeration**: Comma-separated values input
   - **Pattern**: Regex input with validation
   - **ConceptType**: Concept picker dropdown
2. Implement inline editing for existing restrictions
3. Add validation with helpful error messages
4. Add help text and examples for each type

**Deliverables**:
- ✅ All 7 restriction types supported
- ✅ Inline editing works
- ✅ Validation prevents invalid data
- ✅ User guidance via help text

### Phase 3: Polish & UX Enhancements
**Estimated Time**: 1-2 hours

**Tasks**:
1. Add restriction templates (common patterns)
2. Improve mobile responsiveness
3. Add keyboard shortcuts
4. Performance optimization
5. Final accessibility audit

**Deliverables**:
- ✅ Templates for quick setup
- ✅ Mobile-friendly
- ✅ Keyboard navigation
- ✅ Accessible to screen readers

## Detailed Design

### Restrictions Section Component Structure

```razor
<!-- Within AdminConceptDialog.razor, inside concept edit form -->

<!-- Restrictions Section -->
<div class="mb-3">
    <div class="d-flex justify-content-between align-items-center">
        <h6 class="mb-0">
            <i class="bi bi-shield-check"></i> Restrictions
            <span class="badge bg-secondary ms-2">@conceptRestrictions.Count()</span>
        </h6>
        <button class="btn btn-link btn-sm" @onclick="ToggleRestrictionsSection">
            <i class="bi bi-chevron-@(restrictionsSectionExpanded ? "up" : "down")"></i>
        </button>
    </div>

    @if (restrictionsSectionExpanded)
    {
        <!-- Restrictions List -->
        <div class="restrictions-list mt-2">
            @if (conceptRestrictions.Any())
            {
                @foreach (var restriction in conceptRestrictions)
                {
                    <div class="restriction-item @(editingRestrictionId == restriction.Id ? "editing" : "")">
                        @if (editingRestrictionId == restriction.Id)
                        {
                            <!-- Edit Form -->
                            <RestrictionEditForm
                                Restriction="@editingRestriction"
                                OnSave="SaveRestriction"
                                OnCancel="CancelRestrictionEdit"
                                IsSaving="@isSavingRestriction" />
                        }
                        else
                        {
                            <!-- Display View -->
                            <RestrictionDisplayView
                                Restriction="@restriction"
                                OnEdit="() => EditRestriction(restriction)"
                                OnDelete="() => DeleteRestriction(restriction)" />
                        }
                    </div>
                }
            }
            else
            {
                <div class="text-muted small text-center py-3">
                    No restrictions defined. Click "Add Restriction" to create one.
                </div>
            }
        </div>

        <!-- Add Restriction Button & Form -->
        <div class="mt-2">
            @if (!isAddingRestriction)
            {
                <button class="btn btn-sm btn-outline-success w-100"
                        @onclick="StartAddRestriction">
                    <i class="bi bi-plus-circle"></i> Add Restriction
                </button>
            }
            else
            {
                <div class="add-restriction-form">
                    <RestrictionEditForm
                        Restriction="@newRestriction"
                        IsNew="true"
                        OnSave="AddRestriction"
                        OnCancel="CancelAddRestriction"
                        IsSaving="@isSavingRestriction" />
                </div>
            }
        </div>
    }
</div>
```

### RestrictionEditForm Component

**New Component**: `Components/Shared/RestrictionEditForm.razor`

This is a reusable form component that:
- Shows different fields based on restriction type
- Provides validation
- Shows help text and examples
- Handles both create and update scenarios

**Key Features**:
1. **Type Selector**: Dropdown with all 7 types
2. **Dynamic Fields**: Shows only relevant fields for selected type
3. **Help Text**: Inline guidance for each field
4. **Examples**: Shows example values/patterns
5. **Validation**: Client-side validation before submit

#### Form Field Logic by Type

```csharp
// Pseudo-code for field visibility

if (RestrictionType == "Required")
{
    // No additional fields needed
    // Just property name and IsMandatory checkbox
}
else if (RestrictionType == "ValueType")
{
    // Show ValueType dropdown
    // Options: string, integer, decimal, boolean, date, uri, concept
}
else if (RestrictionType == "Cardinality")
{
    // Show MinCardinality number input (optional)
    // Show MaxCardinality number input (optional)
    // Help: "Leave blank for unbounded"
}
else if (RestrictionType == "Range")
{
    // Show MinValue input
    // Show MaxValue input
    // Note: Type depends on ValueType field
}
else if (RestrictionType == "Enumeration")
{
    // Show AllowedValues textarea
    // Help: "Enter comma-separated values (e.g., red, green, blue)"
}
else if (RestrictionType == "Pattern")
{
    // Show Pattern input
    // Help: "Enter a regular expression pattern"
    // Example: ^\d{5}$ for US zip code
}
else if (RestrictionType == "ConceptType")
{
    // Show AllowedConceptId dropdown
    // Load all concepts in ontology
}
```

### State Management in AdminConceptDialog

```csharp
// Add to existing state fields

// Restrictions state
private List<ConceptRestriction> conceptRestrictions = new();
private bool restrictionsSectionExpanded = true; // Default expanded
private bool isAddingRestriction = false;
private ConceptRestriction newRestriction = new();
private int? editingRestrictionId = null;
private ConceptRestriction editingRestriction = new();
private bool isSavingRestriction = false;
```

### Service Integration

```csharp
// Load restrictions when editing concept
private void ToggleEdit(Concept concept)
{
    if (editingConceptId == concept.Id)
    {
        CancelEdit();
    }
    else
    {
        editingConceptId = concept.Id;
        editingConcept = new Concept { /* copy fields */ };

        // Load restrictions for this concept
        LoadRestrictionsForConcept(concept.Id);
    }
}

private async Task LoadRestrictionsForConcept(int conceptId)
{
    try
    {
        var restrictions = await RestrictionService.GetByConceptIdAsync(conceptId);
        conceptRestrictions = restrictions.ToList();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to load restrictions for concept {ConceptId}", conceptId);
        ToastService.ShowError("Failed to load restrictions");
        conceptRestrictions = new List<ConceptRestriction>();
    }
}

// Add new restriction
private async Task AddRestriction()
{
    // Validate
    if (string.IsNullOrWhiteSpace(newRestriction.PropertyName))
    {
        ToastService.ShowError("Property name is required");
        return;
    }

    try
    {
        isSavingRestriction = true;
        StateHasChanged();

        // Set ConceptId
        newRestriction.ConceptId = editingConceptId.Value;

        // Create via service
        await RestrictionService.CreateAsync(newRestriction);

        ToastService.ShowSuccess($"Restriction on '{newRestriction.PropertyName}' added");

        // Reload restrictions
        await LoadRestrictionsForConcept(editingConceptId.Value);

        // Reset state
        isAddingRestriction = false;
        newRestriction = new ConceptRestriction();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to create restriction");
        ToastService.ShowError($"Failed to add restriction: {ex.Message}");
    }
    finally
    {
        isSavingRestriction = false;
        StateHasChanged();
    }
}

// Edit existing restriction
private void EditRestriction(ConceptRestriction restriction)
{
    editingRestrictionId = restriction.Id;
    editingRestriction = new ConceptRestriction
    {
        Id = restriction.Id,
        ConceptId = restriction.ConceptId,
        PropertyName = restriction.PropertyName,
        RestrictionType = restriction.RestrictionType,
        MinCardinality = restriction.MinCardinality,
        MaxCardinality = restriction.MaxCardinality,
        ValueType = restriction.ValueType,
        MinValue = restriction.MinValue,
        MaxValue = restriction.MaxValue,
        AllowedConceptId = restriction.AllowedConceptId,
        AllowedValues = restriction.AllowedValues,
        Pattern = restriction.Pattern,
        Description = restriction.Description,
        IsMandatory = restriction.IsMandatory
    };
}

// Save edited restriction
private async Task SaveRestriction()
{
    if (editingRestrictionId == null)
        return;

    try
    {
        isSavingRestriction = true;
        StateHasChanged();

        await RestrictionService.UpdateAsync(editingRestriction);

        ToastService.ShowSuccess($"Restriction on '{editingRestriction.PropertyName}' updated");

        // Reload restrictions
        await LoadRestrictionsForConcept(editingConceptId.Value);

        // Clear edit state
        editingRestrictionId = null;
        editingRestriction = new ConceptRestriction();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to update restriction");
        ToastService.ShowError($"Failed to save restriction: {ex.Message}");
    }
    finally
    {
        isSavingRestriction = false;
        StateHasChanged();
    }
}

// Delete restriction (already implemented in OntologyView, adapt here)
private async Task DeleteRestriction(ConceptRestriction restriction)
{
    var confirmed = await ConfirmService.ShowAsync(
        title: "Delete Restriction",
        message: $"Are you sure you want to delete the restriction on property \"{restriction.PropertyName}\"?",
        confirmText: "Delete",
        type: ConfirmType.Danger
    );

    if (!confirmed)
        return;

    try
    {
        await RestrictionService.DeleteAsync(restriction.Id);

        ToastService.ShowSuccess($"Restriction on \"{restriction.PropertyName}\" deleted");

        // Reload restrictions
        await LoadRestrictionsForConcept(editingConceptId.Value);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to delete restriction");
        ToastService.ShowError($"Failed to delete restriction: {ex.Message}");
    }
}
```

## User-Friendly Design Principles

### 1. Clear Labels
- Use plain language instead of technical terms
- "Property Name" instead of "PropertyName"
- "Minimum Value" instead of "MinValue"

### 2. Help Text
Each field should have contextual help:
```html
<label>Property Name</label>
<input type="text" ... />
<small class="form-text text-muted">
    The name of the property this restriction applies to (e.g., "age", "hasParent")
</small>
```

### 3. Examples
Show real-world examples for each restriction type:

**Required**:
> Example: Ensure every Person has a "name" property

**ValueType**:
> Example: Specify that "age" must be an integer, not text

**Cardinality**:
> Example: A Person must have exactly 2 biological parents (min=2, max=2)

**Range**:
> Example: Age must be between 0 and 120

**Enumeration**:
> Example: Color must be one of: red, green, blue, yellow

**Pattern**:
> Example: US Zip Code must match: ^\d{5}$

**ConceptType**:
> Example: The "hasSupervisor" property must reference a Person concept

### 4. Visual Hierarchy
- Badge for restriction type (color-coded)
- Icon for each restriction type
- Clear separation between restrictions

### 5. Validation Messages
- "Property name is required"
- "Please select a restriction type"
- "Minimum value must be less than maximum value"
- "Invalid regex pattern: {error}"
- "Please select a concept type"

## CSS Additions

**File**: `wwwroot/css/admin-dialogs.css`

```css
/* ============================================
   Restrictions Section
   ============================================ */

.restrictions-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.restriction-item {
    padding: 0.75rem;
    border: 1px solid var(--bs-border-color);
    border-radius: 0.375rem;
    background-color: var(--bs-body-bg);
    transition: all 150ms ease;
}

.restriction-item:hover {
    border-color: var(--bs-primary);
    background-color: rgba(var(--bs-primary-rgb), 0.02);
}

.restriction-item.editing {
    border-color: var(--bs-primary);
    background-color: rgba(var(--bs-primary-rgb), 0.05);
}

.restriction-type-badge {
    font-size: 0.75rem;
    padding: 0.25rem 0.5rem;
    border-radius: 0.25rem;
}

.restriction-help-text {
    font-size: 0.875rem;
    color: var(--bs-secondary);
    margin-top: 0.25rem;
}

.restriction-example {
    font-size: 0.875rem;
    padding: 0.5rem;
    background-color: var(--bs-light);
    border-left: 3px solid var(--bs-info);
    border-radius: 0.25rem;
    margin-top: 0.5rem;
}

[data-bs-theme="dark"] .restriction-example {
    background-color: rgba(255, 255, 255, 0.05);
}

.add-restriction-form {
    padding: 1rem;
    border: 2px dashed var(--bs-border-color);
    border-radius: 0.375rem;
    background-color: rgba(var(--bs-success-rgb), 0.05);
}

/* Restriction Type Colors */
.restriction-type-required { background-color: #dc3545; color: white; }
.restriction-type-valuetype { background-color: #0dcaf0; color: white; }
.restriction-type-cardinality { background-color: #6610f2; color: white; }
.restriction-type-range { background-color: #fd7e14; color: white; }
.restriction-type-enumeration { background-color: #20c997; color: white; }
.restriction-type-pattern { background-color: #6f42c1; color: white; }
.restriction-type-concepttype { background-color: #0d6efd; color: white; }
```

## Files to Create

1. `/Components/Shared/RestrictionEditForm.razor` (~300 lines)
   - Dynamic form component
   - Type-specific field rendering
   - Validation logic
   - Help text and examples

2. `/Components/Shared/RestrictionDisplayView.razor` (~100 lines)
   - Display component for restriction
   - Human-readable description
   - Edit/Delete action buttons
   - Type-specific badges

## Files to Modify

1. `/Components/Ontology/Admin/AdminConceptDialog.razor`
   - Add restrictions section
   - Add state management for restrictions
   - Add service integration methods
   - Inject IRestrictionService

2. `/wwwroot/css/admin-dialogs.css`
   - Add restriction-specific styles
   - Color-coded badges
   - Example boxes
   - Responsive adjustments

## Testing Checklist

### Phase 1
- [ ] Restrictions load when editing concept
- [ ] Restrictions section expands/collapses
- [ ] Add restriction button shows form
- [ ] Required type works
- [ ] ValueType type works
- [ ] Cardinality type works
- [ ] Form validates property name
- [ ] Success toast shows after add
- [ ] Restrictions list updates after add
- [ ] Cancel clears form

### Phase 2
- [ ] Range type works
- [ ] Enumeration type works
- [ ] Pattern type works
- [ ] ConceptType type works
- [ ] Edit button opens inline form
- [ ] Edit form pre-populates values
- [ ] Save updates restriction
- [ ] Cancel discards changes
- [ ] Validation prevents invalid data
- [ ] Help text displays correctly

### Phase 3
- [ ] Restriction templates work
- [ ] Mobile responsive
- [ ] Keyboard shortcuts work
- [ ] Screen reader accessible
- [ ] Performance acceptable (<500ms operations)

## Performance Considerations

- **Lazy Load**: Only load restrictions when concept is being edited
- **Debounce**: Validation runs after user stops typing (300ms)
- **Caching**: Cache concept list for ConceptType dropdown
- **Efficient Queries**: RestrictionService already optimized

## Security & Permissions

- **Permission Required**: ViewAddEdit for create/update, FullAccess for delete
- **Validation**: Client-side validation for UX, server-side for security
- **XSS Prevention**: All user input escaped in Blazor by default
- **CSRF**: Protected by ASP.NET Core anti-forgery tokens

## Success Metrics

- ✅ Users can add restrictions without technical knowledge
- ✅ Form completion time < 2 minutes for simple restrictions
- ✅ Error rate < 5% (validation catches issues)
- ✅ All 7 restriction types supported
- ✅ No new build errors or warnings

---

**Estimated Total Time**: 5-8 hours
**Complexity**: Medium-High
**Dependencies**: IRestrictionService, ConfirmService, ToastService
**Next Steps**: Begin Phase 1 implementation
