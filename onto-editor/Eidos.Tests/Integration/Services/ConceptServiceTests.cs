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
            _mockContextFactory.Object
        );
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task CreateAsync_WithRecordUndo_ShouldUseCommandFactory()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        var mockCommand = new Mock<ICommand>();
        _mockCommandFactory
            .Setup(f => f.CreateConceptCommand(concept))
            .Returns(mockCommand.Object);

        // Act
        var result = await _service.CreateAsync(concept, recordUndo: true);

        // Assert - Verify command was created (CommandInvoker.ExecuteAsync can't be mocked)
        _mockCommandFactory.Verify(f => f.CreateConceptCommand(concept), Times.Once);
        Assert.Equal(concept, result);
    }

    [Fact]
    public async Task CreateAsync_WithoutRecordUndo_ShouldAddDirectly()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        _mockConceptRepository
            .Setup(r => r.AddAsync(concept))
            .ReturnsAsync(concept);

        // Act
        var result = await _service.CreateAsync(concept, recordUndo: false);

        // Assert
        _mockConceptRepository.Verify(r => r.AddAsync(concept), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(concept.OntologyId), Times.Once);
        Assert.Equal(concept, result);
    }

    [Fact]
    public async Task CreateAsync_WithoutPermission_ShouldThrowUnauthorized()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                concept.OntologyId,
                _testUser.Id,
                null,
                PermissionLevel.ViewAndAdd))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateAsync(concept, recordUndo: false));
    }

    [Fact]
    public async Task UpdateAsync_WithRecordUndo_ShouldUseCommandFactory()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Updated Concept");
        var mockCommand = new Mock<ICommand>();
        _mockCommandFactory
            .Setup(f => f.UpdateConceptCommand(concept))
            .Returns(mockCommand.Object);

        // Act
        var result = await _service.UpdateAsync(concept, recordUndo: true);

        // Assert - Verify command was created (CommandInvoker.ExecuteAsync can't be mocked)
        _mockCommandFactory.Verify(f => f.UpdateConceptCommand(concept), Times.Once);
        Assert.Equal(concept, result);
    }

    [Fact]
    public async Task UpdateAsync_WithoutRecordUndo_ShouldUpdateDirectly()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Updated Concept");

        // Act
        var result = await _service.UpdateAsync(concept, recordUndo: false);

        // Assert
        _mockConceptRepository.Verify(r => r.UpdateAsync(concept), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(concept.OntologyId), Times.Once);
        Assert.Equal(concept, result);
    }

    [Fact]
    public async Task UpdateAsync_WithoutPermission_ShouldThrowUnauthorized()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                concept.OntologyId,
                _testUser.Id,
                null,
                PermissionLevel.ViewAddEdit))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateAsync(concept, recordUndo: false));
    }

    [Fact]
    public async Task DeleteAsync_WithRecordUndo_ShouldUseCommandFactory()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        concept.Id = 123;
        var mockCommand = new Mock<ICommand>();

        _mockConceptRepository
            .Setup(r => r.GetByIdAsync(123))
            .ReturnsAsync(concept);
        _mockCommandFactory
            .Setup(f => f.DeleteConceptCommand(123))
            .Returns(mockCommand.Object);

        // Act
        await _service.DeleteAsync(123, recordUndo: true);

        // Assert - Verify command was created (CommandInvoker.ExecuteAsync can't be mocked)
        _mockCommandFactory.Verify(f => f.DeleteConceptCommand(123), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithoutRecordUndo_ShouldDeleteDirectly()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        concept.Id = 123;

        _mockConceptRepository
            .Setup(r => r.GetByIdAsync(123))
            .ReturnsAsync(concept);

        // Act
        await _service.DeleteAsync(123, recordUndo: false);

        // Assert
        _mockConceptRepository.Verify(r => r.DeleteAsync(123), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(concept.OntologyId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithoutPermission_ShouldThrowUnauthorized()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        concept.Id = 123;

        _mockConceptRepository
            .Setup(r => r.GetByIdAsync(123))
            .ReturnsAsync(concept);
        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                concept.OntologyId,
                _testUser.Id,
                null,
                PermissionLevel.ViewAddEdit))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.DeleteAsync(123, recordUndo: false));
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

    [Fact]
    public async Task CreateAsync_ShouldBroadcastViaSignalR()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        _mockConceptRepository
            .Setup(r => r.AddAsync(concept))
            .ReturnsAsync(concept);

        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group("ontology_1")).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        // Act
        await _service.CreateAsync(concept, recordUndo: false);

        // Assert
        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "ConceptChanged",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldBroadcastViaSignalR()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Updated Concept");

        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group("ontology_1")).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        // Act
        await _service.UpdateAsync(concept, recordUndo: false);

        // Assert
        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "ConceptChanged",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldBroadcastViaSignalR()
    {
        // Arrange
        var concept = TestDataBuilder.CreateConcept(1, "Test Concept");
        concept.Id = 123;

        _mockConceptRepository
            .Setup(r => r.GetByIdAsync(123))
            .ReturnsAsync(concept);

        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group("ontology_1")).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        // Act
        await _service.DeleteAsync(123, recordUndo: false);

        // Assert
        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "ConceptChanged",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }
}
