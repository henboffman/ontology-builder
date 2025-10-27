using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services.Interfaces;

namespace Eidos.Services
{
    /// <summary>
    /// Facade service that coordinates ontology operations
    /// Delegates to focused services for Single Responsibility
    /// </summary>
    public class OntologyService : IOntologyService
    {
        private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
        private readonly IOntologyRepository _ontologyRepository;
        private readonly IConceptService _conceptService;
        private readonly IRelationshipService _relationshipService;
        private readonly IPropertyService _propertyService;
        private readonly IRelationshipSuggestionService _suggestionService;
        private readonly Commands.CommandInvoker _commandInvoker;
        private readonly IUserService _userService;
        private readonly IOntologyShareService _shareService;

        public OntologyService(
            IDbContextFactory<OntologyDbContext> contextFactory,
            IOntologyRepository ontologyRepository,
            IConceptService conceptService,
            IRelationshipService relationshipService,
            IPropertyService propertyService,
            IRelationshipSuggestionService suggestionService,
            Commands.CommandInvoker commandInvoker,
            IUserService userService,
            IOntologyShareService shareService)
        {
            _contextFactory = contextFactory;
            _ontologyRepository = ontologyRepository;
            _conceptService = conceptService;
            _relationshipService = relationshipService;
            _propertyService = propertyService;
            _suggestionService = suggestionService;
            _commandInvoker = commandInvoker;
            _userService = userService;
            _shareService = shareService;
        }

        // Ontology operations - delegate to repository
        public async Task<List<Ontology>> GetAllOntologiesAsync()
        {
            return await _ontologyRepository.GetAllWithBasicDataAsync();
        }

        public async Task<List<Ontology>> GetOntologiesForCurrentUserAsync()
        {
            var currentUser = await _userService.GetCurrentUserAsync().ConfigureAwait(false);
            if (currentUser == null)
            {
                return new List<Ontology>();
            }

            // Store the user ID before making the next database call
            // This ensures the UserManager's DbContext operation completes
            var userId = currentUser.Id;

            var allOntologies = await _ontologyRepository.GetAllWithBasicDataAsync().ConfigureAwait(false);

            // Get ontologies owned by the user
            var ownedOntologies = allOntologies.Where(o => o.UserId == userId).ToList();

            // Get ontologies shared with the user
            var sharedOntologies = await _shareService.GetSharedOntologiesForUserAsync(userId).ConfigureAwait(false);

            // Combine and deduplicate (in case an ontology is both owned and shared)
            var combinedOntologies = ownedOntologies
                .Concat(sharedOntologies)
                .GroupBy(o => o.Id)
                .Select(g => g.First())
                .OrderByDescending(o => o.UpdatedAt)
                .ToList();

            return combinedOntologies;
        }

        public async Task<Ontology?> GetOntologyAsync(int id)
        {
            return await _ontologyRepository.GetWithAllRelatedDataAsync(id);
        }

        public async Task<Ontology?> GetOntologyAsync(int id, Action<ImportProgress>? onProgress = null)
        {
            return await _ontologyRepository.GetWithProgressAsync(id, onProgress);
        }

        public async Task<Ontology> CreateOntologyAsync(Ontology ontology)
        {
            // Assign to current user if not already assigned
            if (string.IsNullOrEmpty(ontology.UserId))
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    ontology.UserId = currentUser.Id;
                }
                else
                {
                    throw new InvalidOperationException("No current user found. Cannot create ontology.");
                }
            }

            return await _ontologyRepository.AddAsync(ontology);
        }

        public async Task<Ontology> UpdateOntologyAsync(Ontology ontology)
        {
            await _ontologyRepository.UpdateAsync(ontology);
            return ontology;
        }

        public async Task DeleteOntologyAsync(int id)
        {
            // Check ownership before deleting
            var ontology = await _ontologyRepository.GetByIdAsync(id);
            if (ontology == null)
            {
                throw new InvalidOperationException($"Ontology with ID {id} not found.");
            }

            var currentUser = await _userService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new InvalidOperationException("No current user found. Cannot delete ontology.");
            }

            // Both UserId and currentUser.Id are strings now
            if (ontology.UserId != currentUser.Id)
            {
                throw new UnauthorizedAccessException($"You do not have permission to delete this ontology. It belongs to another user.");
            }

            await _ontologyRepository.DeleteAsync(id);
        }

        // Concept operations - delegate to ConceptService
        public async Task<Concept> CreateConceptAsync(Concept concept, bool recordUndo = true)
        {
            return await _conceptService.CreateAsync(concept, recordUndo);
        }

        public async Task<Concept> UpdateConceptAsync(Concept concept, bool recordUndo = true)
        {
            return await _conceptService.UpdateAsync(concept, recordUndo);
        }

        public async Task DeleteConceptAsync(int id, bool recordUndo = true)
        {
            await _conceptService.DeleteAsync(id, recordUndo);
        }

        // Relationship operations - delegate to RelationshipService
        public async Task<Relationship> CreateRelationshipAsync(Relationship relationship, bool recordUndo = true)
        {
            return await _relationshipService.CreateAsync(relationship, recordUndo);
        }

        public async Task<Relationship> UpdateRelationshipAsync(Relationship relationship, bool recordUndo = true)
        {
            return await _relationshipService.UpdateAsync(relationship, recordUndo);
        }

        public async Task DeleteRelationshipAsync(int id, bool recordUndo = true)
        {
            await _relationshipService.DeleteAsync(id, recordUndo);
        }

        // Property operations - delegate to PropertyService
        public async Task<Property> CreatePropertyAsync(Property property)
        {
            return await _propertyService.CreateAsync(property);
        }

        public async Task<Property> UpdatePropertyAsync(Property property)
        {
            return await _propertyService.UpdateAsync(property);
        }

        public async Task DeletePropertyAsync(int id)
        {
            await _propertyService.DeleteAsync(id);
        }

        // Validation helpers - delegate to RelationshipService and SuggestionService
        public async Task<bool> CanCreateRelationshipAsync(int sourceId, int targetId, string relationType)
        {
            return await _relationshipService.CanCreateAsync(sourceId, targetId, relationType);
        }

        public async Task<List<string>> GetSuggestedRelationshipsAsync(int conceptId)
        {
            return await _suggestionService.GetSuggestionsAsync(conceptId);
        }

        // Undo/Redo operations - delegate to CommandInvoker
        public async Task<bool> UndoAsync()
        {
            return await _commandInvoker.UndoAsync();
        }

        public async Task<bool> RedoAsync()
        {
            return await _commandInvoker.RedoAsync();
        }

        public bool CanUndo() => _commandInvoker.CanUndo();
        public bool CanRedo() => _commandInvoker.CanRedo();

        // Fork/Clone/Provenance operations
        public async Task<Ontology> CloneOntologyAsync(int sourceOntologyId, string newName, string? provenanceNotes = null)
        {
            var sourceOntology = await GetOntologyAsync(sourceOntologyId);
            if (sourceOntology == null)
            {
                throw new ArgumentException($"Source ontology with ID {sourceOntologyId} not found");
            }

            // Get current user
            var currentUser = await _userService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new InvalidOperationException("User must be authenticated to clone an ontology");
            }

            // Create new ontology with cloned data
            var clonedOntology = new Ontology
            {
                UserId = currentUser.Id,
                Name = newName,
                Description = sourceOntology.Description,
                Namespace = sourceOntology.Namespace,
                Tags = sourceOntology.Tags,
                License = sourceOntology.License,
                Author = currentUser.UserName ?? currentUser.Email,
                Version = "1.0",
                UsesBFO = sourceOntology.UsesBFO,
                UsesProvO = sourceOntology.UsesProvO,
                Notes = sourceOntology.Notes,
                ParentOntologyId = sourceOntologyId,
                ProvenanceType = "clone",
                ProvenanceNotes = provenanceNotes ?? $"Cloned from '{sourceOntology.Name}'"
            };

            var createdOntology = await CreateOntologyAsync(clonedOntology);

            // Clone all concepts (optimized - batch insert instead of N+1)
            var conceptMapping = new Dictionary<int, int>(); // old ID -> new ID
            var clonedConcepts = new List<Concept>();
            var clonedProperties = new List<Property>();

            // Prepare all concepts first
            foreach (var concept in sourceOntology.Concepts)
            {
                var clonedConcept = new Concept
                {
                    OntologyId = createdOntology.Id,
                    Name = concept.Name,
                    Definition = concept.Definition,
                    SimpleExplanation = concept.SimpleExplanation,
                    Examples = concept.Examples,
                    PositionX = concept.PositionX,
                    PositionY = concept.PositionY,
                    Category = concept.Category,
                    Color = concept.Color,
                    SourceOntology = concept.SourceOntology,
                    Properties = new List<Property>() // Initialize for later
                };

                clonedConcepts.Add(clonedConcept);

                // Store original ID temporarily for mapping
                clonedConcept.Id = concept.Id; // Temporary, will be replaced after insert
            }

            // Batch insert all concepts at once
            await using (var context = await _contextFactory.CreateDbContextAsync())
            {
                // Clear temporary IDs before insert
                foreach (var c in clonedConcepts)
                {
                    var originalId = c.Id;
                    c.Id = 0; // Reset for insert

                    // Prepare properties for this concept
                    var originalConcept = sourceOntology.Concepts.First(sc => sc.Id == originalId);
                    foreach (var property in originalConcept.Properties)
                    {
                        c.Properties.Add(new Property
                        {
                            Name = property.Name,
                            Value = property.Value,
                            DataType = property.DataType,
                            Description = property.Description
                        });
                    }
                }

                context.Concepts.AddRange(clonedConcepts);
                await context.SaveChangesAsync();

                // Now build the mapping with the new IDs
                for (int i = 0; i < sourceOntology.Concepts.Count; i++)
                {
                    conceptMapping[sourceOntology.Concepts.ToList()[i].Id] = clonedConcepts[i].Id;
                }
            }

            // Clone all relationships (optimized - batch insert instead of N+1)
            var clonedRelationships = new List<Relationship>();
            foreach (var relationship in sourceOntology.Relationships)
            {
                if (conceptMapping.ContainsKey(relationship.SourceConceptId) &&
                    conceptMapping.ContainsKey(relationship.TargetConceptId))
                {
                    clonedRelationships.Add(new Relationship
                    {
                        OntologyId = createdOntology.Id,
                        SourceConceptId = conceptMapping[relationship.SourceConceptId],
                        TargetConceptId = conceptMapping[relationship.TargetConceptId],
                        RelationType = relationship.RelationType,
                        Label = relationship.Label,
                        Description = relationship.Description,
                        OntologyUri = relationship.OntologyUri,
                        Strength = relationship.Strength
                    });
                }
            }

            // Batch insert all relationships at once
            if (clonedRelationships.Any())
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                context.Relationships.AddRange(clonedRelationships);
                await context.SaveChangesAsync();
            }

            // Clone custom templates (optimized - batch insert instead of N+1)
            var clonedTemplates = sourceOntology.CustomTemplates.Select(template => new CustomConceptTemplate
            {
                OntologyId = createdOntology.Id,
                Category = template.Category,
                Type = template.Type,
                Description = template.Description,
                Examples = template.Examples,
                Color = template.Color
            }).ToList();

            if (clonedTemplates.Any())
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                context.CustomConceptTemplates.AddRange(clonedTemplates);
                await context.SaveChangesAsync();
            }

            return await GetOntologyAsync(createdOntology.Id) ?? createdOntology;
        }

        public async Task<Ontology> ForkOntologyAsync(int sourceOntologyId, string newName, string? provenanceNotes = null)
        {
            // Fork is similar to clone but with "fork" provenance type
            // This allows for semantic distinction: clone = exact copy, fork = starting point for divergence
            var sourceOntology = await GetOntologyAsync(sourceOntologyId);
            if (sourceOntology == null)
            {
                throw new ArgumentException($"Source ontology with ID {sourceOntologyId} not found");
            }

            var forkedOntology = await CloneOntologyAsync(sourceOntologyId, newName, provenanceNotes);

            // Update the provenance type to "fork"
            forkedOntology.ProvenanceType = "fork";
            forkedOntology.ProvenanceNotes = provenanceNotes ?? $"Forked from '{sourceOntology.Name}'";

            return await UpdateOntologyAsync(forkedOntology);
        }

        public async Task<List<Ontology>> GetOntologyLineageAsync(int ontologyId)
        {
            var lineage = new List<Ontology>();
            using var context = await _contextFactory.CreateDbContextAsync();

            int? currentId = ontologyId;
            while (currentId.HasValue)
            {
                var ontology = await context.Ontologies
                    .Include(o => o.ParentOntology)
                    .FirstOrDefaultAsync(o => o.Id == currentId.Value);

                if (ontology == null) break;

                lineage.Add(ontology);

                currentId = ontology.ParentOntologyId;
            }

            return lineage;
        }

        public async Task<List<Ontology>> GetOntologyDescendantsAsync(int ontologyId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Recursively fetch descendants using C# to ensure compatibility with all providers
            var result = new List<Ontology>();
            var queue = new Queue<int>();
            queue.Enqueue(ontologyId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();

                // Get all direct children of current ontology
                var children = await context.Ontologies
                    .Where(o => o.ParentOntologyId == currentId)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var child in children)
                {
                    result.Add(child);
                    queue.Enqueue(child.Id);
                }
            }

            return result;
        }
    }
}
