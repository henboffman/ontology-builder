using System.ComponentModel.DataAnnotations;

namespace Eidos.Models;

/// <summary>
/// Junction table linking notes to tags (many-to-many relationship)
/// Allows notes to have multiple tags and tags to be assigned to multiple notes
/// </summary>
public class NoteTagAssignment
{
    public int Id { get; set; }

    /// <summary>
    /// The note being tagged
    /// </summary>
    public int NoteId { get; set; }
    public Note Note { get; set; } = null!;

    /// <summary>
    /// The tag being assigned
    /// </summary>
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    /// <summary>
    /// When the tag was assigned to this note
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who assigned this tag
    /// </summary>
    [Required]
    public string AssignedBy { get; set; } = string.Empty;
    public ApplicationUser Assigner { get; set; } = null!;
}
