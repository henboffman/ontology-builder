using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing relationship operations
/// Focused on Single Responsibility Principle
/// </summary>
public interface IRelationshipService
{
    /// <summary>
    /// Create a new relationship
    /// </summary>
    Task<Relationship> CreateAsync(Relationship relationship, bool recordUndo = true);

    /// <summary>
    /// Update an existing relationship
    /// </summary>
    Task<Relationship> UpdateAsync(Relationship relationship, bool recordUndo = true);

    /// <summary>
    /// Delete a relationship by ID
    /// </summary>
    Task DeleteAsync(int id, bool recordUndo = true);

    /// <summary>
    /// Get a relationship by ID
    /// </summary>
    Task<Relationship?> GetByIdAsync(int id);

    /// <summary>
    /// Get all relationships for an ontology
    /// </summary>
    Task<IEnumerable<Relationship>> GetByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Get relationships involving a specific concept
    /// </summary>
    Task<IEnumerable<Relationship>> GetByConceptIdAsync(int conceptId);

    /// <summary>
    /// Check if a relationship can be created (validation)
    /// </summary>
    Task<bool> CanCreateAsync(int sourceId, int targetId, string relationType);
}
