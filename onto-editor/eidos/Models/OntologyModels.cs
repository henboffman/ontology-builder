using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Eidos.Models
{
    /// <summary>
    /// Represents a complete ontology - a structured knowledge domain
    /// </summary>
    public class Ontology
    {
        public int Id { get; set; }

        /// <summary>
        /// The workspace this ontology belongs to
        /// </summary>
        public int? WorkspaceId { get; set; }
        public Workspace? Workspace { get; set; }

        /// <summary>
        /// The user who owns this ontology (ApplicationUser ID from Identity)
        /// NOTE: This is deprecated in favor of Workspace.UserId, but kept for backwards compatibility during migration
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Namespace configuration for TTL export
        [StringLength(500)]
        public string? Namespace { get; set; }

        /// <summary>
        /// Stores namespace prefix mappings from imported TTL files as JSON
        /// Format: {"prefix": "namespace_uri", "ex": "http://example.org/"}
        /// This preserves the original prefixes when round-tripping TTL files
        /// </summary>
        public string? NamespacePrefixes { get; set; }

        // Tags for categorization (stored as comma-separated string)
        public string? Tags { get; set; }

        // License information
        [StringLength(200)]
        public string? License { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? Author { get; set; }
        public string? Version { get; set; }

        // Denormalized counts for performance (updated on concept/relationship changes)
        public int ConceptCount { get; set; } = 0;
        public int RelationshipCount { get; set; } = 0;

        // Ontology framework tracking
        public bool UsesBFO { get; set; } = false; // Uses Basic Formal Ontology as foundation
        public bool UsesProvO { get; set; } = false; // Uses PROV-O for provenance tracking

        // Notes - markdown format for documentation
        public string? Notes { get; set; }

        // Provenance tracking - for fork/clone functionality
        /// <summary>
        /// ID of the ontology this was forked/derived from (if applicable)
        /// </summary>
        public int? ParentOntologyId { get; set; }

        /// <summary>
        /// Navigation property to the parent ontology (if forked)
        /// </summary>
        public Ontology? ParentOntology { get; set; }

        /// <summary>
        /// Type of derivation: "fork", "clone", "derived", "template", or null if original
        /// </summary>
        [StringLength(50)]
        public string? ProvenanceType { get; set; }

        /// <summary>
        /// Optional notes about why/how this ontology was derived
        /// </summary>
        public string? ProvenanceNotes { get; set; }

        // Visibility and Permissions
        /// <summary>
        /// Visibility level: "private", "group", "public"
        /// - private: Only owner can access
        /// - group: Owner + specified groups can access
        /// - public: Anyone can view (read-only by default)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Visibility { get; set; } = "private";

        /// <summary>
        /// Whether public users can edit (only relevant if Visibility is "public")
        /// </summary>
        public bool AllowPublicEdit { get; set; } = false;

        /// <summary>
        /// Whether this ontology requires approval for changes.
        /// When enabled, users with Edit permissions create merge requests instead of direct edits.
        /// Only users with FullAccess can approve/reject merge requests.
        /// </summary>
        public bool RequiresApproval { get; set; } = false;

        // Navigation properties
        public ICollection<Concept> Concepts { get; set; } = new List<Concept>();
        public ICollection<Relationship> Relationships { get; set; } = new List<Relationship>();
        public ICollection<CustomConceptTemplate> CustomTemplates { get; set; } = new List<CustomConceptTemplate>();
        public ICollection<OntologyLink> LinkedOntologies { get; set; } = new List<OntologyLink>();
        public ICollection<Individual> Individuals { get; set; } = new List<Individual>();
        public ICollection<IndividualRelationship> IndividualRelationships { get; set; } = new List<IndividualRelationship>();

        /// <summary>
        /// Ontologies that were forked/derived from this one
        /// </summary>
        public ICollection<Ontology> ChildOntologies { get; set; } = new List<Ontology>();

        /// <summary>
        /// Group permissions for this ontology
        /// </summary>
        public ICollection<OntologyGroupPermission> GroupPermissions { get; set; } = new List<OntologyGroupPermission>();

        /// <summary>
        /// Tags/folders for organizing ontologies (relational model)
        /// </summary>
        public ICollection<OntologyTag> OntologyTags { get; set; } = new List<OntologyTag>();
    }

/// <summary>
/// Visibility level constants for ontologies
/// </summary>
public static class OntologyVisibility
{
    public const string Private = "private";
    public const string Group = "group";
    public const string Public = "public";
}

    /// <summary>
    /// Represents a link to an ontology - either external (URI-based) or internal (database reference).
    /// External links reference standard ontologies like BFO, PROV-O.
    /// Internal links create virtualized nodes that sync with source ontologies.
    /// </summary>
    public class OntologyLink
    {
        public int Id { get; set; }

        /// <summary>
        /// Parent ontology that contains this link
        /// </summary>
        public int OntologyId { get; set; }
        public Ontology Ontology { get; set; } = null!;

        /// <summary>
        /// Type of link: External (URI-based) or Internal (database FK)
        /// </summary>
        [Required]
        public Enums.LinkType LinkType { get; set; } = Enums.LinkType.External;

        /// <summary>
        /// URI/namespace of the linked ontology (for External links)
        /// Example: http://purl.obolibrary.org/obo/bfo.owl
        /// </summary>
        [StringLength(500)]
        public string? Uri { get; set; }

        /// <summary>
        /// ID of the linked ontology in the database (for Internal links)
        /// Creates a virtualized reference to another user's ontology
        /// </summary>
        public int? LinkedOntologyId { get; set; }

        /// <summary>
        /// Navigation property to the linked ontology (for Internal links)
        /// </summary>
        public Ontology? LinkedOntology { get; set; }

        /// <summary>
        /// Display name for the linked ontology
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Prefix used in the TTL file (e.g., bfo, prov, foaf)
        /// </summary>
        [StringLength(50)]
        public string? Prefix { get; set; }

        /// <summary>
        /// Description of what this ontology provides
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Graph position X coordinate (for Internal links displayed as nodes)
        /// </summary>
        public double? PositionX { get; set; }

        /// <summary>
        /// Graph position Y coordinate (for Internal links displayed as nodes)
        /// </summary>
        public double? PositionY { get; set; }

        /// <summary>
        /// Color for the virtualized node (for Internal links)
        /// </summary>
        [StringLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// Whether concepts from this ontology were imported
        /// </summary>
        public bool ConceptsImported { get; set; } = false;

        /// <summary>
        /// Number of concepts imported/available from this ontology
        /// </summary>
        public int ImportedConceptCount { get; set; } = 0;

        /// <summary>
        /// When the link was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the link metadata was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the linked ontology was last synchronized (for Internal links)
        /// Used to detect if source ontology has changed
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// Whether the linked ontology has updates available (for Internal links)
        /// Set to true when source ontology UpdatedAt > LastSyncedAt
        /// </summary>
        public bool UpdateAvailable { get; set; } = false;
    }

    /// <summary>
    /// A concept (node) in the ontology - represents a thing, idea, or category
    /// </summary>
    public class Concept
    {
        public int Id { get; set; }
        public int OntologyId { get; set; }
        public Ontology Ontology { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Definition { get; set; }

        // User-friendly explanation
        public string? SimpleExplanation { get; set; }

        // Examples help users understand
        public string? Examples { get; set; }

        // Visual positioning for graph display
        public double? PositionX { get; set; }
        public double? PositionY { get; set; }

        // Categorization
        public string? Category { get; set; }
        public string? Color { get; set; } // For visual distinction
        public string? SourceOntology { get; set; } // Track which ontology this concept was imported from

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Property> Properties { get; set; } = new List<Property>();
        public ICollection<ConceptProperty> ConceptProperties { get; set; } = new List<ConceptProperty>(); // OWL property definitions
        public ICollection<Relationship> RelationshipsAsSource { get; set; } = new List<Relationship>();
        public ICollection<Relationship> RelationshipsAsTarget { get; set; } = new List<Relationship>();
        public ICollection<ConceptRestriction> Restrictions { get; set; } = new List<ConceptRestriction>();

        /// <summary>
        /// Auto-generated concept note (Obsidian-style)
        /// Each concept gets a markdown note for detailed documentation
        /// </summary>
        public Note? ConceptNote { get; set; }

        /// <summary>
        /// Incoming links from notes that reference this concept using [[concept-name]] syntax
        /// Enables backlinks (Obsidian-style)
        /// </summary>
        public ICollection<NoteLink> IncomingNoteLinks { get; set; } = new List<NoteLink>();

        /// <summary>
        /// Auto-detected mentions of this concept in notes
        /// Enables automatic backlinks panel showing which notes mention this concept
        /// </summary>
        public ICollection<NoteConceptLink> NoteMentions { get; set; } = new List<NoteConceptLink>();
    }

    /// <summary>
    /// A relationship (edge) between two concepts
    /// </summary>
    public class Relationship
    {
        public int Id { get; set; }
        public int OntologyId { get; set; }
        public Ontology Ontology { get; set; } = null!;

        public int SourceConceptId { get; set; }
        public Concept SourceConcept { get; set; } = null!;

        public int TargetConceptId { get; set; }
        public Concept TargetConcept { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string RelationType { get; set; } = string.Empty; // "is-a", "part-of", "related-to", etc.

        // Optional custom label to display on graph edges (defaults to RelationType if not specified)
        [StringLength(200)]
        public string? Label { get; set; }

        public string? Description { get; set; }

        // Ontology URI for standard relationships (e.g., "http://www.w3.org/2000/01/rdf-schema#subClassOf")
        [StringLength(500)]
        public string? OntologyUri { get; set; }

        // Strength of relationship (optional)
        public decimal? Strength { get; set; } // 0.0 to 1.0

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Properties/attributes of a concept
    /// </summary>
    public class Property
    {
        public int Id { get; set; }
        public int ConceptId { get; set; }
        public Concept Concept { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Value { get; set; }
        public string? DataType { get; set; } // "string", "number", "boolean", "date", etc.

        public string? Description { get; set; }
    }

    /// <summary>
    /// Custom concept templates - user-defined templates specific to an ontology
    /// </summary>
    public class CustomConceptTemplate
    {
        public int Id { get; set; }
        public int OntologyId { get; set; }
        public Ontology Ontology { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? Examples { get; set; }

        [StringLength(50)]
        public string Color { get; set; } = "#4A90E2";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Common relationship types for guidance
    /// </summary>
    public static class CommonRelationshipTypes
    {
        public static readonly List<RelationshipTypeTemplate> Templates = new()
        {
            // RDF/RDFS Standard Relations
            new("subclass-of", "RDFS Subclass", "X is a subclass of Y", "Mammal subclass-of Animal",
                "http://www.w3.org/2000/01/rdf-schema#subClassOf"),
            new("type", "RDF Type", "X is an instance of class Y", "Fido type Dog",
                "http://www.w3.org/1999/02/22-rdf-syntax-ns#type"),
            new("subproperty-of", "RDFS Subproperty", "Property X is a subproperty of Y", "owns subproperty-of has",
                "http://www.w3.org/2000/01/rdf-schema#subPropertyOf"),

            // OWL Standard Relations
            new("equivalent-class", "OWL Equivalent", "X is equivalent to Y", "Person equivalent-class Human",
                "http://www.w3.org/2002/07/owl#equivalentClass"),
            new("disjoint-with", "OWL Disjoint", "X and Y have no common instances", "Male disjoint-with Female",
                "http://www.w3.org/2002/07/owl#disjointWith"),

            // BFO (Basic Formal Ontology) Relations
            new("part-of", "BFO Part", "X is part of Y", "Heart part-of Body",
                "http://purl.obolibrary.org/obo/BFO_0000050"),
            new("has-part", "BFO Has Part", "X has Y as a part", "Body has-part Heart",
                "http://purl.obolibrary.org/obo/BFO_0000051"),
            new("participates-in", "BFO Participates", "X participates in process Y", "Player participates-in Game",
                "http://purl.obolibrary.org/obo/BFO_0000056"),
            new("has-participant", "BFO Has Participant", "Process X has participant Y", "Game has-participant Player",
                "http://purl.obolibrary.org/obo/BFO_0000057"),
            new("realizes", "BFO Realizes", "Process X realizes disposition Y", "Running realizes Ability",
                "http://purl.obolibrary.org/obo/BFO_0000055"),
            new("realized-in", "BFO Realized In", "Disposition X is realized in process Y", "Ability realized-in Running",
                "http://purl.obolibrary.org/obo/BFO_0000054"),

            // RO (Relations Ontology) Common Relations
            new("located-in", "RO Location", "X is located in Y", "Paris located-in France",
                "http://purl.obolibrary.org/obo/RO_0002162"),
            new("has-input", "RO Input", "Process X has input Y", "Cooking has-input Ingredients",
                "http://purl.obolibrary.org/obo/RO_0002233"),
            new("has-output", "RO Output", "Process X has output Y", "Cooking has-output Meal",
                "http://purl.obolibrary.org/obo/RO_0002234"),
            new("develops-from", "RO Development", "X develops from Y", "Adult develops-from Child",
                "http://purl.obolibrary.org/obo/RO_0002202"),
            new("overlaps", "RO Overlap", "X overlaps with Y", "Morning overlaps Sunrise",
                "http://purl.obolibrary.org/obo/RO_0002131"),

            // Common User-Friendly Relations (no standard URI)
            new("is-a", "Type/Subtype", "X is a type of Y", "Dog is-a Animal"),
            new("has-a", "Possession", "X has Y", "Person has-a Name"),
            new("related-to", "Generic Association", "X is related to Y", "Temperature related-to Weather"),
            new("causes", "Causation", "X causes Y", "Rain causes Wetness"),
            new("similar-to", "Similarity", "X is similar to Y", "Cat similar-to Dog"),
            new("opposite-of", "Opposition", "X is opposite of Y", "Hot opposite-of Cold"),
            new("instance-of", "Instantiation", "X is an instance of Y", "Fido instance-of Dog"),
        };
    }

    public record RelationshipTypeTemplate(
        string Type,
        string Category,
        string Pattern,
        string Example,
        string? OntologyUri = null
    );

    /// <summary>
    /// Common concept templates for quick creation
    /// </summary>
    public static class CommonConceptTemplates
    {
        public static readonly List<ConceptTemplate> Templates = new()
        {
            // BFO (Basic Formal Ontology) Categories
            new("Continuant", "BFO Entity", "An entity that persists through time (BFO)", "Person, Cell, Organization", "#2E86AB"),
            new("Occurrent", "BFO Process", "A process or event that unfolds over time (BFO)", "Running, Meeting, Life", "#A23B72"),
            new("Material Entity", "BFO Object", "A physical object with matter (BFO)", "Rock, Car, Organism", "#4A90E2"),
            new("Process", "BFO Activity", "A dynamic activity or transformation (BFO)", "Metabolism, Manufacturing, Learning", "#E94B3C"),
            new("Quality", "BFO Attribute", "A dependent quality of an entity (BFO)", "Mass, Temperature, Color", "#6BCF7F"),
            new("Role", "BFO Function", "A realizable role or function (BFO)", "Student Role, Patient Role, Driver Role", "#1ABC9C"),
            new("Disposition", "BFO Capability", "A capability or tendency (BFO)", "Fragility, Solubility, Ability to Learn", "#9B59B6"),
            new("Function", "BFO Purpose", "The function or purpose of something (BFO)", "Heart Function, Software Function", "#E67E22"),

            // Common General Templates
            new("Entity", "Thing", "A concrete or abstract entity", "Person, Organization, Place", "#4A90E2"),
            new("Action", "Activity", "An action or process", "Running, Thinking, Processing", "#E94B3C"),
            new("Attribute", "Property", "A quality or attribute", "Size, Speed, Intensity", "#6BCF7F"),
            new("Event", "Occurrence", "An event or occurrence", "Birthday, Meeting, Accident", "#F4A261"),
            new("Concept", "Abstract Idea", "An abstract concept", "Love, Freedom, Justice", "#9B59B6"),
            new("State", "Condition", "A state or condition", "Active, Dormant, Complete", "#E67E22"),
            new("Relation", "Connection", "A type of relationship", "Friendship, Ownership, Membership", "#3498DB"),
        };
    }

    public record ConceptTemplate(
        string Category,
        string Type,
        string Description,
        string Examples,
        string Color
    );
}
