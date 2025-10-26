using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;

namespace Eidos.Services.Commands;

/// <summary>
/// Factory for creating command instances with injected dependencies
/// </summary>
public class CommandFactory : ICommandFactory
{
    private readonly IConceptRepository _conceptRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;

    public CommandFactory(
        IConceptRepository conceptRepository,
        IRelationshipRepository relationshipRepository,
        IOntologyRepository ontologyRepository,
        IDbContextFactory<OntologyDbContext> contextFactory)
    {
        _conceptRepository = conceptRepository;
        _relationshipRepository = relationshipRepository;
        _ontologyRepository = ontologyRepository;
        _contextFactory = contextFactory;
    }

    public ICommand CreateConceptCommand(Concept concept)
    {
        return new CreateConceptCommand(_conceptRepository, _ontologyRepository, concept);
    }

    public ICommand UpdateConceptCommand(Concept concept)
    {
        return new UpdateConceptCommand(_conceptRepository, _ontologyRepository, concept);
    }

    public ICommand DeleteConceptCommand(int conceptId)
    {
        return new DeleteConceptCommand(_conceptRepository, _ontologyRepository, _contextFactory, conceptId);
    }

    public ICommand CreateRelationshipCommand(Relationship relationship)
    {
        return new CreateRelationshipCommand(_relationshipRepository, _ontologyRepository, relationship);
    }

    public ICommand UpdateRelationshipCommand(Relationship relationship)
    {
        return new UpdateRelationshipCommand(_relationshipRepository, _ontologyRepository, relationship);
    }

    public ICommand DeleteRelationshipCommand(int relationshipId)
    {
        return new DeleteRelationshipCommand(_relationshipRepository, _ontologyRepository, _contextFactory, relationshipId);
    }
}
