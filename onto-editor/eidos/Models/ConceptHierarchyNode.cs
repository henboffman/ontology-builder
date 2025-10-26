namespace Eidos.Models;

/// <summary>
/// Represents a node in the concept hierarchy tree
/// Used for displaying hierarchical relationships in a tree view
/// </summary>
public class ConceptHierarchyNode
{
    /// <summary>
    /// The concept represented by this node
    /// </summary>
    public Concept Concept { get; set; } = null!;

    /// <summary>
    /// Child concepts (subconcepts) of this concept
    /// </summary>
    public List<ConceptHierarchyNode> Children { get; set; } = new();

    /// <summary>
    /// Parent concept ID (null for root concepts)
    /// </summary>
    public int? ParentConceptId { get; set; }

    /// <summary>
    /// Depth level in the hierarchy (0 for root)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Whether this node is currently expanded in the UI
    /// </summary>
    public bool IsExpanded { get; set; } = false;

    /// <summary>
    /// Whether this node has children (for lazy loading)
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Count of total descendants (recursive)
    /// </summary>
    public int DescendantCount { get; set; }
}
