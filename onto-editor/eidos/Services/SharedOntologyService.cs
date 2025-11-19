using Eidos.Data;
using Eidos.Models;
using Eidos.Models.DTOs;
using Eidos.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Eidos.Services
{
    /// <summary>
    /// Service for managing shared ontology access and user state
    /// Provides unified view of ontologies shared via both share links and group membership
    /// </summary>
    public class SharedOntologyService
    {
        private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
        private readonly ILogger<SharedOntologyService> _logger;
        private readonly IMemoryCache _cache;
        private const string CACHE_KEY_PREFIX = "SharedOntologies_";
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public SharedOntologyService(
            IDbContextFactory<OntologyDbContext> contextFactory,
            ILogger<SharedOntologyService> logger,
            IMemoryCache cache)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Get all shared ontologies for a user with filtering, sorting, and pagination
        /// Combines both share link and group-based access
        /// </summary>
        public async Task<SharedOntologyResult> GetSharedOntologiesAsync(
            string userId,
            SharedOntologyFilter filter,
            int page = 1,
            int pageSize = 24)
        {
            try
            {
                // Try cache first
                var cacheKey = $"{CACHE_KEY_PREFIX}{userId}_{filter.GetHashCode()}_{page}_{pageSize}";
                if (_cache.TryGetValue<SharedOntologyResult>(cacheKey, out var cachedResult) && cachedResult != null)
                {
                    _logger.LogDebug("Returning cached shared ontologies for user {UserId}", userId);
                    return cachedResult;
                }

                await using var context = await _contextFactory.CreateDbContextAsync();

                // Calculate cutoff date for recent access
                var cutoffDate = DateTime.UtcNow.AddDays(-filter.DaysBack);

                // Query share link ontologies - materialize separately
                var shareLinkResults = await context.UserShareAccesses
                    .Where(usa => usa.UserId == userId &&
                                  usa.OntologyShare != null &&
                                  usa.OntologyShare.IsActive &&
                                  (!usa.OntologyShare.ExpiresAt.HasValue ||
                                   usa.OntologyShare.ExpiresAt.Value > DateTime.UtcNow) &&
                                  usa.OntologyShare.Ontology!.UserId != userId) // Exclude own ontologies
                    .Select(usa => new
                    {
                        OntologyId = usa.OntologyShare!.OntologyId,
                        Ontology = usa.OntologyShare.Ontology!,
                        PermissionLevel = usa.OntologyShare.PermissionLevel,
                        LastAccessedAt = (DateTime?)usa.LastAccessedAt, // Cast to nullable to match group results
                        AccessType = SharedAccessType.ShareLink,
                        GroupName = (string?)null,
                        GroupMemberCount = (int?)null
                    })
                    .AsNoTracking()
                    .ToListAsync();

                // Query group-based ontologies - materialize separately
                var groupResults = await context.UserGroupMembers
                    .Where(ugm => ugm.UserId == userId &&
                                  ugm.UserGroup.IsActive)
                    .SelectMany(ugm => ugm.UserGroup.OntologyPermissions
                        .Where(ogp => ogp.Ontology.UserId != userId), // Exclude own ontologies
                        (ugm, ogp) => new
                        {
                            OntologyId = ogp.OntologyId,
                            Ontology = ogp.Ontology,
                            PermissionLevelString = ogp.PermissionLevel,
                            LastAccessedAt = ogp.LastAccessedAt,
                            AccessType = SharedAccessType.Group,
                            GroupName = ogp.UserGroup.Name,
                            GroupMemberCount = (int?)ogp.UserGroup.Members.Count
                        })
                    .AsNoTracking()
                    .ToListAsync();

                // Convert group results to use PermissionLevel enum
                var groupResultsConverted = groupResults.Select(g => new
                {
                    g.OntologyId,
                    g.Ontology,
                    PermissionLevel = ParsePermissionLevel(g.PermissionLevelString),
                    g.LastAccessedAt,
                    g.AccessType,
                    g.GroupName,
                    g.GroupMemberCount
                }).ToList();

                // Combine both result sets in memory
                var allResults = shareLinkResults
                    .Concat(groupResultsConverted)
                    .ToList();

                // Apply filters in memory
                if (filter.DaysBack > 0)
                {
                    allResults = allResults
                        .Where(x => x.LastAccessedAt.HasValue && x.LastAccessedAt.Value >= cutoffDate)
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var searchLower = filter.SearchTerm.ToLower();
                    allResults = allResults
                        .Where(x =>
                            x.Ontology.Name.ToLower().Contains(searchLower) ||
                            (x.Ontology.Description != null && x.Ontology.Description.ToLower().Contains(searchLower)))
                        .ToList();
                }

                if (filter.AccessTypeFilter.HasValue && filter.AccessTypeFilter.Value != SharedAccessType.Both)
                {
                    allResults = allResults
                        .Where(x => x.AccessType == filter.AccessTypeFilter.Value)
                        .ToList();
                }

                // Get user states for filtering and display
                var userStates = await context.SharedOntologyUserStates
                    .Where(s => s.UserId == userId)
                    .AsNoTracking()
                    .ToListAsync();

                var userStatesDict = userStates.ToDictionary(s => s.OntologyId, s => s);

                // Group by OntologyId and handle "Both" access type
                var groupedResults = allResults
                    .GroupBy(x => x.OntologyId)
                    .Select(g =>
                    {
                        var first = g.First();
                        var hasShareLink = g.Any(x => x.AccessType == SharedAccessType.ShareLink);
                        var hasGroup = g.Any(x => x.AccessType == SharedAccessType.Group);

                        // Determine access type
                        var accessType = (hasShareLink && hasGroup) ? SharedAccessType.Both :
                                       hasShareLink ? SharedAccessType.ShareLink :
                                       SharedAccessType.Group;

                        // Take highest permission level if multiple access methods
                        var maxPermission = g.Max(x => x.PermissionLevel);

                        // Take most recent access time
                        var lastAccessed = g.Where(x => x.LastAccessedAt.HasValue)
                                           .OrderByDescending(x => x.LastAccessedAt)
                                           .FirstOrDefault()?.LastAccessedAt;

                        // Get group name (prefer first group if multiple)
                        var groupName = g.FirstOrDefault(x => x.AccessType == SharedAccessType.Group)?.GroupName;
                        var groupMemberCount = g.FirstOrDefault(x => x.AccessType == SharedAccessType.Group)?.GroupMemberCount;

                        // Get user state
                        userStatesDict.TryGetValue(first.OntologyId, out var userState);

                        return new SharedOntologyInfo
                        {
                            OntologyId = first.OntologyId,
                            Name = first.Ontology.Name,
                            Description = first.Ontology.Description,
                            OwnerId = first.Ontology.UserId,
                            OwnerName = first.Ontology.User?.DisplayName ?? "Unknown",
                            OwnerPhotoUrl = null, // Photo URL not currently stored in ApplicationUser
                            AccessType = accessType,
                            PermissionLevel = maxPermission,
                            LastAccessedAt = lastAccessed,
                            ConceptCount = first.Ontology.ConceptCount,
                            RelationshipCount = first.Ontology.RelationshipCount,
                            CreatedAt = first.Ontology.CreatedAt,
                            UpdatedAt = first.Ontology.UpdatedAt,
                            IsPinned = userState?.IsPinned ?? false,
                            IsHidden = userState?.IsHidden ?? false,
                            PinnedAt = userState?.PinnedAt,
                            GroupName = groupName,
                            GroupMemberCount = groupMemberCount
                        };
                    })
                    .ToList();

                // Apply hidden/pinned filters
                if (!filter.IncludeHidden)
                {
                    groupedResults = groupedResults.Where(x => !x.IsHidden).ToList();
                }

                if (filter.PinnedOnly)
                {
                    groupedResults = groupedResults.Where(x => x.IsPinned).ToList();
                }

                // Apply sorting
                groupedResults = filter.SortBy switch
                {
                    SharedOntologySortBy.LastAccessed => groupedResults
                        .OrderByDescending(x => x.LastAccessedAt ?? DateTime.MinValue)
                        .ToList(),
                    SharedOntologySortBy.Name => groupedResults
                        .OrderBy(x => x.Name)
                        .ToList(),
                    SharedOntologySortBy.ConceptCount => groupedResults
                        .OrderByDescending(x => x.ConceptCount)
                        .ToList(),
                    SharedOntologySortBy.PinnedFirst => groupedResults
                        .OrderByDescending(x => x.IsPinned)
                        .ThenByDescending(x => x.LastAccessedAt ?? DateTime.MinValue)
                        .ToList(),
                    _ => groupedResults
                        .OrderByDescending(x => x.LastAccessedAt ?? DateTime.MinValue)
                        .ToList()
                };

                // Apply pagination
                var totalCount = groupedResults.Count;
                var paginatedResults = groupedResults
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new SharedOntologyResult
                {
                    Ontologies = paginatedResults,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                // Cache the result
                _cache.Set(cacheKey, result, _cacheExpiration);

                _logger.LogInformation(
                    "Retrieved {Count} shared ontologies for user {UserId} (page {Page}/{TotalPages})",
                    paginatedResults.Count, userId, page, result.TotalPages);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shared ontologies for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Pin a shared ontology for quick access
        /// </summary>
        public async Task PinOntologyAsync(string userId, int ontologyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var state = await GetOrCreateUserStateAsync(context, userId, ontologyId);
                state.IsPinned = true;
                state.PinnedAt = DateTime.UtcNow;
                state.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                // Invalidate cache
                InvalidateUserCache(userId);

                _logger.LogInformation("User {UserId} pinned ontology {OntologyId}", userId, ontologyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pinning ontology {OntologyId} for user {UserId}", ontologyId, userId);
                throw;
            }
        }

        /// <summary>
        /// Unpin a shared ontology
        /// </summary>
        public async Task UnpinOntologyAsync(string userId, int ontologyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var state = await context.SharedOntologyUserStates
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.OntologyId == ontologyId);

                if (state != null)
                {
                    state.IsPinned = false;
                    state.PinnedAt = null;
                    state.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    // Invalidate cache
                    InvalidateUserCache(userId);

                    _logger.LogInformation("User {UserId} unpinned ontology {OntologyId}", userId, ontologyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpinning ontology {OntologyId} for user {UserId}", ontologyId, userId);
                throw;
            }
        }

        /// <summary>
        /// Hide a shared ontology from the list
        /// </summary>
        public async Task HideOntologyAsync(string userId, int ontologyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var state = await GetOrCreateUserStateAsync(context, userId, ontologyId);
                state.IsHidden = true;
                state.HiddenAt = DateTime.UtcNow;
                state.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                // Invalidate cache
                InvalidateUserCache(userId);

                _logger.LogInformation("User {UserId} hid ontology {OntologyId}", userId, ontologyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hiding ontology {OntologyId} for user {UserId}", ontologyId, userId);
                throw;
            }
        }

        /// <summary>
        /// Unhide a shared ontology
        /// </summary>
        public async Task UnhideOntologyAsync(string userId, int ontologyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var state = await context.SharedOntologyUserStates
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.OntologyId == ontologyId);

                if (state != null)
                {
                    state.IsHidden = false;
                    state.HiddenAt = null;
                    state.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    // Invalidate cache
                    InvalidateUserCache(userId);

                    _logger.LogInformation("User {UserId} unhid ontology {OntologyId}", userId, ontologyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unhiding ontology {OntologyId} for user {UserId}", ontologyId, userId);
                throw;
            }
        }

        /// <summary>
        /// Dismiss a shared ontology (soft delete from shared list)
        /// </summary>
        public async Task DismissOntologyAsync(string userId, int ontologyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var state = await GetOrCreateUserStateAsync(context, userId, ontologyId);
                state.IsDismissed = true;
                state.DismissedAt = DateTime.UtcNow;
                state.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                // Invalidate cache
                InvalidateUserCache(userId);

                _logger.LogInformation("User {UserId} dismissed ontology {OntologyId}", userId, ontologyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing ontology {OntologyId} for user {UserId}", ontologyId, userId);
                throw;
            }
        }

        /// <summary>
        /// Update LastAccessedAt for share link access
        /// </summary>
        public async Task UpdateShareLinkAccessAsync(string userId, int ontologyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var shareAccess = await context.UserShareAccesses
                    .FirstOrDefaultAsync(usa =>
                        usa.UserId == userId &&
                        usa.OntologyShare != null &&
                        usa.OntologyShare.OntologyId == ontologyId);

                if (shareAccess != null)
                {
                    shareAccess.LastAccessedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    // Invalidate cache
                    InvalidateUserCache(userId);

                    _logger.LogDebug("Updated share link access time for user {UserId} on ontology {OntologyId}",
                        userId, ontologyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating share link access for ontology {OntologyId} by user {UserId}",
                    ontologyId, userId);
                // Don't throw - this is a non-critical update
            }
        }

        /// <summary>
        /// Update LastAccessedAt for group permission access
        /// </summary>
        public async Task UpdateGroupAccessAsync(string userId, int ontologyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Find the group permission(s) for this user and ontology
                var groupPermissions = await context.OntologyGroupPermissions
                    .Where(ogp => ogp.OntologyId == ontologyId &&
                                  ogp.UserGroup.Members.Any(m => m.UserId == userId))
                    .ToListAsync();

                if (groupPermissions.Any())
                {
                    foreach (var permission in groupPermissions)
                    {
                        permission.LastAccessedAt = DateTime.UtcNow;
                    }

                    await context.SaveChangesAsync();

                    // Invalidate cache
                    InvalidateUserCache(userId);

                    _logger.LogDebug("Updated group access time for user {UserId} on ontology {OntologyId}",
                        userId, ontologyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group access for ontology {OntologyId} by user {UserId}",
                    ontologyId, userId);
                // Don't throw - this is a non-critical update
            }
        }

        /// <summary>
        /// Get or create user state for an ontology
        /// </summary>
        private async Task<SharedOntologyUserState> GetOrCreateUserStateAsync(
            OntologyDbContext context,
            string userId,
            int ontologyId)
        {
            var state = await context.SharedOntologyUserStates
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OntologyId == ontologyId);

            if (state == null)
            {
                state = new SharedOntologyUserState
                {
                    UserId = userId,
                    OntologyId = ontologyId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.SharedOntologyUserStates.Add(state);
            }

            return state;
        }

        /// <summary>
        /// Reset all user state (unhide all hidden ontologies, optionally unpin all)
        /// </summary>
        public async Task ResetAllUserStateAsync(string userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var userStates = await context.SharedOntologyUserStates
                .Where(s => s.UserId == userId && s.IsHidden)
                .ToListAsync();

            foreach (var state in userStates)
            {
                state.IsHidden = false;
                state.UpdatedAt = DateTime.UtcNow;
            }

            if (userStates.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Reset state for {Count} hidden ontologies for user {UserId}", userStates.Count, userId);
            }

            InvalidateUserCache(userId);
        }

        /// <summary>
        /// Parse string permission level to enum
        /// </summary>
        private static PermissionLevel ParsePermissionLevel(string permissionLevelString)
        {
            return permissionLevelString?.ToLower() switch
            {
                "view" => PermissionLevel.View,
                "viewandadd" => PermissionLevel.ViewAndAdd,
                "viewaddedit" => PermissionLevel.ViewAddEdit,
                "fullaccess" => PermissionLevel.FullAccess,
                _ => PermissionLevel.View // Default to most restrictive
            };
        }

        /// <summary>
        /// Invalidate all cached results for a user
        /// </summary>
        private void InvalidateUserCache(string userId)
        {
            // Since we can't enumerate cache keys in IMemoryCache, we use a simple approach:
            // The cache will naturally expire after 5 minutes
            // For immediate updates, we could implement a more sophisticated cache invalidation strategy
            _logger.LogDebug("Cache invalidation triggered for user {UserId}", userId);
        }
    }
}
