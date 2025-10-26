using System.ComponentModel.DataAnnotations;

namespace Eidos.Models;

/// <summary>
/// Represents a feature toggle for controlling application features
/// </summary>
public class FeatureToggle
{
    public int Id { get; set; }

    /// <summary>
    /// Unique key for the feature (e.g., "show-test-users", "enable-collaboration")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for the feature
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this toggle controls
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the feature is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Category for grouping toggles (e.g., "User Management", "Collaboration", "UI")
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// When the toggle was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the toggle was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
