using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Hubs;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services;
using Eidos.Services.Commands;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Eidos.Tests.Integration.Services;

/// <summary>
/// TRUE integration tests for ConceptService using real repositories and database
/// Tests the full stack: Service → Repository → Database
/// </summary>
public class ConceptServiceIntegrationTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly ConceptService _service;
    private readonly IConceptRepository _conceptRepository; // REAL
    private readonly IOntologyRepository _ontologyRepository; // REAL
    private readonly IRelationshipRepository _relationshipRepository; // REAL
    private readonly Mock<ICommandFactory> _mockCommandFactory;
    private readonly Mock<CommandInvoker> _mockCommandInvoker;
    private readonly Mock<IHubContext<OntologyHub>> _mockHubContext;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IOntologyShareService> _mockShareService;
    private readonly Mock<IOntologyActivityService> _mockActivityService;
    private readonly Mock<IDbContextFactory<OntologyDbContext>> _mockContextFactory;
    private readonly ApplicationUser _testUser;

    public ConceptServiceIntegrationTests()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);

        // Use REAL repositories for true integration testing
        _conceptRepository = new ConceptRepository(_contextFactory);
        _ontologyRepository = new OntologyRepository(_contextFactory);
        _relationshipRepository = new RelationshipRepository(_contextFactory);

        // Mock only external concerns
        _mockCommandFactory = new Mock<ICommandFactory>();
        _mockCommandInvoker = new Mock<CommandInvoker>();
        _mockHubContext = new Mock<IHubContext<OntologyHub>>();
        _mockUserService = new Mock<IUserService>();
        _mockShareService = new Mock<IOntologyShareService>();
        _mockActivityService = new Mock<IOntologyActivityService>();
        _mockContextFactory = new Mock<IDbContextFactory<OntologyDbContext>>();

        _testUser = TestDataBuilder.CreateUser();
        _mockUserService.Setup(s => s.GetCurrentUserAsync()).ReturnsAsync(_testUser);

        // Default permission to allow operations
        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<PermissionLevel>()))
            .ReturnsAsync(true);

        // Setup SignalR hub context
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        _service = new ConceptService(
            _conceptRepository,      // REAL - actual database operations
            _ontologyRepository,     // REAL - actual database operations
            _relationshipRepository, // REAL - actual database operations
            _mockCommandFactory.Object,
            _mockCommandInvoker.Object,
            _mockHubContext.Object,
            _mockUserService.Object,
            _mockShareService.Object,
            _mockActivityService.Object,
            _mockContextFactory.Object
        );
    }

    public void Dispose()
    {
        // In-memory database will be cleaned up automatically
    }

    [Fact]
    public async Task CreateConcept_ShouldPersistToDatabase_AndBeRetrievable()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        var concept = TestDataBuilder.CreateConcept(ontology.Id, "Person");

        // Act
        var created = await _service.CreateAsync(concept, recordUndo: false);

        // Assert - Verify it's ACTUALLY in the database
        var retrieved = await _conceptRepository.GetByIdAsync(created.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Person", retrieved.Name);
        Assert.Equal(ontology.Id, retrieved.OntologyId);
        Assert.True(retrieved.Id > 0, "Concept should have been assigned an ID");
    }

    [Fact]
    public async Task CreateConcept_ShouldUpdateOntologyTimestamp()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        var originalTimestamp = ontology.UpdatedAt;
        await Task.Delay(10); // Ensure timestamp difference

        var concept = TestDataBuilder.CreateConcept(ontology.Id, "Organization");

        // Act
        await _service.CreateAsync(concept, recordUndo: false);

        // Assert - Verify ontology timestamp was updated
        var updatedOntology = await _ontologyRepository.GetByIdAsync(ontology.Id);
        Assert.NotNull(updatedOntology);
        Assert.True(updatedOntology.UpdatedAt > originalTimestamp,
            "Ontology timestamp should be updated when concept is added");
    }

    [Fact]
    public async Task UpdateConcept_ShouldPersistChangesToDatabase()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        var concept = TestDataBuilder.CreateConcept(ontology.Id, "Original Name");
        var created = await _conceptRepository.AddAsync(concept);

        // Act - Update the concept
        created.Name = "Updated Name";
        created.Definition = "New definition";
        await _service.UpdateAsync(created, recordUndo: false);

        // Assert - Verify changes are in database
        var retrieved = await _conceptRepository.GetByIdAsync(created.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.Name);
        Assert.Equal("New definition", retrieved.Definition);
    }

    [Fact]
    public async Task DeleteConcept_ShouldRemoveFromDatabase()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        var concept = TestDataBuilder.CreateConcept(ontology.Id, "ToDelete");
        var created = await _conceptRepository.AddAsync(concept);

        // Act
        await _service.DeleteAsync(created.Id, recordUndo: false);

        // Assert - Verify it's gone from database
        var retrieved = await _conceptRepository.GetByIdAsync(created.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByOntologyId_ShouldReturnAllConceptsForOntology()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Concept 1"));
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Concept 2"));
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Concept 3"));

        // Act
        var concepts = await _service.GetByOntologyIdAsync(ontology.Id);

        // Assert
        Assert.Equal(3, concepts.Count());
        Assert.All(concepts, c => Assert.Equal(ontology.Id, c.OntologyId));
    }

    [Fact]
    public async Task SearchConcepts_ShouldFindMatchingConcepts()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Person"));
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Personal Data"));
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Organization"));

        // Act
        var results = await _service.SearchAsync("Person");

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, c => Assert.Contains("Person", c.Name));
    }

    [Fact]
    public async Task CreateConcept_WithoutPermission_ShouldThrowAndNotPersist()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        var concept = TestDataBuilder.CreateConcept(ontology.Id, "Test");

        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                ontology.Id,
                _testUser.Id,
                null,
                PermissionLevel.ViewAndAdd))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateAsync(concept, recordUndo: false));

        // Verify concept was NOT added to database
        var concepts = await _conceptRepository.GetByOntologyIdAsync(ontology.Id);
        Assert.Empty(concepts);
    }

    [Fact]
    public async Task UpdateConcept_WithoutPermission_ShouldThrowAndNotUpdate()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        var concept = TestDataBuilder.CreateConcept(ontology.Id, "Original");
        var created = await _conceptRepository.AddAsync(concept);

        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                ontology.Id,
                _testUser.Id,
                null,
                PermissionLevel.ViewAddEdit))
            .ReturnsAsync(false);

        // Act & Assert
        created.Name = "Hacked Name";
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateAsync(created, recordUndo: false));

        // Verify concept was NOT updated in database
        var retrieved = await _conceptRepository.GetByIdAsync(created.Id);
        Assert.Equal("Original", retrieved.Name);
    }

    [Fact]
    public async Task DeleteConcept_WithoutPermission_ShouldThrowAndNotDelete()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        var concept = TestDataBuilder.CreateConcept(ontology.Id, "Protected");
        var created = await _conceptRepository.AddAsync(concept);

        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                ontology.Id,
                _testUser.Id,
                null,
                PermissionLevel.ViewAddEdit))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.DeleteAsync(created.Id, recordUndo: false));

        // Verify concept still exists in database
        var retrieved = await _conceptRepository.GetByIdAsync(created.Id);
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task CreateMultipleConcepts_ShouldAllPersist()
    {
        // Arrange
        var ontology = await CreateRealOntology();

        // Act - Create multiple concepts
        var person = await _service.CreateAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Person"), recordUndo: false);
        var organization = await _service.CreateAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Organization"), recordUndo: false);
        var location = await _service.CreateAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Location"), recordUndo: false);

        // Assert - All should be in database with unique IDs
        var allConcepts = await _conceptRepository.GetByOntologyIdAsync(ontology.Id);
        Assert.Equal(3, allConcepts.Count());

        var ids = allConcepts.Select(c => c.Id).ToList();
        Assert.Equal(3, ids.Distinct().Count()); // All IDs should be unique

        Assert.Contains(allConcepts, c => c.Name == "Person");
        Assert.Contains(allConcepts, c => c.Name == "Organization");
        Assert.Contains(allConcepts, c => c.Name == "Location");
    }

    /// <summary>
    /// Helper to create a real ontology in the database
    /// </summary>
    private async Task<Ontology> CreateRealOntology(string name = "Test Ontology")
    {
        var ontology = TestDataBuilder.CreateOntology(name: name, userId: _testUser.Id);
        return await _ontologyRepository.AddAsync(ontology);
    }
}
