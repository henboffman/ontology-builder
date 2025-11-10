using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Eidos.Services;

/// <summary>
/// Service for detecting changes between ontology states and generating diffs
/// </summary>
public class ChangeDetectionService : IChangeDetectionService
{
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IConceptRepository _conceptRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IIndividualRepository _individualRepository;
    private readonly ILogger<ChangeDetectionService> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ChangeDetectionService(
        IOntologyRepository ontologyRepository,
        IConceptRepository conceptRepository,
        IRelationshipRepository relationshipRepository,
        IIndividualRepository individualRepository,
        ILogger<ChangeDetectionService> logger)
    {
        _ontologyRepository = ontologyRepository;
        _conceptRepository = conceptRepository;
        _relationshipRepository = relationshipRepository;
        _individualRepository = individualRepository;
        _logger = logger;
    }

    public async Task<MergeRequestChange?> DetectConceptChangeAsync(Concept? before, Concept? after, int mergeRequestId)
    {
        // Determine change type
        MergeRequestChangeType changeType;
        string entityName;
        string? beforeJson = null;
        string? afterJson = null;

        if (before == null && after != null)
        {
            changeType = MergeRequestChangeType.Add;
            entityName = after.Name;
            afterJson = JsonSerializer.Serialize(after, _jsonOptions);
        }
        else if (before != null && after == null)
        {
            changeType = MergeRequestChangeType.Delete;
            entityName = before.Name;
            beforeJson = JsonSerializer.Serialize(before, _jsonOptions);
        }
        else if (before != null && after != null)
        {
            changeType = MergeRequestChangeType.Modify;
            entityName = after.Name;
            beforeJson = JsonSerializer.Serialize(before, _jsonOptions);
            afterJson = JsonSerializer.Serialize(after, _jsonOptions);
        }
        else
        {
            return null; // Both null, no change
        }

        var change = new MergeRequestChange
        {
            MergeRequestId = mergeRequestId,
            ChangeType = changeType,
            EntityType = EntityType.Concept,
            EntityId = after?.Id ?? before?.Id ?? 0,
            EntityName = entityName,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            ChangeSummary = GenerateConceptChangeSummary(before, after, changeType)
        };

        return change;
    }

    public async Task<MergeRequestChange?> DetectRelationshipChangeAsync(Relationship? before, Relationship? after, int mergeRequestId)
    {
        MergeRequestChangeType changeType;
        string entityName;
        string? beforeJson = null;
        string? afterJson = null;

        if (before == null && after != null)
        {
            changeType = MergeRequestChangeType.Add;
            entityName = $"{after.SourceConcept?.Name ?? "?"} -> {after.TargetConcept?.Name ?? "?"}";
            afterJson = JsonSerializer.Serialize(after, _jsonOptions);
        }
        else if (before != null && after == null)
        {
            changeType = MergeRequestChangeType.Delete;
            entityName = $"{before.SourceConcept?.Name ?? "?"} -> {before.TargetConcept?.Name ?? "?"}";
            beforeJson = JsonSerializer.Serialize(before, _jsonOptions);
        }
        else if (before != null && after != null)
        {
            changeType = MergeRequestChangeType.Modify;
            entityName = $"{after.SourceConcept?.Name ?? "?"} -> {after.TargetConcept?.Name ?? "?"}";
            beforeJson = JsonSerializer.Serialize(before, _jsonOptions);
            afterJson = JsonSerializer.Serialize(after, _jsonOptions);
        }
        else
        {
            return null;
        }

        var change = new MergeRequestChange
        {
            MergeRequestId = mergeRequestId,
            ChangeType = changeType,
            EntityType = EntityType.Relationship,
            EntityId = after?.Id ?? before?.Id ?? 0,
            EntityName = entityName,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            ChangeSummary = GenerateRelationshipChangeSummary(before, after, changeType)
        };

        return change;
    }

    public async Task<MergeRequestChange?> DetectIndividualChangeAsync(Individual? before, Individual? after, int mergeRequestId)
    {
        MergeRequestChangeType changeType;
        string entityName;
        string? beforeJson = null;
        string? afterJson = null;

        if (before == null && after != null)
        {
            changeType = MergeRequestChangeType.Add;
            entityName = after.Name;
            afterJson = JsonSerializer.Serialize(after, _jsonOptions);
        }
        else if (before != null && after == null)
        {
            changeType = MergeRequestChangeType.Delete;
            entityName = before.Name;
            beforeJson = JsonSerializer.Serialize(before, _jsonOptions);
        }
        else if (before != null && after != null)
        {
            changeType = MergeRequestChangeType.Modify;
            entityName = after.Name;
            beforeJson = JsonSerializer.Serialize(before, _jsonOptions);
            afterJson = JsonSerializer.Serialize(after, _jsonOptions);
        }
        else
        {
            return null;
        }

        var change = new MergeRequestChange
        {
            MergeRequestId = mergeRequestId,
            ChangeType = changeType,
            EntityType = EntityType.Individual,
            EntityId = after?.Id ?? before?.Id ?? 0,
            EntityName = entityName,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            ChangeSummary = GenerateIndividualChangeSummary(before, after, changeType)
        };

        return change;
    }

    public async Task<List<MergeRequestChange>> DetectAllChangesAsync(int ontologyId, string baseSnapshotJson, int mergeRequestId)
    {
        var changes = new List<MergeRequestChange>();

        try
        {
            // Parse base snapshot
            var baseSnapshot = JsonSerializer.Deserialize<OntologySnapshot>(baseSnapshotJson);
            if (baseSnapshot == null)
            {
                _logger.LogWarning("Failed to parse base snapshot for ontology {OntologyId}", ontologyId);
                return changes;
            }

            // Get current state
            var currentConcepts = await _conceptRepository.GetByOntologyIdAsync(ontologyId);
            var currentRelationships = await _relationshipRepository.GetByOntologyIdAsync(ontologyId);
            var currentIndividuals = await _individualRepository.GetByOntologyIdAsync(ontologyId);

            // Detect concept changes
            var conceptChanges = await DetectConceptChangesAsync(
                baseSnapshot.Concepts,
                currentConcepts.ToList(),
                mergeRequestId);
            changes.AddRange(conceptChanges);

            // Detect relationship changes
            var relationshipChanges = await DetectRelationshipChangesAsync(
                baseSnapshot.Relationships,
                currentRelationships.ToList(),
                mergeRequestId);
            changes.AddRange(relationshipChanges);

            // Detect individual changes
            var individualChanges = await DetectIndividualChangesAsync(
                baseSnapshot.Individuals,
                currentIndividuals.ToList(),
                mergeRequestId);
            changes.AddRange(individualChanges);

            // Set order index
            for (int i = 0; i < changes.Count; i++)
            {
                changes[i].OrderIndex = i;
            }

            _logger.LogInformation("Detected {ChangeCount} changes for ontology {OntologyId}", changes.Count, ontologyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting changes for ontology {OntologyId}", ontologyId);
        }

        return changes;
    }

    public async Task<string> CreateSnapshotAsync(int ontologyId)
    {
        var concepts = await _conceptRepository.GetByOntologyIdAsync(ontologyId);
        var relationships = await _relationshipRepository.GetByOntologyIdAsync(ontologyId);
        var individuals = await _individualRepository.GetByOntologyIdAsync(ontologyId);

        var snapshot = new OntologySnapshot
        {
            OntologyId = ontologyId,
            Timestamp = DateTime.UtcNow,
            Concepts = concepts.ToList(),
            Relationships = relationships.ToList(),
            Individuals = individuals.ToList()
        };

        return JsonSerializer.Serialize(snapshot, _jsonOptions);
    }

    public string GenerateChangeSummary(MergeRequestChange change)
    {
        return change.EntityType switch
        {
            EntityType.Concept => GenerateConceptChangeSummaryFromJson(change),
            EntityType.Relationship => GenerateRelationshipChangeSummaryFromJson(change),
            EntityType.Individual => GenerateIndividualChangeSummaryFromJson(change),
            _ => $"{change.ChangeTypeDisplay} {change.EntityTypeDisplay}"
        };
    }

    public async Task<bool> HasConflictAsync(MergeRequestChange change, int ontologyId)
    {
        // Check if the entity has been modified since the base snapshot
        try
        {
            if (change.ChangeType == MergeRequestChangeType.Add)
            {
                // For additions, check if an entity with the same name already exists
                if (change.EntityType == EntityType.Concept)
                {
                    var concepts = await _conceptRepository.GetByOntologyIdAsync(ontologyId);
                    return concepts.Any(c => c.Name == change.EntityName);
                }
            }
            else if (change.ChangeType == MergeRequestChangeType.Modify || change.ChangeType == MergeRequestChangeType.Delete)
            {
                // For modifications/deletions, check if the entity still exists and hasn't been modified
                // This is a simplified check - in production, you'd compare timestamps or checksums
                if (change.EntityType == EntityType.Concept)
                {
                    var concept = await _conceptRepository.GetByIdAsync(change.EntityId);
                    if (concept == null && change.ChangeType == MergeRequestChangeType.Modify)
                    {
                        return true; // Entity was deleted
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking conflict for change {ChangeId}", change.Id);
            return false;
        }
    }

    // Private helper methods

    private async Task<List<MergeRequestChange>> DetectConceptChangesAsync(
        List<Concept> baseConcepts,
        List<Concept> currentConcepts,
        int mergeRequestId)
    {
        var changes = new List<MergeRequestChange>();

        // Find added concepts
        foreach (var current in currentConcepts)
        {
            var baseMatch = baseConcepts.FirstOrDefault(b => b.Id == current.Id);
            if (baseMatch == null)
            {
                var change = await DetectConceptChangeAsync(null, current, mergeRequestId);
                if (change != null) changes.Add(change);
            }
        }

        // Find deleted concepts
        foreach (var baseItem in baseConcepts)
        {
            var currentMatch = currentConcepts.FirstOrDefault(c => c.Id == baseItem.Id);
            if (currentMatch == null)
            {
                var change = await DetectConceptChangeAsync(baseItem, null, mergeRequestId);
                if (change != null) changes.Add(change);
            }
        }

        // Find modified concepts
        foreach (var current in currentConcepts)
        {
            var baseMatch = baseConcepts.FirstOrDefault(b => b.Id == current.Id);
            if (baseMatch != null && HasConceptChanged(baseMatch, current))
            {
                var change = await DetectConceptChangeAsync(baseMatch, current, mergeRequestId);
                if (change != null) changes.Add(change);
            }
        }

        return changes;
    }

    private async Task<List<MergeRequestChange>> DetectRelationshipChangesAsync(
        List<Relationship> baseRelationships,
        List<Relationship> currentRelationships,
        int mergeRequestId)
    {
        var changes = new List<MergeRequestChange>();

        // Find added relationships
        foreach (var current in currentRelationships)
        {
            var baseMatch = baseRelationships.FirstOrDefault(b => b.Id == current.Id);
            if (baseMatch == null)
            {
                var change = await DetectRelationshipChangeAsync(null, current, mergeRequestId);
                if (change != null) changes.Add(change);
            }
        }

        // Find deleted relationships
        foreach (var baseItem in baseRelationships)
        {
            var currentMatch = currentRelationships.FirstOrDefault(c => c.Id == baseItem.Id);
            if (currentMatch == null)
            {
                var change = await DetectRelationshipChangeAsync(baseItem, null, mergeRequestId);
                if (change != null) changes.Add(change);
            }
        }

        // Find modified relationships
        foreach (var current in currentRelationships)
        {
            var baseMatch = baseRelationships.FirstOrDefault(b => b.Id == current.Id);
            if (baseMatch != null && HasRelationshipChanged(baseMatch, current))
            {
                var change = await DetectRelationshipChangeAsync(baseMatch, current, mergeRequestId);
                if (change != null) changes.Add(change);
            }
        }

        return changes;
    }

    private async Task<List<MergeRequestChange>> DetectIndividualChangesAsync(
        List<Individual> baseIndividuals,
        List<Individual> currentIndividuals,
        int mergeRequestId)
    {
        var changes = new List<MergeRequestChange>();

        // Find added individuals
        foreach (var current in currentIndividuals)
        {
            var baseMatch = baseIndividuals.FirstOrDefault(b => b.Id == current.Id);
            if (baseMatch == null)
            {
                var change = await DetectIndividualChangeAsync(null, current, mergeRequestId);
                if (change != null) changes.Add(change);
            }
        }

        // Find deleted individuals
        foreach (var baseItem in baseIndividuals)
        {
            var currentMatch = currentIndividuals.FirstOrDefault(c => c.Id == baseItem.Id);
            if (currentMatch == null)
            {
                var change = await DetectIndividualChangeAsync(baseItem, null, mergeRequestId);
                if (change != null) changes.Add(change);
            }
        }

        // Find modified individuals
        foreach (var current in currentIndividuals)
        {
            var baseMatch = baseIndividuals.FirstOrDefault(b => b.Id == current.Id);
            if (baseMatch != null && HasIndividualChanged(baseMatch, current))
            {
                var change = await DetectIndividualChangeAsync(baseMatch, current, mergeRequestId);
                if (change != null) changes.Add(change);
            }
        }

        return changes;
    }

    private bool HasConceptChanged(Concept before, Concept after)
    {
        return before.Name != after.Name ||
               before.Definition != after.Definition ||
               before.SimpleExplanation != after.SimpleExplanation ||
               before.Examples != after.Examples ||
               before.Category != after.Category ||
               before.Color != after.Color ||
               before.PositionX != after.PositionX ||
               before.PositionY != after.PositionY;
    }

    private bool HasRelationshipChanged(Relationship before, Relationship after)
    {
        return before.RelationType != after.RelationType ||
               before.Description != after.Description ||
               before.Strength != after.Strength ||
               before.SourceConceptId != after.SourceConceptId ||
               before.TargetConceptId != after.TargetConceptId;
    }

    private bool HasIndividualChanged(Individual before, Individual after)
    {
        return before.Name != after.Name ||
               before.Description != after.Description ||
               before.ConceptId != after.ConceptId;
    }

    private string GenerateConceptChangeSummary(Concept? before, Concept? after, MergeRequestChangeType changeType)
    {
        if (changeType == MergeRequestChangeType.Add)
        {
            return $"Added concept '{after!.Name}'";
        }
        else if (changeType == MergeRequestChangeType.Delete)
        {
            return $"Deleted concept '{before!.Name}'";
        }
        else
        {
            var changes = new List<string>();
            if (before!.Name != after!.Name)
                changes.Add($"name: '{before.Name}' → '{after.Name}'");
            if (before.Definition != after.Definition)
                changes.Add("definition changed");
            if (before.Category != after.Category)
                changes.Add($"category: '{before.Category}' → '{after.Category}'");

            return $"Modified concept '{after.Name}': {string.Join(", ", changes)}";
        }
    }

    private string GenerateRelationshipChangeSummary(Relationship? before, Relationship? after, MergeRequestChangeType changeType)
    {
        if (changeType == MergeRequestChangeType.Add)
        {
            return $"Added relationship: {after!.RelationType}";
        }
        else if (changeType == MergeRequestChangeType.Delete)
        {
            return $"Deleted relationship: {before!.RelationType}";
        }
        else
        {
            return $"Modified relationship: {after!.RelationType}";
        }
    }

    private string GenerateIndividualChangeSummary(Individual? before, Individual? after, MergeRequestChangeType changeType)
    {
        if (changeType == MergeRequestChangeType.Add)
        {
            return $"Added individual '{after!.Name}'";
        }
        else if (changeType == MergeRequestChangeType.Delete)
        {
            return $"Deleted individual '{before!.Name}'";
        }
        else
        {
            return $"Modified individual '{after!.Name}'";
        }
    }

    private string GenerateConceptChangeSummaryFromJson(MergeRequestChange change)
    {
        try
        {
            if (change.ChangeType == MergeRequestChangeType.Add)
            {
                return $"Added concept '{change.EntityName}'";
            }
            else if (change.ChangeType == MergeRequestChangeType.Delete)
            {
                return $"Deleted concept '{change.EntityName}'";
            }
            else
            {
                return $"Modified concept '{change.EntityName}'";
            }
        }
        catch
        {
            return change.ChangeSummary ?? $"{change.ChangeTypeDisplay} {change.EntityName}";
        }
    }

    private string GenerateRelationshipChangeSummaryFromJson(MergeRequestChange change)
    {
        return change.ChangeSummary ?? $"{change.ChangeTypeDisplay} relationship";
    }

    private string GenerateIndividualChangeSummaryFromJson(MergeRequestChange change)
    {
        return change.ChangeSummary ?? $"{change.ChangeTypeDisplay} individual '{change.EntityName}'";
    }

    // Snapshot model
    private class OntologySnapshot
    {
        public int OntologyId { get; set; }
        public DateTime Timestamp { get; set; }
        public List<Concept> Concepts { get; set; } = new();
        public List<Relationship> Relationships { get; set; } = new();
        public List<Individual> Individuals { get; set; } = new();
    }
}
