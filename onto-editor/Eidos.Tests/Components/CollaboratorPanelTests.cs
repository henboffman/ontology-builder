using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Eidos.Components.Shared;
using Eidos.Models.DTOs;
using Eidos.Models.Enums;
using Eidos.Services.Interfaces;

namespace Eidos.Tests.Components;

public class CollaboratorPanelTests : TestContext
{
    private readonly Mock<IOntologyShareService> _shareServiceMock;
    private readonly Mock<ILogger<CollaboratorPanel>> _loggerMock;

    public CollaboratorPanelTests()
    {
        _shareServiceMock = new Mock<IOntologyShareService>();
        _loggerMock = new Mock<ILogger<CollaboratorPanel>>();

        Services.AddSingleton(_shareServiceMock.Object);
        Services.AddSingleton(_loggerMock.Object);
    }

    [Fact]
    public void CollaboratorPanel_ShowsLoadingState_Initially()
    {
        // Arrange
        _shareServiceMock
            .Setup(s => s.GetCollaboratorsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<CollaboratorInfo>());

        // Act
        var cut = RenderComponent<CollaboratorPanel>(parameters => parameters
            .Add(p => p.OntologyId, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var spinner = cut.Find(".spinner-border");
            Assert.NotNull(spinner);
        }, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CollaboratorPanel_DisplaysEmptyState_WhenNoCollaborators()
    {
        // Arrange
        _shareServiceMock
            .Setup(s => s.GetCollaboratorsAsync(1, It.IsAny<int>()))
            .ReturnsAsync(new List<CollaboratorInfo>());

        // Act
        var cut = RenderComponent<CollaboratorPanel>(parameters => parameters
            .Add(p => p.OntologyId, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var emptyMessage = cut.Find(".text-muted");
            Assert.Contains("No collaborators", emptyMessage.TextContent);
        });
    }

    [Fact]
    public void CollaboratorPanel_DisplaysCollaborators_WhenDataExists()
    {
        // Arrange
        var collaborators = new List<CollaboratorInfo>
        {
            new CollaboratorInfo
            {
                UserId = "user-123",
                Name = "Test User",
                Email = "test@example.com",
                PermissionLevel = PermissionLevel.ViewAddEdit,
                FirstAccessedAt = DateTime.UtcNow.AddDays(-5),
                LastAccessedAt = DateTime.UtcNow.AddHours(-2),
                AccessCount = 10,
                IsGuest = false,
                EditStats = new CollaboratorEditStats
                {
                    TotalEdits = 15,
                    ConceptsCreated = 5,
                    ConceptsUpdated = 3,
                    RelationshipsCreated = 7
                },
                RecentActivities = new List<CollaboratorActivity>
                {
                    new CollaboratorActivity
                    {
                        Id = 1,
                        ActivityType = "create",
                        EntityType = "concept",
                        EntityName = "TestConcept",
                        Description = "Created concept TestConcept",
                        CreatedAt = DateTime.UtcNow.AddHours(-1)
                    }
                }
            }
        };

        _shareServiceMock
            .Setup(s => s.GetCollaboratorsAsync(1, It.IsAny<int>()))
            .ReturnsAsync(collaborators);

        // Act
        var cut = RenderComponent<CollaboratorPanel>(parameters => parameters
            .Add(p => p.OntologyId, 1)
            .Add(p => p.ShowDetails, true)
            .Add(p => p.ShowActivity, true));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var collaboratorCard = cut.Find(".collaborator-card");
            Assert.NotNull(collaboratorCard);
            Assert.Contains("Test User", cut.Markup);
            Assert.Contains("test@example.com", cut.Markup);
        });
    }

    [Fact]
    public void CollaboratorPanel_DisplaysGuestCollaborator_Correctly()
    {
        // Arrange
        var collaborators = new List<CollaboratorInfo>
        {
            new CollaboratorInfo
            {
                UserId = null,
                Name = "Guest User",
                Email = null,
                PermissionLevel = PermissionLevel.View,
                FirstAccessedAt = DateTime.UtcNow.AddDays(-1),
                LastAccessedAt = DateTime.UtcNow.AddHours(-1),
                AccessCount = 1,
                IsGuest = true,
                EditStats = new CollaboratorEditStats { TotalEdits = 0 },
                RecentActivities = new List<CollaboratorActivity>()
            }
        };

        _shareServiceMock
            .Setup(s => s.GetCollaboratorsAsync(1, It.IsAny<int>()))
            .ReturnsAsync(collaborators);

        // Act
        var cut = RenderComponent<CollaboratorPanel>(parameters => parameters
            .Add(p => p.OntologyId, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Guest User", cut.Markup);
            Assert.Contains("Guest", cut.Markup); // Guest badge
            Assert.Contains("View Only", cut.Markup); // Permission level
        });
    }

    [Fact]
    public void CollaboratorPanel_DisplaysCorrectPermissionBadge()
    {
        // Arrange
        var collaborators = new List<CollaboratorInfo>
        {
            new CollaboratorInfo
            {
                UserId = "user-123",
                Name = "Full Access User",
                Email = "admin@example.com",
                PermissionLevel = PermissionLevel.FullAccess,
                FirstAccessedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = 1,
                IsGuest = false,
                EditStats = new CollaboratorEditStats(),
                RecentActivities = new List<CollaboratorActivity>()
            }
        };

        _shareServiceMock
            .Setup(s => s.GetCollaboratorsAsync(1, It.IsAny<int>()))
            .ReturnsAsync(collaborators);

        // Act
        var cut = RenderComponent<CollaboratorPanel>(parameters => parameters
            .Add(p => p.OntologyId, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Full Access", cut.Markup);
            Assert.Contains("bg-success", cut.Markup); // Full access uses success badge
        });
    }

    [Fact]
    public void CollaboratorPanel_DisplaysEditStatistics_WhenShowDetailsIsTrue()
    {
        // Arrange
        var collaborators = new List<CollaboratorInfo>
        {
            new CollaboratorInfo
            {
                UserId = "user-123",
                Name = "Active User",
                Email = "active@example.com",
                PermissionLevel = PermissionLevel.ViewAddEdit,
                FirstAccessedAt = DateTime.UtcNow.AddDays(-10),
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = 50,
                IsGuest = false,
                EditStats = new CollaboratorEditStats
                {
                    TotalEdits = 25,
                    ConceptsCreated = 10,
                    ConceptsUpdated = 5,
                    ConceptsDeleted = 2,
                    RelationshipsCreated = 8
                },
                RecentActivities = new List<CollaboratorActivity>()
            }
        };

        _shareServiceMock
            .Setup(s => s.GetCollaboratorsAsync(1, It.IsAny<int>()))
            .ReturnsAsync(collaborators);

        // Act
        var cut = RenderComponent<CollaboratorPanel>(parameters => parameters
            .Add(p => p.OntologyId, 1)
            .Add(p => p.ShowDetails, true));

        cut.WaitForState(() => !cut.Instance.GetType()
            .GetProperty("IsLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(cut.Instance) as bool? ?? true);

        // Click to expand details
        var expandButton = cut.Find(".btn-outline-primary");
        expandButton.Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("25", cut.Markup); // Total edits
            Assert.Contains("50", cut.Markup); // Access count
        });
    }

    [Fact]
    public void CollaboratorPanel_DisplaysRecentActivity_WhenShowActivityIsTrue()
    {
        // Arrange
        var collaborators = new List<CollaboratorInfo>
        {
            new CollaboratorInfo
            {
                UserId = "user-123",
                Name = "Active User",
                Email = "active@example.com",
                PermissionLevel = PermissionLevel.ViewAddEdit,
                FirstAccessedAt = DateTime.UtcNow.AddDays(-10),
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = 50,
                IsGuest = false,
                EditStats = new CollaboratorEditStats { TotalEdits = 5 },
                RecentActivities = new List<CollaboratorActivity>
                {
                    new CollaboratorActivity
                    {
                        Id = 1,
                        ActivityType = "create",
                        EntityType = "concept",
                        EntityName = "NewConcept",
                        Description = "Created concept NewConcept",
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    },
                    new CollaboratorActivity
                    {
                        Id = 2,
                        ActivityType = "update",
                        EntityType = "relationship",
                        EntityName = "TestRelationship",
                        Description = "Updated relationship TestRelationship",
                        CreatedAt = DateTime.UtcNow.AddHours(-1)
                    }
                }
            }
        };

        _shareServiceMock
            .Setup(s => s.GetCollaboratorsAsync(1, It.IsAny<int>()))
            .ReturnsAsync(collaborators);

        // Act
        var cut = RenderComponent<CollaboratorPanel>(parameters => parameters
            .Add(p => p.OntologyId, 1)
            .Add(p => p.ShowDetails, true)
            .Add(p => p.ShowActivity, true));

        cut.WaitForState(() => !cut.Instance.GetType()
            .GetProperty("IsLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(cut.Instance) as bool? ?? true);

        // Click to expand details
        var expandButton = cut.Find(".btn-outline-primary");
        expandButton.Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Created concept NewConcept", cut.Markup);
            Assert.Contains("Updated relationship TestRelationship", cut.Markup);
        });
    }

    [Fact]
    public void CollaboratorPanel_ShowsErrorMessage_WhenServiceFails()
    {
        // Arrange
        _shareServiceMock
            .Setup(s => s.GetCollaboratorsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var cut = RenderComponent<CollaboratorPanel>(parameters => parameters
            .Add(p => p.OntologyId, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            var errorAlert = cut.Find(".alert-danger");
            Assert.NotNull(errorAlert);
            Assert.Contains("Failed to load collaborators", errorAlert.TextContent);
        });
    }

    [Fact]
    public void CollaboratorPanel_CallsServiceWithCorrectParameters()
    {
        // Arrange
        _shareServiceMock
            .Setup(s => s.GetCollaboratorsAsync(123, 15))
            .ReturnsAsync(new List<CollaboratorInfo>());

        // Act
        var cut = RenderComponent<CollaboratorPanel>(parameters => parameters
            .Add(p => p.OntologyId, 123)
            .Add(p => p.RecentActivityLimit, 15));

        // Assert
        cut.WaitForAssertion(() =>
        {
            _shareServiceMock.Verify(
                s => s.GetCollaboratorsAsync(123, 15),
                Times.Once
            );
        });
    }
}
