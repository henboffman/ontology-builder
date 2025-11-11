using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services.Interfaces;

namespace Eidos.Services;

/// <summary>
/// Service for managing entity comments with permissions, @mentions, and denormalized counts
/// </summary>
public class EntityCommentService : IEntityCommentService
{
    private readonly IEntityCommentRepository _repository;
    private readonly OntologyPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<EntityCommentService> _logger;

    public EntityCommentService(
        IEntityCommentRepository repository,
        OntologyPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        ILogger<EntityCommentService> logger)
    {
        _repository = repository;
        _permissionService = permissionService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<EntityComment> AddCommentAsync(int ontologyId, string entityType, int entityId, string userId, string text, int? parentCommentId = null)
    {
        // Permission check
        if (!await CanAddCommentAsync(ontologyId, userId))
        {
            _logger.LogWarning("User {UserId} attempted to add comment to ontology {OntologyId} without permission", userId, ontologyId);
            throw new UnauthorizedAccessException("You do not have permission to comment on this ontology");
        }

        // Validate parent comment exists and belongs to same entity
        if (parentCommentId.HasValue)
        {
            var parentComment = await _repository.GetByIdAsync(parentCommentId.Value);
            if (parentComment == null)
            {
                throw new ArgumentException("Parent comment not found", nameof(parentCommentId));
            }

            if (parentComment.OntologyId != ontologyId ||
                parentComment.EntityType != entityType ||
                parentComment.EntityId != entityId)
            {
                throw new ArgumentException("Parent comment does not belong to the same entity", nameof(parentCommentId));
            }
        }

        // Create comment
        var comment = new EntityComment
        {
            OntologyId = ontologyId,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            Text = text,
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        var savedComment = await _repository.AddAsync(comment);

        // Extract and save mentions
        var mentions = MentionParser.ExtractMentions(text);
        if (mentions.Any())
        {
            await ProcessMentionsAsync(savedComment.Id, mentions);
        }

        // Update comment count
        bool isTopLevel = !parentCommentId.HasValue;
        await _repository.IncrementCommentCountAsync(ontologyId, entityType, entityId, isTopLevel);

        _logger.LogInformation("User {UserId} added comment {CommentId} to {EntityType} {EntityId} in ontology {OntologyId}",
            userId, savedComment.Id, entityType, entityId, ontologyId);

        return savedComment;
    }

    public async Task<EntityComment> UpdateCommentAsync(int commentId, string userId, string newText)
    {
        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null)
        {
            throw new ArgumentException("Comment not found", nameof(commentId));
        }

        if (!await CanEditCommentAsync(commentId, userId))
        {
            _logger.LogWarning("User {UserId} attempted to edit comment {CommentId} without permission", userId, commentId);
            throw new UnauthorizedAccessException("You can only edit your own comments");
        }

        comment.Text = newText;
        comment.EditedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(comment);

        // Re-process mentions (remove old, add new)
        // For simplicity, we could delete all old mentions and re-create them
        // In Phase 2, we'll handle this with SignalR updates

        _logger.LogInformation("User {UserId} updated comment {CommentId}", userId, commentId);

        return comment;
    }

    public async Task DeleteCommentAsync(int commentId, string userId)
    {
        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null)
        {
            throw new ArgumentException("Comment not found", nameof(commentId));
        }

        if (!await CanDeleteCommentAsync(commentId, userId))
        {
            _logger.LogWarning("User {UserId} attempted to delete comment {CommentId} without permission", userId, commentId);
            throw new UnauthorizedAccessException("You do not have permission to delete this comment");
        }

        bool isTopLevel = !comment.ParentCommentId.HasValue;

        await _repository.DeleteAsync(commentId);

        // Update comment count
        await _repository.DecrementCommentCountAsync(comment.OntologyId, comment.EntityType, comment.EntityId, isTopLevel);

        _logger.LogInformation("User {UserId} deleted comment {CommentId}", userId, commentId);
    }

    public async Task<IEnumerable<EntityComment>> GetCommentsForEntityAsync(int ontologyId, string entityType, int entityId)
    {
        return await _repository.GetByEntityAsync(ontologyId, entityType, entityId);
    }

    public async Task<IEnumerable<EntityComment>> GetTopLevelCommentsForEntityAsync(int ontologyId, string entityType, int entityId)
    {
        return await _repository.GetTopLevelCommentsByEntityAsync(ontologyId, entityType, entityId);
    }

    public async Task<EntityComment?> GetCommentWithRepliesAsync(int commentId)
    {
        return await _repository.GetWithRepliesAsync(commentId);
    }

    public async Task ResolveCommentThreadAsync(int commentId, string userId)
    {
        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null)
        {
            throw new ArgumentException("Comment not found", nameof(commentId));
        }

        // Only top-level comments can be resolved
        if (comment.ParentCommentId.HasValue)
        {
            throw new ArgumentException("Only top-level comments can be resolved", nameof(commentId));
        }

        // Check if user can edit (permission check)
        if (!await CanAddCommentAsync(comment.OntologyId, userId))
        {
            throw new UnauthorizedAccessException("You do not have permission to resolve comments in this ontology");
        }

        comment.IsResolved = true;
        await _repository.UpdateAsync(comment);

        // Update unresolved count
        await _repository.UpdateUnresolvedThreadCountAsync(comment.OntologyId, comment.EntityType, comment.EntityId, true);

        _logger.LogInformation("User {UserId} resolved comment thread {CommentId}", userId, commentId);
    }

    public async Task UnresolveCommentThreadAsync(int commentId, string userId)
    {
        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null)
        {
            throw new ArgumentException("Comment not found", nameof(commentId));
        }

        if (comment.ParentCommentId.HasValue)
        {
            throw new ArgumentException("Only top-level comments can be unresolved", nameof(commentId));
        }

        if (!await CanAddCommentAsync(comment.OntologyId, userId))
        {
            throw new UnauthorizedAccessException("You do not have permission to unresolve comments in this ontology");
        }

        comment.IsResolved = false;
        await _repository.UpdateAsync(comment);

        await _repository.UpdateUnresolvedThreadCountAsync(comment.OntologyId, comment.EntityType, comment.EntityId, false);

        _logger.LogInformation("User {UserId} unresolved comment thread {CommentId}", userId, commentId);
    }

    public async Task<IEnumerable<EntityComment>> GetUnresolvedThreadsAsync(int ontologyId)
    {
        return await _repository.GetUnresolvedThreadsByOntologyAsync(ontologyId);
    }

    public async Task<IEnumerable<CommentMention>> GetUnreadMentionsAsync(string userId)
    {
        return await _repository.GetUnreadMentionsAsync(userId);
    }

    public async Task MarkMentionAsViewedAsync(int mentionId)
    {
        await _repository.MarkMentionAsViewedAsync(mentionId);
    }

    public async Task<Dictionary<int, EntityCommentCount>> GetCommentCountsForEntitiesAsync(int ontologyId, string entityType, IEnumerable<int> entityIds)
    {
        var counts = await _repository.GetCommentCountsAsync(ontologyId, entityType, entityIds);
        return counts.ToDictionary(c => c.EntityId, c => c);
    }

    public async Task<bool> CanAddCommentAsync(int ontologyId, string userId)
    {
        // Users can comment if they can view the ontology
        return await _permissionService.CanViewAsync(ontologyId, userId);
    }

    public async Task<bool> CanEditCommentAsync(int commentId, string userId)
    {
        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null)
        {
            return false;
        }

        // Only the comment author can edit
        return comment.UserId == userId;
    }

    public async Task<bool> CanDeleteCommentAsync(int commentId, string userId)
    {
        var comment = await _repository.GetByIdAsync(commentId);
        if (comment == null)
        {
            return false;
        }

        // Comment author can delete
        if (comment.UserId == userId)
        {
            return true;
        }

        // Ontology owner/admin can delete any comment
        return await _permissionService.CanManageAsync(comment.OntologyId, userId);
    }

    /// <summary>
    /// Processes @mentions in a comment by resolving them to user IDs and creating CommentMention records
    /// </summary>
    private async Task ProcessMentionsAsync(int commentId, List<string> mentions)
    {
        foreach (var mention in mentions)
        {
            // Try to find user by email
            var user = await _userManager.FindByEmailAsync(mention);

            // If not found by email, try by username (if different from email)
            if (user == null && !mention.Contains("@"))
            {
                user = await _userManager.FindByNameAsync(mention);
            }

            if (user != null)
            {
                var commentMention = new CommentMention
                {
                    CommentId = commentId,
                    MentionedUserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    HasViewed = false
                };

                await _repository.AddMentionAsync(commentMention);
                _logger.LogDebug("Created mention for user {MentionedUserId} in comment {CommentId}", user.Id, commentId);
            }
            else
            {
                _logger.LogWarning("Could not resolve mention '{Mention}' to a user in comment {CommentId}", mention, commentId);
            }
        }
    }
}
