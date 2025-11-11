using System.ComponentModel.DataAnnotations;

namespace Eidos.Models.DTOs;

/// <summary>
/// DTO for creating or updating a comment
/// </summary>
public class CreateCommentRequest
{
    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    public int EntityId { get; set; }

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;

    public int? ParentCommentId { get; set; }
}

/// <summary>
/// DTO for updating a comment's text
/// </summary>
public class UpdateCommentRequest
{
    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// DTO for comment response
/// </summary>
public class CommentResponse
{
    public int Id { get; set; }
    public int OntologyId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int? ParentCommentId { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public List<string> MentionedUserIds { get; set; } = new();
    public List<CommentResponse> Replies { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public static CommentResponse FromEntity(EntityComment comment, bool canEdit, bool canDelete)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            OntologyId = comment.OntologyId,
            EntityType = comment.EntityType,
            EntityId = comment.EntityId,
            UserId = comment.UserId,
            UserName = comment.User?.DisplayName ?? "Unknown",
            UserEmail = comment.User?.Email ?? "",
            Text = comment.Text,
            ParentCommentId = comment.ParentCommentId,
            IsResolved = comment.IsResolved,
            CreatedAt = comment.CreatedAt,
            EditedAt = comment.EditedAt,
            MentionedUserIds = comment.Mentions?.Select(m => m.MentionedUserId).ToList() ?? new(),
            Replies = comment.Replies?.Select(r => FromEntity(r, canEdit, canDelete)).ToList() ?? new(),
            CanEdit = canEdit,
            CanDelete = canDelete
        };
    }
}

/// <summary>
/// DTO for comment mention notification
/// </summary>
public class CommentMentionResponse
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public CommentResponse Comment { get; set; } = null!;
    public bool HasViewed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ViewedAt { get; set; }
}
