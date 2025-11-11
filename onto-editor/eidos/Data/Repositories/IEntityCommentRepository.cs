using Eidos.Models;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository interface for entity comment operations
/// </summary>
public interface IEntityCommentRepository : IRepository<EntityComment>
{
    /// <summary>
    /// Gets all comments for a specific entity with mentions and user details
    /// </summary>
    Task<IEnumerable<EntityComment>> GetByEntityAsync(int ontologyId, string entityType, int entityId);

    /// <summary>
    /// Gets all top-level comments (no parent) for a specific entity
    /// </summary>
    Task<IEnumerable<EntityComment>> GetTopLevelCommentsByEntityAsync(int ontologyId, string entityType, int entityId);

    /// <summary>
    /// Gets a comment with all its replies (threaded)
    /// </summary>
    Task<EntityComment?> GetWithRepliesAsync(int commentId);

    /// <summary>
    /// Gets all comments by a specific user in an ontology
    /// </summary>
    Task<IEnumerable<EntityComment>> GetByUserIdAsync(string userId, int ontologyId);

    /// <summary>
    /// Gets all unresolved comment threads for an ontology
    /// </summary>
    Task<IEnumerable<EntityComment>> GetUnresolvedThreadsByOntologyAsync(int ontologyId);

    /// <summary>
    /// Adds a comment mention
    /// </summary>
    Task<CommentMention> AddMentionAsync(CommentMention mention);

    /// <summary>
    /// Gets unread mentions for a specific user
    /// </summary>
    Task<IEnumerable<CommentMention>> GetUnreadMentionsAsync(string userId);

    /// <summary>
    /// Marks a mention as viewed
    /// </summary>
    Task MarkMentionAsViewedAsync(int mentionId);

    /// <summary>
    /// Gets or creates an EntityCommentCount record for an entity
    /// </summary>
    Task<EntityCommentCount> GetOrCreateCommentCountAsync(int ontologyId, string entityType, int entityId);

    /// <summary>
    /// Increments the comment count for an entity
    /// </summary>
    Task IncrementCommentCountAsync(int ontologyId, string entityType, int entityId, bool isTopLevel);

    /// <summary>
    /// Decrements the comment count for an entity
    /// </summary>
    Task DecrementCommentCountAsync(int ontologyId, string entityType, int entityId, bool isTopLevel);

    /// <summary>
    /// Updates unresolved thread count when a thread is resolved/unresolved
    /// </summary>
    Task UpdateUnresolvedThreadCountAsync(int ontologyId, string entityType, int entityId, bool isResolved);

    /// <summary>
    /// Gets comment counts for multiple entities (for badge display)
    /// </summary>
    Task<IEnumerable<EntityCommentCount>> GetCommentCountsAsync(int ontologyId, string entityType, IEnumerable<int> entityIds);
}
