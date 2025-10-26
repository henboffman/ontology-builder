namespace Eidos.Models;

/// <summary>
/// Represents a user in the system
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// Unique username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the user
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Email address (optional for MVP, required for future auth)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user last logged in (for future use)
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // NOTE: Navigation property removed during Identity migration
    // Ontologies are now owned by ApplicationUser, not legacy User
}
