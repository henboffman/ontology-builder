# Notes Functionality - Requirements Analysis
**Feature Version**: 1.1
**Created**: 2025-11-14
**Status**: In Progress

## Executive Summary

This feature represents a major architectural shift for Eidos. The application will move from an ontology-centric model to a **workspace-centric** model, where each workspace (the "Eidos base entity") contains:
1. **One Ontology** (existing functionality)
2. **Notes Collection** (new functionality)

## Requirements

### 1. Workspace Entity (Eidos Base Entity)

**Status**: Name TBD (recommend "Workspace")

#### Core Requirements
- **REQ-WS-001**: Replace Ontology as the top-level entity
- **REQ-WS-002**: Each workspace must contain exactly one Ontology
- **REQ-WS-003**: Each workspace must contain one Notes collection
- **REQ-WS-004**: Workspace must have a unique name and owner
- **REQ-WS-005**: Workspace inherits all current Ontology metadata (version, visibility, permissions, etc.)

#### Data Model
```
Workspace
├── Id (int, PK)
├── Name (string, required)
├── Description (string, optional)
├── UserId (string, required, FK to AspNetUsers)
├── Visibility (enum: Private/Group/Public)
├── AllowPublicEdit (bool)
├── CreatedAt (DateTime)
├── UpdatedAt (DateTime)
├── Ontology (1:1 relationship)
└── Notes (1:many relationship)
```

### 2. Notes Collection

**Status**: New functionality

#### Core Requirements
- **REQ-NOTES-001**: Each workspace has one flat collection of notes
- **REQ-NOTES-002**: Notes can be created, edited, and deleted by authorized users
- **REQ-NOTES-003**: Notes support two render formats: Markdown and Rich Text
- **REQ-NOTES-004**: Notes use [[concept-name]] syntax to reference concepts
- **REQ-NOTES-005**: When a [[concept]] reference is parsed, the concept is auto-created in the ontology if it doesn't exist
- **REQ-NOTES-006**: Each concept in the ontology has an auto-generated note named `<concept-name>-note`
- **REQ-NOTES-007**: Concept notes are automatically created when a concept is created
- **REQ-NOTES-008**: Concept notes can be edited by users
- **REQ-NOTES-009**: Deleting a concept should not delete its note (note becomes orphaned)

#### Data Model
```
Note
├── Id (int, PK)
├── WorkspaceId (int, FK)
├── Title (string, required)
├── Content (text, required)
├── RenderFormat (enum: Markdown/RichText)
├── IsConceptNote (bool) - true if auto-generated for a concept
├── LinkedConceptId (int?, FK to Concept) - if IsConceptNote = true
├── UserId (string, FK to AspNetUsers) - creator
├── CreatedAt (DateTime)
├── UpdatedAt (DateTime)
└── Tags (optional, future enhancement)
```

### 3. Concept Linking via [[]] Syntax

**Status**: Critical new functionality

#### Core Requirements
- **REQ-LINK-001**: Parse note content for [[concept-name]] syntax
- **REQ-LINK-002**: When [[concept-name]] is found, check if concept exists in ontology
- **REQ-LINK-003**: If concept doesn't exist, auto-create it with:
  - Name: extracted from [[]]
  - Definition: "Auto-created from note reference"
  - Category: "Note-Referenced"
  - Default color
- **REQ-LINK-004**: Create bidirectional reference tracking (note → concept)
- **REQ-LINK-005**: Display linked concepts in note editor (visual feedback)
- **REQ-LINK-006**: Allow clicking [[concept]] references to navigate to concept in graph view

#### Technical Approach
```csharp
// Parse on note save
List<string> ExtractConceptReferences(string noteContent)
{
    var regex = new Regex(@"\[\[([^\]]+)\]\]");
    var matches = regex.Matches(noteContent);
    return matches.Select(m => m.Groups[1].Value).ToList();
}

// Auto-create concepts
foreach (var conceptName in conceptReferences)
{
    if (!await ConceptExists(conceptName))
    {
        await ConceptService.CreateAsync(new Concept
        {
            Name = conceptName,
            Definition = "Auto-created from note reference",
            Category = "Note-Referenced",
            Color = DefaultColor
        });
    }
}
```

### 4. Concept Notes (Auto-Generated)

**Status**: Automatic functionality

#### Core Requirements
- **REQ-CN-001**: When a concept is created, auto-create a note with title `<concept-name>-note`
- **REQ-CN-002**: Set `IsConceptNote = true` and `LinkedConceptId = concept.Id`
- **REQ-CN-003**: Default content: "# {ConceptName}\n\nNotes about this concept..."
- **REQ-CN-004**: Users can edit concept notes freely
- **REQ-CN-005**: Concept notes appear in both Notes view and Concept detail view
- **REQ-CN-006**: If concept is renamed, update note title automatically

#### Implementation Hook
```csharp
// In ConceptService.CreateAsync
public async Task<Concept> CreateAsync(Concept concept)
{
    var created = await _repository.AddAsync(concept);

    // Auto-create concept note
    await _noteService.CreateConceptNoteAsync(new Note
    {
        WorkspaceId = concept.Ontology.WorkspaceId,
        Title = $"{concept.Name}-note",
        Content = $"# {concept.Name}\n\nNotes about this concept...",
        RenderFormat = RenderFormat.Markdown,
        IsConceptNote = true,
        LinkedConceptId = concept.Id,
        UserId = currentUserId
    });

    return created;
}
```

### 5. UI Requirements

#### Notes View
- **REQ-UI-001**: New top-level navigation item "Notes"
- **REQ-UI-002**: Notes list view with search and filter
- **REQ-UI-003**: Note editor with markdown/rich text toggle
- **REQ-UI-004**: Live preview for markdown
- **REQ-UI-005**: Syntax highlighting for [[concept]] references
- **REQ-UI-006**: Autocomplete for concept names when typing [[
- **REQ-UI-007**: Click [[concept]] to navigate to concept in graph view

#### Workspace View
- **REQ-UI-008**: Update home page to show workspaces instead of ontologies
- **REQ-UI-009**: Workspace card shows both ontology stats and note count
- **REQ-UI-010**: Workspace detail view has tabs: Ontology, Notes, Settings

#### Concept View
- **REQ-UI-011**: Concept detail panel shows linked concept note
- **REQ-UI-012**: "Edit Note" button opens concept note in editor

### 6. Migration Requirements

**Status**: Critical for existing users

#### Core Requirements
- **REQ-MIG-001**: Create migration to convert existing Ontology records to Workspaces
- **REQ-MIG-002**: Auto-generate workspace names from ontology names
- **REQ-MIG-003**: Preserve all ontology relationships and permissions
- **REQ-MIG-004**: No data loss during migration
- **REQ-MIG-005**: Support rollback if migration fails

#### Migration Steps
1. Create Workspace table
2. Create Note table
3. For each Ontology:
   - Create Workspace with same metadata
   - Link Ontology to Workspace (add WorkspaceId FK)
   - Set Workspace.OntologyId = Ontology.Id
4. For each Concept:
   - Create auto-generated concept note
5. Update all foreign keys and references

### 7. Permission Requirements

**Status**: Extend existing system

#### Core Requirements
- **REQ-PERM-001**: Permissions move from Ontology to Workspace level
- **REQ-PERM-002**: If user has Workspace access, they have access to both Ontology and Notes
- **REQ-PERM-003**: Permission levels apply to both ontology and notes:
  - **View**: Read-only for ontology and notes
  - **ViewAndAdd**: Can add concepts and create notes
  - **ViewAddEdit**: Can edit concepts and edit notes
  - **FullAccess**: Can manage workspace, ontology, and notes
- **REQ-PERM-004**: Concept notes inherit concept permissions

## Non-Functional Requirements

### Performance
- **NFR-PERF-001**: Note content search must return results < 500ms for workspaces with < 1000 notes
- **NFR-PERF-002**: [[concept]] parsing must complete < 100ms for notes < 10KB
- **NFR-PERF-003**: Workspace loading must remain < 2 seconds

### Scalability
- **NFR-SCALE-001**: Support up to 10,000 notes per workspace
- **NFR-SCALE-002**: Support [[concept]] references in notes up to 100KB

### Usability
- **NFR-UX-001**: Note editor must auto-save every 30 seconds
- **NFR-UX-002**: [[concept]] autocomplete must appear within 200ms of typing [[
- **NFR-UX-003**: Markdown preview must update in real-time (< 100ms lag)

## Dependencies

### External Libraries
1. **Markdown Parser**: Markdig (already used in application)
2. **Rich Text Editor**: TBD - Options:
   - Quill.js
   - TipTap
   - ProseMirror
3. **Syntax Highlighting**: Prism.js or highlight.js

### Internal Dependencies
1. Existing Concept/Ontology system
2. Permission system
3. User management
4. Real-time collaboration (SignalR) - extend to notes

## Out of Scope (Future Enhancements)

1. Note versioning/history
2. Note templates
3. Note sharing (separate from workspace sharing)
4. Note tags and categories
5. Full-text search across all notes
6. Export notes to PDF/DOCX
7. Import notes from Markdown files
8. Note attachments (images, files)
9. Collaborative editing (multiple users editing same note)
10. Note linking (notes referencing other notes)

## Success Criteria

1. Users can create workspaces containing both ontologies and notes
2. [[concept]] syntax automatically creates concepts in ontology
3. Concept notes are auto-generated and editable
4. Zero data loss during migration from ontology-centric to workspace-centric model
5. All existing features continue to work unchanged
6. Notes render correctly in both Markdown and Rich Text formats
7. Performance remains acceptable (< 2s load times)

## Risks and Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Migration data loss | Critical | Low | Comprehensive testing, backup strategy, rollback plan |
| Performance degradation | High | Medium | Indexing, query optimization, pagination |
| UI complexity | Medium | Medium | User testing, iterative design, clear navigation |
| [[concept]] parsing bugs | Medium | Medium | Extensive unit tests, regex validation |
| Rich text editor integration | Medium | Low | Evaluate libraries thoroughly, have fallback option |

## Open Questions

1. **Workspace naming**: Should we rename "Eidos base entity" to "Workspace", "Project", "Knowledge Base", or keep "Eidos"?
   - **Recommendation**: "Workspace" - familiar to users, clear purpose

2. **Rich Text Format**: Which rich text editor library?
   - **Recommendation**: Start with Markdown only (v1.1), add Rich Text in v1.2

3. **Note organization**: Should notes have folders/categories in v1.1?
   - **Recommendation**: Flat structure for v1.1, add organization in v1.2

4. **Concept auto-creation**: Should there be a confirmation dialog?
   - **Recommendation**: No confirmation, but show toast notification "Created concept: {name}"

5. **Orphaned notes**: What happens to concept notes when concept is deleted?
   - **Recommendation**: Keep note, set LinkedConceptId = null, show as orphaned in UI

## Next Steps

1. Get stakeholder approval on "Workspace" naming
2. Finalize data model with DBA review
3. Create detailed migration plan
4. Begin Phase 2: Architecture & Design
5. Create database schema and migrations
6. Implement core entities and repositories
7. Build Notes UI components
8. Implement [[concept]] parsing
9. Execute migration with test data
10. Comprehensive testing
