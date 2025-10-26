using Eidos.Models;
using static Eidos.Services.TtlExportService;

namespace Eidos.Services.Interfaces;

public interface ITtlExportService
{
    /// <summary>
    /// Export ontology to the specified RDF format
    /// </summary>
    string ExportToFormat(Ontology ontology, RdfFormat format);

    /// <summary>
    /// Export ontology to Turtle (TTL) format
    /// </summary>
    string ExportToTtl(Ontology ontology);
}
