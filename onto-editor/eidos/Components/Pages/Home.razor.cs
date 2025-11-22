using Eidos.Services;
using Eidos.Services.Interfaces;
using Eidos.Components.Shared;
using Eidos.Constants;
using Eidos.Models.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using OntologyModel = Eidos.Models.Ontology;
using WorkspaceModel = Eidos.Models.Workspace;
using CollaborationPost = Eidos.Models.CollaborationPost;
using ImportProgress = Eidos.Models.ImportProgress;
using OntologyVisibility = Eidos.Models.OntologyVisibility;

namespace Eidos.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private IOntologyService OntologyService { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IOntologyShareService ShareService { get; set; } = default!;
    [Inject] private OntologyTemplateService TemplateService { get; set; } = default!;
    [Inject] private OntologyDownloadService DownloadService { get; set; } = default!;
    [Inject] private ITtlImportService ImportService { get; set; } = default!;
    [Inject] private TutorialService TutorialService { get; set; } = default!;
    [Inject] private OntologyPermissionService PermissionService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<Home> Logger { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ToastService ToastService { get; set; } = default!;
    [Inject] private ICollaborationBoardService CollaborationService { get; set; } = default!;
    [Inject] private HolidayThemeService HolidayService { get; set; } = default!;
    [Inject] private WorkspaceService WorkspaceService { get; set; } = default!;

    private List<OntologyModel>? ontologies;
    private bool showCreateDialog = false;
    private bool showCreateWorkspaceDialog = false;
    private bool showGettingStarted = false;
    private OntologyModel newOntology = new();
    private string selectedTemplate = "";
    private string startFromType = "scratch"; // "scratch", "template", or "existing"
    private int selectedSourceOntologyId = 0;
    private string derivationType = "clone"; // "clone" or "fork"
    private string provenanceNotes = "";
    private TutorialOverlay? tutorialOverlay;
    private bool isImporting = false;
    private ImportProgress? importProgress = null;
    private bool isAuthenticated = false;
    private string? currentUserId = null;
    private Dictionary<int, string> permissionButtonTexts = new();

    // Dashboard state
    private string searchFilter = string.Empty;
    private string sortBy = "updated-desc";
    private string selectedTagFilter = string.Empty;
    private string viewType = "grid";

    // Folder/Tag state
    private List<FolderSidebar.FolderInfo> folderList = new();
    private bool showAddFolderDialog = false;
    private string newFolderName = string.Empty;
    private string newFolderColor = "#0d6efd"; // Bootstrap primary blue
    private int sharedWithMeCount = 0;
    private int publicOntologiesCount = 0;

    // Collaboration state
    private List<CollaborationPost> recentCollaborationPosts = new();
    private bool collaborationExpanded = false; // Collapsed by default
    private bool sharedWithMeExpanded = true; // Expanded by default to showcase the new feature

    protected override async Task OnInitializedAsync()
    {
        isAuthenticated = await UserService.IsAuthenticatedAsync();

        // Get current user ID for permission checking
        if (isAuthenticated)
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            currentUserId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        await LoadOntologies();

        // Load permission button texts for all ontologies
        if (ontologies != null)
        {
            foreach (var ontology in ontologies)
            {
                permissionButtonTexts[ontology.Id] = await GetPermissionButtonText(ontology);
            }
        }

        // Load folders from tags
        await LoadFolders();

        // Load recent collaboration posts
        if (isAuthenticated)
        {
            await LoadRecentCollaborationPosts();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Register component reference for drag and drop
            var dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("setHomeComponentRef", dotNetRef);

            await TutorialService.InitializeAsync(JSRuntime);

            if (tutorialOverlay != null)
            {
                // 500ms delay ensures DOM is fully rendered and interactive elements are ready
                // This prevents tutorial tooltips from positioning incorrectly on slow devices
                await Task.Delay(500);
                tutorialOverlay.Show();
            }
        }
    }

    private Task HandleTutorialComplete()
    {
        // Tutorial completed
        return Task.CompletedTask;
    }

    private Task HandleTutorialSkip()
    {
        // Tutorial skipped
        return Task.CompletedTask;
    }

    private async Task RestartTutorial()
    {
        await TutorialService.ResetTutorialAsync(JSRuntime);
        tutorialOverlay?.Show();
    }

    private async Task LoadOntologies()
    {
        // Use permission service to get only ontologies the user can access
        ontologies = await PermissionService.GetAccessibleOntologiesAsync(currentUserId);

        // Calculate special folder counts
        if (ontologies != null && !string.IsNullOrEmpty(currentUserId))
        {
            // Shared with Me: ontologies where user is NOT the owner but has access
            sharedWithMeCount = ontologies.Count(o => o.UserId != currentUserId);

            // Public Ontologies: ontologies with Public visibility
            publicOntologiesCount = ontologies.Count(o => o.Visibility == OntologyVisibility.Public);
        }
        else
        {
            sharedWithMeCount = 0;
            publicOntologiesCount = 0;
        }

        // Reload permission button texts
        if (ontologies != null)
        {
            permissionButtonTexts.Clear();
            foreach (var ontology in ontologies)
            {
                permissionButtonTexts[ontology.Id] = await GetPermissionButtonText(ontology);
            }
        }
    }

    private async Task LoadRecentCollaborationPosts()
    {
        try
        {
            var allPosts = await CollaborationService.GetActivePostsAsync();
            recentCollaborationPosts = allPosts.Take(5).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading recent collaboration posts");
            recentCollaborationPosts = new List<CollaborationPost>();
        }
    }

    private async Task LoadFolders()
    {
        if (string.IsNullOrEmpty(currentUserId) || ontologies == null)
        {
            folderList.Clear();
            return;
        }

        try
        {
            // Get all unique tags for the current user
            var tags = await OntologyService.GetUserTagsAsync(currentUserId);

            // Build folder list with counts
            folderList = new List<FolderSidebar.FolderInfo>();

            foreach (var tag in tags)
            {
                var count = ontologies.Count(o => o.OntologyTags?.Any(t => t.Tag == tag) ?? false);
                folderList.Add(new FolderSidebar.FolderInfo
                {
                    Tag = tag,
                    Color = ontologies
                        .SelectMany(o => o.OntologyTags ?? new List<Eidos.Models.OntologyTag>())
                        .FirstOrDefault(t => t.Tag == tag)?.Color,
                    Count = count
                });
            }

            // Sort folders alphabetically
            folderList = folderList.OrderBy(f => f.Tag).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading folders");
            folderList.Clear();
        }
    }

    private bool IsSharedWithMe(OntologyModel ontology)
    {
        // An ontology is "shared with me" if the current user is not the owner
        return !string.IsNullOrEmpty(currentUserId) && ontology.UserId != currentUserId;
    }

    private async Task<string> GetPermissionButtonText(OntologyModel ontology)
    {
        // If user owns the ontology, they have full access
        if (ontology.UserId == currentUserId)
        {
            return "View & Edit";
        }

        // Check permission level via share service
        var permissionLevel = await ShareService.GetPermissionLevelAsync(ontology.Id, currentUserId, null);

        return permissionLevel switch
        {
            PermissionLevel.View => "View",
            PermissionLevel.ViewAndAdd => "View & Add",
            PermissionLevel.ViewAddEdit => "View & Edit",
            PermissionLevel.FullAccess => "Manage",
            _ => "View & Edit" // Default for owner
        };
    }

    private Task HandleFeatureToggleChanged()
    {
        // Feature toggle changed, UI will be refreshed automatically
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task HandleFolderSelected(string? tag)
    {
        selectedTagFilter = tag ?? string.Empty;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task ShowAddFolderDialog()
    {
        showAddFolderDialog = true;
        newFolderName = string.Empty;
        newFolderColor = "#0d6efd";
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task CreateFolder()
    {
        if (string.IsNullOrWhiteSpace(newFolderName))
        {
            return;
        }

        // Add the folder to the list immediately with 0 count
        // The folder will show in the sidebar and be ready to receive ontologies via drag & drop
        if (!folderList.Any(f => f.Tag.Equals(newFolderName, StringComparison.OrdinalIgnoreCase)))
        {
            folderList.Add(new FolderSidebar.FolderInfo
            {
                Tag = newFolderName,
                Color = newFolderColor,
                Count = 0
            });

            // Sort folders alphabetically
            folderList = folderList.OrderBy(f => f.Tag).ToList();
        }

        showAddFolderDialog = false;
        StateHasChanged();
    }

    private Task CancelAddFolder()
    {
        showAddFolderDialog = false;
        newFolderName = string.Empty;
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task HandleDrop(string tag, int ontologyId)
    {
        if (ontologyId == 0 || string.IsNullOrEmpty(currentUserId))
        {
            Logger.LogWarning("HandleDrop called with invalid data: ontologyId={OntologyId}, userId={UserId}", ontologyId, currentUserId);
            return;
        }

        // Prevent dropping on special folders
        if (tag == "$$SHARED_WITH_ME$$" || tag == "$$PUBLIC$$")
        {
            Logger.LogWarning("Attempted to drop ontology on special folder: {Tag}", tag);
            return;
        }

        await InvokeAsync(async () =>
        {
            try
            {
                // Get the folder color if it exists
                var folder = folderList.FirstOrDefault(f => f.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
                var color = folder?.Color;

                // Add the tag to the ontology with the folder's color
                await OntologyService.AddTagAsync(ontologyId, tag, color);

                // Reload ontologies and folders to reflect the change
                await LoadOntologies();
                await LoadFolders();

                // Force a UI update
                await InvokeAsync(StateHasChanged);

                Logger.LogInformation("Successfully added tag '{Tag}' to ontology {OntologyId}", tag, ontologyId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error adding tag via drag and drop");
            }
        });
    }

    private async Task RemoveTag(int ontologyId, string tag)
    {
        try
        {
            await OntologyService.RemoveTagAsync(ontologyId, tag);

            // Force a complete data refresh by directly calling the database again
            var freshOntologies = await PermissionService.GetAccessibleOntologiesAsync(currentUserId);

            // Create a completely new list to break any reference caching
            ontologies = freshOntologies.Select(o => o).ToList();

            await LoadFolders();

            // Force complete UI re-render
            await InvokeAsync(StateHasChanged);

            Logger.LogInformation("Successfully removed tag '{Tag}' from ontology {OntologyId}", tag, ontologyId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing tag");
        }
    }

    private IEnumerable<OntologyModel> FilteredOntologies
    {
        get
        {
            if (ontologies == null) return Enumerable.Empty<OntologyModel>();

            var filtered = ontologies.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                var query = searchFilter.ToLower();
                filtered = filtered.Where(o =>
                    (o.Name?.ToLower().Contains(query) ?? false) ||
                    (o.Description?.ToLower().Contains(query) ?? false) ||
                    (o.Author?.ToLower().Contains(query) ?? false) ||
                    (o.Tags?.ToLower().Contains(query) ?? false)
                );
            }

            // Apply tag filter (including special folders)
            if (!string.IsNullOrWhiteSpace(selectedTagFilter))
            {
                // Handle special folders
                if (selectedTagFilter == "$$SHARED_WITH_ME$$")
                {
                    // Filter for ontologies NOT owned by current user
                    filtered = filtered.Where(o => o.UserId != currentUserId);
                }
                else if (selectedTagFilter == "$$PUBLIC$$")
                {
                    // Filter for public ontologies
                    filtered = filtered.Where(o => o.Visibility == OntologyVisibility.Public);
                }
                else
                {
                    // Regular tag filter
                    filtered = filtered.Where(o =>
                        o.OntologyTags?.Any(t => t.Tag.Equals(selectedTagFilter, StringComparison.OrdinalIgnoreCase)) ?? false
                    );
                }
            }

            // Apply sorting
            filtered = sortBy switch
            {
                "updated-desc" => filtered.OrderByDescending(o => o.UpdatedAt),
                "updated-asc" => filtered.OrderBy(o => o.UpdatedAt),
                "created-desc" => filtered.OrderByDescending(o => o.CreatedAt),
                "created-asc" => filtered.OrderBy(o => o.CreatedAt),
                "name-asc" => filtered.OrderBy(o => o.Name),
                "name-desc" => filtered.OrderByDescending(o => o.Name),
                "concepts-desc" => filtered.OrderByDescending(o => o.ConceptCount),
                "concepts-asc" => filtered.OrderBy(o => o.ConceptCount),
                _ => filtered.OrderByDescending(o => o.UpdatedAt)
            };

            return filtered;
        }
    }

    private List<string> GetAllTags()
    {
        if (ontologies == null) return new List<string>();

        return ontologies
            .Where(o => o.OntologyTags != null && o.OntologyTags.Any())
            .SelectMany(o => o.OntologyTags.Select(t => t.Tag))
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }

    private void ClearFilters()
    {
        searchFilter = string.Empty;
        selectedTagFilter = string.Empty;
    }

    private void ShowCreateOntologyDialog()
    {
        newOntology = new OntologyModel
        {
            Version = "1.0"
        };
        selectedTemplate = "";
        showCreateDialog = true;
    }

    private void ShowCreateWorkspaceDialog()
    {
        showCreateWorkspaceDialog = true;
    }

    private async Task HandleWorkspaceCreated(WorkspaceModel workspace)
    {
        // Reload ontologies list to include the new workspace's ontology
        await LoadOntologies();

        // Navigate to the new workspace
        Navigation.NavigateTo($"workspace/{workspace.Id}");
    }

    private void CreateNewWorkspace()
    {
        // Navigate to the workspaces page where user can create a new workspace
        Navigation.NavigateTo("/workspaces");
    }

    private async Task CreateOntology()
    {
        // Security: Prevent ontology creation if not authenticated
        if (!isAuthenticated)
        {
            Logger.LogWarning("Unauthorized ontology creation attempt - user not authenticated");
            ToastService.ShowError("You must be signed in to create ontologies");
            return;
        }

        // Input validation
        if (string.IsNullOrWhiteSpace(newOntology.Name))
        {
            ToastService.ShowError("Ontology name is required");
            return;
        }

        // Validate name length (prevent excessively long names)
        if (newOntology.Name.Length > 200)
        {
            ToastService.ShowError("Ontology name must be 200 characters or less");
            return;
        }

        // Sanitize input - trim whitespace
        newOntology.Name = newOntology.Name.Trim();
        newOntology.Description = newOntology.Description?.Trim();
        newOntology.Author = newOntology.Author?.Trim();

        OntologyModel createdOntology;

        // Handle fork/clone from existing ontology
        if (startFromType == "existing" && selectedSourceOntologyId > 0)
        {
            try
            {
                if (derivationType == "fork")
                {
                    createdOntology = await OntologyService.ForkOntologyAsync(
                        selectedSourceOntologyId,
                        newOntology.Name,
                        string.IsNullOrWhiteSpace(provenanceNotes) ? null : provenanceNotes
                    );
                }
                else // clone
                {
                    createdOntology = await OntologyService.CloneOntologyAsync(
                        selectedSourceOntologyId,
                        newOntology.Name,
                        string.IsNullOrWhiteSpace(provenanceNotes) ? null : provenanceNotes
                    );
                }

                ToastService.ShowSuccess($"Successfully {derivationType}d ontology!");
                showCreateDialog = false;
                await LoadOntologies();
                Navigation.NavigateTo($"ontology/{createdOntology.Id}");
                return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to {DerivationType} ontology {SourceId}", derivationType, selectedSourceOntologyId);
                ToastService.ShowError($"Failed to {derivationType} ontology: {ex.Message}");
                return;
            }
        }

        // Check if we should download and import an ontology from a standard framework
        // Using constants for template keys improves maintainability
        if (startFromType == "template" && OntologyTemplateKeys.IsImportableTemplate(selectedTemplate))
        {
            // Create empty ontology first
            createdOntology = await OntologyService.CreateOntologyAsync(newOntology);

            // Download and import the selected framework
            try
            {
                isImporting = true;
                StateHasChanged();

                var ontologyContent = await DownloadService.GetOntologyAsync(selectedTemplate);
                if (!string.IsNullOrEmpty(ontologyContent))
                {
                    // Parse and merge the ontology content
                    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ontologyContent));
                    var parseResult = await ImportService.ParseTtlFileAsync(stream);

                    if (parseResult.Success && parseResult.ParsedGraph != null)
                    {
                        await ImportService.MergeIntoExistingAsync(createdOntology.Id, parseResult.ParsedGraph, (progress) =>
                        {
                            importProgress = progress;
                            InvokeAsync(StateHasChanged);
                        });

                        // Update ontology metadata if available
                        var source = DownloadService.GetOntologySource(selectedTemplate);
                        if (source != null)
                        {
                            createdOntology.Namespace = source.Namespace;
                            await OntologyService.UpdateOntologyAsync(createdOntology);
                        }
                    }
                }

                isImporting = false;
                importProgress = null;
            }
            catch (Exception ex)
            {
                isImporting = false;
                importProgress = null;
                // Log error but don't fail - user can still work with empty ontology
                Logger.LogWarning(ex, "Failed to import template {Template} for new ontology", selectedTemplate);
            }
        }
        // Create from BFO template if either the template is selected OR the UsesBFO checkbox is checked
        else if (startFromType == "template" && (selectedTemplate == "bfo" || newOntology.UsesBFO))
        {
            // Create from BFO template
            createdOntology = await TemplateService.CreateFromBFOTemplateAsync(
                newOntology.Name,
                newOntology.Description,
                newOntology.Author
            );

            // Add PROV-O concepts if checked
            if (newOntology.UsesProvO)
            {
                createdOntology.UsesProvO = true;
                await OntologyService.UpdateOntologyAsync(createdOntology);
                await TemplateService.AddProvOConceptsAsync(createdOntology);
            }
        }
        else
        {
            // Create empty ontology
            createdOntology = await OntologyService.CreateOntologyAsync(newOntology);

            // Add PROV-O concepts if checked (even for empty ontology)
            if (newOntology.UsesProvO)
            {
                await TemplateService.AddProvOConceptsAsync(createdOntology);
            }
        }

        showCreateDialog = false;
        await LoadOntologies();

        // Navigate to the new ontology
        Navigation.NavigateTo($"ontology/{createdOntology.Id}");
    }

    private void ViewOntology(int id)
    {
        Navigation.NavigateTo($"ontology/{id}");
    }

    private string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1) return "just now";
        if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}d ago";
        if (timeSpan.TotalDays < 30) return $"{(int)(timeSpan.TotalDays / 7)}w ago";
        if (timeSpan.TotalDays < 365) return $"{(int)(timeSpan.TotalDays / 30)}mo ago";

        return $"{(int)(timeSpan.TotalDays / 365)}y ago";
    }

    /// <summary>
    /// Gets a holiday emoji based on the current date
    /// Returns empty string if no holiday is active
    /// </summary>
    private string GetHolidayEmoji(int index)
    {
        var holiday = HolidayService.GetCurrentHoliday();
        return holiday?.GetEmoji(index) ?? string.Empty;
    }
}
