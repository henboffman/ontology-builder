using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing entity comments with permissions, @mentions, and real-time notifications
/// </summary>
public interface IEntityCommentService
{
    /// <summary>
    /// Adds a comment to an entity
    /// </summary>
    Task<EntityComment> AddCommentAsync(int ontologyId, string entityType, int entityId, string userId, string text, int? parentCommentId = null);

    /// <summary>
    /// Updates an existing comment (only by the author)
    /// </summary>
    Task<EntityComment> UpdateCommentAsync(int commentId, string userId, string newText);

    /// <summary>
    /// Deletes a comment (only by the author or ontology admin)
    /// </summary>
    Task DeleteCommentAsync(int commentId, string userId);

    /// <summary>
    /// Gets all comments for a specific entity
    /// </summary>
    Task<IEnumerable<EntityComment>> GetCommentsForEntityAsync(int ontologyId, string entityType, int entityId);

    /// <summary>
    /// Gets all top-level comments for a specific entity
    /// </summary>
    Task<IEnumerable<EntityComment>> GetTopLevelCommentsForEntityAsync(int ontologyId, string entityType, int entityId);

    /// <summary>
    /// Gets a comment with all its replies
    /// </summary>
    Task<EntityComment?> GetCommentWithRepliesAsync(int commentId);

    /// <summary>
    /// Resolves a comment thread (marks as resolved)
    /// </summary>
    Task ResolveCommentThreadAsync(int commentId, string userId);

    /// <summary>
    /// Unresolves a comment thread
    /// </summary>
    Task UnresolveCommentThreadAsync(int commentId, string userId);

    /// <summary>
    /// Gets all unresolved threads for an ontology
    /// </summary>
    Task<IEnumerable<EntityComment>> GetUnresolvedThreadsAsync(int ontologyId);

    /// <summary>
    /// Gets unread mentions for a user
    /// </summary>
    Task<IEnumerable<CommentMention>> GetUnreadMentionsAsync(string userId);

    /// <summary>
    /// Marks a mention as viewed
    /// </summary>
    Task MarkMentionAsViewedAsync(int mentionId);

    /// <summary>
    /// Gets comment counts for multiple entities (for badge display)
    /// </summary>
    Task<Dictionary<int, EntityCommentCount>> GetCommentCountsForEntitiesAsync(int ontologyId, string entityType, IEnumerable<int> entityIds);

    /// <summary>
    /// Checks if a user can add a comment to an entity
    /// </summary>
    Task<bool> CanAddCommentAsync(int ontologyId, string userId);

    /// <summary>
    /// Checks if a user can edit a specific comment
    /// </summary>
    Task<bool> CanEditCommentAsync(int commentId, string userId);

    /// <summary>
    /// Checks if a user can delete a specific comment
    /// </summary>
    Task<bool> CanDeleteCommentAsync(int commentId, string userId);
}
