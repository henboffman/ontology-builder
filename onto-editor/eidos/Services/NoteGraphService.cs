using Eidos.Data.Repositories;
using Eidos.Models;
using Microsoft.Extensions.Logging;

namespace Eidos.Services;

/// <summary>
/// Service for building note network graph data
/// Transforms notes, concepts, and relationships into a hierarchical graph structure
/// </summary>
public class NoteGraphService
{
    private readonly NoteRepository _noteRepository;
    private readonly NoteConceptLinkRepository _noteConceptLinkRepository;
    private readonly ConceptRepository _conceptRepository;
    private readonly RelationshipRepository _relationshipRepository;
    private readonly WorkspaceRepository _workspaceRepository;
    private readonly ILogger<NoteGraphService> _logger;

    public NoteGraphService(
        NoteRepository noteRepository,
        NoteConceptLinkRepository noteConceptLinkRepository,
        ConceptRepository conceptRepository,
        RelationshipRepository relationshipRepository,
        WorkspaceRepository workspaceRepository,
        ILogger<NoteGraphService> logger)
    {
        _noteRepository = noteRepository;
        _noteConceptLinkRepository = noteConceptLinkRepository;
        _conceptRepository = conceptRepository;
        _relationshipRepository = relationshipRepository;
        _workspaceRepository = workspaceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Build hierarchical graph data showing notes, their concepts, and relationships
    /// Creates a structure where:
    /// - Notes are parent nodes
    /// - Concepts mentioned in notes are child nodes
    /// - Relationships between concepts are shown as edges
    /// </summary>
    public async Task<NoteGraphData> BuildGraphAsync(int workspaceId)
    {
        try
        {
            // Get workspace to access ontology
            var workspace = await _workspaceRepository.GetByIdAsync(workspaceId, includeOntology: true);
            if (workspace?.Ontology == null)
            {
                _logger.LogWarning("Workspace {WorkspaceId} has no ontology", workspaceId);
                return new NoteGraphData();
            }

            var ontologyId = workspace.Ontology.Id;

            // Get all notes in workspace
            var notes = await _noteRepository.GetByWorkspaceIdAsync(workspaceId);

            // Get all concept links for this workspace
            var conceptLinks = await _noteConceptLinkRepository.GetByWorkspaceIdAsync(workspaceId);

            // Get all concepts in the ontology
            var allConcepts = (await _conceptRepository.GetByOntologyIdAsync(ontologyId)).ToList();
            var conceptsById = allConcepts.ToDictionary(c => c.Id);

            // Get all relationships in the ontology
            var relationships = (await _relationshipRepository.GetByOntologyIdAsync(ontologyId)).ToList();

            var nodes = new List<NoteGraphNode>();
            var edges = new List<NoteGraphEdge>();

            // Step 1: Create note nodes (parent nodes)
            foreach (var note in notes)
            {
                nodes.Add(new NoteGraphNode
                {
                    Id = $"note-{note.Id}",
                    NoteId = note.Id,
                    Label = note.Title,
                    NodeType = "note",
                    ConceptCount = conceptLinks.Count(cl => cl.NoteId == note.Id),
                    Tags = new List<string>(), // TODO: Load tags from NoteTagAssignments
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt,
                    IsConceptNote = note.LinkedConceptId.HasValue
                });
            }

            // Step 2: Create concept nodes for concepts mentioned in notes
            var conceptsInNotes = new HashSet<int>();
            foreach (var link in conceptLinks)
            {
                conceptsInNotes.Add(link.ConceptId);

                // Add concept node if not already added
                if (!nodes.Any(n => n.Id == $"concept-{link.ConceptId}") && conceptsById.ContainsKey(link.ConceptId))
                {
                    var concept = conceptsById[link.ConceptId];
                    nodes.Add(new NoteGraphNode
                    {
                        Id = $"concept-{concept.Id}",
                        ConceptId = concept.Id,
                        Label = concept.Name,
                        NodeType = "concept",
                        ConceptCategory = concept.Category,
                        ConceptCount = 0,
                        Tags = new List<string>(),
                        CreatedAt = concept.CreatedAt,
                        UpdatedAt = DateTime.UtcNow,
                        IsConceptNote = false
                    });
                }

                // Create edge from note to concept (parent-child relationship)
                edges.Add(new NoteGraphEdge
                {
                    Id = $"note-{link.NoteId}-concept-{link.ConceptId}",
                    Source = $"note-{link.NoteId}",
                    Target = $"concept-{link.ConceptId}",
                    EdgeType = "contains",
                    SharedConceptCount = link.TotalMentions,
                    SharedConceptIds = new List<int> { link.ConceptId }
                });
            }

            // Step 3: Add relationship edges between concepts
            foreach (var relationship in relationships)
            {
                // Only add relationships between concepts that are mentioned in notes
                if (conceptsInNotes.Contains(relationship.SourceConceptId) &&
                    conceptsInNotes.Contains(relationship.TargetConceptId))
                {
                    edges.Add(new NoteGraphEdge
                    {
                        Id = $"rel-{relationship.Id}",
                        Source = $"concept-{relationship.SourceConceptId}",
                        Target = $"concept-{relationship.TargetConceptId}",
                        EdgeType = relationship.RelationType ?? "related-to",
                        Label = relationship.Label,
                        SharedConceptCount = 1,
                        SharedConceptIds = new List<int> { relationship.SourceConceptId, relationship.TargetConceptId }
                    });
                }
            }

            var graphData = new NoteGraphData
            {
                Nodes = nodes,
                Edges = edges,
                Statistics = new NoteGraphStatistics
                {
                    TotalNotes = notes.Count,
                    TotalConcepts = conceptsInNotes.Count,
                    TotalRelationships = relationships.Count(r =>
                        conceptsInNotes.Contains(r.SourceConceptId) &&
                        conceptsInNotes.Contains(r.TargetConceptId)),
                    TotalEdges = edges.Count,
                    AverageConceptsPerNote = notes.Count > 0 ? conceptLinks.Count / (double)notes.Count : 0,
                    ConnectedNotes = nodes.Count(n => n.NodeType == "note" && edges.Any(e => e.Source == n.Id)),
                    IsolatedNotes = nodes.Count(n => n.NodeType == "note" && !edges.Any(e => e.Source == n.Id))
                }
            };

            _logger.LogInformation("Built hierarchical graph for workspace {WorkspaceId}: {NoteCount} notes, {ConceptCount} concepts, {EdgeCount} edges",
                workspaceId, notes.Count, conceptsInNotes.Count, edges.Count);

            return graphData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building note graph for workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }

    /// <summary>
    /// Filter graph data based on criteria
    /// </summary>
    public NoteGraphData FilterGraph(NoteGraphData graphData, NoteGraphFilter filter)
    {
        var filteredNodes = graphData.Nodes.AsEnumerable();

        // Filter by tags
        if (filter.Tags?.Any() == true)
        {
            filteredNodes = filteredNodes.Where(n =>
                n.Tags.Any(tag => filter.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        // Filter by date range
        if (filter.StartDate.HasValue)
        {
            filteredNodes = filteredNodes.Where(n => n.CreatedAt >= filter.StartDate.Value);
        }
        if (filter.EndDate.HasValue)
        {
            filteredNodes = filteredNodes.Where(n => n.CreatedAt <= filter.EndDate.Value);
        }

        // Filter by search term (note title)
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            filteredNodes = filteredNodes.Where(n =>
                n.Label.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var filteredNodesList = filteredNodes.ToList();
        var filteredNodeIds = filteredNodesList.Select(n => n.Id).ToHashSet();

        // Filter edges to only include those between filtered nodes
        var filteredEdges = graphData.Edges
            .Where(e => filteredNodeIds.Contains(e.Source) && filteredNodeIds.Contains(e.Target))
            .ToList();

        return new NoteGraphData
        {
            Nodes = filteredNodesList,
            Edges = filteredEdges,
            Statistics = new NoteGraphStatistics
            {
                TotalNotes = filteredNodesList.Count,
                TotalEdges = filteredEdges.Count,
                AverageConceptsPerNote = filteredNodesList.Count > 0 ? filteredNodesList.Average(n => n.ConceptCount) : 0,
                ConnectedNotes = filteredNodesList.Count(n => filteredEdges.Any(e => e.Source == n.Id || e.Target == n.Id)),
                IsolatedNotes = filteredNodesList.Count(n => !filteredEdges.Any(e => e.Source == n.Id || e.Target == n.Id))
            }
        };
    }
}

/// <summary>
/// Complete note graph data structure
/// </summary>
public class NoteGraphData
{
    public List<NoteGraphNode> Nodes { get; set; } = new();
    public List<NoteGraphEdge> Edges { get; set; } = new();
    public NoteGraphStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Node representing a note or concept in the graph
/// </summary>
public class NoteGraphNode
{
    public string Id { get; set; } = string.Empty;
    public int? NoteId { get; set; }  // Set for note nodes
    public int? ConceptId { get; set; }  // Set for concept nodes
    public string Label { get; set; } = string.Empty;
    public string NodeType { get; set; } = "note";  // "note" or "concept"
    public string? ConceptCategory { get; set; }  // For concept nodes
    public int ConceptCount { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsConceptNote { get; set; }
}

/// <summary>
/// Edge representing relationships between nodes
/// </summary>
public class NoteGraphEdge
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string EdgeType { get; set; } = "shared";  // "contains", "is-a", "part-of", "related-to", etc.
    public string? Label { get; set; }  // Optional label for relationship edges
    public int SharedConceptCount { get; set; }
    public List<int> SharedConceptIds { get; set; } = new();
}

/// <summary>
/// Graph statistics
/// </summary>
public class NoteGraphStatistics
{
    public int TotalNotes { get; set; }
    public int TotalConcepts { get; set; }
    public int TotalRelationships { get; set; }
    public int TotalEdges { get; set; }
    public double AverageConceptsPerNote { get; set; }
    public int ConnectedNotes { get; set; }
    public int IsolatedNotes { get; set; }
}

/// <summary>
/// Filter criteria for note graph
/// </summary>
public class NoteGraphFilter
{
    public List<string>? Tags { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public List<int>? ConceptIds { get; set; }
}
