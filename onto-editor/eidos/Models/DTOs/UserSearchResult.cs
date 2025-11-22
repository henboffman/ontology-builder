namespace Eidos.Models.DTOs;

/// <summary>
/// DTO for user search results
/// Used in user picker components for granting access
/// </summary>
public class UserSearchResult
{
    /// <summary>
    /// User ID (Identity ID)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name (or email if no display name)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Profile photo URL (from OAuth provider or Gravatar)
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Whether this user has collaborated with the searching user before
    /// Used for sorting/prioritization
    /// </summary>
    public bool HasCollaborated { get; set; } = false;
}
