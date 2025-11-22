using Eidos.Data;
using Eidos.Models;
using Eidos.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Eidos.Services
{
    /// <summary>
    /// Service for managing workspace permissions (group and user access)
    /// </summary>
    public class WorkspacePermissionService
    {
        private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
        private readonly ILogger<WorkspacePermissionService> _logger;

        public WorkspacePermissionService(
            IDbContextFactory<OntologyDbContext> contextFactory,
            ILogger<WorkspacePermissionService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Grant a group permission to access a workspace
        /// </summary>
        public async Task GrantGroupPermissionAsync(
            int workspaceId,
            int groupId,
            PermissionLevel permissionLevel)
        {
            _logger.LogInformation(
                "GrantGroupPermissionAsync called - WorkspaceId: {WorkspaceId}, GroupId: {GroupId}, Permission: {Permission}",
                workspaceId, groupId, permissionLevel);

            using var context = await _contextFactory.CreateDbContextAsync();

            // Check if permission already exists
            var existingPermission = await context.WorkspaceGroupPermissions
                .FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId && p.UserGroupId == groupId);

            _logger.LogInformation(
                "Existing permission check - Found: {Found}",
                existingPermission != null);

            if (existingPermission != null)
            {
                // Update existing permission
                existingPermission.PermissionLevel = permissionLevel;
                _logger.LogInformation(
                    "Updated permission for group {GroupId} on workspace {WorkspaceId} to {PermissionLevel}",
                    groupId, workspaceId, permissionLevel);
            }
            else
            {
                // Create new permission
                var permission = new WorkspaceGroupPermission
                {
                    WorkspaceId = workspaceId,
                    UserGroupId = groupId,
                    PermissionLevel = permissionLevel,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation(
                    "Creating new permission record - WorkspaceId: {WorkspaceId}, UserGroupId: {UserGroupId}, PermissionLevel: {PermissionLevel}",
                    permission.WorkspaceId, permission.UserGroupId, permission.PermissionLevel);

                context.WorkspaceGroupPermissions.Add(permission);
                _logger.LogInformation(
                    "Added permission to context (not yet saved) - {PermissionLevel} permission to group {GroupId} for workspace {WorkspaceId}",
                    permissionLevel, groupId, workspaceId);
            }

            var changeCount = await context.SaveChangesAsync();
            _logger.LogInformation(
                "SaveChanges completed - {ChangeCount} entities saved",
                changeCount);
        }

        /// <summary>
        /// Grant a user direct access to a workspace
        /// </summary>
        public async Task GrantUserAccessAsync(
            int workspaceId,
            string userId,
            PermissionLevel permissionLevel)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // Check if access already exists
            var existingAccess = await context.WorkspaceUserAccesses
                .FirstOrDefaultAsync(a => a.WorkspaceId == workspaceId && a.SharedWithUserId == userId);

            if (existingAccess != null)
            {
                // Update existing access
                existingAccess.PermissionLevel = permissionLevel;
                _logger.LogInformation(
                    "Updated access for user {UserId} on workspace {WorkspaceId} to {PermissionLevel}",
                    userId, workspaceId, permissionLevel);
            }
            else
            {
                // Create new access
                var access = new WorkspaceUserAccess
                {
                    WorkspaceId = workspaceId,
                    SharedWithUserId = userId,
                    PermissionLevel = permissionLevel,
                    CreatedAt = DateTime.UtcNow
                };

                context.WorkspaceUserAccesses.Add(access);
                _logger.LogInformation(
                    "Granted {PermissionLevel} access to user {UserId} for workspace {WorkspaceId}",
                    permissionLevel, userId, workspaceId);
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Revoke a group's permission to access a workspace
        /// </summary>
        public async Task<bool> RevokeGroupPermissionAsync(int workspaceId, int groupId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var permission = await context.WorkspaceGroupPermissions
                .FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId && p.UserGroupId == groupId);

            if (permission == null)
            {
                _logger.LogWarning(
                    "No permission found for group {GroupId} on workspace {WorkspaceId}",
                    groupId, workspaceId);
                return false;
            }

            context.WorkspaceGroupPermissions.Remove(permission);
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Revoked permission for group {GroupId} on workspace {WorkspaceId}",
                groupId, workspaceId);

            return true;
        }

        /// <summary>
        /// Revoke a user's direct access to a workspace
        /// </summary>
        public async Task<bool> RevokeUserAccessAsync(int workspaceId, string userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var access = await context.WorkspaceUserAccesses
                .FirstOrDefaultAsync(a => a.WorkspaceId == workspaceId && a.SharedWithUserId == userId);

            if (access == null)
            {
                _logger.LogWarning(
                    "No access found for user {UserId} on workspace {WorkspaceId}",
                    userId, workspaceId);
                return false;
            }

            context.WorkspaceUserAccesses.Remove(access);
            await context.SaveChangesAsync();

            _logger.LogInformation(
                "Revoked access for user {UserId} on workspace {WorkspaceId}",
                userId, workspaceId);

            return true;
        }

        /// <summary>
        /// Get all groups with access to a workspace
        /// </summary>
        public async Task<List<WorkspaceGroupPermission>> GetWorkspaceGroupPermissionsAsync(int workspaceId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.WorkspaceGroupPermissions
                .Include(p => p.UserGroup)
                    .ThenInclude(g => g.Members)
                .Where(p => p.WorkspaceId == workspaceId)
                .ToListAsync();
        }

        /// <summary>
        /// Get all users with direct access to a workspace
        /// </summary>
        public async Task<List<WorkspaceUserAccess>> GetWorkspaceUserAccessesAsync(int workspaceId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.WorkspaceUserAccesses
                .Include(a => a.SharedWithUser)
                .Where(a => a.WorkspaceId == workspaceId)
                .ToListAsync();
        }
    }
}
