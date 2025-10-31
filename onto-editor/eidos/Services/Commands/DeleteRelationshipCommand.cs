using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;

namespace Eidos.Services.Commands;

/// <summary>
/// Command to delete a relationship
/// </summary>
public class DeleteRelationshipCommand : ICommand
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly int _relationshipId;
    private Relationship? _deletedRelationship;

    public int OntologyId { get; private set; }
    public string Description => $"Delete relationship: {_deletedRelationship?.RelationType ?? _relationshipId.ToString()}";

    public DeleteRelationshipCommand(
        IRelationshipRepository relationshipRepository,
        IOntologyRepository ontologyRepository,
        IDbContextFactory<OntologyDbContext> contextFactory,
        int relationshipId)
    {
        _relationshipRepository = relationshipRepository;
        _ontologyRepository = ontologyRepository;
        _contextFactory = contextFactory;
        _relationshipId = relationshipId;
    }

    public async Task ExecuteAsync()
    {
        // Capture the relationship before deletion for undo
        _deletedRelationship = await _relationshipRepository.GetByIdAsync(_relationshipId);

        if (_deletedRelationship == null)
            throw new InvalidOperationException($"Relationship {_relationshipId} not found");

        OntologyId = _deletedRelationship.OntologyId;

        await _relationshipRepository.DeleteAsync(_relationshipId);
        await _ontologyRepository.UpdateTimestampAsync(OntologyId);
        await _ontologyRepository.DecrementRelationshipCountAsync(OntologyId);
    }

    public async Task UndoAsync()
    {
        if (_deletedRelationship == null)
            throw new InvalidOperationException("Cannot undo: deleted relationship not captured");

        // Re-create the relationship with the same ID
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Relationships.Add(_deletedRelationship);
        await context.SaveChangesAsync();
        await _ontologyRepository.UpdateTimestampAsync(OntologyId);
        await _ontologyRepository.IncrementRelationshipCountAsync(OntologyId);
    }

    public async Task RedoAsync()
    {
        await _relationshipRepository.DeleteAsync(_relationshipId);
        await _ontologyRepository.UpdateTimestampAsync(OntologyId);
        await _ontologyRepository.DecrementRelationshipCountAsync(OntologyId);
    }
}
