using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Models.Events;
using Eidos.Services.Commands;
using Eidos.Services.Interfaces;
using Eidos.Hubs;
using Microsoft.AspNetCore.SignalR;

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

    public RelationshipService(
        IRelationshipRepository relationshipRepository,
        IOntologyRepository ontologyRepository,
        ICommandFactory commandFactory,
        CommandInvoker commandInvoker,
        IHubContext<OntologyHub> hubContext,
        IUserService userService,
        IOntologyShareService shareService)
    {
        _relationshipRepository = relationshipRepository;
        _ontologyRepository = ontologyRepository;
        _commandFactory = commandFactory;
        _commandInvoker = commandInvoker;
        _hubContext = hubContext;
        _userService = userService;
        _shareService = shareService;
    }

    public async Task<Relationship> CreateAsync(Relationship relationship, bool recordUndo = true)
    {
        // Verify user has permission to add relationships (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            relationship.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAndAdd);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to add relationships to ontology {relationship.OntologyId}");
        }

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
        }

        // Broadcast relationship creation to other users in the ontology
        await BroadcastRelationshipChange(relationship.OntologyId, ChangeType.Added, relationship);

        return relationship;
    }

    public async Task<Relationship> UpdateAsync(Relationship relationship, bool recordUndo = true)
    {
        // Verify user has permission to edit relationships (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            relationship.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to edit relationships in ontology {relationship.OntologyId}");
        }

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
        var hasPermission = await _shareService.HasPermissionAsync(
            ontologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to delete relationships from ontology {ontologyId}");
        }

        if (recordUndo)
        {
            var command = _commandFactory.DeleteRelationshipCommand(id);
            await _commandInvoker.ExecuteAsync(command);
        }
        else
        {
            await _relationshipRepository.DeleteAsync(id);
            await _ontologyRepository.UpdateTimestampAsync(ontologyId);
        }

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
}
