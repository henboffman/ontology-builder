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
    /// PERFORMANCE: Optimized to only query required fields instead of loading full ontology with nested includes
    /// </summary>
    public async Task<bool> CanViewAsync(int ontologyId, string? userId)
    {
        // Query only the fields needed for permission check (no navigation properties loaded)
        var ontologyInfo = await _context.Ontologies
            .Where(o => o.Id == ontologyId)
            .Select(o => new
            {
                o.UserId,
                o.Visibility,
                o.Id
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (ontologyInfo == null)
            return false;

        // Owner can always view
        if (!string.IsNullOrEmpty(userId) && ontologyInfo.UserId == userId)
            return true;

        // Check visibility
        if (ontologyInfo.Visibility == OntologyVisibility.Public)
            return true;

        if (ontologyInfo.Visibility == OntologyVisibility.Group && !string.IsNullOrEmpty(userId))
        {
            // Efficient query: Check if user is in any group that has access to this ontology
            var hasGroupAccess = await _context.OntologyGroupPermissions
                .Where(gp => gp.OntologyId == ontologyId)
                .AnyAsync(gp => gp.UserGroup.Members.Any(m => m.UserId == userId));

            return hasGroupAccess;
        }

        // Private ontologies are only visible to owner
        return false;
    }

    /// <summary>
    /// Check if user can edit an ontology
    /// PERFORMANCE: Optimized to only query required fields instead of loading full ontology with nested includes
    /// </summary>
    public async Task<bool> CanEditAsync(int ontologyId, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        // Query only the fields needed for permission check (no navigation properties loaded)
        var ontologyInfo = await _context.Ontologies
            .Where(o => o.Id == ontologyId)
            .Select(o => new
            {
                o.UserId,
                o.Visibility,
                o.AllowPublicEdit,
                o.Id
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (ontologyInfo == null)
            return false;

        // Owner can always edit
        if (ontologyInfo.UserId == userId)
            return true;

        // Check if public editing is allowed
        if (ontologyInfo.Visibility == OntologyVisibility.Public && ontologyInfo.AllowPublicEdit)
            return true;

        // Check group permissions for edit access
        if (ontologyInfo.Visibility == OntologyVisibility.Group)
        {
            // Efficient query: Check if user is in any group with Edit or Admin permission for this ontology
            var hasEditAccess = await _context.OntologyGroupPermissions
                .Where(gp => gp.OntologyId == ontologyId)
                .Where(gp => gp.PermissionLevel == PermissionLevels.Edit ||
                             gp.PermissionLevel == PermissionLevels.Admin)
                .AnyAsync(gp => gp.UserGroup.Members.Any(m => m.UserId == userId));

            return hasEditAccess;
        }

        return false;
    }

    /// <summary>
    /// Check if user can manage an ontology (change settings, permissions, delete)
    /// PERFORMANCE: Optimized to only query required fields instead of loading full ontology with nested includes
    /// </summary>
    public async Task<bool> CanManageAsync(int ontologyId, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        // Query only the fields needed for permission check (no navigation properties loaded)
        var ontologyInfo = await _context.Ontologies
            .Where(o => o.Id == ontologyId)
            .Select(o => new
            {
                o.UserId,
                o.Visibility,
                o.Id
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (ontologyInfo == null)
            return false;

        // Only owner or group admins can manage
        if (ontologyInfo.UserId == userId)
            return true;

        // Check if user has admin permission through a group
        if (ontologyInfo.Visibility == OntologyVisibility.Group)
        {
            // Efficient query: Check if user is in any group with Admin permission for this ontology
            var hasAdminAccess = await _context.OntologyGroupPermissions
                .Where(gp => gp.OntologyId == ontologyId)
                .Where(gp => gp.PermissionLevel == PermissionLevels.Admin)
                .AnyAsync(gp => gp.UserGroup.Members.Any(m => m.UserId == userId));

            return hasAdminAccess;
        }

        return false;
    }

    /// <summary>
    /// Get all ontologies a user can view
    /// </summary>
    public async Task<List<Ontology>> GetAccessibleOntologiesAsync(string? userId)
    {
        // Load ontologies without navigation properties to avoid performance issues
        var ontologies = await _context.Ontologies.ToListAsync();

        // Load group permissions with members for group visibility checks
        if (ontologies.Any(o => o.Visibility == OntologyVisibility.Group))
        {
            var ontologyIds = ontologies.Select(o => o.Id).ToList();
            var groupPerms = await _context.OntologyGroupPermissions
                .Include(gp => gp.UserGroup)
                .ThenInclude(g => g.Members)
                .Where(gp => ontologyIds.Contains(gp.OntologyId))
                .AsNoTracking()
                .ToListAsync();

            // Manually populate the GroupPermissions navigation property
            foreach (var ontology in ontologies)
            {
                ontology.GroupPermissions = groupPerms
                    .Where(gp => gp.OntologyId == ontology.Id)
                    .ToList();
            }
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
    /// PERFORMANCE: Optimized to only query required fields instead of loading full ontology with nested includes
    /// </summary>
    public async Task<string> GetPermissionLevelAsync(int ontologyId, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return "none";

        // Query only the fields needed for permission check (no navigation properties loaded)
        var ontologyInfo = await _context.Ontologies
            .Where(o => o.Id == ontologyId)
            .Select(o => new
            {
                o.UserId,
                o.Visibility,
                o.AllowPublicEdit,
                o.Id
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (ontologyInfo == null)
            return "none";

        // Owner has admin level
        if (ontologyInfo.UserId == userId)
            return PermissionLevels.Admin;

        // Public access
        if (ontologyInfo.Visibility == OntologyVisibility.Public)
        {
            return ontologyInfo.AllowPublicEdit ? PermissionLevels.Edit : PermissionLevels.View;
        }

        // Group access
        if (ontologyInfo.Visibility == OntologyVisibility.Group)
        {
            // Efficient query: Get the user's permission level through group membership
            var permissionLevel = await _context.OntologyGroupPermissions
                .Where(gp => gp.OntologyId == ontologyId)
                .Where(gp => gp.UserGroup.Members.Any(m => m.UserId == userId))
                .Select(gp => gp.PermissionLevel)
                .FirstOrDefaultAsync();

            if (permissionLevel != null)
            {
                return permissionLevel;
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
