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

        public async Task<Ontology> RevertToVersionAsync(int ontologyId, int versionNumber)
        {
            // Get the ontology
            var ontology = await GetOntologyAsync(ontologyId);
            if (ontology == null)
            {
                throw new ArgumentException($"Ontology with ID {ontologyId} not found");
            }

            // Verify user has permission
            var currentUser = await _userService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new InvalidOperationException("User must be authenticated to revert an ontology");
            }

            if (ontology.UserId != currentUser.Id)
            {
                throw new UnauthorizedAccessException("You do not have permission to revert this ontology");
            }

            await using var context = await _contextFactory.CreateDbContextAsync();

            // Get all activities up to the target version
            var activities = await context.OntologyActivities
                .Where(a => a.OntologyId == ontologyId && a.VersionNumber <= versionNumber)
                .OrderBy(a => a.VersionNumber)
                .ToListAsync();

            if (!activities.Any(a => a.VersionNumber == versionNumber))
            {
                throw new ArgumentException($"Version {versionNumber} not found for ontology {ontologyId}");
            }

            // Group activities by entity to get the latest state of each entity at the target version
            var conceptSnapshots = activities
                .Where(a => a.EntityType == EntityTypes.Concept && a.EntityId.HasValue)
                .GroupBy(a => a.EntityId!.Value)
                .Select(g => g.OrderByDescending(a => a.VersionNumber).First())
                .Where(a => a.ActivityType != ActivityTypes.Delete && a.AfterSnapshot != null)
                .ToList();

            var relationshipSnapshots = activities
                .Where(a => a.EntityType == EntityTypes.Relationship && a.EntityId.HasValue)
                .GroupBy(a => a.EntityId!.Value)
                .Select(g => g.OrderByDescending(a => a.VersionNumber).First())
                .Where(a => a.ActivityType != ActivityTypes.Delete && a.AfterSnapshot != null)
                .ToList();

            // Check if we have any data to restore
            if (!conceptSnapshots.Any() && !relationshipSnapshots.Any())
            {
                throw new InvalidOperationException(
                    $"No activity snapshots found for version {versionNumber}. " +
                    "This may indicate that activity tracking was not enabled when this ontology was created, " +
                    "or that version {versionNumber} represents an empty ontology state.");
            }

            // Delete all current data in correct order to respect foreign key constraints
            // Order: ConceptRestrictions -> Individuals -> Relationships -> Concepts
            var currentRestrictions = await context.ConceptRestrictions
                .Where(cr => cr.Concept.OntologyId == ontologyId)
                .ToListAsync();
            var currentIndividuals = await context.Individuals.Where(i => i.OntologyId == ontologyId).ToListAsync();
            var currentRelationships = await context.Relationships.Where(r => r.OntologyId == ontologyId).ToListAsync();
            var currentConcepts = await context.Concepts.Where(c => c.OntologyId == ontologyId).ToListAsync();

            var deletionCounts = new
            {
                Restrictions = currentRestrictions.Count,
                Individuals = currentIndividuals.Count,
                Relationships = currentRelationships.Count,
                Concepts = currentConcepts.Count
            };

            // Delete concept restrictions first (they reference concepts)
            if (currentRestrictions.Any())
            {
                context.ConceptRestrictions.RemoveRange(currentRestrictions);
                await context.SaveChangesAsync();
            }

            // Delete individuals second (they reference concepts)
            if (currentIndividuals.Any())
            {
                context.Individuals.RemoveRange(currentIndividuals);
                await context.SaveChangesAsync();
            }

            // Delete relationships third (they reference concepts)
            if (currentRelationships.Any())
            {
                context.Relationships.RemoveRange(currentRelationships);
                await context.SaveChangesAsync();
            }

            // Finally delete concepts
            if (currentConcepts.Any())
            {
                context.Concepts.RemoveRange(currentConcepts);
                await context.SaveChangesAsync();
            }

            // Verify deletion worked
            var remainingConcepts = await context.Concepts.Where(c => c.OntologyId == ontologyId).CountAsync();
            if (remainingConcepts > 0)
            {
                throw new InvalidOperationException(
                    $"Failed to delete all concepts. {remainingConcepts} concepts still remain after deletion.");
            }

            // Recreate concepts from snapshots
            var restoredConcepts = new Dictionary<int, int>(); // old ID -> new ID mapping
            var restorationErrors = new List<string>();

            foreach (var snapshot in conceptSnapshots)
            {
                try
                {
                    var conceptData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(snapshot.AfterSnapshot!);

                    // Get old ID - handle null case
                    if (!conceptData.TryGetProperty("Id", out var idProp) || idProp.ValueKind == System.Text.Json.JsonValueKind.Null)
                    {
                        restorationErrors.Add($"Concept snapshot has null or missing Id");
                        continue;
                    }
                    var oldId = idProp.GetInt32();

                    // Helper to get double value safely
                    double GetDoubleOrDefault(System.Text.Json.JsonElement element, string propertyName, double defaultValue = 0)
                    {
                        if (element.TryGetProperty(propertyName, out var prop) &&
                            prop.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            return prop.GetDouble();
                        }
                        return defaultValue;
                    }

                    var concept = new Concept
                    {
                        OntologyId = ontologyId,
                        Name = conceptData.GetProperty("Name").GetString() ?? "",
                        Definition = conceptData.TryGetProperty("Definition", out var def) && def.ValueKind != System.Text.Json.JsonValueKind.Null ? def.GetString() : null,
                        SimpleExplanation = conceptData.TryGetProperty("SimpleExplanation", out var exp) && exp.ValueKind != System.Text.Json.JsonValueKind.Null ? exp.GetString() : null,
                        Examples = conceptData.TryGetProperty("Examples", out var ex) && ex.ValueKind != System.Text.Json.JsonValueKind.Null ? ex.GetString() : null,
                        Color = conceptData.TryGetProperty("Color", out var col) && col.ValueKind != System.Text.Json.JsonValueKind.Null ? col.GetString() : null,
                        PositionX = GetDoubleOrDefault(conceptData, "PositionX", 0),
                        PositionY = GetDoubleOrDefault(conceptData, "PositionY", 0),
                        Category = conceptData.TryGetProperty("Category", out var cat) && cat.ValueKind != System.Text.Json.JsonValueKind.Null ? cat.GetString() : null
                    };

                    context.Concepts.Add(concept);
                    await context.SaveChangesAsync(); // Save to get new ID

                    restoredConcepts[oldId] = concept.Id;
                }
                catch (Exception ex)
                {
                    restorationErrors.Add($"Concept snapshot error: {ex.Message}");
                    continue;
                }
            }

            // Check if any concepts were restored
            if (conceptSnapshots.Any() && !restoredConcepts.Any())
            {
                throw new InvalidOperationException(
                    $"Failed to restore any concepts from {conceptSnapshots.Count} snapshot(s). Errors: " +
                    string.Join("; ", restorationErrors.Take(3)));
            }

            // Recreate relationships from snapshots
            var restoredRelationshipsCount = 0;
            foreach (var snapshot in relationshipSnapshots)
            {
                try
                {
                    var relData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(snapshot.AfterSnapshot!);

                    var oldSourceId = relData.GetProperty("SourceConceptId").GetInt32();
                    var oldTargetId = relData.GetProperty("TargetConceptId").GetInt32();

                    // Map old IDs to new IDs
                    if (!restoredConcepts.ContainsKey(oldSourceId) || !restoredConcepts.ContainsKey(oldTargetId))
                    {
                        continue; // Skip if referenced concepts don't exist
                    }

                    // Helper to get nullable int safely
                    int? GetNullableInt(System.Text.Json.JsonElement element, string propertyName)
                    {
                        if (element.TryGetProperty(propertyName, out var prop) &&
                            prop.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            return prop.GetInt32();
                        }
                        return null;
                    }

                    var relationship = new Relationship
                    {
                        OntologyId = ontologyId,
                        SourceConceptId = restoredConcepts[oldSourceId],
                        TargetConceptId = restoredConcepts[oldTargetId],
                        RelationType = relData.GetProperty("RelationType").GetString() ?? "",
                        Label = relData.TryGetProperty("Label", out var lbl) && lbl.ValueKind != System.Text.Json.JsonValueKind.Null ? lbl.GetString() : null,
                        Description = relData.TryGetProperty("Description", out var desc) && desc.ValueKind != System.Text.Json.JsonValueKind.Null ? desc.GetString() : null,
                        OntologyUri = relData.TryGetProperty("OntologyUri", out var uri) && uri.ValueKind != System.Text.Json.JsonValueKind.Null ? uri.GetString() : null,
                        Strength = GetNullableInt(relData, "Strength")
                    };

                    context.Relationships.Add(relationship);
                    restoredRelationshipsCount++;
                }
                catch (Exception ex)
                {
                    restorationErrors.Add($"Relationship snapshot error: {ex.Message}");
                    continue;
                }
            }

            await context.SaveChangesAsync();

            // Log restoration summary
            if (restorationErrors.Any())
            {
                // Some errors occurred but we still restored some data
                System.Diagnostics.Debug.WriteLine(
                    $"Revert completed with {restorationErrors.Count} error(s). " +
                    $"Restored {restoredConcepts.Count} concepts and {restoredRelationshipsCount} relationships.");
            }

            // Update ontology's UpdatedAt timestamp
            ontology.UpdatedAt = DateTime.UtcNow;
            await UpdateOntologyAsync(ontology);

            return await GetOntologyAsync(ontologyId) ?? ontology;
        }

        // ==================== Tag/Folder Management ====================

        public async Task<OntologyTag> AddTagAsync(int ontologyId, string tag, string? color = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // Check if tag already exists for this ontology
            var existingTag = await context.OntologyTags
                .FirstOrDefaultAsync(t => t.OntologyId == ontologyId && t.Tag == tag);

            if (existingTag != null)
            {
                return existingTag; // Tag already exists, return it
            }

            var ontologyTag = new OntologyTag
            {
                OntologyId = ontologyId,
                Tag = tag,
                Color = color,
                CreatedAt = DateTime.UtcNow
            };

            context.OntologyTags.Add(ontologyTag);
            await context.SaveChangesAsync();

            return ontologyTag;
        }

        public async Task RemoveTagAsync(int ontologyId, string tag)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var ontologyTag = await context.OntologyTags
                .FirstOrDefaultAsync(t => t.OntologyId == ontologyId && t.Tag == tag);

            if (ontologyTag != null)
            {
                context.OntologyTags.Remove(ontologyTag);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Ontology>> GetOntologiesByTagAsync(string userId, string tag)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Ontologies
                .Include(o => o.OntologyTags)
                .Where(o => o.UserId == userId && o.OntologyTags.Any(t => t.Tag == tag))
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserTagsAsync(string userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.OntologyTags
                .Where(t => t.Ontology.UserId == userId)
                .Select(t => t.Tag)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }

        public async Task<OntologyTag?> UpdateTagColorAsync(int ontologyId, string tag, string color)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var ontologyTag = await context.OntologyTags
                .FirstOrDefaultAsync(t => t.OntologyId == ontologyId && t.Tag == tag);

            if (ontologyTag != null)
            {
                ontologyTag.Color = color;
                await context.SaveChangesAsync();
            }

            return ontologyTag;
        }
    }
}
