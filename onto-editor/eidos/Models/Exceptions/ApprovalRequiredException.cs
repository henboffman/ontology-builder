namespace Eidos.Models.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to make a direct edit to an ontology
/// that requires approval workflow. The user should create a merge request instead.
/// </summary>
public class ApprovalRequiredException : Exception
{
    public int OntologyId { get; }
    public string OperationType { get; }

    public ApprovalRequiredException(int ontologyId, string operationType)
        : base($"This ontology requires approval for changes. Please create a merge request instead of making direct edits.")
    {
        OntologyId = ontologyId;
        OperationType = operationType;
    }

    public ApprovalRequiredException(int ontologyId, string operationType, string message)
        : base(message)
    {
        OntologyId = ontologyId;
        OperationType = operationType;
    }
}
