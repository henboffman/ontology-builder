using System.ComponentModel.DataAnnotations;

namespace Eidos.Models;

/// <summary>
/// User preferences for customizing the application experience
/// Stores default colors, templates, and other user-specific settings
/// </summary>
public class UserPreferences
{
    public int Id { get; set; }

    /// <summary>
    /// The user who owns these preferences (ApplicationUser ID from Identity)
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    // === Concept Color Defaults ===

    /// <summary>
    /// Default color for Entity concepts
    /// </summary>
    [StringLength(50)]
    public string EntityColor { get; set; } = "#4A90E2"; // Blue

    /// <summary>
    /// Default color for Process concepts
    /// </summary>
    [StringLength(50)]
    public string ProcessColor { get; set; } = "#E67E22"; // Orange

    /// <summary>
    /// Default color for Quality concepts
    /// </summary>
    [StringLength(50)]
    public string QualityColor { get; set; } = "#6BCF7F"; // Green

    /// <summary>
    /// Default color for Role concepts
    /// </summary>
    [StringLength(50)]
    public string RoleColor { get; set; } = "#9B59B6"; // Purple

    /// <summary>
    /// Default color for Function concepts
    /// </summary>
    [StringLength(50)]
    public string FunctionColor { get; set; } = "#E74C3C"; // Red

    /// <summary>
    /// Default color for Information concepts
    /// </summary>
    [StringLength(50)]
    public string InformationColor { get; set; } = "#3498DB"; // Light Blue

    /// <summary>
    /// Default color for Event concepts
    /// </summary>
    [StringLength(50)]
    public string EventColor { get; set; } = "#F39C12"; // Amber

    /// <summary>
    /// Default color for uncategorized/custom concepts
    /// </summary>
    [StringLength(50)]
    public string DefaultConceptColor { get; set; } = "#95A5A6"; // Gray

    // === Relationship Color Defaults ===

    /// <summary>
    /// Default color for "is-a" relationships
    /// </summary>
    [StringLength(50)]
    public string IsARelationshipColor { get; set; } = "#2C3E50"; // Dark Blue

    /// <summary>
    /// Default color for "part-of" relationships
    /// </summary>
    [StringLength(50)]
    public string PartOfRelationshipColor { get; set; } = "#16A085"; // Teal

    /// <summary>
    /// Default color for "has-part" relationships
    /// </summary>
    [StringLength(50)]
    public string HasPartRelationshipColor { get; set; } = "#27AE60"; // Green

    /// <summary>
    /// Default color for "related-to" relationships
    /// </summary>
    [StringLength(50)]
    public string RelatedToRelationshipColor { get; set; } = "#7F8C8D"; // Gray

    /// <summary>
    /// Default color for uncategorized/custom relationships
    /// </summary>
    [StringLength(50)]
    public string DefaultRelationshipColor { get; set; } = "#34495E"; // Dark Gray

    // === Graph Display Preferences ===

    /// <summary>
    /// Default node size in graph view
    /// </summary>
    public int DefaultNodeSize { get; set; } = 40;

    /// <summary>
    /// Default edge thickness in graph view
    /// </summary>
    public int DefaultEdgeThickness { get; set; } = 2;

    /// <summary>
    /// Whether to show labels on edges by default
    /// </summary>
    public bool ShowEdgeLabels { get; set; } = true;

    /// <summary>
    /// Whether to auto-color concepts by category
    /// </summary>
    public bool AutoColorByCategory { get; set; } = true;

    /// <summary>
    /// Text size scale for graph labels (50-150, default 100)
    /// </summary>
    public int TextSizeScale { get; set; } = 100;

    // === Theme Preferences ===

    /// <summary>
    /// UI theme preference: "light" or "dark"
    /// </summary>
    [StringLength(20)]
    public string Theme { get; set; } = "light";

    /// <summary>
    /// Layout style preference: "sidebar" or "topbar"
    /// </summary>
    [StringLength(20)]
    public string LayoutStyle { get; set; } = "topbar";

    // === User Interface Preferences ===

    /// <summary>
    /// Whether to show the keyboard shortcuts banner
    /// </summary>
    public bool ShowKeyboardShortcuts { get; set; } = true;

    // === Metadata ===

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Get the default color for a given concept category
    /// </summary>
    public string GetColorForCategory(string? category)
    {
        if (string.IsNullOrEmpty(category))
            return DefaultConceptColor;

        return category.ToLower() switch
        {
            "entity" => EntityColor,
            "process" => ProcessColor,
            "quality" => QualityColor,
            "role" => RoleColor,
            "function" => FunctionColor,
            "information" => InformationColor,
            "event" => EventColor,
            _ => DefaultConceptColor
        };
    }

    /// <summary>
    /// Get the default color for a given relationship type
    /// </summary>
    public string GetColorForRelationshipType(string? relationType)
    {
        if (string.IsNullOrEmpty(relationType))
            return DefaultRelationshipColor;

        var normalizedType = relationType.ToLower().Replace(" ", "-").Replace("_", "-");

        return normalizedType switch
        {
            "is-a" or "isa" or "subclass-of" or "type" => IsARelationshipColor,
            "part-of" or "partof" => PartOfRelationshipColor,
            "has-part" or "haspart" => HasPartRelationshipColor,
            "related-to" or "relatedto" or "related" => RelatedToRelationshipColor,
            _ => DefaultRelationshipColor
        };
    }
}
