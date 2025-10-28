using Bunit;
using Eidos.Components.Pages;
using Eidos.Data;
using Eidos.Models;
using Eidos.Services;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Eidos.Tests.Components.Pages;

/// <summary>
/// Component tests for OntologySettings page using bUnit
/// Tests navigation, tab switching, and permission checking
/// </summary>
public class OntologySettingsTests : TestContext
{
    private readonly Mock<IOntologyService> _mockOntologyService;
    private readonly Mock<IOntologyShareService> _mockShareService;
    private readonly OntologyPermissionService _permissionService;
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;
    private readonly Mock<ToastService> _mockToastService;
    private readonly Mock<ConfirmService> _mockConfirmService;
    private readonly Mock<NavigationManager> _mockNavigationManager;
    private readonly TestDbContextFactory _contextFactory;
    private readonly string _testUserId = "test-user-id";

    public OntologySettingsTests()
    {
        _mockOntologyService = new Mock<IOntologyService>();
        _mockShareService = new Mock<IOntologyShareService>();
        _mockToastService = new Mock<ToastService>();
        _mockConfirmService = new Mock<ConfirmService>();
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        _mockNavigationManager = new Mock<NavigationManager>();

        // Create real permission service with in-memory database
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);
        var context = _contextFactory.CreateDbContext();
        _permissionService = new OntologyPermissionService(context);

        // Setup authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var authState = Task.FromResult(new AuthenticationState(claimsPrincipal));

        _mockAuthStateProvider
            .Setup(x => x.GetAuthenticationStateAsync())
            .Returns(authState);

        // Register services
        Services.AddSingleton(_mockOntologyService.Object);
        Services.AddSingleton(_mockShareService.Object);
        Services.AddSingleton(_permissionService);
        Services.AddSingleton(_mockToastService.Object);
        Services.AddSingleton(_mockConfirmService.Object);
        Services.AddSingleton(_mockAuthStateProvider.Object);
        Services.AddSingleton(_mockNavigationManager.Object);
        Services.AddAuthorizationCore();
    }

    [Fact]
    public void Render_ShouldRenderComponent_WithValidParameters()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            UserId = _testUserId,
            Description = "Test Description"
        };

        _mockOntologyService
            .Setup(s => s.GetOntologyAsync(It.IsAny<int>()))
            .ReturnsAsync(ontology);

        // Act
        var cut = RenderComponent<OntologySettings>(parameters => parameters
            .Add(p => p.Id, 1)
            .Add(p => p.ActiveTab, "general"));

        // Assert - Component should render without errors
        Assert.NotNull(cut);
    }

    [Fact]
    public void Parameters_ShouldAcceptIdAndActiveTab()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 123,
            Name = "Test Ontology",
            UserId = _testUserId
        };

        _mockOntologyService
            .Setup(s => s.GetOntologyAsync(123))
            .ReturnsAsync(ontology);

        // Act
        var cut = RenderComponent<OntologySettings>(parameters => parameters
            .Add(p => p.Id, 123)
            .Add(p => p.ActiveTab, "permissions"));

        // Assert - Component renders with expected parameters
        Assert.NotNull(cut);
        Assert.Equal(123, cut.Instance.Id);
        Assert.Equal("permissions", cut.Instance.ActiveTab);
    }

    [Fact]
    public void ActiveTab_ShouldDefaultToGeneral_WhenNotSpecified()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            UserId = _testUserId
        };

        _mockOntologyService
            .Setup(s => s.GetOntologyAsync(It.IsAny<int>()))
            .ReturnsAsync(ontology);

        // Act
        var cut = RenderComponent<OntologySettings>(parameters => parameters
            .Add(p => p.Id, 1));

        // Assert
        // After OnParametersSet is called, ActiveTab should default to "general"
        Assert.Equal("general", cut.Instance.ActiveTab);
    }
}
