namespace Eidos.Models
{
    /// <summary>
    /// Metadata about a relationship that was collapsed into a concept group.
    /// Used to track and restore relationships when groups are expanded.
    /// </summary>
    public class CollapsedRelationshipInfo
    {
        /// <summary>
        /// The ID of the relationship in the database
        /// </summary>
        public int RelationshipId { get; set; }

        /// <summary>
        /// The source concept ID (original from concept)
        /// </summary>
        public int FromConceptId { get; set; }

        /// <summary>
        /// The target concept ID (original to concept)
        /// </summary>
        public int ToConceptId { get; set; }

        /// <summary>
        /// Whether the source concept is a grouped child (hidden)
        /// </summary>
        public bool IsFromGroupedChild { get; set; }

        /// <summary>
        /// Whether the target concept is a grouped child (hidden)
        /// </summary>
        public bool IsToGroupedChild { get; set; }

        /// <summary>
        /// The external concept ID (the one NOT in the group) if applicable
        /// Null if both concepts are in the group
        /// </summary>
        public int? ExternalConceptId { get; set; }

        /// <summary>
        /// The relationship type (e.g., "is-a", "part-of", "related-to")
        /// </summary>
        public string RelationshipType { get; set; } = string.Empty;

        /// <summary>
        /// Whether this edge should be hidden (internal to group) or rerouted (external connection)
        /// </summary>
        public bool ShouldBeRerouted => ExternalConceptId.HasValue;
    }
}
