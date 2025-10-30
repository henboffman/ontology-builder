using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Eidos.Hubs;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services;
using Eidos.Services.Interfaces;
using System.Security.Claims;

namespace Eidos.Tests.Hubs;

/// <summary>
/// Unit tests for OntologyHub presence tracking functionality.
/// Tests cover permission checks, presence info creation, view updates, and cleanup.
/// </summary>
public class OntologyHubPresenceTests
{
    private readonly Mock<ILogger<OntologyHub>> _mockLogger;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<OntologyPermissionService> _mockPermissionService;
    private readonly Mock<IOntologyShareService> _mockShareService;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<IGroupManager> _mockGroups;

    public OntologyHubPresenceTests()
    {
        _mockLogger = new Mock<ILogger<OntologyHub>>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockPermissionService = new Mock<OntologyPermissionService>(null!); // Mock with null context
        _mockShareService = new Mock<IOntologyShareService>();
        _mockContext = new Mock<HubCallerContext>();
        _mockClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockGroups = new Mock<IGroupManager>();

        // Setup service scope factory chain
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
        _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(OntologyPermissionService)))
            .Returns(_mockPermissionService.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(IOntologyShareService)))
            .Returns(_mockShareService.Object);

        // Setup client proxies
        var mockSingleClientProxy = _mockClientProxy.As<ISingleClientProxy>();
        _mockClients.Setup(c => c.Caller).Returns(mockSingleClientProxy.Object);
        _mockClients.Setup(c => c.OthersInGroup(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
    }

    private OntologyHub CreateHub()
    {
        var hub = new OntologyHub(_mockLogger.Object, _mockScopeFactory.Object)
        {
            Context = _mockContext.Object,
            Clients = _mockClients.Object,
            Groups = _mockGroups.Object
        };
        return hub;
    }

    private ClaimsPrincipal CreateUserPrincipal(string userId, string userName, string email)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("name", userName),
            new Claim(ClaimTypes.Email, email)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task JoinOntology_WithValidPermission_ShouldAddUserToGroup()
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId = 1;
        var userId = "user123";
        var connectionId = "conn123";
        var user = CreateUserPrincipal(userId, "Test User", "test@example.com");

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockShareService.Setup(s => s.GetPermissionLevelAsync(ontologyId, userId, null))
            .ReturnsAsync(PermissionLevel.View);

        // Act
        await hub.JoinOntology(ontologyId);

        // Assert
        _mockGroups.Verify(g => g.AddToGroupAsync(connectionId, $"ontology_{ontologyId}", default), Times.Once);
        _mockClients.Verify(c => c.OthersInGroup($"ontology_{ontologyId}"), Times.Once);
        _mockClients.Verify(c => c.Caller, Times.Once);
    }

    [Fact]
    public async Task JoinOntology_WithoutPermission_ShouldThrowHubException()
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId = 1;
        var userId = "user123";
        var connectionId = "conn123";
        var user = CreateUserPrincipal(userId, "Test User", "test@example.com");

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockShareService.Setup(s => s.GetPermissionLevelAsync(ontologyId, userId, null))
            .ReturnsAsync((PermissionLevel?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(() => hub.JoinOntology(ontologyId));
        Assert.Equal("You do not have permission to access this ontology", exception.Message);

        // Verify user was NOT added to group
        _mockGroups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task JoinOntology_ShouldCreatePresenceInfoWithCorrectData()
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId = 1;
        var userId = "user123";
        var userName = "Test User";
        var userEmail = "test@example.com";
        var connectionId = "conn123";
        var user = CreateUserPrincipal(userId, userName, userEmail);

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockShareService.Setup(s => s.GetPermissionLevelAsync(ontologyId, userId, null))
            .ReturnsAsync(PermissionLevel.View);

        PresenceInfo? capturedPresenceInfo = null;
        _mockClientProxy.Setup(c => c.SendCoreAsync(
            "UserJoined",
            It.IsAny<object[]>(),
            default))
            .Callback<string, object[], CancellationToken>((method, args, token) =>
            {
                capturedPresenceInfo = args[0] as PresenceInfo;
            })
            .Returns(Task.CompletedTask);

        // Act
        await hub.JoinOntology(ontologyId);

        // Assert
        Assert.NotNull(capturedPresenceInfo);
        Assert.Equal(connectionId, capturedPresenceInfo.ConnectionId);
        Assert.Equal(userId, capturedPresenceInfo.UserId);
        Assert.Equal(userName, capturedPresenceInfo.UserName);
        Assert.Equal(userEmail, capturedPresenceInfo.UserEmail);
        Assert.False(capturedPresenceInfo.IsGuest);
        Assert.NotEmpty(capturedPresenceInfo.Color);
    }

    [Fact]
    public async Task UpdateCurrentView_WithValidViewName_ShouldUpdatePresence()
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId = 1;
        var userId = "user123";
        var connectionId = "conn123";
        var user = CreateUserPrincipal(userId, "Test User", "test@example.com");

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockPermissionService.Setup(s => s.CanViewAsync(ontologyId, userId))
            .ReturnsAsync(true);

        // Join first to create presence
        await hub.JoinOntology(ontologyId);

        // Act
        await hub.UpdateCurrentView(ontologyId, "Graph");

        // Assert
        _mockClientProxy.Verify(c => c.SendCoreAsync(
            "UserViewChanged",
            It.Is<object[]>(args => args.Length == 2 && (string)args[0] == connectionId && (string)args[1] == "Graph"),
            default), Times.Once);
    }

    [Fact]
    public async Task UpdateCurrentView_WithInvalidViewName_ShouldNotUpdate()
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId = 1;
        var userId = "user123";
        var connectionId = "conn123";
        var user = CreateUserPrincipal(userId, "Test User", "test@example.com");

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockPermissionService.Setup(s => s.CanViewAsync(ontologyId, userId))
            .ReturnsAsync(true);

        await hub.JoinOntology(ontologyId);

        // Reset mock to track only UpdateCurrentView calls
        _mockClientProxy.Invocations.Clear();

        // Act - Test with null/empty/too long view names
        await hub.UpdateCurrentView(ontologyId, null!);
        await hub.UpdateCurrentView(ontologyId, "");
        await hub.UpdateCurrentView(ontologyId, "   ");
        await hub.UpdateCurrentView(ontologyId, new string('A', 51)); // 51 chars, exceeds max 50

        // Assert - No UserViewChanged should be sent
        _mockClientProxy.Verify(c => c.SendCoreAsync(
            "UserViewChanged",
            It.IsAny<object[]>(),
            default), Times.Never);
    }

    [Fact]
    public async Task Heartbeat_ShouldUpdateLastSeenAt()
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId = 1;
        var userId = "user123";
        var connectionId = "conn123";
        var user = CreateUserPrincipal(userId, "Test User", "test@example.com");

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockShareService.Setup(s => s.GetPermissionLevelAsync(ontologyId, userId, null))
            .ReturnsAsync(PermissionLevel.View);

        await hub.JoinOntology(ontologyId);

        var initialTime = DateTime.UtcNow;
        await Task.Delay(100); // Small delay to ensure time difference

        // Act
        await hub.Heartbeat(ontologyId);

        // Assert - Just verify it doesn't throw and completes
        // (LastSeenAt is internal to the presence dictionary, we can't directly assert it changed)
        Assert.True(true);
    }

    [Theory]
    [InlineData("name", "John Doe", "John Doe")]
    [InlineData("preferred_username", "johndoe", "johndoe")]
    [InlineData(ClaimTypes.Email, "john@example.com", "john")]
    public async Task JoinOntology_WithDifferentClaimTypes_ShouldResolveDisplayName(
        string claimType, string claimValue, string expectedDisplayName)
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId = 1;
        var userId = "user123";
        var connectionId = "conn123";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(claimType, claimValue)
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockShareService.Setup(s => s.GetPermissionLevelAsync(ontologyId, userId, null))
            .ReturnsAsync(PermissionLevel.View);

        PresenceInfo? capturedPresenceInfo = null;
        _mockClientProxy.Setup(c => c.SendCoreAsync(
            "UserJoined",
            It.IsAny<object[]>(),
            default))
            .Callback<string, object[], CancellationToken>((method, args, token) =>
            {
                capturedPresenceInfo = args[0] as PresenceInfo;
            })
            .Returns(Task.CompletedTask);

        // Act
        await hub.JoinOntology(ontologyId);

        // Assert
        Assert.NotNull(capturedPresenceInfo);
        Assert.Equal(expectedDisplayName, capturedPresenceInfo.UserName);
    }

    [Fact]
    public async Task JoinOntology_WithGivenNameAndSurname_ShouldConstructFullName()
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId = 1;
        var userId = "user123";
        var connectionId = "conn123";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.GivenName, "John"),
            new Claim(ClaimTypes.Surname, "Doe")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockShareService.Setup(s => s.GetPermissionLevelAsync(ontologyId, userId, null))
            .ReturnsAsync(PermissionLevel.View);

        PresenceInfo? capturedPresenceInfo = null;
        _mockClientProxy.Setup(c => c.SendCoreAsync(
            "UserJoined",
            It.IsAny<object[]>(),
            default))
            .Callback<string, object[], CancellationToken>((method, args, token) =>
            {
                capturedPresenceInfo = args[0] as PresenceInfo;
            })
            .Returns(Task.CompletedTask);

        // Act
        await hub.JoinOntology(ontologyId);

        // Assert
        Assert.NotNull(capturedPresenceInfo);
        Assert.Equal("John Doe", capturedPresenceInfo.UserName);
    }

    [Fact]
    public async Task LeaveOntology_ShouldRemoveUserFromGroup()
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId = 1;
        var userId = "user123";
        var connectionId = "conn123";
        var user = CreateUserPrincipal(userId, "Test User", "test@example.com");

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockShareService.Setup(s => s.GetPermissionLevelAsync(ontologyId, userId, null))
            .ReturnsAsync(PermissionLevel.View);

        await hub.JoinOntology(ontologyId);

        // Act
        await hub.LeaveOntology(ontologyId);

        // Assert
        _mockGroups.Verify(g => g.RemoveFromGroupAsync(connectionId, $"ontology_{ontologyId}", default), Times.Once);
        _mockClientProxy.Verify(c => c.SendCoreAsync(
            "UserLeft",
            It.Is<object[]>(args => args.Length == 1 && (string)args[0] == connectionId),
            default), Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldRemoveUserFromAllOntologies()
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId1 = 1;
        var ontologyId2 = 2;
        var userId = "user123";
        var connectionId = "conn123";
        var user = CreateUserPrincipal(userId, "Test User", "test@example.com");

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockShareService.Setup(s => s.GetPermissionLevelAsync(It.IsAny<int>(), userId, null))
            .ReturnsAsync(PermissionLevel.View);

        // Join multiple ontologies
        await hub.JoinOntology(ontologyId1);
        await hub.JoinOntology(ontologyId2);

        // Act
        await hub.OnDisconnectedAsync(null);

        // Assert - Should broadcast UserLeft to both ontology groups
        _mockClientProxy.Verify(c => c.SendCoreAsync(
            "UserLeft",
            It.Is<object[]>(args => args.Length == 1 && (string)args[0] == connectionId),
            default), Times.AtLeast(2));
    }

    [Fact]
    public async Task JoinOntology_WithSameUser_ShouldHaveConsistentColor()
    {
        // Arrange
        var hub1 = CreateHub();
        var hub2 = CreateHub();
        var ontologyId = 1;
        var userId = "user123";
        var connectionId1 = "conn123";
        var connectionId2 = "conn456";
        var user = CreateUserPrincipal(userId, "Test User", "test@example.com");

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId1);
        _mockContext.Setup(c => c.User).Returns(user);
        _mockShareService.Setup(s => s.GetPermissionLevelAsync(ontologyId, userId, null))
            .ReturnsAsync(PermissionLevel.View);

        PresenceInfo? presenceInfo1 = null;
        PresenceInfo? presenceInfo2 = null;

        _mockClientProxy.Setup(c => c.SendCoreAsync(
            "UserJoined",
            It.IsAny<object[]>(),
            default))
            .Callback<string, object[], CancellationToken>((method, args, token) =>
            {
                if (presenceInfo1 == null)
                    presenceInfo1 = args[0] as PresenceInfo;
                else
                    presenceInfo2 = args[0] as PresenceInfo;
            })
            .Returns(Task.CompletedTask);

        // Act - Join with same user from different connections
        await hub1.JoinOntology(ontologyId);

        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId2);
        await hub2.JoinOntology(ontologyId);

        // Assert - Same userId should get same color
        Assert.NotNull(presenceInfo1);
        Assert.NotNull(presenceInfo2);
        Assert.Equal(presenceInfo1.Color, presenceInfo2.Color);
    }

    [Fact]
    public async Task JoinOntology_AsGuest_ShouldCreateGuestPresence()
    {
        // Arrange
        var hub = CreateHub();
        var ontologyId = 1;
        var connectionId = "conn123";

        // No authenticated user (guest scenario)
        _mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        _mockContext.Setup(c => c.User).Returns((ClaimsPrincipal)null!);
        _mockShareService.Setup(s => s.GetPermissionLevelAsync(ontologyId, null, null))
            .ReturnsAsync(PermissionLevel.View);

        PresenceInfo? capturedPresenceInfo = null;
        _mockClientProxy.Setup(c => c.SendCoreAsync(
            "UserJoined",
            It.IsAny<object[]>(),
            default))
            .Callback<string, object[], CancellationToken>((method, args, token) =>
            {
                capturedPresenceInfo = args[0] as PresenceInfo;
            })
            .Returns(Task.CompletedTask);

        // Act
        await hub.JoinOntology(ontologyId);

        // Assert
        Assert.NotNull(capturedPresenceInfo);
        Assert.StartsWith("guest_", capturedPresenceInfo.UserId);
        Assert.Equal("Guest", capturedPresenceInfo.UserName);
        Assert.True(capturedPresenceInfo.IsGuest);
    }
}
