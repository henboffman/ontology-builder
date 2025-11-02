# Implementation Timeline - OntologyView Refactoring

**Last Updated**: February 2025
**Total Duration**: 7 weeks
**Status**: Planning Complete - Ready to Begin

---

## Overview

This document provides a week-by-week implementation plan for the OntologyView refactoring. The refactoring is divided into 4 phases executed over 7 weeks, with each phase building incrementally on the previous work.

### Key Principles

1. **Incremental**: Each change is small and reversible
2. **Always Working**: Application remains functional after each commit
3. **Test First**: Write tests before or alongside each extraction
4. **One at a Time**: Extract one component/class per commit
5. **Verify**: Test after each extraction to ensure no regressions

---

## Phase 1: State Management (Week 1)

**Goal**: Extract state into dedicated classes for clear ownership and easier testing.

### Week 1: State Classes

**Duration**: 5 days

#### Day 1: OntologyViewState.cs

**Tasks**:
1. ✅ Create `Models/ViewState/OntologyViewState.cs`
2. ✅ Move ontology data properties (Ontology, Concepts, Relationships, Individuals)
3. ✅ Move view state properties (CurrentView, IsLoading, ErrorMessage)
4. ✅ Move selection state (SelectedConcept, SelectedRelationship, SelectedIndividual)
5. ✅ Move UI state (sidebar visibility, panel visibility)
6. ✅ Move permission state (CanEdit, CanManage)
7. ✅ Add state change event (`OnStateChanged`)
8. ✅ Add state mutation methods (`SetSelectedConcept`, `ClearSelection`, etc.)
9. ✅ Write unit tests for OntologyViewState

**Commit**: `refactor: extract OntologyViewState class`

**Verification**:
- Build succeeds
- All tests pass
- OntologyView still renders correctly

#### Day 2: GraphViewState.cs

**Tasks**:
1. ✅ Create `Models/ViewState/GraphViewState.cs`
2. ✅ Move graph instance properties (GraphElementId, IsGraphInitialized)
3. ✅ Move layout settings (CurrentLayout, ShowIndividuals, ShowLabels)
4. ✅ Move interaction state (IsNodeDragging, HoveredNodeId)
5. ✅ Move filtering properties (CategoryFilter, HiddenConceptIds)
6. ✅ Add state change event (`OnGraphStateChanged`)
7. ✅ Add state mutation methods (`ToggleIndividuals`, `SetLayout`, etc.)
8. ✅ Write unit tests for GraphViewState

**Commit**: `refactor: extract GraphViewState class`

**Verification**:
- Build succeeds
- All tests pass
- Graph view still works correctly

#### Day 3-4: Integrate State Classes into OntologyView

**Status**: ✅ COMPLETED (November 2, 2025)

**Tasks**:
1. ✅ Instantiate `OntologyViewState` and `GraphViewState` in OntologyView
2. ✅ Replace all direct state field accesses with backward-compatible properties
3. ✅ Subscribe to state change events (`OnStateChanged`, `OnGraphStateChanged`)
4. ✅ Call `StateHasChanged()` when state changes via event handlers
5. ⏭️ Update all child components to receive state via parameters (deferred to Phase 2)
6. ✅ Run full test suite

**Commit**: `refactor: integrate state classes into OntologyView`

**Verification**:
- ✅ Build succeeds (0 errors, 11 pre-existing warnings)
- ✅ All existing functionality preserved through backward-compatible properties
- ✅ State classes instantiated and subscribed to in OnInitializedAsync
- ✅ Event handlers properly unsubscribed in DisposeAsync

**Implementation Details**:
- Added `using Eidos.Models.ViewState;` directive
- Created `viewState` and `graphState` instances
- Implemented backward-compatible properties that delegate to state classes:
  - `ontology` → `viewState.Ontology`
  - `viewMode` → `viewState.CurrentViewMode`
  - `selectedConcept` → `viewState.SelectedConcept`
  - `selectedRelationship` → `viewState.SelectedRelationship`
  - `selectedIndividual` → `viewState.SelectedIndividual`
  - `userPermissionLevel` → `viewState.UserPermissionLevel`
  - `currentUserId` → `viewState.CurrentUserId`
  - `individuals` → `viewState.Individuals`
  - `graphColorMode` → `graphState.ColorMode`
- Updated permission helper methods to use state class properties:
  - `CanAdd()` → `viewState.CanAdd`
  - `CanEdit()` → `viewState.CanEdit`
  - `CanFullAccess()` → `viewState.CanManage`

#### Day 5: Documentation and Review

**Tasks**:
1. ✅ Update code comments and XML documentation
2. ✅ Review state class design for any improvements
3. ✅ Update development ledger with progress
4. ✅ Demo to team/stakeholders

**Deliverables**:
- ✅ OntologyViewState.cs (~200 lines)
- ✅ GraphViewState.cs (~150 lines)
- ✅ Unit tests for both classes
- ✅ Updated OntologyView.razor using state classes

---

## Phase 2: UI Components (Weeks 2-4)

**Goal**: Extract UI components for reusability and reduced duplication.

### Week 2: Core Components

**Duration**: 5 days

#### Day 1: OntologyHeader.razor

**Tasks**:
1. ✅ Create `Components/Ontology/OntologyHeader.razor`
2. ✅ Extract header markup from OntologyView
3. ✅ Define parameters (Ontology, ShowPresence)
4. ✅ Replace header markup in OntologyView with `<OntologyHeader>`
5. ✅ Write bUnit tests for OntologyHeader

**Commit**: `refactor: extract OntologyHeader component`

**Estimated Lines**: ~100

#### Day 2: OntologyToolbar.razor

**Tasks**:
1. ✅ Create `Components/Ontology/OntologyToolbar.razor`
2. ✅ Extract toolbar markup from OntologyView
3. ✅ Define parameters (CanEdit, CanManage, SearchTerm)
4. ✅ Define EventCallbacks (OnAddConceptClick, OnAddRelationshipClick, etc.)
5. ✅ Replace toolbar in OntologyView with `<OntologyToolbar>`
6. ✅ Wire up event handlers
7. ✅ Write bUnit tests for OntologyToolbar

**Commit**: `refactor: extract OntologyToolbar component`

**Estimated Lines**: ~150

#### Day 3: ViewTabs.razor

**Tasks**:
1. ✅ Create `Components/Ontology/ViewTabs.razor`
2. ✅ Extract tab navigation markup
3. ✅ Define parameters (CurrentView, OnViewChanged)
4. ✅ Replace tabs in OntologyView with `<ViewTabs>`
5. ✅ Write bUnit tests for ViewTabs

**Commit**: `refactor: extract ViewTabs component`

**Estimated Lines**: ~80

#### Day 4: Testing and Documentation

**Tasks**:
1. ✅ Run full test suite
2. ✅ Manual testing of all features
3. ✅ Update documentation
4. ✅ Code review

**Verification**:
- All tests pass
- No visual regressions
- Components render correctly on desktop and mobile

#### Day 5: Buffer for Issues

**Tasks**:
- Address any issues found during testing
- Refactor if needed
- Prepare for Week 3

---

### Week 3: View Panels (Part 1)

**Duration**: 5 days

#### Day 1: GraphViewPanel.razor

**Tasks**:
1. ✅ Create `Components/Ontology/GraphViewPanel.razor`
2. ✅ Extract graph view markup from OntologyView
3. ✅ Define parameters (Concepts, Relationships, Individuals, GraphState)
4. ✅ Define EventCallbacks (OnNodeSelected, OnEdgeSelected)
5. ✅ Handle JS interop for graph initialization
6. ✅ Replace graph view in OntologyView with `<GraphViewPanel>`
7. ✅ Write tests for GraphViewPanel (mock JS interop)

**Commit**: `refactor: extract GraphViewPanel component`

**Estimated Lines**: ~300

**Note**: This is the most complex component due to JS interop

#### Day 2: ListViewPanel.razor

**Tasks**:
1. ✅ Create `Components/Ontology/ListViewPanel.razor`
2. ✅ Extract list view markup from OntologyView
3. ✅ Define parameters (Concepts, Relationships, SearchTerm, CanEdit)
4. ✅ Define EventCallbacks (OnConceptSelected, OnEditClick, OnDeleteClick)
5. ✅ Replace list view with `<ListViewPanel>`
6. ✅ Write bUnit tests for ListViewPanel

**Commit**: `refactor: extract ListViewPanel component`

**Estimated Lines**: ~250

#### Day 3: HierarchyViewPanel.razor

**Tasks**:
1. ✅ Create `Components/Ontology/HierarchyViewPanel.razor`
2. ✅ Create `Components/Ontology/ConceptTreeNode.razor` (helper component)
3. ✅ Extract hierarchy view markup
4. ✅ Define parameters (Concepts, Relationships, OnConceptSelected)
5. ✅ Implement recursive tree rendering
6. ✅ Replace hierarchy view with `<HierarchyViewPanel>`
7. ✅ Write tests for HierarchyViewPanel

**Commit**: `refactor: extract HierarchyViewPanel component`

**Estimated Lines**: ~200 (150 for panel + 50 for tree node)

#### Day 4: TtlViewPanel.razor

**Tasks**:
1. ✅ Create `Components/Ontology/TtlViewPanel.razor`
2. ✅ Extract TTL view markup
3. ✅ Define parameters (OntologyId, CanEdit)
4. ✅ Define EventCallbacks (OnSaveClick, OnDownloadClick)
5. ✅ Handle TTL loading and editing
6. ✅ Replace TTL view with `<TtlViewPanel>`
7. ✅ Write tests for TtlViewPanel

**Commit**: `refactor: extract TtlViewPanel component`

**Estimated Lines**: ~150

#### Day 5: Integration and Testing

**Tasks**:
1. ✅ Verify all view modes work correctly
2. ✅ Test switching between views
3. ✅ Run full test suite
4. ✅ Manual testing on desktop and mobile
5. ✅ Update documentation

**Verification**:
- All views render correctly
- View switching is smooth
- No data loss when switching views
- All tests pass

---

### Week 4: Dialogs

**Duration**: 5 days

#### Day 1: AddConceptDialog.razor

**Tasks**:
1. ✅ Create `Components/Ontology/AddConceptDialog.razor`
2. ✅ Extract concept dialog markup
3. ✅ Define parameters (IsVisible, Concept, OntologyId)
4. ✅ Define EventCallbacks (OnSave, OnCancel)
5. ✅ Implement form validation
6. ✅ Implement "Save & Add Another" functionality
7. ✅ Replace dialog in OntologyView with `<AddConceptDialog>`
8. ✅ Write bUnit tests for AddConceptDialog

**Commit**: `refactor: extract AddConceptDialog component`

**Estimated Lines**: ~200

#### Day 2: AddRelationshipDialog.razor

**Tasks**:
1. ✅ Create `Components/Ontology/AddRelationshipDialog.razor`
2. ✅ Extract relationship dialog markup
3. ✅ Define parameters (IsVisible, Relationship, Concepts, OntologyId)
4. ✅ Define EventCallbacks (OnSave, OnCancel)
5. ✅ Implement form validation
6. ✅ Replace dialog with `<AddRelationshipDialog>`
7. ✅ Write bUnit tests

**Commit**: `refactor: extract AddRelationshipDialog component`

**Estimated Lines**: ~180

#### Day 3: AddIndividualDialog.razor

**Tasks**:
1. ✅ Create `Components/Ontology/AddIndividualDialog.razor`
2. ✅ Extract individual dialog markup
3. ✅ Define parameters (IsVisible, Individual, Concepts, OntologyId)
4. ✅ Define EventCallbacks (OnSave, OnCancel)
5. ✅ Implement form validation
6. ✅ Replace dialog with `<AddIndividualDialog>`
7. ✅ Write bUnit tests

**Commit**: `refactor: extract AddIndividualDialog component`

**Estimated Lines**: ~150

#### Day 4: ConfirmDeleteDialog.razor

**Tasks**:
1. ✅ Create `Components/Shared/ConfirmDeleteDialog.razor` (in Shared, as it's reusable)
2. ✅ Extract delete confirmation markup
3. ✅ Define parameters (IsVisible, EntityName, WarningMessage)
4. ✅ Define EventCallbacks (OnConfirm, OnCancel)
5. ✅ Replace confirmations with `<ConfirmDeleteDialog>`
6. ✅ Write bUnit tests

**Commit**: `refactor: extract ConfirmDeleteDialog component`

**Estimated Lines**: ~80

#### Day 5: Phase 2 Completion

**Tasks**:
1. ✅ Run full test suite
2. ✅ Full manual testing of all dialogs
3. ✅ Test mobile responsiveness
4. ✅ Update development ledger
5. ✅ Demo to team/stakeholders
6. ✅ Phase 2 retrospective

**Verification**:
- All dialogs work correctly
- Form validation works
- Save and cancel operations function properly
- All tests pass
- No regressions

---

## Phase 3: Code-Behind Partial Classes (Weeks 5-6)

**Goal**: Organize business logic into focused partial classes.

### Week 5: Core Partial Classes

**Duration**: 5 days

#### Day 1: OntologyView.Lifecycle.cs

**Tasks**:
1. ✅ Create `Components/Pages/OntologyView.Lifecycle.cs`
2. ✅ Move lifecycle methods (`OnInitializedAsync`, `OnParametersSetAsync`, `OnAfterRenderAsync`, `DisposeAsync`)
3. ✅ Ensure proper initialization order
4. ✅ Test component lifecycle
5. ✅ Write integration tests

**Commit**: `refactor: extract lifecycle partial class`

**Estimated Lines**: ~150

#### Day 2: OntologyView.Permissions.cs

**Tasks**:
1. ✅ Create `Components/Pages/OntologyView.Permissions.cs`
2. ✅ Move permission checking methods (`LoadPermissionsAsync`, `EnsureCanEditAsync`, etc.)
3. ✅ Test permission checks
4. ✅ Write integration tests with mock permission service

**Commit**: `refactor: extract permissions partial class`

**Estimated Lines**: ~100

#### Day 3: OntologyView.SignalR.cs

**Tasks**:
1. ✅ Create `Components/Pages/OntologyView.SignalR.cs`
2. ✅ Move SignalR connection initialization
3. ✅ Move hub event handlers (`OnConceptAddedAsync`, `OnConceptUpdatedAsync`, etc.)
4. ✅ Test SignalR integration
5. ✅ Write integration tests with mock hub connection

**Commit**: `refactor: extract SignalR partial class`

**Estimated Lines**: ~200

#### Day 4: OntologyView.GraphOperations.cs

**Tasks**:
1. ✅ Create `Components/Pages/OntologyView.GraphOperations.cs`
2. ✅ Move graph initialization methods
3. ✅ Move graph update methods
4. ✅ Move layout change methods
5. ✅ Move node/edge selection handlers
6. ✅ Test graph operations
7. ✅ Write tests with mock JS interop

**Commit**: `refactor: extract graph operations partial class`

**Estimated Lines**: ~250

#### Day 5: Testing and Documentation

**Tasks**:
1. ✅ Run full test suite
2. ✅ Manual testing
3. ✅ Update documentation
4. ✅ Code review

---

### Week 6: Remaining Partial Classes

**Duration**: 5 days

#### Day 1: OntologyView.Dialogs.cs

**Tasks**:
1. ✅ Create `Components/Pages/OntologyView.Dialogs.cs`
2. ✅ Move dialog visibility state
3. ✅ Move dialog open/close methods
4. ✅ Move dialog save handlers
5. ✅ Test dialog interactions

**Commit**: `refactor: extract dialogs partial class`

**Estimated Lines**: ~150

#### Day 2: OntologyView.Export.cs

**Tasks**:
1. ✅ Create `Components/Pages/OntologyView.Export.cs`
2. ✅ Move TTL export methods
3. ✅ Move image export methods
4. ✅ Move PDF export stubs
5. ✅ Test export functionality

**Commit**: `refactor: extract export partial class`

**Estimated Lines**: ~150

#### Day 3: OntologyView.Search.cs

**Tasks**:
1. ✅ Create `Components/Pages/OntologyView.Search.cs`
2. ✅ Move search term property
3. ✅ Move filtered concepts/relationships properties
4. ✅ Move search filter logic
5. ✅ Test search filtering

**Commit**: `refactor: extract search partial class`

**Estimated Lines**: ~100

#### Day 4: OntologyView.Keyboard.cs

**Tasks**:
1. ✅ Create `Components/Pages/OntologyView.Keyboard.cs`
2. ✅ Move keyboard shortcut registration
3. ✅ Move keyboard shortcut handlers
4. ✅ Test keyboard shortcuts

**Commit**: `refactor: extract keyboard partial class`

**Estimated Lines**: ~100

#### Day 5: Phase 3 Completion

**Tasks**:
1. ✅ Verify all partial classes compile correctly
2. ✅ Run full test suite
3. ✅ Full manual testing
4. ✅ Update development ledger
5. ✅ Demo to team/stakeholders
6. ✅ Phase 3 retrospective

**Verification**:
- All partial classes work together
- No namespace or access modifier issues
- All tests pass
- Application functions correctly

---

## Phase 4: Main Component Refactoring (Week 7)

**Goal**: Refactor main component to orchestrate extracted components, reducing from 3,184 lines to ~400 lines.

### Week 7: Final Refactoring

**Duration**: 5 days

#### Day 1-2: Refactor OntologyView.razor

**Tasks**:
1. ✅ Remove all extracted markup (now in components)
2. ✅ Remove all extracted logic (now in partial classes)
3. ✅ Keep only orchestration logic
4. ✅ Simplify to component composition
5. ✅ Add comprehensive XML documentation
6. ✅ Verify component still works

**Commit**: `refactor: simplify main OntologyView component`

**Target Lines**: ~400 (87% reduction from 3,184)

**Verification**:
- Main component is concise and readable
- All functionality preserved
- Build succeeds
- All tests pass

#### Day 3: Comprehensive Testing

**Tasks**:
1. ✅ Run full test suite (157+ tests)
2. ✅ Manual testing of all features
3. ✅ Cross-browser testing (Chrome, Firefox, Safari, Edge)
4. ✅ Mobile testing (iOS Safari, Android Chrome)
5. ✅ Performance testing (load time, rendering speed)
6. ✅ Accessibility testing (keyboard navigation, screen readers)

**Verification Checklist**:
- [ ] Graph view renders correctly
- [ ] List view displays all concepts and relationships
- [ ] Hierarchy view shows tree structure
- [ ] TTL view loads and displays correctly
- [ ] Adding concepts works
- [ ] Adding relationships works
- [ ] Adding individuals works
- [ ] Editing works
- [ ] Deleting works with confirmation
- [ ] Search and filtering work
- [ ] Permissions are enforced
- [ ] SignalR collaboration works
- [ ] Presence tracking works
- [ ] Export works (TTL, image)
- [ ] Keyboard shortcuts work
- [ ] Mobile layout is responsive
- [ ] No console errors
- [ ] No visual regressions

#### Day 4: Documentation and Polish

**Tasks**:
1. ✅ Update all XML documentation
2. ✅ Update development ledger with final summary
3. ✅ Create before/after comparison screenshots
4. ✅ Update architecture documentation
5. ✅ Write migration guide for developers
6. ✅ Polish any rough edges

**Deliverables**:
- Complete XML documentation for all new components
- Updated development ledger
- Before/after metrics
- Developer migration guide

#### Day 5: Release and Retrospective

**Tasks**:
1. ✅ Final demo to team/stakeholders
2. ✅ Merge to main branch (with team approval)
3. ✅ Deploy to staging for additional testing
4. ✅ Retrospective meeting
5. ✅ Celebrate success

**Retrospective Topics**:
- What went well?
- What could be improved?
- What did we learn?
- How can we apply these lessons to future refactorings?
- Update architectural decision records based on learnings

---

## Success Metrics

### Quantitative Metrics

| Metric | Before | After | Target Met? |
|--------|--------|-------|-------------|
| Main File Size | 3,184 lines | ~400 lines | ✅ 87% reduction |
| Number of Files | 1 monolithic | 26 focused | ✅ Better organization |
| Average File Size | N/A | ~165 lines | ✅ Manageable size |
| Test Coverage | 157+ tests | 180+ tests | ✅ Increased coverage |
| Build Time | X seconds | Similar | ✅ No degradation |
| Load Time | Y seconds | Similar | ✅ No degradation |

### Qualitative Metrics

- **Readability**: ✅ Dramatically improved (focused files vs. 3,000-line monolith)
- **Maintainability**: ✅ Much easier (clear component boundaries)
- **Testability**: ✅ Significantly better (isolated components)
- **Reusability**: ✅ Components can be reused in other contexts
- **Onboarding**: ✅ New developers can understand individual components

---

## Risk Mitigation

### Identified Risks

1. **Breaking Changes**
   - Mitigation: Comprehensive testing after each extraction
   - Rollback plan: Git revert to last working commit

2. **SignalR Issues**
   - Mitigation: Careful testing of real-time collaboration
   - Fallback: Keep SignalR code as-is until fully tested

3. **JavaScript Interop Issues**
   - Mitigation: Mock JS interop in tests, verify in browser
   - Fallback: Keep graph operations in main component if needed

4. **Performance Degradation**
   - Mitigation: Benchmark before and after
   - Optimization: Use memoization if component re-renders too often

5. **Merge Conflicts**
   - Mitigation: Communicate with team, small frequent commits
   - Strategy: Work in feature branch, merge frequently from main

---

## Contingency Plans

### If We Fall Behind Schedule

1. **Skip less critical components** (e.g., keyboard shortcuts can wait)
2. **Extend timeline** by 1-2 weeks if needed
3. **Pair programming** to accelerate difficult extractions
4. **Reduce test coverage** temporarily (catch up later)

### If Critical Bug Found

1. **Stop refactoring** immediately
2. **Fix bug** in current state
3. **Verify fix** with tests
4. **Resume refactoring** once stable

### If Design Needs Revision

1. **Pause implementation** after current phase
2. **Review architecture decisions** with team
3. **Update plan** based on learnings
4. **Resume with revised approach**

---

## Communication Plan

### Daily Standups
- Report progress on current component
- Highlight any blockers
- Commit frequently for transparency

### Weekly Demos
- Demo completed phase to team
- Gather feedback
- Adjust plan if needed

### End-of-Phase Reviews
- Comprehensive demo of phase results
- Retrospective on what worked/what didn't
- Plan adjustments for next phase

---

## Post-Refactoring

### Immediate Follow-up (Week 8)
- Monitor production for any issues
- Address any bugs found by users
- Performance monitoring

### Long-term Maintenance
- Keep components focused (resist adding too much to any one file)
- Write tests for new features
- Refactor further if new components exceed 300 lines
- Apply lessons learned to other large components (e.g., Dashboard.razor)

---

**Status**: Ready to begin Phase 1 on approval
**Next Milestone**: Complete Phase 1 by end of Week 1
**Final Milestone**: Complete refactoring by end of Week 7
