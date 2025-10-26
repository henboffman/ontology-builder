using Eidos.Models;
using VDS.RDF;

namespace Eidos.Services.Import;

/// <summary>
/// Service for parsing RDF/TTL files
/// Follows Single Responsibility Principle - only handles parsing
/// </summary>
public interface IRdfParser
{
    /// <summary>
    /// Parse an RDF/TTL file from a stream
    /// </summary>
    Task<TtlImportResult> ParseAsync(Stream fileStream);
}
