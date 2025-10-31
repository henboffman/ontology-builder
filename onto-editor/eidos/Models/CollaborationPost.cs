using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Eidos.Models
{
    /// <summary>
    /// Represents a "Looking for Group" bulletin board post where users can find collaborators
    /// </summary>
    public class CollaborationPost
    {
        public int Id { get; set; }

        /// <summary>
        /// User who created this collaboration post
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Optional: If this is for an existing ontology
        /// </summary>
        public int? OntologyId { get; set; }
        public Ontology? Ontology { get; set; }

        /// <summary>
        /// The user group created for this collaboration project
        /// Members of this group will have access to the ontology
        /// </summary>
        public int? CollaborationProjectGroupId { get; set; }
        public UserGroup? CollaborationProjectGroup { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The domain or topic area (e.g., "Healthcare", "Environmental Science", "Business")
        /// </summary>
        [StringLength(100)]
        public string? Domain { get; set; }

        /// <summary>
        /// Tags/keywords for filtering (comma-separated)
        /// </summary>
        [StringLength(500)]
        public string? Tags { get; set; }

        /// <summary>
        /// What kind of help is needed (e.g., "Co-designer", "Domain expert", "Technical contributor")
        /// </summary>
        [StringLength(500)]
        public string? LookingFor { get; set; }

        /// <summary>
        /// Expected time commitment (e.g., "Few hours", "Weekly", "Ongoing")
        /// </summary>
        [StringLength(100)]
        public string? TimeCommitment { get; set; }

        /// <summary>
        /// Skill level needed (e.g., "Beginner friendly", "Intermediate", "Advanced")
        /// </summary>
        [StringLength(50)]
        public string? SkillLevel { get; set; }

        /// <summary>
        /// Is this post still active/looking for collaborators?
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Number of views this post has received
        /// </summary>
        public int ViewCount { get; set; } = 0;

        /// <summary>
        /// Number of responses/interests received
        /// </summary>
        public int ResponseCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this post was last bumped/refreshed
        /// </summary>
        public DateTime? LastBumpedAt { get; set; }

        // Navigation properties
        public ICollection<CollaborationResponse> Responses { get; set; } = new List<CollaborationResponse>();
    }

    /// <summary>
    /// Represents a user's response/interest in a collaboration post
    /// </summary>
    public class CollaborationResponse
    {
        public int Id { get; set; }

        public int CollaborationPostId { get; set; }
        public CollaborationPost CollaborationPost { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Contact email or preferred contact method
        /// </summary>
        [StringLength(200)]
        public string? ContactInfo { get; set; }

        /// <summary>
        /// Status: Pending, Accepted, Declined, Collaborating
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
