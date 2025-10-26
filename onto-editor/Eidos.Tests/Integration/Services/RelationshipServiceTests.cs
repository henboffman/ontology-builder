using Eidos.Data.Repositories;
using Eidos.Hubs;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services;
using Eidos.Services.Commands;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Eidos.Tests.Integration.Services;

/// <summary>
/// Integration tests for RelationshipService
/// </summary>
public class RelationshipServiceTests : IDisposable
{
    private readonly Mock<IRelationshipRepository> _mockRelationshipRepository;
    private readonly Mock<IOntologyRepository> _mockOntologyRepository;
    private readonly Mock<ICommandFactory> _mockCommandFactory;
    private readonly Mock<CommandInvoker> _mockCommandInvoker;
    private readonly Mock<IHubContext<OntologyHub>> _mockHubContext;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IOntologyShareService> _mockShareService;
    private readonly RelationshipService _service;
    private readonly ApplicationUser _testUser;

    public RelationshipServiceTests()
    {
        _mockRelationshipRepository = new Mock<IRelationshipRepository>();
        _mockOntologyRepository = new Mock<IOntologyRepository>();
        _mockCommandFactory = new Mock<ICommandFactory>();
        _mockCommandInvoker = new Mock<CommandInvoker>();
        _mockHubContext = new Mock<IHubContext<OntologyHub>>();
        _mockUserService = new Mock<IUserService>();
        _mockShareService = new Mock<IOntologyShareService>();

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

        // Setup SignalR hub context
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        _service = new RelationshipService(
            _mockRelationshipRepository.Object,
            _mockOntologyRepository.Object,
            _mockCommandFactory.Object,
            _mockCommandInvoker.Object,
            _mockHubContext.Object,
            _mockUserService.Object,
            _mockShareService.Object
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
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);
        var mockCommand = new Mock<ICommand>();
        _mockCommandFactory
            .Setup(f => f.CreateRelationshipCommand(relationship))
            .Returns(mockCommand.Object);

        // Act
        var result = await _service.CreateAsync(relationship, recordUndo: true);

        // Assert
        _mockCommandFactory.Verify(f => f.CreateRelationshipCommand(relationship), Times.Once);
        Assert.Equal(relationship, result);
    }

    [Fact]
    public async Task CreateAsync_WithoutRecordUndo_ShouldAddDirectly()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);
        _mockRelationshipRepository
            .Setup(r => r.AddAsync(relationship))
            .ReturnsAsync(relationship);

        // Act
        var result = await _service.CreateAsync(relationship, recordUndo: false);

        // Assert
        _mockRelationshipRepository.Verify(r => r.AddAsync(relationship), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(relationship.OntologyId), Times.Once);
        Assert.Equal(relationship, result);
    }

    [Fact]
    public async Task CreateAsync_WithoutPermission_ShouldThrowUnauthorized()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);
        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                relationship.OntologyId,
                _testUser.Id,
                null,
                PermissionLevel.ViewAndAdd))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateAsync(relationship, recordUndo: false));
    }

    [Fact]
    public async Task UpdateAsync_WithRecordUndo_ShouldUseCommandFactory()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3, "updated-type");
        var mockCommand = new Mock<ICommand>();
        _mockCommandFactory
            .Setup(f => f.UpdateRelationshipCommand(relationship))
            .Returns(mockCommand.Object);

        // Act
        var result = await _service.UpdateAsync(relationship, recordUndo: true);

        // Assert
        _mockCommandFactory.Verify(f => f.UpdateRelationshipCommand(relationship), Times.Once);
        Assert.Equal(relationship, result);
    }

    [Fact]
    public async Task UpdateAsync_WithoutRecordUndo_ShouldUpdateDirectly()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);

        // Act
        var result = await _service.UpdateAsync(relationship, recordUndo: false);

        // Assert
        _mockRelationshipRepository.Verify(r => r.UpdateAsync(relationship), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(relationship.OntologyId), Times.Once);
        Assert.Equal(relationship, result);
    }

    [Fact]
    public async Task UpdateAsync_WithoutPermission_ShouldThrowUnauthorized()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);
        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                relationship.OntologyId,
                _testUser.Id,
                null,
                PermissionLevel.ViewAddEdit))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateAsync(relationship, recordUndo: false));
    }

    [Fact]
    public async Task DeleteAsync_WithRecordUndo_ShouldUseCommandFactory()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);
        relationship.Id = 123;
        var mockCommand = new Mock<ICommand>();

        _mockRelationshipRepository
            .Setup(r => r.GetByIdAsync(123))
            .ReturnsAsync(relationship);
        _mockCommandFactory
            .Setup(f => f.DeleteRelationshipCommand(123))
            .Returns(mockCommand.Object);

        // Act
        await _service.DeleteAsync(123, recordUndo: true);

        // Assert
        _mockCommandFactory.Verify(f => f.DeleteRelationshipCommand(123), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithoutRecordUndo_ShouldDeleteDirectly()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);
        relationship.Id = 123;

        _mockRelationshipRepository
            .Setup(r => r.GetByIdAsync(123))
            .ReturnsAsync(relationship);

        // Act
        await _service.DeleteAsync(123, recordUndo: false);

        // Assert
        _mockRelationshipRepository.Verify(r => r.DeleteAsync(123), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(relationship.OntologyId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithoutPermission_ShouldThrowUnauthorized()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);
        relationship.Id = 123;

        _mockRelationshipRepository
            .Setup(r => r.GetByIdAsync(123))
            .ReturnsAsync(relationship);
        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                relationship.OntologyId,
                _testUser.Id,
                null,
                PermissionLevel.ViewAddEdit))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.DeleteAsync(123, recordUndo: false));
    }

    [Fact]
    public async Task DeleteAsync_WhenRelationshipNotFound_ShouldNotThrow()
    {
        // Arrange
        _mockRelationshipRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Relationship?)null);

        // Act & Assert - should not throw
        await _service.DeleteAsync(999, recordUndo: false);

        // Verify delete was not called
        _mockRelationshipRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnRelationship()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);
        relationship.Id = 123;

        _mockRelationshipRepository
            .Setup(r => r.GetByIdAsync(123))
            .ReturnsAsync(relationship);

        // Act
        var result = await _service.GetByIdAsync(123);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        _mockRelationshipRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Relationship?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOntologyIdAsync_ShouldReturnRelationships()
    {
        // Arrange
        var relationships = new List<Relationship>
        {
            TestDataBuilder.CreateRelationship(1, 2, 3),
            TestDataBuilder.CreateRelationship(1, 3, 4)
        };

        _mockRelationshipRepository
            .Setup(r => r.GetByOntologyIdAsync(1))
            .ReturnsAsync(relationships);

        // Act
        var result = await _service.GetByOntologyIdAsync(1);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Equal(1, r.OntologyId));
    }

    [Fact]
    public async Task GetByConceptIdAsync_ShouldReturnRelationships()
    {
        // Arrange
        var relationships = new List<Relationship>
        {
            TestDataBuilder.CreateRelationship(1, 2, 3),
            TestDataBuilder.CreateRelationship(1, 2, 4)
        };

        _mockRelationshipRepository
            .Setup(r => r.GetByConceptIdAsync(2))
            .ReturnsAsync(relationships);

        // Act
        var result = await _service.GetByConceptIdAsync(2);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Equal(2, r.SourceConceptId));
    }

    [Fact]
    public async Task CanCreateAsync_WithSelfRelationship_ShouldReturnFalse()
    {
        // Act
        var result = await _service.CanCreateAsync(5, 5, "is-a");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanCreateAsync_WhenRelationshipExists_ShouldReturnFalse()
    {
        // Arrange
        _mockRelationshipRepository
            .Setup(r => r.ExistsAsync(2, 3, "is-a"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CanCreateAsync(2, 3, "is-a");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanCreateAsync_WhenRelationshipDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        _mockRelationshipRepository
            .Setup(r => r.ExistsAsync(2, 3, "is-a"))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CanCreateAsync(2, 3, "is-a");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldBroadcastViaSignalR()
    {
        // Arrange
        var relationship = TestDataBuilder.CreateRelationship(1, 2, 3);
        _mockRelationshipRepository
            .Setup(r => r.AddAsync(relationship))
            .ReturnsAsync(relationship);

        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group("ontology_1")).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        // Act
        await _service.CreateAsync(relationship, recordUndo: false);

        // Assert
        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "RelationshipChanged",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }
}
