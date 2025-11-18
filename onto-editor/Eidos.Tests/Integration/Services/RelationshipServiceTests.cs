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
    private readonly Mock<IOntologyActivityService> _mockActivityService;
    private readonly OntologyPermissionService _permissionService;
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
        _mockActivityService = new Mock<IOntologyActivityService>();

        var mockContextFactory = new Mock<IDbContextFactory<OntologyDbContext>>();
        _permissionService = new OntologyPermissionService(mockContextFactory.Object);

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
            _mockShareService.Object,
            _mockActivityService.Object,
            _permissionService
        );
    }

    public void Dispose()
    {
        // Cleanup if needed
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

}
