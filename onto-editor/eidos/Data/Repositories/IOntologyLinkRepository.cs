using Eidos.Models;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository interface for OntologyLink operations
/// Handles both external (URI-based) and internal (virtualized) ontology links
/// </summary>
public interface IOntologyLinkRepository : IRepository<OntologyLink>
{
    /// <summary>
    /// Get all links for a specific ontology
    /// </summary>
    /// <param name="ontologyId">The ontology ID</param>
    /// <returns>Collection of ontology links</returns>
    Task<IEnumerable<OntologyLink>> GetByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Get a link with all related entities loaded
    /// Includes Ontology and LinkedOntology navigation properties
    /// </summary>
    /// <param name="id">The link ID</param>
    /// <returns>OntologyLink with related entities, or null if not found</returns>
    Task<OntologyLink?> GetWithRelatedAsync(int id);

    /// <summary>
    /// Get all internal (virtualized) links for an ontology
    /// </summary>
    /// <param name="ontologyId">The ontology ID</param>
    /// <returns>Collection of internal ontology links</returns>
    Task<IEnumerable<OntologyLink>> GetInternalLinksByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Get all external (URI-based) links for an ontology
    /// </summary>
    /// <param name="ontologyId">The ontology ID</param>
    /// <returns>Collection of external ontology links</returns>
    Task<IEnumerable<OntologyLink>> GetExternalLinksByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Get all ontologies that link to a specific target ontology
    /// Used for detecting circular dependencies and understanding dependencies
    /// </summary>
    /// <param name="targetOntologyId">The target ontology ID</param>
    /// <returns>Collection of ontologies that reference the target</returns>
    Task<IEnumerable<int>> GetDependentOntologyIdsAsync(int targetOntologyId);

    /// <summary>
    /// Check if a link already exists between two ontologies
    /// Prevents duplicate internal links
    /// </summary>
    /// <param name="parentOntologyId">The parent ontology ID</param>
    /// <param name="linkedOntologyId">The linked ontology ID</param>
    /// <returns>True if link exists, false otherwise</returns>
    Task<bool> LinkExistsAsync(int parentOntologyId, int linkedOntologyId);

    /// <summary>
    /// Get links that need synchronization (UpdateAvailable = true)
    /// Used for batch sync operations
    /// </summary>
    /// <param name="ontologyId">Optional ontology ID filter</param>
    /// <returns>Collection of links needing sync</returns>
    Task<IEnumerable<OntologyLink>> GetLinksNeedingSyncAsync(int? ontologyId = null);
}
