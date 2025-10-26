using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Eidos.Models
{
    /// <summary>
    /// Represents an individual instance of a concept (e.g., "Socrates" is an individual of type "Person")
    /// In OWL/RDF terms, this is a "named individual" - a specific member of a class
    /// </summary>
    public class Individual
    {
        public int Id { get; set; }

        public int OntologyId { get; set; }
        public Ontology Ontology { get; set; } = null!;

        /// <summary>
        /// The concept (class) that this individual is an instance of
        /// </summary>
        public int ConceptId { get; set; }
        public Concept Concept { get; set; } = null!;

        /// <summary>
        /// Name of this individual instance (e.g., "Socrates", "Golden Gate Bridge")
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description or notes about this individual
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Optional label for display purposes
        /// </summary>
        [StringLength(200)]
        public string? Label { get; set; }

        /// <summary>
        /// URI for this individual (for RDF/OWL export)
        /// If not specified, will be generated from the namespace and name
        /// </summary>
        [StringLength(500)]
        public string? Uri { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<IndividualProperty> Properties { get; set; } = new List<IndividualProperty>();
        public ICollection<IndividualRelationship> RelationshipsAsSource { get; set; } = new List<IndividualRelationship>();
        public ICollection<IndividualRelationship> RelationshipsAsTarget { get; set; } = new List<IndividualRelationship>();
    }

    /// <summary>
    /// Property values for an individual instance
    /// Unlike concept properties (which define schema), these are actual values
    /// Example: Individual "Socrates" has property "birthYear" = "-470"
    /// </summary>
    public class IndividualProperty
    {
        public int Id { get; set; }

        public int IndividualId { get; set; }
        public Individual Individual { get; set; } = null!;

        /// <summary>
        /// Name of the property (e.g., "birthYear", "nationality")
        /// Could optionally link to a Property definition from the Concept
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The actual value for this property (stored as string, interpreted by DataType)
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Data type: "string", "number", "boolean", "date", "uri", etc.
        /// </summary>
        [StringLength(50)]
        public string? DataType { get; set; }

        /// <summary>
        /// Optional description of this property value
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Optional reference to a Property definition from the Concept
        /// </summary>
        public int? ConceptPropertyId { get; set; }
    }

    /// <summary>
    /// Relationships between individual instances (object properties in OWL)
    /// Example: Individual "Socrates" has relationship "teaches" to individual "Plato"
    /// </summary>
    public class IndividualRelationship
    {
        public int Id { get; set; }

        public int OntologyId { get; set; }
        public Ontology Ontology { get; set; } = null!;

        /// <summary>
        /// Source individual (subject)
        /// </summary>
        public int SourceIndividualId { get; set; }
        public Individual SourceIndividual { get; set; } = null!;

        /// <summary>
        /// Target individual (object)
        /// </summary>
        public int TargetIndividualId { get; set; }
        public Individual TargetIndividual { get; set; } = null!;

        /// <summary>
        /// Type of relationship (e.g., "teaches", "locatedIn", "knows")
        /// Can be based on relationship types defined between concepts
        /// </summary>
        [Required]
        [StringLength(200)]
        public string RelationType { get; set; } = string.Empty;

        /// <summary>
        /// Optional custom label for display
        /// </summary>
        [StringLength(200)]
        public string? Label { get; set; }

        /// <summary>
        /// Optional description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// URI for this relationship type (for RDF/OWL export)
        /// </summary>
        [StringLength(500)]
        public string? OntologyUri { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
