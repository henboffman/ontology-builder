using Eidos.Models;
using Eidos.Services.Export;
using System.Text.Json;
using Xunit;

namespace Eidos.Tests.Integration.Services;

public class JsonExportStrategyTests
{
    [Fact]
    public void Export_ShouldIncludeAllOntologyMetadata()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);
        var result = JsonDocument.Parse(json);

        // Assert
        Assert.Equal("Test Ontology", result.RootElement.GetProperty("name").GetString());
        Assert.Equal("A test ontology", result.RootElement.GetProperty("description").GetString());
        Assert.Equal("http://example.org/test#", result.RootElement.GetProperty("namespace").GetString());
        Assert.Equal("Test Author", result.RootElement.GetProperty("author").GetString());
        Assert.Equal("1.0", result.RootElement.GetProperty("version").GetString());
        Assert.True(result.RootElement.GetProperty("usesBFO").GetBoolean());
        Assert.Equal("private", result.RootElement.GetProperty("visibility").GetString());
    }

    [Fact]
    public void Export_ShouldIncludeConceptsWithAllFields()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);
        var result = JsonDocument.Parse(json);

        // Assert
        var concepts = result.RootElement.GetProperty("concepts");
        Assert.Equal(2, concepts.GetArrayLength());

        var concept = concepts[0];
        Assert.Equal("Person", concept.GetProperty("name").GetString());
        Assert.Equal("A human being", concept.GetProperty("definition").GetString());
        Assert.Equal("An individual human", concept.GetProperty("simpleExplanation").GetString());
        Assert.Equal("John, Mary", concept.GetProperty("examples").GetString());
        Assert.Equal("Entity", concept.GetProperty("category").GetString());
        Assert.Equal("#4A90E2", concept.GetProperty("color").GetString());
        Assert.Equal(100.5, concept.GetProperty("positionX").GetDouble());
        Assert.Equal(200.3, concept.GetProperty("positionY").GetDouble());
    }

    [Fact]
    public void Export_ShouldIncludeConceptProperties()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);
        var result = JsonDocument.Parse(json);

        // Assert
        var properties = result.RootElement.GetProperty("concepts")[0].GetProperty("properties");
        Assert.Equal(1, properties.GetArrayLength());

        var property = properties[0];
        Assert.Equal("age", property.GetProperty("name").GetString());
        Assert.Equal("integer", property.GetProperty("dataType").GetString());
        Assert.Equal("Age in years", property.GetProperty("description").GetString());
    }

    [Fact]
    public void Export_ShouldIncludeRelationshipsWithAllFields()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);
        var result = JsonDocument.Parse(json);

        // Assert
        var relationships = result.RootElement.GetProperty("relationships");
        Assert.Equal(1, relationships.GetArrayLength());

        var relationship = relationships[0];
        Assert.Equal("Student", relationship.GetProperty("sourceConcept").GetString());
        Assert.Equal("is-a", relationship.GetProperty("relationType").GetString());
        Assert.Equal("Person", relationship.GetProperty("targetConcept").GetString());
        Assert.Equal("subclass of", relationship.GetProperty("label").GetString());
        Assert.Equal("Student is a type of Person", relationship.GetProperty("description").GetString());
        Assert.Equal("http://www.w3.org/2000/01/rdf-schema#subClassOf", relationship.GetProperty("ontologyUri").GetString());
        Assert.Equal(1.0m, relationship.GetProperty("strength").GetDecimal());
    }

    [Fact]
    public void Export_ShouldIncludeIndividuals()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);
        var result = JsonDocument.Parse(json);

        // Assert
        var individuals = result.RootElement.GetProperty("individuals");
        Assert.Equal(2, individuals.GetArrayLength());

        var individual = individuals[0];
        Assert.Equal("Socrates", individual.GetProperty("name").GetString());
        Assert.Equal("Person", individual.GetProperty("conceptName").GetString());
        Assert.Equal("Ancient Greek philosopher", individual.GetProperty("description").GetString());
        Assert.Equal("Socrates of Athens", individual.GetProperty("label").GetString());
        Assert.Equal("http://example.org/test#Socrates", individual.GetProperty("uri").GetString());
    }

    [Fact]
    public void Export_ShouldIncludeIndividualProperties()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);
        var result = JsonDocument.Parse(json);

        // Assert
        var properties = result.RootElement.GetProperty("individuals")[0].GetProperty("properties");
        Assert.Equal(1, properties.GetArrayLength());

        var property = properties[0];
        Assert.Equal("birthYear", property.GetProperty("name").GetString());
        Assert.Equal("-470", property.GetProperty("value").GetString());
        Assert.Equal("integer", property.GetProperty("dataType").GetString());
    }

    [Fact]
    public void Export_ShouldIncludeIndividualRelationships()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);
        var result = JsonDocument.Parse(json);

        // Assert
        var relationships = result.RootElement.GetProperty("individualRelationships");
        Assert.Equal(1, relationships.GetArrayLength());

        var relationship = relationships[0];
        Assert.Equal("Socrates", relationship.GetProperty("sourceIndividual").GetString());
        Assert.Equal("teaches", relationship.GetProperty("relationType").GetString());
        Assert.Equal("Plato", relationship.GetProperty("targetIndividual").GetString());
        Assert.Equal("taught by", relationship.GetProperty("label").GetString());
    }

    [Fact]
    public void Export_ShouldIncludeLinkedOntologies()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);
        var result = JsonDocument.Parse(json);

        // Assert
        var linkedOntologies = result.RootElement.GetProperty("linkedOntologies");
        Assert.Equal(1, linkedOntologies.GetArrayLength());

        var linked = linkedOntologies[0];
        Assert.Equal("BFO", linked.GetProperty("name").GetString());
        Assert.Equal("http://purl.obolibrary.org/obo/bfo.owl", linked.GetProperty("uri").GetString());
        Assert.Equal("bfo", linked.GetProperty("prefix").GetString());
        Assert.True(linked.GetProperty("conceptsImported").GetBoolean());
        Assert.Equal(10, linked.GetProperty("importedConceptCount").GetInt32());
    }

    [Fact]
    public void Export_ShouldIncludeCustomTemplates()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);
        var result = JsonDocument.Parse(json);

        // Assert
        var templates = result.RootElement.GetProperty("customTemplates");
        Assert.Equal(1, templates.GetArrayLength());

        var template = templates[0];
        Assert.Equal("Custom Category", template.GetProperty("category").GetString());
        Assert.Equal("Custom Type", template.GetProperty("type").GetString());
        Assert.Equal("A custom template", template.GetProperty("description").GetString());
        Assert.Equal("#FF5733", template.GetProperty("color").GetString());
    }

    [Fact]
    public void Export_ShouldFormatAsIndentedJson()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);

        // Assert
        Assert.Contains("\n", json); // Should have newlines (indented)
        Assert.Contains("  ", json); // Should have indentation
    }

    [Fact]
    public void Export_ShouldUseCamelCasePropertyNames()
    {
        // Arrange
        var strategy = new JsonExportStrategy();
        var ontology = CreateTestOntology();

        // Act
        var json = strategy.Export(ontology);

        // Assert
        Assert.Contains("\"conceptName\"", json); // camelCase, not ConceptName
        Assert.Contains("\"createdAt\"", json); // camelCase, not CreatedAt
        Assert.Contains("\"sourceIndividual\"", json); // camelCase
    }

    private Ontology CreateTestOntology()
    {
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            Description = "A test ontology",
            Namespace = "http://example.org/test#",
            Author = "Test Author",
            Version = "1.0",
            UsesBFO = true,
            UsesProvO = false,
            Visibility = "private",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ConceptCount = 2,
            RelationshipCount = 1
        };

        // Create concepts
        var person = new Concept
        {
            Id = 1,
            OntologyId = 1,
            Name = "Person",
            Definition = "A human being",
            SimpleExplanation = "An individual human",
            Examples = "John, Mary",
            Category = "Entity",
            Color = "#4A90E2",
            PositionX = 100.5,
            PositionY = 200.3,
            CreatedAt = DateTime.UtcNow
        };

        var student = new Concept
        {
            Id = 2,
            OntologyId = 1,
            Name = "Student",
            Definition = "A person who studies",
            Category = "Entity",
            Color = "#E94B3C",
            CreatedAt = DateTime.UtcNow
        };

        // Add concept property
        person.Properties.Add(new Property
        {
            Id = 1,
            ConceptId = 1,
            Name = "age",
            DataType = "integer",
            Description = "Age in years"
        });

        ontology.Concepts.Add(person);
        ontology.Concepts.Add(student);

        // Create relationship
        var relationship = new Relationship
        {
            Id = 1,
            OntologyId = 1,
            SourceConceptId = 2,
            SourceConcept = student,
            TargetConceptId = 1,
            TargetConcept = person,
            RelationType = "is-a",
            Label = "subclass of",
            Description = "Student is a type of Person",
            OntologyUri = "http://www.w3.org/2000/01/rdf-schema#subClassOf",
            Strength = 1.0m,
            CreatedAt = DateTime.UtcNow
        };

        ontology.Relationships.Add(relationship);

        // Create individuals
        var socrates = new Individual
        {
            Id = 1,
            OntologyId = 1,
            ConceptId = 1,
            Concept = person,
            Name = "Socrates",
            Description = "Ancient Greek philosopher",
            Label = "Socrates of Athens",
            Uri = "http://example.org/test#Socrates",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        socrates.Properties.Add(new IndividualProperty
        {
            Id = 1,
            IndividualId = 1,
            Name = "birthYear",
            Value = "-470",
            DataType = "integer"
        });

        var plato = new Individual
        {
            Id = 2,
            OntologyId = 1,
            ConceptId = 1,
            Concept = person,
            Name = "Plato",
            Description = "Ancient Greek philosopher",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        ontology.Individuals.Add(socrates);
        ontology.Individuals.Add(plato);

        // Create individual relationship
        var individualRelationship = new IndividualRelationship
        {
            Id = 1,
            OntologyId = 1,
            SourceIndividualId = 1,
            SourceIndividual = socrates,
            TargetIndividualId = 2,
            TargetIndividual = plato,
            RelationType = "teaches",
            Label = "taught by",
            CreatedAt = DateTime.UtcNow
        };

        ontology.IndividualRelationships.Add(individualRelationship);

        // Add linked ontology
        ontology.LinkedOntologies.Add(new OntologyLink
        {
            Id = 1,
            OntologyId = 1,
            Name = "BFO",
            Uri = "http://purl.obolibrary.org/obo/bfo.owl",
            Prefix = "bfo",
            ConceptsImported = true,
            ImportedConceptCount = 10,
            CreatedAt = DateTime.UtcNow
        });

        // Add custom template
        ontology.CustomTemplates.Add(new CustomConceptTemplate
        {
            Id = 1,
            OntologyId = 1,
            Category = "Custom Category",
            Type = "Custom Type",
            Description = "A custom template",
            Color = "#FF5733",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        return ontology;
    }
}
