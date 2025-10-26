# Eidos Development Ledger

**Purpose:** Track development decisions, patterns, styles, and features for future reference and consistency.

---

## Table of Contents
- [Architecture Patterns](#architecture-patterns)
- [UI/UX Patterns](#uiux-patterns)
- [Code Style Guidelines](#code-style-guidelines)
- [Feature History](#feature-history)
- [Current Capabilities](#current-capabilities)
- [Planned Enhancements](#planned-enhancements)

---

## Architecture Patterns

### Service Layer Pattern
- **Pattern**: Focused services with single responsibility
- **Location**: `Services/`
- **Structure**:
  - Interface in `Services/Interfaces/`
  - Implementation in `Services/`
  - Inject via DI in `Program.cs`
- **Example**: `ConceptService`, `RelationshipService`, `OntologyService`

### Repository Pattern
- **Pattern**: Generic base repository with specialized implementations
- **Location**: `Data/Repositories/`
- **Structure**:
  - `IRepository<T>` - Generic interface
  - `Repository<T>` - Generic base implementation
  - Specialized: `IOntologyRepository`, `IConceptRepository`, etc.
- **Key Decision**: Use DbContextFactory for Blazor Server concurrency

### Command Pattern (Undo/Redo)
- **Pattern**: Command pattern for reversible operations
- **Location**: `Services/Commands/`
- **Structure**:
  - `ICommand` interface with `Execute()` and `Undo()`
  - `CommandFactory` creates command instances
  - `CommandInvoker` manages undo/redo stacks
- **Limitation**: Max 50 operations per ontology

### Component Pattern
- **Pattern**: Reusable Blazor components with clear responsibilities
- **Location**: `Components/Shared/`, `Components/Pages/`
- **Render Mode**: `@rendermode="InteractiveServer"` for interactive components
- **Structure**:
  - Parameter properties with `[Parameter]`
  - EventCallbacks for parent communication
  - Local state management

---

## UI/UX Patterns

### Design System
- **Framework**: Bootstrap 5
- **Icons**: Bootstrap Icons (CDN)
- **Color Palette**: CSS custom properties in `app.css`
  - Primary: Blue (`--bs-primary`)
  - Success: Green (`--bs-success`)
  - Danger: Red (`--bs-danger`)
  - Warning: Yellow (`--bs-warning`)
  - Info: Cyan (`--bs-info`)

### Modal Dialogs
- **Pattern**: Bootstrap modals with Blazor component wrapping
- **Structure**:
  - `Show()` method to display
  - `Hide()` method to close
  - `OnConfirm` / `OnCancel` callbacks
- **Examples**: `ConfirmDialog`, `ForkCloneDialog`, `ExportDialog`

### Toast Notifications
- **Pattern**: Global toast service for user feedback
- **Service**: `ToastNotificationService`
- **Component**: `ToastNotification` in `MainLayout`
- **Usage**: Inject service, call `ShowSuccess()`, `ShowError()`, `ShowWarning()`, `ShowInfo()`

### Card-Based UI
- **Pattern**: Bootstrap cards for content containers
- **Structure**:
  - `.card` wrapper
  - `.card-header` for titles
  - `.card-body` for content
  - `.card-footer` for actions
- **Consistent across**: Home page, OntologyView, Settings

### Accordion Navigation
- **Pattern**: Bootstrap accordion for collapsible sections
- **Structure**:
  - `.accordion` wrapper with unique id
  - `.accordion-item` for each section
  - `.accordion-header` with button
  - `.accordion-collapse` for collapsible content
- **Usage**: Features page, complex forms

### Sidebar Navigation
- **Pattern**: Fixed sidebar with hierarchical navigation
- **Component**: `NavMenu`
- **Structure**:
  - Brand header
  - Navigation section
  - Recent ontologies (if authenticated)
  - User info footer

---

## Code Style Guidelines

### Naming Conventions
- **Classes/Interfaces**: PascalCase (`ConceptService`, `IOntologyRepository`)
- **Methods**: PascalCase (`CreateAsync`, `GetByIdAsync`)
- **Parameters**: camelCase (`ontologyId`, `recordUndo`)
- **Private fields**: camelCase with underscore (`_repository`, `_logger`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE

### Async Patterns
- **All I/O operations**: Use async/await
- **Method naming**: Suffix with `Async` (e.g., `CreateAsync`)
- **Cancellation**: Use `CancellationToken` for long operations
- **Current count**: 377+ async operations across services

### Nullable Reference Types
- **Enabled throughout**: `#nullable enable`
- **Null handling**: Explicit null checks and null-forgiving operators only when safe
- **Return types**: Use `?` for nullable returns (`Concept?`, `string?`)

### Error Handling
- **Custom exceptions**: 8 exception types in `Exceptions/`
- **User-friendly messages**: All exceptions include context
- **Global handler**: Exception middleware with correlation IDs
- **Logging**: Structured logging with ILogger

### Documentation
- **XML comments**: All public members
- **Inline comments**: Complex logic only
- **README files**: For major features and setup
- **Format**:
  ```csharp
  /// <summary>
  /// Brief description of what this does
  /// </summary>
  /// <param name="paramName">Description</param>
  /// <returns>Description of return value</returns>
  ```

---

## Feature History

### Phase 1 Refactoring (October 2024)
**Goal**: Break monolithic OntologyService into focused services

**Changes**:
- Created `ConceptService` - Concept CRUD operations
- Created `RelationshipService` - Relationship CRUD operations
- Created `PropertyService` - Property management
- Created `OntologyShareService` - Sharing and permissions
- Reduced `OntologyService` by 75%
- Improved testability and maintainability

**Pattern Established**: Single Responsibility Principle, Service Layer Pattern

### Phase 2 Refactoring (October 2024)
**Goal**: Further service decomposition and separation of concerns

**Changes**:
- Created `RelationshipSuggestionService` - BFO-based suggestions
- Created `UserService` - User management
- Extracted additional concerns from OntologyService
- Improved dependency injection structure

**Pattern Established**: Interface Segregation, Dependency Inversion

### Testing Infrastructure (October 2024)
**Goal**: Comprehensive test coverage

**Additions**:
- Repository integration tests (19 tests)
- Service unit tests with mocks (58 tests)
- Service integration tests with real database (25 tests)
- Workflow end-to-end tests (7 tests)
- Blazor component tests (36 tests)
- **Total**: 137 tests, 100% pass rate, 845ms execution

**Pattern Established**: Layered testing (unit, integration, workflow)

### Features Page (October 2024)
**Goal**: Showcase application capabilities

**Addition**:
- Created `/features` page with accordion navigation
- Documented 100+ quality characteristics
- Added to sidebar navigation
- Loaded Bootstrap JavaScript for interactive components

**Pattern Established**: Accordion navigation for complex content

---

## Current Capabilities

### Ontology Management
✅ **CRUD operations** for ontologies
✅ **Rich metadata**: Name, Description, Namespace, Author, License, Version, Tags, Notes
✅ **Framework tracking**: BFO, PROV-O flags
✅ **Provenance tracking**: Fork/Clone with lineage
✅ **Multi-ontology support**: Unlimited ontologies per user

### Concept Management
✅ **CRUD operations** for concepts
✅ **Metadata**: Name, Definition, Simple Explanation, Examples, Category, Color
✅ **Visual positioning**: Persistent X/Y coordinates
✅ **Custom templates**: Reusable by category and type
✅ **BFO templates**: Pre-built ontology framework templates
✅ **Search**: Real-time search across all fields

### Relationship Management
✅ **CRUD operations** for relationships
✅ **17+ standard types**: RDF/RDFS, OWL, BFO, RO
✅ **Custom labels**: Override display labels
✅ **Relationship strength**: Optional 0.0-1.0 values
✅ **Ontology URI mapping**: Link to standard ontologies
✅ **Smart suggestions**: BFO pattern-based

### Property Management
✅ **Name-value pairs**: Extensible properties for concepts
✅ **Multiple data types**: String, number, boolean, date
✅ **Descriptions**: Document property meanings

### Data Management
✅ **Undo/Redo**: Command pattern with 6 command types
✅ **Import/Export**: 5 formats (TTL, JSON, CSV×3)
✅ **TTL import**: Parse and import Turtle files
✅ **Auto-save**: Automatic persistence

### Collaboration
✅ **SignalR hub**: Real-time collaborative editing
✅ **Share links**: Cryptographically secure tokens
✅ **4 permission levels**: View, Comment, Edit, Admin
✅ **Guest access**: Unauthenticated users via share links
✅ **Access tracking**: Analytics and monitoring

### Visualization
✅ **5 view modes**: Graph, List, TTL, Notes, Templates
✅ **D3.js graph**: Force-directed layout with drag-and-drop
✅ **Cytoscape integration**: Alternative graph rendering
✅ **Color coding**: Custom colors per concept
✅ **Position persistence**: Save node coordinates

### Security
✅ **OAuth 2.0**: GitHub, Google, Microsoft
✅ **ASP.NET Identity**: Enterprise authentication
✅ **Rate limiting**: 4 endpoint-specific rules
✅ **6 security headers**: CSP, HSTS, X-Frame-Options, etc.
✅ **Security auditing**: Comprehensive event logging

### Accessibility
✅ **WCAG 2.1 compliant**: Full accessibility support
✅ **14+ keyboard shortcuts**: Productivity features
✅ **ARIA attributes**: 20+ roles and attributes
✅ **Text scaling**: 50-150% customizable
✅ **Screen reader support**: Complete semantic markup

---

## Planned Enhancements

### Feature Parity with Protege

**Status**: Planning
**Date**: 2024-10-25
**Goal**: Achieve full ontology management capabilities while maintaining simplicity

#### Missing Features Analysis

##### 1. Hierarchical Concept Relationships ⭐ HIGH PRIORITY
**Current State**: Relationships exist but no explicit parent/child hierarchy
**Needed**:
- Concept hierarchy tree view (parent/child relationships)
- `subClassOf` relationship type (already have it in relationship types)
- Tree navigation in UI
- Expand/collapse hierarchy
- Visual indication of depth level

**Implementation Notes**:
- Add `ParentConceptId` to Concept model? Or use Relationships with `subClassOf` type?
- Tree component in Shared/
- Recursive loading of children
- Drag-and-drop to reorganize hierarchy

##### 2. Concept Restrictions & Axioms ⭐ HIGH PRIORITY
**Current State**: Basic concept definitions only
**Needed**:
- Cardinality restrictions (min, max, exactly)
- Value restrictions (someValuesFrom, allValuesFrom)
- Existential restrictions
- Universal restrictions
- Self restrictions

**Implementation Notes**:
- New table: `ConceptRestrictions`
- Fields: RestrictionType, Property, Value, Cardinality
- UI: Restrictions tab in concept editor
- Keep simple: dropdown for restriction type, guided input

##### 3. Individuals/Instances ⭐ HIGH PRIORITY
**Current State**: No instance support
**Needed**:
- Create instances of concepts
- Assert facts about individuals
- Instance-level relationships
- Distinguish classes from instances

**Implementation Notes**:
- New table: `Individuals`
- Fields: Name, ConceptId (class membership), Properties
- New table: `IndividualRelationships`
- UI: Instances tab on ontology view
- Visual distinction: different color/icon from concepts

##### 4. Class Expressions 🔸 MEDIUM PRIORITY
**Current State**: No complex class expressions
**Needed**:
- Union of classes
- Intersection of classes
- Complement of classes
- Enumeration of individuals

**Implementation Notes**:
- New table: `ClassExpressions`
- Fields: ExpressionType (Union, Intersection, Complement), Components (JSON)
- UI: Expression builder (keep simple, maybe JSON editor initially)
- Display as derived concepts

##### 5. Disjointness & Equivalence 🔸 MEDIUM PRIORITY
**Current State**: No disjointness or equivalence
**Needed**:
- Mark concepts as disjoint
- Mark concepts as equivalent
- Disjoint unions
- Display in UI

**Implementation Notes**:
- Add to Relationships table with special types: `disjointWith`, `equivalentClass`
- UI: Section in concept editor
- Validation: Check for contradictions

##### 6. Domain & Range for Relationships 🔸 MEDIUM PRIORITY
**Current State**: Relationships have source and target but no formal domain/range
**Needed**:
- Define domain (source concept types allowed)
- Define range (target concept types allowed)
- Validation based on domain/range
- Display in UI

**Implementation Notes**:
- Add `DomainConceptId`, `RangeConceptId` to Relationship model
- Validation service to check domain/range
- UI: Dropdown to select domain/range in relationship editor

##### 7. Inverse Relationships 🔸 MEDIUM PRIORITY
**Current State**: No inverse relationship support
**Needed**:
- Define inverse of a relationship
- Automatic inference of inverse facts
- Display in UI

**Implementation Notes**:
- Add `InverseRelationshipId` to Relationship model
- Service logic: when creating relationship, create inverse
- UI: Dropdown to select inverse in relationship editor

##### 8. Enhanced Annotations 🔹 LOW PRIORITY
**Current State**: Basic properties on concepts
**Needed**:
- Annotations on concepts, relationships, properties
- Standard annotation properties (rdfs:label, rdfs:comment, etc.)
- Multiple languages support
- Version info

**Implementation Notes**:
- Extend Properties table to support annotations
- Add `AnnotationType` field
- UI: Annotations tab
- Keep simple: key-value pairs with language tags

##### 9. Reasoning/Inference Support 🔹 LOW PRIORITY
**Current State**: No reasoning
**Needed**:
- Basic class hierarchy inference
- Property inference
- Consistency checking
- Explanation of inferences

**Implementation Notes**:
- Integration with reasoning library (HermiT, Pellet, or custom)
- Run reasoning on-demand (button in UI)
- Display inferred relationships separately
- Could be complex - start with basic hierarchy inference

##### 10. SPARQL Query Support 🔹 LOW PRIORITY
**Current State**: Search only, no querying
**Needed**:
- SPARQL endpoint
- Query builder UI
- Execute queries
- Display results

**Implementation Notes**:
- dotNetRDF already supports SPARQL
- Service: SparqlService with query execution
- UI: Query editor (textarea) and results table
- Advanced feature - keep simple or defer

##### 11. Advanced Import/Export 🔹 LOW PRIORITY
**Current State**: TTL, JSON, CSV
**Needed**:
- RDF/XML import/export
- OWL XML import/export
- Manchester syntax support
- SKOS support

**Implementation Notes**:
- dotNetRDF supports multiple formats
- Extend ExportService with new strategies
- Extend ImportService
- UI: Format selector in import/export dialogs

##### 12. Concept Metadata Enhancements 🔹 LOW PRIORITY
**Current State**: Basic metadata
**Needed**:
- Deprecation status
- Version info per concept
- Change tracking
- Editorial annotations

**Implementation Notes**:
- Add fields to Concept: `IsDeprecated`, `Version`, `LastModifiedBy`
- UI: Show deprecated concepts with strikethrough
- History tracking (could be complex)

---

## Implementation Priority

### Phase 3: Core Ontology Features (Immediate)
**Target**: Q4 2024

1. **Hierarchical Relationships** ⭐
   - Enable parent/child concept relationships
   - Tree view component
   - Keep existing graph view, add hierarchy tab

2. **Individuals/Instances** ⭐
   - Support for ontology instances
   - Instance editor
   - Distinguish from concepts visually

3. **Concept Restrictions** ⭐
   - Basic cardinality constraints
   - Simple restrictions UI
   - Validation logic

### Phase 4: Advanced Ontology Features (Next)
**Target**: Q1 2025

4. **Domain & Range**
   - Relationship validation
   - Enhanced relationship editor

5. **Disjointness & Equivalence**
   - Class relationships
   - Validation checks

6. **Inverse Relationships**
   - Bidirectional relationships
   - Automatic inverse creation

### Phase 5: Expert Features (Future)
**Target**: Q2 2025

7. **Class Expressions**
   - Union, intersection, complement
   - Expression builder UI

8. **Basic Reasoning**
   - Hierarchy inference
   - Consistency checking

9. **SPARQL Queries**
   - Query editor
   - Results display

### Phase 6: Polish & Advanced (Future)
**Target**: Q3 2025

10. **Enhanced Annotations**
11. **Advanced Import/Export**
12. **Concept Metadata**

---

## Design Principles

### Simplicity First
- **Guideline**: Every feature must justify its UI complexity
- **Pattern**: Use progressive disclosure - advanced features hidden by default
- **Example**: Start with simple forms, add "Advanced" accordion for complex options

### Consistency
- **Guideline**: Follow established patterns throughout
- **Pattern**: Reuse components, services, and UI patterns
- **Example**: All editors use same card layout with header, body, footer

### Accessibility
- **Guideline**: All features must be keyboard accessible
- **Pattern**: Add ARIA attributes, test with screen readers
- **Example**: All modals have focus management and Esc to close

### Performance
- **Guideline**: Page load < 2 seconds, operations < 500ms
- **Pattern**: Use async loading, pagination, lazy loading
- **Example**: Graph view loads progressively for large ontologies

### Testing
- **Guideline**: All new features require tests
- **Pattern**: Unit tests (service behavior), Integration tests (database), Workflow tests (end-to-end)
- **Example**: 3 test types per new service

---

## Technology Stack

### Frontend
- **Framework**: Blazor Server (.NET 9)
- **UI**: Bootstrap 5.3
- **Icons**: Bootstrap Icons 1.11.3
- **Graphs**: D3.js, Cytoscape 3.28.1
- **Real-time**: SignalR 8.0

### Backend
- **Framework**: ASP.NET Core 9.0
- **Language**: C# 13
- **ORM**: Entity Framework Core 9.0
- **Database**: SQL Server (Local Docker, Azure SQL)
- **Authentication**: ASP.NET Core Identity, OAuth 2.0

### Libraries
- **RDF**: dotNetRDF
- **Rate Limiting**: AspNetCoreRateLimit
- **Secrets**: Azure Key Vault

### Testing
- **Framework**: xUnit
- **Mocking**: Moq 4.20.72
- **Blazor**: bUnit 1.40.0
- **Database**: EF Core InMemory

---

## Notes for Future Development

### Before Adding New Features
1. ✅ Consult this ledger for patterns and styles
2. ✅ Check Current Capabilities to avoid duplication
3. ✅ Review Design Principles for guidance
4. ✅ Plan tests before implementing
5. ✅ Update this ledger after completion

### When Modifying Existing Features
1. ✅ Maintain backward compatibility
2. ✅ Update existing tests
3. ✅ Add migration if database changes
4. ✅ Update user documentation
5. ✅ Log change in Feature History

### When Refactoring
1. ✅ Run all 137 tests before and after
2. ✅ Measure performance impact
3. ✅ Update architecture documentation
4. ✅ Review for breaking changes
5. ✅ Update this ledger with new patterns

---

**Last Updated**: 2024-10-25
**Current Version**: 1.0
**Test Suite**: 137 tests, 100% pass rate
**Next Review**: Before Phase 3 implementation
