using Eidos.Constants;
using Eidos.Models;

namespace Eidos.Services
{
    public enum OperationType
    {
        CreateConcept,
        UpdateConcept,
        DeleteConcept,
        CreateRelationship,
        UpdateRelationship,
        DeleteRelationship
    }

    public class Operation
    {
        public OperationType Type { get; set; }
        public int OntologyId { get; set; }
        public object? Data { get; set; }
        public object? PreviousData { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class UndoRedoService
    {
        private readonly Stack<Operation> _undoStack = new();
        private readonly Stack<Operation> _redoStack = new();
        private readonly int _maxHistorySize = AppConstants.History.MaxHistorySize;

        public event Action? StateChanged;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void RecordOperation(OperationType type, int ontologyId, object? data, object? previousData = null)
        {
            var operation = new Operation
            {
                Type = type,
                OntologyId = ontologyId,
                Data = data,
                PreviousData = previousData,
                Timestamp = DateTime.UtcNow
            };

            _undoStack.Push(operation);

            // Limit history size
            if (_undoStack.Count > _maxHistorySize)
            {
                var tempStack = new Stack<Operation>(_undoStack.Reverse().Take(_maxHistorySize).Reverse());
                _undoStack.Clear();
                foreach (var op in tempStack)
                {
                    _undoStack.Push(op);
                }
            }

            // Clear redo stack when new operation is recorded
            _redoStack.Clear();

            StateChanged?.Invoke();
        }

        public Operation? GetUndoOperation()
        {
            if (_undoStack.Count == 0) return null;

            var operation = _undoStack.Pop();
            _redoStack.Push(operation);
            StateChanged?.Invoke();
            return operation;
        }

        public Operation? GetRedoOperation()
        {
            if (_redoStack.Count == 0) return null;

            var operation = _redoStack.Pop();
            _undoStack.Push(operation);
            StateChanged?.Invoke();
            return operation;
        }

        public void Clear(int? ontologyId = null)
        {
            if (ontologyId.HasValue)
            {
                // Clear operations for specific ontology
                var undoOps = _undoStack.Where(op => op.OntologyId != ontologyId.Value).Reverse().ToList();
                _undoStack.Clear();
                foreach (var op in undoOps)
                {
                    _undoStack.Push(op);
                }

                var redoOps = _redoStack.Where(op => op.OntologyId != ontologyId.Value).Reverse().ToList();
                _redoStack.Clear();
                foreach (var op in redoOps)
                {
                    _redoStack.Push(op);
                }
            }
            else
            {
                // Clear all operations
                _undoStack.Clear();
                _redoStack.Clear();
            }

            StateChanged?.Invoke();
        }

        public int GetUndoCount() => _undoStack.Count;
        public int GetRedoCount() => _redoStack.Count;
    }
}
