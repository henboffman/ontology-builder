using Eidos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eidos.Data.Repositories
{
    /// <summary>
    /// Repository for tag data access operations
    /// Manages workspace-scoped tags for note organization
    /// Uses DbContextFactory to ensure each operation gets its own DbContext instance,
    /// preventing concurrency issues in Blazor Server applications
    /// </summary>
    public class TagRepository
    {
        private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
        private readonly ILogger<TagRepository> _logger;

        public TagRepository(
            IDbContextFactory<OntologyDbContext> contextFactory,
            ILogger<TagRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Get tag by ID
        /// </summary>
        public async Task<Tag?> GetByIdAsync(int id)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.Tags
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag {TagId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get all tags in a workspace
        /// </summary>
        public async Task<List<Tag>> GetByWorkspaceIdAsync(int workspaceId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.Tags
                    .Where(t => t.WorkspaceId == workspaceId)
                    .OrderBy(t => t.Name)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tags for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Get tag by name in a workspace (for uniqueness check)
        /// </summary>
        public async Task<Tag?> GetByNameAsync(int workspaceId, string name)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.Tags
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag '{TagName}' in workspace {WorkspaceId}", name, workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Get tags with their note assignment counts
        /// </summary>
        public async Task<List<(Tag Tag, int NoteCount)>> GetTagsWithCountsAsync(int workspaceId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Use GroupJoin to efficiently load tags with counts in a single query
                var tagsWithCounts = await (
                    from tag in context.Tags.Where(t => t.WorkspaceId == workspaceId)
                    join assignment in context.NoteTagAssignments on tag.Id equals assignment.TagId into assignments
                    select new
                    {
                        Tag = tag,
                        NoteCount = assignments.Count()
                    }
                )
                .OrderBy(t => t.Tag.Name)
                .AsNoTracking()
                .ToListAsync();

                return tagsWithCounts
                    .Select(tc => (tc.Tag, tc.NoteCount))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tags with counts for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Create a new tag
        /// </summary>
        public async Task<Tag> CreateAsync(Tag tag)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                tag.CreatedAt = DateTime.UtcNow;

                context.Tags.Add(tag);
                await context.SaveChangesAsync();

                _logger.LogInformation("Created tag {TagId} '{TagName}' in workspace {WorkspaceId}",
                    tag.Id, tag.Name, tag.WorkspaceId);

                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag '{TagName}' in workspace {WorkspaceId}",
                    tag.Name, tag.WorkspaceId);
                throw;
            }
        }

        /// <summary>
        /// Update an existing tag
        /// </summary>
        public async Task UpdateAsync(Tag tag)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                context.Tags.Update(tag);
                await context.SaveChangesAsync();

                _logger.LogInformation("Updated tag {TagId}", tag.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag {TagId}", tag.Id);
                throw;
            }
        }

        /// <summary>
        /// Delete a tag
        /// Note: Due to DeleteBehavior.Restrict on NoteTagAssignment -> Tag,
        /// all tag assignments must be manually deleted first
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var tag = await context.Tags.FindAsync(id);
                if (tag == null)
                {
                    return false;
                }

                // Check if tag has any assignments
                var hasAssignments = await context.NoteTagAssignments
                    .AnyAsync(nta => nta.TagId == id);

                if (hasAssignments)
                {
                    throw new InvalidOperationException(
                        $"Cannot delete tag '{tag.Name}' because it is assigned to one or more notes. " +
                        "Remove all assignments first.");
                }

                context.Tags.Remove(tag);
                await context.SaveChangesAsync();

                _logger.LogInformation("Deleted tag {TagId} '{TagName}'", id, tag.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag {TagId}", id);
                throw;
            }
        }

        /// <summary>
        /// Delete a tag and all its assignments
        /// </summary>
        public async Task<bool> DeleteWithAssignmentsAsync(int id)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var tag = await context.Tags.FindAsync(id);
                if (tag == null)
                {
                    return false;
                }

                // Delete all assignments first (due to DeleteBehavior.Restrict)
                var assignments = await context.NoteTagAssignments
                    .Where(nta => nta.TagId == id)
                    .ToListAsync();

                if (assignments.Any())
                {
                    context.NoteTagAssignments.RemoveRange(assignments);
                }

                // Now delete the tag
                context.Tags.Remove(tag);
                await context.SaveChangesAsync();

                _logger.LogInformation("Deleted tag {TagId} '{TagName}' and {AssignmentCount} assignments",
                    id, tag.Name, assignments.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag {TagId} with assignments", id);
                throw;
            }
        }

        /// <summary>
        /// Check if a tag name exists in a workspace (for validation)
        /// </summary>
        public async Task<bool> ExistsAsync(int workspaceId, string name, int? excludeTagId = null)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var query = context.Tags
                    .Where(t => t.WorkspaceId == workspaceId && t.Name == name);

                if (excludeTagId.HasValue)
                {
                    query = query.Where(t => t.Id != excludeTagId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking tag existence for '{TagName}' in workspace {WorkspaceId}",
                    name, workspaceId);
                throw;
            }
        }
    }
}
