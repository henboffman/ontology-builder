using Eidos.Models;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository interface for Individual-specific operations
/// </summary>
public interface IIndividualRepository : IRepository<Individual>
{
    /// <summary>
    /// Get an individual with all its properties
    /// </summary>
    Task<Individual?> GetWithPropertiesAsync(int id);

    /// <summary>
    /// Get an individual with all its properties and relationships
    /// </summary>
    Task<Individual?> GetWithDetailsAsync(int id);

    /// <summary>
    /// Get all individuals for a specific ontology
    /// </summary>
    Task<IEnumerable<Individual>> GetByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Get all individuals that are instances of a specific concept
    /// </summary>
    Task<IEnumerable<Individual>> GetByConceptIdAsync(int conceptId);

    /// <summary>
    /// Search individuals by name or description
    /// </summary>
    Task<IEnumerable<Individual>> SearchAsync(string query, int? ontologyId = null);

    /// <summary>
    /// Get count of individuals for a specific concept
    /// </summary>
    Task<int> GetCountByConceptIdAsync(int conceptId);
}
