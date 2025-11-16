# Notes Functionality - Architecture Design
**Feature Version**: 1.1
**Created**: 2025-11-14
**Status**: In Progress

## Architecture Overview

This feature introduces a **workspace-centric architecture** where the Workspace entity becomes the new root container for both Ontology and Notes functionality.

```
┌─────────────────────────────────────────────────────────────┐
│                         Workspace                            │
│  (New root entity - replaces Ontology as top level)        │
│                                                              │
│  ┌──────────────────────┐    ┌──────────────────────────┐  │
│  │      Ontology        │    │        Notes             │  │
│  │  (Existing system)   │    │    (New system)          │  │
│  │                      │    │                          │  │
│  │  ├─ Concepts         │    │  ├─ User Notes          │  │
│  │  ├─ Relationships    │    │  └─ Concept Notes       │  │
│  │  ├─ Individuals      │    │     (Auto-generated)     │  │
│  │  └─ Properties       │    │                          │  │
│  └──────────────────────┘    └──────────────────────────┘  │
│                                                              │
│                    [[concept]] references                    │
│           Notes ──────────────────────────> Concepts        │
└─────────────────────────────────────────────────────────────┘
```

## Database Schema Design

### 1. Workspace Entity (New)

```csharp
public class Workspace
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Owner
    [Required]
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Visibility settings (moved from Ontology)
    public OntologyVisibility Visibility { get; set; } = OntologyVisibility.Private;
    public bool AllowPublicEdit { get; set; } = false;

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relationships
    public Ontology? Ontology { get; set; } // 1:1
    public ICollection<Note> Notes { get; set; } = new List<Note>(); // 1:many

    // Permissions
    public ICollection<WorkspaceGroupPermission> GroupPermissions { get; set; } = new List<WorkspaceGroupPermission>();
    public ICollection<WorkspaceUserAccess> UserAccesses { get; set; } = new List<WorkspaceUserAccess>();
}
```

**Migration Note**: Existing `OntologyGroupPermissions` and `UserShareAccesses` will be migrated to `WorkspaceGroupPermissions` and `WorkspaceUserAccess`.

### 2. Note Entity (New)

```csharp
public enum RenderFormat
{
    Markdown,
    RichText
}

public class Note
{
    public int Id { get; set; }

    // Workspace relationship
    public int WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public RenderFormat RenderFormat { get; set; } = RenderFormat.Markdown;

    // Concept note tracking
    public bool IsConceptNote { get; set; } = false;
    public int? LinkedConceptId { get; set; }
    public Concept? LinkedConcept { get; set; }

    // Creator
    [Required]
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Future: Tags, Categories
    // public string? Tags { get; set; }
}
```

### 3. ConceptReference Entity (New - for tracking [[]] references)

```csharp
public class ConceptReference
{
    public int Id { get; set; }

    // Note that contains the reference
    public int NoteId { get; set; }
    public Note Note { get; set; } = null!;

    // Concept being referenced
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;

    // Position in note (for highlighting)
    public int CharacterPosition { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 4. Updated Ontology Entity

```csharp
public class Ontology
{
    public int Id { get; set; }

    // NEW: Link to workspace
    public int WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    // Existing fields remain unchanged
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    // ... all existing properties ...

    // REMOVED: Visibility, AllowPublicEdit (moved to Workspace)
    // REMOVED: UserId (moved to Workspace)
}
```

### 5. Updated Concept Entity

```csharp
public class Concept
{
    // Existing fields...

    // NEW: Concept note relationship
    public Note? ConceptNote { get; set; }

    // NEW: Track if created from note reference
    public bool CreatedFromNoteReference { get; set; } = false;
    public int? SourceNoteId { get; set; }

    // NEW: References from notes
    public ICollection<ConceptReference> References { get; set; } = new List<ConceptReference>();
}
```

## Database Migration Strategy

### Phase 1: Schema Creation

```sql
-- Step 1: Create Workspace table
CREATE TABLE Workspaces (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    UserId NVARCHAR(450) NOT NULL,
    Visibility INT NOT NULL DEFAULT 0,
    AllowPublicEdit BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Workspaces_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);

-- Step 2: Create Notes table
CREATE TABLE Notes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    WorkspaceId INT NOT NULL,
    Title NVARCHAR(500) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    RenderFormat INT NOT NULL DEFAULT 0,
    IsConceptNote BIT NOT NULL DEFAULT 0,
    LinkedConceptId INT NULL,
    UserId NVARCHAR(450) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Notes_Workspaces FOREIGN KEY (WorkspaceId) REFERENCES Workspaces(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Notes_Concepts FOREIGN KEY (LinkedConceptId) REFERENCES Concepts(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Notes_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);

-- Step 3: Create ConceptReferences table
CREATE TABLE ConceptReferences (
    Id INT PRIMARY KEY IDENTITY(1,1),
    NoteId INT NOT NULL,
    ConceptId INT NOT NULL,
    CharacterPosition INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_ConceptReferences_Notes FOREIGN KEY (NoteId) REFERENCES Notes(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ConceptReferences_Concepts FOREIGN KEY (ConceptId) REFERENCES Concepts(Id) ON DELETE CASCADE
);

-- Step 4: Add WorkspaceId to Ontologies
ALTER TABLE Ontologies ADD WorkspaceId INT NULL;
ALTER TABLE Ontologies ADD CONSTRAINT FK_Ontologies_Workspaces FOREIGN KEY (WorkspaceId) REFERENCES Workspaces(Id);
```

### Phase 2: Data Migration

```sql
-- Migrate each Ontology to a Workspace
INSERT INTO Workspaces (Name, Description, UserId, Visibility, AllowPublicEdit, CreatedAt, UpdatedAt)
SELECT
    Name,
    Description,
    UserId,
    Visibility,
    AllowPublicEdit,
    CreatedAt,
    UpdatedAt
FROM Ontologies;

-- Update Ontologies with WorkspaceId
UPDATE o
SET o.WorkspaceId = w.Id
FROM Ontologies o
INNER JOIN Workspaces w ON w.Name = o.Name AND w.UserId = o.UserId;

-- Create concept notes for all existing concepts
INSERT INTO Notes (WorkspaceId, Title, Content, RenderFormat, IsConceptNote, LinkedConceptId, UserId, CreatedAt, UpdatedAt)
SELECT
    ont.WorkspaceId,
    CONCAT(c.Name, '-note'),
    CONCAT('# ', c.Name, CHAR(13), CHAR(10), CHAR(13), CHAR(10), 'Notes about this concept...'),
    0, -- Markdown
    1, -- IsConceptNote = true
    c.Id,
    ont.UserId,
    GETUTCDATE(),
    GETUTCDATE()
FROM Concepts c
INNER JOIN Ontologies ont ON c.OntologyId = ont.Id;

-- Migrate permissions (if using group permissions)
-- This will need custom logic based on your permission structure
```

### Phase 3: Cleanup

```sql
-- Make WorkspaceId NOT NULL after migration
ALTER TABLE Ontologies ALTER COLUMN WorkspaceId INT NOT NULL;

-- Remove redundant columns from Ontologies (optional - keep for backwards compat)
-- ALTER TABLE Ontologies DROP COLUMN Visibility;
-- ALTER TABLE Ontologies DROP COLUMN AllowPublicEdit;
-- ALTER TABLE Ontologies DROP COLUMN UserId;
```

## Service Layer Architecture

### 1. WorkspaceService (New)

```csharp
public interface IWorkspaceService
{
    Task<Workspace> CreateAsync(Workspace workspace);
    Task<Workspace> GetByIdAsync(int id);
    Task<List<Workspace>> GetByUserIdAsync(string userId);
    Task<Workspace> UpdateAsync(Workspace workspace);
    Task DeleteAsync(int id);

    // Permission checking
    Task<bool> CanViewAsync(int workspaceId, string userId);
    Task<bool> CanEditAsync(int workspaceId, string userId);
    Task<bool> CanManageAsync(int workspaceId, string userId);

    // Stats
    Task<WorkspaceStats> GetStatsAsync(int workspaceId);
}

public class WorkspaceStats
{
    public int ConceptCount { get; set; }
    public int RelationshipCount { get; set; }
    public int NoteCount { get; set; }
    public int ConceptNoteCount { get; set; }
    public int UserNoteCount { get; set; }
}
```

### 2. NoteService (New)

```csharp
public interface INoteService
{
    Task<Note> CreateAsync(Note note);
    Task<Note> CreateConceptNoteAsync(int workspaceId, int conceptId, string userId);
    Task<Note> GetByIdAsync(int id);
    Task<List<Note>> GetByWorkspaceIdAsync(int workspaceId);
    Task<List<Note>> GetConceptNotesAsync(int workspaceId);
    Task<List<Note>> GetUserNotesAsync(int workspaceId);
    Task<Note?> GetConceptNoteAsync(int conceptId);
    Task<Note> UpdateAsync(Note note);
    Task DeleteAsync(int id);

    // [[concept]] parsing
    Task<List<string>> ParseConceptReferencesAsync(string content);
    Task ProcessConceptReferencesAsync(int noteId, string content);
    Task<List<ConceptReference>> GetReferencesInNoteAsync(int noteId);
    Task<List<Note>> GetNotesReferencingConceptAsync(int conceptId);
}
```

### 3. Updated ConceptService

```csharp
public interface IConceptService
{
    // Existing methods...

    // NEW: Auto-create concept note when creating concept
    Task<Concept> CreateAsync(Concept concept, bool createNote = true);

    // NEW: Get concept's note
    Task<Note?> GetConceptNoteAsync(int conceptId);

    // NEW: Create from note reference
    Task<Concept> CreateFromNoteReferenceAsync(string conceptName, int ontologyId, int sourceNoteId);
}
```

## UI Architecture

### Component Structure

```
Components/
├── Workspace/
│   ├── WorkspaceCard.razor          (List view card)
│   ├── WorkspaceList.razor          (Grid of workspaces)
│   ├── WorkspaceDetail.razor        (Tabbed view: Ontology + Notes)
│   └── WorkspaceSettings.razor      (Settings dialog)
│
├── Notes/
│   ├── NoteList.razor               (List of notes with search)
│   ├── NoteEditor.razor             (Markdown/Rich text editor)
│   ├── NotePreview.razor            (Markdown preview pane)
│   ├── ConceptReferenceHighlight.razor (Highlight [[concept]] syntax)
│   └── ConceptAutocomplete.razor    (Autocomplete for [[
│
├── Ontology/
│   ├── ConceptDetailPanel.razor     (Updated: show concept note)
│   └── [existing components]
│
└── Pages/
    ├── WorkspaceView.razor          (Replaces OntologyView as main page)
    ├── NotesView.razor              (Notes-focused view)
    └── [existing pages]
```

### Navigation Updates

```html
<!-- Updated NavMenu.razor -->
<div class="nav-item px-3">
    <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
        <span class="bi bi-grid-3x3-gap-fill" aria-hidden="true"></span> Workspaces
    </NavLink>
</div>

<!-- Within a workspace -->
<div class="nav-item px-3">
    <NavLink class="nav-link" href="@($"workspace/{WorkspaceId}/ontology")">
        <span class="bi bi-diagram-3-fill" aria-hidden="true"></span> Ontology
    </NavLink>
</div>
<div class="nav-item px-3">
    <NavLink class="nav-link" href="@($"workspace/{WorkspaceId}/notes")">
        <span class="bi bi-journal-text" aria-hidden="true"></span> Notes
    </NavLink>
</div>
```

### Routing Updates

```csharp
// App.razor or Program.cs routing
@page "/workspace/{WorkspaceId:int}"
@page "/workspace/{WorkspaceId:int}/ontology"
@page "/workspace/{WorkspaceId:int}/notes"
@page "/workspace/{WorkspaceId:int}/notes/{NoteId:int}"
@page "/workspace/{WorkspaceId:int}/settings"
```

## Concept Reference Parsing Implementation

### Core Parser

```csharp
public class ConceptReferenceParser
{
    private static readonly Regex ConceptReferenceRegex =
        new Regex(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

    public List<ConceptReferenceMatch> Parse(string content)
    {
        var matches = new List<ConceptReferenceMatch>();
        var regexMatches = ConceptReferenceRegex.Matches(content);

        foreach (Match match in regexMatches)
        {
            matches.Add(new ConceptReferenceMatch
            {
                ConceptName = match.Groups[1].Value.Trim(),
                StartPosition = match.Index,
                EndPosition = match.Index + match.Length,
                FullMatch = match.Value
            });
        }

        return matches;
    }
}

public class ConceptReferenceMatch
{
    public string ConceptName { get; set; } = string.Empty;
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public string FullMatch { get; set; } = string.Empty;
}
```

### Auto-Creation Logic

```csharp
public class NoteService : INoteService
{
    private readonly IConceptService _conceptService;
    private readonly ConceptReferenceParser _parser;

    public async Task ProcessConceptReferencesAsync(int noteId, string content)
    {
        var note = await _noteRepository.GetByIdAsync(noteId);
        if (note == null) return;

        var references = _parser.Parse(content);
        var workspace = await _workspaceRepository.GetByIdAsync(note.WorkspaceId);
        var ontology = workspace.Ontology;

        foreach (var reference in references)
        {
            // Check if concept exists
            var concept = await _conceptService.GetByNameAsync(ontology.Id, reference.ConceptName);

            if (concept == null)
            {
                // Auto-create concept
                concept = await _conceptService.CreateFromNoteReferenceAsync(
                    reference.ConceptName,
                    ontology.Id,
                    noteId
                );

                _logger.LogInformation("Auto-created concept '{ConceptName}' from note {NoteId}",
                    reference.ConceptName, noteId);
            }

            // Create/update reference record
            await _conceptReferenceRepository.UpsertAsync(new ConceptReference
            {
                NoteId = noteId,
                ConceptId = concept.Id,
                CharacterPosition = reference.StartPosition
            });
        }

        // Remove obsolete references
        await CleanupObsoleteReferencesAsync(noteId, references);
    }
}
```

## JavaScript Interop for Note Editor

### Markdown Editor Integration

```javascript
// wwwroot/js/noteEditor.js
window.noteEditorFunctions = {
    initializeMarkdownEditor: function(elementId, dotNetHelper) {
        const editor = document.getElementById(elementId);

        // Syntax highlighting for [[concept]] references
        editor.addEventListener('input', function(e) {
            highlightConceptReferences(editor);
        });

        // Autocomplete for [[
        editor.addEventListener('keyup', function(e) {
            const cursorPos = editor.selectionStart;
            const text = editor.value;

            if (text.substring(cursorPos - 2, cursorPos) === '[[') {
                dotNetHelper.invokeMethodAsync('ShowConceptAutocomplete', cursorPos);
            }
        });
    },

    highlightConceptReferences: function(editor) {
        const text = editor.value;
        const regex = /\[\[([^\]]+)\]\]/g;
        let match;

        // This would integrate with a highlighting library
        // For now, we'll use CSS classes
        while ((match = regex.exec(text)) !== null) {
            // Highlight logic here
        }
    }
};
```

## Performance Considerations

### Indexing Strategy

```sql
-- Workspace indexes
CREATE INDEX IX_Workspaces_UserId ON Workspaces(UserId);
CREATE INDEX IX_Workspaces_Visibility ON Workspaces(Visibility);

-- Note indexes
CREATE INDEX IX_Notes_WorkspaceId ON Notes(WorkspaceId);
CREATE INDEX IX_Notes_LinkedConceptId ON Notes(LinkedConceptId);
CREATE INDEX IX_Notes_IsConceptNote ON Notes(IsConceptNote);
CREATE INDEX IX_Notes_UserId ON Notes(UserId);

-- Full-text search on note content (future)
CREATE FULLTEXT INDEX ON Notes(Title, Content) KEY INDEX PK_Notes;

-- ConceptReference indexes
CREATE INDEX IX_ConceptReferences_NoteId ON ConceptReferences(NoteId);
CREATE INDEX IX_ConceptReferences_ConceptId ON ConceptReferences(ConceptId);
```

### Caching Strategy

```csharp
// Cache workspace stats
services.AddMemoryCache();

public async Task<WorkspaceStats> GetStatsAsync(int workspaceId)
{
    return await _cache.GetOrCreateAsync($"workspace-stats-{workspaceId}", async entry =>
    {
        entry.SlidingExpiration = TimeSpan.FromMinutes(5);
        return await CalculateStatsAsync(workspaceId);
    });
}
```

## Security Considerations

### Permission Enforcement

```csharp
// All note operations must check workspace permissions
public async Task<Note> GetByIdAsync(int id)
{
    var note = await _repository.GetByIdAsync(id);
    if (note == null) return null;

    var canView = await _workspacePermissionService.CanViewAsync(
        note.WorkspaceId,
        _currentUser.Id
    );

    if (!canView)
        throw new UnauthorizedAccessException("Cannot access this note");

    return note;
}
```

### XSS Prevention in Notes

```csharp
// Sanitize rich text content
public async Task<Note> UpdateAsync(Note note)
{
    if (note.RenderFormat == RenderFormat.RichText)
    {
        note.Content = SanitizeHtml(note.Content);
    }

    return await _repository.UpdateAsync(note);
}

private string SanitizeHtml(string html)
{
    var sanitizer = new HtmlSanitizer();
    return sanitizer.Sanitize(html);
}
```

## Testing Strategy

### Unit Tests

```csharp
public class ConceptReferenceParserTests
{
    [Fact]
    public void Parse_SingleReference_ExtractsConceptName()
    {
        var parser = new ConceptReferenceParser();
        var content = "This is a note about [[Person]] concept.";

        var references = parser.Parse(content);

        Assert.Single(references);
        Assert.Equal("Person", references[0].ConceptName);
    }

    [Fact]
    public void Parse_MultipleReferences_ExtractsAll()
    {
        var parser = new ConceptReferenceParser();
        var content = "[[Person]] relates to [[Organization]] and [[Location]].";

        var references = parser.Parse(content);

        Assert.Equal(3, references.Count);
    }
}
```

### Integration Tests

```csharp
public class NoteServiceTests
{
    [Fact]
    public async Task ProcessConceptReferences_CreatesNewConcepts()
    {
        // Arrange
        var note = await CreateTestNoteAsync("This references [[NewConcept]]");

        // Act
        await _noteService.ProcessConceptReferencesAsync(note.Id, note.Content);

        // Assert
        var concept = await _conceptService.GetByNameAsync(note.Workspace.Ontology.Id, "NewConcept");
        Assert.NotNull(concept);
        Assert.True(concept.CreatedFromNoteReference);
    }
}
```

## Rollout Plan

### Phase 1: Foundation (Week 1-2)
1. Create database schema and migrations
2. Implement Workspace, Note, ConceptReference entities
3. Create repositories
4. Write unit tests

### Phase 2: Services (Week 3-4)
1. Implement WorkspaceService
2. Implement NoteService
3. Implement ConceptReferenceParser
4. Update ConceptService for auto-note creation
5. Write service integration tests

### Phase 3: Migration (Week 5)
1. Create migration scripts
2. Test migration on dev database
3. Create rollback scripts
4. Document migration procedure

### Phase 4: UI (Week 6-8)
1. Create Workspace components
2. Create Note editor components
3. Implement [[concept]] highlighting
4. Update navigation and routing
5. UI testing

### Phase 5: Testing & Polish (Week 9-10)
1. End-to-end testing
2. Performance testing
3. Security audit
4. Documentation
5. User acceptance testing

## Success Metrics

1. **Migration Success**: 100% of ontologies migrated to workspaces with zero data loss
2. **Performance**: Note editor loads in < 500ms, saves in < 200ms
3. **Concept Auto-Creation**: 95%+ accuracy in parsing [[]] references
4. **User Adoption**: 50%+ of users create at least one note in first week
5. **Stability**: Zero critical bugs in production after 2 weeks

## Next Steps

1. Review and approve architecture
2. Create database migration scripts
3. Begin implementation of core entities
4. Set up development environment
5. Create proof-of-concept for [[]] parsing
