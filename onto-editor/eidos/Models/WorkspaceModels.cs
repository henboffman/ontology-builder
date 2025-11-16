using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Eidos.Models.Enums;

namespace Eidos.Models
{
    /// <summary>
    /// Workspace - The root entity (like an Obsidian vault)
    /// Contains both an Ontology (structured knowledge) and Notes (unstructured knowledge)
    /// </summary>
    public class Workspace
    {
        public int Id { get; set; }

        /// <summary>
        /// Workspace name (like vault name in Obsidian)
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the workspace
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Owner of this workspace
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Visibility settings (moved from Ontology)
        /// Values: "private", "group", "public"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Visibility { get; set; } = "private";

        /// <summary>
        /// Whether public users can edit (if Visibility is Public)
        /// </summary>
        public bool AllowPublicEdit { get; set; } = false;

        /// <summary>
        /// Metadata
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties

        /// <summary>
        /// The ontology contained in this workspace (1:1 relationship)
        /// </summary>
        public Ontology? Ontology { get; set; }

        /// <summary>
        /// All notes in this workspace (1:many relationship)
        /// </summary>
        public ICollection<Note> Notes { get; set; } = new List<Note>();

        /// <summary>
        /// Group-based permissions for this workspace
        /// </summary>
        public ICollection<WorkspaceGroupPermission> GroupPermissions { get; set; } = new List<WorkspaceGroupPermission>();

        /// <summary>
        /// Direct user access permissions (for sharing with specific users)
        /// </summary>
        public ICollection<WorkspaceUserAccess> UserAccesses { get; set; } = new List<WorkspaceUserAccess>();

        /// <summary>
        /// Denormalized stats for performance
        /// </summary>
        public int NoteCount { get; set; } = 0;
        public int ConceptNoteCount { get; set; } = 0;
        public int UserNoteCount { get; set; } = 0;
    }

    /// <summary>
    /// Note - A markdown document (like a file in Obsidian)
    /// Can be a user-created note or an auto-generated concept note
    /// Content is stored separately in NoteContent table for performance
    /// </summary>
    public class Note
    {
        public int Id { get; set; }

        /// <summary>
        /// Workspace this note belongs to
        /// </summary>
        public int WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        /// <summary>
        /// Note title (corresponds to filename in Obsidian)
        /// For concept notes: "{ConceptName}"
        /// For user notes: any title
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Markdown content (stored in separate table for performance)
        /// Load explicitly with Include(n => n.Content)
        /// </summary>
        public NoteContent? Content { get; set; }

        /// <summary>
        /// Denormalized content length (updated on save)
        /// Enables efficient filtering and sorting without loading content
        /// </summary>
        public int ContentLength { get; set; } = 0;

        /// <summary>
        /// Denormalized link count (updated on save)
        /// Number of [[concept]] references in this note
        /// </summary>
        public int LinkCount { get; set; } = 0;

        /// <summary>
        /// Whether this is an auto-generated concept note
        /// </summary>
        public bool IsConceptNote { get; set; } = false;

        /// <summary>
        /// If IsConceptNote=true, the concept this note is linked to
        /// </summary>
        public int? LinkedConceptId { get; set; }
        public Concept? LinkedConcept { get; set; }

        /// <summary>
        /// User who created this note
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Metadata
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Outgoing links from this note to concepts (parsed from [[]] syntax)
        /// </summary>
        public ICollection<NoteLink> OutgoingLinks { get; set; } = new List<NoteLink>();

        /// <summary>
        /// Incoming links (backlinks) - notes that link to this note's concept
        /// Only applicable if IsConceptNote=true
        /// </summary>
        public ICollection<NoteLink> IncomingLinks { get; set; } = new List<NoteLink>();
    }

    /// <summary>
    /// NoteLink - Represents a [[wiki-style link]] from a note to a concept
    /// Enables bidirectional linking and backlinks (Obsidian-style)
    /// </summary>
    public class NoteLink
    {
        public int Id { get; set; }

        /// <summary>
        /// The note containing the [[link]]
        /// </summary>
        public int SourceNoteId { get; set; }
        public Note SourceNote { get; set; } = null!;

        /// <summary>
        /// The concept being referenced
        /// </summary>
        public int TargetConceptId { get; set; }
        public Concept TargetConcept { get; set; } = null!;

        /// <summary>
        /// Position in the note where the link appears (for context snippets)
        /// </summary>
        public int CharacterPosition { get; set; }

        /// <summary>
        /// Snippet of text around the link (for backlinks panel)
        /// </summary>
        [StringLength(500)]
        public string? ContextSnippet { get; set; }

        /// <summary>
        /// When this link was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// NoteContent - Stores the actual markdown content separately from Note metadata
    /// This separation provides 10-100x faster note list queries by avoiding loading large text fields
    /// Content should be loaded explicitly with Include(n => n.Content) when needed
    /// </summary>
    public class NoteContent
    {
        /// <summary>
        /// Primary key and foreign key to Note (1:1 relationship)
        /// </summary>
        public int NoteId { get; set; }
        public Note Note { get; set; } = null!;

        /// <summary>
        /// Raw markdown content
        /// </summary>
        [Required]
        public string MarkdownContent { get; set; } = string.Empty;

        /// <summary>
        /// Cached HTML rendering of markdown (optional performance optimization)
        /// Can be regenerated from MarkdownContent if null
        /// </summary>
        public string? RenderedHtml { get; set; }

        /// <summary>
        /// Last time content was modified
        /// Used to invalidate cached HTML rendering
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Group-based permissions for workspaces (replaces OntologyGroupPermission)
    /// </summary>
    public class WorkspaceGroupPermission
    {
        public int Id { get; set; }

        public int WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public int UserGroupId { get; set; }
        public UserGroup UserGroup { get; set; } = null!;

        public PermissionLevel PermissionLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Direct user access to workspaces (replaces UserShareAccess)
    /// </summary>
    public class WorkspaceUserAccess
    {
        public int Id { get; set; }

        public int WorkspaceId { get; set; }
        public Workspace Workspace { get; set; } = null!;

        [Required]
        public string SharedWithUserId { get; set; } = string.Empty;
        public ApplicationUser SharedWithUser { get; set; } = null!;

        public PermissionLevel PermissionLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
