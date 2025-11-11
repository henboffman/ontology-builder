using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Eidos.Models;
using Eidos.Models.DTOs;
using Eidos.Services.Interfaces;
using Eidos.Hubs;
using System.Security.Claims;

namespace Eidos.Endpoints;

[ApiController]
[Route("api/ontologies/{ontologyId}/comments")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly IEntityCommentService _commentService;
    private readonly IHubContext<CommentHub> _commentHub;
    private readonly ILogger<CommentController> _logger;

    public CommentController(
        IEntityCommentService commentService,
        IHubContext<CommentHub> commentHub,
        ILogger<CommentController> logger)
    {
        _commentService = commentService;
        _commentHub = commentHub;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    /// <summary>
    /// Get all comments for a specific entity
    /// </summary>
    [HttpGet("{entityType}/{entityId}")]
    public async Task<ActionResult<List<CommentResponse>>> GetComments(
        int ontologyId,
        string entityType,
        int entityId)
    {
        try
        {
            var userId = GetUserId();
            var comments = await _commentService.GetTopLevelCommentsForEntityAsync(ontologyId, entityType, entityId);

            var responses = new List<CommentResponse>();
            foreach (var comment in comments)
            {
                var canEdit = await _commentService.CanEditCommentAsync(comment.Id, userId);
                var canDelete = await _commentService.CanDeleteCommentAsync(comment.Id, userId);
                responses.Add(CommentResponse.FromEntity(comment, canEdit, canDelete));
            }

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for {EntityType} {EntityId} in ontology {OntologyId}",
                entityType, entityId, ontologyId);
            return StatusCode(500, "An error occurred while retrieving comments");
        }
    }

    /// <summary>
    /// Add a new comment
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CommentResponse>> AddComment(
        int ontologyId,
        [FromBody] CreateCommentRequest request)
    {
        try
        {
            var userId = GetUserId();

            if (!await _commentService.CanAddCommentAsync(ontologyId, userId))
            {
                return Forbid();
            }

            var comment = await _commentService.AddCommentAsync(
                ontologyId,
                request.EntityType,
                request.EntityId,
                userId,
                request.Text,
                request.ParentCommentId);

            var canEdit = await _commentService.CanEditCommentAsync(comment.Id, userId);
            var canDelete = await _commentService.CanDeleteCommentAsync(comment.Id, userId);
            var response = CommentResponse.FromEntity(comment, canEdit, canDelete);

            // Notify via SignalR
            await _commentHub.Clients.Group($"ontology-{ontologyId}")
                .SendAsync("CommentAdded", response);

            _logger.LogInformation("User {UserId} added comment {CommentId} to {EntityType} {EntityId}",
                userId, comment.Id, request.EntityType, request.EntityId);

            return CreatedAtAction(nameof(GetComment), new { ontologyId, commentId = comment.Id }, response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to ontology {OntologyId}", ontologyId);
            return StatusCode(500, "An error occurred while adding the comment");
        }
    }

    /// <summary>
    /// Get a specific comment with replies
    /// </summary>
    [HttpGet("{commentId}")]
    public async Task<ActionResult<CommentResponse>> GetComment(int ontologyId, int commentId)
    {
        try
        {
            var userId = GetUserId();
            var comment = await _commentService.GetCommentWithRepliesAsync(commentId);

            if (comment == null)
            {
                return NotFound();
            }

            if (comment.OntologyId != ontologyId)
            {
                return BadRequest("Comment does not belong to this ontology");
            }

            var canEdit = await _commentService.CanEditCommentAsync(comment.Id, userId);
            var canDelete = await _commentService.CanDeleteCommentAsync(comment.Id, userId);

            return Ok(CommentResponse.FromEntity(comment, canEdit, canDelete));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment {CommentId}", commentId);
            return StatusCode(500, "An error occurred while retrieving the comment");
        }
    }

    /// <summary>
    /// Update a comment's text
    /// </summary>
    [HttpPut("{commentId}")]
    public async Task<ActionResult<CommentResponse>> UpdateComment(
        int ontologyId,
        int commentId,
        [FromBody] UpdateCommentRequest request)
    {
        try
        {
            var userId = GetUserId();

            if (!await _commentService.CanEditCommentAsync(commentId, userId))
            {
                return Forbid();
            }

            var comment = await _commentService.UpdateCommentAsync(commentId, userId, request.Text);

            var canEdit = await _commentService.CanEditCommentAsync(comment.Id, userId);
            var canDelete = await _commentService.CanDeleteCommentAsync(comment.Id, userId);
            var response = CommentResponse.FromEntity(comment, canEdit, canDelete);

            // Notify via SignalR
            await _commentHub.Clients.Group($"ontology-{ontologyId}")
                .SendAsync("CommentUpdated", response);

            _logger.LogInformation("User {UserId} updated comment {CommentId}", userId, commentId);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment {CommentId}", commentId);
            return StatusCode(500, "An error occurred while updating the comment");
        }
    }

    /// <summary>
    /// Delete a comment
    /// </summary>
    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(int ontologyId, int commentId)
    {
        try
        {
            var userId = GetUserId();

            if (!await _commentService.CanDeleteCommentAsync(commentId, userId))
            {
                return Forbid();
            }

            await _commentService.DeleteCommentAsync(commentId, userId);

            // Notify via SignalR
            await _commentHub.Clients.Group($"ontology-{ontologyId}")
                .SendAsync("CommentDeleted", commentId);

            _logger.LogInformation("User {UserId} deleted comment {CommentId}", userId, commentId);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            return StatusCode(500, "An error occurred while deleting the comment");
        }
    }

    /// <summary>
    /// Resolve a comment thread
    /// </summary>
    [HttpPost("{commentId}/resolve")]
    public async Task<IActionResult> ResolveThread(int ontologyId, int commentId)
    {
        try
        {
            var userId = GetUserId();
            await _commentService.ResolveCommentThreadAsync(commentId, userId);

            // Notify via SignalR
            await _commentHub.Clients.Group($"ontology-{ontologyId}")
                .SendAsync("ThreadResolved", commentId);

            _logger.LogInformation("User {UserId} resolved thread {CommentId}", userId, commentId);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving thread {CommentId}", commentId);
            return StatusCode(500, "An error occurred while resolving the thread");
        }
    }

    /// <summary>
    /// Unresolve a comment thread
    /// </summary>
    [HttpPost("{commentId}/unresolve")]
    public async Task<IActionResult> UnresolveThread(int ontologyId, int commentId)
    {
        try
        {
            var userId = GetUserId();
            await _commentService.UnresolveCommentThreadAsync(commentId, userId);

            // Notify via SignalR
            await _commentHub.Clients.Group($"ontology-{ontologyId}")
                .SendAsync("ThreadUnresolved", commentId);

            _logger.LogInformation("User {UserId} unresolved thread {CommentId}", userId, commentId);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unresolving thread {CommentId}", commentId);
            return StatusCode(500, "An error occurred while unresolving the thread");
        }
    }

    /// <summary>
    /// Get all unresolved threads for an ontology
    /// </summary>
    [HttpGet("unresolved")]
    public async Task<ActionResult<List<CommentResponse>>> GetUnresolvedThreads(int ontologyId)
    {
        try
        {
            var userId = GetUserId();
            var threads = await _commentService.GetUnresolvedThreadsAsync(ontologyId);

            var responses = new List<CommentResponse>();
            foreach (var thread in threads)
            {
                var canEdit = await _commentService.CanEditCommentAsync(thread.Id, userId);
                var canDelete = await _commentService.CanDeleteCommentAsync(thread.Id, userId);
                responses.Add(CommentResponse.FromEntity(thread, canEdit, canDelete));
            }

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unresolved threads for ontology {OntologyId}", ontologyId);
            return StatusCode(500, "An error occurred while retrieving unresolved threads");
        }
    }

    /// <summary>
    /// Get unread mentions for the current user
    /// </summary>
    [HttpGet("~/api/comments/mentions/unread")]
    public async Task<ActionResult<List<CommentMentionResponse>>> GetUnreadMentions()
    {
        try
        {
            var userId = GetUserId();
            var mentions = await _commentService.GetUnreadMentionsAsync(userId);

            var responses = mentions.Select(m => new CommentMentionResponse
            {
                Id = m.Id,
                CommentId = m.CommentId,
                Comment = CommentResponse.FromEntity(m.Comment!, false, false),
                HasViewed = m.HasViewed,
                CreatedAt = m.CreatedAt,
                ViewedAt = m.ViewedAt
            }).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread mentions for user");
            return StatusCode(500, "An error occurred while retrieving mentions");
        }
    }

    /// <summary>
    /// Mark a mention as viewed
    /// </summary>
    [HttpPost("~/api/comments/mentions/{mentionId}/mark-viewed")]
    public async Task<IActionResult> MarkMentionViewed(int mentionId)
    {
        try
        {
            await _commentService.MarkMentionAsViewedAsync(mentionId);
            _logger.LogInformation("Mention {MentionId} marked as viewed", mentionId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking mention {MentionId} as viewed", mentionId);
            return StatusCode(500, "An error occurred while marking the mention as viewed");
        }
    }
}
