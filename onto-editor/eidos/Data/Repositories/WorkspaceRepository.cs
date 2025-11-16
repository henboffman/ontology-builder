using Eidos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eidos.Data.Repositories
{
    /// <summary>
    /// Repository for workspace data access operations
    /// Follows the repository pattern for consistent data access and error handling
    /// </summary>
    public class WorkspaceRepository
    {
        private readonly OntologyDbContext _context;
        private readonly ILogger<WorkspaceRepository> _logger;

        public WorkspaceRepository(
            OntologyDbContext context,
            ILogger<WorkspaceRepository> _logger)
        {
            _context = context;
            this._logger = _logger;
        }

        /// <summary>
        /// Get workspace by ID with optional includes
        /// </summary>
        public async Task<Workspace?> GetByIdAsync(int id, bool includeOntology = false, bool includeNotes = false)
        {
            try
            {
                var query = _context.Workspaces.AsQueryable();

                if (includeOntology)
                {
                    query = query.Include(w => w.Ontology);
                }

                if (includeNotes)
                {
                    query = query.Include(w => w.Notes);
                }

                return await query.FirstOrDefaultAsync(w => w.Id == id);
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
                return await _context.Workspaces
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
                return await _context.Workspaces
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
                workspace.CreatedAt = DateTime.UtcNow;
                workspace.UpdatedAt = DateTime.UtcNow;

                _context.Workspaces.Add(workspace);
                await _context.SaveChangesAsync();

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
                workspace.UpdatedAt = DateTime.UtcNow;

                _context.Workspaces.Update(workspace);
                await _context.SaveChangesAsync();

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
                var workspace = await _context.Workspaces.FindAsync(id);
                if (workspace != null)
                {
                    _context.Workspaces.Remove(workspace);
                    await _context.SaveChangesAsync();

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
        /// Update denormalized note counts
        /// </summary>
        public async Task UpdateNoteCountsAsync(int workspaceId)
        {
            try
            {
                var workspace = await _context.Workspaces.FindAsync(workspaceId);
                if (workspace == null) return;

                workspace.NoteCount = await _context.Notes
                    .CountAsync(n => n.WorkspaceId == workspaceId);

                workspace.ConceptNoteCount = await _context.Notes
                    .CountAsync(n => n.WorkspaceId == workspaceId && n.IsConceptNote);

                workspace.UserNoteCount = await _context.Notes
                    .CountAsync(n => n.WorkspaceId == workspaceId && !n.IsConceptNote);

                workspace.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogDebug("Updated note counts for workspace {WorkspaceId}", workspaceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note counts for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Check if user has access to workspace
        /// </summary>
        public async Task<bool> UserHasAccessAsync(int workspaceId, string userId)
        {
            try
            {
                // Owner check
                var isOwner = await _context.Workspaces
                    .AnyAsync(w => w.Id == workspaceId && w.UserId == userId);

                if (isOwner) return true;

                // Direct user access check
                var hasDirectAccess = await _context.WorkspaceUserAccesses
                    .AnyAsync(wua => wua.WorkspaceId == workspaceId && wua.SharedWithUserId == userId);

                if (hasDirectAccess) return true;

                // Group access check
                var userGroupIds = await _context.UserGroupMembers
                    .Where(ugm => ugm.UserId == userId)
                    .Select(ugm => ugm.UserGroupId)
                    .ToListAsync();

                if (userGroupIds.Any())
                {
                    var hasGroupAccess = await _context.WorkspaceGroupPermissions
                        .AnyAsync(wgp => wgp.WorkspaceId == workspaceId && userGroupIds.Contains(wgp.UserGroupId));

                    if (hasGroupAccess) return true;
                }

                // Public workspace check
                var isPublic = await _context.Workspaces
                    .AnyAsync(w => w.Id == workspaceId && w.Visibility == "public");

                return isPublic;
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
