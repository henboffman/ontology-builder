# Virtualized Ontology Nodes Feature

**Created**: November 5, 2025
**Status**: Planning
**Feature Type**: Major Feature - Graph Integration

## Overview

This feature enables users to embed other ontologies as virtualized nodes within their graphs. These nodes provide a read-only, live view of another ontology that updates automatically when the source ontology changes. This creates a powerful composition model for building complex knowledge structures from reusable ontology components.

## Problem Statement

Users need to:
1. Reference and reuse existing ontologies without duplicating content
2. Compose complex knowledge models from smaller, focused ontologies
3. Keep their graphs synchronized with source ontologies when they update
4. Maintain semantic relationships between different ontologies
5. Prevent accidental or destructive modifications to shared ontologies

Current limitations:
- No way to link ontologies together visually in the graph
- Must manually clone and update if source ontology changes
- No composition model for building from reusable components
- Difficult to maintain semantic integrity across related ontologies

## Goals

### Primary Goals
1. **Virtualization**: Display another ontology as a special node in the current graph
2. **Access Control**: Only allow linking to ontologies the user has permission to view
3. **Automatic Sync**: Reflect changes in source ontology automatically
4. **Read-Only Enforcement**: Prevent destructive edits to virtualized ontology
5. **Visual Distinction**: Clearly distinguish virtualized nodes from regular concepts
6. **Semantic Relationships**: Support relationships between parent and virtualized ontologies

### Secondary Goals
1. **Performance**: Efficient loading and rendering of virtualized nodes
2. **Export Support**: Include virtualized ontology references in TTL/JSON export
3. **Collaboration**: Show when multiple users are viewing the same virtualized ontology
4. **Versioning**: Optional pinning to specific ontology versions

## User Stories

### As an ontology builder:
- I want to insert another ontology as a node so I can compose complex models
- I want the virtualized ontology to update automatically when the source changes
- I want to prevent accidental edits to ontologies I'm referencing
- I want to see relationships between my concepts and virtualized ontology concepts
- I want to only link ontologies I have permission to view

### As a collaborator:
- I want to see which ontologies are virtualized vs native
- I want to drill into a virtualized ontology to explore it
- I want to create relationships to concepts within virtualized ontologies

## Design Decisions

Following the architecture patterns established in `ontologyview-refactor-2025-02/architecture-decisions.md`:

### 1. Data Model Approach
**Decision**: Extend existing `OntologyLink` model to support internal ontology references (not just external URIs)

**Rationale**:
- `OntologyLink` already exists for external ontology references
- Can add `LinkedOntologyId` foreign key to reference internal ontologies
- Maintains consistency with existing data model
- Allows both external and internal ontology links

**Changes**:
- Add `LinkedOntologyId` nullable FK to `OntologyLink`
- Add `LinkedOntology` navigation property
- Add `LinkType` enum: `External`, `Internal`
- Validation: must have either `Uri` (external) or `LinkedOntologyId` (internal)

### 2. Graph Visualization Approach
**Decision**: Render virtualized ontologies as special "meta-nodes" with distinct visual style

**Rationale**:
- Clearly distinguishable from regular concepts
- Can expand/collapse to show contained concepts
- Follows existing graph rendering patterns
- Reuses Cytoscape.js for consistency

**Visual Design**:
- Shape: Rounded rectangle (different from concept circles)
- Color: Gradient or special theme color (e.g., purple gradient)
- Icon: Link/network icon overlay
- Size: Larger than concept nodes to indicate it contains multiple concepts
- Border: Dashed or double border to show it's external
- Label: Ontology name with concept count badge

### 3. Synchronization Strategy
**Decision**: Lazy loading with cache invalidation on SignalR updates

**Rationale**:
- Don't load full ontology until user expands the node
- Use SignalR to detect when source ontology changes
- Invalidate cache and show "update available" indicator
- User can refresh to get latest version
- Balances performance with freshness

**Implementation**:
- Load ontology metadata (name, concept count) initially
- Load full graph when user expands/drills down
- Subscribe to SignalR updates for linked ontology
- Show badge when updates are available
- Auto-refresh option in settings

### 4. Permission Enforcement
**Decision**: Check permissions at link creation and at runtime

**Rationale**:
- User must have `CanView` permission on target ontology
- Re-check permission before loading ontology data
- If permission revoked, show error state in node
- Follows existing `OntologyPermissionService` patterns

**Checks**:
- Link creation: Validate user has view access
- Initial load: Verify permission still valid
- Expansion: Re-verify before loading full data
- Handle permission loss gracefully (show lock icon)

### 5. Edit Protection Strategy
**Decision**: UI-level blocking with clear visual indicators

**Rationale**:
- Virtualized concepts cannot be edited through parent ontology
- Disable edit buttons and show tooltip explaining why
- User must navigate to source ontology to make edits
- Prevents accidental modifications

**UI Changes**:
- Disable edit/delete buttons on virtualized concepts
- Show "View in source ontology" link instead
- Tooltip: "This concept is from a linked ontology and cannot be edited here"
- Lock icon overlay on virtualized concept nodes

### 6. Relationship Handling
**Decision**: Allow relationships between native and virtualized concepts, stored in parent ontology

**Rationale**:
- Users need to connect their concepts to virtualized ones
- Relationships are owned by parent ontology
- When virtualized ontology is unlinked, relationships are preserved but marked as broken
- Follows existing relationship model

**Storage**:
- Relationships stored in parent ontology's `Relationships` table
- `SourceConcept` can be virtualized or native
- `TargetConcept` can be virtualized or native
- Add `IsVirtualized` flag to relationship rendering

## Technical Architecture

### Database Schema Changes

#### Modified Tables

**OntologyLink** (extends existing):
```csharp
public class OntologyLink
{
    public int Id { get; set; }
    public int OntologyId { get; set; } // Parent ontology
    public Ontology Ontology { get; set; }

    // NEW: Type of link
    public LinkType Type { get; set; } // External or Internal

    // Existing: For external ontologies
    public string? Uri { get; set; }
    public string? Prefix { get; set; }

    // NEW: For internal ontology references
    public int? LinkedOntologyId { get; set; }
    public Ontology? LinkedOntology { get; set; }

    // Display properties
    public string Name { get; set; }
    public string? Description { get; set; }

    // Visualization properties (NEW)
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public string? Color { get; set; }

    // Metadata
    public bool ConceptsImported { get; set; }
    public int ImportedConceptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum LinkType
{
    External = 0, // URI-based external ontology
    Internal = 1  // Internal ontology reference
}
```

**VirtualizedConceptView** (NEW - materialized view for performance):
```csharp
// Denormalized view for efficient querying of concepts through links
public class VirtualizedConceptView
{
    public int ParentOntologyId { get; set; }
    public int OntologyLinkId { get; set; }
    public int ConceptId { get; set; }
    public string ConceptName { get; set; }
    public string ConceptDescription { get; set; }
    public string SourceOntologyName { get; set; }
    // Flattened for graph rendering
}
```

### Service Layer

**IOntologyLinkService** (NEW):
```csharp
public interface IOntologyLinkService
{
    // Link Management
    Task<OntologyLink> CreateInternalLinkAsync(int parentOntologyId, int linkedOntologyId, string userId);
    Task<OntologyLink> CreateExternalLinkAsync(int ontologyId, string uri, string name, string userId);
    Task UpdateLinkPositionAsync(int linkId, double x, double y, string userId);
    Task RemoveLinkAsync(int linkId, string userId);

    // Querying
    Task<List<OntologyLink>> GetLinkedOntologiesAsync(int ontologyId);
    Task<Ontology?> GetLinkedOntologyDetailsAsync(int linkId, string userId);
    Task<List<Concept>> GetVirtualizedConceptsAsync(int linkId, string userId);

    // Validation
    Task<bool> CanLinkOntologyAsync(int parentOntologyId, int targetOntologyId, string userId);
    Task<bool> HasCircularDependencyAsync(int parentOntologyId, int targetOntologyId);
}
```

**Existing Services Modified**:
- `OntologyService`: Add methods to check if ontology is linked elsewhere
- `ConceptService`: Add flag to indicate if concept is virtualized
- `GraphVisualizationService` (NEW or extend existing): Handle rendering virtualized nodes

### UI Components

**New Components**:

1. **LinkOntologyDialog.razor**
   - Search/browse ontologies user has access to
   - Preview ontology (name, description, concept count)
   - Position selector for graph placement
   - Permission check indicator

2. **VirtualizedOntologyNode.razor**
   - Special rendering for virtualized ontology in graph
   - Expand/collapse functionality
   - Update available indicator
   - Link to view source ontology

3. **VirtualizedConceptDetails.razor**
   - Read-only view of virtualized concept
   - "View in source ontology" button
   - Link back to parent ontology

**Modified Components**:
- `OntologyView.razor`: Add link ontology button
- `GraphViewPanel.razor`: Render virtualized nodes differently
- `ConceptDialog.razor`: Disable editing for virtualized concepts
- `RelationshipDialog.razor`: Allow creating relationships to virtualized concepts

### Graph Visualization Changes

**Cytoscape.js Integration**:
```javascript
// New node class for virtualized ontologies
cytoscape.add({
  classes: 'virtualized-ontology',
  data: {
    id: 'link_123',
    type: 'ontology-link',
    label: 'Linked Ontology Name',
    conceptCount: 42,
    linkedOntologyId: 123,
    updateAvailable: false
  },
  style: {
    'background-color': '#9333ea', // Purple gradient
    'shape': 'round-rectangle',
    'border-width': 3,
    'border-style': 'dashed',
    'width': 120,
    'height': 80,
    'label': 'data(label)'
  }
});
```

**Interaction Behaviors**:
- Single click: Select node
- Double click: Expand to show contained concepts
- Right click: Context menu (View source, Unlink, Refresh)
- Hover: Show tooltip with concept count and last updated

## Implementation Phases

### Phase 1: Database & Core Models (Week 1)
- [ ] Create migration for `OntologyLink` changes
- [ ] Add `LinkType` enum
- [ ] Add `LinkedOntologyId` FK and navigation
- [ ] Add position properties
- [ ] Update `DbContext` configuration
- [ ] Write repository tests

### Phase 2: Service Layer (Week 1-2)
- [ ] Implement `IOntologyLinkService`
- [ ] Permission checking logic
- [ ] Circular dependency detection
- [ ] Cache strategy for linked ontologies
- [ ] Service unit tests

### Phase 3: Graph Visualization (Week 2)
- [ ] Create virtualized node rendering in Cytoscape
- [ ] Implement expand/collapse behavior
- [ ] Add visual distinction styling
- [ ] Update graph layout algorithm to handle meta-nodes
- [ ] Add interaction handlers

### Phase 4: UI Components (Week 2-3)
- [ ] Build `LinkOntologyDialog.razor`
- [ ] Create `VirtualizedOntologyNode.razor`
- [ ] Modify `ConceptDialog` for read-only mode
- [ ] Add toolbar button to link ontologies
- [ ] Implement drag-and-drop positioning

### Phase 5: SignalR Synchronization (Week 3)
- [ ] Subscribe to source ontology updates
- [ ] Implement cache invalidation
- [ ] Add update available indicator
- [ ] Create refresh mechanism
- [ ] Handle permission revocation

### Phase 6: Export & Import (Week 3-4)
- [ ] Update TTL export to include owl:imports for linked ontologies
- [ ] Update JSON export with link metadata
- [ ] Handle import of linked ontology references
- [ ] Version pinning (optional)

### Phase 7: Testing & Polish (Week 4)
- [ ] Integration tests for linking workflow
- [ ] Permission enforcement tests
- [ ] UI/UX testing
- [ ] Performance testing with many links
- [ ] Documentation updates

## User Experience Flow

### Linking an Ontology

1. User clicks "Link Ontology" button in toolbar
2. Dialog opens showing ontologies they have access to
3. User searches/filters for desired ontology
4. Preview shows name, description, concept count
5. User confirms and clicks "Link"
6. Virtualized node appears in graph at center
7. User can drag to position
8. Toast notification: "Linked [Ontology Name] successfully"

### Viewing Virtualized Content

1. User double-clicks virtualized ontology node
2. Node expands to show contained concepts as child nodes
3. Concepts are visually marked as virtualized (lighter color, lock icon)
4. User can create relationships to virtualized concepts
5. Clicking virtualized concept shows read-only details
6. "View in Source" button navigates to source ontology

### Handling Updates

1. Source ontology is updated by its owner
2. SignalR notifies all linked parent ontologies
3. Update indicator badge appears on virtualized node
4. User clicks badge to refresh
5. Graph updates with new concepts/changes
6. Existing relationships are preserved

### Managing Permissions

1. If user's view permission is revoked:
2. Virtualized node shows lock icon and error state
3. Tooltip: "Access to this ontology has been revoked"
4. User can unlink but not view contents
5. Relationships to virtualized concepts remain but show warning

## Security Considerations

1. **Permission Validation**: Always check `CanView` before loading data
2. **No Data Leakage**: Don't expose ontology structure if permission denied
3. **Audit Trail**: Log all link/unlink operations
4. **Rate Limiting**: Prevent abuse of link creation API
5. **Circular Reference Prevention**: Validate no cycles in ontology graph

## Performance Considerations

1. **Lazy Loading**: Don't load full ontology until expanded
2. **Caching**: Cache linked ontology metadata (5 min TTL)
3. **Pagination**: If linked ontology has 1000+ concepts, paginate expansion
4. **Incremental Rendering**: Use virtual scrolling for large concept lists
5. **Debouncing**: Throttle SignalR update notifications

## Open Questions

1. **Versioning**: Should users be able to pin to specific ontology versions?
2. **Nested Links**: Allow ontologies to link to ontologies that themselves have links?
3. **Bulk Linking**: Support linking multiple ontologies at once?
4. **Link Sharing**: When parent is shared, should links be accessible?
5. **Conflict Resolution**: How to handle when linked ontology is deleted?

## Success Metrics

1. **Adoption**: 30% of users create at least one ontology link within 2 weeks
2. **Composition**: Average ontology has 2-3 links after 1 month
3. **Performance**: Graph with 5 linked ontologies renders in <2 seconds
4. **Reliability**: 99.9% uptime for synchronization mechanism
5. **User Satisfaction**: 4.5/5 stars in post-feature survey

## Related Features

- Collaboration Board: Discover ontologies to link
- Permission System: Enforce access control
- SignalR Presence: Real-time updates
- Fork/Clone: Alternative to virtualization for full copy
- Export: Include links in semantic export

## Resources

- [OWL 2 Imports](https://www.w3.org/TR/owl2-syntax/#Imports)
- [Cytoscape.js Compound Nodes](https://js.cytoscape.org/#notation/compound-nodes)
- [SignalR Groups](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups)
