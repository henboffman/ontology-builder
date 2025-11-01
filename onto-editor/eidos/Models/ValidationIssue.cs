using Eidos.Models.Enums;

namespace Eidos.Models;

/// <summary>
/// Represents a validation issue found in an ontology
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// Unique identifier for this issue instance
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Severity level (Error, Warning, Info)
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// Type of validation issue
    /// </summary>
    public ValidationType Type { get; set; }

    /// <summary>
    /// Entity type that has the issue ("Concept" or "Relationship")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Database ID of the entity
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Name or display text of the entity
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Short message describing the issue
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed information about the issue
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Suggested action to resolve the issue
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;

    /// <summary>
    /// For duplicate issues, the ID of the other conflicting entity
    /// </summary>
    public int? RelatedEntityId { get; set; }

    /// <summary>
    /// For duplicate issues, the name of the other conflicting entity
    /// </summary>
    public string? RelatedEntityName { get; set; }
}
