using System;
using System.Collections.Generic;
using System.Linq;
using Eidos.Models.Enums;

namespace Eidos.Models.ViewState
{
    /// <summary>
    /// Manages the general component state for the OntologyView component.
    /// This includes ontology data, selections, UI state, and permissions.
    /// </summary>
    public class OntologyViewState
    {
        #region Events

        /// <summary>
        /// Event fired when any state property changes, allowing subscribers to react to state updates.
        /// </summary>
        public event Action? OnStateChanged;

        #endregion

        #region Ontology Data Properties

        /// <summary>
        /// The current ontology being viewed/edited.
        /// </summary>
        public Ontology? Ontology { get; private set; }

        /// <summary>
        /// List of all concepts in the ontology. Convenience property for easier access.
        /// </summary>
        public IReadOnlyList<Concept> Concepts => Ontology?.Concepts?.ToList() ?? new List<Concept>();

        /// <summary>
        /// List of all relationships in the ontology. Convenience property for easier access.
        /// </summary>
        public IReadOnlyList<Relationship> Relationships => Ontology?.Relationships?.ToList() ?? new List<Relationship>();

        /// <summary>
        /// List of all individuals (instances) in the ontology.
        /// </summary>
        public IEnumerable<Individual>? Individuals { get; private set; }

        #endregion

        #region View Mode State

        /// <summary>
        /// The current view mode (Graph, List, Hierarchy, etc.).
        /// </summary>
        public ViewMode CurrentViewMode { get; private set; } = ViewMode.Graph;

        #endregion

        #region Loading and Error State

        /// <summary>
        /// Indicates whether data is currently being loaded.
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Error message if loading or operations failed.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        #endregion

        #region Selection State

        /// <summary>
        /// The currently selected concept, if any.
        /// </summary>
        public Concept? SelectedConcept { get; private set; }

        /// <summary>
        /// The currently selected relationship, if any.
        /// </summary>
        public Relationship? SelectedRelationship { get; private set; }

        /// <summary>
        /// The currently selected individual, if any.
        /// </summary>
        public Individual? SelectedIndividual { get; private set; }

        /// <summary>
        /// The ID of the selected concept (for hierarchy navigation).
        /// </summary>
        public int? SelectedConceptId => SelectedConcept?.Id;

        #endregion

        #region UI State

        /// <summary>
        /// Indicates whether the sidebar is visible (for mobile/responsive layouts).
        /// </summary>
        public bool IsSidebarVisible { get; private set; } = true;

        /// <summary>
        /// Indicates whether the details panel is visible.
        /// </summary>
        public bool IsDetailsPanelVisible { get; private set; } = true;

        /// <summary>
        /// Indicates whether the validation panel is expanded.
        /// </summary>
        public bool IsValidationPanelExpanded { get; private set; } = false;

        #endregion

        #region Permission State

        /// <summary>
        /// The current user's permission level for this ontology.
        /// </summary>
        public PermissionLevel? UserPermissionLevel { get; private set; }

        /// <summary>
        /// The current user's ID.
        /// </summary>
        public string? CurrentUserId { get; private set; }

        /// <summary>
        /// Indicates whether the current user can add new concepts/relationships.
        /// </summary>
        public bool CanAdd => UserPermissionLevel.HasValue &&
                              UserPermissionLevel.Value >= PermissionLevel.ViewAndAdd;

        /// <summary>
        /// Indicates whether the current user can edit existing concepts/relationships.
        /// </summary>
        public bool CanEdit => UserPermissionLevel.HasValue &&
                               UserPermissionLevel.Value >= PermissionLevel.ViewAddEdit;

        /// <summary>
        /// Indicates whether the current user has full access (manage settings, sharing, etc.).
        /// </summary>
        public bool CanManage => UserPermissionLevel.HasValue &&
                                 UserPermissionLevel.Value == PermissionLevel.FullAccess;

        #endregion

        #region State Update Methods

        /// <summary>
        /// Sets the ontology data and updates related state.
        /// </summary>
        /// <param name="ontology">The ontology to set.</param>
        public void SetOntology(Ontology? ontology)
        {
            Ontology = ontology;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the individuals list.
        /// </summary>
        /// <param name="individuals">The individuals to set.</param>
        public void SetIndividuals(IEnumerable<Individual>? individuals)
        {
            Individuals = individuals;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the current view mode.
        /// </summary>
        /// <param name="viewMode">The view mode to set.</param>
        public void SetViewMode(ViewMode viewMode)
        {
            CurrentViewMode = viewMode;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the loading state.
        /// </summary>
        /// <param name="isLoading">Whether data is being loaded.</param>
        public void SetLoading(bool isLoading)
        {
            IsLoading = isLoading;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets an error message.
        /// </summary>
        /// <param name="errorMessage">The error message to display.</param>
        public void SetError(string? errorMessage)
        {
            ErrorMessage = errorMessage;
            NotifyStateChanged();
        }

        /// <summary>
        /// Clears the error message.
        /// </summary>
        public void ClearError()
        {
            ErrorMessage = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the selected concept and clears other selections.
        /// </summary>
        /// <param name="concept">The concept to select.</param>
        public void SetSelectedConcept(Concept? concept)
        {
            SelectedConcept = concept;
            SelectedRelationship = null;
            SelectedIndividual = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the selected relationship and clears other selections.
        /// </summary>
        /// <param name="relationship">The relationship to select.</param>
        public void SetSelectedRelationship(Relationship? relationship)
        {
            SelectedRelationship = relationship;
            SelectedConcept = null;
            SelectedIndividual = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the selected individual and clears other selections.
        /// </summary>
        /// <param name="individual">The individual to select.</param>
        public void SetSelectedIndividual(Individual? individual)
        {
            SelectedIndividual = individual;
            SelectedConcept = null;
            SelectedRelationship = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Clears all selections (concept, relationship, individual).
        /// </summary>
        public void ClearSelection()
        {
            SelectedConcept = null;
            SelectedRelationship = null;
            SelectedIndividual = null;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the sidebar visibility.
        /// </summary>
        /// <param name="isVisible">Whether the sidebar should be visible.</param>
        public void SetSidebarVisibility(bool isVisible)
        {
            IsSidebarVisible = isVisible;
            NotifyStateChanged();
        }

        /// <summary>
        /// Toggles the sidebar visibility.
        /// </summary>
        public void ToggleSidebar()
        {
            IsSidebarVisible = !IsSidebarVisible;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the details panel visibility.
        /// </summary>
        /// <param name="isVisible">Whether the details panel should be visible.</param>
        public void SetDetailsPanelVisibility(bool isVisible)
        {
            IsDetailsPanelVisible = isVisible;
            NotifyStateChanged();
        }

        /// <summary>
        /// Toggles the details panel visibility.
        /// </summary>
        public void ToggleDetailsPanel()
        {
            IsDetailsPanelVisible = !IsDetailsPanelVisible;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the validation panel expanded state.
        /// </summary>
        /// <param name="isExpanded">Whether the validation panel should be expanded.</param>
        public void SetValidationPanelExpanded(bool isExpanded)
        {
            IsValidationPanelExpanded = isExpanded;
            NotifyStateChanged();
        }

        /// <summary>
        /// Toggles the validation panel expanded state.
        /// </summary>
        public void ToggleValidationPanel()
        {
            IsValidationPanelExpanded = !IsValidationPanelExpanded;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the user's permission level for the current ontology.
        /// </summary>
        /// <param name="permissionLevel">The permission level to set.</param>
        public void SetPermissionLevel(PermissionLevel? permissionLevel)
        {
            UserPermissionLevel = permissionLevel;
            NotifyStateChanged();
        }

        /// <summary>
        /// Sets the current user's ID.
        /// </summary>
        /// <param name="userId">The user ID to set.</param>
        public void SetCurrentUserId(string? userId)
        {
            CurrentUserId = userId;
            NotifyStateChanged();
        }

        #endregion

        #region Notification Methods

        /// <summary>
        /// Notifies subscribers that the state has changed.
        /// Call this method after updating state properties to trigger UI updates.
        /// </summary>
        public void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets a concept by its ID.
        /// </summary>
        /// <param name="conceptId">The concept ID to find.</param>
        /// <returns>The concept if found, otherwise null.</returns>
        public Concept? GetConceptById(int conceptId)
        {
            return Concepts.FirstOrDefault(c => c.Id == conceptId);
        }

        /// <summary>
        /// Gets a relationship by its ID.
        /// </summary>
        /// <param name="relationshipId">The relationship ID to find.</param>
        /// <returns>The relationship if found, otherwise null.</returns>
        public Relationship? GetRelationshipById(int relationshipId)
        {
            return Relationships.FirstOrDefault(r => r.Id == relationshipId);
        }

        /// <summary>
        /// Gets an individual by its ID.
        /// </summary>
        /// <param name="individualId">The individual ID to find.</param>
        /// <returns>The individual if found, otherwise null.</returns>
        public Individual? GetIndividualById(int individualId)
        {
            return Individuals?.FirstOrDefault(i => i.Id == individualId);
        }

        /// <summary>
        /// Checks if there are any unsaved changes (placeholder for future implementation).
        /// </summary>
        /// <returns>True if there are unsaved changes, otherwise false.</returns>
        public bool HasUnsavedChanges()
        {
            // This would be implemented based on dirty tracking logic
            // For now, return false as the current implementation saves immediately
            return false;
        }

        #endregion
    }
}
