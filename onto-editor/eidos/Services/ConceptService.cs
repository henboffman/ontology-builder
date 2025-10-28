using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Models.Events;
using Eidos.Services.Commands;
using Eidos.Services.Interfaces;
using Eidos.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Eidos.Services;

/// <summary>
/// Service for managing concept operations
/// Follows Single Responsibility Principle - only handles concept CRUD
/// Uses Command Pattern for undo/redo support
/// Broadcasts changes via SignalR for real-time collaboration
/// Enforces permission checks for security
/// </summary>
public class ConceptService : IConceptService
{
    private readonly IConceptRepository _conceptRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ICommandFactory _commandFactory;
    private readonly CommandInvoker _commandInvoker;
    private readonly IHubContext<OntologyHub> _hubContext;
    private readonly IUserService _userService;
    private readonly IOntologyShareService _shareService;
    private readonly IOntologyActivityService _activityService;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;

    public ConceptService(
        IConceptRepository conceptRepository,
        IOntologyRepository ontologyRepository,
        IRelationshipRepository relationshipRepository,
        ICommandFactory commandFactory,
        CommandInvoker commandInvoker,
        IHubContext<OntologyHub> hubContext,
        IUserService userService,
        IOntologyShareService shareService,
        IOntologyActivityService activityService,
        IDbContextFactory<OntologyDbContext> contextFactory)
    {
        _conceptRepository = conceptRepository;
        _ontologyRepository = ontologyRepository;
        _relationshipRepository = relationshipRepository;
        _commandFactory = commandFactory;
        _commandInvoker = commandInvoker;
        _hubContext = hubContext;
        _userService = userService;
        _shareService = shareService;
        _activityService = activityService;
        _contextFactory = contextFactory;
    }

    public async Task<Concept> CreateAsync(Concept concept, bool recordUndo = true)
    {
        // Verify user has permission to add concepts (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            concept.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAndAdd);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to add concepts to ontology {concept.OntologyId}");
        }

        if (recordUndo)
        {
            var command = _commandFactory.CreateConceptCommand(concept);
            await _commandInvoker.ExecuteAsync(command);
        }
        else
        {
            concept.CreatedAt = DateTime.UtcNow;
            await _conceptRepository.AddAsync(concept);
            await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);
            await _ontologyRepository.IncrementConceptCountAsync(concept.OntologyId);
        }

        // Record activity for version control
        await RecordConceptActivity(concept, ActivityTypes.Create, null, concept);

        // Broadcast concept creation to other users in the ontology
        await BroadcastConceptChange(concept.OntologyId, ChangeType.Added, concept);

        return concept;
    }

    public async Task<Concept> UpdateAsync(Concept concept, bool recordUndo = true)
    {
        // Verify user has permission to edit concepts (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            concept.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to edit concepts in ontology {concept.OntologyId}");
        }

        // Capture before state for activity tracking
        var beforeConcept = await _conceptRepository.GetByIdAsync(concept.Id);

        if (recordUndo)
        {
            var command = _commandFactory.UpdateConceptCommand(concept);
            await _commandInvoker.ExecuteAsync(command);
        }
        else
        {
            await _conceptRepository.UpdateAsync(concept);
            await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);
        }

        // Record activity for version control
        await RecordConceptActivity(concept, ActivityTypes.Update, beforeConcept, concept);

        // Broadcast concept update to other users in the ontology
        await BroadcastConceptChange(concept.OntologyId, ChangeType.Updated, concept);

        return concept;
    }

    public async Task DeleteAsync(int id, bool recordUndo = true)
    {
        var concept = await _conceptRepository.GetByIdAsync(id);
        if (concept == null)
            return;

        var ontologyId = concept.OntologyId;

        // Verify user has permission to delete concepts (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            ontologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to delete concepts from ontology {ontologyId}");
        }

        if (recordUndo)
        {
            var command = _commandFactory.DeleteConceptCommand(id);
            await _commandInvoker.ExecuteAsync(command);
        }
        else
        {
            await _conceptRepository.DeleteAsync(id);
            await _ontologyRepository.UpdateTimestampAsync(ontologyId);
            await _ontologyRepository.DecrementConceptCountAsync(ontologyId);
        }

        // Record activity for version control
        await RecordConceptActivity(concept, ActivityTypes.Delete, concept, null);

        // Broadcast concept deletion to other users in the ontology
        await BroadcastConceptChange(ontologyId, ChangeType.Deleted, null, id);
    }

    public async Task<Concept?> GetByIdAsync(int id)
    {
        return await _conceptRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Concept>> GetByOntologyIdAsync(int ontologyId)
    {
        return await _conceptRepository.GetByOntologyIdAsync(ontologyId);
    }

    public async Task<IEnumerable<Concept>> SearchAsync(string query)
    {
        return await _conceptRepository.SearchAsync(query);
    }

    public async Task<IEnumerable<ConceptHierarchyNode>> GetHierarchyAsync(int ontologyId)
    {
        // Get all concepts for this ontology
        var concepts = await _conceptRepository.GetByOntologyIdAsync(ontologyId);

        // Get all hierarchical relationships (subclass-of, is-a)
        var allRelationships = await _relationshipRepository.GetByOntologyIdAsync(ontologyId);
        var hierarchicalRelationships = allRelationships
            .Where(r => r.RelationType == "subclass-of" || r.RelationType == "is-a")
            .ToList();

        // Build parent-child mapping
        var childToParentMap = hierarchicalRelationships
            .GroupBy(r => r.SourceConceptId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.TargetConceptId).ToList());

        // Find root concepts (concepts with no parents)
        var conceptsWithParents = childToParentMap.Keys.ToHashSet();
        var rootConcepts = concepts.Where(c => !conceptsWithParents.Contains(c.Id)).ToList();

        // Build hierarchy recursively
        var rootNodes = new List<ConceptHierarchyNode>();
        foreach (var rootConcept in rootConcepts)
        {
            var node = BuildHierarchyNode(rootConcept, concepts, hierarchicalRelationships, 0);
            rootNodes.Add(node);
        }

        return rootNodes;
    }

    public async Task<IEnumerable<Concept>> GetParentConceptsAsync(int conceptId)
    {
        var concept = await _conceptRepository.GetByIdAsync(conceptId);
        if (concept == null)
            return Enumerable.Empty<Concept>();

        // Get all hierarchical relationships where this concept is the source (child)
        var allRelationships = await _relationshipRepository.GetByConceptIdAsync(conceptId);
        var parentRelationships = allRelationships
            .Where(r => (r.RelationType == "subclass-of" || r.RelationType == "is-a")
                     && r.SourceConceptId == conceptId)
            .ToList();

        // Get the parent concepts (optimized - single query instead of N+1)
        var parentIds = parentRelationships.Select(r => r.TargetConceptId).Distinct().ToList();

        if (!parentIds.Any())
            return new List<Concept>();

        // Use DbContext directly for efficient batch query
        await using var context = await _contextFactory.CreateDbContextAsync();
        var parents = await context.Concepts
            .Where(c => parentIds.Contains(c.Id))
            .AsNoTracking()
            .ToListAsync();

        return parents;
    }

    public async Task<IEnumerable<Concept>> GetChildConceptsAsync(int conceptId)
    {
        var concept = await _conceptRepository.GetByIdAsync(conceptId);
        if (concept == null)
            return Enumerable.Empty<Concept>();

        // Get all hierarchical relationships where this concept is the target (parent)
        var allRelationships = await _relationshipRepository.GetByConceptIdAsync(conceptId);
        var childRelationships = allRelationships
            .Where(r => (r.RelationType == "subclass-of" || r.RelationType == "is-a")
                     && r.TargetConceptId == conceptId)
            .ToList();

        // Get the child concepts (optimized - single query instead of N+1)
        var childIds = childRelationships.Select(r => r.SourceConceptId).Distinct().ToList();

        if (!childIds.Any())
            return new List<Concept>();

        // Use DbContext directly for efficient batch query
        await using var context = await _contextFactory.CreateDbContextAsync();
        var children = await context.Concepts
            .Where(c => childIds.Contains(c.Id))
            .AsNoTracking()
            .ToListAsync();

        return children;
    }

    /// <summary>
    /// Recursively builds a hierarchy node with its children
    /// </summary>
    private ConceptHierarchyNode BuildHierarchyNode(
        Concept concept,
        IEnumerable<Concept> allConcepts,
        List<Relationship> hierarchicalRelationships,
        int level)
    {
        var node = new ConceptHierarchyNode
        {
            Concept = concept,
            Level = level,
            IsExpanded = level < 2 // Auto-expand first 2 levels
        };

        // Find children (concepts that have this concept as parent via subclass-of/is-a)
        var childRelationships = hierarchicalRelationships
            .Where(r => r.TargetConceptId == concept.Id)
            .ToList();

        var childConceptIds = childRelationships.Select(r => r.SourceConceptId).Distinct();
        var childConcepts = allConcepts.Where(c => childConceptIds.Contains(c.Id)).ToList();

        // Recursively build child nodes
        foreach (var childConcept in childConcepts)
        {
            var childNode = BuildHierarchyNode(childConcept, allConcepts, hierarchicalRelationships, level + 1);
            childNode.ParentConceptId = concept.Id;
            node.Children.Add(childNode);
        }

        // Calculate descendant count
        node.DescendantCount = node.Children.Count + node.Children.Sum(c => c.DescendantCount);

        return node;
    }

    /// <summary>
    /// Broadcasts concept changes to all users in the ontology group
    /// </summary>
    private async Task BroadcastConceptChange(int ontologyId, ChangeType changeType, Concept? concept, int? deletedConceptId = null)
    {
        var groupName = $"ontology_{ontologyId}";
        var changeEvent = new ConceptChangedEvent
        {
            ChangeType = changeType,
            OntologyId = ontologyId,
            Concept = concept,
            DeletedConceptId = deletedConceptId
        };

        await _hubContext.Clients.Group(groupName).SendAsync("ConceptChanged", changeEvent);
    }

    /// <summary>
    /// Records concept activity for version control
    /// </summary>
    private async Task RecordConceptActivity(Concept concept, string activityType, Concept? before, Concept? after)
    {
        try
        {
            var currentUser = await _userService.GetCurrentUserAsync();

            var activity = new OntologyActivity
            {
                OntologyId = concept.OntologyId,
                UserId = currentUser?.Id,
                ActorName = currentUser?.Email ?? "Unknown User",
                ActivityType = activityType,
                EntityType = EntityTypes.Concept,
                EntityId = concept.Id,
                EntityName = concept.Name,
                Description = activityType switch
                {
                    ActivityTypes.Create => $"Created concept '{concept.Name}'",
                    ActivityTypes.Update => $"Updated concept '{concept.Name}'",
                    ActivityTypes.Delete => $"Deleted concept '{concept.Name}'",
                    _ => $"Modified concept '{concept.Name}'"
                },
                BeforeSnapshot = before != null ? SerializeConceptToJson(before) : null,
                AfterSnapshot = after != null ? SerializeConceptToJson(after) : null,
                CreatedAt = DateTime.UtcNow
            };

            await _activityService.RecordActivityAsync(activity);
        }
        catch (Exception)
        {
            // Don't fail the operation if activity recording fails
            // Just log and continue
        }
    }

    /// <summary>
    /// Serializes a concept to JSON for snapshot storage
    /// </summary>
    private string SerializeConceptToJson(Concept concept)
    {
        var snapshot = new
        {
            concept.Id,
            concept.Name,
            concept.Definition,
            concept.SimpleExplanation,
            concept.Examples,
            concept.Color,
            concept.PositionX,
            concept.PositionY,
            concept.Category,
            concept.CreatedAt
        };

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false });
    }
}
