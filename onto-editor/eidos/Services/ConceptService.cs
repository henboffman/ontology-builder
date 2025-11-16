using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Models.Events;
using Eidos.Models.Exceptions;
using Eidos.Services.Commands;
using Eidos.Services.Interfaces;
using Eidos.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Logging;

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
    private readonly OntologyPermissionService _permissionService;
    private readonly NoteRepository _noteRepository;
    private readonly WorkspaceRepository _workspaceRepository;
    private readonly ILogger<ConceptService> _logger;

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
        IDbContextFactory<OntologyDbContext> contextFactory,
        OntologyPermissionService permissionService,
        NoteRepository noteRepository,
        WorkspaceRepository workspaceRepository,
        ILogger<ConceptService> logger)
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
        _permissionService = permissionService;
        _noteRepository = noteRepository;
        _workspaceRepository = workspaceRepository;
        _logger = logger;
    }

    public async Task<Concept> CreateAsync(Concept concept, bool recordUndo = true)
    {
        // Verify user has permission to add concepts (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var canEdit = await _permissionService.CanEditAsync(concept.OntologyId, currentUser?.Id);

        if (!canEdit)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to add concepts to ontology {concept.OntologyId}");
        }

        // Check if approval workflow is required (throws ApprovalRequiredException if needed)
        await CheckApprovalModeAsync(concept.OntologyId, currentUser?.Id, "create concept");

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

        // Auto-create concept note for this concept
        await AutoCreateConceptNoteAsync(concept.Id, concept.Name, concept.OntologyId, currentUser?.Id);

        return concept;
    }

    public async Task<Concept> UpdateAsync(Concept concept, bool recordUndo = true)
    {
        // Verify user has permission to edit concepts (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var canEdit = await _permissionService.CanEditAsync(concept.OntologyId, currentUser?.Id);

        if (!canEdit)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to edit concepts in ontology {concept.OntologyId}");
        }

        // Check if approval workflow is required (throws ApprovalRequiredException if needed)
        await CheckApprovalModeAsync(concept.OntologyId, currentUser?.Id, "update concept");

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
        var canEdit = await _permissionService.CanEditAsync(ontologyId, currentUser?.Id);

        if (!canEdit)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to delete concepts from ontology {ontologyId}");
        }

        // Check if approval workflow is required (throws ApprovalRequiredException if needed)
        await CheckApprovalModeAsync(ontologyId, currentUser?.Id, "delete concept");

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

    /// <summary>
    /// Updates the position of a concept node in the graph.
    /// This method is lightweight and doesn't trigger undo/redo or activity tracking.
    /// </summary>
    /// <param name="conceptId">The concept ID</param>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    public async Task UpdatePositionAsync(int conceptId, double x, double y)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var concept = await context.Concepts.FindAsync(conceptId);
        if (concept == null)
            throw new InvalidOperationException($"Concept {conceptId} not found");

        concept.PositionX = x;
        concept.PositionY = y;

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Batch update positions for multiple concepts.
    /// More efficient than calling UpdatePositionAsync multiple times.
    /// </summary>
    /// <param name="positions">Dictionary of concept ID to (X, Y) coordinates</param>
    public async Task UpdatePositionsBatchAsync(Dictionary<int, (double X, double Y)> positions)
    {
        if (positions == null || positions.Count == 0)
            return;

        using var context = await _contextFactory.CreateDbContextAsync();

        var conceptIds = positions.Keys.ToList();
        var concepts = await context.Concepts
            .Where(c => conceptIds.Contains(c.Id))
            .ToListAsync();

        foreach (var concept in concepts)
        {
            if (positions.TryGetValue(concept.Id, out var pos))
            {
                concept.PositionX = pos.X;
                concept.PositionY = pos.Y;
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Quick update for concept name - for inline editing
    /// </summary>
    public async Task<bool> UpdateConceptNameAsync(int conceptId, string newName, string userId)
    {
        try
        {
            var concept = await _conceptRepository.GetByIdAsync(conceptId);
            if (concept == null)
                return false;

            // Validate permissions
            var canEdit = await _permissionService.CanEditAsync(concept.OntologyId, userId);

            if (!canEdit)
                return false;

            // Validate name
            if (string.IsNullOrWhiteSpace(newName))
                return false;

            // Update name
            var oldName = concept.Name;
            var beforeConcept = new Concept { Id = concept.Id, Name = oldName };

            concept.Name = newName.Trim();
            await _conceptRepository.UpdateAsync(concept);
            await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);

            // Record activity for version control
            await RecordConceptActivity(concept, ActivityTypes.Update, beforeConcept, concept);

            // Broadcast update via SignalR
            await BroadcastConceptChange(concept.OntologyId, ChangeType.Updated, concept);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the ontology requires approval for changes and if the current user
    /// has sufficient permissions to bypass the approval workflow.
    /// Throws ApprovalRequiredException if approval is required and user only has edit permissions.
    /// </summary>
    /// <param name="ontologyId">The ontology ID to check</param>
    /// <param name="userId">The current user ID</param>
    /// <param name="operationType">The type of operation being attempted (e.g., "create", "update", "delete")</param>
    private async Task CheckApprovalModeAsync(int ontologyId, string? userId, string operationType)
    {
        if (string.IsNullOrEmpty(userId))
            return; // Permission checks will handle this

        // Get the ontology to check if approval is required
        var ontology = await _ontologyRepository.GetByIdAsync(ontologyId);
        if (ontology == null || !ontology.RequiresApproval)
            return; // No approval required

        // Check if user has FullAccess (can bypass approval)
        var canManage = await _permissionService.CanManageAsync(ontologyId, userId);
        if (canManage)
            return; // User has FullAccess, can bypass approval workflow

        // User only has Edit permission but approval is required
        throw new ApprovalRequiredException(
            ontologyId,
            operationType,
            $"This ontology requires approval for {operationType} operations. Please create a merge request instead."
        );
    }

    /// <summary>
    /// Auto-creates a concept note for a newly created concept
    /// </summary>
    private async Task AutoCreateConceptNoteAsync(int conceptId, string conceptName, int ontologyId, string? userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Cannot create concept note: userId is null");
                return;
            }

            // Check if concept note already exists
            var existingNote = await _noteRepository.GetConceptNoteAsync(conceptId);
            if (existingNote != null)
            {
                return; // Note already exists
            }

            // Find workspace with this ontology
            var workspaces = await _workspaceRepository.GetByUserIdAsync(userId);
            Workspace? workspace = null;
            foreach (var ws in workspaces)
            {
                var fullWs = await _workspaceRepository.GetByIdAsync(ws.Id, includeOntology: true);
                if (fullWs?.Ontology?.Id == ontologyId)
                {
                    workspace = fullWs;
                    break;
                }
            }

            if (workspace == null)
            {
                _logger.LogWarning("Cannot create concept note: No workspace found for ontology {OntologyId}", ontologyId);
                return;
            }

            // Generate default content for concept note
            var markdownContent = $@"# {conceptName}

*Auto-generated note for concept: {conceptName}*

## Definition

[Add the concept's definition here]

## Notes

[Add your notes about this concept here]
";

            var note = new Note
            {
                WorkspaceId = workspace.Id,
                Title = conceptName,
                IsConceptNote = true,
                LinkedConceptId = conceptId,
                UserId = userId,
                LinkCount = 0
            };

            await _noteRepository.CreateAsync(note, markdownContent);

            // Update workspace note counts
            await _workspaceRepository.UpdateNoteCountsAsync(workspace.Id);

            _logger.LogInformation("Auto-created concept note {NoteId} for concept {ConceptId} '{ConceptName}'",
                note.Id, conceptId, conceptName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-creating concept note for concept {ConceptId}", conceptId);
            // Don't throw - this is a nice-to-have feature, don't break concept creation
        }
    }
}
