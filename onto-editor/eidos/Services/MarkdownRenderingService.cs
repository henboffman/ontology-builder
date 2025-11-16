using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text.RegularExpressions;

namespace Eidos.Services
{
    /// <summary>
    /// Service for rendering markdown with custom [[wiki-link]] support
    /// Converts [[concept name]] into clickable HTML links
    /// </summary>
    public class MarkdownRenderingService
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownRenderingService()
        {
            // Configure Markdig pipeline with extensions
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
        }

        /// <summary>
        /// Render markdown to HTML with wiki-links converted to clickable elements
        /// </summary>
        public string RenderToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return string.Empty;
            }

            // First, convert [[wiki-links]] to temporary placeholders
            // This prevents Markdig from interfering with them
            var processed = PreprocessWikiLinks(markdown);

            // Render markdown to HTML
            var html = Markdown.ToHtml(processed, _pipeline);

            // Convert placeholders back to HTML wiki-links
            html = PostprocessWikiLinks(html);

            return html;
        }

        /// <summary>
        /// Convert [[wiki-links]] to temporary placeholders before markdown processing
        /// </summary>
        private string PreprocessWikiLinks(string markdown)
        {
            // Match [[concept name]]
            var wikiLinkRegex = new Regex(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

            return wikiLinkRegex.Replace(markdown, match =>
            {
                var conceptName = match.Groups[1].Value;
                // Use a unique marker wrapped in a span that won't be affected by markdown rendering
                var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(conceptName));
                return $"<span data-wikilink=\"{base64}\">{System.Web.HttpUtility.HtmlEncode(conceptName)}</span>";
            });
        }

        /// <summary>
        /// Convert placeholders back to clickable HTML wiki-links
        /// </summary>
        private string PostprocessWikiLinks(string html)
        {
            // Match the span elements we created
            var placeholderRegex = new Regex(@"<span data-wikilink=""([^""]+)"">([^<]+)</span>", RegexOptions.Compiled);

            return placeholderRegex.Replace(html, match =>
            {
                var base64 = match.Groups[1].Value;
                var displayText = match.Groups[2].Value;

                try
                {
                    var conceptName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                    // Create a clickable link that can be handled by JavaScript
                    return $"<a href=\"#\" class=\"wiki-link\" data-concept=\"{System.Web.HttpUtility.HtmlEncode(conceptName)}\" onclick=\"return handleWikiLinkClick(this, event);\">{System.Web.HttpUtility.HtmlEncode(conceptName)}</a>";
                }
                catch
                {
                    // If decoding fails, return the original text
                    return displayText;
                }
            });
        }

        /// <summary>
        /// Extract all wiki-links from markdown text
        /// </summary>
        public List<string> ExtractWikiLinks(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return new List<string>();
            }

            var wikiLinkRegex = new Regex(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);
            var matches = wikiLinkRegex.Matches(markdown);

            return matches
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Render plain text preview (no HTML, just the text)
        /// </summary>
        public string RenderToPlainText(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return string.Empty;
            }

            // Replace wiki-links with just the concept name
            var wikiLinkRegex = new Regex(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);
            var text = wikiLinkRegex.Replace(markdown, "$1");

            // Parse markdown
            var document = Markdown.Parse(text, _pipeline);

            // Extract plain text
            return ExtractPlainText(document);
        }

        private string ExtractPlainText(MarkdownObject obj)
        {
            var text = new System.Text.StringBuilder();

            if (obj is LeafBlock leafBlock)
            {
                if (leafBlock.Inline != null)
                {
                    foreach (var inline in leafBlock.Inline)
                    {
                        if (inline is LiteralInline literal)
                        {
                            text.Append(literal.Content.ToString());
                        }
                    }
                }
            }
            else if (obj is ContainerBlock containerBlock)
            {
                foreach (var child in containerBlock)
                {
                    text.Append(ExtractPlainText(child));
                    text.Append(" ");
                }
            }

            return text.ToString();
        }
    }
}
