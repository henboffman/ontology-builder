using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Eidos.Models.Enums;

namespace Eidos.Models;

/// <summary>
/// Represents a property definition for a concept (OWL class).
/// Properties define the schema/structure that instances of the concept can have.
/// This is different from IndividualProperty which holds actual values for instances.
///
/// In OWL terms:
/// - ConceptProperty defines the property with its domain and range
/// - IndividualProperty asserts property values for specific individuals
/// </summary>
public class ConceptProperty
{
    public int Id { get; set; }

    /// <summary>
    /// The concept (class) that this property is defined for (the domain in OWL)
    /// </summary>
    public int ConceptId { get; set; }

    [JsonIgnore]
    public Concept Concept { get; set; } = null!;

    /// <summary>
    /// Name of the property (e.g., "age", "name", "knows", "hasAuthor")
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of property: DataProperty (relates to literal values) or ObjectProperty (relates to other individuals)
    /// </summary>
    [Required]
    public PropertyType PropertyType { get; set; }

    /// <summary>
    /// For DataProperty: the type of literal value (e.g., "string", "integer", "decimal", "boolean", "date")
    /// Corresponds to XSD datatypes in RDF/OWL
    /// </summary>
    [StringLength(50)]
    public string? DataType { get; set; }

    /// <summary>
    /// For ObjectProperty: the concept (class) that this property relates to (the range in OWL)
    /// Example: For "knows" property on Person, this would point to Person concept
    /// </summary>
    public int? RangeConceptId { get; set; }

    [JsonIgnore]
    public Concept? RangeConcept { get; set; }

    /// <summary>
    /// Whether this property is required for instances of the concept
    /// In OWL, this corresponds to minimum cardinality restriction
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether this property can have only one value (functional property in OWL)
    /// Example: A person can have only one "birthDate"
    /// If false, property can have multiple values (e.g., person can "know" multiple people)
    /// </summary>
    public bool IsFunctional { get; set; }

    /// <summary>
    /// Human-readable description of what this property represents
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URI for this property (for RDF/OWL export)
    /// If not specified, will be generated from the ontology namespace and property name
    /// </summary>
    [StringLength(500)]
    public string? Uri { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
