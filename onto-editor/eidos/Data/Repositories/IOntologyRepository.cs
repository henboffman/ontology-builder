using Eidos.Models;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository interface for Ontology-specific operations
/// </summary>
public interface IOntologyRepository : IRepository<Ontology>
{
    /// <summary>
    /// Get an ontology with all related data (concepts, relationships, etc.)
    /// </summary>
    Task<Ontology?> GetWithAllRelatedDataAsync(int id);

    /// <summary>
    /// Get an ontology with progress reporting
    /// </summary>
    Task<Ontology?> GetWithProgressAsync(int id, Action<ImportProgress>? onProgress = null);

    /// <summary>
    /// Get all ontologies with basic concept and relationship counts
    /// </summary>
    Task<List<Ontology>> GetAllWithBasicDataAsync();

    /// <summary>
    /// Update the UpdatedAt timestamp for an ontology
    /// </summary>
    Task UpdateTimestampAsync(int ontologyId);

    /// <summary>
    /// Increment the concept count for an ontology
    /// </summary>
    Task IncrementConceptCountAsync(int ontologyId);

    /// <summary>
    /// Decrement the concept count for an ontology
    /// </summary>
    Task DecrementConceptCountAsync(int ontologyId);

    /// <summary>
    /// Increment the relationship count for an ontology
    /// </summary>
    Task IncrementRelationshipCountAsync(int ontologyId);

    /// <summary>
    /// Decrement the relationship count for an ontology
    /// </summary>
    Task DecrementRelationshipCountAsync(int ontologyId);
}
