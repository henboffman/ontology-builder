using Eidos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eidos.Data.Repositories
{
    /// <summary>
    /// Repository for note data access operations
    /// Optimized for performance with separate content loading
    /// </summary>
    public class NoteRepository
    {
        private readonly OntologyDbContext _context;
        private readonly ILogger<NoteRepository> _logger;

        public NoteRepository(
            OntologyDbContext context,
            ILogger<NoteRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get note by ID (metadata only, no content)
        /// </summary>
        public async Task<Note?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Notes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note {NoteId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get note by ID with content
        /// </summary>
        public async Task<Note?> GetByIdWithContentAsync(int id)
        {
            try
            {
                return await _context.Notes
                    .Include(n => n.Content)
                    .FirstOrDefaultAsync(n => n.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note {NoteId} with content", id);
                throw;
            }
        }

        /// <summary>
        /// Get note by ID with all related data (content, links, concept)
        /// </summary>
        public async Task<Note?> GetByIdWithAllDataAsync(int id)
        {
            try
            {
                return await _context.Notes
                    .Include(n => n.Content)
                    .Include(n => n.OutgoingLinks)
                        .ThenInclude(nl => nl.TargetConcept)
                    .Include(n => n.LinkedConcept)
                    .FirstOrDefaultAsync(n => n.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note {NoteId} with all data", id);
                throw;
            }
        }

        /// <summary>
        /// Get all notes in a workspace (metadata only, for list view)
        /// </summary>
        public async Task<List<Note>> GetByWorkspaceIdAsync(int workspaceId, bool conceptNotesOnly = false, bool userNotesOnly = false)
        {
            try
            {
                var query = _context.Notes
                    .Where(n => n.WorkspaceId == workspaceId)
                    .AsNoTracking();

                if (conceptNotesOnly)
                {
                    query = query.Where(n => n.IsConceptNote);
                }
                else if (userNotesOnly)
                {
                    query = query.Where(n => !n.IsConceptNote);
                }

                return await query
                    .OrderByDescending(n => n.UpdatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Get concept note for a specific concept
        /// </summary>
        public async Task<Note?> GetConceptNoteAsync(int conceptId)
        {
            try
            {
                return await _context.Notes
                    .Include(n => n.Content)
                    .FirstOrDefaultAsync(n => n.LinkedConceptId == conceptId && n.IsConceptNote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting concept note for concept {ConceptId}", conceptId);
                throw;
            }
        }

        /// <summary>
        /// Search notes by title
        /// </summary>
        public async Task<List<Note>> SearchByTitleAsync(int workspaceId, string searchTerm)
        {
            try
            {
                return await _context.Notes
                    .Where(n => n.WorkspaceId == workspaceId &&
                               EF.Functions.Like(n.Title, $"%{searchTerm}%"))
                    .OrderByDescending(n => n.UpdatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching notes in workspace {WorkspaceId} for term {SearchTerm}",
                    workspaceId, searchTerm);
                throw;
            }
        }

        /// <summary>
        /// Create a new note with content
        /// </summary>
        public async Task<Note> CreateAsync(Note note, string markdownContent)
        {
            try
            {
                note.CreatedAt = DateTime.UtcNow;
                note.UpdatedAt = DateTime.UtcNow;
                note.ContentLength = markdownContent?.Length ?? 0;

                _context.Notes.Add(note);
                await _context.SaveChangesAsync();

                // Create content separately
                if (!string.IsNullOrEmpty(markdownContent))
                {
                    var noteContent = new NoteContent
                    {
                        NoteId = note.Id,
                        MarkdownContent = markdownContent,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.NoteContents.Add(noteContent);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created note {NoteId} in workspace {WorkspaceId}",
                    note.Id, note.WorkspaceId);

                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note in workspace {WorkspaceId}", note.WorkspaceId);
                throw;
            }
        }

        /// <summary>
        /// Update note metadata only (no content)
        /// </summary>
        public async Task UpdateMetadataAsync(int noteId, int linkCount)
        {
            try
            {
                // Use ExecuteUpdate for direct database update without tracking issues
                await _context.Notes
                    .Where(n => n.Id == noteId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(n => n.LinkCount, linkCount)
                        .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));

                _logger.LogInformation("Updated note {NoteId} metadata", noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note {NoteId} metadata", noteId);
                throw;
            }
        }

        /// <summary>
        /// Update note content
        /// </summary>
        public async Task UpdateContentAsync(int noteId, string markdownContent)
        {
            try
            {
                var note = await _context.Notes.FindAsync(noteId);
                if (note == null)
                {
                    throw new InvalidOperationException($"Note {noteId} not found");
                }

                // Update content length
                note.ContentLength = markdownContent?.Length ?? 0;
                note.UpdatedAt = DateTime.UtcNow;

                // Update or create content
                var noteContent = await _context.NoteContents.FindAsync(noteId);
                if (noteContent == null)
                {
                    noteContent = new NoteContent
                    {
                        NoteId = noteId,
                        MarkdownContent = markdownContent ?? string.Empty,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.NoteContents.Add(noteContent);
                }
                else
                {
                    noteContent.MarkdownContent = markdownContent ?? string.Empty;
                    noteContent.RenderedHtml = null; // Invalidate cache
                    noteContent.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated content for note {NoteId}", noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating content for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Delete a note and its content
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            try
            {
                var note = await _context.Notes.FindAsync(id);
                if (note != null)
                {
                    _context.Notes.Remove(note);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Deleted note {NoteId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note {NoteId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get backlinks for a concept (notes that link to it)
        /// </summary>
        public async Task<List<NoteLink>> GetBacklinksAsync(int conceptId)
        {
            try
            {
                return await _context.NoteLinks
                    .Where(nl => nl.TargetConceptId == conceptId)
                    .Include(nl => nl.SourceNote)
                    .OrderByDescending(nl => nl.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting backlinks for concept {ConceptId}", conceptId);
                throw;
            }
        }

        /// <summary>
        /// Create or update note links from parsed content
        /// </summary>
        public async Task UpdateNoteLinksAsync(int noteId, List<NoteLink> links)
        {
            try
            {
                // Remove existing links
                var existingLinks = await _context.NoteLinks
                    .Where(nl => nl.SourceNoteId == noteId)
                    .ToListAsync();

                _context.NoteLinks.RemoveRange(existingLinks);

                // Add new links
                if (links.Any())
                {
                    _context.NoteLinks.AddRange(links);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated {Count} links for note {NoteId}", links.Count, noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating links for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Get recent notes in workspace
        /// </summary>
        public async Task<List<Note>> GetRecentNotesAsync(int workspaceId, int count = 10)
        {
            try
            {
                return await _context.Notes
                    .Where(n => n.WorkspaceId == workspaceId)
                    .OrderByDescending(n => n.UpdatedAt)
                    .Take(count)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent notes for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }
    }
}
