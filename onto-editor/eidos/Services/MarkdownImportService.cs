using Eidos.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Eidos.Services
{
    /// <summary>
    /// Service for importing markdown files into workspaces with frontmatter parsing.
    /// Supports YAML-style frontmatter for metadata like tags, aliases, and creation dates.
    /// </summary>
    public class MarkdownImportService
    {
        private readonly NoteService _noteService;
        private readonly TagService _tagService;
        private readonly ILogger<MarkdownImportService> _logger;

        public MarkdownImportService(
            NoteService noteService,
            TagService tagService,
            ILogger<MarkdownImportService> logger)
        {
            _noteService = noteService;
            _tagService = tagService;
            _logger = logger;
        }

        /// <summary>
        /// Import a single markdown file into a workspace
        /// </summary>
        public async Task<ImportResult> ImportMarkdownFileAsync(
            int workspaceId,
            string userId,
            string fileName,
            string fileContent)
        {
            try
            {
                var (frontmatter, content) = ParseFrontmatter(fileContent);

                // Extract title from frontmatter or filename
                var title = frontmatter.ContainsKey("title")
                    ? frontmatter["title"]
                    : Path.GetFileNameWithoutExtension(fileName);

                // Create the note
                var note = await _noteService.CreateNoteAsync(workspaceId, userId, title, content);

                // Process tags from frontmatter
                var importedTags = new List<string>();
                if (frontmatter.ContainsKey("tags"))
                {
                    var tagsList = ParseTags(frontmatter["tags"]);
                    foreach (var tagName in tagsList)
                    {
                        try
                        {
                            // Get or create tag
                            var tag = await _tagService.GetOrCreateTagFromTextAsync(workspaceId, userId, tagName);

                            // Assign tag to note
                            await _tagService.AssignTagToNoteAsync(note.Id, tag.Id, userId);
                            importedTags.Add(tagName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to import tag {TagName} for note {NoteId}", tagName, note.Id);
                        }
                    }
                }

                _logger.LogInformation("Imported markdown file {FileName} as note {NoteId} with {TagCount} tags",
                    fileName, note.Id, importedTags.Count);

                return new ImportResult
                {
                    Success = true,
                    NoteId = note.Id,
                    NoteTitle = title,
                    ImportedTags = importedTags,
                    FileName = fileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import markdown file {FileName}", fileName);
                return new ImportResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    FileName = fileName
                };
            }
        }

        /// <summary>
        /// Import multiple markdown files at once
        /// </summary>
        public async Task<BatchImportResult> ImportMarkdownFilesAsync(
            int workspaceId,
            string userId,
            Dictionary<string, string> files)
        {
            var results = new List<ImportResult>();

            foreach (var file in files)
            {
                var result = await ImportMarkdownFileAsync(workspaceId, userId, file.Key, file.Value);
                results.Add(result);
            }

            return new BatchImportResult
            {
                TotalFiles = files.Count,
                SuccessCount = results.Count(r => r.Success),
                FailureCount = results.Count(r => !r.Success),
                Results = results
            };
        }

        /// <summary>
        /// Parse YAML-style frontmatter from markdown content.
        /// Frontmatter should be delimited by --- at the start and end.
        /// </summary>
        private (Dictionary<string, string> frontmatter, string content) ParseFrontmatter(string markdown)
        {
            var frontmatter = new Dictionary<string, string>();
            var content = markdown;

            // Check if markdown starts with frontmatter delimiter
            if (!markdown.TrimStart().StartsWith("---"))
            {
                return (frontmatter, content);
            }

            // Find the end delimiter
            var lines = markdown.Split('\n');
            var frontmatterEndIndex = -1;

            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "---")
                {
                    frontmatterEndIndex = i;
                    break;
                }
            }

            if (frontmatterEndIndex == -1)
            {
                return (frontmatter, content);
            }

            // Parse frontmatter key-value pairs
            for (int i = 1; i < frontmatterEndIndex; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = line.Substring(0, colonIndex).Trim();
                    var value = line.Substring(colonIndex + 1).Trim();

                    // Remove quotes if present
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                        (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    frontmatter[key] = value;
                }
            }

            // Extract content (everything after the second ---)
            content = string.Join('\n', lines.Skip(frontmatterEndIndex + 1)).TrimStart();

            return (frontmatter, content);
        }

        /// <summary>
        /// Parse tags from frontmatter value.
        /// Supports both YAML list format [tag1, tag2] and comma-separated strings.
        /// </summary>
        private List<string> ParseTags(string tagString)
        {
            var tags = new List<string>();

            if (string.IsNullOrWhiteSpace(tagString))
            {
                return tags;
            }

            // Remove brackets if present [tag1, tag2]
            tagString = tagString.Trim();
            if (tagString.StartsWith("[") && tagString.EndsWith("]"))
            {
                tagString = tagString.Substring(1, tagString.Length - 2);
            }

            // Split by comma and clean up
            var tagArray = tagString.Split(',');
            foreach (var tag in tagArray)
            {
                var cleanTag = tag.Trim().Trim('"', '\'');
                if (!string.IsNullOrWhiteSpace(cleanTag))
                {
                    tags.Add(cleanTag);
                }
            }

            return tags;
        }
    }

    /// <summary>
    /// Result of importing a single markdown file
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public int NoteId { get; set; }
        public string NoteTitle { get; set; } = string.Empty;
        public List<string> ImportedTags { get; set; } = new();
        public string FileName { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of importing multiple markdown files
    /// </summary>
    public class BatchImportResult
    {
        public int TotalFiles { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<ImportResult> Results { get; set; } = new();
    }
}
