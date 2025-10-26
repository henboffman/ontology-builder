using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;
using Eidos.Services.Interfaces;

namespace Eidos.Services;

/// <summary>
/// Service for managing feature toggles
/// Provides in-memory caching for performance
/// </summary>
public class FeatureToggleService : IFeatureToggleService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private Dictionary<string, bool>? _cache;
    private DateTime? _cacheTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public FeatureToggleService(IDbContextFactory<OntologyDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> IsEnabledAsync(string key)
    {
        // Check cache first
        if (_cache != null && _cacheTime.HasValue && DateTime.UtcNow - _cacheTime.Value < _cacheExpiration)
        {
            return _cache.TryGetValue(key, out var value) && value;
        }

        // Refresh cache
        await RefreshCacheAsync();

        return _cache != null && _cache.TryGetValue(key, out var enabled) && enabled;
    }

    public async Task<FeatureToggle?> GetByKeyAsync(string key)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.FeatureToggles
            .FirstOrDefaultAsync(f => f.Key == key);
    }

    public async Task<IEnumerable<FeatureToggle>> GetAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.FeatureToggles
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<FeatureToggle>> GetByCategoryAsync(string category)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.FeatureToggles
            .Where(f => f.Category == category)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task EnableAsync(string key)
    {
        await SetEnabledAsync(key, true);
    }

    public async Task DisableAsync(string key)
    {
        await SetEnabledAsync(key, false);
    }

    public async Task ToggleAsync(string key)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var toggle = await context.FeatureToggles.FirstOrDefaultAsync(f => f.Key == key);

        if (toggle != null)
        {
            toggle.IsEnabled = !toggle.IsEnabled;
            toggle.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            InvalidateCache();
        }
    }

    public async Task UpdateAsync(FeatureToggle toggle)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        toggle.UpdatedAt = DateTime.UtcNow;
        context.FeatureToggles.Update(toggle);
        await context.SaveChangesAsync();
        InvalidateCache();
    }

    private async Task SetEnabledAsync(string key, bool enabled)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var toggle = await context.FeatureToggles.FirstOrDefaultAsync(f => f.Key == key);

        if (toggle != null)
        {
            toggle.IsEnabled = enabled;
            toggle.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            InvalidateCache();
        }
    }

    private async Task RefreshCacheAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        _cache = await context.FeatureToggles
            .ToDictionaryAsync(f => f.Key, f => f.IsEnabled);
        _cacheTime = DateTime.UtcNow;
    }

    private void InvalidateCache()
    {
        _cache = null;
        _cacheTime = null;
    }
}
