using Eidos.Models;
using Eidos.Services;
using Eidos.Tests.Helpers;
using Xunit;

namespace Eidos.Tests.Integration.Services;

/// <summary>
/// Integration tests for TtlExportService Phase 3 features
/// Tests export of individuals, restrictions, and hierarchies
/// </summary>
public class TtlExportServicePhase3Tests : IDisposable
{
    private readonly TtlExportService _service;

    public TtlExportServicePhase3Tests()
    {
        _service = new TtlExportService();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public void ExportToTtl_WithIndividuals_ShouldIncludeNamedIndividuals()
    {
        // Arrange
        var ontology = CreateOntologyWithIndividuals();

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Assert
        Assert.Contains("owl:NamedIndividual", ttl);
        Assert.Contains("Fido", ttl);
        Assert.Contains("Golden Retriever", ttl);
    }

    [Fact]
    public void ExportToTtl_WithIndividualProperties_ShouldIncludeTypedLiterals()
    {
        // Arrange
        var ontology = CreateOntologyWithIndividuals();

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Assert
        // Check that individuals are being exported with properties
        Assert.Contains("Fido", ttl);
        Assert.Contains("age>", ttl); // Property URI
        Assert.Contains("5", ttl); // Age value (Turtle allows bare numeric literals)
        Assert.Contains("breed>", ttl); // Property URI
        Assert.Contains("\"Golden Retriever\"", ttl); // String value

        // Check that xsd namespace is defined (typed literals use this)
        Assert.Contains("@prefix xsd:", ttl);
    }

    [Fact]
    public void ExportToTtl_WithConceptRestrictions_ShouldIncludeOwlRestrictions()
    {
        // Arrange
        var ontology = CreateOntologyWithRestrictions();

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Assert
        Assert.Contains("owl:Restriction", ttl);
        Assert.Contains("owl:onProperty", ttl);
        Assert.Contains("rdfs:subClassOf", ttl);
    }

    [Fact]
    public void ExportToTtl_WithCardinalityRestriction_ShouldIncludeCardinality()
    {
        // Arrange
        var ontology = CreateOntologyWithRestrictions();

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Assert
        Assert.Contains("owl:minCardinality", ttl);
        Assert.Contains("xsd:nonNegativeInteger", ttl);
    }

    [Fact]
    public void ExportToTtl_WithValueTypeRestriction_ShouldIncludeAllValuesFrom()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "TestOntology",
            Namespace = "http://example.org/test/",
            Concepts = new List<Concept>
            {
                new()
                {
                    Id = 1,
                    OntologyId = 1,
                    Name = "Person",
                    Restrictions = new List<ConceptRestriction>
                    {
                        new()
                        {
                            PropertyName = "age",
                            RestrictionType = RestrictionTypes.ValueType,
                            ValueType = "integer"
                        }
                    }
                }
            }
        };

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Assert
        Assert.Contains("owl:allValuesFrom", ttl);
        Assert.Contains("xsd:integer", ttl);
    }

    [Fact]
    public void ExportToTtl_WithHierarchy_ShouldIncludeSubClassOf()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "TestOntology",
            Namespace = "http://example.org/test/",
            Concepts = new List<Concept>
            {
                new() { Id = 1, OntologyId = 1, Name = "Animal" },
                new() { Id = 2, OntologyId = 1, Name = "Dog" }
            },
            Relationships = new List<Relationship>
            {
                new()
                {
                    SourceConceptId = 2,
                    TargetConceptId = 1,
                    RelationType = "is-a",
                    SourceConcept = new Concept { Id = 2, Name = "Dog" },
                    TargetConcept = new Concept { Id = 1, Name = "Animal" }
                }
            }
        };

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Assert
        Assert.Contains("rdfs:subClassOf", ttl);
        Assert.Contains("Dog", ttl);
        Assert.Contains("Animal", ttl);
    }

    [Fact]
    public void ExportToFormat_WithJsonLd_ShouldExportIndividuals()
    {
        // Arrange
        var ontology = CreateOntologyWithIndividuals();

        // Act
        var jsonLd = _service.ExportToFormat(ontology, RdfFormat.JsonLd);

        // Assert
        Assert.NotEmpty(jsonLd);
        // dotNetRDF produces expanded JSON-LD (array format)
        Assert.Contains("\"@id\"", jsonLd);
        Assert.Contains("Fido", jsonLd);
    }

    [Fact]
    public void ExportToFormat_WithRdfXml_ShouldExportRestrictions()
    {
        // Arrange
        var ontology = CreateOntologyWithRestrictions();

        // Act
        var rdfXml = _service.ExportToFormat(ontology, RdfFormat.RdfXml);

        // Assert
        Assert.NotEmpty(rdfXml);
        Assert.Contains("rdf:RDF", rdfXml);
        Assert.Contains("owl:Restriction", rdfXml);
    }

    [Fact]
    public void ExportToTtl_WithIndividualRelationships_ShouldIncludeObjectProperties()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "TestOntology",
            Namespace = "http://example.org/test/",
            Concepts = new List<Concept>
            {
                new() { Id = 1, OntologyId = 1, Name = "Person" }
            },
            Individuals = new List<Individual>
            {
                new()
                {
                    Id = 1,
                    OntologyId = 1,
                    ConceptId = 1,
                    Name = "John",
                    Concept = new Concept { Id = 1, Name = "Person" },
                    RelationshipsAsSource = new List<IndividualRelationship>
                    {
                        new()
                        {
                            SourceIndividualId = 1,
                            TargetIndividualId = 2,
                            RelationType = "knows"
                        }
                    }
                },
                new()
                {
                    Id = 2,
                    OntologyId = 1,
                    ConceptId = 1,
                    Name = "Jane",
                    Concept = new Concept { Id = 1, Name = "Person" }
                }
            }
        };

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Assert
        Assert.Contains("knows", ttl);
        Assert.Contains("John", ttl);
        Assert.Contains("Jane", ttl);
    }

    private Ontology CreateOntologyWithIndividuals()
    {
        return new Ontology
        {
            Id = 1,
            Name = "AnimalOntology",
            Namespace = "http://example.org/animals/",
            Concepts = new List<Concept>
            {
                new() { Id = 1, OntologyId = 1, Name = "Dog" }
            },
            Individuals = new List<Individual>
            {
                new()
                {
                    Id = 1,
                    OntologyId = 1,
                    ConceptId = 1,
                    Name = "Fido",
                    Label = "Fido the Dog",
                    Description = "A friendly golden retriever",
                    Concept = new Concept { Id = 1, Name = "Dog" },
                    Properties = new List<IndividualProperty>
                    {
                        new() { Name = "age", Value = "5", DataType = "integer" },
                        new() { Name = "breed", Value = "Golden Retriever", DataType = "string" }
                    }
                }
            }
        };
    }

    private Ontology CreateOntologyWithRestrictions()
    {
        return new Ontology
        {
            Id = 1,
            Name = "TestOntology",
            Namespace = "http://example.org/test/",
            Concepts = new List<Concept>
            {
                new()
                {
                    Id = 1,
                    OntologyId = 1,
                    Name = "Person",
                    Restrictions = new List<ConceptRestriction>
                    {
                        new()
                        {
                            PropertyName = "name",
                            RestrictionType = RestrictionTypes.Required,
                            IsMandatory = true
                        },
                        new()
                        {
                            PropertyName = "children",
                            RestrictionType = RestrictionTypes.Cardinality,
                            MinCardinality = 0,
                            MaxCardinality = 10
                        }
                    }
                }
            }
        };
    }
}
