using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;

namespace Eidos.Services;

/// <summary>
/// Service for managing users, roles, and groups
/// </summary>
public class UserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly OntologyDbContext _context;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        OntologyDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    #region User Operations

    /// <summary>
    /// Get all users with their roles
    /// </summary>
    public async Task<List<UserWithRoles>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var usersWithRoles = new List<UserWithRoles>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            usersWithRoles.Add(new UserWithRoles
            {
                User = user,
                Roles = roles.ToList()
            });
        }

        return usersWithRoles;
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    /// <summary>
    /// Get user roles
    /// </summary>
    public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    /// <summary>
    /// Get user groups
    /// </summary>
    public async Task<List<UserGroup>> GetUserGroupsAsync(string userId)
    {
        return await _context.UserGroupMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.UserGroup)
            .Select(m => m.UserGroup)
            .ToListAsync();
    }

    #endregion

    #region Role Management

    /// <summary>
    /// Get all available roles
    /// </summary>
    public async Task<List<IdentityRole>> GetAllRolesAsync()
    {
        return await _roleManager.Roles.ToListAsync();
    }

    /// <summary>
    /// Assign role to user
    /// </summary>
    public async Task<IdentityResult> AssignRoleAsync(ApplicationUser user, string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = $"Role '{roleName}' does not exist."
            });
        }

        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = $"User already has role '{roleName}'."
            });
        }

        return await _userManager.AddToRoleAsync(user, roleName);
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    public async Task<IdentityResult> RemoveRoleAsync(ApplicationUser user, string roleName)
    {
        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = $"User does not have role '{roleName}'."
            });
        }

        return await _userManager.RemoveFromRoleAsync(user, roleName);
    }

    #endregion

    #region Group Management

    /// <summary>
    /// Get all user groups
    /// </summary>
    public async Task<List<UserGroup>> GetAllGroupsAsync()
    {
        return await _context.UserGroups
            .Include(g => g.CreatedByUser)
            .Include(g => g.Members)
            .ThenInclude(m => m.User)
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get group by ID
    /// </summary>
    public async Task<UserGroup?> GetGroupByIdAsync(int groupId)
    {
        return await _context.UserGroups
            .Include(g => g.CreatedByUser)
            .Include(g => g.Members)
            .ThenInclude(m => m.User)
            .Include(g => g.Members)
            .ThenInclude(m => m.AddedByUser)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    /// <summary>
    /// Create a new user group
    /// </summary>
    public async Task<UserGroup> CreateGroupAsync(string name, string? description, string createdByUserId, string? color = null)
    {
        // Check if group name already exists
        if (await _context.UserGroups.AnyAsync(g => g.Name == name))
        {
            throw new InvalidOperationException($"A group with the name '{name}' already exists.");
        }

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

        _context.UserGroups.Add(group);
        await _context.SaveChangesAsync();

        return group;
    }

    /// <summary>
    /// Update group details
    /// </summary>
    public async Task<UserGroup> UpdateGroupAsync(int groupId, string? name, string? description, string? color)
    {
        var group = await _context.UserGroups.FindAsync(groupId);
        if (group == null)
        {
            throw new InvalidOperationException($"Group with ID {groupId} not found.");
        }

        // Check if new name conflicts with existing group
        if (name != null && name != group.Name)
        {
            if (await _context.UserGroups.AnyAsync(g => g.Name == name && g.Id != groupId))
            {
                throw new InvalidOperationException($"A group with the name '{name}' already exists.");
            }
            group.Name = name;
        }

        if (description != null)
        {
            group.Description = description;
        }

        if (color != null)
        {
            group.Color = color;
        }

        group.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return group;
    }

    /// <summary>
    /// Delete a group (soft delete by marking as inactive)
    /// </summary>
    public async Task DeleteGroupAsync(int groupId)
    {
        var group = await _context.UserGroups.FindAsync(groupId);
        if (group == null)
        {
            throw new InvalidOperationException($"Group with ID {groupId} not found.");
        }

        group.IsActive = false;
        group.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Add user to group
    /// </summary>
    public async Task<UserGroupMember> AddUserToGroupAsync(int groupId, string userId, string addedByUserId, bool isGroupAdmin = false)
    {
        // Check if user is already in the group
        var existingMembership = await _context.UserGroupMembers
            .FirstOrDefaultAsync(m => m.UserGroupId == groupId && m.UserId == userId);

        if (existingMembership != null)
        {
            throw new InvalidOperationException("User is already a member of this group.");
        }

        var member = new UserGroupMember
        {
            UserGroupId = groupId,
            UserId = userId,
            AddedByUserId = addedByUserId,
            IsGroupAdmin = isGroupAdmin,
            JoinedAt = DateTime.UtcNow
        };

        _context.UserGroupMembers.Add(member);
        await _context.SaveChangesAsync();

        return member;
    }

    /// <summary>
    /// Remove user from group
    /// </summary>
    public async Task RemoveUserFromGroupAsync(int groupId, string userId)
    {
        var member = await _context.UserGroupMembers
            .FirstOrDefaultAsync(m => m.UserGroupId == groupId && m.UserId == userId);

        if (member == null)
        {
            throw new InvalidOperationException("User is not a member of this group.");
        }

        _context.UserGroupMembers.Remove(member);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update group member admin status
    /// </summary>
    public async Task UpdateMemberAdminStatusAsync(int groupId, string userId, bool isGroupAdmin)
    {
        var member = await _context.UserGroupMembers
            .FirstOrDefaultAsync(m => m.UserGroupId == groupId && m.UserId == userId);

        if (member == null)
        {
            throw new InvalidOperationException("User is not a member of this group.");
        }

        member.IsGroupAdmin = isGroupAdmin;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Check if user is a group admin
    /// </summary>
    public async Task<bool> IsUserGroupAdminAsync(int groupId, string userId)
    {
        return await _context.UserGroupMembers
            .AnyAsync(m => m.UserGroupId == groupId && m.UserId == userId && m.IsGroupAdmin);
    }

    /// <summary>
    /// Check if user created the group
    /// </summary>
    public async Task<bool> IsUserGroupCreatorAsync(int groupId, string userId)
    {
        var group = await _context.UserGroups.FindAsync(groupId);
        return group?.CreatedByUserId == userId;
    }

    /// <summary>
    /// Get group members
    /// </summary>
    public async Task<List<UserGroupMember>> GetGroupMembersAsync(int groupId)
    {
        return await _context.UserGroupMembers
            .Where(m => m.UserGroupId == groupId)
            .Include(m => m.User)
            .Include(m => m.AddedByUser)
            .OrderBy(m => m.User.UserName)
            .ToListAsync();
    }

    #endregion
}

/// <summary>
/// DTO for user with their roles
/// </summary>
public class UserWithRoles
{
    public ApplicationUser User { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
}
