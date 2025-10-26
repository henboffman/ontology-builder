namespace Eidos.Models;

/// <summary>
/// Represents a guest user's session when accessing a shared ontology
/// Tracks guest activity without requiring full authentication
/// </summary>
public class GuestSession
{
    public int Id { get; set; }

    /// <summary>
    /// The share link this guest used
    /// </summary>
    public int OntologyShareId { get; set; }
    public OntologyShare? OntologyShare { get; set; }

    /// <summary>
    /// Unique session identifier (stored in cookie/local storage)
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Display name the guest chose (optional)
    /// </summary>
    public string? GuestName { get; set; }

    /// <summary>
    /// When the session started
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last activity time (for cleanup of stale sessions)
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the session is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// IP address for security/analytics (optional)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent for analytics (optional)
    /// </summary>
    public string? UserAgent { get; set; }
}
