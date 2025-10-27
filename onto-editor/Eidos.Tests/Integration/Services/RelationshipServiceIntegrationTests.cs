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
/// TRUE integration tests for RelationshipService using real repositories and database
/// Tests the full stack: Service → Repository → Database
/// </summary>
public class RelationshipServiceIntegrationTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly RelationshipService _service;
    private readonly IRelationshipRepository _relationshipRepository; // REAL
    private readonly IOntologyRepository _ontologyRepository; // REAL
    private readonly IConceptRepository _conceptRepository; // REAL
    private readonly Mock<ICommandFactory> _mockCommandFactory;
    private readonly Mock<CommandInvoker> _mockCommandInvoker;
    private readonly Mock<IHubContext<OntologyHub>> _mockHubContext;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IOntologyShareService> _mockShareService;
    private readonly Mock<IOntologyActivityService> _mockActivityService;
    private readonly ApplicationUser _testUser;

    public RelationshipServiceIntegrationTests()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);

        // Use REAL repositories for true integration testing
        _relationshipRepository = new RelationshipRepository(_contextFactory);
        _ontologyRepository = new OntologyRepository(_contextFactory);
        _conceptRepository = new ConceptRepository(_contextFactory);

        // Mock only external concerns
        _mockCommandFactory = new Mock<ICommandFactory>();
        _mockCommandInvoker = new Mock<CommandInvoker>();
        _mockHubContext = new Mock<IHubContext<OntologyHub>>();
        _mockUserService = new Mock<IUserService>();
        _mockShareService = new Mock<IOntologyShareService>();
        _mockActivityService = new Mock<IOntologyActivityService>();

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

        _service = new RelationshipService(
            _relationshipRepository,  // REAL - actual database operations
            _ontologyRepository,      // REAL - actual database operations
            _mockCommandFactory.Object,
            _mockCommandInvoker.Object,
            _mockHubContext.Object,
            _mockUserService.Object,
            _mockShareService.Object,
            _mockActivityService.Object
        );
    }

    public void Dispose()
    {
        // In-memory database will be cleaned up automatically
    }

    [Fact]
    public async Task CreateRelationship_ShouldPersistToDatabase_AndBeRetrievable()
    {
        // Arrange
        var (ontology, source, target) = await CreateOntologyWithConcepts();
        var relationship = TestDataBuilder.CreateRelationship(
            ontology.Id, source.Id, target.Id, "is-a");

        // Act
        var created = await _service.CreateAsync(relationship, recordUndo: false);

        // Assert - Verify it's ACTUALLY in the database
        var retrieved = await _relationshipRepository.GetByIdAsync(created.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("is-a", retrieved.RelationType);
        Assert.Equal(source.Id, retrieved.SourceConceptId);
        Assert.Equal(target.Id, retrieved.TargetConceptId);
        Assert.True(retrieved.Id > 0, "Relationship should have been assigned an ID");
    }

    [Fact]
    public async Task CreateRelationship_ShouldUpdateOntologyTimestamp()
    {
        // Arrange
        var (ontology, source, target) = await CreateOntologyWithConcepts();
        var originalTimestamp = ontology.UpdatedAt;
        await Task.Delay(10); // Ensure timestamp difference

        var relationship = TestDataBuilder.CreateRelationship(
            ontology.Id, source.Id, target.Id, "has-part");

        // Act
        await _service.CreateAsync(relationship, recordUndo: false);

        // Assert - Verify ontology timestamp was updated
        var updatedOntology = await _ontologyRepository.GetByIdAsync(ontology.Id);
        Assert.NotNull(updatedOntology);
        Assert.True(updatedOntology.UpdatedAt > originalTimestamp,
            "Ontology timestamp should be updated when relationship is added");
    }

    [Fact]
    public async Task UpdateRelationship_ShouldPersistChangesToDatabase()
    {
        // Arrange
        var (ontology, source, target) = await CreateOntologyWithConcepts();
        var relationship = TestDataBuilder.CreateRelationship(
            ontology.Id, source.Id, target.Id, "is-a");
        var created = await _relationshipRepository.AddAsync(relationship);

        // Act - Update the relationship
        created.RelationType = "part-of";
        created.Description = "New description";
        await _service.UpdateAsync(created, recordUndo: false);

        // Assert - Verify changes are in database
        var retrieved = await _relationshipRepository.GetByIdAsync(created.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("part-of", retrieved.RelationType);
        Assert.Equal("New description", retrieved.Description);
    }

    [Fact]
    public async Task DeleteRelationship_ShouldRemoveFromDatabase()
    {
        // Arrange
        var (ontology, source, target) = await CreateOntologyWithConcepts();
        var relationship = TestDataBuilder.CreateRelationship(
            ontology.Id, source.Id, target.Id);
        var created = await _relationshipRepository.AddAsync(relationship);

        // Act
        await _service.DeleteAsync(created.Id, recordUndo: false);

        // Assert - Verify it's gone from database
        var retrieved = await _relationshipRepository.GetByIdAsync(created.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetByOntologyId_ShouldReturnAllRelationshipsForOntology()
    {
        // Arrange
        var (ontology, source, target) = await CreateOntologyWithConcepts();
        var concept3 = await _conceptRepository.AddAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Concept 3"));

        await _relationshipRepository.AddAsync(
            TestDataBuilder.CreateRelationship(ontology.Id, source.Id, target.Id, "is-a"));
        await _relationshipRepository.AddAsync(
            TestDataBuilder.CreateRelationship(ontology.Id, source.Id, concept3.Id, "has-part"));
        await _relationshipRepository.AddAsync(
            TestDataBuilder.CreateRelationship(ontology.Id, target.Id, concept3.Id, "related-to"));

        // Act
        var relationships = await _service.GetByOntologyIdAsync(ontology.Id);

        // Assert
        Assert.Equal(3, relationships.Count());
        Assert.All(relationships, r => Assert.Equal(ontology.Id, r.OntologyId));
    }

    [Fact]
    public async Task GetByConceptId_ShouldReturnAllRelationshipsForConcept()
    {
        // Arrange
        var (ontology, source, target) = await CreateOntologyWithConcepts();
        var concept3 = await _conceptRepository.AddAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Concept 3"));

        // Source is part of 2 relationships
        await _relationshipRepository.AddAsync(
            TestDataBuilder.CreateRelationship(ontology.Id, source.Id, target.Id));
        await _relationshipRepository.AddAsync(
            TestDataBuilder.CreateRelationship(ontology.Id, source.Id, concept3.Id));

        // Act
        var relationships = await _service.GetByConceptIdAsync(source.Id);

        // Assert
        Assert.Equal(2, relationships.Count());
        Assert.All(relationships, r => Assert.Equal(source.Id, r.SourceConceptId));
    }

    [Fact]
    public async Task CanCreateRelationship_WithSelfReference_ShouldReturnFalse()
    {
        // Arrange
        var (ontology, source, _) = await CreateOntologyWithConcepts();

        // Act
        var canCreate = await _service.CanCreateAsync(source.Id, source.Id, "is-a");

        // Assert
        Assert.False(canCreate, "Should not allow self-relationships");
    }

    [Fact]
    public async Task CanCreateRelationship_WhenDuplicateExists_ShouldReturnFalse()
    {
        // Arrange
        var (ontology, source, target) = await CreateOntologyWithConcepts();

        // Create existing relationship
        await _relationshipRepository.AddAsync(
            TestDataBuilder.CreateRelationship(ontology.Id, source.Id, target.Id, "is-a"));

        // Act
        var canCreate = await _service.CanCreateAsync(source.Id, target.Id, "is-a");

        // Assert
        Assert.False(canCreate, "Should not allow duplicate relationships");
    }

    [Fact]
    public async Task CanCreateRelationship_WhenValid_ShouldReturnTrue()
    {
        // Arrange
        var (ontology, source, target) = await CreateOntologyWithConcepts();

        // Act
        var canCreate = await _service.CanCreateAsync(source.Id, target.Id, "is-a");

        // Assert
        Assert.True(canCreate, "Should allow valid new relationships");
    }

    [Fact]
    public async Task CreateRelationship_WithoutPermission_ShouldThrowAndNotPersist()
    {
        // Arrange
        var (ontology, source, target) = await CreateOntologyWithConcepts();
        var relationship = TestDataBuilder.CreateRelationship(
            ontology.Id, source.Id, target.Id);

        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                ontology.Id,
                _testUser.Id,
                null,
                PermissionLevel.ViewAndAdd))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateAsync(relationship, recordUndo: false));

        // Verify relationship was NOT added to database
        var relationships = await _relationshipRepository.GetByOntologyIdAsync(ontology.Id);
        Assert.Empty(relationships);
    }

    [Fact]
    public async Task CreateMultipleRelationships_ShouldAllPersist()
    {
        // Arrange
        var (ontology, concept1, concept2) = await CreateOntologyWithConcepts();
        var concept3 = await _conceptRepository.AddAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Concept 3"));

        // Act - Create multiple relationships
        var rel1 = await _service.CreateAsync(
            TestDataBuilder.CreateRelationship(ontology.Id, concept1.Id, concept2.Id, "is-a"),
            recordUndo: false);
        var rel2 = await _service.CreateAsync(
            TestDataBuilder.CreateRelationship(ontology.Id, concept1.Id, concept3.Id, "has-part"),
            recordUndo: false);
        var rel3 = await _service.CreateAsync(
            TestDataBuilder.CreateRelationship(ontology.Id, concept2.Id, concept3.Id, "related-to"),
            recordUndo: false);

        // Assert - All should be in database with unique IDs
        var allRelationships = await _relationshipRepository.GetByOntologyIdAsync(ontology.Id);
        Assert.Equal(3, allRelationships.Count());

        var ids = allRelationships.Select(r => r.Id).ToList();
        Assert.Equal(3, ids.Distinct().Count()); // All IDs should be unique

        Assert.Contains(allRelationships, r => r.RelationType == "is-a");
        Assert.Contains(allRelationships, r => r.RelationType == "has-part");
        Assert.Contains(allRelationships, r => r.RelationType == "related-to");
    }

    [Fact]
    public async Task GetWithConceptsAsync_ShouldLoadRelationshipWithSourceAndTarget()
    {
        // Arrange
        var (ontology, source, target) = await CreateOntologyWithConcepts();
        var relationship = TestDataBuilder.CreateRelationship(
            ontology.Id, source.Id, target.Id);
        var created = await _relationshipRepository.AddAsync(relationship);

        // Act
        var retrieved = await _relationshipRepository.GetWithConceptsAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.SourceConcept);
        Assert.NotNull(retrieved.TargetConcept);
        Assert.Equal(source.Name, retrieved.SourceConcept.Name);
        Assert.Equal(target.Name, retrieved.TargetConcept.Name);
    }

    /// <summary>
    /// Helper to create a real ontology with two concepts in the database
    /// </summary>
    private async Task<(Ontology ontology, Concept source, Concept target)> CreateOntologyWithConcepts()
    {
        var ontology = TestDataBuilder.CreateOntology(name: "Test Ontology", userId: _testUser.Id);
        var createdOntology = await _ontologyRepository.AddAsync(ontology);

        var source = await _conceptRepository.AddAsync(
            TestDataBuilder.CreateConcept(createdOntology.Id, "Source Concept"));
        var target = await _conceptRepository.AddAsync(
            TestDataBuilder.CreateConcept(createdOntology.Id, "Target Concept"));

        return (createdOntology, source, target);
    }
}
