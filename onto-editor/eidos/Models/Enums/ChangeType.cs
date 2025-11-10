namespace Eidos.Models.Enums;

/// <summary>
/// Type of change in a merge request.
/// </summary>
public enum MergeRequestChangeType
{
    /// <summary>
    /// Adding a new entity
    /// </summary>
    Add = 0,

    /// <summary>
    /// Modifying an existing entity
    /// </summary>
    Modify = 1,

    /// <summary>
    /// Deleting an existing entity
    /// </summary>
    Delete = 2
}
