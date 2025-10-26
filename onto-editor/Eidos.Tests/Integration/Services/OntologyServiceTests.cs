using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services;
using Eidos.Services.Commands;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Moq;
using Xunit;

namespace Eidos.Tests.Integration.Services;

/// <summary>
/// Integration tests for OntologyService
/// </summary>
public class OntologyServiceTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly OntologyService _service;
    private readonly Mock<IConceptService> _mockConceptService;
    private readonly Mock<IRelationshipService> _mockRelationshipService;
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<IRelationshipSuggestionService> _mockSuggestionService;
    private readonly Mock<CommandInvoker> _mockCommandInvoker;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IOntologyShareService> _mockShareService;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly ApplicationUser _testUser;

    public OntologyServiceTests()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);
        _ontologyRepository = new OntologyRepository(_contextFactory);

        _mockConceptService = new Mock<IConceptService>();
        _mockRelationshipService = new Mock<IRelationshipService>();
        _mockPropertyService = new Mock<IPropertyService>();
        _mockSuggestionService = new Mock<IRelationshipSuggestionService>();
        _mockCommandInvoker = new Mock<CommandInvoker>();
        _mockUserService = new Mock<IUserService>();
        _mockShareService = new Mock<IOntologyShareService>();

        _testUser = TestDataBuilder.CreateUser();
        _mockUserService.Setup(s => s.GetCurrentUserAsync()).ReturnsAsync(_testUser);

        _service = new OntologyService(
            _contextFactory,
            _ontologyRepository,
            _mockConceptService.Object,
            _mockRelationshipService.Object,
            _mockPropertyService.Object,
            _mockSuggestionService.Object,
            _mockCommandInvoker.Object,
            _mockUserService.Object,
            _mockShareService.Object
        );
    }

    public void Dispose()
    {
        // In-memory database will be cleaned up automatically
    }

    [Fact]
    public async Task GetAllOntologiesAsync_ShouldReturnAllOntologies()
    {
        // Arrange
        await _ontologyRepository.AddAsync(TestDataBuilder.CreateOntology(name: "Ontology 1", userId: _testUser.Id));
        await _ontologyRepository.AddAsync(TestDataBuilder.CreateOntology(name: "Ontology 2", userId: _testUser.Id));

        // Act
        var result = await _service.GetAllOntologiesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Name == "Ontology 1");
        Assert.Contains(result, o => o.Name == "Ontology 2");
    }

    [Fact]
    public async Task GetOntologiesForCurrentUserAsync_ShouldReturnOnlyUserOntologies()
    {
        // Arrange
        var userOntology = TestDataBuilder.CreateOntology(name: "User Ontology", userId: _testUser.Id);
        var otherOntology = TestDataBuilder.CreateOntology(name: "Other Ontology", userId: "other-user-id");

        await _ontologyRepository.AddAsync(userOntology);
        await _ontologyRepository.AddAsync(otherOntology);

        _mockShareService
            .Setup(s => s.GetSharedOntologiesForUserAsync(_testUser.Id))
            .ReturnsAsync(new List<Ontology>());

        // Act
        var result = await _service.GetOntologiesForCurrentUserAsync();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, o => o.Name == "User Ontology");
        Assert.DoesNotContain(result, o => o.Name == "Other Ontology");
    }

    [Fact]
    public async Task GetOntologiesForCurrentUserAsync_ShouldIncludeSharedOntologies()
    {
        // Arrange
        var ownedOntology = TestDataBuilder.CreateOntology(name: "Owned", userId: _testUser.Id);
        var sharedOntology = TestDataBuilder.CreateOntology(name: "Shared", userId: "other-user-id");

        await _ontologyRepository.AddAsync(ownedOntology);
        await _ontologyRepository.AddAsync(sharedOntology);

        _mockShareService
            .Setup(s => s.GetSharedOntologiesForUserAsync(_testUser.Id))
            .ReturnsAsync(new List<Ontology> { sharedOntology });

        // Act
        var result = await _service.GetOntologiesForCurrentUserAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Name == "Owned");
        Assert.Contains(result, o => o.Name == "Shared");
    }

    [Fact]
    public async Task GetOntologyAsync_ShouldReturnOntologyWithRelatedData()
    {
        // Arrange
        var ontology = TestDataBuilder.CreateOntology(userId: _testUser.Id);
        var created = await _ontologyRepository.AddAsync(ontology);

        // Act
        var result = await _service.GetOntologyAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task CreateOntologyAsync_ShouldCreateOntology_WithCurrentUser()
    {
        // Arrange
        var ontology = TestDataBuilder.CreateOntology(name: "New Ontology", userId: null);

        // Act
        var result = await _service.CreateOntologyAsync(ontology);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(_testUser.Id, result.UserId);
        Assert.Equal("New Ontology", result.Name);
    }

    [Fact]
    public async Task CreateOntologyAsync_ShouldThrowException_WhenNoCurrentUser()
    {
        // Arrange
        _mockUserService.Setup(s => s.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser)null);
        var ontology = TestDataBuilder.CreateOntology(name: "New Ontology", userId: null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateOntologyAsync(ontology));
    }

    [Fact]
    public async Task UpdateOntologyAsync_ShouldUpdateOntology()
    {
        // Arrange
        var ontology = TestDataBuilder.CreateOntology(name: "Original", userId: _testUser.Id);
        var created = await _ontologyRepository.AddAsync(ontology);
        created.Name = "Updated";
        created.Description = "Updated description";

        // Act
        var result = await _service.UpdateOntologyAsync(created);

        // Assert
        Assert.Equal("Updated", result.Name);
        Assert.Equal("Updated description", result.Description);

        var fromDb = await _ontologyRepository.GetByIdAsync(created.Id);
        Assert.Equal("Updated", fromDb.Name);
    }

    [Fact]
    public async Task DeleteOntologyAsync_ShouldDeleteOntology_WhenUserIsOwner()
    {
        // Arrange
        var ontology = TestDataBuilder.CreateOntology(userId: _testUser.Id);
        var created = await _ontologyRepository.AddAsync(ontology);

        // Act
        await _service.DeleteOntologyAsync(created.Id);

        // Assert
        var result = await _ontologyRepository.GetByIdAsync(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteOntologyAsync_ShouldThrowUnauthorized_WhenUserIsNotOwner()
    {
        // Arrange
        var ontology = TestDataBuilder.CreateOntology(userId: "other-user-id");
        var created = await _ontologyRepository.AddAsync(ontology);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.DeleteOntologyAsync(created.Id));
    }

    [Fact]
    public async Task DeleteOntologyAsync_ShouldThrowException_WhenOntologyNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteOntologyAsync(99999));
    }

    [Fact]
    public async Task CloneOntologyAsync_ShouldCreateCloneWithProvenanceData()
    {
        // Arrange
        var sourceOntology = TestDataBuilder.CreateOntology(name: "Source", userId: _testUser.Id);
        var created = await _ontologyRepository.AddAsync(sourceOntology);

        // Setup concept service to return created concepts
        _mockConceptService
            .Setup(s => s.CreateAsync(It.IsAny<Concept>(), false))
            .ReturnsAsync((Concept c, bool _) =>
            {
                c.Id = new Random().Next(1, 10000);
                return c;
            });

        // Act
        var result = await _service.CloneOntologyAsync(created.Id, "Cloned Ontology", "Test clone");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cloned Ontology", result.Name);
        Assert.Equal("clone", result.ProvenanceType);
        Assert.Equal("Test clone", result.ProvenanceNotes);
        Assert.Equal(created.Id, result.ParentOntologyId);
        Assert.Equal(_testUser.Id, result.UserId);
    }

    [Fact]
    public async Task CloneOntologyAsync_ShouldThrowException_WhenSourceNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CloneOntologyAsync(99999, "Clone", null));
    }

    [Fact]
    public async Task CloneOntologyAsync_ShouldThrowException_WhenNoCurrentUser()
    {
        // Arrange
        var ontology = TestDataBuilder.CreateOntology(userId: _testUser.Id);
        var created = await _ontologyRepository.AddAsync(ontology);

        _mockUserService.Setup(s => s.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CloneOntologyAsync(created.Id, "Clone", null));
    }

    [Fact]
    public async Task ForkOntologyAsync_ShouldCreateForkWithCorrectProvenanceType()
    {
        // Arrange
        var sourceOntology = TestDataBuilder.CreateOntology(name: "Source", userId: _testUser.Id);
        var created = await _ontologyRepository.AddAsync(sourceOntology);

        _mockConceptService
            .Setup(s => s.CreateAsync(It.IsAny<Concept>(), false))
            .ReturnsAsync((Concept c, bool _) =>
            {
                c.Id = new Random().Next(1, 10000);
                return c;
            });

        // Act
        var result = await _service.ForkOntologyAsync(created.Id, "Forked Ontology", "Test fork");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Forked Ontology", result.Name);
        Assert.Equal("fork", result.ProvenanceType);
        Assert.Equal("Test fork", result.ProvenanceNotes);
        Assert.Equal(created.Id, result.ParentOntologyId);
    }

    [Fact]
    public async Task GetOntologyLineageAsync_ShouldReturnAncestorChain()
    {
        // Arrange
        var original = TestDataBuilder.CreateOntology(name: "Original", userId: _testUser.Id);
        original = await _ontologyRepository.AddAsync(original);

        var clone = TestDataBuilder.CreateOntology(
            name: "Clone",
            userId: _testUser.Id,
            parentOntologyId: original.Id,
            provenanceType: "clone");
        clone = await _ontologyRepository.AddAsync(clone);

        var fork = TestDataBuilder.CreateOntology(
            name: "Fork",
            userId: _testUser.Id,
            parentOntologyId: clone.Id,
            provenanceType: "fork");
        fork = await _ontologyRepository.AddAsync(fork);

        // Act
        var result = await _service.GetOntologyLineageAsync(fork.Id);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Fork", result[0].Name);
        Assert.Equal("Clone", result[1].Name);
        Assert.Equal("Original", result[2].Name);
    }

    [Fact]
    public async Task GetOntologyDescendantsAsync_ShouldReturnAllDescendants()
    {
        // Arrange
        var original = TestDataBuilder.CreateOntology(name: "Original", userId: _testUser.Id);
        original = await _ontologyRepository.AddAsync(original);

        var child1 = TestDataBuilder.CreateOntology(
            name: "Child 1",
            userId: _testUser.Id,
            parentOntologyId: original.Id);
        child1 = await _ontologyRepository.AddAsync(child1);

        var child2 = TestDataBuilder.CreateOntology(
            name: "Child 2",
            userId: _testUser.Id,
            parentOntologyId: original.Id);
        child2 = await _ontologyRepository.AddAsync(child2);

        var grandchild = TestDataBuilder.CreateOntology(
            name: "Grandchild",
            userId: _testUser.Id,
            parentOntologyId: child1.Id);
        grandchild = await _ontologyRepository.AddAsync(grandchild);

        // Act
        var result = await _service.GetOntologyDescendantsAsync(original.Id);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, o => o.Name == "Child 1");
        Assert.Contains(result, o => o.Name == "Child 2");
        Assert.Contains(result, o => o.Name == "Grandchild");
    }

    [Fact]
    public async Task CreateConceptAsync_ShouldDelegateToConceptService()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        _mockConceptService
            .Setup(s => s.CreateAsync(concept, true))
            .ReturnsAsync(concept);

        // Act
        var result = await _service.CreateConceptAsync(concept, true);

        // Assert
        _mockConceptService.Verify(s => s.CreateAsync(concept, true), Times.Once);
        Assert.Equal(concept, result);
    }

    [Fact]
    public async Task UpdateConceptAsync_ShouldDelegateToConceptService()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Updated Concept");
        _mockConceptService
            .Setup(s => s.UpdateAsync(concept, true))
            .ReturnsAsync(concept);

        // Act
        var result = await _service.UpdateConceptAsync(concept, true);

        // Assert
        _mockConceptService.Verify(s => s.UpdateAsync(concept, true), Times.Once);
        Assert.Equal(concept, result);
    }

    [Fact]
    public async Task DeleteConceptAsync_ShouldDelegateToConceptService()
    {
        // Arrange
        var conceptId = 123;

        // Act
        await _service.DeleteConceptAsync(conceptId, true);

        // Assert
        _mockConceptService.Verify(s => s.DeleteAsync(conceptId, true), Times.Once);
    }

    [Fact]
    public async Task CreateRelationshipAsync_ShouldDelegateToRelationshipService()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);
        _mockRelationshipService
            .Setup(s => s.CreateAsync(relationship, true))
            .ReturnsAsync(relationship);

        // Act
        var result = await _service.CreateRelationshipAsync(relationship, true);

        // Assert
        _mockRelationshipService.Verify(s => s.CreateAsync(relationship, true), Times.Once);
        Assert.Equal(relationship, result);
    }

}
