using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;

namespace Eidos.Services;

/// <summary>
/// Service for managing user groups and group memberships
/// </summary>
public class UserGroupService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly ILogger<UserGroupService> _logger;

    public UserGroupService(
        IDbContextFactory<OntologyDbContext> contextFactory,
        ILogger<UserGroupService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Create a new user group
    /// </summary>
    public async Task<UserGroup> CreateGroupAsync(string name, string? description, string createdByUserId, string? color = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var group = new UserGroup
        {
            Name = name,
            Description = description,
            CreatedByUserId = createdByUserId,
            Color = color,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.UserGroups.Add(group);
        await context.SaveChangesAsync();

        _logger.LogInformation("Created user group {GroupId} '{GroupName}' by user {UserId}",
            group.Id, group.Name, createdByUserId);

        return group;
    }

    /// <summary>
    /// Add a user to a group
    /// </summary>
    public async Task<UserGroupMember> AddUserToGroupAsync(int groupId, string userId, string addedByUserId, bool isGroupAdmin = false)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Check if user is already in the group
        var existingMember = await context.UserGroupMembers
            .FirstOrDefaultAsync(m => m.UserGroupId == groupId && m.UserId == userId);

        if (existingMember != null)
        {
            _logger.LogWarning("User {UserId} is already a member of group {GroupId}", userId, groupId);
            return existingMember;
        }

        var member = new UserGroupMember
        {
            UserGroupId = groupId,
            UserId = userId,
            AddedByUserId = addedByUserId,
            JoinedAt = DateTime.UtcNow,
            IsGroupAdmin = isGroupAdmin
        };

        context.UserGroupMembers.Add(member);

        // Update the group's UpdatedAt timestamp
        var group = await context.UserGroups.FindAsync(groupId);
        if (group != null)
        {
            group.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Added user {UserId} to group {GroupId} by {AddedByUserId}",
            userId, groupId, addedByUserId);

        return member;
    }

    /// <summary>
    /// Remove a user from a group
    /// </summary>
    public async Task<bool> RemoveUserFromGroupAsync(int groupId, string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var member = await context.UserGroupMembers
            .FirstOrDefaultAsync(m => m.UserGroupId == groupId && m.UserId == userId);

        if (member == null)
        {
            _logger.LogWarning("User {UserId} is not a member of group {GroupId}", userId, groupId);
            return false;
        }

        context.UserGroupMembers.Remove(member);

        // Update the group's UpdatedAt timestamp
        var group = await context.UserGroups.FindAsync(groupId);
        if (group != null)
        {
            group.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Removed user {UserId} from group {GroupId}", userId, groupId);

        return true;
    }

    /// <summary>
    /// Grant a group permission to access an ontology
    /// </summary>
    public async Task<OntologyGroupPermission> GrantGroupPermissionAsync(
        int ontologyId,
        int groupId,
        string permissionLevel,
        string grantedByUserId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Check if permission already exists
        var existingPermission = await context.OntologyGroupPermissions
            .FirstOrDefaultAsync(p => p.OntologyId == ontologyId && p.UserGroupId == groupId);

        if (existingPermission != null)
        {
            // Update existing permission
            existingPermission.PermissionLevel = permissionLevel;
            existingPermission.GrantedAt = DateTime.UtcNow;
            existingPermission.GrantedByUserId = grantedByUserId;

            await context.SaveChangesAsync();

            _logger.LogInformation("Updated permission for group {GroupId} on ontology {OntologyId} to {PermissionLevel}",
                groupId, ontologyId, permissionLevel);

            return existingPermission;
        }

        var permission = new OntologyGroupPermission
        {
            OntologyId = ontologyId,
            UserGroupId = groupId,
            PermissionLevel = permissionLevel,
            GrantedAt = DateTime.UtcNow,
            GrantedByUserId = grantedByUserId
        };

        context.OntologyGroupPermissions.Add(permission);
        await context.SaveChangesAsync();

        _logger.LogInformation("Granted {PermissionLevel} permission to group {GroupId} for ontology {OntologyId}",
            permissionLevel, groupId, ontologyId);

        return permission;
    }

    /// <summary>
    /// Revoke a group's permission to access an ontology
    /// </summary>
    public async Task<bool> RevokeGroupPermissionAsync(int ontologyId, int groupId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var permission = await context.OntologyGroupPermissions
            .FirstOrDefaultAsync(p => p.OntologyId == ontologyId && p.UserGroupId == groupId);

        if (permission == null)
        {
            _logger.LogWarning("No permission found for group {GroupId} on ontology {OntologyId}", groupId, ontologyId);
            return false;
        }

        context.OntologyGroupPermissions.Remove(permission);
        await context.SaveChangesAsync();

        _logger.LogInformation("Revoked permission for group {GroupId} on ontology {OntologyId}", groupId, ontologyId);

        return true;
    }

    /// <summary>
    /// Get all members of a group
    /// </summary>
    public async Task<List<UserGroupMember>> GetGroupMembersAsync(int groupId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.UserGroupMembers
            .Include(m => m.User)
            .Include(m => m.AddedByUser)
            .Where(m => m.UserGroupId == groupId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all groups a user belongs to
    /// </summary>
    public async Task<List<UserGroup>> GetUserGroupsAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.UserGroupMembers
            .Include(m => m.UserGroup)
            .Where(m => m.UserId == userId && m.UserGroup.IsActive)
            .Select(m => m.UserGroup)
            .ToListAsync();
    }

    /// <summary>
    /// Check if a user is a member of a group
    /// </summary>
    public async Task<bool> IsUserInGroupAsync(int groupId, string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.UserGroupMembers
            .AnyAsync(m => m.UserGroupId == groupId && m.UserId == userId);
    }

    /// <summary>
    /// Check if a user is a group admin
    /// </summary>
    public async Task<bool> IsUserGroupAdminAsync(int groupId, string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.UserGroupMembers
            .AnyAsync(m => m.UserGroupId == groupId && m.UserId == userId && m.IsGroupAdmin);
    }

    /// <summary>
    /// Get a group by ID
    /// </summary>
    public async Task<UserGroup?> GetGroupByIdAsync(int groupId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        return await context.UserGroups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.OntologyPermissions)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    /// <summary>
    /// Delete a group (only if the user is the creator or an admin)
    /// </summary>
    public async Task<bool> DeleteGroupAsync(int groupId, string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var group = await context.UserGroups.FindAsync(groupId);

        if (group == null)
        {
            return false;
        }

        // Only the creator can delete the group
        if (group.CreatedByUserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete group {GroupId} but is not the creator",
                userId, groupId);
            return false;
        }

        // Mark as inactive instead of deleting (soft delete)
        group.IsActive = false;
        group.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Deactivated group {GroupId} by user {UserId}", groupId, userId);

        return true;
    }

    /// <summary>
    /// Update group admin status for a member
    /// </summary>
    public async Task<bool> UpdateMemberAdminStatusAsync(int groupId, string userId, bool isAdmin, string updatedByUserId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var group = await context.UserGroups.FindAsync(groupId);
        if (group == null || group.CreatedByUserId != updatedByUserId)
        {
            _logger.LogWarning("User {UpdatedByUserId} not authorized to update admin status in group {GroupId}",
                updatedByUserId, groupId);
            return false;
        }

        var member = await context.UserGroupMembers
            .FirstOrDefaultAsync(m => m.UserGroupId == groupId && m.UserId == userId);

        if (member == null)
        {
            return false;
        }

        member.IsGroupAdmin = isAdmin;
        await context.SaveChangesAsync();

        _logger.LogInformation("Updated admin status for user {UserId} in group {GroupId} to {IsAdmin}",
            userId, groupId, isAdmin);

        return true;
    }
}
