using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing concept properties
/// </summary>
public interface IPropertyService
{
    /// <summary>
    /// Create a new property
    /// </summary>
    Task<Property> CreateAsync(Property property);

    /// <summary>
    /// Update an existing property
    /// </summary>
    Task<Property> UpdateAsync(Property property);

    /// <summary>
    /// Delete a property by ID
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Get all properties for a concept
    /// </summary>
    Task<IEnumerable<Property>> GetByConceptIdAsync(int conceptId);
}
