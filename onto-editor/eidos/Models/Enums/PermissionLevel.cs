namespace Eidos.Models.Enums;

/// <summary>
/// Defines permission levels for collaborative access to ontologies
/// </summary>
public enum PermissionLevel
{
    /// <summary>
    /// Can only view the ontology (read-only)
    /// </summary>
    View = 0,

    /// <summary>
    /// Can view and add new concepts/relationships but cannot edit existing ones
    /// </summary>
    ViewAndAdd = 1,

    /// <summary>
    /// Can view, add, and edit concepts and relationships
    /// </summary>
    ViewAddEdit = 2,

    /// <summary>
    /// Full access including delete and ontology settings (same as owner)
    /// </summary>
    FullAccess = 3
}
