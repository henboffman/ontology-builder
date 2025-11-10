using Eidos.Models;
using Eidos.Models.DTOs;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for tracking when users view ontologies and retrieving "What's New" data
/// </summary>
public interface IOntologyViewHistoryService
{
    /// <summary>
    /// Record that a user has viewed an ontology
    /// Updates the last viewed timestamp, current session ID, and increments view count
    /// </summary>
    Task RecordViewAsync(int ontologyId, string userId, string sessionId);

    /// <summary>
    /// Get changes made to an ontology since the user's last session
    /// Returns null if this is the user's first visit or if there are no changes
    /// Uses session-based tracking instead of timestamp-based
    /// </summary>
    Task<WhatsNewDto?> GetWhatsNewAsync(int ontologyId, string userId, string sessionId);

    /// <summary>
    /// Mark the "What's New" panel as dismissed for the current set of changes
    /// Updates the LastDismissedAt timestamp and LastDismissedSessionId
    /// </summary>
    Task DismissWhatsNewAsync(int ontologyId, string userId, string sessionId);

    /// <summary>
    /// Get the user's view history for an ontology
    /// </summary>
    Task<OntologyViewHistory?> GetViewHistoryAsync(int ontologyId, string userId);
}
