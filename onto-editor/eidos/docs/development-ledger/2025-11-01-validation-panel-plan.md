# Automatic Validation Panel - Implementation Plan

**Date**: November 1, 2025
**Feature**: VS Code-Style Problems Panel for Ontology Validation
**Status**: Planning Phase

## User Request

> "Can we add some type of automatic review where the application calls out any issues it sees to the user, such as if they manage to get duplicate entries in for concepts or relationships, almost like syntax highlighting in a code editor. It should be an obvious visual indicator when there is an issue on a concept or a relationship (maybe we manage this in the list page?) There should also be easy to notice visual indicators (maybe in the topbar) that inform the user of the issues that would be in the lists down below. Also, maybe the user can have a collapsable panel at the top that shows both the problematic concepts and relationships listed in a single panel/list? Or they could click the item from that list and it would take them to the actual entry (like the problems panel in VS Code)."

## Feature Overview

Create an automatic validation system similar to VS Code's Problems panel that:
1. Detects data quality issues in real-time
2. Shows visual indicators on problematic entries
3. Displays issue count in top bar
4. Provides collapsible problems panel
5. Enables click-to-navigate to problematic entries

## Validation Rules to Implement

### Duplicate Detection
- **Duplicate Concepts**: Same name (case-insensitive)
- **Duplicate Relationships**: Same triple (Subject-Predicate-Object)

### Data Quality Issues
- **Orphaned Concepts**: Concepts with no relationships
- **Missing Descriptions**: Concepts without description text
- **Broken References**: Relationships referencing non-existent concepts (shouldn't happen, but check)
- **Circular Relationships**: Concept → Concept (self-referencing)

### Naming Conventions (Optional)
- **Empty Names**: Concepts with whitespace-only names
- **Special Characters**: Names with problematic characters
- **URI Validation**: Invalid URI format in concept URIs

## Architecture Design

### 1. Validation Service

**File**: `Services/OntologyValidationService.cs`

```csharp
public interface IOntologyValidationService
{
    Task<ValidationResult> ValidateOntologyAsync(int ontologyId);
    Task<List<ValidationIssue>> GetIssuesAsync(int ontologyId);
}

public class ValidationResult
{
    public int TotalIssues { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public List<ValidationIssue> Issues { get; set; }
}

public class ValidationIssue
{
    public int Id { get; set; }
    public ValidationSeverity Severity { get; set; } // Error, Warning, Info
    public ValidationType Type { get; set; } // Duplicate, Orphaned, MissingDescription, etc.
    public string EntityType { get; set; } // "Concept" or "Relationship"
    public int EntityId { get; set; }
    public string EntityName { get; set; }
    public string Message { get; set; }
    public string Details { get; set; }
    public string RecommendedAction { get; set; }
}

public enum ValidationSeverity
{
    Error,   // Red - must fix
    Warning, // Yellow - should fix
    Info     // Blue - consider fixing
}

public enum ValidationType
{
    DuplicateConcept,
    DuplicateRelationship,
    OrphanedConcept,
    MissingDescription,
    CircularRelationship,
    InvalidUri
}
```

### 2. Validation Component

**File**: `Components/Ontology/ValidationPanel.razor`

Collapsible panel at top of OntologyView showing all issues:

```razor
<div class="validation-panel @(isCollapsed ? "collapsed" : "expanded")">
    <div class="validation-header" @onclick="TogglePanel">
        <div class="d-flex align-items-center">
            <i class="bi bi-chevron-@(isCollapsed ? "down" : "up")"></i>
            <strong>Problems</strong>
            <span class="badge bg-danger ms-2">@errorCount errors</span>
            <span class="badge bg-warning text-dark ms-1">@warningCount warnings</span>
        </div>
        <button class="btn btn-sm btn-outline-primary" @onclick:stopPropagation="true" @onclick="RefreshValidation">
            <i class="bi bi-arrow-clockwise"></i> Refresh
        </button>
    </div>

    @if (!isCollapsed)
    {
        <div class="validation-body">
            @foreach (var issue in issues)
            {
                <div class="validation-issue severity-@issue.Severity.ToString().ToLower()"
                     @onclick="() => NavigateToIssue(issue)">
                    <i class="bi bi-@GetIconForSeverity(issue.Severity)"></i>
                    <div class="issue-content">
                        <div class="issue-message">@issue.Message</div>
                        <div class="issue-details text-muted small">
                            @issue.EntityType: @issue.EntityName
                        </div>
                    </div>
                    <i class="bi bi-arrow-right text-muted"></i>
                </div>
            }
        </div>
    }
</div>
```

### 3. Top Bar Badge

Add to `OntologyView.razor` header area:

```razor
<div class="validation-badge">
    @if (validationResult?.TotalIssues > 0)
    {
        <button class="btn btn-sm"
                class="@(validationResult.ErrorCount > 0 ? "btn-danger" : "btn-warning")"
                @onclick="ScrollToValidationPanel"
                title="@validationResult.TotalIssues issues found">
            <i class="bi bi-exclamation-triangle"></i>
            @validationResult.TotalIssues
        </button>
    }
    else
    {
        <span class="badge bg-success">
            <i class="bi bi-check-circle"></i> No issues
        </span>
    }
</div>
```

### 4. List View Visual Indicators

Modify list view items to show validation status:

```razor
<div class="list-item @GetValidationClass(concept.Id)">
    @if (HasIssue(concept.Id))
    {
        <i class="bi bi-exclamation-triangle text-@GetSeverityColor(concept.Id)"></i>
    }
    <span>@concept.Name</span>
</div>
```

**CSS for highlighting**:
```css
.list-item.has-error {
    border-left: 3px solid var(--bs-danger);
    background-color: rgba(220, 53, 69, 0.1);
}

.list-item.has-warning {
    border-left: 3px solid var(--bs-warning);
    background-color: rgba(255, 193, 7, 0.1);
}
```

### 5. Navigation System

```csharp
private async Task NavigateToIssue(ValidationIssue issue)
{
    // Switch to appropriate tab
    if (issue.EntityType == "Concept")
    {
        selectedTab = "list"; // or "hierarchy"

        // Scroll to concept in list
        await JSRuntime.InvokeVoidAsync("scrollToElement", $"concept-{issue.EntityId}");

        // Optionally open edit dialog
        var concept = ontology.Concepts.FirstOrDefault(c => c.Id == issue.EntityId);
        if (concept != null)
        {
            selectedConcept = concept;
            StateHasChanged();
        }
    }
    else if (issue.EntityType == "Relationship")
    {
        selectedTab = "list"; // Show relationships list
        await JSRuntime.InvokeVoidAsync("scrollToElement", $"relationship-{issue.EntityId}");
    }
}
```

## Implementation Steps

### Phase 1: Service Layer
1. Create `IOntologyValidationService` interface
2. Implement `OntologyValidationService` with core validation logic
3. Add duplicate detection (concepts and relationships)
4. Add data quality checks (orphaned concepts, missing descriptions)
5. Register service in `Program.cs`
6. Write unit tests

### Phase 2: Validation Panel Component
1. Create `ValidationPanel.razor` component
2. Implement collapsible UI
3. Add issue grouping by severity
4. Style with Bootstrap and custom CSS
5. Add refresh button

### Phase 3: OntologyView Integration
1. Inject validation service into `OntologyView`
2. Add top bar validation badge
3. Add validation panel to page layout
4. Implement click-to-navigate logic
5. Add JavaScript helpers for smooth scrolling

### Phase 4: List View Indicators
1. Add validation status to list view items
2. Implement visual indicators (border, icon, background)
3. Add tooltip showing issue details
4. Ensure responsive design

### Phase 5: Real-time Updates
1. Trigger validation after concept/relationship changes
2. Update validation panel automatically
3. Debounce validation calls (avoid excessive checks)
4. Cache validation results

## File Structure

```
Services/
  OntologyValidationService.cs (NEW)
  Interfaces/
    IOntologyValidationService.cs (NEW)

Components/
  Ontology/
    ValidationPanel.razor (NEW)
    ValidationPanel.razor.css (NEW)

wwwroot/
  js/
    validation-helpers.js (NEW)

Models/
  ValidationResult.cs (NEW)
  ValidationIssue.cs (NEW)
  Enums/
    ValidationSeverity.cs (NEW)
    ValidationType.cs (NEW)
```

## UI/UX Patterns to Follow

### Visual Hierarchy
- **Errors**: Red (🔴) - Must fix
- **Warnings**: Yellow/Orange (🟡) - Should fix
- **Info**: Blue (🔵) - Consider fixing

### Icons (Bootstrap Icons)
- `bi-exclamation-triangle` - Errors/Warnings
- `bi-info-circle` - Information
- `bi-check-circle` - No issues (success state)
- `bi-arrow-clockwise` - Refresh validation
- `bi-chevron-up/down` - Collapse/Expand

### Panel Behavior
- Starts **expanded** if issues exist
- Starts **collapsed** if no issues
- Remembers state in session storage
- Auto-expands when new issues detected

### Accessibility
- Keyboard navigation (Tab, Enter)
- ARIA labels for screen readers
- Color + icon (not color alone)
- Focus management when navigating

## Performance Considerations

### Validation Caching
- Cache results for 30 seconds
- Invalidate on concept/relationship changes
- Use in-memory cache (IMemoryCache)

### Lazy Loading
- Only validate visible ontologies
- Defer validation until user opens ontology
- Background validation for active ontology

### Efficient Queries
- Use `AsNoTracking()` for read-only validation
- Single query with all necessary `Include()`s
- Project to DTOs to reduce memory

## Testing Strategy

### Unit Tests
- Each validation rule independently
- Edge cases (empty ontology, large ontology)
- Performance tests (1000+ concepts)

### Integration Tests
- Service integration with repository
- Component rendering with issues
- Navigation from panel to entity

### Manual Testing
- Create duplicate concepts
- Create orphaned concepts
- Verify visual indicators
- Test click-to-navigate

## Future Enhancements (Not in Scope)

- Custom validation rules (user-defined)
- Validation rule toggles (enable/disable specific checks)
- Export validation report (PDF/CSV)
- Validation history/trends
- Auto-fix suggestions with one-click repair
- Batch operations (fix all similar issues)

## References to Existing Patterns

### Button Styling (from bulk-create-enhancements.md)
- Use `btn-outline-primary` for secondary actions
- Use `btn-danger` for errors
- Use `btn-warning` for warnings

### Toast Notifications
- Success: `ToastService.ShowSuccess()`
- Warning: `ToastService.ShowWarning()`
- Error: `ToastService.ShowError()`

### Bootstrap Alert Classes
- `alert-danger` for errors
- `alert-warning` for warnings
- `alert-info` for information

### HashSet Pattern for Duplicates
- Use `StringComparer.OrdinalIgnoreCase`
- O(1) lookup performance
- From duplicate detection in bulk create

## Estimated Effort

- **Phase 1** (Service): 2-3 hours
- **Phase 2** (Panel): 2-3 hours
- **Phase 3** (Integration): 1-2 hours
- **Phase 4** (Indicators): 1-2 hours
- **Phase 5** (Real-time): 1 hour
- **Testing**: 2 hours
- **Total**: ~10-15 hours

## Success Criteria

✅ Validation runs automatically when ontology loads
✅ Issues displayed in collapsible panel
✅ Badge in top bar shows issue count
✅ Visual indicators on list view items
✅ Click-to-navigate works smoothly
✅ Performance remains good with 100+ concepts
✅ No false positives in duplicate detection
✅ Accessible via keyboard and screen reader
