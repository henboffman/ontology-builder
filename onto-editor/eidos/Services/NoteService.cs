using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Eidos.Services
{
    /// <summary>
    /// Business logic service for note management (Obsidian-style)
    /// Handles note CRUD, [[wiki-link]] parsing, and auto-concept creation
    /// </summary>
    public class NoteService
    {
        private readonly NoteRepository _noteRepository;
        private readonly WorkspaceRepository _workspaceRepository;
        private readonly IConceptService _conceptService;
        private readonly IConceptRepository _conceptRepository;
        private readonly WikiLinkParser _wikiLinkParser;
        private readonly ILogger<NoteService> _logger;

        public NoteService(
            NoteRepository noteRepository,
            WorkspaceRepository workspaceRepository,
            IConceptService conceptService,
            IConceptRepository conceptRepository,
            WikiLinkParser wikiLinkParser,
            ILogger<NoteService> logger)
        {
            _noteRepository = noteRepository;
            _workspaceRepository = workspaceRepository;
            _conceptService = conceptService;
            _conceptRepository = conceptRepository;
            _wikiLinkParser = wikiLinkParser;
            _logger = logger;
        }

        /// <summary>
        /// Create a new user note
        /// </summary>
        public async Task<Note> CreateNoteAsync(int workspaceId, string userId, string title, string markdownContent)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentException("Note title is required", nameof(title));
                }

                // Check workspace access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);
                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException($"User {userId} does not have access to workspace {workspaceId}");
                }

                // Create note
                var note = new Note
                {
                    WorkspaceId = workspaceId,
                    Title = title.Trim(),
                    IsConceptNote = false,
                    UserId = userId,
                    LinkCount = _wikiLinkParser.CountLinks(markdownContent)
                };

                var created = await _noteRepository.CreateAsync(note, markdownContent);

                // Process [[wiki-links]] and create concepts if needed
                await ProcessWikiLinksAsync(created.Id, workspaceId, markdownContent);

                // Update workspace note counts
                await _workspaceRepository.UpdateNoteCountsAsync(workspaceId);

                _logger.LogInformation("Created note {NoteId} titled '{Title}' in workspace {WorkspaceId}",
                    created.Id, title, workspaceId);

                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note '{Title}' in workspace {WorkspaceId}", title, workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Create a concept note (auto-generated for each concept)
        /// </summary>
        public async Task<Note> CreateConceptNoteAsync(int workspaceId, string userId, int conceptId, string conceptName)
        {
            try
            {
                // Check if concept note already exists
                var existing = await _noteRepository.GetConceptNoteAsync(conceptId);
                if (existing != null)
                {
                    _logger.LogWarning("Concept note already exists for concept {ConceptId}", conceptId);
                    return existing;
                }

                // Generate default content for concept note
                var markdownContent = $@"# {conceptName}

*Auto-generated note for concept: {conceptName}*

## Definition

[Add the concept's definition here]

## Notes

[Add your notes about this concept here]
";

                var note = new Note
                {
                    WorkspaceId = workspaceId,
                    Title = conceptName,
                    IsConceptNote = true,
                    LinkedConceptId = conceptId,
                    UserId = userId,
                    LinkCount = 0
                };

                var created = await _noteRepository.CreateAsync(note, markdownContent);

                // Update workspace note counts
                await _workspaceRepository.UpdateNoteCountsAsync(workspaceId);

                _logger.LogInformation("Created concept note {NoteId} for concept {ConceptId}",
                    created.Id, conceptId);

                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating concept note for concept {ConceptId}", conceptId);
                throw;
            }
        }

        /// <summary>
        /// Update note content and process wiki-links
        /// </summary>
        public async Task UpdateNoteContentAsync(int noteId, string userId, string markdownContent)
        {
            try
            {
                var note = await _noteRepository.GetByIdAsync(noteId);
                if (note == null)
                {
                    throw new InvalidOperationException($"Note {noteId} not found");
                }

                // Check access (only owner can edit for now)
                if (note.UserId != userId)
                {
                    throw new UnauthorizedAccessException($"User {userId} cannot edit note {noteId}");
                }

                // Update content
                await _noteRepository.UpdateContentAsync(noteId, markdownContent);

                // Update link count
                var linkCount = _wikiLinkParser.CountLinks(markdownContent);
                await _noteRepository.UpdateMetadataAsync(noteId, linkCount);

                // Process [[wiki-links]]
                await ProcessWikiLinksAsync(noteId, note.WorkspaceId, markdownContent);

                _logger.LogInformation("Updated content for note {NoteId}", noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating content for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Process [[wiki-links]] in note content
        /// Auto-creates concepts that don't exist
        /// </summary>
        private async Task ProcessWikiLinksAsync(int noteId, int workspaceId, string markdownContent)
        {
            try
            {
                // Parse links with context
                var parsedLinks = _wikiLinkParser.ExtractLinksWithContext(markdownContent);

                if (!parsedLinks.Any())
                {
                    // No links, remove existing ones
                    await _noteRepository.UpdateNoteLinksAsync(noteId, new List<NoteLink>());
                    return;
                }

                // Get workspace to find ontology
                var workspace = await _workspaceRepository.GetByIdAsync(workspaceId, includeOntology: true);
                if (workspace?.Ontology == null)
                {
                    _logger.LogWarning("Workspace {WorkspaceId} has no ontology, cannot create concepts from links", workspaceId);
                    return;
                }

                var ontologyId = workspace.Ontology.Id;
                var noteLinks = new List<NoteLink>();

                foreach (var parsedLink in parsedLinks)
                {
                    // Find or create concept
                    var concept = await FindOrCreateConceptAsync(ontologyId, parsedLink.ConceptName, workspace.UserId);

                    if (concept != null)
                    {
                        var noteLink = new NoteLink
                        {
                            SourceNoteId = noteId,
                            TargetConceptId = concept.Id,
                            CharacterPosition = parsedLink.Position,
                            ContextSnippet = parsedLink.ContextSnippet,
                            CreatedAt = DateTime.UtcNow
                        };

                        noteLinks.Add(noteLink);
                    }
                }

                // Update note links in database
                await _noteRepository.UpdateNoteLinksAsync(noteId, noteLinks);

                _logger.LogInformation("Processed {Count} wiki-links for note {NoteId}", noteLinks.Count, noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing wiki-links for note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Find existing concept by name or create new one
        /// Core Obsidian-style auto-creation logic
        /// Also auto-creates a concept note if one doesn't exist
        /// </summary>
        private async Task<Concept?> FindOrCreateConceptAsync(int ontologyId, string conceptName, string userId)
        {
            try
            {
                // Find existing concept by name (case-insensitive)
                var existing = await _conceptRepository.GetByOntologyIdAsync(ontologyId);
                var concept = existing.FirstOrDefault(c =>
                    c.Name.Equals(conceptName, StringComparison.OrdinalIgnoreCase));

                if (concept != null)
                {
                    // Concept exists - ensure it has a note
                    await EnsureConceptNoteExistsAsync(concept.Id, conceptName, userId);
                    return concept;
                }

                // Create new concept
                var newConcept = new Concept
                {
                    Name = conceptName,
                    Definition = $"Auto-created from wiki-link [[{conceptName}]]",
                    Category = "Auto-Created",
                    OntologyId = ontologyId
                };

                var created = await _conceptService.CreateAsync(newConcept, recordUndo: false);

                _logger.LogInformation("Auto-created concept '{ConceptName}' in ontology {OntologyId} from wiki-link",
                    conceptName, ontologyId);

                // Auto-create concept note for new concept
                if (created != null)
                {
                    await EnsureConceptNoteExistsAsync(created.Id, conceptName, userId);
                }

                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding or creating concept '{ConceptName}' in ontology {OntologyId}",
                    conceptName, ontologyId);
                return null;
            }
        }

        /// <summary>
        /// Ensure a concept note exists for the given concept
        /// If it doesn't exist, create it
        /// </summary>
        private async Task EnsureConceptNoteExistsAsync(int conceptId, string conceptName, string userId)
        {
            try
            {
                // Check if concept note already exists
                var existingNote = await _noteRepository.GetConceptNoteAsync(conceptId);
                if (existingNote != null)
                {
                    return; // Note already exists
                }

                // Get the workspace for this concept (via ontology)
                var concept = await _conceptRepository.GetByIdAsync(conceptId);
                if (concept == null)
                {
                    _logger.LogWarning("Cannot create concept note: Concept {ConceptId} not found", conceptId);
                    return;
                }

                // Find workspace with this ontology
                // We need to load ontology separately since Get ByUserIdAsync doesn't include it
                var workspaces = await _workspaceRepository.GetByUserIdAsync(userId);
                Workspace? workspace = null;
                foreach (var ws in workspaces)
                {
                    var fullWs = await _workspaceRepository.GetByIdAsync(ws.Id, includeOntology: true);
                    if (fullWs?.Ontology?.Id == concept.OntologyId)
                    {
                        workspace = fullWs;
                        break;
                    }
                }

                if (workspace == null)
                {
                    _logger.LogWarning("Cannot create concept note: No workspace found for ontology {OntologyId}", concept.OntologyId);
                    return;
                }

                // Create concept note
                await CreateConceptNoteAsync(workspace.Id, userId, conceptId, conceptName);

                _logger.LogInformation("Auto-created concept note for '{ConceptName}' (Concept {ConceptId})",
                    conceptName, conceptId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring concept note exists for concept {ConceptId}", conceptId);
                // Don't throw - this is a nice-to-have feature
            }
        }

        /// <summary>
        /// Get note by ID with content
        /// </summary>
        public async Task<Note?> GetNoteWithContentAsync(int noteId, string userId)
        {
            try
            {
                var note = await _noteRepository.GetByIdWithContentAsync(noteId);

                if (note == null)
                {
                    return null;
                }

                // Check access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(note.WorkspaceId, userId);
                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} denied access to note {NoteId}", userId, noteId);
                    return null;
                }

                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note {NoteId} for user {UserId}", noteId, userId);
                throw;
            }
        }

        /// <summary>
        /// Get all notes in a workspace
        /// </summary>
        public async Task<List<Note>> GetWorkspaceNotesAsync(int workspaceId, string userId, bool conceptNotesOnly = false)
        {
            try
            {
                // Check access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);
                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} denied access to workspace {WorkspaceId} notes", userId, workspaceId);
                    return new List<Note>();
                }

                return await _noteRepository.GetByWorkspaceIdAsync(workspaceId, conceptNotesOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notes for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        /// <summary>
        /// Get backlinks for a concept
        /// </summary>
        public async Task<List<NoteLink>> GetBacklinksAsync(int conceptId)
        {
            try
            {
                return await _noteRepository.GetBacklinksAsync(conceptId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting backlinks for concept {ConceptId}", conceptId);
                throw;
            }
        }

        /// <summary>
        /// Delete a note
        /// </summary>
        public async Task<bool> DeleteNoteAsync(int noteId, string userId)
        {
            try
            {
                var note = await _noteRepository.GetByIdAsync(noteId);

                if (note == null)
                {
                    return false;
                }

                // Only owner can delete
                if (note.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete note {NoteId} owned by {OwnerId}",
                        userId, noteId, note.UserId);
                    return false;
                }

                await _noteRepository.DeleteAsync(noteId);

                // Update workspace note counts
                await _workspaceRepository.UpdateNoteCountsAsync(note.WorkspaceId);

                _logger.LogInformation("Deleted note {NoteId}", noteId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note {NoteId}", noteId);
                throw;
            }
        }

        /// <summary>
        /// Search notes by title and content
        /// </summary>
        public async Task<List<Note>> SearchNotesAsync(int workspaceId, string userId, string searchTerm)
        {
            try
            {
                // Check access
                var hasAccess = await _workspaceRepository.UserHasAccessAsync(workspaceId, userId);
                if (!hasAccess)
                {
                    return new List<Note>();
                }

                return await _noteRepository.SearchNotesAsync(workspaceId, searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching notes in workspace {WorkspaceId} for '{SearchTerm}'",
                    workspaceId, searchTerm);
                throw;
            }
        }

        /// <summary>
        /// Bulk create concept notes for all concepts in an ontology that don't have notes yet
        /// Used when migrating legacy ontologies to the workspace system
        /// </summary>
        public async Task<int> EnsureConceptNotesForOntologyAsync(int workspaceId, int ontologyId, string userId)
        {
            try
            {
                _logger.LogInformation("Ensuring concept notes exist for ontology {OntologyId} in workspace {WorkspaceId}", ontologyId, workspaceId);

                // Get all concepts in the ontology
                var concepts = await _conceptRepository.GetByOntologyIdAsync(ontologyId);

                if (!concepts.Any())
                {
                    _logger.LogInformation("No concepts found in ontology {OntologyId}", ontologyId);
                    return 0;
                }

                int createdCount = 0;

                foreach (var concept in concepts)
                {
                    // Check if concept note already exists
                    var existingNote = await _noteRepository.GetConceptNoteAsync(concept.Id);
                    if (existingNote == null)
                    {
                        // Create concept note
                        await CreateConceptNoteAsync(workspaceId, userId, concept.Id, concept.Name);
                        createdCount++;
                    }
                }

                if (createdCount > 0)
                {
                    // Update workspace note counts
                    await _workspaceRepository.UpdateNoteCountsAsync(workspaceId);
                    _logger.LogInformation("Created {Count} concept notes for ontology {OntologyId}", createdCount, ontologyId);
                }
                else
                {
                    _logger.LogInformation("All concepts in ontology {OntologyId} already have notes", ontologyId);
                }

                return createdCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring concept notes for ontology {OntologyId}", ontologyId);
                throw;
            }
        }
    }
}
