# In-Context Commenting & Discussion Threads - Implementation Plan

**Feature Owner**: Requirements Architect
**Created**: November 9, 2025
**Status**: Planning
**Priority**: High
**Estimated Complexity**: Medium-High

---

## Executive Summary

This document outlines the architecture and implementation plan for **In-Context Commenting & Discussion Threads** - a Figma/Google Docs-style commenting system that allows users to attach threaded discussions directly to ontology entities (concepts and relationships).

### Value Proposition

Currently, Eidos has a general "Notes View" for ontology-wide documentation. However, modern collaboration tools (Figma, Google Docs, Notion) have proven that **entity-specific threaded discussions** are far more effective for:

- **Contextual Collaboration**: Questions and discussions stay attached to the specific entity being discussed
- **@Mentions**: Direct colleague notification for quick feedback loops
- **Real-Time Awareness**: Leverage existing SignalR infrastructure for instant comment notifications
- **Knowledge Retention**: Comments become searchable, historical context for design decisions
- **Reduced Communication Overhead**: No need to switch to Slack/email to discuss specific entities

---

## Current Architecture Analysis

### Existing Infrastructure (✅ Available for Reuse)

Based on codebase analysis, we have excellent foundational pieces:

1. **SignalR Hub (`OntologyHub.cs`)**
   - Already broadcasting real-time updates
   - Permission-aware (checks `CanViewAsync` before joining)
   - Group-based messaging (`ontology_{id}`)
   - Presence tracking (users, avatars, current view)
   - **Reusable for**: Broadcasting new comments/replies in real-time

2. **Comment Model Pattern (`MergeRequestComment.cs`)**
   - Proven comment structure with:
     - User attribution
     - Timestamps (created, edited)
     - System vs. user comments
     - Optional entity reference (we'll adapt this)
   - **Reusable as**: Template for `EntityComment` model

3. **Permission System (`OntologyPermissionService`)**
   - Already checking view/edit/manage permissions
   - Group-based access control
   - **Reusable for**: Comment create/edit/delete permissions

4. **Entity Models**
   - `Concept` (Lines 249-284 in `OntologyModels.cs`)
   - `Relationship` (Lines 289-319)
   - Both have `Id`, `OntologyId`, and metadata
   - **Reusable for**: Polymorphic comment targeting

### Gaps to Address

1. **No generic commenting infrastructure** - `MergeRequestComment` is specific to merge requests
2. **No @mention parsing or notification system**
3. **No context menu/right-click UI pattern** in graph or list views
4. **No comment count badges** on entities
5. **No comment thread UI components**

---

## Proposed Architecture

### 1. Data Model

Create a flexible, polymorphic commenting system:

```csharp
/// <summary>
/// Represents an in-context comment attached to an ontology entity
/// Supports threaded discussions via ParentCommentId
/// </summary>
public class EntityComment
{
    public int Id { get; set; }

    /// <summary>
    /// The ontology this comment belongs to (for scoping and permissions)
    /// </summary>
    public int OntologyId { get; set; }
    public Ontology Ontology { get; set; } = null!;

    /// <summary>
    /// Entity type: "Concept", "Relationship", "Individual"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity being commented on (polymorphic reference)
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// User who wrote the comment
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// The comment text (Markdown supported)
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Parent comment for threading (null for top-level comments)
    /// </summary>
    public int? ParentCommentId { get; set; }
    public EntityComment? ParentComment { get; set; }

    /// <summary>
    /// Child replies (for threaded navigation)
    /// </summary>
    public ICollection<EntityComment> Replies { get; set; } = new List<EntityComment>();

    /// <summary>
    /// Whether the comment has been resolved (for issues/questions)
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// User who resolved the comment (if applicable)
    /// </summary>
    public string? ResolvedByUserId { get; set; }
    public ApplicationUser? ResolvedBy { get; set; }

    /// <summary>
    /// When the comment was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the comment was last edited
    /// </summary>
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// Collection of @mentions in this comment
    /// </summary>
    public ICollection<CommentMention> Mentions { get; set; } = new List<CommentMention>();
}

/// <summary>
/// Represents a user @mentioned in a comment
/// </summary>
public class CommentMention
{
    public int Id { get; set; }

    public int CommentId { get; set; }
    public EntityComment Comment { get; set; } = null!;

    /// <summary>
    /// User who was mentioned
    /// </summary>
    [Required]
    public string MentionedUserId { get; set; } = string.Empty;
    public ApplicationUser MentionedUser { get; set; } = null!;

    /// <summary>
    /// Whether the mentioned user has viewed this comment
    /// </summary>
    public bool HasViewed { get; set; }

    /// <summary>
    /// When the mention was created (same as comment creation)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Denormalized comment counts on entities (for performance)
/// </summary>
public class EntityCommentCount
{
    public int Id { get; set; }

    public int OntologyId { get; set; }
    public Ontology Ontology { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    /// <summary>
    /// Total number of comments (including replies)
    /// </summary>
    public int TotalComments { get; set; }

    /// <summary>
    /// Number of unresolved comment threads
    /// </summary>
    public int UnresolvedThreads { get; set; }

    /// <summary>
    /// When the last comment was added
    /// </summary>
    public DateTime LastCommentAt { get; set; }

    /// <summary>
    /// Last updated timestamp (for cache invalidation)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

**Design Rationale**:
- **Polymorphic targeting**: `EntityType` + `EntityId` allows comments on any entity without foreign key constraints
- **Threaded structure**: `ParentCommentId` enables nested replies (like Figma/Google Docs)
- **Resolution tracking**: `IsResolved` supports question/issue workflows
- **Denormalized counts**: Separate table for badge counts (avoids N+1 queries)
- **Mention extraction**: `CommentMention` table for efficient mention queries and notifications

### 2. Service Layer

```csharp
/// <summary>
/// Service for managing in-context comments and discussions
/// </summary>
public interface IEntityCommentService
{
    // Core CRUD
    Task<EntityComment> AddCommentAsync(int ontologyId, string entityType, int entityId,
        string userId, string text, int? parentCommentId = null);
    Task<EntityComment> UpdateCommentAsync(int commentId, string userId, string newText);
    Task DeleteCommentAsync(int commentId, string userId);

    // Thread retrieval
    Task<List<EntityComment>> GetCommentsForEntityAsync(int ontologyId, string entityType, int entityId);
    Task<EntityComment?> GetCommentThreadAsync(int commentId); // Get full thread with replies

    // Resolution
    Task MarkThreadResolvedAsync(int commentId, string userId, bool resolved);

    // Mentions
    Task<List<CommentMention>> GetUserMentionsAsync(string userId, int ontologyId, bool unreadOnly = false);
    Task MarkMentionViewedAsync(int mentionId, string userId);

    // Comment counts
    Task<EntityCommentCount?> GetCommentCountAsync(int ontologyId, string entityType, int entityId);
    Task<Dictionary<int, EntityCommentCount>> GetCommentCountsForEntitiesAsync(int ontologyId, string entityType, List<int> entityIds);

    // Permission checks
    Task<bool> CanAddCommentAsync(int ontologyId, string userId);
    Task<bool> CanEditCommentAsync(int commentId, string userId);
    Task<bool> CanDeleteCommentAsync(int commentId, string userId);
}
```

**Implementation Notes**:
- Reuse `OntologyPermissionService` for access control
- Parse @mentions from comment text (e.g., `@username` or `@email`)
- Update denormalized counts in same transaction
- Emit SignalR events for real-time updates

### 3. SignalR Integration

Add new methods to `OntologyHub`:

```csharp
/// <summary>
/// Add a comment to an entity
/// </summary>
public async Task AddComment(int ontologyId, string entityType, int entityId, string text, int? parentCommentId)
{
    // Permission check (must have view access + edit if required)
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!await _commentService.CanAddCommentAsync(ontologyId, userId))
        throw new HubException("You do not have permission to comment");

    // Create comment
    var comment = await _commentService.AddCommentAsync(ontologyId, entityType, entityId, userId, text, parentCommentId);

    // Broadcast to all users in this ontology
    var groupName = GetOntologyGroupName(ontologyId);
    await Clients.Group(groupName).SendAsync("CommentAdded", comment);

    // Send mention notifications to specific users
    foreach (var mention in comment.Mentions)
    {
        await Clients.User(mention.MentionedUserId).SendAsync("MentionNotification", comment);
    }
}

/// <summary>
/// Update an existing comment
/// </summary>
public async Task UpdateComment(int commentId, string newText)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!await _commentService.CanEditCommentAsync(commentId, userId))
        throw new HubException("You do not have permission to edit this comment");

    var comment = await _commentService.UpdateCommentAsync(commentId, userId, newText);

    // Broadcast update
    var groupName = GetOntologyGroupName(comment.OntologyId);
    await Clients.Group(groupName).SendAsync("CommentUpdated", comment);
}

/// <summary>
/// Delete a comment
/// </summary>
public async Task DeleteComment(int commentId)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!await _commentService.CanDeleteCommentAsync(commentId, userId))
        throw new HubException("You do not have permission to delete this comment");

    var comment = await _commentService.DeleteCommentAsync(commentId, userId);

    // Broadcast deletion
    var groupName = GetOntologyGroupName(comment.OntologyId);
    await Clients.Group(groupName).SendAsync("CommentDeleted", commentId, comment.EntityType, comment.EntityId);
}

/// <summary>
/// Mark a comment thread as resolved/unresolved
/// </summary>
public async Task ResolveCommentThread(int commentId, bool resolved)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    await _commentService.MarkThreadResolvedAsync(commentId, userId, resolved);

    var comment = await _commentService.GetCommentThreadAsync(commentId);
    var groupName = GetOntologyGroupName(comment.OntologyId);
    await Clients.Group(groupName).SendAsync("CommentResolutionChanged", commentId, resolved, userId);
}
```

**Real-Time Events**:
- `CommentAdded`: New comment created (broadcast to all)
- `CommentUpdated`: Comment edited (broadcast to all)
- `CommentDeleted`: Comment removed (broadcast to all)
- `Comment ResolutionChanged`: Thread marked resolved/unresolved
- `MentionNotification`: Targeted notification to mentioned user

### 4. UI Components

#### 4.1 Context Menu Integration

Add to graph view and list view:

```razor
@* Right-click context menu for concepts and relationships *@
<div class="context-menu" @ref="_contextMenuRef" style="display: @(_showContextMenu ? "block" : "none")">
    <ul class="context-menu-list">
        <li @onclick="@(() => OpenCommentPanel())">
            <i class="bi bi-chat-dots"></i>
            Add Comment
            @if (_commentCount > 0)
            {
                <span class="badge bg-primary">@_commentCount</span>
            }
        </li>
        @if (_commentCount > 0)
        {
            <li @onclick="@(() => OpenCommentPanel())">
                <i class="bi bi-chat-text"></i>
                View Comments (@_commentCount)
            </li>
        }
        <!-- Other context menu items... -->
    </ul>
</div>
```

#### 4.2 Comment Thread Panel

Create `EntityCommentPanel.razor`:

```razor
@* Sliding panel from right side (like Figma) *@
<div class="comment-panel @(_isOpen ? "open" : "")">
    <div class="comment-panel-header">
        <h5>
            @EntityType: @EntityName
            @if (_comments.Any())
            {
                <span class="badge bg-secondary ms-2">@_comments.Count</span>
            }
        </h5>
        <button class="btn-close" @onclick="Close"></button>
    </div>

    <div class="comment-panel-body">
        @if (!_comments.Any())
        {
            <div class="empty-state">
                <i class="bi bi-chat-dots"></i>
                <p>No comments yet. Start a discussion!</p>
            </div>
        }
        else
        {
            @foreach (var comment in _comments.Where(c => c.ParentCommentId == null))
            {
                <CommentThread Comment="@comment" OnReply="HandleReply" OnResolve="HandleResolve" />
            }
        }
    </div>

    <div class="comment-panel-footer">
        <CommentEditor OnSubmit="HandleNewComment" Placeholder="Add a comment..." />
    </div>
</div>
```

#### 4.3 Comment Thread Component

Create `CommentThread.razor`:

```razor
@* Individual comment with nested replies *@
<div class="comment-thread @(Comment.IsResolved ? "resolved" : "")">
    <div class="comment-item">
        <div class="comment-avatar" style="background-color: @GetUserColor(Comment.User)">
            @if (!string.IsNullOrEmpty(Comment.User.ProfilePhotoUrl))
            {
                <img src="@Comment.User.ProfilePhotoUrl" alt="@Comment.User.UserName" />
            }
            else
            {
                <span>@GetInitials(Comment.User.UserName)</span>
            }
        </div>

        <div class="comment-content">
            <div class="comment-header">
                <strong>@Comment.User.UserName</strong>
                <span class="comment-time">@FormatTimestamp(Comment.CreatedAt)</span>
                @if (Comment.EditedAt.HasValue)
                {
                    <span class="comment-edited">(edited)</span>
                }
            </div>

            <div class="comment-text">
                @((MarkupString)ParseMarkdown(Comment.Text))
            </div>

            <div class="comment-actions">
                <button class="btn-link" @onclick="@(() => OnReply.InvokeAsync(Comment))">
                    <i class="bi bi-reply"></i> Reply
                </button>

                @if (CanResolve)
                {
                    <button class="btn-link" @onclick="@(() => OnResolve.InvokeAsync(Comment))">
                        <i class="bi bi-check-circle"></i>
                        @(Comment.IsResolved ? "Unresolve" : "Resolve")
                    </button>
                }

                @if (CanEdit)
                {
                    <button class="btn-link" @onclick="StartEdit">
                        <i class="bi bi-pencil"></i> Edit
                    </button>
                }

                @if (CanDelete)
                {
                    <button class="btn-link text-danger" @onclick="DeleteComment">
                        <i class="bi bi-trash"></i> Delete
                    </button>
                }
            </div>
        </div>
    </div>

    @* Nested replies *@
    @if (Comment.Replies.Any())
    {
        <div class="comment-replies">
            @foreach (var reply in Comment.Replies)
            {
                <CommentThread Comment="@reply" OnReply="OnReply" OnResolve="OnResolve" />
            }
        </div>
    }
</div>
```

#### 4.4 Comment Editor with @Mentions

Create `CommentEditor.razor`:

```razor
@* Rich text editor with @mention autocomplete *@
<div class="comment-editor">
    <textarea
        @bind="_text"
        @bind:event="oninput"
        @onkeyup="HandleKeyUp"
        placeholder="@Placeholder"
        class="form-control"
        rows="3">
    </textarea>

    @if (_showMentionDropdown)
    {
        <div class="mention-dropdown">
            @foreach (var user in _filteredUsers)
            {
                <div class="mention-option" @onclick="@(() => SelectMention(user))">
                    <div class="mention-avatar" style="background-color: @user.Color">
                        @GetInitials(user.UserName)
                    </div>
                    <div class="mention-info">
                        <strong>@user.UserName</strong>
                        <small>@user.Email</small>
                    </div>
                </div>
            }
        </div>
    }

    <div class="comment-editor-footer">
        <div class="comment-editor-help">
            <small>Markdown supported. Type <code>@</code> to mention users.</small>
        </div>
        <button
            class="btn btn-primary btn-sm"
            @onclick="Submit"
            disabled="@string.IsNullOrWhiteSpace(_text)">
            Comment
        </button>
    </div>
</div>

@code {
    private string _text = string.Empty;
    private bool _showMentionDropdown = false;
    private List<PresenceInfo> _filteredUsers = new();

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        // Detect '@' character and show mention dropdown
        if (_text.EndsWith("@"))
        {
            _showMentionDropdown = true;
            _filteredUsers = await GetOntologyUsers();
        }
        else if (_showMentionDropdown)
        {
            // Filter users based on text after '@'
            var mentionText = ExtractMentionText(_text);
            _filteredUsers = await SearchUsers(mentionText);
        }
    }

    private void SelectMention(PresenceInfo user)
    {
        // Replace '@searchtext' with '@username'
        _text = ReplaceMentionText(_text, user.UserName);
        _showMentionDropdown = false;
    }

    private async Task Submit()
    {
        await OnSubmit.InvokeAsync(_text);
        _text = string.Empty;
    }
}
```

### 5. Visual Design (Minimal Sci-Fi Aesthetic)

Following the application's established design system:

```css
/* Comment Panel - Sliding from right */
.comment-panel {
    position: fixed;
    top: 0;
    right: -400px;
    width: 400px;
    height: 100vh;
    background: var(--bg-primary);
    border-left: 1px solid var(--border-color);
    box-shadow: -4px 0 12px rgba(0, 0, 0, 0.1);
    transition: right var(--transition-normal) var(--transition-ease);
    z-index: 1000;
    display: flex;
    flex-direction: column;
}

.comment-panel.open {
    right: 0;
}

/* Comment Thread - Clean hierarchy */
.comment-thread {
    padding: var(--space-md);
    border-bottom: 1px solid var(--border-color);
}

.comment-thread.resolved {
    opacity: 0.6;
    background: var(--bg-secondary);
}

.comment-item {
    display: flex;
    gap: var(--space-md);
}

.comment-avatar {
    width: 32px;
    height: 32px;
    border-radius: var(--radius-full);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: white;
}

.comment-content {
    flex: 1;
    min-width: 0;
}

.comment-header {
    display: flex;
    align-items: center;
    gap: var(--space-sm);
    margin-bottom: var(--space-xs);
    font-size: 0.875rem;
}

.comment-time {
    color: var(--text-muted);
    font-size: 0.75rem;
}

.comment-text {
    color: var(--text-primary);
    margin-bottom: var(--space-sm);
    line-height: 1.5;
}

.comment-actions {
    display: flex;
    gap: var(--space-md);
    font-size: 0.75rem;
}

.comment-actions .btn-link {
    padding: 0;
    color: var(--text-secondary);
    text-decoration: none;
    transition: color var(--transition-fast) var(--transition-ease);
}

.comment-actions .btn-link:hover {
    color: var(--primary-color);
}

/* Nested replies - subtle indentation */
.comment-replies {
    margin-left: var(--space-3xl);
    padding-left: var(--space-md);
    border-left: 2px solid var(--border-color);
}

/* Mention dropdown - clean list */
.mention-dropdown {
    position: absolute;
    bottom: 100%;
    left: 0;
    right: 0;
    background: var(--bg-primary);
    border: 1px solid var(--border-color);
    border-radius: var(--radius-md);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    max-height: 200px;
    overflow-y: auto;
}

.mention-option {
    display: flex;
    align-items: center;
    gap: var(--space-sm);
    padding: var(--space-sm);
    cursor: pointer;
    transition: background-color var(--transition-fast) var(--transition-ease);
}

.mention-option:hover {
    background-color: var(--bg-secondary);
}

/* Comment badge on entities */
.entity-comment-badge {
    position: absolute;
    top: -8px;
    right: -8px;
    background: var(--primary-color);
    color: white;
    border-radius: var(--radius-full);
    padding: 2px 6px;
    font-size: 0.75rem;
    font-weight: 600;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}
```

---

## Implementation Phases

### Phase 1: Database & Core Services (Week 1)

1. **Database Migration**
   - Create `EntityComments`, `CommentMentions`, `EntityCommentCounts` tables
   - Add indexes: `(OntologyId, EntityType, EntityId)`, `(UserId)`, `(ParentCommentId)`
   - Test with seed data

2. **Service Layer**
   - Implement `EntityCommentService`
   - Add mention parsing logic
   - Implement denormalized count updates
   - Write unit tests

3. **Repository Layer**
   - Create `IEntityCommentRepository`
   - Implement CRUD operations
   - Add efficient query methods (e.g., `GetThreadWithReplies`)

### Phase 2: SignalR Integration (Week 1-2)

1. **Hub Methods**
   - Add `AddComment`, `UpdateComment`, `DeleteComment`, `ResolveCommentThread`
   - Permission checks using existing `OntologyPermissionService`
   - Real-time event broadcasting

2. **Client-Side Hub Connection**
   - Update JavaScript interop to listen for comment events
   - Handle real-time comment updates in UI

### Phase 3: UI Components (Week 2-3)

1. **Context Menu Integration**
   - Add right-click handler to graph nodes (Cytoscape.js)
   - Add right-click handler to list view rows
   - Show "Add Comment" option with badge

2. **Comment Panel Component**
   - Create sliding panel (similar to Figma)
   - Thread rendering with nesting
   - Load comments on panel open

3. **Comment Editor**
   - Markdown support
   - @mention autocomplete dropdown
   - Submit via Enter key (Shift+Enter for new line)

4. **Comment Thread Display**
   - Avatar rendering
   - Timestamp formatting ("2 hours ago")
   - Resolve/unresolve button
   - Edit/delete for own comments

### Phase 4: @Mention Notifications (Week 3)

1. **Mention Parsing**
   - Regex to extract @username or @email from comment text
   - Validate mentioned users exist and have access to ontology

2. **Targeted Notifications**
   - SignalR: Send `MentionNotification` to specific user
   - In-app notification badge/panel
   - Optional: Email notifications for offline users

3. **Mention Management**
   - Track viewed/unviewed mentions
   - "View all mentions" page

### Phase 5: Polish & Testing (Week 4)

1. **Performance Optimization**
   - Lazy load comment threads (pagination)
   - Cache comment counts
   - Optimize N+1 queries

2. **Accessibility**
   - Keyboard navigation in comment panel
   - Screen reader support
   - ARIA labels

3. **Testing**
   - Unit tests for service layer
   - Integration tests for SignalR hub
   - End-to-end tests for comment workflows
   - Load testing with many concurrent comments

---

## Database Schema

```sql
-- EntityComments table
CREATE TABLE EntityComments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OntologyId INTEGER NOT NULL,
    EntityType TEXT NOT NULL CHECK(EntityType IN ('Concept', 'Relationship', 'Individual')),
    EntityId INTEGER NOT NULL,
    UserId TEXT NOT NULL,
    Text TEXT NOT NULL,
    ParentCommentId INTEGER NULL,
    IsResolved INTEGER NOT NULL DEFAULT 0,
    ResolvedByUserId TEXT NULL,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    EditedAt TEXT NULL,
    FOREIGN KEY (OntologyId) REFERENCES Ontologies(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    FOREIGN KEY (ParentCommentId) REFERENCES EntityComments(Id) ON DELETE CASCADE,
    FOREIGN KEY (ResolvedByUserId) REFERENCES AspNetUsers(Id)
);

-- Indexes for performance
CREATE INDEX IX_EntityComments_Entity ON EntityComments(OntologyId, EntityType, EntityId);
CREATE INDEX IX_EntityComments_User ON EntityComments(UserId);
CREATE INDEX IX_EntityComments_Parent ON EntityComments(ParentCommentId);
CREATE INDEX IX_EntityComments_Created ON EntityComments(CreatedAt DESC);

-- CommentMentions table
CREATE TABLE CommentMentions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CommentId INTEGER NOT NULL,
    MentionedUserId TEXT NOT NULL,
    HasViewed INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CommentId) REFERENCES EntityComments(Id) ON DELETE CASCADE,
    FOREIGN KEY (MentionedUserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

CREATE INDEX IX_CommentMentions_User ON CommentMentions(MentionedUserId, HasViewed);
CREATE INDEX IX_CommentMentions_Comment ON CommentMentions(CommentId);

-- EntityCommentCounts table (denormalized for performance)
CREATE TABLE EntityCommentCounts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OntologyId INTEGER NOT NULL,
    EntityType TEXT NOT NULL,
    EntityId INTEGER NOT NULL,
    TotalComments INTEGER NOT NULL DEFAULT 0,
    UnresolvedThreads INTEGER NOT NULL DEFAULT 0,
    LastCommentAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(OntologyId, EntityType, EntityId)
);

CREATE INDEX IX_EntityCommentCounts_Entity ON EntityCommentCounts(OntologyId, EntityType, EntityId);
```

---

## API Endpoints (Optional REST Fallback)

While SignalR is primary, provide REST endpoints for:

```csharp
// GET /api/ontologies/{id}/comments/{entityType}/{entityId}
// Returns all comments for an entity

// POST /api/ontologies/{id}/comments
// Body: { entityType, entityId, text, parentCommentId }
// Creates a new comment

// PUT /api/comments/{id}
// Body: { text }
// Updates comment text

// DELETE /api/comments/{id}
// Deletes a comment

// POST /api/comments/{id}/resolve
// Marks comment thread as resolved

// GET /api/mentions
// Returns user's @mentions (with pagination)
```

---

## Testing Strategy

### Unit Tests

```csharp
public class EntityCommentServiceTests
{
    [Fact]
    public async Task AddComment_WithValidData_CreatesComment()
    {
        // Arrange
        var service = CreateService();

        // Act
        var comment = await service.AddCommentAsync(
            ontologyId: 1,
            entityType: "Concept",
            entityId: 42,
            userId: "user1",
            text: "This concept needs clarification @user2"
        );

        // Assert
        Assert.NotNull(comment);
        Assert.Equal("This concept needs clarification @user2", comment.Text);
        Assert.Single(comment.Mentions); // Should parse @user2
    }

    [Fact]
    public async Task AddComment_UpdatesCommentCount()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.AddCommentAsync(1, "Concept", 42, "user1", "Test");
        var count = await service.GetCommentCountAsync(1, "Concept", 42);

        // Assert
        Assert.Equal(1, count.TotalComments);
    }

    [Fact]
    public async Task AddReply_CreatesThreadedComment()
    {
        // Arrange
        var service = CreateService();
        var parent = await service.AddCommentAsync(1, "Concept", 42, "user1", "Question");

        // Act
        var reply = await service.AddCommentAsync(1, "Concept", 42, "user2", "Answer", parent.Id);

        // Assert
        Assert.Equal(parent.Id, reply.ParentCommentId);
    }
}
```

### Integration Tests

```csharp
public class OntologyHubCommentTests
{
    [Fact]
    public async Task AddComment_BroadcastsToAllUsers()
    {
        // Arrange
        var hub = CreateHub();
        var mockClients = new Mock<IHubCallerClients>();

        // Act
        await hub.AddComment(ontologyId: 1, entityType: "Concept", entityId: 42, text: "Test", parentCommentId: null);

        // Assert
        mockClients.Verify(c => c.Group("ontology_1").SendAsync("CommentAdded", It.IsAny<EntityComment>()), Times.Once);
    }
}
```

---

## Performance Considerations

1. **Comment Count Caching**
   - Use `EntityCommentCounts` table for badge display
   - Update counts in same transaction as comment CRUD
   - Rebuild counts nightly via background job

2. **Lazy Loading**
   - Load first 10 comments initially
   - "Load more" pagination for large threads
   - Load replies on demand (collapsed by default)

3. **Real-Time Optimization**
   - Only broadcast to users in same ontology (SignalR groups)
   - Debounce typing indicators for @mention search
   - Limit mention search to users with ontology access

4. **Database Indexes**
   - Composite index on `(OntologyId, EntityType, EntityId)` for fast lookups
   - Index on `CreatedAt DESC` for sorting
   - Index on `ParentCommentId` for thread queries

---

## Security Considerations

1. **Permission Checks**
   - Must have `CanView` permission to read comments
   - Must have `CanEdit` permission to add comments (configurable)
   - Can only edit/delete own comments (unless admin)
   - Ontology owner can delete any comment

2. **Input Validation**
   - Sanitize HTML in Markdown rendering (prevent XSS)
   - Limit comment length (e.g., 10,000 characters)
   - Rate limit comment creation (prevent spam)
   - Validate mentioned users exist and have access

3. **Mention Privacy**
   - Only allow @mentions for users with ontology access
   - Don't leak user email addresses in autocomplete

---

## Future Enhancements (Out of Scope for V1)

1. **Rich Reactions**
   - 👍 👎 ❤️ emojis on comments (like Slack)

2. **Comment Search**
   - Full-text search across all comments in ontology

3. **Comment Export**
   - Include comments in TTL export as annotations
   - PDF export with discussion history

4. **Comment Templates**
   - Pre-defined comment types: "Question", "Suggestion", "Issue"

5. **@Channel Mentions**
   - `@everyone` to notify all collaborators

6. **Comment Attachments**
   - Upload images/files to comments

7. **Comment Analytics**
   - Most commented concepts
   - Active discussion participants
   - Resolution time metrics

---

## Success Metrics

Track these after launch:

1. **Adoption Rate**
   - % of ontologies with comments
   - Average comments per ontology
   - Active commenters per ontology

2. **Collaboration Metrics**
   - @mention usage frequency
   - Reply rate (how many comments get replies)
   - Time to resolution (for resolved threads)

3. **Performance Metrics**
   - Comment load time (< 200ms)
   - SignalR latency for real-time updates (< 100ms)
   - Database query performance (< 50ms)

---

## Conclusion

This implementation plan provides a comprehensive, production-ready commenting system that:

✅ Leverages existing SignalR infrastructure
✅ Follows established code patterns (like `MergeRequestComment`)
✅ Provides Figma/Google Docs-style UX
✅ Supports @mentions with real-time notifications
✅ Maintains the minimal sci-fi design aesthetic
✅ Scales efficiently with denormalized counts
✅ Includes comprehensive testing strategy

**Next Steps**: Review this plan, clarify any ambiguities, and proceed to Phase 1 implementation.
