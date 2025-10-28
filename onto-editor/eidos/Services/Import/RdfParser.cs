using Eidos.Models;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace Eidos.Services.Import;

/// <summary>
/// Parses RDF/TTL files into IGraph
/// </summary>
public class RdfParser : IRdfParser
{
    // Maximum file size: 10MB (defense-in-depth, matches frontend validation)
    private const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

    public Task<TtlImportResult> ParseAsync(Stream fileStream)
    {
        try
        {
            var graph = new Graph();

            // Security: Validate stream size before reading to prevent DoS attacks
            if (fileStream.CanSeek && fileStream.Length > MAX_FILE_SIZE_BYTES)
            {
                return Task.FromResult(new TtlImportResult
                {
                    Success = false,
                    ErrorMessage = $"File size exceeds maximum allowed size of {MAX_FILE_SIZE_BYTES / (1024 * 1024)}MB"
                });
            }

            // Read the content to detect format with size limit enforcement
            using var memoryStream = new MemoryStream();

            // Security: Copy with buffer to prevent memory exhaustion from large files
            var buffer = new byte[8192]; // 8KB buffer
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalBytesRead += bytesRead;

                // Security: Enforce size limit during copy for non-seekable streams
                if (totalBytesRead > MAX_FILE_SIZE_BYTES)
                {
                    return Task.FromResult(new TtlImportResult
                    {
                        Success = false,
                        ErrorMessage = $"File size exceeds maximum allowed size of {MAX_FILE_SIZE_BYTES / (1024 * 1024)}MB"
                    });
                }

                memoryStream.Write(buffer, 0, bytesRead);
            }

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
