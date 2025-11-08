# Global Search Feature - Development Plan

**Date**: November 7, 2025
**Status**: 🔵 In Progress
**Priority**: High
**Feature ID**: FEAT-GLOBAL-SEARCH

---

## Overview

Implement a macOS Spotlight-style global search feature that allows users to quickly find and navigate to concepts, relationships, and individuals across the entire ontology using a keyboard shortcut (Cmd+Shift+Space).

## Requirements

### Functional Requirements

1. **Keyboard Shortcut**: Cmd+Shift+Space opens the search overlay
2. **Search Scope**: Search across:
   - Concepts (Name, Definition, SimpleExplanation, Examples, Category)
   - Relationships (RelationType, Description)
   - Individuals (Name, Label, Description)
3. **Search Behavior**:
   - Substring matching (case-insensitive)
   - Real-time search as user types
   - Debounced to avoid excessive queries (300ms)
4. **Search Results**:
   - Display results grouped by type (Concepts, Relationships, Individuals)
   - Show relevant metadata (type, definition preview, etc.)
   - Highlight matching text
   - Keyboard navigation (Up/Down arrows)
   - Click or Enter to navigate to item
5. **UI/UX**:
   - Floating overlay centered on screen
   - macOS Spotlight-style appearance
   - Blur/dim background when open
   - ESC key closes the search
   - Click outside closes the search
   - Smooth animations

### Non-Functional Requirements

1. **Performance**: Search results appear within 100ms
2. **Accessibility**: Full keyboard navigation support
3. **Responsive**: Works on all screen sizes
4. **User Experience**: Intuitive, familiar macOS-style interface

## Architecture

### Component Structure

```
Components/Shared/
└── GlobalSearch.razor         # Main search component

Services/
└── GlobalSearchService.cs     # Search logic service
```

### Data Flow

1. User presses Cmd+Shift+Space
2. GlobalSearch component displays
3. User types search query
4. Debounced search triggers GlobalSearchService
5. Service searches current ontology data
6. Results returned and rendered
7. User selects result → Navigate to item
8. Component closes

## Design Decisions

### ADR-GS-001: Client-Side Search vs Server-Side Search

**Decision**: Use client-side search (in-memory)

**Context**:
- Ontology data is already loaded in OntologyView component
- Typical ontology size is small-medium (10-500 items)
- Real-time search experience is critical

**Alternatives**:
- Server-side API search endpoint
  - ❌ Adds network latency
  - ❌ Requires additional backend endpoint
  - ✅ Scalable for very large datasets

**Consequences**:
- ✅ Instant search results (no network delay)
- ✅ Simpler implementation
- ✅ Works offline/local development
- ❌ May need optimization for very large ontologies (1000+ items)

**Status**: Active

### ADR-GS-002: Search Algorithm

**Decision**: Use simple substring matching with LINQ

**Context**:
- Need fast, simple search across multiple fields
- Fuzzy matching not required initially

**Alternatives**:
- Fuzzy matching (Levenshtein distance)
  - ✅ Handles typos
  - ❌ More complex
  - ❌ Slower performance
- Full-text search engine
  - ✅ Advanced features
  - ❌ Overkill for this use case

**Consequences**:
- ✅ Simple, maintainable code
- ✅ Fast performance
- ❌ Exact substring match required
- 📝 Can enhance later if needed

**Status**: Active

### ADR-GS-003: State Management

**Decision**: Use component-local state with cascade parameter for ontology data

**Context**:
- Search component needs access to ontology data
- OntologyView already has this data

**Implementation**:
- Pass ontology as cascade parameter
- Component manages own show/hide state
- Keyboard shortcut registered in OntologyView

**Status**: Active

## Implementation Plan

### Phase 1: Backend Service (30 min)

**File**: `Services/GlobalSearchService.cs`

**Tasks**:
- [ ] Create GlobalSearchService with ILogger injection
- [ ] Implement SearchConcepts method (Name, Definition, SimpleExplanation, Examples, Category)
- [ ] Implement SearchRelationships method (RelationType, Description)
- [ ] Implement SearchIndividuals method (Name, Label, Description)
- [ ] Implement unified Search method returning grouped results
- [ ] Add XML documentation

**Search Result Model**:
```csharp
public class SearchResult
{
    public string Type { get; set; } // "Concept", "Relationship", "Individual"
    public int Id { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; } // Additional context
    public string MatchedText { get; set; } // What matched
}

public class SearchResults
{
    public List<SearchResult> Concepts { get; set; }
    public List<SearchResult> Relationships { get; set; }
    public List<SearchResult> Individuals { get; set; }
    public int TotalCount => Concepts.Count + Relationships.Count + Individuals.Count;
}
```

### Phase 2: UI Component (1 hour)

**File**: `Components/Shared/GlobalSearch.razor`

**Tasks**:
- [ ] Create component with CascadingParameter for Ontology
- [ ] Add IsVisible property and toggle methods
- [ ] Create search input with autofocus
- [ ] Implement debounced search (300ms)
- [ ] Render results grouped by type
- [ ] Add keyboard navigation (Up/Down/Enter/Esc)
- [ ] Add click handlers for navigation
- [ ] Style with macOS Spotlight appearance
- [ ] Add backdrop blur/dim effect
- [ ] Add smooth animations

**Component Interface**:
```razor
@inject GlobalSearchService SearchService
@inject NavigationManager Navigation

<CascadingParameter Name="Ontology" Type="Ontology" />

@code {
    private bool isVisible;
    private string searchQuery;
    private SearchResults results;
    private int selectedIndex;

    public void Show() { isVisible = true; }
    public void Hide() { isVisible = false; }
    private async Task PerformSearch(string query) { }
    private void NavigateToResult(SearchResult result) { }
    private void HandleKeyDown(KeyboardEventArgs e) { }
}
```

### Phase 3: Integration (30 min)

**File**: `Components/Pages/OntologyView.razor`

**Tasks**:
- [ ] Add GlobalSearch component reference
- [ ] Register keyboard shortcut (Cmd+Shift+Space and Ctrl+Shift+Space for Windows)
- [ ] Implement ShowGlobalSearch method
- [ ] Pass ontology as CascadingValue

**Keyboard Handler**:
```csharp
private async Task HandleKeyDown(KeyboardEventArgs e)
{
    // Cmd+Shift+Space (Mac) or Ctrl+Shift+Space (Windows)
    if ((e.MetaKey || e.CtrlKey) && e.ShiftKey && e.Code == "Space")
    {
        e.PreventDefault();
        globalSearch?.Show();
    }
}
```

### Phase 4: Styling (30 min)

**File**: `wwwroot/css/components/global-search.css`

**Tasks**:
- [ ] Create macOS Spotlight-style design
- [ ] Backdrop blur effect
- [ ] Search input styling
- [ ] Results list styling
- [ ] Hover and selected states
- [ ] Keyboard focus indicators
- [ ] Smooth transitions

**Visual Design**:
- Width: 600px
- Max height: 500px
- Border radius: 12px
- Shadow: Large, soft shadow
- Background: Semi-transparent with blur
- Input: Large, minimal design
- Results: Clean list with hover states

### Phase 5: Navigation Logic (30 min)

**Tasks**:
- [ ] Implement navigation to Concept (List View, highlight row)
- [ ] Implement navigation to Relationship (List View, highlight row)
- [ ] Implement navigation to Individual (List View, highlight row)
- [ ] Optionally switch to appropriate view mode
- [ ] Close search after navigation

### Phase 6: Testing (30 min)

**Tasks**:
- [ ] Test keyboard shortcut on Mac and Windows
- [ ] Test search across all entity types
- [ ] Test keyboard navigation
- [ ] Test ESC and click-outside closing
- [ ] Test with empty results
- [ ] Test with large result sets
- [ ] Test debouncing
- [ ] Test accessibility (keyboard-only usage)

## Technical Specifications

### Search Performance

- **Target**: < 100ms for search execution
- **Approach**: In-memory LINQ queries
- **Optimization**: Debounce user input (300ms)

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Cmd+Shift+Space (Mac) | Open search |
| Ctrl+Shift+Space (Windows) | Open search |
| ESC | Close search |
| Up Arrow | Previous result |
| Down Arrow | Next result |
| Enter | Navigate to selected result |

### Accessibility

- ARIA labels for search input
- ARIA live region for result count
- Keyboard focus management
- Clear focus indicators
- Screen reader announcements

## Dependencies

- None (uses existing infrastructure)

## Risks & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Keyboard shortcut conflicts | Medium | Low | Make configurable in user preferences |
| Performance with large ontologies | High | Medium | Add virtualization if > 500 results |
| Browser compatibility | Medium | Low | Test across browsers, provide fallback |

## Testing Strategy

### Manual Testing
- Keyboard shortcut registration
- Search accuracy across entity types
- Navigation to results
- UI/UX polish (animations, styling)

### Automated Testing
- Unit tests for GlobalSearchService
- Component tests for keyboard navigation

## Success Criteria

- [ ] User can open search with Cmd+Shift+Space
- [ ] Search returns results from concepts, relationships, and individuals
- [ ] Results appear within 100ms
- [ ] User can navigate results with keyboard
- [ ] User can navigate to selected item
- [ ] UI matches macOS Spotlight aesthetic
- [ ] Works on both Mac and Windows
- [ ] Fully keyboard accessible

## Timeline

**Estimated Time**: 3.5 hours

- Phase 1 (Backend): 30 min
- Phase 2 (UI Component): 1 hour
- Phase 3 (Integration): 30 min
- Phase 4 (Styling): 30 min
- Phase 5 (Navigation): 30 min
- Phase 6 (Testing): 30 min

## Future Enhancements

- Fuzzy matching for typo tolerance
- Search history (recent searches)
- Search filters (by type, by ontology)
- Search shortcuts (type prefix like "c:" for concepts only)
- Customizable keyboard shortcut in user preferences
- Search across all user's ontologies (not just current)

---

**Implementation Status**: Ready to begin
**Next Step**: Create GlobalSearchService.cs
