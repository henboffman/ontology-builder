using Bunit;
using Eidos.Components.Shared;
using Eidos.Models;
using Eidos.Services;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Eidos.Tests.Components;

/// <summary>
/// Component tests for ForkCloneDialog using bUnit
/// </summary>
public class ForkCloneDialogTests : TestContext
{
    private readonly Mock<IOntologyService> _mockOntologyService;
    private readonly Mock<NavigationManager> _mockNavigationManager;
    private readonly Mock<ToastService> _mockToastService;

    public ForkCloneDialogTests()
    {
        _mockOntologyService = new Mock<IOntologyService>();
        _mockNavigationManager = new Mock<NavigationManager>();
        _mockToastService = new Mock<ToastService>();

        // Register services
        Services.AddSingleton(_mockOntologyService.Object);
        Services.AddSingleton(_mockNavigationManager.Object);
        Services.AddSingleton(_mockToastService.Object);
    }

    [Fact]
    public void ShowFork_ShouldDisplayModal_WithForkContent()
    {
        // Arrange
        var testOntology = TestDataBuilder.CreateOntology(name: "Test Ontology");
        var cut = RenderComponent<ForkCloneDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowFork(testOntology));

        // Assert
        Assert.Contains("Fork Ontology", cut.Markup);
        Assert.Contains("Test Ontology", cut.Markup);
        Assert.Contains("Creates a new ontology based on this one", cut.Markup);
    }

    [Fact]
    public void ShowClone_ShouldDisplayModal_WithCloneContent()
    {
        // Arrange
        var testOntology = TestDataBuilder.CreateOntology(name: "Test Ontology");
        var cut = RenderComponent<ForkCloneDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowClone(testOntology));

        // Assert
        Assert.Contains("Clone Ontology", cut.Markup);
        Assert.Contains("Test Ontology", cut.Markup);
        Assert.Contains("Creates an exact copy for experimentation", cut.Markup);
    }

    [Fact]
    public void ForkDialog_ShouldHaveDisabledButton_WhenNameIsEmpty()
    {
        // Arrange
        var testOntology = TestDataBuilder.CreateOntology(name: "Test Ontology");
        var cut = RenderComponent<ForkCloneDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowFork(testOntology));

        // Assert
        var button = cut.Find("button.btn-primary");
        Assert.NotNull(button.GetAttribute("disabled"));
    }

    [Fact]
    public void Hide_ShouldHideModal()
    {
        // Arrange
        var testOntology = TestDataBuilder.CreateOntology(name: "Test Ontology");
        var cut = RenderComponent<ForkCloneDialog>();
        cut.InvokeAsync(() => cut.Instance.ShowFork(testOntology));

        // Act
        cut.InvokeAsync(() => cut.Instance.Hide());

        // Assert
        Assert.DoesNotContain("Fork Ontology", cut.Markup);
    }

    [Fact]
    public async Task ExecuteFork_ShouldCallOntologyService_AndNavigate()
    {
        // Arrange
        var testOntology = TestDataBuilder.CreateOntology(name: "Test Ontology");
        testOntology.Id = 1; // Set ID for the test

        var forkedOntology = TestDataBuilder.CreateOntology(name: "Forked Ontology", provenanceType: "fork");
        forkedOntology.Id = 2;

        _mockOntologyService
            .Setup(s => s.ForkOntologyAsync(1, "Forked Ontology", null))
            .ReturnsAsync(forkedOntology);

        var cut = RenderComponent<ForkCloneDialog>();
        await cut.InvokeAsync(() => cut.Instance.ShowFork(testOntology));

        // Act - Set the name input
        var input = cut.Find("input[type='text']");
        input.Input("Forked Ontology");
        await cut.InvokeAsync(() => Task.CompletedTask); // Allow state to update

        // Trigger the fork button click
        var button = cut.Find("button.btn-primary");
        await button.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _mockOntologyService.Verify(
            s => s.ForkOntologyAsync(1, "Forked Ontology", null),
            Times.Once
        );
    }
}
