using Eidos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eidos.Data.Repositories
{
    /// <summary>
    /// Repository for NoteAttachment data access
    /// Handles CRUD operations for image attachments in notes
    /// </summary>
    public class NoteAttachmentRepository
    {
        private readonly OntologyDbContext _context;
        private readonly ILogger<NoteAttachmentRepository> _logger;

        public NoteAttachmentRepository(
            OntologyDbContext context,
            ILogger<NoteAttachmentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get attachment by ID (without binary data for performance)
        /// </summary>
        public async Task<NoteAttachment?> GetByIdAsync(int id, bool includeData = false)
        {
            try
            {
                var query = _context.NoteAttachments.AsQueryable();

                if (!includeData)
                {
                    // Don't load binary data for metadata queries
                    query = query.Select(a => new NoteAttachment
                    {
                        Id = a.Id,
                        NoteId = a.NoteId,
                        WorkspaceId = a.WorkspaceId,
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        FileSizeBytes = a.FileSizeBytes,
                        UploadedByUserId = a.UploadedByUserId,
                        AltText = a.AltText,
                        ContentHash = a.ContentHash,
                        CreatedAt = a.CreatedAt,
                        LastAccessedAt = a.LastAccessedAt,
                        Data = Array.Empty<byte>() // Empty array, not loaded
                    });
                }

                return await query.FirstOrDefaultAsync(a => a.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get attachment {AttachmentId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get attachment with binary data for download
        /// </summary>
        public async Task<NoteAttachment?> GetWithDataAsync(int id)
        {
            try
            {
                return await _context.NoteAttachments
                    .FirstOrDefaultAsync(a => a.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get attachment {AttachmentId} with data", id);
                throw;
            }
        }

        /// <summary>
        /// Get all attachments for a note (without binary data)
        /// </summary>
        public async Task<List<NoteAttachment>> GetByNoteIdAsync(int noteId)
        {
            try
            {
                return await _context.NoteAttachments
                    .Where(a => a.NoteId == noteId)
                    .Select(a => new NoteAttachment
                    {
                        Id = a.Id,
                        NoteId = a.NoteId,
                        WorkspaceId = a.WorkspaceId,
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        FileSizeBytes = a.FileSizeBytes,
                        UploadedByUserId = a.UploadedByUserId,
                        AltText = a.AltText,
                        ContentHash = a.ContentHash,
                        CreatedAt = a.CreatedAt,
                        LastAccessedAt = a.LastAccessedAt,
                        Data = Array.Empty<byte>()
                    })
                    .OrderBy(a => a.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get attachments for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Create new attachment
        /// </summary>
        public async Task<NoteAttachment> CreateAsync(NoteAttachment attachment)
        {
            try
            {
                _context.NoteAttachments.Add(attachment);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Created attachment {AttachmentId} for note {NoteId} by user {UserId}",
                    attachment.Id, attachment.NoteId, attachment.UploadedByUserId);

                return attachment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create attachment for note {NoteId}", attachment.NoteId);
                throw;
            }
        }

        /// <summary>
        /// Delete attachment
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var attachment = await _context.NoteAttachments.FindAsync(id);
                if (attachment == null)
                {
                    return false;
                }

                _context.NoteAttachments.Remove(attachment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted attachment {AttachmentId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete attachment {AttachmentId}", id);
                throw;
            }
        }

        /// <summary>
        /// Update last accessed timestamp (for analytics/cleanup)
        /// </summary>
        public async Task UpdateLastAccessedAsync(int id)
        {
            try
            {
                var attachment = await _context.NoteAttachments.FindAsync(id);
                if (attachment != null)
                {
                    attachment.LastAccessedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update last accessed time for attachment {AttachmentId}", id);
                // Don't throw - this is not critical
            }
        }

        /// <summary>
        /// Get total attachment size for a workspace (for quota tracking)
        /// </summary>
        public async Task<long> GetWorkspaceTotalSizeAsync(int workspaceId)
        {
            try
            {
                return await _context.NoteAttachments
                    .Where(a => a.WorkspaceId == workspaceId)
                    .SumAsync(a => (long)a.FileSizeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total size for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Check if content hash already exists (for deduplication)
        /// </summary>
        public async Task<NoteAttachment?> FindByContentHashAsync(int workspaceId, string contentHash)
        {
            try
            {
                return await _context.NoteAttachments
                    .Where(a => a.WorkspaceId == workspaceId && a.ContentHash == contentHash)
                    .Select(a => new NoteAttachment
                    {
                        Id = a.Id,
                        NoteId = a.NoteId,
                        WorkspaceId = a.WorkspaceId,
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        FileSizeBytes = a.FileSizeBytes,
                        UploadedByUserId = a.UploadedByUserId,
                        AltText = a.AltText,
                        ContentHash = a.ContentHash,
                        CreatedAt = a.CreatedAt,
                        LastAccessedAt = a.LastAccessedAt,
                        Data = Array.Empty<byte>()
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find attachment by hash in workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }
    }
}
