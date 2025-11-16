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
                var query = context.Workspaces.AsQueryable();

                if (includeOntology)
                {
                    query = query.Include(w => w.Ontology);
                }

                if (includeNotes)
                {
                    query = query.Include(w => w.Notes);
                }

                return await query.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id);
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

                // Update workspace counts directly in database without loading entity
                await context.Workspaces
                    .Where(w => w.Id == workspaceId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(w => w.NoteCount, totalCount)
                        .SetProperty(w => w.ConceptNoteCount, conceptNoteCount)
                        .SetProperty(w => w.UserNoteCount, userNoteCount)
                        .SetProperty(w => w.UpdatedAt, DateTime.UtcNow));

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
        /// Check if user has access to workspace (optimized to single query)
        /// </summary>
        public async Task<bool> UserHasAccessAsync(int workspaceId, string userId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Single query with all access checks combined
                var hasAccess = await context.Workspaces
                    .Where(w => w.Id == workspaceId)
                    .Select(w =>
                        // Owner check
                        w.UserId == userId ||
                        // Public check
                        w.Visibility == "public" ||
                        // Direct user access
                        w.UserAccesses.Any(ua => ua.SharedWithUserId == userId) ||
                        // Group access (user is member of group that has permission)
                        w.GroupPermissions.Any(gp =>
                            gp.UserGroup.Members.Any(m => m.UserId == userId)
                        )
                    )
                    .FirstOrDefaultAsync();

                return hasAccess;
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
