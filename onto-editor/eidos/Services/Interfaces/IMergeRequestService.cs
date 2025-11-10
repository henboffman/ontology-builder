using Eidos.Models;
using Eidos.Models.Enums;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service interface for merge request operations
/// </summary>
public interface IMergeRequestService
{
    /// <summary>
    /// Creates a new merge request
    /// </summary>
    Task<MergeRequest> CreateMergeRequestAsync(int ontologyId, string title, string? description, string createdByUserId, MergeRequestPriority priority = MergeRequestPriority.Normal);

    /// <summary>
    /// Gets a merge request by ID with all details
    /// </summary>
    Task<MergeRequest?> GetMergeRequestAsync(int id);

    /// <summary>
    /// Gets all merge requests for an ontology
    /// </summary>
    Task<IEnumerable<MergeRequest>> GetMergeRequestsForOntologyAsync(int ontologyId);

    /// <summary>
    /// Gets pending merge requests for an ontology
    /// </summary>
    Task<IEnumerable<MergeRequest>> GetPendingMergeRequestsAsync(int ontologyId);

    /// <summary>
    /// Gets merge requests created by a user
    /// </summary>
    Task<IEnumerable<MergeRequest>> GetUserMergeRequestsAsync(string userId);

    /// <summary>
    /// Gets merge requests assigned to a reviewer
    /// </summary>
    Task<IEnumerable<MergeRequest>> GetReviewerMergeRequestsAsync(string userId);

    /// <summary>
    /// Submits a merge request for review
    /// </summary>
    Task SubmitForReviewAsync(int mergeRequestId, string? reviewerUserId = null);

    /// <summary>
    /// Approves a merge request
    /// </summary>
    Task ApproveAsync(int mergeRequestId, string reviewerUserId, string? comments = null);

    /// <summary>
    /// Rejects a merge request
    /// </summary>
    Task RejectAsync(int mergeRequestId, string reviewerUserId, string comments);

    /// <summary>
    /// Requests changes on a merge request
    /// </summary>
    Task RequestChangesAsync(int mergeRequestId, string reviewerUserId, string comments);

    /// <summary>
    /// Merges an approved merge request into the ontology
    /// </summary>
    Task<bool> MergeAsync(int mergeRequestId, string userId);

    /// <summary>
    /// Closes a merge request without merging
    /// </summary>
    Task CloseAsync(int mergeRequestId, string userId);

    /// <summary>
    /// Adds a comment to a merge request
    /// </summary>
    Task<MergeRequestComment> AddCommentAsync(int mergeRequestId, string userId, string text, int? changeId = null);

    /// <summary>
    /// Adds a change to a merge request
    /// </summary>
    Task<MergeRequestChange> AddChangeAsync(MergeRequestChange change);

    /// <summary>
    /// Updates the change statistics on a merge request
    /// </summary>
    Task UpdateChangeStatisticsAsync(int mergeRequestId);

    /// <summary>
    /// Checks if there are conflicts with the current ontology state
    /// </summary>
    Task<bool> DetectConflictsAsync(int mergeRequestId);

    /// <summary>
    /// Gets the count of pending reviews for an ontology
    /// </summary>
    Task<int> GetPendingReviewCountAsync(int ontologyId);
}
