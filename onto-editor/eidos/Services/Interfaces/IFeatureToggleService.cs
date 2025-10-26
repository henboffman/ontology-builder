using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing feature toggles
/// </summary>
public interface IFeatureToggleService
{
    /// <summary>
    /// Check if a feature is enabled by key
    /// </summary>
    Task<bool> IsEnabledAsync(string key);

    /// <summary>
    /// Get a feature toggle by key
    /// </summary>
    Task<FeatureToggle?> GetByKeyAsync(string key);

    /// <summary>
    /// Get all feature toggles
    /// </summary>
    Task<IEnumerable<FeatureToggle>> GetAllAsync();

    /// <summary>
    /// Get feature toggles by category
    /// </summary>
    Task<IEnumerable<FeatureToggle>> GetByCategoryAsync(string category);

    /// <summary>
    /// Enable a feature
    /// </summary>
    Task EnableAsync(string key);

    /// <summary>
    /// Disable a feature
    /// </summary>
    Task DisableAsync(string key);

    /// <summary>
    /// Toggle a feature on/off
    /// </summary>
    Task ToggleAsync(string key);

    /// <summary>
    /// Update a feature toggle
    /// </summary>
    Task UpdateAsync(FeatureToggle toggle);
}
