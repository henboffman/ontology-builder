using Eidos.Constants;
using Eidos.Data;
using Eidos.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace Eidos.Services
{
    public class OntologyDownloadService
    {
        private readonly ILogger<OntologyDownloadService> _logger;
        private readonly IWebHostEnvironment _environment;

        // Local file paths and ontology metadata
        private static readonly Dictionary<string, OntologySource> OntologySources = new()
        {
            ["bfo"] = new OntologySource(
                "wwwroot/ttl/bfo-core.ttl",
                "Basic Formal Ontology",
                "BFO",
                OntologyNamespaces.BfoBase
            ),
            ["ro"] = new OntologySource(
                "wwwroot/rdf/Relations.rdf",
                "Relations Ontology",
                "RO",
                OntologyNamespaces.RelationOntology
            ),
            ["owl"] = new OntologySource(
                "",
                "OWL Web Ontology Language",
                "OWL",
                OntologyNamespaces.Owl
            ),
            ["rdfs"] = new OntologySource(
                "",
                "RDF Schema",
                "RDFS",
                OntologyNamespaces.RdfSchema
            ),
            ["skos"] = new OntologySource(
                "wwwroot/rdf/skos.rdf",
                "Simple Knowledge Organization System",
                "SKOS",
                OntologyNamespaces.Skos
            ),
            ["foaf"] = new OntologySource(
                "wwwroot/rdf/foaf.rdf",
                "Friend of a Friend",
                "FOAF",
                "http://xmlns.com/foaf/0.1/"
            ),
            ["schema"] = new OntologySource(
                "wwwroot/ttl/schema.ttl",
                "Schema.org",
                "schema",
                "https://schema.org/"
            ),
            ["dcterms"] = new OntologySource(
                "wwwroot/rdf/dublin_core_terms.rdf",
                "Dublin Core Metadata Terms",
                "dcterms",
                OntologyNamespaces.DublinCoreTerms
            ),
            ["software-description"] = new OntologySource(
                "wwwroot/ttl/software-description.ttl",
                "Software Description Ontology",
                "sd",
                "http://example.org/software-description/"
            )
        };

        public OntologyDownloadService(
            ILogger<OntologyDownloadService> logger,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async Task<string?> GetOntologyAsync(string templateKey)
        {
            if (!OntologySources.TryGetValue(templateKey, out var source))
            {
                _logger.LogWarning($"Unknown ontology template: {templateKey}");
                return null;
            }

            // Check if local file path is specified
            if (string.IsNullOrEmpty(source.Url))
            {
                _logger.LogWarning($"No local file configured for {templateKey}");
                return null;
            }

            // Build the full path to the local file
            var localFilePath = Path.Combine(_environment.ContentRootPath, source.Url);

            // Read from local file
            try
            {
                if (!File.Exists(localFilePath))
                {
                    _logger.LogError($"Local ontology file not found: {localFilePath}");
                    return null;
                }

                _logger.LogInformation($"Loading ontology from local file: {localFilePath}");
                var content = await File.ReadAllTextAsync(localFilePath);

                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning($"Local ontology file is empty: {localFilePath}");
                    return null;
                }

                _logger.LogInformation($"Successfully loaded {templateKey} from local file ({content.Length} bytes)");
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to read local ontology file: {localFilePath}");
                return null;
            }
        }

        public OntologySource? GetOntologySource(string templateKey)
        {
            OntologySources.TryGetValue(templateKey, out var source);
            return source;
        }

        public IEnumerable<KeyValuePair<string, OntologySource>> GetAllSources()
        {
            return OntologySources;
        }

    }

    public record OntologySource(
        string Url,
        string Name,
        string Prefix,
        string Namespace
    );
}
