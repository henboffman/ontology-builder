# Eidos Development Ledger

This document tracks major development milestones, features, and changes to the Eidos Ontology Builder application.

---

## 2025-11-23 - Full Screen Graph View

### Features Added

#### 🖥️ Full Screen Mode for Graph Editing
An immersive full screen mode for the graph view that maximizes the canvas space and minimizes distractions, enabling users to focus entirely on graph editing and visualization.

**Key Capabilities:**
- **Full Screen Toggle**: Dedicated button to enter/exit full screen mode
- **Keyboard Shortcut**: `Option+F` (Mac) / `Alt+F` (Windows/Linux) for quick toggling
- **Focused Interface**: Shows only essential controls - graph canvas, quick actions, undo/redo, and exit button
- **Responsive Layout**: Content automatically fits viewport dimensions
- **Dark Mode Support**: Full screen mode respects theme preferences
- **Preserved State**: Graph layout, zoom level, and selections maintained when entering/exiting full screen

**Hidden Elements in Full Screen Mode:**
- Navigation sidebar
- View mode selector
- Ontology information panel
- Secondary toolbars
- Page header and breadcrumbs

**Visible Elements in Full Screen Mode:**
- Interactive graph canvas (maximized)
- Quick action buttons (Add Concept, Add Relationship)
- Undo/Redo buttons
- Exit full screen button (top-right corner)
- Keyboard shortcuts remain functional

#### 🎨 UI Components

**GraphView.razor** (`Components/Ontology/GraphView.razor`):
- Added `isFullScreen` state variable
- Full screen toggle button in graph header
- Conditional rendering of non-essential UI elements
- Exit full screen button overlay
- CSS classes for full screen styling
- JavaScript interop for full screen API integration

**graph-view.css** (`wwwroot/css/components/graph-view.css`):
- `.graph-full-screen` class for full viewport coverage
- `.graph-full-screen-overlay` for essential controls
- `.exit-fullscreen-btn` for exit button positioning
- Responsive height/width calculations
- z-index management for overlays
- Smooth transitions between modes

#### ⌨️ Keyboard Integration

**keyboardShortcuts.js** (`wwwroot/js/keyboardShortcuts.js`):
- Added `Option+F` / `Alt+F` shortcut handler
- Triggers `toggleFullScreen` action
- Cross-platform support (Mac Option key, Windows/Linux Alt key)
- Event prevention to avoid browser conflicts

**KeyboardShortcutsDialog.razor** (`Components/Shared/KeyboardShortcutsDialog.razor`):
- Added full screen shortcut to "Navigation" section
- Description: "Toggle full screen mode (Graph view)"
- Visual: `Alt/⌥ + F`

### Files Modified

#### UI Components
- `Components/Ontology/GraphView.razor` - Added full screen state and UI (~50 lines added)
- `Components/Shared/KeyboardShortcutsDialog.razor` - Added keyboard shortcut documentation (~7 lines)

#### Styles
- `wwwroot/css/components/graph-view.css` (NEW) - Full screen mode styling (~100 lines)

#### JavaScript
- `wwwroot/js/keyboardShortcuts.js` - Full screen keyboard shortcut (~8 lines)

### Technical Details

#### Implementation Approach
1. **State Management**: Added `isFullScreen` boolean to GraphView component
2. **CSS-Based Hiding**: Used conditional rendering and CSS classes to show/hide elements
3. **Viewport Sizing**: Graph canvas expands to `100vw` x `100vh` in full screen mode
4. **Action Preservation**: Quick actions and undo/redo remain accessible via floating overlay
5. **Exit Strategy**: Multiple exit methods - button, keyboard shortcut, Escape key

#### Full Screen Toggle Flow
1. User clicks full screen button or presses `Option+F`
2. `isFullScreen` state set to `true`
3. Component re-renders with full screen classes applied
4. Graph canvas expands to fill viewport
5. Non-essential UI elements hidden via conditional rendering
6. Essential controls shown in overlay
7. JavaScript triggers graph resize/refresh
8. Exit: User clicks exit button, presses `Option+F` again, or presses `Escape`

#### Responsive Behavior
- **Desktop**: Full viewport (100vw x 100vh)
- **Tablet**: Full viewport with touch-optimized controls
- **Mobile**: Full viewport with simplified action buttons

### Performance Considerations

- **No Re-initialization**: Graph instance preserved when toggling full screen
- **Efficient Re-render**: Only UI chrome changes, graph data unchanged
- **CSS Transitions**: Smooth mode transitions without JavaScript animations
- **Z-Index Management**: Proper layering prevents UI conflicts
- **Memory**: No additional graph instances created

### User Experience Enhancements

- **Uncluttered Interface**: Focus entirely on graph editing
- **Preserved Context**: All graph interactions remain available
- **Quick Exit**: Multiple exit options for user preference
- **Visual Feedback**: Exit button visually distinct and always accessible
- **Keyboard-First**: Full screen accessible entirely via keyboard
- **Tooltip Guidance**: Tooltips explain full screen toggle functionality
- **Dark Mode Integration**: Full screen respects theme preference

### Browser Compatibility

- Modern browsers with CSS viewport units support
- Fallback for browsers without full screen API
- Cross-platform keyboard shortcut support
- Touch-friendly controls for mobile browsers

###  Browser Fullscreen API Integration

**graphFullscreen.js** (`wwwroot/js/graphFullscreen.js` - NEW):
- Implements browser's native Fullscreen API with cross-browser support
- `enterFullscreen()`: Requests browser fullscreen with vendor prefixes (webkit, moz, ms)
- `exitFullscreen()`: Exits fullscreen mode
- `isFullscreen()`: Checks current fullscreen state
- `moveModalToFullscreen()`: Automatically moves Bootstrap modals into fullscreen container
- `restoreModal()`: Returns modals to original DOM positions when exiting fullscreen
- Fullscreen change event listeners to sync UI state with browser fullscreen
- ESC key support - automatically syncs Blazor state when user exits via ESC

**Modal Dialog Support**:
When in browser fullscreen mode, only the fullscreen element and its children are visible. Bootstrap modal dialogs are typically rendered at the body level, making them invisible during fullscreen. The solution:

- Event listeners detect `shown.bs.modal` events
- Modals are automatically moved into the fullscreen container with high z-index (10002)
- Modal backdrops moved into fullscreen container (z-index 10001)
- Original parent elements tracked in a Map for proper restoration
- Modals restored to original positions on `hidden.bs.modal` or fullscreen exit
- No user-visible disruption - modals appear normally during fullscreen

**Files Modified**:
- `Components/Ontology/GraphView.razor:242-267` - Added IJSRuntime injection and Fullscreen API calls
- `Components/App.razor:56` - Added graphFullscreen.js script reference
- `wwwroot/js/graphFullscreen.js` (NEW) - Full browser Fullscreen API implementation (~140 lines)

### Future Enhancements

- Custom full screen toolbar configuration
- Save full screen preference per user
- Full screen mode for other views (Hierarchy, Mind Map)
- Picture-in-picture mode for multi-ontology comparison

---

## 2025-11-07 - Global Search Feature

### Features Added

#### 🔍 Global Search with Spotlight-Style Interface
A powerful universal search feature that enables users to quickly find any entity in their ontology using a macOS Spotlight-inspired interface, accessible from anywhere in the application.

**Key Capabilities:**
- **Universal Search**: Search across Concepts, Relationships, and Individuals simultaneously
- **Instant Access**: Keyboard shortcut `Cmd+Shift+Space` (Mac) or `Ctrl+Shift+Space` (Windows/Linux)
- **Real-time Results**: 300ms debounced search with instant result updates as you type
- **Smart Navigation**: Automatic view switching to List view and item highlighting on selection
- **Keyboard-First Design**: Full keyboard navigation with arrow keys, Enter, and Esc
- **Grouped Results**: Results organized by entity type (Concepts, Relationships, Individuals)
- **Visual Feedback**: Icons, badges, and hover states for intuitive navigation
- **Focus Management**: Automatic focus restoration prevents "touchy" keyboard shortcut behavior

#### 🎨 UI Components

**GlobalSearch.razor** (`Components/Shared/GlobalSearch.razor`):
- Spotlight-style modal overlay with backdrop blur
- Search input with icon and clear button
- Grouped result sections with headers and counts
- Keyboard navigation (arrow keys, Enter, Esc)
- Mouse hover selection
- Empty states for no results and no query
- Footer with keyboard shortcut hints
- Smooth animations (fadeIn, slideDown)
- Full dark mode support

**global-search.css** (`wwwroot/css/components/global-search.css`):
- macOS Spotlight-inspired styling
- Responsive design (mobile and desktop)
- Custom scrollbar styling
- Hover and selected states
- Badge and icon styling
- Animation keyframes
- Theme-aware colors using CSS variables

#### 🔧 Service Layer

**GlobalSearchService** (`Services/GlobalSearchService.cs`):
- `Search(Ontology, string query)`: Main search method returning SearchResults
- **Multi-field matching**: Searches across all relevant entity fields
  - Concepts: Name, Definition, SimpleExplanation, Examples, Category, Type
  - Relationships: RelationshipType, Description
  - Individuals: Name, Label, Description
- **Case-insensitive search**: User-friendly querying
- **Partial matching**: Find "auth" in "Authentication"
- **Result ranking**: Orders results by entity type
- **Matched text tracking**: Shows which field matched the query

**Search Models** (`Models/SearchResult.cs`, `Models/SearchResults.cs`):
- `SearchResult`: Individual result with Title, Subtitle, Icon, MatchedText, EntityId, EntityType
- `SearchResults`: Container with Concepts, Relationships, Individuals collections
- `TotalCount` property for quick result counting
- `All()` method for flattened result list

#### ⌨️ Keyboard Integration

**OntologyView.razor** (`Components/Pages/OntologyView.razor`):
- Integrated GlobalSearch component
- Keyboard handler for Cmd+Shift+Space / Ctrl+Shift+Space
- `HandleGlobalSearch()` method to show/hide search
- `HandleSearchResultSelected()` method for navigation
- Focus restoration after search closes
- Toast notification on result selection

**keyboardShortcuts.js** (`wwwroot/js/keyboardShortcuts.js`):
- JavaScript handler for global keyboard shortcut
- Prevents default browser behavior
- Calls Blazor component method via DotNet reference
- Cross-platform support (Mac Cmd key, Windows/Linux Ctrl key)

### Files Modified

#### Services
- `Services/GlobalSearchService.cs` (NEW) - Search logic and result ranking (~150 lines)

#### Models
- `Models/SearchResult.cs` (NEW) - Individual search result model
- `Models/SearchResults.cs` (NEW) - Search results container

#### UI Components
- `Components/Shared/GlobalSearch.razor` (NEW) - Search UI component (~270 lines)
- `Components/Pages/OntologyView.razor` - Integrated search, keyboard handler (~30 lines added)

#### Styles
- `wwwroot/css/components/global-search.css` (NEW) - Spotlight-style CSS (~240 lines)
- `Components/App.razor` - Added global-search.css reference (1 line)

#### JavaScript
- `wwwroot/js/keyboardShortcuts.js` - Global search keyboard shortcut (~10 lines)

### Technical Details

#### Search Algorithm
1. **Query Processing**: Trim and validate search query (minimum 1 character)
2. **Multi-Field Matching**: Search across all relevant fields for each entity type
3. **Case-Insensitive**: Use `StringComparison.OrdinalIgnoreCase` for all comparisons
4. **Result Building**: Create SearchResult objects with Title, Subtitle, Icon, MatchedText
5. **Grouping**: Organize results by entity type (Concepts, Relationships, Individuals)
6. **Return**: Wrapped in SearchResults container with TotalCount

#### Keyboard Shortcut Implementation
- **JavaScript**: Listens for `Cmd+Shift+Space` (Mac) or `Ctrl+Shift+Space` (Windows/Linux)
- **Blazor Interop**: Calls C# method via DotNet reference
- **Focus Management**: Automatically focuses search input when shown, restores focus when closed
- **Debounce**: 300ms delay on search input to reduce unnecessary processing

#### Focus Restoration Fix
Previous implementation had a "touchy" keyboard shortcut that would close immediately after opening. Fixed by:
1. Removing automatic focus restoration in `Hide()` method
2. Adding `OnHide` event callback from GlobalSearch to parent
3. Parent component manages focus restoration timing
4. Prevents keyboard shortcut from immediately closing the dialog

#### Navigation Flow
1. User presses keyboard shortcut → Search dialog appears
2. User types query → Results update in real-time (300ms debounce)
3. User navigates with arrow keys or mouse → Selection highlights
4. User presses Enter or clicks result → Navigation begins
5. View switches to List view → Item is highlighted
6. Toast notification confirms navigation → Search dialog closes
7. Focus restored to main view → User can continue working

### Performance Considerations

- **In-Memory Search**: All search operations use LINQ on already-loaded ontology data
- **No Backend Calls**: Search doesn't trigger database queries or API calls
- **Debounced Input**: 300ms delay reduces unnecessary search executions
- **Efficient Matching**: Early termination when first match is found for MatchedText
- **Minimal DOM**: Results virtualized via Blazor's efficient rendering
- **CSS Animations**: Lightweight transitions (opacity, transform) using GPU acceleration

### User Experience Enhancements

- **Spotlight-Style UI**: Familiar interface for macOS users, intuitive for all users
- **Visual Icons**: Each entity type has distinct icon (bi-diagram-3, bi-arrow-left-right, bi-person-fill)
- **Context Preview**: Subtitle shows definition/description for context
- **Match Indicators**: Badge shows which field matched (e.g., "NAME", "DEFINITION")
- **Keyboard Shortcuts Legend**: Footer displays available keyboard actions
- **Smooth Animations**: fadeIn for backdrop, slideDown for dialog
- **Responsive Design**: Works on mobile, tablet, and desktop

### Accessibility

- **Keyboard-First**: Fully navigable without mouse
- **Semantic HTML**: Proper HTML elements for screen readers
- **Focus Management**: Clear focus indicators and logical focus order
- **High Contrast**: Dark mode support with theme-aware colors
- **ARIA Attributes**: Proper labeling for assistive technologies
- **Escape Key**: Consistent close behavior across all contexts

### Documentation Updates

#### User Guide
- Added comprehensive "Global Search" section to USER_GUIDE.md
- Updated Table of Contents with new section
- Added search keyboard shortcuts to shortcuts table
- Included search tips and example workflows

#### Release Notes
- Created RELEASE_SUMMARY_2025-11-07.md with full deployment guide
- Documented features, testing status, deployment steps
- Added troubleshooting section for common issues
- Included user training demo script

### Testing Performed

- ✅ Keyboard shortcut works from all view modes (Graph, List, TTL, Notes, Templates)
- ✅ Search returns correct results for concepts, relationships, individuals
- ✅ Case-insensitive search works correctly
- ✅ Partial matching works as expected
- ✅ Real-time search with 300ms debounce
- ✅ Keyboard navigation (arrow keys, Enter, Esc)
- ✅ Mouse navigation and hover states
- ✅ Result selection navigates to correct item in List view
- ✅ Toast notification displays selected item
- ✅ Focus restoration prevents "touchy" keyboard shortcut
- ✅ Dark mode styling works correctly
- ✅ Mobile responsive design verified
- ✅ Cross-browser testing (Chrome, Firefox, Safari)

### Future Enhancements

Potential improvements for future iterations:
- **Search History**: Remember recent searches for quick access
- **Fuzzy Matching**: Tolerate typos and spelling variations
- **Advanced Filters**: Filter by entity type, category, or date
- **Search Highlighting**: Highlight matched text in results
- **Recent Items**: Show recently viewed items when query is empty
- **Search Analytics**: Track popular searches and optimize results
- **Keyboard Shortcuts in Results**: Show entity-specific actions (edit, delete)
- **Multi-Select**: Select multiple results for batch operations

---

## 2025-11-05 - Concept Property Definitions (OWL Properties)

### Features Added

#### 🎯 Concept Property Definition System
A comprehensive system for defining OWL-compliant property schemas on concepts, enabling proper semantic modeling with DataProperty and ObjectProperty types.

**Key Capabilities:**
- **Property Type Specification**: Define properties as DataProperty (literal values) or ObjectProperty (references to other individuals)
- **Data Type Constraints**: Specify data types for DataProperty (string, integer, decimal, boolean, date, anyURI)
- **Range Specification**: For ObjectProperty, specify which concept type the property points to
- **Cardinality Controls**: Mark properties as required (IsRequired) or single-valued (IsFunctional)
- **Full OWL Export**: Properties export to TTL format with proper owl:DatatypeProperty and owl:ObjectProperty declarations
- **JSON Export**: Properties included in JSON export for data interchange
- **Clone Support**: Property definitions preserved when cloning ontologies

#### 📊 UI Components

**ConceptPropertyManager.razor** (`Components/Ontology/Admin/ConceptPropertyManager.razor`):
- Integrated into Admin Concept Dialog
- Add/edit/delete property definitions
- Property type selector (DataProperty vs ObjectProperty)
- Data type dropdown for DataProperty
- Concept selector for ObjectProperty range
- IsRequired and IsFunctional checkboxes
- URI input for custom property identifiers
- Description field for documentation

#### 🔧 Service Layer

**ConceptPropertyService** (`Services/ConceptPropertyService.cs`):
- `CreatePropertyAsync()`: Create new property definition with validation
- `UpdatePropertyAsync()`: Update existing property definition
- `DeletePropertyAsync()`: Remove property definition
- `GetPropertiesByConceptIdAsync()`: Retrieve all properties for a concept
- `GetPropertyByIdAsync()`: Get single property by ID
- Full validation of property types and ranges

**ConceptPropertyRepository** (`Data/Repositories/ConceptPropertyRepository.cs`):
- CRUD operations for ConceptProperty entities
- Efficient loading with Include for RangeConcept navigation
- Proper EF Core tracking and change detection

#### 📤 Export Enhancements

**TTL Export** (`Services/TtlExportService.cs`):
- Exports owl:DatatypeProperty declarations with rdfs:domain and rdfs:range
- Exports owl:ObjectProperty declarations with rdfs:domain and range concept
- Includes owl:FunctionalProperty declarations for IsFunctional properties
- Full OWL 2 compliance with proper URIs and namespacing

**JSON Export** (`Services/Export/JsonExportStrategy.cs`):
- Added ConceptProperties collection to concept export
- Exports property name, type, data type, range concept name
- Includes IsRequired, IsFunctional, Description, Uri, timestamps
- ConceptPropertyExportModel for clean JSON structure

### Database Changes

**Migration: AddConceptPropertyDefinitions**
- New table: `ConceptProperties`
  - Primary key: Id
  - Foreign keys: ConceptId (required), RangeConceptId (nullable for ObjectProperty)
  - Columns: Name, PropertyType, DataType, IsRequired, IsFunctional, Description, Uri, CreatedAt, UpdatedAt
  - Index on ConceptId for efficient concept property lookups
  - Index on RangeConceptId for relationship queries

### Files Modified

#### Database & Models
- `Models/ConceptProperty.cs` - New model with PropertyType enum (DataProperty, ObjectProperty)
- `Models/Concept.cs` - Added ConceptProperties navigation property
- `Data/OntologyDbContext.cs` - Added ConceptProperties DbSet and relationships
- `Migrations/20251105014621_AddConceptPropertyDefinitions.cs` - Migration file

#### Services
- `Services/ConceptPropertyService.cs` (NEW) - Business logic for property management
- `Services/Interfaces/IConceptPropertyService.cs` (NEW) - Service interface
- `Services/TtlExportService.cs` - Enhanced TTL export with property definitions
- `Services/Export/JsonExportStrategy.cs` - JSON export with ConceptProperties
- `Services/OntologyService.cs` - Clone functionality updated for properties

#### Repositories
- `Data/Repositories/ConceptPropertyRepository.cs` (NEW) - Data access for properties
- `Data/Repositories/Interfaces/IConceptPropertyRepository.cs` (NEW) - Repository interface
- `Data/Repositories/OntologyRepository.cs` - Added ConceptProperties eager loading

#### UI Components
- `Components/Ontology/Admin/ConceptPropertyManager.razor` (NEW) - Property management UI
- `Components/Ontology/Admin/AdminConceptDialog.razor` - Integrated property manager

### Technical Details

#### Property Type System
**DataProperty** - Properties with literal values:
- Supported data types: string, integer, decimal, boolean, date, anyURI
- Exports to owl:DatatypeProperty with xsd:datatype range
- Example: "age" property with integer datatype

**ObjectProperty** - Properties referencing other individuals:
- Range specified as a Concept from the ontology
- Exports to owl:ObjectProperty with concept URI as range
- Example: "hasAuthor" property with range "Person" concept

#### OWL Export Format
```turtle
# DataProperty example
:hasAge rdf:type owl:DatatypeProperty ;
        rdfs:domain :Person ;
        rdfs:range xsd:integer ;
        owl:FunctionalProperty true ;
        rdfs:comment "The person's age in years" .

# ObjectProperty example
:hasAuthor rdf:type owl:ObjectProperty ;
           rdfs:domain :Book ;
           rdfs:range :Person ;
           rdfs:comment "The author who wrote the book" .
```

#### Clone Functionality Fix
**Problem**: When cloning ontologies, ConceptProperty foreign keys weren't being set correctly.

**Solution** (OntologyService.cs lines 308-355):
1. Clone ConceptProperty objects and add to concept collection
2. Save concepts to database (assigns IDs)
3. Explicitly set `ConceptId` on each property
4. Remap `RangeConceptId` to point to cloned concept IDs using concept mapping dictionary
5. Save changes to persist foreign keys

```csharp
// Remap foreign keys after concepts have IDs
foreach (var clonedConcept in clonedConcepts)
{
    foreach (var conceptProperty in clonedConcept.ConceptProperties)
    {
        conceptProperty.ConceptId = clonedConcept.Id;

        if (conceptProperty.RangeConceptId.HasValue &&
            conceptMapping.ContainsKey(conceptProperty.RangeConceptId.Value))
        {
            conceptProperty.RangeConceptId = conceptMapping[conceptProperty.RangeConceptId.Value];
        }
    }
}
```

### User Benefits

1. **Semantic Compliance**: Create OWL 2 compliant ontologies with proper property definitions
2. **Protégé Compatibility**: Export to TTL and import into Protégé with full property schema intact
3. **Type Safety**: Define expected data types for properties, improving data quality
4. **Reusability**: Define properties once on concepts, use for all individuals of that type
5. **Documentation**: Property descriptions help explain the ontology structure
6. **Validation Ready**: Foundation for future property value validation on individuals

### Known Limitations & Future Enhancements

**Current Limitations:**
- Individual editor does not yet use ConceptProperty definitions for form generation
- No runtime validation of property values against definitions
- No support for property restrictions (allValuesFrom, someValuesFrom)
- No inverse property declarations
- No property chains or characteristics (transitive, symmetric, etc.)

**Potential Future Enhancements:**
- Dynamic individual property forms based on ConceptProperty definitions
- Property value validation against data types and ranges
- OWL restriction support (allValuesFrom, someValuesFrom, hasValue)
- Inverse property declarations (owl:inverseOf)
- Property characteristics (owl:TransitiveProperty, owl:SymmetricProperty)
- Property chains for complex inferences
- Import ConceptProperty definitions from TTL files
- Bulk property definition import

### Testing Recommendations

**Manual Testing Scenarios:**
1. **Create DataProperty**:
   - Create concept "Person"
   - Add DataProperty "age" with integer datatype
   - Mark as required and functional
   - Export to TTL, verify owl:DatatypeProperty declaration

2. **Create ObjectProperty**:
   - Create concepts "Book" and "Author"
   - Add ObjectProperty "hasAuthor" on Book with range Author
   - Export to TTL, verify owl:ObjectProperty with range

3. **Clone Ontology**:
   - Create ontology with concepts and properties
   - Clone ontology
   - Verify all properties copied with correct foreign keys
   - Verify RangeConceptId remapped to new concept IDs

4. **JSON Export**:
   - Create properties on concepts
   - Export to JSON
   - Verify conceptProperties array includes all properties
   - Verify rangeConceptName populated for ObjectProperty

5. **Edit Properties**:
   - Create property definition
   - Edit name, type, description
   - Verify changes persist
   - Delete property, verify removal

### Performance Considerations

- Properties eagerly loaded with concepts using `Include()` to avoid N+1 queries
- OntologyRepository uses `AsSplitQuery()` to prevent cartesian explosion
- ConceptPropertyService validates property uniqueness per concept
- Clone operation batches property creation for efficiency

---

## 2025-11-01 - UI/UX Enhancements: List View Tabs & Interactive Validation

**Update 3**: Comprehensive improvements to List View organization and validation workflow

### Features Added

#### 📑 Tabbed List View
Enhanced the List view with a tabbed interface separating Concepts and Relationships for better organization and clarity.

**Key Features:**
- **Horizontal Tab Navigation**: Clean tabs for switching between Concepts and Relationships
- **Consistent Design**: Matches the existing tab styling throughout the application
- **Active State Indicators**: Visual feedback showing which tab is active
- **Icon Integration**: Bootstrap icons for visual identification (lightbulb for Concepts, diagram for Relationships)
- **Dark Mode Support**: Full theme support with proper contrast and styling
- **Responsive Design**: Adapts to mobile and tablet screen sizes

**Benefits:**
- Reduces visual clutter when working with large ontologies
- Allows users to focus on one entity type at a time
- Improves navigation and cognitive load
- Consistent with modern UI patterns

#### ✅ Interactive Validation Panel
Made validation issues clickable with smooth scrolling to the problematic items in List view.

**Key Features:**
- **Clickable Issues**: Click any validation issue to jump to the item
- **Smart Navigation**: Only works on List view where items are visible
- **Smooth Scrolling**: Animated scroll with centering
- **Visual Highlight**: 2-second pulsing highlight on the target element
- **Hover Effects**: Issues highlight on hover for better UX
- **Element Identification**: Automatically identifies concepts vs relationships

**User Experience:**
- Issues are styled as clickable cards with cursor pointer
- Hover shows subtle background color change
- Clicked item gets highlighted with blue border and fade animation
- Scroll centers the item in viewport for optimal visibility

#### 🔄 Auto-Refresh Validation
Validation now automatically updates whenever users make changes to the ontology.

**Triggers:**
- Creating a new concept
- Updating an existing concept
- Deleting a concept
- Creating/updating relationships
- Deleting relationships
- Undo/Redo operations
- Any operation that modifies ontology data

**Benefits:**
- Always see up-to-date validation status
- Immediate feedback on changes
- No manual refresh needed
- Real-time issue count updates

### Files Modified

#### Components
- `Components/Ontology/ListView.razor` (Modified)
  - Lines 1-23: Added tabbed navigation wrapper
  - Lines 24-197: Wrapped Concepts section in tab pane
  - Lines 199-308: Wrapped Relationships section in tab pane
  - Line 316: Added `activeListTab` state variable
  - Lines 529-603: Added tab styling (CSS)

- `Components/Pages/OntologyView.razor` (Modified)
  - Lines 465-472: Made validation issues clickable with handlers
  - Line 1316: Added validation refresh to `LoadOntology()`
  - Line 1326: Added `hoveredIssueId` state variable
  - Lines 1328-1341: Added `HandleValidationIssueClick()` method

#### CSS
- `wwwroot/css/ontology-tabs-layout.css` (Modified)
  - Lines 216-234: Added validation issue hover and highlight styles
  - Includes `.validation-issue-item:hover` styling
  - Includes `.validation-highlight` animation
  - Includes `@keyframes highlight-pulse` animation

#### JavaScript
- `wwwroot/js/validation-helpers.js` (Existing - No Changes)
  - Already had `scrollToElement()` function
  - Handles smooth scroll and highlight animation

### Technical Details

#### Tab Implementation
The tab system uses a simple state-based approach:
- `activeListTab` string variable tracks which tab is active
- Conditional classes (`active`) applied based on state
- Display controlled with `display: none` vs `display: block`
- CSS transitions provide smooth visual feedback

#### Validation Click Handler
```csharp
private async Task HandleValidationIssueClick(ValidationIssue issue)
{
    // Only handle clicks if we're on the List view
    if (currentView == "List" && issue.EntityId.HasValue)
    {
        // Determine the element ID based on entity type
        string elementId = issue.EntityType == "Concept"
            ? $"concept-{issue.EntityId.Value}"
            : $"relationship-{issue.EntityId.Value}";

        // Use JavaScript to scroll to the element
        await JSRuntime.InvokeVoidAsync("scrollToElement", elementId);
    }
}
```

#### Validation Auto-Refresh
Modified `LoadOntology()` to always call `LoadValidation()`:
```csharp
private async Task LoadOntology()
{
    ontology = await OntologyService.GetOntologyAsync(Id);

    if (graphView != null && viewMode == ViewMode.Graph)
    {
        await Task.Delay(100);
        await graphView.RefreshGraph();
    }

    // Refresh validation after loading ontology
    await LoadValidation();
}
```

### User Experience Improvements

1. **Reduced Cognitive Load**: Separate tabs keep concepts and relationships organized
2. **Immediate Feedback**: Auto-refreshing validation shows issues as they occur
3. **Quick Issue Resolution**: Click to jump directly to problematic items
4. **Visual Clarity**: Highlight animation makes it obvious which item was clicked
5. **Consistent Interface**: Tab design matches rest of application

### Testing Recommendations

- Verify tab switching works smoothly
- Test validation click on both concepts and relationships
- Confirm highlight animation displays correctly
- Check dark mode styling
- Validate mobile/tablet responsiveness
- Test validation refresh on all CRUD operations

---

## 2025-11-01 - Bulk Creation Feature with Spreadsheet-Like Interface

**Update 2**: Added direct Excel paste support into grid and improved button visibility

### Features Added

#### 📊 Bulk Creation Dialog
A comprehensive bulk creation system that allows users to create multiple concepts or relationships at once using a spreadsheet-like interface.

**Key Capabilities:**
- **Two Creation Modes**:
  - **Concepts Only**: Single-column entry for quickly adding multiple concept names
  - **Relationships**: Create relationship types or full triples (Subject | Relationship | Object)

- **Relationship Formats**:
  - **Simple Mode**: Just relationship type names (e.g., manages, employs, owns)
  - **Full Triple Mode**: Complete relationship definitions with auto-concept creation

- **Spreadsheet-Like Grid**:
  - Editable table interface for relationship triples
  - Tab to move between cells, Enter for new row
  - Add/remove rows dynamically
  - Real-time validation with error highlighting

- **Paste Support**:
  - **Direct Grid Paste**: Copy rows from Excel and paste directly into the grid (NEW in Update 2)
  - Automatic tab-separated parsing (from Excel/Sheets)
  - Parse pipe-delimited format (Subject | Relationship | Object)
  - Bulk import from text area
  - Toast notifications confirm successful paste

- **Auto-Concept Creation**:
  - When creating relationship triples, missing concepts are automatically created
  - Preview shows which new concepts will be created
  - Concepts created first, then relationships

- **Multi-Step Workflow**:
  1. **Mode Selection**: Choose concepts vs relationships
  2. **Data Entry**: Textarea or grid input with paste support
  3. **Preview**: Review items and new concepts before creation
  4. **Processing**: Progress bar with real-time updates
  5. **Results**: Success count, error summary, and option to create more

- **User Experience Enhancements**:
  - Pro tips with keyboard shortcuts
  - Line/row counters
  - Valid item counts
  - Progress tracking (percentage and current/total)
  - Error collection and display
  - Success toast notifications

#### 🎨 UI Components

**BulkCreateDialog.razor** (`Components/Ontology/BulkCreateDialog.razor`):
- Modal dialog with 5-step wizard interface
- Responsive card-based mode selection
- Textarea for simple bulk entry
- Editable grid for relationship triples
- Preview table with validation
- Animated progress indicator
- Success/error summary

**OntologyView.razor** (Modified):
- Added "Bulk Create" button to action panel
- Button positioned after "Add Concept" and "Add Relationship"
- **Warning color styling** (yellow/orange) for high visibility (Update 2)
- Permission-aware (disabled if user can't add)

### Files Modified

#### UI Components
- `Components/Ontology/BulkCreateDialog.razor` (NEW - 850+ lines)
  - Complete wizard-based bulk creation interface
  - Handles both concepts and relationships
  - Multi-format support (text, grid, paste)
  - **Direct grid paste handler** with clipboard API (Update 2 - line 601-681)
  - Auto-parse tab-separated and pipe-delimited data

- `Components/Pages/OntologyView.razor` (Modified)
  - Line 613-618: Added "Bulk Create" button
  - Line 1003: Added `showBulkCreate` flag
  - Line 1287-1300: Added `ShowBulkCreateDialog()` and `OnBulkCreateComplete()` methods
  - Line 187-194: Added BulkCreateDialog component instance

#### Documentation
- `DEVELOPMENT_LEDGER.md` (This entry)

### Technical Details

#### Direct Excel Paste (Update 2)

**Feature**: Users can now copy data from Excel/Google Sheets and paste directly into the grid without using the textarea.

**Implementation**:
```csharp
private async Task HandleGridPaste(ClipboardEventArgs e)
{
    // Get clipboard via JS interop
    var clipboardText = await JSRuntime.InvokeAsync<string>("navigator.clipboard.readText");

    // Parse tab-separated (Excel) or pipe-delimited data
    foreach (var line in lines)
    {
        if (line.Contains('\t'))
            parts = line.Split('\t');  // Excel format
        else if (line.Contains('|'))
            parts = line.Split('|');   // Pipe format

        // Create RelationshipTriple from parsed parts
        relationshipTriples.Add(new RelationshipTriple {
            Subject = parts[0],
            Relationship = parts[1],
            Object = parts[2]
        });
    }
}
```

**User Experience**:
1. Copy rows from Excel (Subject, Relationship, Object columns)
2. Click on the grid area (it has `tabindex="0"` for focus)
3. Press Ctrl+V (or Cmd+V on Mac)
4. Grid instantly populates with parsed data
5. Toast notification shows "Pasted X rows from clipboard"
6. Empty rows added at end for manual additions

**Button Visibility Enhancement**:
- Changed from `btn-outline-primary` (subtle blue outline)
- To `btn-warning text-dark` (yellow/orange with dark text)
- Much more prominent in the action panel
- Stands out between green "Add Concept" and blue "Add Relationship"

#### Bulk Creation Workflow

**Concepts Mode:**
1. User enters concept names (one per line) in textarea
2. Preview shows all concepts to be created
3. Click "Create All" to add concepts via `ConceptService.CreateConceptAsync()`
4. Progress updates in real-time
5. Success summary shows created count

**Relationships - Simple Mode:**
1. User enters relationship type names (one per line)
2. Preview shows all types
3. Types are validated and made available for use
4. Useful for pre-defining common relationship vocabularies

**Relationships - Full Triple Mode:**
1. User enters triples in grid or pastes from spreadsheet
2. System parses format: `Subject | Relationship | Object` or tab-separated
3. Validates all fields are present
4. Identifies concepts that don't exist in ontology
5. Preview shows:
   - All triples to be created
   - New concepts that will be auto-created
6. Creation process:
   - First: Create missing concepts via `ConceptService.CreateConceptAsync()`
   - Reload concepts to get new IDs
   - Second: Create relationships via `RelationshipService.CreateRelationshipAsync()`
   - Lookup concept IDs using case-insensitive dictionary
7. Progress tracking for both concepts and relationships
8. Error collection with detailed messages

#### Paste Parsing Logic
Supports multiple formats:
- **Pipe-delimited**: `Dog | is-a | Mammal`
- **Tab-separated**: `Dog\tis-a\tMammal` (from Excel/Sheets)
- **Single column**: Just relationship types (Simple mode)

Parser splits by delimiter, trims whitespace, validates field count.

#### Grid Interaction
- Each row is a `RelationshipTriple` object with Subject, Relationship, Object properties
- Keyboard shortcuts:
  - **Enter**: Move to next row (auto-add if on last row)
  - **Tab**: Move to next cell
- Validation marks rows as error if any field is missing
- Error messages displayed inline beneath invalid rows

#### Permission Checks
Bulk create respects ontology permissions:
- Button disabled if `!CanAdd()` returns false
- Uses existing permission checking infrastructure
- Same permissions as individual concept/relationship creation

### Design Patterns & Code Style

**Patterns Used:**
- **Wizard/Stepper Pattern**: Multi-step modal with clear progression
- **Repository Pattern**: Uses `IConceptService` and `IRelationshipService` (existing)
- **Error Collection**: Captures errors without stopping batch process
- **Progress Reporting**: Real-time progress updates with percentage calculation
- **Auto-Complete**: Creates dependent entities (concepts) before main entities (relationships)

**Code Style Consistency:**
- Bootstrap 5 classes for styling (consistent with app)
- Icon usage from Bootstrap Icons (`bi-table`, `bi-plus-circle`, etc.)
- Small button sizes (`btn-sm`) for compact UI
- Toast notifications for user feedback
- `@code` block organization following existing Eidos conventions
- Logging pattern would follow existing `Logger.LogInformation()` style (not yet implemented)

**Error Handling:**
- Try-catch around individual item creation to prevent one error from stopping batch
- Error messages collected in list
- Display up to 5 errors in summary, indicate if more exist
- Continue processing remaining items after error

### User Benefits

1. **Efficiency**: Create dozens of concepts/relationships in seconds instead of one-by-one
2. **Spreadsheet Familiarity**: Users can prepare data in Excel/Sheets and paste
3. **Reduced Friction**: Auto-create concepts eliminates need to create them separately first
4. **Visibility**: Preview step prevents accidental bulk creation
5. **Error Recovery**: Partial success still creates valid items, shows errors for manual fix
6. **Flexibility**: Multiple input methods (textarea, grid, paste) suit different workflows

### Known Limitations & Future Enhancements

**Current Limitations:**
- Simple relationship mode doesn't actually store types as reusable templates (would need new table)
- No import/export of bulk data to CSV
- Grid doesn't have advanced Excel features (copy/paste within grid, drag-fill, etc.)
- No undo for bulk operations (would need batch command pattern)

**Potential Future Enhancements:**
- Import from CSV/Excel files directly
- Export validation errors to file
- Template saving for common bulk patterns
- Bulk edit (update existing items)
- Bulk delete with multi-select
- Drag-and-drop file upload
- Relationship type storage and autocomplete
- Individual property bulk setting (category, color for concepts)
- Relationship property bulk setting (custom labels, bidirectionality)

### Testing Recommendations

**Manual Testing Scenarios:**
1. **Concepts - Simple Entry**:
   - Enter 5-10 concept names
   - Verify preview shows all
   - Verify all created successfully

2. **Concepts - Paste from Spreadsheet**:
   - Copy column of names from Excel
   - Paste into textarea
   - Verify parsing works correctly

3. **Relationships - Simple Mode**:
   - Enter relationship types like "manages", "employs"
   - Verify they appear in relationship editor dropdown

4. **Relationships - Full Triples (All Concepts Exist)**:
   - Create concepts: Dog, Cat, Mammal
   - Bulk create: `Dog | is-a | Mammal`, `Cat | is-a | Mammal`
   - Verify both relationships created

5. **Relationships - Full Triples (Auto-Create Concepts)**:
   - Bulk create: `Apple | is-a | Fruit`, `Banana | is-a | Fruit`
   - Verify "Fruit" preview shows as new concept
   - Verify all 3 items created (1 concept, 2 relationships)

6. **Error Handling**:
   - Try creating duplicate concepts
   - Try creating self-referencing relationships
   - Verify errors shown but other items created

7. **Permission Checks**:
   - Open ontology with view-only access
   - Verify Bulk Create button is disabled

8. **Progress Tracking**:
   - Create 20+ items
   - Verify progress bar animates smoothly
   - Verify counts update correctly

### Performance Considerations

- Creates items sequentially (not parallel) to avoid race conditions
- Uses small delay (`await Task.Delay(10)`) to allow UI updates
- For very large batches (100+), consider:
  - Batch chunking
  - Background job processing
  - WebSocket/SignalR progress updates
- Current implementation suitable for typical usage (5-50 items)

---

## 2025-10-31 - Collaboration Board & Automated Group Management

### Features Added

#### 🤝 Collaboration Board System
- **CollaborationBoardService**: Comprehensive service for managing collaboration posts and responses
  - `GetActivePostsAsync()`: Get all active collaboration posts
  - `SearchPostsAsync()`: Search and filter posts by domain, skill level, keywords
  - `GetPostDetailsAsync()`: Get detailed post information with view tracking
  - `GetMyPostsAsync()`: Get user's own collaboration posts
  - `CreatePostAsync()`: Create post with automatic group creation and permission grant
  - `UpdatePostAsync()`: Update post details
  - `DeletePostAsync()`: Delete collaboration post
  - `TogglePostActiveStatusAsync()`: Pause/resume recruiting
  - `AddResponseAsync()`: Apply to collaboration projects
  - `GetPostResponsesAsync()`: Get all responses for a post
  - `UpdateResponseStatusAsync()`: Accept/reject responses with automatic group membership

#### 🔑 Automated Permission Workflow
- **Automatic Group Creation**: When creating a collaboration post:
  - Creates user group named "Collaboration: [Project Title]"
  - Adds post creator as group admin
  - Links collaboration post to the group via `CollaborationProjectGroupId`
  - Sets ontology visibility to "Group" if ontology is attached
  - Grants group "Edit" permission on the ontology

- **Seamless Collaborator Onboarding**: When accepting a collaboration response:
  - Automatically adds user to the collaboration project group
  - User immediately gains edit access to the ontology
  - No manual permission configuration required
  - When declining/removing, automatically removes user from group

#### 🎨 UI Components
- **CollaborationBoard.razor** (`/collaboration`): Browse and search active projects
  - Filter by domain and skill level
  - Search functionality
  - View detailed project cards
  - Apply to projects

- **MyCollaborationPosts.razor** (`/collaboration/my-posts`): Manage your posts
  - View all your collaboration posts
  - Toggle active status
  - Manage responses (accept/reject)
  - View response details

- **CollaborationPostDetail.razor**: Detailed post view with response management
  - Full post information
  - Applicant list with experience and motivation
  - Accept/decline buttons for post owners

#### 🔧 Permission System Enhancements
- **PermissionsSettingsTab.razor**: Implemented `LoadGroupAccess()` method
  - Displays collaboration groups that have access to ontology
  - Shows group names, member counts, and permission levels
  - Real-time group permission visibility
  - Fixed permission level enum mapping (view, edit, admin → View, ViewAddEdit, FullAccess)

- **OntologyHub.cs**: Updated SignalR permission checks
  - Changed from old `UserShareAccesses` check to `OntologyPermissionService.CanViewAsync()`
  - Now properly recognizes group-based permissions
  - Enables real-time collaboration for group members

#### 🧪 Development Tools
- **DevSwitchUser.razor** (`/dev/switch-user`): Multi-user testing page (development only)
  - Quick links to switch between test users
  - Interactive server mode for seamless user switching

- **DevSwitchUserEndpoint.cs**: API endpoint for user switching
  - Handles authentication outside Blazor response pipeline
  - Signs out current user and signs in as selected user
  - Sets cookie to prevent auto-login middleware from overriding
  - Development-only with environment check

- **DevelopmentAuthMiddleware.cs**: Enhanced auto-login middleware
  - Checks for "manual-user-switch" cookie
  - Skips auto-login when user has manually switched accounts
  - Preserves manual user switches across page refreshes

### Database Changes
- Uses existing `CollaborationPosts` table with `CollaborationProjectGroupId` column
- Uses existing `UserGroups`, `UserGroupMembers`, `OntologyGroupPermissions` tables
- No new migrations required

### Files Modified

#### Services
- `/Services/CollaborationBoardService.cs` - Added automatic group creation and permission grant logic
- `/Services/OntologyPermissionService.cs` - Used for permission checks in hub

#### UI Components
- `/Components/Settings/PermissionsSettingsTab.razor` - Implemented group access display
- `/Components/Pages/CollaborationBoard.razor` - Main collaboration discovery page
- `/Components/Pages/MyCollaborationPosts.razor` - User's posts management page
- `/Components/Pages/DevSwitchUser.razor` - Development user switcher (NEW)

#### SignalR
- `/Hubs/OntologyHub.cs` - Updated permission checks to use OntologyPermissionService

#### Endpoints
- `/Endpoints/DevSwitchUserEndpoint.cs` - User switching API (NEW)

#### Middleware
- `/Middleware/DevelopmentAuthMiddleware.cs` - Added manual switch cookie check

#### Documentation
- `/Components/Shared/ReleaseNotes.razor` - Documented collaboration features
- `/DEVELOPMENT_LEDGER.md` - This entry

### Technical Details

#### Collaboration Lifecycle
1. **Post Creation**:
   - User creates collaboration post for their ontology
   - System creates `UserGroup` named "Collaboration: [Title]"
   - Creator added as group admin
   - Group granted "Edit" permission to ontology
   - Ontology visibility changed to "Group"

2. **Response Submission**:
   - Other users apply to join the project
   - Response created with "Pending" status

3. **Response Acceptance**:
   - Post owner accepts response
   - Status changed to "Accepted"
   - User automatically added to collaboration group
   - User immediately gains edit access to ontology

4. **Collaboration**:
   - Collaborators can view and edit ontology
   - Real-time presence tracking via SignalR
   - Permission checks validate group membership
   - Visible in Permissions tab

#### Permission Level Mapping
Database stores permission levels as strings, but UI uses enum:
- `"view"` → `PermissionLevel.View`
- `"edit"` → `PermissionLevel.ViewAddEdit`
- `"admin"` → `PermissionLevel.FullAccess`

Fixed in `PermissionsSettingsTab.razor` line 178-183

#### Development Testing Workflow
1. Navigate to `/dev/switch-user`
2. Click user to switch to (e.g., test@test.com, collab@test.com)
3. Cookie prevents auto-login from overriding
4. Test collaboration workflow from different user perspectives

### Bug Fixes
- Fixed permission denied errors when group members tried to access ontologies
- Fixed "No groups have been granted access yet" message when groups existed
- Fixed enum mapping errors in PermissionsSettingsTab
- Fixed `OntologyId.HasValue` error (changed to `OntologyId > 0`)

### Performance Considerations
- Group creation and permission grant happen in single transaction
- Efficient permission checks via `OntologyPermissionService`
- Real-time updates via SignalR for collaborators
- Cookie-based state for user switching (minimal overhead)

### Security Enhancements
- Development user switcher only works in Development environment
- Permission checks enforce at multiple layers (Hub, Service, UI)
- Group membership validated before granting ontology access
- Automatic permission management reduces manual configuration errors

### Future Improvements
- [ ] Email notifications when responses are accepted/declined
- [ ] In-app notifications for collaboration updates
- [ ] Collaboration analytics (response rates, active projects)
- [ ] Project completion/archival workflow
- [ ] Collaboration activity feed
- [ ] Group chat/messaging for collaborators
- [ ] Permission level adjustments after acceptance

---

## 2025-10-26 - Group Management & Permission System

### Features Added

#### 🔐 Ontology Permission System
- **OntologyPermissionService**: Comprehensive service for managing ontology access control
  - `CanViewAsync()`: Check if user can view an ontology
  - `CanEditAsync()`: Check if user can edit an ontology
  - `CanManageAsync()`: Check if user can manage (admin) an ontology
  - `GetAccessibleOntologiesAsync()`: Get all ontologies user has access to
  - `GrantGroupAccessAsync()`: Grant group access with specific permission level
  - `RevokeGroupAccessAsync()`: Revoke group access
  - `GetGroupPermissionsAsync()`: Get all group permissions for an ontology
  - `UpdateVisibilityAsync()`: Update ontology visibility settings

#### 👥 Group Management UI
- **Permissions Tab in OntologySettingsDialog**: New tabbed interface for managing ontology settings
  - **General Tab**: Existing ontology metadata (name, description, author, version, etc.)
  - **Permissions Tab**: New permission management interface
    - Visibility controls (Private, Group, Public)
    - Allow public edit toggle for public ontologies
    - Group permission management:
      - Add groups with permission levels (View, Edit, Admin)
      - Change permission levels for existing groups
      - Remove group access
      - View member counts and grant history
      - Real-time updates

#### 🛡️ Role-Based Access Control Integration
- Integrated `OntologyPermissionService` into `Home.razor`:
  - Filters ontology list to show only accessible ontologies
- Integrated permission checks into `OntologyView.razor`:
  - Checks view permission before loading ontology
  - Checks edit permission and combines with share permissions
  - Prioritizes most restrictive permission level
  - Shows appropriate error messages for unauthorized access

#### 🔧 OAuth Login Improvements
- **Conditional OAuth Buttons**: Login page now only shows OAuth buttons for configured providers
  - `LoginModel.IsGoogleConfigured`: Detects if Google OAuth is configured
  - `LoginModel.IsMicrosoftConfigured`: Detects if Microsoft OAuth is configured
  - `LoginModel.IsGitHubConfigured`: Detects if GitHub OAuth is configured
  - Prevents users from clicking unconfigured OAuth providers
- **OAuth Error Handling**: Added comprehensive try-catch blocks to all OAuth event handlers
  - GitHub OAuth `OnCreatingTicket` event
  - Google OAuth `OnCreatingTicket` event
  - Microsoft OAuth `OnCreatingTicket` event
  - Prevents login failures from breaking the authentication flow
  - Logs errors for debugging without disrupting user experience

### Database Changes
- No new migrations required (uses existing `OntologyGroupPermissions`, `UserGroups`, `UserGroupMembers` tables)
- Leverages existing `Visibility` and `AllowPublicEdit` columns on `Ontologies` table

### Files Modified

#### Services
- `/Services/OntologyPermissionService.cs` - Core permission management logic

#### UI Components
- `/Components/Ontology/OntologySettingsDialog.razor` - Added Permissions tab with group management
- `/Components/Pages/OntologyView.razor` - Integrated permission checks
- `/Components/Pages/Home.razor` - Filtered ontology list by permissions

#### Authentication
- `/Pages/Account/Login.cshtml` - Conditional OAuth button rendering
- `/Pages/Account/Login.cshtml.cs` - OAuth provider configuration detection
- `/Program.cs` - OAuth event handler error handling

### Tests Added
- `/Eidos.Tests/Integration/Services/OntologyPermissionServiceTests.cs` - 20+ comprehensive tests covering:
  - View permission checks (owner, public, private, group)
  - Edit permission checks (owner, public edit, group edit/view)
  - Manage permission checks (owner, group admin)
  - Group permission management (grant, update, revoke)
  - Visibility updates
  - Accessible ontologies query

- `/Eidos.Tests/Unit/Pages/LoginModelTests.cs` - 12 tests covering:
  - OAuth provider configuration detection (Google, Microsoft, GitHub)
  - Multiple provider scenarios
  - Register mode detection
  - URL toggling

### Technical Details

#### Permission Hierarchy
1. **Owner**: Full access (view, edit, manage)
2. **Group Admin**: Full access except changing ownership
3. **Group Edit**: Can view and edit
4. **Group View**: Can only view
5. **Public Edit**: Anyone can edit public ontologies with flag enabled
6. **Public View**: Anyone can view public ontologies

#### Visibility Levels
- **Private**: Only owner can access
- **Group**: Only owner and specified groups can access
- **Public**: Anyone can view, optionally edit

### Performance Considerations
- Permission checks use EF Core's `Include()` for efficient loading
- Group permissions cached in memory during component lifetime
- Real-time updates use `InvokeAsync()` for reactive UI

### Security Enhancements
- OAuth login errors no longer expose sensitive error details to users
- Permission checks enforce at both UI and service layers
- Group membership validated before granting access

---

## Previous Entries

### 2025-10-25 - Admin Dashboard & User Management
- Added role-based access control (RBAC)
- Created Admin dashboard at `/admin`
- Implemented UserManagementService
- Added user roles: Admin, PowerUser, User, Guest
- Created user groups and group membership management

### 2025-10-24 - Database Optimization
- Downgraded Azure SQL to GP_S_Gen5_1 (80% cost reduction)
- Fixed N+1 queries in ConceptService
- Optimized batch operations in OntologyService
- Added UserPreferences caching

### 2025-10-23 - Activity Tracking
- Added OntologyActivity tracking system
- Implemented version control foundation
- Added collaborator visibility
- Created activity timeline

### 2025-10-22 - Initial Deployment
- Deployed to Azure App Service
- Configured Application Insights
- Set up CI/CD with GitHub Actions
- Implemented health checks

---

## Testing

To run the new tests:

```bash
# Run all tests
dotnet test

# Run only permission tests
dotnet test --filter "FullyQualifiedName~OntologyPermissionServiceTests"

# Run only login tests
dotnet test --filter "FullyQualifiedName~LoginModelTests"
```

---

## Next Steps / Roadmap

### Immediate
- [ ] Deploy group management features to production
- [ ] Update user documentation with group management workflow
- [ ] Monitor OAuth error logs

### Short Term
- [ ] Add group creation UI for regular users
- [ ] Implement group invitation system
- [ ] Add email notifications for group access grants

### Long Term
- [ ] Implement fine-grained permissions (per-concept, per-relationship)
- [ ] Add permission inheritance for ontology hierarchies
- [ ] Create permission audit log
