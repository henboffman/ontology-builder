using System.Text.RegularExpressions;

namespace Eidos.Services;

/// <summary>
/// Utility class for parsing @mentions from comment text.
/// Supports both email-based mentions (@user@domain.com) and username mentions (@username).
/// </summary>
public class MentionParser
{
    // Regex pattern matches:
    // - @username (alphanumeric, dots, hyphens, underscores)
    // - @email@domain.com (full email addresses)
    private static readonly Regex MentionRegex = new(
        @"@([\w.-]+@[\w.-]+|[\w.]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Extracts all unique mentions from comment text.
    /// Returns the text after the @ symbol (either username or full email).
    /// </summary>
    /// <param name="text">Comment text containing @mentions</param>
    /// <returns>List of unique mentioned usernames/emails</returns>
    public static List<string> ExtractMentions(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        var matches = MentionRegex.Matches(text);
        var mentions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var mention = match.Groups[1].Value;
                mentions.Add(mention);
            }
        }

        return mentions.ToList();
    }

    /// <summary>
    /// Validates if a string is a well-formed mention (starts with @ and contains valid characters).
    /// </summary>
    /// <param name="mention">The mention to validate (with or without @)</param>
    /// <returns>True if valid mention format</returns>
    public static bool IsValidMention(string mention)
    {
        if (string.IsNullOrWhiteSpace(mention))
        {
            return false;
        }

        // Remove leading @ if present
        var cleanMention = mention.TrimStart('@');

        // Check if it matches our pattern
        return MentionRegex.IsMatch($"@{cleanMention}");
    }

    /// <summary>
    /// Highlights mentions in text by wrapping them in HTML <span> tags.
    /// Useful for rendering comments with highlighted mentions.
    /// </summary>
    /// <param name="text">Comment text</param>
    /// <param name="cssClass">CSS class to apply to mention spans</param>
    /// <returns>Text with mentions wrapped in spans</returns>
    public static string HighlightMentions(string text, string cssClass = "mention")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return MentionRegex.Replace(text, match =>
        {
            return $"<span class=\"{cssClass}\">{match.Value}</span>";
        });
    }

    /// <summary>
    /// Resolves mentions to user IDs by looking up users in the database.
    /// </summary>
    /// <param name="mentions">List of mention strings (username or email)</param>
    /// <param name="userLookupFunc">Function to look up user ID by email or username</param>
    /// <returns>Dictionary mapping mention text to user ID</returns>
    public static async Task<Dictionary<string, string>> ResolveMentionsToUserIdsAsync(
        List<string> mentions,
        Func<string, Task<string?>> userLookupFunc)
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mention in mentions)
        {
            var userId = await userLookupFunc(mention);
            if (!string.IsNullOrEmpty(userId))
            {
                resolved[mention] = userId;
            }
        }

        return resolved;
    }
}
