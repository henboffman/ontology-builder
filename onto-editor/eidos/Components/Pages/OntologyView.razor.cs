using Eidos.Models;
using Eidos.Models.DTOs;
using Eidos.Models.Enums;
using Eidos.Models.Events;
using Eidos.Models.ViewState;
using Eidos.Services;
using Eidos.Services.Interfaces;
using Eidos.Components.Shared;
using Eidos.Components.Ontology;
using Eidos.Components.Ontology.Admin;
using VDS.RDF;
using Microsoft.JSInterop;
using static Eidos.Services.TtlExportService;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using OntologyModel = Eidos.Models.Ontology;

namespace Eidos.Components.Pages;

public partial class OntologyView : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public int Id { get; set; }

    // State Management Classes
    private OntologyViewState viewState = new();
    private GraphViewState graphState = new();

    // Fields being migrated to viewState
    // private OntologyModel? ontology; -> viewState.Ontology
    // private ViewMode viewMode = ViewMode.Graph; -> viewState.CurrentViewMode
    // private Concept? selectedConcept = null; -> viewState.SelectedConcept
    // private Relationship? selectedRelationship = null; -> viewState.SelectedRelationship
    // private PermissionLevel? userPermissionLevel; -> viewState.UserPermissionLevel
    // private string? currentUserId; -> viewState.CurrentUserId
    // private Individual? selectedIndividual; -> viewState.SelectedIndividual

    // Fields being migrated to graphState
    // private string graphColorMode = "concept"; -> graphState.ColorMode

    // Fields that remain in component (dialog state, editing state, etc.)
    private string? loadError;
    private bool showBulkCreate = false;

    [Parameter]
    [SupplyParameterFromQuery(Name = "guestSession")]
    public string? GuestSessionToken { get; set; }

    private GraphViewContainer? graphViewContainer;
    private AddConceptFloatingPanel? addConceptFloatingPanel;
    private AdminConceptDialog? adminConceptDialog;
    private MergeRequestDialog? mergeRequestDialog;
    private MergeRequestListPanel? mergeRequestListPanel;
    private GlobalSearch? globalSearch;
    private ElementReference mainContainer;
    private bool hasPendingMergeRequests = false;
    private int pendingMergeRequestCount = 0;

    // Concept dialog state (moved from ConceptManagementPanel)
    private bool showAddConcept = false;
    private Concept? editingConcept = null;
    private Concept newConcept = new();
    private string? lastUsedConceptColor = null;  // Persists color across consecutive concept creation

    // Individual dialog state (moved from IndividualManagementPanel)
    private bool showAddIndividual = false;
    private Individual? editingIndividual = null;
    private Individual newIndividual = new();
    private List<IndividualProperty> newIndividualProperties = new();

    // Relationship dialog state (moved from RelationshipManagementPanel)
    private bool showAddRelationship = false;
    private Relationship? editingRelationship = null;
    private Relationship newRelationship = new();
    private string customRelationType = string.Empty;

    private string editingNotes = string.Empty;
    private string sortOption = "category";
    private bool shouldPulseSidebar = false;
    private int? firstSelectedConceptId = null;
    private string activeListTab = "concepts";  // Preserves tab selection in List view

    // Validation
    private ValidationResult? validationResult = null;

    // Hierarchy view state
    private IEnumerable<ConceptHierarchyNode>? hierarchyNodes;

    // Backward compatibility properties that delegate to viewState
    private OntologyModel? ontology
    {
        get => viewState.Ontology;
        set => viewState.SetOntology(value);
    }

    private ViewMode viewMode
    {
        get => viewState.CurrentViewMode;
        set => viewState.SetViewMode(value);
    }

    private Concept? selectedConcept
    {
        get => viewState.SelectedConcept;
        set
        {
            viewState.SetSelectedConcept(value);
            // Load properties when concept is selected
            _ = LoadSelectedConceptPropertiesAsync();
        }
    }

    private ICollection<ConceptProperty>? selectedConceptProperties = null;

    private Relationship? selectedRelationship
    {
        get => viewState.SelectedRelationship;
        set => viewState.SetSelectedRelationship(value);
    }

    private Individual? selectedIndividual
    {
        get => viewState.SelectedIndividual;
        set => viewState.SetSelectedIndividual(value);
    }

    private int? selectedConceptId => viewState.SelectedConceptId;

    private PermissionLevel? userPermissionLevel
    {
        get => viewState.UserPermissionLevel;
        set => viewState.SetPermissionLevel(value);
    }

    private string? currentUserId
    {
        get => viewState.CurrentUserId;
        set => viewState.SetCurrentUserId(value);
    }

    private IEnumerable<Individual>? individuals
    {
        get => viewState.Individuals;
        set => viewState.SetIndividuals(value);
    }

    // Backward compatibility property for graphColorMode
    private string graphColorMode
    {
        get => graphState.ColorMode;
        set => graphState.SetColorMode(value);
    }

    // Restrictions view state
    private IEnumerable<ConceptRestriction>? selectedConceptRestrictions;

    // Settings dialog state
    private bool showSettingsDialog = false;
    private string editingOntologyName = string.Empty;
    private string? editingOntologyDescription;
    private string? editingOntologyAuthor;
    private string? editingOntologyVersion;
    private string? editingOntologyNamespace;
    private string? editingOntologyTags;
    private string? editingOntologyLicense;

    // Presence tracking
    private List<PresenceInfo> presenceUsers = new();
    private System.Timers.Timer? heartbeatTimer;

    // Recent items tracking
    private List<RecentItem> recentItems = new();
    private bool editingOntologyUsesBFO;
    private bool editingOntologyUsesProvO;
    private string editingOntologyVisibility = OntologyVisibility.Private;
    private bool editingOntologyAllowPublicEdit = false;

    // TTL Import state
    private bool showImportDialog = false;
    private TtlImportDialog.ImportStep importStep = TtlImportDialog.ImportStep.Upload;
    private bool isProcessingFile = false;
    private bool isImporting = false;
    private string? importError = null;
    private TtlImportResult? importResult = null;
    private MergePreview? mergePreview = null;
    private TtlImportDialog.ImportOption importOption = TtlImportDialog.ImportOption.Merge;

    // Export Dialog
    private ExportDialog? exportDialog;

    // Share/Fork/Clone/Lineage dialogs
    private bool showShareModal = false;
    private OntologyLineage? lineageComponent;
    private ForkCloneDialog? forkCloneDialog;
    private LinkOntologyDialog? linkOntologyDialog;

    // SignalR real-time collaboration
    private DotNetObjectReference<OntologyView>? dotNetRef;
    private bool hasRendered = false;

    // Keyboard shortcuts banner
    private bool showKeyboardShortcutsBanner = true;
    private bool showGlobalSearchBanner = true;

    // What's New feature
    private WhatsNewDto? whatsNew = null;
    private string currentSessionId = Guid.NewGuid().ToString(); // Generate unique session ID for this browser session

    // Notes for search
    private List<Note> workspaceNotes = new();

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to state change events
        viewState.OnStateChanged += HandleStateChanged;
        graphState.OnGraphStateChanged += HandleGraphStateChanged;

        try
        {
            // Get current user ID
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            currentUserId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Load user preferences for keyboard shortcuts banner
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var preferences = await PreferencesService.GetPreferencesAsync(currentUserId);
                showKeyboardShortcutsBanner = preferences.ShowKeyboardShortcuts;
                showGlobalSearchBanner = preferences.ShowGlobalSearchBanner;
            }

            ontology = await OntologyService.GetOntologyAsync(Id);

            if (ontology == null)
            {
                loadError = $"Ontology with ID {Id} was not found. You may not have permission to access this ontology, or it may have been deleted.";
                return;
            }

            // Load workspace notes for search (with content for searchability)
            try
            {
                Logger.LogInformation("[OnParametersSetAsync] Fetching workspace for ontology {OntologyId}", Id);
                var workspace = await WorkspaceService.EnsureWorkspaceForOntologyAsync(Id);
                if (workspace != null)
                {
                    Logger.LogInformation("[OnParametersSetAsync] Workspace found: {WorkspaceId}, loading notes with content", workspace.Id);
                    workspaceNotes = await NoteRepository.GetByWorkspaceIdWithContentAsync(workspace.Id);
                    Logger.LogInformation("[OnParametersSetAsync] Loaded {Count} workspace notes for search in ontology {OntologyId}", workspaceNotes.Count, Id);
                }
                else
                {
                    Logger.LogWarning("[OnParametersSetAsync] Workspace is null for ontology {OntologyId}", Id);
                    workspaceNotes = new List<Note>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[OnParametersSetAsync] Failed to load workspace notes for ontology {OntologyId}", Id);
                workspaceNotes = new List<Note>();
            }

            // IMPORTANT: Check share permissions FIRST before regular permissions
            // This allows users with share links to access ontologies they wouldn't normally have access to
            var sharePermission = await ShareService.GetPermissionLevelAsync(ontology.Id, currentUserId, GuestSessionToken);

            if (sharePermission.HasValue)
            {
                // User has valid share access (either via link or guest session)
                userPermissionLevel = sharePermission;
            }
            else
            {
                // No share permission, check regular ontology visibility permissions
                var canView = await PermissionService.CanViewAsync(ontology.Id, currentUserId);
                if (!canView)
                {
                    loadError = "You do not have permission to view this ontology.";
                    return;
                }

                // Check if user can edit based on ontology visibility permissions
                var canEdit = await PermissionService.CanEditAsync(ontology.Id, currentUserId);

                if (canEdit)
                {
                    // User can edit based on ontology visibility/ownership
                    userPermissionLevel = PermissionLevel.FullAccess;
                }
                else if (canView)
                {
                    // User can only view
                    userPermissionLevel = PermissionLevel.View;
                }
                else
                {
                    // No access (shouldn't reach here due to canView check above)
                    loadError = "You do not have permission to access this ontology.";
                    return;
                }
            }

            // Load validation results
            await LoadValidation();

            // Load recent items
            LoadRecentItems();

            // Load pending merge requests count (for managers/owners)
            await LoadPendingMergeRequestCount();

            // Load "What's New" data and record view
            await LoadWhatsNewAsync();
        }
        catch (Exception ex)
        {
            loadError = $"An error occurred while loading the ontology: {ex.Message}";
        }
    }

    private async Task LoadPendingMergeRequestCount()
    {
        // Check if there are pending merge requests (for admins)
        Console.WriteLine($"[MR Check] CanManage: {viewState.CanManage}, UserId: {currentUserId}");
        if (viewState.CanManage && !string.IsNullOrEmpty(currentUserId))
        {
            var mergeRequests = await MergeRequestService.GetMergeRequestsForOntologyAsync(Id);
            Console.WriteLine($"[MR Check] Total MRs: {mergeRequests.Count()}");
            var pendingRequests = mergeRequests.Where(mr =>
                mr.Status == Models.Enums.MergeRequestStatus.PendingReview ||
                mr.Status == Models.Enums.MergeRequestStatus.ChangesRequested).ToList();
            Console.WriteLine($"[MR Check] Pending MRs: {pendingRequests.Count}");
            hasPendingMergeRequests = pendingRequests.Any();
            pendingMergeRequestCount = pendingRequests.Count;
        }
        else
        {
            Console.WriteLine($"[MR Check] Skipping - not authorized");
            hasPendingMergeRequests = false;
            pendingMergeRequestCount = 0;
        }
    }

    private async Task LoadOntology()
    {
        Logger.LogInformation("[LoadOntology] Starting LoadOntology for ontology {OntologyId}", Id);
        ontology = await OntologyService.GetOntologyAsync(Id);

        // Load workspace notes for search (with content for searchability)
        try
        {
            Logger.LogInformation("[LoadOntology] Fetching workspace for ontology {OntologyId}", Id);
            var workspace = await WorkspaceService.EnsureWorkspaceForOntologyAsync(Id);
            if (workspace != null)
            {
                Logger.LogInformation("[LoadOntology] Workspace found: {WorkspaceId}, loading notes with content", workspace.Id);
                workspaceNotes = await NoteRepository.GetByWorkspaceIdWithContentAsync(workspace.Id);
                Logger.LogInformation("[LoadOntology] Loaded {Count} workspace notes for search in ontology {OntologyId}", workspaceNotes.Count, Id);
            }
            else
            {
                Logger.LogWarning("[LoadOntology] Workspace is null for ontology {OntologyId}", Id);
                workspaceNotes = new List<Note>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[LoadOntology] Failed to load workspace notes for ontology {OntologyId}", Id);
            workspaceNotes = new List<Note>();
        }

        // Reload merge request count
        await LoadPendingMergeRequestCount();

        // Note: We don't reload "What's New" here because that's only for initial page load
        // to show what OTHER users changed since the last visit. During the current session,
        // we don't want to reset the panel.

        if (graphViewContainer != null && viewMode == ViewMode.Graph)
        {
            await Task.Delay(100);
            await graphViewContainer.RefreshGraph();
        }
    }

    private async Task LoadWhatsNewAsync()
    {
        Console.WriteLine($"[OntologyView] LoadWhatsNewAsync called with session {currentSessionId}");
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            Console.WriteLine($"[OntologyView] User authenticated: {user?.Identity?.IsAuthenticated}");

            if (user?.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"[OntologyView] UserId: {userId}, SessionId: {currentSessionId}");
                if (!string.IsNullOrEmpty(userId))
                {
                    // Record the view with current session ID
                    await ViewHistoryService.RecordViewAsync(Id, userId, currentSessionId);
                    Console.WriteLine($"[OntologyView] Recorded view for session {currentSessionId}");

                    // Get what's new data (changes from OTHER users since last session)
                    whatsNew = await ViewHistoryService.GetWhatsNewAsync(Id, userId, currentSessionId);
                    Console.WriteLine($"[OntologyView] WhatsNew result: {(whatsNew != null ? $"{whatsNew.TotalChanges} changes" : "null")}");

                    // Trigger re-render to show the panel
                    StateHasChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load what's new for ontology {OntologyId}", Id);
            // Don't fail the page load if this fails
        }
    }

    private async Task HandleWhatsNewDismiss()
    {
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await ViewHistoryService.DismissWhatsNewAsync(Id, userId, currentSessionId);
                    Console.WriteLine($"[OntologyView] Dismissed what's new for session {currentSessionId}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to dismiss what's new for ontology {OntologyId}", Id);
        }
    }

    private void GoBack()
    {
        Navigation.NavigateTo("");
    }

    // Global Search Handlers
    private void HandleKeyDown(KeyboardEventArgs e)
    {
        // Cmd+K (Mac) or Ctrl+K (Windows/Linux)
        if ((e.MetaKey || e.CtrlKey) && e.Key == "k" && !e.ShiftKey && !e.AltKey)
        {
            globalSearch?.Show();
        }
    }

    private void HandleGlobalSearch()
    {
        globalSearch?.Show();
    }

    private async Task RestoreFocusAfterSearch()
    {
        // Restore focus to main container so keyboard shortcut works again
        try
        {
            await Task.Delay(100); // Small delay to ensure dialog is closed
            await mainContainer.FocusAsync();
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to restore focus after search");
        }
    }

    private async Task HandleSearchResultSelected(SearchResult result)
    {
        if (result == null) return;

        try
        {
            switch (result.Type)
            {
                case "Concept":
                    // Context-aware navigation based on current view mode
                    if (viewMode == ViewMode.Graph)
                    {
                        // Stay in graph view and zoom to the concept
                        selectedConcept = ontology?.Concepts.FirstOrDefault(c => c.Id == result.Id);
                        StateHasChanged();
                        await Task.Delay(50); // Allow state to propagate

                        if (graphViewContainer != null)
                        {
                            var zoomed = await graphViewContainer.ZoomToConcept(result.Id);
                            if (zoomed)
                            {
                                ToastService.ShowSuccess($"Zoomed to concept: {result.Title}");
                            }
                            else
                            {
                                ToastService.ShowWarning($"Concept not found in graph: {result.Title}");
                            }
                        }
                    }
                    else
                    {
                        // Switch to list view and select the concept
                        viewMode = ViewMode.List;
                        selectedConcept = ontology?.Concepts.FirstOrDefault(c => c.Id == result.Id);
                        ToastService.ShowSuccess($"Navigated to concept: {result.Title}");
                    }
                    break;

                case "Relationship":
                    // Switch to list view and select the relationship
                    viewMode = ViewMode.List;
                    selectedRelationship = ontology?.Relationships.FirstOrDefault(r => r.Id == result.Id);
                    ToastService.ShowSuccess($"Navigated to relationship: {result.Title}");
                    break;

                case "Individual":
                    // Switch to list view and select the individual
                    viewMode = ViewMode.List;
                    selectedIndividual = ontology?.Individuals.FirstOrDefault(i => i.Id == result.Id);
                    ToastService.ShowSuccess($"Navigated to individual: {result.Title}");
                    break;

                case "Note":
                    // Navigate to workspace view with the note selected
                    var note = workspaceNotes.FirstOrDefault(n => n.Id == result.Id);
                    if (note != null)
                    {
                        Navigation.NavigateTo($"workspace/{note.WorkspaceId}?noteId={note.Id}");
                        ToastService.ShowSuccess($"Opening note: {result.Title}");
                    }
                    else
                    {
                        ToastService.ShowWarning($"Note not found: {result.Title}");
                    }
                    break;
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error navigating to search result: {Type} {Id}", result.Type, result.Id);
            ToastService.ShowError("Failed to navigate to result");
        }
    }

    private async Task HandleActionExecuted(string actionId)
    {
        try
        {
            switch (actionId)
            {
                case "add-concept":
                    ShowAddConceptDialog();
                    break;

                case "add-relationship":
                    ShowAddRelationshipDialog();
                    break;

                case "export-ttl":
                case "export-json":
                    // Both export actions open the export dialog which allows choosing format
                    ShowExportDialog();
                    break;

                case "import-ttl":
                    ShowImportDialog();
                    break;

                case "toggle-theme":
                    await ToggleThemeAsync();
                    break;

                case "view-graph":
                    viewMode = ViewMode.Graph;
                    StateHasChanged();
                    ToastService.ShowSuccess("Switched to Graph View");
                    break;

                case "view-list":
                    viewMode = ViewMode.List;
                    StateHasChanged();
                    ToastService.ShowSuccess("Switched to List View");
                    break;

                case "view-hierarchy":
                    viewMode = ViewMode.Hierarchy;
                    StateHasChanged();
                    ToastService.ShowSuccess("Switched to Hierarchy View");
                    break;

                case "settings":
                    ShowSettingsDialog();
                    break;

                default:
                    Logger.LogWarning("Unknown action: {ActionId}", actionId);
                    ToastService.ShowWarning($"Action not implemented: {actionId}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing action: {ActionId}", actionId);
            ToastService.ShowError($"Failed to execute action: {actionId}");
        }
    }

    private async Task ToggleThemeAsync()
    {
        try
        {
            // Get current theme preference
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                // For non-authenticated users, toggle via JS
                await JS.InvokeVoidAsync("ThemeHandler.toggleTheme");
                ToastService.ShowSuccess("Theme toggled");
                return;
            }

            // Get current preferences
            var prefs = await PreferencesService.GetPreferencesAsync(userId);
            var newTheme = prefs.Theme == "dark" ? "light" : "dark";

            // Update theme preference in database
            await PreferencesService.UpdateThemeAsync(newTheme);

            // Apply theme via JS
            await JS.InvokeVoidAsync("ThemeHandler.setTheme", newTheme);

            ToastService.ShowSuccess($"Switched to {newTheme} mode");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error toggling theme");
            ToastService.ShowError("Failed to toggle theme");
        }
    }

    private async Task HandleUndo()
    {
        var success = await OntologyService.UndoAsync();
        if (success)
        {
            await LoadOntology();
            StateHasChanged();
        }
    }

    private async Task HandleRedo()
    {
        var success = await OntologyService.RedoAsync();
        if (success)
        {
            await LoadOntology();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Gets all concepts connected to the selected concept via relationships.
    /// Overload that uses the currently selected concept from state.
    /// </summary>
    private List<SelectedNodeDetailsPanel.ConceptConnection> GetConnectedConcepts()
    {
        if (selectedConcept == null)
            return new List<SelectedNodeDetailsPanel.ConceptConnection>();

        return GetConnectedConcepts(selectedConcept.Id);
    }

    /// <summary>
    /// Gets all concepts connected to a specific concept via relationships.
    /// Returns connections ordered by concept name.
    /// </summary>
    /// <param name="conceptId">The ID of the concept to find connections for</param>
    private List<SelectedNodeDetailsPanel.ConceptConnection> GetConnectedConcepts(int conceptId)
    {
        if (ontology == null)
            return new List<SelectedNodeDetailsPanel.ConceptConnection>();

        var connections = new List<SelectedNodeDetailsPanel.ConceptConnection>();

        // Find all relationships where this concept is either source or target
        foreach (var relationship in ontology.Relationships)
        {
            int? connectedConceptId = null;
            bool isOutgoing = false;

            if (relationship.SourceConceptId == conceptId)
            {
                connectedConceptId = relationship.TargetConceptId;
                isOutgoing = true;
            }
            else if (relationship.TargetConceptId == conceptId)
            {
                connectedConceptId = relationship.SourceConceptId;
                isOutgoing = false;
            }

            if (connectedConceptId.HasValue)
            {
                var connectedConcept = ontology.Concepts.FirstOrDefault(c => c.Id == connectedConceptId.Value);
                if (connectedConcept != null)
                {
                    connections.Add(new SelectedNodeDetailsPanel.ConceptConnection
                    {
                        ConceptId = connectedConcept.Id,
                        ConceptName = connectedConcept.Name,
                        ConceptColor = connectedConcept.Color ?? "#4A90E2",
                        RelationType = relationship.RelationType,
                        IsOutgoing = isOutgoing
                    });
                }
            }
        }

        return connections.OrderBy(c => c.ConceptName).ToList();
    }

    /// <summary>
    /// Loads property definitions for the currently selected concept
    /// </summary>
    private async Task LoadSelectedConceptPropertiesAsync()
    {
        if (selectedConcept == null)
        {
            selectedConceptProperties = null;
            return;
        }

        try
        {
            selectedConceptProperties = await ConceptPropertyService.GetPropertiesByConceptIdAsync(selectedConcept.Id);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load properties for concept {ConceptId}", selectedConcept.Id);
            selectedConceptProperties = new List<ConceptProperty>();
        }
    }

    /// <summary>
    /// Handles changes to concept properties (add/edit/delete)
    /// </summary>
    private async Task HandlePropertiesChanged()
    {
        await LoadSelectedConceptPropertiesAsync();
        StateHasChanged();
    }

    private async Task SwitchViewMode(ViewMode mode)
    {
        viewMode = mode;
        CancelConceptDialog();
        CancelRelationshipDialog();
        selectedConcept = null;

        // Update presence with current view
        await UpdateCurrentView(mode.ToString());

        if (mode == ViewMode.Notes)
        {
            editingNotes = ontology?.Notes ?? string.Empty;
        }
        else if (mode == ViewMode.Hierarchy)
        {
            if (ontology != null)
            {
                hierarchyNodes = await ConceptService.GetHierarchyAsync(ontology.Id);
            }
        }
        else if (mode == ViewMode.Instances)
        {
            if (ontology != null)
            {
                individuals = await IndividualService.GetByOntologyIdAsync(ontology.Id);
            }
        }
    }

    private async Task NavigateToUserView(string viewName)
    {
        // Convert the view name string to ViewMode enum
        if (Enum.TryParse<ViewMode>(viewName, true, out var targetView))
        {
            await SwitchViewMode(targetView);
            ToastService.ShowInfo($"Navigated to {viewName} view");
        }
        else
        {
            Logger.LogWarning("Unable to navigate to view: {ViewName}", viewName);
        }
    }

    private async Task ShowAddConceptDialog()
    {
        string colorToUse;

        // Use last used color if available, otherwise load from preferences
        if (!string.IsNullOrWhiteSpace(lastUsedConceptColor))
        {
            colorToUse = lastUsedConceptColor;
            Logger.LogInformation("Using last used color {Color} for new concept", colorToUse);
        }
        else
        {
            try
            {
                var prefs = await PreferencesService.GetCurrentUserPreferencesAsync();
                colorToUse = prefs.DefaultConceptColor;
                Logger.LogInformation("Applied default color {Color} from preferences", colorToUse);
            }
            catch (Exception ex)
            {
                // Fallback to random color if preferences fail to load
                Logger.LogError(ex, "Failed to load user preferences for concept creation, using random color");
                colorToUse = "#" + Random.Shared.Next(0x1000000).ToString("X6");
            }
        }

        newConcept = new Concept
        {
            OntologyId = Id,
            Color = colorToUse
        };

        showAddConcept = true;
        editingConcept = null;

        CancelRelationshipDialog();
        CancelIndividualDialog();
        selectedConcept = null;
        StateHasChanged();
    }

    private void ShowBulkCreateDialog()
    {
        showBulkCreate = true;

        // Hide all other dialogs
        CancelConceptDialog();
        CancelRelationshipDialog();
        CancelIndividualDialog();
    }

    private async Task ShowLinkOntologyDialog()
    {
        if (linkOntologyDialog != null)
        {
            await linkOntologyDialog.ShowAsync();
        }
    }

    private async Task HandleOntologyLinked()
    {
        // Reload the ontology to reflect the newly linked ontology
        await LoadOntology();
        StateHasChanged();
    }

    private async Task ShowAdminConceptDialog()
    {
        if (adminConceptDialog != null)
        {
            await adminConceptDialog.Show();
        }
    }

    private async Task HandleAdminDialogChanged()
    {
        // Reload ontology data after admin dialog makes changes
        await LoadOntology();
        StateHasChanged();
    }

    private async Task ShowMergeRequestDialog()
    {
        if (mergeRequestDialog != null)
        {
            await mergeRequestDialog.Show();
        }
    }

    private async Task HandleMergeRequestCreated()
    {
        ToastService.ShowSuccess("Merge request created successfully! An administrator will review your changes.");
        await LoadOntology();

        // Refresh the merge request list panel if it exists
        if (mergeRequestListPanel != null)
        {
            await mergeRequestListPanel.Refresh();
        }

        StateHasChanged();
    }

    private void HandleViewMergeRequests()
    {
        // Switch to merge requests view
        viewMode = ViewMode.MergeRequests;
        ToastService.ShowInfo($"Showing {pendingMergeRequestCount} pending merge request(s)");
        StateHasChanged();
    }

    private async Task OnBulkCreateComplete()
    {
        showBulkCreate = false;
        await LoadOntology();
        ToastService.ShowSuccess("Bulk creation completed successfully!");
    }

    private async Task HandleConceptNameUpdate((int conceptId, string newName) update)
    {
        try
        {
            // Get current user ID
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                ToastService.ShowError("You must be logged in");
                return;
            }

            var success = await ConceptService.UpdateConceptNameAsync(update.conceptId, update.newName, userId);

            if (success)
            {
                // Update local state
                var concept = ontology.Concepts.FirstOrDefault(c => c.Id == update.conceptId);
                if (concept != null)
                {
                    concept.Name = update.newName;
                    StateHasChanged();
                }

                ToastService.ShowSuccess("Concept name updated");
            }
            else
            {
                ToastService.ShowError("Failed to update concept name");
                // Reload to ensure consistency
                await LoadOntology();
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error updating concept: {ex.Message}");
            await LoadOntology();
        }
    }

    private void EditConcept(Concept concept)
    {
        editingConcept = concept;
        newConcept = new Concept
        {
            Id = concept.Id,
            OntologyId = concept.OntologyId,
            Name = concept.Name,
            Category = concept.Category,
            SimpleExplanation = concept.SimpleExplanation,
            Definition = concept.Definition,
            Examples = concept.Examples,
            Color = concept.Color,
            SourceOntology = concept.SourceOntology
        };
        showAddConcept = true;

        CancelRelationshipDialog();
        CancelIndividualDialog();
        selectedConcept = null;
        StateHasChanged();
    }

    private async Task DuplicateConcept(Concept concept)
    {
        newConcept = new Concept
        {
            OntologyId = Id,
            Name = $"{concept.Name} (Copy)",
            Category = concept.Category,
            SimpleExplanation = concept.SimpleExplanation,
            Definition = concept.Definition,
            Examples = concept.Examples,
            Color = concept.Color
        };
        editingConcept = null;
        showAddConcept = true;

        CancelRelationshipDialog();
        CancelIndividualDialog();
        selectedConcept = null;

        shouldPulseSidebar = true;
        StateHasChanged();
        await Task.Delay(3600);
        shouldPulseSidebar = false;

        ToastService.ShowInfo($"Ready to create copy of \"{concept.Name}\". Click 'Add' to save or 'Cancel' to discard.", 5000);
    }

    private async Task SaveConcept()
    {
        if (string.IsNullOrWhiteSpace(newConcept.Name))
            return;

        // Check permissions
        if (editingConcept != null && userPermissionLevel < PermissionLevel.ViewAddEdit)
        {
            ToastService.ShowError("You do not have permission to edit concepts");
            return;
        }
        else if (editingConcept == null && userPermissionLevel < PermissionLevel.ViewAndAdd)
        {
            ToastService.ShowError("You do not have permission to add concepts");
            return;
        }

        try
        {
            if (editingConcept != null)
            {
                // Update existing concept
                editingConcept.Name = newConcept.Name;
                editingConcept.Category = newConcept.Category;
                editingConcept.SimpleExplanation = newConcept.SimpleExplanation;
                editingConcept.Definition = newConcept.Definition;
                editingConcept.Examples = newConcept.Examples;
                editingConcept.Color = newConcept.Color;
                editingConcept.SourceOntology = newConcept.SourceOntology;

                await ConceptService.UpdateAsync(editingConcept);
                ToastService.ShowSuccess($"Updated concept \"{newConcept.Name}\"");
                editingConcept = null;
            }
            else
            {
                // Create new concept
                newConcept.OntologyId = Id;
                await ConceptService.CreateAsync(newConcept);
                ToastService.ShowSuccess($"Created concept \"{newConcept.Name}\"");
            }

            showAddConcept = false;
            newConcept = new Concept();
            await LoadOntology();
        }
        catch (Exception ex)
        {
            var errorMessage = ex.InnerException != null
                ? $"{ex.Message} - {ex.InnerException.Message}"
                : ex.Message;
            ToastService.ShowError($"Failed to save concept: {errorMessage}", 8000);
        }
    }

    private async Task SaveConceptAndAddAnother()
    {
        if (string.IsNullOrWhiteSpace(newConcept.Name))
            return;

        // Check permissions
        if (userPermissionLevel < PermissionLevel.ViewAndAdd)
        {
            ToastService.ShowError("You do not have permission to add concepts");
            return;
        }

        try
        {
            var conceptName = newConcept.Name;
            newConcept.OntologyId = Id;
            await ConceptService.CreateAsync(newConcept);
            ToastService.ShowSuccess($"Created concept \"{conceptName}\"", 2000);

            // Get user preferences for the new concept defaults
            var prefs = await PreferencesService.GetCurrentUserPreferencesAsync();

            // Clear the form with fresh defaults
            newConcept = new Concept
            {
                OntologyId = Id,
                Color = prefs.DefaultConceptColor,
                Category = newConcept.Category // Keep the same category for convenience
            };

            // Keep the form open
            showAddConcept = true;
            await LoadOntology();
            StateHasChanged();

            // Focus the name input field
            await FocusConceptNameInput();
        }
        catch (Exception ex)
        {
            var errorMessage = ex.InnerException != null
                ? $"{ex.Message} - {ex.InnerException.Message}"
                : ex.Message;
            ToastService.ShowError($"Failed to save concept: {errorMessage}", 8000);
        }
    }

    private void CancelConceptDialog()
    {
        showAddConcept = false;
        editingConcept = null;
        newConcept = new Concept();
        StateHasChanged();
    }

    private void OnConceptColorChanged(string? color)
    {
        newConcept.Color = color;

        // Update last used color when user manually changes it
        if (!string.IsNullOrWhiteSpace(color))
        {
            lastUsedConceptColor = color;
            Logger.LogInformation("Updated last used concept color to {Color}", color);
        }
    }

    private async Task OnConceptCategoryChanged(string? category)
    {
        newConcept.Category = category;

        // Auto-apply color based on category if preferences allow
        try
        {
            var prefs = await PreferencesService.GetCurrentUserPreferencesAsync();

            if (prefs.AutoColorByCategory)
            {
                var autoColor = prefs.GetColorForCategory(category);
                newConcept.Color = autoColor;

                // Also update last used color when auto-applying
                lastUsedConceptColor = autoColor;
                Logger.LogInformation("Auto-applied and saved color {Color} for category {Category}", autoColor, category);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to auto-apply color for category {Category}", category);
        }
    }

    private void ApplyConceptTemplate(string templateValue)
    {
        if (string.IsNullOrWhiteSpace(templateValue))
            return;

        // Check if it's a custom template (format: "custom:123") or default (format: "default:Entity")
        if (templateValue.StartsWith("custom:"))
        {
            var templateIdStr = templateValue.Substring(7); // Remove "custom:" prefix
            if (int.TryParse(templateIdStr, out var templateId))
            {
                var customTemplate = ontology?.CustomTemplates?.FirstOrDefault(t => t.Id == templateId);
                if (customTemplate != null)
                {
                    newConcept.Category = customTemplate.Category;
                    newConcept.SimpleExplanation = customTemplate.Description;
                    newConcept.Examples = customTemplate.Examples;
                    newConcept.Color = customTemplate.Color;
                    ToastService.ShowInfo($"Applied custom template '{customTemplate.Category}' - customize as needed");
                }
            }
        }
        else if (templateValue.StartsWith("default:"))
        {
            var templateCategory = templateValue.Substring(8); // Remove "default:" prefix
            var template = CommonConceptTemplates.Templates.FirstOrDefault(t => t.Category == templateCategory);
            if (template != null)
            {
                newConcept.Category = template.Category;
                newConcept.SimpleExplanation = template.Description;
                newConcept.Examples = template.Examples;
                newConcept.Color = template.Color;
                ToastService.ShowInfo($"Applied {template.Category} template - customize as needed");
            }
        }
    }

    private async Task FocusConceptNameInput()
    {
        try
        {
            await JS.InvokeVoidAsync("setTimeout", new object[]
            {
                DotNetObjectReference.Create(new FocusHelper(async () =>
                {
                    await JS.InvokeVoidAsync("eval", "document.querySelector('.concept-name-input')?.focus()");
                })),
                100
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to focus concept name input");
        }
    }

    // Helper class for JS callback
    private class FocusHelper
    {
        private readonly Func<Task> _action;
        public FocusHelper(Func<Task> action) => _action = action;

        [JSInvokable]
        public async Task Invoke() => await _action();
    }

    #region Relationship Management Methods

    private Task ShowAddRelationshipDialog()
    {
        newRelationship = new Relationship { OntologyId = Id };
        customRelationType = string.Empty;
        showAddRelationship = true;
        editingRelationship = null;
        CancelConceptDialog();
        CancelIndividualDialog();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task ShowAddRelationshipDialog(int sourceConceptId, int targetConceptId)
    {
        newRelationship = new Relationship
        {
            OntologyId = Id,
            SourceConceptId = sourceConceptId,
            TargetConceptId = targetConceptId
        };
        customRelationType = string.Empty;
        showAddRelationship = true;
        editingRelationship = null;
        CancelConceptDialog();
        CancelIndividualDialog();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void ShowEditRelationshipDialog(Relationship relationship)
    {
        editingRelationship = relationship;
        newRelationship = new Relationship
        {
            Id = relationship.Id,
            OntologyId = relationship.OntologyId,
            SourceConceptId = relationship.SourceConceptId,
            TargetConceptId = relationship.TargetConceptId,
            RelationType = relationship.RelationType,
            Label = relationship.Label,
            Description = relationship.Description
        };
        customRelationType = string.Empty;
        showAddRelationship = true;
        CancelConceptDialog();
        CancelIndividualDialog();
        StateHasChanged();
    }

    private Task ShowDuplicateRelationshipDialog(Relationship relationship)
    {
        newRelationship = new Relationship
        {
            OntologyId = Id,
            SourceConceptId = relationship.SourceConceptId,
            TargetConceptId = relationship.TargetConceptId,
            RelationType = relationship.RelationType,
            Label = relationship.Label,
            Description = relationship.Description
        };
        customRelationType = string.Empty;
        editingRelationship = null;
        showAddRelationship = true;
        CancelConceptDialog();
        CancelIndividualDialog();
        StateHasChanged();

        ToastService.ShowInfo("Ready to create copy of relationship. Click 'Add' to save or 'Cancel' to discard.", 5000);
        return Task.CompletedTask;
    }

    private void CancelRelationshipDialog()
    {
        showAddRelationship = false;
        editingRelationship = null;
        newRelationship = new Relationship();
        customRelationType = string.Empty;
        StateHasChanged();
    }

    private async Task SaveRelationship()
    {
        // Check permissions
        if (editingRelationship != null && userPermissionLevel < PermissionLevel.ViewAddEdit)
        {
            ToastService.ShowError("You do not have permission to edit relationships");
            return;
        }
        else if (editingRelationship == null && userPermissionLevel < PermissionLevel.ViewAndAdd)
        {
            ToastService.ShowError("You do not have permission to add relationships");
            return;
        }

        var sourceConcept = ontology?.Concepts.FirstOrDefault(c => c.Id == newRelationship.SourceConceptId);
        var targetConcept = ontology?.Concepts.FirstOrDefault(c => c.Id == newRelationship.TargetConceptId);

        if (sourceConcept == null || targetConcept == null)
        {
            ToastService.ShowError("Source or target concept not found");
            return;
        }

        // Use custom relationship type if specified
        if (!string.IsNullOrWhiteSpace(customRelationType))
        {
            newRelationship.RelationType = customRelationType;
        }

        if (string.IsNullOrWhiteSpace(newRelationship.RelationType))
        {
            ToastService.ShowError("Please select or enter a relationship type");
            return;
        }

        try
        {
            if (editingRelationship != null)
            {
                // Update existing relationship
                editingRelationship.SourceConceptId = newRelationship.SourceConceptId;
                editingRelationship.TargetConceptId = newRelationship.TargetConceptId;
                editingRelationship.RelationType = newRelationship.RelationType;
                editingRelationship.Label = newRelationship.Label;
                editingRelationship.Description = newRelationship.Description;

                await OntologyService.UpdateRelationshipAsync(editingRelationship);
                ToastService.ShowSuccess("Updated relationship");
                editingRelationship = null;
            }
            else
            {
                // Create new relationship
                newRelationship.OntologyId = Id;
                await OntologyService.CreateRelationshipAsync(newRelationship);
                ToastService.ShowSuccess($"Created relationship: {sourceConcept.Name} → {targetConcept.Name}");
            }

            showAddRelationship = false;
            customRelationType = string.Empty;
            newRelationship = new Relationship();
            await LoadOntology();
        }
        catch (Exception ex)
        {
            var errorMessage = ex.InnerException != null
                ? $"{ex.Message} - {ex.InnerException.Message}"
                : ex.Message;
            ToastService.ShowError($"Failed to save relationship: {errorMessage}", 8000);
        }
    }

    private IEnumerable<string> GetExistingRelationshipTypes()
    {
        if (ontology?.Relationships == null)
            return Enumerable.Empty<string>();

        return ontology.Relationships
            .Select(r => r.RelationType)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .OrderBy(t => t);
    }

    #endregion

    private async Task HandleNodeCtrlClick(int conceptId)
    {
        if (firstSelectedConceptId == null)
        {
            firstSelectedConceptId = conceptId;
            var concept = ontology?.Concepts.FirstOrDefault(c => c.Id == conceptId);
            if (concept != null)
            {
                ToastService.ShowInfo($"First node selected: \"{concept.Name}\". Cmd/Ctrl+click another node to create relationship.", 5000);
            }
        }
        else if (firstSelectedConceptId == conceptId)
        {
            firstSelectedConceptId = null;
            ToastService.ShowInfo("Selection cancelled");
        }
        else
        {
            var sourceConcept = ontology?.Concepts.FirstOrDefault(c => c.Id == firstSelectedConceptId.Value);
            var targetConcept = ontology?.Concepts.FirstOrDefault(c => c.Id == conceptId);

            if (sourceConcept != null && targetConcept != null)
            {
                await ShowAddRelationshipDialog(firstSelectedConceptId.Value, conceptId);
                ToastService.ShowInfo($"Creating relationship: \"{sourceConcept.Name}\" → \"{targetConcept.Name}\"", 3000);
            }

            firstSelectedConceptId = null;
        }
    }

    private void EditRelationship(Relationship relationship)
    {
        ShowEditRelationshipDialog(relationship);
    }

    private async Task DuplicateRelationship(Relationship relationship)
    {
        await ShowDuplicateRelationshipDialog(relationship);
        shouldPulseSidebar = true;
        StateHasChanged();
        await Task.Delay(3600);
        shouldPulseSidebar = false;
    }

    private async Task DeleteConcept(int conceptId)
    {
        if (!CanEdit())
        {
            ToastService.ShowError("You do not have permission to delete concepts");
            return;
        }

        var concept = ontology?.Concepts.FirstOrDefault(c => c.Id == conceptId);
        if (concept == null) return;

        var confirmed = await ConfirmService.ShowAsync(
            "Delete Concept",
            $"Are you sure you want to delete \"{concept.Name}\"? This will also delete all related relationships.",
            "Delete",
            ConfirmType.Danger
        );

        if (confirmed)
        {
            try
            {
                await OntologyService.DeleteConceptAsync(conceptId);
                selectedConcept = null;
                await LoadOntology();
                ToastService.ShowSuccess($"Deleted concept \"{concept.Name}\"");
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Failed to delete concept: {ex.Message}");
            }
        }
    }

    private async Task DeleteRelationship(int relationshipId)
    {
        if (!CanEdit())
        {
            ToastService.ShowError("You do not have permission to delete relationships");
            return;
        }

        var relationship = ontology?.Relationships.FirstOrDefault(r => r.Id == relationshipId);
        if (relationship == null) return;

        var confirmed = await ConfirmService.ShowAsync(
            "Delete Relationship",
            $"Are you sure you want to delete the relationship from \"{relationship.SourceConcept.Name}\" to \"{relationship.TargetConcept.Name}\"?",
            "Delete",
            ConfirmType.Danger
        );

        if (confirmed)
        {
            try
            {
                await OntologyService.DeleteRelationshipAsync(relationshipId);
                await LoadOntology();
                ToastService.ShowSuccess("Deleted relationship");
            }
            catch (Exception ex)
            {
                ToastService.ShowError($"Failed to delete relationship: {ex.Message}");
            }
        }
    }

    private async Task HandleMindMapConceptClick(int conceptId)
    {
        var concept = ontology?.Concepts.FirstOrDefault(c => c.Id == conceptId);
        if (concept != null)
        {
            await SelectConcept(concept);
        }
    }

    private async Task SelectConcept(Concept concept)
    {
        // Load restrictions FIRST (before setting state)
        if (concept != null)
        {
            selectedConceptRestrictions = await RestrictionService.GetByConceptIdAsync(concept.Id);
        }

        // Set selected concept - ViewState.SetSelectedConcept will automatically clear relationship and individual
        selectedConcept = concept;

        // Track in recent items
        if (concept != null && ontology != null && !string.IsNullOrEmpty(currentUserId))
        {
            RecentItemsService.AddRecentConcept(currentUserId, ontology.Id, concept);
            LoadRecentItems();
        }

        CancelConceptDialog();
        CancelRelationshipDialog();

        // Force UI update for the details sidebar
        StateHasChanged();

        // If in Graph view, zoom to the selected concept
        if (concept != null && viewMode == ViewMode.Graph && graphViewContainer != null)
        {
            await Task.Delay(50); // Allow state to propagate
            await graphViewContainer.ZoomToConcept(concept.Id);
        }
    }

    private async Task ViewConceptNoteFromModal(Concept concept)
    {
        if (concept == null)
        {
            ToastService.ShowWarning("Please select a concept first");
            return;
        }

        if (ontology == null)
        {
            ToastService.ShowWarning("Ontology not loaded");
            return;
        }

        try
        {
            // Ensure the ontology has a workspace (for backwards compatibility with legacy ontologies)
            var workspace = await WorkspaceService.EnsureWorkspaceForOntologyAsync(ontology.Id);

            if (workspace == null)
            {
                ToastService.ShowError("Unable to create or access workspace for this ontology");
                return;
            }

            // Find the concept note for this concept
            var note = await NoteRepository.GetConceptNoteAsync(concept.Id);

            if (note != null)
            {
                // Navigate to workspace with the note pre-selected
                Navigation.NavigateTo($"workspace/{note.WorkspaceId}?noteId={note.Id}");
            }
            else
            {
                // Navigate to the workspace - note will be created automatically when needed
                Navigation.NavigateTo($"workspace/{workspace.Id}");
                ToastService.ShowInfo($"Opening workspace. Note for '{concept.Name}' will be created automatically when needed.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error navigating to concept note for concept {ConceptId}", concept.Id);
            ToastService.ShowError($"Error navigating to note: {ex.Message}");
        }
    }

    private async Task NavigateToWorkspaceNotes()
    {
        if (ontology == null)
        {
            ToastService.ShowWarning("Ontology not loaded");
            return;
        }

        try
        {
            // Ensure the ontology has a workspace (for backwards compatibility with legacy ontologies)
            var workspace = await WorkspaceService.EnsureWorkspaceForOntologyAsync(ontology.Id);

            if (workspace == null)
            {
                ToastService.ShowError("Unable to create or access workspace for this ontology");
                return;
            }

            // Navigate to the workspace notes view
            Navigation.NavigateTo($"workspace/{workspace.Id}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error navigating to workspace notes for ontology {OntologyId}", ontology.Id);
            ToastService.ShowError($"Error navigating to workspace notes: {ex.Message}");
        }
    }

    private void SelectRelationship(Relationship relationship)
    {
        // Set selected relationship - ViewState.SetSelectedRelationship will automatically clear concept and individual
        selectedRelationship = relationship;

        // Track in recent items
        if (relationship != null && ontology != null && !string.IsNullOrEmpty(currentUserId))
        {
            RecentItemsService.AddRecentRelationship(currentUserId, ontology.Id, relationship);
            LoadRecentItems();
        }

        CancelConceptDialog();
        CancelRelationshipDialog();

        // Force UI update for the details sidebar
        StateHasChanged();
    }

    private async Task HandleRecentItemSelected(RecentItem item)
    {
        if (item.Type == RecentItemType.Concept)
        {
            var concept = ontology?.Concepts.FirstOrDefault(c => c.Id == item.Id);
            if (concept != null)
            {
                // Switch to List view and Concepts tab
                if (viewMode != ViewMode.List)
                {
                    await SwitchViewMode(ViewMode.List);
                }
                activeListTab = "concepts";

                await SelectConcept(concept);

                // Scroll to the selected concept in the list
                await Task.Delay(100); // Small delay to ensure DOM is updated
                await JS.InvokeVoidAsync("eval", $@"
                    const element = document.getElementById('concept-{concept.Id}');
                    if (element) {{
                        element.scrollIntoView({{ behavior: 'smooth', block: 'center' }});
                        element.classList.add('highlight-flash');
                        setTimeout(() => element.classList.remove('highlight-flash'), 2000);
                    }}
                ");
            }
        }
        else if (item.Type == RecentItemType.Relationship)
        {
            var relationship = ontology?.Relationships.FirstOrDefault(r => r.Id == item.Id);
            if (relationship != null)
            {
                // Switch to List view and Relationships tab
                if (viewMode != ViewMode.List)
                {
                    await SwitchViewMode(ViewMode.List);
                }
                activeListTab = "relationships";

                SelectRelationship(relationship);

                // Scroll to the selected relationship in the list
                await Task.Delay(100); // Small delay to ensure DOM is updated
                await JS.InvokeVoidAsync("eval", $@"
                    const element = document.getElementById('relationship-{relationship.Id}');
                    if (element) {{
                        element.scrollIntoView({{ behavior: 'smooth', block: 'center' }});
                        element.classList.add('highlight-flash');
                        setTimeout(() => element.classList.remove('highlight-flash'), 2000);
                    }}
                ");
            }
        }
    }

    private void HandleClearRecentItems()
    {
        if (ontology != null && !string.IsNullOrEmpty(currentUserId))
        {
            RecentItemsService.ClearRecentItems(currentUserId, ontology.Id);
            LoadRecentItems();
        }
    }

    private void LoadRecentItems()
    {
        if (ontology != null && !string.IsNullOrEmpty(currentUserId))
        {
            recentItems = RecentItemsService.GetRecentItems(currentUserId, ontology.Id);
            StateHasChanged();
        }
    }

    private async Task HandleNodeClick(int conceptId)
    {
        var concept = ontology?.Concepts.FirstOrDefault(c => c.Id == conceptId);
        if (concept != null)
        {
            await SelectConcept(concept);
        }
    }

    private void HandleNodeEditClick(int conceptId)
    {
        var concept = ontology?.Concepts.FirstOrDefault(c => c.Id == conceptId);
        if (concept != null)
        {
            EditConcept(concept);
        }
    }

    private void HandleEdgeClick(int relationshipId)
    {
        var relationship = ontology?.Relationships.FirstOrDefault(r => r.Id == relationshipId);
        if (relationship != null)
        {
            SelectRelationship(relationship);
        }
    }

    private void HandleIndividualClick(int individualId)
    {
        var individual = ontology?.Individuals.FirstOrDefault(i => i.Id == individualId);
        if (individual != null)
        {
            SelectIndividual(individual);
        }
    }

    private void HandleOntologyLinkClick(int linkId)
    {
        var link = ontology?.LinkedOntologies?.FirstOrDefault(l => l.Id == linkId);
        if (link != null)
        {
            // Show a toast with link information for now
            ToastService.ShowInfo($"Linked Ontology: {link.Name}");
            // TODO: In the future, this could open a details panel or navigate to the linked ontology
        }
    }

    private void HandleVirtualConceptClick((string nodeId, string label) data)
    {
        // Parse the virtual concept node ID: "virtualconcept-{linkId}-{conceptId}"
        var parts = data.nodeId.Split('-');
        if (parts.Length != 3 || parts[0] != "virtualconcept")
        {
            ToastService.ShowWarning("Invalid virtual concept ID format");
            return;
        }

        if (!int.TryParse(parts[1], out int linkId) || !int.TryParse(parts[2], out int conceptId))
        {
            ToastService.ShowWarning("Could not parse virtual concept ID");
            return;
        }

        // Find the linked ontology and the virtual concept
        var link = ontology?.LinkedOntologies?.FirstOrDefault(l => l.Id == linkId);
        if (link?.LinkedOntology?.Concepts == null)
        {
            ToastService.ShowWarning("Linked ontology not found");
            return;
        }

        var virtualConcept = link.LinkedOntology.Concepts.FirstOrDefault(c => c.Id == conceptId);
        if (virtualConcept == null)
        {
            ToastService.ShowWarning("Virtual concept not found");
            return;
        }

        // Select the virtual concept to show in details panel
        SelectConcept(virtualConcept);
    }

    private async Task HandleVirtualConceptCtrlClick((string nodeId, string label) data)
    {
        // Parse the virtual concept node ID: "virtualconcept-{linkId}-{conceptId}"
        var parts = data.nodeId.Split('-');
        if (parts.Length != 3 || parts[0] != "virtualconcept")
        {
            ToastService.ShowError("Invalid virtual concept ID");
            return;
        }

        if (!int.TryParse(parts[1], out int linkId) || !int.TryParse(parts[2], out int conceptId))
        {
            ToastService.ShowError("Invalid virtual concept ID format");
            return;
        }

        // Find the linked ontology and the virtual concept
        var link = ontology?.LinkedOntologies?.FirstOrDefault(l => l.Id == linkId);
        if (link?.LinkedOntology == null)
        {
            ToastService.ShowError("Linked ontology not found");
            return;
        }

        var virtualConcept = link.LinkedOntology.Concepts?.FirstOrDefault(c => c.Id == conceptId);
        if (virtualConcept == null)
        {
            ToastService.ShowError("Virtual concept not found");
            return;
        }

        // Handle relationship creation logic
        if (firstSelectedConceptId == null)
        {
            // This is the first selection - import the virtual concept and select it
            ToastService.ShowInfo($"Importing \"{virtualConcept.Name}\" from {link.Name}...");

            // Import the concept into the current ontology
            var importedConcept = new Concept
            {
                OntologyId = ontology.Id,
                Name = virtualConcept.Name,
                Definition = virtualConcept.Definition,
                SimpleExplanation = virtualConcept.SimpleExplanation,
                Examples = virtualConcept.Examples,
                Category = virtualConcept.Category,
                Color = virtualConcept.Color,
                SourceOntology = link.Name // Mark where it came from
            };

            await ConceptService.CreateAsync(importedConcept);

            // Import concept properties
            if (virtualConcept.ConceptProperties?.Any() == true)
            {
                foreach (var virtualProperty in virtualConcept.ConceptProperties)
                {
                    var importedProperty = new ConceptProperty
                    {
                        ConceptId = importedConcept.Id,
                        Name = virtualProperty.Name,
                        Uri = virtualProperty.Uri,
                        PropertyType = virtualProperty.PropertyType,
                        DataType = virtualProperty.DataType,
                        RangeConceptId = virtualProperty.RangeConceptId,
                        IsRequired = virtualProperty.IsRequired,
                        IsFunctional = virtualProperty.IsFunctional,
                        Description = virtualProperty.Description
                    };

                    await ConceptPropertyService.CreateAsync(importedProperty);
                }
            }

            await LoadOntology();

            // Select the newly imported concept
            firstSelectedConceptId = importedConcept.Id;
            ToastService.ShowInfo($"First node selected: \"{importedConcept.Name}\". Ctrl+click another node to create relationship.", 5000);
        }
        else
        {
            // Second selection - import if needed and create relationship
            var sourceConcept = ontology?.Concepts.FirstOrDefault(c => c.Id == firstSelectedConceptId.Value);
            if (sourceConcept == null)
            {
                ToastService.ShowError("Source concept not found");
                firstSelectedConceptId = null;
                return;
            }

            // Import the virtual concept
            ToastService.ShowInfo($"Importing \"{virtualConcept.Name}\" from {link.Name}...");

            var importedConcept = new Concept
            {
                OntologyId = ontology.Id,
                Name = virtualConcept.Name,
                Definition = virtualConcept.Definition,
                SimpleExplanation = virtualConcept.SimpleExplanation,
                Examples = virtualConcept.Examples,
                Category = virtualConcept.Category,
                Color = virtualConcept.Color,
                SourceOntology = link.Name
            };

            await ConceptService.CreateAsync(importedConcept);

            // Import concept properties
            if (virtualConcept.ConceptProperties?.Any() == true)
            {
                foreach (var virtualProperty in virtualConcept.ConceptProperties)
                {
                    var importedProperty = new ConceptProperty
                    {
                        ConceptId = importedConcept.Id,
                        Name = virtualProperty.Name,
                        Uri = virtualProperty.Uri,
                        PropertyType = virtualProperty.PropertyType,
                        DataType = virtualProperty.DataType,
                        RangeConceptId = virtualProperty.RangeConceptId,
                        IsRequired = virtualProperty.IsRequired,
                        IsFunctional = virtualProperty.IsFunctional,
                        Description = virtualProperty.Description
                    };

                    await ConceptPropertyService.CreateAsync(importedProperty);
                }
            }

            await LoadOntology();

            // Show dialog to create relationship
            ShowAddRelationshipDialog(sourceConcept.Id, importedConcept.Id);
            firstSelectedConceptId = null;
        }
    }

    /// <summary>
    /// Handles importing a virtual concept from the list view.
    /// This simply imports the concept without any relationship creation logic.
    /// </summary>
    private async Task HandleVirtualConceptImportFromList((int linkId, Concept concept) data)
    {
        if (ontology == null) return;

        var (linkId, virtualConceptParam) = data;
        var link = ontology.LinkedOntologies?.FirstOrDefault(l => l.Id == linkId);

        if (link == null)
        {
            ToastService.ShowError("Linked ontology not found");
            return;
        }

        // Reload the virtual concept from the linked ontology to ensure ConceptProperties are loaded
        var virtualConcept = link.LinkedOntology?.Concepts.FirstOrDefault(c => c.Id == virtualConceptParam.Id);

        if (virtualConcept == null)
        {
            ToastService.ShowError("Virtual concept not found in linked ontology");
            return;
        }

        // Check if this concept is already imported
        var existingConcept = ontology.Concepts.FirstOrDefault(c =>
            c.Name == virtualConcept.Name && c.SourceOntology == link.Name);

        if (existingConcept != null)
        {
            ToastService.ShowWarning($"Concept \"{virtualConcept.Name}\" from {link.Name} is already imported");
            return;
        }

        ToastService.ShowInfo($"Importing \"{virtualConcept.Name}\" from {link.Name}...");

        // Import the concept into the current ontology
        var importedConcept = new Concept
        {
            OntologyId = ontology.Id,
            Name = virtualConcept.Name,
            Definition = virtualConcept.Definition,
            SimpleExplanation = virtualConcept.SimpleExplanation,
            Examples = virtualConcept.Examples,
            Category = virtualConcept.Category,
            Color = virtualConcept.Color,
            SourceOntology = link.Name // Mark where it came from
        };

        await ConceptService.CreateAsync(importedConcept);

        // Import concept properties (property definitions)
        if (virtualConcept.ConceptProperties?.Any() == true)
        {
            foreach (var virtualProperty in virtualConcept.ConceptProperties)
            {
                var importedProperty = new ConceptProperty
                {
                    ConceptId = importedConcept.Id,
                    Name = virtualProperty.Name,
                    Uri = virtualProperty.Uri,
                    PropertyType = virtualProperty.PropertyType,
                    DataType = virtualProperty.DataType,
                    RangeConceptId = virtualProperty.RangeConceptId,
                    IsRequired = virtualProperty.IsRequired,
                    IsFunctional = virtualProperty.IsFunctional,
                    Description = virtualProperty.Description
                };

                await ConceptPropertyService.CreateAsync(importedProperty);
            }
        }

        await LoadOntology();

        var propertyCount = virtualConcept.ConceptProperties?.Count ?? 0;
        var message = propertyCount > 0
            ? $"Successfully imported \"{virtualConcept.Name}\" with {propertyCount} {(propertyCount == 1 ? "property" : "properties")}"
            : $"Successfully imported \"{virtualConcept.Name}\"";
        ToastService.ShowSuccess(message);
    }

    private void ShowImportDialog()
    {
        showImportDialog = true;
        importStep = TtlImportDialog.ImportStep.Upload;
        importError = null;
        importResult = null;
        mergePreview = null;
        importOption = TtlImportDialog.ImportOption.Merge;
    }

    private void CancelImport()
    {
        showImportDialog = false;
        importStep = TtlImportDialog.ImportStep.Upload;
        importError = null;
        importResult = null;
        mergePreview = null;
    }

    private void ShowSettingsDialog()
    {
        if (ontology == null) return;

        // Navigate to standalone settings page instead of showing dialog
        Navigation.NavigateTo($"ontology/{ontology.Id}/settings");
    }

    private void ShowExportDialog()
    {
        exportDialog?.Show();
    }

    private async Task SaveSettings()
    {
        if (ontology == null) return;

        if (!CanFullAccess())
        {
            ToastService.ShowError("You do not have permission to modify ontology settings");
            return;
        }

        try
        {
            ontology.Name = editingOntologyName;
            ontology.Description = editingOntologyDescription;
            ontology.Author = editingOntologyAuthor;
            ontology.Version = editingOntologyVersion;
            ontology.Namespace = editingOntologyNamespace;
            ontology.Tags = editingOntologyTags;
            ontology.License = editingOntologyLicense;
            ontology.UsesBFO = editingOntologyUsesBFO;
            ontology.UsesProvO = editingOntologyUsesProvO;
            ontology.Visibility = editingOntologyVisibility;
            ontology.AllowPublicEdit = editingOntologyAllowPublicEdit;
            ontology.UpdatedAt = DateTime.UtcNow;

            await OntologyService.UpdateOntologyAsync(ontology);
            showSettingsDialog = false;

            ToastService.ShowSuccess("Ontology settings updated successfully");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to save settings: {ex.Message}");
        }
    }

    private void CancelSettings()
    {
        showSettingsDialog = false;
    }

    private void ShowShareModalFromHeader()
    {
        showShareModal = true;
    }

    private async Task ShowLineageDialog()
    {
        if (lineageComponent != null && ontology != null)
        {
            await lineageComponent.Show(ontology);
        }
    }

    private void ShowForkDialogFromHeader()
    {
        if (forkCloneDialog != null && ontology != null)
        {
            forkCloneDialog.ShowFork(ontology);
        }
    }

    private void ShowCloneDialogFromHeader()
    {
        if (forkCloneDialog != null && ontology != null)
        {
            forkCloneDialog.ShowClone(ontology);
        }
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        importError = null;
        isProcessingFile = true;

        try
        {
            var file = e.File;

            // Sanitize filename for logging/display purposes
            var sanitizedFilename = SanitizeFilename(file.Name);

            // Validate file size
            if (file.Size > 10 * 1024 * 1024)
            {
                importError = "File size exceeds 10MB limit";
                isProcessingFile = false;
                return;
            }

            // Validate MIME type - allow common RDF/TTL content types
            // Note: Different browsers may report different MIME types for .ttl files
            var allowedContentTypes = new[]
            {
                "text/turtle",           // Standard TTL MIME type
                "application/x-turtle",  // Alternative TTL MIME type
                "application/rdf+xml",   // RDF/XML MIME type
                "text/plain",            // Browsers often send text/plain for .ttl files
                "application/octet-stream" // Generic binary, check extension
            };

            if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                // If MIME type is not recognized, check file extension as fallback
                var allowedExtensions = new[] { ".ttl", ".turtle", ".rdf" };
                var fileExtension = Path.GetExtension(sanitizedFilename).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    importError = $"Invalid file type. Only TTL/RDF files are allowed. (Content-Type: {file.ContentType})";
                    isProcessingFile = false;
                    return;
                }
            }

            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            importResult = await TtlImportService.ParseTtlFileAsync(memoryStream);

            if (!importResult.Success)
            {
                importError = importResult.ErrorMessage;
                isProcessingFile = false;
                return;
            }

            if (importResult.ParsedGraph != null)
            {
                mergePreview = await TtlImportService.PreviewMergeAsync(Id, importResult.ParsedGraph);
            }

            importStep = TtlImportDialog.ImportStep.Preview;
        }
        catch (Exception ex)
        {
            importError = $"Error processing file: {ex.Message}";
        }
        finally
        {
            isProcessingFile = false;
        }
    }

    private async Task ExecuteImport()
    {
        if (importResult?.ParsedGraph == null)
            return;

        isImporting = true;
        importError = null;

        try
        {
            if (importOption == TtlImportDialog.ImportOption.Merge)
            {
                await TtlImportService.MergeIntoExistingAsync(Id, importResult.ParsedGraph);
                await LoadOntology();
                showImportDialog = false;
                ToastService.ShowSuccess($"Imported {importResult.ConceptCount} concepts successfully");
            }
            else
            {
                var newOntology = await TtlImportService.ImportAsNewOntologyAsync(importResult.ParsedGraph);
                ToastService.ShowSuccess($"Created new ontology \"{newOntology.Name}\"");
                Navigation.NavigateTo($"ontology/{newOntology.Id}");
            }
        }
        catch (Exception ex)
        {
            importError = $"Import failed: {ex.Message}";
            ToastService.ShowError($"Import failed: {ex.Message}");
        }
        finally
        {
            isImporting = false;
        }
    }

    private async Task SaveNotes()
    {
        if (ontology == null) return;

        if (!CanEdit())
        {
            ToastService.ShowError("You do not have permission to edit notes");
            return;
        }

        try
        {
            ontology.Notes = editingNotes;
            ontology.UpdatedAt = DateTime.UtcNow;
            await OntologyService.UpdateOntologyAsync(ontology);
            ToastService.ShowSuccess("Notes saved successfully");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to save notes: {ex.Message}");
        }
    }


    // Permission checking methods
    private bool CanAdd()
    {
        return viewState.CanAdd;
    }

    private bool CanEdit()
    {
        return viewState.CanEdit;
    }

    private bool CanFullAccess()
    {
        return viewState.CanManage;
    }


    // EventCallback helper method for conditional callbacks
    private EventCallback<T> GetConditionalCallback<T>(bool condition, Func<T, Task> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback<T>.Empty;
    }

    // Overload for parameterless callbacks (async)
    private EventCallback GetConditionalCallback(bool condition, Func<Task> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback.Empty;
    }

    // Overload for parameterless callbacks (void)
    private EventCallback GetConditionalCallback(bool condition, Action callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback.Empty;
    }

    // Overload for Action<T> callbacks (void)
    private EventCallback<T> GetConditionalCallback<T>(bool condition, Action<T> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback<T>.Empty;
    }

    private EventCallback<CustomConceptTemplate> GetConditionalCallback(bool condition, Action<CustomConceptTemplate> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback<CustomConceptTemplate>.Empty;
    }

    private EventCallback<CustomConceptTemplate> GetConditionalCallback(bool condition, Func<CustomConceptTemplate, Task> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback<CustomConceptTemplate>.Empty;
    }

    private EventCallback<Concept> GetConditionalCallback(bool condition, Action<Concept> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback<Concept>.Empty;
    }

    private EventCallback<Concept> GetConditionalCallback(bool condition, Func<Concept, Task> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback<Concept>.Empty;
    }

    private EventCallback<Relationship> GetConditionalCallback(bool condition, Action<Relationship> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback<Relationship>.Empty;
    }

    private EventCallback<Relationship> GetConditionalCallback(bool condition, Func<Relationship, Task> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback<Relationship>.Empty;
    }

    private EventCallback<int> GetConditionalCallback(bool condition, Action<int> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback<int>.Empty;
    }

    private EventCallback<int> GetConditionalCallback(bool condition, Func<int, Task> callback)
    {
        return condition ? EventCallback.Factory.Create(this, callback) : EventCallback<int>.Empty;
    }

    // Custom Template CRUD Operations
    private async Task SaveCustomTemplate(CustomConceptTemplate template)
    {
        if (ontology == null)
            return;

        if (!CanEdit())
        {
            ToastService.ShowError("You do not have permission to create templates");
            return;
        }

        try
        {
            template.OntologyId = ontology.Id;
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            ontology.CustomTemplates.Add(template);
            await OntologyService.UpdateOntologyAsync(ontology);
            ToastService.ShowSuccess($"Template '{template.Category}' created successfully");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to create template: {ex.Message}");
        }
    }

    private async Task UpdateCustomTemplate(CustomConceptTemplate template)
    {
        if (ontology == null)
            return;

        if (!CanEdit())
        {
            ToastService.ShowError("You do not have permission to update templates");
            return;
        }

        try
        {
            template.UpdatedAt = DateTime.UtcNow;
            await OntologyService.UpdateOntologyAsync(ontology);
            ToastService.ShowSuccess($"Template '{template.Category}' updated successfully");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to update template: {ex.Message}");
        }
    }

    private async Task DeleteCustomTemplate(CustomConceptTemplate template)
    {
        if (ontology == null)
            return;

        if (!CanEdit())
        {
            ToastService.ShowError("You do not have permission to delete templates");
            return;
        }

        try
        {
            ontology.CustomTemplates.Remove(template);
            await OntologyService.UpdateOntologyAsync(ontology);
            ToastService.ShowSuccess($"Template '{template.Category}' deleted successfully");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to delete template: {ex.Message}");
        }
    }

    /// <summary>
    /// Sanitizes a filename by removing path traversal attempts and dangerous characters.
    /// This prevents security issues when logging or displaying filenames.
    /// </summary>
    private string SanitizeFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return "unknown";

        // Remove any path components (directory traversal protection)
        filename = Path.GetFileName(filename);

        // Define characters that should be removed for security
        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\0' })
            .ToHashSet();

        // Filter out invalid characters
        var sanitized = string.Concat(filename.Where(c => !invalidChars.Contains(c)));

        // Ensure we don't return an empty string
        return string.IsNullOrWhiteSpace(sanitized) ? "sanitized_file" : sanitized;
    }

    // ===== SignalR Real-Time Collaboration =====

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Set up keyboard shortcut event listeners
            await JS.InvokeVoidAsync("eval", @"
                // Handle action shortcuts (add concept, etc.)
                document.addEventListener('keyboardShortcut', (e) => {
                    if (e.detail.action === 'openCommandPalette') {
                        // Trigger the global search via Cmd+Shift+Space
                        const event = new KeyboardEvent('keydown', {
                            key: ' ',
                            code: 'Space',
                            metaKey: true,
                            shiftKey: true,
                            bubbles: true
                        });
                        document.dispatchEvent(event);
                    } else if (e.detail.action === 'addConcept') {
                        // Find and click the Add Concept button
                        const addBtn = document.querySelector('button[title*=""Add Concept""], button:has(i.bi-plus-circle)');
                        if (addBtn) addBtn.click();
                    } else if (e.detail.action === 'addRelationship') {
                        // Find and click the Add Relationship button
                        const addBtn = document.querySelector('button[title*=""Add Relationship""], button:has(i.bi-arrow-left-right)');
                        if (addBtn) addBtn.click();
                    } else if (e.detail.action === 'importTtl') {
                        // Find and click the Import button
                        const importBtn = document.querySelector('button[title*=""Import""]');
                        if (importBtn) importBtn.click();
                    } else if (e.detail.action === 'openSettings') {
                        // Find and click the Settings button
                        const settingsBtn = document.querySelector('button[title*=""Settings""], a[href*=""settings""]');
                        if (settingsBtn) settingsBtn.click();
                    }
                });

                // Handle view mode change shortcuts
                document.addEventListener('viewModeChange', (e) => {
                    const mode = e.detail.mode;
                    // Find the corresponding view mode button and click it
                    const buttons = document.querySelectorAll('.view-mode-nav button');
                    buttons.forEach(btn => {
                        const text = btn.textContent.trim();
                        if (text.toLowerCase() === mode.toLowerCase() ||
                            (mode === 'Ttl' && text.toUpperCase() === 'TTL')) {
                            btn.click();
                        }
                    });
                });
            ");
        }

        // Initialize SignalR once the ontology is loaded (may not happen on firstRender if data is loading async)
        if (ontology != null && !hasRendered)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);

            try
            {
                await JS.InvokeVoidAsync("ontologyHub.init", dotNetRef, ontology.Id);

                // Send initial view to other users
                await UpdateCurrentView(viewMode.ToString());

                // Start heartbeat timer to keep presence active
                StartHeartbeatTimer();
            }
            catch (Exception ex)
            {
                // Log error but don't fail - real-time updates are optional
                Console.WriteLine($"Error initializing SignalR: {ex.Message}");
            }
        }
    }

    [JSInvokable]
    public async Task HandleConceptChanged(ConceptChangedEvent changeEvent)
    {
        try
        {
            if (ontology == null || changeEvent.OntologyId != ontology.Id)
            {
                return;
            }

            // Reload the ontology to get the latest data and replace the entire object reference
            var freshOntology = await OntologyService.GetOntologyAsync(Id);

            if (freshOntology != null)
            {
                // Replace the entire ontology object to trigger Blazor change detection
                ontology = freshOntology;

                // Force UI update
                await InvokeAsync(() =>
                {
                    StateHasChanged();

                    // Refresh the graph view if it's visible
                    if (viewMode == ViewMode.Graph && graphViewContainer != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(100);
                            await graphViewContainer.RefreshGraph();
                        });
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling concept change: {ex.Message}");
        }
    }

    [JSInvokable]
    public async Task HandleRelationshipChanged(RelationshipChangedEvent changeEvent)
    {
        try
        {
            if (ontology == null || changeEvent.OntologyId != ontology.Id)
                return;

            // Reload the ontology to get the latest data and replace the entire object reference
            var freshOntology = await OntologyService.GetOntologyAsync(Id);

            if (freshOntology != null)
            {
                // Replace the entire ontology object to trigger Blazor change detection
                ontology = freshOntology;

                // Force UI update
                await InvokeAsync(() =>
                {
                    StateHasChanged();

                    // Refresh the graph view if it's visible
                    if (viewMode == ViewMode.Graph && graphViewContainer != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(100);
                            await graphViewContainer.RefreshGraph();
                        });
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling relationship change: {ex.Message}");
        }
    }

    // Presence tracking methods
    [JSInvokable]
    public Task HandleUserJoined(PresenceInfo presenceInfo)
    {
        if (!presenceUsers.Any(u => u.ConnectionId == presenceInfo.ConnectionId))
        {
            presenceUsers.Add(presenceInfo);
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task HandleUserLeft(string connectionId)
    {
        var user = presenceUsers.FirstOrDefault(u => u.ConnectionId == connectionId);
        if (user != null)
        {
            presenceUsers.Remove(user);
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task HandlePresenceList(List<PresenceInfo> userList)
    {
        // Filter out our own connection
        presenceUsers = userList.Where(u => u.ConnectionId != dotNetRef?.Value.GetHashCode().ToString()).ToList();
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task HandleUserViewChanged(string connectionId, string viewName)
    {
        var user = presenceUsers.FirstOrDefault(u => u.ConnectionId == connectionId);
        if (user != null)
        {
            user.CurrentView = viewName;
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    private void StartHeartbeatTimer()
    {
        // Send heartbeat every 30 seconds to keep presence active
        heartbeatTimer = new System.Timers.Timer(30000);
        heartbeatTimer.Elapsed += async (sender, e) =>
        {
            if (ontology != null)
            {
                try
                {
                    await JS.InvokeVoidAsync("ontologyHub.sendHeartbeat", ontology.Id);
                }
                catch
                {
                    // Ignore heartbeat errors
                }
            }
        };
        heartbeatTimer.Start();
    }

    private async Task UpdateCurrentView(string viewName)
    {
        if (ontology != null)
        {
            try
            {
                await JS.InvokeVoidAsync("ontologyHub.updateCurrentView", ontology.Id, viewName);
            }
            catch
            {
                // Ignore view update errors
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Unsubscribe from state change events
        viewState.OnStateChanged -= HandleStateChanged;
        graphState.OnGraphStateChanged -= HandleGraphStateChanged;

        // Stop heartbeat timer
        if (heartbeatTimer != null)
        {
            heartbeatTimer.Stop();
            heartbeatTimer.Dispose();
            heartbeatTimer = null;
        }

        if (ontology != null)
        {
            try
            {
                await JS.InvokeVoidAsync("ontologyHub.disconnect", ontology.Id);
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        dotNetRef?.Dispose();
    }

    /// <summary>
    /// Handles state changes from OntologyViewState and triggers UI re-render.
    /// </summary>
    private void HandleStateChanged()
    {
        StateHasChanged();
    }

    /// <summary>
    /// Handles graph state changes from GraphViewState and triggers UI re-render.
    /// </summary>
    private void HandleGraphStateChanged()
    {
        StateHasChanged();
    }

    // Individual/Instance management methods
    private void SelectIndividual(Individual individual)
    {
        selectedIndividual = individual;
        StateHasChanged();
    }

    private void ShowAddIndividualDialog()
    {
        if (ontology == null) return;

        newIndividual = new Individual
        {
            OntologyId = Id
        };
        newIndividualProperties = new List<IndividualProperty>();
        editingIndividual = null;
        showAddIndividual = true;

        // Hide other dialogs
        CancelConceptDialog();
        CancelRelationshipDialog();
        selectedConcept = null;
        StateHasChanged();
    }

    private void EditIndividual(Individual individual)
    {
        editingIndividual = individual;
        newIndividual = new Individual
        {
            Id = individual.Id,
            OntologyId = individual.OntologyId,
            ConceptId = individual.ConceptId,
            Name = individual.Name,
            Description = individual.Description,
            Label = individual.Label,
            Uri = individual.Uri
        };

        // Clone properties
        newIndividualProperties = individual.Properties
            .Select(p => new IndividualProperty
            {
                Id = p.Id,
                IndividualId = p.IndividualId,
                Name = p.Name,
                Value = p.Value,
                DataType = p.DataType
            })
            .ToList();

        showAddIndividual = true;

        // Hide other dialogs
        CancelConceptDialog();
        CancelRelationshipDialog();
        selectedConcept = null;
        StateHasChanged();
    }

    private async Task DeleteIndividual(Individual individual)
    {
        var confirmed = await ConfirmService.ShowAsync(
            "Delete Individual",
            $"Are you sure you want to delete the individual \"{individual.Name}\"?",
            "Delete",
            ConfirmType.Danger
        );

        if (confirmed)
        {
            try
            {
                await IndividualService.DeleteAsync(individual.Id);
                ToastService.ShowSuccess($"Individual \"{individual.Name}\" deleted successfully.");
                await HandleIndividualDeleted(individual.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting individual {IndividualId}", individual.Id);
                ToastService.ShowError($"Failed to delete individual: {ex.Message}");
            }
        }
    }

    private async Task SaveIndividual()
    {
        if (string.IsNullOrWhiteSpace(newIndividual.Name) || newIndividual.ConceptId == 0)
        {
            return;
        }

        // Check permissions
        if (editingIndividual != null && userPermissionLevel < PermissionLevel.ViewAddEdit)
        {
            ToastService.ShowError("You do not have permission to edit individuals");
            return;
        }
        else if (editingIndividual == null && userPermissionLevel < PermissionLevel.ViewAndAdd)
        {
            ToastService.ShowError("You do not have permission to add individuals");
            return;
        }

        try
        {
            newIndividual.Properties = newIndividualProperties;

            if (editingIndividual != null)
            {
                // Update existing individual
                await IndividualService.UpdateAsync(newIndividual);
                ToastService.ShowSuccess($"Updated individual \"{newIndividual.Name}\"");
                await HandleIndividualUpdated(newIndividual);
                editingIndividual = null;
            }
            else
            {
                // Create new individual
                var createdIndividual = await IndividualService.CreateAsync(newIndividual);
                ToastService.ShowSuccess($"Created individual \"{newIndividual.Name}\"");
                await HandleIndividualCreated(createdIndividual);
            }

            showAddIndividual = false;
            newIndividual = new Individual();
            newIndividualProperties = new List<IndividualProperty>();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            var errorMessage = ex.InnerException != null
                ? $"{ex.Message} - {ex.InnerException.Message}"
                : ex.Message;
            ToastService.ShowError($"Failed to save individual: {errorMessage}", 8000);
        }
    }

    private void CancelIndividualDialog()
    {
        showAddIndividual = false;
        editingIndividual = null;
        newIndividual = new Individual();
        newIndividualProperties = new List<IndividualProperty>();
        StateHasChanged();
    }

    // Individual event handlers
    private async Task HandleIndividualCreated(Individual individual)
    {
        // Reload individuals list
        if (ontology != null)
        {
            individuals = await IndividualService.GetByOntologyIdAsync(ontology.Id);
        }
        StateHasChanged();
    }

    private async Task HandleIndividualUpdated(Individual individual)
    {
        // Reload individuals list
        if (ontology != null)
        {
            individuals = await IndividualService.GetByOntologyIdAsync(ontology.Id);
        }
        StateHasChanged();
    }

    private async Task HandleIndividualDeleted(int individualId)
    {
        // Reload individuals list
        if (ontology != null)
        {
            individuals = await IndividualService.GetByOntologyIdAsync(ontology.Id);
        }

        // Clear selection if the deleted individual was selected
        if (selectedIndividual?.Id == individualId)
        {
            selectedIndividual = null;
        }

        StateHasChanged();
    }


    // Restriction management methods
    private void ShowAddRestrictionDialog()
    {
        ToastService.ShowInfo("Restriction creation dialog will be implemented in the next phase.");
    }

    private void EditRestriction(ConceptRestriction restriction)
    {
        ToastService.ShowInfo("Restriction editing will be implemented in the next phase.");
    }

    private async Task DeleteRestriction(ConceptRestriction restriction)
    {
        var confirmed = await ConfirmService.ShowAsync(
            "Delete Restriction",
            $"Are you sure you want to delete this restriction on property \"{restriction.PropertyName}\"?",
            "Delete",
            ConfirmType.Danger
        );

        if (confirmed)
        {
            try
            {
                await RestrictionService.DeleteAsync(restriction.Id);
                ToastService.ShowSuccess($"Restriction on \"{restriction.PropertyName}\" deleted successfully.");

                // Reload restrictions for the selected concept
                if (selectedConcept != null)
                {
                    selectedConceptRestrictions = await RestrictionService.GetByConceptIdAsync(selectedConcept.Id);
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting restriction {RestrictionId}", restriction.Id);
                ToastService.ShowError($"Failed to delete restriction: {ex.Message}");
            }
        }
    }

    // Version Control
    private async Task HandleRevertRequested(int versionNumber)
    {
        var confirmed = await ConfirmService.ShowAsync(
            "Revert to Version",
            $"Are you sure you want to revert this ontology to version {versionNumber}? This will restore all concepts and relationships to their state at that version. This action cannot be undone.",
            "Revert",
            ConfirmType.Warning
        );

        if (confirmed)
        {
            try
            {
                ToastService.ShowInfo($"Reverting to version {versionNumber}...");

                Logger.LogInformation("Reverting ontology {OntologyId} to version {VersionNumber}", Id, versionNumber);

                // Call the revert service
                var revertedOntology = await OntologyService.RevertToVersionAsync(Id, versionNumber);

                // Reload the ontology with all data
                ontology = await OntologyService.GetOntologyAsync(Id);
                if (ontology == null)
                {
                    throw new InvalidOperationException("Failed to reload ontology after revert");
                }

                // Refresh the UI
                StateHasChanged();

                ToastService.ShowSuccess($"Successfully reverted to version {versionNumber}. Please refresh the page to see the changes.");
                Logger.LogInformation("Successfully reverted ontology {OntologyId} to version {VersionNumber}", Id, versionNumber);
            }
            catch (UnauthorizedAccessException)
            {
                ToastService.ShowError("You do not have permission to revert this ontology.");
                Logger.LogWarning("Unauthorized revert attempt for ontology {OntologyId}", Id);
            }
            catch (ArgumentException ex)
            {
                ToastService.ShowError($"Invalid version: {ex.Message}");
                Logger.LogWarning(ex, "Invalid version {VersionNumber} for ontology {OntologyId}", versionNumber, Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error reverting ontology {OntologyId} to version {VersionNumber}", Id, versionNumber);
                ToastService.ShowError($"Failed to revert to version {versionNumber}: {ex.Message}");
            }
        }
    }

    // Mobile Editor Management
    private void CloseMobileEditor()
    {
        CancelConceptDialog();
        CancelRelationshipDialog();

        // Close individual dialog
        CancelIndividualDialog();

        selectedConcept = null;
        selectedRelationship = null;
    }



    // Keyboard shortcuts banner dismiss methods
    private void DismissKeyboardShortcutsTemporarily()
    {
        showKeyboardShortcutsBanner = false;
    }

    private async Task DismissKeyboardShortcutsPermanently()
    {
        showKeyboardShortcutsBanner = false;

        if (!string.IsNullOrEmpty(currentUserId))
        {
            try
            {
                await PreferencesService.UpdateShowKeyboardShortcutsAsync(currentUserId, false);
                ToastService.ShowSuccess("Keyboard shortcuts banner will no longer be shown");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save keyboard shortcuts preference for user {UserId}", currentUserId);
                ToastService.ShowError("Failed to save preference");
            }
        }
    }

    private void DismissGlobalSearchBannerTemporarily()
    {
        showGlobalSearchBanner = false;
    }

    private async Task DismissGlobalSearchBannerPermanently()
    {
        showGlobalSearchBanner = false;

        if (!string.IsNullOrEmpty(currentUserId))
        {
            try
            {
                await PreferencesService.UpdateShowGlobalSearchBannerAsync(currentUserId, false);
                ToastService.ShowSuccess("Global search banner will no longer be shown");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save global search banner preference for user {UserId}", currentUserId);
                ToastService.ShowError("Failed to save preference");
            }
        }
    }

    #region Validation

    private async Task LoadValidation()
    {
        try
        {
            validationResult = await ValidationService.ValidateOntologyAsync(Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load validation for ontology {OntologyId}", Id);
        }
    }

    private async Task RefreshValidation()
    {
        await LoadValidation();
        StateHasChanged();
    }

    private async Task HandleIssueClick(ValidationIssue issue)
    {
        try
        {
            if (issue.EntityType == "Concept")
            {
                // Switch to list view
                viewMode = ViewMode.List;
                StateHasChanged();

                // Find and select the concept
                var concept = ontology?.Concepts.FirstOrDefault(c => c.Id == issue.EntityId);
                if (concept != null)
                {
                    selectedConcept = concept;

                    // Scroll to the concept
                    await Task.Delay(100); // Let UI update
                    await JS.InvokeVoidAsync("scrollToElement", $"concept-{issue.EntityId}");
                }
            }
            else if (issue.EntityType == "Relationship")
            {
                // Switch to list view
                viewMode = ViewMode.List;
                StateHasChanged();

                // Find and select the relationship
                var relationship = ontology?.Relationships.FirstOrDefault(r => r.Id == issue.EntityId);
                if (relationship != null)
                {
                    selectedRelationship = relationship;

                    // Scroll to the relationship
                    await Task.Delay(100); // Let UI update
                    await JS.InvokeVoidAsync("scrollToElement", $"relationship-{issue.EntityId}");
                }
            }

            ToastService.ShowInfo($"Navigated to: {issue.EntityName}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to navigate to issue {IssueId}", issue.Id);
            ToastService.ShowError("Failed to navigate to issue");
        }
    }

    #endregion


}
