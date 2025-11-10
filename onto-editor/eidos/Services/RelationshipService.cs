using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Models.Events;
using Eidos.Models.Exceptions;
using Eidos.Services.Commands;
using Eidos.Services.Interfaces;
using Eidos.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace Eidos.Services;

/// <summary>
/// Service for managing relationship operations
/// Follows Single Responsibility Principle - only handles relationship CRUD
/// Uses Command Pattern for undo/redo support
/// Broadcasts changes via SignalR for real-time collaboration
/// Enforces permission checks for security
/// </summary>
public class RelationshipService : IRelationshipService
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly ICommandFactory _commandFactory;
    private readonly CommandInvoker _commandInvoker;
    private readonly IHubContext<OntologyHub> _hubContext;
    private readonly IUserService _userService;
    private readonly IOntologyShareService _shareService;
    private readonly IOntologyActivityService _activityService;
    private readonly OntologyPermissionService _permissionService;

    public RelationshipService(
        IRelationshipRepository relationshipRepository,
        IOntologyRepository ontologyRepository,
        ICommandFactory commandFactory,
        CommandInvoker commandInvoker,
        IHubContext<OntologyHub> hubContext,
        IUserService userService,
        IOntologyShareService shareService,
        IOntologyActivityService activityService,
        OntologyPermissionService permissionService)
    {
        _relationshipRepository = relationshipRepository;
        _ontologyRepository = ontologyRepository;
        _commandFactory = commandFactory;
        _commandInvoker = commandInvoker;
        _hubContext = hubContext;
        _userService = userService;
        _shareService = shareService;
        _activityService = activityService;
        _permissionService = permissionService;
    }

    public async Task<Relationship> CreateAsync(Relationship relationship, bool recordUndo = true)
    {
        // Verify user has permission to add relationships (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var canEdit = await _permissionService.CanEditAsync(relationship.OntologyId, currentUser?.Id);

        if (!canEdit)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to add relationships to ontology {relationship.OntologyId}");
        }

        // Check if approval workflow is required (throws ApprovalRequiredException if needed)
        await CheckApprovalModeAsync(relationship.OntologyId, currentUser?.Id, "create relationship");

        if (recordUndo)
        {
            var command = _commandFactory.CreateRelationshipCommand(relationship);
            await _commandInvoker.ExecuteAsync(command);
        }
        else
        {
            relationship.CreatedAt = DateTime.UtcNow;
            await _relationshipRepository.AddAsync(relationship);
            await _ontologyRepository.UpdateTimestampAsync(relationship.OntologyId);
            await _ontologyRepository.IncrementRelationshipCountAsync(relationship.OntologyId);
        }

        // Record activity for version control
        await RecordRelationshipActivity(relationship, ActivityTypes.Create, null, relationship);

        // Broadcast relationship creation to other users in the ontology
        await BroadcastRelationshipChange(relationship.OntologyId, ChangeType.Added, relationship);

        return relationship;
    }

    public async Task<Relationship> UpdateAsync(Relationship relationship, bool recordUndo = true)
    {
        // Verify user has permission to edit relationships (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var canEdit = await _permissionService.CanEditAsync(relationship.OntologyId, currentUser?.Id);

        if (!canEdit)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to edit relationships in ontology {relationship.OntologyId}");
        }

        // Check if approval workflow is required (throws ApprovalRequiredException if needed)
        await CheckApprovalModeAsync(relationship.OntologyId, currentUser?.Id, "update relationship");

        // Capture before state for activity tracking
        var beforeRelationship = await _relationshipRepository.GetByIdAsync(relationship.Id);

        if (recordUndo)
        {
            var command = _commandFactory.UpdateRelationshipCommand(relationship);
            await _commandInvoker.ExecuteAsync(command);
        }
        else
        {
            await _relationshipRepository.UpdateAsync(relationship);
            await _ontologyRepository.UpdateTimestampAsync(relationship.OntologyId);
        }

        // Record activity for version control
        await RecordRelationshipActivity(relationship, ActivityTypes.Update, beforeRelationship, relationship);

        // Broadcast relationship update to other users in the ontology
        await BroadcastRelationshipChange(relationship.OntologyId, ChangeType.Updated, relationship);

        return relationship;
    }

    public async Task DeleteAsync(int id, bool recordUndo = true)
    {
        var relationship = await _relationshipRepository.GetByIdAsync(id);
        if (relationship == null)
            return;

        var ontologyId = relationship.OntologyId;

        // Verify user has permission to delete relationships (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var canEdit = await _permissionService.CanEditAsync(ontologyId, currentUser?.Id);

        if (!canEdit)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to delete relationships from ontology {ontologyId}");
        }

        // Check if approval workflow is required (throws ApprovalRequiredException if needed)
        await CheckApprovalModeAsync(ontologyId, currentUser?.Id, "delete relationship");

        if (recordUndo)
        {
            var command = _commandFactory.DeleteRelationshipCommand(id);
            await _commandInvoker.ExecuteAsync(command);
        }
        else
        {
            await _relationshipRepository.DeleteAsync(id);
            await _ontologyRepository.UpdateTimestampAsync(ontologyId);
            await _ontologyRepository.DecrementRelationshipCountAsync(ontologyId);
        }

        // Record activity for version control
        await RecordRelationshipActivity(relationship, ActivityTypes.Delete, relationship, null);

        // Broadcast relationship deletion to other users in the ontology
        await BroadcastRelationshipChange(ontologyId, ChangeType.Deleted, null, id);
    }

    public async Task<Relationship?> GetByIdAsync(int id)
    {
        return await _relationshipRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Relationship>> GetByOntologyIdAsync(int ontologyId)
    {
        return await _relationshipRepository.GetByOntologyIdAsync(ontologyId);
    }

    public async Task<IEnumerable<Relationship>> GetByConceptIdAsync(int conceptId)
    {
        return await _relationshipRepository.GetByConceptIdAsync(conceptId);
    }

    public async Task<bool> CanCreateAsync(int sourceId, int targetId, string relationType)
    {
        // Prevent self-relationships
        if (sourceId == targetId)
            return false;

        // Check if relationship already exists
        return !await _relationshipRepository.ExistsAsync(sourceId, targetId, relationType);
    }

    /// <summary>
    /// Broadcasts relationship changes to all users in the ontology group
    /// </summary>
    private async Task BroadcastRelationshipChange(int ontologyId, ChangeType changeType, Relationship? relationship, int? deletedRelationshipId = null)
    {
        var groupName = $"ontology_{ontologyId}";
        var changeEvent = new RelationshipChangedEvent
        {
            ChangeType = changeType,
            OntologyId = ontologyId,
            Relationship = relationship,
            DeletedRelationshipId = deletedRelationshipId
        };

        await _hubContext.Clients.Group(groupName).SendAsync("RelationshipChanged", changeEvent);
    }

    /// <summary>
    /// Records relationship activity for version control
    /// </summary>
    private async Task RecordRelationshipActivity(Relationship relationship, string activityType, Relationship? before, Relationship? after)
    {
        try
        {
            var currentUser = await _userService.GetCurrentUserAsync();

            var activity = new OntologyActivity
            {
                OntologyId = relationship.OntologyId,
                UserId = currentUser?.Id,
                ActorName = currentUser?.Email ?? "Unknown User",
                ActivityType = activityType,
                EntityType = EntityTypes.Relationship,
                EntityId = relationship.Id,
                EntityName = relationship.RelationType,
                Description = activityType switch
                {
                    ActivityTypes.Create => $"Created relationship '{relationship.RelationType}'",
                    ActivityTypes.Update => $"Updated relationship '{relationship.RelationType}'",
                    ActivityTypes.Delete => $"Deleted relationship '{relationship.RelationType}'",
                    _ => $"Modified relationship '{relationship.RelationType}'"
                },
                BeforeSnapshot = before != null ? SerializeRelationshipToJson(before) : null,
                AfterSnapshot = after != null ? SerializeRelationshipToJson(after) : null,
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
    /// Serializes a relationship to JSON for snapshot storage
    /// </summary>
    private string SerializeRelationshipToJson(Relationship relationship)
    {
        var snapshot = new
        {
            relationship.Id,
            relationship.SourceConceptId,
            relationship.TargetConceptId,
            relationship.RelationType,
            relationship.Description,
            relationship.CreatedAt
        };

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false });
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
}
