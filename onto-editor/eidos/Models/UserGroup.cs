using System.ComponentModel.DataAnnotations;

namespace Eidos.Models;

/// <summary>
/// User group for organizing users and managing ontology access
/// </summary>
public class UserGroup
{
    public int Id { get; set; }

    /// <summary>
    /// Unique name for the group
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the group's purpose
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// User who created the group (typically an admin)
    /// </summary>
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    /// <summary>
    /// When the group was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the group was modified
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the group is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Color code for UI display (hex color)
    /// </summary>
    [StringLength(7)] // #RRGGBB
    public string? Color { get; set; }

    /// <summary>
    /// Users in this group (many-to-many)
    /// </summary>
    public ICollection<UserGroupMember> Members { get; set; } = new List<UserGroupMember>();

    /// <summary>
    /// Ontologies shared with this group
    /// </summary>
    public ICollection<OntologyGroupPermission> OntologyPermissions { get; set; } = new List<OntologyGroupPermission>();
}

/// <summary>
/// Junction table for users in groups (many-to-many)
/// </summary>
public class UserGroupMember
{
    public int Id { get; set; }

    public int UserGroupId { get; set; }
    public UserGroup UserGroup { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// When the user was added to the group
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who added this user to the group
    /// </summary>
    public string? AddedByUserId { get; set; }
    public ApplicationUser? AddedByUser { get; set; }

    /// <summary>
    /// Whether the user is a group admin (can manage group membership)
    /// </summary>
    public bool IsGroupAdmin { get; set; } = false;
}

/// <summary>
/// Permissions for a group to access an ontology
/// </summary>
public class OntologyGroupPermission
{
    public int Id { get; set; }

    public int OntologyId { get; set; }
    public Ontology Ontology { get; set; } = null!;

    public int UserGroupId { get; set; }
    public UserGroup UserGroup { get; set; } = null!;

    /// <summary>
    /// Permission level: "view", "edit", "admin"
    /// </summary>
    [Required]
    [StringLength(20)]
    public string PermissionLevel { get; set; } = "view";

    /// <summary>
    /// When this permission was granted
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who granted this permission
    /// </summary>
    public string? GrantedByUserId { get; set; }
    public ApplicationUser? GrantedByUser { get; set; }
}

/// <summary>
/// Permission level constants
/// </summary>
public static class PermissionLevels
{
    public const string View = "view";
    public const string Edit = "edit";
    public const string Admin = "admin";
}
