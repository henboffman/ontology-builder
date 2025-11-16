# Notes Tagging & Import Feature - Development Ledger

**Created**: 2025-11-15
**Status**: Planning
**Last Updated**: 2025-11-15

## Quick Summary

This feature set enhances the notes system with:
1. **Tagging System** - Organize notes with tags and virtual folders
2. **Markdown Import** - Import .md files with [[wiki-link]] parsing
3. **Markdown Export** - Export notes with frontmatter preservation
4. **Auto-Save** - Automatic debounced saving while editing
5. **Bracket Wrapping** - Quick wiki-link creation with keyboard shortcut
6. **Workspace Navigation** - Choose between ontology or notes view
7. **SKOS Integration** - Optional ontological representation of tags

## Documentation Files

### Core Documents
- **[requirements.md](./requirements.md)** - Detailed feature requirements and user stories
- **[architecture.md](./architecture.md)** - Technical architecture, database schema, services
- **[implementation-plan.md](./implementation-plan.md)** - Day-by-day implementation steps

## Implementation Status

### Phase 1: Core Tag System ⏳
- [ ] Database schema for tags
- [ ] Tag models and repositories
- [ ] TagService implementation
- [ ] Basic tag UI components
- [ ] Tag assignment functionality

### Phase 2: Virtual Folders ⏸️
- [ ] NotesSidebar component
- [ ] Tag filtering logic
- [ ] Tag count badges
- [ ] State management for filtering

### Phase 3: Auto-Save ⏸️
- [ ] AutoSaveService with debouncing
- [ ] AutoSaveIndicator component
- [ ] JavaScript integration
- [ ] Save status tracking

### Phase 4: Markdown Import ⏸️
- [ ] WikiLinkParserService
- [ ] MarkdownImportService
- [ ] Frontmatter parsing (YamlDotNet)
- [ ] MarkdownImportDialog UI
- [ ] Batch import support

### Phase 5: Markdown Export ⏸️
- [ ] MarkdownExportService
- [ ] Frontmatter generation
- [ ] ZIP export for batches
- [ ] MarkdownExportDialog UI

### Phase 6: Bracket Wrapping & Navigation ⏸️
- [ ] Bracket wrapping JavaScript
- [ ] WorkspaceNavigationDialog
- [ ] Preference storage
- [ ] Routing updates

### Phase 7: SKOS Integration (Optional) ⏸️
- [ ] TagOntologyIntegrationService
- [ ] SKOS Concept creation
- [ ] Tag promotion UI
- [ ] Sync preferences

### Phase 8: Testing & Polish ⏸️
- [ ] Unit tests
- [ ] Integration tests
- [ ] Performance testing
- [ ] UI polish
- [ ] Documentation updates

## Key Decisions Needed

Before starting implementation, please review and decide:

1. ❓ **Tag Colors**: Auto-generate or user-selectable?
2. ❓ **Tag Limits**: Max tags per note? (Suggest: 10)
3. ❓ **Tag Hierarchy**: Support nested tags like `project/frontend`?
4. ❓ **Wiki-Link Ambiguity**: Disambiguation UI or auto-link to first match?
5. ❓ **Concurrent Edits**: Last-write-wins or conflict resolution?
6. ❓ **Import Conflicts**: How to handle duplicate note titles?
7. ❓ **Export Formats**: Support Obsidian/Logseq compatibility?
8. ❓ **SKOS Priority**: Essential or optional for v1?

## Timeline Estimate

**Total Duration**: 18 working days (~3.5 weeks)

- Phase 1: Core Tag System - 3 days
- Phase 2: Virtual Folders - 2 days
- Phase 3: Auto-Save - 2 days
- Phase 4: Markdown Import - 3 days
- Phase 5: Markdown Export - 2 days
- Phase 6: Bracket Wrapping & Navigation - 2 days
- Phase 7: SKOS Integration (Optional) - 2 days
- Phase 8: Testing & Polish - 2 days

## Dependencies

### NuGet Packages
- `YamlDotNet` (>= 13.7.1) - YAML frontmatter parsing

### Existing Systems
- Workspace/Notes infrastructure (✅ Already exists)
- NoteService and NoteRepository (✅ Already exists)
- ConceptService for wiki-link → concept creation (✅ Already exists)
- WikiLinkParser service (✅ Already exists)

### Browser APIs
- File API (for import)
- Blob API (for export)
- Selection API (for bracket wrapping)

## Success Metrics

### Performance Targets
- ✅ Import 100+ markdown files in < 30 seconds
- ✅ Auto-save completes within 500ms of inactivity
- ✅ Tag filtering responds in < 100ms for 1000+ notes
- ✅ Wiki-link parsing accuracy > 99%
- ✅ Export/import roundtrip preserves all content

### User Experience
- ✅ Intuitive tag-based organization
- ✅ Zero data loss in auto-save
- ✅ Mobile-responsive UI
- ✅ Clear error messages
- ✅ Helpful tooltips and documentation

## Technical Highlights

### Database Schema
New tables:
- `Tags` - Tag definitions per workspace
- `NoteTagAssignments` - Many-to-many relationship

Extended tables:
- `Notes` - Add import metadata fields

### Services
- `TagService` - Tag CRUD and assignment
- `MarkdownImportService` - Parse and import .md files
- `MarkdownExportService` - Export with frontmatter
- `WikiLinkParserService` - Extract [[links]]
- `AutoSaveService` - Debounced auto-save
- `TagOntologyIntegrationService` - SKOS integration (optional)

### UI Components
- `NotesSidebar` - Tag navigation
- `TagBadge`, `TagSelector` - Tag UI
- `MarkdownImportDialog`, `MarkdownExportDialog` - Import/export
- `AutoSaveIndicator` - Save status
- `WorkspaceNavigationDialog` - Choose ontology or notes

## Notes & Observations

### Design Inspirations
- **Obsidian**: Wiki-links, tag system, markdown import/export
- **Notion**: Virtual folders based on tags
- **Roam Research**: Bracketed linking, auto-save
- **VS Code**: Workspace concept, status indicators

### Ontological Best Practices
Research indicates **SKOS (Simple Knowledge Organization System)** is the standard for representing tags and classification schemes in ontologies:

- Use `skos:ConceptScheme` for "Note Classification Scheme"
- Each tag becomes a `skos:Concept` instance
- Use `skos:prefLabel` for tag names
- Custom annotation property links notes to tag concepts
- Allows tags to evolve into full ontology concepts

### Auto-Save Considerations
- Debounce period: 2 seconds (balance between UX and server load)
- Queue-based to prevent concurrent writes
- Optimistic UI updates for instant feedback
- Version field for optimistic locking
- Retry logic for transient failures

### Import/Export Format
Standard frontmatter format compatible with Obsidian:
```yaml
---
title: Note Title
tags: [tag1, tag2]
created: 2025-11-15T10:30:00Z
modified: 2025-11-15T14:20:00Z
workspace: Workspace Name
linked_concepts: [Concept1, Concept2]
---
```

## Related Features

### Existing Infrastructure
- Workspaces (container for ontology + notes)
- Notes system with content storage
- WikiLink parsing and preview
- Global search (Cmd+K) includes notes
- Markdown rendering with Markdig

### Future Enhancements
- Bidirectional links (backlinks)
- Note templates
- Note versioning/history
- Collaborative note editing
- Note embedding `![[Note]]`
- Block references `[[Note#block]]`
- Tag hierarchy and nesting
- Smart folders (saved searches)

## Questions & Answers

### Q: Why SKOS instead of custom ontology for tags?
**A**: SKOS is a W3C standard specifically designed for knowledge organization systems like tags, categories, and taxonomies. It's widely adopted and interoperable.

### Q: How do we handle wiki-link ambiguity?
**A**: When `[[Link]]` matches multiple concepts, we'll show a disambiguation dialog. User can also use `[[Concept:Link]]` syntax to specify namespace.

### Q: What about concurrent editing of the same note?
**A**: Use optimistic locking with version field. If conflict detected, show warning and let user choose to overwrite or merge.

### Q: Should tags be workspace-scoped or global?
**A**: Workspace-scoped. Each workspace has its own tag namespace, preventing conflicts and allowing different organization schemes per workspace.

### Q: How to handle large batch imports?
**A**: Process in batches of 10 files at a time, show progress bar, use transactions per file (rollback on error), provide detailed error report at end.

## Contact & Stakeholders

**Developer**: Working with Claude Code
**Documentation**: This development ledger
**User Feedback**: TBD after implementation

## Next Steps

1. ✅ Create development ledger documentation
2. ⏳ Review requirements and architecture with stakeholder
3. ⏸️ Answer key decision questions
4. ⏸️ Begin Phase 1: Core Tag System implementation
5. ⏸️ Set up daily progress tracking

---

**Legend**:
- ✅ Complete
- ⏳ In Progress
- ⏸️ Not Started
- ❌ Blocked
- ❓ Needs Decision
