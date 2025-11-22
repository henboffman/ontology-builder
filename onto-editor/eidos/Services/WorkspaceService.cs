using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Eidos.Services
{
    /// <summary>
    /// Business logic service for workspace management
    /// Handles workspace CRUD operations and permissions
    /// </summary>
    public class WorkspaceService
    {
        private readonly WorkspaceRepository _workspaceRepository;
        private readonly IOntologyService _ontologyService;
        private readonly NoteService _noteService;
        private readonly UserGroupService _userGroupService;
        private readonly WorkspacePermissionService _permissionService;
        private readonly ILogger<WorkspaceService> _logger;

        public WorkspaceService(
            WorkspaceRepository workspaceRepository,
            IOntologyService ontologyService,
            NoteService noteService,
            UserGroupService userGroupService,
            WorkspacePermissionService permissionService,
            ILogger<WorkspaceService> logger)
        {
            _workspaceRepository = workspaceRepository;
            _ontologyService = ontologyService;
            _noteService = noteService;
            _userGroupService = userGroupService;
            _permissionService = permissionService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new workspace for a user
        /// Automatically creates an associated ontology for concept management
        /// </summary>
        public async Task<Workspace> CreateWorkspaceAsync(
            string userId,
            string name,
            string? description = null,
            string visibility = "private",
            bool allowPublicEdit = false)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentException("User ID is required", nameof(userId));
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("Workspace name is required", nameof(name));
                }

                // Validate visibility
                if (!new[] { "private", "group", "public" }.Contains(visibility.ToLower()))
                {
                    throw new ArgumentException("Visibility must be 'private', 'group', or 'public'", nameof(visibility));
                }

                var workspace = new Workspace
                {
                    Name = name.Trim(),
                    Description = description?.Trim(),
                    UserId = userId,
                    Visibility = visibility.ToLower(),
                    AllowPublicEdit = allowPublicEdit,
                    NoteCount = 0,
                    ConceptNoteCount = 0,
                    UserNoteCount = 0
                };

                var created = await _workspaceRepository.CreateAsync(workspace);

                // Create associated ontology for this workspace
                // Ontology visibility matches workspace visibility
                var ontology = new Ontology
                {
                    Name = $"{name} Ontology",
                    Description = $"Auto-generated ontology for workspace '{name}'",
                    UserId = userId,
                    WorkspaceId = created.Id,
                    Visibility = visibility.ToLower(),
                    AllowPublicEdit = allowPublicEdit,
                    Version = "0.1"  // Changed from "1.0" to "0.1" for new workspaces
                };

                await _ontologyService.CreateOntologyAsync(ontology);

                _logger.LogInformation(
                    "User {UserId} created workspace '{WorkspaceName}' with visibility '{Visibility}' and ontology",
                    userId, name, visibility);

                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workspace {WorkspaceName} for user {UserId}", name, userId);
                throw;
            }
        }

        /// <summary>
        /// Get workspace by ID (with access check)
        /// </summary>
        public async Task<Workspace?> GetWorkspaceAsync(int workspaceId, string userId, bool includeOntology = false)
        {
            try
            {
                // Check access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);
                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} denied access to workspace {WorkspaceId}", userId, workspaceId);
                    return null;
                }

                var workspace = await _workspaceRepository.GetByIdAsync(workspaceId, includeOntology);

                if (workspace != null && includeOntology)
                {
                    _logger.LogInformation("Loaded workspace {WorkspaceId}, Ontology: {OntologyId}",
                        workspaceId, workspace.Ontology?.Id ?? -1);
                }

                return workspace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace {WorkspaceId} for user {UserId}", workspaceId, userId);
                throw;
            }
        }

        /// <summary>
        /// Get all workspaces for a user
        /// </summary>
        public async Task<List<Workspace>> GetUserWorkspacesAsync(string userId)
        {
            try
            {
                return await _workspaceRepository.GetByUserIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspaces for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Update workspace metadata
        /// </summary>
        public async Task<Workspace?> UpdateWorkspaceAsync(int workspaceId, string userId, string? name = null, string? description = null, string? visibility = null, bool? allowPublicEdit = null)
        {
            try
            {
                var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

                if (workspace == null)
                {
                    return null;
                }

                // Only owner can update
                if (workspace.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update workspace {WorkspaceId} owned by {OwnerId}",
                        userId, workspaceId, workspace.UserId);
                    return null;
                }

                // Update fields
                if (!string.IsNullOrWhiteSpace(name))
                {
                    workspace.Name = name.Trim();
                }

                if (description != null)
                {
                    workspace.Description = description.Trim();
                }

                if (visibility != null && new[] { "private", "group", "public" }.Contains(visibility))
                {
                    workspace.Visibility = visibility;
                }

                if (allowPublicEdit.HasValue)
                {
                    workspace.AllowPublicEdit = allowPublicEdit.Value;
                }

                workspace.UpdatedAt = DateTime.UtcNow;

                return await _workspaceRepository.UpdateAsync(workspace);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workspace {WorkspaceId} for user {UserId}", workspaceId, userId);
                throw;
            }
        }

        /// <summary>
        /// Delete a workspace (owner only)
        /// </summary>
        public async Task<bool> DeleteWorkspaceAsync(int workspaceId, string userId)
        {
            try
            {
                var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);

                if (workspace == null)
                {
                    return false;
                }

                // Only owner can delete
                if (workspace.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete workspace {WorkspaceId} owned by {OwnerId}",
                        userId, workspaceId, workspace.UserId);
                    return false;
                }

                await _workspaceRepository.DeleteAsync(workspaceId);

                _logger.LogInformation("User {UserId} deleted workspace {WorkspaceId}", userId, workspaceId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workspace {WorkspaceId} for user {UserId}", workspaceId, userId);
                throw;
            }
        }

        /// <summary>
        /// Check if user has access to workspace
        /// </summary>
        public async Task<bool> CanAccessWorkspaceAsync(int workspaceId, string userId)
        {
            try
            {
                return await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking workspace {WorkspaceId} access for user {UserId}", workspaceId, userId);
                throw;
            }
        }

        /// <summary>
        /// Update workspace note counts
        /// </summary>
        public async Task UpdateNoteCountsAsync(int workspaceId)
        {
            try
            {
                await _workspaceRepository.UpdateNoteCountsAsync(workspaceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note counts for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Discover public workspaces
        /// </summary>
        public async Task<List<Workspace>> GetPublicWorkspacesAsync(int page = 0, int pageSize = 20)
        {
            try
            {
                return await _workspaceRepository.GetPublicWorkspacesAsync(page * pageSize, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public workspaces");
                throw;
            }
        }

        /// <summary>
        /// Ensures that a workspace exists for a given ontology (for backwards compatibility with legacy ontologies)
        /// If the ontology doesn't have a workspace, creates one automatically
        /// </summary>
        public async Task<Workspace> EnsureWorkspaceForOntologyAsync(int ontologyId)
        {
            try
            {
                // Get the ontology
                var ontology = await _ontologyService.GetOntologyAsync(ontologyId);
                if (ontology == null)
                {
                    throw new ArgumentException($"Ontology {ontologyId} not found", nameof(ontologyId));
                }

                // If it already has a workspace, return it
                if (ontology.WorkspaceId.HasValue)
                {
                    var existingWorkspace = await _workspaceRepository.GetByIdAsync(ontology.WorkspaceId.Value);
                    if (existingWorkspace != null)
                    {
                        return existingWorkspace;
                    }
                }

                // Create a workspace for this legacy ontology
                _logger.LogInformation("Creating workspace for legacy ontology {OntologyId} '{OntologyName}'", ontology.Id, ontology.Name);

                var workspace = new Workspace
                {
                    Name = ontology.Name,
                    Description = ontology.Description ?? $"Auto-generated workspace for ontology '{ontology.Name}'",
                    UserId = ontology.UserId,
                    Visibility = ontology.Visibility,
                    AllowPublicEdit = ontology.AllowPublicEdit,
                    NoteCount = 0,
                    ConceptNoteCount = 0,
                    UserNoteCount = 0
                };

                var created = await _workspaceRepository.CreateAsync(workspace);

                // Update the ontology to link to the new workspace
                ontology.WorkspaceId = created.Id;
                await _ontologyService.UpdateOntologyAsync(ontology);

                // Auto-create concept notes for all existing concepts in the ontology
                var notesCreated = await _noteService.EnsureConceptNotesForOntologyAsync(created.Id, ontologyId, ontology.UserId);

                _logger.LogInformation("Created workspace {WorkspaceId} for legacy ontology {OntologyId} with {NotesCreated} concept notes",
                    created.Id, ontologyId, notesCreated);

                // Reload the workspace with Ontology included to ensure navigation property is populated
                var workspaceWithOntology = await _workspaceRepository.GetByIdAsync(created.Id, includeOntology: true);
                return workspaceWithOntology!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring workspace for ontology {OntologyId}", ontologyId);
                throw;
            }
        }

        /// <summary>
        /// Create workspace with full privacy and access configuration
        /// Handles group creation, member management, and permission grants
        /// </summary>
        public async Task<Workspace> CreateWorkspaceWithAccessAsync(
            string userId,
            string name,
            string? description,
            string visibility,
            bool allowPublicEdit,
            int? groupId = null,
            string? newGroupName = null,
            List<string>? newGroupMemberIds = null,
            List<string>? directAccessUserIds = null,
            string directAccessPermissionLevel = "ViewAddEdit")
        {
            try
            {
                _logger.LogInformation(
                    "Creating workspace '{Name}' with visibility '{Visibility}' for user {UserId}",
                    name, visibility, userId);

                // 1. Create the workspace (and its associated ontology)
                var workspace = await CreateWorkspaceAsync(
                    userId, name, description, visibility, allowPublicEdit);

                // 2. Handle group-based permissions
                if (visibility == "group")
                {
                    int targetGroupId = groupId ?? 0;

                    _logger.LogInformation(
                        "Group handling - GroupId: {GroupId}, TargetGroupId: {TargetGroupId}, NewGroupName: '{NewGroupName}', IsEmpty: {IsEmpty}",
                        groupId, targetGroupId, newGroupName ?? "(null)", string.IsNullOrEmpty(newGroupName));

                    // Create new group if requested
                    if (targetGroupId == -1 && !string.IsNullOrEmpty(newGroupName))
                    {
                        _logger.LogInformation(
                            "Creating new group '{GroupName}' for workspace {WorkspaceId}",
                            newGroupName, workspace.Id);

                        var newGroup = await _userGroupService.CreateGroupAsync(
                            newGroupName,
                            $"Group for workspace '{name}'",
                            userId);

                        targetGroupId = newGroup.Id;

                        // Add members to new group
                        if (newGroupMemberIds?.Any() == true)
                        {
                            foreach (var memberId in newGroupMemberIds)
                            {
                                await _userGroupService.AddUserToGroupAsync(
                                    targetGroupId, memberId, userId, isGroupAdmin: false);
                            }

                            _logger.LogInformation(
                                "Added {MemberCount} members to group {GroupId}",
                                newGroupMemberIds.Count, targetGroupId);
                        }
                    }

                    // Grant group permission to workspace AND its ontology
                    if (targetGroupId > 0)
                    {
                        _logger.LogInformation(
                            "About to grant permission - WorkspaceId: {WorkspaceId}, GroupId: {GroupId}",
                            workspace.Id, targetGroupId);

                        try
                        {
                            // Grant permission to workspace
                            await _permissionService.GrantGroupPermissionAsync(
                                workspace.Id,
                                targetGroupId,
                                Models.Enums.PermissionLevel.ViewAddEdit);

                            _logger.LogInformation(
                                "Successfully granted ViewAddEdit permission to group {GroupId} for workspace {WorkspaceId}",
                                targetGroupId, workspace.Id);

                            // Also grant permission to the workspace's ontology
                            // We need to reload the workspace with its ontology to get the ontology ID
                            var workspaceWithOntology = await _workspaceRepository.GetByIdAsync(workspace.Id, includeOntology: true);
                            if (workspaceWithOntology?.Ontology != null)
                            {
                                await _userGroupService.GrantGroupPermissionAsync(
                                    workspaceWithOntology.Ontology.Id,
                                    targetGroupId,
                                    "edit",  // Maps to ViewAddEdit
                                    userId);

                                _logger.LogInformation(
                                    "Successfully granted edit permission to group {GroupId} for ontology {OntologyId}",
                                    targetGroupId, workspaceWithOntology.Ontology.Id);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Could not find ontology for workspace {WorkspaceId} to grant group permission",
                                    workspace.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "FAILED to grant permission to group {GroupId} for workspace {WorkspaceId}",
                                targetGroupId, workspace.Id);
                            throw;
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Skipping group permission grant - targetGroupId is {TargetGroupId}",
                            targetGroupId);
                    }
                }

                // 3. Grant direct user access (to workspace only)
                // Note: For ontologies, access is controlled via groups and share links
                // Direct user access is a workspace-level feature
                if (directAccessUserIds?.Any() == true)
                {
                    var permissionLevel = Enum.Parse<Models.Enums.PermissionLevel>(
                        directAccessPermissionLevel, ignoreCase: true);

                    foreach (var accessUserId in directAccessUserIds)
                    {
                        // Grant access to workspace
                        await _permissionService.GrantUserAccessAsync(
                            workspace.Id,
                            accessUserId,
                            permissionLevel);
                    }

                    _logger.LogInformation(
                        "Granted {PermissionLevel} access to {UserCount} users for workspace {WorkspaceId}",
                        directAccessPermissionLevel, directAccessUserIds.Count, workspace.Id);
                }

                _logger.LogInformation(
                    "Successfully created workspace {WorkspaceId} with all permissions configured",
                    workspace.Id);

                return workspace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create workspace with access configuration");
                throw;
            }
        }
    }
}
