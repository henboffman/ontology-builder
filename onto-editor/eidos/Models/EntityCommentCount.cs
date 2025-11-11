using System.ComponentModel.DataAnnotations;

namespace Eidos.Models
{
    /// <summary>
    /// Denormalized table for fast comment count lookups on entities.
    /// Updated transactionally whenever comments are added/removed.
    /// Enables efficient display of comment badges without expensive COUNT queries.
    /// </summary>
    public class EntityCommentCount
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ontology this entity belongs to
        /// </summary>
        [Required]
        public int OntologyId { get; set; }

        /// <summary>
        /// Type of entity: "Concept", "Relationship", or "Individual"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the entity (Concept.Id, Relationship.Id, or Individual.Id)
        /// </summary>
        [Required]
        public int EntityId { get; set; }

        /// <summary>
        /// Total number of comments (including replies) on this entity
        /// </summary>
        [Required]
        public int TotalComments { get; set; }

        /// <summary>
        /// Number of unresolved top-level comment threads
        /// </summary>
        [Required]
        public int UnresolvedThreads { get; set; }

        /// <summary>
        /// Navigation property to ontology
        /// </summary>
        public Ontology? Ontology { get; set; }
    }
}
