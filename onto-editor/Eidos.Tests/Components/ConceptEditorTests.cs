using Bunit;
using Eidos.Components.Ontology;
using Eidos.Models;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Eidos.Tests.Components;

/// <summary>
/// Component tests for ConceptEditor using bUnit
/// </summary>
public class ConceptEditorTests : TestContext
{
    [Fact]
    public void Render_ShouldShowAddMode_WhenIsEditingIsFalse()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, false));

        // Assert
        Assert.Contains("Add New Concept", cut.Markup);
        Assert.Contains("Add", cut.Markup);
    }

    [Fact]
    public void Render_ShouldShowEditMode_WhenIsEditingIsTrue()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, true));

        // Assert
        Assert.Contains("Edit Concept", cut.Markup);
        Assert.Contains("Save", cut.Markup);
    }

    [Fact]
    public void Render_ShouldShowTemplateSelector_WhenNotEditing()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, false));

        // Assert
        Assert.Contains("Start from Template", cut.Markup);
        Assert.Contains("Choose a template", cut.Markup);
    }

    [Fact]
    public void Render_ShouldNotShowTemplateSelector_WhenEditing()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, true));

        // Assert
        Assert.DoesNotContain("Start from Template", cut.Markup);
    }

    [Fact]
    public void Render_ShouldDisplayCustomTemplates_WhenProvided()
    {
        // Arrange
        var customTemplates = new List<CustomConceptTemplate>
        {
            new CustomConceptTemplate
            {
                Id = 1,
                Category = "My Custom Concept",
                Type = "Entity"
            }
        };

        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, false)
            .Add(p => p.CustomTemplates, customTemplates));

        // Assert
        Assert.Contains("Your Custom Templates", cut.Markup);
        Assert.Contains("My Custom Concept", cut.Markup);
    }

    [Fact]
    public void Render_ShouldDisableSaveButton_WhenNameIsEmpty()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.ConceptName, ""));

        // Assert
        var saveButton = cut.Find("button.btn-primary");
        Assert.NotNull(saveButton.GetAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldEnableSaveButton_WhenNameIsProvided()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.ConceptName, "Test Concept"));

        // Assert
        var saveButton = cut.Find("button.btn-primary");
        Assert.Null(saveButton.GetAttribute("disabled"));
    }

    [Fact]
    public void InputName_ShouldTriggerCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var receivedValue = "";

        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.ConceptName, "")
            .Add(p => p.ConceptNameChanged, EventCallback.Factory.Create<string>(
                this, (value) =>
                {
                    callbackInvoked = true;
                    receivedValue = value;
                })));

        // Act
        var input = cut.Find("input[placeholder*='Mammal']");
        input.Change("New Concept");

        // Assert
        Assert.True(callbackInvoked);
        Assert.Equal("New Concept", receivedValue);
    }

    [Fact]
    public void ClickSaveButton_ShouldTriggerOnSaveClick()
    {
        // Arrange
        var callbackInvoked = false;

        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.ConceptName, "Test Concept")
            .Add(p => p.OnSaveClick, EventCallback.Factory.Create(
                this, () => callbackInvoked = true)));

        // Act
        var saveButton = cut.Find("button.btn-primary");
        saveButton.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void ClickCancelButton_ShouldTriggerOnCancelClick()
    {
        // Arrange
        var callbackInvoked = false;

        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.OnCancelClick, EventCallback.Factory.Create(
                this, () => callbackInvoked = true)));

        // Act
        var cancelButton = cut.Find("button.btn-outline-secondary");
        cancelButton.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void ToggleHelpButton_ShouldShowAndHideHelp()
    {
        // Arrange
        var cut = RenderComponent<ConceptEditor>();

        // Assert initially hidden
        Assert.DoesNotContain("A concept is a fundamental idea", cut.Markup);

        // Act - Click help button to show
        var helpButton = cut.Find("button[title='What is a concept?']");
        helpButton.Click();

        // Assert help is shown
        Assert.Contains("A concept is a fundamental idea", cut.Markup);

        // Act - Click again to hide
        helpButton.Click();

        // Assert help is hidden again
        Assert.DoesNotContain("A concept is a fundamental idea", cut.Markup);
    }

    [Fact]
    public void ChangeColor_ShouldTriggerCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var receivedValue = "";

        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.ConceptColor, "#000000")
            .Add(p => p.ConceptColorChanged, EventCallback.Factory.Create<string?>(
                this, (value) =>
                {
                    callbackInvoked = true;
                    receivedValue = value ?? "";
                })));

        // Act
        var colorInput = cut.Find("input[type='color']");
        colorInput.Change("#FF5733");

        // Assert
        Assert.True(callbackInvoked);
        Assert.Equal("#FF5733", receivedValue);
    }

    [Fact]
    public void SelectTemplate_ShouldTriggerCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var receivedValue = "";

        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, false)
            .Add(p => p.OnTemplateSelected, EventCallback.Factory.Create<string>(
                this, (value) =>
                {
                    callbackInvoked = true;
                    receivedValue = value;
                })));

        // Act
        var templateSelect = cut.Find("select.form-select");
        templateSelect.Change("default:Person");

        // Assert
        Assert.True(callbackInvoked);
        Assert.Equal("default:Person", receivedValue);
    }

    [Fact]
    public void Render_WithPulseShouldPulse_ShouldAddPulseClass()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.ShouldPulse, true));

        // Assert
        Assert.Contains("pulse-attention", cut.Markup);
    }

    [Fact]
    public void Render_WithoutPulse_ShouldNotAddPulseClass()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.ShouldPulse, false));

        // Assert
        Assert.DoesNotContain("pulse-attention", cut.Markup);
    }

    [Fact]
    public void Render_ShouldShowSaveAndAddAnotherButton_WhenNotEditing()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, false));

        // Assert
        Assert.Contains("Save & Add Another", cut.Markup);
    }

    [Fact]
    public void Render_ShouldNotShowSaveAndAddAnotherButton_WhenEditing()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, true));

        // Assert
        Assert.DoesNotContain("Save & Add Another", cut.Markup);
    }

    [Fact]
    public void Render_ShouldDisableSaveAndAddAnotherButton_WhenNameIsEmpty()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, false)
            .Add(p => p.ConceptName, ""));

        // Assert
        var button = cut.Find("button.btn-success");
        Assert.NotNull(button.GetAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldEnableSaveAndAddAnotherButton_WhenNameIsProvided()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, false)
            .Add(p => p.ConceptName, "Test Concept"));

        // Assert
        var button = cut.Find("button.btn-success");
        Assert.Null(button.GetAttribute("disabled"));
    }

    [Fact]
    public void ClickSaveAndAddAnotherButton_ShouldTriggerCallback()
    {
        // Arrange
        var callbackInvoked = false;

        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, false)
            .Add(p => p.ConceptName, "Test Concept")
            .Add(p => p.OnSaveAndAddAnotherClick, EventCallback.Factory.Create(
                this, () => callbackInvoked = true)));

        // Act
        var button = cut.Find("button.btn-success");
        button.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void SaveAndAddAnotherButton_ShouldShowCtrlEnterTooltip()
    {
        // Act
        var cut = RenderComponent<ConceptEditor>(parameters => parameters
            .Add(p => p.IsEditing, false)
            .Add(p => p.ConceptName, "Test Concept"));

        // Assert
        var button = cut.Find("button.btn-success");
        var title = button.GetAttribute("title");
        Assert.NotNull(title);
        Assert.Contains("Ctrl+Enter", title);
    }
}
