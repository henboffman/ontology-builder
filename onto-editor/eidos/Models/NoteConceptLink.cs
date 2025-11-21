using System.ComponentModel.DataAnnotations;

namespace Eidos.Models;

/// <summary>
/// Represents an automatically detected link between a Note and a Concept
/// Created when the note's content mentions the concept's name
/// This enables automatic backlinks and semantic connections between notes and the ontology
/// </summary>
public class NoteConceptLink
{
    public int Id { get; set; }

    /// <summary>
    /// The note that contains the mention
    /// </summary>
    [Required]
    public int NoteId { get; set; }
    public Note Note { get; set; } = null!;

    /// <summary>
    /// The concept that is mentioned in the note
    /// </summary>
    [Required]
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;

    /// <summary>
    /// Character position of the first mention in the note content
    /// Used for scrolling to the mention when clicking a backlink
    /// -1 if position tracking is disabled or unavailable
    /// </summary>
    public int FirstMentionPosition { get; set; } = -1;

    /// <summary>
    /// Total number of times this concept is mentioned in the note
    /// Updated whenever the note is saved
    /// </summary>
    public int TotalMentions { get; set; } = 0;

    /// <summary>
    /// When this link was first detected
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this link was last updated (e.g., mention count changed, position updated)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
