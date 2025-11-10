namespace Eidos.Models.Enums;

/// <summary>
/// Type of entity that can be changed in a merge request.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// A concept entity
    /// </summary>
    Concept = 0,

    /// <summary>
    /// A relationship entity
    /// </summary>
    Relationship = 1,

    /// <summary>
    /// An individual entity
    /// </summary>
    Individual = 2,

    /// <summary>
    /// A concept property definition
    /// </summary>
    ConceptProperty = 3,

    /// <summary>
    /// Ontology metadata
    /// </summary>
    OntologyMetadata = 4
}
