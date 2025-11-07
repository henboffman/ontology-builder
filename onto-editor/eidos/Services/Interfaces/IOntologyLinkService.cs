using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing ontology links (both external URI-based and internal virtualized links)
/// Handles permission checking, circular dependency detection, and link operations
/// </summary>
public interface IOntologyLinkService
{
    /// <summary>
    /// Create an external link to an ontology via URI
    /// </summary>
    /// <param name="ontologyId">The ontology to add the link to</param>
    /// <param name="uri">The URI of the external ontology</param>
    /// <param name="label">Optional label for the link</param>
    /// <param name="userId">The user creating the link (for permission checking)</param>
    /// <returns>The created OntologyLink, or null if creation failed</returns>
    Task<OntologyLink?> CreateExternalLinkAsync(int ontologyId, string uri, string? label, string userId);

    /// <summary>
    /// Create an internal link to another ontology in the database (virtualized node)
    /// Performs permission checking and circular dependency detection
    /// </summary>
    /// <param name="parentOntologyId">The ontology to add the link to</param>
    /// <param name="linkedOntologyId">The ontology to link (will be virtualized)</param>
    /// <param name="userId">The user creating the link (for permission checking)</param>
    /// <param name="positionX">Optional X position for graph visualization</param>
    /// <param name="positionY">Optional Y position for graph visualization</param>
    /// <param name="color">Optional custom color for the node</param>
    /// <returns>Result containing the created link or error message</returns>
    Task<(bool Success, OntologyLink? Link, string? ErrorMessage)> CreateInternalLinkAsync(
        int parentOntologyId,
        int linkedOntologyId,
        string userId,
        double? positionX = null,
        double? positionY = null,
        string? color = null);

    /// <summary>
    /// Get all links for an ontology
    /// </summary>
    /// <param name="ontologyId">The ontology ID</param>
    /// <returns>Collection of links</returns>
    Task<IEnumerable<OntologyLink>> GetLinksForOntologyAsync(int ontologyId);

    /// <summary>
    /// Get all internal (virtualized) links for an ontology
    /// </summary>
    /// <param name="ontologyId">The ontology ID</param>
    /// <returns>Collection of internal links</returns>
    Task<IEnumerable<OntologyLink>> GetInternalLinksAsync(int ontologyId);

    /// <summary>
    /// Get all external (URI-based) links for an ontology
    /// </summary>
    /// <param name="ontologyId">The ontology ID</param>
    /// <returns>Collection of external links</returns>
    Task<IEnumerable<OntologyLink>> GetExternalLinksAsync(int ontologyId);

    /// <summary>
    /// Delete a link
    /// </summary>
    /// <param name="linkId">The link ID to delete</param>
    /// <param name="userId">The user deleting the link (for permission checking)</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteLinkAsync(int linkId, string userId);

    /// <summary>
    /// Update link position (for graph visualization)
    /// </summary>
    /// <param name="linkId">The link ID</param>
    /// <param name="positionX">New X position</param>
    /// <param name="positionY">New Y position</param>
    /// <param name="userId">The user updating (for permission checking)</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateLinkPositionAsync(int linkId, double positionX, double positionY, string userId);

    /// <summary>
    /// Update link color
    /// </summary>
    /// <param name="linkId">The link ID</param>
    /// <param name="color">New color value</param>
    /// <param name="userId">The user updating (for permission checking)</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateLinkColorAsync(int linkId, string color, string userId);

    /// <summary>
    /// Check if creating a link would create a circular dependency
    /// Uses DFS graph traversal to detect cycles
    /// </summary>
    /// <param name="parentOntologyId">The parent ontology</param>
    /// <param name="targetOntologyId">The ontology to link</param>
    /// <returns>True if circular dependency would be created</returns>
    Task<bool> WouldCreateCircularDependencyAsync(int parentOntologyId, int targetOntologyId);

    /// <summary>
    /// Get the full dependency chain for an ontology
    /// Returns all ontologies that this ontology depends on (directly or transitively)
    /// </summary>
    /// <param name="ontologyId">The ontology ID</param>
    /// <returns>Set of ontology IDs in the dependency chain</returns>
    Task<HashSet<int>> GetDependencyChainAsync(int ontologyId);

    /// <summary>
    /// Get ontologies that depend on a specific ontology
    /// Returns ontologies that have internal links pointing to the target
    /// </summary>
    /// <param name="ontologyId">The target ontology ID</param>
    /// <returns>Collection of dependent ontology IDs</returns>
    Task<IEnumerable<int>> GetDependentOntologiesAsync(int ontologyId);

    /// <summary>
    /// Mark a link as having an update available
    /// Called when the linked ontology is modified
    /// </summary>
    /// <param name="linkId">The link ID</param>
    /// <returns>True if marked successfully</returns>
    Task<bool> MarkUpdateAvailableAsync(int linkId);

    /// <summary>
    /// Synchronize a link (update LastSyncedAt timestamp)
    /// Called after the parent ontology processes updates from linked ontology
    /// </summary>
    /// <param name="linkId">The link ID</param>
    /// <returns>True if synced successfully</returns>
    Task<bool> SyncLinkAsync(int linkId);

    /// <summary>
    /// Get all links that need synchronization
    /// </summary>
    /// <param name="ontologyId">Optional ontology filter</param>
    /// <returns>Collection of links needing sync</returns>
    Task<IEnumerable<OntologyLink>> GetLinksNeedingSyncAsync(int? ontologyId = null);

    /// <summary>
    /// Get ontologies available for linking (user has view access)
    /// Excludes the current ontology and any that would create circular dependencies
    /// </summary>
    /// <param name="ontologyId">The ontology that will have the link</param>
    /// <param name="userId">The user requesting (for permission filtering)</param>
    /// <returns>Collection of ontologies available for linking</returns>
    Task<IEnumerable<Ontology>> GetAvailableOntologiesForLinkingAsync(int ontologyId, string userId);
}
