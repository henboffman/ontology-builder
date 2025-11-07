# Architecture Decisions: Virtualized Ontology Nodes

**Created**: November 5, 2025
**Status**: Proposed

---

## ADR-VN-001: Use Existing OntologyLink Model

**Status**: Accepted

**Context**:
Need to store relationships between parent and linked ontologies. An `OntologyLink` model already exists for external URI references.

**Decision**:
Extend the existing `OntologyLink` model to support both external (URI-based) and internal (database FK-based) ontology links.

**Alternatives Considered**:
1. Create new `VirtualizedOntology` model
   - ❌ Duplicates logic for external links
   - ❌ More complex querying (two tables)

2. Extend `OntologyLink` (CHOSEN)
   - ✅ Reuses existing model
   - ✅ Single query for all links
   - ✅ Consistent API

**Rationale**:
- External and internal links are semantically similar
- Union type pattern (`LinkType` enum) is clean
- Reduces code duplication
- Follows existing patterns in codebase

---

## ADR-VN-002: Lazy Loading with Cache Invalidation

**Status**: Accepted

**Context**:
Virtualized ontologies could have hundreds of concepts. Loading all data upfront would hurt performance.

**Decision**:
Use lazy loading: load only metadata initially, fetch full data on-demand when user expands the node. Use SignalR for cache invalidation.

**Alternatives Considered**:
1. Eager loading
   - ❌ Slow initial page load
   - ❌ Wastes bandwidth for unexpanded links

2. Polling for updates
   - ❌ Increased server load
   - ❌ Delayed update notifications

3. Lazy loading + SignalR (CHOSEN)
   - ✅ Fast initial load
   - ✅ Real-time updates
   - ✅ Efficient bandwidth usage

**Rationale**:
- SignalR already used for collaboration
- Users typically only expand 1-2 links
- Real-time updates improve UX
- Balances performance with freshness

---

## ADR-VN-003: UI-Level Edit Protection

**Status**: Accepted

**Context**:
Virtualized concepts should be read-only from parent ontology but editable in source.

**Decision**:
Implement protection at UI layer by disabling edit buttons and showing explanatory messages. No server-side write protection (source ontology remains editable).

**Alternatives Considered**:
1. Server-side write blocking
   - ❌ Complex permission logic
   - ❌ User confused why they can't edit in source

2. Duplicate concepts (fork)
   - ❌ Loses synchronization
   - ❌ Not virtualized

3. UI-level blocking (CHOSEN)
   - ✅ Simple to implement
   - ✅ Clear user feedback
   - ✅ Source remains editable

**Rationale**:
- Users can navigate to source to make edits
- Clear visual indicators prevent confusion
- Maintains integrity without complex locking

---

## ADR-VN-004: Circular Dependency Detection via Graph Traversal

**Status**: Accepted

**Context**:
Must prevent circular ontology references (A → B → C → A) which would cause infinite loading loops.

**Decision**:
Use depth-first search to detect cycles before allowing link creation.

**Alternatives Considered**:
1. No cycle detection
   - ❌ Risk of infinite loops
   - ❌ Poor UX

2. DFS with visited set (CHOSEN)
   - ✅ O(V+E) time complexity
   - ✅ Catches all cycles
   - ✅ Easy to understand

3. Topological sort
   - ❌ Overkill for validation
   - ❌ More complex

**Rationale**:
- DFS is standard algorithm for cycle detection
- Performance adequate for typical ontology graphs (<100 nodes)
- Prevents data corruption

---

## ADR-VN-005: Relationships Between Native and Virtualized Concepts

**Status**: Accepted

**Context**:
Users need to create semantic relationships between their concepts and virtualized ones.

**Decision**:
Allow relationships to reference virtualized concepts. Store relationships in parent ontology. When link removed, relationships become orphaned but preserved.

**Alternatives Considered**:
1. Block relationships to virtualized concepts
   - ❌ Defeats purpose of virtualization
   - ❌ Users cannot connect knowledge

2. Store in source ontology
   - ❌ Source owner sees random relationships
   - ❌ Permission complexity

3. Store in parent, allow orphans (CHOSEN)
   - ✅ User owns their relationships
   - ✅ Can reconnect if re-linked
   - ✅ Clear ownership

**Rationale**:
- Relationships belong to parent ontology creator
- Orphaned relationships can be detected and cleaned
- Maintains semantic integrity

---

## ADR-VN-006: Compound Nodes in Cytoscape

**Status**: Accepted

**Context**:
Need to render linked ontology as expandable container with child concept nodes.

**Decision**:
Use Cytoscape.js compound nodes feature. Link node is parent, virtualized concepts are children.

**Alternatives Considered**:
1. Separate graph instance
   - ❌ Complex state synchronization
   - ❌ Cannot create relationships across graphs

2. Visual grouping only (no parent-child)
   - ❌ Children can escape bounds
   - ❌ Less clear ownership

3. Compound nodes (CHOSEN)
   - ✅ Built-in Cytoscape feature
   - ✅ Automatic bounds management
   - ✅ Clear visual hierarchy

**Rationale**:
- Compound nodes designed for this use case
- Automatic layout constrains children
- Standard graph visualization pattern

---

## ADR-VN-007: Permission Checking at Link Creation and Access

**Status**: Accepted

**Context**:
User permissions could change after link is created (revoked access, ontology deleted).

**Decision**:
Check permissions at link creation AND every time linked ontology is accessed.

**Alternatives Considered**:
1. Check only at creation
   - ❌ Stale permissions
   - ❌ Potential data leakage

2. Check only at access
   - ❌ Confusing failures
   - ❌ Database foreign keys to inaccessible data

3. Check at both (CHOSEN)
   - ✅ Prevents invalid links
   - ✅ Handles permission changes
   - ✅ Secure

**Rationale**:
- Defense in depth
- Graceful degradation when access revoked
- Follows existing `OntologyPermissionService` patterns

---

## ADR-VN-008: Visual Distinction via Style Classes

**Status**: Accepted

**Context**:
Virtualized nodes must be clearly distinguishable from regular concepts.

**Decision**:
Use distinct visual style: rounded rectangle shape, purple gradient, dashed border, larger size.

**Alternatives Considered**:
1. Same style as concepts
   - ❌ Confusing
   - ❌ Users don't know what's virtualized

2. Icon overlay only
   - ❌ Too subtle
   - ❌ Hard to see at zoom levels

3. Distinct style (CHOSEN)
   - ✅ Immediately recognizable
   - ✅ Follows design system (purple = linked)
   - ✅ Accessible

**Rationale**:
- Shape difference easiest to spot
- Color coding reinforces meaning
- Size indicates "meta" nature

---

## ADR-VN-009: Export Using OWL Imports

**Status**: Accepted

**Context**:
TTL export should preserve linked ontology relationships in standards-compliant way.

**Decision**:
Use `owl:imports` directive in TTL export to reference linked ontologies by their namespace URI.

**Alternatives Considered**:
1. Custom annotation property
   - ❌ Non-standard
   - ❌ Protégé won't recognize

2. owl:imports (CHOSEN)
   - ✅ OWL 2 standard
   - ✅ Protégé compatible
   - ✅ Semantic web best practice

3. Don't export links
   - ❌ Loses information
   - ❌ Not round-trippable

**Rationale**:
- Follows OWL 2 specification
- Interoperable with other ontology tools
- Preserves semantic meaning

---

## Summary of Key Decisions

1. **Extend OntologyLink model** with `LinkType` enum
2. **Lazy load** with SignalR synchronization
3. **UI-level protection** for virtualized concepts
4. **Graph traversal** for cycle detection
5. **Allow relationships** between native and virtualized
6. **Compound nodes** for graph rendering
7. **Dual permission checks** at creation and access
8. **Distinct visual style** for recognition
9. **OWL imports** for semantic export

These decisions prioritize:
- **User experience**: Clear visual feedback, fast performance
- **Data integrity**: Cycle prevention, permission enforcement
- **Standards compliance**: OWL 2 compatible export
- **Maintainability**: Reuse existing patterns and models
