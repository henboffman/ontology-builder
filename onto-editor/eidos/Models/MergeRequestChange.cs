using System.ComponentModel.DataAnnotations;
using Eidos.Models.Enums;

namespace Eidos.Models;

/// <summary>
/// Represents a single change within a merge request (add, modify, or delete an entity).
/// </summary>
public class MergeRequestChange
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The merge request this change belongs to
    /// </summary>
    public int MergeRequestId { get; set; }
    public MergeRequest MergeRequest { get; set; } = null!;

    /// <summary>
    /// Type of change (Add, Modify, Delete)
    /// </summary>
    public MergeRequestChangeType ChangeType { get; set; }

    /// <summary>
    /// Type of entity being changed (Concept, Relationship, Individual)
    /// </summary>
    public EntityType EntityType { get; set; }

    /// <summary>
    /// ID of the entity being changed (0 for new entities)
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Display name/title of the entity
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Before state of the entity (JSON snapshot)
    /// Null for Add operations
    /// </summary>
    public string? BeforeJson { get; set; }

    /// <summary>
    /// After state of the entity (JSON snapshot)
    /// Null for Delete operations
    /// </summary>
    public string? AfterJson { get; set; }

    /// <summary>
    /// Human-readable summary of what changed
    /// E.g., "Changed definition from 'X' to 'Y'"
    /// </summary>
    public string? ChangeSummary { get; set; }

    /// <summary>
    /// Order of this change within the merge request
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// When this change was recorded
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this specific change has a conflict with current state
    /// </summary>
    public bool HasConflict { get; set; }

    /// <summary>
    /// Description of the conflict if one exists
    /// </summary>
    public string? ConflictDescription { get; set; }

    /// <summary>
    /// Gets a display-friendly change type string
    /// </summary>
    public string ChangeTypeDisplay => ChangeType switch
    {
        MergeRequestChangeType.Add => "Added",
        MergeRequestChangeType.Modify => "Modified",
        MergeRequestChangeType.Delete => "Deleted",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets a display-friendly entity type string
    /// </summary>
    public string EntityTypeDisplay => EntityType switch
    {
        EntityType.Concept => "Concept",
        EntityType.Relationship => "Relationship",
        EntityType.Individual => "Individual",
        EntityType.ConceptProperty => "Property",
        _ => "Entity"
    };

    /// <summary>
    /// Gets a color class for UI display
    /// </summary>
    public string ColorClass => ChangeType switch
    {
        MergeRequestChangeType.Add => "text-success",
        MergeRequestChangeType.Modify => "text-warning",
        MergeRequestChangeType.Delete => "text-danger",
        _ => "text-secondary"
    };

    /// <summary>
    /// Gets an icon class for UI display
    /// </summary>
    public string IconClass => ChangeType switch
    {
        MergeRequestChangeType.Add => "bi-plus-circle",
        MergeRequestChangeType.Modify => "bi-pencil",
        MergeRequestChangeType.Delete => "bi-trash",
        _ => "bi-circle"
    };
}
