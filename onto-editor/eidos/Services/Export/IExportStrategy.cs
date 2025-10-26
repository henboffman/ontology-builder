using Eidos.Models;

namespace Eidos.Services.Export;

/// <summary>
/// Defines a strategy for exporting ontologies to different formats
/// </summary>
public interface IExportStrategy
{
    /// <summary>
    /// The format name this strategy handles (e.g., "Turtle", "JSON", "CSV")
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// The file extension for this format (e.g., ".ttl", ".json", ".csv")
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// The MIME content type for this format
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Export the ontology to the format handled by this strategy
    /// </summary>
    /// <param name="ontology">The ontology to export</param>
    /// <returns>The exported content as a string</returns>
    string Export(Ontology ontology);
}

/// <summary>
/// Service to manage and execute export strategies
/// </summary>
public interface IOntologyExporter
{
    /// <summary>
    /// Get all available export formats
    /// </summary>
    IEnumerable<string> GetAvailableFormats();

    /// <summary>
    /// Export an ontology using the specified format
    /// </summary>
    /// <param name="ontology">The ontology to export</param>
    /// <param name="format">The format name</param>
    /// <returns>The exported content</returns>
    string Export(Ontology ontology, string format);

    /// <summary>
    /// Get the file extension for a given format
    /// </summary>
    string GetFileExtension(string format);

    /// <summary>
    /// Get the content type for a given format
    /// </summary>
    string GetContentType(string format);
}
