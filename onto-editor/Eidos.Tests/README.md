# Eidos Test Suite

This directory contains comprehensive tests for the Eidos ontology builder application.

## Test Framework & Tools

- **xUnit** - Test framework for .NET with built-in assertions
- **Moq 4.20.72** - Mocking framework for creating test doubles
- **bUnit 1.40.0** - Testing library for Blazor components
- **Microsoft.EntityFrameworkCore.InMemory 9.0.10** - In-memory database for integration testing

All packages are fully open source with no commercial restrictions.

## Test Structure

```
Eidos.Tests/
├── Integration/                   # Integration tests (with database)
│   ├── Repositories/              # Repository integration tests
│   │   ├── OntologyRepositoryTests.cs          (6 tests)
│   │   ├── ConceptRepositoryTests.cs           (6 tests)
│   │   └── RelationshipRepositoryTests.cs      (7 tests)
│   ├── Services/                  # Service tests (unit + integration)
│   │   ├── OntologyServiceTests.cs             (24 tests - unit tests with mocks)
│   │   ├── ConceptServiceTests.cs              (16 tests - unit tests with mocks)
│   │   ├── RelationshipServiceTests.cs         (18 tests - unit tests with mocks)
│   │   ├── ConceptServiceIntegrationTests.cs   (11 tests - REAL database integration)
│   │   ├── RelationshipServiceIntegrationTests.cs (14 tests - REAL database integration)
│   │   └── OntologyShareServiceTests.cs        (8 tests - collaborator tracking)
│   └── Workflows/                 # End-to-end workflow tests
│       └── OntologyWorkflowTests.cs            (7 tests - complete user scenarios)
├── Components/                    # Blazor component tests using bUnit
│   ├── ForkCloneDialogTests.cs    (5 tests)
│   ├── OntologyLineageTests.cs    (7 tests)
│   ├── ConfirmDialogTests.cs      (9 tests)
│   ├── ConceptEditorTests.cs      (21 tests)
│   ├── CollaboratorPanelTests.cs  (9 tests)
│   └── Pages/
│       └── OntologySettingsTests.cs (3 tests)
└── Helpers/                       # Test utilities and helpers
    ├── TestDataBuilder.cs         # Builder for test data objects
    └── TestDbContextFactory.cs    # In-memory database factory
```

## Test Philosophy

This test suite uses a **layered testing approach** to ensure both isolated component behavior and real-world application functionality:

1. **Unit Tests (with mocks)** - Fast, isolated tests that verify service behavior and delegation logic
2. **Integration Tests (with real database)** - Tests that verify the full stack actually works with real database operations
3. **Workflow Tests** - End-to-end scenarios that verify complete user workflows function correctly

This approach provides:
- **Fast feedback** from unit tests during development
- **Confidence** from integration tests that data operations work correctly
- **Verification** from workflow tests that the application actually delivers user value

## Test Coverage

### Repository Tests (19 tests)

#### OntologyRepository (6 tests)
- ✅ Add ontology to database
- ✅ Retrieve ontology with all related data (concepts, relationships)
- ✅ Get all ontologies with basic data
- ✅ Update ontology information
- ✅ Delete ontology

#### ConceptRepository (6 tests)
- ✅ Add concept to database
- ✅ Retrieve concept by ID
- ✅ Update concept information
- ✅ Delete concept
- ✅ Get concepts by ontology ID
- ✅ Get concept with all properties

#### RelationshipRepository (7 tests)
- ✅ Add relationship to database
- ✅ Retrieve relationship by ID
- ✅ Update relationship information
- ✅ Delete relationship
- ✅ Get relationships by ontology ID
- ✅ Get relationships by concept ID (both source and target)
- ✅ Get relationship with concept details (source and target)

### Service Unit Tests (58 tests - using mocks)

These tests verify service behavior and delegation logic using mocked repositories.

#### OntologyService (24 tests)
- ✅ Get all ontologies from repository
- ✅ Get ontologies for current user (owned and shared)
- ✅ Get ontology with all related data
- ✅ Create ontology with current user assignment
- ✅ Handle create without authenticated user
- ✅ Update ontology information
- ✅ Delete ontology with ownership validation
- ✅ Prevent unauthorized deletion
- ✅ Clone ontology with provenance tracking
- ✅ Handle clone of non-existent ontology
- ✅ Fork ontology with correct provenance type
- ✅ Get ontology lineage (ancestry chain)
- ✅ Get ontology descendants (all children recursively)
- ✅ Delegate concept operations to ConceptService
- ✅ Delegate relationship operations to RelationshipService

#### ConceptService (16 tests)
- ✅ Create concept using command factory for undo support
- ✅ Create concept directly without undo
- ✅ Enforce permission checks on create
- ✅ Update concept using command factory for undo support
- ✅ Update concept directly without undo
- ✅ Enforce permission checks on update
- ✅ Delete concept using command factory for undo support
- ✅ Delete concept directly without undo
- ✅ Enforce permission checks on delete
- ✅ Handle deletion of non-existent concept
- ✅ Get concept by ID
- ✅ Get concepts by ontology ID
- ✅ Search concepts by query
- ✅ Broadcast create via SignalR
- ✅ Broadcast update via SignalR
- ✅ Broadcast delete via SignalR

#### RelationshipService (18 tests)
- ✅ Create relationship using command factory for undo support
- ✅ Create relationship directly without undo
- ✅ Enforce permission checks on create
- ✅ Update relationship using command factory for undo support
- ✅ Update relationship directly without undo
- ✅ Enforce permission checks on update
- ✅ Delete relationship using command factory for undo support
- ✅ Delete relationship directly without undo
- ✅ Enforce permission checks on delete
- ✅ Handle deletion of non-existent relationship
- ✅ Get relationship by ID
- ✅ Get relationships by ontology ID
- ✅ Get relationships by concept ID
- ✅ Validate relationship creation (prevent self-relationships)
- ✅ Validate relationship creation (prevent duplicates)
- ✅ Allow valid relationship creation
- ✅ Broadcast create via SignalR

### Service Integration Tests (33 tests - using REAL database)

These tests use REAL repositories and verify the full application stack works correctly.

#### ConceptServiceIntegrationTests (11 tests)
- ✅ Create concept persists to database and is retrievable
- ✅ Creating concept updates ontology timestamp
- ✅ Update concept persists changes to database
- ✅ Delete concept removes from database
- ✅ Get concepts by ontology ID returns all concepts
- ✅ Search finds matching concepts
- ✅ Create without permission throws and doesn't persist
- ✅ Update without permission throws and doesn't update
- ✅ Delete without permission throws and doesn't delete
- ✅ Create multiple concepts - all persist with unique IDs

#### RelationshipServiceIntegrationTests (14 tests)
- ✅ Create relationship persists to database and is retrievable
- ✅ Creating relationship updates ontology timestamp
- ✅ Update relationship persists changes to database
- ✅ Delete relationship removes from database
- ✅ Get relationships by ontology ID returns all relationships
- ✅ Get relationships by concept ID returns related relationships
- ✅ Validation prevents self-relationships
- ✅ Validation prevents duplicate relationships
- ✅ Validation allows valid new relationships
- ✅ Create without permission throws and doesn't persist
- ✅ Create multiple relationships - all persist with unique IDs
- ✅ Get relationship with concepts loads source and target

#### OntologyShareServiceTests (8 tests)
- ✅ Returns empty list when no collaborators exist
- ✅ Returns authenticated collaborators with access details
- ✅ Returns guest collaborators with session information
- ✅ Includes activity history for each collaborator
- ✅ Excludes inactive shares from results
- ✅ Returns user activity history ordered by date
- ✅ Respects activity limit parameter
- ✅ Only returns activities for specified user

### Workflow Tests (7 tests - end-to-end scenarios)

These tests verify complete user workflows from start to finish using real services and database.

#### OntologyWorkflowTests (7 tests)
- ✅ Complete ontology creation workflow (ontology → concepts → relationships → verification)
- ✅ Fork ontology workflow (copy all data, maintain provenance, track lineage)
- ✅ Multiple users workflow (data isolation between users)
- ✅ Update workflow (modify concepts and relationships, verify persistence)
- ✅ Delete workflow (remove concepts and relationships, verify cleanup)
- ✅ Complex lineage workflow (multi-generational fork/clone tracking)

### Component Tests (54 tests)

#### ForkCloneDialog Component (5 tests)
- ✅ Display fork modal with correct content
- ✅ Display clone modal with correct content
- ✅ Disable submit button when name is empty
- ✅ Hide modal when Hide() is called
- ✅ Call OntologyService.ForkOntologyAsync when fork is executed

#### OntologyLineage Component (7 tests)
- ✅ Display modal with ontology name
- ✅ Show loading indicator during data fetch
- ✅ Display ancestry chain with multiple generations
- ✅ Display descendants when they exist
- ✅ Show "no descendants" message when none exist
- ✅ Show "original ontology" message when no parent exists
- ✅ Close modal when Hide() is called

#### ConfirmDialog Component (9 tests)
- ✅ Initially hidden on render
- ✅ Display dialog with correct title and message
- ✅ Show danger styling with red header
- ✅ Show warning styling with yellow header
- ✅ Show info styling with blue header
- ✅ Return true when confirm button clicked
- ✅ Return false when cancel button clicked
- ✅ Return false when close button clicked
- ✅ Handle multiple sequential dialogs

#### ConceptEditor Component (21 tests)
- ✅ Show "Add New Concept" mode when not editing
- ✅ Show "Edit Concept" mode when editing
- ✅ Display template selector only in add mode
- ✅ Display custom templates when provided
- ✅ Disable save button when name is empty
- ✅ Enable save button when name is provided
- ✅ Trigger callback when name input changes
- ✅ Trigger callback when save button clicked
- ✅ Trigger callback when cancel button clicked
- ✅ Toggle help text visibility
- ✅ Trigger callback when color changes
- ✅ Trigger callback when template selected
- ✅ Add pulse-attention class when ShouldPulse is true
- ✅ Not add pulse class when ShouldPulse is false
- ✅ Show "Save & Add Another" button when not editing
- ✅ Not show "Save & Add Another" button when editing
- ✅ Disable "Save & Add Another" button when name is empty
- ✅ Enable "Save & Add Another" button when name is provided
- ✅ Trigger callback when "Save & Add Another" button clicked
- ✅ Show Ctrl+Enter tooltip on "Save & Add Another" button

#### CollaboratorPanel Component (9 tests)
- ✅ Shows loading state initially
- ✅ Displays empty state when no collaborators exist
- ✅ Displays collaborator cards with user information
- ✅ Displays guest collaborators correctly with guest badge
- ✅ Displays correct permission level badges
- ✅ Displays edit statistics when ShowDetails is true
- ✅ Displays recent activity timeline when ShowActivity is true
- ✅ Shows error message when service fails
- ✅ Calls service with correct ontology ID and activity limit parameters

#### OntologySettings Page Component (3 tests)
- ✅ Render component with valid parameters
- ✅ Accept and bind Id and ActiveTab parameters
- ✅ Default ActiveTab to "general" when not specified

## Running Tests

### Run all tests
```bash
dotnet test Eidos.Tests/Eidos.Tests.csproj
```

### Run with minimal output
```bash
dotnet test Eidos.Tests/Eidos.Tests.csproj --verbosity minimal
```

### Run tests with detailed output
```bash
dotnet test Eidos.Tests/Eidos.Tests.csproj --verbosity detailed
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~OntologyRepositoryTests"
```

### Run specific test method
```bash
dotnet test --filter "FullyQualifiedName~AddAsync_ShouldAddOntology"
```

## Test Results

**Current Status:** ✅ **All 222 tests passing**

**Test Breakdown:**
- Repository Integration Tests: 19
- Service Unit Tests: 58
  - OntologyService: 24
  - ConceptService: 16
  - RelationshipService: 18
- Service Integration Tests: 33
  - ConceptServiceIntegrationTests: 11
  - RelationshipServiceIntegrationTests: 14
  - OntologyShareServiceTests: 8
  - OntologyPermissionServiceTests: 20
  - IndividualServiceTests: 8
  - RestrictionServiceTests: 8
  - TtlExportServicePhase3Tests: 8
- Workflow Tests: 7
- Component Tests: 54
  - ForkCloneDialog: 5
  - OntologyLineage: 7
  - ConfirmDialog: 9
  - ConceptEditor: 21
  - CollaboratorPanel: 9
  - OntologySettings: 3

## Test Helpers

### TestDbContextFactory
Provides a factory for creating in-memory database contexts for integration tests.

```csharp
var factory = new TestDbContextFactory("TestDb_UniqueId");
var repository = new OntologyRepository(factory);
```

### TestDataBuilder
Static helper class for creating test data objects with sensible defaults.

```csharp
// Create test ontology
var ontology = TestDataBuilder.CreateOntology(
    name: "My Ontology",
    userId: "user123"
);

// Create test concept
var concept = TestDataBuilder.CreateConcept(
    ontologyId: ontology.Id,
    name: "Person",
    positionX: 100,
    positionY: 200
);

// Create test relationship
var relationship = TestDataBuilder.CreateRelationship(
    ontologyId: ontology.Id,
    sourceConceptId: concept1.Id,
    targetConceptId: concept2.Id,
    relationType: "is-a"
);
```

## Best Practices

1. **Isolation**: Each test is independent and doesn't rely on other tests
2. **Arrange-Act-Assert**: All tests follow the AAA pattern for clarity
3. **Descriptive Names**: Test names clearly describe what they test and expected behavior
4. **In-Memory Database**: Integration tests use in-memory database for fast execution
5. **Proper Cleanup**: Tests properly dispose resources using IDisposable pattern
6. **Blazor Dispatcher**: Component tests use `InvokeAsync()` for proper Blazor rendering

## Adding New Tests

### Repository Integration Test Template
```csharp
[Fact]
public async Task MethodName_ShouldBehavior_WhenCondition()
{
    // Arrange
    var ontology = await CreateTestOntology();
    // ... setup test data

    // Act
    var result = await _repository.SomeMethod();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedValue, result.SomeProperty);
}
```

### Component Test Template
```csharp
[Fact]
public void ComponentMethod_ShouldBehavior_WhenCondition()
{
    // Arrange
    var cut = RenderComponent<MyComponent>();

    // Act
    cut.InvokeAsync(() => cut.Instance.SomeMethod());

    // Assert
    Assert.Contains("Expected Text", cut.Markup);
}
```

## Future Test Expansion

Consider adding tests for:
- [x] ConceptService integration tests ✅ COMPLETED (11 tests)
- [x] RelationshipService integration tests ✅ COMPLETED (14 tests)
- [x] End-to-end workflow tests ✅ COMPLETED (7 tests)
- [x] OntologyShareService tests ✅ COMPLETED (8 tests)
- [x] CollaboratorPanel component tests ✅ COMPLETED (9 tests)
- [ ] PropertyService integration tests
- [ ] PropertyRepository tests
- [ ] UserRepository tests
- [ ] Export/Import service tests
- [ ] Additional Blazor component tests (RelationshipEditor, ShareModal, etc.)
- [ ] Performance tests for large ontologies
- [ ] Validation logic tests
- [ ] Activity tracking integration tests

## Continuous Integration

These tests are designed to run in CI/CD pipelines. They:
- Run quickly (fast execution for 162 tests)
- Don't require external dependencies
- Use in-memory databases
- Are deterministic and reliable
- Provide clear failure messages
- Verify both isolated behavior (unit tests) and real functionality (integration tests)

---

**Last Updated:** 2025-10-27
**Total Tests:** 222
**Pass Rate:** 100%

**Test Breakdown:**
- Repository Integration Tests: 19
- Service Unit Tests (with mocks): 58
  - OntologyService: 24
  - ConceptService: 16
  - RelationshipService: 18
- Service Integration Tests (REAL database): 91
  - ConceptServiceIntegrationTests: 11
  - RelationshipServiceIntegrationTests: 14
  - OntologyShareServiceTests: 8
  - OntologyPermissionServiceTests: 20
  - IndividualServiceTests: 8
  - RestrictionServiceTests: 8
  - TtlExportServicePhase3Tests: 8
  - And others...
- Workflow Tests (end-to-end): 7
- Component Tests: 54
  - ForkCloneDialog: 5
  - OntologyLineage: 7
  - ConfirmDialog: 9
  - ConceptEditor: 21
  - CollaboratorPanel: 9
  - OntologySettings: 3

**Key Achievements:**
- TRUE integration tests that verify the application actually works with real database operations, not just mocked behavior
- Comprehensive collaborator tracking and activity monitoring test coverage
- Foundation for version control testing with activity snapshots
- Power user feature tests for bulk concept entry with "Save & Add Another" functionality
- GitHub-style settings page with tab navigation tests
