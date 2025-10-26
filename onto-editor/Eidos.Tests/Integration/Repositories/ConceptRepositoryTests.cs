using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Tests.Helpers;
using Xunit;

namespace Eidos.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for ConceptRepository
/// </summary>
public class ConceptRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly ConceptRepository _repository;
    private readonly OntologyRepository _ontologyRepository;
    private readonly ApplicationUser _testUser;

    public ConceptRepositoryTests()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);
        _repository = new ConceptRepository(_contextFactory);
        _ontologyRepository = new OntologyRepository(_contextFactory);
        _testUser = TestDataBuilder.CreateUser();
    }

    public void Dispose()
    {
        // In-memory database will be cleaned up automatically
    }

    [Fact]
    public async Task AddAsync_ShouldAddConcept()
    {
        // Arrange
        var ontology = await CreateTestOntology();
        var concept = TestDataBuilder.CreateConcept(ontologyId: ontology.Id, name: "Person");

        // Act
        var result = await _repository.AddAsync(concept);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Person", result.Name);
        Assert.Equal(ontology.Id, result.OntologyId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnConcept()
    {
        // Arrange
        var ontology = await CreateTestOntology();
        var concept = TestDataBuilder.CreateConcept(ontologyId: ontology.Id, name: "Organization");
        var created = await _repository.AddAsync(concept);

        // Act
        var result = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Organization", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateConcept()
    {
        // Arrange
        var ontology = await CreateTestOntology();
        var concept = TestDataBuilder.CreateConcept(ontologyId: ontology.Id, name: "Original");
        var created = await _repository.AddAsync(concept);

        // Act
        created.Name = "Updated";
        created.Definition = "Updated definition";
        await _repository.UpdateAsync(created);

        // Assert
        var result = await _repository.GetByIdAsync(created.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        Assert.Equal("Updated definition", result.Definition);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveConcept()
    {
        // Arrange
        var ontology = await CreateTestOntology();
        var concept = TestDataBuilder.CreateConcept(ontologyId: ontology.Id);
        var created = await _repository.AddAsync(concept);

        // Act
        await _repository.DeleteAsync(created.Id);

        // Assert
        var result = await _repository.GetByIdAsync(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOntologyIdAsync_ShouldReturnConceptsForOntology()
    {
        // Arrange
        var ontology1 = await CreateTestOntology("Ontology 1");
        var ontology2 = await CreateTestOntology("Ontology 2");

        await _repository.AddAsync(TestDataBuilder.CreateConcept(ontology1.Id, "Concept1-A"));
        await _repository.AddAsync(TestDataBuilder.CreateConcept(ontology1.Id, "Concept1-B"));
        await _repository.AddAsync(TestDataBuilder.CreateConcept(ontology2.Id, "Concept2-A"));

        // Act
        var result = await _repository.GetByOntologyIdAsync(ontology1.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.Equal(ontology1.Id, c.OntologyId));
        Assert.Contains(result, c => c.Name == "Concept1-A");
        Assert.Contains(result, c => c.Name == "Concept1-B");
    }

    [Fact]
    public async Task GetWithPropertiesAsync_ShouldLoadConceptWithProperties()
    {
        // Arrange
        var ontology = await CreateTestOntology();
        var concept = TestDataBuilder.CreateConcept(ontology.Id, "Person");
        var created = await _repository.AddAsync(concept);

        // Add properties using context directly
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            context.Properties.Add(TestDataBuilder.CreateProperty(created.Id, "age", "30"));
            context.Properties.Add(TestDataBuilder.CreateProperty(created.Id, "name", "John"));
            await context.SaveChangesAsync();
        }

        // Act
        var result = await _repository.GetWithPropertiesAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Properties.Count);
        Assert.Contains(result.Properties, p => p.Name == "age");
        Assert.Contains(result.Properties, p => p.Name == "name");
    }

    private async Task<Ontology> CreateTestOntology(string name = "Test Ontology")
    {
        var ontology = TestDataBuilder.CreateOntology(name: name, userId: _testUser.Id);
        return await _ontologyRepository.AddAsync(ontology);
    }
}
