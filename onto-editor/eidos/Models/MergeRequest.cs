using System.ComponentModel.DataAnnotations;
using Eidos.Models.Enums;

namespace Eidos.Models;

/// <summary>
/// Represents a merge request for batch changes to an ontology when approval mode is enabled.
/// </summary>
public class MergeRequest
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The ontology this merge request targets
    /// </summary>
    public int OntologyId { get; set; }
    public Ontology Ontology { get; set; } = null!;

    /// <summary>
    /// Title/summary of the merge request
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the changes
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Current status of the merge request
    /// </summary>
    public MergeRequestStatus Status { get; set; } = MergeRequestStatus.Draft;

    /// <summary>
    /// User who created this merge request
    /// </summary>
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    /// <summary>
    /// When the merge request was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the merge request was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User assigned to review this merge request (optional)
    /// </summary>
    public string? AssignedReviewerUserId { get; set; }
    public ApplicationUser? AssignedReviewer { get; set; }

    /// <summary>
    /// When the merge request was submitted for review
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// User who reviewed/approved/rejected this merge request
    /// </summary>
    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }

    /// <summary>
    /// When the merge request was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Comments from the reviewer
    /// </summary>
    public string? ReviewComments { get; set; }

    /// <summary>
    /// When the merge request was merged (approved and applied)
    /// </summary>
    public DateTime? MergedAt { get; set; }

    /// <summary>
    /// Snapshot of ontology state when MR was created (JSON)
    /// Used for conflict detection
    /// </summary>
    public string? BaseSnapshotJson { get; set; }

    /// <summary>
    /// Summary statistics of changes
    /// </summary>
    public int ConceptsAdded { get; set; }
    public int ConceptsModified { get; set; }
    public int ConceptsDeleted { get; set; }
    public int RelationshipsAdded { get; set; }
    public int RelationshipsModified { get; set; }
    public int RelationshipsDeleted { get; set; }
    public int IndividualsAdded { get; set; }
    public int IndividualsModified { get; set; }
    public int IndividualsDeleted { get; set; }

    /// <summary>
    /// Whether there are conflicts with the current ontology state
    /// </summary>
    public bool HasConflicts { get; set; }

    /// <summary>
    /// Priority level of this merge request
    /// </summary>
    public MergeRequestPriority Priority { get; set; } = MergeRequestPriority.Normal;

    /// <summary>
    /// Collection of individual changes in this merge request
    /// </summary>
    public ICollection<MergeRequestChange> Changes { get; set; } = new List<MergeRequestChange>();

    /// <summary>
    /// Comments on this merge request
    /// </summary>
    public ICollection<MergeRequestComment> Comments { get; set; } = new List<MergeRequestComment>();

    /// <summary>
    /// Gets total number of changes
    /// </summary>
    public int TotalChanges =>
        ConceptsAdded + ConceptsModified + ConceptsDeleted +
        RelationshipsAdded + RelationshipsModified + RelationshipsDeleted +
        IndividualsAdded + IndividualsModified + IndividualsDeleted;

    /// <summary>
    /// Checks if the merge request can be edited
    /// </summary>
    public bool CanEdit => Status == MergeRequestStatus.Draft;

    /// <summary>
    /// Checks if the merge request can be submitted
    /// </summary>
    public bool CanSubmit => Status == MergeRequestStatus.Draft && TotalChanges > 0;

    /// <summary>
    /// Checks if the merge request can be reviewed
    /// </summary>
    public bool CanReview => Status == MergeRequestStatus.PendingReview;

    /// <summary>
    /// Checks if the merge request is closed
    /// </summary>
    public bool IsClosed => Status == MergeRequestStatus.Merged ||
                            Status == MergeRequestStatus.Rejected ||
                            Status == MergeRequestStatus.Closed;
}
