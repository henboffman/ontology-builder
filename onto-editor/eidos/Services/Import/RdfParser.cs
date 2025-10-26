using Eidos.Models;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace Eidos.Services.Import;

/// <summary>
/// Parses RDF/TTL files into IGraph
/// </summary>
public class RdfParser : IRdfParser
{
    public Task<TtlImportResult> ParseAsync(Stream fileStream)
    {
        try
        {
            var graph = new Graph();

            // Read the content to detect format
            using var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            var content = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            memoryStream.Position = 0;

            // Detect format based on content
            IRdfReader parser;
            if (content.TrimStart().StartsWith("<") && (content.Contains("<?xml") || content.Contains("<rdf:RDF") || content.Contains("xmlns:rdf")))
            {
                // RDF/XML format
                parser = new RdfXmlParser();
            }
            else
            {
                // TTL/Turtle format
                parser = new TurtleParser();
            }

            using (var reader = new StreamReader(memoryStream))
            {
                parser.Load(graph, reader);
            }

            var result = new TtlImportResult
            {
                Success = true,
                ConceptCount = 0,
                RelationshipCount = 0,
                ParsedGraph = graph
            };

            // Extract ontology metadata
            var ontologyNode = graph.Triples
                .Where(t => t.Predicate.ToString().Contains("type") &&
                            t.Object.ToString().Contains("Ontology"))
                .Select(t => t.Subject)
                .FirstOrDefault();

            if (ontologyNode != null)
            {
                result.OntologyName = RdfUtilities.ExtractLocalName(ontologyNode.ToString());
                result.OntologyUri = ontologyNode.ToString();

                // Try to get ontology description
                var descTriple = graph.Triples
                    .Where(t => t.Subject.Equals(ontologyNode) &&
                               (t.Predicate.ToString().Contains("comment") ||
                                t.Predicate.ToString().Contains("description")))
                    .FirstOrDefault();

                if (descTriple != null && descTriple.Object is ILiteralNode literal)
                {
                    result.OntologyDescription = literal.Value;
                }
            }

            // Count classes (concepts) - excluding blank nodes
            // Support OWL/RDFS classes and SKOS concepts
            var classTriples = graph.Triples
                .Where(t => t.Predicate.ToString().Contains("type") &&
                           (t.Object.ToString().Contains("Class") ||
                            t.Object.ToString().Contains("owl#Class") ||
                            t.Object.ToString().Contains("skos#Concept") ||
                            t.Object.ToString().Contains("/skos/core#Concept")) &&
                           !(t.Subject is IBlankNode))
                .ToList();

            result.ConceptCount = classTriples.Count(t =>
            {
                var localName = RdfUtilities.ExtractLocalName(t.Subject.ToString());
                return !localName.StartsWith("_:") && !localName.StartsWith("autos");
            });

            // Count relationships
            var subClassTriples = graph.Triples
                .Where(t => t.Predicate.ToString().Contains("subClassOf") && !(t.Subject is IBlankNode))
                .ToList();

            var objectProperties = graph.Triples
                .Where(t => t.Predicate.ToString().Contains("type") &&
                           (t.Object.ToString().Contains("ObjectProperty") || t.Object.ToString().Contains("owl#ObjectProperty")))
                .ToList();

            result.RelationshipCount = subClassTriples.Count + objectProperties.Count;

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TtlImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to parse RDF file: {ex.Message}"
            });
        }
    }
}
