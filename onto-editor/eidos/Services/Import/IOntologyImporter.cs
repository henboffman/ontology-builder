using Eidos.Models;
using VDS.RDF;

namespace Eidos.Services.Import;

/// <summary>
/// Service for importing RDF graphs as new ontologies
/// Follows Single Responsibility Principle
/// </summary>
public interface IOntologyImporter
{
    /// <summary>
    /// Import an RDF graph as a new ontology
    /// </summary>
    Task<Ontology> ImportAsNewAsync(IGraph graph, string? customName = null, string? customDescription = null);
}
