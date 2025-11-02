# OntologyView Refactoring Initiative

**Date**: February 2025
**Status**: Planning Complete - Ready for Implementation
**Lead Developer**: Claude Code with User Approval
**Priority**: High
**Estimated Duration**: 7 weeks (4 phases)

---

## User Request

> "lets refactor the ontologyview file and split it into smaller components"

---

## Executive Summary

The OntologyView.razor component has grown to **3,184 lines** and suffers from the "God Component" anti-pattern, making it difficult to maintain, test, and extend. This initiative will decompose it into **23 focused components and classes**, reducing the main file to approximately **400 lines** - an **87% reduction**.

### Key Metrics

| Metric | Current | Target | Improvement |
|--------|---------|--------|-------------|
| Main File Size | 3,184 lines | ~400 lines | 87% reduction |
| Number of Components | 1 monolithic | 23 focused files | Better separation |
| Average Component Size | N/A | ~152 lines | Manageable size |
| State Management | 50+ scattered fields | 2 centralized classes | Clear ownership |
| Testability | Poor | Good | Isolated testing |
| Code Duplication | High (desktop/mobile) | Minimal | DRY principle |

---

## Problems Addressed

### 1. God Component Anti-Pattern
- **Issue**: Single file handles state, UI, API calls, SignalR, permissions, dialogs, graph visualization, and more
- **Impact**: High cognitive load, difficult to understand and modify
- **Solution**: Decompose into focused components with single responsibilities

### 2. State Management Chaos
- **Issue**: 50+ state fields scattered throughout with no clear ownership
- **Impact**: Difficult to track state changes, prone to bugs
- **Solution**: Centralized state management in `OntologyViewState` and `GraphViewState` classes

### 3. Code Duplication
- **Issue**: Details panels, toolbars, and dialogs rendered twice (desktop/mobile)
- **Impact**: Maintenance burden, inconsistencies between views
- **Solution**: Extract reusable components used by both layouts

### 4. Poor Testability
- **Issue**: Cannot test individual features in isolation
- **Impact**: Limited test coverage, regression risks
- **Solution**: Small, focused components with clear interfaces

### 5. Difficult Onboarding
- **Issue**: New developers face 3,000+ line file with multiple concerns
- **Impact**: Slow ramp-up time, confusion
- **Solution**: Clear component hierarchy with focused responsibilities

---

## Solution Architecture

### Component Hierarchy

```
OntologyView.razor (~400 lines)
├── State Management
│   ├── OntologyViewState.cs
│   └── GraphViewState.cs
├── UI Components
│   ├── OntologyHeader.razor
│   ├── OntologyToolbar.razor
│   ├── ViewTabs.razor
│   ├── GraphViewPanel.razor
│   ├── ListViewPanel.razor
│   ├── HierarchyViewPanel.razor
│   ├── TtlViewPanel.razor
│   ├── SelectedNodeDetailsPanel.razor (existing)
│   ├── ConceptDetailsFloatingPanel.razor (existing)
│   ├── PresenceIndicator.razor (existing)
│   └── ActivityFeed.razor (existing)
├── Dialogs (extracted)
│   ├── AddConceptDialog.razor
│   ├── AddRelationshipDialog.razor
│   ├── AddIndividualDialog.razor
│   └── ConfirmDeleteDialog.razor
└── Code-Behind (partial classes)
    ├── OntologyView.Lifecycle.cs
    ├── OntologyView.Permissions.cs
    ├── OntologyView.SignalR.cs
    ├── OntologyView.GraphOperations.cs
    ├── OntologyView.Dialogs.cs
    ├── OntologyView.Export.cs
    ├── OntologyView.Search.cs
    └── OntologyView.Keyboard.cs
```

---

## Implementation Phases

### Phase 1: State Management (Week 1)
Extract state into dedicated classes for clear ownership and easier testing.
- `OntologyViewState.cs`
- `GraphViewState.cs`

### Phase 2: UI Components (Weeks 2-4)
Extract UI components for reusability and reduced duplication.
- `OntologyHeader.razor`
- `OntologyToolbar.razor`
- `ViewTabs.razor`
- `GraphViewPanel.razor`
- `ListViewPanel.razor`
- `HierarchyViewPanel.razor`
- `TtlViewPanel.razor`
- `AddConceptDialog.razor`
- `AddRelationshipDialog.razor`
- `AddIndividualDialog.razor`
- `ConfirmDeleteDialog.razor`

### Phase 3: Code-Behind Partial Classes (Weeks 5-6)
Organize business logic into focused partial classes.
- `OntologyView.Lifecycle.cs`
- `OntologyView.Permissions.cs`
- `OntologyView.SignalR.cs`
- `OntologyView.GraphOperations.cs`
- `OntologyView.Dialogs.cs`
- `OntologyView.Export.cs`
- `OntologyView.Search.cs`
- `OntologyView.Keyboard.cs`

### Phase 4: Main Component Refactoring (Week 7)
Refactor main component to orchestrate extracted components.
- Remove extracted UI (now in components)
- Remove extracted logic (now in partial classes)
- Simplify to ~400 lines
- Add comprehensive documentation

---

## Benefits

### Immediate
- **Maintainability**: Easier to understand and modify individual components
- **Testability**: Can test components in isolation
- **Code Reusability**: Components can be reused in other contexts
- **Reduced Duplication**: Single source of truth for each feature

### Long-term
- **Faster Development**: New features easier to add
- **Better Onboarding**: New developers can understand components independently
- **Improved Collaboration**: Multiple developers can work on different components
- **Reduced Bugs**: Smaller surface area for each component reduces regression risks

---

## Risk Mitigation

### Testing Strategy
- Write tests for each extracted component as it's created
- Maintain 100% test pass rate throughout refactoring
- Compare rendered output before/after to ensure no regressions

### Incremental Rollout
- Extract one component at a time
- Verify functionality after each extraction
- Keep git commits small and focused

### Rollback Plan
- Each phase is independently reversible
- Git history maintains working states
- Can pause between phases if issues arise

---

## Documentation Structure

This directory contains:
- **README.md** (this file): Overview and rationale
- **refactoring-plan.md**: Detailed component specifications
- **architecture-decisions.md**: Key architectural choices
- **implementation-timeline.md**: Week-by-week implementation guide
- **component-specifications.md**: Detailed specs for each component

---

## Related Context

### Previous Related Work
- **hide-concept-dialogs.css**: Disabled old Cytoscape.js qtip dialogs in favor of SelectedNodeDetailsPanel
- **SelectedNodeDetailsPanel.razor**: Sidebar panel for concept/relationship details (created in previous session)
- **ConceptDetailsFloatingPanel.razor**: Draggable floating panel for concept details (created in previous session)

### Project Context
- **Application**: Eidos Ontology Builder
- **Framework**: Blazor Server (.NET 9)
- **Current Status**: 157+ tests passing, 0 errors, 20 warnings

---

**Last Updated**: February 2025
**Next Review**: After Phase 1 completion
