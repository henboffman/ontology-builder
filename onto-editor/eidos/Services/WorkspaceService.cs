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
        private readonly ILogger<WorkspaceService> _logger;

        public WorkspaceService(
            WorkspaceRepository workspaceRepository,
            IOntologyService ontologyService,
            NoteService noteService,
            ILogger<WorkspaceService> logger)
        {
            _workspaceRepository = workspaceRepository;
            _ontologyService = ontologyService;
            _noteService = noteService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new workspace for a user
        /// Automatically creates an associated ontology for concept management
        /// </summary>
        public async Task<Workspace> CreateWorkspaceAsync(string userId, string name, string? description = null)
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

                var workspace = new Workspace
                {
                    Name = name.Trim(),
                    Description = description?.Trim(),
                    UserId = userId,
                    Visibility = "private", // Default to private
                    AllowPublicEdit = false,
                    NoteCount = 0,
                    ConceptNoteCount = 0,
                    UserNoteCount = 0
                };

                var created = await _workspaceRepository.CreateAsync(workspace);

                // Create associated ontology for this workspace
                var ontology = new Ontology
                {
                    Name = $"{name} Ontology",
                    Description = $"Auto-generated ontology for workspace '{name}'",
                    UserId = userId,
                    WorkspaceId = created.Id,
                    Visibility = "private",
                    AllowPublicEdit = false,
                    Version = "1.0"
                };

                await _ontologyService.CreateOntologyAsync(ontology);

                _logger.LogInformation("User {UserId} created workspace '{WorkspaceName}' with ontology", userId, name);

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
        public async Task<Workspace?> UpdateWorkspaceAsync(int workspaceId, string userId, string? name = null, string? description = null, string? visibility = null)
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
    }
}
