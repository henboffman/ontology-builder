using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing concept restrictions
/// Focused on Single Responsibility Principle
/// </summary>
public interface IRestrictionService
{
    /// <summary>
    /// Create a new restriction
    /// </summary>
    Task<ConceptRestriction> CreateAsync(ConceptRestriction restriction);

    /// <summary>
    /// Update an existing restriction
    /// </summary>
    Task<ConceptRestriction> UpdateAsync(ConceptRestriction restriction);

    /// <summary>
    /// Delete a restriction by ID
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Get a restriction by ID
    /// </summary>
    Task<ConceptRestriction?> GetByIdAsync(int id);

    /// <summary>
    /// Get a restriction with details loaded
    /// </summary>
    Task<ConceptRestriction?> GetWithDetailsAsync(int id);

    /// <summary>
    /// Get all restrictions for a specific concept
    /// </summary>
    Task<IEnumerable<ConceptRestriction>> GetByConceptIdAsync(int conceptId);

    /// <summary>
    /// Get all restrictions for concepts in an ontology
    /// </summary>
    Task<IEnumerable<ConceptRestriction>> GetByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Delete all restrictions for a specific concept
    /// </summary>
    Task DeleteByConceptIdAsync(int conceptId);

    /// <summary>
    /// Validate if a property value satisfies the restrictions for a concept
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage)> ValidatePropertyAsync(int conceptId, string propertyName, string? value);
}
