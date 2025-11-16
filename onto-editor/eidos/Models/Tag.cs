using System.ComponentModel.DataAnnotations;

namespace Eidos.Models;

/// <summary>
/// Represents a tag for organizing notes within a workspace
/// Tags enable virtual folder organization similar to Obsidian/Notion
/// </summary>
public class Tag
{
    public int Id { get; set; }

    /// <summary>
    /// The tag name (e.g., "project", "important", "research")
    /// Must be unique within the workspace
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The workspace this tag belongs to
    /// </summary>
    public int WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    /// <summary>
    /// Optional hex color for visual distinction (e.g., "#3498db")
    /// If null, a color will be auto-generated
    /// </summary>
    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color (e.g., #3498db)")]
    public string? Color { get; set; }

    /// <summary>
    /// Optional description of what this tag represents
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// When the tag was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created the tag
    /// </summary>
    [Required]
    public string CreatedBy { get; set; } = string.Empty;
    public ApplicationUser Creator { get; set; } = null!;

    /// <summary>
    /// Navigation property to note assignments
    /// </summary>
    public ICollection<NoteTagAssignment> NoteAssignments { get; set; } = new List<NoteTagAssignment>();

    /// <summary>
    /// Get a display-friendly version of the tag name
    /// Converts kebab-case and snake_case to Title Case
    /// </summary>
    public string GetDisplayName()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return string.Empty;

        // Replace hyphens and underscores with spaces
        var displayName = Name.Replace('-', ' ').Replace('_', ' ');

        // Capitalize first letter of each word
        var words = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w =>
            char.ToUpper(w[0]) + w.Substring(1).ToLower()));
    }

    /// <summary>
    /// Get a color for this tag, auto-generating if not set
    /// </summary>
    public string GetColor()
    {
        if (!string.IsNullOrEmpty(Color))
            return Color;

        // Auto-generate color based on tag name hash
        return GenerateColorFromName(Name);
    }

    /// <summary>
    /// Generate a consistent color from tag name
    /// Uses hash to ensure same tag name always gets same color
    /// </summary>
    private static string GenerateColorFromName(string name)
    {
        // Predefined palette of visually distinct colors
        var colors = new[]
        {
            "#3498db", // Blue
            "#e74c3c", // Red
            "#2ecc71", // Green
            "#f39c12", // Orange
            "#9b59b6", // Purple
            "#1abc9c", // Turquoise
            "#e67e22", // Carrot
            "#34495e", // Dark gray
            "#16a085", // Dark turquoise
            "#27ae60", // Dark green
            "#2980b9", // Dark blue
            "#8e44ad", // Dark purple
            "#c0392b", // Dark red
            "#d35400", // Pumpkin
        };

        // Use hash to select color consistently
        var hash = name.GetHashCode();
        var index = Math.Abs(hash) % colors.Length;
        return colors[index];
    }
}
