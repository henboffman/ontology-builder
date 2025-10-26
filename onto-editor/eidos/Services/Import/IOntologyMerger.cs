using Eidos.Models;
using VDS.RDF;

namespace Eidos.Services.Import;

/// <summary>
/// Service for merging RDF graphs into existing ontologies
/// </summary>
public interface IOntologyMerger
{
    /// <summary>
    /// Preview what would be merged
    /// </summary>
    Task<MergePreview> PreviewMergeAsync(int ontologyId, IGraph graph);

    /// <summary>
    /// Merge an RDF graph into an existing ontology
    /// </summary>
    Task<Ontology> MergeAsync(int ontologyId, IGraph graph, Action<ImportProgress>? onProgress = null, int batchSize = 50);
}
