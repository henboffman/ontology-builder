using Eidos.Models;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository interface for Concept-specific operations
/// </summary>
public interface IConceptRepository : IRepository<Concept>
{
    /// <summary>
    /// Get a concept with all its properties
    /// </summary>
    Task<Concept?> GetWithPropertiesAsync(int id);

    /// <summary>
    /// Get all concepts for a specific ontology
    /// </summary>
    Task<IEnumerable<Concept>> GetByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Search concepts by name or content
    /// </summary>
    Task<IEnumerable<Concept>> SearchAsync(string query);
}
