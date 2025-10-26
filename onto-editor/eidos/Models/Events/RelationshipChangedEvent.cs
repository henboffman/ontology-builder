namespace Eidos.Models.Events;

/// <summary>
/// Event data for relationship changes (add, update, delete) to be broadcast via SignalR
/// </summary>
public class RelationshipChangedEvent
{
    /// <summary>
    /// The type of change that occurred
    /// </summary>
    public ChangeType ChangeType { get; set; }

    /// <summary>
    /// The ID of the ontology that contains this relationship
    /// </summary>
    public int OntologyId { get; set; }

    /// <summary>
    /// The relationship that was added or updated (null for delete operations)
    /// </summary>
    public Relationship? Relationship { get; set; }

    /// <summary>
    /// The ID of the relationship that was deleted (only populated for delete operations)
    /// </summary>
    public int? DeletedRelationshipId { get; set; }

    /// <summary>
    /// The connection ID of the user who made the change (to avoid echoing back)
    /// </summary>
    public string? OriginatorConnectionId { get; set; }
}
