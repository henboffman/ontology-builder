using System;
using System.ComponentModel.DataAnnotations;

namespace Eidos.Models
{
    /// <summary>
    /// Represents a user @mention within a comment.
    /// Enables notification and tracking of who was mentioned.
    /// </summary>
    public class CommentMention
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The comment containing this mention
        /// </summary>
        [Required]
        public int CommentId { get; set; }

        /// <summary>
        /// Navigation property to comment
        /// </summary>
        public EntityComment? Comment { get; set; }

        /// <summary>
        /// User who was mentioned
        /// </summary>
        [Required]
        public string MentionedUserId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to mentioned user
        /// </summary>
        public ApplicationUser? MentionedUser { get; set; }

        /// <summary>
        /// Whether the mentioned user has viewed this comment
        /// </summary>
        public bool HasViewed { get; set; }

        /// <summary>
        /// When the mention was created (same as comment creation)
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the mentioned user viewed the comment (null if not viewed)
        /// </summary>
        public DateTime? ViewedAt { get; set; }
    }
}
