# In-Context Commenting - Architecture Context

**Last Updated**: November 9, 2025
**Status**: Planning Phase

---

## Purpose

This document captures key architectural context, decisions, and rationale for the In-Context Commenting feature. It serves as a reference for developers implementing the feature and explains **why** certain technical choices were made.

---

## Existing Infrastructure Leverage

### 1. SignalR Hub (OntologyHub.cs)

**What exists**: Fully functional real-time hub with:
- User presence tracking
- Permission-aware group joining (`ontology_{id}` groups)
- Broadcast methods for concept/relationship changes
- Heartbeat mechanism (30-second intervals)

**How we'll use it**:
```csharp
// New methods to add to OntologyHub.cs
public async Task AddComment(int ontologyId, string entityType, int entityId, string text, int? parentCommentId)
{
    // 1. Reuse existing permission check pattern
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var hasPermission = await _permissionService.CanEditAsync(ontologyId, userId);

    // 2. Create comment via service
    var comment = await _commentService.AddCommentAsync(...);

    // 3. Broadcast to existing group
    var groupName = GetOntologyGroupName(ontologyId); // Already exists!
    await Clients.Group(groupName).SendAsync("CommentAdded", comment);

    // 4. Send targeted mention notifications
    foreach (var mention in comment.Mentions)
    {
        await Clients.User(mention.MentionedUserId).SendAsync("MentionNotification", comment);
    }
}
```

**Why this works**: The hub already handles authentication, group membership, and permission checks. We're just adding new message types to the existing infrastructure.

### 2. Comment Model Pattern (MergeRequestComment.cs)

**What exists**: Well-designed comment model for merge requests:
```csharp
public class MergeRequestComment
{
    public int Id { get; set; }
    public int MergeRequestId { get; set; }
    public string UserId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsSystemComment { get; set; }
    public int? MergeRequestChangeId { get; set; } // Optional reference to specific change
}
```

**How we'll adapt it**:
```csharp
public class EntityComment
{
    // Similar structure but entity-agnostic
    public int Id { get; set; }
    public int OntologyId { get; set; } // Scope to ontology
    public string EntityType { get; set; } // "Concept", "Relationship", "Individual"
    public int EntityId { get; set; } // Polymorphic reference
    public string UserId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public int? ParentCommentId { get; set; } // NEW: For threading
    public bool IsResolved { get; set; } // NEW: For discussion resolution
}
```

**Why polymorphic reference**: Instead of foreign keys to `Concepts`, `Relationships`, and `Individuals`, we use `EntityType` + `EntityId`. This:
- Avoids 3 separate comment tables
- Allows future extensibility (e.g., comments on properties)
- Simplifies queries when loading all comments for an ontology

**Trade-off**: No database-level referential integrity. We accept this because:
1. We control entity deletion (can cascade manually)
2. Simplifies schema
3. Common pattern in EF Core for polymorphic associations

### 3. Permission System (OntologyPermissionService.cs)

**What exists**:
```csharp
public interface IOntologyPermissionService
{
    Task<bool> CanViewAsync(int ontologyId, string? userId);
    Task<bool> CanEditAsync(int ontologyId, string? userId);
    Task<bool> CanManageAsync(int ontologyId, string? userId);
    // ... other methods
}
```

**How we'll use it**:
```csharp
public async Task<bool> CanAddCommentAsync(int ontologyId, string userId)
{
    // Commenting requires at least view access (can be configurable)
    // For now, anyone who can view can comment
    return await _permissionService.CanViewAsync(ontologyId, userId);
}

public async Task<bool> CanEditCommentAsync(int commentId, string userId)
{
    var comment = await _repository.GetByIdAsync(commentId);
    if (comment == null) return false;

    // Users can edit their own comments
    if (comment.UserId == userId) return true;

    // Ontology managers can edit any comment
    return await _permissionService.CanManageAsync(comment.OntologyId, userId);
}
```

**Design Decision**: Comment permissions follow ontology permissions. We don't create a separate comment permission system.

---

## Key Architectural Decisions

### Decision 1: Polymorphic Entity Reference

**Problem**: How to reference concepts, relationships, and individuals without 3 separate comment tables?

**Options Considered**:
1. Separate tables: `ConceptComments`, `RelationshipComments`, `IndividualComments`
2. Polymorphic with `EntityType` + `EntityId`
3. Single foreign key to abstract `OntologyEntity` table

**Chosen**: Option 2 (Polymorphic)

**Rationale**:
- ✅ Single table for all comments (simplifies queries)
- ✅ Extensible to future entity types
- ✅ Common pattern in EF Core (`[Owned]` types, discriminators)
- ⚠️ No FK integrity (acceptable trade-off)
- ✅ Easy to query all comments for an ontology

**Implementation Note**:
```csharp
// Querying comments for a specific concept
var comments = await _context.EntityComments
    .Where(c => c.OntologyId == ontologyId
        && c.EntityType == "Concept"
        && c.EntityId == conceptId)
    .Include(c => c.Replies)
    .ToListAsync();

// Querying all comments in an ontology
var allComments = await _context.EntityComments
    .Where(c => c.OntologyId == ontologyId)
    .ToListAsync();
```

### Decision 2: Threaded Comments via ParentCommentId

**Problem**: How to structure replies and nested discussions?

**Options Considered**:
1. Flat list (all comments at same level)
2. Two-level (comment + replies only)
3. Infinite nesting via `ParentCommentId`

**Chosen**: Option 3 (Infinite nesting)

**Rationale**:
- ✅ Matches user expectations (Figma, Slack, Reddit)
- ✅ Flexible for complex discussions
- ✅ Simple self-referential FK
- ⚠️ Potential deep nesting (mitigate with UI recursion limits)

**Implementation Note**:
```csharp
// Loading full thread with replies
var thread = await _context.EntityComments
    .Where(c => c.Id == commentId)
    .Include(c => c.Replies)
        .ThenInclude(r => r.Replies) // Nested includes
    .FirstOrDefaultAsync();

// UI can limit display depth to avoid UX issues
@code {
    private const int MAX_NEST_LEVEL = 5;

    private void RenderComment(EntityComment comment, int level = 0)
    {
        if (level >= MAX_NEST_LEVEL) return; // Stop deep nesting
        // Render comment and replies
    }
}
```

### Decision 3: Denormalized Comment Counts

**Problem**: How to display comment count badges without N+1 queries?

**Options Considered**:
1. Count on-demand (`COUNT(*)` for each entity)
2. Cache in memory (Redis/IMemoryCache)
3. Denormalized table (`EntityCommentCounts`)

**Chosen**: Option 3 (Denormalized table)

**Rationale**:
- ✅ Fastest reads (pre-computed counts)
- ✅ No cache invalidation complexity
- ✅ Survives restarts (unlike in-memory cache)
- ⚠️ Requires updates on comment CRUD (acceptable overhead)

**Implementation Pattern**:
```csharp
public async Task AddCommentAsync(...)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 1. Create comment
        var comment = new EntityComment { ... };
        _context.EntityComments.Add(comment);
        await _context.SaveChangesAsync();

        // 2. Update denormalized count (in same transaction)
        var count = await _context.EntityCommentCounts
            .FirstOrDefaultAsync(c => c.OntologyId == ontologyId
                && c.EntityType == entityType
                && c.EntityId == entityId);

        if (count == null)
        {
            count = new EntityCommentCount
            {
                OntologyId = ontologyId,
                EntityType = entityType,
                EntityId = entityId,
                TotalComments = 1,
                UnresolvedThreads = comment.ParentCommentId == null && !comment.IsResolved ? 1 : 0,
                LastCommentAt = DateTime.UtcNow
            };
            _context.EntityCommentCounts.Add(count);
        }
        else
        {
            count.TotalComments++;
            if (comment.ParentCommentId == null && !comment.IsResolved)
                count.UnresolvedThreads++;
            count.LastCommentAt = DateTime.UtcNow;
            count.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return comment;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Nightly Reconciliation Job** (optional, for data integrity):
```csharp
// Background job to rebuild counts (runs at 3 AM daily)
public async Task ReconcileCommentCounts()
{
    var counts = await _context.EntityComments
        .GroupBy(c => new { c.OntologyId, c.EntityType, c.EntityId })
        .Select(g => new
        {
            g.Key.OntologyId,
            g.Key.EntityType,
            g.Key.EntityId,
            TotalComments = g.Count(),
            UnresolvedThreads = g.Count(c => c.ParentCommentId == null && !c.IsResolved),
            LastCommentAt = g.Max(c => c.CreatedAt)
        })
        .ToListAsync();

    // Update EntityCommentCounts table...
}
```

### Decision 4: @Mention Parsing Approach

**Problem**: How to extract and track @mentions from comment text?

**Options Considered**:
1. Parse on display (extract @mentions when rendering)
2. Parse on creation (extract @mentions when comment is created)
3. Markdown-style links (`[@username]`)

**Chosen**: Option 2 (Parse on creation)

**Rationale**:
- ✅ Enables targeted notifications immediately
- ✅ Allows querying "my mentions" efficiently
- ✅ Supports offline email notifications
- ✅ Better performance (parse once, not on every render)

**Implementation Pattern**:
```csharp
public async Task<EntityComment> AddCommentAsync(int ontologyId, string entityType,
    int entityId, string userId, string text, int? parentCommentId = null)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    // 1. Create comment
    var comment = new EntityComment { OntologyId = ontologyId, Text = text, ... };
    _context.EntityComments.Add(comment);
    await _context.SaveChangesAsync(); // Get comment.Id

    // 2. Parse @mentions
    var mentionedUsers = await ExtractMentionsAsync(text, ontologyId);

    // 3. Create CommentMention records
    foreach (var mentionedUser in mentionedUsers)
    {
        var mention = new CommentMention
        {
            CommentId = comment.Id,
            MentionedUserId = mentionedUser.Id,
            HasViewed = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.CommentMentions.Add(mention);
    }

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();

    return comment;
}

private async Task<List<ApplicationUser>> ExtractMentionsAsync(string text, int ontologyId)
{
    // Regex to find @mentions: @username or @email
    var regex = new Regex(@"@([\w.-]+@[\w.-]+|[\w.]+)");
    var matches = regex.Matches(text);

    var mentionedUsers = new List<ApplicationUser>();

    foreach (Match match in matches)
    {
        var identifier = match.Groups[1].Value;

        // Try to find user by username or email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == identifier || u.Email == identifier);

        if (user == null) continue;

        // Verify user has access to this ontology
        var hasAccess = await _permissionService.CanViewAsync(ontologyId, user.Id);
        if (hasAccess)
        {
            mentionedUsers.Add(user);
        }
    }

    return mentionedUsers.Distinct().ToList();
}
```

### Decision 5: Real-Time Broadcasting Strategy

**Problem**: How to notify users of new comments without overloading SignalR?

**Options Considered**:
1. Broadcast every comment to all connected users
2. Only broadcast to users in the same ontology
3. Only broadcast to users viewing the same entity

**Chosen**: Option 2 (Ontology-scoped broadcasting)

**Rationale**:
- ✅ Already implemented via SignalR groups (`ontology_{id}`)
- ✅ Users see all activity in their ontology (awareness)
- ✅ Manageable message volume (ontologies are scoped)
- ⚠️ Entity-scoped would reduce messages but adds complexity

**Implementation**:
```csharp
// In OntologyHub.cs
public async Task AddComment(...)
{
    // ... create comment ...

    // Broadcast to all users in this ontology's SignalR group
    var groupName = GetOntologyGroupName(ontologyId); // "ontology_42"
    await Clients.Group(groupName).SendAsync("CommentAdded", new
    {
        Comment = comment,
        EntityType = entityType,
        EntityId = entityId
    });
}
```

**Client-Side Handling** (in Blazor component):
```csharp
protected override async Task OnInitializedAsync()
{
    // Subscribe to SignalR events
    await HubConnection.On<dynamic>("CommentAdded", async (data) =>
    {
        // Only update UI if user is viewing this entity
        if (data.EntityType == CurrentEntityType && data.EntityId == CurrentEntityId)
        {
            _comments.Add(data.Comment);
            StateHasChanged();
        }

        // Always update comment count badge
        await RefreshCommentCountBadge(data.EntityType, data.EntityId);
    });
}
```

---

## UI/UX Design Decisions

### Decision 6: Context Menu vs. Button

**Problem**: How should users access the comment feature?

**Options Considered**:
1. Right-click context menu (like Figma)
2. Dedicated "Comment" button on each entity
3. Floating action button (FAB)
4. All of the above

**Chosen**: Option 4 (Multiple entry points)

**Rationale**:
- ✅ Right-click for power users (familiar pattern)
- ✅ Button for discoverability (new users)
- ✅ FAB for mobile (touch-friendly)
- ✅ Flexibility in different view modes

**Implementation**:
```razor
@* Graph View - Right-click on Cytoscape node *@
<script>
cy.on('cxttap', 'node', function(event) {
    var node = event.target;
    var entityType = node.data('type'); // "Concept"
    var entityId = node.data('id');
    DotNet.invokeMethodAsync('Eidos', 'ShowCommentContextMenu', entityType, entityId, event.position);
});
</script>

@* List View - Button in row *@
<tr>
    <td>@concept.Name</td>
    <td>
        <button class="btn btn-sm btn-outline-secondary" @onclick="@(() => OpenComments("Concept", concept.Id))">
            <i class="bi bi-chat-dots"></i>
            @if (_commentCounts.TryGetValue(concept.Id, out var count) && count > 0)
            {
                <span class="badge bg-primary">@count</span>
            }
        </button>
    </td>
</tr>

@* Mobile - Floating action button *@
<div class="fab-container">
    <button class="fab" @onclick="OpenCommentPanel">
        <i class="bi bi-chat-dots"></i>
    </button>
</div>
```

### Decision 7: Sliding Panel vs. Modal

**Problem**: How to display the comment thread UI?

**Options Considered**:
1. Modal dialog (blocks background)
2. Sliding panel from right (like Figma)
3. Inline expansion (like Google Docs)

**Chosen**: Option 2 (Sliding panel)

**Rationale**:
- ✅ Maintains graph/list context (can see entity while commenting)
- ✅ Familiar pattern (Figma, Notion, GitHub)
- ✅ Can adjust width (responsive)
- ✅ Minimal aesthetic (clean slide transition)

**CSS Implementation**:
```css
.comment-panel {
    position: fixed;
    top: 0;
    right: -400px; /* Hidden by default */
    width: 400px;
    height: 100vh;
    background: var(--bg-primary);
    border-left: 1px solid var(--border-color);
    box-shadow: -4px 0 12px rgba(0, 0, 0, 0.1);
    transition: right var(--transition-normal) var(--transition-ease); /* 250ms */
    z-index: 1000;
}

.comment-panel.open {
    right: 0; /* Slide in */
}

/* Mobile: Full-width overlay */
@media (max-width: 768px) {
    .comment-panel {
        right: -100%;
        width: 100%;
    }
}
```

### Decision 8: Comment Resolution UI

**Problem**: How to mark comment threads as "resolved" (for issues/questions)?

**Options Considered**:
1. Checkbox on each comment
2. "Resolve" button on parent comment only
3. Automatic resolution when marked (no confirmation)

**Chosen**: Option 2 + Option 3

**Rationale**:
- ✅ Only parent comments can be resolved (not individual replies)
- ✅ Immediate resolution (no confirmation dialog)
- ✅ Clear visual distinction (opacity change, checkmark)

**UI Pattern**:
```razor
<div class="comment-thread @(Comment.IsResolved ? "resolved" : "")">
    <div class="comment-item">
        @* Comment content *@

        <div class="comment-actions">
            @if (Comment.ParentCommentId == null) @* Only show on parent comments *@
            {
                <button class="btn-link" @onclick="ToggleResolved">
                    <i class="bi @(Comment.IsResolved ? "bi-arrow-counterclockwise" : "bi-check-circle")"></i>
                    @(Comment.IsResolved ? "Unresolve" : "Resolve")
                </button>
            }
        </div>
    </div>

    @* Replies remain visible even when resolved *@
    <div class="comment-replies">
        @foreach (var reply in Comment.Replies)
        {
            <CommentThread Comment="@reply" />
        }
    </div>
</div>
```

**CSS for Resolved State**:
```css
.comment-thread.resolved {
    opacity: 0.6;
    background: var(--bg-secondary);
}

.comment-thread.resolved .comment-text {
    text-decoration: line-through;
}
```

---

## Performance Optimization Strategies

### Strategy 1: Lazy Loading with Pagination

**Problem**: Large comment threads can slow down initial load.

**Solution**:
```csharp
public async Task<List<EntityComment>> GetCommentsForEntityAsync(
    int ontologyId, string entityType, int entityId,
    int page = 1, int pageSize = 10)
{
    // Load parent comments only (replies loaded on-demand)
    return await _context.EntityComments
        .Where(c => c.OntologyId == ontologyId
            && c.EntityType == entityType
            && c.EntityId == entityId
            && c.ParentCommentId == null) // Top-level only
        .OrderByDescending(c => c.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Include(c => c.User) // Eager load user for display
        .AsNoTracking() // Read-only query
        .ToListAsync();
}

// Load replies on-demand when user clicks "Show replies"
public async Task<List<EntityComment>> GetRepliesAsync(int parentCommentId)
{
    return await _context.EntityComments
        .Where(c => c.ParentCommentId == parentCommentId)
        .OrderBy(c => c.CreatedAt)
        .Include(c => c.User)
        .AsNoTracking()
        .ToListAsync();
}
```

### Strategy 2: Batch Comment Count Queries

**Problem**: Loading comment counts for all entities in a list/graph view requires many queries.

**Solution**:
```csharp
public async Task<Dictionary<int, EntityCommentCount>> GetCommentCountsForEntitiesAsync(
    int ontologyId, string entityType, List<int> entityIds)
{
    // Single query for all entities
    return await _context.EntityCommentCounts
        .Where(c => c.OntologyId == ontologyId
            && c.EntityType == entityType
            && entityIds.Contains(c.EntityId))
        .AsNoTracking()
        .ToDictionaryAsync(c => c.EntityId);
}
```

**Usage in Blazor**:
```csharp
protected override async Task OnInitializedAsync()
{
    // Load all concepts
    _concepts = await _conceptService.GetConceptsAsync(OntologyId);

    // Batch-load comment counts (single query)
    var conceptIds = _concepts.Select(c => c.Id).ToList();
    _commentCounts = await _commentService.GetCommentCountsForEntitiesAsync(
        OntologyId, "Concept", conceptIds);
}
```

### Strategy 3: SignalR Message Batching

**Problem**: Rapid comment creation (e.g., copy/paste 10 comments) floods SignalR.

**Solution**:
```csharp
// Debounce SignalR broadcasts on client side
private DateTime _lastBroadcast = DateTime.MinValue;
private const int BROADCAST_DELAY_MS = 500;

public async Task AddComment(...)
{
    // Create comment in database
    var comment = await _commentService.AddCommentAsync(...);

    // Debounce broadcasts
    var now = DateTime.UtcNow;
    if ((now - _lastBroadcast).TotalMilliseconds > BROADCAST_DELAY_MS)
    {
        await Clients.Group(groupName).SendAsync("CommentAdded", comment);
        _lastBroadcast = now;
    }
    else
    {
        // Queue for batched send
        _pendingComments.Add(comment);
    }
}
```

---

## Security Considerations

### Consideration 1: XSS Prevention in Markdown

**Risk**: Users can inject malicious HTML via Markdown comments.

**Mitigation**:
```csharp
// Use a sanitized Markdown parser
public string ParseMarkdown(string text)
{
    var pipeline = new MarkdownPipelineBuilder()
        .DisableHtml() // Disable raw HTML
        .UseAdvancedExtensions()
        .Build();

    var html = Markdown.ToHtml(text, pipeline);
    return html; // Safe to render
}
```

### Consideration 2: Rate Limiting

**Risk**: Users spam comments to abuse storage/notifications.

**Mitigation**:
```csharp
// Simple in-memory rate limiter (per-user)
private static readonly ConcurrentDictionary<string, (int Count, DateTime Window)> _rateLimits = new();

public async Task<bool> CanAddCommentAsync(string userId)
{
    const int MAX_COMMENTS_PER_MINUTE = 10;
    var now = DateTime.UtcNow;

    var limit = _rateLimits.GetOrAdd(userId, _ => (0, now));

    // Reset window if 1 minute has passed
    if ((now - limit.Window).TotalMinutes >= 1)
    {
        _rateLimits[userId] = (1, now);
        return true;
    }

    // Check if under limit
    if (limit.Count < MAX_COMMENTS_PER_MINUTE)
    {
        _rateLimits[userId] = (limit.Count + 1, limit.Window);
        return true;
    }

    return false; // Rate limit exceeded
}
```

### Consideration 3: Mention Privacy

**Risk**: @mentions leak user information to unauthorized users.

**Mitigation**:
```csharp
// Only allow @mentions for users with ontology access
private async Task<List<ApplicationUser>> ExtractMentionsAsync(string text, int ontologyId)
{
    // ... regex parsing ...

    foreach (Match match in matches)
    {
        var user = await _context.Users.FirstOrDefaultAsync(/* ... */);
        if (user == null) continue;

        // CRITICAL: Verify user has access to ontology
        var hasAccess = await _permissionService.CanViewAsync(ontologyId, user.Id);
        if (!hasAccess) continue; // Skip unauthorized users

        mentionedUsers.Add(user);
    }

    return mentionedUsers;
}
```

---

## Testing Strategy

### Unit Test Example

```csharp
[Fact]
public async Task AddComment_ParsesMentionsCorrectly()
{
    // Arrange
    var service = CreateServiceWithMockUsers(new[]
    {
        new ApplicationUser { Id = "user1", UserName = "alice", Email = "alice@example.com" },
        new ApplicationUser { Id = "user2", UserName = "bob", Email = "bob@example.com" }
    });

    // Act
    var comment = await service.AddCommentAsync(
        ontologyId: 1,
        entityType: "Concept",
        entityId: 42,
        userId: "user1",
        text: "Hey @alice and @bob@example.com, check this out!"
    );

    // Assert
    Assert.Equal(2, comment.Mentions.Count);
    Assert.Contains(comment.Mentions, m => m.MentionedUserId == "user1");
    Assert.Contains(comment.Mentions, m => m.MentionedUserId == "user2");
}
```

---

## Future Extensibility

### Planned Enhancements (Post-V1)

1. **Rich Text Editor**: Replace simple textarea with TinyMCE/Quill for formatting
2. **Comment Reactions**: 👍 👎 ❤️ emoji reactions (like Slack)
3. **Comment Search**: Full-text search across all comments
4. **Comment Attachments**: Upload images/files to comments
5. **Comment Templates**: Pre-defined comment types ("Question", "Suggestion", "Issue")
6. **@Channel Mentions**: `@everyone` to notify all collaborators

### Extension Points

The architecture supports these extensions without major refactoring:

```csharp
// Extension point: Custom comment types
public class EntityComment
{
    // ... existing fields ...

    [StringLength(50)]
    public string? CommentType { get; set; } // "Question", "Suggestion", "Issue", null (general)

    public string? Metadata { get; set; } // JSON field for type-specific data
}

// Extension point: Attachments
public class CommentAttachment
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public string FileName { get; set; }
    public string FileUrl { get; set; } // Blob storage URL
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; }
}

// Extension point: Reactions
public class CommentReaction
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public string UserId { get; set; }
    public string Emoji { get; set; } // "👍", "❤️", etc.
    public DateTime CreatedAt { get; set; }
}
```

---

## Conclusion

This architecture document provides the **why** behind technical decisions. Key takeaways:

1. **Leverage existing infrastructure** (SignalR, permissions, comment patterns)
2. **Polymorphic design** for flexibility and simplicity
3. **Denormalized counts** for performance
4. **Security-first** approach (sanitize, rate limit, permission check)
5. **Minimal aesthetic** aligned with application design system

**Next**: Review with team, address concerns, proceed to implementation.
