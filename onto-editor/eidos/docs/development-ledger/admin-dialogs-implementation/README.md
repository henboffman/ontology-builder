# Admin Dialogs Implementation
**Created**: November 2, 2025
**Status**: Planning
**Feature**: Quick admin interface for managing entities (Concepts, Relationships, Individuals)

## Overview

Implementing focused admin dialogs that provide a condensed interface for entity management with:
- Scrollable entity list
- Search/filter capabilities
- Quick delete operations
- Inline edit functionality
- Clean, minimal UI focused on efficiency

## Documentation Structure

This implementation is documented across focused files:

- **README.md** (this file) - Overview and navigation
- **requirements.md** - Feature requirements and user stories
- **architecture.md** - Technical design and component structure
- **implementation-plan.md** - Step-by-step implementation tasks
- **ui-design.md** - Visual design, layout, and styling specifications
- **reference-patterns.md** - Links to existing patterns to follow

## Quick Links

### Planning Documents
- [Requirements](./requirements.md) - What we're building and why
- [Architecture](./architecture.md) - How components fit together
- [UI Design](./ui-design.md) - Visual specifications

### Implementation
- [Implementation Plan](./implementation-plan.md) - Ordered tasks and file changes
- [Reference Patterns](./reference-patterns.md) - Existing code to follow

## Goals

1. **Fast Access** - Quick way to view/edit/delete entities without navigating away
2. **Focused Interface** - Single-purpose dialogs, no feature bloat
3. **Consistent UX** - Follow existing FloatingPanel patterns
4. **Admin-Only** - Requires FullAccess permission level
5. **Clean Code** - Reusable components, clear separation of concerns

## Non-Goals

- Not replacing existing detailed editors (ConceptEditor, etc.)
- Not adding bulk operations (separate feature)
- Not a full admin dashboard (this is entity-specific)

## Success Criteria

- [ ] Admin can open entity list dialog from ontology page
- [ ] Search/filter updates list in real-time
- [ ] Click entity to edit inline
- [ ] Delete entity with confirmation
- [ ] Dialog is responsive (desktop + mobile)
- [ ] Follows existing style patterns
- [ ] Permission-gated (admin only)

## Related Features

- Existing entity editors: `/Components/Ontology/*ManagementPanel.razor`
- FloatingPanel pattern: `/Components/Shared/FloatingPanel.razor`
- List components: `/Components/Ontology/ConceptListView.razor`
- Permission system: `/Services/OntologyPermissionService.cs`

---
**Next Step**: Review [requirements.md](./requirements.md)
