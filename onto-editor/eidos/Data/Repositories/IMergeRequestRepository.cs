using Eidos.Models;
using Eidos.Models.Enums;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository interface for merge request operations
/// </summary>
public interface IMergeRequestRepository : IRepository<MergeRequest>
{
    /// <summary>
    /// Gets a merge request with all its changes and comments
    /// </summary>
    Task<MergeRequest?> GetWithDetailsAsync(int id);

    /// <summary>
    /// Gets all merge requests for an ontology
    /// </summary>
    Task<IEnumerable<MergeRequest>> GetByOntologyIdAsync(int ontologyId);

    /// <summary>
    /// Gets merge requests by status for an ontology
    /// </summary>
    Task<IEnumerable<MergeRequest>> GetByOntologyIdAndStatusAsync(int ontologyId, MergeRequestStatus status);

    /// <summary>
    /// Gets merge requests created by a specific user
    /// </summary>
    Task<IEnumerable<MergeRequest>> GetByCreatedByUserIdAsync(string userId);

    /// <summary>
    /// Gets merge requests assigned to a specific reviewer
    /// </summary>
    Task<IEnumerable<MergeRequest>> GetByAssignedReviewerAsync(string userId);

    /// <summary>
    /// Gets pending review count for an ontology
    /// </summary>
    Task<int> GetPendingReviewCountAsync(int ontologyId);

    /// <summary>
    /// Adds a change to a merge request
    /// </summary>
    Task<MergeRequestChange> AddChangeAsync(MergeRequestChange change);

    /// <summary>
    /// Adds a comment to a merge request
    /// </summary>
    Task<MergeRequestComment> AddCommentAsync(MergeRequestComment comment);

    /// <summary>
    /// Gets all changes for a merge request
    /// </summary>
    Task<IEnumerable<MergeRequestChange>> GetChangesAsync(int mergeRequestId);

    /// <summary>
    /// Gets all comments for a merge request
    /// </summary>
    Task<IEnumerable<MergeRequestComment>> GetCommentsAsync(int mergeRequestId);

    /// <summary>
    /// Updates merge request status
    /// </summary>
    Task UpdateStatusAsync(int mergeRequestId, MergeRequestStatus status);
}
