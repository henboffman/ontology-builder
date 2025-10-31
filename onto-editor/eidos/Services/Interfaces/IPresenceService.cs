using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing user presence in ontology editing sessions
/// Supports both in-memory (development) and distributed (Redis for production) storage
/// </summary>
public interface IPresenceService
{
    /// <summary>
    /// Add or update user presence for an ontology
    /// </summary>
    Task AddOrUpdatePresenceAsync(int ontologyId, PresenceInfo presenceInfo);

    /// <summary>
    /// Remove user presence from an ontology
    /// </summary>
    Task RemovePresenceAsync(int ontologyId, string connectionId);

    /// <summary>
    /// Get all users present in an ontology
    /// </summary>
    Task<List<PresenceInfo>> GetPresenceListAsync(int ontologyId);

    /// <summary>
    /// Update user's current view
    /// </summary>
    Task UpdateCurrentViewAsync(int ontologyId, string connectionId, string viewName);

    /// <summary>
    /// Update user's last seen time (heartbeat)
    /// </summary>
    Task UpdateLastSeenAsync(int ontologyId, string connectionId);

    /// <summary>
    /// Check if a connection exists in an ontology
    /// </summary>
    Task<bool> ConnectionExistsAsync(int ontologyId, string connectionId);

    /// <summary>
    /// Remove connection from all ontologies (cleanup on disconnect)
    /// </summary>
    Task RemoveConnectionFromAllOntologiesAsync(string connectionId);

    /// <summary>
    /// Clean up stale presence entries (older than threshold)
    /// </summary>
    Task CleanupStalePresenceAsync(TimeSpan threshold);
}
