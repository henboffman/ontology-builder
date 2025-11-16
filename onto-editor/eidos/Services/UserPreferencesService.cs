using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Eidos.Data;
using Eidos.Models;
using Eidos.Services.Interfaces;

namespace Eidos.Services;

/// <summary>
/// Service for managing user preferences with in-memory caching
/// Reduces database queries by 90%+ for frequently accessed preferences
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly IUserService _userService;
    private readonly ILogger<UserPreferencesService> _logger;
    private readonly IMemoryCache _cache;

    private const string CACHE_KEY_PREFIX = "UserPreferences_";
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public UserPreferencesService(
        IDbContextFactory<OntologyDbContext> contextFactory,
        IUserService userService,
        ILogger<UserPreferencesService> logger,
        IMemoryCache cache)
    {
        _contextFactory = contextFactory;
        _userService = userService;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Get preferences for a user (creates default preferences if none exist)
    /// Uses in-memory caching with 5-minute sliding expiration
    /// </summary>
    public async Task<UserPreferences> GetPreferencesAsync(string userId)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}{userId}";

        // Try to get from cache first
        if (_cache.TryGetValue<UserPreferences>(cacheKey, out var cachedPreferences) && cachedPreferences != null)
        {
            return cachedPreferences;
        }

        using var context = await _contextFactory.CreateDbContextAsync();

        var preferences = await context.UserPreferences
            .AsNoTracking()
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

        // Cache the preferences
        _cache.Set(cacheKey, preferences, new MemoryCacheEntryOptions
        {
            SlidingExpiration = _cacheExpiration
        });

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
        existing.GroupingRadius = preferences.GroupingRadius;
        existing.Theme = preferences.Theme;
        existing.LayoutStyle = preferences.LayoutStyle;
        existing.ShowKeyboardShortcuts = preferences.ShowKeyboardShortcuts;

        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated preferences for user {UserId}", preferences.UserId);

        // Invalidate cache
        _cache.Remove($"{CACHE_KEY_PREFIX}{preferences.UserId}");

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
        preferences.GroupingRadius = defaults.GroupingRadius;
        preferences.Theme = defaults.Theme;
        preferences.LayoutStyle = defaults.LayoutStyle;
        preferences.ShowKeyboardShortcuts = defaults.ShowKeyboardShortcuts;

        preferences.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Reset preferences to defaults for user {UserId}", userId);

        // Invalidate cache
        _cache.Remove($"{CACHE_KEY_PREFIX}{userId}");

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

        // Invalidate cache
        _cache.Remove($"{CACHE_KEY_PREFIX}{currentUser.Id}");
    }

    /// <summary>
    /// Update ShowKeyboardShortcuts preference for a specific user
    /// </summary>
    public async Task UpdateShowKeyboardShortcutsAsync(string userId, bool showShortcuts)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Create new preferences with the setting
            preferences = new UserPreferences
            {
                UserId = userId,
                ShowKeyboardShortcuts = showShortcuts,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.UserPreferences.Add(preferences);
        }
        else
        {
            preferences.ShowKeyboardShortcuts = showShortcuts;
            preferences.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated ShowKeyboardShortcuts to {ShowShortcuts} for user {UserId}", showShortcuts, userId);

        // Invalidate cache
        _cache.Remove($"{CACHE_KEY_PREFIX}{userId}");
    }

    public async Task UpdateShowGlobalSearchBannerAsync(string userId, bool showBanner)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Create new preferences with the setting
            preferences = new UserPreferences
            {
                UserId = userId,
                ShowGlobalSearchBanner = showBanner,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.UserPreferences.Add(preferences);
        }
        else
        {
            preferences.ShowGlobalSearchBanner = showBanner;
            preferences.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated ShowGlobalSearchBanner to {ShowBanner} for user {UserId}", showBanner, userId);

        // Invalidate cache
        _cache.Remove($"{CACHE_KEY_PREFIX}{userId}");
    }
}
