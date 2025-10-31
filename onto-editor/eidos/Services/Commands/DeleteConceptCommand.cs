using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;

namespace Eidos.Services.Commands;

/// <summary>
/// Command to delete a concept
/// </summary>
public class DeleteConceptCommand : ICommand
{
    private readonly IConceptRepository _conceptRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly int _conceptId;
    private Concept? _deletedConcept;

    public int OntologyId { get; private set; }
    public string Description => $"Delete concept: {_deletedConcept?.Name ?? _conceptId.ToString()}";

    public DeleteConceptCommand(
        IConceptRepository conceptRepository,
        IOntologyRepository ontologyRepository,
        IDbContextFactory<OntologyDbContext> contextFactory,
        int conceptId)
    {
        _conceptRepository = conceptRepository;
        _ontologyRepository = ontologyRepository;
        _contextFactory = contextFactory;
        _conceptId = conceptId;
    }

    public async Task ExecuteAsync()
    {
        // Capture the concept before deletion for undo
        _deletedConcept = await _conceptRepository.GetWithPropertiesAsync(_conceptId);

        if (_deletedConcept == null)
            throw new InvalidOperationException($"Concept {_conceptId} not found");

        OntologyId = _deletedConcept.OntologyId;

        await _conceptRepository.DeleteAsync(_conceptId);
        await _ontologyRepository.UpdateTimestampAsync(OntologyId);
        await _ontologyRepository.DecrementConceptCountAsync(OntologyId);
    }

    public async Task UndoAsync()
    {
        if (_deletedConcept == null)
            throw new InvalidOperationException("Cannot undo: deleted concept not captured");

        // Re-create the concept with the same ID
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Concepts.Add(_deletedConcept);
        await context.SaveChangesAsync();
        await _ontologyRepository.UpdateTimestampAsync(OntologyId);
        await _ontologyRepository.IncrementConceptCountAsync(OntologyId);
    }

    public async Task RedoAsync()
    {
        await _conceptRepository.DeleteAsync(_conceptId);
        await _ontologyRepository.UpdateTimestampAsync(OntologyId);
        await _ontologyRepository.DecrementConceptCountAsync(OntologyId);
    }
}
