using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Eidos.Models
{
    /// <summary>
    /// Represents a comment attached to a specific entity (Concept, Relationship, or Individual)
    /// in an ontology. Supports threaded discussions via ParentCommentId.
    /// </summary>
    public class EntityComment
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ontology this comment belongs to (for efficient querying)
        /// </summary>
        [Required]
        public int OntologyId { get; set; }

        /// <summary>
        /// Type of entity being commented on: "Concept", "Relationship", or "Individual"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the entity being commented on (Concept.Id, Relationship.Id, or Individual.Id)
        /// </summary>
        [Required]
        public int EntityId { get; set; }

        /// <summary>
        /// User who created this comment
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to the user
        /// </summary>
        public ApplicationUser? User { get; set; }

        /// <summary>
        /// Comment text (supports Markdown)
        /// </summary>
        [Required]
        [MaxLength(5000)]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Parent comment ID for threaded discussions (null for top-level comments)
        /// </summary>
        public int? ParentCommentId { get; set; }

        /// <summary>
        /// Navigation property to parent comment
        /// </summary>
        public EntityComment? ParentComment { get; set; }

        /// <summary>
        /// Child comments (replies)
        /// </summary>
        public ICollection<EntityComment> Replies { get; set; } = new List<EntityComment>();

        /// <summary>
        /// Whether this comment thread has been resolved
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// When the comment was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the comment was last edited (null if never edited)
        /// </summary>
        public DateTime? EditedAt { get; set; }

        /// <summary>
        /// @Mentions in this comment
        /// </summary>
        public ICollection<CommentMention> Mentions { get; set; } = new List<CommentMention>();

        /// <summary>
        /// Navigation property to ontology
        /// </summary>
        public Ontology? Ontology { get; set; }
    }
}
