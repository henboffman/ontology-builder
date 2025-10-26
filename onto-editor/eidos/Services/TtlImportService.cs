using Eidos.Data;
using Eidos.Models;
using Eidos.Services.Import;
using Eidos.Services.Interfaces;
using VDS.RDF;
using VDS.RDF.Parsing;
using Microsoft.EntityFrameworkCore;

namespace Eidos.Services
{
    /// <summary>
    /// Facade service that delegates to focused import services
    /// Maintains backward compatibility with ITtlImportService
    /// </summary>
    public class TtlImportService : ITtlImportService
    {
        private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
        private readonly IOntologyService _ontologyService;
        private readonly IRdfParser _rdfParser;
        private readonly IOntologyImporter _ontologyImporter;
        private readonly ILogger<TtlImportService> _logger;

        public TtlImportService(
            IDbContextFactory<OntologyDbContext> contextFactory,
            IOntologyService ontologyService,
            IRdfParser rdfParser,
            IOntologyImporter ontologyImporter,
            ILogger<TtlImportService> logger)
        {
            _contextFactory = contextFactory;
            _ontologyService = ontologyService;
            _rdfParser = rdfParser;
            _ontologyImporter = ontologyImporter;
            _logger = logger;
        }

        // Delegate parsing to RdfParser
        public Task<TtlImportResult> ParseTtlFileAsync(Stream fileStream)
        {
            return _rdfParser.ParseAsync(fileStream);
        }

        // Delegate importing to OntologyImporter
        public async Task<Ontology> ImportAsNewOntologyAsync(IGraph graph, string? customName = null, string? customDescription = null)
        {
            return await _ontologyImporter.ImportAsNewAsync(graph, customName, customDescription);
        }

        public async Task<MergePreview> PreviewMergeAsync(int ontologyId, IGraph graph)
        {
            var existingOntology = await _ontologyService.GetOntologyAsync(ontologyId);
            if (existingOntology == null)
            {
                throw new ArgumentException("Ontology not found");
            }

            var preview = new MergePreview
            {
                NewConcepts = new List<string>(),
                ExistingConcepts = new List<string>(),
                NewRelationships = new List<string>(),
                ConflictingConcepts = new List<string>()
            };

            // Get classes from TTL
            var classTriples = graph.Triples
                .Where(t => t.Predicate.ToString().Contains("type") &&
                           (t.Object.ToString().Contains("Class") || t.Object.ToString().Contains("owl#Class")))
                .ToList();

            foreach (var triple in classTriples)
            {
                // Skip blank nodes
                if (triple.Subject is IBlankNode)
                    continue;

                var localName = RdfUtilities.ExtractLocalName(triple.Subject.ToString());
                if (localName.StartsWith("_:") || localName.StartsWith("autos"))
                    continue;

                var className = RdfUtilities.GetLabel(graph, triple.Subject) ?? localName;

                var existingConcept = existingOntology.Concepts
                    .FirstOrDefault(c => c.Name.Equals(className, StringComparison.OrdinalIgnoreCase));

                if (existingConcept != null)
                {
                    var ttlComment = RdfUtilities.GetComment(graph, triple.Subject);
                    if (!string.IsNullOrEmpty(ttlComment) && ttlComment != existingConcept.Definition)
                    {
                        preview.ConflictingConcepts.Add($"{className} (different definitions)");
                    }
                    else
                    {
                        preview.ExistingConcepts.Add(className);
                    }
                }
                else
                {
                    preview.NewConcepts.Add(className);
                }
            }

            // Count new relationships
            var subClassTriples = graph.Triples
                .Where(t => t.Predicate.ToString().Contains("subClassOf"))
                .Count();

            preview.NewRelationships.Add($"{subClassTriples} hierarchical relationships (is-a)");

            return preview;
        }

        public async Task<Ontology> MergeIntoExistingAsync(int ontologyId, IGraph graph, Action<ImportProgress>? onProgress = null, int batchSize = 50)
        {
            var ontology = await _ontologyService.GetOntologyAsync(ontologyId);
            if (ontology == null)
            {
                throw new ArgumentException("Ontology not found");
            }

            _logger.LogInformation($"Starting merge into ontology {ontologyId}");

            onProgress?.Invoke(new ImportProgress
            {
                Stage = "Initializing",
                Current = 0,
                Total = 100,
                Message = "Preparing to import ontology..."
            });

            // Get existing concepts by name for quick lookup
            var existingConceptMap = ontology.Concepts
                .ToDictionary(c => c.Name.ToLower(), c => c);

            // Import classes as concepts
            var classTriples = graph.Triples
                .Where(t => t.Predicate.ToString().Contains("type") &&
                           (t.Object.ToString().Contains("Class") || t.Object.ToString().Contains("owl#Class")))
                .ToList();

            _logger.LogInformation($"Found {classTriples.Count} class triples to process");

            onProgress?.Invoke(new ImportProgress
            {
                Stage = "Importing Concepts",
                Current = 0,
                Total = classTriples.Count,
                Message = $"Processing {classTriples.Count} concepts..."
            });

            var conceptMap = new Dictionary<string, Concept>();
            var processedCount = 0;

            // Process concepts in batches
            for (int i = 0; i < classTriples.Count; i += batchSize)
            {
                var batch = classTriples.Skip(i).Take(batchSize).ToList();

                foreach (var triple in batch)
                {
                    // Skip blank nodes
                    if (triple.Subject is IBlankNode)
                        continue;

                    var localName = RdfUtilities.ExtractLocalName(triple.Subject.ToString());
                    if (localName.StartsWith("_:") || localName.StartsWith("autos"))
                        continue;

                    var label = RdfUtilities.GetLabel(graph, triple.Subject) ?? localName;
                    if (string.IsNullOrWhiteSpace(label)) continue;

                    // Check if concept already exists
                    if (existingConceptMap.TryGetValue(label.ToLower(), out var existingConcept))
                    {
                        // Use existing concept
                        conceptMap[triple.Subject.ToString()] = existingConcept;
                    }
                    else
                    {
                        // Create new concept
                        var comment = RdfUtilities.GetComment(graph, triple.Subject);
                        var concept = new Concept
                        {
                            OntologyId = ontology.Id,
                            Name = label,
                            Definition = comment,
                            SimpleExplanation = comment,
                            Category = "Imported",
                            Color = RdfUtilities.GenerateColorFromName(label),
                            SourceOntology = RdfUtilities.ExtractOntologyName(graph) ?? "Imported Ontology"
                        };

                        var createdConcept = await _ontologyService.CreateConceptAsync(concept);
                        conceptMap[triple.Subject.ToString()] = createdConcept;
                        existingConceptMap[label.ToLower()] = createdConcept;
                    }
                }

                processedCount += batch.Count;
                _logger.LogInformation($"Processed {processedCount}/{classTriples.Count} concepts");

                onProgress?.Invoke(new ImportProgress
                {
                    Stage = "Importing Concepts",
                    Current = processedCount,
                    Total = classTriples.Count,
                    Message = $"Imported {processedCount} of {classTriples.Count} concepts..."
                });

                // Allow other operations to proceed
                await Task.Delay(1);
            }

            // Import relationships (same logic as ImportAsNewOntologyAsync)
            var subClassTriples = graph.Triples
                .Where(t => t.Predicate.ToString().Contains("subClassOf"))
                .ToList();

            _logger.LogInformation($"Found {subClassTriples.Count} relationship triples to process");

            onProgress?.Invoke(new ImportProgress
            {
                Stage = "Importing Relationships",
                Current = 0,
                Total = subClassTriples.Count,
                Message = $"Processing {subClassTriples.Count} relationships..."
            });

            var relationshipsProcessed = 0;

            // Process relationships in batches
            for (int i = 0; i < subClassTriples.Count; i += batchSize)
            {
                var batch = subClassTriples.Skip(i).Take(batchSize).ToList();

                foreach (var triple in batch)
                {
                    var sourceUri = triple.Subject.ToString();
                    var targetUri = triple.Object.ToString();

                    if (conceptMap.TryGetValue(sourceUri, out var sourceConcept) &&
                        conceptMap.TryGetValue(targetUri, out var targetConcept))
                    {
                        // Check if relationship already exists
                        var canCreate = await _ontologyService.CanCreateRelationshipAsync(
                            sourceConcept.Id, targetConcept.Id, "is-a");

                        if (canCreate)
                        {
                            var relationship = new Relationship
                            {
                                OntologyId = ontology.Id,
                                SourceConceptId = sourceConcept.Id,
                                TargetConceptId = targetConcept.Id,
                                RelationType = "is-a",
                                Description = $"{sourceConcept.Name} is a type of {targetConcept.Name}"
                            };

                            await _ontologyService.CreateRelationshipAsync(relationship);
                        }
                    }
                }

                relationshipsProcessed += batch.Count;
                _logger.LogInformation($"Processed {relationshipsProcessed}/{subClassTriples.Count} relationships");

                onProgress?.Invoke(new ImportProgress
                {
                    Stage = "Importing Relationships",
                    Current = relationshipsProcessed,
                    Total = subClassTriples.Count,
                    Message = $"Imported {relationshipsProcessed} of {subClassTriples.Count} relationships..."
                });

                // Allow other operations to proceed
                await Task.Delay(1);
            }

            onProgress?.Invoke(new ImportProgress
            {
                Stage = "Finalizing",
                Current = 100,
                Total = 100,
                Message = "Saving ontology links..."
            });

            // Save linked ontologies
            await SaveLinkedOntologiesAsync(ontology.Id, graph, conceptMap.Count);

            _logger.LogInformation($"Merge complete: {conceptMap.Count} concepts, {relationshipsProcessed} relationships");

            onProgress?.Invoke(new ImportProgress
            {
                Stage = "Complete",
                Current = 100,
                Total = 100,
                Message = $"Import complete! {conceptMap.Count} concepts and {relationshipsProcessed} relationships imported."
            });

            return await _ontologyService.GetOntologyAsync(ontology.Id) ?? ontology;
        }

        private async Task SaveLinkedOntologiesAsync(int ontologyId, IGraph graph, int importedConceptCount)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var linkedOntologies = ExtractLinkedOntologies(graph);

            foreach (var linkedInfo in linkedOntologies)
            {
                // Check if this ontology link already exists
                var existingLink = await context.OntologyLinks
                    .FirstOrDefaultAsync(l => l.OntologyId == ontologyId && l.Uri == linkedInfo.Uri);

                if (existingLink == null)
                {
                    var ontologyLink = new OntologyLink
                    {
                        OntologyId = ontologyId,
                        Uri = linkedInfo.Uri,
                        Name = linkedInfo.Name,
                        Prefix = linkedInfo.Prefix,
                        Description = linkedInfo.Description,
                        ConceptsImported = importedConceptCount > 0,
                        ImportedConceptCount = importedConceptCount,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.OntologyLinks.Add(ontologyLink);
                }
                else
                {
                    // Update existing link
                    existingLink.ConceptsImported = true;
                    existingLink.ImportedConceptCount += importedConceptCount;
                }
            }

            await context.SaveChangesAsync();
        }


        private List<LinkedOntologyInfo> ExtractLinkedOntologies(IGraph graph)
        {
            var linkedOntologies = new List<LinkedOntologyInfo>();
            var knownOntologies = GetKnownOntologyInfo();

            // Extract namespace prefixes from the graph
            foreach (var ns in graph.NamespaceMap.Prefixes)
            {
                var uri = graph.NamespaceMap.GetNamespaceUri(ns).ToString();

                // Skip standard/common prefixes that aren't ontologies
                if (RdfUtilities.IsStandardPrefix(ns)) continue;

                // Check if this is a known ontology
                var knownInfo = knownOntologies.FirstOrDefault(k => uri.Contains(k.Identifier));

                var linkedOnt = new LinkedOntologyInfo
                {
                    Uri = uri,
                    Prefix = ns,
                    Name = knownInfo?.Name ?? RdfUtilities.ExtractOntologyNameFromUri(uri),
                    Description = knownInfo?.Description,
                    ConceptsImported = false
                };

                linkedOntologies.Add(linkedOnt);
            }

            return linkedOntologies;
        }


        private List<KnownOntologyInfo> GetKnownOntologyInfo()
        {
            return new List<KnownOntologyInfo>
            {
                new KnownOntologyInfo
                {
                    Identifier = "bfo",
                    Name = "Basic Formal Ontology (BFO)",
                    Description = "A small, upper level ontology designed for use in supporting information retrieval, analysis and integration in scientific and other domains"
                },
                new KnownOntologyInfo
                {
                    Identifier = "prov",
                    Name = "PROV Ontology (PROV-O)",
                    Description = "The PROV Ontology provides a set of classes, properties, and restrictions for representing and interchanging provenance information"
                },
                new KnownOntologyInfo
                {
                    Identifier = "foaf",
                    Name = "Friend of a Friend (FOAF)",
                    Description = "FOAF is an ontology for describing people, their activities and their relations to other people and objects"
                },
                new KnownOntologyInfo
                {
                    Identifier = "schema",
                    Name = "Schema.org",
                    Description = "Schema.org is a collaborative project providing schemas for structured data on the Internet"
                },
                new KnownOntologyInfo
                {
                    Identifier = "dcat",
                    Name = "Data Catalog Vocabulary (DCAT)",
                    Description = "DCAT is an RDF vocabulary designed to facilitate interoperability between data catalogs"
                }
            };
        }
    }

    public class LinkedOntologyInfo
    {
        public string Uri { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool ConceptsImported { get; set; }
    }

    public class KnownOntologyInfo
    {
        public string Identifier { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class TtlImportResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OntologyName { get; set; }
        public string? OntologyUri { get; set; }
        public string? OntologyDescription { get; set; }
        public int ConceptCount { get; set; }
        public int RelationshipCount { get; set; }
        public IGraph? ParsedGraph { get; set; }
        public List<LinkedOntologyInfo> LinkedOntologies { get; set; } = new();
    }

    public class MergePreview
    {
        public List<string> NewConcepts { get; set; } = new();
        public List<string> ExistingConcepts { get; set; } = new();
        public List<string> ConflictingConcepts { get; set; } = new();
        public List<string> NewRelationships { get; set; } = new();
    }
}
