using Microsoft.EntityFrameworkCore;
using Xunit;
using Eidos.Data;
using Eidos.Models;
using Eidos.Services;

namespace Eidos.Tests.Integration.Services;

public class OntologyPermissionServiceTests : IDisposable
{
    private readonly OntologyDbContext _context;
    private readonly OntologyPermissionService _service;

    public OntologyPermissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<OntologyDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new OntologyDbContext(options);
        _service = new OntologyPermissionService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CanViewAsync Tests

    [Fact]
    public async Task CanViewAsync_OwnerCanView_ReturnsTrue()
    {
        // Arrange
        var userId = "user1";
        var ontology = new Ontology
        {
            Name = "Test Ontology",
            UserId = userId,
            Visibility = OntologyVisibility.Private,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanViewAsync(ontology.Id, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanViewAsync_PublicOntology_AnyoneCanView()
    {
        // Arrange
        var ontology = new Ontology
        {
            Name = "Public Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Public,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        // Act
        var resultAuthenticated = await _service.CanViewAsync(ontology.Id, "otherUser");
        var resultAnonymous = await _service.CanViewAsync(ontology.Id, null);

        // Assert
        Assert.True(resultAuthenticated);
        Assert.True(resultAnonymous);
    }

    [Fact]
    public async Task CanViewAsync_PrivateOntology_OnlyOwnerCanView()
    {
        // Arrange
        var ontology = new Ontology
        {
            Name = "Private Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Private,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        // Act
        var ownerResult = await _service.CanViewAsync(ontology.Id, "owner");
        var otherResult = await _service.CanViewAsync(ontology.Id, "otherUser");
        var anonymousResult = await _service.CanViewAsync(ontology.Id, null);

        // Assert
        Assert.True(ownerResult);
        Assert.False(otherResult);
        Assert.False(anonymousResult);
    }

    [Fact]
    public async Task CanViewAsync_GroupOntology_GroupMemberCanView()
    {
        // Arrange
        var userId = "user1";
        var group = new UserGroup
        {
            Name = "Test Group",
            CreatedByUserId = "admin",
            CreatedAt = DateTime.UtcNow
        };
        _context.UserGroups.Add(group);
        await _context.SaveChangesAsync();

        var membership = new UserGroupMember
        {
            UserGroupId = group.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
        _context.UserGroupMembers.Add(membership);

        var ontology = new Ontology
        {
            Name = "Group Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Group,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        var permission = new OntologyGroupPermission
        {
            OntologyId = ontology.Id,
            UserGroupId = group.Id,
            PermissionLevel = PermissionLevels.View,
            GrantedByUserId = "owner",
            GrantedAt = DateTime.UtcNow
        };
        _context.OntologyGroupPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        var memberResult = await _service.CanViewAsync(ontology.Id, userId);
        var nonMemberResult = await _service.CanViewAsync(ontology.Id, "otherUser");

        // Assert
        Assert.True(memberResult);
        Assert.False(nonMemberResult);
    }

    #endregion

    #region CanEditAsync Tests

    [Fact]
    public async Task CanEditAsync_Owner_CanEdit()
    {
        // Arrange
        var userId = "user1";
        var ontology = new Ontology
        {
            Name = "Test Ontology",
            UserId = userId,
            Visibility = OntologyVisibility.Private,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanEditAsync(ontology.Id, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanEditAsync_PublicWithAllowEdit_AnyoneCanEdit()
    {
        // Arrange
        var ontology = new Ontology
        {
            Name = "Public Editable",
            UserId = "owner",
            Visibility = OntologyVisibility.Public,
            AllowPublicEdit = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanEditAsync(ontology.Id, "otherUser");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanEditAsync_PublicWithoutAllowEdit_OnlyOwnerCanEdit()
    {
        // Arrange
        var ontology = new Ontology
        {
            Name = "Public View-Only",
            UserId = "owner",
            Visibility = OntologyVisibility.Public,
            AllowPublicEdit = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        // Act
        var ownerResult = await _service.CanEditAsync(ontology.Id, "owner");
        var otherResult = await _service.CanEditAsync(ontology.Id, "otherUser");

        // Assert
        Assert.True(ownerResult);
        Assert.False(otherResult);
    }

    [Fact]
    public async Task CanEditAsync_GroupMemberWithEditPermission_CanEdit()
    {
        // Arrange
        var userId = "user1";
        var group = new UserGroup
        {
            Name = "Editors",
            CreatedByUserId = "admin",
            CreatedAt = DateTime.UtcNow
        };
        _context.UserGroups.Add(group);
        await _context.SaveChangesAsync();

        var membership = new UserGroupMember
        {
            UserGroupId = group.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
        _context.UserGroupMembers.Add(membership);

        var ontology = new Ontology
        {
            Name = "Group Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Group,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        var permission = new OntologyGroupPermission
        {
            OntologyId = ontology.Id,
            UserGroupId = group.Id,
            PermissionLevel = PermissionLevels.Edit,
            GrantedByUserId = "owner",
            GrantedAt = DateTime.UtcNow
        };
        _context.OntologyGroupPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanEditAsync(ontology.Id, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanEditAsync_GroupMemberWithViewPermission_CannotEdit()
    {
        // Arrange
        var userId = "user1";
        var group = new UserGroup
        {
            Name = "Viewers",
            CreatedByUserId = "admin",
            CreatedAt = DateTime.UtcNow
        };
        _context.UserGroups.Add(group);
        await _context.SaveChangesAsync();

        var membership = new UserGroupMember
        {
            UserGroupId = group.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
        _context.UserGroupMembers.Add(membership);

        var ontology = new Ontology
        {
            Name = "Group Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Group,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        var permission = new OntologyGroupPermission
        {
            OntologyId = ontology.Id,
            UserGroupId = group.Id,
            PermissionLevel = PermissionLevels.View,
            GrantedByUserId = "owner",
            GrantedAt = DateTime.UtcNow
        };
        _context.OntologyGroupPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanEditAsync(ontology.Id, userId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CanManageAsync Tests

    [Fact]
    public async Task CanManageAsync_Owner_CanManage()
    {
        // Arrange
        var userId = "user1";
        var ontology = new Ontology
        {
            Name = "Test Ontology",
            UserId = userId,
            Visibility = OntologyVisibility.Private,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanManageAsync(ontology.Id, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanManageAsync_GroupAdminPermission_CanManage()
    {
        // Arrange
        var userId = "user1";
        var group = new UserGroup
        {
            Name = "Admins",
            CreatedByUserId = "superadmin",
            CreatedAt = DateTime.UtcNow
        };
        _context.UserGroups.Add(group);
        await _context.SaveChangesAsync();

        var membership = new UserGroupMember
        {
            UserGroupId = group.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
        _context.UserGroupMembers.Add(membership);

        var ontology = new Ontology
        {
            Name = "Group Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Group,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        var permission = new OntologyGroupPermission
        {
            OntologyId = ontology.Id,
            UserGroupId = group.Id,
            PermissionLevel = PermissionLevels.Admin,
            GrantedByUserId = "owner",
            GrantedAt = DateTime.UtcNow
        };
        _context.OntologyGroupPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanManageAsync(ontology.Id, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanManageAsync_GroupEditPermission_CannotManage()
    {
        // Arrange
        var userId = "user1";
        var group = new UserGroup
        {
            Name = "Editors",
            CreatedByUserId = "admin",
            CreatedAt = DateTime.UtcNow
        };
        _context.UserGroups.Add(group);
        await _context.SaveChangesAsync();

        var membership = new UserGroupMember
        {
            UserGroupId = group.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
        _context.UserGroupMembers.Add(membership);

        var ontology = new Ontology
        {
            Name = "Group Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Group,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        var permission = new OntologyGroupPermission
        {
            OntologyId = ontology.Id,
            UserGroupId = group.Id,
            PermissionLevel = PermissionLevels.Edit,
            GrantedByUserId = "owner",
            GrantedAt = DateTime.UtcNow
        };
        _context.OntologyGroupPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CanManageAsync(ontology.Id, userId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Group Permission Management Tests

    [Fact]
    public async Task GrantGroupAccessAsync_NewPermission_CreatesPermission()
    {
        // Arrange
        var group = new UserGroup
        {
            Name = "Test Group",
            CreatedByUserId = "admin",
            CreatedAt = DateTime.UtcNow
        };
        _context.UserGroups.Add(group);

        var ontology = new Ontology
        {
            Name = "Test Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Group,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        // Act
        await _service.GrantGroupAccessAsync(ontology.Id, group.Id, PermissionLevels.Edit, "owner");

        // Assert
        var permission = await _context.OntologyGroupPermissions
            .FirstOrDefaultAsync(p => p.OntologyId == ontology.Id && p.UserGroupId == group.Id);

        Assert.NotNull(permission);
        Assert.Equal(PermissionLevels.Edit, permission.PermissionLevel);
        Assert.Equal("owner", permission.GrantedByUserId);
    }

    [Fact]
    public async Task GrantGroupAccessAsync_ExistingPermission_UpdatesPermission()
    {
        // Arrange
        var group = new UserGroup
        {
            Name = "Test Group",
            CreatedByUserId = "admin",
            CreatedAt = DateTime.UtcNow
        };
        _context.UserGroups.Add(group);

        var ontology = new Ontology
        {
            Name = "Test Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Group,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);

        var permission = new OntologyGroupPermission
        {
            OntologyId = ontology.Id,
            UserGroupId = group.Id,
            PermissionLevel = PermissionLevels.View,
            GrantedByUserId = "owner",
            GrantedAt = DateTime.UtcNow
        };
        _context.OntologyGroupPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        await _service.GrantGroupAccessAsync(ontology.Id, group.Id, PermissionLevels.Admin, "owner");

        // Assert
        var updatedPermission = await _context.OntologyGroupPermissions
            .FirstOrDefaultAsync(p => p.OntologyId == ontology.Id && p.UserGroupId == group.Id);

        Assert.NotNull(updatedPermission);
        Assert.Equal(PermissionLevels.Admin, updatedPermission.PermissionLevel);
    }

    [Fact]
    public async Task RevokeGroupAccessAsync_ExistingPermission_RemovesPermission()
    {
        // Arrange
        var group = new UserGroup
        {
            Name = "Test Group",
            CreatedByUserId = "admin",
            CreatedAt = DateTime.UtcNow
        };
        _context.UserGroups.Add(group);

        var ontology = new Ontology
        {
            Name = "Test Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Group,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);

        var permission = new OntologyGroupPermission
        {
            OntologyId = ontology.Id,
            UserGroupId = group.Id,
            PermissionLevel = PermissionLevels.View,
            GrantedByUserId = "owner",
            GrantedAt = DateTime.UtcNow
        };
        _context.OntologyGroupPermissions.Add(permission);
        await _context.SaveChangesAsync();

        // Act
        await _service.RevokeGroupAccessAsync(ontology.Id, group.Id);

        // Assert
        var removedPermission = await _context.OntologyGroupPermissions
            .FirstOrDefaultAsync(p => p.OntologyId == ontology.Id && p.UserGroupId == group.Id);

        Assert.Null(removedPermission);
    }

    [Fact]
    public async Task GetGroupPermissionsAsync_ReturnsAllPermissions()
    {
        // Arrange
        var group1 = new UserGroup { Name = "Group 1", CreatedByUserId = "admin", CreatedAt = DateTime.UtcNow };
        var group2 = new UserGroup { Name = "Group 2", CreatedByUserId = "admin", CreatedAt = DateTime.UtcNow };
        _context.UserGroups.AddRange(group1, group2);

        var ontology = new Ontology
        {
            Name = "Test Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Group,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        var permission1 = new OntologyGroupPermission
        {
            OntologyId = ontology.Id,
            UserGroupId = group1.Id,
            PermissionLevel = PermissionLevels.View,
            GrantedByUserId = "owner",
            GrantedAt = DateTime.UtcNow
        };
        var permission2 = new OntologyGroupPermission
        {
            OntologyId = ontology.Id,
            UserGroupId = group2.Id,
            PermissionLevel = PermissionLevels.Edit,
            GrantedByUserId = "owner",
            GrantedAt = DateTime.UtcNow
        };
        _context.OntologyGroupPermissions.AddRange(permission1, permission2);
        await _context.SaveChangesAsync();

        // Act
        var permissions = await _service.GetGroupPermissionsAsync(ontology.Id);

        // Assert
        Assert.Equal(2, permissions.Count);
        Assert.Contains(permissions, p => p.UserGroup.Name == "Group 1" && p.PermissionLevel == PermissionLevels.View);
        Assert.Contains(permissions, p => p.UserGroup.Name == "Group 2" && p.PermissionLevel == PermissionLevels.Edit);
    }

    #endregion

    #region Visibility Management Tests

    [Fact]
    public async Task UpdateVisibilityAsync_UpdatesOntologyVisibility()
    {
        // Arrange
        var ontology = new Ontology
        {
            Name = "Test Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Private,
            AllowPublicEdit = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = ontology.UpdatedAt;

        // Act
        await Task.Delay(10); // Ensure time difference
        await _service.UpdateVisibilityAsync(ontology.Id, OntologyVisibility.Public, true);

        // Assert
        var updated = await _context.Ontologies.FindAsync(ontology.Id);
        Assert.NotNull(updated);
        Assert.Equal(OntologyVisibility.Public, updated.Visibility);
        Assert.True(updated.AllowPublicEdit);
        Assert.True(updated.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateVisibilityAsync_InvalidVisibility_ThrowsException()
    {
        // Arrange
        var ontology = new Ontology
        {
            Name = "Test Ontology",
            UserId = "owner",
            Visibility = OntologyVisibility.Private,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.Add(ontology);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.UpdateVisibilityAsync(ontology.Id, "invalid", false));
    }

    #endregion

    #region GetAccessibleOntologiesAsync Tests

    [Fact]
    public async Task GetAccessibleOntologiesAsync_ReturnsOwnedAndPublicOntologies()
    {
        // Arrange
        var userId = "user1";
        var ownedOntology = new Ontology
        {
            Name = "My Ontology",
            UserId = userId,
            Visibility = OntologyVisibility.Private,
            CreatedAt = DateTime.UtcNow
        };
        var publicOntology = new Ontology
        {
            Name = "Public Ontology",
            UserId = "other",
            Visibility = OntologyVisibility.Public,
            CreatedAt = DateTime.UtcNow
        };
        var privateOntology = new Ontology
        {
            Name = "Other's Private",
            UserId = "other",
            Visibility = OntologyVisibility.Private,
            CreatedAt = DateTime.UtcNow
        };
        _context.Ontologies.AddRange(ownedOntology, publicOntology, privateOntology);
        await _context.SaveChangesAsync();

        // Act
        var accessible = await _service.GetAccessibleOntologiesAsync(userId);

        // Assert
        Assert.Equal(2, accessible.Count);
        Assert.Contains(accessible, o => o.Name == "My Ontology");
        Assert.Contains(accessible, o => o.Name == "Public Ontology");
        Assert.DoesNotContain(accessible, o => o.Name == "Other's Private");
    }

    #endregion
}
