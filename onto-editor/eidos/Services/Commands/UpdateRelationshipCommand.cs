using Eidos.Data.Repositories;
using Eidos.Models;

namespace Eidos.Services.Commands;

/// <summary>
/// Command to update an existing relationship
/// </summary>
public class UpdateRelationshipCommand : ICommand
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly Relationship _newState;
    private Relationship? _previousState;

    public int OntologyId => _newState.OntologyId;
    public string Description => $"Update relationship: {_newState.RelationType}";

    public UpdateRelationshipCommand(
        IRelationshipRepository relationshipRepository,
        IOntologyRepository ontologyRepository,
        Relationship newState)
    {
        _relationshipRepository = relationshipRepository;
        _ontologyRepository = ontologyRepository;
        _newState = newState;
    }

    public async Task ExecuteAsync()
    {
        // Capture previous state for undo
        _previousState = await _relationshipRepository.GetByIdAsync(_newState.Id);

        await _relationshipRepository.UpdateAsync(_newState);
        await _ontologyRepository.UpdateTimestampAsync(_newState.OntologyId);
    }

    public async Task UndoAsync()
    {
        if (_previousState == null)
            throw new InvalidOperationException("Cannot undo: previous state not captured");

        await _relationshipRepository.UpdateAsync(_previousState);
        await _ontologyRepository.UpdateTimestampAsync(_newState.OntologyId);
    }

    public async Task RedoAsync()
    {
        await _relationshipRepository.UpdateAsync(_newState);
        await _ontologyRepository.UpdateTimestampAsync(_newState.OntologyId);
    }
}
