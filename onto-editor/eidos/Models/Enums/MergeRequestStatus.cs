namespace Eidos.Models.Enums;

/// <summary>
/// Status of a merge request in the approval workflow.
/// </summary>
public enum MergeRequestStatus
{
    /// <summary>
    /// Draft state - changes are being made but not yet submitted for review
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Pending review - submitted and waiting for reviewer
    /// </summary>
    PendingReview = 1,

    /// <summary>
    /// Approved - reviewer approved but not yet merged
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Rejected - reviewer rejected the changes
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Merged - changes have been applied to the ontology
    /// </summary>
    Merged = 4,

    /// <summary>
    /// Closed - manually closed without merging
    /// </summary>
    Closed = 5,

    /// <summary>
    /// Needs changes - reviewer requested modifications
    /// </summary>
    ChangesRequested = 6
}
