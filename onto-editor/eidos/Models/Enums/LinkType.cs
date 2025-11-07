namespace Eidos.Models.Enums;

/// <summary>
/// Defines the type of ontology link.
/// Links can reference external ontologies via URI or internal ontologies in the database.
/// </summary>
public enum LinkType
{
    /// <summary>
    /// External Link - references an ontology via URI (e.g., BFO, PROV-O, FOAF)
    /// Example: http://purl.obolibrary.org/obo/bfo.owl
    /// Used for standard ontology imports
    /// </summary>
    External = 0,

    /// <summary>
    /// Internal Link - references another ontology in the database by ID
    /// Example: Link to "Library Ontology" (ID: 42) as a virtualized node
    /// Used for composing ontologies from other user-created ontologies
    /// Enables ontology reuse and synchronization
    /// </summary>
    Internal = 1
}
