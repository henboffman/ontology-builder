using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services.Interfaces;
using VDS.RDF;

namespace Eidos.Services.Import;

/// <summary>
/// Imports RDF graphs as new ontologies
/// </summary>
public class OntologyImporter : IOntologyImporter
{
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IConceptService _conceptService;
    private readonly IRelationshipService _relationshipService;
    private readonly ILogger<OntologyImporter> _logger;

    public OntologyImporter(
        IOntologyRepository ontologyRepository,
        IConceptService conceptService,
        IRelationshipService relationshipService,
        ILogger<OntologyImporter> logger)
    {
        _ontologyRepository = ontologyRepository;
        _conceptService = conceptService;
        _relationshipService = relationshipService;
        _logger = logger;
    }

    public async Task<Ontology> ImportAsNewAsync(IGraph graph, string? customName = null, string? customDescription = null)
    {
        // Create new ontology
        var ontology = new Ontology
        {
            Name = customName ?? RdfUtilities.ExtractOntologyName(graph) ?? "Imported Ontology",
            Description = customDescription ?? RdfUtilities.ExtractOntologyDescription(graph) ?? "Imported from RDF file",
            Version = "1.0 (imported)"
        };

        ontology = await _ontologyRepository.AddAsync(ontology);

        // Import classes as concepts
        // Support OWL/RDFS classes and SKOS concepts
        var classTriples = graph.Triples
            .Where(t => t.Predicate.ToString().Contains("type") &&
                       (t.Object.ToString().Contains("Class") ||
                        t.Object.ToString().Contains("owl#Class") ||
                        t.Object.ToString().Contains("skos#Concept") ||
                        t.Object.ToString().Contains("/skos/core#Concept")))
            .ToList();

        // Debug logging
        _logger.LogInformation("[SKOS Import] Found {ClassTriplesCount} class/concept triples", classTriples.Count);
        _logger.LogInformation("[SKOS Import] Total triples in graph: {TotalTriples}", graph.Triples.Count);

        // Check for SKOS-specific patterns
        var skosConceptTriples = graph.Triples
            .Where(t => t.Object.ToString().Contains("skos") && t.Object.ToString().Contains("Concept"))
            .ToList();
        _logger.LogInformation("[SKOS Import] Found {SkosConceptTriples} triples with 'skos' and 'Concept'", skosConceptTriples.Count);

        foreach (var t in skosConceptTriples.Take(5))
        {
            _logger.LogDebug("  - Subject: {Subject}, Predicate: {Predicate}, Object: {Object}", t.Subject, t.Predicate, t.Object);
        }

        var conceptMap = new Dictionary<string, Concept>();

        foreach (var triple in classTriples)
        {
            if (triple.Subject is IBlankNode)
                continue;

            var className = RdfUtilities.ExtractLocalName(triple.Subject.ToString());
            if (string.IsNullOrWhiteSpace(className) || className.StartsWith("_:") || className.StartsWith("autos"))
                continue;

            var label = RdfUtilities.GetLabel(graph, triple.Subject) ?? className;
            var comment = RdfUtilities.GetComment(graph, triple.Subject);

            var concept = new Concept
            {
                OntologyId = ontology.Id,
                Name = label,
                Definition = comment,
                SimpleExplanation = comment,
                Category = "Imported",
                Color = RdfUtilities.GenerateColorFromName(className),
                SourceOntology = customName ?? RdfUtilities.ExtractOntologyName(graph) ?? "Imported Ontology"
            };

            var createdConcept = await _conceptService.CreateAsync(concept, recordUndo: false);
            conceptMap[triple.Subject.ToString()] = createdConcept;
        }

        // Import subClassOf and SKOS broader/narrower relationships
        var subClassTriples = graph.Triples
            .Where(t => t.Predicate.ToString().Contains("subClassOf"))
            .ToList();

        var skosNarrowerTriples = graph.Triples
            .Where(t => t.Predicate.ToString().Contains("narrower") || t.Predicate.ToString().Contains("skos#narrower"))
            .ToList();

        var skosBroaderTriples = graph.Triples
            .Where(t => t.Predicate.ToString().Contains("broader") || t.Predicate.ToString().Contains("skos#broader"))
            .ToList();

        foreach (var triple in subClassTriples)
        {
            var sourceUri = triple.Subject.ToString();
            var targetUri = triple.Object.ToString();

            if (conceptMap.TryGetValue(sourceUri, out var sourceConcept) &&
                conceptMap.TryGetValue(targetUri, out var targetConcept))
            {
                var relationship = new Relationship
                {
                    OntologyId = ontology.Id,
                    SourceConceptId = sourceConcept.Id,
                    TargetConceptId = targetConcept.Id,
                    RelationType = "subclass-of",
                    Label = "subClassOf",
                    OntologyUri = "http://www.w3.org/2000/01/rdf-schema#subClassOf",
                    Description = $"{sourceConcept.Name} is a subclass of {targetConcept.Name}"
                };

                await _relationshipService.CreateAsync(relationship, recordUndo: false);
            }
        }

        // Import SKOS broader relationships (concept A is broader than concept B means B is narrower than A)
        foreach (var triple in skosBroaderTriples)
        {
            var sourceUri = triple.Subject.ToString();
            var targetUri = triple.Object.ToString();

            if (conceptMap.TryGetValue(sourceUri, out var sourceConcept) &&
                conceptMap.TryGetValue(targetUri, out var targetConcept))
            {
                var relationship = new Relationship
                {
                    OntologyId = ontology.Id,
                    SourceConceptId = sourceConcept.Id,
                    TargetConceptId = targetConcept.Id,
                    RelationType = "broader",
                    Label = "skos:broader",
                    OntologyUri = "http://www.w3.org/2004/02/skos/core#broader",
                    Description = $"{sourceConcept.Name} is broader than {targetConcept.Name}"
                };

                await _relationshipService.CreateAsync(relationship, recordUndo: false);
            }
        }

        // Import SKOS narrower relationships
        foreach (var triple in skosNarrowerTriples)
        {
            var sourceUri = triple.Subject.ToString();
            var targetUri = triple.Object.ToString();

            if (conceptMap.TryGetValue(sourceUri, out var sourceConcept) &&
                conceptMap.TryGetValue(targetUri, out var targetConcept))
            {
                var relationship = new Relationship
                {
                    OntologyId = ontology.Id,
                    SourceConceptId = sourceConcept.Id,
                    TargetConceptId = targetConcept.Id,
                    RelationType = "narrower",
                    Label = "skos:narrower",
                    OntologyUri = "http://www.w3.org/2004/02/skos/core#narrower",
                    Description = $"{sourceConcept.Name} is narrower than {targetConcept.Name}"
                };

                await _relationshipService.CreateAsync(relationship, recordUndo: false);
            }
        }

        // Import object properties as relationships
        var objectPropertyTriples = graph.Triples
            .Where(t => t.Predicate.ToString().Contains("type") &&
                       t.Object.ToString().Contains("ObjectProperty"))
            .ToList();

        foreach (var propTriple in objectPropertyTriples)
        {
            var propertyName = RdfUtilities.ExtractLocalName(propTriple.Subject.ToString());

            var domainTriple = graph.Triples
                .Where(t => t.Subject.Equals(propTriple.Subject) && t.Predicate.ToString().Contains("domain"))
                .FirstOrDefault();

            var rangeTriple = graph.Triples
                .Where(t => t.Subject.Equals(propTriple.Subject) && t.Predicate.ToString().Contains("range"))
                .FirstOrDefault();

            if (domainTriple != null && rangeTriple != null &&
                conceptMap.TryGetValue(domainTriple.Object.ToString(), out var domainConcept) &&
                conceptMap.TryGetValue(rangeTriple.Object.ToString(), out var rangeConcept))
            {
                var relationship = new Relationship
                {
                    OntologyId = ontology.Id,
                    SourceConceptId = domainConcept.Id,
                    TargetConceptId = rangeConcept.Id,
                    RelationType = propertyName ?? "related-to",
                    Description = RdfUtilities.GetComment(graph, propTriple.Subject)
                };

                await _relationshipService.CreateAsync(relationship, recordUndo: false);
            }
        }

        // Reload to get all data
        return await _ontologyRepository.GetWithAllRelatedDataAsync(ontology.Id) ?? ontology;
    }
}
