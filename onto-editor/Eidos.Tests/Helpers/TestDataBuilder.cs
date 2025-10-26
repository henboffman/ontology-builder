using Eidos.Data;
using Eidos.Models;
using Microsoft.EntityFrameworkCore;

namespace Eidos.Tests.Helpers;

/// <summary>
/// Helper class for creating test database contexts
/// </summary>
public class TestDbContextFactory : IDbContextFactory<OntologyDbContext>
{
    private readonly DbContextOptions<OntologyDbContext> _options;

    public TestDbContextFactory(string databaseName)
    {
        _options = new DbContextOptionsBuilder<OntologyDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    public OntologyDbContext CreateDbContext()
    {
        return new OntologyDbContext(_options);
    }

    public Task<OntologyDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }
}

/// <summary>
/// Builder class for creating test data objects
/// </summary>
public class TestDataBuilder
{
    public static Ontology CreateOntology(
        string name = "Test Ontology",
        string userId = "test-user-id",
        int? parentOntologyId = null,
        string? provenanceType = null)
    {
        return new Ontology
        {
            // Don't set Id - let EF Core generate it
            Name = name,
            UserId = userId,
            Description = "Test description",
            Namespace = "http://test.com/ontology#",
            Tags = "test,sample",
            License = "MIT",
            Author = "Test Author",
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ParentOntologyId = parentOntologyId,
            ProvenanceType = provenanceType,
            Concepts = new List<Concept>(),
            Relationships = new List<Relationship>()
        };
    }

    public static Concept CreateConcept(
        int ontologyId,
        string name = "Test Concept",
        double positionX = 100,
        double positionY = 100)
    {
        return new Concept
        {
            // Don't set Id - let EF Core generate it
            OntologyId = ontologyId,
            Name = name,
            Definition = "Test definition",
            SimpleExplanation = "Test explanation",
            Examples = "Test examples",
            PositionX = positionX,
            PositionY = positionY,
            Category = "Test Category",
            Color = "#FF0000",
            Properties = new List<Property>()
        };
    }

    public static Relationship CreateRelationship(
        int ontologyId,
        int sourceConceptId,
        int targetConceptId,
        string relationType = "is-a")
    {
        return new Relationship
        {
            // Don't set Id - let EF Core generate it
            OntologyId = ontologyId,
            SourceConceptId = sourceConceptId,
            TargetConceptId = targetConceptId,
            RelationType = relationType,
            Description = "Test relationship"
        };
    }

    public static Property CreateProperty(
        int conceptId,
        string name = "TestProperty",
        string value = "TestValue")
    {
        return new Property
        {
            // Don't set Id - let EF Core generate it
            ConceptId = conceptId,
            Name = name,
            Value = value,
            DataType = "string"
        };
    }

    public static ApplicationUser CreateUser(
        string id = "test-user-id",
        string userName = "testuser",
        string email = "test@example.com")
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = userName,
            Email = email
        };
    }
}
