# Requirements - Admin Dialogs
**Last Updated**: November 2, 2025

## User Stories

### Primary User: Ontology Administrator

**As an administrator**, I want to:
1. Quickly view all entities in an ontology without leaving the page
2. Search/filter the entity list to find specific items
3. Edit entity properties inline without opening full editor
4. Delete entities with a single click (+ confirmation)
5. See entity metadata (creation date, modified, author)

## Functional Requirements

### FR-1: Entity List Dialog
- Display all entities of a given type (Concepts, Relationships, or Individuals)
- Show key properties: Name, Description, Category/Type
- Scrollable list (virtualized if >100 items)
- Sort by: Name (A-Z), Created (newest), Modified (recent)
- Visual indicators: Validation status, orphaned entities

### FR-2: Search & Filter
- Real-time text search across name and description
- Filter by category (for concepts)
- Filter by relationship type (for relationships)
- Clear search/filters button
- Show result count: "Showing 5 of 42 concepts"

### FR-3: Quick Edit
- Click entity row to expand inline editor
- Editable fields: Name, Description, Category/Type
- Save/Cancel buttons
- Validation feedback (duplicate names, required fields)
- Auto-saves on blur (optional, TBD)

### FR-4: Quick Delete
- Delete icon on each row
- Confirmation dialog with entity name
- Shows impact: "This will delete 3 relationships"
- Success/error toast notification
- List updates immediately after delete

### FR-5: Permissions
- Requires `FullAccess` permission level
- Show "Admin Only" badge on dialog trigger
- Gracefully handle permission errors

## Non-Functional Requirements

### NFR-1: Performance
- List renders in <200ms for 100 items
- Search/filter responds in <50ms
- Smooth animations (200ms transitions)

### NFR-2: Responsive Design
- Desktop: 800px wide dialog
- Tablet: 90vw wide
- Mobile: Full screen with slide-up animation

### NFR-3: Accessibility
- Keyboard navigation (Tab, Enter, Esc)
- Screen reader support (ARIA labels)
- Focus management on open/close

### NFR-4: Code Quality
- Single Responsibility - each component <300 lines
- Reusable components (EntityListItem, SearchBar)
- TypeScript for complex JS interactions (if needed)
- Unit tests for service methods

## Entity Types

### Concept Admin Dialog
Fields:
- Name (required, unique)
- Description (optional)
- Category (optional)
- Definition (optional)
- Examples (optional)

Actions:
- Edit concept
- Delete concept (check for relationships first)
- Duplicate concept

### Relationship Admin Dialog
Fields:
- From Concept (readonly in edit)
- To Concept (readonly in edit)
- Relationship Type (required)
- Description (optional)

Actions:
- Edit relationship
- Delete relationship
- Reverse relationship (swap from/to)

### Individual Admin Dialog
Fields:
- Name (required)
- Concept Type (required)
- Description (optional)

Actions:
- Edit individual
- Delete individual (check for relationships)
- Change concept type

## User Flow

1. Admin opens ontology page
2. Clicks "Manage Entities" button (or similar)
3. Selects entity type (Concepts/Relationships/Individuals)
4. Dialog opens with entity list
5. Admin searches/filters to find entity
6. Clicks row to edit OR clicks delete icon
7. Makes changes, saves
8. Dialog updates list, shows success toast
9. Closes dialog or continues managing

## Edge Cases

- Empty ontology: Show "No entities yet. Add one to get started."
- Search with no results: "No matches for 'xyz'. Try different keywords."
- Delete last concept: Warn "This is the last concept. Ontology will be empty."
- Concurrent edits: Show warning "Another user modified this. Reload?"
- Permission revoked mid-session: Disable edit/delete, show message

## Integration Points

- **OntologyView.razor** - Add "Manage Entities" button
- **ConceptService/RelationshipService/IndividualService** - Use existing CRUD
- **OntologyPermissionService** - Check FullAccess permission
- **ToastService** - Success/error notifications
- **ConfirmService** - Delete confirmations
- **SignalR Hub** - Broadcast changes to other users (optional)

## Success Metrics

- Admin can find and edit an entity in <5 seconds
- Delete operation completes in <1 second
- Zero navigation away from ontology page needed
- User satisfaction: "Much faster than before"

---
**Next**: See [architecture.md](./architecture.md) for technical design
