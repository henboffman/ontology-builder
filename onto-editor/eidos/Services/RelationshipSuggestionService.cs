using Eidos.Data.Repositories;
using Eidos.Services.Interfaces;

namespace Eidos.Services;

/// <summary>
/// Service for suggesting relationships based on concept properties
/// Separated for Single Responsibility - only handles suggestion logic
/// </summary>
public class RelationshipSuggestionService : IRelationshipSuggestionService
{
    private readonly IConceptRepository _conceptRepository;

    public RelationshipSuggestionService(IConceptRepository conceptRepository)
    {
        _conceptRepository = conceptRepository;
    }

    public async Task<List<string>> GetSuggestionsAsync(int conceptId)
    {
        var concept = await _conceptRepository.GetWithPropertiesAsync(conceptId);
        if (concept == null)
            return new List<string>();

        return GetSuggestionsByCategory(concept.Category);
    }

    public Task<List<string>> GetSuggestionsByCategoryAsync(string sourceCategory, string targetCategory)
    {
        var suggestions = new List<string>();

        // Add suggestions based on source category
        suggestions.AddRange(GetSuggestionsByCategory(sourceCategory));

        // Could add logic here for category combinations
        // For now, just return source category suggestions

        return Task.FromResult(suggestions);
    }

    private List<string> GetSuggestionsByCategory(string? category)
    {
        var suggestions = new List<string>();

        if (string.IsNullOrEmpty(category))
            return suggestions;

        // BFO-based suggestions
        if (category.Contains("Continuant") || category.Contains("Material Entity"))
        {
            suggestions.Add("Continuants can use 'part-of' or 'has-part' to relate to other entities");
            suggestions.Add("Consider 'subclass-of' to relate to a parent class");
            suggestions.Add("Material entities can have 'quality' attributes");
        }
        else if (category.Contains("Occurrent") || category.Contains("Process"))
        {
            suggestions.Add("Processes can use 'has-participant' to relate to entities involved");
            suggestions.Add("Consider 'has-input' and 'has-output' for process flow");
            suggestions.Add("Use 'realizes' to connect to dispositions or functions");
        }
        else if (category.Contains("Quality"))
        {
            suggestions.Add("Qualities depend on entities - consider what they inhere in");
            suggestions.Add("Use 'subclass-of' to create quality hierarchies");
        }
        else if (category.Contains("Role") || category.Contains("Function"))
        {
            suggestions.Add("Roles are 'realized-in' processes");
            suggestions.Add("Consider what entity 'has' this role");
        }
        else if (category.Contains("Disposition"))
        {
            suggestions.Add("Dispositions are 'realized-in' specific processes");
            suggestions.Add("Consider what bearer has this disposition");
        }
        // General suggestions
        else if (category == "Entity" || category == "Thing")
        {
            suggestions.Add("Consider 'is-a' or 'subclass-of' for taxonomic relationships");
            suggestions.Add("Use 'part-of' for compositional relationships");
        }
        else if (category == "Action" || category == "Activity")
        {
            suggestions.Add("Actions can have 'has-participant' relationships");
            suggestions.Add("Consider 'has-input' and 'has-output' for action flow");
        }

        return suggestions;
    }
}
