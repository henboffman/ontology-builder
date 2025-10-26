using Bunit;
using Eidos.Components.Shared;
using Eidos.Models;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Eidos.Tests.Components;

/// <summary>
/// Component tests for OntologyLineage using bUnit
/// </summary>
public class OntologyLineageTests : TestContext
{
    private readonly Mock<IOntologyService> _mockOntologyService;
    private readonly Mock<NavigationManager> _mockNavigationManager;

    public OntologyLineageTests()
    {
        _mockOntologyService = new Mock<IOntologyService>();
        _mockNavigationManager = new Mock<NavigationManager>();

        Services.AddSingleton(_mockOntologyService.Object);
        Services.AddSingleton(_mockNavigationManager.Object);
    }

    [Fact]
    public async Task Show_ShouldDisplayModal_WithOntologyName()
    {
        // Arrange
        var testOntology = TestDataBuilder.CreateOntology(name: "Test Ontology");
        testOntology.Id = 1;

        _mockOntologyService
            .Setup(s => s.GetOntologyLineageAsync(1))
            .ReturnsAsync(new List<Ontology> { testOntology });

        _mockOntologyService
            .Setup(s => s.GetOntologyDescendantsAsync(1))
            .ReturnsAsync(new List<Ontology>());

        var cut = RenderComponent<OntologyLineage>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Show(testOntology));

        // Assert
        Assert.Contains("Ontology Lineage: Test Ontology", cut.Markup);
    }

    [Fact]
    public async Task Show_ShouldDisplayLoadingIndicator_Initially()
    {
        // Arrange
        var testOntology = TestDataBuilder.CreateOntology(name: "Test Ontology");
        testOntology.Id = 1;

        var tcs = new TaskCompletionSource<List<Ontology>>();
        _mockOntologyService
            .Setup(s => s.GetOntologyLineageAsync(1))
            .Returns(tcs.Task);

        _mockOntologyService
            .Setup(s => s.GetOntologyDescendantsAsync(1))
            .ReturnsAsync(new List<Ontology>());

        var cut = RenderComponent<OntologyLineage>();

        // Act
        var showTask = cut.InvokeAsync(() => cut.Instance.Show(testOntology));

        // Assert - should show loading
        Assert.Contains("Loading ontology lineage", cut.Markup);

        // Complete the task
        tcs.SetResult(new List<Ontology> { testOntology });
        await showTask;
    }

    [Fact]
    public async Task Show_ShouldDisplayAncestryChain_WithMultipleGenerations()
    {
        // Arrange
        var original = TestDataBuilder.CreateOntology(name: "Original", provenanceType: null);
        original.Id = 1;

        var clone = TestDataBuilder.CreateOntology(name: "Clone", provenanceType: "clone", parentOntologyId: 1);
        clone.Id = 2;
        clone.ParentOntologyId = 1;

        var fork = TestDataBuilder.CreateOntology(name: "Fork", provenanceType: "fork", parentOntologyId: 2);
        fork.Id = 3;
        fork.ParentOntologyId = 2;

        var lineage = new List<Ontology> { fork, clone, original };

        _mockOntologyService
            .Setup(s => s.GetOntologyLineageAsync(3))
            .ReturnsAsync(lineage);

        _mockOntologyService
            .Setup(s => s.GetOntologyDescendantsAsync(3))
            .ReturnsAsync(new List<Ontology>());

        var cut = RenderComponent<OntologyLineage>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Show(fork));

        // Assert
        Assert.Contains("Fork", cut.Markup);
        Assert.Contains("Clone", cut.Markup);
        Assert.Contains("Original", cut.Markup);
        Assert.Contains("Ancestry", cut.Markup);
    }

    [Fact]
    public async Task Show_ShouldDisplayDescendants_WhenTheyExist()
    {
        // Arrange
        var original = TestDataBuilder.CreateOntology(name: "Original");
        original.Id = 1;

        var child1 = TestDataBuilder.CreateOntology(name: "Child 1", provenanceType: "fork");
        child1.Id = 2;

        var child2 = TestDataBuilder.CreateOntology(name: "Child 2", provenanceType: "clone");
        child2.Id = 3;

        _mockOntologyService
            .Setup(s => s.GetOntologyLineageAsync(1))
            .ReturnsAsync(new List<Ontology> { original });

        _mockOntologyService
            .Setup(s => s.GetOntologyDescendantsAsync(1))
            .ReturnsAsync(new List<Ontology> { child1, child2 });

        var cut = RenderComponent<OntologyLineage>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Show(original));

        // Assert
        Assert.Contains("Descendants (2)", cut.Markup);
        Assert.Contains("Child 1", cut.Markup);
        Assert.Contains("Child 2", cut.Markup);
        Assert.Contains("fork", cut.Markup);
        Assert.Contains("clone", cut.Markup);
    }

    [Fact]
    public async Task Show_ShouldDisplayNoDescendantsMessage_WhenNoneExist()
    {
        // Arrange
        var testOntology = TestDataBuilder.CreateOntology(name: "Lonely Ontology");
        testOntology.Id = 1;

        _mockOntologyService
            .Setup(s => s.GetOntologyLineageAsync(1))
            .ReturnsAsync(new List<Ontology> { testOntology });

        _mockOntologyService
            .Setup(s => s.GetOntologyDescendantsAsync(1))
            .ReturnsAsync(new List<Ontology>());

        var cut = RenderComponent<OntologyLineage>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Show(testOntology));

        // Assert
        Assert.Contains("No ontologies have been forked or cloned from this one yet", cut.Markup);
    }

    [Fact]
    public async Task Show_ShouldDisplayOriginalOntologyMessage_WhenNoParent()
    {
        // Arrange
        var testOntology = TestDataBuilder.CreateOntology(name: "Original Ontology");
        testOntology.Id = 1;

        _mockOntologyService
            .Setup(s => s.GetOntologyLineageAsync(1))
            .ReturnsAsync(new List<Ontology> { testOntology });

        _mockOntologyService
            .Setup(s => s.GetOntologyDescendantsAsync(1))
            .ReturnsAsync(new List<Ontology>());

        var cut = RenderComponent<OntologyLineage>();

        // Act
        await cut.InvokeAsync(() => cut.Instance.Show(testOntology));

        // Assert
        Assert.Contains("This is an original ontology with no parent lineage", cut.Markup);
    }

    [Fact]
    public async Task Hide_ShouldCloseModal()
    {
        // Arrange
        var testOntology = TestDataBuilder.CreateOntology(name: "Test Ontology");
        testOntology.Id = 1;

        _mockOntologyService
            .Setup(s => s.GetOntologyLineageAsync(1))
            .ReturnsAsync(new List<Ontology> { testOntology });

        _mockOntologyService
            .Setup(s => s.GetOntologyDescendantsAsync(1))
            .ReturnsAsync(new List<Ontology>());

        var cut = RenderComponent<OntologyLineage>();
        await cut.InvokeAsync(() => cut.Instance.Show(testOntology));

        // Act
        await cut.InvokeAsync(() => cut.Instance.Hide());

        // Assert
        Assert.DoesNotContain("Ontology Lineage", cut.Markup);
    }
}
