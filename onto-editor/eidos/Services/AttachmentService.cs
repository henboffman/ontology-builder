using Eidos.Data.Repositories;
using Eidos.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Eidos.Services
{
    /// <summary>
    /// Service for managing note attachments (images) with security validation
    /// Security features:
    /// - File type validation (images only)
    /// - Size limits (1MB default, configurable)
    /// - Permission checks (workspace access required)
    /// - Content-type verification
    /// - Filename sanitization
    /// - SHA256 content hashing for integrity/deduplication
    /// </summary>
    public class AttachmentService
    {
        private readonly NoteAttachmentRepository _attachmentRepository;
        private readonly WorkspaceRepository _workspaceRepository;
        private readonly OntologyPermissionService _permissionService;
        private readonly ILogger<AttachmentService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Security configuration
        private const int MaxFileSizeBytes = 1048576; // 1MB
        private const int MaxAttachmentsPerNote = 50;
        private const long MaxWorkspaceStorageBytes = 104857600; // 100MB per workspace

        private static readonly HashSet<string> AllowedContentTypes = new()
        {
            "image/png",
            "image/jpeg",
            "image/gif",
            "image/webp",
            "image/svg+xml"
        };

        private static readonly HashSet<string> AllowedFileExtensions = new()
        {
            ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg"
        };

        public AttachmentService(
            NoteAttachmentRepository attachmentRepository,
            WorkspaceRepository workspaceRepository,
            OntologyPermissionService permissionService,
            ILogger<AttachmentService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _attachmentRepository = attachmentRepository;
            _workspaceRepository = workspaceRepository;
            _permissionService = permissionService;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Upload an image attachment to a note
        /// Validates file type, size, permissions, and workspace quota
        /// </summary>
        public async Task<(bool Success, string Message, NoteAttachment? Attachment)> UploadAttachmentAsync(
            int noteId,
            int workspaceId,
            string userId,
            Stream fileStream,
            string fileName,
            string contentType)
        {
            try
            {
                // 1. Validate permission - user must have edit access to workspace
                var canEdit = await _permissionService.CanEditWorkspaceAsync(workspaceId, userId);
                if (!canEdit)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to upload to workspace {WorkspaceId} without permission",
                        userId, workspaceId);
                    return (false, "You do not have permission to upload attachments to this workspace.", null);
                }

                // 2. Validate file size
                if (fileStream.Length > MaxFileSizeBytes)
                {
                    return (false, $"File size exceeds maximum of {MaxFileSizeBytes / 1024 / 1024}MB.", null);
                }

                if (fileStream.Length == 0)
                {
                    return (false, "File is empty.", null);
                }

                // 3. Validate content type
                if (!AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to upload disallowed content type {ContentType}",
                        userId, contentType);
                    return (false, "Only image files (PNG, JPEG, GIF, WebP, SVG) are allowed.", null);
                }

                // 4. Sanitize and validate filename
                var sanitizedFileName = SanitizeFileName(fileName);
                var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();
                if (!AllowedFileExtensions.Contains(extension))
                {
                    return (false, "Invalid file extension. Only image files are allowed.", null);
                }

                // 5. Check workspace storage quota
                var currentSize = await _attachmentRepository.GetWorkspaceTotalSizeAsync(workspaceId);
                if (currentSize + fileStream.Length > MaxWorkspaceStorageBytes)
                {
                    return (false, $"Workspace storage quota ({MaxWorkspaceStorageBytes / 1024 / 1024}MB) exceeded.", null);
                }

                // 6. Check attachment count per note
                var existingAttachments = await _attachmentRepository.GetByNoteIdAsync(noteId);
                if (existingAttachments.Count >= MaxAttachmentsPerNote)
                {
                    return (false, $"Maximum of {MaxAttachmentsPerNote} attachments per note exceeded.", null);
                }

                // 7. Read file data and compute hash
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                var fileData = memoryStream.ToArray();
                var contentHash = ComputeSHA256Hash(fileData);

                // 8. Check for duplicate content (optional deduplication)
                var duplicate = await _attachmentRepository.FindByContentHashAsync(workspaceId, contentHash);
                if (duplicate != null)
                {
                    _logger.LogInformation(
                        "Found duplicate attachment {DuplicateId} for hash {Hash}",
                        duplicate.Id, contentHash);
                    // Return existing attachment (saves storage)
                    return (true, "File already exists (duplicate detected).", duplicate);
                }

                // 9. Create attachment entity
                var attachment = new NoteAttachment
                {
                    NoteId = noteId,
                    WorkspaceId = workspaceId,
                    FileName = sanitizedFileName,
                    ContentType = contentType,
                    FileSizeBytes = fileData.Length,
                    Data = fileData,
                    UploadedByUserId = userId,
                    ContentHash = contentHash,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };

                // 10. Save to database
                var created = await _attachmentRepository.CreateAsync(attachment);

                _logger.LogInformation(
                    "User {UserId} uploaded attachment {AttachmentId} ({FileSize} bytes) to note {NoteId}",
                    userId, created.Id, created.FileSizeBytes, noteId);

                return (true, "Image uploaded successfully.", created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to upload attachment for note {NoteId} by user {UserId}",
                    noteId, userId);
                return (false, "An error occurred while uploading the file.", null);
            }
        }

        /// <summary>
        /// Get attachment with permission check
        /// </summary>
        public async Task<(bool Success, string Message, NoteAttachment? Attachment)> GetAttachmentAsync(
            int attachmentId,
            string userId)
        {
            try
            {
                // Get attachment metadata first (without binary data)
                var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, includeData: false);
                if (attachment == null)
                {
                    return (false, "Attachment not found.", null);
                }

                // Check permission - user must have view access to workspace
                var canView = await _permissionService.CanViewWorkspaceAsync(attachment.WorkspaceId, userId);
                if (!canView)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to access attachment {AttachmentId} without permission",
                        userId, attachmentId);
                    return (false, "You do not have permission to access this attachment.", null);
                }

                // Load full attachment with binary data
                var fullAttachment = await _attachmentRepository.GetWithDataAsync(attachmentId);
                if (fullAttachment == null)
                {
                    return (false, "Attachment data not found.", null);
                }

                // Update last accessed timestamp in background with its own scope to avoid disposed context errors
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var repository = scope.ServiceProvider.GetRequiredService<NoteAttachmentRepository>();
                        await repository.UpdateLastAccessedAsync(attachmentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update last accessed timestamp for attachment {AttachmentId}", attachmentId);
                    }
                });

                return (true, string.Empty, fullAttachment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get attachment {AttachmentId}", attachmentId);
                return (false, "An error occurred while retrieving the attachment.", null);
            }
        }

        /// <summary>
        /// Delete attachment with permission check
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteAttachmentAsync(
            int attachmentId,
            string userId)
        {
            try
            {
                // Get attachment to check permissions
                var attachment = await _attachmentRepository.GetByIdAsync(attachmentId, includeData: false);
                if (attachment == null)
                {
                    return (false, "Attachment not found.");
                }

                // Check permission - user must have edit access to workspace
                var canEdit = await _permissionService.CanEditWorkspaceAsync(attachment.WorkspaceId, userId);
                if (!canEdit)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to delete attachment {AttachmentId} without permission",
                        userId, attachmentId);
                    return (false, "You do not have permission to delete this attachment.");
                }

                // Delete attachment
                var deleted = await _attachmentRepository.DeleteAsync(attachmentId);
                if (!deleted)
                {
                    return (false, "Failed to delete attachment.");
                }

                _logger.LogInformation(
                    "User {UserId} deleted attachment {AttachmentId}",
                    userId, attachmentId);

                return (true, "Attachment deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete attachment {AttachmentId}", attachmentId);
                return (false, "An error occurred while deleting the attachment.");
            }
        }

        /// <summary>
        /// Get all attachments for a note (metadata only)
        /// </summary>
        public async Task<List<NoteAttachment>> GetNoteAttachmentsAsync(int noteId, string userId)
        {
            try
            {
                // Get first attachment to check workspace
                var attachments = await _attachmentRepository.GetByNoteIdAsync(noteId);
                if (!attachments.Any())
                {
                    return new List<NoteAttachment>();
                }

                // Check permission
                var workspaceId = attachments.First().WorkspaceId;
                var canView = await _permissionService.CanViewWorkspaceAsync(workspaceId, userId);
                if (!canView)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to list attachments for note {NoteId} without permission",
                        userId, noteId);
                    return new List<NoteAttachment>();
                }

                return attachments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get attachments for note {NoteId}", noteId);
                return new List<NoteAttachment>();
            }
        }

        /// <summary>
        /// Sanitize filename to prevent path traversal and injection attacks
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            // Remove path components
            fileName = Path.GetFileName(fileName);

            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            // Limit length
            if (sanitized.Length > 200)
            {
                var extension = Path.GetExtension(sanitized);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
                sanitized = nameWithoutExt.Substring(0, 200 - extension.Length) + extension;
            }

            // Ensure we have a filename
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = $"image_{Guid.NewGuid():N}.png";
            }

            return sanitized;
        }

        /// <summary>
        /// Compute SHA256 hash of file content for integrity and deduplication
        /// </summary>
        private string ComputeSHA256Hash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
