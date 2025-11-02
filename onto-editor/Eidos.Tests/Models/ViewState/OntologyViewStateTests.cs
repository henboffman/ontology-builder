using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Models.ViewState;
using Xunit;

namespace Eidos.Tests.Models.ViewState;

/// <summary>
/// Unit tests for the OntologyViewState class
/// </summary>
public class OntologyViewStateTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var state = new OntologyViewState();

        // Assert
        Assert.Null(state.Ontology);
        Assert.Empty(state.Concepts);
        Assert.Empty(state.Relationships);
        Assert.Empty(state.Individuals);
        Assert.Equal(ViewMode.Graph, state.CurrentViewMode);
        Assert.False(state.IsLoading);
        Assert.Null(state.ErrorMessage);
        Assert.Null(state.SelectedConcept);
        Assert.Null(state.SelectedRelationship);
        Assert.Null(state.SelectedIndividual);
        Assert.True(state.IsSidebarVisible);
        Assert.False(state.IsDetailsPanelVisible);
        Assert.False(state.IsValidationPanelExpanded);
        Assert.Null(state.UserPermissionLevel);
        Assert.Null(state.CurrentUserId);
    }

    [Fact]
    public void SetOntology_UpdatesOntologyProperty()
    {
        // Arrange
        var state = new OntologyViewState();
        var ontology = new Ontology { Id = 1, Name = "Test Ontology" };

        // Act
        state.SetOntology(ontology);

        // Assert
        Assert.Equal(ontology, state.Ontology);
    }

    [Fact]
    public void SetOntology_TriggersStateChangedEvent()
    {
        // Arrange
        var state = new OntologyViewState();
        var ontology = new Ontology { Id = 1, Name = "Test Ontology" };
        var eventTriggered = false;
        state.OnStateChanged += () => eventTriggered = true;

        // Act
        state.SetOntology(ontology);

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void SetSelectedConcept_UpdatesSelectionAndClearsOthers()
    {
        // Arrange
        var state = new OntologyViewState();
        var concept = new Concept { Id = 1, Name = "Test Concept" };
        var relationship = new Relationship { Id = 1 };
        var individual = new Individual { Id = 1 };

        state.SetSelectedRelationship(relationship);
        state.SetSelectedIndividual(individual);

        // Act
        state.SetSelectedConcept(concept);

        // Assert
        Assert.Equal(concept, state.SelectedConcept);
        Assert.Null(state.SelectedRelationship);
        Assert.Null(state.SelectedIndividual);
    }

    [Fact]
    public void SetSelectedRelationship_UpdatesSelectionAndClearsOthers()
    {
        // Arrange
        var state = new OntologyViewState();
        var concept = new Concept { Id = 1, Name = "Test Concept" };
        var relationship = new Relationship { Id = 1 };
        var individual = new Individual { Id = 1 };

        state.SetSelectedConcept(concept);
        state.SetSelectedIndividual(individual);

        // Act
        state.SetSelectedRelationship(relationship);

        // Assert
        Assert.Null(state.SelectedConcept);
        Assert.Equal(relationship, state.SelectedRelationship);
        Assert.Null(state.SelectedIndividual);
    }

    [Fact]
    public void SetSelectedIndividual_UpdatesSelectionAndClearsOthers()
    {
        // Arrange
        var state = new OntologyViewState();
        var concept = new Concept { Id = 1, Name = "Test Concept" };
        var relationship = new Relationship { Id = 1 };
        var individual = new Individual { Id = 1 };

        state.SetSelectedConcept(concept);
        state.SetSelectedRelationship(relationship);

        // Act
        state.SetSelectedIndividual(individual);

        // Assert
        Assert.Null(state.SelectedConcept);
        Assert.Null(state.SelectedRelationship);
        Assert.Equal(individual, state.SelectedIndividual);
    }

    [Fact]
    public void ClearSelection_ClearsAllSelections()
    {
        // Arrange
        var state = new OntologyViewState();
        state.SetSelectedConcept(new Concept { Id = 1 });
        state.SetSelectedRelationship(new Relationship { Id = 1 });
        state.SetSelectedIndividual(new Individual { Id = 1 });

        // Act
        state.ClearSelection();

        // Assert
        Assert.Null(state.SelectedConcept);
        Assert.Null(state.SelectedRelationship);
        Assert.Null(state.SelectedIndividual);
    }

    [Fact]
    public void SetViewMode_UpdatesViewMode()
    {
        // Arrange
        var state = new OntologyViewState();

        // Act
        state.SetViewMode(ViewMode.List);

        // Assert
        Assert.Equal(ViewMode.List, state.CurrentViewMode);
    }

    [Fact]
    public void SetLoadingState_UpdatesLoadingAndError()
    {
        // Arrange
        var state = new OntologyViewState();

        // Act
        state.SetLoadingState(true, "Test error");

        // Assert
        Assert.True(state.IsLoading);
        Assert.Equal("Test error", state.ErrorMessage);
    }

    [Fact]
    public void SetPermissions_UpdatesPermissionProperties()
    {
        // Arrange
        var state = new OntologyViewState();
        var userId = "user123";
        var permissionLevel = PermissionLevel.ViewAddEdit;

        // Act
        state.SetPermissions(userId, permissionLevel);

        // Assert
        Assert.Equal(userId, state.CurrentUserId);
        Assert.Equal(permissionLevel, state.UserPermissionLevel);
    }

    [Fact]
    public void CanAdd_ReturnsTrueForViewAndAddPermission()
    {
        // Arrange
        var state = new OntologyViewState();
        state.SetPermissions("user", PermissionLevel.ViewAndAdd);

        // Assert
        Assert.True(state.CanAdd);
    }

    [Fact]
    public void CanEdit_ReturnsTrueForViewAddEditPermission()
    {
        // Arrange
        var state = new OntologyViewState();
        state.SetPermissions("user", PermissionLevel.ViewAddEdit);

        // Assert
        Assert.True(state.CanEdit);
        Assert.True(state.CanAdd);
    }

    [Fact]
    public void CanManage_ReturnsTrueForFullAccessPermission()
    {
        // Arrange
        var state = new OntologyViewState();
        state.SetPermissions("user", PermissionLevel.FullAccess);

        // Assert
        Assert.True(state.CanManage);
        Assert.True(state.CanEdit);
        Assert.True(state.CanAdd);
    }

    [Fact]
    public void CanAdd_ReturnsFalseForViewOnlyPermission()
    {
        // Arrange
        var state = new OntologyViewState();
        state.SetPermissions("user", PermissionLevel.View);

        // Assert
        Assert.False(state.CanAdd);
        Assert.False(state.CanEdit);
        Assert.False(state.CanManage);
    }

    [Fact]
    public void ToggleSidebar_TogglesVisibility()
    {
        // Arrange
        var state = new OntologyViewState();
        var initialValue = state.IsSidebarVisible;

        // Act
        state.ToggleSidebar();

        // Assert
        Assert.NotEqual(initialValue, state.IsSidebarVisible);
    }

    [Fact]
    public void ToggleDetailsPanel_TogglesVisibility()
    {
        // Arrange
        var state = new OntologyViewState();
        var initialValue = state.IsDetailsPanelVisible;

        // Act
        state.ToggleDetailsPanel();

        // Assert
        Assert.NotEqual(initialValue, state.IsDetailsPanelVisible);
    }

    [Fact]
    public void GetConceptById_ReturnsCorrectConcept()
    {
        // Arrange
        var state = new OntologyViewState();
        var concept1 = new Concept { Id = 1, Name = "Concept 1" };
        var concept2 = new Concept { Id = 2, Name = "Concept 2" };
        var ontology = new Ontology();
        ontology.Concepts.Add(concept1);
        ontology.Concepts.Add(concept2);
        state.SetOntology(ontology);

        // Act
        var result = state.GetConceptById(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Concept 2", result.Name);
    }

    [Fact]
    public void GetConceptById_ReturnsNullForNonExistentId()
    {
        // Arrange
        var state = new OntologyViewState();
        var ontology = new Ontology();
        state.SetOntology(ontology);

        // Act
        var result = state.GetConceptById(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRelationshipById_ReturnsCorrectRelationship()
    {
        // Arrange
        var state = new OntologyViewState();
        var rel1 = new Relationship { Id = 1, RelationType = "is-a" };
        var rel2 = new Relationship { Id = 2, RelationType = "part-of" };
        var ontology = new Ontology();
        ontology.Relationships.Add(rel1);
        ontology.Relationships.Add(rel2);
        state.SetOntology(ontology);

        // Act
        var result = state.GetRelationshipById(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("part-of", result.RelationType);
    }

    [Fact]
    public void NotifyStateChanged_TriggersEvent()
    {
        // Arrange
        var state = new OntologyViewState();
        var eventTriggered = false;
        state.OnStateChanged += () => eventTriggered = true;

        // Act
        state.NotifyStateChanged();

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void OnStateChanged_CanHaveMultipleSubscribers()
    {
        // Arrange
        var state = new OntologyViewState();
        var eventCount = 0;
        state.OnStateChanged += () => eventCount++;
        state.OnStateChanged += () => eventCount++;

        // Act
        state.NotifyStateChanged();

        // Assert
        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void Concepts_ReturnsEmptyListWhenOntologyIsNull()
    {
        // Arrange
        var state = new OntologyViewState();

        // Assert
        Assert.NotNull(state.Concepts);
        Assert.Empty(state.Concepts);
    }

    [Fact]
    public void Relationships_ReturnsEmptyListWhenOntologyIsNull()
    {
        // Arrange
        var state = new OntologyViewState();

        // Assert
        Assert.NotNull(state.Relationships);
        Assert.Empty(state.Relationships);
    }

    [Fact]
    public void Individuals_ReturnsEmptyListWhenOntologyIsNull()
    {
        // Arrange
        var state = new OntologyViewState();

        // Assert
        Assert.NotNull(state.Individuals);
        Assert.Empty(state.Individuals);
    }
}
