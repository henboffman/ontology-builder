using Eidos.Models.Enums;

namespace Eidos.Models;

/// <summary>
/// Represents a shareable link for collaborative access to an ontology
/// </summary>
public class OntologyShare
{
    public int Id { get; set; }

    /// <summary>
    /// The ontology being shared
    /// </summary>
    public int OntologyId { get; set; }
    public Ontology? Ontology { get; set; }

    /// <summary>
    /// User who created the share (owner)
    /// </summary>
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    /// <summary>
    /// Unique token for the share link (used in URL)
    /// Security: Use cryptographically secure random token
    /// </summary>
    public string ShareToken { get; set; } = string.Empty;

    /// <summary>
    /// Permission level granted to users accessing via this link
    /// </summary>
    public PermissionLevel PermissionLevel { get; set; }

    /// <summary>
    /// Whether guests (unauthenticated users) can access via this link
    /// </summary>
    public bool AllowGuestAccess { get; set; }

    /// <summary>
    /// Optional: Expiration date for the share link
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this share link is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Number of times this link has been accessed
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// Last time the link was accessed
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// When the share was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional note about the share
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Guest sessions using this share
    /// </summary>
    public ICollection<GuestSession>? GuestSessions { get; set; }
}
