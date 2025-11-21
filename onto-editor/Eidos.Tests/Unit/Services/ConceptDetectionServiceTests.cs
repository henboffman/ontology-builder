using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services;
using Eidos.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Eidos.Tests.Unit.Services;

/// <summary>
/// Unit tests for ConceptDetectionService
/// Tests the core concept detection algorithm for accuracy and edge cases
/// </summary>
public class ConceptDetectionServiceTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly ConceptDetectionService _service;
    private readonly WorkspaceRepository _workspaceRepository;
    private readonly ConceptRepository _conceptRepository;
    private readonly Mock<ILogger<ConceptDetectionService>> _mockLogger;
    private readonly ApplicationUser _testUser;

    public ConceptDetectionServiceTests()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);

        var workspaceLogger = new Mock<ILogger<WorkspaceRepository>>();
        _workspaceRepository = new WorkspaceRepository(_contextFactory, workspaceLogger.Object);
        _conceptRepository = new ConceptRepository(_contextFactory);
        _mockLogger = new Mock<ILogger<ConceptDetectionService>>();

        _service = new ConceptDetectionService(
            _workspaceRepository,
            _conceptRepository,
            _mockLogger.Object);

        _testUser = TestDataBuilder.CreateUser();
    }

    public void Dispose()
    {
        // TestDbContextFactory doesn't implement IDisposable
        // In-memory database will be disposed when context is disposed
    }

    [Fact]
    public async Task DetectConceptsAsync_EmptyContent_ReturnsEmptyList()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var noteId = 1;

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, "");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectConceptsAsync_NullContent_ReturnsEmptyList()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var noteId = 1;

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, null!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectConceptsAsync_NoConceptsInOntology_ReturnsEmptyList()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var noteId = 1;
        var content = "This is a test note with some content.";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectConceptsAsync_SimpleConcept_DetectsCorrectly()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept = await CreateConceptAsync(ontology.Id, "Machine Learning");
        var noteId = 1;
        var content = "Machine Learning is a subset of artificial intelligence.";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Single(result);
        Assert.Equal(concept.Id, result[0].ConceptId);
        Assert.Equal(noteId, result[0].NoteId);
        Assert.Equal(1, result[0].TotalMentions);
        Assert.Equal(0, result[0].FirstMentionPosition);
    }

    [Fact]
    public async Task DetectConceptsAsync_MultipleMentions_CountsCorrectly()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept = await CreateConceptAsync(ontology.Id, "AI");
        var noteId = 1;
        var content = @"AI is important. The field of AI is growing. AI research continues.";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Single(result);
        Assert.Equal(3, result[0].TotalMentions);
        Assert.Equal(0, result[0].FirstMentionPosition); // First "AI" starts at position 0
    }

    [Fact]
    public async Task DetectConceptsAsync_CaseInsensitive_DetectsAllVariants()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept = await CreateConceptAsync(ontology.Id, "neural network");
        var noteId = 1;
        var content = @"Neural Network is a concept. The neural network is powerful. NEURAL NETWORK architectures vary.";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Single(result);
        Assert.Equal(3, result[0].TotalMentions);
    }

    [Fact]
    public async Task DetectConceptsAsync_WholeWordMatching_IgnoresPartialMatches()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept = await CreateConceptAsync(ontology.Id, "learning");
        var noteId = 1;
        var content = @"Machine learning is different from elearning or relearning.";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].TotalMentions); // Only matches "learning" in "Machine learning"
    }

    [Fact]
    public async Task DetectConceptsAsync_LongerPhraseTakesPrecedence_AvoidsDuplicates()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept1 = await CreateConceptAsync(ontology.Id, "machine");
        var concept2 = await CreateConceptAsync(ontology.Id, "machine learning");
        var noteId = 1;
        var content = @"Machine learning is a subset of AI.";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Single(result);
        Assert.Equal(concept2.Id, result[0].ConceptId); // Should match "machine learning", not "machine"
        Assert.Equal(1, result[0].TotalMentions);
    }

    [Fact]
    public async Task DetectConceptsAsync_MultipleConcepts_DetectsAll()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept1 = await CreateConceptAsync(ontology.Id, "AI");
        var concept2 = await CreateConceptAsync(ontology.Id, "neural network");
        var concept3 = await CreateConceptAsync(ontology.Id, "deep learning");
        var noteId = 1;
        var content = @"AI uses neural network models for deep learning. Neural network techniques are powerful.";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Equal(3, result.Count);

        var aiLink = result.First(r => r.ConceptId == concept1.Id);
        Assert.Equal(1, aiLink.TotalMentions);

        var nnLink = result.First(r => r.ConceptId == concept2.Id);
        Assert.Equal(2, nnLink.TotalMentions);

        var dlLink = result.First(r => r.ConceptId == concept3.Id);
        Assert.Equal(1, dlLink.TotalMentions);
    }

    [Fact]
    public async Task DetectConceptsAsync_SpecialCharactersInConcept_EscapesCorrectly()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept = await CreateConceptAsync(ontology.Id, "C++");
        var noteId = 1;
        var content = @"C++ is a programming language. I love C++ development.";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].TotalMentions);
    }

    [Fact]
    public async Task DetectConceptsAsync_ConceptWithDots_EscapesCorrectly()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept = await CreateConceptAsync(ontology.Id, "ASP.NET");
        var noteId = 1;
        var content = @"ASP.NET is a web framework. I build with ASP.NET Core.";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].TotalMentions);
    }

    [Fact]
    public async Task DetectConceptsAsync_ConceptInMarkdown_DetectsInText()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept = await CreateConceptAsync(ontology.Id, "artificial intelligence");
        var noteId = 1;
        var content = @"# Artificial Intelligence

**Artificial intelligence** is the simulation of human intelligence.

- Artificial Intelligence applications
- AI vs artificial intelligence";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Single(result);
        Assert.Equal(4, result[0].TotalMentions);
    }

    [Fact]
    public async Task DetectConceptsAsync_OverlappingConcepts_PrioritizesLonger()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept1 = await CreateConceptAsync(ontology.Id, "neural");
        var concept2 = await CreateConceptAsync(ontology.Id, "neural network");
        var concept3 = await CreateConceptAsync(ontology.Id, "network");
        var noteId = 1;
        var content = @"A neural network is used in deep learning. A network can be neural.";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        // Should detect:
        // - "neural network" once (first sentence)
        // - "network" once (second sentence - doesn't overlap with "neural")
        // - "neural" once (second sentence - doesn't overlap with "network")
        Assert.Equal(3, result.Count);

        var nnLink = result.First(r => r.ConceptId == concept2.Id);
        Assert.Equal(1, nnLink.TotalMentions);

        var networkLink = result.First(r => r.ConceptId == concept3.Id);
        Assert.Equal(1, networkLink.TotalMentions);

        var neuralLink = result.First(r => r.ConceptId == concept1.Id);
        Assert.Equal(1, neuralLink.TotalMentions);
    }

    [Fact]
    public async Task DetectConceptsAsync_NoWorkspace_ReturnsEmptyList()
    {
        // Arrange
        var noteId = 1;
        var nonExistentWorkspaceId = 99999;
        var content = "Some content";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, nonExistentWorkspaceId, content);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectConceptsAsync_WorkspaceWithNoOntology_ReturnsEmptyList()
    {
        // Arrange
        var workspace = await CreateWorkspaceAsync(null);
        var noteId = 1;
        var content = "Some content";

        // Act
        var result = await _service.DetectConceptsAsync(noteId, workspace.Id, content);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectConceptBatchAsync_MultipleNotes_DetectsAllCorrectly()
    {
        // Arrange
        var (workspace, ontology, _) = await CreateWorkspaceWithOntologyAsync();
        var concept1 = await CreateConceptAsync(ontology.Id, "AI");
        var concept2 = await CreateConceptAsync(ontology.Id, "machine learning");

        var notes = new List<(int noteId, string content)>
        {
            (1, "AI is important for machine learning."),
            (2, "Machine learning is a subset of AI."),
            (3, "Neither concept appears here.")
        };

        // Act
        var result = await _service.DetectConceptsBatchAsync(workspace.Id, notes);

        // Assert
        Assert.Equal(3, result.Count);

        // Note 1 should have both concepts
        Assert.Equal(2, result[1].Count);

        // Note 2 should have both concepts
        Assert.Equal(2, result[2].Count);

        // Note 3 should have no concepts
        Assert.Empty(result[3]);
    }

    // Helper Methods

    private async Task<(Workspace workspace, Ontology ontology, ApplicationUser user)> CreateWorkspaceWithOntologyAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Create workspace first
        var workspace = new Workspace
        {
            Name = "Test Workspace",
            UserId = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        // Create ontology and link it to workspace
        // Note: WorkspaceId foreign key is on Ontology, not Workspace
        var ontology = new Ontology
        {
            Name = "Test Ontology",
            Description = "Test ontology for concept detection",
            UserId = _testUser.Id,
            WorkspaceId = workspace.Id,
            ConceptCount = 0,
            RelationshipCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Ontologies.Add(ontology);
        await context.SaveChangesAsync();

        return (workspace, ontology, _testUser);
    }

    private async Task<Workspace> CreateWorkspaceAsync(int? ontologyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var workspace = new Workspace
        {
            Name = "Test Workspace",
            UserId = _testUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        return workspace;
    }

    private async Task<Concept> CreateConceptAsync(int ontologyId, string name)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var concept = new Concept
        {
            Name = name,
            Definition = $"Definition of {name}",
            OntologyId = ontologyId,
            CreatedAt = DateTime.UtcNow
        };

        context.Concepts.Add(concept);
        await context.SaveChangesAsync();

        return concept;
    }
}
