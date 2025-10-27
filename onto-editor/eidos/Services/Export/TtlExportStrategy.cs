using Eidos.Constants;
using Eidos.Models;
using VDS.RDF;
using VDS.RDF.Writing;

namespace Eidos.Services.Export;

/// <summary>
/// Supported RDF serialization formats
/// </summary>
public enum RdfFormat
{
    Turtle,
    RdfXml,
    NTriples,
    JsonLd
}

public class TtlExportStrategy : IExportStrategy
{
    private readonly RdfFormat _format;

    public TtlExportStrategy() : this(RdfFormat.Turtle)
    {
    }

    public TtlExportStrategy(RdfFormat format)
    {
        _format = format;
    }

    public string FormatName => _format.ToString();

    public string FileExtension => _format switch
    {
        RdfFormat.RdfXml => ".rdf",
        RdfFormat.NTriples => ".nt",
        RdfFormat.JsonLd => ".jsonld",
        _ => ".ttl"
    };

    public string ContentType => _format switch
    {
        RdfFormat.RdfXml => "application/rdf+xml",
        RdfFormat.NTriples => "application/n-triples",
        RdfFormat.JsonLd => "application/ld+json",
        _ => "text/turtle"
    };

    public string Export(Ontology ontology)
    {
        var graph = BuildRdfGraph(ontology);

        using var stringWriter = new System.IO.StringWriter();

        // JsonLdWriter requires a TripleStore, not just a Graph
        if (_format == RdfFormat.JsonLd)
        {
            var store = new VDS.RDF.TripleStore();
            store.Add(graph);
            var jsonLdWriter = new JsonLdWriter();

            // Write to a temp file since JsonLdWriter doesn't support TextWriter
            var tempFile = System.IO.Path.GetTempFileName();
            try
            {
                jsonLdWriter.Save(store, tempFile);
                return System.IO.File.ReadAllText(tempFile);
            }
            finally
            {
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }

        IRdfWriter writer = _format switch
        {
            RdfFormat.RdfXml => new RdfXmlWriter(),
            RdfFormat.NTriples => new NTriplesWriter(),
            _ => new CompressingTurtleWriter()
        };

        writer.Save(graph, stringWriter);
        return stringWriter.ToString();
    }

    private IGraph BuildRdfGraph(Ontology ontology)
    {
        var graph = new Graph();
        graph.NamespaceMap.AddNamespace("rdf", UriFactory.Create(OntologyNamespaces.RdfSyntax));
        graph.NamespaceMap.AddNamespace("rdfs", UriFactory.Create(OntologyNamespaces.RdfSchema));
        graph.NamespaceMap.AddNamespace("owl", UriFactory.Create(OntologyNamespaces.Owl));
        graph.NamespaceMap.AddNamespace("dc", UriFactory.Create(OntologyNamespaces.DublinCoreElements));

        if (ontology.UsesBFO)
        {
            graph.NamespaceMap.AddNamespace("bfo", UriFactory.Create(OntologyNamespaces.BfoPrefix));
        }

        if (ontology.UsesProvO)
        {
            graph.NamespaceMap.AddNamespace("prov", UriFactory.Create(OntologyNamespaces.ProvO));
        }

        // Create base URI for this ontology - use custom namespace if provided
        var baseUri = !string.IsNullOrWhiteSpace(ontology.Namespace)
            ? ontology.Namespace
            : OntologyNamespaces.CreateDefaultNamespace(ontology.Name);

        // Ensure namespace ends with / or #
        baseUri = OntologyNamespaces.NormalizeNamespace(baseUri);

        graph.BaseUri = UriFactory.Create(baseUri);

        // Ontology metadata
        var ontologyNode = graph.CreateUriNode(UriFactory.Create(baseUri));
        var rdfType = graph.CreateUriNode("rdf:type");
        var owlOntology = graph.CreateUriNode("owl:Ontology");
        graph.Assert(ontologyNode, rdfType, owlOntology);

        if (!string.IsNullOrWhiteSpace(ontology.Description))
        {
            var dcDescription = graph.CreateUriNode("dc:description");
            var descriptionLiteral = graph.CreateLiteralNode(ontology.Description);
            graph.Assert(ontologyNode, dcDescription, descriptionLiteral);
        }

        if (!string.IsNullOrWhiteSpace(ontology.Author))
        {
            var dcCreator = graph.CreateUriNode("dc:creator");
            var authorLiteral = graph.CreateLiteralNode(ontology.Author);
            graph.Assert(ontologyNode, dcCreator, authorLiteral);
        }

        if (!string.IsNullOrWhiteSpace(ontology.Version))
        {
            var owlVersionInfo = graph.CreateUriNode("owl:versionInfo");
            var versionLiteral = graph.CreateLiteralNode(ontology.Version);
            graph.Assert(ontologyNode, owlVersionInfo, versionLiteral);
        }

        if (!string.IsNullOrWhiteSpace(ontology.License))
        {
            var dcLicense = graph.CreateUriNode("dc:license");
            var licenseLiteral = graph.CreateLiteralNode(ontology.License);
            graph.Assert(ontologyNode, dcLicense, licenseLiteral);
        }

        if (!string.IsNullOrWhiteSpace(ontology.Tags))
        {
            var dcSubject = graph.CreateUriNode("dc:subject");
            var tagsLiteral = graph.CreateLiteralNode(ontology.Tags);
            graph.Assert(ontologyNode, dcSubject, tagsLiteral);
        }

        var owlClass = graph.CreateUriNode("owl:Class");
        var rdfsLabel = graph.CreateUriNode("rdfs:label");
        var rdfsComment = graph.CreateUriNode("rdfs:comment");
        var rdfsSubClassOf = graph.CreateUriNode("rdfs:subClassOf");

        // Export concepts as OWL classes
        foreach (var concept in ontology.Concepts)
        {
            var conceptUri = CreateConceptUri(baseUri, concept);
            var conceptNode = graph.CreateUriNode(UriFactory.Create(conceptUri));

            // Type declaration
            graph.Assert(conceptNode, rdfType, owlClass);

            // Label
            var labelLiteral = graph.CreateLiteralNode(concept.Name);
            graph.Assert(conceptNode, rdfsLabel, labelLiteral);

            // Definition as comment
            if (!string.IsNullOrWhiteSpace(concept.Definition))
            {
                var definitionLiteral = graph.CreateLiteralNode(concept.Definition);
                graph.Assert(conceptNode, rdfsComment, definitionLiteral);
            }

            // Simple explanation
            if (!string.IsNullOrWhiteSpace(concept.SimpleExplanation))
            {
                var explanationPredicate = graph.CreateUriNode(UriFactory.Create(baseUri + "simpleExplanation"));
                var explanationLiteral = graph.CreateLiteralNode(concept.SimpleExplanation);
                graph.Assert(conceptNode, explanationPredicate, explanationLiteral);
            }

            // Examples
            if (!string.IsNullOrWhiteSpace(concept.Examples))
            {
                var examplesPredicate = graph.CreateUriNode(UriFactory.Create(baseUri + "examples"));
                var examplesLiteral = graph.CreateLiteralNode(concept.Examples);
                graph.Assert(conceptNode, examplesPredicate, examplesLiteral);
            }

            // Category
            if (!string.IsNullOrWhiteSpace(concept.Category))
            {
                var categoryPredicate = graph.CreateUriNode(UriFactory.Create(baseUri + "category"));
                var categoryLiteral = graph.CreateLiteralNode(concept.Category);
                graph.Assert(conceptNode, categoryPredicate, categoryLiteral);
            }
        }

        // Export relationships
        foreach (var relationship in ontology.Relationships)
        {
            var sourceUri = CreateConceptUri(baseUri, relationship.SourceConcept);
            var targetUri = CreateConceptUri(baseUri, relationship.TargetConcept);

            var sourceNode = graph.CreateUriNode(UriFactory.Create(sourceUri));
            var targetNode = graph.CreateUriNode(UriFactory.Create(targetUri));

            // Handle is-a relationships as rdfs:subClassOf
            if (relationship.RelationType.ToLower() == "is-a")
            {
                graph.Assert(sourceNode, rdfsSubClassOf, targetNode);
            }
            else
            {
                // Create custom property for other relationship types
                var propertyUri = CreatePropertyUri(baseUri, relationship.RelationType);
                var propertyNode = graph.CreateUriNode(UriFactory.Create(propertyUri));
                graph.Assert(sourceNode, propertyNode, targetNode);
            }
        }

        return graph;
    }

    private string CreateConceptUri(string baseUri, Concept concept)
    {
        // Check if this is a PROV-O concept
        if (concept.Name.StartsWith("prov:"))
        {
            var provName = concept.Name.Substring(5); // Remove "prov:" prefix
            return OntologyNamespaces.CreateProvOUri(provName);
        }

        // Check if this is a BFO concept (simplified check)
        if (concept.SourceOntology == "BFO" || new[] { "Entity", "Continuant", "Occurrent", "Process", "Temporal Region", "Independent Continuant", "Dependent Continuant" }.Contains(concept.Name))
        {
            var bfoName = concept.Name.Replace(" ", "");
            return OntologyNamespaces.CreateBfoUri(bfoName);
        }

        // Regular concept
        var localName = concept.Name.Replace(" ", "_").Replace("-", "_");
        return baseUri + localName;
    }

    private string CreatePropertyUri(string baseUri, string relationType)
    {
        // Check for PROV-O relationship types
        if (new[] { "wasGeneratedBy", "used", "wasAssociatedWith", "wasAttributedTo", "wasDerivedFrom" }.Contains(relationType))
        {
            return OntologyNamespaces.CreateProvOUri(relationType);
        }

        // Check for common OWL/RDFS properties
        if (relationType.ToLower() == "depends-on")
        {
            return baseUri + "dependsOn";
        }

        var localName = relationType.Replace(" ", "_").Replace("-", "_");
        return baseUri + localName;
    }
}
