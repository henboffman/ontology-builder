namespace Eidos.Models.Events;

/// <summary>
/// Event data for individual instance changes (add, update, delete) to be broadcast via SignalR
/// </summary>
public class IndividualChangedEvent
{
    /// <summary>
    /// The type of change that occurred
    /// </summary>
    public ChangeType ChangeType { get; set; }

    /// <summary>
    /// The ID of the ontology that contains this individual
    /// </summary>
    public int OntologyId { get; set; }

    /// <summary>
    /// The individual that was added or updated (null for delete operations)
    /// </summary>
    public Individual? Individual { get; set; }

    /// <summary>
    /// The ID of the individual that was deleted (only populated for delete operations)
    /// </summary>
    public int? DeletedIndividualId { get; set; }

    /// <summary>
    /// The connection ID of the user who made the change (to avoid echoing back)
    /// </summary>
    public string? OriginatorConnectionId { get; set; }
}
