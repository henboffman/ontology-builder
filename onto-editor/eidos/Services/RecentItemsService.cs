using Eidos.Models;
using Microsoft.Extensions.Logging;

namespace Eidos.Services;

/// <summary>
/// Service for tracking and managing recently accessed ontology items.
/// Provides quick access to recently viewed/edited concepts and relationships.
/// </summary>
public class RecentItemsService
{
    private readonly ILogger<RecentItemsService> _logger;
    private readonly Dictionary<string, List<RecentItem>> _userRecentItems = new();
    private const int MaxRecentItems = 10;

    public RecentItemsService(ILogger<RecentItemsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a concept to the user's recent items list.
    /// </summary>
    public void AddRecentConcept(string userId, int ontologyId, Concept concept)
    {
        if (concept == null || string.IsNullOrEmpty(userId))
        {
            return;
        }

        var key = GetUserKey(userId, ontologyId);
        var recentItem = new RecentItem
        {
            Type = RecentItemType.Concept,
            Id = concept.Id,
            Name = concept.Name,
            Subtitle = concept.Definition ?? concept.Category ?? "Concept",
            Icon = "bi-diagram-3",
            AccessedAt = DateTime.UtcNow
        };

        AddItem(key, recentItem);
        _logger.LogDebug("Added concept {ConceptName} to recent items for user {UserId}", concept.Name, userId);
    }

    /// <summary>
    /// Adds a relationship to the user's recent items list.
    /// </summary>
    public void AddRecentRelationship(string userId, int ontologyId, Relationship relationship)
    {
        if (relationship == null || string.IsNullOrEmpty(userId))
        {
            return;
        }

        var key = GetUserKey(userId, ontologyId);
        var recentItem = new RecentItem
        {
            Type = RecentItemType.Relationship,
            Id = relationship.Id,
            Name = relationship.RelationType ?? "Unnamed Relationship",
            Subtitle = $"{relationship.SourceConcept?.Name} → {relationship.TargetConcept?.Name}",
            Icon = "bi-arrow-left-right",
            AccessedAt = DateTime.UtcNow
        };

        AddItem(key, recentItem);
        _logger.LogDebug("Added relationship {RelationshipType} to recent items for user {UserId}",
            relationship.RelationType, userId);
    }

    /// <summary>
    /// Gets the recent items for a user in a specific ontology.
    /// </summary>
    public List<RecentItem> GetRecentItems(string userId, int ontologyId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return new List<RecentItem>();
        }

        var key = GetUserKey(userId, ontologyId);
        lock (_userRecentItems)
        {
            if (_userRecentItems.TryGetValue(key, out var items))
            {
                return items.OrderByDescending(i => i.AccessedAt).ToList();
            }
        }

        return new List<RecentItem>();
    }

    /// <summary>
    /// Clears all recent items for a user in a specific ontology.
    /// </summary>
    public void ClearRecentItems(string userId, int ontologyId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var key = GetUserKey(userId, ontologyId);
        lock (_userRecentItems)
        {
            _userRecentItems.Remove(key);
        }

        _logger.LogDebug("Cleared recent items for user {UserId} in ontology {OntologyId}", userId, ontologyId);
    }

    /// <summary>
    /// Removes a specific item from recent items.
    /// </summary>
    public void RemoveItem(string userId, int ontologyId, RecentItemType type, int itemId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var key = GetUserKey(userId, ontologyId);
        lock (_userRecentItems)
        {
            if (_userRecentItems.TryGetValue(key, out var items))
            {
                var item = items.FirstOrDefault(i => i.Type == type && i.Id == itemId);
                if (item != null)
                {
                    items.Remove(item);
                    _logger.LogDebug("Removed {Type} {Id} from recent items for user {UserId}",
                        type, itemId, userId);
                }
            }
        }
    }

    private void AddItem(string key, RecentItem item)
    {
        lock (_userRecentItems)
        {
            if (!_userRecentItems.ContainsKey(key))
            {
                _userRecentItems[key] = new List<RecentItem>();
            }

            var items = _userRecentItems[key];

            // Remove existing entry if present (to update timestamp)
            var existing = items.FirstOrDefault(i => i.Type == item.Type && i.Id == item.Id);
            if (existing != null)
            {
                items.Remove(existing);
            }

            // Add to front of list
            items.Insert(0, item);

            // Keep only the most recent items
            if (items.Count > MaxRecentItems)
            {
                items.RemoveRange(MaxRecentItems, items.Count - MaxRecentItems);
            }
        }
    }

    private string GetUserKey(string userId, int ontologyId)
    {
        return $"{userId}:{ontologyId}";
    }
}

/// <summary>
/// Represents a recently accessed item (concept or relationship).
/// </summary>
public class RecentItem
{
    /// <summary>
    /// Type of item (Concept or Relationship)
    /// </summary>
    public RecentItemType Type { get; set; }

    /// <summary>
    /// Entity ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Secondary text (definition, relationship endpoints, etc.)
    /// </summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// Bootstrap icon class
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// When the item was last accessed
    /// </summary>
    public DateTime AccessedAt { get; set; }
}

/// <summary>
/// Type of recent item
/// </summary>
public enum RecentItemType
{
    Concept,
    Relationship
}
