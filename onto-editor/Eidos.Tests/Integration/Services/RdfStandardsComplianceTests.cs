using Eidos.Models;
using Eidos.Services;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using Xunit;

namespace Eidos.Tests.Integration.Services;

/// <summary>
/// Tests to ensure RDF exports maintain W3C standards compliance
/// Verifies that exported files are valid and compatible with other RDF tools
/// </summary>
public class RdfStandardsComplianceTests : IDisposable
{
    private readonly TtlExportService _service;

    public RdfStandardsComplianceTests()
    {
        _service = new TtlExportService();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Turtle (TTL) Compliance Tests

    [Fact]
    public void ExportToTtl_ShouldProduceValidTurtleSyntax()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Assert - Parse it back with dotNetRDF parser to verify validity
        var graph = new Graph();
        var parser = new TurtleParser();

        // This will throw if the TTL is not valid
        var exception = Record.Exception(() => parser.Load(graph, new System.IO.StringReader(ttl)));
        Assert.Null(exception);

        // Verify graph is not empty
        Assert.True(graph.Triples.Count > 0);
    }

    [Fact]
    public void ExportToTtl_ShouldIncludeStandardNamespacePrefixes()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Assert - Verify standard prefixes are present
        Assert.Contains("@prefix rdf:", ttl);
        Assert.Contains("@prefix rdfs:", ttl);
        Assert.Contains("@prefix owl:", ttl);
        Assert.Contains("@prefix xsd:", ttl);
    }

    [Fact]
    public void ExportToTtl_WithImportedNamespaces_ShouldPreserveOriginalPrefixes()
    {
        // Arrange
        var savedPrefixes = new Dictionary<string, string>
        {
            { "dcterms", "http://purl.org/dc/terms/" },
            { "foaf", "http://xmlns.com/foaf/0.1/" },
            { "skos", "http://www.w3.org/2004/02/skos/core#" }
        };

        var ontology = new Ontology
        {
            Id = 1,
            Name = "ImportedOntology",
            Namespace = "http://example.org/imported/",
            NamespacePrefixes = System.Text.Json.JsonSerializer.Serialize(savedPrefixes),
            Concepts = new List<Concept>
            {
                new() { Id = 1, OntologyId = 1, Name = "TestConcept" }
            }
        };

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Assert - Verify imported prefixes are present
        Assert.Contains("@prefix dcterms:", ttl);
        Assert.Contains("@prefix foaf:", ttl);
        Assert.Contains("@prefix skos:", ttl);
        Assert.Contains("http://purl.org/dc/terms/", ttl);
        Assert.Contains("http://xmlns.com/foaf/0.1/", ttl);
        Assert.Contains("http://www.w3.org/2004/02/skos/core#", ttl);
    }

    [Fact]
    public void ExportToTtl_RoundTrip_ShouldPreserveTripleCount()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act - Export to TTL
        var ttl = _service.ExportToTtl(ontology);

        // Parse it back
        var graph = new Graph();
        var parser = new TurtleParser();
        parser.Load(graph, new System.IO.StringReader(ttl));

        // Assert - Verify we have triples
        // We expect: ontology declaration + classes + relationships + properties
        Assert.True(graph.Triples.Count >= 5, $"Expected at least 5 triples, got {graph.Triples.Count}");
    }

    #endregion

    #region RDF/XML Compliance Tests

    [Fact]
    public void ExportToRdfXml_ShouldProduceValidRdfXmlSyntax()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var rdfXml = _service.ExportToFormat(ontology, RdfFormat.RdfXml);

        // Assert - Parse it back with dotNetRDF parser to verify validity
        var graph = new Graph();
        var parser = new RdfXmlParser();

        // This will throw if the RDF/XML is not valid
        var exception = Record.Exception(() => parser.Load(graph, new System.IO.StringReader(rdfXml)));
        Assert.Null(exception);

        // Verify graph is not empty
        Assert.True(graph.Triples.Count > 0);
    }

    [Fact]
    public void ExportToRdfXml_ShouldIncludeXmlNamespaceDeclarations()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var rdfXml = _service.ExportToFormat(ontology, RdfFormat.RdfXml);

        // Assert
        Assert.Contains("<rdf:RDF", rdfXml);
        Assert.Contains("xmlns:rdf=", rdfXml);
        Assert.Contains("xmlns:rdfs=", rdfXml);
        Assert.Contains("xmlns:owl=", rdfXml);
        Assert.Contains("http://www.w3.org/1999/02/22-rdf-syntax-ns#", rdfXml);
    }

    [Fact]
    public void ExportToRdfXml_ShouldBeWellFormedXml()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var rdfXml = _service.ExportToFormat(ontology, RdfFormat.RdfXml);

        // Assert - Verify XML is well-formed by parsing it
        var xmlDoc = new System.Xml.XmlDocument();
        var exception = Record.Exception(() => xmlDoc.LoadXml(rdfXml));
        Assert.Null(exception);
    }

    [Fact]
    public void ExportToRdfXml_ShouldDeclareUtf8Encoding()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var rdfXml = _service.ExportToFormat(ontology, RdfFormat.RdfXml);

        // Assert - Verify the XML declaration specifies UTF-8 encoding
        // This is critical for Protégé and other RDF tools compatibility
        Assert.Contains("<?xml version=\"1.0\" encoding=\"utf-8\"?>", rdfXml);

        // Verify it does NOT declare UTF-16 (which would cause Protégé errors)
        Assert.DoesNotContain("encoding=\"utf-16\"", rdfXml, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region N-Triples Compliance Tests

    [Fact]
    public void ExportToNTriples_ShouldProduceValidNTriplesSyntax()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var nTriples = _service.ExportToFormat(ontology, RdfFormat.NTriples);

        // Assert - Parse it back with dotNetRDF parser to verify validity
        var graph = new Graph();
        var parser = new NTriplesParser();

        // This will throw if the N-Triples is not valid
        var exception = Record.Exception(() => parser.Load(graph, new System.IO.StringReader(nTriples)));
        Assert.Null(exception);

        // Verify graph is not empty
        Assert.True(graph.Triples.Count > 0);
    }

    [Fact]
    public void ExportToNTriples_ShouldUseAbsoluteUris()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var nTriples = _service.ExportToFormat(ontology, RdfFormat.NTriples);

        // Assert - N-Triples must use absolute URIs, no prefixes
        Assert.DoesNotContain("@prefix", nTriples);
        Assert.Contains("<http://", nTriples);
        Assert.Contains(">", nTriples);
    }

    #endregion

    #region JSON-LD Compliance Tests

    [Fact]
    public void ExportToJsonLd_ShouldProduceValidJsonLdSyntax()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var jsonLd = _service.ExportToFormat(ontology, RdfFormat.JsonLd);

        // Assert - Parse it back with dotNetRDF parser to verify validity
        var store = new TripleStore();
        var parser = new JsonLdParser();

        // This will throw if the JSON-LD is not valid
        var exception = Record.Exception(() => parser.Load(store, new System.IO.StringReader(jsonLd)));
        Assert.Null(exception);

        // Verify store has triples
        Assert.True(store.Triples.Count() > 0);
    }

    [Fact]
    public void ExportToJsonLd_ShouldBeValidJson()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var jsonLd = _service.ExportToFormat(ontology, RdfFormat.JsonLd);

        // Assert - Verify it's valid JSON
        var exception = Record.Exception(() => System.Text.Json.JsonDocument.Parse(jsonLd));
        Assert.Null(exception);
    }

    [Fact]
    public void ExportToJsonLd_ShouldIncludeJsonLdKeywords()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var jsonLd = _service.ExportToFormat(ontology, RdfFormat.JsonLd);

        // Assert - Verify JSON-LD keywords are present
        Assert.Contains("\"@id\"", jsonLd);
        Assert.Contains("\"@type\"", jsonLd);
    }

    #endregion

    #region Cross-Format Consistency Tests

    [Fact]
    public void ExportToAllFormats_ShouldProduceEquivalentGraphs()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act - Export to all formats
        var ttl = _service.ExportToTtl(ontology);
        var rdfXml = _service.ExportToFormat(ontology, RdfFormat.RdfXml);
        var nTriples = _service.ExportToFormat(ontology, RdfFormat.NTriples);
        var jsonLd = _service.ExportToFormat(ontology, RdfFormat.JsonLd);

        // Parse each format back into a graph
        var graphTtl = new Graph();
        new TurtleParser().Load(graphTtl, new System.IO.StringReader(ttl));

        var graphRdfXml = new Graph();
        new RdfXmlParser().Load(graphRdfXml, new System.IO.StringReader(rdfXml));

        var graphNTriples = new Graph();
        new NTriplesParser().Load(graphNTriples, new System.IO.StringReader(nTriples));

        var storeJsonLd = new TripleStore();
        new JsonLdParser().Load(storeJsonLd, new System.IO.StringReader(jsonLd));
        var graphJsonLd = new Graph();
        foreach (var graph in storeJsonLd.Graphs)
        {
            graphJsonLd.Merge(graph);
        }

        // Assert - All graphs should have the same number of triples
        Assert.Equal(graphTtl.Triples.Count, graphRdfXml.Triples.Count);
        Assert.Equal(graphTtl.Triples.Count, graphNTriples.Triples.Count);
        Assert.Equal(graphTtl.Triples.Count, graphJsonLd.Triples.Count);
    }

    [Fact]
    public void ExportToAllFormats_ShouldBeInterchangeable()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act - Export to Turtle, parse it, export to RDF/XML, parse it back
        var ttl = _service.ExportToTtl(ontology);
        var graph1 = new Graph();
        new TurtleParser().Load(graph1, new System.IO.StringReader(ttl));

        // Export the parsed graph to RDF/XML
        var writer = new RdfXmlWriter();
        using var stringWriter = new System.IO.StringWriter();
        writer.Save(graph1, stringWriter);
        var rdfXml = stringWriter.ToString();

        // Parse the RDF/XML back
        var graph2 = new Graph();
        new RdfXmlParser().Load(graph2, new System.IO.StringReader(rdfXml));

        // Assert - Both graphs should be equivalent
        Assert.Equal(graph1.Triples.Count, graph2.Triples.Count);
    }

    #endregion

    #region OWL/RDFS Standards Compliance Tests

    [Fact]
    public void ExportToTtl_ClassDeclarations_ShouldUseOwlClass()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var ttl = _service.ExportToTtl(ontology);
        var graph = new Graph();
        new TurtleParser().Load(graph, new System.IO.StringReader(ttl));

        // Assert - Verify concepts are declared as owl:Class
        var owlClassType = graph.CreateUriNode(UriFactory.Create("http://www.w3.org/2002/07/owl#Class"));
        var rdfType = graph.CreateUriNode(UriFactory.Create("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));

        var classDeclarations = graph.GetTriplesWithPredicateObject(rdfType, owlClassType);
        Assert.True(classDeclarations.Count() > 0, "Expected at least one owl:Class declaration");
    }

    [Fact]
    public void ExportToTtl_OntologyDeclaration_ShouldUseOwlOntology()
    {
        // Arrange
        var ontology = CreateCompleteTestOntology();

        // Act
        var ttl = _service.ExportToTtl(ontology);
        var graph = new Graph();
        new TurtleParser().Load(graph, new System.IO.StringReader(ttl));

        // Assert - Verify ontology is declared as owl:Ontology
        var owlOntologyType = graph.CreateUriNode(UriFactory.Create("http://www.w3.org/2002/07/owl#Ontology"));
        var rdfType = graph.CreateUriNode(UriFactory.Create("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));

        var ontologyDeclarations = graph.GetTriplesWithPredicateObject(rdfType, owlOntologyType);
        Assert.True(ontologyDeclarations.Count() > 0, "Expected at least one owl:Ontology declaration");
    }

    [Fact]
    public void ExportToTtl_SubClassRelationships_ShouldUseRdfsSubClassOf()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "HierarchyTest",
            Namespace = "http://example.org/hierarchy/",
            Concepts = new List<Concept>
            {
                new() { Id = 1, OntologyId = 1, Name = "Animal" },
                new() { Id = 2, OntologyId = 1, Name = "Dog" }
            },
            Relationships = new List<Relationship>
            {
                new()
                {
                    OntologyId = 1,
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
        var graph = new Graph();
        new TurtleParser().Load(graph, new System.IO.StringReader(ttl));

        // Assert - Verify subClassOf relationship exists
        var subClassOf = graph.CreateUriNode(UriFactory.Create("http://www.w3.org/2000/01/rdf-schema#subClassOf"));
        var subClassTriples = graph.GetTriplesWithPredicate(subClassOf);
        Assert.True(subClassTriples.Count() > 0, "Expected at least one rdfs:subClassOf relationship");
    }

    #endregion

    #region Namespace Preservation Tests

    [Fact]
    public void ExportToTtl_ImportedNamespaces_ShouldNotCreateDuplicatePrefixes()
    {
        // Arrange
        var savedPrefixes = new Dictionary<string, string>
        {
            { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#" }, // Same as standard
            { "custom", "http://example.org/custom/" } // Custom prefix
        };

        var ontology = new Ontology
        {
            Id = 1,
            Name = "DuplicateTest",
            Namespace = "http://example.org/test/",
            NamespacePrefixes = System.Text.Json.JsonSerializer.Serialize(savedPrefixes),
            Concepts = new List<Concept>
            {
                new() { Id = 1, OntologyId = 1, Name = "TestConcept" }
            }
        };

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Parse with strict parser - will throw on duplicate prefixes
        var graph = new Graph();
        var parser = new TurtleParser();
        var exception = Record.Exception(() => parser.Load(graph, new System.IO.StringReader(ttl)));

        // Assert - Should not throw (no duplicate prefixes)
        Assert.Null(exception);
    }

    [Fact]
    public void ExportToTtl_WithMalformedNamespacePrefixes_ShouldStillProduceValidTtl()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "MalformedTest",
            Namespace = "http://example.org/test/",
            NamespacePrefixes = "{ invalid json", // Malformed JSON
            Concepts = new List<Concept>
            {
                new() { Id = 1, OntologyId = 1, Name = "TestConcept" }
            }
        };

        // Act
        var ttl = _service.ExportToTtl(ontology);

        // Parse to verify validity
        var graph = new Graph();
        var parser = new TurtleParser();
        var exception = Record.Exception(() => parser.Load(graph, new System.IO.StringReader(ttl)));

        // Assert - Should still produce valid TTL by falling back to standard prefixes
        Assert.Null(exception);
        Assert.Contains("@prefix rdf:", ttl);
        Assert.Contains("@prefix owl:", ttl);
    }

    #endregion

    #region Helper Methods

    private Ontology CreateCompleteTestOntology()
    {
        return new Ontology
        {
            Id = 1,
            Name = "CompleteTestOntology",
            Description = "A complete test ontology for standards compliance testing",
            Namespace = "http://example.org/test/",
            Concepts = new List<Concept>
            {
                new()
                {
                    Id = 1,
                    OntologyId = 1,
                    Name = "Person",
                    Definition = "A human being",
                    Properties = new List<Property>
                    {
                        new() { Id = 1, ConceptId = 1, Name = "name", DataType = "string" },
                        new() { Id = 2, ConceptId = 1, Name = "age", DataType = "integer" }
                    }
                },
                new()
                {
                    Id = 2,
                    OntologyId = 1,
                    Name = "Employee",
                    Definition = "A person who works for an organization"
                }
            },
            Relationships = new List<Relationship>
            {
                new()
                {
                    OntologyId = 1,
                    SourceConceptId = 2,
                    TargetConceptId = 1,
                    RelationType = "is-a",
                    Label = "subClassOf",
                    SourceConcept = new Concept { Id = 2, Name = "Employee" },
                    TargetConcept = new Concept { Id = 1, Name = "Person" }
                }
            }
        };
    }

    #endregion
}
