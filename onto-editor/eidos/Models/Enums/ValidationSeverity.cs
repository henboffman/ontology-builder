namespace Eidos.Models.Enums;

/// <summary>
/// Severity level for validation issues
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Critical issue that must be fixed - Red indicator
    /// </summary>
    Error,

    /// <summary>
    /// Important issue that should be fixed - Yellow/Orange indicator
    /// </summary>
    Warning,

    /// <summary>
    /// Informational issue to consider fixing - Blue indicator
    /// </summary>
    Info
}
