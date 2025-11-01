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
            Description = ontology.Description,
            Namespace = ontology.Namespace,
            NamespacePrefixes = ontology.NamespacePrefixes,
            Author = ontology.Author,
            Version = ontology.Version,
            License = ontology.License,
            Tags = ontology.Tags,
            Notes = ontology.Notes,
            UsesBFO = ontology.UsesBFO,
            UsesProvO = ontology.UsesProvO,
            Visibility = ontology.Visibility,
            AllowPublicEdit = ontology.AllowPublicEdit,
            ProvenanceType = ontology.ProvenanceType,
            ProvenanceNotes = ontology.ProvenanceNotes,
            CreatedAt = ontology.CreatedAt,
            UpdatedAt = ontology.UpdatedAt,
            ConceptCount = ontology.ConceptCount,
            RelationshipCount = ontology.RelationshipCount,

            Concepts = ontology.Concepts.Select(c => new ConceptExportModel
            {
                Name = c.Name,
                Definition = c.Definition,
                SimpleExplanation = c.SimpleExplanation,
                Examples = c.Examples,
                Category = c.Category,
                Color = c.Color,
                SourceOntology = c.SourceOntology,
                PositionX = c.PositionX,
                PositionY = c.PositionY,
                CreatedAt = c.CreatedAt,
                Properties = c.Properties.Select(p => new PropertyExportModel
                {
                    Name = p.Name,
                    Value = p.Value,
                    DataType = p.DataType,
                    Description = p.Description
                }).ToList(),
                Restrictions = c.Restrictions.Select(r => new RestrictionExportModel
                {
                    RestrictionType = r.RestrictionType,
                    PropertyName = r.PropertyName,
                    MinCardinality = r.MinCardinality,
                    MaxCardinality = r.MaxCardinality,
                    ValueType = r.ValueType,
                    MinValue = r.MinValue,
                    MaxValue = r.MaxValue,
                    AllowedConceptName = r.AllowedConcept?.Name,
                    AllowedValues = r.AllowedValues,
                    Pattern = r.Pattern,
                    Description = r.Description,
                    IsMandatory = r.IsMandatory
                }).ToList()
            }).ToList(),

            Relationships = ontology.Relationships.Select(r => new RelationshipExportModel
            {
                SourceConcept = r.SourceConcept.Name,
                RelationType = r.RelationType,
                TargetConcept = r.TargetConcept.Name,
                Label = r.Label,
                Description = r.Description,
                OntologyUri = r.OntologyUri,
                Strength = r.Strength,
                CreatedAt = r.CreatedAt
            }).ToList(),

            Individuals = ontology.Individuals.Select(i => new IndividualExportModel
            {
                Name = i.Name,
                ConceptName = i.Concept.Name,
                Description = i.Description,
                Label = i.Label,
                Uri = i.Uri,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                Properties = i.Properties.Select(p => new IndividualPropertyExportModel
                {
                    Name = p.Name,
                    Value = p.Value,
                    DataType = p.DataType,
                    Description = p.Description
                }).ToList()
            }).ToList(),

            IndividualRelationships = ontology.IndividualRelationships.Select(ir => new IndividualRelationshipExportModel
            {
                SourceIndividual = ir.SourceIndividual.Name,
                RelationType = ir.RelationType,
                TargetIndividual = ir.TargetIndividual.Name,
                Label = ir.Label,
                Description = ir.Description,
                OntologyUri = ir.OntologyUri,
                CreatedAt = ir.CreatedAt
            }).ToList(),

            LinkedOntologies = ontology.LinkedOntologies.Select(lo => new LinkedOntologyExportModel
            {
                Name = lo.Name,
                Uri = lo.Uri,
                Prefix = lo.Prefix,
                Description = lo.Description,
                ConceptsImported = lo.ConceptsImported,
                ImportedConceptCount = lo.ImportedConceptCount,
                CreatedAt = lo.CreatedAt
            }).ToList(),

            CustomTemplates = ontology.CustomTemplates.Select(ct => new CustomTemplateExportModel
            {
                Category = ct.Category,
                Type = ct.Type,
                Description = ct.Description,
                Examples = ct.Examples,
                Color = ct.Color,
                CreatedAt = ct.CreatedAt,
                UpdatedAt = ct.UpdatedAt
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
        public string? Description { get; set; }
        public string? Namespace { get; set; }
        public string? NamespacePrefixes { get; set; }
        public string? Author { get; set; }
        public string? Version { get; set; }
        public string? License { get; set; }
        public string? Tags { get; set; }
        public string? Notes { get; set; }
        public bool UsesBFO { get; set; }
        public bool UsesProvO { get; set; }
        public string Visibility { get; set; } = string.Empty;
        public bool AllowPublicEdit { get; set; }
        public string? ProvenanceType { get; set; }
        public string? ProvenanceNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int ConceptCount { get; set; }
        public int RelationshipCount { get; set; }
        public List<ConceptExportModel> Concepts { get; set; } = new();
        public List<RelationshipExportModel> Relationships { get; set; } = new();
        public List<IndividualExportModel> Individuals { get; set; } = new();
        public List<IndividualRelationshipExportModel> IndividualRelationships { get; set; } = new();
        public List<LinkedOntologyExportModel> LinkedOntologies { get; set; } = new();
        public List<CustomTemplateExportModel> CustomTemplates { get; set; } = new();
    }

    private class ConceptExportModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Definition { get; set; }
        public string? SimpleExplanation { get; set; }
        public string? Examples { get; set; }
        public string? Category { get; set; }
        public string? Color { get; set; }
        public string? SourceOntology { get; set; }
        public double? PositionX { get; set; }
        public double? PositionY { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PropertyExportModel> Properties { get; set; } = new();
        public List<RestrictionExportModel> Restrictions { get; set; } = new();
    }

    private class RelationshipExportModel
    {
        public string SourceConcept { get; set; } = string.Empty;
        public string RelationType { get; set; } = string.Empty;
        public string TargetConcept { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? Description { get; set; }
        public string? OntologyUri { get; set; }
        public decimal? Strength { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class PropertyExportModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? DataType { get; set; }
        public string? Description { get; set; }
    }

    private class RestrictionExportModel
    {
        public string RestrictionType { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public int? MinCardinality { get; set; }
        public int? MaxCardinality { get; set; }
        public string? ValueType { get; set; }
        public string? MinValue { get; set; }
        public string? MaxValue { get; set; }
        public string? AllowedConceptName { get; set; }
        public string? AllowedValues { get; set; }
        public string? Pattern { get; set; }
        public string? Description { get; set; }
        public bool IsMandatory { get; set; }
    }

    private class IndividualExportModel
    {
        public string Name { get; set; } = string.Empty;
        public string ConceptName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Label { get; set; }
        public string? Uri { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<IndividualPropertyExportModel> Properties { get; set; } = new();
    }

    private class IndividualPropertyExportModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? DataType { get; set; }
        public string? Description { get; set; }
    }

    private class IndividualRelationshipExportModel
    {
        public string SourceIndividual { get; set; } = string.Empty;
        public string RelationType { get; set; } = string.Empty;
        public string TargetIndividual { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? Description { get; set; }
        public string? OntologyUri { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class LinkedOntologyExportModel
    {
        public string Name { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;
        public string? Prefix { get; set; }
        public string? Description { get; set; }
        public bool ConceptsImported { get; set; }
        public int ImportedConceptCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class CustomTemplateExportModel
    {
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Examples { get; set; }
        public string Color { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
