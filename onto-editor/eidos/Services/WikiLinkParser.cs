using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Eidos.Services
{
    /// <summary>
    /// Parses [[wiki-style links]] from markdown content (Obsidian-style)
    /// Extracts concept references and their positions for backlinks functionality
    /// </summary>
    public class WikiLinkParser
    {
        /// <summary>
        /// Regex pattern to match [[concept]] or [[concept|display text]] syntax
        /// Group 1: The concept name
        /// Group 2: Optional display text (after |)
        /// </summary>
        private static readonly Regex WikiLinkRegex = new(
            @"\[\[([^\]|]+)(?:\|([^\]]+))?\]\]",
            RegexOptions.Compiled
        );

        /// <summary>
        /// Context window size (characters before and after the link)
        /// </summary>
        private const int ContextWindowSize = 50;

        /// <summary>
        /// Represents a parsed wiki link with its position and context
        /// </summary>
        public class ParsedLink
        {
            /// <summary>
            /// The concept name being referenced
            /// </summary>
            public string ConceptName { get; set; } = string.Empty;

            /// <summary>
            /// Optional display text (if using [[concept|display]] syntax)
            /// </summary>
            public string? DisplayText { get; set; }

            /// <summary>
            /// Character position where the link starts in the markdown
            /// </summary>
            public int Position { get; set; }

            /// <summary>
            /// Context snippet around the link (for backlinks panel)
            /// </summary>
            public string ContextSnippet { get; set; } = string.Empty;

            /// <summary>
            /// The full matched text (e.g., "[[Person]]" or "[[Person|John Doe]]")
            /// </summary>
            public string MatchedText { get; set; } = string.Empty;
        }

        /// <summary>
        /// Extract all [[wiki-links]] from markdown content
        /// </summary>
        /// <param name="markdownContent">The markdown content to parse</param>
        /// <returns>List of unique concept names referenced</returns>
        public List<string> ExtractConceptNames(string markdownContent)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                return new List<string>();
            }

            var matches = WikiLinkRegex.Matches(markdownContent);

            return matches
                .Select(m => m.Groups[1].Value.Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Extract all [[wiki-links]] with their positions and context
        /// </summary>
        /// <param name="markdownContent">The markdown content to parse</param>
        /// <returns>List of parsed links with position and context information</returns>
        public List<ParsedLink> ExtractLinksWithContext(string markdownContent)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                return new List<ParsedLink>();
            }

            var matches = WikiLinkRegex.Matches(markdownContent);
            var links = new List<ParsedLink>();

            foreach (Match match in matches)
            {
                var conceptName = match.Groups[1].Value.Trim();

                // Skip empty concept names
                if (string.IsNullOrWhiteSpace(conceptName))
                {
                    continue;
                }

                var displayText = match.Groups.Count > 2 && match.Groups[2].Success
                    ? match.Groups[2].Value.Trim()
                    : null;

                var link = new ParsedLink
                {
                    ConceptName = conceptName,
                    DisplayText = displayText,
                    Position = match.Index,
                    MatchedText = match.Value,
                    ContextSnippet = ExtractContextSnippet(markdownContent, match.Index, match.Length)
                };

                links.Add(link);
            }

            return links;
        }

        /// <summary>
        /// Count the number of [[wiki-links]] in markdown content
        /// </summary>
        /// <param name="markdownContent">The markdown content to parse</param>
        /// <returns>Count of links</returns>
        public int CountLinks(string markdownContent)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                return 0;
            }

            return WikiLinkRegex.Matches(markdownContent).Count;
        }

        /// <summary>
        /// Check if markdown content contains any [[wiki-links]]
        /// </summary>
        /// <param name="markdownContent">The markdown content to check</param>
        /// <returns>True if contains links, false otherwise</returns>
        public bool ContainsLinks(string markdownContent)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                return false;
            }

            return WikiLinkRegex.IsMatch(markdownContent);
        }

        /// <summary>
        /// Replace [[wiki-links]] with custom replacement text
        /// Useful for rendering links as HTML or other formats
        /// </summary>
        /// <param name="markdownContent">The markdown content</param>
        /// <param name="replacementFunc">Function to generate replacement text for each link</param>
        /// <returns>Content with links replaced</returns>
        public string ReplaceLinks(string markdownContent, Func<ParsedLink, string> replacementFunc)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                return markdownContent;
            }

            var links = ExtractLinksWithContext(markdownContent);

            // Process links in reverse order to maintain correct positions
            var result = markdownContent;
            foreach (var link in links.OrderByDescending(l => l.Position))
            {
                var replacement = replacementFunc(link);
                result = result.Remove(link.Position, link.MatchedText.Length);
                result = result.Insert(link.Position, replacement);
            }

            return result;
        }

        /// <summary>
        /// Convert [[wiki-links]] to HTML anchor tags
        /// </summary>
        /// <param name="markdownContent">The markdown content</param>
        /// <param name="linkUrlTemplate">URL template with {0} placeholder for concept name</param>
        /// <returns>Content with HTML links</returns>
        public string ConvertLinksToHtml(string markdownContent, string linkUrlTemplate = "/concept/{0}")
        {
            return ReplaceLinks(markdownContent, link =>
            {
                var url = string.Format(linkUrlTemplate, Uri.EscapeDataString(link.ConceptName));
                var displayText = link.DisplayText ?? link.ConceptName;
                return $"<a href=\"{url}\" class=\"wiki-link\" data-concept=\"{link.ConceptName}\">{displayText}</a>";
            });
        }

        /// <summary>
        /// Extract context snippet around a link position
        /// </summary>
        /// <param name="content">The full content</param>
        /// <param name="linkPosition">Position where the link starts</param>
        /// <param name="linkLength">Length of the link text</param>
        /// <returns>Context snippet with ellipsis if truncated</returns>
        private string ExtractContextSnippet(string content, int linkPosition, int linkLength)
        {
            // Calculate snippet boundaries
            var startPos = Math.Max(0, linkPosition - ContextWindowSize);
            var endPos = Math.Min(content.Length, linkPosition + linkLength + ContextWindowSize);

            // Extract the snippet
            var snippet = content.Substring(startPos, endPos - startPos);

            // Add ellipsis if truncated
            var prefix = startPos > 0 ? "..." : "";
            var suffix = endPos < content.Length ? "..." : "";

            // Clean up: replace newlines with spaces, collapse multiple spaces
            snippet = Regex.Replace(snippet, @"\s+", " ").Trim();

            return $"{prefix}{snippet}{suffix}";
        }

        /// <summary>
        /// Validate a concept name (check if it's a valid wiki-link target)
        /// </summary>
        /// <param name="conceptName">The concept name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValidConceptName(string conceptName)
        {
            if (string.IsNullOrWhiteSpace(conceptName))
            {
                return false;
            }

            // Concept names cannot contain certain characters
            var invalidChars = new[] { '[', ']', '|', '\n', '\r' };

            return !conceptName.Any(c => invalidChars.Contains(c));
        }

        /// <summary>
        /// Escape a concept name for use in wiki-link syntax
        /// </summary>
        /// <param name="conceptName">The concept name to escape</param>
        /// <returns>Escaped concept name safe for use in [[]]</returns>
        public string EscapeConceptName(string conceptName)
        {
            if (string.IsNullOrWhiteSpace(conceptName))
            {
                return conceptName;
            }

            // Remove or replace invalid characters
            return conceptName
                .Replace("[", "")
                .Replace("]", "")
                .Replace("|", "-")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Trim();
        }
    }
}
