using Eidos.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eidos.Services.Export;

public class JsonExportStrategy : IExportStrategy
{
    public string FormatName => "JSON";
    public string FileExtension => ".json";
    public string ContentType => "application/json";

    public string Export(Ontology ontology)
    {
        var exportModel = new OntologyExportModel
        {
            Name = ontology.Name,
            Namespace = ontology.Namespace,
            Author = ontology.Author,
            License = ontology.License,
            Tags = ontology.Tags,
            Notes = ontology.Notes,
            CreatedAt = ontology.CreatedAt,
            UpdatedAt = ontology.UpdatedAt,
            Concepts = ontology.Concepts.Select(c => new ConceptExportModel
            {
                Name = c.Name,
                Category = c.Category,
                Definition = c.Definition,
                SimpleExplanation = c.SimpleExplanation,
                Examples = c.Examples,
                Color = c.Color,
                SourceOntology = c.SourceOntology,
                CreatedAt = c.CreatedAt
            }).ToList(),
            Relationships = ontology.Relationships.Select(r => new RelationshipExportModel
            {
                SourceConcept = r.SourceConcept.Name,
                RelationType = r.RelationType,
                TargetConcept = r.TargetConcept.Name,
                Label = r.Label,
                Description = r.Description,
                CreatedAt = r.CreatedAt
            }).ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(exportModel, options);
    }

    // Export models for cleaner JSON structure
    private class OntologyExportModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Namespace { get; set; }
        public string? Author { get; set; }
        public string? License { get; set; }
        public string? Tags { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ConceptExportModel> Concepts { get; set; } = new();
        public List<RelationshipExportModel> Relationships { get; set; } = new();
    }

    private class ConceptExportModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Definition { get; set; }
        public string? SimpleExplanation { get; set; }
        public string? Examples { get; set; }
        public string? Color { get; set; }
        public string? SourceOntology { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class RelationshipExportModel
    {
        public string SourceConcept { get; set; } = string.Empty;
        public string RelationType { get; set; } = string.Empty;
        public string TargetConcept { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
