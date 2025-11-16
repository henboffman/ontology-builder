using Eidos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eidos.Data.Repositories
{
    /// <summary>
    /// Repository for note-tag assignment operations
    /// Manages many-to-many relationships between notes and tags
    /// Uses DbContextFactory to ensure each operation gets its own DbContext instance,
    /// preventing concurrency issues in Blazor Server applications
    /// </summary>
    public class NoteTagAssignmentRepository
    {
        private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
        private readonly ILogger<NoteTagAssignmentRepository> _logger;

        public NoteTagAssignmentRepository(
            IDbContextFactory<OntologyDbContext> contextFactory,
            ILogger<NoteTagAssignmentRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Get all tag assignments for a note
        /// </summary>
        public async Task<List<NoteTagAssignment>> GetByNoteIdAsync(int noteId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.NoteTagAssignments
                    .Where(nta => nta.NoteId == noteId)
                    .Include(nta => nta.Tag)
                    .OrderBy(nta => nta.Tag.Name)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag assignments for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Get all notes assigned to a tag
        /// </summary>
        public async Task<List<NoteTagAssignment>> GetByTagIdAsync(int tagId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.NoteTagAssignments
                    .Where(nta => nta.TagId == tagId)
                    .Include(nta => nta.Note)
                    .OrderByDescending(nta => nta.Note.UpdatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note assignments for tag {TagId}", tagId);
                throw;
            }
        }

        /// <summary>
        /// Get all notes with a specific tag (returns just the notes)
        /// </summary>
        public async Task<List<Note>> GetNotesByTagIdAsync(int tagId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.NoteTagAssignments
                    .Where(nta => nta.TagId == tagId)
                    .Select(nta => nta.Note)
                    .OrderByDescending(n => n.UpdatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes for tag {TagId}", tagId);
                throw;
            }
        }

        /// <summary>
        /// Get all tags for a note (returns just the tags)
        /// </summary>
        public async Task<List<Tag>> GetTagsByNoteIdAsync(int noteId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.NoteTagAssignments
                    .Where(nta => nta.NoteId == noteId)
                    .Select(nta => nta.Tag)
                    .OrderBy(t => t.Name)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tags for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Assign a tag to a note
        /// </summary>
        public async Task<NoteTagAssignment> AssignAsync(int noteId, int tagId, string userId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Check if assignment already exists
                var existing = await context.NoteTagAssignments
                    .FirstOrDefaultAsync(nta => nta.NoteId == noteId && nta.TagId == tagId);

                if (existing != null)
                {
                    _logger.LogWarning("Tag {TagId} already assigned to note {NoteId}", tagId, noteId);
                    return existing;
                }

                var assignment = new NoteTagAssignment
                {
                    NoteId = noteId,
                    TagId = tagId,
                    AssignedBy = userId,
                    AssignedAt = DateTime.UtcNow
                };

                context.NoteTagAssignments.Add(assignment);
                await context.SaveChangesAsync();

                _logger.LogInformation("Assigned tag {TagId} to note {NoteId} by user {UserId}",
                    tagId, noteId, userId);

                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning tag {TagId} to note {NoteId}", tagId, noteId);
                throw;
            }
        }

        /// <summary>
        /// Assign multiple tags to a note at once
        /// </summary>
        public async Task AssignMultipleAsync(int noteId, List<int> tagIds, string userId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Get existing assignments to avoid duplicates
                var existingTagIds = await context.NoteTagAssignments
                    .Where(nta => nta.NoteId == noteId)
                    .Select(nta => nta.TagId)
                    .ToListAsync();

                // Filter out tags that are already assigned
                var newTagIds = tagIds.Except(existingTagIds).ToList();

                if (!newTagIds.Any())
                {
                    _logger.LogInformation("All tags already assigned to note {NoteId}", noteId);
                    return;
                }

                var assignments = newTagIds.Select(tagId => new NoteTagAssignment
                {
                    NoteId = noteId,
                    TagId = tagId,
                    AssignedBy = userId,
                    AssignedAt = DateTime.UtcNow
                }).ToList();

                context.NoteTagAssignments.AddRange(assignments);
                await context.SaveChangesAsync();

                _logger.LogInformation("Assigned {Count} tags to note {NoteId} by user {UserId}",
                    newTagIds.Count, noteId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning multiple tags to note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Unassign a tag from a note
        /// </summary>
        public async Task<bool> UnassignAsync(int noteId, int tagId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var assignment = await context.NoteTagAssignments
                    .FirstOrDefaultAsync(nta => nta.NoteId == noteId && nta.TagId == tagId);

                if (assignment == null)
                {
                    _logger.LogWarning("Tag assignment not found: Tag {TagId} on Note {NoteId}", tagId, noteId);
                    return false;
                }

                context.NoteTagAssignments.Remove(assignment);
                await context.SaveChangesAsync();

                _logger.LogInformation("Unassigned tag {TagId} from note {NoteId}", tagId, noteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning tag {TagId} from note {NoteId}", tagId, noteId);
                throw;
            }
        }

        /// <summary>
        /// Replace all tag assignments for a note (atomic operation)
        /// </summary>
        public async Task ReplaceAssignmentsAsync(int noteId, List<int> tagIds, string userId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Remove existing assignments
                var existingAssignments = await context.NoteTagAssignments
                    .Where(nta => nta.NoteId == noteId)
                    .ToListAsync();

                context.NoteTagAssignments.RemoveRange(existingAssignments);

                // Add new assignments
                if (tagIds.Any())
                {
                    var newAssignments = tagIds.Select(tagId => new NoteTagAssignment
                    {
                        NoteId = noteId,
                        TagId = tagId,
                        AssignedBy = userId,
                        AssignedAt = DateTime.UtcNow
                    }).ToList();

                    context.NoteTagAssignments.AddRange(newAssignments);
                }

                await context.SaveChangesAsync();

                _logger.LogInformation("Replaced tag assignments for note {NoteId}: {Count} tags",
                    noteId, tagIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing tag assignments for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Remove all tag assignments for a note
        /// </summary>
        public async Task RemoveAllByNoteIdAsync(int noteId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var assignments = await context.NoteTagAssignments
                    .Where(nta => nta.NoteId == noteId)
                    .ToListAsync();

                if (assignments.Any())
                {
                    context.NoteTagAssignments.RemoveRange(assignments);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Removed {Count} tag assignments from note {NoteId}",
                        assignments.Count, noteId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing all tag assignments for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Remove all note assignments for a tag
        /// Used when deleting a tag
        /// </summary>
        public async Task RemoveAllByTagIdAsync(int tagId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var assignments = await context.NoteTagAssignments
                    .Where(nta => nta.TagId == tagId)
                    .ToListAsync();

                if (assignments.Any())
                {
                    context.NoteTagAssignments.RemoveRange(assignments);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Removed {Count} note assignments from tag {TagId}",
                        assignments.Count, tagId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing all note assignments for tag {TagId}", tagId);
                throw;
            }
        }

        /// <summary>
        /// Check if a tag is assigned to a note
        /// </summary>
        public async Task<bool> IsAssignedAsync(int noteId, int tagId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.NoteTagAssignments
                    .AnyAsync(nta => nta.NoteId == noteId && nta.TagId == tagId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if tag {TagId} is assigned to note {NoteId}", tagId, noteId);
                throw;
            }
        }

        /// <summary>
        /// Get tag assignment count for a tag
        /// </summary>
        public async Task<int> GetAssignmentCountAsync(int tagId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.NoteTagAssignments
                    .CountAsync(nta => nta.TagId == tagId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignment count for tag {TagId}", tagId);
                throw;
            }
        }
    }
}
