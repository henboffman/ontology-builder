using System.ComponentModel.DataAnnotations;

namespace Eidos.Models;

/// <summary>
/// Tracks when users view ontologies to enable "What's New" feature
/// Records the last time a user viewed an ontology so we can show changes since their last visit
/// </summary>
public class OntologyViewHistory
{
    public int Id { get; set; }

    /// <summary>
    /// The ontology that was viewed
    /// </summary>
    public int OntologyId { get; set; }
    public Ontology Ontology { get; set; } = null!;

    /// <summary>
    /// The user who viewed the ontology
    /// </summary>
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// When the user last viewed this ontology
    /// Updated each time the user opens the ontology
    /// </summary>
    public DateTime LastViewedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user dismissed/acknowledged the "What's New" panel
    /// This allows showing the panel again after new changes occur
    /// </summary>
    public DateTime? LastDismissedAt { get; set; }

    /// <summary>
    /// The current session identifier for the user's browser session
    /// Used to determine what's new since the last session (not just timestamp-based)
    /// This is more reliable than timestamps for tracking "what's new"
    /// </summary>
    [StringLength(100)]
    public string? CurrentSessionId { get; set; }

    /// <summary>
    /// The session ID when the user last dismissed the "What's New" panel
    /// Used to show new changes only from OTHER users since the last dismissal
    /// </summary>
    [StringLength(100)]
    public string? LastDismissedSessionId { get; set; }

    /// <summary>
    /// How many times the user has viewed this ontology
    /// Can be used for analytics and determining "frequent collaborators"
    /// </summary>
    public int ViewCount { get; set; } = 1;

    /// <summary>
    /// Created timestamp for the first view
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
