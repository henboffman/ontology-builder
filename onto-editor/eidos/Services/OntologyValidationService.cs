using Eidos.Data;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Eidos.Services;

/// <summary>
/// Service for validating ontology data quality and detecting issues
/// </summary>
public class OntologyValidationService : IOntologyValidationService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OntologyValidationService> _logger;
    private const string CacheKeyPrefix = "validation_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public OntologyValidationService(
        IDbContextFactory<OntologyDbContext> contextFactory,
        IMemoryCache cache,
        ILogger<OntologyValidationService> logger)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateOntologyAsync(int ontologyId)
    {
        var cacheKey = $"{CacheKeyPrefix}{ontologyId}";

        // Try to get from cache first
        if (_cache.TryGetValue<ValidationResult>(cacheKey, out var cachedResult) && cachedResult != null)
        {
            _logger.LogDebug("Returning cached validation result for ontology {OntologyId}", ontologyId);
            return cachedResult;
        }

        _logger.LogInformation("Validating ontology {OntologyId}", ontologyId);

        var result = new ValidationResult { OntologyId = ontologyId };

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Load ontology with all related data
            var ontology = await context.Ontologies
                .AsNoTracking()
                .Include(o => o.Concepts)
                .Include(o => o.Relationships)
                    .ThenInclude(r => r.SourceConcept)
                .Include(o => o.Relationships)
                    .ThenInclude(r => r.TargetConcept)
                .FirstOrDefaultAsync(o => o.Id == ontologyId);

            if (ontology == null)
            {
                _logger.LogWarning("Ontology {OntologyId} not found", ontologyId);
                return result;
            }

            // Run all validation checks
            CheckDuplicateConcepts(ontology, result);
            CheckDuplicateRelationships(ontology, result);
            CheckOrphanedConcepts(ontology, result);
            CheckMissingDescriptions(ontology, result);
            CheckCircularRelationships(ontology, result);

            // Cache the result
            _cache.Set(cacheKey, result, CacheDuration);

            _logger.LogInformation(
                "Validation complete for ontology {OntologyId}: {TotalIssues} issues ({ErrorCount} errors, {WarningCount} warnings)",
                ontologyId, result.TotalIssues, result.ErrorCount, result.WarningCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ontology {OntologyId}", ontologyId);
        }

        return result;
    }

    public async Task<List<ValidationIssue>> GetIssuesAsync(int ontologyId)
    {
        var result = await ValidateOntologyAsync(ontologyId);
        return result.Issues;
    }

    public void InvalidateCache(int ontologyId)
    {
        var cacheKey = $"{CacheKeyPrefix}{ontologyId}";
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated validation cache for ontology {OntologyId}", ontologyId);
    }

    #region Validation Checks

    private void CheckDuplicateConcepts(Ontology ontology, ValidationResult result)
    {
        // Group concepts by name (case-insensitive)
        var duplicateGroups = ontology.Concepts
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var concepts = group.ToList();
            for (int i = 0; i < concepts.Count; i++)
            {
                var concept = concepts[i];
                var others = concepts.Where((c, idx) => idx != i).ToList();

                result.Issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Type = ValidationType.DuplicateConcept,
                    EntityType = "Concept",
                    EntityId = concept.Id,
                    EntityName = concept.Name,
                    Message = $"Duplicate concept: '{concept.Name}'",
                    Details = $"Found {concepts.Count} concepts with the same name (case-insensitive)",
                    RecommendedAction = "Remove or rename duplicate concepts. Ontologies should have unique concept names.",
                    RelatedEntityId = others.FirstOrDefault()?.Id,
                    RelatedEntityName = others.FirstOrDefault()?.Name
                });
            }
        }
    }

    private void CheckDuplicateRelationships(Ontology ontology, ValidationResult result)
    {
        // Group relationships by triple (Source-Type-Target)
        var duplicateGroups = ontology.Relationships
            .GroupBy(r => new
            {
                Source = r.SourceConcept.Name,
                Type = r.RelationType,
                Target = r.TargetConcept.Name
            }, new RelationshipTripleComparer())
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var relationships = group.ToList();
            for (int i = 0; i < relationships.Count; i++)
            {
                var rel = relationships[i];
                var tripleName = $"{rel.SourceConcept.Name} → {rel.RelationType} → {rel.TargetConcept.Name}";

                result.Issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Type = ValidationType.DuplicateRelationship,
                    EntityType = "Relationship",
                    EntityId = rel.Id,
                    EntityName = tripleName,
                    Message = $"Duplicate relationship: {tripleName}",
                    Details = $"Found {relationships.Count} identical relationship triples",
                    RecommendedAction = "Remove duplicate relationships. Each triple should exist only once.",
                    RelatedEntityId = relationships.FirstOrDefault(r => r.Id != rel.Id)?.Id
                });
            }
        }
    }

    private void CheckOrphanedConcepts(Ontology ontology, ValidationResult result)
    {
        var conceptsInRelationships = new HashSet<int>();

        // Collect all concepts that participate in relationships
        foreach (var rel in ontology.Relationships)
        {
            conceptsInRelationships.Add(rel.SourceConceptId);
            conceptsInRelationships.Add(rel.TargetConceptId);
        }

        // Find concepts with no relationships
        var orphanedConcepts = ontology.Concepts
            .Where(c => !conceptsInRelationships.Contains(c.Id))
            .ToList();

        foreach (var concept in orphanedConcepts)
        {
            result.Issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Type = ValidationType.OrphanedConcept,
                EntityType = "Concept",
                EntityId = concept.Id,
                EntityName = concept.Name,
                Message = $"Orphaned concept: '{concept.Name}'",
                Details = "This concept has no relationships (neither incoming nor outgoing)",
                RecommendedAction = "Consider adding relationships or removing if not needed. Isolated concepts reduce ontology connectivity."
            });
        }
    }

    private void CheckMissingDescriptions(Ontology ontology, ValidationResult result)
    {
        var conceptsWithoutDescription = ontology.Concepts
            .Where(c => string.IsNullOrWhiteSpace(c.Definition) && string.IsNullOrWhiteSpace(c.SimpleExplanation))
            .ToList();

        foreach (var concept in conceptsWithoutDescription)
        {
            result.Issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Type = ValidationType.MissingDescription,
                EntityType = "Concept",
                EntityId = concept.Id,
                EntityName = concept.Name,
                Message = $"Missing description: '{concept.Name}'",
                Details = "This concept has no definition or explanation text",
                RecommendedAction = "Add a definition or explanation to improve ontology documentation and clarity."
            });
        }
    }

    private void CheckCircularRelationships(Ontology ontology, ValidationResult result)
    {
        var circularRelationships = ontology.Relationships
            .Where(r => r.SourceConceptId == r.TargetConceptId)
            .ToList();

        foreach (var rel in circularRelationships)
        {
            result.Issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Type = ValidationType.CircularRelationship,
                EntityType = "Relationship",
                EntityId = rel.Id,
                EntityName = $"{rel.SourceConcept.Name} → {rel.RelationType} → {rel.TargetConcept.Name}",
                Message = $"Self-referencing relationship: '{rel.SourceConcept.Name}'",
                Details = $"This relationship points from a concept to itself using '{rel.RelationType}'",
                RecommendedAction = "Review if this circular relationship is intentional. Most ontologies avoid self-references."
            });
        }
    }

    #endregion

    /// <summary>
    /// Custom comparer for relationship triples (case-insensitive)
    /// </summary>
    private class RelationshipTripleComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y)
        {
            if (x == null || y == null) return false;

            var xProps = x.GetType().GetProperties();
            var yProps = y.GetType().GetProperties();

            var xSource = xProps.First(p => p.Name == "Source").GetValue(x)?.ToString();
            var xType = xProps.First(p => p.Name == "Type").GetValue(x)?.ToString();
            var xTarget = xProps.First(p => p.Name == "Target").GetValue(x)?.ToString();

            var ySource = yProps.First(p => p.Name == "Source").GetValue(y)?.ToString();
            var yType = yProps.First(p => p.Name == "Type").GetValue(y)?.ToString();
            var yTarget = yProps.First(p => p.Name == "Target").GetValue(y)?.ToString();

            return string.Equals(xSource, ySource, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(xType, yType, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(xTarget, yTarget, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(object obj)
        {
            var props = obj.GetType().GetProperties();
            var source = props.First(p => p.Name == "Source").GetValue(obj)?.ToString()?.ToLowerInvariant() ?? "";
            var type = props.First(p => p.Name == "Type").GetValue(obj)?.ToString()?.ToLowerInvariant() ?? "";
            var target = props.First(p => p.Name == "Target").GetValue(obj)?.ToString()?.ToLowerInvariant() ?? "";

            return HashCode.Combine(source, type, target);
        }
    }
}
