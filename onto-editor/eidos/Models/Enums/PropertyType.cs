namespace Eidos.Models.Enums;

/// <summary>
/// Defines the type of OWL property.
/// In OWL, properties relate individuals (instances) to values or other individuals.
/// </summary>
public enum PropertyType
{
    /// <summary>
    /// Datatype Property - relates individuals to literal values (strings, numbers, dates, etc.)
    /// Example: Person → age → "25" (integer)
    /// Example: Person → name → "John" (string)
    /// </summary>
    DataProperty,

    /// <summary>
    /// Object Property - relates individuals to other individuals
    /// Example: Person → knows → Person
    /// Example: Book → hasAuthor → Person
    /// </summary>
    ObjectProperty
}
