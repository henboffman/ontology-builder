using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services;
using Eidos.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Eidos.Tests.Integration.Workflows;

/// <summary>
/// Integration tests for the complete note-concept linking workflow
/// Tests the full stack: Note creation/update → Concept detection → Link storage
/// </summary>
public class NoteConceptLinkingWorkflowTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly NoteService _noteService;
    private readonly ConceptDetectionService _conceptDetectionService;
    private readonly NoteRepository _noteRepository;
    private readonly WorkspaceRepository _workspaceRepository;
    private readonly ConceptRepository _conceptRepository;
    private readonly NoteConceptLinkRepository _noteConceptLinkRepository;
    private readonly WikiLinkParser _wikiLinkParser;
    private readonly ApplicationUser _testUser;

    public NoteConceptLinkingWorkflowTests()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);

        // Create real repositories
        var noteLogger = new Mock<ILogger<NoteRepository>>();
        _noteRepository = new NoteRepository(_contextFactory, noteLogger.Object);

        var workspaceLogger = new Mock<ILogger<WorkspaceRepository>>();
        _workspaceRepository = new WorkspaceRepository(_contextFactory, workspaceLogger.Object);

        _conceptRepository = new ConceptRepository(_contextFactory);

        var linkLogger = new Mock<ILogger<NoteConceptLinkRepository>>();
        _noteConceptLinkRepository = new NoteConceptLinkRepository(_contextFactory, linkLogger.Object);

        var detectionLogger = new Mock<ILogger<ConceptDetectionService>>();
        _conceptDetectionService = new ConceptDetectionService(
            _workspaceRepository,
            _conceptRepository,
            detectionLogger.Object);

        // Create mock concept service for wiki-links
        var mockConceptService = new Mock<Eidos.Services.Interfaces.IConceptService>();
        var mockConceptRepository = new Mock<Eidos.Data.Repositories.IConceptRepository>();

        _wikiLinkParser = new WikiLinkParser();

        var noteServiceLogger = new Mock<ILogger<NoteService>>();
        _noteService = new NoteService(
            _noteRepository,
            _workspaceRepository,
            mockConceptService.Object,
            mockConceptRepository.Object,
            _wikiLinkParser,
            _conceptDetectionService,
            _noteConceptLinkRepository,
            noteServiceLogger.Object);

        _testUser = TestDataBuilder.CreateUser();
    }

    public void Dispose()
    {
        // TestDbContextFactory doesn't implement IDisposable
        // In-memory database will be disposed when context is disposed
    }

    [Fact]
    public async Task CreateNote_WithConceptMentions_AutomaticallyCreatesLinks()
    {
        // Arrange
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();
        var concept1 = await CreateConceptAsync(ontology.Id, "machine learning");
        var concept2 = await CreateConceptAsync(ontology.Id, "artificial intelligence");

        var content = @"# Introduction to AI

Machine learning is a subset of artificial intelligence. Both machine learning and artificial intelligence are important.";

        // Act
        var note = await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "AI Introduction",
            content);

        // Assert - verify note was created
        Assert.NotNull(note);
        Assert.Equal("AI Introduction", note.Title);

        // Assert - verify concept links were automatically created
        var links = await _noteConceptLinkRepository.GetByNoteIdAsync(note.Id);
        Assert.Equal(2, links.Count);

        var mlLink = links.First(l => l.ConceptId == concept1.Id);
        Assert.Equal(2, mlLink.TotalMentions);

        var aiLink = links.First(l => l.ConceptId == concept2.Id);
        Assert.Equal(2, aiLink.TotalMentions);
    }

    [Fact]
    public async Task UpdateNote_AddsNewConcepts_UpdatesLinks()
    {
        // Arrange
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();
        var concept1 = await CreateConceptAsync(ontology.Id, "machine learning");
        var concept2 = await CreateConceptAsync(ontology.Id, "neural networks");

        // Create note with one concept mention
        var initialContent = "Machine learning is important.";
        var note = await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "ML Note",
            initialContent);

        // Verify initial state
        var initialLinks = await _noteConceptLinkRepository.GetByNoteIdAsync(note.Id);
        Assert.Single(initialLinks);

        // Act - update note to mention both concepts
        var updatedContent = "Machine learning uses neural networks. Neural networks are powerful.";
        await _noteService.UpdateNoteContentAsync(note.Id, _testUser.Id, updatedContent);

        // Assert - verify links were updated
        var updatedLinks = await _noteConceptLinkRepository.GetByNoteIdAsync(note.Id);
        Assert.Equal(2, updatedLinks.Count);

        var mlLink = updatedLinks.First(l => l.ConceptId == concept1.Id);
        Assert.Equal(1, mlLink.TotalMentions);

        var nnLink = updatedLinks.First(l => l.ConceptId == concept2.Id);
        Assert.Equal(2, nnLink.TotalMentions);
    }

    [Fact]
    public async Task UpdateNote_RemovesConcepts_UpdatesLinks()
    {
        // Arrange
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();
        var concept1 = await CreateConceptAsync(ontology.Id, "machine learning");
        var concept2 = await CreateConceptAsync(ontology.Id, "deep learning");

        // Create note with both concepts
        var initialContent = "Machine learning and deep learning are related.";
        var note = await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "ML vs DL",
            initialContent);

        // Verify initial state
        var initialLinks = await _noteConceptLinkRepository.GetByNoteIdAsync(note.Id);
        Assert.Equal(2, initialLinks.Count);

        // Act - update note to remove one concept
        var updatedContent = "Machine learning is important.";
        await _noteService.UpdateNoteContentAsync(note.Id, _testUser.Id, updatedContent);

        // Assert - verify links were updated
        var updatedLinks = await _noteConceptLinkRepository.GetByNoteIdAsync(note.Id);
        Assert.Single(updatedLinks);
        Assert.Equal(concept1.Id, updatedLinks[0].ConceptId);
    }

    [Fact]
    public async Task UpdateNote_NoConceptMentions_RemovesAllLinks()
    {
        // Arrange
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();
        var concept = await CreateConceptAsync(ontology.Id, "machine learning");

        // Create note with concept mention
        var initialContent = "Machine learning is important.";
        var note = await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "ML Note",
            initialContent);

        // Verify initial state
        var initialLinks = await _noteConceptLinkRepository.GetByNoteIdAsync(note.Id);
        Assert.Single(initialLinks);

        // Act - update note to remove all concepts
        var updatedContent = "This note no longer mentions any concepts.";
        await _noteService.UpdateNoteContentAsync(note.Id, _testUser.Id, updatedContent);

        // Assert - verify all links were removed
        var updatedLinks = await _noteConceptLinkRepository.GetByNoteIdAsync(note.Id);
        Assert.Empty(updatedLinks);
    }

    [Fact]
    public async Task CreateMultipleNotes_SameConcept_CreatesBacklinks()
    {
        // Arrange
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();
        var concept = await CreateConceptAsync(ontology.Id, "artificial intelligence");

        // Act - create multiple notes mentioning the same concept
        var note1 = await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "Note 1",
            "Artificial intelligence is the future.");

        var note2 = await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "Note 2",
            "I study artificial intelligence at university.");

        var note3 = await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "Note 3",
            "Applications of artificial intelligence are widespread.");

        // Assert - verify backlinks from concept to all notes
        var backlinks = await _noteConceptLinkRepository.GetByConceptIdAsync(concept.Id);
        Assert.Equal(3, backlinks.Count);

        var noteIds = backlinks.Select(bl => bl.NoteId).OrderBy(id => id).ToList();
        Assert.Contains(note1.Id, noteIds);
        Assert.Contains(note2.Id, noteIds);
        Assert.Contains(note3.Id, noteIds);
    }

    [Fact]
    public async Task GetWorkspaceConceptLinks_ReturnsAllLinksInWorkspace()
    {
        // Arrange
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();
        var concept1 = await CreateConceptAsync(ontology.Id, "AI");
        var concept2 = await CreateConceptAsync(ontology.Id, "ML");

        await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "Note 1",
            "AI and ML are related.");

        await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "Note 2",
            "ML is a subset of AI.");

        // Act
        var workspaceLinks = await _noteConceptLinkRepository.GetByWorkspaceIdAsync(workspace.Id);

        // Assert
        Assert.Equal(4, workspaceLinks.Count); // 2 concepts × 2 notes = 4 links
    }

    [Fact]
    public async Task CreateNote_WithLongerAndShorterConcepts_PrioritizesLonger()
    {
        // Arrange
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();
        var shortConcept = await CreateConceptAsync(ontology.Id, "network");
        var longConcept = await CreateConceptAsync(ontology.Id, "neural network");

        var content = "A neural network is a type of network.";

        // Act
        var note = await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "Networks",
            content);

        // Assert - verify only appropriate matches
        var links = await _noteConceptLinkRepository.GetByNoteIdAsync(note.Id);
        Assert.Equal(2, links.Count);

        var longLink = links.First(l => l.ConceptId == longConcept.Id);
        Assert.Equal(1, longLink.TotalMentions); // "neural network"

        var shortLink = links.First(l => l.ConceptId == shortConcept.Id);
        Assert.Equal(1, shortLink.TotalMentions); // standalone "network"
    }

    [Fact]
    public async Task DeleteNote_AutomaticallyDeletesConceptLinks()
    {
        // Arrange
        var (workspace, ontology) = await CreateWorkspaceWithOntologyAsync();
        var concept = await CreateConceptAsync(ontology.Id, "machine learning");

        var note = await _noteService.CreateNoteAsync(
            workspace.Id,
            _testUser.Id,
            "ML Note",
            "Machine learning is important.");

        // Verify link was created
        var initialLinks = await _noteConceptLinkRepository.GetByNoteIdAsync(note.Id);
        Assert.Single(initialLinks);

        // Act - delete note
        await _noteService.DeleteNoteAsync(note.Id, _testUser.Id);

        // Assert - verify links were deleted via cascade
        var remainingLinks = await _noteConceptLinkRepository.GetByNoteIdAsync(note.Id);
        Assert.Empty(remainingLinks);
    }

    // Helper Methods

    private async Task<(Workspace workspace, Ontology ontology)> CreateWorkspaceWithOntologyAsync()
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
            Description = "Test ontology for linking",
            UserId = _testUser.Id,
            WorkspaceId = workspace.Id,
            ConceptCount = 0,
            RelationshipCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Ontologies.Add(ontology);
        await context.SaveChangesAsync();

        return (workspace, ontology);
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
