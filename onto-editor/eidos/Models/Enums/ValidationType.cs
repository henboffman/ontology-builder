namespace Eidos.Models.Enums;

/// <summary>
/// Types of validation issues that can be detected
/// </summary>
public enum ValidationType
{
    /// <summary>
    /// Duplicate concept with same name (case-insensitive)
    /// </summary>
    DuplicateConcept,

    /// <summary>
    /// Duplicate relationship triple (Subject-Predicate-Object)
    /// </summary>
    DuplicateRelationship,

    /// <summary>
    /// Concept with no relationships (incoming or outgoing)
    /// </summary>
    OrphanedConcept,

    /// <summary>
    /// Concept with no description text
    /// </summary>
    MissingDescription,

    /// <summary>
    /// Relationship that references the same concept as source and target
    /// </summary>
    CircularRelationship,

    /// <summary>
    /// Concept with invalid URI format
    /// </summary>
    InvalidUri
}
