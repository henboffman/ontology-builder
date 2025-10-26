using VDS.RDF;

namespace Eidos.Services.Import;

/// <summary>
/// Utility methods for working with RDF graphs
/// </summary>
public static class RdfUtilities
{
    /// <summary>
    /// Extract the local name from a URI
    /// </summary>
    public static string ExtractLocalName(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return string.Empty;

        // Remove angle brackets if present
        uri = uri.Trim('<', '>');

        // Try to extract after # or last /
        var hashIndex = uri.LastIndexOf('#');
        if (hashIndex >= 0 && hashIndex < uri.Length - 1)
        {
            return uri.Substring(hashIndex + 1);
        }

        var slashIndex = uri.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < uri.Length - 1)
        {
            return uri.Substring(slashIndex + 1);
        }

        return uri;
    }

    /// <summary>
    /// Get the label for a node from rdfs:label or skos:prefLabel
    /// </summary>
    public static string? GetLabel(IGraph graph, INode node)
    {
        var labelTriple = graph.Triples
            .Where(t => t.Subject.Equals(node) &&
                       (t.Predicate.ToString().Contains("label") ||
                        t.Predicate.ToString().Contains("prefLabel")))
            .FirstOrDefault();

        if (labelTriple != null && labelTriple.Object is ILiteralNode literal)
        {
            return literal.Value;
        }

        return null;
    }

    /// <summary>
    /// Get the comment/description for a node
    /// </summary>
    public static string? GetComment(IGraph graph, INode node)
    {
        var commentTriple = graph.Triples
            .Where(t => t.Subject.Equals(node) &&
                       (t.Predicate.ToString().Contains("comment") ||
                        t.Predicate.ToString().Contains("description") ||
                        t.Predicate.ToString().Contains("definition")))
            .FirstOrDefault();

        if (commentTriple != null && commentTriple.Object is ILiteralNode literal)
        {
            return literal.Value;
        }

        return null;
    }

    /// <summary>
    /// Generate a color from a string name (for consistency)
    /// </summary>
    public static string GenerateColorFromName(string name)
    {
        var hash = name.GetHashCode();
        var r = (hash & 0xFF0000) >> 16;
        var g = (hash & 0x00FF00) >> 8;
        var b = (hash & 0x0000FF);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Extract ontology name from a graph
    /// </summary>
    public static string? ExtractOntologyName(IGraph graph)
    {
        var ontologyNode = graph.Triples
            .Where(t => t.Predicate.ToString().Contains("type") &&
                        t.Object.ToString().Contains("Ontology"))
            .Select(t => t.Subject)
            .FirstOrDefault();

        if (ontologyNode == null)
            return null;

        // Try to get title or label
        var titleTriple = graph.Triples
            .Where(t => t.Subject.Equals(ontologyNode) &&
                       (t.Predicate.ToString().Contains("title") ||
                        t.Predicate.ToString().Contains("label")))
            .FirstOrDefault();

        if (titleTriple != null && titleTriple.Object is ILiteralNode literal)
        {
            return literal.Value;
        }

        // Fallback to extracting from URI
        return ExtractLocalName(ontologyNode.ToString());
    }

    /// <summary>
    /// Extract ontology description from a graph
    /// </summary>
    public static string? ExtractOntologyDescription(IGraph graph)
    {
        var ontologyNode = graph.Triples
            .Where(t => t.Predicate.ToString().Contains("type") &&
                        t.Object.ToString().Contains("Ontology"))
            .Select(t => t.Subject)
            .FirstOrDefault();

        if (ontologyNode == null)
            return null;

        return GetComment(graph, ontologyNode);
    }

    /// <summary>
    /// Check if a prefix is a standard/well-known ontology prefix
    /// </summary>
    public static bool IsStandardPrefix(string prefix)
    {
        var standardPrefixes = new[] { "rdf", "rdfs", "owl", "xsd", "dc", "dcterms", "foaf", "skos" };
        return standardPrefixes.Contains(prefix.ToLower());
    }

    /// <summary>
    /// Extract ontology name from URI
    /// </summary>
    public static string ExtractOntologyNameFromUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return "Unknown";

        uri = uri.Trim('<', '>');

        // Remove file extension if present
        if (uri.EndsWith(".owl") || uri.EndsWith(".ttl") || uri.EndsWith(".rdf"))
        {
            uri = uri.Substring(0, uri.LastIndexOf('.'));
        }

        // Extract name from path
        var name = ExtractLocalName(uri);

        // Convert to friendly name (e.g., "bfo" -> "BFO", "prov-o" -> "PROV-O")
        return name.ToUpper();
    }
}
