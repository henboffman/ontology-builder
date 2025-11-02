using Eidos.Models.ViewState;
using Xunit;
using static Eidos.Models.ViewState.GraphViewState;

namespace Eidos.Tests.Models.ViewState;

/// <summary>
/// Unit tests for the GraphViewState class
/// </summary>
public class GraphViewStateTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var state = new GraphViewState();

        // Assert
        Assert.Equal("ontology-graph", state.GraphElementId);
        Assert.False(state.IsGraphInitialized);
        Assert.Null(state.LastRefreshTime);
        Assert.Equal(GraphLayout.Hierarchical, state.CurrentLayout);
        Assert.False(state.ShowIndividuals);
        Assert.True(state.ShowLabels);
        Assert.True(state.ShowEdgeLabels);
        Assert.Equal(GraphColorMode.ByCategory, state.ColorMode);
        Assert.False(state.IsNodeDragging);
        Assert.Null(state.HoveredNodeId);
        Assert.Null(state.DraggedNodeId);
        Assert.False(state.IsPanning);
        Assert.Equal(1.0, state.ZoomLevel);
        Assert.Null(state.CategoryFilter);
        Assert.Empty(state.HiddenConceptIds);
        Assert.Empty(state.HiddenRelationshipTypes);
        Assert.Null(state.SearchText);
        Assert.Equal(0.1, state.MinZoom);
        Assert.Equal(3.0, state.MaxZoom);
        Assert.True(state.AnimateLayout);
        Assert.Equal(500, state.AnimationDuration);
    }

    [Fact]
    public void SetGraphInitialized_UpdatesProperty()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.SetGraphInitialized(true);

        // Assert
        Assert.True(state.IsGraphInitialized);
        Assert.NotNull(state.LastRefreshTime);
    }

    [Fact]
    public void SetGraphInitialized_TriggersStateChangedEvent()
    {
        // Arrange
        var state = new GraphViewState();
        var eventTriggered = false;
        state.OnGraphStateChanged += () => eventTriggered = true;

        // Act
        state.SetGraphInitialized(true);

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void SetLayout_UpdatesLayout()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.SetLayout(GraphLayout.ForceDirected);

        // Assert
        Assert.Equal(GraphLayout.ForceDirected, state.CurrentLayout);
    }

    [Fact]
    public void SetLayout_TriggersStateChangedEvent()
    {
        // Arrange
        var state = new GraphViewState();
        var eventTriggered = false;
        state.OnGraphStateChanged += () => eventTriggered = true;

        // Act
        state.SetLayout(GraphLayout.Circular);

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void ToggleIndividuals_TogglesShowIndividuals()
    {
        // Arrange
        var state = new GraphViewState();
        var initialValue = state.ShowIndividuals;

        // Act
        state.ToggleIndividuals();

        // Assert
        Assert.NotEqual(initialValue, state.ShowIndividuals);
    }

    [Fact]
    public void ToggleLabels_TogglesShowLabels()
    {
        // Arrange
        var state = new GraphViewState();
        var initialValue = state.ShowLabels;

        // Act
        state.ToggleLabels();

        // Assert
        Assert.NotEqual(initialValue, state.ShowLabels);
    }

    [Fact]
    public void ToggleEdgeLabels_TogglesShowEdgeLabels()
    {
        // Arrange
        var state = new GraphViewState();
        var initialValue = state.ShowEdgeLabels;

        // Act
        state.ToggleEdgeLabels();

        // Assert
        Assert.NotEqual(initialValue, state.ShowEdgeLabels);
    }

    [Fact]
    public void SetColorMode_UpdatesColorMode()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.SetColorMode(GraphColorMode.Monochrome);

        // Assert
        Assert.Equal(GraphColorMode.Monochrome, state.ColorMode);
    }

    [Fact]
    public void SetNodeDragging_UpdatesProperties()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.SetNodeDragging(true, "node-123");

        // Assert
        Assert.True(state.IsNodeDragging);
        Assert.Equal("node-123", state.DraggedNodeId);
    }

    [Fact]
    public void SetNodeDragging_False_ClearsDraggedNodeId()
    {
        // Arrange
        var state = new GraphViewState();
        state.SetNodeDragging(true, "node-123");

        // Act
        state.SetNodeDragging(false, null);

        // Assert
        Assert.False(state.IsNodeDragging);
        Assert.Null(state.DraggedNodeId);
    }

    [Fact]
    public void SetHoveredNode_UpdatesHoveredNodeId()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.SetHoveredNode("node-456");

        // Assert
        Assert.Equal("node-456", state.HoveredNodeId);
    }

    [Fact]
    public void SetPanning_UpdatesPanning()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.SetPanning(true);

        // Assert
        Assert.True(state.IsPanning);
    }

    [Fact]
    public void SetZoomLevel_UpdatesZoomLevel()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.SetZoomLevel(1.5);

        // Assert
        Assert.Equal(1.5, state.ZoomLevel);
    }

    [Fact]
    public void SetZoomLevel_ClampsToMinimum()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.SetZoomLevel(0.05); // Below MinZoom

        // Assert
        Assert.Equal(state.MinZoom, state.ZoomLevel);
    }

    [Fact]
    public void SetZoomLevel_ClampsToMaximum()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.SetZoomLevel(5.0); // Above MaxZoom

        // Assert
        Assert.Equal(state.MaxZoom, state.ZoomLevel);
    }

    [Fact]
    public void FilterByCategory_UpdatesCategoryFilter()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.FilterByCategory("TestCategory");

        // Assert
        Assert.Equal("TestCategory", state.CategoryFilter);
    }

    [Fact]
    public void FilterByCategory_Null_ClearsFilter()
    {
        // Arrange
        var state = new GraphViewState();
        state.FilterByCategory("TestCategory");

        // Act
        state.FilterByCategory(null);

        // Assert
        Assert.Null(state.CategoryFilter);
    }

    [Fact]
    public void SetSearchText_UpdatesSearchText()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.SetSearchText("test search");

        // Assert
        Assert.Equal("test search", state.SearchText);
    }

    [Fact]
    public void HideConcept_AddsToHiddenConcepts()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.HideConcept(1);

        // Assert
        Assert.Contains(1, state.HiddenConceptIds);
    }

    [Fact]
    public void ShowConcept_RemovesFromHiddenConcepts()
    {
        // Arrange
        var state = new GraphViewState();
        state.HideConcept(1);

        // Act
        state.ShowConcept(1);

        // Assert
        Assert.DoesNotContain(1, state.HiddenConceptIds);
    }

    [Fact]
    public void ToggleConceptVisibility_TogglesHiddenState()
    {
        // Arrange
        var state = new GraphViewState();

        // Act - Hide
        state.ToggleConceptVisibility(1);

        // Assert - Hidden
        Assert.Contains(1, state.HiddenConceptIds);

        // Act - Show
        state.ToggleConceptVisibility(1);

        // Assert - Visible
        Assert.DoesNotContain(1, state.HiddenConceptIds);
    }

    [Fact]
    public void IsConceptHidden_ReturnsTrueForHiddenConcept()
    {
        // Arrange
        var state = new GraphViewState();
        state.HideConcept(1);

        // Assert
        Assert.True(state.IsConceptHidden(1));
    }

    [Fact]
    public void IsConceptHidden_ReturnsFalseForVisibleConcept()
    {
        // Arrange
        var state = new GraphViewState();

        // Assert
        Assert.False(state.IsConceptHidden(1));
    }

    [Fact]
    public void HideRelationshipType_AddsToHiddenRelationshipTypes()
    {
        // Arrange
        var state = new GraphViewState();

        // Act
        state.HideRelationshipType("is-a");

        // Assert
        Assert.Contains("is-a", state.HiddenRelationshipTypes);
    }

    [Fact]
    public void ShowRelationshipType_RemovesFromHiddenRelationshipTypes()
    {
        // Arrange
        var state = new GraphViewState();
        state.HideRelationshipType("is-a");

        // Act
        state.ShowRelationshipType("is-a");

        // Assert
        Assert.DoesNotContain("is-a", state.HiddenRelationshipTypes);
    }

    [Fact]
    public void IsRelationshipTypeHidden_ReturnsTrueForHiddenType()
    {
        // Arrange
        var state = new GraphViewState();
        state.HideRelationshipType("part-of");

        // Assert
        Assert.True(state.IsRelationshipTypeHidden("part-of"));
    }

    [Fact]
    public void IsRelationshipTypeHidden_ReturnsFalseForVisibleType()
    {
        // Arrange
        var state = new GraphViewState();

        // Assert
        Assert.False(state.IsRelationshipTypeHidden("part-of"));
    }

    [Fact]
    public void GetHiddenConceptCount_ReturnsCorrectCount()
    {
        // Arrange
        var state = new GraphViewState();
        state.HideConcept(1);
        state.HideConcept(2);
        state.HideConcept(3);

        // Assert
        Assert.Equal(3, state.GetHiddenConceptCount());
    }

    [Fact]
    public void GetHiddenRelationshipTypeCount_ReturnsCorrectCount()
    {
        // Arrange
        var state = new GraphViewState();
        state.HideRelationshipType("is-a");
        state.HideRelationshipType("part-of");

        // Assert
        Assert.Equal(2, state.GetHiddenRelationshipTypeCount());
    }

    [Fact]
    public void ResetFilters_ClearsAllFilters()
    {
        // Arrange
        var state = new GraphViewState();
        state.FilterByCategory("TestCategory");
        state.SetSearchText("test");
        state.HideConcept(1);
        state.HideConcept(2);
        state.HideRelationshipType("is-a");

        // Act
        state.ResetFilters();

        // Assert
        Assert.Null(state.CategoryFilter);
        Assert.Null(state.SearchText);
        Assert.Empty(state.HiddenConceptIds);
        Assert.Empty(state.HiddenRelationshipTypes);
    }

    [Fact]
    public void ResetToDefaults_ResetsAllSettings()
    {
        // Arrange
        var state = new GraphViewState();
        state.SetLayout(GraphLayout.ForceDirected);
        state.ToggleIndividuals();
        state.ToggleLabels();
        state.SetColorMode(GraphColorMode.Monochrome);
        state.SetZoomLevel(2.0);
        state.FilterByCategory("Test");

        // Act
        state.ResetToDefaults();

        // Assert
        Assert.Equal(GraphLayout.Hierarchical, state.CurrentLayout);
        Assert.False(state.ShowIndividuals);
        Assert.True(state.ShowLabels);
        Assert.True(state.ShowEdgeLabels);
        Assert.Equal(GraphColorMode.ByCategory, state.ColorMode);
        Assert.Equal(1.0, state.ZoomLevel);
        Assert.Null(state.CategoryFilter);
        Assert.Empty(state.HiddenConceptIds);
    }

    // Note: SetAnimationSettings method not implemented in current GraphViewState
    // Animation settings are public properties that can be set directly if needed

    [Fact]
    public void NotifyStateChanged_TriggersEvent()
    {
        // Arrange
        var state = new GraphViewState();
        var eventTriggered = false;
        state.OnGraphStateChanged += () => eventTriggered = true;

        // Act
        state.NotifyStateChanged();

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void OnGraphStateChanged_CanHaveMultipleSubscribers()
    {
        // Arrange
        var state = new GraphViewState();
        var eventCount = 0;
        state.OnGraphStateChanged += () => eventCount++;
        state.OnGraphStateChanged += () => eventCount++;

        // Act
        state.NotifyStateChanged();

        // Assert
        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void AllGraphLayoutValues_CanBeSet()
    {
        // Arrange
        var state = new GraphViewState();
        var layouts = new[]
        {
            GraphLayout.Hierarchical,
            GraphLayout.ForceDirected,
            GraphLayout.Circular,
            GraphLayout.Grid,
            GraphLayout.Concentric,
            GraphLayout.Breadth
        };

        // Act & Assert
        foreach (var layout in layouts)
        {
            state.SetLayout(layout);
            Assert.Equal(layout, state.CurrentLayout);
        }
    }

    [Fact]
    public void AllColorModeValues_CanBeSet()
    {
        // Arrange
        var state = new GraphViewState();
        var colorModes = new[]
        {
            GraphColorMode.ByCategory,
            GraphColorMode.Gradient,
            GraphColorMode.Monochrome,
            GraphColorMode.Random
        };

        // Act & Assert
        foreach (var colorMode in colorModes)
        {
            state.SetColorMode(colorMode);
            Assert.Equal(colorMode, state.ColorMode);
        }
    }
}
