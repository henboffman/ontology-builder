using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;
using Eidos.Services;
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
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OntologyDbContext>>();

        try
        {
            // Get the current user ID from the authenticated context
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation(
                "JoinOntology called: OntologyId={OntologyId}, UserId={UserId}, ConnectionId={ConnectionId}",
                ontologyId,
                userId ?? "NULL",
                Context.ConnectionId);

            // Verify the user has permission to access this ontology
            // Create a new context for this operation
            await using var context = await contextFactory.CreateDbContextAsync();

            var ontology = await context.Ontologies
                .Where(o => o.Id == ontologyId)
                .Select(o => new { o.UserId })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (ontology == null)
            {
                _logger.LogWarning(
                    "Ontology not found: OntologyId={OntologyId}, UserId={UserId}",
                    ontologyId,
                    userId ?? "NULL");
                throw new HubException("Ontology not found");
            }

            _logger.LogInformation(
                "Ontology found: OntologyId={OntologyId}, OwnerId={OwnerId}, RequestingUserId={UserId}",
                ontologyId,
                ontology.UserId ?? "NULL",
                userId ?? "NULL");

            // Check if user has permission to join:
            // 1. Owner always has permission
            // 2. Users with share access have permission
            // 3. Check OntologyShares and UserShareAccesses tables
            var hasPermission = false;
            var permissionReason = "none";

            if (string.IsNullOrEmpty(userId))
            {
                permissionReason = "not_authenticated";
            }
            else if (ontology.UserId == userId)
            {
                // Owner always has permission
                hasPermission = true;
                permissionReason = "owner";
            }
            else
            {
                // Check if user has been granted access via sharing
                var hasShareAccess = await context.UserShareAccesses
                    .Where(usa => usa.UserId == userId)
                    .Where(usa => usa.OntologyShare != null && usa.OntologyShare.OntologyId == ontologyId)
                    .Where(usa => usa.OntologyShare != null && usa.OntologyShare.IsActive)
                    .AnyAsync();

                if (hasShareAccess)
                {
                    hasPermission = true;
                    permissionReason = "shared_access";
                }
                else
                {
                    permissionReason = "no_access";
                }
            }

            _logger.LogInformation(
                "Permission check result: HasPermission={HasPermission}, Reason={Reason}, OntologyId={OntologyId}, UserId={UserId}",
                hasPermission,
                permissionReason,
                ontologyId,
                userId ?? "NULL");

            if (!hasPermission)
            {
                _logger.LogWarning(
                    "Permission denied: UserId={UserId}, OntologyId={OntologyId}, Reason={Reason}",
                    userId ?? "Guest",
                    ontologyId,
                    permissionReason);
                throw new HubException("You do not have permission to join this ontology");
            }


            _logger.LogInformation(
                "User {UserId} successfully joined ontology {OntologyId}",
                userId,
                ontologyId);

            var groupName = GetOntologyGroupName(ontologyId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Create presence info for this user
            // Try multiple claim types for display name to support different auth providers
            var userName = GetDisplayName(Context.User) ?? "Guest";
            var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
            var profilePhotoUrl = GetProfilePhotoUrl(Context.User);
            var isGuest = string.IsNullOrEmpty(userId);

            var presenceInfo = new PresenceInfo
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId ?? $"guest_{Context.ConnectionId}",
                UserName = userName,
                UserEmail = userEmail,
                ProfilePhotoUrl = profilePhotoUrl,
                JoinedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                Color = GetUserColor(userId ?? Context.ConnectionId),
                IsGuest = isGuest
            };

            // Add to presence tracking
            var ontologyPresence = _presenceByOntology.GetOrAdd(ontologyId, _ => new ConcurrentDictionary<string, PresenceInfo>());
            ontologyPresence[Context.ConnectionId] = presenceInfo;

            _logger.LogInformation(
                "User {UserId} joined ontology {OntologyId}",
                userId ?? "Guest",
                ontologyId);

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
            // Validate viewName to prevent storing arbitrary strings
            if (string.IsNullOrWhiteSpace(viewName) || viewName.Length > 50)
            {
                _logger.LogWarning("Invalid view name provided: {ViewName}", viewName);
                return;
            }

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

    /// <summary>
    /// Gets the profile photo URL from the user's claims
    /// Supports multiple authentication providers including Entra ID, Google, GitHub, Microsoft
    /// </summary>
    /// <remarks>
    /// For Entra ID (Azure AD) profile photos via Microsoft Graph API:
    ///
    /// 1. Add the Microsoft.Graph NuGet package
    /// 2. Request the "User.Read" scope in your authentication configuration
    /// 3. Add optional claim "picture" to your app registration in Azure Portal:
    ///    - App Registration > Token Configuration > Add optional claim > ID token > "picture"
    ///
    /// Alternative approach for Entra ID without optional claims:
    /// - Use Microsoft Graph API to fetch photo: GET https://graph.microsoft.com/v1.0/me/photo/$value
    /// - Requires creating a service to fetch and cache photos
    /// - Cache photos to avoid rate limiting
    ///
    /// Example Microsoft Graph API integration:
    /// <code>
    /// var graphClient = new GraphServiceClient(...);
    /// var photoStream = await graphClient.Me.Photo.Content.Request().GetAsync();
    /// // Convert stream to base64 or upload to blob storage
    /// </code>
    /// </remarks>
    private static string? GetProfilePhotoUrl(System.Security.Claims.ClaimsPrincipal? user)
    {
        if (user == null) return null;

        // Priority order for profile photo claims:
        // 1. "picture" - Standard OAuth 2.0 claim (Google, GitHub, Microsoft, Entra ID with optional claim)
        // 2. "avatar_url" - GitHub specific
        // 3. "photo" - Some OAuth providers
        // 4. "image" - Alternative claim name

        // Try "picture" claim (most common - OIDC standard)
        var pictureClaim = user.FindFirst("picture")?.Value;
        if (!string.IsNullOrWhiteSpace(pictureClaim)) return pictureClaim;

        // Try "avatar_url" (GitHub)
        var avatarUrl = user.FindFirst("avatar_url")?.Value;
        if (!string.IsNullOrWhiteSpace(avatarUrl)) return avatarUrl;

        // Try "photo" claim
        var photoClaim = user.FindFirst("photo")?.Value;
        if (!string.IsNullOrWhiteSpace(photoClaim)) return photoClaim;

        // Try "image" claim
        var imageClaim = user.FindFirst("image")?.Value;
        if (!string.IsNullOrWhiteSpace(imageClaim)) return imageClaim;

        return null;
    }
}
