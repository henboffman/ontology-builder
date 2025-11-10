namespace Eidos.Models.DTOs;

/// <summary>
/// Summary of changes made to an ontology since the user's last visit
/// Powers the "What's New" feature
/// </summary>
public class WhatsNewDto
{
    /// <summary>
    /// When the user last viewed this ontology
    /// </summary>
    public DateTime LastViewedAt { get; set; }

    /// <summary>
    /// Total number of changes since last visit
    /// </summary>
    public int TotalChanges { get; set; }

    /// <summary>
    /// Number of concepts added
    /// </summary>
    public int ConceptsAdded { get; set; }

    /// <summary>
    /// Number of concepts modified
    /// </summary>
    public int ConceptsModified { get; set; }

    /// <summary>
    /// Number of concepts deleted
    /// </summary>
    public int ConceptsDeleted { get; set; }

    /// <summary>
    /// Number of relationships added
    /// </summary>
    public int RelationshipsAdded { get; set; }

    /// <summary>
    /// Number of relationships modified
    /// </summary>
    public int RelationshipsModified { get; set; }

    /// <summary>
    /// Number of relationships deleted
    /// </summary>
    public int RelationshipsDeleted { get; set; }

    /// <summary>
    /// Recent activity entries (limited to top N most recent)
    /// </summary>
    public List<OntologyActivityDto> RecentActivities { get; set; } = new();

    /// <summary>
    /// List of users who made changes
    /// </summary>
    public List<string> ContributorNames { get; set; } = new();

    /// <summary>
    /// Has the user dismissed the "What's New" panel for these changes?
    /// </summary>
    public bool HasDismissed { get; set; }
}
