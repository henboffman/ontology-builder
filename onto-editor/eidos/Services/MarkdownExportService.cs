using Eidos.Data.Repositories;
using Eidos.Models;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;

namespace Eidos.Services
{
    /// <summary>
    /// Service for exporting workspace notes to markdown files
    /// Supports single file and ZIP archive export with YAML frontmatter
    /// </summary>
    public class MarkdownExportService
    {
        private readonly NoteRepository _noteRepository;
        private readonly TagService _tagService;
        private readonly ILogger<MarkdownExportService> _logger;

        public MarkdownExportService(
            NoteRepository noteRepository,
            TagService tagService,
            ILogger<MarkdownExportService> logger)
        {
            _noteRepository = noteRepository;
            _tagService = tagService;
            _logger = logger;
        }

        /// <summary>
        /// Export all notes in a workspace as a ZIP file
        /// </summary>
        public async Task<byte[]> ExportNotesAsZipAsync(int workspaceId, string userId)
        {
            try
            {
                // Get all notes WITH content
                var notes = await _noteRepository.GetByWorkspaceIdWithContentAsync(workspaceId);

                if (!notes.Any())
                {
                    throw new InvalidOperationException("No notes to export");
                }

                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var note in notes)
                    {
                        // Generate markdown content with frontmatter
                        var markdown = await GenerateMarkdownWithFrontmatterAsync(note, workspaceId, userId);

                        // Sanitize filename
                        var filename = SanitizeFilename(note.Title) + ".md";

                        // Create entry in ZIP
                        var entry = archive.CreateEntry(filename, CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                        await writer.WriteAsync(markdown);
                    }
                }

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting notes for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Export a single note as markdown with frontmatter
        /// </summary>
        public async Task<string> ExportSingleNoteAsync(int noteId, int workspaceId, string userId)
        {
            try
            {
                var note = await _noteRepository.GetByIdWithContentAsync(noteId);

                if (note == null)
                {
                    throw new ArgumentException($"Note {noteId} not found", nameof(noteId));
                }

                if (note.WorkspaceId != workspaceId)
                {
                    throw new UnauthorizedAccessException("Note does not belong to this workspace");
                }

                return await GenerateMarkdownWithFrontmatterAsync(note, workspaceId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Generate markdown content with YAML frontmatter for a note
        /// </summary>
        private async Task<string> GenerateMarkdownWithFrontmatterAsync(Note note, int workspaceId, string userId)
        {
            var sb = new StringBuilder();

            // Add YAML frontmatter
            sb.AppendLine("---");
            sb.AppendLine($"title: {EscapeYaml(note.Title)}");
            sb.AppendLine($"created: {note.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}");
            sb.AppendLine($"updated: {note.UpdatedAt:yyyy-MM-ddTHH:mm:ssZ}");

            // Add tags if present
            var tags = await _tagService.GetNoteTagsAsync(note.Id);
            if (tags.Any())
            {
                sb.AppendLine("tags:");
                foreach (var tag in tags)
                {
                    sb.AppendLine($"  - {EscapeYaml(tag.Name)}");
                }
            }

            // Add concept note flag if applicable
            if (note.IsConceptNote && note.LinkedConcept != null)
            {
                sb.AppendLine($"concept: {EscapeYaml(note.LinkedConcept.Name)}");
                sb.AppendLine($"concept_id: {note.LinkedConceptId}");
            }

            // Add import metadata if present
            if (!string.IsNullOrEmpty(note.ImportedFrom))
            {
                sb.AppendLine($"imported_from: {EscapeYaml(note.ImportedFrom)}");
                if (note.ImportedAt.HasValue)
                {
                    sb.AppendLine($"imported_at: {note.ImportedAt.Value:yyyy-MM-ddTHH:mm:ssZ}");
                }
            }

            sb.AppendLine("---");
            sb.AppendLine();

            // Add markdown content
            if (note.Content != null)
            {
                sb.Append(note.Content.MarkdownContent);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Sanitize a filename to remove invalid characters
        /// </summary>
        private string SanitizeFilename(string filename)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", filename.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');

            // Ensure filename isn't too long
            if (sanitized.Length > 200)
            {
                sanitized = sanitized.Substring(0, 200);
            }

            return sanitized;
        }

        /// <summary>
        /// Escape special characters in YAML values
        /// </summary>
        private string EscapeYaml(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";

            // If value contains special characters, quote it
            if (value.Contains(':') || value.Contains('#') || value.Contains('[') ||
                value.Contains(']') || value.Contains('{') || value.Contains('}') ||
                value.Contains('\"') || value.Contains('\'') || value.StartsWith('-') ||
                value.StartsWith('>') || value.StartsWith('|'))
            {
                // Escape quotes and wrap in quotes
                return $"\"{value.Replace("\"", "\\\"")}\"";
            }

            return value;
        }
    }
}
