namespace Eidos.Exceptions;

/// <summary>
/// Base exception for all ontology-related errors
/// </summary>
public abstract class OntologyException : Exception
{
    public string ErrorCode { get; }
    public string UserFriendlyMessage { get; }

    protected OntologyException(
        string errorCode,
        string userFriendlyMessage,
        string technicalMessage,
        Exception? innerException = null)
        : base(technicalMessage, innerException)
    {
        ErrorCode = errorCode;
        UserFriendlyMessage = userFriendlyMessage;
    }
}

/// <summary>
/// Thrown when an ontology is not found
/// </summary>
public class OntologyNotFoundException : OntologyException
{
    public int OntologyId { get; }

    public OntologyNotFoundException(int ontologyId)
        : base(
            "ONTOLOGY_NOT_FOUND",
            "The requested ontology could not be found. It may have been deleted or you may not have permission to access it.",
            $"Ontology with ID {ontologyId} not found")
    {
        OntologyId = ontologyId;
    }
}

/// <summary>
/// Thrown when a concept is not found
/// </summary>
public class ConceptNotFoundException : OntologyException
{
    public int ConceptId { get; }

    public ConceptNotFoundException(int conceptId)
        : base(
            "CONCEPT_NOT_FOUND",
            "The requested concept could not be found.",
            $"Concept with ID {conceptId} not found")
    {
        ConceptId = conceptId;
    }
}

/// <summary>
/// Thrown when a relationship is not found
/// </summary>
public class RelationshipNotFoundException : OntologyException
{
    public int RelationshipId { get; }

    public RelationshipNotFoundException(int relationshipId)
        : base(
            "RELATIONSHIP_NOT_FOUND",
            "The requested relationship could not be found.",
            $"Relationship with ID {relationshipId} not found")
    {
        RelationshipId = relationshipId;
    }
}

/// <summary>
/// Thrown when a share link is not found or invalid
/// </summary>
public class ShareLinkNotFoundException : OntologyException
{
    public string ShareToken { get; }

    public ShareLinkNotFoundException(string shareToken)
        : base(
            "SHARE_LINK_NOT_FOUND",
            "This share link is not valid. It may have been deleted or never existed.",
            $"Share link with token {shareToken} not found")
    {
        ShareToken = shareToken;
    }
}

/// <summary>
/// Thrown when a share link has expired
/// </summary>
public class ShareLinkExpiredException : OntologyException
{
    public string ShareToken { get; }
    public DateTime ExpirationDate { get; }

    public ShareLinkExpiredException(string shareToken, DateTime expirationDate)
        : base(
            "SHARE_LINK_EXPIRED",
            "This share link has expired and is no longer valid.",
            $"Share link {shareToken} expired on {expirationDate:yyyy-MM-dd HH:mm:ss}")
    {
        ShareToken = shareToken;
        ExpirationDate = expirationDate;
    }
}

/// <summary>
/// Thrown when a share link has been deactivated
/// </summary>
public class ShareLinkDeactivatedException : OntologyException
{
    public string ShareToken { get; }

    public ShareLinkDeactivatedException(string shareToken)
        : base(
            "SHARE_LINK_DEACTIVATED",
            "This share link has been deactivated by the owner.",
            $"Share link {shareToken} has been deactivated")
    {
        ShareToken = shareToken;
    }
}

/// <summary>
/// Thrown when user lacks required permission
/// </summary>
public class InsufficientPermissionException : OntologyException
{
    public string RequiredPermission { get; }
    public string? ActualPermission { get; }

    public InsufficientPermissionException(string requiredPermission, string? actualPermission = null)
        : base(
            "INSUFFICIENT_PERMISSION",
            "You do not have permission to perform this action.",
            $"Required permission: {requiredPermission}, Actual permission: {actualPermission ?? "None"}")
    {
        RequiredPermission = requiredPermission;
        ActualPermission = actualPermission;
    }
}

/// <summary>
/// Thrown when import/export operation fails
/// </summary>
public class OntologyImportException : OntologyException
{
    public OntologyImportException(string userMessage, string technicalMessage, Exception? innerException = null)
        : base(
            "ONTOLOGY_IMPORT_FAILED",
            userMessage,
            technicalMessage,
            innerException)
    {
    }
}

/// <summary>
/// Thrown when a validation error occurs
/// </summary>
public class OntologyValidationException : OntologyException
{
    public Dictionary<string, string[]> ValidationErrors { get; }

    public OntologyValidationException(Dictionary<string, string[]> validationErrors)
        : base(
            "VALIDATION_FAILED",
            "The data you provided is invalid. Please check the form and try again.",
            $"Validation failed with {validationErrors.Count} error(s)")
    {
        ValidationErrors = validationErrors;
    }

    public OntologyValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { { field, new[] { error } } })
    {
    }
}
