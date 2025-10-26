using Eidos.Models;
using Eidos.Models.DTOs;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing ontology version history and activity tracking
/// </summary>
public interface IOntologyActivityService
{
    /// <summary>
    /// Get the complete activity history for an ontology
    /// </summary>
    Task<List<OntologyActivityDto>> GetActivityHistoryAsync(int ontologyId, int skip = 0, int take = 50);

    /// <summary>
    /// Get activity history filtered by entity type
    /// </summary>
    Task<List<OntologyActivityDto>> GetActivityByEntityTypeAsync(int ontologyId, string entityType, int skip = 0, int take = 50);

    /// <summary>
    /// Get activity history filtered by user
    /// </summary>
    Task<List<OntologyActivityDto>> GetActivityByUserAsync(int ontologyId, string userId, int skip = 0, int take = 50);

    /// <summary>
    /// Get a specific version by version number
    /// </summary>
    Task<OntologyActivity?> GetVersionAsync(int ontologyId, int versionNumber);

    /// <summary>
    /// Get all versions for a specific entity
    /// </summary>
    Task<List<OntologyActivityDto>> GetEntityHistoryAsync(int ontologyId, string entityType, int entityId);

    /// <summary>
    /// Compare two versions and return the differences
    /// </summary>
    Task<VersionComparisonDto> CompareVersionsAsync(int activityId1, int activityId2);

    /// <summary>
    /// Revert the ontology to a specific version
    /// </summary>
    Task RevertToVersionAsync(int ontologyId, int versionNumber, string userId, string actorName);

    /// <summary>
    /// Get summary statistics for version history
    /// </summary>
    Task<VersionHistoryStatsDto> GetVersionStatsAsync(int ontologyId);

    /// <summary>
    /// Record a new activity (used by other services when entities change)
    /// </summary>
    Task RecordActivityAsync(OntologyActivity activity);

    /// <summary>
    /// Get the current version number for an ontology
    /// </summary>
    Task<int> GetCurrentVersionNumberAsync(int ontologyId);
}
