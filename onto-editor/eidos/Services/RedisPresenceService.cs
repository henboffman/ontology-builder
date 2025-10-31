using Eidos.Models;
using Eidos.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Eidos.Services;

/// <summary>
/// Redis-based presence service for distributed user presence tracking
/// Enables horizontal scaling by storing presence data in Redis
/// </summary>
public class RedisPresenceService : IPresenceService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisPresenceService> _logger;
    private const string PresenceKeyPrefix = "presence:ontology:";
    private const string ConnectionIndexPrefix = "presence:connection:";

    public RedisPresenceService(
        IDistributedCache cache,
        ILogger<RedisPresenceService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task AddOrUpdatePresenceAsync(int ontologyId, PresenceInfo presenceInfo)
    {
        try
        {
            var ontologyKey = GetOntologyKey(ontologyId);
            var connectionKey = GetConnectionKey(presenceInfo.ConnectionId);

            // Get existing presence list
            var presenceList = await GetPresenceListAsync(ontologyId);

            // Remove existing entry if present
            presenceList.RemoveAll(p => p.ConnectionId == presenceInfo.ConnectionId);

            // Add new/updated entry
            presenceList.Add(presenceInfo);

            // Store updated list
            var json = JsonSerializer.Serialize(presenceList);
            await _cache.SetStringAsync(ontologyKey, json, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(2) // Expire after 2 hours of inactivity
            });

            // Store connection -> ontology mapping for cleanup
            await _cache.SetStringAsync(connectionKey, ontologyId.ToString(), new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(2)
            });

            _logger.LogDebug(
                "Added/updated presence for user {UserId} in ontology {OntologyId}",
                presenceInfo.UserId,
                ontologyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding/updating presence for ontology {OntologyId}", ontologyId);
            // Don't throw - presence tracking failures shouldn't break the application
        }
    }

    public async Task RemovePresenceAsync(int ontologyId, string connectionId)
    {
        try
        {
            var ontologyKey = GetOntologyKey(ontologyId);
            var connectionKey = GetConnectionKey(connectionId);

            // Get existing presence list
            var presenceList = await GetPresenceListAsync(ontologyId);

            // Remove the connection
            var removed = presenceList.RemoveAll(p => p.ConnectionId == connectionId);

            if (removed > 0)
            {
                if (presenceList.Count == 0)
                {
                    // Remove the key entirely if no users left
                    await _cache.RemoveAsync(ontologyKey);
                }
                else
                {
                    // Update the list
                    var json = JsonSerializer.Serialize(presenceList);
                    await _cache.SetStringAsync(ontologyKey, json, new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromHours(2)
                    });
                }

                // Remove connection mapping
                await _cache.RemoveAsync(connectionKey);

                _logger.LogDebug(
                    "Removed presence for connection {ConnectionId} from ontology {OntologyId}",
                    connectionId,
                    ontologyId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing presence from ontology {OntologyId}", ontologyId);
        }
    }

    public async Task<List<PresenceInfo>> GetPresenceListAsync(int ontologyId)
    {
        try
        {
            var ontologyKey = GetOntologyKey(ontologyId);
            var json = await _cache.GetStringAsync(ontologyKey);

            if (string.IsNullOrEmpty(json))
            {
                return new List<PresenceInfo>();
            }

            return JsonSerializer.Deserialize<List<PresenceInfo>>(json) ?? new List<PresenceInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting presence list for ontology {OntologyId}", ontologyId);
            return new List<PresenceInfo>();
        }
    }

    public async Task UpdateCurrentViewAsync(int ontologyId, string connectionId, string viewName)
    {
        try
        {
            var presenceList = await GetPresenceListAsync(ontologyId);
            var presence = presenceList.FirstOrDefault(p => p.ConnectionId == connectionId);

            if (presence != null)
            {
                presence.CurrentView = viewName;
                presence.LastSeenAt = DateTime.UtcNow;

                var ontologyKey = GetOntologyKey(ontologyId);
                var json = JsonSerializer.Serialize(presenceList);
                await _cache.SetStringAsync(ontologyKey, json, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(2)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating view for connection {ConnectionId}", connectionId);
        }
    }

    public async Task UpdateLastSeenAsync(int ontologyId, string connectionId)
    {
        try
        {
            var presenceList = await GetPresenceListAsync(ontologyId);
            var presence = presenceList.FirstOrDefault(p => p.ConnectionId == connectionId);

            if (presence != null)
            {
                presence.LastSeenAt = DateTime.UtcNow;

                var ontologyKey = GetOntologyKey(ontologyId);
                var json = JsonSerializer.Serialize(presenceList);
                await _cache.SetStringAsync(ontologyKey, json, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(2)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last seen for connection {ConnectionId}", connectionId);
        }
    }

    public async Task<bool> ConnectionExistsAsync(int ontologyId, string connectionId)
    {
        try
        {
            var presenceList = await GetPresenceListAsync(ontologyId);
            return presenceList.Any(p => p.ConnectionId == connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking connection existence");
            return false;
        }
    }

    public async Task RemoveConnectionFromAllOntologiesAsync(string connectionId)
    {
        try
        {
            var connectionKey = GetConnectionKey(connectionId);
            var ontologyIdStr = await _cache.GetStringAsync(connectionKey);

            if (!string.IsNullOrEmpty(ontologyIdStr) && int.TryParse(ontologyIdStr, out var ontologyId))
            {
                await RemovePresenceAsync(ontologyId, connectionId);
            }

            _logger.LogDebug("Removed connection {ConnectionId} from all ontologies", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection {ConnectionId} from all ontologies", connectionId);
        }
    }

    public async Task CleanupStalePresenceAsync(TimeSpan threshold)
    {
        // This is a simplified version - in production, you'd want a background service
        // that periodically scans Redis keys and removes stale entries
        _logger.LogInformation("Cleanup of stale presence entries is handled by Redis TTL");
        await Task.CompletedTask;
    }

    private static string GetOntologyKey(int ontologyId) => $"{PresenceKeyPrefix}{ontologyId}";
    private static string GetConnectionKey(string connectionId) => $"{ConnectionIndexPrefix}{connectionId}";
}
