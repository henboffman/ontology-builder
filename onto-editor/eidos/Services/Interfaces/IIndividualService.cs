using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing individual/instance operations
/// Focused on Single Responsibility Principle
/// </summary>
public interface IIndividualService
{
    /// <summary>
    /// Create a new individual instance
    /// </summary>
    Task<Individual> CreateAsync(Individual individual);

    /// <summary>
    /// Update an existing individual
    /// </summary>
    Task<Individual> UpdateAsync(Individual individual);

    /// <summary>
    /// Delete an individual by ID
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Get an individual by ID
    /// </summary>
    Task<Individual?> GetByIdAsync(int id);

    /// <summary>
    /// Get an individual with all properties loaded
    /// </summary>
    Task<Individual?> GetWithPropertiesAsync(int id);

    /// <summary>
    /// Get an individual with all properties and relationships loaded
    /// </summary>
    Task<Individual?> GetWithDetailsAsync(int id);

    /// <summary>
    /// Get all individuals for an ontology
    /// </summary>
    Task<IEnumerable<Individual>> GetByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Get all individuals that are instances of a specific concept
    /// </summary>
    Task<IEnumerable<Individual>> GetByConceptIdAsync(int conceptId);

    /// <summary>
    /// Search individuals by query
    /// </summary>
    Task<IEnumerable<Individual>> SearchAsync(string query, int? ontologyId = null);

    /// <summary>
    /// Get count of individuals for a specific concept
    /// </summary>
    Task<int> GetCountByConceptIdAsync(int conceptId);

    /// <summary>
    /// Add a property to an individual
    /// </summary>
    Task<IndividualProperty> AddPropertyAsync(IndividualProperty property);

    /// <summary>
    /// Update an individual property
    /// </summary>
    Task<IndividualProperty> UpdatePropertyAsync(IndividualProperty property);

    /// <summary>
    /// Delete an individual property
    /// </summary>
    Task DeletePropertyAsync(int propertyId);

    /// <summary>
    /// Create a relationship between two individuals
    /// </summary>
    Task<IndividualRelationship> CreateRelationshipAsync(IndividualRelationship relationship);

    /// <summary>
    /// Delete an individual relationship
    /// </summary>
    Task DeleteRelationshipAsync(int relationshipId);
}
