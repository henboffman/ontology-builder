# Notes Enhancement Requirements - 2025-11-15

## Overview
Comprehensive enhancement to the notes system to support tagging, virtual folders, markdown import/export, auto-save, and improved workspace navigation.

## Requirements

### 1. Note Tagging System
**User Story**: As a user, I want to tag my notes so I can organize them into virtual folders.

**Acceptance Criteria**:
- Users can add multiple tags to a note (e.g., #project, #important, #research)
- Tags are displayed as badges/chips on the note
- Tags can be added/removed via UI
- Tag autocomplete based on existing tags in the workspace
- Tags are searchable in Cmd+K global search

**Database Requirements**:
- `NoteTags` table with many-to-many relationship
- Tag model with Name, WorkspaceId, CreatedAt
- Junction table: `NoteTagAssignments` (NoteId, TagId)

### 2. Virtual Folders (Tag-Based Organization)
**User Story**: As a user, I want to view my notes organized by tags, similar to the ontology dashboard.

**Acceptance Criteria**:
- Left sidebar shows "All Notes" and tags as virtual folders
- Clicking a tag shows all notes with that tag
- Tag count badges show number of notes per tag
- Drag-and-drop support to add/remove tags (optional enhancement)
- Styling matches ontology dashboard aesthetic

**UI Components**:
- `NotesSidebar.razor` - Similar to ontology list panel
- `TagFilterPanel.razor` - Tag selection and filtering
- Virtual folder hierarchy display

### 3. Ontological Integration of Tags
**User Story**: As an ontologist, I want note tags to be represented in my ontology using best practices.

**Research Required**:
- SKOS (Simple Knowledge Organization System) - Common for tagging/categorization
- Dublin Core Metadata Terms - Subject/tag representation
- PROV-O - For provenance and organization
- Schema.org - Common tag/category patterns

**Proposed Approach**:
- Use SKOS:Concept for tags
- Create "Note Classification Scheme" as SKOS:ConceptScheme
- Link notes to tags via custom annotation property
- Support both informal tags and formal ontology concepts

**Implementation**:
- `TagOntologyService` - Sync tags to ontology
- User preference: "Sync tags to ontology" (default: true)
- Tags become SKOS:Concept instances
- Option to promote tags to full concepts

### 4. Auto-Save for Notes
**User Story**: As a user, I want my notes to save automatically while I work, so I don't lose changes.

**Acceptance Criteria**:
- Auto-save triggers after 2 seconds of inactivity
- Visual indicator shows "Saving..." and "All changes saved"
- Debounced save to prevent excessive database writes
- Save on navigation away from note
- Handle concurrent editing conflicts (if applicable)

**Technical Approach**:
- Use Blazor timer to debounce input
- `AutoSaveService` to manage save queue
- Visual feedback component
- Optimistic UI updates

### 5. Markdown File Import
**User Story**: As a user, I want to import markdown files from my file system into the notes system.

**Acceptance Criteria**:
- File picker dialog supports .md and .markdown files
- Supports single file or batch import
- Parses frontmatter (YAML) for metadata (title, tags, date)
- Extracts [[wiki-links]] and creates concepts automatically
- Option to "Import as-is" or "Parse and link"
- Progress indicator for batch imports
- Error handling for malformed files

**Wiki-Link Parsing**:
- Regex: `\[\[([^\]]+)\]\]`
- Create concept if it doesn't exist
- Link note to concept via `LinkedConceptId` or relationship
- Optional: Parse `[[Concept|Display Text]]` format

**UI Components**:
- `MarkdownImportDialog.razor` - File picker and options
- `ImportProgressPanel.razor` - Show import status
- Match bulk import dialog styling

### 6. Markdown File Export
**User Story**: As a user, I want to export my notes as markdown files.

**Acceptance Criteria**:
- Export single note or multiple notes
- Frontmatter includes: title, tags, created/modified dates
- Preserves [[wiki-links]]
- Option to export as ZIP for multiple files
- Export file naming: `{note-title}.md` (sanitized)
- Include linked concepts in export metadata

**Export Format**:
```markdown
---
title: My Note Title
tags: [project, research, important]
created: 2025-11-15T10:30:00Z
modified: 2025-11-15T14:20:00Z
workspace: My Workspace
linked_concepts: [Concept1, Concept2]
---

# My Note Title

Content with [[WikiLinks]] preserved...
```

### 7. Bracket Wrapping Shortcut
**User Story**: As a user, I want to quickly create wiki-links by selecting text and pressing `[`.

**Acceptance Criteria**:
- Select word (double-click or manual selection)
- Press `[` key → wraps in `[[selection]]`
- Press `[` again while selected → wraps again `[[[selection]]]` (if needed)
- Works with multi-word selections
- Undo wrapping with Ctrl+Z

**Technical Approach**:
- JavaScript keydown handler in markdown editor
- TextArea selection API
- Replace selected text with wrapped version
- Preserve cursor position

### 8. Workspace Selection Dialog
**User Story**: As a user, when I open a workspace, I want to choose whether to view the ontology or notes.

**Acceptance Criteria**:
- Dialog appears when clicking workspace from dashboard
- Two options: "Open Ontology" and "Open Notes"
- Option to set default preference per workspace
- Checkbox: "Remember my choice for this workspace"
- Styling matches bulk import dialog aesthetic
- Can be dismissed with Esc or clicking outside

**Navigation Flow**:
1. User clicks workspace on dashboard
2. Dialog appears with two prominent buttons
3. User selects "Ontology" → Navigate to `/ontology/{id}`
4. User selects "Notes" → Navigate to `/workspace/{id}/notes`
5. Preference saved if "Remember" checked

**UI Components**:
- `WorkspaceNavigationDialog.razor`
- Update `Home.razor` to show dialog on workspace click
- Update routing to support `/workspace/{id}/notes`

## Terminology Decision: "Workspace"

The user suggested renaming "thing" to "Workspace" - this makes semantic sense:
- A Workspace contains both an Ontology and a collection of Notes
- Aligns with common terminology (VS Code workspaces, Notion workspaces, etc.)
- Better UX than "thing"

**Scope**: This is a larger refactoring that could be done separately or alongside this work.

## Technical Architecture

### Database Schema Changes

```sql
-- Tags table
CREATE TABLE Tags (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    WorkspaceId INT NOT NULL,
    Color NVARCHAR(7), -- Hex color for tag badge
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (WorkspaceId) REFERENCES Workspaces(Id),
    UNIQUE (Name, WorkspaceId)
);

-- Note-Tag junction table
CREATE TABLE NoteTagAssignments (
    Id INT PRIMARY KEY IDENTITY,
    NoteId INT NOT NULL,
    TagId INT NOT NULL,
    AssignedAt DATETIME2 NOT NULL,
    FOREIGN KEY (NoteId) REFERENCES Notes(Id) ON DELETE CASCADE,
    FOREIGN KEY (TagId) REFERENCES Tags(Id) ON DELETE CASCADE,
    UNIQUE (NoteId, TagId)
);

-- Add fields to Notes table for import metadata
ALTER TABLE Notes ADD ImportedFrom NVARCHAR(500) NULL;
ALTER TABLE Notes ADD ImportedAt DATETIME2 NULL;
ALTER TABLE Notes ADD Frontmatter NVARCHAR(MAX) NULL; -- Store original YAML
```

### New Services

1. **TagService** - CRUD for tags, tag assignments
2. **MarkdownImportService** - Parse and import markdown files
3. **MarkdownExportService** - Export notes to markdown
4. **WikiLinkParserService** - Extract and process [[wiki-links]]
5. **AutoSaveService** - Debounced auto-save with queue management
6. **TagOntologyIntegrationService** - Sync tags to SKOS concepts

### New Components

1. **NotesSidebar.razor** - Tag-based virtual folder navigation
2. **TagBadge.razor** - Display and manage tags on notes
3. **TagSelector.razor** - Add/remove tags with autocomplete
4. **MarkdownImportDialog.razor** - File import UI
5. **MarkdownExportDialog.razor** - Export options UI
6. **AutoSaveIndicator.razor** - "Saving..." / "Saved" status
7. **WorkspaceNavigationDialog.razor** - Choose ontology or notes view
8. **WikiLinkEditor.razor** - Enhanced markdown editor with bracket wrapping

### JavaScript Modules

1. **wikiLinkEditor.js** - Bracket wrapping, selection handling
2. **markdownImport.js** - File picker, drag-drop support
3. **autoSave.js** - Debounce and save coordination

## Implementation Phases

### Phase 1: Core Tag System (Priority 1)
- Database schema for tags
- Tag CRUD service
- UI for adding/removing tags
- Tag display on notes

### Phase 2: Virtual Folders (Priority 1)
- Sidebar with tag navigation
- Filter notes by tag
- Tag count badges
- All Notes view

### Phase 3: Auto-Save (Priority 2)
- Debounced save service
- Visual indicator
- Save on navigation

### Phase 4: Markdown Import (Priority 2)
- File picker dialog
- Wiki-link parsing
- Concept creation from links
- Batch import support

### Phase 5: Markdown Export (Priority 2)
- Export single note
- Export multiple notes
- Frontmatter generation
- ZIP for batch export

### Phase 6: Bracket Wrapping (Priority 3)
- JavaScript selection handler
- Keyboard shortcut
- Wrap/unwrap logic

### Phase 7: Workspace Navigation Dialog (Priority 3)
- Navigation dialog component
- Preference storage
- Update routing

### Phase 8: Ontological Integration (Priority 4)
- SKOS concept creation
- Tag sync service
- UI preference for sync
- Tag promotion to concepts

## Open Questions

1. **Tag Color Assignment**: Auto-generate or user-selectable?
2. **Tag Hierarchy**: Support nested tags (e.g., `project/frontend`, `project/backend`)?
3. **Tag Limits**: Max tags per note? Max tag length?
4. **Wiki-Link Ambiguity**: How to handle `[[Link]]` when multiple concepts match?
5. **Concurrent Editing**: How to handle conflicts in auto-save?
6. **Export Format**: Support other formats (Obsidian, Logseq, Roam)?
7. **Import Conflicts**: What if imported note title already exists?
8. **Ontology Sync Direction**: Tags → Concepts only, or bidirectional?

## Success Metrics

- Users can import 100+ markdown files without issues
- Auto-save completes within 500ms of user inactivity
- Tag navigation performs well with 1000+ notes
- Wiki-link parsing accuracy > 99%
- Export/import roundtrip preserves all content and links

## Related Documentation

- See `obsidian-style-requirements.md` for wiki-link patterns
- See `data-storage-optimization.md` for content storage approach
- See existing `WORKSPACE_NOTES_FEATURE.md` for workspace architecture

## Next Steps

1. Review and approve requirements
2. Database schema design and migration
3. Service layer implementation
4. UI component development
5. Testing and refinement
