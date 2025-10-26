namespace Eidos.Constants;

/// <summary>
/// Constants for ontology template keys used throughout the application.
/// Centralizing these prevents typos and makes refactoring easier.
/// </summary>
public static class OntologyTemplateKeys
{
    public const string BasicFormalOntology = "bfo";
    public const string RelationsOntology = "ro";
    public const string OwlWebOntologyLanguage = "owl";
    public const string RdfSchema = "rdfs";
    public const string SimpleKnowledgeOrganizationSystem = "skos";
    public const string FriendOfAFriend = "foaf";
    public const string SchemaOrg = "schema";
    public const string DublinCoreTerms = "dcterms";
    public const string SoftwareDescription = "software-description";

    /// <summary>
    /// Returns all valid template keys that support automatic import
    /// </summary>
    public static readonly HashSet<string> ImportableTemplates = new()
    {
        RelationsOntology,
        OwlWebOntologyLanguage,
        SimpleKnowledgeOrganizationSystem,
        FriendOfAFriend,
        SchemaOrg,
        DublinCoreTerms,
        SoftwareDescription
    };

    /// <summary>
    /// Check if a template key is valid and supports import
    /// </summary>
    public static bool IsImportableTemplate(string templateKey)
    {
        return !string.IsNullOrEmpty(templateKey) && ImportableTemplates.Contains(templateKey);
    }
}
