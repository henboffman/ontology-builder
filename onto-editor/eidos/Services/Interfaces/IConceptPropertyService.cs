using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing concept property definitions (OWL properties at class level).
/// Handles CRUD operations and validation for DataProperty and ObjectProperty definitions.
/// </summary>
public interface IConceptPropertyService
{
    /// <summary>
    /// Gets all property definitions for a concept
    /// </summary>
    /// <param name="conceptId">The concept ID</param>
    /// <returns>List of property definitions for the concept</returns>
    Task<ICollection<ConceptProperty>> GetPropertiesByConceptIdAsync(int conceptId);

    /// <summary>
    /// Gets a specific property definition by ID
    /// </summary>
    /// <param name="id">The property definition ID</param>
    /// <returns>The property definition, or null if not found</returns>
    Task<ConceptProperty?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new property definition for a concept
    /// </summary>
    /// <param name="conceptProperty">The property definition to create</param>
    /// <returns>The created property definition with assigned ID</returns>
    /// <exception cref="ArgumentException">If validation fails</exception>
    Task<ConceptProperty> CreateAsync(ConceptProperty conceptProperty);

    /// <summary>
    /// Updates an existing property definition
    /// </summary>
    /// <param name="conceptProperty">The property definition with updated values</param>
    /// <returns>The updated property definition</returns>
    /// <exception cref="ArgumentException">If validation fails or property not found</exception>
    Task<ConceptProperty> UpdateAsync(ConceptProperty conceptProperty);

    /// <summary>
    /// Deletes a property definition
    /// </summary>
    /// <param name="id">The property definition ID to delete</param>
    /// <exception cref="ArgumentException">If property not found</exception>
    Task DeleteAsync(int id);

    /// <summary>
    /// Checks if a property name already exists for a concept
    /// </summary>
    /// <param name="conceptId">The concept ID</param>
    /// <param name="propertyName">The property name to check</param>
    /// <param name="excludePropertyId">Optional property ID to exclude from check (for updates)</param>
    /// <returns>True if name exists, false otherwise</returns>
    Task<bool> PropertyNameExistsAsync(int conceptId, string propertyName, int? excludePropertyId = null);

    /// <summary>
    /// Validates a property definition
    /// </summary>
    /// <param name="conceptProperty">The property to validate</param>
    /// <exception cref="ArgumentException">If validation fails with detailed error message</exception>
    Task ValidateAsync(ConceptProperty conceptProperty);
}
