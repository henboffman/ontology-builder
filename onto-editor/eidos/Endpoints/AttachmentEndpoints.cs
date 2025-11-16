using Eidos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eidos.Endpoints
{
    /// <summary>
    /// Minimal API endpoints for note attachment upload/download
    /// Security: All endpoints require authentication and workspace permissions
    /// </summary>
    public static class AttachmentEndpoints
    {
        public static void MapAttachmentEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/attachments")
                .RequireAuthorization();

            // Upload image to a note
            group.MapPost("/upload/{noteId}", UploadAttachment)
                .DisableAntiforgery() // Required for file uploads
                .WithName("UploadAttachment")
                .WithDescription("Upload an image attachment to a note");

            // Download/view attachment
            group.MapGet("/{attachmentId}", GetAttachment)
                .WithName("GetAttachment")
                .WithDescription("Get an attachment by ID");

            // Delete attachment
            group.MapDelete("/{attachmentId}", DeleteAttachment)
                .WithName("DeleteAttachment")
                .WithDescription("Delete an attachment");

            // List attachments for a note
            group.MapGet("/note/{noteId}", ListNoteAttachments)
                .WithName("ListNoteAttachments")
                .WithDescription("List all attachments for a note");
        }

        /// <summary>
        /// Upload an image attachment to a note
        /// POST /api/attachments/upload/{noteId}
        /// </summary>
        private static async Task<IResult> UploadAttachment(
            int noteId,
            [FromForm] int workspaceId,
            [FromForm] IFormFile file,
            ClaimsPrincipal user,
            AttachmentService attachmentService,
            ILogger<AttachmentService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                // Validate file
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { message = "No file provided" });
                }

                // Open file stream
                await using var stream = file.OpenReadStream();

                // Upload with security validation
                var (success, message, attachment) = await attachmentService.UploadAttachmentAsync(
                    noteId,
                    workspaceId,
                    userId,
                    stream,
                    file.FileName,
                    file.ContentType);

                if (!success)
                {
                    logger.LogWarning(
                        "Upload failed for user {UserId}, note {NoteId}: {Message}",
                        userId, noteId, message);
                    return Results.BadRequest(new { message });
                }

                // Return attachment metadata (without binary data)
                return Results.Ok(new
                {
                    id = attachment!.Id,
                    fileName = attachment.FileName,
                    contentType = attachment.ContentType,
                    fileSizeBytes = attachment.FileSizeBytes,
                    createdAt = attachment.CreatedAt,
                    url = $"/api/attachments/{attachment.Id}"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading attachment for note {NoteId}", noteId);
                return Results.Problem("An error occurred while uploading the file.");
            }
        }

        /// <summary>
        /// Get attachment by ID (returns binary image data)
        /// GET /api/attachments/{attachmentId}
        /// </summary>
        private static async Task<IResult> GetAttachment(
            int attachmentId,
            ClaimsPrincipal user,
            AttachmentService attachmentService,
            ILogger<AttachmentService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var (success, message, attachment) = await attachmentService.GetAttachmentAsync(
                    attachmentId,
                    userId);

                if (!success)
                {
                    if (message.Contains("permission"))
                    {
                        return Results.Forbid();
                    }
                    return Results.NotFound(new { message });
                }

                // Return image file
                return Results.File(
                    attachment!.Data,
                    attachment.ContentType,
                    attachment.FileName,
                    enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving attachment {AttachmentId}", attachmentId);
                return Results.Problem("An error occurred while retrieving the file.");
            }
        }

        /// <summary>
        /// Delete attachment
        /// DELETE /api/attachments/{attachmentId}
        /// </summary>
        private static async Task<IResult> DeleteAttachment(
            int attachmentId,
            ClaimsPrincipal user,
            AttachmentService attachmentService,
            ILogger<AttachmentService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var (success, message) = await attachmentService.DeleteAttachmentAsync(
                    attachmentId,
                    userId);

                if (!success)
                {
                    if (message.Contains("permission"))
                    {
                        return Results.Forbid();
                    }
                    return Results.NotFound(new { message });
                }

                return Results.Ok(new { message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
                return Results.Problem("An error occurred while deleting the file.");
            }
        }

        /// <summary>
        /// List all attachments for a note (metadata only, no binary data)
        /// GET /api/attachments/note/{noteId}
        /// </summary>
        private static async Task<IResult> ListNoteAttachments(
            int noteId,
            ClaimsPrincipal user,
            AttachmentService attachmentService,
            ILogger<AttachmentService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var attachments = await attachmentService.GetNoteAttachmentsAsync(noteId, userId);

                var result = attachments.Select(a => new
                {
                    id = a.Id,
                    fileName = a.FileName,
                    contentType = a.ContentType,
                    fileSizeBytes = a.FileSizeBytes,
                    altText = a.AltText,
                    createdAt = a.CreatedAt,
                    url = $"/api/attachments/{a.Id}"
                });

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing attachments for note {NoteId}", noteId);
                return Results.Problem("An error occurred while retrieving attachments.");
            }
        }
    }
}
