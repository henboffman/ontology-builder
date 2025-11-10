using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for detecting and calculating differences between ontology states
/// </summary>
public interface IChangeDetectionService
{
    /// <summary>
    /// Compares two concepts and generates a change summary
    /// </summary>
    Task<MergeRequestChange?> DetectConceptChangeAsync(Concept? before, Concept? after, int mergeRequestId);

    /// <summary>
    /// Compares two relationships and generates a change summary
    /// </summary>
    Task<MergeRequestChange?> DetectRelationshipChangeAsync(Relationship? before, Relationship? after, int mergeRequestId);

    /// <summary>
    /// Compares two individuals and generates a change summary
    /// </summary>
    Task<MergeRequestChange?> DetectIndividualChangeAsync(Individual? before, Individual? after, int mergeRequestId);

    /// <summary>
    /// Detects all changes between two ontology states
    /// </summary>
    Task<List<MergeRequestChange>> DetectAllChangesAsync(int ontologyId, string baseSnapshotJson, int mergeRequestId);

    /// <summary>
    /// Creates a snapshot of the current ontology state
    /// </summary>
    Task<string> CreateSnapshotAsync(int ontologyId);

    /// <summary>
    /// Generates a human-readable summary of changes
    /// </summary>
    string GenerateChangeSummary(MergeRequestChange change);

    /// <summary>
    /// Checks if a change conflicts with the current ontology state
    /// </summary>
    Task<bool> HasConflictAsync(MergeRequestChange change, int ontologyId);
}
