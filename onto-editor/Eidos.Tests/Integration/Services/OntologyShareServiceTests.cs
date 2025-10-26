using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Eidos.Data;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services;

namespace Eidos.Tests.Integration.Services;

public class OntologyShareServiceTests : IDisposable
{
    private readonly OntologyDbContext _context;
    private readonly Mock<ILogger<OntologyShareService>> _loggerMock;
    private readonly OntologyShareService _service;

    public OntologyShareServiceTests()
    {
        var options = new DbContextOptionsBuilder<OntologyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OntologyDbContext(options);
        _loggerMock = new Mock<ILogger<OntologyShareService>>();

        var contextFactory = new Mock<IDbContextFactory<OntologyDbContext>>();
        contextFactory.Setup(f => f.CreateDbContextAsync(default))
            .ReturnsAsync(_context);

        _service = new OntologyShareService(contextFactory.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetCollaboratorsAsync_ReturnsEmptyList_WhenNoCollaborators()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            UserId = "owner-123"
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCollaboratorsAsync(ontology.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCollaboratorsAsync_ReturnsAuthenticatedCollaborators()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = "testuser",
            Email = "test@example.com"
        };
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            UserId = "owner-123"
        };
        var share = new OntologyShare
        {
            Id = 1,
            OntologyId = ontology.Id,
            CreatedByUserId = "owner-123",
            ShareToken = "test-token",
            PermissionLevel = PermissionLevel.ViewAddEdit,
            AllowGuestAccess = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var userAccess = new UserShareAccess
        {
            UserId = user.Id,
            OntologyShareId = share.Id,
            FirstAccessedAt = DateTime.UtcNow.AddDays(-5),
            LastAccessedAt = DateTime.UtcNow.AddHours(-2),
            AccessCount = 10
        };

        _context.Users.Add(user);
        _context.Ontologies.Add(ontology);
        _context.OntologyShares.Add(share);
        _context.UserShareAccesses.Add(userAccess);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCollaboratorsAsync(ontology.Id);

        // Assert
        Assert.Single(result);
        var collaborator = result.First();
        Assert.Equal(user.Id, collaborator.UserId);
        Assert.Equal(user.Email, collaborator.Email);
        Assert.Equal(PermissionLevel.ViewAddEdit, collaborator.PermissionLevel);
        Assert.Equal(10, collaborator.AccessCount);
        Assert.False(collaborator.IsGuest);
    }

    [Fact]
    public async Task GetCollaboratorsAsync_ReturnsGuestCollaborators()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            UserId = "owner-123"
        };
        var share = new OntologyShare
        {
            Id = 1,
            OntologyId = ontology.Id,
            CreatedByUserId = "owner-123",
            ShareToken = "test-token",
            PermissionLevel = PermissionLevel.View,
            AllowGuestAccess = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var guestSession = new GuestSession
        {
            Id = 1,
            OntologyShareId = share.Id,
            SessionToken = "guest-token",
            GuestName = "Guest User",
            IpAddress = "192.168.1.1",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastActivityAt = DateTime.UtcNow.AddHours(-1),
            IsActive = true
        };

        _context.Ontologies.Add(ontology);
        _context.OntologyShares.Add(share);
        _context.GuestSessions.Add(guestSession);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCollaboratorsAsync(ontology.Id);

        // Assert
        Assert.Single(result);
        var collaborator = result.First();
        Assert.Null(collaborator.UserId);
        Assert.Equal("Guest User", collaborator.Name);
        Assert.Null(collaborator.Email);
        Assert.Equal(PermissionLevel.View, collaborator.PermissionLevel);
        Assert.True(collaborator.IsGuest);
    }

    [Fact]
    public async Task GetCollaboratorsAsync_IncludesActivityHistory()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = "testuser",
            Email = "test@example.com"
        };
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            UserId = "owner-123"
        };
        var share = new OntologyShare
        {
            Id = 1,
            OntologyId = ontology.Id,
            CreatedByUserId = "owner-123",
            ShareToken = "test-token",
            PermissionLevel = PermissionLevel.ViewAddEdit,
            AllowGuestAccess = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var userAccess = new UserShareAccess
        {
            UserId = user.Id,
            OntologyShareId = share.Id,
            FirstAccessedAt = DateTime.UtcNow.AddDays(-5),
            LastAccessedAt = DateTime.UtcNow,
            AccessCount = 10
        };

        // Add activity records
        var activities = new[]
        {
            new OntologyActivity
            {
                OntologyId = ontology.Id,
                UserId = user.Id,
                ActorName = user.Email!,
                ActivityType = "create",
                EntityType = "concept",
                EntityName = "TestConcept",
                Description = "Created concept TestConcept",
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new OntologyActivity
            {
                OntologyId = ontology.Id,
                UserId = user.Id,
                ActorName = user.Email!,
                ActivityType = "update",
                EntityType = "concept",
                EntityName = "TestConcept",
                Description = "Updated concept TestConcept",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _context.Users.Add(user);
        _context.Ontologies.Add(ontology);
        _context.OntologyShares.Add(share);
        _context.UserShareAccesses.Add(userAccess);
        _context.OntologyActivities.AddRange(activities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCollaboratorsAsync(ontology.Id, recentActivityLimit: 5);

        // Assert
        Assert.Single(result);
        var collaborator = result.First();
        Assert.Equal(2, collaborator.RecentActivities.Count);
        Assert.Equal(2, collaborator.EditStats.TotalEdits);
        Assert.Equal(1, collaborator.EditStats.ConceptsCreated);
        Assert.Equal(1, collaborator.EditStats.ConceptsUpdated);
    }

    [Fact]
    public async Task GetCollaboratorsAsync_ExcludesInactiveShares()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = "testuser",
            Email = "test@example.com"
        };
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            UserId = "owner-123"
        };
        var inactiveShare = new OntologyShare
        {
            Id = 1,
            OntologyId = ontology.Id,
            CreatedByUserId = "owner-123",
            ShareToken = "test-token",
            PermissionLevel = PermissionLevel.ViewAddEdit,
            AllowGuestAccess = true,
            IsActive = false, // Inactive
            CreatedAt = DateTime.UtcNow
        };
        var userAccess = new UserShareAccess
        {
            UserId = user.Id,
            OntologyShareId = inactiveShare.Id,
            FirstAccessedAt = DateTime.UtcNow.AddDays(-5),
            LastAccessedAt = DateTime.UtcNow,
            AccessCount = 10
        };

        _context.Users.Add(user);
        _context.Ontologies.Add(ontology);
        _context.OntologyShares.Add(inactiveShare);
        _context.UserShareAccesses.Add(userAccess);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCollaboratorsAsync(ontology.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserActivityAsync_ReturnsActivityHistory()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            UserId = "owner-123"
        };
        var activities = new[]
        {
            new OntologyActivity
            {
                OntologyId = ontology.Id,
                UserId = "user-123",
                ActorName = "test@example.com",
                ActivityType = "create",
                EntityType = "concept",
                EntityName = "Concept1",
                Description = "Created concept Concept1",
                CreatedAt = DateTime.UtcNow.AddHours(-5),
                VersionNumber = 1
            },
            new OntologyActivity
            {
                OntologyId = ontology.Id,
                UserId = "user-123",
                ActorName = "test@example.com",
                ActivityType = "create",
                EntityType = "relationship",
                EntityName = "Relationship1",
                Description = "Created relationship Relationship1",
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                VersionNumber = 2
            },
            new OntologyActivity
            {
                OntologyId = ontology.Id,
                UserId = "user-123",
                ActorName = "test@example.com",
                ActivityType = "update",
                EntityType = "concept",
                EntityName = "Concept1",
                Description = "Updated concept Concept1",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                VersionNumber = 3
            }
        };

        _context.Ontologies.Add(ontology);
        _context.OntologyActivities.AddRange(activities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserActivityAsync(ontology.Id, "user-123", limit: 10);

        // Assert
        Assert.Equal(3, result.Count);
        // Should be ordered by most recent first
        Assert.Equal("update", result[0].ActivityType);
        Assert.Equal(3, result[0].VersionNumber);
        Assert.Equal("create", result[1].ActivityType);
        Assert.Equal("relationship", result[1].EntityType);
        Assert.Equal("create", result[2].ActivityType);
        Assert.Equal("concept", result[2].EntityType);
    }

    [Fact]
    public async Task GetUserActivityAsync_RespectsLimit()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            UserId = "owner-123"
        };

        // Create 10 activities
        var activities = Enumerable.Range(1, 10).Select(i => new OntologyActivity
        {
            OntologyId = ontology.Id,
            UserId = "user-123",
            ActorName = "test@example.com",
            ActivityType = "create",
            EntityType = "concept",
            EntityName = $"Concept{i}",
            Description = $"Created concept Concept{i}",
            CreatedAt = DateTime.UtcNow.AddHours(-i)
        }).ToList();

        _context.Ontologies.Add(ontology);
        _context.OntologyActivities.AddRange(activities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserActivityAsync(ontology.Id, "user-123", limit: 5);

        // Assert
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetUserActivityAsync_OnlyReturnsActivitiesForSpecifiedUser()
    {
        // Arrange
        var ontology = new Ontology
        {
            Id = 1,
            Name = "Test Ontology",
            UserId = "owner-123"
        };
        var activities = new[]
        {
            new OntologyActivity
            {
                OntologyId = ontology.Id,
                UserId = "user-123",
                ActorName = "user1@example.com",
                ActivityType = "create",
                EntityType = "concept",
                EntityName = "Concept1",
                Description = "Created concept Concept1",
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            },
            new OntologyActivity
            {
                OntologyId = ontology.Id,
                UserId = "user-456", // Different user
                ActorName = "user2@example.com",
                ActivityType = "create",
                EntityType = "concept",
                EntityName = "Concept2",
                Description = "Created concept Concept2",
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            }
        };

        _context.Ontologies.Add(ontology);
        _context.OntologyActivities.AddRange(activities);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserActivityAsync(ontology.Id, "user-123", limit: 10);

        // Assert
        Assert.Single(result);
        Assert.Equal("user-123", activities[0].UserId);
    }
}
