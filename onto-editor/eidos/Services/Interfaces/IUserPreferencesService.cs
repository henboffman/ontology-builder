using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing user preferences
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Get preferences for a user (creates default preferences if none exist)
    /// </summary>
    Task<UserPreferences> GetPreferencesAsync(string userId);

    /// <summary>
    /// Get preferences for the current authenticated user
    /// </summary>
    Task<UserPreferences> GetCurrentUserPreferencesAsync();

    /// <summary>
    /// Update user preferences
    /// </summary>
    Task<UserPreferences> UpdatePreferencesAsync(UserPreferences preferences);

    /// <summary>
    /// Reset preferences to defaults for a user
    /// </summary>
    Task<UserPreferences> ResetToDefaultsAsync(string userId);

    /// <summary>
    /// Update theme preference for the current user
    /// </summary>
    Task UpdateThemeAsync(string theme);

    /// <summary>
    /// Update ShowKeyboardShortcuts preference for a specific user
    /// </summary>
    Task UpdateShowKeyboardShortcutsAsync(string userId, bool showShortcuts);
}
