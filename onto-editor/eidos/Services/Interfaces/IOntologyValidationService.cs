using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for validating ontology data quality and detecting issues
/// </summary>
public interface IOntologyValidationService
{
    /// <summary>
    /// Validates an entire ontology and returns all issues found
    /// </summary>
    /// <param name="ontologyId">ID of the ontology to validate</param>
    /// <returns>Validation result with all issues</returns>
    Task<ValidationResult> ValidateOntologyAsync(int ontologyId);

    /// <summary>
    /// Gets validation issues for a specific ontology (cached if available)
    /// </summary>
    /// <param name="ontologyId">ID of the ontology</param>
    /// <returns>List of validation issues</returns>
    Task<List<ValidationIssue>> GetIssuesAsync(int ontologyId);

    /// <summary>
    /// Invalidates the validation cache for an ontology, forcing re-validation
    /// </summary>
    /// <param name="ontologyId">ID of the ontology</param>
    void InvalidateCache(int ontologyId);
}
