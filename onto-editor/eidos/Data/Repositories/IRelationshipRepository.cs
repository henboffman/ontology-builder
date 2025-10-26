using Eidos.Models;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository interface for Relationship-specific operations
/// </summary>
public interface IRelationshipRepository : IRepository<Relationship>
{
    /// <summary>
    /// Get all relationships for a specific ontology
    /// </summary>
    Task<IEnumerable<Relationship>> GetByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Get relationships involving a specific concept (as source or target)
    /// </summary>
    Task<IEnumerable<Relationship>> GetByConceptIdAsync(int conceptId);

    /// <summary>
    /// Check if a relationship already exists between two concepts
    /// </summary>
    Task<bool> ExistsAsync(int sourceId, int targetId, string relationType);

    /// <summary>
    /// Get a relationship with source and target concept details
    /// </summary>
    Task<Relationship?> GetWithConceptsAsync(int id);
}
