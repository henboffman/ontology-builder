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
/// Integration tests for ConceptService
/// </summary>
public class ConceptServiceTests : IDisposable
{
    private readonly Mock<IConceptRepository> _mockConceptRepository;
    private readonly Mock<IOntologyRepository> _mockOntologyRepository;
    private readonly Mock<IRelationshipRepository> _mockRelationshipRepository;
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
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<ConceptService>> _mockLogger;
    private readonly ConceptService _service;
    private readonly ApplicationUser _testUser;

    public ConceptServiceTests()
    {
        _mockConceptRepository = new Mock<IConceptRepository>();
        _mockOntologyRepository = new Mock<IOntologyRepository>();
        _mockRelationshipRepository = new Mock<IRelationshipRepository>();
        _mockCommandFactory = new Mock<ICommandFactory>();
        _mockCommandInvoker = new Mock<CommandInvoker>();
        _mockHubContext = new Mock<IHubContext<OntologyHub>>();
        _mockUserService = new Mock<IUserService>();
        _mockShareService = new Mock<IOntologyShareService>();
        _mockActivityService = new Mock<IOntologyActivityService>();
        _mockContextFactory = new Mock<IDbContextFactory<OntologyDbContext>>();
        _permissionService = new OntologyPermissionService(_mockContextFactory.Object);
        var noteLogger = new Mock<Microsoft.Extensions.Logging.ILogger<NoteRepository>>();
        _noteRepository = new NoteRepository(_mockContextFactory.Object, noteLogger.Object);
        var workspaceLogger = new Mock<Microsoft.Extensions.Logging.ILogger<WorkspaceRepository>>();
        _workspaceRepository = new WorkspaceRepository(_mockContextFactory.Object, workspaceLogger.Object);
        _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ConceptService>>();

        _testUser = TestDataBuilder.CreateUser();
        _mockUserService.Setup(s => s.GetCurrentUserAsync()).ReturnsAsync(_testUser);

        // Setup default permission to allow operations
        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<PermissionLevel>()))
            .ReturnsAsync(true);

        // Setup SignalR hub context to prevent null reference
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        _service = new ConceptService(
            _mockConceptRepository.Object,
            _mockOntologyRepository.Object,
            _mockRelationshipRepository.Object,
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
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task DeleteAsync_WhenConceptNotFound_ShouldNotThrow()
    {
        // Arrange
        _mockConceptRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Concept?)null);

        // Act & Assert - should not throw
        await _service.DeleteAsync(999, recordUndo: false);

        // Verify delete was not called
        _mockConceptRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnConcept()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        concept.Id = 123;

        _mockConceptRepository
            .Setup(r => r.GetByIdAsync(123))
            .ReturnsAsync(concept);

        // Act
        var result = await _service.GetByIdAsync(123);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.Id);
        Assert.Equal("Test Concept", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        _mockConceptRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Concept?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOntologyIdAsync_ShouldReturnConcepts()
    {
        // Arrange
        var concepts = new List<Concept>
        {
            TestDataBuilder.CreateConcept(1, "Concept 1"),
            TestDataBuilder.CreateConcept(1, "Concept 2")
        };

        _mockConceptRepository
            .Setup(r => r.GetByOntologyIdAsync(1))
            .ReturnsAsync(concepts);

        // Act
        var result = await _service.GetByOntologyIdAsync(1);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.Equal(1, c.OntologyId));
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnMatchingConcepts()
    {
        // Arrange
        var concepts = new List<Concept>
        {
            TestDataBuilder.CreateConcept(1, "Person"),
            TestDataBuilder.CreateConcept(1, "Personal Data")
        };

        _mockConceptRepository
            .Setup(r => r.SearchAsync("Person"))
            .ReturnsAsync(concepts);

        // Act
        var result = await _service.SearchAsync("Person");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.Contains("Person", c.Name));
    }

}
