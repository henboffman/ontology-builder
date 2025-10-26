using Eidos.Models;
using VDS.RDF;

namespace Eidos.Services.Interfaces;

public interface ITtlImportService
{
    /// <summary>
    /// Parse a TTL/RDF file and return import information
    /// </summary>
    Task<TtlImportResult> ParseTtlFileAsync(Stream fileStream);

    /// <summary>
    /// Import the parsed graph as a new ontology
    /// </summary>
    Task<Ontology> ImportAsNewOntologyAsync(IGraph graph, string? customName = null, string? customDescription = null);

    /// <summary>
    /// Preview what would happen if merging into an existing ontology
    /// </summary>
    Task<MergePreview> PreviewMergeAsync(int ontologyId, IGraph graph);

    /// <summary>
    /// Merge the parsed graph into an existing ontology
    /// </summary>
    Task<Ontology> MergeIntoExistingAsync(int ontologyId, IGraph graph, Action<ImportProgress>? onProgress = null, int batchSize = 50);
}
