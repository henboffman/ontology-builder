# Architecture Decisions - OntologyView Refactoring

**Last Updated**: February 2025

---

## Overview

This document captures the key architectural decisions made during the OntologyView refactoring initiative. Each decision includes the context, alternatives considered, the chosen approach, and the rationale.

---

## ADR-001: State Management Approach

**Status**: Accepted

**Context**:
The original OntologyView component had 50+ state fields scattered throughout the code with no clear ownership or organization. This made it difficult to track state changes and understand data flow.

**Decision**:
We will extract state into two dedicated classes:
1. **OntologyViewState**: General component state (ontology data, view mode, selections, permissions)
2. **GraphViewState**: Graph-specific state (layout, filters, visibility toggles)

**Alternatives Considered**:

1. **Keep state in main component**
   - ❌ Rejected: Continues existing problems, no improvement in maintainability

2. **Use Flux/Redux pattern with central store**
   - ❌ Rejected: Overkill for component-level state, adds unnecessary complexity and boilerplate
   - Would require actions, reducers, and middleware

3. **Use Blazor State Management libraries (Fluxor, Blazor-State)**
   - ❌ Rejected: External dependency for localized problem, learning curve for team
   - These are better suited for app-wide state

4. **Extract to two focused state classes (CHOSEN)**
   - ✅ Selected: Simple, testable, no external dependencies
   - Clear ownership of related state
   - Easy to extend with methods and events
   - Follows Single Responsibility Principle

**Rationale**:
- Two state classes provide clear separation between general view state and graph-specific state
- Simple C# classes are easy to understand and test
- No external dependencies or learning curve
- Can easily add computed properties and state validation
- Event-based notification (`OnStateChanged`) allows components to react to state changes

**Consequences**:
- State classes will need to be instantiated in main component
- Child components will receive state as parameters
- Need to manually call `NotifyStateChanged()` after mutations
- State is not automatically persisted (acceptable for this use case)

---

## ADR-002: Component Decomposition Strategy

**Status**: Accepted

**Context**:
The 3,184-line OntologyView.razor file needed to be broken into smaller, manageable pieces. We needed a strategy for determining component boundaries.

**Decision**:
Decompose using a three-tier strategy:
1. **UI Components**: Standalone, reusable UI elements (header, toolbar, view panels, dialogs)
2. **Code-Behind Partial Classes**: Business logic grouped by functional area (lifecycle, permissions, SignalR, etc.)
3. **State Classes**: Data and state management

**Alternatives Considered**:

1. **Feature-based decomposition** (all files for one feature together)
   - ❌ Rejected: Doesn't address the main component's size, just moves files around
   - Example: `Features/Concepts/AddConceptDialog.razor`, `Features/Concepts/ConceptList.razor`

2. **Pure component extraction** (only UI components, keep logic in main file)
   - ❌ Rejected: Doesn't reduce main component complexity significantly
   - Logic would still be scattered across 2,000+ lines

3. **Microservices-style** (each view mode as separate page)
   - ❌ Rejected: Breaks the unified experience, would require significant routing changes
   - Sharing state across "microservice" pages is difficult

4. **Three-tier decomposition (CHOSEN)**
   - ✅ Selected: Addresses both UI and logic complexity
   - UI components are reusable and testable
   - Partial classes organize logic without changing the public API
   - State classes provide clean data management

**Rationale**:
- **UI Components** handle rendering and user interaction, can be reused and tested independently
- **Partial Classes** organize business logic by concern (easier to find related code)
- **State Classes** centralize data, making it easier to reason about state changes
- This approach maintains the single-page experience while dramatically improving maintainability

**Consequences**:
- More files to navigate (26 vs 1), but each file is focused and manageable
- Component tree is deeper, but composition is explicit and understandable
- Partial classes require careful attention to method naming to avoid conflicts
- IDE support for partial classes is excellent in Visual Studio/Rider

---

## ADR-003: Partial Classes vs Full Separation

**Status**: Accepted

**Context**:
Business logic (lifecycle, permissions, SignalR, graph operations) needed to be extracted from the main component. We had to decide between partial classes (keeping the same class name) or separate service classes.

**Decision**:
Use partial classes to organize business logic into focused files while maintaining a single `OntologyView` class.

**Alternatives Considered**:

1. **Separate service classes**
   - ❌ Rejected: Would need dependency injection for each service
   - Would require passing state and callbacks around
   - Example: `OntologyViewSignalRService`, `OntologyViewPermissionService`

2. **Nested classes**
   - ❌ Rejected: Awkward syntax, doesn't solve file size problem
   - Example: `OntologyView.SignalR`, `OntologyView.Permissions` as nested classes in same file

3. **Extension methods**
   - ❌ Rejected: Cannot access private fields, limits encapsulation
   - Would need to make many fields public

4. **Partial classes (CHOSEN)**
   - ✅ Selected: Organizes code into multiple files while maintaining single class
   - All partial classes share the same private fields and methods
   - No DI or boilerplate needed
   - IDE support is excellent (Go To Definition works across files)

**Rationale**:
- Partial classes are a natural fit for organizing large Blazor components
- Maintains the component's public API (still a single `OntologyView` class)
- All partial class files can access the same state, services, and methods
- No performance overhead (compiled into single class)
- Clear file naming convention: `OntologyView.{Concern}.cs`

**Consequences**:
- Developers need to understand that multiple files contribute to the same class
- Careful naming needed to avoid method conflicts across partial files
- All partial files must be in the same namespace and have matching class modifiers
- Each partial file should focus on a single concern (aligned with SRP)

---

## ADR-004: Dialog Component Extraction

**Status**: Accepted

**Context**:
The original component had inline dialog markup duplicated for desktop and mobile views. We needed to extract dialogs into reusable components.

**Decision**:
Extract all dialogs (AddConcept, AddRelationship, AddIndividual, ConfirmDelete) as separate Razor components with clear parameter contracts.

**Alternatives Considered**:

1. **Keep dialogs inline in main component**
   - ❌ Rejected: Contributes to file size, duplicated markup for desktop/mobile

2. **Use generic dialog component with slots**
   - ❌ Rejected: Too flexible, loses type safety
   - Example: `<GenericDialog><RenderFragment>...</RenderFragment></GenericDialog>`

3. **Extract dialog content only, keep container inline**
   - ❌ Rejected: Doesn't reduce duplication enough, split responsibility

4. **Extract complete dialog components (CHOSEN)**
   - ✅ Selected: Clear boundaries, reusable, testable
   - Each dialog is self-contained with its own validation logic
   - Can be tested independently with bUnit

**Rationale**:
- Dialogs are natural component boundaries (modal UI, form logic, validation)
- Each dialog has clear inputs (entity to edit, list of concepts) and outputs (save/cancel events)
- Extracting dialogs eliminates desktop/mobile duplication
- Dialog components can be reused elsewhere if needed

**Consequences**:
- Need to manage dialog visibility state in parent component
- Dialog save handlers need to be in parent (to access services and state)
- Each dialog needs well-defined parameter contract
- Using Eidos.Components.Shared.Modal for consistent styling

---

## ADR-005: View Panel Organization

**Status**: Accepted

**Context**:
The component supports four view modes (Graph, List, Hierarchy, TTL). We needed to decide how to organize the view-specific UI.

**Decision**:
Extract each view mode into a dedicated panel component:
- `GraphViewPanel.razor`
- `ListViewPanel.razor`
- `HierarchyViewPanel.razor`
- `TtlViewPanel.razor`

Main component uses `@switch` statement to render the appropriate panel based on `CurrentView`.

**Alternatives Considered**:

1. **Keep all views inline with @if blocks**
   - ❌ Rejected: Main file remains too large, hard to maintain

2. **Dynamic component loading**
   - ❌ Rejected: Runtime overhead, loses compile-time type safety
   - Example: `<DynamicComponent Type="@currentViewType" Parameters="@parameters" />`

3. **Separate pages per view mode**
   - ❌ Rejected: Breaks single-page experience, complicates navigation and state sharing

4. **Dedicated panel components with @switch (CHOSEN)**
   - ✅ Selected: Clean separation, compile-time type safety, no overhead
   - Each view is self-contained and testable
   - Easy to add new views in the future

**Rationale**:
- Each view mode has distinct rendering logic and user interactions
- Panel components can be tested independently
- `@switch` statement in main component is concise and explicit
- No runtime overhead compared to dynamic components
- Easy to pass view-specific parameters to each panel

**Consequences**:
- Need to pass common state (concepts, relationships) to each panel
- Event callbacks must be consistent across panels
- Each panel may have different parameters based on its needs
- GraphViewPanel is most complex (JS interop), others are simpler

---

## ADR-006: JavaScript Interop Approach

**Status**: Accepted

**Context**:
The graph visualization uses Cytoscape.js (JavaScript library). We needed to decide how to handle JS interop in the refactored architecture.

**Decision**:
Keep JS interop calls in `OntologyView.GraphOperations.cs` partial class. Do not create a separate JS interop service.

**Alternatives Considered**:

1. **Create CytoscapeJsService for JS interop**
   - ❌ Rejected: Adds layer of indirection without significant benefit
   - Would need to inject service into component

2. **Move JS interop to GraphViewPanel component**
   - ❌ Rejected: Panel becomes less reusable, harder to test
   - Would need to expose more events and state synchronization

3. **Keep JS interop in partial class (CHOSEN)**
   - ✅ Selected: Simple, direct access to component state
   - Partial class has access to all component fields and methods
   - Easy to call `StateHasChanged()` after JS operations

**Rationale**:
- JS interop is tightly coupled to component state and lifecycle
- Partial class approach keeps all graph operations in one place
- No additional DI or boilerplate needed
- GraphViewPanel focuses on markup, partial class handles logic

**Consequences**:
- GraphViewPanel delegates graph initialization to parent component via events
- Parent component (`OntologyView.GraphOperations.cs`) manages Cytoscape.js instance
- Need to ensure JS resources are properly disposed in `DisposeAsync`
- JS file (`graphVisualization.js`) remains unchanged

---

## ADR-007: SignalR Integration Pattern

**Status**: Accepted

**Context**:
The component uses SignalR for real-time collaboration. We needed to decide how to integrate SignalR in the refactored architecture.

**Decision**:
Keep SignalR hub connection and event handlers in `OntologyView.SignalR.cs` partial class. Initialize connection in `OnInitializedAsync` and dispose in `DisposeAsync`.

**Alternatives Considered**:

1. **Create SignalRService wrapper**
   - ❌ Rejected: Adds abstraction without clear benefit
   - Still need to subscribe/unsubscribe to events in component

2. **Move SignalR to separate background service**
   - ❌ Rejected: Complicates state synchronization
   - Component would need to subscribe to service events

3. **Keep in partial class (CHOSEN)**
   - ✅ Selected: Direct access to state, simple lifecycle management
   - Hub events can directly update state and call `StateHasChanged()`
   - Lifecycle is managed in `Lifecycle.cs` partial class

**Rationale**:
- SignalR events trigger state changes and UI updates
- Partial class has direct access to state classes and services
- No performance overhead or additional abstractions
- Clear separation in `SignalR.cs` makes it easy to find collaboration code

**Consequences**:
- Must ensure hub connection is properly disposed
- Hub event handlers must be async-safe (use `InvokeAsync` for state changes)
- SignalR lifecycle is tied to component lifecycle
- Connection state is part of component state

---

## ADR-008: Testing Strategy

**Status**: Accepted

**Context**:
The refactored components need to be testable. We needed to decide on testing approaches for different component types.

**Decision**:
Use layered testing strategy:
1. **Unit tests** for state classes (OntologyViewState, GraphViewState)
2. **Component tests** with bUnit for UI components (dialogs, panels)
3. **Integration tests** for partial classes that use services
4. **Mock JS interop** for graph operations

**Alternatives Considered**:

1. **E2E tests only (Playwright/Selenium)**
   - ❌ Rejected: Slow, brittle, hard to maintain
   - Doesn't provide fast feedback for individual components

2. **No testing** (manual testing only)
   - ❌ Rejected: High regression risk, slows development

3. **Unit tests only**
   - ❌ Rejected: Doesn't test component integration or rendering

4. **Layered testing (CHOSEN)**
   - ✅ Selected: Balances speed, coverage, and maintainability
   - Fast unit tests for state logic
   - bUnit tests for component rendering and interaction
   - Integration tests for service dependencies

**Rationale**:
- State classes are pure C# - easy to unit test
- UI components can be tested with bUnit (rendering, events, parameters)
- Partial classes that use services need integration tests with mocks
- JS interop can be mocked in tests (bUnit supports this)
- Maintain 100% test pass rate throughout refactoring

**Consequences**:
- Need to write tests for each new component as it's created
- May need to create test helpers for common setups
- Mock SignalR hub connection in tests
- Use TestContext with dependency injection for service mocks

---

## ADR-009: Component Communication Pattern

**Status**: Accepted

**Context**:
Child components need to communicate with parent component (e.g., button click in toolbar should open dialog in parent).

**Decision**:
Use **EventCallback** parameters for child-to-parent communication. Parent component passes callbacks to children, which invoke them when events occur.

**Alternatives Considered**:

1. **Pub/Sub event bus**
   - ❌ Rejected: Overkill for parent-child communication
   - Harder to trace event flow

2. **Two-way binding** for everything
   - ❌ Rejected: Not all data flows both ways
   - Two-way binding (`@bind-Value`) is appropriate for form fields, not actions

3. **Cascading parameters**
   - ❌ Rejected: Breaks component encapsulation
   - Makes it hard to reuse components in different contexts

4. **EventCallback parameters (CHOSEN)**
   - ✅ Selected: Standard Blazor pattern, explicit contracts
   - Easy to trace event flow from child to parent
   - Type-safe with `EventCallback<T>`

**Rationale**:
- EventCallback is the idiomatic Blazor approach
- Explicit in component API (clear which events a component raises)
- Strongly typed with `EventCallback<T>`
- Supports async handlers naturally
- Easy to test (can verify callback was invoked)

**Consequences**:
- Parent component needs to define handlers for all child events
- Large number of event callbacks in some components (e.g., OntologyToolbar)
- Need to remember to call `await EventCallback.InvokeAsync()` in child components
- Cannot easily share events across deeply nested components (use cascading parameters for that)

---

## ADR-010: Dependency Injection for Components

**Status**: Accepted

**Context**:
Components need access to services (OntologyService, ConceptService, etc.). We needed to decide where to inject services.

**Decision**:
- **Main component** (`OntologyView.razor`) injects all required services
- **UI components** (panels, dialogs) receive data via parameters and raise events to parent
- **Dialogs** inject services only when they handle their own save logic

**Alternatives Considered**:

1. **Inject services into every component**
   - ❌ Rejected: Makes components less reusable and harder to test
   - Couples components to service implementations

2. **No service injection, pass everything as parameters**
   - ❌ Rejected: Parameter lists become unwieldy
   - Dialogs would need to pass entities to parent for saving

3. **Hybrid approach (CHOSEN)**
   - ✅ Selected: Main component orchestrates, UI components focus on presentation
   - Dialogs inject services to keep their logic self-contained
   - UI panels are pure presentation (data in, events out)

**Rationale**:
- Main component is the natural orchestration point
- UI panels (List, Hierarchy) don't need services - they just display data
- Dialogs handle their own validation and save logic for encapsulation
- Reduces coupling in presentation components
- Makes UI components easier to test (just pass test data)

**Consequences**:
- Main component has many service injections
- Parent component is responsible for reloading data after changes
- Dialog components need to handle errors from service calls
- Need to mock services when testing dialogs

---

## ADR-011: Error Handling Strategy

**Status**: Accepted

**Context**:
Operations can fail (network errors, validation failures, permission denials). We needed a consistent error handling approach.

**Decision**:
- Use **try-catch** blocks around all service calls
- Display errors to user via **ToastService**
- Store critical errors in **OntologyViewState.ErrorMessage** for page-level display
- Log all errors with structured logging

**Alternatives Considered**:

1. **Global error boundary**
   - ❌ Rejected: Loses context about where error occurred
   - Cannot provide targeted error messages

2. **Return Result<T> objects instead of throwing**
   - ❌ Rejected: Services already use exceptions
   - Would require changing all service signatures

3. **Try-catch with toast + state (CHOSEN)**
   - ✅ Selected: User-friendly, consistent across app
   - Toast for transient errors (save failed, permission denied)
   - State.ErrorMessage for critical errors (ontology not found)

**Rationale**:
- ToastService is already used throughout app for notifications
- Users need immediate feedback when operations fail
- Critical errors (load failures) should be persistent on page
- Structured logging helps with troubleshooting in production

**Consequences**:
- Every service call needs try-catch wrapper
- Error messages must be user-friendly (not technical exceptions)
- Need to distinguish between transient and critical errors
- Console logs for developers, toasts for users

---

## ADR-012: Mobile Responsiveness Strategy

**Status**: Accepted

**Context**:
The application must work on mobile devices. The original component had duplicated markup for desktop/mobile views.

**Decision**:
- Use **CSS media queries** and **Bootstrap responsive classes** for layout adaptation
- **Single set of components** used for both desktop and mobile
- **Floating panel** for concept details on mobile (instead of sidebar)
- **Collapsible sections** for better mobile UX

**Alternatives Considered**:

1. **Separate components for desktop/mobile**
   - ❌ Rejected: Code duplication, maintenance burden

2. **Separate routes/pages for mobile**
   - ❌ Rejected: Complicates navigation, state management

3. **Responsive design with single component set (CHOSEN)**
   - ✅ Selected: DRY principle, consistent behavior
   - Bootstrap grid handles most layout changes
   - Floating panel provides mobile-optimized details view

**Rationale**:
- Modern CSS makes responsive design straightforward
- Bootstrap 5 has excellent mobile support out of the box
- Single component set is easier to maintain and test
- Floating panel is better UX for mobile than sidebar

**Consequences**:
- Need to test on multiple screen sizes
- Some components may need conditional rendering based on screen size
- JavaScript may need to detect screen size for certain behaviors
- Graph visualization needs touch-friendly controls

---

## Summary

These architectural decisions provide a solid foundation for the OntologyView refactoring. Key themes:

1. **Simplicity**: Prefer simple solutions over complex abstractions
2. **Standard Patterns**: Use idiomatic Blazor patterns (EventCallback, parameters, partial classes)
3. **Testability**: Design for testability with clear boundaries and dependencies
4. **Maintainability**: Organize code by concern, keep files focused
5. **No Over-Engineering**: Avoid premature abstraction and external dependencies

These decisions will be reviewed after Phase 1 implementation to validate assumptions and adjust if needed.

---

**Next Review**: After Phase 1 completion (state management classes implemented)
