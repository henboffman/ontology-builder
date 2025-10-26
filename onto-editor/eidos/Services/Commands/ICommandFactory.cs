using Eidos.Models;

namespace Eidos.Services.Commands;

/// <summary>
/// Factory for creating command instances with proper dependencies
/// </summary>
public interface ICommandFactory
{
    ICommand CreateConceptCommand(Concept concept);
    ICommand UpdateConceptCommand(Concept concept);
    ICommand DeleteConceptCommand(int conceptId);

    ICommand CreateRelationshipCommand(Relationship relationship);
    ICommand UpdateRelationshipCommand(Relationship relationship);
    ICommand DeleteRelationshipCommand(int relationshipId);
}
