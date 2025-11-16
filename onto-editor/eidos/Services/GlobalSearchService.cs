using Eidos.Models;
using Microsoft.Extensions.Logging;

namespace Eidos.Services;

/// <summary>
/// Service for performing global search across ontology entities (concepts, relationships, individuals).
/// Provides fast in-memory substring search with grouped results.
/// </summary>
public class GlobalSearchService
{
    private readonly ILogger<GlobalSearchService> _logger;

    public GlobalSearchService(ILogger<GlobalSearchService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Performs a global search across all entities in the given ontology.
    /// </summary>
    /// <param name="ontology">The ontology to search within</param>
    /// <param name="query">The search query (case-insensitive substring match)</param>
    /// <param name="maxResults">Maximum number of results per category (default: 10)</param>
    /// <param name="notes">Optional list of notes to search (from workspace)</param>
    /// <returns>Grouped search results</returns>
    public SearchResults Search(Ontology ontology, string query, int maxResults = 10, List<Note>? notes = null)
    {
        if (ontology == null)
        {
            _logger.LogWarning("Search called with null ontology");
            return new SearchResults();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResults();
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        var results = new SearchResults
        {
            Actions = SearchActions(normalizedQuery, maxResults),
            Concepts = SearchConcepts(ontology, normalizedQuery, maxResults),
            Relationships = SearchRelationships(ontology, normalizedQuery, maxResults),
            Individuals = SearchIndividuals(ontology, normalizedQuery, maxResults),
            Notes = SearchNotes(notes, normalizedQuery, maxResults)
        };

        _logger.LogInformation("Global search for '{Query}' - Notes param: {NotesCount}, Note results: {NoteResults}, Total: {TotalCount}",
            query, notes?.Count ?? 0, results.Notes.Count, results.TotalCount);

        return results;
    }

    /// <summary>
    /// Searches available command palette actions.
    /// </summary>
    private List<SearchResult> SearchActions(string query, int maxResults)
    {
        var allActions = new List<SearchResult>
        {
            new SearchResult
            {
                Type = "Action",
                ActionId = "add-concept",
                Title = "Add Concept",
                Subtitle = "Create a new concept in this ontology",
                Icon = "bi-plus-circle",
                Shortcut = "⌘⇧C",
                MatchedText = "Create"
            },
            new SearchResult
            {
                Type = "Action",
                ActionId = "add-relationship",
                Title = "Add Relationship",
                Subtitle = "Create a new relationship between concepts",
                Icon = "bi-arrow-left-right",
                Shortcut = "⌘⇧R",
                MatchedText = "Create"
            },
            new SearchResult
            {
                Type = "Action",
                ActionId = "export-ttl",
                Title = "Export to TTL",
                Subtitle = "Export ontology as Turtle format",
                Icon = "bi-download",
                MatchedText = "Export"
            },
            new SearchResult
            {
                Type = "Action",
                ActionId = "export-json",
                Title = "Export to JSON",
                Subtitle = "Export ontology as JSON format",
                Icon = "bi-file-earmark-code",
                MatchedText = "Export"
            },
            new SearchResult
            {
                Type = "Action",
                ActionId = "import-ttl",
                Title = "Import from TTL",
                Subtitle = "Import ontology from Turtle file",
                Icon = "bi-upload",
                MatchedText = "Import"
            },
            new SearchResult
            {
                Type = "Action",
                ActionId = "toggle-theme",
                Title = "Toggle Dark Mode",
                Subtitle = "Switch between light and dark themes",
                Icon = "bi-moon-stars",
                MatchedText = "Theme"
            },
            new SearchResult
            {
                Type = "Action",
                ActionId = "view-graph",
                Title = "Switch to Graph View",
                Subtitle = "View ontology as interactive graph",
                Icon = "bi-diagram-3",
                MatchedText = "View"
            },
            new SearchResult
            {
                Type = "Action",
                ActionId = "view-list",
                Title = "Switch to List View",
                Subtitle = "View concepts and relationships as lists",
                Icon = "bi-list-ul",
                MatchedText = "View"
            },
            new SearchResult
            {
                Type = "Action",
                ActionId = "view-hierarchy",
                Title = "Switch to Hierarchy View",
                Subtitle = "View concept hierarchy tree",
                Icon = "bi-diagram-2",
                MatchedText = "View"
            },
            new SearchResult
            {
                Type = "Action",
                ActionId = "settings",
                Title = "Ontology Settings",
                Subtitle = "Edit ontology metadata and permissions",
                Icon = "bi-gear",
                MatchedText = "Settings"
            }
        };

        // Filter actions based on query
        return allActions
            .Where(a =>
                a.Title.ToLowerInvariant().Contains(query) ||
                a.Subtitle.ToLowerInvariant().Contains(query) ||
                a.MatchedText.ToLowerInvariant().Contains(query))
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Searches concepts by name, definition, explanation, examples, and category.
    /// </summary>
    private List<SearchResult> SearchConcepts(Ontology ontology, string query, int maxResults)
    {
        if (ontology.Concepts == null || !ontology.Concepts.Any())
        {
            return new List<SearchResult>();
        }

        return ontology.Concepts
            .Where(c =>
                (c.Name?.ToLowerInvariant().Contains(query) == true) ||
                (c.Definition?.ToLowerInvariant().Contains(query) == true) ||
                (c.SimpleExplanation?.ToLowerInvariant().Contains(query) == true) ||
                (c.Examples?.ToLowerInvariant().Contains(query) == true) ||
                (c.Category?.ToLowerInvariant().Contains(query) == true))
            .Take(maxResults)
            .Select(c => new SearchResult
            {
                Type = "Concept",
                Id = c.Id,
                Title = c.Name,
                Subtitle = GetConceptSubtitle(c, query),
                MatchedText = GetMatchedField(c, query),
                Icon = "bi-diagram-3"
            })
            .ToList();
    }

    /// <summary>
    /// Searches relationships by type and description.
    /// </summary>
    private List<SearchResult> SearchRelationships(Ontology ontology, string query, int maxResults)
    {
        if (ontology.Relationships == null || !ontology.Relationships.Any())
        {
            return new List<SearchResult>();
        }

        return ontology.Relationships
            .Where(r =>
                (r.RelationType?.ToLowerInvariant().Contains(query) == true) ||
                (r.Description?.ToLowerInvariant().Contains(query) == true))
            .Take(maxResults)
            .Select(r => new SearchResult
            {
                Type = "Relationship",
                Id = r.Id,
                Title = r.RelationType ?? "Unnamed Relationship",
                Subtitle = $"{r.SourceConcept?.Name} → {r.TargetConcept?.Name}",
                MatchedText = r.RelationType?.ToLowerInvariant().Contains(query) == true
                    ? "Type"
                    : "Description",
                Icon = "bi-arrow-left-right"
            })
            .ToList();
    }

    /// <summary>
    /// Searches individuals by name, label, and description.
    /// </summary>
    private List<SearchResult> SearchIndividuals(Ontology ontology, string query, int maxResults)
    {
        if (ontology.Individuals == null || !ontology.Individuals.Any())
        {
            return new List<SearchResult>();
        }

        return ontology.Individuals
            .Where(i =>
                (i.Name?.ToLowerInvariant().Contains(query) == true) ||
                (i.Label?.ToLowerInvariant().Contains(query) == true) ||
                (i.Description?.ToLowerInvariant().Contains(query) == true))
            .Take(maxResults)
            .Select(i => new SearchResult
            {
                Type = "Individual",
                Id = i.Id,
                Title = i.Name ?? i.Label ?? "Unnamed Individual",
                Subtitle = i.Concept != null ? $"Instance of {i.Concept.Name}" : "Individual",
                MatchedText = GetIndividualMatchedField(i, query),
                Icon = "bi-person-fill"
            })
            .ToList();
    }

    /// <summary>
    /// Searches notes by title and content.
    /// </summary>
    private List<SearchResult> SearchNotes(List<Note>? notes, string query, int maxResults)
    {
        if (notes == null || !notes.Any())
        {
            return new List<SearchResult>();
        }

        return notes
            .Where(n =>
                (n.Title?.ToLowerInvariant().Contains(query) == true) ||
                (n.Content?.MarkdownContent?.ToLowerInvariant().Contains(query) == true))
            .Take(maxResults)
            .Select(n => new SearchResult
            {
                Type = "Note",
                Id = n.Id,
                Title = n.Title ?? "Untitled Note",
                Subtitle = GetNoteSubtitle(n, query),
                MatchedText = GetNoteMatchedField(n, query),
                Icon = n.IsConceptNote ? "bi-file-earmark-text" : "bi-journal-text"
            })
            .ToList();
    }

    /// <summary>
    /// Gets a subtitle for a concept based on what matched.
    /// </summary>
    private string GetConceptSubtitle(Concept concept, string query)
    {
        // Prioritize showing the definition if it matches
        if (concept.Definition?.ToLowerInvariant().Contains(query) == true)
        {
            return TruncateText(concept.Definition, 80);
        }

        if (concept.SimpleExplanation?.ToLowerInvariant().Contains(query) == true)
        {
            return TruncateText(concept.SimpleExplanation, 80);
        }

        // Otherwise show definition or category
        if (!string.IsNullOrEmpty(concept.Definition))
        {
            return TruncateText(concept.Definition, 80);
        }

        if (!string.IsNullOrEmpty(concept.Category))
        {
            return $"Category: {concept.Category}";
        }

        return "Concept";
    }

    /// <summary>
    /// Gets which field matched for a concept.
    /// </summary>
    private string GetMatchedField(Concept concept, string query)
    {
        if (concept.Name?.ToLowerInvariant().Contains(query) == true)
            return "Name";
        if (concept.Definition?.ToLowerInvariant().Contains(query) == true)
            return "Definition";
        if (concept.SimpleExplanation?.ToLowerInvariant().Contains(query) == true)
            return "Explanation";
        if (concept.Examples?.ToLowerInvariant().Contains(query) == true)
            return "Examples";
        if (concept.Category?.ToLowerInvariant().Contains(query) == true)
            return "Category";
        return "Name";
    }

    /// <summary>
    /// Gets which field matched for an individual.
    /// </summary>
    private string GetIndividualMatchedField(Individual individual, string query)
    {
        if (individual.Name?.ToLowerInvariant().Contains(query) == true)
            return "Name";
        if (individual.Label?.ToLowerInvariant().Contains(query) == true)
            return "Label";
        if (individual.Description?.ToLowerInvariant().Contains(query) == true)
            return "Description";
        return "Name";
    }

    /// <summary>
    /// Gets a subtitle for a note based on what matched.
    /// </summary>
    private string GetNoteSubtitle(Note note, string query)
    {
        // If content matches, show a preview
        if (note.Content?.MarkdownContent?.ToLowerInvariant().Contains(query) == true)
        {
            return TruncateText(note.Content.MarkdownContent, 80);
        }

        // Otherwise show note type
        if (note.IsConceptNote)
        {
            return "Concept Note";
        }

        return "Note";
    }

    /// <summary>
    /// Gets which field matched for a note.
    /// </summary>
    private string GetNoteMatchedField(Note note, string query)
    {
        if (note.Title?.ToLowerInvariant().Contains(query) == true)
            return "Title";
        if (note.Content?.MarkdownContent?.ToLowerInvariant().Contains(query) == true)
            return "Content";
        return "Title";
    }

    /// <summary>
    /// Truncates text to a maximum length with ellipsis.
    /// </summary>
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength - 3) + "...";
    }
}

/// <summary>
/// Represents a single search result item.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Type of entity: "Concept", "Relationship", "Individual", or "Action"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID (0 for actions)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Primary display text (e.g., name)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Secondary display text (e.g., definition preview, concept names)
    /// </summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// Which field matched the search query
    /// </summary>
    public string MatchedText { get; set; } = string.Empty;

    /// <summary>
    /// Bootstrap icon class for the result
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Action identifier for command palette actions
    /// </summary>
    public string? ActionId { get; set; }

    /// <summary>
    /// Keyboard shortcut hint (e.g., "Ctrl+N")
    /// </summary>
    public string? Shortcut { get; set; }
}

/// <summary>
/// Grouped search results by entity type.
/// </summary>
public class SearchResults
{
    /// <summary>
    /// Command palette actions
    /// </summary>
    public List<SearchResult> Actions { get; set; } = new();

    /// <summary>
    /// Concept search results
    /// </summary>
    public List<SearchResult> Concepts { get; set; } = new();

    /// <summary>
    /// Relationship search results
    /// </summary>
    public List<SearchResult> Relationships { get; set; } = new();

    /// <summary>
    /// Individual search results
    /// </summary>
    public List<SearchResult> Individuals { get; set; } = new();

    /// <summary>
    /// Note search results
    /// </summary>
    public List<SearchResult> Notes { get; set; } = new();

    /// <summary>
    /// Total count of all results
    /// </summary>
    public int TotalCount => Actions.Count + Concepts.Count + Relationships.Count + Individuals.Count + Notes.Count;

    /// <summary>
    /// Gets all results flattened into a single list (for keyboard navigation)
    /// </summary>
    public List<SearchResult> All()
    {
        var all = new List<SearchResult>();
        all.AddRange(Actions);
        all.AddRange(Concepts);
        all.AddRange(Relationships);
        all.AddRange(Individuals);
        all.AddRange(Notes);
        return all;
    }
}
