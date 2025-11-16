using System;
using System.ComponentModel.DataAnnotations;

namespace Eidos.Models
{
    /// <summary>
    /// Represents a visual grouping of concepts in the graph view.
    /// Allows collapsing multiple concepts into a single visual container (iOS folder-style).
    /// This is a UI-only feature and does not affect the ontology structure.
    /// </summary>
    public class ConceptGroup
    {
        public int Id { get; set; }

        /// <summary>
        /// The ontology this group belongs to
        /// </summary>
        public int OntologyId { get; set; }
        public Ontology Ontology { get; set; } = null!;

        /// <summary>
        /// The user who created this group (groups are per-user)
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// The "container" concept that visually represents the group.
        /// When collapsed, only this concept is shown with a stacked visual effect.
        /// </summary>
        public int ParentConceptId { get; set; }
        public Concept ParentConcept { get; set; } = null!;

        /// <summary>
        /// JSON array of concept IDs that are grouped inside the parent.
        /// Format: [123, 456, 789]
        /// These are stored as a flat list (not nested hierarchy).
        /// </summary>
        [Required]
        public string ChildConceptIds { get; set; } = "[]";

        /// <summary>
        /// Whether the group is currently collapsed (true) or expanded (false)
        /// </summary>
        public bool IsCollapsed { get; set; } = true;

        /// <summary>
        /// JSON array of relationship metadata for edges that were collapsed into this group.
        /// Format: [{"relationshipId": 123, "fromConceptId": 456, "toConceptId": 789, "isGroupedChild": true}]
        /// This allows us to restore original edges when the group is expanded.
        /// </summary>
        public string? CollapsedRelationships { get; set; }

        /// <summary>
        /// Optional custom name for the group (defaults to parent concept name)
        /// </summary>
        [StringLength(200)]
        public string? GroupName { get; set; }

        /// <summary>
        /// Visual position of the collapsed group node
        /// </summary>
        public double? CollapsedPositionX { get; set; }
        public double? CollapsedPositionY { get; set; }

        /// <summary>
        /// Maximum nesting depth allowed (default 5)
        /// </summary>
        public int MaxDepth { get; set; } = 5;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
