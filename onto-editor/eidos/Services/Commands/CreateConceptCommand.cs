using Eidos.Data.Repositories;
using Eidos.Models;

namespace Eidos.Services.Commands;

/// <summary>
/// Command to create a new concept
/// </summary>
public class CreateConceptCommand : ICommand
{
    private readonly IConceptRepository _conceptRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly Concept _concept;
    private int _createdId;

    public int OntologyId => _concept.OntologyId;
    public string Description => $"Create concept: {_concept.Name}";

    public CreateConceptCommand(
        IConceptRepository conceptRepository,
        IOntologyRepository ontologyRepository,
        Concept concept)
    {
        _conceptRepository = conceptRepository;
        _ontologyRepository = ontologyRepository;
        _concept = concept;
    }

    public async Task ExecuteAsync()
    {
        _concept.CreatedAt = DateTime.UtcNow;
        var created = await _conceptRepository.AddAsync(_concept);
        _createdId = created.Id;
        await _ontologyRepository.UpdateTimestampAsync(_concept.OntologyId);
        await _ontologyRepository.IncrementConceptCountAsync(_concept.OntologyId);
    }

    public async Task UndoAsync()
    {
        await _conceptRepository.DeleteAsync(_createdId);
        await _ontologyRepository.UpdateTimestampAsync(_concept.OntologyId);
        await _ontologyRepository.DecrementConceptCountAsync(_concept.OntologyId);
    }
}
