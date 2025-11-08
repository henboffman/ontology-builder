using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Eidos.Services;

/// <summary>
/// Service for managing ontology links (both external URI-based and internal virtualized links)
/// Implements permission checking, circular dependency detection, and link operations
/// </summary>
public class OntologyLinkService : IOntologyLinkService
{
    private readonly IOntologyLinkRepository _linkRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly OntologyPermissionService _permissionService;
    private readonly ILogger<OntologyLinkService> _logger;

    public OntologyLinkService(
        IOntologyLinkRepository linkRepository,
        IOntologyRepository ontologyRepository,
        OntologyPermissionService permissionService,
        ILogger<OntologyLinkService> logger)
    {
        _linkRepository = linkRepository;
        _ontologyRepository = ontologyRepository;
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<OntologyLink?> CreateExternalLinkAsync(
        int ontologyId,
        string uri,
        string? label,
        string userId)
    {
        try
        {
            // Check if user has edit permission on the parent ontology
            var canEdit = await _permissionService.CanEditAsync(ontologyId, userId);
            if (!canEdit)
            {
                _logger.LogWarning("User {UserId} does not have permission to edit ontology {OntologyId}",
                    userId, ontologyId);
                return null;
            }

            // Validate URI
            if (string.IsNullOrWhiteSpace(uri))
            {
                _logger.LogWarning("Cannot create external link with empty URI for ontology {OntologyId}",
                    ontologyId);
                return null;
            }

            var link = new OntologyLink
            {
                OntologyId = ontologyId,
                LinkType = LinkType.External,
                Uri = uri,
                Name = label ?? uri,
                UpdatedAt = DateTime.UtcNow
            };

            var createdLink = await _linkRepository.AddAsync(link);
            _logger.LogInformation("User {UserId} created external link {LinkId} to {Uri} for ontology {OntologyId}",
                userId, createdLink.Id, uri, ontologyId);

            return createdLink;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating external link for ontology {OntologyId} to {Uri}",
                ontologyId, uri);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, OntologyLink? Link, string? ErrorMessage)> CreateInternalLinkAsync(
        int parentOntologyId,
        int linkedOntologyId,
        string userId,
        double? positionX = null,
        double? positionY = null,
        string? color = null)
    {
        try
        {
            // 1. Check if user can edit the parent ontology
            var canEdit = await _permissionService.CanEditAsync(parentOntologyId, userId);
            if (!canEdit)
            {
                return (false, null, "You do not have permission to edit this ontology");
            }

            // 2. Check if user can view the linked ontology
            var canView = await _permissionService.CanViewAsync(linkedOntologyId, userId);
            if (!canView)
            {
                return (false, null, "You do not have permission to view the target ontology");
            }

            // 3. Check for self-reference
            if (parentOntologyId == linkedOntologyId)
            {
                return (false, null, "Cannot link an ontology to itself");
            }

            // 4. Check if link already exists
            var linkExists = await _linkRepository.LinkExistsAsync(parentOntologyId, linkedOntologyId);
            if (linkExists)
            {
                return (false, null, "This ontology is already linked");
            }

            // 5. Check for circular dependencies
            var wouldCreateCycle = await WouldCreateCircularDependencyAsync(parentOntologyId, linkedOntologyId);
            if (wouldCreateCycle)
            {
                return (false, null, "Cannot create link: would create circular dependency");
            }

            // 6. Verify both ontologies exist
            var parentExists = await _ontologyRepository.ExistsAsync(parentOntologyId);
            var linkedExists = await _ontologyRepository.ExistsAsync(linkedOntologyId);
            if (!parentExists || !linkedExists)
            {
                return (false, null, "One or both ontologies do not exist");
            }

            // 7. Create the link
            var link = new OntologyLink
            {
                OntologyId = parentOntologyId,
                LinkType = LinkType.Internal,
                LinkedOntologyId = linkedOntologyId,
                PositionX = positionX,
                PositionY = positionY,
                Color = color,
                UpdatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow,
                UpdateAvailable = false
            };

            var createdLink = await _linkRepository.AddAsync(link);
            _logger.LogInformation(
                "User {UserId} created internal link {LinkId} from ontology {ParentId} to ontology {LinkedId}",
                userId, createdLink.Id, parentOntologyId, linkedOntologyId);

            return (true, createdLink, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating internal link from ontology {ParentId} to ontology {LinkedId}",
                parentOntologyId, linkedOntologyId);
            return (false, null, "An error occurred while creating the link");
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OntologyLink>> GetLinksForOntologyAsync(int ontologyId)
    {
        try
        {
            return await _linkRepository.GetByOntologyIdAsync(ontologyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting links for ontology {OntologyId}", ontologyId);
            return Enumerable.Empty<OntologyLink>();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OntologyLink>> GetInternalLinksAsync(int ontologyId)
    {
        try
        {
            return await _linkRepository.GetInternalLinksByOntologyIdAsync(ontologyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting internal links for ontology {OntologyId}", ontologyId);
            return Enumerable.Empty<OntologyLink>();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OntologyLink>> GetExternalLinksAsync(int ontologyId)
    {
        try
        {
            return await _linkRepository.GetExternalLinksByOntologyIdAsync(ontologyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external links for ontology {OntologyId}", ontologyId);
            return Enumerable.Empty<OntologyLink>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteLinkAsync(int linkId, string userId)
    {
        try
        {
            var link = await _linkRepository.GetByIdAsync(linkId);
            if (link == null)
            {
                _logger.LogWarning("Link {LinkId} not found", linkId);
                return false;
            }

            // Check if user has edit permission on the parent ontology
            var canEdit = await _permissionService.CanEditAsync(link.OntologyId, userId);
            if (!canEdit)
            {
                _logger.LogWarning("User {UserId} does not have permission to delete link {LinkId}",
                    userId, linkId);
                return false;
            }

            await _linkRepository.DeleteAsync(linkId);
            _logger.LogInformation("User {UserId} deleted link {LinkId} from ontology {OntologyId}",
                userId, linkId, link.OntologyId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting link {LinkId}", linkId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateLinkPositionAsync(int linkId, double positionX, double positionY, string userId)
    {
        try
        {
            var link = await _linkRepository.GetByIdAsync(linkId);
            if (link == null)
            {
                return false;
            }

            var canEdit = await _permissionService.CanEditAsync(link.OntologyId, userId);
            if (!canEdit)
            {
                return false;
            }

            link.PositionX = positionX;
            link.PositionY = positionY;
            link.UpdatedAt = DateTime.UtcNow;

            await _linkRepository.UpdateAsync(link);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating position for link {LinkId}", linkId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateLinkColorAsync(int linkId, string color, string userId)
    {
        try
        {
            var link = await _linkRepository.GetByIdAsync(linkId);
            if (link == null)
            {
                return false;
            }

            var canEdit = await _permissionService.CanEditAsync(link.OntologyId, userId);
            if (!canEdit)
            {
                return false;
            }

            link.Color = color;
            link.UpdatedAt = DateTime.UtcNow;

            await _linkRepository.UpdateAsync(link);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating color for link {LinkId}", linkId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> WouldCreateCircularDependencyAsync(int parentOntologyId, int targetOntologyId)
    {
        try
        {
            // If parent == target, it's a self-reference (circular)
            if (parentOntologyId == targetOntologyId)
            {
                return true;
            }

            // Get all ontologies that the target depends on
            var targetDependencies = await GetDependencyChainAsync(targetOntologyId);

            // If the target depends on the parent (directly or transitively),
            // then linking parent -> target would create a cycle
            return targetDependencies.Contains(parentOntologyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking circular dependency between {ParentId} and {TargetId}",
                parentOntologyId, targetOntologyId);
            // Return true (assume circular) to be safe
            return true;
        }
    }

    /// <inheritdoc/>
    public async Task<HashSet<int>> GetDependencyChainAsync(int ontologyId)
    {
        try
        {
            var dependencies = new HashSet<int>();
            var visited = new HashSet<int>();
            await BuildDependencyChainAsync(ontologyId, dependencies, visited);
            return dependencies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dependency chain for ontology {OntologyId}", ontologyId);
            return new HashSet<int>();
        }
    }

    /// <summary>
    /// Recursive DFS helper to build the full dependency chain
    /// </summary>
    private async Task BuildDependencyChainAsync(int ontologyId, HashSet<int> dependencies, HashSet<int> visited)
    {
        // Prevent infinite loops
        if (visited.Contains(ontologyId))
        {
            return;
        }

        visited.Add(ontologyId);

        // Get all internal links from this ontology
        var links = await _linkRepository.GetInternalLinksByOntologyIdAsync(ontologyId);

        foreach (var link in links)
        {
            if (link.LinkedOntologyId.HasValue)
            {
                var linkedId = link.LinkedOntologyId.Value;

                // Add to dependencies
                dependencies.Add(linkedId);

                // Recursively get dependencies of the linked ontology
                await BuildDependencyChainAsync(linkedId, dependencies, visited);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetDependentOntologiesAsync(int ontologyId)
    {
        try
        {
            return await _linkRepository.GetDependentOntologyIdsAsync(ontologyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dependent ontologies for {OntologyId}", ontologyId);
            return Enumerable.Empty<int>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> MarkUpdateAvailableAsync(int linkId)
    {
        try
        {
            var link = await _linkRepository.GetByIdAsync(linkId);
            if (link == null)
            {
                return false;
            }

            link.UpdateAvailable = true;
            link.UpdatedAt = DateTime.UtcNow;

            await _linkRepository.UpdateAsync(link);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking update available for link {LinkId}", linkId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SyncLinkAsync(int linkId)
    {
        try
        {
            var link = await _linkRepository.GetByIdAsync(linkId);
            if (link == null)
            {
                return false;
            }

            link.UpdateAvailable = false;
            link.LastSyncedAt = DateTime.UtcNow;
            link.UpdatedAt = DateTime.UtcNow;

            await _linkRepository.UpdateAsync(link);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing link {LinkId}", linkId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OntologyLink>> GetLinksNeedingSyncAsync(int? ontologyId = null)
    {
        try
        {
            return await _linkRepository.GetLinksNeedingSyncAsync(ontologyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting links needing sync for ontology {OntologyId}", ontologyId);
            return Enumerable.Empty<OntologyLink>();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Ontology>> GetAvailableOntologiesForLinkingAsync(int ontologyId, string userId)
    {
        try
        {
            // Get all ontologies the user can view
            var allOntologies = await _ontologyRepository.GetAllAsync();
            var availableOntologies = new List<Ontology>();

            foreach (var ontology in allOntologies)
            {
                // Skip the current ontology
                if (ontology.Id == ontologyId)
                {
                    continue;
                }

                // Check if user can view this ontology
                var canView = await _permissionService.CanViewAsync(ontology.Id, userId);
                if (!canView)
                {
                    continue;
                }

                // Check if already linked
                var alreadyLinked = await _linkRepository.LinkExistsAsync(ontologyId, ontology.Id);
                if (alreadyLinked)
                {
                    continue;
                }

                // Check if would create circular dependency
                var wouldCreateCycle = await WouldCreateCircularDependencyAsync(ontologyId, ontology.Id);
                if (wouldCreateCycle)
                {
                    continue;
                }

                availableOntologies.Add(ontology);
            }

            return availableOntologies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting available ontologies for linking to ontology {OntologyId}",
                ontologyId);
            return Enumerable.Empty<Ontology>();
        }
    }

    /// <summary>
    /// Updates the position of an ontology link node in the graph.
    /// This method is lightweight and doesn't trigger activity tracking.
    /// </summary>
    /// <param name="linkId">The ontology link ID</param>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    public async Task UpdatePositionAsync(int linkId, double x, double y)
    {
        var link = await _linkRepository.GetByIdAsync(linkId);
        if (link == null)
        {
            _logger.LogWarning("OntologyLink {LinkId} not found for position update", linkId);
            throw new InvalidOperationException($"OntologyLink {linkId} not found");
        }

        link.PositionX = x;
        link.PositionY = y;

        await _linkRepository.UpdateAsync(link);
    }
}
