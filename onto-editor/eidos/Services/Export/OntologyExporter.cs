using Eidos.Models;

namespace Eidos.Services.Export;

/// <summary>
/// Manages and executes export strategies for ontologies
/// Uses the Strategy Pattern to support multiple export formats
/// </summary>
public class OntologyExporter : IOntologyExporter
{
    private readonly Dictionary<string, IExportStrategy> _strategies;

    public OntologyExporter(IEnumerable<IExportStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.FormatName, s => s, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> GetAvailableFormats()
    {
        return _strategies.Keys;
    }

    public string Export(Ontology ontology, string format)
    {
        if (!_strategies.TryGetValue(format, out var strategy))
        {
            throw new ArgumentException($"Unsupported export format: {format}. Available formats: {string.Join(", ", _strategies.Keys)}", nameof(format));
        }

        return strategy.Export(ontology);
    }

    public string GetFileExtension(string format)
    {
        if (!_strategies.TryGetValue(format, out var strategy))
        {
            throw new ArgumentException($"Unsupported export format: {format}", nameof(format));
        }

        return strategy.FileExtension;
    }

    public string GetContentType(string format)
    {
        if (!_strategies.TryGetValue(format, out var strategy))
        {
            throw new ArgumentException($"Unsupported export format: {format}", nameof(format));
        }

        return strategy.ContentType;
    }
}
