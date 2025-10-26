using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;
using Eidos.Services.Interfaces;

namespace Eidos.Services;

/// <summary>
/// Service for managing user preferences
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly IUserService _userService;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(
        IDbContextFactory<OntologyDbContext> contextFactory,
        IUserService userService,
        ILogger<UserPreferencesService> logger)
    {
        _contextFactory = contextFactory;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get preferences for a user (creates default preferences if none exist)
    /// </summary>
    public async Task<UserPreferences> GetPreferencesAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Create default preferences for this user
            preferences = new UserPreferences
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.UserPreferences.Add(preferences);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created default preferences for user {UserId}", userId);
        }

        return preferences;
    }

    /// <summary>
    /// Get preferences for the current authenticated user
    /// </summary>
    public async Task<UserPreferences> GetCurrentUserPreferencesAsync()
    {
        var currentUser = await _userService.GetCurrentUserAsync();

        if (currentUser == null)
        {
            throw new InvalidOperationException("No authenticated user found");
        }

        return await GetPreferencesAsync(currentUser.Id);
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    public async Task<UserPreferences> UpdatePreferencesAsync(UserPreferences preferences)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.UserPreferences.FindAsync(preferences.Id);

        if (existing == null)
        {
            throw new InvalidOperationException($"UserPreferences with ID {preferences.Id} not found");
        }

        // Update all preference values
        existing.EntityColor = preferences.EntityColor;
        existing.ProcessColor = preferences.ProcessColor;
        existing.QualityColor = preferences.QualityColor;
        existing.RoleColor = preferences.RoleColor;
        existing.FunctionColor = preferences.FunctionColor;
        existing.InformationColor = preferences.InformationColor;
        existing.EventColor = preferences.EventColor;
        existing.DefaultConceptColor = preferences.DefaultConceptColor;

        existing.IsARelationshipColor = preferences.IsARelationshipColor;
        existing.PartOfRelationshipColor = preferences.PartOfRelationshipColor;
        existing.HasPartRelationshipColor = preferences.HasPartRelationshipColor;
        existing.RelatedToRelationshipColor = preferences.RelatedToRelationshipColor;
        existing.DefaultRelationshipColor = preferences.DefaultRelationshipColor;

        existing.DefaultNodeSize = preferences.DefaultNodeSize;
        existing.DefaultEdgeThickness = preferences.DefaultEdgeThickness;
        existing.ShowEdgeLabels = preferences.ShowEdgeLabels;
        existing.AutoColorByCategory = preferences.AutoColorByCategory;
        existing.TextSizeScale = preferences.TextSizeScale;
        existing.Theme = preferences.Theme;

        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated preferences for user {UserId}", preferences.UserId);

        return existing;
    }

    /// <summary>
    /// Reset preferences to defaults for a user
    /// </summary>
    public async Task<UserPreferences> ResetToDefaultsAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // No preferences exist, create new defaults
            return await GetPreferencesAsync(userId);
        }

        // Reset to default values
        var defaults = new UserPreferences();

        preferences.EntityColor = defaults.EntityColor;
        preferences.ProcessColor = defaults.ProcessColor;
        preferences.QualityColor = defaults.QualityColor;
        preferences.RoleColor = defaults.RoleColor;
        preferences.FunctionColor = defaults.FunctionColor;
        preferences.InformationColor = defaults.InformationColor;
        preferences.EventColor = defaults.EventColor;
        preferences.DefaultConceptColor = defaults.DefaultConceptColor;

        preferences.IsARelationshipColor = defaults.IsARelationshipColor;
        preferences.PartOfRelationshipColor = defaults.PartOfRelationshipColor;
        preferences.HasPartRelationshipColor = defaults.HasPartRelationshipColor;
        preferences.RelatedToRelationshipColor = defaults.RelatedToRelationshipColor;
        preferences.DefaultRelationshipColor = defaults.DefaultRelationshipColor;

        preferences.DefaultNodeSize = defaults.DefaultNodeSize;
        preferences.DefaultEdgeThickness = defaults.DefaultEdgeThickness;
        preferences.ShowEdgeLabels = defaults.ShowEdgeLabels;
        preferences.AutoColorByCategory = defaults.AutoColorByCategory;
        preferences.TextSizeScale = defaults.TextSizeScale;
        preferences.Theme = defaults.Theme;

        preferences.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Reset preferences to defaults for user {UserId}", userId);

        return preferences;
    }

    /// <summary>
    /// Update theme preference for the current user
    /// </summary>
    public async Task UpdateThemeAsync(string theme)
    {
        var currentUser = await _userService.GetCurrentUserAsync();

        if (currentUser == null)
        {
            _logger.LogWarning("Attempted to update theme with no authenticated user");
            return;
        }

        using var context = await _contextFactory.CreateDbContextAsync();

        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

        if (preferences == null)
        {
            // Create new preferences with theme
            preferences = new UserPreferences
            {
                UserId = currentUser.Id,
                Theme = theme,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.UserPreferences.Add(preferences);
        }
        else
        {
            preferences.Theme = theme;
            preferences.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated theme to {Theme} for user {UserId}", theme, currentUser.Id);
    }
}
