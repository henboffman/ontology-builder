using Eidos.Models.Enums;

namespace Eidos.Models.DTOs;

/// <summary>
/// Information about a collaborator who has access to an ontology
/// </summary>
public class CollaboratorInfo
{
    /// <summary>
    /// User ID (null for guest users)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Collaborator's display name (email for authenticated users, custom name for guests)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address (only for authenticated users)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Permission level this collaborator has
    /// </summary>
    public PermissionLevel PermissionLevel { get; set; }

    /// <summary>
    /// When the collaborator first accessed the ontology
    /// </summary>
    public DateTime FirstAccessedAt { get; set; }

    /// <summary>
    /// When the collaborator last accessed the ontology
    /// </summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>
    /// Total number of times this collaborator has accessed the ontology
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// Whether this is a guest user (vs authenticated user)
    /// </summary>
    public bool IsGuest { get; set; }

    /// <summary>
    /// Recent activities performed by this collaborator
    /// </summary>
    public List<CollaboratorActivity> RecentActivities { get; set; } = new();

    /// <summary>
    /// Summary statistics of this collaborator's edits
    /// </summary>
    public CollaboratorEditStats EditStats { get; set; } = new();
}

/// <summary>
/// Summary of a collaborator's activity
/// </summary>
public class CollaboratorActivity
{
    public int Id { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? VersionNumber { get; set; }
}

/// <summary>
/// Statistics about a collaborator's edits
/// </summary>
public class CollaboratorEditStats
{
    public int TotalEdits { get; set; }
    public int ConceptsCreated { get; set; }
    public int ConceptsUpdated { get; set; }
    public int ConceptsDeleted { get; set; }
    public int RelationshipsCreated { get; set; }
    public int RelationshipsUpdated { get; set; }
    public int RelationshipsDeleted { get; set; }
    public int PropertiesCreated { get; set; }
    public int PropertiesUpdated { get; set; }
    public int PropertiesDeleted { get; set; }
    public DateTime? LastEditDate { get; set; }
}
