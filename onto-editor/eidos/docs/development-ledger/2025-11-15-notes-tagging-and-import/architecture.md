# Notes Tagging & Import - Technical Architecture

## System Overview

This feature set extends the notes system with tagging, virtual folder organization, markdown import/export, auto-save, and improved workspace navigation. The architecture follows established patterns from the ontology system while introducing new patterns for file handling and real-time auto-save.

## Database Schema

### New Tables

#### Tags
```sql
CREATE TABLE Tags (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    WorkspaceId INT NOT NULL,
    Color NVARCHAR(7) NULL,           -- Optional hex color (e.g., #3498db)
    Description NVARCHAR(500) NULL,    -- Optional tag description
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(450) NOT NULL,  -- User who created tag

    CONSTRAINT FK_Tags_Workspace FOREIGN KEY (WorkspaceId) REFERENCES Workspaces(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Tags_Name_Workspace UNIQUE (Name, WorkspaceId),
    CONSTRAINT FK_Tags_User FOREIGN KEY (CreatedBy) REFERENCES AspNetUsers(Id)
);

CREATE INDEX IX_Tags_WorkspaceId ON Tags(WorkspaceId);
CREATE INDEX IX_Tags_Name ON Tags(Name);
```

#### NoteTagAssignments
```sql
CREATE TABLE NoteTagAssignments (
    Id INT PRIMARY KEY IDENTITY,
    NoteId INT NOT NULL,
    TagId INT NOT NULL,
    AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    AssignedBy NVARCHAR(450) NOT NULL, -- User who assigned tag

    CONSTRAINT FK_NoteTagAssignments_Note FOREIGN KEY (NoteId) REFERENCES Notes(Id) ON DELETE CASCADE,
    CONSTRAINT FK_NoteTagAssignments_Tag FOREIGN KEY (TagId) REFERENCES Tags(Id) ON DELETE CASCADE,
    CONSTRAINT FK_NoteTagAssignments_User FOREIGN KEY (AssignedBy) REFERENCES AspNetUsers(Id),
    CONSTRAINT UQ_NoteTagAssignments UNIQUE (NoteId, TagId)
);

CREATE INDEX IX_NoteTagAssignments_NoteId ON NoteTagAssignments(NoteId);
CREATE INDEX IX_NoteTagAssignments_TagId ON NoteTagAssignments(TagId);
```

### Extended Tables

#### Notes - Add Import Metadata
```sql
ALTER TABLE Notes ADD ImportedFrom NVARCHAR(500) NULL;        -- Original file path
ALTER TABLE Notes ADD ImportedAt DATETIME2 NULL;              -- When imported
ALTER TABLE Notes ADD Frontmatter NVARCHAR(MAX) NULL;         -- Original YAML frontmatter
ALTER TABLE Notes ADD LastAutoSaveAt DATETIME2 NULL;          -- Last auto-save timestamp
ALTER TABLE Notes ADD AutoSaveEnabled BIT NOT NULL DEFAULT 1; -- User preference per note
```

## Domain Models

### Tag.cs
```csharp
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;
    public string? Color { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public ApplicationUser Creator { get; set; } = null!;

    // Navigation
    public ICollection<NoteTagAssignment> NoteAssignments { get; set; } = new List<NoteTagAssignment>();
}
```

### NoteTagAssignment.cs
```csharp
public class NoteTagAssignment
{
    public int Id { get; set; }
    public int NoteId { get; set; }
    public Note Note { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string AssignedBy { get; set; } = string.Empty;
    public ApplicationUser Assigner { get; set; } = null!;
}
```

### MarkdownImportResult.cs
```csharp
public class MarkdownImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Note? ImportedNote { get; set; }
    public List<string> ExtractedWikiLinks { get; set; } = new();
    public List<Concept> CreatedConcepts { get; set; } = new();
    public Dictionary<string, string>? Frontmatter { get; set; }
    public List<string> Tags { get; set; } = new();
}
```

### MarkdownImportOptions.cs
```csharp
public class MarkdownImportOptions
{
    public bool ParseWikiLinks { get; set; } = true;
    public bool CreateConceptsFromLinks { get; set; } = true;
    public bool ParseFrontmatter { get; set; } = true;
    public bool OverwriteExisting { get; set; } = false;
    public bool CreateTagsFromFrontmatter { get; set; } = true;
    public string? DefaultCategory { get; set; } // For auto-created concepts
}
```

## Service Layer

### TagService
**Responsibility**: CRUD operations for tags and tag assignments

```csharp
public interface ITagService
{
    // Tag CRUD
    Task<Tag> CreateTagAsync(int workspaceId, string name, string userId, string? color = null);
    Task<Tag?> GetTagByIdAsync(int tagId);
    Task<List<Tag>> GetWorkspaceTagsAsync(int workspaceId);
    Task<Tag> UpdateTagAsync(Tag tag);
    Task DeleteTagAsync(int tagId);

    // Tag assignment
    Task AssignTagToNoteAsync(int noteId, int tagId, string userId);
    Task UnassignTagFromNoteAsync(int noteId, int tagId);
    Task<List<Tag>> GetNoteTagsAsync(int noteId);
    Task<List<Note>> GetNotesByTagAsync(int tagId);

    // Bulk operations
    Task<Dictionary<int, int>> GetTagCountsAsync(int workspaceId); // TagId -> Count
    Task BulkAssignTagsAsync(List<int> noteIds, List<int> tagIds, string userId);

    // Search
    Task<List<Tag>> SearchTagsAsync(int workspaceId, string query);
}
```

### MarkdownImportService
**Responsibility**: Parse and import markdown files into notes

```csharp
public interface IMarkdownImportService
{
    Task<MarkdownImportResult> ImportFileAsync(
        int workspaceId,
        string fileName,
        string content,
        MarkdownImportOptions options,
        string userId);

    Task<List<MarkdownImportResult>> ImportBatchAsync(
        int workspaceId,
        Dictionary<string, string> fileContents, // filename -> content
        MarkdownImportOptions options,
        string userId,
        IProgress<int>? progress = null);

    Dictionary<string, string>? ParseFrontmatter(string content);
    List<string> ExtractWikiLinks(string content);
    string StripFrontmatter(string content);
}
```

**Implementation Details**:
- Use YamlDotNet for frontmatter parsing
- Regex for wiki-link extraction: `\[\[([^\]]+)\]\]`
- Handle alternate syntax: `[[Concept|Display Text]]`
- Sanitize file names for note titles
- Detect conflicts (existing notes with same title)

### MarkdownExportService
**Responsibility**: Export notes to markdown files

```csharp
public interface IMarkdownExportService
{
    Task<string> ExportNoteAsync(int noteId, bool includeFrontmatter = true);
    Task<Dictionary<string, string>> ExportNotesAsync(List<int> noteIds, bool includeFrontmatter = true);
    Task<byte[]> ExportNotesAsZipAsync(List<int> noteIds, bool includeFrontmatter = true);

    string GenerateFrontmatter(Note note, List<Tag> tags, List<Concept> linkedConcepts);
    string SanitizeFileName(string title);
}
```

**Export Format**:
```yaml
---
title: Note Title
tags: [tag1, tag2, tag3]
created: 2025-11-15T10:30:00Z
modified: 2025-11-15T14:20:00Z
workspace: Workspace Name
linked_concepts: [Concept1, Concept2]
---

Note content with [[WikiLinks]]...
```

### WikiLinkParserService
**Responsibility**: Extract and process wiki-links in markdown

```csharp
public interface IWikiLinkParserService
{
    List<WikiLink> ParseWikiLinks(string markdown);
    Task<Concept?> FindOrCreateConceptAsync(string linkText, int ontologyId, string userId, string? defaultCategory = null);
    Task LinkNoteToConceptsAsync(int noteId, List<string> conceptNames);
    string RenderWikiLinksAsHtml(string markdown, Dictionary<string, int> conceptIdMap);
}

public class WikiLink
{
    public string LinkText { get; set; } = string.Empty;
    public string? DisplayText { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
}
```

### AutoSaveService
**Responsibility**: Manage debounced auto-save for notes

```csharp
public interface IAutoSaveService
{
    Task<bool> QueueSaveAsync(int noteId, string content, string userId);
    Task<bool> ForceSaveAsync(int noteId);
    void CancelPendingSave(int noteId);
    AutoSaveStatus GetSaveStatus(int noteId);
}

public enum AutoSaveStatus
{
    Idle,
    Pending,
    Saving,
    Saved,
    Error
}
```

**Implementation**:
- Use `System.Threading.Timer` for debouncing
- 2-second delay after last keystroke
- Queue saves to prevent concurrent writes
- Emit events for UI status updates
- Handle failures with retry logic

### TagOntologyIntegrationService
**Responsibility**: Sync tags with SKOS concepts in ontology

```csharp
public interface ITagOntologyIntegrationService
{
    Task<bool> SyncTagToOntologyAsync(int tagId);
    Task<bool> SyncAllTagsAsync(int workspaceId);
    Task<Concept?> GetConceptForTagAsync(int tagId);
    Task<bool> PromoteTagToConceptAsync(int tagId, string category);
}
```

**SKOS Integration**:
- Create SKOS:ConceptScheme: "Note Classification Scheme"
- Each tag becomes a SKOS:Concept
- Use skos:prefLabel for tag name
- Custom annotation property links notes to tag concepts
- Option in user preferences: "Sync tags to ontology"

## Repository Layer

### TagRepository
```csharp
public interface ITagRepository
{
    Task<Tag> CreateAsync(Tag tag);
    Task<Tag?> GetByIdAsync(int id);
    Task<List<Tag>> GetByWorkspaceIdAsync(int workspaceId);
    Task<Tag?> GetByNameAsync(int workspaceId, string name);
    Task<Tag> UpdateAsync(Tag tag);
    Task DeleteAsync(int id);
    Task<int> GetNoteCountAsync(int tagId);
    Task<Dictionary<int, int>> GetTagCountsAsync(int workspaceId);
}
```

### NoteTagAssignmentRepository
```csharp
public interface INoteTagAssignmentRepository
{
    Task<NoteTagAssignment> CreateAsync(NoteTagAssignment assignment);
    Task<List<NoteTagAssignment>> GetByNoteIdAsync(int noteId);
    Task<List<NoteTagAssignment>> GetByTagIdAsync(int tagId);
    Task DeleteAsync(int noteId, int tagId);
    Task<bool> ExistsAsync(int noteId, int tagId);
}
```

## UI Components

### NotesSidebar.razor
**Purpose**: Tag-based virtual folder navigation

**Features**:
- "All Notes" default view
- Tag list with note counts
- Collapsible tag groups (optional)
- Tag color indicators
- Search/filter tags
- Add new tag button

**Styling**: Match OntologyControlBar.razor aesthetic

### TagBadge.razor
**Purpose**: Display tag as a badge/chip

**Props**:
- `Tag` - The tag to display
- `Removable` - Show remove button
- `OnRemove` - Callback when removed
- `Size` - sm, md, lg

### TagSelector.razor
**Purpose**: Add/remove tags with autocomplete

**Features**:
- Dropdown with existing tags
- Autocomplete as user types
- Create new tag inline
- Multi-select support
- Keyboard navigation

### MarkdownImportDialog.razor
**Purpose**: Import markdown files UI

**Features**:
- File picker (single/multiple)
- Drag-and-drop support
- Import options panel
- Progress bar for batch import
- Error display for failed imports
- Preview of files to import

**Styling**: Match BulkConceptImportDialog.razor

### MarkdownExportDialog.razor
**Purpose**: Export notes to markdown

**Features**:
- Select notes to export
- Include/exclude frontmatter option
- Export as individual files or ZIP
- Download button
- Preview export format

### AutoSaveIndicator.razor
**Purpose**: Show auto-save status

**Design**:
```html
<div class="autosave-indicator">
    @if (Status == AutoSaveStatus.Saving)
    {
        <i class="bi bi-cloud-arrow-up"></i> Saving...
    }
    else if (Status == AutoSaveStatus.Saved)
    {
        <i class="bi bi-cloud-check"></i> All changes saved
    }
    else if (Status == AutoSaveStatus.Error)
    {
        <i class="bi bi-exclamation-triangle"></i> Save failed
    }
</div>
```

### WorkspaceNavigationDialog.razor
**Purpose**: Choose to open ontology or notes view

**Layout**:
```
+----------------------------------------+
|  Open "My Workspace"                   |
|                                        |
|  +---------------+  +---------------+  |
|  | [Graph Icon]  |  | [Notes Icon]  |  |
|  |   Ontology    |  |     Notes     |  |
|  +---------------+  +---------------+  |
|                                        |
|  □ Remember my choice                  |
+----------------------------------------+
```

### WikiLinkEditor.razor (Enhanced)
**Purpose**: Markdown editor with wiki-link support

**Features**:
- Syntax highlighting for [[wiki-links]]
- Bracket wrapping shortcut
- Link preview on hover
- Auto-complete for existing concepts
- Create concept inline from link

## JavaScript Modules

### wikiLinkEditor.js
```javascript
export function initializeWikiLinkEditor(editorElement, dotNetRef) {
    editorElement.addEventListener('keydown', handleBracketWrap);
    editorElement.addEventListener('dblclick', handleDoubleClick);

    // Highlight wiki-links
    highlightWikiLinks(editorElement);
}

function handleBracketWrap(event) {
    if (event.key === '[' && hasSelection()) {
        event.preventDefault();
        wrapSelection('[[', ']]');
    }
}

function wrapSelection(prefix, suffix) {
    const selection = window.getSelection();
    const text = selection.toString();
    const wrapped = prefix + text + suffix;
    document.execCommand('insertText', false, wrapped);
}
```

### markdownImport.js
```javascript
export async function openFilePicker(accept, multiple) {
    return new Promise((resolve) => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = accept || '.md,.markdown';
        input.multiple = multiple || false;

        input.onchange = async (e) => {
            const files = Array.from(e.target.files);
            const results = [];

            for (const file of files) {
                const content = await file.text();
                results.push({ name: file.name, content });
            }

            resolve(results);
        };

        input.click();
    });
}

export function setupDragDrop(dropZone, dotNetRef) {
    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropZone.classList.add('drag-over');
    });

    dropZone.addEventListener('drop', async (e) => {
        e.preventDefault();
        dropZone.classList.remove('drag-over');

        const files = Array.from(e.dataTransfer.files)
            .filter(f => f.name.endsWith('.md') || f.name.endsWith('.markdown'));

        const results = [];
        for (const file of files) {
            const content = await file.text();
            results.push({ name: file.name, content });
        }

        await dotNetRef.invokeMethodAsync('OnFilesDropped', results);
    });
}
```

### autoSave.js
```javascript
export class AutoSaveManager {
    constructor(dotNetRef, debounceMs = 2000) {
        this.dotNetRef = dotNetRef;
        this.debounceMs = debounceMs;
        this.timers = new Map();
    }

    queueSave(noteId, content) {
        // Clear existing timer
        if (this.timers.has(noteId)) {
            clearTimeout(this.timers.get(noteId));
        }

        // Set new timer
        const timer = setTimeout(() => {
            this.dotNetRef.invokeMethodAsync('SaveNote', noteId, content);
            this.timers.delete(noteId);
        }, this.debounceMs);

        this.timers.set(noteId, timer);
    }

    forceSave(noteId) {
        if (this.timers.has(noteId)) {
            clearTimeout(this.timers.get(noteId));
            this.timers.delete(noteId);
        }
        this.dotNetRef.invokeMethodAsync('ForceSaveNote', noteId);
    }

    cancelSave(noteId) {
        if (this.timers.has(noteId)) {
            clearTimeout(this.timers.get(noteId));
            this.timers.delete(noteId);
        }
    }
}
```

## Routing Updates

### New Routes
```csharp
// WorkspaceView - Notes view
@page "/workspace/{Id:int}/notes"

// TagView - Notes filtered by tag
@page "/workspace/{WorkspaceId:int}/tags/{TagId:int}"

// Import/Export pages (optional)
@page "/workspace/{Id:int}/import"
@page "/workspace/{Id:int}/export"
```

## State Management

### NotesState (Scoped Service)
```csharp
public class NotesState
{
    public int? SelectedWorkspaceId { get; set; }
    public int? SelectedTagId { get; set; }
    public List<Note> FilteredNotes { get; set; } = new();
    public List<Tag> WorkspaceTags { get; set; } = new();
    public AutoSaveStatus CurrentSaveStatus { get; set; }

    public event Action? OnChange;

    public void SetSelectedTag(int? tagId)
    {
        SelectedTagId = tagId;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

## Performance Considerations

### Auto-Save Optimization
- Debounce saves to 2 seconds
- Use `AsNoTracking()` for read operations
- Queue saves to prevent concurrent writes
- Implement optimistic locking with version field

### Tag Query Optimization
- Eager load tags with notes: `.Include(n => n.TagAssignments).ThenInclude(ta => ta.Tag)`
- Cache tag counts in memory (5-minute expiration)
- Index on WorkspaceId and TagId
- Pagination for notes list (50 per page)

### Import Performance
- Process files in parallel (batch of 10)
- Use bulk insert for concepts
- Transaction per file (rollback on error)
- Progress reporting every 10%

## Security Considerations

### File Upload Security
- Validate file extensions (.md, .markdown only)
- Limit file size (max 5MB per file)
- Scan for XSS in markdown content
- Sanitize file names (no path traversal)

### Permission Checks
- Verify workspace access before import/export
- Check edit permissions before auto-save
- Tag CRUD requires workspace edit permission
- Export respects note visibility

## Testing Strategy

### Unit Tests
- TagService CRUD operations
- Wiki-link parsing accuracy
- Frontmatter parsing edge cases
- Auto-save debouncing logic

### Integration Tests
- Import markdown with wiki-links
- Export and re-import roundtrip
- Tag filtering and search
- Concurrent auto-save

### E2E Tests
- Full import workflow
- Tag creation and assignment
- Workspace navigation dialog
- Bracket wrapping shortcut

## Migration Path

### Phase 1: Schema Migration
1. Create Tags table
2. Create NoteTagAssignments table
3. Alter Notes table for import fields

### Phase 2: Service Layer
1. Implement TagService and repository
2. Implement WikiLinkParserService
3. Implement MarkdownImportService
4. Implement AutoSaveService

### Phase 3: UI Components
1. Build NotesSidebar
2. Build TagSelector and TagBadge
3. Build MarkdownImportDialog
4. Build AutoSaveIndicator

### Phase 4: Export & Navigation
1. Implement MarkdownExportService
2. Build MarkdownExportDialog
3. Build WorkspaceNavigationDialog
4. Update routing

### Phase 5: Advanced Features
1. Implement bracket wrapping
2. SKOS integration
3. Tag hierarchy (if needed)
4. Advanced import options

## Open Questions & Decisions Needed

1. **Tag Colors**: Auto-generate from palette or user-selectable?
2. **Tag Limits**: Max tags per note (suggest: 10), max tag length (suggest: 50 chars)?
3. **Concurrent Editing**: How to handle conflicts? Suggest: last-write-wins with version warning
4. **Wiki-Link Ambiguity**: If [[Person]] could match multiple concepts, how to resolve? Suggest: show disambiguation dialog
5. **Export Naming**: How to handle duplicate note titles? Suggest: append counter (note-1.md, note-2.md)
6. **SKOS Namespace**: Custom namespace or use standard SKOS? Suggest: custom with SKOS imports
7. **Tag Deletion**: What happens to notes with deleted tags? Suggest: unassign but keep notes
8. **Import Conflicts**: Overwrite, skip, or create duplicate? Suggest: show conflict resolution UI

## Dependencies

### NuGet Packages
- `YamlDotNet` (>=13.0.0) - YAML frontmatter parsing
- `SharpZipLib` or `System.IO.Compression` - ZIP export
- `Markdig` (existing) - Markdown rendering

### JavaScript Libraries
- No new dependencies (use vanilla JS)

## Rollout Plan

### Week 1: Foundation
- Database schema and migrations
- Domain models
- Repository layer
- TagService implementation

### Week 2: Import System
- MarkdownImportService
- WikiLinkParserService
- MarkdownImportDialog UI
- Basic file parsing tests

### Week 3: Tag UI
- NotesSidebar component
- TagBadge and TagSelector
- Tag assignment UI
- Virtual folder navigation

### Week 4: Auto-Save
- AutoSaveService
- AutoSaveIndicator component
- JavaScript integration
- Debouncing tests

### Week 5: Export & Navigation
- MarkdownExportService
- Export dialog
- WorkspaceNavigationDialog
- Bracket wrapping feature

### Week 6: Polish & Integration
- SKOS integration (optional)
- Performance optimization
- Bug fixes
- Documentation updates

## Success Criteria

- ✅ Import 100+ markdown files in < 30 seconds
- ✅ Auto-save completes within 500ms of inactivity
- ✅ Tag filtering responds in < 100ms for 1000+ notes
- ✅ Wiki-link parsing accuracy > 99%
- ✅ Export/import roundtrip preserves all content
- ✅ Zero data loss in auto-save
- ✅ Mobile-responsive tag UI
