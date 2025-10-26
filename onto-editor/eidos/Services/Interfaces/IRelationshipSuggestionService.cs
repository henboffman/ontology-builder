namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for suggesting relationships based on concept properties
/// Separated from RelationshipService for Single Responsibility
/// </summary>
public interface IRelationshipSuggestionService
{
    /// <summary>
    /// Get suggested relationships for a concept based on its category and properties
    /// </summary>
    Task<List<string>> GetSuggestionsAsync(int conceptId);

    /// <summary>
    /// Get suggested relationship types based on concept categories
    /// </summary>
    Task<List<string>> GetSuggestionsByCategoryAsync(string sourceCategory, string targetCategory);
}
