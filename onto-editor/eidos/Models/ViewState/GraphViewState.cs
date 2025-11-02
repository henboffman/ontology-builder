using System;
using System.Collections.Generic;
using System.Linq;

namespace Eidos.Models.ViewState
{
    /// <summary>
    /// Manages the graph-specific state for the GraphView component.
    /// This includes graph instance properties, layout settings, interaction state, and filtering.
    /// </summary>
    public class GraphViewState
    {
        #region Events

        /// <summary>
        /// Event fired when any graph state property changes, allowing subscribers to react to updates.
        /// </summary>
        public event Action? OnGraphStateChanged;

        #endregion

        #region Graph Layout Enum

        /// <summary>
        /// Available graph layout algorithms.
        /// </summary>
        public enum GraphLayout
        {
            /// <summary>
            /// Hierarchical layout with nodes arranged in levels.
            /// </summary>
            Hierarchical,

            /// <summary>
            /// Force-directed layout with physics simulation.
            /// </summary>
            ForceDirected,

            /// <summary>
            /// Circular layout with nodes arranged in a circle.
            /// </summary>
            Circular,

            /// <summary>
            /// Grid layout with nodes arranged in a grid pattern.
            /// </summary>
            Grid,

            /// <summary>
            /// Concentric circles layout based on node connectivity.
            /// </summary>
            Concentric,

            /// <summary>
            /// Breadth-first search layout.
            /// </summary>
            Breadth
        }

        #endregion

        #region Graph Instance Properties

        /// <summary>
        /// The unique identifier for the graph element in the DOM.
        /// </summary>
        public string GraphElementId { get; private set; } = "cy";

        /// <summary>
        /// Indicates whether the graph instance has been initialized.
        /// </summary>
        public bool IsGraphInitialized { get; private set; }

        /// <summary>
        /// Timestamp of when the graph was last refreshed.
        /// </summary>
        public DateTime? LastRefreshTime { get; private set; }

        #endregion

        #region Layout Settings

        /// <summary>
        /// The current layout algorithm being used.
        /// </summary>
        public GraphLayout CurrentLayout { get; private set; } = GraphLayout.Hierarchical;

        /// <summary>
        /// Indicates whether individuals (instances) should be shown in the graph.
        /// </summary>
        public bool ShowIndividuals { get; private set; } = false;

        /// <summary>
        /// Indicates whether node labels should be shown in the graph.
        /// </summary>
        public bool ShowLabels { get; private set; } = true;

        /// <summary>
        /// Indicates whether edge (relationship) labels should be shown.
        /// </summary>
        public bool ShowEdgeLabels { get; private set; } = true;

        /// <summary>
        /// The color mode for the graph: "concept" (colored by type), "source" (colored by source ontology), etc.
        /// </summary>
        public string ColorMode { get; private set; } = "concept";

        #endregion

        #region Interaction State

        /// <summary>
        /// Indicates whether a node is currently being dragged.
        /// </summary>
        public bool IsNodeDragging { get; private set; }

        /// <summary>
        /// The ID of the node currently being hovered over.
        /// </summary>
        public string? HoveredNodeId { get; private set; }

        /// <summary>
        /// The ID of the node currently being dragged.
        /// </summary>
        public string? DraggedNodeId { get; private set; }

        /// <summary>
        /// Indicates whether the graph is currently being panned.
        /// </summary>
        public bool IsPanning { get; private set; }

        /// <summary>
        /// The current zoom level (1.0 = 100%).
        /// </summary>
        public double ZoomLevel { get; private set; } = 1.0;

        #endregion

        #region Filtering State

        /// <summary>
        /// The category filter applied to the graph (empty or null means no filter).
        /// </summary>
        public string? CategoryFilter { get; private set; }

        /// <summary>
        /// Set of concept IDs that are currently hidden from the graph.
        /// </summary>
        public HashSet<int> HiddenConceptIds { get; private set; } = new HashSet<int>();

        /// <summary>
        /// Set of relationship types that are currently hidden from the graph.
        /// </summary>
        public HashSet<string> HiddenRelationshipTypes { get; private set; } = new HashSet<string>();

        /// <summary>
        /// The search/filter text entered by the user.
        /// </summary>
        public string? SearchText { get; private set; }

        #endregion

        #region Graph Display Settings

        /// <summary>
        /// The minimum zoom level allowed.
        /// </summary>
        public double MinZoom { get; private set; } = 0.1;

        /// <summary>
        /// The maximum zoom level allowed.
        /// </summary>
        public double MaxZoom { get; private set; } = 5.0;

        /// <summary>
        /// Indicates whether the graph should animate layout changes.
        /// </summary>
        public bool AnimateLayout { get; private set; } = true;

        /// <summary>
        /// The duration of layout animations in milliseconds.
        /// </summary>
        public int AnimationDuration { get; private set; } = 500;

        #endregion

        #region State Update Methods

        /// <summary>
        /// Sets the graph element ID.
        /// </summary>
        /// <param name="elementId">The DOM element ID for the graph container.</param>
        public void SetGraphElementId(string elementId)
        {
            GraphElementId = elementId ?? "cy";
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the graph initialization state.
        /// </summary>
        /// <param name="isInitialized">Whether the graph has been initialized.</param>
        public void SetGraphInitialized(bool isInitialized)
        {
            IsGraphInitialized = isInitialized;
            if (isInitialized)
            {
                LastRefreshTime = DateTime.UtcNow;
            }
            NotifyStateChanged();
        }

        /// <summary>
        /// Updates the last refresh timestamp to now.
        /// </summary>
        public void MarkRefreshed()
        {
            LastRefreshTime = DateTime.UtcNow;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the current layout algorithm.
        /// </summary>
        /// <param name="layout">The layout to apply.</param>
        public void SetLayout(GraphLayout layout)
        {
            CurrentLayout = layout;
            NotifyStateChanged();
        }

        /// <summary>
        /// Toggles the visibility of individuals in the graph.
        /// </summary>
        public void ToggleIndividuals()
        {
            ShowIndividuals = !ShowIndividuals;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets whether individuals should be shown.
        /// </summary>
        /// <param name="show">Whether to show individuals.</param>
        public void SetShowIndividuals(bool show)
        {
            ShowIndividuals = show;
            NotifyStateChanged();
        }

        /// <summary>
        /// Toggles the visibility of node labels.
        /// </summary>
        public void ToggleLabels()
        {
            ShowLabels = !ShowLabels;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets whether node labels should be shown.
        /// </summary>
        /// <param name="show">Whether to show labels.</param>
        public void SetShowLabels(bool show)
        {
            ShowLabels = show;
            NotifyStateChanged();
        }

        /// <summary>
        /// Toggles the visibility of edge labels.
        /// </summary>
        public void ToggleEdgeLabels()
        {
            ShowEdgeLabels = !ShowEdgeLabels;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets whether edge labels should be shown.
        /// </summary>
        /// <param name="show">Whether to show edge labels.</param>
        public void SetShowEdgeLabels(bool show)
        {
            ShowEdgeLabels = show;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the color mode for the graph visualization.
        /// </summary>
        /// <param name="mode">The color mode (e.g., "concept", "source", "category").</param>
        public void SetColorMode(string mode)
        {
            ColorMode = mode ?? "concept";
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the node dragging state.
        /// </summary>
        /// <param name="isDragging">Whether a node is being dragged.</param>
        /// <param name="nodeId">The ID of the node being dragged (optional).</param>
        public void SetNodeDragging(bool isDragging, string? nodeId = null)
        {
            IsNodeDragging = isDragging;
            DraggedNodeId = isDragging ? nodeId : null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the hovered node.
        /// </summary>
        /// <param name="nodeId">The ID of the hovered node, or null if no node is hovered.</param>
        public void SetHoveredNode(string? nodeId)
        {
            HoveredNodeId = nodeId;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the panning state.
        /// </summary>
        /// <param name="isPanning">Whether the graph is being panned.</param>
        public void SetPanning(bool isPanning)
        {
            IsPanning = isPanning;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the zoom level.
        /// </summary>
        /// <param name="zoomLevel">The new zoom level (clamped between MinZoom and MaxZoom).</param>
        public void SetZoomLevel(double zoomLevel)
        {
            ZoomLevel = Math.Clamp(zoomLevel, MinZoom, MaxZoom);
            NotifyStateChanged();
        }

        /// <summary>
        /// Resets the zoom level to 1.0 (100%).
        /// </summary>
        public void ResetZoom()
        {
            ZoomLevel = 1.0;
            NotifyStateChanged();
        }

        /// <summary>
        /// Applies a category filter to the graph.
        /// </summary>
        /// <param name="category">The category to filter by, or null to clear the filter.</param>
        public void FilterByCategory(string? category)
        {
            CategoryFilter = category;
            NotifyStateChanged();
        }

        /// <summary>
        /// Clears the category filter.
        /// </summary>
        public void ClearCategoryFilter()
        {
            CategoryFilter = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Hides a concept from the graph.
        /// </summary>
        /// <param name="conceptId">The ID of the concept to hide.</param>
        public void HideConcept(int conceptId)
        {
            HiddenConceptIds.Add(conceptId);
            NotifyStateChanged();
        }

        /// <summary>
        /// Shows a previously hidden concept.
        /// </summary>
        /// <param name="conceptId">The ID of the concept to show.</param>
        public void ShowConcept(int conceptId)
        {
            HiddenConceptIds.Remove(conceptId);
            NotifyStateChanged();
        }

        /// <summary>
        /// Toggles the visibility of a concept.
        /// </summary>
        /// <param name="conceptId">The ID of the concept to toggle.</param>
        public void ToggleConceptVisibility(int conceptId)
        {
            if (HiddenConceptIds.Contains(conceptId))
            {
                HiddenConceptIds.Remove(conceptId);
            }
            else
            {
                HiddenConceptIds.Add(conceptId);
            }
            NotifyStateChanged();
        }

        /// <summary>
        /// Clears all hidden concepts.
        /// </summary>
        public void ShowAllConcepts()
        {
            HiddenConceptIds.Clear();
            NotifyStateChanged();
        }

        /// <summary>
        /// Hides a relationship type from the graph.
        /// </summary>
        /// <param name="relationshipType">The relationship type to hide.</param>
        public void HideRelationshipType(string relationshipType)
        {
            if (!string.IsNullOrWhiteSpace(relationshipType))
            {
                HiddenRelationshipTypes.Add(relationshipType);
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// Shows a previously hidden relationship type.
        /// </summary>
        /// <param name="relationshipType">The relationship type to show.</param>
        public void ShowRelationshipType(string relationshipType)
        {
            HiddenRelationshipTypes.Remove(relationshipType);
            NotifyStateChanged();
        }

        /// <summary>
        /// Clears all hidden relationship types.
        /// </summary>
        public void ShowAllRelationshipTypes()
        {
            HiddenRelationshipTypes.Clear();
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the search/filter text.
        /// </summary>
        /// <param name="searchText">The search text to apply.</param>
        public void SetSearchText(string? searchText)
        {
            SearchText = searchText;
            NotifyStateChanged();
        }

        /// <summary>
        /// Clears the search/filter text.
        /// </summary>
        public void ClearSearch()
        {
            SearchText = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the minimum zoom level.
        /// </summary>
        /// <param name="minZoom">The minimum zoom level.</param>
        public void SetMinZoom(double minZoom)
        {
            MinZoom = Math.Max(0.01, minZoom);
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the maximum zoom level.
        /// </summary>
        /// <param name="maxZoom">The maximum zoom level.</param>
        public void SetMaxZoom(double maxZoom)
        {
            MaxZoom = Math.Max(1.0, maxZoom);
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets whether layout changes should be animated.
        /// </summary>
        /// <param name="animate">Whether to animate layout changes.</param>
        public void SetAnimateLayout(bool animate)
        {
            AnimateLayout = animate;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the animation duration for layout changes.
        /// </summary>
        /// <param name="durationMs">The duration in milliseconds.</param>
        public void SetAnimationDuration(int durationMs)
        {
            AnimationDuration = Math.Max(0, durationMs);
            NotifyStateChanged();
        }

        #endregion

        #region Notification Methods

        /// <summary>
        /// Notifies subscribers that the graph state has changed.
        /// Call this method after updating state properties to trigger UI updates.
        /// </summary>
        public void NotifyStateChanged()
        {
            OnGraphStateChanged?.Invoke();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if a concept is currently hidden.
        /// </summary>
        /// <param name="conceptId">The concept ID to check.</param>
        /// <returns>True if the concept is hidden, otherwise false.</returns>
        public bool IsConceptHidden(int conceptId)
        {
            return HiddenConceptIds.Contains(conceptId);
        }

        /// <summary>
        /// Checks if a relationship type is currently hidden.
        /// </summary>
        /// <param name="relationshipType">The relationship type to check.</param>
        /// <returns>True if the relationship type is hidden, otherwise false.</returns>
        public bool IsRelationshipTypeHidden(string relationshipType)
        {
            return HiddenRelationshipTypes.Contains(relationshipType);
        }

        /// <summary>
        /// Gets the count of currently hidden concepts.
        /// </summary>
        /// <returns>The number of hidden concepts.</returns>
        public int GetHiddenConceptCount()
        {
            return HiddenConceptIds.Count;
        }

        /// <summary>
        /// Gets the count of currently hidden relationship types.
        /// </summary>
        /// <returns>The number of hidden relationship types.</returns>
        public int GetHiddenRelationshipTypeCount()
        {
            return HiddenRelationshipTypes.Count;
        }

        /// <summary>
        /// Resets all filters and hidden items to their default state.
        /// </summary>
        public void ResetFilters()
        {
            CategoryFilter = null;
            HiddenConceptIds.Clear();
            HiddenRelationshipTypes.Clear();
            SearchText = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Resets the graph state to default values.
        /// </summary>
        public void ResetToDefaults()
        {
            CurrentLayout = GraphLayout.Hierarchical;
            ShowIndividuals = false;
            ShowLabels = true;
            ShowEdgeLabels = true;
            ColorMode = "concept";
            ZoomLevel = 1.0;
            ResetFilters();
            NotifyStateChanged();
        }

        #endregion
    }
}
