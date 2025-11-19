using System;
using System.ComponentModel.DataAnnotations;

namespace Eidos.Models
{
    /// <summary>
    /// Stores user-specific state for shared ontologies (pin, hide, dismiss)
    /// Separate from access permissions to allow independent state management
    /// </summary>
    public class SharedOntologyUserState
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public int OntologyId { get; set; }
        public Ontology Ontology { get; set; } = null!;

        /// <summary>
        /// Whether user has pinned this shared ontology for quick access
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Whether user has hidden this shared ontology from their list
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Whether user has dismissed this shared ontology (soft delete from shared list)
        /// </summary>
        public bool IsDismissed { get; set; }

        /// <summary>
        /// When the ontology was pinned
        /// </summary>
        public DateTime? PinnedAt { get; set; }

        /// <summary>
        /// When the ontology was hidden
        /// </summary>
        public DateTime? HiddenAt { get; set; }

        /// <summary>
        /// When the ontology was dismissed
        /// </summary>
        public DateTime? DismissedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
