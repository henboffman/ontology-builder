using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing concept operations
/// Focused on Single Responsibility Principle
/// </summary>
public interface IConceptService
{
    /// <summary>
    /// Create a new concept
    /// </summary>
    Task<Concept> CreateAsync(Concept concept, bool recordUndo = true);

    /// <summary>
    /// Update an existing concept
    /// </summary>
    Task<Concept> UpdateAsync(Concept concept, bool recordUndo = true);

    /// <summary>
    /// Delete a concept by ID
    /// </summary>
    Task DeleteAsync(int id, bool recordUndo = true);

    /// <summary>
    /// Get a concept by ID
    /// </summary>
    Task<Concept?> GetByIdAsync(int id);

    /// <summary>
    /// Get all concepts for an ontology
    /// </summary>
    Task<IEnumerable<Concept>> GetByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Search concepts by query
    /// </summary>
    Task<IEnumerable<Concept>> SearchAsync(string query);

    /// <summary>
    /// Get the concept hierarchy for an ontology
    /// Returns root concepts with their children recursively
    /// </summary>
    Task<IEnumerable<ConceptHierarchyNode>> GetHierarchyAsync(int ontologyId);

    /// <summary>
    /// Get all parent concepts for a given concept (following subclass-of relationships)
    /// </summary>
    Task<IEnumerable<Concept>> GetParentConceptsAsync(int conceptId);

    /// <summary>
    /// Get all child concepts for a given concept (following subclass-of relationships)
    /// </summary>
    Task<IEnumerable<Concept>> GetChildConceptsAsync(int conceptId);

    /// <summary>
    /// Updates the position of a concept node in the graph
    /// </summary>
    Task UpdatePositionAsync(int conceptId, double x, double y);

    /// <summary>
    /// Batch update positions for multiple concepts
    /// </summary>
    Task UpdatePositionsBatchAsync(Dictionary<int, (double X, double Y)> positions);

    /// <summary>
    /// Quick update for concept name - for inline editing
    /// </summary>
    Task<bool> UpdateConceptNameAsync(int conceptId, string newName, string userId);
}
