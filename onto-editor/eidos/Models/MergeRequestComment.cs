using System.ComponentModel.DataAnnotations;

namespace Eidos.Models;

/// <summary>
/// Represents a comment on a merge request for discussion and feedback.
/// </summary>
public class MergeRequestComment
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The merge request this comment belongs to
    /// </summary>
    public int MergeRequestId { get; set; }
    public MergeRequest MergeRequest { get; set; } = null!;

    /// <summary>
    /// User who wrote the comment
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// The comment text
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// When the comment was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the comment was last edited
    /// </summary>
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// Whether this is a system-generated comment (e.g., "Status changed to Approved")
    /// </summary>
    public bool IsSystemComment { get; set; }

    /// <summary>
    /// Optional reference to a specific change if commenting on a particular change
    /// </summary>
    public int? MergeRequestChangeId { get; set; }
    public MergeRequestChange? MergeRequestChange { get; set; }
}
