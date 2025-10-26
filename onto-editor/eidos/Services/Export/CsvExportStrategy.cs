using Eidos.Models;
using System.Text;

namespace Eidos.Services.Export;

public class CsvExportStrategy : IExportStrategy
{
    public string FormatName => "CSV";
    public string FileExtension => ".csv";
    public string ContentType => "text/csv";

    public string Export(Ontology ontology)
    {
        return ExportFullOntologyToCsv(ontology);
    }

    public string ExportConceptsToCsv(Ontology ontology)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Name,Category,Definition,SimpleExplanation,Examples,Color,SourceOntology,CreatedAt");

        // Data rows
        foreach (var concept in ontology.Concepts.OrderBy(c => c.Name))
        {
            sb.AppendLine($"{EscapeCsv(concept.Name)}," +
                         $"{EscapeCsv(concept.Category)}," +
                         $"{EscapeCsv(concept.Definition)}," +
                         $"{EscapeCsv(concept.SimpleExplanation)}," +
                         $"{EscapeCsv(concept.Examples)}," +
                         $"{EscapeCsv(concept.Color)}," +
                         $"{EscapeCsv(concept.SourceOntology)}," +
                         $"{concept.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return sb.ToString();
    }

    public string ExportRelationshipsToCsv(Ontology ontology)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("SourceConcept,RelationType,TargetConcept,Label,Description,CreatedAt");

        // Data rows
        foreach (var relationship in ontology.Relationships.OrderBy(r => r.SourceConcept.Name))
        {
            sb.AppendLine($"{EscapeCsv(relationship.SourceConcept.Name)}," +
                         $"{EscapeCsv(relationship.RelationType)}," +
                         $"{EscapeCsv(relationship.TargetConcept.Name)}," +
                         $"{EscapeCsv(relationship.Label)}," +
                         $"{EscapeCsv(relationship.Description)}," +
                         $"{relationship.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return sb.ToString();
    }

    public string ExportFullOntologyToCsv(Ontology ontology)
    {
        var sb = new StringBuilder();

        // Ontology metadata
        sb.AppendLine("# Ontology Metadata");
        sb.AppendLine($"Name,{EscapeCsv(ontology.Name)}");
        sb.AppendLine($"Namespace,{EscapeCsv(ontology.Namespace)}");
        sb.AppendLine($"Author,{EscapeCsv(ontology.Author)}");
        sb.AppendLine($"License,{EscapeCsv(ontology.License)}");
        sb.AppendLine($"Tags,{EscapeCsv(ontology.Tags)}");
        sb.AppendLine($"CreatedAt,{ontology.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"UpdatedAt,{ontology.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // Concepts
        sb.AppendLine("# Concepts");
        sb.Append(ExportConceptsToCsv(ontology));
        sb.AppendLine();

        // Relationships
        sb.AppendLine("# Relationships");
        sb.Append(ExportRelationshipsToCsv(ontology));

        return sb.ToString();
    }

    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // If the value contains comma, quote, or newline, wrap it in quotes and escape internal quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
