using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Hubs;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services;
using Eidos.Services.Commands;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Eidos.Tests.Integration.Services;

/// <summary>
/// TRUE integration tests for ConceptService using real repositories and database
/// Tests the full stack: Service → Repository → Database
/// </summary>
public class ConceptServiceIntegrationTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly ConceptService _service;
    private readonly IConceptRepository _conceptRepository; // REAL
    private readonly IOntologyRepository _ontologyRepository; // REAL
    private readonly IRelationshipRepository _relationshipRepository; // REAL
    private readonly Mock<ICommandFactory> _mockCommandFactory;
    private readonly Mock<CommandInvoker> _mockCommandInvoker;
    private readonly Mock<IHubContext<OntologyHub>> _mockHubContext;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IOntologyShareService> _mockShareService;
    private readonly Mock<IOntologyActivityService> _mockActivityService;
    private readonly Mock<IDbContextFactory<OntologyDbContext>> _mockContextFactory;
    private readonly OntologyPermissionService _permissionService;
    private readonly NoteRepository _noteRepository;
    private readonly WorkspaceRepository _workspaceRepository;
    private readonly ConceptDetectionService _conceptDetectionService;
    private readonly NoteConceptLinkRepository _noteConceptLinkRepository;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<ConceptService>> _mockLogger;
    private readonly ApplicationUser _testUser;

    public ConceptServiceIntegrationTests()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);

        // Use REAL repositories for true integration testing
        _conceptRepository = new ConceptRepository(_contextFactory);
        _ontologyRepository = new OntologyRepository(_contextFactory);
        _relationshipRepository = new RelationshipRepository(_contextFactory);

        // Mock only external concerns
        _mockCommandFactory = new Mock<ICommandFactory>();
        _mockCommandInvoker = new Mock<CommandInvoker>();
        _mockHubContext = new Mock<IHubContext<OntologyHub>>();
        _mockUserService = new Mock<IUserService>();
        _mockShareService = new Mock<IOntologyShareService>();
        _mockActivityService = new Mock<IOntologyActivityService>();
        _mockContextFactory = new Mock<IDbContextFactory<OntologyDbContext>>();
        _permissionService = new OntologyPermissionService(_contextFactory);
        var noteLogger = new Mock<Microsoft.Extensions.Logging.ILogger<NoteRepository>>();
        _noteRepository = new NoteRepository(_contextFactory, noteLogger.Object);
        var workspaceLogger = new Mock<Microsoft.Extensions.Logging.ILogger<WorkspaceRepository>>();
        _workspaceRepository = new WorkspaceRepository(_contextFactory, workspaceLogger.Object);
        var detectionLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ConceptDetectionService>>();
        _conceptDetectionService = new ConceptDetectionService(_workspaceRepository, _conceptRepository, detectionLogger.Object);
        var linkLogger = new Mock<Microsoft.Extensions.Logging.ILogger<NoteConceptLinkRepository>>();
        _noteConceptLinkRepository = new NoteConceptLinkRepository(_contextFactory, linkLogger.Object);
        _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ConceptService>>();

        _testUser = TestDataBuilder.CreateUser();
        _mockUserService.Setup(s => s.GetCurrentUserAsync()).ReturnsAsync(_testUser);

        // Default permission to allow operations
        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<PermissionLevel>()))
            .ReturnsAsync(true);

        // Setup SignalR hub context
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        _service = new ConceptService(
            _conceptRepository,      // REAL - actual database operations
            _ontologyRepository,     // REAL - actual database operations
            _relationshipRepository, // REAL - actual database operations
            _mockCommandFactory.Object,
            _mockCommandInvoker.Object,
            _mockHubContext.Object,
            _mockUserService.Object,
            _mockShareService.Object,
            _mockActivityService.Object,
            _mockContextFactory.Object,
            _permissionService,
            _noteRepository,
            _workspaceRepository,
            _conceptDetectionService,
            _noteConceptLinkRepository,
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        // In-memory database will be cleaned up automatically
    }

    [Fact]
    public async Task UpdateConcept_ShouldPersistChangesToDatabase()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        var concept = TestDataBuilder.CreateConcept(ontology.Id, "Original Name");
        var created = await _conceptRepository.AddAsync(concept);

        // Act - Update the concept
        created.Name = "Updated Name";
        created.Definition = "New definition";
        await _service.UpdateAsync(created, recordUndo: false);

        // Assert - Verify changes are in database
        var retrieved = await _conceptRepository.GetByIdAsync(created.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.Name);
        Assert.Equal("New definition", retrieved.Definition);
    }

    [Fact]
    public async Task GetByOntologyId_ShouldReturnAllConceptsForOntology()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Concept 1"));
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Concept 2"));
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Concept 3"));

        // Act
        var concepts = await _service.GetByOntologyIdAsync(ontology.Id);

        // Assert
        Assert.Equal(3, concepts.Count());
        Assert.All(concepts, c => Assert.Equal(ontology.Id, c.OntologyId));
    }

    [Fact]
    public async Task SearchConcepts_ShouldFindMatchingConcepts()
    {
        // Arrange
        var ontology = await CreateRealOntology();
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Person"));
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Personal Data"));
        await _conceptRepository.AddAsync(TestDataBuilder.CreateConcept(ontology.Id, "Organization"));

        // Act
        var results = await _service.SearchAsync("Person");

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, c => Assert.Contains("Person", c.Name));
    }

    /// <summary>
    /// Helper to create a real ontology in the database
    /// </summary>
    private async Task<Ontology> CreateRealOntology(string name = "Test Ontology")
    {
        var ontology = TestDataBuilder.CreateOntology(name: name, userId: _testUser.Id);
        return await _ontologyRepository.AddAsync(ontology);
    }
}
