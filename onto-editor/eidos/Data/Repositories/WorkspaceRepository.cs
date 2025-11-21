using Eidos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eidos.Data.Repositories
{
    /// <summary>
    /// Repository for workspace data access operations
    /// Follows the repository pattern for consistent data access and error handling
    /// Uses DbContextFactory to ensure each operation gets its own DbContext instance,
    /// preventing concurrency issues in Blazor Server applications
    /// </summary>
    public class WorkspaceRepository
    {
        private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
        private readonly ILogger<WorkspaceRepository> _logger;

        public WorkspaceRepository(
            IDbContextFactory<OntologyDbContext> contextFactory,
            ILogger<WorkspaceRepository> _logger)
        {
            _contextFactory = contextFactory;
            this._logger = _logger;
        }

        /// <summary>
        /// Get workspace by ID with optional includes
        /// </summary>
        public async Task<Workspace?> GetByIdAsync(int id, bool includeOntology = false, bool includeNotes = false)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var workspace = await context.Workspaces
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == id);

                if (workspace == null)
                {
                    return null;
                }

                if (includeOntology)
                {
                    // Load ontology separately since FK is on Ontology side
                    workspace.Ontology = await context.Ontologies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.WorkspaceId == id);
                }

                if (includeNotes)
                {
                    workspace.Notes = await context.Notes
                        .Where(n => n.WorkspaceId == id)
                        .AsNoTracking()
                        .ToListAsync();
                }

                return workspace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace {WorkspaceId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get all workspaces for a user
        /// </summary>
        public async Task<List<Workspace>> GetByUserIdAsync(string userId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.Workspaces
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.UpdatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspaces for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get public workspaces (for discovery)
        /// </summary>
        public async Task<List<Workspace>> GetPublicWorkspacesAsync(int skip = 0, int take = 20)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.Workspaces
                    .Where(w => w.Visibility == "public")
                    .OrderByDescending(w => w.UpdatedAt)
                    .Skip(skip)
                    .Take(take)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public workspaces");
                throw;
            }
        }

        /// <summary>
        /// Create a new workspace
        /// </summary>
        public async Task<Workspace> CreateAsync(Workspace workspace)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                workspace.CreatedAt = DateTime.UtcNow;
                workspace.UpdatedAt = DateTime.UtcNow;

                context.Workspaces.Add(workspace);
                await context.SaveChangesAsync();

                _logger.LogInformation("Created workspace {WorkspaceId} for user {UserId}",
                    workspace.Id, workspace.UserId);

                return workspace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workspace for user {UserId}", workspace.UserId);
                throw;
            }
        }

        /// <summary>
        /// Update an existing workspace
        /// </summary>
        public async Task<Workspace> UpdateAsync(Workspace workspace)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                workspace.UpdatedAt = DateTime.UtcNow;

                context.Workspaces.Update(workspace);
                await context.SaveChangesAsync();

                _logger.LogInformation("Updated workspace {WorkspaceId}", workspace.Id);

                return workspace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workspace {WorkspaceId}", workspace.Id);
                throw;
            }
        }

        /// <summary>
        /// Delete a workspace (cascades to notes)
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var workspace = await context.Workspaces.FindAsync(id);
                if (workspace != null)
                {
                    context.Workspaces.Remove(workspace);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Deleted workspace {WorkspaceId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workspace {WorkspaceId}", id);
                throw;
            }
        }

        /// <summary>
        /// Update denormalized note counts (optimized with single query)
        /// </summary>
        public async Task UpdateNoteCountsAsync(int workspaceId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Calculate all counts in a single query and update directly
                // This is much more efficient than loading the entity and making 3 separate count queries
                var counts = await context.Notes
                    .Where(n => n.WorkspaceId == workspaceId)
                    .GroupBy(n => 1) // Group all records together
                    .Select(g => new
                    {
                        TotalCount = g.Count(),
                        ConceptNoteCount = g.Count(n => n.IsConceptNote),
                        UserNoteCount = g.Count(n => !n.IsConceptNote)
                    })
                    .FirstOrDefaultAsync();

                // If no notes exist, set counts to 0
                var totalCount = counts?.TotalCount ?? 0;
                var conceptNoteCount = counts?.ConceptNoteCount ?? 0;
                var userNoteCount = counts?.UserNoteCount ?? 0;

                // Update workspace counts
                // Note: Using load-modify-save pattern for compatibility with in-memory database tests
                var workspace = await context.Workspaces.FindAsync(workspaceId);
                if (workspace != null)
                {
                    workspace.NoteCount = totalCount;
                    workspace.ConceptNoteCount = conceptNoteCount;
                    workspace.UserNoteCount = userNoteCount;
                    workspace.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }

                _logger.LogDebug("Updated note counts for workspace {WorkspaceId}: Total={TotalCount}, Concept={ConceptCount}, User={UserCount}",
                    workspaceId, totalCount, conceptNoteCount, userNoteCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note counts for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Check if user has access to workspace (optimized with share link support)
        /// </summary>
        public async Task<bool> UserHasAccessAsync(int workspaceId, string userId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // First check standard access (owner, public, direct, group) using LEFT JOIN to get OntologyId
                var hasStandardAccess = await context.Workspaces
                    .Where(w => w.Id == workspaceId)
                    .GroupJoin(
                        context.Ontologies,
                        w => w.Id,
                        o => o.WorkspaceId,
                        (w, ontologies) => new { Workspace = w, Ontology = ontologies.FirstOrDefault() })
                    .Select(joined => new
                    {
                        HasAccess =
                            // Owner check
                            joined.Workspace.UserId == userId ||
                            // Public check
                            joined.Workspace.Visibility == "public" ||
                            // Direct user access
                            joined.Workspace.UserAccesses.Any(ua => ua.SharedWithUserId == userId) ||
                            // Group access (user is member of group that has permission)
                            joined.Workspace.GroupPermissions.Any(gp =>
                                gp.UserGroup.Members.Any(m => m.UserId == userId)
                            ),
                        OntologyId = joined.Ontology != null ? (int?)joined.Ontology.Id : null
                    })
                    .FirstOrDefaultAsync();

                if (hasStandardAccess == null)
                {
                    _logger.LogWarning("Workspace {WorkspaceId} not found for user {UserId} access check", workspaceId, userId);
                    return false;
                }

                if (hasStandardAccess.HasAccess)
                {
                    _logger.LogDebug("User {UserId} has standard access to workspace {WorkspaceId}", userId, workspaceId);
                    return true;
                }

                // If no standard access and workspace has an ontology, check share link access
                if (hasStandardAccess.OntologyId.HasValue)
                {
                    _logger.LogDebug("Checking share link access for user {UserId} to workspace {WorkspaceId} via ontology {OntologyId}",
                        userId, workspaceId, hasStandardAccess.OntologyId.Value);

                    var hasShareLinkAccess = await context.UserShareAccesses
                        .AnyAsync(usa => usa.OntologyShare != null &&
                                        usa.OntologyShare.OntologyId == hasStandardAccess.OntologyId.Value &&
                                        usa.UserId == userId);

                    if (hasShareLinkAccess)
                    {
                        _logger.LogInformation("User {UserId} granted workspace {WorkspaceId} access via ontology share link", userId, workspaceId);
                    }
                    else
                    {
                        _logger.LogWarning("User {UserId} has NO share link access to workspace {WorkspaceId} (OntologyId: {OntologyId})",
                            userId, workspaceId, hasStandardAccess.OntologyId.Value);
                    }

                    return hasShareLinkAccess;
                }
                else
                {
                    _logger.LogWarning("Workspace {WorkspaceId} has NO linked ontology, cannot check share link access for user {UserId}",
                        workspaceId, userId);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking access for user {UserId} to workspace {WorkspaceId}",
                    userId, workspaceId);
                throw;
            }
        }
    }
}
