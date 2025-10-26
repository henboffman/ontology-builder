using Eidos.Models;
using Eidos.Models.DTOs;
using Eidos.Models.Enums;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing collaborative sharing of ontologies
/// </summary>
public interface IOntologyShareService
{
    /// <summary>
    /// Create a new share link for an ontology
    /// </summary>
    Task<OntologyShare> CreateShareAsync(int ontologyId, string userId, PermissionLevel permissionLevel, bool allowGuestAccess, DateTime? expiresAt = null, string? note = null);

    /// <summary>
    /// Get a share by its token
    /// </summary>
    Task<OntologyShare?> GetShareByTokenAsync(string shareToken);

    /// <summary>
    /// Get all shares for an ontology
    /// </summary>
    Task<IEnumerable<OntologyShare>> GetSharesForOntologyAsync(int ontologyId);

    /// <summary>
    /// Update an existing share
    /// </summary>
    Task<OntologyShare> UpdateShareAsync(int shareId, PermissionLevel? permissionLevel = null, bool? allowGuestAccess = null, bool? isActive = null, DateTime? expiresAt = null);

    /// <summary>
    /// Delete a share
    /// </summary>
    Task DeleteShareAsync(int shareId);

    /// <summary>
    /// Validate if a share is accessible
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage)> ValidateShareAccessAsync(string shareToken);

    /// <summary>
    /// Record access to a share
    /// </summary>
    Task RecordShareAccessAsync(string shareToken, string? userId = null);

    /// <summary>
    /// Create or update a guest session
    /// </summary>
    Task<GuestSession> CreateGuestSessionAsync(int shareId, string? guestName = null, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Get guest session by token
    /// </summary>
    Task<GuestSession?> GetGuestSessionByTokenAsync(string sessionToken);

    /// <summary>
    /// Update guest session activity
    /// </summary>
    Task UpdateGuestSessionActivityAsync(string sessionToken);

    /// <summary>
    /// Check if a user/guest has permission to perform an operation
    /// </summary>
    Task<bool> HasPermissionAsync(int ontologyId, string? userId, string? sessionToken, PermissionLevel requiredLevel);

    /// <summary>
    /// Get the permission level for a user/guest on an ontology
    /// </summary>
    Task<PermissionLevel?> GetPermissionLevelAsync(int ontologyId, string? userId, string? sessionToken);

    /// <summary>
    /// Get all ontologies shared with a specific user
    /// </summary>
    Task<List<Ontology>> GetSharedOntologiesForUserAsync(string userId);

    /// <summary>
    /// Get all collaborators (users and guests) who have access to an ontology
    /// Includes their access information and recent activities
    /// </summary>
    Task<List<CollaboratorInfo>> GetCollaboratorsAsync(int ontologyId, int recentActivityLimit = 10);

    /// <summary>
    /// Get detailed activity history for a specific user on an ontology
    /// </summary>
    Task<List<CollaboratorActivity>> GetUserActivityAsync(int ontologyId, string userId, int limit = 50);
}
