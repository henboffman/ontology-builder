# Obsidian-Style Notes - Simplified Requirements
**Feature Version**: 1.1
**Created**: 2025-11-14
**Updated**: 2025-11-14 (Obsidian-style simplification)
**Status**: In Progress

## Obsidian Comparison

### What We're Taking from Obsidian

1. **[[Wiki-style Links]]** - Double bracket syntax for linking
2. **Backlinks** - Show which notes reference a concept/note
3. **Graph View Integration** - Notes and concepts in same graph
4. **Markdown-First** - Simple, fast markdown editing
5. **Flat File Structure** - No complex hierarchies initially
6. **Auto-Creation** - Create concept when you link to it
7. **Preview Mode** - View rendered markdown alongside edit

### What We're NOT Taking (Out of Scope for v1.1)

1. Tags (#tag syntax) - future enhancement
2. Dataview/queries - future enhancement
3. Plugins - not applicable
4. Canvas - not applicable
5. Templates - future enhancement
6. Daily notes - future enhancement
7. Folders/hierarchy - future enhancement

## Simplified Core Requirements

### 1. Workspace = Vault (Obsidian terminology)

```
Workspace
├── Ontology (graph of concepts)
└── Notes (collection of markdown files)
    ├── User Notes (created manually)
    └── Concept Notes (auto-created for each concept)
```

**Key Principle**: A workspace is like an Obsidian vault - it's a self-contained knowledge base with both structured (ontology) and unstructured (notes) knowledge.

### 2. Notes Are Simple Markdown Files

```markdown
# My Note Title

This is a note about [[Person]] and [[Organization]].

When I write [[NewConcept]], it auto-creates that concept in the ontology.

## Features
- Markdown formatting
- [[Wiki-style links]]
- Auto-creation of concepts
- Backlinks panel
```

**Behavior**:
- Pure markdown (no rich text editor in v1.1)
- Real-time preview
- [[concept]] creates concept if doesn't exist
- Clicking [[concept]] navigates to concept in graph view

### 3. Every Concept Gets a Note

**Obsidian Pattern**: In Obsidian, everything is a note. We adapt this:

- When you create a concept in the graph, it auto-creates `<ConceptName>.md`
- The concept note appears in the notes list
- You can add detailed information about the concept
- The note and concept are bidirectionally linked

**Example**:
```
Create concept "Person" in graph
  ↓
Auto-creates note "Person.md" with content:
---
# Person

*Auto-generated note for concept: Person*

## Definition
[The concept's definition from the ontology]

## Related Concepts
- [[Organization]]
- [[Location]]

## Notes
[User can add notes here]
```

### 4. Bidirectional Linking (Obsidian's Core Feature)

**In Obsidian**: When Note A links to Note B using [[Note B]], you see:
- In Note A: The link to Note B
- In Note B: A "backlinks" panel showing that Note A links here

**In Eidos**:
```
Note "Project Ideas" contains: [[Person]] and [[Organization]]

When viewing Concept "Person":
├── Shows in graph view
├── Shows concept note "Person.md"
└── Shows backlinks:
    - "Project Ideas" note references this concept
    - "Team Structure" note references this concept
```

### 5. Unified Graph View

**Obsidian Pattern**: Graph view shows all notes and their connections.

**Eidos Adaptation**:
```
Graph View shows:
├── Concepts (circular nodes - existing functionality)
├── Concept Notes (circular nodes, special styling)
├── User Notes (rectangular nodes, different color)
└── Links:
    ├── Relationships (existing - between concepts)
    └── References (new - from notes to concepts)
```

**Example**:
```
     ┌─────────────┐
     │   Person    │ (Concept - circular)
     └─────┬───────┘
           │
    ┌──────┴───────────┬─────────────┐
    │                  │             │
┌───▼────┐      ┌──────▼───────┐  ┌─▼─────────┐
│Person  │      │Project Ideas │  │   Team    │
│ .md    │      │    Note      │  │ Structure │
└────────┘      └──────────────┘  └───────────┘
(Concept Note)   (User Note)       (User Note)
```

## Simplified Data Model

### 1. Workspace (Unchanged from previous design)

```csharp
public class Workspace
{
    public int Id { get; set; }
    public string Name { get; set; } // Vault name
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Ontology? Ontology { get; set; }
    public ICollection<Note> Notes { get; set; }
}
```

### 2. Note (Simplified - Obsidian-style)

```csharp
public class Note
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }

    [Required]
    public string Title { get; set; } // File name without .md

    [Required]
    public string Content { get; set; } // Pure markdown

    // Concept linking
    public bool IsConceptNote { get; set; } = false;
    public int? LinkedConceptId { get; set; }

    // Metadata
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Relationships
    public Workspace Workspace { get; set; } = null!;
    public Concept? LinkedConcept { get; set; }
    public ICollection<NoteLink> OutgoingLinks { get; set; } // [[concept]] references
}
```

### 3. NoteLink (Replaces ConceptReference - simpler)

```csharp
public class NoteLink
{
    public int Id { get; set; }

    // Source note
    public int SourceNoteId { get; set; }
    public Note SourceNote { get; set; } = null!;

    // Target (can be concept or another note)
    public int? TargetConceptId { get; set; }
    public Concept? TargetConcept { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

## Obsidian-Style UI Components

### 1. Three-Pane Layout (Obsidian Standard)

```
┌────────────┬──────────────────┬─────────────┐
│            │                  │             │
│  File      │   Note Editor    │  Backlinks  │
│  Explorer  │   (Markdown)     │  Panel      │
│            │                  │             │
│  - Notes   │   # My Note      │  Linked     │
│    • Note1 │   [[Person]]     │  Mentions:  │
│    • Note2 │   [[Org]]        │  • Note3    │
│            │                  │  • Concept  │
│  - Concept │   [Preview ▼]    │             │
│    Notes   │                  │             │
│    • Person│                  │             │
│    • Org   │                  │             │
│            │                  │             │
└────────────┴──────────────────┴─────────────┘
```

### 2. Graph View (Enhanced)

```
Same as existing graph view, but adds:
- Rectangular nodes for user notes
- Different color for concept notes
- Lines showing note → concept references
- Click note to open in editor
- Click concept to see concept + concept note
```

### 3. Quick Switcher (Obsidian's Cmd+O)

```
Press Ctrl+K or Cmd+K:

┌────────────────────────────────────┐
│ 🔍 Quick Open...                   │
├────────────────────────────────────┤
│ 📄 Project Ideas (note)            │
│ 🔵 Person (concept)                │
│ 📄 Person.md (concept note)        │
│ 🔵 Organization (concept)          │
└────────────────────────────────────┘
```

## Simplified Implementation Plan

### Phase 1: Core Models (Week 1)

1. Create Workspace model
2. Create Note model (markdown only)
3. Create NoteLink model
4. Update Concept with note relationship
5. Generate migrations

### Phase 2: Link Parser (Week 1)

```csharp
public class WikiLinkParser
{
    private static readonly Regex WikiLinkRegex =
        new(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

    public List<string> ExtractLinks(string markdown)
    {
        return WikiLinkRegex.Matches(markdown)
            .Select(m => m.Groups[1].Value.Trim())
            .ToList();
    }
}
```

### Phase 3: Auto-Creation Logic (Week 2)

```csharp
public async Task SaveNoteAsync(Note note)
{
    // 1. Save note
    await _noteRepository.UpdateAsync(note);

    // 2. Parse [[links]]
    var links = _parser.ExtractLinks(note.Content);

    // 3. For each link:
    foreach (var link in links)
    {
        // Check if concept exists
        var concept = await _conceptService.GetByNameAsync(link);

        if (concept == null)
        {
            // Auto-create concept
            concept = await _conceptService.CreateAsync(new Concept
            {
                Name = link,
                Definition = $"Created from note: {note.Title}",
                Category = "Note-Created"
            });
        }

        // Create link
        await _noteLinkRepository.CreateAsync(new NoteLink
        {
            SourceNoteId = note.Id,
            TargetConceptId = concept.Id
        });
    }
}
```

### Phase 4: Markdown Editor UI (Week 3)

**Simple Split-Pane Editor**:

```razor
<div class="note-editor">
    <div class="editor-toolbar">
        <button @onclick="TogglePreview">
            @(showPreview ? "Edit" : "Preview")
        </button>
    </div>

    <div class="editor-content">
        @if (showPreview)
        {
            <!-- Markdown preview using Markdig -->
            <div class="markdown-preview">
                @((MarkupString)RenderMarkdown(note.Content))
            </div>
        }
        else
        {
            <!-- Simple textarea with [[]] highlighting -->
            <textarea @bind="note.Content"
                      @bind:event="oninput"
                      class="markdown-editor" />
        }
    </div>
</div>
```

### Phase 5: Backlinks Panel (Week 3)

```razor
<div class="backlinks-panel">
    <h3>Backlinks</h3>

    @if (backlinks.Any())
    {
        <ul>
            @foreach (var backlink in backlinks)
            {
                <li>
                    <a href="/note/@backlink.SourceNoteId">
                        @backlink.SourceNote.Title
                    </a>
                    <div class="backlink-context">
                        @GetContextSnippet(backlink)
                    </div>
                </li>
            }
        </ul>
    }
    else
    {
        <p class="text-muted">No backlinks yet</p>
    }
</div>
```

### Phase 6: Graph View Integration (Week 4)

**Update Cytoscape.js to show notes**:

```javascript
// Add note nodes to graph
notes.forEach(note => {
    cy.add({
        group: 'nodes',
        data: {
            id: 'note-' + note.id,
            label: note.title,
            type: note.isConceptNote ? 'concept-note' : 'user-note'
        },
        classes: note.isConceptNote ? 'concept-note-node' : 'user-note-node'
    });
});

// Add note → concept links
noteLinks.forEach(link => {
    cy.add({
        group: 'edges',
        data: {
            id: 'link-' + link.id,
            source: 'note-' + link.sourceNoteId,
            target: 'concept-' + link.targetConceptId
        },
        classes: 'note-link'
    });
});

// Styling
cy.style()
    .selector('.user-note-node')
    .style({
        'shape': 'rectangle',
        'background-color': '#9b59b6',
        'width': 100,
        'height': 50
    })
    .selector('.concept-note-node')
    .style({
        'shape': 'round-rectangle',
        'background-color': '#3498db',
        'border-width': 2,
        'border-color': '#2ecc71'
    });
```

## User Experience Flow

### Creating a Note

1. Click "New Note" button
2. Enter title: "Project Ideas"
3. Start typing markdown:
   ```markdown
   # Project Ideas

   We should explore [[Machine Learning]] for our product.
   This relates to [[AI]] and [[Data Science]].
   ```
4. As you type `[[Machine Learning]]`:
   - Autocomplete suggests existing concepts
   - If you press Enter, it auto-creates "Machine Learning" concept
   - Concept appears in graph view immediately
5. Click "Preview" to see rendered markdown
6. Backlinks panel shows other notes/concepts linking here

### Creating a Concept

1. Create concept "Person" in graph view
2. System auto-creates "Person.md" note with template
3. You can edit "Person.md" to add detailed notes about the concept
4. Any note that mentions [[Person]] shows up in backlinks

### Navigating

1. **From Note to Concept**: Click [[Person]] link → opens concept in graph
2. **From Concept to Note**: View concept → see concept note panel
3. **From Note to Note**: (Future) [[Another Note]] syntax
4. **Quick Switcher**: Ctrl+K → type → open anything

## Migration from Previous Design

### What Changed

| Previous Design | Obsidian-Style | Reason |
|----------------|----------------|---------|
| Rich Text support | Markdown only | Simpler, Obsidian-style |
| ConceptReference entity | NoteLink entity | More flexible for future note→note links |
| Complex UI | Three-pane layout | Familiar to Obsidian users |
| Separate views | Unified graph | Better knowledge visualization |

### What Stayed the Same

- Workspace as top-level entity
- Auto-create concepts from [[]] syntax
- Concept notes (one per concept)
- Markdown rendering with Markdig
- Permission system

## Example Scenarios

### Scenario 1: Research Notes

```markdown
# AI Research Notes

Reading about [[Machine Learning]] today.

Key concepts:
- [[Neural Networks]]
- [[Deep Learning]]
- [[Supervised Learning]]

All of these relate to [[AI]] which is part of [[Computer Science]].
```

**Result**: Creates 6 concepts automatically, links them all in graph view.

### Scenario 2: Meeting Notes

```markdown
# Team Meeting - 2025-11-14

Attendees: [[Alice]], [[Bob]], [[Carol]]
Topic: [[Project Phoenix]]

Discussion:
- Need to research [[React]] vs [[Vue]]
- [[Alice]] will lead the frontend team
- Next meeting: review [[Architecture Design]]
```

**Result**: Creates person concepts, project concepts, tech concepts all linked together.

### Scenario 3: Concept Deep Dive

Click on "Machine Learning" concept → See:
- Concept in graph view
- Concept note "Machine Learning.md"
- Backlinks panel showing:
  - "AI Research Notes" mentions this
  - "Project Ideas" mentions this
  - "Team Meeting" mentions this

## Success Metrics

1. **Ease of Use**: Users can create their first note in < 30 seconds
2. **Link Creation**: 90%+ of [[]] links successfully create concepts
3. **Discoverability**: Users find backlinks feature within first session
4. **Performance**: Graph renders with 100+ notes + concepts in < 2 seconds
5. **Adoption**: 70%+ of users create at least 5 notes in first week

## Next Steps

1. ✅ Document Obsidian-style requirements
2. Implement simplified data models
3. Create WikiLinkParser
4. Build three-pane UI layout
5. Integrate with graph view
6. Add backlinks functionality
7. Create quick switcher
8. Migration script for workspaces
9. User testing with Obsidian users

This Obsidian-inspired approach is **much simpler** than the original design while providing a **powerful and familiar** experience for knowledge management users.
