using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Eidos.Models;
using Eidos.Services.Interfaces;
using System.Security.Claims;

namespace Eidos.Hubs;

/// <summary>
/// SignalR Hub for real-time collaborative ontology editing.
/// Handles broadcasting of concept and relationship changes to connected clients.
/// </summary>
[Authorize]
public class OntologyHub : Hub
{
    private readonly ILogger<OntologyHub> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public OntologyHub(
        ILogger<OntologyHub> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// Called when a user joins an ontology editing session
    /// </summary>
    /// <param name="ontologyId">The ID of the ontology to join</param>
    public async Task JoinOntology(int ontologyId)
    {
        // Create a scope to access scoped services
        using var scope = _serviceScopeFactory.CreateScope();
        var shareService = scope.ServiceProvider.GetRequiredService<IOntologyShareService>();

        try
        {
            // Get the current user ID from the authenticated context
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Verify the user has permission to access this ontology
            var permissionLevel = await shareService.GetPermissionLevelAsync(
                ontologyId,
                userId,
                sessionToken: null);

            if (permissionLevel == null)
            {
                _logger.LogWarning(
                    "User {UserId} (ConnectionId: {ConnectionId}) attempted to join ontology {OntologyId} without permission",
                    userId ?? "Guest",
                    Context.ConnectionId,
                    ontologyId);
                throw new HubException("You do not have permission to access this ontology");
            }

            var groupName = GetOntologyGroupName(ontologyId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation(
                "User {UserId} joined ontology {OntologyId} with permission {PermissionLevel}",
                userId ?? "Guest",
                ontologyId,
                permissionLevel);

            // Notify other users that someone joined
            await Clients.OthersInGroup(groupName).SendAsync("UserJoined", Context.ConnectionId);
        }
        catch (HubException)
        {
            // Re-throw HubException as-is so client receives proper error message
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining ontology {OntologyId}", ontologyId);
            throw new HubException("An error occurred while joining the ontology");
        }
    }

    /// <summary>
    /// Called when a user leaves an ontology editing session
    /// </summary>
    public async Task LeaveOntology(int ontologyId)
    {
        try
        {
            var groupName = GetOntologyGroupName(ontologyId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation(
                "User {UserId} (ConnectionId: {ConnectionId}) left ontology {OntologyId}",
                userId ?? "Guest",
                Context.ConnectionId,
                ontologyId);

            // Notify other users that someone left (optional - for presence)
            await Clients.OthersInGroup(groupName).SendAsync("UserLeft", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving ontology {OntologyId}", ontologyId);
            // Don't throw - leaving a group shouldn't fail the disconnect process
        }
    }

    /// <summary>
    /// Called when the connection is closed
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("User {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Gets the SignalR group name for an ontology
    /// </summary>
    private static string GetOntologyGroupName(int ontologyId)
    {
        return $"ontology_{ontologyId}";
    }
}
