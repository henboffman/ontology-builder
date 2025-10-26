namespace Eidos.Services.Commands;

/// <summary>
/// Command invoker that manages execution and undo/redo stacks
/// Replaces the old UndoRedoService with a cleaner Command Pattern approach
/// </summary>
public class CommandInvoker
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();
    private const int MaxStackSize = 50; // Limit stack size to prevent memory issues

    /// <summary>
    /// Execute a command and add it to the undo stack
    /// </summary>
    public async Task ExecuteAsync(ICommand command)
    {
        await command.ExecuteAsync();

        _undoStack.Push(command);
        _redoStack.Clear(); // Clear redo stack when new command is executed

        // Limit stack size
        if (_undoStack.Count > MaxStackSize)
        {
            var tempStack = new Stack<ICommand>(_undoStack.Reverse().Take(MaxStackSize).Reverse());
            _undoStack.Clear();
            foreach (var cmd in tempStack)
            {
                _undoStack.Push(cmd);
            }
        }
    }

    /// <summary>
    /// Undo the last command
    /// </summary>
    public async Task<bool> UndoAsync()
    {
        if (_undoStack.Count == 0)
            return false;

        var command = _undoStack.Pop();
        try
        {
            await command.UndoAsync();
            _redoStack.Push(command);
            return true;
        }
        catch
        {
            // If undo fails, push it back
            _undoStack.Push(command);
            throw;
        }
    }

    /// <summary>
    /// Redo the last undone command
    /// </summary>
    public async Task<bool> RedoAsync()
    {
        if (_redoStack.Count == 0)
            return false;

        var command = _redoStack.Pop();
        try
        {
            await command.RedoAsync();
            _undoStack.Push(command);
            return true;
        }
        catch
        {
            // If redo fails, push it back
            _redoStack.Push(command);
            throw;
        }
    }

    /// <summary>
    /// Check if undo is available
    /// </summary>
    public bool CanUndo() => _undoStack.Count > 0;

    /// <summary>
    /// Check if redo is available
    /// </summary>
    public bool CanRedo() => _redoStack.Count > 0;

    /// <summary>
    /// Get the description of the next undo command
    /// </summary>
    public string? GetUndoDescription() => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;

    /// <summary>
    /// Get the description of the next redo command
    /// </summary>
    public string? GetRedoDescription() => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    /// <summary>
    /// Clear all undo/redo history
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    /// <summary>
    /// Get the current undo stack size
    /// </summary>
    public int UndoStackSize => _undoStack.Count;

    /// <summary>
    /// Get the current redo stack size
    /// </summary>
    public int RedoStackSize => _redoStack.Count;
}
