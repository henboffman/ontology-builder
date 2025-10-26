using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;
using Eidos.Models.DTOs;
using Eidos.Models.Enums;
using Eidos.Services.Interfaces;

namespace Eidos.Services;

/// <summary>
/// Service for managing collaborative sharing of ontologies
/// Handles share link generation, validation, and permission checking
/// </summary>
public class OntologyShareService : IOntologyShareService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly ILogger<OntologyShareService> _logger;

    public OntologyShareService(
        IDbContextFactory<OntologyDbContext> contextFactory,
        ILogger<OntologyShareService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Create a new share link with cryptographically secure token
    /// </summary>
    public async Task<OntologyShare> CreateShareAsync(
        int ontologyId,
        string userId,
        PermissionLevel permissionLevel,
        bool allowGuestAccess,
        DateTime? expiresAt = null,
        string? note = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Verify ontology exists and user has permission
        var ontology = await context.Ontologies
            .FirstOrDefaultAsync(o => o.Id == ontologyId);

        if (ontology == null)
        {
            throw new InvalidOperationException($"Ontology {ontologyId} not found");
        }

        // Security: Generate cryptographically secure random token
        var shareToken = GenerateSecureToken();

        var share = new OntologyShare
        {
            OntologyId = ontologyId,
            CreatedByUserId = userId,
            ShareToken = shareToken,
            PermissionLevel = permissionLevel,
            AllowGuestAccess = allowGuestAccess,
            ExpiresAt = expiresAt,
            Note = note,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.OntologyShares.Add(share);
        await context.SaveChangesAsync();

        _logger.LogInformation("Created share link for ontology {OntologyId} with permission level {PermissionLevel}",
            ontologyId, permissionLevel);

        return share;
    }

    /// <summary>
    /// Get share by token with related data
    /// </summary>
    public async Task<OntologyShare?> GetShareByTokenAsync(string shareToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.OntologyShares
            .Include(s => s.Ontology)
            .Include(s => s.CreatedBy)
            .FirstOrDefaultAsync(s => s.ShareToken == shareToken);
    }

    /// <summary>
    /// Get all shares for an ontology
    /// </summary>
    public async Task<IEnumerable<OntologyShare>> GetSharesForOntologyAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.OntologyShares
            .Include(s => s.GuestSessions)
            .Where(s => s.OntologyId == ontologyId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Update an existing share
    /// </summary>
    public async Task<OntologyShare> UpdateShareAsync(
        int shareId,
        PermissionLevel? permissionLevel = null,
        bool? allowGuestAccess = null,
        bool? isActive = null,
        DateTime? expiresAt = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var share = await context.OntologyShares.FindAsync(shareId);
        if (share == null)
        {
            throw new InvalidOperationException($"Share {shareId} not found");
        }

        if (permissionLevel.HasValue)
            share.PermissionLevel = permissionLevel.Value;

        if (allowGuestAccess.HasValue)
            share.AllowGuestAccess = allowGuestAccess.Value;

        if (isActive.HasValue)
            share.IsActive = isActive.Value;

        if (expiresAt != null)
            share.ExpiresAt = expiresAt;

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated share {ShareId}", shareId);

        return share;
    }

    /// <summary>
    /// Delete a share and all associated guest sessions
    /// </summary>
    public async Task DeleteShareAsync(int shareId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var share = await context.OntologyShares.FindAsync(shareId);
        if (share == null)
        {
            throw new InvalidOperationException($"Share {shareId} not found");
        }

        context.OntologyShares.Remove(share);
        await context.SaveChangesAsync();

        _logger.LogInformation("Deleted share {ShareId}", shareId);
    }

    /// <summary>
    /// Validate if share is accessible (active, not expired, etc.)
    /// </summary>
    public async Task<(bool IsValid, string? ErrorMessage)> ValidateShareAccessAsync(string shareToken)
    {
        var share = await GetShareByTokenAsync(shareToken);

        if (share == null)
        {
            return (false, "Share link not found");
        }

        if (!share.IsActive)
        {
            return (false, "This share link has been deactivated");
        }

        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
        {
            return (false, "This share link has expired");
        }

        return (true, null);
    }

    /// <summary>
    /// Record access to a share (increment count, update last access)
    /// For authenticated users, also tracks which users have accessed which shares
    /// </summary>
    public async Task RecordShareAccessAsync(string shareToken, string? userId = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var share = await context.OntologyShares
            .FirstOrDefaultAsync(s => s.ShareToken == shareToken);

        if (share != null)
        {
            // Update share access count
            share.AccessCount++;
            share.LastAccessedAt = DateTime.UtcNow;

            // If user is authenticated, track their access to this share
            if (!string.IsNullOrEmpty(userId))
            {
                var userAccess = await context.UserShareAccesses
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.OntologyShareId == share.Id);

                if (userAccess == null)
                {
                    // First time this user is accessing this share
                    userAccess = new UserShareAccess
                    {
                        UserId = userId,
                        OntologyShareId = share.Id,
                        FirstAccessedAt = DateTime.UtcNow,
                        LastAccessedAt = DateTime.UtcNow,
                        AccessCount = 1
                    };
                    context.UserShareAccesses.Add(userAccess);
                }
                else
                {
                    // Update existing user access record
                    userAccess.LastAccessedAt = DateTime.UtcNow;
                    userAccess.AccessCount++;
                }
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("Recorded share access for token {ShareToken} by user {UserId}",
                shareToken, userId ?? "(anonymous)");
        }
    }

    /// <summary>
    /// Create a guest session for anonymous users
    /// </summary>
    public async Task<GuestSession> CreateGuestSessionAsync(
        int shareId,
        string? guestName = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var sessionToken = GenerateSecureToken();

        var session = new GuestSession
        {
            OntologyShareId = shareId,
            SessionToken = sessionToken,
            GuestName = guestName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        context.GuestSessions.Add(session);
        await context.SaveChangesAsync();

        _logger.LogInformation("Created guest session for share {ShareId}", shareId);

        return session;
    }

    /// <summary>
    /// Get guest session by token
    /// </summary>
    public async Task<GuestSession?> GetGuestSessionByTokenAsync(string sessionToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.GuestSessions
            .Include(g => g.OntologyShare)
                .ThenInclude(s => s!.Ontology)
            .FirstOrDefaultAsync(g => g.SessionToken == sessionToken && g.IsActive);
    }

    /// <summary>
    /// Update last activity time for a guest session
    /// </summary>
    public async Task UpdateGuestSessionActivityAsync(string sessionToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var session = await context.GuestSessions
            .FirstOrDefaultAsync(g => g.SessionToken == sessionToken);

        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Check if user/guest has required permission level
    /// </summary>
    public async Task<bool> HasPermissionAsync(
        int ontologyId,
        string? userId,
        string? sessionToken,
        PermissionLevel requiredLevel)
    {
        var actualLevel = await GetPermissionLevelAsync(ontologyId, userId, sessionToken);

        if (actualLevel == null)
            return false;

        // Check if actual level meets or exceeds required level
        return actualLevel.Value >= requiredLevel;
    }

    /// <summary>
    /// Get the permission level for a user/guest
    /// Returns null if no access
    /// </summary>
    public async Task<PermissionLevel?> GetPermissionLevelAsync(
        int ontologyId,
        string? userId,
        string? sessionToken)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Check if user is the owner FIRST (full access) - owners always have full access regardless of share links
        if (!string.IsNullOrEmpty(userId))
        {
            var ontology = await context.Ontologies
                .FirstOrDefaultAsync(o => o.Id == ontologyId);

            if (ontology?.UserId == userId)
            {
                // Owner always has full access, don't check share links
                return PermissionLevel.FullAccess;
            }

            // Only check share links if user is NOT the owner
            // Check if authenticated user has accessed this ontology via a share link
            var userShareAccess = await context.UserShareAccesses
                .Include(usa => usa.OntologyShare)
                .Where(usa => usa.UserId == userId)
                .Where(usa => usa.OntologyShare != null &&
                             usa.OntologyShare.OntologyId == ontologyId &&
                             usa.OntologyShare.IsActive)
                .FirstOrDefaultAsync();

            if (userShareAccess?.OntologyShare != null)
            {
                // Validate expiration
                if (userShareAccess.OntologyShare.ExpiresAt.HasValue &&
                    userShareAccess.OntologyShare.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return null; // Expired
                }

                return userShareAccess.OntologyShare.PermissionLevel;
            }
        }

        // Check for guest session
        if (!string.IsNullOrEmpty(sessionToken))
        {
            var session = await context.GuestSessions
                .Include(g => g.OntologyShare)
                .FirstOrDefaultAsync(g => g.SessionToken == sessionToken && g.IsActive);

            if (session?.OntologyShare != null &&
                session.OntologyShare.OntologyId == ontologyId &&
                session.OntologyShare.IsActive)
            {
                // Validate expiration
                if (session.OntologyShare.ExpiresAt.HasValue &&
                    session.OntologyShare.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return null; // Expired
                }

                return session.OntologyShare.PermissionLevel;
            }
        }

        // No access found
        return null;
    }

    /// <summary>
    /// Get all ontologies shared with a specific user
    /// This does NOT include ontologies owned by the user
    /// Returns ontologies that the user has accessed via share links
    /// </summary>
    public async Task<List<Ontology>> GetSharedOntologiesForUserAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Find all shares that this user has accessed
        var sharedOntologies = await context.UserShareAccesses
            .Where(ua => ua.UserId == userId)
            .Include(ua => ua.OntologyShare)
                .ThenInclude(s => s!.Ontology)
            .Where(ua => ua.OntologyShare != null &&
                         ua.OntologyShare.IsActive &&
                         (!ua.OntologyShare.ExpiresAt.HasValue || ua.OntologyShare.ExpiresAt.Value > DateTime.UtcNow))
            .Select(ua => ua.OntologyShare!.Ontology!)
            .Where(o => o.UserId != userId) // Exclude ontologies owned by the user
            .Distinct()
            .ToListAsync();

        _logger.LogInformation("Found {Count} shared ontologies for user {UserId}",
            sharedOntologies.Count, userId);

        return sharedOntologies;
    }

    /// <summary>
    /// Get all collaborators (authenticated users and guests) who have access to an ontology
    /// Includes their access information and recent activity
    /// </summary>
    public async Task<List<CollaboratorInfo>> GetCollaboratorsAsync(int ontologyId, int recentActivityLimit = 10)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var collaborators = new List<CollaboratorInfo>();

        // Get authenticated users who have accessed this ontology via share links
        var userAccesses = await context.UserShareAccesses
            .Include(ua => ua.User)
            .Include(ua => ua.OntologyShare)
            .Where(ua => ua.OntologyShare != null &&
                         ua.OntologyShare.OntologyId == ontologyId &&
                         ua.OntologyShare.IsActive)
            .ToListAsync();

        foreach (var userAccess in userAccesses)
        {
            if (userAccess.User == null || userAccess.OntologyShare == null) continue;

            // Get recent activities for this user
            var recentActivities = await context.OntologyActivities
                .Where(a => a.OntologyId == ontologyId && a.UserId == userAccess.UserId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(recentActivityLimit)
                .Select(a => new CollaboratorActivity
                {
                    Id = a.Id,
                    ActivityType = a.ActivityType,
                    EntityType = a.EntityType,
                    EntityName = a.EntityName,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt,
                    VersionNumber = a.VersionNumber
                })
                .ToListAsync();

            // Calculate edit statistics
            var editStats = await CalculateEditStatsAsync(context, ontologyId, userAccess.UserId);

            collaborators.Add(new CollaboratorInfo
            {
                UserId = userAccess.UserId,
                Name = userAccess.User.UserName ?? userAccess.User.Email ?? "Unknown User",
                Email = userAccess.User.Email,
                PermissionLevel = userAccess.OntologyShare.PermissionLevel,
                FirstAccessedAt = userAccess.FirstAccessedAt,
                LastAccessedAt = userAccess.LastAccessedAt,
                AccessCount = userAccess.AccessCount,
                IsGuest = false,
                RecentActivities = recentActivities,
                EditStats = editStats
            });
        }

        // Get active guest sessions for this ontology
        var guestSessions = await context.GuestSessions
            .Include(g => g.OntologyShare)
            .Where(g => g.IsActive &&
                       g.OntologyShare != null &&
                       g.OntologyShare.OntologyId == ontologyId &&
                       g.OntologyShare.IsActive)
            .ToListAsync();

        foreach (var guestSession in guestSessions)
        {
            if (guestSession.OntologyShare == null) continue;

            // Get recent activities for this guest
            var recentActivities = await context.OntologyActivities
                .Where(a => a.OntologyId == ontologyId && a.GuestSessionToken == guestSession.SessionToken)
                .OrderByDescending(a => a.CreatedAt)
                .Take(recentActivityLimit)
                .Select(a => new CollaboratorActivity
                {
                    Id = a.Id,
                    ActivityType = a.ActivityType,
                    EntityType = a.EntityType,
                    EntityName = a.EntityName,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt,
                    VersionNumber = a.VersionNumber
                })
                .ToListAsync();

            // Calculate edit statistics for guest
            var editStats = await CalculateEditStatsForGuestAsync(context, ontologyId, guestSession.SessionToken);

            collaborators.Add(new CollaboratorInfo
            {
                UserId = null,
                Name = guestSession.GuestName ?? $"Guest ({guestSession.SessionToken.Substring(0, 8)}...)",
                Email = null,
                PermissionLevel = guestSession.OntologyShare.PermissionLevel,
                FirstAccessedAt = guestSession.CreatedAt,
                LastAccessedAt = guestSession.LastActivityAt,
                AccessCount = 1, // Guest sessions don't track access count separately
                IsGuest = true,
                RecentActivities = recentActivities,
                EditStats = editStats
            });
        }

        _logger.LogInformation("Retrieved {Count} collaborators for ontology {OntologyId}",
            collaborators.Count, ontologyId);

        return collaborators.OrderByDescending(c => c.LastAccessedAt).ToList();
    }

    /// <summary>
    /// Get detailed activity history for a specific user on an ontology
    /// </summary>
    public async Task<List<CollaboratorActivity>> GetUserActivityAsync(int ontologyId, string userId, int limit = 50)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var activities = await context.OntologyActivities
            .Where(a => a.OntologyId == ontologyId && a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new CollaboratorActivity
            {
                Id = a.Id,
                ActivityType = a.ActivityType,
                EntityType = a.EntityType,
                EntityName = a.EntityName,
                Description = a.Description,
                CreatedAt = a.CreatedAt,
                VersionNumber = a.VersionNumber
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} activities for user {UserId} on ontology {OntologyId}",
            activities.Count, userId, ontologyId);

        return activities;
    }

    /// <summary>
    /// Calculate edit statistics for a user
    /// </summary>
    private async Task<CollaboratorEditStats> CalculateEditStatsAsync(
        OntologyDbContext context,
        int ontologyId,
        string userId)
    {
        var activities = await context.OntologyActivities
            .Where(a => a.OntologyId == ontologyId && a.UserId == userId)
            .ToListAsync();

        return new CollaboratorEditStats
        {
            TotalEdits = activities.Count,
            ConceptsCreated = activities.Count(a => a.EntityType == EntityTypes.Concept && a.ActivityType == ActivityTypes.Create),
            ConceptsUpdated = activities.Count(a => a.EntityType == EntityTypes.Concept && a.ActivityType == ActivityTypes.Update),
            ConceptsDeleted = activities.Count(a => a.EntityType == EntityTypes.Concept && a.ActivityType == ActivityTypes.Delete),
            RelationshipsCreated = activities.Count(a => a.EntityType == EntityTypes.Relationship && a.ActivityType == ActivityTypes.Create),
            RelationshipsUpdated = activities.Count(a => a.EntityType == EntityTypes.Relationship && a.ActivityType == ActivityTypes.Update),
            RelationshipsDeleted = activities.Count(a => a.EntityType == EntityTypes.Relationship && a.ActivityType == ActivityTypes.Delete),
            PropertiesCreated = activities.Count(a => a.EntityType == EntityTypes.Property && a.ActivityType == ActivityTypes.Create),
            PropertiesUpdated = activities.Count(a => a.EntityType == EntityTypes.Property && a.ActivityType == ActivityTypes.Update),
            PropertiesDeleted = activities.Count(a => a.EntityType == EntityTypes.Property && a.ActivityType == ActivityTypes.Delete),
            LastEditDate = activities.Any() ? activities.Max(a => a.CreatedAt) : null
        };
    }

    /// <summary>
    /// Calculate edit statistics for a guest user
    /// </summary>
    private async Task<CollaboratorEditStats> CalculateEditStatsForGuestAsync(
        OntologyDbContext context,
        int ontologyId,
        string guestSessionToken)
    {
        var activities = await context.OntologyActivities
            .Where(a => a.OntologyId == ontologyId && a.GuestSessionToken == guestSessionToken)
            .ToListAsync();

        return new CollaboratorEditStats
        {
            TotalEdits = activities.Count,
            ConceptsCreated = activities.Count(a => a.EntityType == EntityTypes.Concept && a.ActivityType == ActivityTypes.Create),
            ConceptsUpdated = activities.Count(a => a.EntityType == EntityTypes.Concept && a.ActivityType == ActivityTypes.Update),
            ConceptsDeleted = activities.Count(a => a.EntityType == EntityTypes.Concept && a.ActivityType == ActivityTypes.Delete),
            RelationshipsCreated = activities.Count(a => a.EntityType == EntityTypes.Relationship && a.ActivityType == ActivityTypes.Create),
            RelationshipsUpdated = activities.Count(a => a.EntityType == EntityTypes.Relationship && a.ActivityType == ActivityTypes.Update),
            RelationshipsDeleted = activities.Count(a => a.EntityType == EntityTypes.Relationship && a.ActivityType == ActivityTypes.Delete),
            PropertiesCreated = activities.Count(a => a.EntityType == EntityTypes.Property && a.ActivityType == ActivityTypes.Create),
            PropertiesUpdated = activities.Count(a => a.EntityType == EntityTypes.Property && a.ActivityType == ActivityTypes.Update),
            PropertiesDeleted = activities.Count(a => a.EntityType == EntityTypes.Property && a.ActivityType == ActivityTypes.Delete),
            LastEditDate = activities.Any() ? activities.Max(a => a.CreatedAt) : null
        };
    }

    /// <summary>
    /// Generate a cryptographically secure random token
    /// Security: Uses RandomNumberGenerator for cryptographically secure randomness
    /// </summary>
    private static string GenerateSecureToken()
    {
        // Generate 32 bytes (256 bits) of cryptographically secure random data
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        // Convert to URL-safe base64 string
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
