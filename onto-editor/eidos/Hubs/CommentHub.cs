using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Eidos.Services.Interfaces;
using System.Security.Claims;

namespace Eidos.Hubs;

/// <summary>
/// SignalR Hub for real-time comment notifications
/// </summary>
[Authorize]
public class CommentHub : Hub
{
    private readonly IEntityCommentService _commentService;
    private readonly ILogger<CommentHub> _logger;

    public CommentHub(
        IEntityCommentService commentService,
        ILogger<CommentHub> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    private string GetUserId() => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    /// <summary>
    /// Join an ontology group to receive comment notifications
    /// </summary>
    public async Task JoinOntology(int ontologyId)
    {
        var userId = GetUserId();
        var groupName = $"ontology-{ontologyId}";

        // Verify user has access to this ontology
        if (await _commentService.CanAddCommentAsync(ontologyId, userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} joined comment group for ontology {OntologyId}",
                userId, ontologyId);
        }
        else
        {
            _logger.LogWarning("User {UserId} attempted to join comment group for ontology {OntologyId} without permission",
                userId, ontologyId);
        }
    }

    /// <summary>
    /// Leave an ontology group
    /// </summary>
    public async Task LeaveOntology(int ontologyId)
    {
        var userId = GetUserId();
        var groupName = $"ontology-{ontologyId}";

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} left comment group for ontology {OntologyId}",
            userId, ontologyId);
    }

    /// <summary>
    /// Join an entity-specific group to receive comment notifications for a specific entity
    /// </summary>
    public async Task JoinEntity(int ontologyId, string entityType, int entityId)
    {
        var userId = GetUserId();
        var groupName = $"entity-{ontologyId}-{entityType}-{entityId}";

        if (await _commentService.CanAddCommentAsync(ontologyId, userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} joined comment group for {EntityType} {EntityId} in ontology {OntologyId}",
                userId, entityType, entityId, ontologyId);
        }
    }

    /// <summary>
    /// Leave an entity-specific group
    /// </summary>
    public async Task LeaveEntity(int ontologyId, string entityType, int entityId)
    {
        var userId = GetUserId();
        var groupName = $"entity-{ontologyId}-{entityType}-{entityId}";

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} left comment group for {EntityType} {EntityId} in ontology {OntologyId}",
            userId, entityType, entityId, ontologyId);
    }

    /// <summary>
    /// Notify that a user is typing a comment
    /// </summary>
    public async Task UserTyping(int ontologyId, string entityType, int entityId, string userName)
    {
        var userId = GetUserId();
        var groupName = $"entity-{ontologyId}-{entityType}-{entityId}";

        // Broadcast to all except the sender
        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new
        {
            UserId = userId,
            UserName = userName,
            EntityType = entityType,
            EntityId = entityId
        });
    }

    /// <summary>
    /// Notify that a user stopped typing
    /// </summary>
    public async Task UserStoppedTyping(int ontologyId, string entityType, int entityId, string userId)
    {
        var groupName = $"entity-{ontologyId}-{entityType}-{entityId}";

        await Clients.OthersInGroup(groupName).SendAsync("UserStoppedTyping", new
        {
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId
        });
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} connected to CommentHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (exception != null)
        {
            _logger.LogWarning(exception, "User {UserId} disconnected from CommentHub with error", userId);
        }
        else
        {
            _logger.LogInformation("User {UserId} disconnected from CommentHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
