# Implementation Plan - Notes Tagging & Import

## Overview
This document outlines the step-by-step implementation plan for the notes tagging, import/export, auto-save, and workspace navigation features.

## Phase 1: Core Tag System (Days 1-3)

### Day 1: Database & Models

**Tasks**:
1. Create migration for Tags table
2. Create migration for NoteTagAssignments table
3. Add import metadata fields to Notes table
4. Create Tag.cs model
5. Create NoteTagAssignment.cs model
6. Update OntologyDbContext with new DbSets
7. Run migrations

**Files to Create/Modify**:
- `Migrations/YYYYMMDD_AddTagsSystem.cs`
- `Models/Tag.cs`
- `Models/NoteTagAssignment.cs`
- `Data/OntologyDbContext.cs`

**Testing**:
- Verify migrations apply cleanly
- Test FK constraints
- Verify cascade deletes work correctly

### Day 2: Repository & Service Layer

**Tasks**:
1. Create ITagRepository and TagRepository
2. Create INoteTagAssignmentRepository and NoteTagAssignmentRepository
3. Create ITagService interface
4. Implement TagService
5. Add service registration in Program.cs
6. Write unit tests for TagService

**Files to Create**:
- `Data/Repositories/TagRepository.cs`
- `Data/Repositories/Interfaces/ITagRepository.cs`
- `Data/Repositories/NoteTagAssignmentRepository.cs`
- `Data/Repositories/Interfaces/INoteTagAssignmentRepository.cs`
- `Services/TagService.cs`
- `Services/Interfaces/ITagService.cs`
- `Eidos.Tests/Unit/Services/TagServiceTests.cs`

**Key Methods**:
```csharp
// TagService methods to implement
- CreateTagAsync
- GetWorkspaceTagsAsync
- AssignTagToNoteAsync
- UnassignTagFromNoteAsync
- GetNoteTagsAsync
- GetNotesByTagAsync
- GetTagCountsAsync
- DeleteTagAsync
```

### Day 3: Basic Tag UI

**Tasks**:
1. Create TagBadge component
2. Create TagSelector component
3. Update WorkspaceView to show tags
4. Add tag assignment UI to note editor
5. Style tag badges with colors

**Files to Create**:
- `Components/Notes/TagBadge.razor`
- `Components/Notes/TagBadge.razor.css`
- `Components/Notes/TagSelector.razor`
- `Components/Notes/TagSelector.razor.css`
- `wwwroot/css/components/tag-badge.css`

**TagBadge Component**:
```razor
<span class="tag-badge" style="background-color: @Tag.Color">
    @Tag.Name
    @if (Removable)
    {
        <button @onclick="OnRemove" class="tag-remove">×</button>
    }
</span>
```

**Acceptance Criteria**:
- [ ] Users can create tags
- [ ] Users can assign tags to notes
- [ ] Tags display as colored badges
- [ ] Users can remove tags from notes

---

## Phase 2: Virtual Folders (Days 4-5)

### Day 4: Sidebar Navigation

**Tasks**:
1. Create NotesSidebar component
2. Implement tag filtering logic
3. Add "All Notes" default view
4. Display tag counts
5. Style to match ontology dashboard

**Files to Create**:
- `Components/Notes/NotesSidebar.razor`
- `Components/Notes/NotesSidebar.razor.css`
- `wwwroot/css/components/notes-sidebar.css`

**NotesSidebar Structure**:
```
┌─────────────────────┐
│ 📝 All Notes  (45) │
├─────────────────────┤
│ TAGS                │
│ 🏷️ project    (12) │
│ 🏷️ research   (8)  │
│ 🏷️ important  (5)  │
│ 🏷️ draft      (20) │
└─────────────────────┘
```

### Day 5: Tag Filtering & State Management

**Tasks**:
1. Create NotesState service for state management
2. Implement tag click to filter notes
3. Update note list to show filtered results
4. Add search within filtered notes
5. Breadcrumb navigation (All Notes > Tag Name)

**Files to Create**:
- `Services/NotesState.cs`
- Update `Components/Pages/WorkspaceView.razor`

**Acceptance Criteria**:
- [ ] Clicking tag shows only notes with that tag
- [ ] Tag counts are accurate
- [ ] "All Notes" shows all workspace notes
- [ ] Search works within filtered view

---

## Phase 3: Auto-Save (Days 6-7)

### Day 6: Auto-Save Service

**Tasks**:
1. Create IAutoSaveService interface
2. Implement AutoSaveService with debouncing
3. Add auto-save status tracking
4. Handle save queue and conflicts
5. Write unit tests for debouncing

**Files to Create**:
- `Services/AutoSaveService.cs`
- `Services/Interfaces/IAutoSaveService.cs`
- `Models/AutoSaveStatus.cs`
- `Eidos.Tests/Unit/Services/AutoSaveServiceTests.cs`

**Debounce Logic**:
```csharp
private Dictionary<int, Timer> _timers = new();
private const int DEBOUNCE_MS = 2000;

public Task QueueSaveAsync(int noteId, string content, string userId)
{
    if (_timers.ContainsKey(noteId))
    {
        _timers[noteId].Dispose();
    }

    var timer = new Timer(async _ =>
    {
        await SaveNoteAsync(noteId, content, userId);
        _timers.Remove(noteId);
    }, null, DEBOUNCE_MS, Timeout.Infinite);

    _timers[noteId] = timer;
    return Task.CompletedTask;
}
```

### Day 7: Auto-Save UI Integration

**Tasks**:
1. Create AutoSaveIndicator component
2. Update markdown editor to trigger auto-save
3. Add JavaScript interop for input events
4. Display save status in UI
5. Handle navigation with unsaved changes

**Files to Create**:
- `Components/Notes/AutoSaveIndicator.razor`
- `wwwroot/js/autoSave.js`

**AutoSaveIndicator**:
```razor
<div class="autosave-indicator @GetStatusClass()">
    @switch (Status)
    {
        case AutoSaveStatus.Saving:
            <i class="bi bi-cloud-arrow-up"></i> <span>Saving...</span>
            break;
        case AutoSaveStatus.Saved:
            <i class="bi bi-cloud-check"></i> <span>Saved</span>
            break;
        case AutoSaveStatus.Error:
            <i class="bi bi-exclamation-triangle"></i> <span>Save failed</span>
            break;
    }
</div>
```

**Acceptance Criteria**:
- [ ] Notes auto-save 2 seconds after last edit
- [ ] Save indicator shows current status
- [ ] No save when user is actively typing
- [ ] Force save on navigation away

---

## Phase 4: Markdown Import (Days 8-10)

### Day 8: Wiki-Link Parser

**Tasks**:
1. Create WikiLinkParserService
2. Implement regex for extracting [[links]]
3. Handle [[Concept|Display Text]] format
4. Write comprehensive parser tests
5. Support escaping \[\[not a link\]\]

**Files to Create**:
- `Services/WikiLinkParserService.cs`
- `Services/Interfaces/IWikiLinkParserService.cs`
- `Models/WikiLink.cs`
- `Eidos.Tests/Unit/Services/WikiLinkParserTests.cs`

**Regex Patterns**:
```csharp
// Basic: [[Concept]]
private const string BASIC_PATTERN = @"\[\[([^\]|]+)\]\]";

// With display text: [[Concept|Display]]
private const string DISPLAY_PATTERN = @"\[\[([^\]|]+)\|([^\]]+)\]\]";

// Combined
private const string WIKI_LINK_PATTERN = @"\[\[([^\]|]+)(?:\|([^\]]+))?\]\]";
```

### Day 9: Markdown Import Service

**Tasks**:
1. Create MarkdownImportService
2. Install YamlDotNet NuGet package
3. Implement frontmatter parsing
4. Implement file content processing
5. Create concepts from wiki-links
6. Handle import errors gracefully

**Files to Create**:
- `Services/MarkdownImportService.cs`
- `Services/Interfaces/IMarkdownImportService.cs`
- `Models/MarkdownImportResult.cs`
- `Models/MarkdownImportOptions.cs`

**Frontmatter Parsing**:
```csharp
public Dictionary<string, string>? ParseFrontmatter(string content)
{
    var match = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);
    if (!match.Success) return null;

    var yaml = match.Groups[1].Value;
    var deserializer = new DeserializerBuilder().Build();
    return deserializer.Deserialize<Dictionary<string, string>>(yaml);
}
```

### Day 10: Import Dialog UI

**Tasks**:
1. Create MarkdownImportDialog component
2. Implement file picker with JS interop
3. Add drag-and-drop support
4. Show import progress
5. Display import results and errors

**Files to Create**:
- `Components/Notes/MarkdownImportDialog.razor`
- `Components/Notes/MarkdownImportDialog.razor.css`
- `Components/Notes/ImportProgressPanel.razor`
- `wwwroot/js/markdownImport.js`

**JavaScript File Picker**:
```javascript
export async function openFilePicker(multiple) {
    return new Promise((resolve) => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.md,.markdown';
        input.multiple = multiple;

        input.onchange = async (e) => {
            const files = [];
            for (const file of e.target.files) {
                files.push({
                    name: file.name,
                    content: await file.text()
                });
            }
            resolve(files);
        };

        input.click();
    });
}
```

**Acceptance Criteria**:
- [ ] Import single markdown file
- [ ] Import multiple files (batch)
- [ ] Parse YAML frontmatter
- [ ] Extract and create concepts from [[links]]
- [ ] Show progress for batch imports
- [ ] Display error messages for failed imports

---

## Phase 5: Markdown Export (Days 11-12)

### Day 11: Export Service

**Tasks**:
1. Create MarkdownExportService
2. Implement frontmatter generation
3. Implement single note export
4. Implement batch export with ZIP
5. Sanitize file names

**Files to Create**:
- `Services/MarkdownExportService.cs`
- `Services/Interfaces/IMarkdownExportService.cs`
- `Eidos.Tests/Unit/Services/MarkdownExportServiceTests.cs`

**Frontmatter Generation**:
```csharp
public string GenerateFrontmatter(Note note, List<Tag> tags, List<Concept> linkedConcepts)
{
    var sb = new StringBuilder();
    sb.AppendLine("---");
    sb.AppendLine($"title: {note.Title}");

    if (tags.Any())
    {
        sb.AppendLine($"tags: [{string.Join(", ", tags.Select(t => t.Name))}]");
    }

    sb.AppendLine($"created: {note.CreatedAt:O}");
    sb.AppendLine($"modified: {note.UpdatedAt:O}");

    if (linkedConcepts.Any())
    {
        sb.AppendLine($"linked_concepts: [{string.Join(", ", linkedConcepts.Select(c => c.Name))}]");
    }

    sb.AppendLine("---");
    sb.AppendLine();

    return sb.ToString();
}
```

### Day 12: Export Dialog UI

**Tasks**:
1. Create MarkdownExportDialog component
2. Add note selection UI
3. Add export options (frontmatter, format)
4. Implement download functionality
5. Create ZIP for batch export

**Files to Create**:
- `Components/Notes/MarkdownExportDialog.razor`
- `Components/Notes/MarkdownExportDialog.razor.css`

**Export Options**:
- ☑ Include frontmatter
- ☑ Preserve wiki-links
- ☑ Export as ZIP (for multiple files)

**Acceptance Criteria**:
- [ ] Export single note as .md file
- [ ] Export multiple notes as ZIP
- [ ] Frontmatter includes all metadata
- [ ] Wiki-links are preserved
- [ ] File names are sanitized

---

## Phase 6: Bracket Wrapping & Navigation (Days 13-14)

### Day 13: Bracket Wrapping Feature

**Tasks**:
1. Enhance wikiLinkEditor.js
2. Implement selection + [ key handling
3. Wrap selected text in [[]]
4. Support multiple wrapping (press [ again)
5. Add keyboard shortcut documentation

**Files to Modify**:
- `wwwroot/js/wikiLinkEditor.js`
- `Components/Pages/WorkspaceView.razor`

**Implementation**:
```javascript
function handleBracketWrap(event) {
    if (event.key === '[') {
        const selection = window.getSelection();

        if (selection.toString().length > 0) {
            event.preventDefault();
            const text = selection.toString();

            // Wrap in brackets
            const wrapped = '[[' + text + ']]';
            document.execCommand('insertText', false, wrapped);
        }
    }
}
```

### Day 14: Workspace Navigation Dialog

**Tasks**:
1. Create WorkspaceNavigationDialog component
2. Add icons for Ontology and Notes
3. Implement "Remember my choice" preference
4. Update Home.razor to show dialog
5. Add routing for /workspace/{id}/notes

**Files to Create**:
- `Components/Shared/WorkspaceNavigationDialog.razor`
- `Components/Shared/WorkspaceNavigationDialog.razor.css`

**Files to Modify**:
- `Components/Pages/Home.razor`
- `Program.cs` (routing)

**Dialog Layout**:
```
┌─────────────────────────────────┐
│ Open "My Workspace"             │
│                                 │
│  [🌐]          [📝]            │
│ Ontology       Notes            │
│                                 │
│ ☐ Remember my choice            │
│                                 │
│         [Cancel]                │
└─────────────────────────────────┘
```

**Acceptance Criteria**:
- [ ] Dialog appears when clicking workspace
- [ ] Two buttons: Ontology and Notes
- [ ] "Remember choice" saves preference
- [ ] Can dismiss with Esc or Cancel
- [ ] Styling matches existing dialogs

---

## Phase 7: SKOS Integration (Optional - Days 15-16)

### Day 15: Tag Ontology Service

**Tasks**:
1. Research SKOS best practices
2. Create TagOntologyIntegrationService
3. Implement tag → SKOS Concept sync
4. Create "Note Classification Scheme"
5. Add user preference for auto-sync

**Files to Create**:
- `Services/TagOntologyIntegrationService.cs`
- `Services/Interfaces/ITagOntologyIntegrationService.cs`

**SKOS Structure**:
```
ConceptScheme: "Note Classification Scheme"
└── Concepts (Tags)
    ├── Concept: "project" (skos:prefLabel)
    ├── Concept: "research" (skos:prefLabel)
    └── Concept: "important" (skos:prefLabel)
```

### Day 16: SKOS UI Integration

**Tasks**:
1. Add "Sync tags to ontology" preference
2. Create tag promotion UI
3. Show tag-concept linkage in ontology
4. Add visual indicator for synced tags

**Files to Modify**:
- `Components/Settings/PreferencesSettings.razor`
- `Components/Notes/TagBadge.razor`

**Acceptance Criteria**:
- [ ] Tags can sync to ontology as SKOS Concepts
- [ ] User can enable/disable sync
- [ ] Tags can be promoted to full concepts
- [ ] Synced tags show indicator

---

## Phase 8: Testing & Polish (Days 17-18)

### Day 17: Comprehensive Testing

**Tasks**:
1. Write integration tests for import/export
2. Test auto-save with concurrent edits
3. Test tag filtering with large datasets
4. Performance testing (1000+ notes)
5. Edge case testing (special characters, empty notes)

**Test Files to Create**:
- `Eidos.Tests/Integration/MarkdownImportExportTests.cs`
- `Eidos.Tests/Integration/TagFilteringTests.cs`
- `Eidos.Tests/Integration/AutoSaveTests.cs`

### Day 18: UI Polish & Documentation

**Tasks**:
1. Improve error messages
2. Add loading states
3. Improve mobile responsiveness
4. Add tooltips and help text
5. Update user documentation

**Documentation to Update**:
- `docs/user-guides/WORKSPACES_AND_NOTES.md`
- `docs/features/NOTES_TAGGING.md` (new)
- `docs/features/MARKDOWN_IMPORT_EXPORT.md` (new)
- Release notes

---

## Testing Checklist

### Unit Tests
- [ ] TagService CRUD operations
- [ ] WikiLinkParser extracts links correctly
- [ ] WikiLinkParser handles [[Concept|Display]]
- [ ] WikiLinkParser handles escaped brackets
- [ ] Frontmatter parsing with various YAML formats
- [ ] AutoSave debouncing works correctly
- [ ] File name sanitization

### Integration Tests
- [ ] Create tag and assign to note
- [ ] Filter notes by tag
- [ ] Import markdown with frontmatter
- [ ] Import markdown with wiki-links creates concepts
- [ ] Export note and verify frontmatter
- [ ] Export/import roundtrip preserves content
- [ ] Auto-save persists changes
- [ ] Concurrent note edits don't conflict

### E2E Tests
- [ ] Full import workflow (file picker → parse → save)
- [ ] Tag creation and filtering
- [ ] Bracket wrapping shortcut
- [ ] Workspace navigation dialog
- [ ] Batch export to ZIP

### Performance Tests
- [ ] Import 100 markdown files in < 30 seconds
- [ ] Tag filtering with 1000 notes in < 100ms
- [ ] Auto-save completes in < 500ms
- [ ] Search across 1000 notes in < 200ms

---

## Rollback Plan

If critical issues arise:

### Phase Rollback Points
1. After Phase 1: Can use tags without virtual folders
2. After Phase 2: Can use virtual folders without auto-save
3. After Phase 3: Can use auto-save independently
4. After Phase 4: Can skip export if import works
5. After Phase 5: Can skip navigation dialog
6. After Phase 6: Can skip SKOS integration

### Database Rollback
- Keep migrations reversible
- Test migration down() before deployment
- Backup database before each phase
- Keep previous schema for 1 week

---

## Deployment Checklist

### Pre-Deployment
- [ ] All tests passing
- [ ] Code review completed
- [ ] Database backup created
- [ ] Migration scripts reviewed
- [ ] Performance benchmarks met

### Deployment Steps
1. Backup production database
2. Deploy database migrations
3. Deploy application code
4. Verify health checks
5. Smoke test core functionality
6. Monitor error logs for 1 hour

### Post-Deployment
- [ ] Verify auto-save working
- [ ] Test import/export
- [ ] Check tag filtering performance
- [ ] Monitor database query performance
- [ ] Review user feedback

---

## Dependencies & Prerequisites

### NuGet Packages
```bash
dotnet add package YamlDotNet --version 13.7.1
```

### Database
- SQL Server 2019+ or Azure SQL
- EF Core 9.0 migrations

### Browser Compatibility
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

---

## Open Questions for Review

Before starting implementation, please review and decide:

1. **Tag Colors**: Auto-generate from palette or let users choose?
2. **Tag Limits**: Max tags per note? (Suggest: 10)
3. **Tag Hierarchy**: Support nested tags? (e.g., project/frontend)
4. **Wiki-Link Ambiguity**: Disambiguation UI or auto-link to first match?
5. **Concurrent Edits**: Last-write-wins or conflict resolution UI?
6. **Import Conflicts**: How to handle existing note with same title?
7. **Export Format**: Support other formats (Obsidian, Logseq)?
8. **SKOS Integration**: Priority or optional for later?

---

## Success Metrics

After completion, we should achieve:

- ✅ Users can import their Obsidian/markdown notes
- ✅ Notes auto-save without user intervention
- ✅ Tags provide intuitive organization
- ✅ Wiki-links connect notes to ontology
- ✅ Export preserves all content and metadata
- ✅ Performance meets benchmarks
- ✅ Zero data loss incidents
- ✅ Positive user feedback on UX

---

## Next Steps

1. Review this implementation plan
2. Answer open questions above
3. Create tasks in project board
4. Begin Phase 1: Core Tag System
5. Schedule daily standup check-ins
