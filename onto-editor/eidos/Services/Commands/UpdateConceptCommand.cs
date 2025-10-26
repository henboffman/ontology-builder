using Eidos.Data.Repositories;
using Eidos.Models;

namespace Eidos.Services.Commands;

/// <summary>
/// Command to update an existing concept
/// </summary>
public class UpdateConceptCommand : ICommand
{
    private readonly IConceptRepository _conceptRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly Concept _newState;
    private Concept? _previousState;

    public int OntologyId => _newState.OntologyId;
    public string Description => $"Update concept: {_newState.Name}";

    public UpdateConceptCommand(
        IConceptRepository conceptRepository,
        IOntologyRepository ontologyRepository,
        Concept newState)
    {
        _conceptRepository = conceptRepository;
        _ontologyRepository = ontologyRepository;
        _newState = newState;
    }

    public async Task ExecuteAsync()
    {
        // Capture previous state for undo
        _previousState = await _conceptRepository.GetByIdAsync(_newState.Id);

        await _conceptRepository.UpdateAsync(_newState);
        await _ontologyRepository.UpdateTimestampAsync(_newState.OntologyId);
    }

    public async Task UndoAsync()
    {
        if (_previousState == null)
            throw new InvalidOperationException("Cannot undo: previous state not captured");

        await _conceptRepository.UpdateAsync(_previousState);
        await _ontologyRepository.UpdateTimestampAsync(_newState.OntologyId);
    }

    public async Task RedoAsync()
    {
        await _conceptRepository.UpdateAsync(_newState);
        await _ontologyRepository.UpdateTimestampAsync(_newState.OntologyId);
    }
}
