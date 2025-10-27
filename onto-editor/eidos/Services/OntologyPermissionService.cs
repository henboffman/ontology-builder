using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;

namespace Eidos.Services;

/// <summary>
/// Service for managing and checking ontology permissions
/// </summary>
public class OntologyPermissionService
{
    private readonly OntologyDbContext _context;

    public OntologyPermissionService(OntologyDbContext context)
    {
        _context = context;
    }

    #region Permission Checks

    /// <summary>
    /// Check if user can view an ontology
    /// </summary>
    public async Task<bool> CanViewAsync(int ontologyId, string? userId)
    {
        var ontology = await _context.Ontologies
            .Include(o => o.GroupPermissions)
            .ThenInclude(gp => gp.UserGroup)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(o => o.Id == ontologyId);

        if (ontology == null)
            return false;

        // Owner can always view
        if (!string.IsNullOrEmpty(userId) && ontology.UserId == userId)
            return true;

        // Check visibility
        if (ontology.Visibility == OntologyVisibility.Public)
            return true;

        if (ontology.Visibility == OntologyVisibility.Group && !string.IsNullOrEmpty(userId))
        {
            // Check if user is in any group that has access
            return ontology.GroupPermissions.Any(gp =>
                gp.UserGroup.Members.Any(m => m.UserId == userId));
        }

        // Private ontologies are only visible to owner
        return false;
    }

    /// <summary>
    /// Check if user can edit an ontology
    /// </summary>
    public async Task<bool> CanEditAsync(int ontologyId, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        var ontology = await _context.Ontologies
            .Include(o => o.GroupPermissions)
            .ThenInclude(gp => gp.UserGroup)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(o => o.Id == ontologyId);

        if (ontology == null)
            return false;

        // Owner can always edit
        if (ontology.UserId == userId)
            return true;

        // Check if public editing is allowed
        if (ontology.Visibility == OntologyVisibility.Public && ontology.AllowPublicEdit)
            return true;

        // Check group permissions for edit access
        if (ontology.Visibility == OntologyVisibility.Group)
        {
            var userGroupPermission = ontology.GroupPermissions.FirstOrDefault(gp =>
                gp.UserGroup.Members.Any(m => m.UserId == userId));

            if (userGroupPermission != null)
            {
                return userGroupPermission.PermissionLevel == PermissionLevels.Edit ||
                       userGroupPermission.PermissionLevel == PermissionLevels.Admin;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if user can manage an ontology (change settings, permissions, delete)
    /// </summary>
    public async Task<bool> CanManageAsync(int ontologyId, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        var ontology = await _context.Ontologies
            .Include(o => o.GroupPermissions)
            .ThenInclude(gp => gp.UserGroup)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(o => o.Id == ontologyId);

        if (ontology == null)
            return false;

        // Only owner or group admins can manage
        if (ontology.UserId == userId)
            return true;

        // Check if user has admin permission through a group
        if (ontology.Visibility == OntologyVisibility.Group)
        {
            var userGroupPermission = ontology.GroupPermissions.FirstOrDefault(gp =>
                gp.UserGroup.Members.Any(m => m.UserId == userId));

            if (userGroupPermission != null)
            {
                return userGroupPermission.PermissionLevel == PermissionLevels.Admin;
            }
        }

        return false;
    }

    /// <summary>
    /// Get all ontologies a user can view
    /// </summary>
    public async Task<List<Ontology>> GetAccessibleOntologiesAsync(string? userId)
    {
        // Load ontologies with minimal includes to avoid in-memory provider issues
        var ontologies = await _context.Ontologies.ToListAsync();

        // Load group permissions separately if needed for group visibility checks
        if (ontologies.Any(o => o.Visibility == OntologyVisibility.Group))
        {
            var ontologyIds = ontologies.Select(o => o.Id).ToList();
            var groupPerms = await _context.OntologyGroupPermissions
                .Where(gp => ontologyIds.Contains(gp.OntologyId))
                .ToListAsync();
        }

        return ontologies.Where(o =>
        {
            // Owner can see their own
            if (!string.IsNullOrEmpty(userId) && o.UserId == userId)
                return true;

            // Public ontologies are visible to all
            if (o.Visibility == OntologyVisibility.Public)
                return true;

            // Group ontologies are visible to group members
            if (o.Visibility == OntologyVisibility.Group && !string.IsNullOrEmpty(userId))
            {
                return o.GroupPermissions != null && o.GroupPermissions.Any(gp =>
                    gp.UserGroup?.Members != null && gp.UserGroup.Members.Any(m => m.UserId == userId));
            }

            // Private ontologies are not visible
            return false;
        }).ToList();
    }

    /// <summary>
    /// Get permission level for a user on an ontology
    /// </summary>
    public async Task<string> GetPermissionLevelAsync(int ontologyId, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return "none";

        var ontology = await _context.Ontologies
            .Include(o => o.GroupPermissions)
            .ThenInclude(gp => gp.UserGroup)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(o => o.Id == ontologyId);

        if (ontology == null)
            return "none";

        // Owner has admin level
        if (ontology.UserId == userId)
            return PermissionLevels.Admin;

        // Public access
        if (ontology.Visibility == OntologyVisibility.Public)
        {
            return ontology.AllowPublicEdit ? PermissionLevels.Edit : PermissionLevels.View;
        }

        // Group access
        if (ontology.Visibility == OntologyVisibility.Group)
        {
            var userGroupPermission = ontology.GroupPermissions.FirstOrDefault(gp =>
                gp.UserGroup.Members.Any(m => m.UserId == userId));

            if (userGroupPermission != null)
            {
                return userGroupPermission.PermissionLevel;
            }
        }

        return "none";
    }

    #endregion

    #region Group Permission Management

    /// <summary>
    /// Grant a group access to an ontology
    /// </summary>
    public async Task GrantGroupAccessAsync(int ontologyId, int groupId, string permissionLevel, string grantedByUserId)
    {
        // Check if permission already exists
        var existingPermission = await _context.OntologyGroupPermissions
            .FirstOrDefaultAsync(p => p.OntologyId == ontologyId && p.UserGroupId == groupId);

        if (existingPermission != null)
        {
            // Update existing permission
            existingPermission.PermissionLevel = permissionLevel;
            existingPermission.GrantedAt = DateTime.UtcNow;
            existingPermission.GrantedByUserId = grantedByUserId;
        }
        else
        {
            // Create new permission
            var permission = new OntologyGroupPermission
            {
                OntologyId = ontologyId,
                UserGroupId = groupId,
                PermissionLevel = permissionLevel,
                GrantedAt = DateTime.UtcNow,
                GrantedByUserId = grantedByUserId
            };

            _context.OntologyGroupPermissions.Add(permission);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Revoke a group's access to an ontology
    /// </summary>
    public async Task RevokeGroupAccessAsync(int ontologyId, int groupId)
    {
        var permission = await _context.OntologyGroupPermissions
            .FirstOrDefaultAsync(p => p.OntologyId == ontologyId && p.UserGroupId == groupId);

        if (permission != null)
        {
            _context.OntologyGroupPermissions.Remove(permission);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get all groups that have access to an ontology
    /// </summary>
    public async Task<List<OntologyGroupPermission>> GetGroupPermissionsAsync(int ontologyId)
    {
        return await _context.OntologyGroupPermissions
            .Include(p => p.UserGroup)
            .ThenInclude(g => g.Members)
            .Where(p => p.OntologyId == ontologyId)
            .OrderBy(p => p.UserGroup.Name)
            .ToListAsync();
    }

    #endregion

    #region Visibility Management

    /// <summary>
    /// Update ontology visibility settings
    /// </summary>
    public async Task UpdateVisibilityAsync(int ontologyId, string visibility, bool allowPublicEdit)
    {
        var ontology = await _context.Ontologies.FindAsync(ontologyId);
        if (ontology == null)
        {
            throw new InvalidOperationException($"Ontology with ID {ontologyId} not found.");
        }

        // Validate visibility value
        if (visibility != OntologyVisibility.Private &&
            visibility != OntologyVisibility.Group &&
            visibility != OntologyVisibility.Public)
        {
            throw new ArgumentException($"Invalid visibility value: {visibility}");
        }

        ontology.Visibility = visibility;
        ontology.AllowPublicEdit = allowPublicEdit;
        ontology.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    #endregion

    #region User Group Membership Queries

    /// <summary>
    /// Get all groups a user belongs to
    /// </summary>
    public async Task<List<UserGroup>> GetUserGroupsAsync(string userId)
    {
        return await _context.UserGroupMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.UserGroup)
            .Select(m => m.UserGroup)
            .ToListAsync();
    }

    /// <summary>
    /// Check if user is a member of a group
    /// </summary>
    public async Task<bool> IsUserInGroupAsync(string userId, int groupId)
    {
        return await _context.UserGroupMembers
            .AnyAsync(m => m.UserId == userId && m.UserGroupId == groupId);
    }

    #endregion
}
