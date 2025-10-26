using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Tests.Helpers;
using Xunit;

namespace Eidos.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for RelationshipRepository
/// </summary>
public class RelationshipRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly RelationshipRepository _repository;
    private readonly OntologyRepository _ontologyRepository;
    private readonly ConceptRepository _conceptRepository;
    private readonly ApplicationUser _testUser;

    public RelationshipRepositoryTests()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);
        _repository = new RelationshipRepository(_contextFactory);
        _ontologyRepository = new OntologyRepository(_contextFactory);
        _conceptRepository = new ConceptRepository(_contextFactory);
        _testUser = TestDataBuilder.CreateUser();
    }

    public void Dispose()
    {
        // In-memory database will be cleaned up automatically
    }

    [Fact]
    public async Task AddAsync_ShouldAddRelationship()
    {
        // Arrange
        var (ontology, source, target) = await CreateTestOntologyWithConcepts();
        var relationship = TestDataBuilder.CreateRelationship(
            ontologyId: ontology.Id,
            sourceConceptId: source.Id,
            targetConceptId: target.Id,
            relationType: "is-a"
        );

        // Act
        var result = await _repository.AddAsync(relationship);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(source.Id, result.SourceConceptId);
        Assert.Equal(target.Id, result.TargetConceptId);
        Assert.Equal("is-a", result.RelationType);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnRelationship()
    {
        // Arrange
        var (ontology, source, target) = await CreateTestOntologyWithConcepts();
        var relationship = TestDataBuilder.CreateRelationship(
            ontology.Id, source.Id, target.Id, "has-part"
        );
        var created = await _repository.AddAsync(relationship);

        // Act
        var result = await _repository.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("has-part", result.RelationType);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateRelationship()
    {
        // Arrange
        var (ontology, source, target) = await CreateTestOntologyWithConcepts();
        var relationship = TestDataBuilder.CreateRelationship(
            ontology.Id, source.Id, target.Id
        );
        var created = await _repository.AddAsync(relationship);

        // Act
        created.RelationType = "part-of";
        created.Description = "Updated description";
        await _repository.UpdateAsync(created);

        // Assert
        var result = await _repository.GetByIdAsync(created.Id);
        Assert.NotNull(result);
        Assert.Equal("part-of", result.RelationType);
        Assert.Equal("Updated description", result.Description);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveRelationship()
    {
        // Arrange
        var (ontology, source, target) = await CreateTestOntologyWithConcepts();
        var relationship = TestDataBuilder.CreateRelationship(
            ontology.Id, source.Id, target.Id
        );
        var created = await _repository.AddAsync(relationship);

        // Act
        await _repository.DeleteAsync(created.Id);

        // Assert
        var result = await _repository.GetByIdAsync(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOntologyIdAsync_ShouldReturnRelationshipsForOntology()
    {
        // Arrange
        var (ontology1, source1, target1) = await CreateTestOntologyWithConcepts("Ontology 1");
        var (ontology2, source2, target2) = await CreateTestOntologyWithConcepts("Ontology 2");

        await _repository.AddAsync(TestDataBuilder.CreateRelationship(
            ontology1.Id, source1.Id, target1.Id, "is-a"));
        await _repository.AddAsync(TestDataBuilder.CreateRelationship(
            ontology1.Id, source1.Id, target1.Id, "has-part"));
        await _repository.AddAsync(TestDataBuilder.CreateRelationship(
            ontology2.Id, source2.Id, target2.Id, "is-a"));

        // Act
        var result = await _repository.GetByOntologyIdAsync(ontology1.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Equal(ontology1.Id, r.OntologyId));
    }

    [Fact]
    public async Task GetByConceptIdAsync_ShouldReturnRelationshipsForConcept()
    {
        // Arrange
        var ontology = await CreateTestOntology();
        var concept1 = await _conceptRepository.AddAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Concept1"));
        var concept2 = await _conceptRepository.AddAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Concept2"));
        var concept3 = await _conceptRepository.AddAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Concept3"));

        // Concept1 is source of 2 relationships
        await _repository.AddAsync(TestDataBuilder.CreateRelationship(
            ontology.Id, concept1.Id, concept2.Id));
        await _repository.AddAsync(TestDataBuilder.CreateRelationship(
            ontology.Id, concept1.Id, concept3.Id));
        // Concept1 is target of 1 relationship
        await _repository.AddAsync(TestDataBuilder.CreateRelationship(
            ontology.Id, concept2.Id, concept1.Id));

        // Act
        var result = await _repository.GetByConceptIdAsync(concept1.Id);

        // Assert
        Assert.Equal(3, result.Count());
        Assert.Contains(result, r => r.SourceConceptId == concept1.Id && r.TargetConceptId == concept2.Id);
        Assert.Contains(result, r => r.SourceConceptId == concept1.Id && r.TargetConceptId == concept3.Id);
        Assert.Contains(result, r => r.SourceConceptId == concept2.Id && r.TargetConceptId == concept1.Id);
    }

    [Fact]
    public async Task GetWithConceptsAsync_ShouldLoadRelationshipWithSourceAndTarget()
    {
        // Arrange
        var (ontology, source, target) = await CreateTestOntologyWithConcepts();
        var relationship = TestDataBuilder.CreateRelationship(
            ontology.Id, source.Id, target.Id
        );
        var created = await _repository.AddAsync(relationship);

        // Act
        var result = await _repository.GetWithConceptsAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.SourceConcept);
        Assert.NotNull(result.TargetConcept);
        Assert.Equal(source.Id, result.SourceConcept.Id);
        Assert.Equal(target.Id, result.TargetConcept.Id);
    }

    private async Task<Ontology> CreateTestOntology(string name = "Test Ontology")
    {
        var ontology = TestDataBuilder.CreateOntology(name: name, userId: _testUser.Id);
        return await _ontologyRepository.AddAsync(ontology);
    }

    private async Task<(Ontology ontology, Concept source, Concept target)> CreateTestOntologyWithConcepts(
        string ontologyName = "Test Ontology")
    {
        var ontology = await CreateTestOntology(ontologyName);
        var source = await _conceptRepository.AddAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Source Concept"));
        var target = await _conceptRepository.AddAsync(
            TestDataBuilder.CreateConcept(ontology.Id, "Target Concept"));

        return (ontology, source, target);
    }
}
