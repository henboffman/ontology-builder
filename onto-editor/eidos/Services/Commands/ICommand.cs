namespace Eidos.Services.Commands;

/// <summary>
/// Command pattern interface for undoable operations
/// Follows Command Pattern for clean undo/redo implementation
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Execute the command
    /// </summary>
    Task ExecuteAsync();

    /// <summary>
    /// Undo the command
    /// </summary>
    Task UndoAsync();

    /// <summary>
    /// Redo the command (by default, just re-execute)
    /// </summary>
    Task RedoAsync() => ExecuteAsync();

    /// <summary>
    /// The ID of the ontology this command affects
    /// </summary>
    int OntologyId { get; }

    /// <summary>
    /// Description of the command for debugging/logging
    /// </summary>
    string Description { get; }
}
