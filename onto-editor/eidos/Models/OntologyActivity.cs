using System.ComponentModel.DataAnnotations;

namespace Eidos.Models;

/// <summary>
/// Tracks all activity and changes made to an ontology
/// Forms the foundation for version control and collaboration history
/// </summary>
public class OntologyActivity
{
    public int Id { get; set; }

    /// <summary>
    /// The ontology this activity is associated with
    /// </summary>
    public int OntologyId { get; set; }
    public Ontology Ontology { get; set; } = null!;

    /// <summary>
    /// User who performed the action (null for guest users)
    /// </summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Guest session token if this was performed by a guest
    /// </summary>
    public string? GuestSessionToken { get; set; }

    /// <summary>
    /// Display name for the actor (user email or guest name)
    /// Denormalized for performance and to preserve history if user is deleted
    /// </summary>
    [StringLength(200)]
    public string ActorName { get; set; } = string.Empty;

    /// <summary>
    /// Type of activity: "create", "update", "delete", "import", "export"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity type affected: "ontology", "concept", "relationship", "property", "individual", "restriction"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity (if applicable)
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Name/label of the affected entity for display purposes
    /// Denormalized to preserve history if entity is deleted
    /// </summary>
    [StringLength(500)]
    public string? EntityName { get; set; }

    /// <summary>
    /// Human-readable description of the change
    /// E.g., "Created concept 'Person'", "Updated relationship 'is-a'", "Deleted property 'age'"
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON snapshot of the entity state before the change (for rollback/diff)
    /// Null for create operations
    /// </summary>
    public string? BeforeSnapshot { get; set; }

    /// <summary>
    /// JSON snapshot of the entity state after the change (for rollback/diff)
    /// Null for delete operations
    /// </summary>
    public string? AfterSnapshot { get; set; }

    /// <summary>
    /// IP address of the user who made the change (for audit purposes)
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string (for tracking which client was used)
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// When this activity occurred
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional version number for this change (for version control)
    /// Auto-incremented per ontology
    /// </summary>
    public int? VersionNumber { get; set; }

    /// <summary>
    /// Optional commit message/notes from the user
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Common activity type constants
/// </summary>
public static class ActivityTypes
{
    public const string Create = "create";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string Import = "import";
    public const string Export = "export";
    public const string Fork = "fork";
    public const string Clone = "clone";
    public const string Share = "share";
}

/// <summary>
/// Common entity type constants
/// </summary>
public static class EntityTypes
{
    public const string Ontology = "ontology";
    public const string Concept = "concept";
    public const string Relationship = "relationship";
    public const string Property = "property";
    public const string Individual = "individual";
    public const string Restriction = "restriction";
    public const string Template = "template";
}
