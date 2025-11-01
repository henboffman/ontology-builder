namespace Eidos.Models;

/// <summary>
/// Result of validating an ontology
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// ID of the ontology that was validated
    /// </summary>
    public int OntologyId { get; set; }

    /// <summary>
    /// Total number of issues found
    /// </summary>
    public int TotalIssues => Issues.Count;

    /// <summary>
    /// Number of error-level issues
    /// </summary>
    public int ErrorCount => Issues.Count(i => i.Severity == Enums.ValidationSeverity.Error);

    /// <summary>
    /// Number of warning-level issues
    /// </summary>
    public int WarningCount => Issues.Count(i => i.Severity == Enums.ValidationSeverity.Warning);

    /// <summary>
    /// Number of info-level issues
    /// </summary>
    public int InfoCount => Issues.Count(i => i.Severity == Enums.ValidationSeverity.Info);

    /// <summary>
    /// All validation issues found
    /// </summary>
    public List<ValidationIssue> Issues { get; set; } = new();

    /// <summary>
    /// Timestamp when validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the ontology passes validation (no errors)
    /// </summary>
    public bool IsValid => ErrorCount == 0;
}
