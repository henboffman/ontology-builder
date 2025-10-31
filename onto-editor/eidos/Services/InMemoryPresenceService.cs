using Eidos.Models;
using Eidos.Services.Interfaces;
using System.Collections.Concurrent;

namespace Eidos.Services;

/// <summary>
/// In-memory presence service for development/single-server deployments
/// Uses static ConcurrentDictionary for fast access
/// NOTE: Not suitable for production horizontal scaling
/// </summary>
public class InMemoryPresenceService : IPresenceService
{
    private readonly ILogger<InMemoryPresenceService> _logger;

    // In-memory storage for presence tracking
    // Key: OntologyId, Value: Dictionary of ConnectionId -> PresenceInfo
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, PresenceInfo>> _presenceByOntology = new();

    public InMemoryPresenceService(ILogger<InMemoryPresenceService> logger)
    {
        _logger = logger;
    }

    public Task AddOrUpdatePresenceAsync(int ontologyId, PresenceInfo presenceInfo)
    {
        var ontologyPresence = _presenceByOntology.GetOrAdd(ontologyId, _ => new ConcurrentDictionary<string, PresenceInfo>());
        ontologyPresence[presenceInfo.ConnectionId] = presenceInfo;

        _logger.LogDebug(
            "Added/updated presence for user {UserId} in ontology {OntologyId}",
            presenceInfo.UserId,
            ontologyId);

        return Task.CompletedTask;
    }

    public Task RemovePresenceAsync(int ontologyId, string connectionId)
    {
        if (_presenceByOntology.TryGetValue(ontologyId, out var ontologyPresence))
        {
            ontologyPresence.TryRemove(connectionId, out _);

            // Clean up empty ontology presence dictionaries
            if (ontologyPresence.IsEmpty)
            {
                _presenceByOntology.TryRemove(ontologyId, out _);
            }

            _logger.LogDebug(
                "Removed presence for connection {ConnectionId} from ontology {OntologyId}",
                connectionId,
                ontologyId);
        }

        return Task.CompletedTask;
    }

    public Task<List<PresenceInfo>> GetPresenceListAsync(int ontologyId)
    {
        if (_presenceByOntology.TryGetValue(ontologyId, out var ontologyPresence))
        {
            return Task.FromResult(ontologyPresence.Values.ToList());
        }

        return Task.FromResult(new List<PresenceInfo>());
    }

    public Task UpdateCurrentViewAsync(int ontologyId, string connectionId, string viewName)
    {
        if (_presenceByOntology.TryGetValue(ontologyId, out var ontologyPresence))
        {
            if (ontologyPresence.TryGetValue(connectionId, out var presence))
            {
                presence.CurrentView = viewName;
                presence.LastSeenAt = DateTime.UtcNow;
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateLastSeenAsync(int ontologyId, string connectionId)
    {
        if (_presenceByOntology.TryGetValue(ontologyId, out var ontologyPresence))
        {
            if (ontologyPresence.TryGetValue(connectionId, out var presence))
            {
                presence.LastSeenAt = DateTime.UtcNow;
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> ConnectionExistsAsync(int ontologyId, string connectionId)
    {
        if (_presenceByOntology.TryGetValue(ontologyId, out var ontologyPresence))
        {
            return Task.FromResult(ontologyPresence.ContainsKey(connectionId));
        }

        return Task.FromResult(false);
    }

    public Task RemoveConnectionFromAllOntologiesAsync(string connectionId)
    {
        foreach (var (ontologyId, ontologyPresence) in _presenceByOntology)
        {
            if (ontologyPresence.TryRemove(connectionId, out _))
            {
                // Clean up empty dictionaries
                if (ontologyPresence.IsEmpty)
                {
                    _presenceByOntology.TryRemove(ontologyId, out _);
                }
            }
        }

        _logger.LogDebug("Removed connection {ConnectionId} from all ontologies", connectionId);
        return Task.CompletedTask;
    }

    public Task CleanupStalePresenceAsync(TimeSpan threshold)
    {
        var cutoffTime = DateTime.UtcNow - threshold;
        var removed = 0;

        foreach (var (ontologyId, ontologyPresence) in _presenceByOntology)
        {
            var staleConnections = ontologyPresence
                .Where(kvp => kvp.Value.LastSeenAt < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var connectionId in staleConnections)
            {
                if (ontologyPresence.TryRemove(connectionId, out _))
                {
                    removed++;
                }
            }

            // Clean up empty dictionaries
            if (ontologyPresence.IsEmpty)
            {
                _presenceByOntology.TryRemove(ontologyId, out _);
            }
        }

        if (removed > 0)
        {
            _logger.LogInformation("Cleaned up {Count} stale presence entries", removed);
        }

        return Task.CompletedTask;
    }
}
