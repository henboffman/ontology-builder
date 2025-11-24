using Eidos.Components.Shared;
using Eidos.Components.Workspace;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services;
using Eidos.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Eidos.Components.Pages;

public partial class WorkspaceView : ComponentBase, IAsyncDisposable
{
    private enum NoteTypeFilter
    {
        All,
        Notes,
        Concepts
    }

    private enum NoteSortOption
    {
        TitleAsc,
        TitleDesc,
        ModifiedDesc,
        ModifiedAsc
    }

    [Parameter]
    public int WorkspaceId { get; set; }

    [Inject]
    private Services.WorkspaceService WorkspaceService { get; set; } = default!;

    [Inject]
    private NoteService NoteService { get; set; } = default!;

    [Inject]
    private TagService TagService { get; set; } = default!;

    [Inject]
    private AutoSaveService AutoSaveService { get; set; } = default!;

    [Inject]
    private IConceptService ConceptService { get; set; } = default!;

    [Inject]
    private IRelationshipService RelationshipService { get; set; } = default!;

    [Inject]
    private MarkdownRenderingService MarkdownRenderer { get; set; } = default!;

    [Inject]
    private MarkdownExportService MarkdownExportService { get; set; } = default!;

    [Inject]
    private IUserPreferencesService UserPreferencesService { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ConceptDetectionService ConceptDetectionService { get; set; } = default!;

    [Inject]
    private NoteConceptLinkRepository NoteConceptLinkRepository { get; set; } = default!;

    private Models.Workspace? workspace;
    private List<Note> notes = new();
    private Note? selectedNote;
    private List<NoteLink> backlinks = new();
    private List<Concept> relatedConcepts = new();

    // Smart suggestions (concept links)
    private List<NoteConceptLink> currentNoteConceptLinks = new();
    private bool loadingConceptLinks = false;

    private string noteContent = string.Empty;
    private string searchTerm = string.Empty;
    private string newNoteTitle = string.Empty;

    private bool loading = true;
    private bool saving = false;
    private bool showPreview = false;
    private bool creatingNote = false;
    private bool isCondensedView = true; // Default to condensed view (VS Code style)
    private bool showNoteGraph = false; // Note graph view toggle
    private bool hideKeyboardHints = false;
    private bool isRightPaneCollapsed = false;
    private bool isTagsExpanded = true; // Default to expanded

    private string? currentUserId;

    // Auto-save
    private AutoSaveStatus autoSaveStatus = AutoSaveStatus.Idle;
    private DateTime? lastSavedTime = null;

    // Auto-create for new notes
    private System.Threading.Timer? autoCreateTimer;
    private bool noteAutoCreated = false; // Track if note was auto-created

    // Quick Switcher
    private WorkspaceQuickSwitcher? quickSwitcher;

    // Tags
    private List<(Tag Tag, int NoteCount)> workspaceTags = new();
    private int? selectedTagFilter = null;
    private bool showTagManagement = false;
    private bool showMarkdownImport = false;
    private bool showMarkdownTips = false;
    private List<Tag> selectedNoteTags = new();

    // Note type filter and sorting
    private NoteTypeFilter noteTypeFilter = NoteTypeFilter.All;
    private NoteSortOption noteSortOption = NoteSortOption.ModifiedDesc;

    // Element references for JavaScript interop
    private ElementReference editorTextArea;
    private ElementReference newNoteTextArea;
    private IJSObjectReference? wikiLinkEditorModule;
    private IJSObjectReference? imageUploadModule;
    private IJSObjectReference? imageLightboxModule;
    private DotNetObjectReference<WorkspaceView>? dotNetHelper;

    // Grid Mode State
    private bool isGridModeEnabled = false;
    private List<OpenNoteState> openNotesInGrid = new();
    private int maxVisibleNotesInGrid = 4;
    private bool isWorkspaceFullScreen = false;
    private NoteGridLayout? noteGridLayoutRef;

    private IEnumerable<Note> FilteredNotes
    {
        get
        {
            var filtered = notes.AsEnumerable();

            // Apply note type filter
            if (noteTypeFilter == NoteTypeFilter.Notes)
            {
                filtered = filtered.Where(n => !n.IsConceptNote);
            }
            else if (noteTypeFilter == NoteTypeFilter.Concepts)
            {
                filtered = filtered.Where(n => n.IsConceptNote);
            }

            // Search is now handled server-side in OnSearchChanged()
            // No client-side search filtering needed

            // Apply tag filter
            if (selectedTagFilter.HasValue)
            {
                filtered = filtered.Where(n => noteTagsCache.ContainsKey(n.Id) && noteTagsCache[n.Id].Any(t => t.Id == selectedTagFilter.Value));
            }

            // Apply sorting
            filtered = noteSortOption switch
            {
                NoteSortOption.TitleAsc => filtered.OrderBy(n => n.Title),
                NoteSortOption.TitleDesc => filtered.OrderByDescending(n => n.Title),
                NoteSortOption.ModifiedDesc => filtered.OrderByDescending(n => n.UpdatedAt),
                NoteSortOption.ModifiedAsc => filtered.OrderBy(n => n.UpdatedAt),
                _ => filtered.OrderByDescending(n => n.UpdatedAt)
            };

            return filtered;
        }
    }

    // Cache of note tags for filtering
    private Dictionary<int, List<Tag>> noteTagsCache = new();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        currentUserId = authState.User.FindFirst(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            Navigation.NavigateTo("/Account/Login");
            return;
        }

        // Load user preferences for notes sort order
        try
        {
            var preferences = await UserPreferencesService.GetPreferencesAsync(currentUserId);
            if (Enum.TryParse<NoteSortOption>(preferences.DefaultNotesSortOrder, out var sortOption))
            {
                noteSortOption = sortOption;
            }
        }
        catch (Exception ex)
        {
            // If loading preferences fails, just use the default
            Console.WriteLine($"Failed to load notes sort preference: {ex.Message}");
        }

        await LoadWorkspaceAsync();

        // Subscribe to auto-save status changes
        AutoSaveService.StatusChanged += OnAutoSaveStatusChanged;

        // Check for noteId query parameter and auto-select that note
        var uri = new Uri(Navigation.Uri);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var noteIdParam = queryParams["noteId"];

        if (!string.IsNullOrEmpty(noteIdParam) && int.TryParse(noteIdParam, out int noteIdToSelect))
        {
            await SelectNote(noteIdToSelect);
        }
    }

    private async Task LoadWorkspaceAsync()
    {
        try
        {
            loading = true;

            workspace = await WorkspaceService.GetWorkspaceAsync(WorkspaceId, currentUserId!, includeOntology: true);

            if (workspace != null)
            {
                // If workspace doesn't have an ontology loaded, it might be a legacy workspace
                // Try to reload it to ensure the Ontology navigation property is populated
                if (workspace.Ontology == null)
                {
                    Console.WriteLine($"[WorkspaceView] Workspace {WorkspaceId} has no ontology loaded, reloading...");
                    workspace = await WorkspaceService.GetWorkspaceAsync(WorkspaceId, currentUserId!, includeOntology: true);

                    if (workspace?.Ontology != null)
                    {
                        Console.WriteLine($"[WorkspaceView] Successfully loaded ontology {workspace.Ontology.Id} for workspace {WorkspaceId}");
                    }
                    else
                    {
                        Console.WriteLine($"[WorkspaceView] Workspace {WorkspaceId} still has no ontology after reload");
                    }
                }

                await LoadNotesAsync();
                await LoadTagsAsync();
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error loading workspace: {ex.Message}");
        }
        finally
        {
            loading = false;
        }
    }

    private async Task LoadNotesAsync()
    {
        try
        {
            notes = await NoteService.GetWorkspaceNotesAsync(WorkspaceId, currentUserId!);

            // Preload tags for all notes to enable tag filtering
            foreach (var note in notes)
            {
                if (!noteTagsCache.ContainsKey(note.Id))
                {
                    var tags = await TagService.GetNoteTagsAsync(note.Id);
                    noteTagsCache[note.Id] = tags;
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error loading notes: {ex.Message}");
        }
    }

    private async Task SelectNote(int noteId)
    {
        try
        {
            Console.WriteLine($"[SelectNote] Selecting note ID: {noteId}");

            // Save current note if it was being edited
            if (selectedNote != null && !string.IsNullOrEmpty(noteContent))
            {
                Console.WriteLine($"[SelectNote] Saving current note first");
                await SaveNote();
            }

            var note = await NoteService.GetNoteWithContentAsync(noteId, currentUserId!);

            if (note != null)
            {
                Console.WriteLine($"[SelectNote] Loaded note: {note.Title} (ID: {note.Id})");

                // If grid mode is enabled, add note to grid
                if (isGridModeEnabled)
                {
                    // Check if note is already in grid
                    if (!openNotesInGrid.Any(n => n.NoteId == note.Id))
                    {
                        var noteState = new OpenNoteState
                        {
                            NoteId = note.Id,
                            Title = note.Title,
                            Content = note.Content?.MarkdownContent ?? string.Empty,
                            OriginalContent = note.Content?.MarkdownContent ?? string.Empty,
                            GridPosition = openNotesInGrid.Count,
                            IsDirty = false,
                            IsFocused = true
                        };

                        // Unfocus all other notes
                        foreach (var openNote in openNotesInGrid)
                        {
                            openNote.IsFocused = false;
                        }

                        openNotesInGrid.Add(noteState);
                        Console.WriteLine($"[SelectNote] Added note to grid: {note.Title}");
                    }
                    else
                    {
                        // Just focus the existing note in grid
                        foreach (var openNote in openNotesInGrid)
                        {
                            openNote.IsFocused = openNote.NoteId == note.Id;
                        }
                        Console.WriteLine($"[SelectNote] Note already in grid, focused it: {note.Title}");
                    }

                    await InvokeAsync(StateHasChanged);
                    return; // Don't continue with normal note selection
                }

                selectedNote = note;
                noteContent = note.Content?.MarkdownContent ?? string.Empty;
                showPreview = true; // Default to preview mode
                creatingNote = false;

                Console.WriteLine($"[SelectNote] Set selectedNote, calling StateHasChanged");

                // Load backlinks and related concepts
                // If this is a concept note, get backlinks via the concept
                if (note.LinkedConceptId.HasValue)
                {
                    backlinks = await NoteService.GetBacklinksAsync(note.LinkedConceptId.Value);

                    // Load related concepts for concept notes
                    await LoadRelatedConceptsAsync(note.LinkedConceptId.Value);
                }
                else
                {
                    relatedConcepts = new List<Concept>();

                    // For regular notes, we need to find a concept with matching name
                    // This allows backlinks to work for any note via [[note title]]
                    if (workspace?.Ontology != null)
                    {
                        var concepts = await ConceptService.GetByOntologyIdAsync(workspace.Ontology.Id);
                        var matchingConcept = concepts.FirstOrDefault(c =>
                            c.Name.Equals(note.Title, StringComparison.OrdinalIgnoreCase));

                        if (matchingConcept != null)
                        {
                            backlinks = await NoteService.GetBacklinksAsync(matchingConcept.Id);
                        }
                        else
                        {
                            backlinks = new List<NoteLink>();
                        }
                    }
                    else
                    {
                        backlinks = new List<NoteLink>();
                    }
                }

                // Load tags for the selected note from cache (or load if not cached)
                if (noteTagsCache.ContainsKey(note.Id))
                {
                    selectedNoteTags = noteTagsCache[note.Id];
                }
                else
                {
                    selectedNoteTags = await TagService.GetNoteTagsAsync(note.Id);
                    noteTagsCache[note.Id] = selectedNoteTags;
                }

                // Load concept links for non-concept notes
                if (!note.IsConceptNote && workspace?.Ontology != null)
                {
                    await LoadConceptLinksAsync(note.Id);
                }
                else
                {
                    currentNoteConceptLinks = new List<NoteConceptLink>();
                }

                // Force UI update
                Console.WriteLine($"[SelectNote] Calling StateHasChanged to update UI");
                StateHasChanged();
            }
            else
            {
                Console.WriteLine($"[SelectNote] Note not found for ID: {noteId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SelectNote] Error: {ex.Message}");
            ToastService.ShowError($"Error loading note: {ex.Message}");
        }
    }

    private void CreateNewNote()
    {
        creatingNote = true;
        selectedNote = null;
        newNoteTitle = string.Empty;
        noteContent = string.Empty;
        showPreview = false;
        noteAutoCreated = false; // Reset flag
    }

    private void CancelNewNote()
    {
        creatingNote = false;
        newNoteTitle = string.Empty;
        noteContent = string.Empty;
        noteAutoCreated = false;
        autoCreateTimer?.Dispose();
        autoCreateTimer = null;
    }

    private async Task SaveNewNote()
    {
        // If note was auto-created, just transition to editing it
        if (noteAutoCreated && selectedNote != null)
        {
            // Note already exists, just close the creating mode
            creatingNote = false;
            noteAutoCreated = false;
            autoCreateTimer?.Dispose();
            autoCreateTimer = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(newNoteTitle))
        {
            ToastService.ShowWarning("Please enter a note title");
            return;
        }

        try
        {
            saving = true;

            var note = await NoteService.CreateNoteAsync(
                WorkspaceId,
                currentUserId!,
                newNoteTitle,
                noteContent
            );

            notes.Insert(0, note);
            ToastService.ShowSuccess($"Created note '{newNoteTitle}'");

            // Select the new note
            creatingNote = false;
            await SelectNote(note.Id);
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error creating note: {ex.Message}");
        }
        finally
        {
            saving = false;
        }
    }

    private void OnNewNoteTitleChanged()
    {
        if (!string.IsNullOrWhiteSpace(newNoteTitle) && !noteAutoCreated)
        {
            QueueAutoCreateNote();
        }
    }

    private void OnNewNoteContentChanged()
    {
        if (!string.IsNullOrWhiteSpace(noteContent) && !noteAutoCreated)
        {
            QueueAutoCreateNote();
        }
    }

    private void QueueAutoCreateNote()
    {
        // Cancel existing timer
        autoCreateTimer?.Dispose();

        // Create new timer for 500ms debounce
        autoCreateTimer = new System.Threading.Timer(async _ =>
        {
            await InvokeAsync(async () =>
            {
                await AutoCreateNote();
            });
        }, null, 500, Timeout.Infinite);
    }

    private async Task AutoCreateNote()
    {
        if (noteAutoCreated || !creatingNote || saving)
        {
            return;
        }

        try
        {
            saving = true;

            // Use title if provided, otherwise generate one
            var title = string.IsNullOrWhiteSpace(newNoteTitle)
                ? await GenerateUntitledNoteName()
                : newNoteTitle;

            var note = await NoteService.CreateNoteAsync(
                WorkspaceId,
                currentUserId!,
                title,
                noteContent
            );

            // Add to list and select
            notes.Insert(0, note);
            selectedNote = note;
            noteAutoCreated = true;

            // Update the title if it was auto-generated
            if (string.IsNullOrWhiteSpace(newNoteTitle))
            {
                newNoteTitle = title;
            }

            // Transition from creating to editing mode
            creatingNote = false;

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error auto-creating note: {ex.Message}");
        }
        finally
        {
            saving = false;
        }
    }

    private async Task<string> GenerateUntitledNoteName()
    {
        // Find the next available "Untitled Note X" number
        var untitledNotes = notes.Where(n => n.Title.StartsWith("Untitled Note")).ToList();
        var maxNumber = 0;

        foreach (var note in untitledNotes)
        {
            var parts = note.Title.Split(' ');
            if (parts.Length >= 3 && int.TryParse(parts[2], out var num))
            {
                maxNumber = Math.Max(maxNumber, num);
            }
        }

        return $"Untitled Note {maxNumber + 1}";
    }

    private async Task SaveNote()
    {
        if (selectedNote == null)
        {
            return;
        }

        try
        {
            saving = true;

            await NoteService.UpdateNoteContentAsync(selectedNote.Id, currentUserId!, noteContent);

            // Reload notes to show any auto-created concept notes from [[wiki-links]]
            await LoadNotesAsync();

            // Update local note timestamp
            var note = notes.FirstOrDefault(n => n.Id == selectedNote.Id);
            if (note != null)
            {
                note.UpdatedAt = DateTime.UtcNow;
            }

            ToastService.ShowSuccess("Note saved");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error saving note: {ex.Message}");
        }
        finally
        {
            saving = false;
        }
    }

    private void OnNoteContentChanged()
    {
        if (selectedNote != null && currentUserId != null)
        {
            // Queue auto-save
            AutoSaveService.QueueSave(selectedNote.Id, selectedNote.Title, noteContent, currentUserId);
        }
    }

    private void OnAutoSaveStatusChanged(object? sender, AutoSaveStatusChangedEventArgs e)
    {
        // Only update UI for the currently selected note
        if (selectedNote != null && e.NoteId == selectedNote.Id)
        {
            autoSaveStatus = e.Status;

            // Update last saved time when status changes to Saved
            if (e.Status == AutoSaveStatus.Saved)
            {
                lastSavedTime = DateTime.UtcNow;
            }

            InvokeAsync(StateHasChanged);
        }
    }

    [JSInvokable]
    public async Task TogglePreview()
    {
        showPreview = !showPreview;

        // Initialize lightbox when switching to preview mode
        if (showPreview)
        {
            await Task.Delay(50); // Small delay to ensure DOM is updated
            await InitializeImageLightbox();
        }
    }

    private void ToggleCondensedView()
    {
        isCondensedView = !isCondensedView;
    }

    private void ToggleNoteView()
    {
        showNoteGraph = !showNoteGraph;
    }

    private void ToggleRightPane()
    {
        isRightPaneCollapsed = !isRightPaneCollapsed;
    }

    private void ToggleTagsExpanded()
    {
        isTagsExpanded = !isTagsExpanded;
    }

    private void ToggleMarkdownTips()
    {
        showMarkdownTips = !showMarkdownTips;
    }

    private void DismissKeyboardHints()
    {
        hideKeyboardHints = true;
    }

    private bool searching = false;
    private System.Threading.Timer? searchDebounceTimer;
    private const int SearchDebounceMilliseconds = 400; // Wait 400ms after user stops typing

    private void OnSearchInputChanged(ChangeEventArgs e)
    {
        searchTerm = e.Value?.ToString() ?? string.Empty;

        // Cancel any pending search
        searchDebounceTimer?.Dispose();

        // If search is cleared, execute immediately
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            searchDebounceTimer = new System.Threading.Timer(async _ =>
            {
                await InvokeAsync(async () => await ExecuteSearchAsync());
            }, null, 0, Timeout.Infinite);
        }
        else
        {
            // Show searching indicator immediately
            searching = true;
            StateHasChanged();

            // Debounce the actual search
            searchDebounceTimer = new System.Threading.Timer(async _ =>
            {
                await InvokeAsync(async () => await ExecuteSearchAsync());
            }, null, SearchDebounceMilliseconds, Timeout.Infinite);
        }
    }

    private async Task ExecuteSearchAsync()
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            searching = false;
            return;
        }

        try
        {
            searching = true;
            StateHasChanged();

            // If there's a search term, use server-side search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Server-side search that looks through both title and content
                var searchResults = await NoteService.SearchNotesAsync(WorkspaceId, currentUserId, searchTerm);
                notes = searchResults;
            }
            else
            {
                // No search term, reload all notes
                await LoadNotesAsync();
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error searching notes: {ex.Message}");
        }
        finally
        {
            searching = false;
            StateHasChanged();
        }
    }

    // JavaScript interop for wiki-link editor and preview
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // Create .NET object reference for JS callbacks
                dotNetHelper = DotNetObjectReference.Create(this);

                // Wait a moment for scripts to load, then initialize handlers
                await Task.Delay(100);
                await JSRuntime.InvokeVoidAsync("WorkspaceKeyboardHandler.initialize", dotNetHelper);
                await JSRuntime.InvokeVoidAsync("initializeWikiLinkPreview", dotNetHelper);

                // Initialize grid keyboard handler
                await JSRuntime.InvokeVoidAsync("noteGridKeyboard.register", dotNetHelper);

                // Initialize wiki-link editor when a note is selected
                if (selectedNote != null)
                {
                    await InitializeWikiLinkEditor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing wiki-link handlers: {ex.Message}");
            }
        }
        else if (selectedNote != null || creatingNote)
        {
            // Re-initialize when note changes
            await InitializeWikiLinkEditor();

            // Initialize lightbox if in preview mode
            if (selectedNote != null && showPreview)
            {
                await Task.Delay(50); // Small delay to ensure DOM is updated
                await InitializeImageLightbox();
            }
        }
    }

    private async Task InitializeWikiLinkEditor()
    {
        try
        {
            var elementId = creatingNote ? "note-editor-new" : $"note-editor-{selectedNote?.Id}";

            await JSRuntime.InvokeVoidAsync(
                "WikiLinkEditor.initialize",
                elementId,
                dotNetHelper
            );

            // Initialize image upload for the editor
            await InitializeImageUpload(elementId);
        }
        catch (Exception ex)
        {
            // Silently fail if editor not ready yet
            Console.WriteLine($"WikiLinkEditor init failed (may retry): {ex.Message}");
        }
    }

    private async Task InitializeImageUpload(string elementId)
    {
        try
        {
            // Load image upload module if not already loaded
            if (imageUploadModule == null)
            {
                imageUploadModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./js/imageUpload.js");
            }

            // Get the note ID for upload
            var noteId = creatingNote ? 0 : selectedNote?.Id ?? 0;

            // Initialize image upload for this editor
            if (noteId > 0 && WorkspaceId > 0)
            {
                await imageUploadModule.InvokeVoidAsync(
                    "initializeImageUpload",
                    elementId,
                    noteId,
                    WorkspaceId
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ImageUpload init failed: {ex.Message}");
        }
    }

    private async Task InitializeImageLightbox()
    {
        try
        {
            // Load image lightbox module if not already loaded
            if (imageLightboxModule == null)
            {
                imageLightboxModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./js/imageLightbox.js");
            }

            // Initialize lightbox for the markdown preview
            await imageLightboxModule.InvokeVoidAsync(
                "initializeImageLightbox",
                "markdown-preview"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ImageLightbox init failed: {ex.Message}");
        }
    }

    private async Task LoadRelatedConceptsAsync(int conceptId)
    {
        try
        {
            if (workspace?.Ontology == null)
            {
                relatedConcepts = new List<Concept>();
                return;
            }

            // Get all relationships for this concept
            var relationships = await RelationshipService.GetByConceptIdAsync(conceptId);
            var conceptIds = new HashSet<int>();

            foreach (var rel in relationships)
            {
                // Add both source and target concepts (excluding the current concept)
                if (rel.SourceConceptId != conceptId)
                {
                    conceptIds.Add(rel.SourceConceptId);
                }
                if (rel.TargetConceptId != conceptId)
                {
                    conceptIds.Add(rel.TargetConceptId);
                }
            }

            // Load the concept details
            var allConcepts = await ConceptService.GetByOntologyIdAsync(workspace.Ontology.Id);
            relatedConcepts = allConcepts
                .Where(c => conceptIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error loading related concepts: {ex.Message}");
            relatedConcepts = new List<Concept>();
        }
    }

    private async Task NavigateToConceptNote(string conceptName)
    {
        try
        {
            Console.WriteLine($"[NavigateToConceptNote] Looking for concept note: '{conceptName}'");
            Console.WriteLine($"[NavigateToConceptNote] Total notes: {notes.Count}, Concept notes: {notes.Count(n => n.IsConceptNote)}");

            // Find the concept note by name
            var note = notes.FirstOrDefault(n =>
                n.IsConceptNote &&
                n.Title.Equals(conceptName, StringComparison.OrdinalIgnoreCase));

            if (note != null)
            {
                Console.WriteLine($"[NavigateToConceptNote] Found note: {note.Title} (ID: {note.Id})");
                await SelectNote(note.Id);
            }
            else
            {
                Console.WriteLine($"[NavigateToConceptNote] Note not found for: '{conceptName}'");
                Console.WriteLine($"[NavigateToConceptNote] Available concept notes:");
                foreach (var n in notes.Where(n => n.IsConceptNote))
                {
                    Console.WriteLine($"  - '{n.Title}' (ID: {n.Id})");
                }
                ToastService.ShowInfo($"Note for '{conceptName}' not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NavigateToConceptNote] Error: {ex.Message}");
            ToastService.ShowError($"Error navigating to concept note: {ex.Message}");
        }
    }

    /// <summary>
    /// Called from JavaScript when user Ctrl+Clicks a [[wiki-link]]
    /// </summary>
    [JSInvokable]
    public async Task NavigateToConcept(string conceptName)
    {
        try
        {
            // First, try to find a note for this concept
            await NavigateToConceptNote(conceptName);
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error navigating to concept: {ex.Message}");
        }
    }

    // Quick Switcher handlers
    [JSInvokable]
    public async Task ShowQuickSwitcher()
    {
        if (quickSwitcher != null && !string.IsNullOrEmpty(currentUserId))
        {
            await quickSwitcher.ShowAsync(currentUserId);
        }
    }

    private async Task HandleQuickSwitcherNoteSelected(Note note)
    {
        if (note != null)
        {
            await SelectNote(note.Id);
        }
    }

    private void HandleQuickSwitcherHide()
    {
        // Quick switcher closed, nothing specific to do
        StateHasChanged();
    }

    // Tag management methods
    private async Task LoadTagsAsync()
    {
        try
        {
            workspaceTags = await TagService.GetWorkspaceTagsWithCountsAsync(WorkspaceId, currentUserId!);
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to load tags: {ex.Message}");
        }
    }

    private void ToggleTagFilter(int tagId)
    {
        if (selectedTagFilter == tagId)
        {
            selectedTagFilter = null;
        }
        else
        {
            selectedTagFilter = tagId;
        }
        StateHasChanged();
    }

    private void ClearTagFilter()
    {
        selectedTagFilter = null;
        StateHasChanged();
    }

    private void SetNoteTypeFilter(NoteTypeFilter filter)
    {
        noteTypeFilter = filter;
        StateHasChanged();
    }

    private async void OnSortChanged()
    {
        // Save the sort preference
        if (!string.IsNullOrEmpty(currentUserId))
        {
            try
            {
                await UserPreferencesService.UpdateDefaultNotesSortOrderAsync(currentUserId, noteSortOption.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save notes sort preference: {ex.Message}");
            }
        }
        StateHasChanged();
    }

    private async Task HandleTagsChanged()
    {
        await LoadTagsAsync();
        StateHasChanged();
    }

    private async Task HandleImportComplete()
    {
        // Reload notes and tags after import
        await LoadNotesAsync();
        await LoadTagsAsync();
        ToastService.ShowSuccess("Markdown files imported successfully");
        StateHasChanged();
    }

    private async Task HandleNoteTagsChanged(List<Tag> newTags)
    {
        if (selectedNote == null || string.IsNullOrEmpty(currentUserId))
            return;

        try
        {
            // Update the tag assignments
            await TagService.ReplaceNoteTagsAsync(selectedNote.Id, newTags.Select(t => t.Id).ToList(), currentUserId, WorkspaceId);

            // Update local state
            selectedNoteTags = newTags;

            // Update the cache
            noteTagsCache[selectedNote.Id] = newTags;

            // Reload workspace tags to update counts
            await LoadTagsAsync();

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to update tags: {ex.Message}");
        }
    }

    private Task HandleWikiLinkCreation(string updatedContent)
    {
        // Update the note content
        noteContent = updatedContent;

        // Trigger the save process
        OnNoteContentChanged();

        StateHasChanged();

        return Task.CompletedTask;
    }

    // === Toolbar Actions ===

    private bool showShareDialog = false;
    private bool showExportDialog = false;
    private bool showDeleteConfirmation = false;
    private bool deleting = false;
    private Note? noteToDelete = null;
    private bool exportInProgress = false;

    private void ShowShareDialog()
    {
        showShareDialog = true;
    }

    private void ShowSettingsDialog()
    {
        // Navigate to the ontology settings page
        if (workspace?.Ontology != null)
        {
            Navigation.NavigateTo($"ontology/{workspace.Ontology.Id}/settings");
        }
    }

    private void ShowExportDialog()
    {
        showExportDialog = true;
    }

    private void CloseExportDialog()
    {
        showExportDialog = false;
        exportInProgress = false;
    }

    private void ShowDeleteConfirmation()
    {
        if (selectedNote != null)
        {
            noteToDelete = selectedNote;
            showDeleteConfirmation = true;
        }
    }

    private void CancelDelete()
    {
        showDeleteConfirmation = false;
        noteToDelete = null;
        deleting = false;
    }

    private async Task ConfirmDelete()
    {
        if (noteToDelete == null || string.IsNullOrEmpty(currentUserId))
            return;

        deleting = true;
        try
        {
            var success = await NoteService.DeleteNoteAsync(noteToDelete.Id, currentUserId);
            if (success)
            {
                ToastService.ShowSuccess($"Deleted '{noteToDelete.Title}'");

                // Find index in FILTERED list before removing
                var filteredList = FilteredNotes.ToList();
                var deletedIndex = filteredList.FindIndex(n => n.Id == noteToDelete.Id);
                var wasSelected = selectedNote?.Id == noteToDelete.Id;

                // Remove from local list - create new list to trigger Blazor change detection
                notes = notes.Where(n => n.Id != noteToDelete.Id).ToList();

                // Clear selection immediately if we deleted the selected note
                if (wasSelected)
                {
                    selectedNote = null;
                    noteContent = string.Empty;
                    selectedNoteTags = new List<Tag>();
                    backlinks = new List<NoteLink>();
                }

                // Close dialog and update UI
                showDeleteConfirmation = false;
                noteToDelete = null;
                StateHasChanged();

                // Now select next note if there are any remaining
                if (wasSelected && notes.Any())
                {
                    // Rebuild filtered list after removal
                    var updatedFilteredList = FilteredNotes.ToList();

                    if (updatedFilteredList.Any())
                    {
                        // Try to select the note at the same index, or the previous one if at the end
                        var nextIndex = deletedIndex >= 0 && deletedIndex < updatedFilteredList.Count
                            ? deletedIndex
                            : updatedFilteredList.Count - 1;

                        var nextNote = updatedFilteredList.ElementAtOrDefault(nextIndex);

                        if (nextNote != null)
                        {
                            await SelectNote(nextNote.Id);
                        }
                    }
                }
            }
            else
            {
                ToastService.ShowError("Failed to delete note. You may not have permission.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error deleting note: {ex.Message}");
        }
        finally
        {
            deleting = false;
        }
    }

    private async Task ExportAllNotes()
    {
        if (string.IsNullOrEmpty(currentUserId) || workspace == null)
            return;

        try
        {
            exportInProgress = true;
            StateHasChanged();

            // Generate ZIP file with all notes
            var zipBytes = await MarkdownExportService.ExportNotesAsZipAsync(WorkspaceId, currentUserId);

            // Generate filename
            var sanitizedWorkspaceName = SanitizeFilename(workspace.Name);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var filename = $"{sanitizedWorkspaceName}_notes_{timestamp}.zip";

            // Trigger download via JavaScript
            await JSRuntime.InvokeVoidAsync("downloadFile", filename, "application/zip", zipBytes);

            ToastService.ShowSuccess($"Exported {notes.Count} note(s) successfully");
            CloseExportDialog();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to export notes: {ex.Message}");
            exportInProgress = false;
            StateHasChanged();
        }
    }

    private async Task ExportSingleNote(int noteId)
    {
        if (string.IsNullOrEmpty(currentUserId) || workspace == null)
            return;

        try
        {
            // Generate markdown for single note
            var markdown = await MarkdownExportService.ExportSingleNoteAsync(noteId, WorkspaceId, currentUserId);

            // Find the note to get its title
            var note = notes.FirstOrDefault(n => n.Id == noteId);
            if (note == null)
            {
                ToastService.ShowError("Note not found");
                return;
            }

            // Generate filename
            var sanitizedTitle = SanitizeFilename(note.Title);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var filename = $"{sanitizedTitle}_{timestamp}.md";

            // Convert markdown string to byte array for download
            var markdownBytes = System.Text.Encoding.UTF8.GetBytes(markdown);

            // Trigger download via JavaScript
            await JSRuntime.InvokeVoidAsync("downloadFile", filename, "text/markdown", markdownBytes);

            ToastService.ShowSuccess($"Exported '{note.Title}' successfully");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to export note: {ex.Message}");
        }
    }

    private string SanitizeFilename(string filename)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", filename.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }

    // === Markdown Toolbar Actions ===

    private async Task InsertBold() => await InsertMarkdown("**", "**", "bold text");
    private async Task InsertItalic() => await InsertMarkdown("*", "*", "italic text");
    private async Task InsertStrikethrough() => await InsertMarkdown("~~", "~~", "strikethrough");
    private async Task InsertHeading1() => await InsertMarkdownLine("# ", "Heading 1");
    private async Task InsertHeading2() => await InsertMarkdownLine("## ", "Heading 2");
    private async Task InsertHeading3() => await InsertMarkdownLine("### ", "Heading 3");
    private async Task InsertLink() => await InsertMarkdown("[", "](url)", "link text");
    private async Task InsertInlineCode() => await InsertMarkdown("`", "`", "code");
    private async Task InsertQuote() => await InsertMarkdown("> ", "", "quote");
    private async Task InsertBulletList() => await InsertMarkdownLine("- ", "list item");
    private async Task InsertNumberedList() => await InsertMarkdownLine("1. ", "numbered item");
    private async Task InsertTaskList() => await InsertMarkdownLine("- [ ] ", "task item");

    /// <summary>
    /// Insert markdown syntax around selected text or at cursor position
    /// </summary>
    private async Task InsertMarkdown(string before, string after, string defaultText)
    {
        try
        {
            var elementId = creatingNote ? "note-editor-new" : $"note-editor-{selectedNote?.Id}";
            await JSRuntime.InvokeVoidAsync("insertMarkdownSyntax", elementId, before, after, defaultText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting markdown: {ex.Message}");
        }
    }

    /// <summary>
    /// Insert markdown syntax at the beginning of the current line
    /// </summary>
    private async Task InsertMarkdownLine(string prefix, string defaultText)
    {
        try
        {
            var elementId = creatingNote ? "note-editor-new" : $"note-editor-{selectedNote?.Id}";
            await JSRuntime.InvokeVoidAsync("insertMarkdownLine", elementId, prefix, defaultText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting markdown line: {ex.Message}");
        }
    }

    /// <summary>
    /// Insert a code block
    /// </summary>
    private async Task InsertCodeBlock()
    {
        try
        {
            var elementId = creatingNote ? "note-editor-new" : $"note-editor-{selectedNote?.Id}";
            await JSRuntime.InvokeVoidAsync("insertCodeBlock", elementId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting code block: {ex.Message}");
        }
    }

    /// <summary>
    /// Insert a markdown table
    /// </summary>
    private async Task InsertTable()
    {
        try
        {
            var elementId = creatingNote ? "note-editor-new" : $"note-editor-{selectedNote?.Id}";
            await JSRuntime.InvokeVoidAsync("insertTable", elementId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting table: {ex.Message}");
        }
    }

    /// <summary>
    /// Insert a horizontal rule
    /// </summary>
    private async Task InsertHorizontalRule()
    {
        try
        {
            var elementId = creatingNote ? "note-editor-new" : $"note-editor-{selectedNote?.Id}";
            await JSRuntime.InvokeVoidAsync("insertHorizontalRule", elementId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting horizontal rule: {ex.Message}");
        }
    }

    // Smart Suggestions (Concept Links)
    private async Task LoadConceptLinksAsync(int noteId)
    {
        try
        {
            loadingConceptLinks = true;
            StateHasChanged();

            // Get concept links from database
            currentNoteConceptLinks = await NoteConceptLinkRepository.GetByNoteIdAsync(noteId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading concept links: {ex.Message}");
            currentNoteConceptLinks = new List<NoteConceptLink>();
        }
        finally
        {
            loadingConceptLinks = false;
            StateHasChanged();
        }
    }

    private async Task HandleConceptSelected(int conceptId)
    {
        try
        {
            if (workspace?.Ontology != null)
            {
                // Navigate to the ontology graph view with the selected concept
                Navigation.NavigateTo($"ontology/{workspace.Ontology.Id}?selectedConcept={conceptId}");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error navigating to concept: {ex.Message}");
        }
    }

    // Cleanup
    #region Grid Mode Methods

    /// <summary>
    /// Toggles grid mode on/off
    /// </summary>
    [JSInvokable]
    public async Task ToggleGridMode()
    {
        isGridModeEnabled = !isGridModeEnabled;

        if (isGridModeEnabled)
        {
            // Initialize grid with currently selected note
            if (selectedNote != null)
            {
                var noteState = new OpenNoteState
                {
                    NoteId = selectedNote.Id,
                    Title = selectedNote.Title,
                    Content = noteContent,
                    OriginalContent = noteContent,
                    GridPosition = 0,
                    IsDirty = false,
                    IsFocused = true
                };
                openNotesInGrid.Add(noteState);
            }

            // Update JavaScript grid mode state
            await JSRuntime.InvokeVoidAsync("noteGridKeyboard.setGridMode", true);
        }
        else
        {
            // Save any dirty notes before exiting grid mode
            foreach (var noteState in openNotesInGrid.Where(n => n.IsDirty))
            {
                await SaveGridNote(noteState);
            }

            openNotesInGrid.Clear();
            await JSRuntime.InvokeVoidAsync("noteGridKeyboard.setGridMode", false);
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Adds a new note to the grid
    /// </summary>
    [JSInvokable]
    public async Task AddNoteToGrid()
    {
        if (!isGridModeEnabled) return;

        // Create a new note
        var noteTitle = $"New Note {DateTime.Now:HH:mm:ss}";
        var createdNote = await NoteService.CreateNoteAsync(WorkspaceId, currentUserId!, noteTitle, string.Empty);
        await LoadNotesAsync(); // Refresh notes list

        // Add to grid
        var noteState = new OpenNoteState
        {
            NoteId = createdNote.Id,
            Title = createdNote.Title,
            Content = string.Empty,
            OriginalContent = string.Empty,
            GridPosition = openNotesInGrid.Count,
            IsDirty = false,
            IsFocused = true
        };

        // Unfocus all other notes
        foreach (var note in openNotesInGrid)
        {
            note.IsFocused = false;
        }

        openNotesInGrid.Add(noteState);
        await InvokeAsync(StateHasChanged);

        ToastService.ShowSuccess("Note added to grid");
    }

    /// <summary>
    /// Closes the currently focused note
    /// </summary>
    [JSInvokable]
    public async Task CloseCurrentNote()
    {
        if (!isGridModeEnabled) return;

        var focusedNote = openNotesInGrid.FirstOrDefault(n => n.IsFocused);
        if (focusedNote == null) return;

        // Save if dirty
        if (focusedNote.IsDirty)
        {
            await SaveGridNote(focusedNote);
        }

        openNotesInGrid.Remove(focusedNote);

        // Reorder grid positions
        for (int i = 0; i < openNotesInGrid.Count; i++)
        {
            openNotesInGrid[i].GridPosition = i;
        }

        // Focus next note if available
        if (openNotesInGrid.Any())
        {
            openNotesInGrid.First().IsFocused = true;
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Focuses a specific note by grid index (0-based)
    /// </summary>
    [JSInvokable]
    public async Task FocusNote(int index)
    {
        if (!isGridModeEnabled || index < 0 || index >= openNotesInGrid.Count) return;

        var visibleNotes = openNotesInGrid.OrderBy(n => n.GridPosition).Take(maxVisibleNotesInGrid).ToList();
        if (index >= visibleNotes.Count) return;

        // Unfocus all notes
        foreach (var note in openNotesInGrid)
        {
            note.IsFocused = false;
        }

        // Focus the selected note
        visibleNotes[index].IsFocused = true;

        await InvokeAsync(StateHasChanged);
        await JSRuntime.InvokeVoidAsync("noteGridKeyboard.focusNoteByIndex", index);
    }

    /// <summary>
    /// Cycles through notes in the grid (direction: 1 for forward, -1 for backward)
    /// </summary>
    [JSInvokable]
    public async Task CycleNoteFocus(int direction)
    {
        if (!isGridModeEnabled || !openNotesInGrid.Any()) return;

        var visibleNotes = openNotesInGrid.OrderBy(n => n.GridPosition).Take(maxVisibleNotesInGrid).ToList();
        var currentIndex = visibleNotes.FindIndex(n => n.IsFocused);

        if (currentIndex == -1) currentIndex = 0; // Default to first if none focused

        // Calculate next index with wrapping
        var nextIndex = (currentIndex + direction + visibleNotes.Count) % visibleNotes.Count;

        // Unfocus all
        foreach (var note in openNotesInGrid)
        {
            note.IsFocused = false;
        }

        // Focus next
        visibleNotes[nextIndex].IsFocused = true;

        await InvokeAsync(StateHasChanged);
        await JSRuntime.InvokeVoidAsync("noteGridKeyboard.focusNoteByIndex", nextIndex);
    }

    /// <summary>
    /// Toggles full screen mode for workspace
    /// </summary>
    [JSInvokable]
    public async Task ToggleFullScreen()
    {
        isWorkspaceFullScreen = !isWorkspaceFullScreen;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Saves a grid note
    /// </summary>
    private async Task SaveGridNote(OpenNoteState noteState)
    {
        try
        {
            // Update note content
            await NoteService.UpdateNoteContentAsync(noteState.NoteId, currentUserId!, noteState.Content);

            noteState.OriginalContent = noteState.Content;
            noteState.IsDirty = false;
            noteState.IsSaving = false;
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to save note: {ex.Message}");
            noteState.IsSaving = false;
        }
    }

    /// <summary>
    /// Handles content changes in grid notes
    /// </summary>
    private async Task HandleGridNoteContentChanged(OpenNoteState noteState)
    {
        // Save the note content to the database
        await SaveGridNote(noteState);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handles title changes in grid notes
    /// </summary>
    private async Task HandleGridNoteTitleChanged((int NoteId, string NewTitle) change)
    {
        try
        {
            // Update note title in database
            await NoteService.UpdateNoteTitleAsync(change.NoteId, currentUserId!, change.NewTitle);

            // Update the note state
            var noteState = openNotesInGrid.FirstOrDefault(n => n.NoteId == change.NoteId);
            if (noteState != null)
            {
                noteState.Title = change.NewTitle;
            }

            // Also reload notes list to update sidebar
            await LoadNotesAsync();

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to save note title: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles closing a note from the grid
    /// </summary>
    private async Task HandleGridNoteClose(int noteId)
    {
        var noteState = openNotesInGrid.FirstOrDefault(n => n.NoteId == noteId);
        if (noteState != null)
        {
            if (noteState.IsDirty)
            {
                await SaveGridNote(noteState);
            }

            openNotesInGrid.Remove(noteState);

            // Reorder positions
            for (int i = 0; i < openNotesInGrid.Count; i++)
            {
                openNotesInGrid[i].GridPosition = i;
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Handles swapping a note from the switcher into the grid
    /// </summary>
    private async Task HandleSwapNote(int noteId)
    {
        var noteToSwap = openNotesInGrid.FirstOrDefault(n => n.NoteId == noteId);
        if (noteToSwap != null && noteGridLayoutRef != null)
        {
            // Find first visible note
            var firstVisible = openNotesInGrid.OrderBy(n => n.GridPosition).First();

            // Swap grid positions
            var tempPosition = firstVisible.GridPosition;
            firstVisible.GridPosition = noteToSwap.GridPosition;
            noteToSwap.GridPosition = tempPosition;

            await InvokeAsync(StateHasChanged);
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        try
        {
            // Dispose search debounce timer
            searchDebounceTimer?.Dispose();

            // Unsubscribe from auto-save status changes
            AutoSaveService.StatusChanged -= OnAutoSaveStatusChanged;

            // Force save any pending auto-save before disposing
            if (selectedNote != null && currentUserId != null)
            {
                await AutoSaveService.ForceSaveAsync(selectedNote.Id, selectedNote.Title, noteContent, currentUserId);
            }

            if (dotNetHelper != null)
            {
                // Dispose JavaScript handlers
                await JSRuntime.InvokeVoidAsync("WorkspaceKeyboardHandler.dispose");
                await JSRuntime.InvokeVoidAsync("disposeWikiLinkPreview");
                await JSRuntime.InvokeVoidAsync("noteGridKeyboard.unregister");

                if (selectedNote != null || creatingNote)
                {
                    var elementId = creatingNote ? "note-editor-new" : $"note-editor-{selectedNote?.Id}";
                    await JSRuntime.InvokeVoidAsync("WikiLinkEditor.dispose", elementId);
                }

                dotNetHelper.Dispose();
            }

            if (wikiLinkEditorModule != null)
            {
                await wikiLinkEditorModule.DisposeAsync();
            }

            if (imageUploadModule != null)
            {
                await imageUploadModule.DisposeAsync();
            }

            if (imageLightboxModule != null)
            {
                await imageLightboxModule.InvokeVoidAsync("disposeImageLightbox");
                await imageLightboxModule.DisposeAsync();
            }
        }
        catch
        {
            // Ignore disposal errors
        }
    }
}
