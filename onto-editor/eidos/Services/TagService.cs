using Eidos.Data.Repositories;
using Eidos.Models;
using Microsoft.Extensions.Logging;

namespace Eidos.Services
{
    /// <summary>
    /// Business logic service for tag management
    /// Handles tag CRUD, assignment operations, and workspace-scoped validation
    /// </summary>
    public class TagService
    {
        private readonly TagRepository _tagRepository;
        private readonly NoteTagAssignmentRepository _assignmentRepository;
        private readonly WorkspaceRepository _workspaceRepository;
        private readonly ILogger<TagService> _logger;

        public TagService(
            TagRepository tagRepository,
            NoteTagAssignmentRepository assignmentRepository,
            WorkspaceRepository workspaceRepository,
            ILogger<TagService> logger)
        {
            _tagRepository = tagRepository;
            _assignmentRepository = assignmentRepository;
            _workspaceRepository = workspaceRepository;
            _logger = logger;
        }

        /// <summary>
        /// Create a new tag in a workspace
        /// </summary>
        public async Task<Tag> CreateTagAsync(int workspaceId, string userId, string name, string? color = null, string? description = null)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("Tag name is required", nameof(name));
                }

                // Normalize tag name (trim whitespace)
                name = name.Trim();

                // Validate tag name length
                if (name.Length > 100)
                {
                    throw new ArgumentException("Tag name cannot exceed 100 characters", nameof(name));
                }

                // Check workspace access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);
                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException($"User {userId} does not have access to workspace {workspaceId}");
                }

                // Check for duplicate tag name (case-insensitive)
                var existing = await _tagRepository.ExistsAsync(workspaceId, name);
                if (existing)
                {
                    throw new InvalidOperationException($"Tag '{name}' already exists in this workspace");
                }

                // Validate color format if provided
                if (!string.IsNullOrWhiteSpace(color) && !IsValidHexColor(color))
                {
                    throw new ArgumentException("Color must be a valid hex color (e.g., #3498db)", nameof(color));
                }

                var tag = new Tag
                {
                    WorkspaceId = workspaceId,
                    Name = name,
                    Color = color,
                    Description = description?.Trim(),
                    CreatedBy = userId
                };

                var created = await _tagRepository.CreateAsync(tag);

                _logger.LogInformation("Created tag {TagId} '{TagName}' in workspace {WorkspaceId} by user {UserId}",
                    created.Id, name, workspaceId, userId);

                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag '{TagName}' in workspace {WorkspaceId}", name, workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Update an existing tag
        /// </summary>
        public async Task<Tag> UpdateTagAsync(int tagId, string userId, string? name = null, string? color = null, string? description = null)
        {
            try
            {
                var tag = await _tagRepository.GetByIdAsync(tagId);
                if (tag == null)
                {
                    throw new InvalidOperationException($"Tag {tagId} not found");
                }

                // Check workspace access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(tag.WorkspaceId, userId);
                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException($"User {userId} does not have access to this tag's workspace");
                }

                // Update name if provided
                if (!string.IsNullOrWhiteSpace(name))
                {
                    name = name.Trim();

                    if (name.Length > 100)
                    {
                        throw new ArgumentException("Tag name cannot exceed 100 characters", nameof(name));
                    }

                    // Check for duplicate name (excluding current tag)
                    var exists = await _tagRepository.ExistsAsync(tag.WorkspaceId, name, excludeTagId: tagId);
                    if (exists)
                    {
                        throw new InvalidOperationException($"Tag '{name}' already exists in this workspace");
                    }

                    tag.Name = name;
                }

                // Update color if provided
                if (color != null) // Allow empty string to clear color
                {
                    if (!string.IsNullOrWhiteSpace(color) && !IsValidHexColor(color))
                    {
                        throw new ArgumentException("Color must be a valid hex color (e.g., #3498db)", nameof(color));
                    }

                    tag.Color = string.IsNullOrWhiteSpace(color) ? null : color;
                }

                // Update description if provided
                if (description != null) // Allow empty string to clear description
                {
                    tag.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
                }

                await _tagRepository.UpdateAsync(tag);

                _logger.LogInformation("Updated tag {TagId} in workspace {WorkspaceId}", tagId, tag.WorkspaceId);

                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag {TagId}", tagId);
                throw;
            }
        }

        /// <summary>
        /// Delete a tag and all its assignments
        /// </summary>
        public async Task<bool> DeleteTagAsync(int tagId, string userId)
        {
            try
            {
                var tag = await _tagRepository.GetByIdAsync(tagId);
                if (tag == null)
                {
                    return false;
                }

                // Check workspace access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(tag.WorkspaceId, userId);
                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException($"User {userId} does not have access to this tag's workspace");
                }

                // Delete tag with all assignments (due to cascade delete restriction)
                var result = await _tagRepository.DeleteWithAssignmentsAsync(tagId);

                if (result)
                {
                    _logger.LogInformation("Deleted tag {TagId} '{TagName}' from workspace {WorkspaceId}",
                        tagId, tag.Name, tag.WorkspaceId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag {TagId}", tagId);
                throw;
            }
        }

        /// <summary>
        /// Get all tags in a workspace
        /// </summary>
        public async Task<List<Tag>> GetWorkspaceTagsAsync(int workspaceId, string userId)
        {
            try
            {
                // Check workspace access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);
                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} denied access to workspace {WorkspaceId} tags", userId, workspaceId);
                    return new List<Tag>();
                }

                return await _tagRepository.GetByWorkspaceIdAsync(workspaceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tags for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Get tags with note counts for a workspace
        /// NOTE: Assumes workspace access has already been verified by the caller
        /// </summary>
        public async Task<List<(Tag Tag, int NoteCount)>> GetWorkspaceTagsWithCountsAsync(int workspaceId, string userId)
        {
            try
            {
                // Skip redundant access check - WorkspaceView already verifies access
                // Removing this prevents DbContext concurrency issues during page refresh
                return await _tagRepository.GetTagsWithCountsAsync(workspaceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tags with counts for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Assign a tag to a note
        /// </summary>
        public async Task<NoteTagAssignment> AssignTagToNoteAsync(int noteId, int tagId, string userId)
        {
            try
            {
                // Verify tag exists and get workspace
                var tag = await _tagRepository.GetByIdAsync(tagId);
                if (tag == null)
                {
                    throw new InvalidOperationException($"Tag {tagId} not found");
                }

                // Check workspace access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(tag.WorkspaceId, userId);
                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException($"User {userId} does not have access to this workspace");
                }

                // Assign tag
                var assignment = await _assignmentRepository.AssignAsync(noteId, tagId, userId);

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
        public async Task AssignMultipleTagsToNoteAsync(int noteId, List<int> tagIds, string userId, int workspaceId)
        {
            try
            {
                // Check workspace access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);
                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException($"User {userId} does not have access to workspace {workspaceId}");
                }

                await _assignmentRepository.AssignMultipleAsync(noteId, tagIds, userId);

                _logger.LogInformation("Assigned {Count} tags to note {NoteId} by user {UserId}",
                    tagIds.Count, noteId, userId);
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
        public async Task<bool> UnassignTagFromNoteAsync(int noteId, int tagId, string userId)
        {
            try
            {
                // Verify tag exists and get workspace
                var tag = await _tagRepository.GetByIdAsync(tagId);
                if (tag == null)
                {
                    return false;
                }

                // Check workspace access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(tag.WorkspaceId, userId);
                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException($"User {userId} does not have access to this workspace");
                }

                var result = await _assignmentRepository.UnassignAsync(noteId, tagId);

                if (result)
                {
                    _logger.LogInformation("Unassigned tag {TagId} from note {NoteId} by user {UserId}",
                        tagId, noteId, userId);
                }

                return result;
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
        public async Task ReplaceNoteTagsAsync(int noteId, List<int> tagIds, string userId, int workspaceId)
        {
            try
            {
                // Check workspace access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);
                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException($"User {userId} does not have access to workspace {workspaceId}");
                }

                await _assignmentRepository.ReplaceAssignmentsAsync(noteId, tagIds, userId);

                _logger.LogInformation("Replaced tag assignments for note {NoteId}: {Count} tags by user {UserId}",
                    noteId, tagIds.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing tag assignments for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Get all tags assigned to a note
        /// </summary>
        public async Task<List<Tag>> GetNoteTagsAsync(int noteId)
        {
            try
            {
                return await _assignmentRepository.GetTagsByNoteIdAsync(noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tags for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Get all notes with a specific tag
        /// </summary>
        public async Task<List<Note>> GetNotesWithTagAsync(int tagId, string userId)
        {
            try
            {
                // Verify tag exists and get workspace
                var tag = await _tagRepository.GetByIdAsync(tagId);
                if (tag == null)
                {
                    return new List<Note>();
                }

                // Check workspace access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(tag.WorkspaceId, userId);
                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} denied access to notes with tag {TagId}", userId, tagId);
                    return new List<Note>();
                }

                return await _assignmentRepository.GetNotesByTagIdAsync(tagId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes with tag {TagId}", tagId);
                throw;
            }
        }

        /// <summary>
        /// Create tag from text (parse frontmatter tag format)
        /// Supports formats: "tagname", "#tagname", "tag-name", "tag_name"
        /// </summary>
        public async Task<Tag> GetOrCreateTagFromTextAsync(int workspaceId, string userId, string tagText)
        {
            try
            {
                // Normalize tag text
                tagText = tagText.Trim();

                // Remove leading # if present
                if (tagText.StartsWith('#'))
                {
                    tagText = tagText.Substring(1);
                }

                // Check if tag already exists
                var existing = await _tagRepository.GetByNameAsync(workspaceId, tagText);
                if (existing != null)
                {
                    return existing;
                }

                // Create new tag
                return await CreateTagAsync(workspaceId, userId, tagText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating tag from text '{TagText}' in workspace {WorkspaceId}",
                    tagText, workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Bulk create or get tags from a list of tag names
        /// Used during markdown import with frontmatter tags
        /// </summary>
        public async Task<List<Tag>> GetOrCreateTagsFromListAsync(int workspaceId, string userId, List<string> tagNames)
        {
            try
            {
                var tags = new List<Tag>();

                foreach (var tagName in tagNames)
                {
                    var tag = await GetOrCreateTagFromTextAsync(workspaceId, userId, tagName);
                    tags.Add(tag);
                }

                return tags;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating tags in workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Validate hex color format
        /// </summary>
        private bool IsValidHexColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return false;
            }

            // Must start with # and have exactly 6 hex digits
            if (!color.StartsWith('#') || color.Length != 7)
            {
                return false;
            }

            // Check that all characters after # are hex digits
            for (int i = 1; i < color.Length; i++)
            {
                char c = color[i];
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
