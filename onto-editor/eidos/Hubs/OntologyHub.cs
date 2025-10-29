using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Eidos.Models;
using Eidos.Services.Interfaces;
using System.Security.Claims;
using System.Collections.Concurrent;

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

    // In-memory storage for presence tracking
    // Key: OntologyId, Value: Dictionary of ConnectionId -> PresenceInfo
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, PresenceInfo>> _presenceByOntology = new();

    // Color palette for user avatars
    private static readonly string[] _avatarColors = new[]
    {
        "#FF6B6B", "#4ECDC4", "#45B7D1", "#FFA07A", "#98D8C8",
        "#F7DC6F", "#BB8FCE", "#85C1E2", "#F8B739", "#52B788",
        "#FFB4A2", "#A8DADC", "#E07A5F", "#81B29A", "#F2CC8F"
    };

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

            // Create presence info for this user
            // Try multiple claim types for display name to support different auth providers
            var userName = GetDisplayName(Context.User) ?? "Guest";
            var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
            var isGuest = string.IsNullOrEmpty(userId);

            var presenceInfo = new PresenceInfo
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId ?? $"guest_{Context.ConnectionId}",
                UserName = userName,
                UserEmail = userEmail,
                JoinedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                Color = GetUserColor(userId ?? Context.ConnectionId),
                IsGuest = isGuest
            };

            // Add to presence tracking
            var ontologyPresence = _presenceByOntology.GetOrAdd(ontologyId, _ => new ConcurrentDictionary<string, PresenceInfo>());
            ontologyPresence[Context.ConnectionId] = presenceInfo;

            _logger.LogInformation(
                "User {UserId} joined ontology {OntologyId} with permission {PermissionLevel}",
                userId ?? "Guest",
                ontologyId,
                permissionLevel);

            // Get all current users in this ontology
            var currentUsers = ontologyPresence.Values.ToList();

            // Notify other users that someone joined (with their presence info)
            await Clients.OthersInGroup(groupName).SendAsync("UserJoined", presenceInfo);

            // Send current presence list to the joining user
            await Clients.Caller.SendAsync("PresenceList", currentUsers);
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

            // Remove from presence tracking
            if (_presenceByOntology.TryGetValue(ontologyId, out var ontologyPresence))
            {
                ontologyPresence.TryRemove(Context.ConnectionId, out _);

                // Clean up empty ontology presence dictionaries
                if (ontologyPresence.IsEmpty)
                {
                    _presenceByOntology.TryRemove(ontologyId, out _);
                }
            }

            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation(
                "User {UserId} (ConnectionId: {ConnectionId}) left ontology {OntologyId}",
                userId ?? "Guest",
                Context.ConnectionId,
                ontologyId);

            // Notify other users that someone left
            await Clients.OthersInGroup(groupName).SendAsync("UserLeft", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving ontology {OntologyId}", ontologyId);
            // Don't throw - leaving a group shouldn't fail the disconnect process
        }
    }

    /// <summary>
    /// Update current user's view (e.g., "Graph", "List", "Hierarchy")
    /// </summary>
    public async Task UpdateCurrentView(int ontologyId, string viewName)
    {
        try
        {
            if (_presenceByOntology.TryGetValue(ontologyId, out var ontologyPresence))
            {
                if (ontologyPresence.TryGetValue(Context.ConnectionId, out var presence))
                {
                    presence.CurrentView = viewName;
                    presence.LastSeenAt = DateTime.UtcNow;

                    var groupName = GetOntologyGroupName(ontologyId);
                    await Clients.OthersInGroup(groupName).SendAsync("UserViewChanged", Context.ConnectionId, viewName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating view for ontology {OntologyId}", ontologyId);
        }
    }

    /// <summary>
    /// Send a heartbeat to update last seen time
    /// </summary>
    public async Task Heartbeat(int ontologyId)
    {
        try
        {
            if (_presenceByOntology.TryGetValue(ontologyId, out var ontologyPresence))
            {
                if (ontologyPresence.TryGetValue(Context.ConnectionId, out var presence))
                {
                    presence.LastSeenAt = DateTime.UtcNow;
                }
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing heartbeat for ontology {OntologyId}", ontologyId);
        }
    }

    /// <summary>
    /// Called when the connection is closed
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("User {ConnectionId} disconnected", Context.ConnectionId);

        // Remove this connection from all ontologies it was in
        foreach (var (ontologyId, ontologyPresence) in _presenceByOntology)
        {
            if (ontologyPresence.TryRemove(Context.ConnectionId, out _))
            {
                var groupName = GetOntologyGroupName(ontologyId);
                await Clients.OthersInGroup(groupName).SendAsync("UserLeft", Context.ConnectionId);

                // Clean up empty dictionaries
                if (ontologyPresence.IsEmpty)
                {
                    _presenceByOntology.TryRemove(ontologyId, out _);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Gets the SignalR group name for an ontology
    /// </summary>
    private static string GetOntologyGroupName(int ontologyId)
    {
        return $"ontology_{ontologyId}";
    }

    /// <summary>
    /// Gets a consistent color for a user based on their ID
    /// </summary>
    private static string GetUserColor(string userId)
    {
        // Use hash code to get consistent color for each user
        var hash = Math.Abs(userId.GetHashCode());
        return _avatarColors[hash % _avatarColors.Length];
    }

    /// <summary>
    /// Gets the display name from the user's claims, trying multiple claim types
    /// to support different authentication providers (Entra ID, Google, GitHub, etc.)
    /// </summary>
    private static string? GetDisplayName(System.Security.Claims.ClaimsPrincipal? user)
    {
        if (user == null) return null;

        // Priority order for display name claims:
        // 1. "name" - Common claim used by most OAuth providers and Entra ID
        // 2. ClaimTypes.Name - Standard .NET claim type
        // 3. "preferred_username" - Used by Entra ID/Azure AD
        // 4. ClaimTypes.GivenName + ClaimTypes.Surname - Full name from parts
        // 5. ClaimTypes.Email - Fallback to email if nothing else available

        // Try "name" claim (most common for OAuth providers and Entra ID)
        var nameClaim = user.FindFirst("name")?.Value;
        if (!string.IsNullOrWhiteSpace(nameClaim)) return nameClaim;

        // Try standard .NET ClaimTypes.Name
        var identityName = user.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(identityName)) return identityName;

        // Try "preferred_username" (Entra ID/Azure AD)
        var preferredUsername = user.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrWhiteSpace(preferredUsername)) return preferredUsername;

        // Try constructing from given name and surname
        var givenName = user.FindFirst(ClaimTypes.GivenName)?.Value;
        var surname = user.FindFirst(ClaimTypes.Surname)?.Value;
        if (!string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(surname))
        {
            return $"{givenName} {surname}".Trim();
        }

        // Fallback to email
        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrWhiteSpace(email))
        {
            // Return just the username part of the email (before @)
            return email.Split('@')[0];
        }

        return null;
    }
}
