using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;
using Eidos.Models.Enums;

namespace Eidos.Services;

/// <summary>
/// Service for managing and checking ontology permissions
/// Uses DbContextFactory to support concurrent operations in SignalR hubs
/// </summary>
public class OntologyPermissionService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;

    public OntologyPermissionService(IDbContextFactory<OntologyDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Permission Checks

    /// <summary>
    /// Check if user can view an ontology
    /// PERFORMANCE: Optimized to only query required fields instead of loading full ontology with nested includes
    /// </summary>
    public async Task<bool> CanViewAsync(int ontologyId, string? userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Query only the fields needed for permission check (no navigation properties loaded)
        var ontologyInfo = await context.Ontologies
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
            var hasGroupAccess = await context.OntologyGroupPermissions
                .Where(gp => gp.OntologyId == ontologyId)
                .AnyAsync(gp => gp.UserGroup.Members.Any(m => m.UserId == userId));

            if (hasGroupAccess)
                return true;
        }

        // Check share link permissions (for any visibility level)
        if (!string.IsNullOrEmpty(userId))
        {
            var hasShareAccess = await context.UserShareAccesses
                .Where(usa => usa.UserId == userId)
                .Join(context.OntologyShares,
                    usa => usa.OntologyShareId,
                    os => os.Id,
                    (usa, os) => new { os.OntologyId, os.IsActive })
                .AnyAsync(share => share.OntologyId == ontologyId && share.IsActive);

            return hasShareAccess;
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
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (string.IsNullOrEmpty(userId))
            return false;

        // Query only the fields needed for permission check (no navigation properties loaded)
        var ontologyInfo = await context.Ontologies
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
            var hasEditAccess = await context.OntologyGroupPermissions
                .Where(gp => gp.OntologyId == ontologyId)
                .Where(gp => gp.PermissionLevel == PermissionLevels.Edit ||
                             gp.PermissionLevel == PermissionLevels.Admin)
                .AnyAsync(gp => gp.UserGroup.Members.Any(m => m.UserId == userId));

            if (hasEditAccess)
                return true;
        }

        // Check share link permissions
        var hasShareAccess = await context.UserShareAccesses
            .Where(usa => usa.UserId == userId)
            .Join(context.OntologyShares,
                usa => usa.OntologyShareId,
                os => os.Id,
                (usa, os) => new { os.OntologyId, os.PermissionLevel, os.IsActive })
            .AnyAsync(share => share.OntologyId == ontologyId
                && share.IsActive
                && (share.PermissionLevel == PermissionLevel.ViewAddEdit || share.PermissionLevel == PermissionLevel.FullAccess));

        return hasShareAccess;
    }

    /// <summary>
    /// Check if user can manage an ontology (change settings, permissions, delete)
    /// PERFORMANCE: Optimized to only query required fields instead of loading full ontology with nested includes
    /// </summary>
    public async Task<bool> CanManageAsync(int ontologyId, string? userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (string.IsNullOrEmpty(userId))
            return false;

        // Query only the fields needed for permission check (no navigation properties loaded)
        var ontologyInfo = await context.Ontologies
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
            var hasAdminAccess = await context.OntologyGroupPermissions
                .Where(gp => gp.OntologyId == ontologyId)
                .Where(gp => gp.PermissionLevel == PermissionLevels.Admin)
                .AnyAsync(gp => gp.UserGroup.Members.Any(m => m.UserId == userId));

            if (hasAdminAccess)
                return true;
        }

        // Check share link permissions for FullAccess
        var hasFullAccessShare = await context.UserShareAccesses
            .Where(usa => usa.UserId == userId)
            .Join(context.OntologyShares,
                usa => usa.OntologyShareId,
                os => os.Id,
                (usa, os) => new { os.OntologyId, os.PermissionLevel, os.IsActive })
            .AnyAsync(share => share.OntologyId == ontologyId
                && share.IsActive
                && share.PermissionLevel == PermissionLevel.FullAccess);

        return hasFullAccessShare;
    }

    /// <summary>
    /// Get all ontologies a user can view
    /// </summary>
    public async Task<List<Ontology>> GetAccessibleOntologiesAsync(string? userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Load ontologies with OntologyTags for folder/tag functionality
        var ontologies = await context.Ontologies
            .Include(o => o.OntologyTags)
            .AsNoTracking()
            .ToListAsync();

        // Load group permissions with members for group visibility checks
        if (ontologies.Any(o => o.Visibility == OntologyVisibility.Group))
        {
            var ontologyIds = ontologies.Select(o => o.Id).ToList();
            var groupPerms = await context.OntologyGroupPermissions
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
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (string.IsNullOrEmpty(userId))
            return "none";

        // Query only the fields needed for permission check (no navigation properties loaded)
        var ontologyInfo = await context.Ontologies
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
            var permissionLevel = await context.OntologyGroupPermissions
                .Where(gp => gp.OntologyId == ontologyId)
                .Where(gp => gp.UserGroup.Members.Any(m => m.UserId == userId))
                .Select(gp => gp.PermissionLevel)
                .FirstOrDefaultAsync();

            if (permissionLevel != null)
            {
                return permissionLevel;
            }
        }

        // Check share link permissions
        var sharePermission = await context.UserShareAccesses
            .Where(usa => usa.UserId == userId)
            .Join(context.OntologyShares,
                usa => usa.OntologyShareId,
                os => os.Id,
                (usa, os) => new { os.OntologyId, os.PermissionLevel, os.IsActive })
            .Where(share => share.OntologyId == ontologyId && share.IsActive)
            .Select(share => share.PermissionLevel)
            .FirstOrDefaultAsync();

        if (sharePermission != null)
        {
            // Convert PermissionLevel enum to string
            return sharePermission switch
            {
                PermissionLevel.View => PermissionLevels.View,
                PermissionLevel.ViewAndAdd => PermissionLevels.Edit, // Map ViewAndAdd to Edit
                PermissionLevel.ViewAddEdit => PermissionLevels.Edit,
                PermissionLevel.FullAccess => PermissionLevels.Admin,
                _ => "none"
            };
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
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Check if permission already exists
        var existingPermission = await context.OntologyGroupPermissions
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

            context.OntologyGroupPermissions.Add(permission);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Revoke a group's access to an ontology
    /// </summary>
    public async Task RevokeGroupAccessAsync(int ontologyId, int groupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var permission = await context.OntologyGroupPermissions
            .FirstOrDefaultAsync(p => p.OntologyId == ontologyId && p.UserGroupId == groupId);

        if (permission != null)
        {
            context.OntologyGroupPermissions.Remove(permission);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get all groups that have access to an ontology
    /// </summary>
    public async Task<List<OntologyGroupPermission>> GetGroupPermissionsAsync(int ontologyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.OntologyGroupPermissions
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
        await using var context = await _contextFactory.CreateDbContextAsync();

        var ontology = await context.Ontologies.FindAsync(ontologyId);
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

        await context.SaveChangesAsync();
    }

    #endregion

    #region User Group Membership Queries

    /// <summary>
    /// Get all groups a user belongs to
    /// </summary>
    public async Task<List<UserGroup>> GetUserGroupsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.UserGroupMembers
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
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.UserGroupMembers
            .AnyAsync(m => m.UserId == userId && m.UserGroupId == groupId);
    }

    #endregion

    #region Workspace Permissions

    /// <summary>
    /// Check if user can view a workspace
    /// </summary>
    public async Task<bool> CanViewWorkspaceAsync(int workspaceId, string? userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var workspaceInfo = await context.Workspaces
            .Where(w => w.Id == workspaceId)
            .Select(w => new
            {
                w.UserId,
                w.Visibility,
                w.Id,
                OntologyId = w.Ontology != null ? (int?)w.Ontology.Id : null
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (workspaceInfo == null)
            return false;

        // Owner can always view
        if (!string.IsNullOrEmpty(userId) && workspaceInfo.UserId == userId)
            return true;

        // Check visibility
        if (workspaceInfo.Visibility == "public")
            return true;

        if (string.IsNullOrEmpty(userId))
            return false; // Not logged in

        // Check group permissions
        var hasGroupPermission = await context.WorkspaceGroupPermissions
            .Where(gp => gp.WorkspaceId == workspaceId)
            .Join(context.UserGroupMembers,
                gp => gp.UserGroupId,
                ugm => ugm.UserGroupId,
                (gp, ugm) => new { gp, ugm })
            .AnyAsync(x => x.ugm.UserId == userId);

        if (hasGroupPermission)
            return true;

        // Check direct user access
        var hasDirectAccess = await context.WorkspaceUserAccesses
            .AnyAsync(ua => ua.WorkspaceId == workspaceId && ua.SharedWithUserId == userId);

        if (hasDirectAccess)
            return true;

        // Check ontology share link access (if workspace has an ontology)
        // This allows users who accessed via ontology share link to also view workspace notes
        if (workspaceInfo.OntologyId.HasValue)
        {
            var hasShareLinkAccess = await context.UserShareAccesses
                .AnyAsync(usa => usa.OntologyShare != null &&
                                usa.OntologyShare.OntologyId == workspaceInfo.OntologyId.Value &&
                                usa.UserId == userId);

            if (hasShareLinkAccess)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if user can edit a workspace
    /// </summary>
    public async Task<bool> CanEditWorkspaceAsync(int workspaceId, string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var workspaceInfo = await context.Workspaces
            .Where(w => w.Id == workspaceId)
            .Select(w => new
            {
                w.UserId,
                w.Visibility,
                w.AllowPublicEdit,
                OntologyId = w.Ontology != null ? (int?)w.Ontology.Id : null
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (workspaceInfo == null)
            return false;

        // Owner can always edit
        if (workspaceInfo.UserId == userId)
            return true;

        // Public workspace with public edit enabled
        if (workspaceInfo.Visibility == "public" && workspaceInfo.AllowPublicEdit)
            return true;

        // Check group permissions (ViewAddEdit or FullAccess)
        var hasEditPermission = await context.WorkspaceGroupPermissions
            .Where(gp => gp.WorkspaceId == workspaceId &&
                        (gp.PermissionLevel == PermissionLevel.ViewAddEdit ||
                         gp.PermissionLevel == PermissionLevel.FullAccess))
            .Join(context.UserGroupMembers,
                gp => gp.UserGroupId,
                ugm => ugm.UserGroupId,
                (gp, ugm) => new { gp, ugm })
            .AnyAsync(x => x.ugm.UserId == userId);

        if (hasEditPermission)
            return true;

        // Check direct user access
        var userAccess = await context.WorkspaceUserAccesses
            .Where(ua => ua.WorkspaceId == workspaceId && ua.SharedWithUserId == userId)
            .Select(ua => ua.PermissionLevel)
            .FirstOrDefaultAsync();

        if (userAccess == PermissionLevel.ViewAddEdit || userAccess == PermissionLevel.FullAccess)
            return true;

        // Check ontology share link access with edit permissions (if workspace has an ontology)
        // Share link must grant ViewAddEdit or FullAccess to allow workspace note editing
        if (workspaceInfo.OntologyId.HasValue)
        {
            var sharePermissionLevel = await context.UserShareAccesses
                .Where(usa => usa.OntologyShare != null &&
                             usa.OntologyShare.OntologyId == workspaceInfo.OntologyId.Value &&
                             usa.UserId == userId)
                .Select(usa => (PermissionLevel?)usa.OntologyShare!.PermissionLevel)
                .FirstOrDefaultAsync();

            if (sharePermissionLevel == PermissionLevel.ViewAddEdit ||
                sharePermissionLevel == PermissionLevel.FullAccess)
                return true;
        }

        return false;
    }

    #endregion
}
