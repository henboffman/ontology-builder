using System;
using System.ComponentModel.DataAnnotations;

namespace Eidos.Models
{
    /// <summary>
    /// Represents a restriction on a concept's properties (similar to OWL restrictions)
    /// Examples: cardinality constraints, value restrictions, type constraints
    /// </summary>
    public class ConceptRestriction
    {
        public int Id { get; set; }

        public int ConceptId { get; set; }
        public Concept Concept { get; set; } = null!;

        /// <summary>
        /// The property this restriction applies to (e.g., "hasParent", "age", "birthDate")
        /// </summary>
        [Required]
        [StringLength(200)]
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Type of restriction: Cardinality, ValueType, Range, Required, Enumeration, Pattern
        /// </summary>
        [Required]
        [StringLength(50)]
        public string RestrictionType { get; set; } = string.Empty;

        /// <summary>
        /// Minimum cardinality (for Cardinality restrictions)
        /// null means no minimum constraint
        /// </summary>
        public int? MinCardinality { get; set; }

        /// <summary>
        /// Maximum cardinality (for Cardinality restrictions)
        /// null means no maximum constraint (unbounded)
        /// </summary>
        public int? MaxCardinality { get; set; }

        /// <summary>
        /// Expected data type for the property value
        /// Examples: "string", "integer", "decimal", "boolean", "date", "uri", "concept"
        /// </summary>
        [StringLength(50)]
        public string? ValueType { get; set; }

        /// <summary>
        /// Minimum value (for Range restrictions on numeric/date properties)
        /// Stored as string to accommodate different types
        /// </summary>
        public string? MinValue { get; set; }

        /// <summary>
        /// Maximum value (for Range restrictions on numeric/date properties)
        /// Stored as string to accommodate different types
        /// </summary>
        public string? MaxValue { get; set; }

        /// <summary>
        /// For concept-valued properties, the ID of the concept that values must be instances of
        /// Example: "hasParent" property must reference an individual of concept "Person"
        /// </summary>
        public int? AllowedConceptId { get; set; }

        /// <summary>
        /// Navigation property to the allowed concept (for type restrictions)
        /// </summary>
        public Concept? AllowedConcept { get; set; }

        /// <summary>
        /// Comma-separated list of allowed values (for Enumeration restrictions)
        /// Example: "red,green,blue" for a color property
        /// </summary>
        public string? AllowedValues { get; set; }

        /// <summary>
        /// Regular expression pattern for string validation (for Pattern restrictions)
        /// Example: "^[A-Z]{2}[0-9]{4}$" for a format like "AB1234"
        /// </summary>
        public string? Pattern { get; set; }

        /// <summary>
        /// Human-readable description of the restriction
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether this restriction is mandatory (hard constraint) or advisory (soft constraint)
        /// </summary>
        public bool IsMandatory { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Common restriction types for concept properties
    /// </summary>
    public static class RestrictionTypes
    {
        /// <summary>
        /// Cardinality restriction: controls how many times a property can appear
        /// Uses MinCardinality and MaxCardinality
        /// </summary>
        public const string Cardinality = "Cardinality";

        /// <summary>
        /// Value type restriction: specifies the expected data type
        /// Uses ValueType field
        /// </summary>
        public const string ValueType = "ValueType";

        /// <summary>
        /// Range restriction: specifies min/max values for numeric or date properties
        /// Uses MinValue and MaxValue
        /// </summary>
        public const string Range = "Range";

        /// <summary>
        /// Required restriction: property must have a value
        /// Equivalent to MinCardinality = 1
        /// </summary>
        public const string Required = "Required";

        /// <summary>
        /// Enumeration restriction: value must be from a predefined list
        /// Uses AllowedValues
        /// </summary>
        public const string Enumeration = "Enumeration";

        /// <summary>
        /// Pattern restriction: string value must match a regex pattern
        /// Uses Pattern field
        /// </summary>
        public const string Pattern = "Pattern";

        /// <summary>
        /// Concept restriction: for object properties, specifies the concept that values must instantiate
        /// Uses AllowedConceptId
        /// </summary>
        public const string ConceptType = "ConceptType";
    }
}
