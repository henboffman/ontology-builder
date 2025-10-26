using Eidos.Models;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository interface for ConceptRestriction-specific operations
/// </summary>
public interface IRestrictionRepository : IRepository<ConceptRestriction>
{
    /// <summary>
    /// Get all restrictions for a specific concept
    /// </summary>
    Task<IEnumerable<ConceptRestriction>> GetByConceptIdAsync(int conceptId);

    /// <summary>
    /// Get a restriction with its related concept data loaded
    /// </summary>
    Task<ConceptRestriction?> GetWithDetailsAsync(int id);

    /// <summary>
    /// Get all restrictions for concepts in a specific ontology
    /// </summary>
    Task<IEnumerable<ConceptRestriction>> GetByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Delete all restrictions for a specific concept
    /// </summary>
    Task DeleteByConceptIdAsync(int conceptId);
}
