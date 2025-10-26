using Bunit;
using Eidos.Components.Shared;
using Eidos.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Eidos.Tests.Components;

/// <summary>
/// Component tests for ConfirmDialog using bUnit
/// </summary>
public class ConfirmDialogTests : TestContext
{
    private readonly ConfirmService _confirmService;

    public ConfirmDialogTests()
    {
        _confirmService = new ConfirmService();
        Services.AddSingleton(_confirmService);
    }

    [Fact]
    public void InitialRender_ShouldNotDisplayDialog()
    {
        // Act
        var cut = RenderComponent<ConfirmDialog>();

        // Assert
        Assert.DoesNotContain("modal", cut.Markup);
    }

    [Fact]
    public async Task ShowAsync_ShouldDisplayDialog_WithCorrectTitle()
    {
        // Arrange
        var cut = RenderComponent<ConfirmDialog>();

        // Act
        var task = _confirmService.ShowAsync("Delete Ontology", "Are you sure?", "Delete");
        await Task.Delay(50); // Give time for state to update
        cut.Render();

        // Assert
        Assert.Contains("Delete Ontology", cut.Markup);
        Assert.Contains("Are you sure?", cut.Markup);
        Assert.Contains("Delete", cut.Markup);
    }

    [Fact]
    public async Task ShowAsync_WithDangerType_ShouldShowRedHeader()
    {
        // Arrange
        var cut = RenderComponent<ConfirmDialog>();

        // Act
        var task = _confirmService.ShowAsync("Delete", "Confirm delete", "Delete", ConfirmType.Danger);
        await Task.Delay(50);
        cut.Render();

        // Assert
        Assert.Contains("bg-danger", cut.Markup);
        Assert.Contains("bi-exclamation-triangle-fill", cut.Markup);
    }

    [Fact]
    public async Task ShowAsync_WithWarningType_ShouldShowYellowHeader()
    {
        // Arrange
        var cut = RenderComponent<ConfirmDialog>();

        // Act
        var task = _confirmService.ShowAsync("Warning", "This is a warning", "Proceed", ConfirmType.Warning);
        await Task.Delay(50);
        cut.Render();

        // Assert
        Assert.Contains("bg-warning", cut.Markup);
        Assert.Contains("bi-exclamation-circle-fill", cut.Markup);
    }

    [Fact]
    public async Task ShowAsync_WithInfoType_ShouldShowBlueHeader()
    {
        // Arrange
        var cut = RenderComponent<ConfirmDialog>();

        // Act
        var task = _confirmService.ShowAsync("Information", "This is info", "OK", ConfirmType.Info);
        await Task.Delay(50);
        cut.Render();

        // Assert
        Assert.Contains("bg-info", cut.Markup);
        Assert.Contains("bi-info-circle-fill", cut.Markup);
    }

    [Fact]
    public async Task ClickConfirm_ShouldReturnTrue()
    {
        // Arrange
        var cut = RenderComponent<ConfirmDialog>();

        // Act
        var task = _confirmService.ShowAsync("Confirm", "Are you sure?");
        await Task.Delay(50);
        cut.Render();

        var confirmButton = cut.Find("button.btn-danger");
        await confirmButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        var result = await task;

        // Assert
        Assert.True(result);
        Assert.DoesNotContain("modal", cut.Markup); // Dialog should be hidden
    }

    [Fact]
    public async Task ClickCancel_ShouldReturnFalse()
    {
        // Arrange
        var cut = RenderComponent<ConfirmDialog>();

        // Act
        var task = _confirmService.ShowAsync("Confirm", "Are you sure?");
        await Task.Delay(50);
        cut.Render();

        var cancelButton = cut.Find("button.btn-secondary");
        await cancelButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        var result = await task;

        // Assert
        Assert.False(result);
        Assert.DoesNotContain("modal", cut.Markup); // Dialog should be hidden
    }

    [Fact]
    public async Task ClickCloseButton_ShouldReturnFalse()
    {
        // Arrange
        var cut = RenderComponent<ConfirmDialog>();

        // Act
        var task = _confirmService.ShowAsync("Confirm", "Are you sure?");
        await Task.Delay(50);
        cut.Render();

        var closeButton = cut.Find("button.btn-close");
        await closeButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        var result = await task;

        // Assert
        Assert.False(result);
        Assert.DoesNotContain("modal", cut.Markup); // Dialog should be hidden
    }

    [Fact]
    public async Task MultipleDialogs_ShouldShowSequentially()
    {
        // Arrange
        var cut = RenderComponent<ConfirmDialog>();

        // Act - First dialog
        var task1 = _confirmService.ShowAsync("First Dialog", "Message 1");
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("First Dialog", cut.Markup);

        var confirmButton1 = cut.Find("button.btn-danger");
        await confirmButton1.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await task1;

        // Second dialog
        var task2 = _confirmService.ShowAsync("Second Dialog", "Message 2");
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Second Dialog", cut.Markup);
        Assert.DoesNotContain("First Dialog", cut.Markup);

        var cancelButton2 = cut.Find("button.btn-secondary");
        await cancelButton2.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        var result2 = await task2;

        // Assert
        Assert.False(result2);
    }
}
