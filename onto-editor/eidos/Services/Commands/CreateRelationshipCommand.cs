using Eidos.Data.Repositories;
using Eidos.Models;

namespace Eidos.Services.Commands;

/// <summary>
/// Command to create a new relationship
/// </summary>
public class CreateRelationshipCommand : ICommand
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly Relationship _relationship;
    private int _createdId;

    public int OntologyId => _relationship.OntologyId;
    public string Description => $"Create relationship: {_relationship.RelationType}";

    public CreateRelationshipCommand(
        IRelationshipRepository relationshipRepository,
        IOntologyRepository ontologyRepository,
        Relationship relationship)
    {
        _relationshipRepository = relationshipRepository;
        _ontologyRepository = ontologyRepository;
        _relationship = relationship;
    }

    public async Task ExecuteAsync()
    {
        _relationship.CreatedAt = DateTime.UtcNow;
        var created = await _relationshipRepository.AddAsync(_relationship);
        _createdId = created.Id;
        await _ontologyRepository.UpdateTimestampAsync(_relationship.OntologyId);
    }

    public async Task UndoAsync()
    {
        await _relationshipRepository.DeleteAsync(_createdId);
        await _ontologyRepository.UpdateTimestampAsync(_relationship.OntologyId);
    }
}
