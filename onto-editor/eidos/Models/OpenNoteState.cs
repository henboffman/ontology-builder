namespace Eidos.Models;

/// <summary>
/// Represents the state of a note currently open in the grid layout
/// </summary>
public class OpenNoteState
{
    /// <summary>
    /// The note ID
    /// </summary>
    public int NoteId { get; set; }

    /// <summary>
    /// Note title for display
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Current note content (markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The position in the grid (0-3 for visible positions, 4+ for hidden notes)
    /// </summary>
    public int GridPosition { get; set; }

    /// <summary>
    /// Whether this note has unsaved changes
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Whether this note is currently being saved
    /// </summary>
    public bool IsSaving { get; set; }

    /// <summary>
    /// Whether this note is currently focused
    /// </summary>
    public bool IsFocused { get; set; }

    /// <summary>
    /// Last modified timestamp for tracking changes
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Original content when note was loaded (for detecting changes)
    /// </summary>
    public string OriginalContent { get; set; } = string.Empty;
}
