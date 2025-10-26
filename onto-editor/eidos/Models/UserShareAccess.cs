namespace Eidos.Models;

/// <summary>
/// Tracks which authenticated users have accessed which shared ontologies
/// This allows us to show shared ontologies in a user's dashboard
/// </summary>
public class UserShareAccess
{
    public int Id { get; set; }

    /// <summary>
    /// The authenticated user who accessed the share
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// The share link that was accessed
    /// </summary>
    public int OntologyShareId { get; set; }
    public OntologyShare? OntologyShare { get; set; }

    /// <summary>
    /// When the user first accessed this share
    /// </summary>
    public DateTime FirstAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user last accessed this share
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// How many times the user has accessed this share
    /// </summary>
    public int AccessCount { get; set; } = 1;
}
