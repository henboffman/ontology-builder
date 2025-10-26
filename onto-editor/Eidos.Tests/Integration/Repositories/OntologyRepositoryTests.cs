using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Eidos.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for OntologyRepository using in-memory database
/// These tests verify database operations work correctly
/// </summary>
public class OntologyRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly OntologyRepository _repository;
    private readonly ApplicationUser _testUser;
    private readonly string _dbName;

    public OntologyRepositoryTests()
    {
        _dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(_dbName);
        _repository = new OntologyRepository(_contextFactory);
        _testUser = TestDataBuilder.CreateUser();
    }

    public void Dispose()
    {
        // In-memory database will be cleaned up automatically
        // No need to explicitly dispose contexts created by the factory
    }

    [Fact]
    public async Task AddAsync_ShouldAddOntology()
    {
        // Arrange
        var ontology = TestDataBuilder.CreateOntology(name: "Test Ontology", userId: _testUser.Id);

        // Act
        var result = await _repository.AddAsync(ontology);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Test Ontology", result.Name);
    }

    [Fact]
    public async Task GetWithAllRelatedDataAsync_ShouldLoadConceptsAndRelationships()
    {
        // Arrange
        var ontology = TestDataBuilder.CreateOntology(userId: _testUser.Id);
        await _repository.AddAsync(ontology);

        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var concept1 = TestDataBuilder.CreateConcept(ontologyId: ontology.Id, name: "Concept 1");
            var concept2 = TestDataBuilder.CreateConcept(ontologyId: ontology.Id, name: "Concept 2");
            context.Concepts.AddRange(concept1, concept2);
            await context.SaveChangesAsync();

            var relationship = TestDataBuilder.CreateRelationship(
                ontologyId: ontology.Id,
                sourceConceptId: concept1.Id,
                targetConceptId: concept2.Id
            );
            context.Relationships.Add(relationship);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await _repository.GetWithAllRelatedDataAsync(ontology.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Concepts.Count);
        Assert.Equal(1, result.Relationships.Count);
    }

    [Fact]
    public async Task GetAllWithBasicDataAsync_ShouldLoadAllOntologies()
    {
        // Arrange
        var ontology1 = TestDataBuilder.CreateOntology(name: "Ontology 1", userId: _testUser.Id);
        var ontology2 = TestDataBuilder.CreateOntology(name: "Ontology 2", userId: _testUser.Id);
        await _repository.AddAsync(ontology1);
        await _repository.AddAsync(ontology2);

        // Act
        var result = await _repository.GetAllWithBasicDataAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Name == "Ontology 1");
        Assert.Contains(result, o => o.Name == "Ontology 2");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateOntology()
    {
        // Arrange
        var ontology = TestDataBuilder.CreateOntology(name: "Original Name", userId: _testUser.Id);
        await _repository.AddAsync(ontology);

        // Act
        ontology.Name = "Updated Name";
        ontology.Description = "Updated Description";
        await _repository.UpdateAsync(ontology);

        // Assert
        var result = await _repository.GetByIdAsync(ontology.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Description", result.Description);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveOntology()
    {
        // Arrange
        var ontology = TestDataBuilder.CreateOntology(userId: _testUser.Id);
        await _repository.AddAsync(ontology);

        // Act
        await _repository.DeleteAsync(ontology.Id);

        // Assert
        var result = await _repository.GetByIdAsync(ontology.Id);
        Assert.Null(result);
    }
}
