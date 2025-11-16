# Workspaces and Notes User Guide

## Overview

Workspaces in Eidos provide an integrated environment for managing both your ontology and related knowledge notes. Each workspace combines a visual ontology graph with a flexible note-taking system, allowing you to capture both structured knowledge (concepts and relationships) and unstructured thoughts (markdown notes).

## Getting Started with Workspaces

### Creating a Workspace

1. Navigate to your dashboard
2. Click "Create New Workspace"
3. Enter a name and optional description
4. Click "Create"

When you create a workspace, Eidos automatically creates an associated ontology for you. This ontology will store all the concepts and relationships you define.

### Understanding the Workspace Layout

Workspaces use a three-pane layout designed for efficient knowledge work:

**Left Pane - Note Explorer**
- Browse all notes in your workspace
- Search notes by title
- Quick-create new notes
- Toggle between condensed and comfortable view modes

**Center Pane - Content Editor**
- Edit markdown notes
- Switch between edit and preview modes
- Auto-saves as you type

**Right Pane - Context Panel**
- View backlinks (notes that reference the current concept)
- See related concepts from your ontology
- Navigate concept relationships

## Working with Notes

### Creating Notes

There are two types of notes in Eidos:

**User Notes** - Free-form markdown notes you create manually
- Click the "+" button in the note explorer
- Enter your note title
- Start writing in markdown

**Concept Notes** - Automatically created for each concept in your ontology
- Created when you reference a concept using `[[brackets]]`
- Also created when adding concepts via the graph view
- Marked with a "Concept" badge

### Writing in Markdown

Notes support full markdown syntax including:
- Headers (`#`, `##`, `###`)
- **Bold** and *italic* text
- Lists (bulleted and numbered)
- Code blocks with syntax highlighting
- Links and images
- Tables

### Wiki-Style Concept Links

The power of Eidos workspaces comes from linking notes to concepts using double-bracket syntax:

```markdown
[[Concept Name]]
```

#### How It Works

1. **Auto-Creation**: When you type `[[New Concept]]`, Eidos automatically:
   - Creates the concept in your ontology
   - Creates a concept note for it
   - Links your current note to that concept

2. **Display Text**: Use a pipe character for custom display text:
   ```markdown
   [[Person|John Doe]]
   ```
   This creates a link to the "Person" concept but displays "John Doe" in your note.

3. **Backlinks**: Every concept note shows which other notes reference it, creating a knowledge graph

#### Example

```markdown
# Project Planning

This project involves [[Software Development]] and [[Project Management]].

The team includes several [[Person|developers]] and a [[Person|project manager]].

Key concepts:
- [[Agile Methodology]]
- [[Sprint Planning]]
- [[Code Review]]
```

This automatically creates 5 concepts in your ontology and links them all to this note.

### Searching and Navigation

**Search Notes**
- Use the search box in the note explorer
- Searches note titles in real-time
- Press Enter to focus on first result

**Quick Switcher**
- Press `Cmd+K` (Mac) or `Ctrl+K` (Windows/Linux)
- Type to filter notes
- Press Enter to open
- Escape to close

**Navigate Between Views**
- Click the graph icon to view your ontology in graph mode
- Click concept names in the right panel to jump to their notes
- Use the "Open Note" button in graph view concept details

## View Modes

### Edit vs Preview

**Edit Mode** (pencil icon)
- Write and edit markdown
- See raw syntax
- Auto-saves every few seconds

**Preview Mode** (eye icon)
- See formatted output
- Click concept links to navigate
- Better for reading

Toggle between modes using the button in the toolbar or keyboard shortcut `Cmd+E`.

### Condensed vs Comfortable

**Condensed View**
- Compact note list
- Shows more notes at once
- Just titles, no metadata
- Better for large workspaces

**Comfortable View**
- Spacious layout
- Shows note type badges
- Display metadata
- Easier to scan

Toggle using the list icon in the note explorer header.

## Integrating Notes and Ontology

### From Notes to Graph

Your wiki-style links automatically populate your ontology graph:

1. Write notes with `[[concept links]]`
2. Switch to graph view (graph icon in toolbar)
3. See all your concepts visualized
4. Add relationships between concepts
5. Define properties and restrictions

### From Graph to Notes

When working in the ontology graph view:

1. Select any concept
2. Click "Open Note" in the details panel
3. Jump to that concept's note
4. See all backlinks (notes that reference this concept)

### Backlinks Panel

The right pane shows backlinks for the current note/concept:

- **For concept notes**: All user notes that reference this concept
- **Context snippets**: See surrounding text where the concept is mentioned
- **Quick navigation**: Click any backlink to jump to that note

This creates a bi-directional knowledge graph where you can navigate by association.

## Organizing Your Workspace

### Best Practices

1. **Start with notes**: Write freely using concept links
2. **Refine in graph**: Add relationships and structure later
3. **Use descriptive names**: Make concept names clear and searchable
4. **Link liberally**: Don't worry about over-linking
5. **Review backlinks**: Discover unexpected connections

### Naming Conventions

- **Concepts**: Use title case (e.g., "Project Management", "Software Development")
- **Notes**: Use descriptive, scannable titles
- **Display text**: Use it to maintain readable prose

### Managing Large Workspaces

- Use condensed view to see more notes
- Search frequently with `Cmd+K`
- Review note counts in workspace metadata
- Consider splitting very large workspaces by domain

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Cmd/Ctrl + K` | Open quick switcher |
| `Cmd/Ctrl + E` | Toggle edit/preview |
| `Cmd/Ctrl + S` | Save note (auto-saves anyway) |
| `/` | Focus search |
| `Escape` | Close dialogs/switcher |

## Tips and Tricks

### Rapid Concept Creation

Type several concepts quickly:
```markdown
[[Person]] works with [[Team]] on [[Project]] using [[Technology]].
```
All four concepts created instantly.

### Explore Connections

1. Open a concept note
2. Check the backlinks panel
3. Click through to see how concepts relate
4. Discover patterns in your knowledge

### Bi-directional Workflow

- **Bottom-up**: Write notes → extract concepts → organize in graph
- **Top-down**: Design ontology → create concept notes → link together

Use whichever feels natural for your task.

### Combine with Graph Features

- Define formal relationships in graph view
- Add properties and restrictions to concepts
- Export ontology to standard formats (OWL, TTL)
- Import existing ontologies as starting points

## Privacy and Sharing

Workspaces inherit visibility settings from their ontology:

- **Private**: Only you can access
- **Group**: Shared with specific user groups
- **Public**: Anyone can view (optional edit permissions)

Change visibility in the ontology settings (gear icon in graph view).

## Troubleshooting

**"View in Graph" button not appearing**
- Ensure your workspace has an associated ontology
- Try refreshing the page
- Check workspace settings

**Concept not auto-creating**
- Check bracket syntax: `[[ConceptName]]`
- Ensure concept name doesn't contain invalid characters ( `[`, `]`, `|`, newlines)
- Try refreshing and typing again

**Backlinks not showing**
- Save your note first
- Links are extracted when you save
- Check that you're using correct bracket syntax

**Search not finding notes**
- Search only looks at note titles, not content
- Use `Cmd+K` quick switcher for faster search
- Check spelling

## Next Steps

- Explore the [Ontology Graph View](./ONTOLOGY_GRAPH_VIEW.md)
- Learn about [Collaboration Features](./COLLABORATION.md)
- See [Keyboard Shortcuts Reference](./KEYBOARD_SHORTCUTS.md)

---

*Last updated: November 15, 2025*
