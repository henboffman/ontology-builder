namespace Eidos.Models.Enums;

/// <summary>
/// Priority level for a merge request.
/// </summary>
public enum MergeRequestPriority
{
    /// <summary>
    /// Low priority - can be reviewed when convenient
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - standard review timeline
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - needs faster review
    /// </summary>
    High = 2,

    /// <summary>
    /// Urgent - needs immediate attention
    /// </summary>
    Urgent = 3
}
