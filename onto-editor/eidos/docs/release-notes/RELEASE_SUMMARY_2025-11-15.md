# Release Summary - November 15, 2025

## Major Feature: Integrated Workspaces and Notes

We're excited to announce a major new feature: **Workspaces with Integrated Note-Taking**. This release transforms Eidos from a pure ontology editor into a complete knowledge management system.

### What's New

#### Workspaces

Workspaces provide an integrated environment that combines your ontology with a markdown-based note system. Each workspace contains:

- One ontology for structured knowledge (concepts and relationships)
- Unlimited markdown notes for unstructured thoughts and documentation
- Automatic bi-directional linking between notes and concepts

#### Three-Pane Interface

The new workspace view features an intuitive layout:

- **Left**: Note explorer with search and quick-create
- **Center**: Full-featured markdown editor with live preview
- **Right**: Backlinks panel showing concept connections

#### Wiki-Style Concept Links

Use double-bracket syntax to create powerful knowledge connections:

```markdown
[[Concept Name]] - Creates and links to a concept
[[Person|John Doe]] - Custom display text
```

When you reference a concept in your notes:
1. The concept is automatically created in your ontology
2. A concept note is generated
3. The link is tracked for backlinks

#### Automatic Concept Creation

No more switching between views to add concepts. Simply write naturally:

```markdown
This project uses [[Agile Methodology]] and [[Sprint Planning]].
The team includes [[Developers]] and a [[Project Manager]].
```

Four concepts created instantly, all linked to your note.

#### Backlinks

Every concept note shows which other notes reference it, creating a dynamic knowledge graph. Navigate your knowledge by association and discover unexpected connections.

#### Seamless Graph Integration

Switch between workspace and graph views effortlessly:

- **View in Graph**: Jump from notes to visual ontology
- **Open Note**: Jump from graph concepts to their notes
- **Related Concepts**: See ontology relationships in note context

#### View Modes

Customize your workspace for your workflow:

- **Edit/Preview**: Toggle between writing and reading
- **Condensed/Comfortable**: Compact or spacious note lists
- **Auto-save**: Never lose your work

#### Quick Switcher

Press `Cmd/Ctrl + K` to instantly search and switch between notes. Fast, keyboard-driven navigation.

### Use Cases

**Research and Writing**
- Capture ideas as you research
- Link concepts as you discover them
- Build your ontology organically

**Documentation**
- Create concept notes with formal definitions
- Add usage examples and context
- Link related documentation

**Knowledge Management**
- Centralize domain knowledge
- Track concept relationships
- Discover knowledge gaps through backlinks

**Collaborative Ontology Building**
- Share workspaces with teams
- Comment on concepts
- Track changes and contributions

### Technical Highlights

- **Performance Optimized**: Separated content storage for fast note lists
- **Wiki-Link Parser**: Robust regex-based parser with 45+ unit tests
- **Legacy Support**: Existing ontologies automatically get workspaces
- **Database Schema**: Clean 1:1 workspace-ontology relationship
- **Auto-Save**: Background saves with visual feedback

### Migration

Existing users: Your ontologies will automatically create workspaces when you access them. All existing data is preserved.

### Breaking Changes

None. This is a purely additive feature.

## Bug Fixes

### Critical

- **Fixed workspace-ontology linking**: Resolved issue where the workspace ID wasn't persisting when creating workspaces for legacy ontologies
- **Database cleanup**: Removed duplicate workspaces created during development
- **Navigation fix**: "View in Graph" button now appears correctly in workspace view

### Minor

- Improved error handling in note creation
- Fixed markdown preview rendering edge cases
- Corrected search behavior when switching workspaces

## Improvements

### User Interface

- **Concept/Note Badges**: Visual indicators distinguish concept notes from user notes (replaced generic blue dots)
- **Responsive Layout**: Works on mobile and tablet devices
- **Loading States**: Clear feedback during operations
- **Empty States**: Helpful guidance when workspace is empty

### Performance

- **Query Optimization**: Efficient loading with `AsNoTracking()` and `Include()`
- **Indexed Foreign Keys**: Fast lookups on WorkspaceId and LinkedConceptId
- **Content Separation**: Notes and content in separate tables for better list performance

### Developer Experience

- **Comprehensive Tests**: 45+ unit tests for WikiLinkParser
- **Clean Architecture**: Repository and service patterns
- **Documentation**: Full user guide and technical docs
- **Code Comments**: Extensive XML documentation

## API Changes

### New Endpoints

**Workspaces**
- `GET /api/workspaces` - List user workspaces
- `GET /api/workspaces/{id}` - Get workspace details
- `POST /api/workspaces` - Create workspace
- `PUT /api/workspaces/{id}` - Update workspace
- `DELETE /api/workspaces/{id}` - Delete workspace

**Notes**
- `GET /api/workspaces/{id}/notes` - List workspace notes
- `GET /api/notes/{id}` - Get note with content
- `POST /api/notes` - Create note
- `PUT /api/notes/{id}` - Update note
- `DELETE /api/notes/{id}` - Delete note
- `GET /api/notes/{id}/backlinks` - Get backlinks

### Modified

- Ontology model now includes optional `WorkspaceId` and `Workspace` navigation property
- Concept model includes `Notes` collection for backlinks

## Database Schema

### New Tables

- `Workspaces` - Workspace metadata
- `Notes` - Note metadata
- `NoteContent` - Markdown content (separate for performance)
- `NoteLinks` - Extracted concept links with position and context

### Migrations

- `20251114005839_AddWorkspaceAndNotesSchema` - Initial workspace schema
- Added indexes on `WorkspaceId`, `LinkedConceptId`, and `SourceNoteId`

## Known Issues

- Tests for some existing services need parameter updates (ConceptService, RelationshipService constructors)
- Full-text search in note content not yet implemented (title search only)
- No version history for note content (table designed for it, not implemented)

## Deprecations

None

## Documentation

### New

- `/docs/user-guides/WORKSPACES_AND_NOTES.md` - Comprehensive user guide
- `/WORKSPACE_NOTES_FEATURE.md` - Technical feature documentation

### Updated

- Project README will be updated with workspace features
- CLAUDE.md updated with new services and architecture

## Upgrade Guide

No action required. When you upgrade:

1. The new workspace tables are created automatically via migrations
2. Your existing ontologies work exactly as before
3. First time you access an ontology, a workspace is auto-created
4. All existing concepts get concept notes automatically

## Credits

This feature represents a major enhancement to Eidos, bringing integrated note-taking and knowledge management to ontology building. Special thanks to the testing framework for catching edge cases.

---

**Version**: See git tags
**Release Date**: November 15, 2025
**Migration Required**: Automatic (via EF migrations)
**Breaking Changes**: None

For questions or issues, please open a GitHub issue or contact support.
