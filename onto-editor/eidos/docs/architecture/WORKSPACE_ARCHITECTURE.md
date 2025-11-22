# Workspace Architecture

## Overview

The workspace feature integrates note-taking with ontology management, providing a unified knowledge management system. This document details the architecture, data model, and implementation patterns.

## Design Principles

1. **Separation of Concerns**: Notes and ontologies are separate but linked entities
2. **Performance First**: Content separated from metadata for efficient queries
3. **Automatic Linking**: Wiki-style syntax creates bidirectional connections
4. **Legacy Compatible**: Existing ontologies seamlessly gain workspace features

## Data Model

### Entity Relationships

```
┌──────────────┐ 1:1  ┌──────────────┐
│  Workspace   │◄────►│   Ontology   │
└──────────────┘      └──────────────┘
        │                      ▲
        │ 1:N                  │
        ▼                      │
┌──────────────┐               │
│     Note     │               │
└──────────────┘               │
        │                      │
        │ 1:1                  │
        ▼                      │
┌──────────────┐               │
│ NoteContent  │               │
└──────────────┘               │
        │                      │
        │ 1:N                  │
        ▼                      │
┌──────────────┐ N:1           │
│   NoteLink   │───────────────┘
└──────────────┘       (via LinkedConceptId)
```

### Core Entities

#### Workspace

Represents a knowledge workspace containing an ontology and notes.

**Properties:**

- `Id`: Primary key
- `Name`: Workspace name
- `Description`: Optional description
- `UserId`: Owner
- `Visibility`: private | group | public
- `AllowPublicEdit`: Boolean
- `NoteCount`, `ConceptNoteCount`, `UserNoteCount`: Computed metrics

**Navigation Properties:**

- `Ontology`: Associated ontology (1:1)
- `Notes`: Collection of notes (1:N)

**Design Notes:**

- 1:1 relationship with Ontology via `Ontology.WorkspaceId`
- Ontology can exist without workspace (legacy support)
- Workspace always has an ontology

#### Note

Metadata for a note without the large markdown content.

**Properties:**

- `Id`: Primary key
- `WorkspaceId`: Parent workspace
- `Title`: Note title
- `IsConceptNote`: Boolean flag
- `LinkedConceptId`: Optional concept link (for concept notes)
- `UserId`: Creator
- `LinkCount`: Number of [[links]] in content
- `CreatedAt`, `UpdatedAt`: Timestamps

**Navigation Properties:**

- `Workspace`: Parent workspace
- `LinkedConcept`: Associated concept (if IsConceptNote)
- `NoteContent`: Markdown content (1:1)
- `OutgoingLinks`: Links to concepts (1:N)

**Design Notes:**

- Content separated for performance (avoid loading large text in lists)
- `IsConceptNote` distinguishes auto-created concept notes from user notes
- `LinkCount` cached for performance

#### NoteContent

Large markdown content separated from note metadata.

**Properties:**

- `Id`: Primary key
- `NoteId`: Parent note (FK)
- `MarkdownContent`: Full markdown text
- `UpdatedAt`: Last modified timestamp

**Design Notes:**

- Separation pattern improves list view performance
- Future: Can add versioning by making 1:N relationship
- Future: Can add collaborative editing metadata (cursors, locks)

#### NoteLink

Extracted [[wiki-link]] with position and context for backlinks.

**Properties:**

- `Id`: Primary key
- `SourceNoteId`: Note containing the link
- `TargetConceptId`: Referenced concept
- `CharacterPosition`: Position in markdown
- `ContextSnippet`: Surrounding text (50 chars each side)
- `CreatedAt`: Link creation time

**Navigation Properties:**

- `SourceNote`: Note containing link
- `TargetConcept`: Referenced concept

**Design Notes:**

- Populated by WikiLinkParser when note is saved
- Context snippets enable rich backlinks panel
- Position enables future features (go-to-definition)

## Services Layer

### WikiLinkParser

Stateless service for parsing `[[wiki-style]]` links.

**Key Methods:**

- `ExtractConceptNames(markdown)` - Get unique concept names
- `ExtractLinksWithContext(markdown)` - Get links with position and context
- `CountLinks(markdown)` - Count links
- `ContainsLinks(markdown)` - Boolean check
- `ReplaceLinks(markdown, func)` - Transform links
- `ConvertLinksToHtml(markdown)` - Generate HTML anchors
- `IsValidConceptName(name)` - Validation
- `EscapeConceptName(name)` - Sanitization

**Design Notes:**

- Uses compiled regex for performance
- Supports display text: `[[Concept|Display]]`
- Context window: 50 characters before/after link
- Handles edge cases (nested brackets, multiline, special chars)
- 45+ unit tests ensure robustness

**Regex Pattern:**

```regex
\[\[([^\]|]+)(?:\|([^\]]+))?\]\]
```

- Group 1: Concept name (required)
- Group 2: Display text (optional, after pipe)

### WorkspaceService

Business logic for workspace management.

**Key Methods:**

- `CreateWorkspaceAsync()` - Create with auto-ontology
- `GetWorkspaceAsync()` - Load with permission check
- `GetUserWorkspacesAsync()` - List user's workspaces
- `UpdateWorkspaceAsync()` - Update metadata
- `DeleteWorkspaceAsync()` - Delete (owner only)
- `EnsureWorkspaceForOntologyAsync()` - Legacy migration

**Design Notes:**

- Permission checking via `OntologyPermissionService`
- Auto-creates ontology when creating workspace
- `EnsureWorkspaceForOntologyAsync` handles legacy ontologies:
  1. Check if ontology has workspace
  2. If not, create workspace with matching metadata
  3. Link ontology via `WorkspaceId`
  4. Auto-create concept notes for existing concepts
  5. Return workspace with Ontology navigation property loaded

**Critical Bug Fix (Nov 15, 2025):**
The `EnsureWorkspaceForOntologyAsync` method was failing to persist the `WorkspaceId` because `OntologyRepository.UpdateAsync` wasn't copying that property. Fixed by adding `existingOntology.WorkspaceId = ontology.WorkspaceId` in the repository update method.

### NoteService

Business logic for note management and wiki-link processing.

**Key Methods:**

- `CreateNoteAsync()` - Create user note
- `CreateConceptNoteAsync()` - Create concept note
- `UpdateNoteContentAsync()` - Update and reprocess links
- `GetNoteWithContentAsync()` - Load with content
- `GetWorkspaceNotesAsync()` - List notes
- `GetBacklinksAsync()` - Find references to concept
- `DeleteNoteAsync()` - Delete with permission check
- `EnsureConceptNotesForOntologyAsync()` - Bulk create for legacy

**Design Notes:**

- `ProcessWikiLinksAsync()` extracts and stores links when note saved
- Auto-creates concepts via `FindOrCreateConceptAsync()`
- Updates `LinkCount` cached value
- Permission checking via workspace access
- Transaction handling ensures consistency

**Wiki-Link Processing Flow:**

1. Parse markdown with `WikiLinkParser.ExtractLinksWithContext()`
2. For each link:
   - Find or create concept in ontology
   - Ensure concept note exists
   - Create NoteLink record with context
3. Delete old links, insert new ones (replace pattern)
4. Update note's `LinkCount`

## Repository Layer

### WorkspaceRepository

Data access for workspaces.

**Key Methods:**

- `GetByIdAsync(id, includeOntology, includeNotes)` - Flexible loading
- `GetByUserIdAsync(userId)` - User's workspaces
- `CreateAsync(workspace)` - Insert
- `UpdateAsync(workspace)` - Update
- `DeleteAsync(id)` - Delete
- `UpdateNoteCountsAsync(id)` - Recompute counts
- `UserHasAccessAsync(workspaceId, userId)` - Permission check
- `GetPublicWorkspacesAsync()` - Discovery

**Design Notes:**

- Uses `Include()` for eager loading navigation properties
- `AsNoTracking()` for read-only queries
- Computed counts updated via raw SQL for performance
- Permission checks delegate to ontology permissions

### NoteRepository

Data access for notes and content.

**Key Methods:**

- `CreateAsync(note, markdown)` - Create with content
- `GetByIdAsync(id)` - Metadata only
- `GetByIdWithContentAsync(id)` - With content
- `GetByWorkspaceIdAsync(workspaceId, conceptNotesOnly)` - List
- `GetConceptNoteAsync(conceptId)` - Find by concept
- `UpdateContentAsync(noteId, markdown)` - Update content
- `UpdateMetadataAsync(noteId, linkCount)` - Update cache
- `UpdateNoteLinksAsync(noteId, links)` - Replace links
- `GetBacklinksAsync(conceptId)` - Find references
- `SearchByTitleAsync(workspaceId, term)` - Title search
- `DeleteAsync(id)` - Delete with content and links

**Design Notes:**

- Separate queries for content vs metadata
- Uses transactions for multi-table updates
- Includes content table and links in delete cascade
- Search uses `LIKE` with wildcards (future: full-text search)

## Performance Optimizations

### Content Separation

Notes and NoteContent in separate tables:

- **Benefit**: List views don't load large markdown text
- **Pattern**: Common in forums (Discourse), blogs (Ghost), wikis (MediaWiki)
- **Trade-off**: Extra join when loading content (acceptable for read performance)

### Query Patterns

**Efficient Loading:**

```csharp
// List view - no content
var notes = await context.Notes
    .AsNoTracking()
    .Where(n => n.WorkspaceId == id)
    .OrderByDescending(n => n.UpdatedAt)
    .ToListAsync();

// Detail view - with content
var note = await context.Notes
    .Include(n => n.NoteContent)
    .Include(n => n.OutgoingLinks)
        .ThenInclude(l => l.TargetConcept)
    .FirstOrDefaultAsync(n => n.Id == id);
```

**Count Updates (SQL):**

```sql
UPDATE Workspaces
SET NoteCount = (SELECT COUNT(*) FROM Notes WHERE WorkspaceId = @id),
    ConceptNoteCount = (SELECT COUNT(*) FROM Notes WHERE WorkspaceId = @id AND IsConceptNote = 1),
    UserNoteCount = (SELECT COUNT(*) FROM Notes WHERE WorkspaceId = @id AND IsConceptNote = 0)
WHERE Id = @id;
```

### Indexes

Critical indexes for performance:

- `Notes.WorkspaceId` (foreign key, list queries)
- `Notes.LinkedConceptId` (concept note lookup)
- `NoteLinks.SourceNoteId` (delete cascade)
- `NoteLinks.TargetConceptId` (backlinks)
- `Ontologies.WorkspaceId` (1:1 relationship)

### Caching

- `LinkCount` cached in Note table (updated on save)
- Note counts cached in Workspace table (updated on create/delete)
- Future: Redis cache for popular notes

## Component Architecture

### Three-Pane Layout

**WorkspaceView.razor** - Main component

```
┌─────────────────────────────────────────────────────┐
│ Workspace Header (title, actions, view mode toggle) │
├──────────┬─────────────────────────┬─────────────────┤
│  Note    │  Markdown Editor        │   Backlinks     │
│ Explorer │  (Edit/Preview toggle)  │  & Related      │
│          │                         │   Concepts      │
│ (search, │  ┌──────────────────┐   │                 │
│  create, │  │ Title Input      │   │  ┌───────────┐  │
│  list)   │  ├──────────────────┤   │  │ Backlink  │  │
│          │  │                  │   │  │ Backlink  │  │
│          │  │  Content         │   │  │ Backlink  │  │
│          │  │                  │   │  └───────────┘  │
│          │  │                  │   │                 │
│          │  └──────────────────┘   │  ┌───────────┐  │
│          │                         │  │ Related   │  │
│          │  [Save] [Preview/Edit]  │  │ Concept   │  │
│          │                         │  └───────────┘  │
└──────────┴─────────────────────────┴─────────────────┘
```

**State Management:**

- `workspace` - Current workspace (from route param)
- `notes` - List of notes
- `selectedNote` - Currently editing
- `noteContent` - Markdown text (two-way bound)
- `showPreview` - Edit vs preview mode
- `isCondensedView` - List density
- `searchTerm` - Filter string
- `saving` - Save indicator

**Component Lifecycle:**

1. `OnParametersSetAsync()` - Load workspace by ID
2. `LoadWorkspaceAsync()` - Fetch with permission check
3. `LoadNotesAsync()` - Get note list
4. User interaction triggers state changes
5. `SaveNote()` - Persist with wiki-link processing

### Quick Switcher

**WorkspaceQuickSwitcher.razor** - Modal note picker

**Features:**

- Keyboard activation (`Cmd/Ctrl + K`)
- Real-time filter
- Keyboard navigation (arrows, enter, escape)
- Focus management
- Click-outside-to-close

**State:**

- `isOpen` - Visibility
- `searchQuery` - Filter text
- `filteredNotes` - Computed property
- `selectedIndex` - Keyboard selection

## Integration Points

### Graph View ↔ Workspace View

**Navigation from Graph to Workspace:**

- Concept details panel has "Open Note" button
- Button appears if concept has associated note
- Navigates to `/workspace/{workspaceId}?noteId={noteId}`

**Navigation from Workspace to Graph:**

- Toolbar has "View in Graph" button
- Appears if workspace has ontology (always, after migration)
- Navigates to `/ontology/{ontologyId}`

**Implementation:**

```csharp
// In SelectedNodeDetailsPanel.razor
private async Task ViewConceptNote()
{
    var note = await NoteRepository.GetConceptNoteAsync(SelectedConcept.Id);
    if (note != null)
    {
        Navigation.NavigateTo($"workspace/{note.WorkspaceId}?noteId={note.Id}");
    }
}

// In WorkspaceView.razor
<button @onclick="@(() => Navigation.NavigateTo($"ontology/{workspace.Ontology.Id}"))">
    <i class="bi bi-diagram-3"></i>
</button>
```

### Permissions

Workspace permissions inherit from ontology permissions:

```csharp
// Check via WorkspaceRepository
var hasAccess = await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);

// Delegates to OntologyPermissionService
public async Task<bool> UserHasAccessAsync(int workspaceId, string userId)
{
    var workspace = await GetByIdAsync(workspaceId, includeOntology: true);
    if (workspace?.Ontology == null) return false;

    return await _permissionService.CanViewAsync(workspace.Ontology.Id, userId);
}
```

Permission levels (from OntologyPermissionService):

- **View**: Can read notes and view graph
- **ViewAndAdd**: Can create notes and concepts
- **ViewAddEdit**: Can edit existing notes/concepts
- **FullAccess**: Can delete, manage permissions

## Migration Strategy

### Legacy Ontology Support

Existing ontologies without workspaces are handled automatically:

**Detection:**

```csharp
if (workspace?.Ontology == null)
{
    // Ontology navigation property not loaded
    // Trigger migration
    workspace = await WorkspaceService.EnsureWorkspaceForOntologyAsync(ontologyId);
}
```

**Migration Steps:**

1. Load ontology
2. Check if `ontology.WorkspaceId` is null
3. If null, create workspace:
   - Name/description from ontology
   - Visibility/permissions from ontology
   - Link via `ontology.WorkspaceId = workspace.Id`
4. Bulk create concept notes for all concepts
5. Update workspace note counts
6. Return workspace with Ontology navigation property

**Database Changes:**

```sql
-- Migration adds nullable WorkspaceId
ALTER TABLE Ontologies ADD COLUMN WorkspaceId INTEGER NULL;
CREATE INDEX IX_Ontologies_WorkspaceId ON Ontologies(WorkspaceId);
ALTER TABLE Ontologies ADD CONSTRAINT FK_Ontologies_Workspaces
    FOREIGN KEY (WorkspaceId) REFERENCES Workspaces(Id) ON DELETE SET NULL;
```

### Automatic Concept Note Creation

When migrating legacy ontology:

```csharp
public async Task<int> EnsureConceptNotesForOntologyAsync(int workspaceId, int ontologyId, string userId)
{
    var concepts = await _conceptRepository.GetByOntologyIdAsync(ontologyId);
    int createdCount = 0;

    foreach (var concept in concepts)
    {
        var existingNote = await _noteRepository.GetConceptNoteAsync(concept.Id);
        if (existingNote == null)
        {
            await CreateConceptNoteAsync(workspaceId, userId, concept.Id, concept.Name);
            createdCount++;
        }
    }

    return createdCount;
}
```

## Error Handling

### Validation

**Note Creation:**

- Title required (non-empty, trimmed)
- Workspace must exist
- User must have access (check via workspace permissions)

**Concept Names (Wiki-Links):**

- Cannot contain: `[`, `]`, `|`, newlines
- Validated by `WikiLinkParser.IsValidConceptName()`
- Invalid characters escaped by `EscapeConceptName()`

### Transaction Handling

**Multi-Table Updates:**

```csharp
using var transaction = await context.Database.BeginTransactionAsync();
try
{
    // 1. Update note content
    await context.SaveChangesAsync();

    // 2. Delete old links
    await context.SaveChangesAsync();

    // 3. Insert new links
    await context.SaveChangesAsync();

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### Permission Denials

All service methods check permissions and return `null` or `false` for unauthorized access:

```csharp
// Service layer
var hasAccess = await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);
if (!hasAccess)
{
    _logger.LogWarning("User {UserId} denied access to workspace {WorkspaceId}", userId, workspaceId);
    return null; // or throw UnauthorizedAccessException
}
```

UI layer handles nulls gracefully:

```csharp
@if (workspace == null)
{
    <div class="error-state">
        <p>Workspace not found or access denied.</p>
    </div>
}
```

## Testing Strategy

### Unit Tests

**WikiLinkParser** (45+ tests):

- Link extraction (simple, display text, multi-word)
- Context snippet generation
- HTML conversion
- Validation and escaping
- Edge cases (nested, multiline, special chars)

Example:

```csharp
[Fact]
public void ExtractLinksWithContext_SimpleLink_ReturnsLinkWithContext()
{
    var content = "This is a note about [[Person]].";
    var result = _parser.ExtractLinksWithContext(content);

    Assert.Single(result);
    Assert.Equal("Person", result[0].ConceptName);
    Assert.Contains("note about", result[0].ContextSnippet);
}
```

### Integration Tests (Future)

- End-to-end workspace creation flow
- Wiki-link processing with database
- Permission checking integration
- Backlinks query performance

### Manual Testing

- Created test workspaces with various note configurations
- Tested legacy ontology migration
- Verified "View in Graph" and "Open Note" navigation
- Tested keyboard shortcuts
- Tested responsive layout

## Security Considerations

### XSS Protection

Markdown rendered via Markdig with sanitization:

```csharp
var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .Build();

var html = Markdown.ToHtml(markdown, pipeline);
// Markdig sanitizes by default (no <script> tags, etc.)
```

### SQL Injection

Protected by parameterized queries via EF Core:

```csharp
// Safe - uses parameters
var notes = await context.Notes
    .Where(n => n.WorkspaceId == workspaceId)
    .ToListAsync();
```

### Permission Bypass

Multiple layers of protection:

1. UI hides unauthorized actions
2. Component checks permissions before rendering
3. Service layer validates access
4. Repository enforces user scoping

### Data Validation

- User input sanitized (trim, escape)
- Concept names validated for illegal characters
- File size limits on markdown content (future)

## Future Enhancements

### Short Term

- Full-text search in note content (SQLite FTS5)
- Note templates for common patterns
- Folder/tag organization
- Export workspace to zip

### Medium Term

- Real-time collaborative editing (SignalR + OT)
- Note version history with diff view
- Attachment support (images, PDFs)
- Graph visualization of note connections

### Long Term

- Plugin system for custom note types
- AI-powered concept extraction
- Knowledge graph analytics
- Mobile apps (Blazor Hybrid)

## Monitoring and Metrics

### Performance Metrics

Track via Application Insights:

- Note save latency
- Wiki-link parse time
- Workspace load time
- Backlinks query performance

### Usage Metrics

- Notes created per workspace
- Links created per note
- Most referenced concepts (backlink count)
- Workspace creation rate

### Error Tracking

Log important events:

- Workspace creation/migration
- Permission denials
- Failed link parsing
- Database errors

---

*Last Updated: November 15, 2025*
*Version: 1.0*
