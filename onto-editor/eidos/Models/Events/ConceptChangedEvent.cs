namespace Eidos.Models.Events;

/// <summary>
/// Event data for concept changes (add, update, delete) to be broadcast via SignalR
/// </summary>
public class ConceptChangedEvent
{
    /// <summary>
    /// The type of change that occurred
    /// </summary>
    public ChangeType ChangeType { get; set; }

    /// <summary>
    /// The ID of the ontology that contains this concept
    /// </summary>
    public int OntologyId { get; set; }

    /// <summary>
    /// The concept that was added or updated (null for delete operations)
    /// </summary>
    public Concept? Concept { get; set; }

    /// <summary>
    /// The ID of the concept that was deleted (only populated for delete operations)
    /// </summary>
    public int? DeletedConceptId { get; set; }

    /// <summary>
    /// The connection ID of the user who made the change (to avoid echoing back)
    /// </summary>
    public string? OriginatorConnectionId { get; set; }
}

/// <summary>
/// Type of change that occurred
/// </summary>
public enum ChangeType
{
    Added,
    Updated,
    Deleted
}
