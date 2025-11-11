# In-Context Commenting - Implementation Task Checklist

**Last Updated**: November 9, 2025
**Status**: Planning Phase
**Total Estimated Time**: 3-4 weeks

---

## Phase 1: Database & Core Services (Week 1)

### 1.1 Database Schema

- [ ] Create `EntityComments` table migration
  - [ ] Add columns: Id, OntologyId, EntityType, EntityId, UserId, Text, ParentCommentId, IsResolved, ResolvedByUserId, CreatedAt, EditedAt
  - [ ] Add foreign keys: OntologyId, UserId, ParentCommentId, ResolvedByUserId
  - [ ] Add CHECK constraint on EntityType (`'Concept'`, `'Relationship'`, `'Individual'`)

- [ ] Create indexes for `EntityComments`
  - [ ] Composite index: `(OntologyId, EntityType, EntityId)`
  - [ ] Index on `UserId`
  - [ ] Index on `ParentCommentId`
  - [ ] Index on `CreatedAt DESC`

- [ ] Create `CommentMentions` table migration
  - [ ] Add columns: Id, CommentId, MentionedUserId, HasViewed, CreatedAt
  - [ ] Add foreign keys: CommentId, MentionedUserId

- [ ] Create indexes for `CommentMentions`
  - [ ] Composite index: `(MentionedUserId, HasViewed)`
  - [ ] Index on `CommentId`

- [ ] Create `EntityCommentCounts` table migration
  - [ ] Add columns: Id, OntologyId, EntityType, EntityId, TotalComments, UnresolvedThreads, LastCommentAt, UpdatedAt
  - [ ] Add UNIQUE constraint: `(OntologyId, EntityType, EntityId)`

- [ ] Run migration: `dotnet ef migrations add AddEntityComments`
- [ ] Test migration: `dotnet ef database update`
- [ ] Seed test data for development

**Files to Create/Modify**:
- `/Migrations/[timestamp]_AddEntityComments.cs`
- `/Models/EntityComment.cs`
- `/Models/CommentMention.cs`
- `/Models/EntityCommentCount.cs`
- `/Data/OntologyDbContext.cs` (add DbSets)

**Acceptance Criteria**:
- ✅ All tables created successfully
- ✅ Foreign keys enforce referential integrity
- ✅ Indexes improve query performance (verify with EXPLAIN)
- ✅ Seed data populates correctly

---

### 1.2 Model Classes

- [ ] Create `EntityComment` model class
  - [ ] Add all properties
  - [ ] Add navigation properties (User, ParentComment, Replies, Mentions, Ontology)
  - [ ] Add data annotations ([Required], [StringLength])

- [ ] Create `CommentMention` model class
  - [ ] Add all properties
  - [ ] Add navigation properties (Comment, MentionedUser)

- [ ] Create `EntityCommentCount` model class
  - [ ] Add all properties
  - [ ] Add navigation property (Ontology)

- [ ] Update `OntologyDbContext`
  - [ ] Add `DbSet<EntityComment> EntityComments`
  - [ ] Add `DbSet<CommentMention> CommentMentions`
  - [ ] Add `DbSet<EntityCommentCount> EntityCommentCounts`
  - [ ] Configure relationships in `OnModelCreating`

**Files to Create**:
- `/Models/EntityComment.cs`
- `/Models/CommentMention.cs`
- `/Models/EntityCommentCount.cs`

**Files to Modify**:
- `/Data/OntologyDbContext.cs`

**Acceptance Criteria**:
- ✅ Models compile without errors
- ✅ Navigation properties work bidirectionally
- ✅ EF Core correctly maps relationships

---

### 1.3 Repository Layer

- [ ] Create `IEntityCommentRepository` interface
  - [ ] Define CRUD methods
  - [ ] Define query methods (GetByEntityAsync, GetThreadAsync, GetMentionsAsync)

- [ ] Implement `EntityCommentRepository`
  - [ ] Implement CRUD operations
  - [ ] Implement `GetCommentsForEntityAsync(ontologyId, entityType, entityId, page, pageSize)`
  - [ ] Implement `GetCommentThreadAsync(commentId)` with recursive includes
  - [ ] Implement `GetRepliesAsync(parentCommentId)`
  - [ ] Implement `GetUserMentionsAsync(userId, ontologyId, unreadOnly)`
  - [ ] Implement `GetCommentCountAsync(ontologyId, entityType, entityId)`
  - [ ] Implement `GetCommentCountsForEntitiesAsync(ontologyId, entityType, entityIds)` (batch query)

- [ ] Write repository unit tests
  - [ ] Test CRUD operations
  - [ ] Test pagination
  - [ ] Test thread loading with nested replies
  - [ ] Test mention queries
  - [ ] Test count queries

**Files to Create**:
- `/Data/Repositories/IEntityCommentRepository.cs`
- `/Data/Repositories/EntityCommentRepository.cs`
- `/Eidos.Tests/Repositories/EntityCommentRepositoryTests.cs`

**Acceptance Criteria**:
- ✅ All repository methods work correctly
- ✅ Unit tests pass (minimum 10 tests)
- ✅ No N+1 query issues (verify with logging)

---

### 1.4 Service Layer

- [ ] Create `IEntityCommentService` interface
  - [ ] Define all public methods (AddComment, UpdateComment, DeleteComment, etc.)
  - [ ] Add XML documentation comments

- [ ] Implement `EntityCommentService`
  - [ ] Implement `AddCommentAsync(ontologyId, entityType, entityId, userId, text, parentCommentId)`
    - [ ] Parse @mentions from text
    - [ ] Create comment in transaction
    - [ ] Create CommentMention records
    - [ ] Update EntityCommentCount (denormalized)
    - [ ] Commit transaction
  - [ ] Implement `UpdateCommentAsync(commentId, userId, newText)`
    - [ ] Update comment text
    - [ ] Update EditedAt timestamp
    - [ ] Re-parse @mentions (handle additions/deletions)
  - [ ] Implement `DeleteCommentAsync(commentId, userId)`
    - [ ] Soft delete vs hard delete (decide)
    - [ ] Update EntityCommentCount
  - [ ] Implement `MarkThreadResolvedAsync(commentId, userId, resolved)`
    - [ ] Update IsResolved flag
    - [ ] Update ResolvedByUserId
    - [ ] Update EntityCommentCount (UnresolvedThreads)
  - [ ] Implement `GetCommentsForEntityAsync(ontologyId, entityType, entityId)`
  - [ ] Implement `GetCommentThreadAsync(commentId)`
  - [ ] Implement `GetUserMentionsAsync(userId, ontologyId, unreadOnly)`
  - [ ] Implement `MarkMentionViewedAsync(mentionId, userId)`
  - [ ] Implement `GetCommentCountAsync(ontologyId, entityType, entityId)`
  - [ ] Implement `GetCommentCountsForEntitiesAsync(ontologyId, entityType, entityIds)`
  - [ ] Implement permission check methods:
    - [ ] `CanAddCommentAsync(ontologyId, userId)` - uses OntologyPermissionService
    - [ ] `CanEditCommentAsync(commentId, userId)` - own comment or manager
    - [ ] `CanDeleteCommentAsync(commentId, userId)` - own comment or manager

- [ ] Create `MentionParser` utility class
  - [ ] Implement regex-based @mention extraction
  - [ ] Support `@username` and `@email@domain.com` formats
  - [ ] Validate mentioned users exist and have ontology access
  - [ ] Return distinct list of ApplicationUser objects

- [ ] Write service unit tests
  - [ ] Test comment creation with mentions
  - [ ] Test mention parsing edge cases (invalid users, duplicate mentions)
  - [ ] Test comment update
  - [ ] Test comment deletion
  - [ ] Test thread resolution
  - [ ] Test permission checks
  - [ ] Test denormalized count updates

**Files to Create**:
- `/Services/Interfaces/IEntityCommentService.cs`
- `/Services/EntityCommentService.cs`
- `/Services/MentionParser.cs`
- `/Eidos.Tests/Services/EntityCommentServiceTests.cs`

**Acceptance Criteria**:
- ✅ All service methods work correctly
- ✅ Unit tests pass (minimum 15 tests)
- ✅ @mention parsing handles all edge cases
- ✅ Denormalized counts stay in sync

---

## Phase 2: SignalR Integration (Week 1-2)

### 2.1 Hub Methods

- [ ] Update `OntologyHub.cs` to inject `IEntityCommentService`
  - [ ] Add constructor parameter
  - [ ] Add private readonly field

- [ ] Implement `AddComment` hub method
  - [ ] Get current user ID from context
  - [ ] Permission check: `await _commentService.CanAddCommentAsync(ontologyId, userId)`
  - [ ] Create comment: `await _commentService.AddCommentAsync(...)`
  - [ ] Broadcast to group: `await Clients.Group(groupName).SendAsync("CommentAdded", comment)`
  - [ ] Send mention notifications: `await Clients.User(mentionedUserId).SendAsync("MentionNotification", comment)`

- [ ] Implement `UpdateComment` hub method
  - [ ] Permission check: `await _commentService.CanEditCommentAsync(commentId, userId)`
  - [ ] Update comment: `await _commentService.UpdateCommentAsync(commentId, userId, newText)`
  - [ ] Broadcast update: `await Clients.Group(groupName).SendAsync("CommentUpdated", comment)`

- [ ] Implement `DeleteComment` hub method
  - [ ] Permission check: `await _commentService.CanDeleteCommentAsync(commentId, userId)`
  - [ ] Delete comment: `await _commentService.DeleteCommentAsync(commentId, userId)`
  - [ ] Broadcast deletion: `await Clients.Group(groupName).SendAsync("CommentDeleted", commentId, entityType, entityId)`

- [ ] Implement `ResolveCommentThread` hub method
  - [ ] Mark thread as resolved: `await _commentService.MarkThreadResolvedAsync(commentId, userId, resolved)`
  - [ ] Broadcast resolution change: `await Clients.Group(groupName).SendAsync("CommentResolutionChanged", commentId, resolved, userId)`

- [ ] Write SignalR hub integration tests
  - [ ] Test AddComment broadcasts correctly
  - [ ] Test mention notifications are sent to specific users
  - [ ] Test permission denied throws HubException
  - [ ] Test UpdateComment broadcasts correctly
  - [ ] Test DeleteComment broadcasts correctly

**Files to Modify**:
- `/Hubs/OntologyHub.cs`

**Files to Create**:
- `/Eidos.Tests/Hubs/OntologyHubCommentTests.cs`

**Acceptance Criteria**:
- ✅ All hub methods work correctly
- ✅ Real-time broadcasts reach all group members
- ✅ Targeted mention notifications work
- ✅ Permission checks prevent unauthorized operations

---

### 2.2 Client-Side SignalR Handlers

- [ ] Update `ontology-hub.js` (or create if doesn't exist)
  - [ ] Add `connection.on("CommentAdded", handleCommentAdded)`
  - [ ] Add `connection.on("CommentUpdated", handleCommentUpdated)`
  - [ ] Add `connection.on("CommentDeleted", handleCommentDeleted)`
  - [ ] Add `connection.on("CommentResolutionChanged", handleCommentResolutionChanged)`
  - [ ] Add `connection.on("MentionNotification", handleMentionNotification)`

- [ ] Create JavaScript interop methods in `wwwroot/js/comment-interop.js`
  - [ ] `invokeAddComment(ontologyId, entityType, entityId, text, parentCommentId)`
  - [ ] `invokeUpdateComment(commentId, newText)`
  - [ ] `invokeDeleteComment(commentId)`
  - [ ] `invokeResolveComment(commentId, resolved)`

**Files to Create/Modify**:
- `/wwwroot/js/ontology-hub.js`
- `/wwwroot/js/comment-interop.js`

**Acceptance Criteria**:
- ✅ Client-side handlers receive SignalR events
- ✅ JavaScript interop calls hub methods correctly
- ✅ No console errors

---

## Phase 3: UI Components (Week 2-3)

### 3.1 Context Menu Integration

- [ ] **Graph View (Cytoscape.js) - Right-click context menu**
  - [ ] Update `wwwroot/js/graph-view.js`
    - [ ] Add `cy.on('cxttap', 'node', handleRightClick)`
    - [ ] Call `DotNet.invokeMethodAsync('Eidos', 'ShowCommentContextMenu', entityType, entityId, position)`
  - [ ] Create `GraphContextMenu.razor` component (or update existing)
    - [ ] Add "Add Comment" menu item with icon
    - [ ] Show comment count badge if > 0
    - [ ] Add "View Comments (X)" if comments exist
  - [ ] Update graph view component to handle context menu callbacks

- [ ] **List View - Right-click on rows**
  - [ ] Update `ListViewPanel.razor`
    - [ ] Add `@oncontextmenu` handler to table rows
    - [ ] Show context menu with "Add Comment" option
  - [ ] Add comment button to each row (always visible)
    - [ ] Icon: `bi-chat-dots`
    - [ ] Badge showing comment count

- [ ] **Mobile - Floating Action Button (FAB)**
  - [ ] Create `FloatingCommentButton.razor` component
    - [ ] Position: bottom-right
    - [ ] Icon: `bi-chat-dots`
    - [ ] Only show on small screens (`@media (max-width: 768px)`)

**Files to Create**:
- `/Components/Ontology/GraphContextMenu.razor`
- `/Components/Shared/FloatingCommentButton.razor`

**Files to Modify**:
- `/Components/Ontology/ListViewPanel.razor`
- `/wwwroot/js/graph-view.js`

**Acceptance Criteria**:
- ✅ Right-click on concept/relationship shows context menu
- ✅ Context menu displays comment count
- ✅ Clicking "Add Comment" opens comment panel
- ✅ FAB appears on mobile devices

---

### 3.2 Comment Panel Component

- [ ] Create `EntityCommentPanel.razor`
  - [ ] Sliding panel from right (400px width)
  - [ ] Header:
    - [ ] Entity type and name (e.g., "Concept: Person")
    - [ ] Comment count badge
    - [ ] Close button
  - [ ] Body:
    - [ ] Empty state when no comments
    - [ ] List of top-level comments (pagination: load 10 at a time)
    - [ ] "Load more" button for pagination
  - [ ] Footer:
    - [ ] CommentEditor component for new comments

- [ ] Add panel state management
  - [ ] `IsOpen` property
  - [ ] `CurrentEntityType` and `CurrentEntityId`
  - [ ] Open/Close methods
  - [ ] Load comments on open

- [ ] Add real-time update handling
  - [ ] Subscribe to SignalR "CommentAdded" event
  - [ ] Update UI when new comment arrives
  - [ ] Only update if panel is viewing the same entity

- [ ] Create `EntityCommentPanel.razor.css` for styling
  - [ ] Sliding animation (right: -400px to right: 0)
  - [ ] Mobile responsive (full-width on small screens)
  - [ ] Minimal sci-fi aesthetic

**Files to Create**:
- `/Components/Shared/EntityCommentPanel.razor`
- `/Components/Shared/EntityCommentPanel.razor.cs` (code-behind)
- `/Components/Shared/EntityCommentPanel.razor.css`

**Acceptance Criteria**:
- ✅ Panel slides in smoothly (250ms transition)
- ✅ Shows correct entity name in header
- ✅ Loads comments on open
- ✅ Real-time updates append new comments
- ✅ Mobile responsive (full-width)

---

### 3.3 Comment Thread Component

- [ ] Create `CommentThread.razor`
  - [ ] Display single comment with:
    - [ ] User avatar (circular, colored background)
    - [ ] User name
    - [ ] Timestamp ("2 hours ago" format)
    - [ ] Comment text (Markdown rendered)
    - [ ] "Reply" button
    - [ ] "Resolve/Unresolve" button (only on parent comments)
    - [ ] "Edit" button (only for own comments)
    - [ ] "Delete" button (only for own comments or managers)
  - [ ] Render nested replies recursively
    - [ ] Indent with left border
    - [ ] Limit nesting depth to 5 levels (UI constraint)
  - [ ] Resolved state visual
    - [ ] Reduced opacity (0.6)
    - [ ] Strikethrough text
    - [ ] Checkmark icon

- [ ] Implement actions
  - [ ] Reply: Open comment editor inline
  - [ ] Resolve: Call hub method `ResolveCommentThread`
  - [ ] Edit: Inline edit mode
  - [ ] Delete: Confirm and call hub method

- [ ] Create `CommentThread.razor.css` for styling
  - [ ] Avatar styling (32px circle)
  - [ ] Indentation for nested replies (margin-left: 48px)
  - [ ] Resolved state styling

**Files to Create**:
- `/Components/Shared/CommentThread.razor`
- `/Components/Shared/CommentThread.razor.cs`
- `/Components/Shared/CommentThread.razor.css`

**Acceptance Criteria**:
- ✅ Comments display correctly with avatar and metadata
- ✅ Markdown renders safely (no XSS)
- ✅ Nested replies display with indentation
- ✅ Actions (reply, edit, delete, resolve) work correctly
- ✅ Resolved comments have distinct visual state

---

### 3.4 Comment Editor Component

- [ ] Create `CommentEditor.razor`
  - [ ] Textarea for comment input
  - [ ] Placeholder text
  - [ ] Character count (max 10,000)
  - [ ] Submit button (disabled when empty)
  - [ ] Markdown help text ("Markdown supported. Type @ to mention users.")

- [ ] Implement @mention autocomplete
  - [ ] Detect '@' character in textarea
  - [ ] Show dropdown with user list
  - [ ] Filter users as typing continues
  - [ ] Select user with click or Enter key
  - [ ] Replace text with `@username`

- [ ] Add keyboard shortcuts
  - [ ] Enter: Submit comment
  - [ ] Shift+Enter: New line
  - [ ] Escape: Cancel (if in reply mode)

- [ ] Create `MentionDropdown.razor` sub-component
  - [ ] List of users with avatars and names
  - [ ] Highlight selected user (keyboard navigation)

- [ ] Create `CommentEditor.razor.css` for styling
  - [ ] Textarea styling (border, focus state)
  - [ ] Mention dropdown styling (floating, scrollable)

**Files to Create**:
- `/Components/Shared/CommentEditor.razor`
- `/Components/Shared/CommentEditor.razor.cs`
- `/Components/Shared/CommentEditor.razor.css`
- `/Components/Shared/MentionDropdown.razor`
- `/Components/Shared/MentionDropdown.razor.css`

**Acceptance Criteria**:
- ✅ Textarea accepts input and submits on Enter
- ✅ @mention dropdown appears when typing '@'
- ✅ Autocomplete filters users correctly
- ✅ Selected mention replaces text in textarea
- ✅ Keyboard shortcuts work correctly

---

### 3.5 Comment Count Badges

- [ ] Update `GraphView` component
  - [ ] Load comment counts on initialization
  - [ ] Display badge on nodes with comments
  - [ ] Update badge on real-time "CommentAdded" event

- [ ] Update `ListViewPanel` component
  - [ ] Load comment counts for all visible concepts/relationships
  - [ ] Display badge in comment button
  - [ ] Update badge on real-time events

- [ ] Create reusable `CommentBadge.razor` component
  - [ ] Small circular badge (16px diameter)
  - [ ] Primary color background
  - [ ] White text
  - [ ] Position: absolute top-right

**Files to Create**:
- `/Components/Shared/CommentBadge.razor`
- `/Components/Shared/CommentBadge.razor.css`

**Files to Modify**:
- `/Components/Ontology/GraphView.razor`
- `/Components/Ontology/ListViewPanel.razor`

**Acceptance Criteria**:
- ✅ Badges display correct counts
- ✅ Badges update in real-time
- ✅ Badges only show when count > 0
- ✅ Badges positioned correctly on nodes/buttons

---

## Phase 4: @Mention Notifications (Week 3)

### 4.1 In-App Notifications

- [ ] Create `NotificationService`
  - [ ] Track unread mention count
  - [ ] Store mention notifications in-memory (or database)
  - [ ] Provide methods: `AddNotification`, `MarkAsRead`, `GetUnreadCount`

- [ ] Update `NavMenu.razor` (or TopBar)
  - [ ] Add notification bell icon with badge
  - [ ] Badge shows unread mention count
  - [ ] Click opens notification dropdown

- [ ] Create `NotificationDropdown.razor`
  - [ ] List of recent mentions
  - [ ] Click mention to jump to entity comment panel
  - [ ] "Mark all as read" button
  - [ ] "View all" link to dedicated page

- [ ] Update SignalR handler for "MentionNotification"
  - [ ] Add to NotificationService
  - [ ] Update bell badge count
  - [ ] Optional: Show toast notification

**Files to Create**:
- `/Services/Interfaces/INotificationService.cs`
- `/Services/NotificationService.cs`
- `/Components/Shared/NotificationDropdown.razor`
- `/Components/Shared/NotificationDropdown.razor.css`

**Files to Modify**:
- `/Components/Layout/NavMenu.razor` or `/Components/Layout/TopBar.razor`
- `/wwwroot/js/ontology-hub.js` (update MentionNotification handler)

**Acceptance Criteria**:
- ✅ Bell icon shows unread mention count
- ✅ Clicking bell opens dropdown with mentions
- ✅ Clicking mention navigates to comment
- ✅ "Mark all as read" clears badge

---

### 4.2 Email Notifications (Optional)

- [ ] Create `EmailNotificationService`
  - [ ] Send email when user is @mentioned (only if offline)
  - [ ] Batch emails (don't send for every mention)
  - [ ] Include link to entity comment

- [ ] Add user preference: "Email me when mentioned"
  - [ ] Update `UserPreferences` model
  - [ ] Add toggle in user settings
  - [ ] Respect user preference before sending email

- [ ] Create email template for mentions
  - [ ] Subject: "You were mentioned in [Ontology Name]"
  - [ ] Body: Comment excerpt, link to comment

**Files to Create** (if implementing):
- `/Services/Interfaces/IEmailNotificationService.cs`
- `/Services/EmailNotificationService.cs`
- `/Templates/MentionEmailTemplate.cshtml`

**Files to Modify** (if implementing):
- `/Models/UserPreferences.cs`
- `/Components/Settings/NotificationSettings.razor`

**Acceptance Criteria** (if implementing):
- ✅ Email sent when user is offline and mentioned
- ✅ User can opt-out via settings
- ✅ Email contains link to comment

---

## Phase 5: Polish & Testing (Week 4)

### 5.1 Performance Optimization

- [ ] Add database query logging to identify N+1 issues
  - [ ] Review all comment queries
  - [ ] Ensure eager loading with `.Include()` where needed

- [ ] Implement comment pagination
  - [ ] Load 10 comments initially
  - [ ] "Load more" button for additional comments
  - [ ] Lazy load replies (collapsed by default)

- [ ] Cache comment counts
  - [ ] Use `EntityCommentCounts` table
  - [ ] Verify counts update correctly on CRUD operations

- [ ] Add rate limiting
  - [ ] Limit comment creation to 10 per minute per user
  - [ ] Return HTTP 429 (Too Many Requests) if exceeded

- [ ] Profile SignalR message volume
  - [ ] Monitor message frequency in production
  - [ ] Consider debouncing if needed

**Acceptance Criteria**:
- ✅ No N+1 queries detected
- ✅ Comment panel loads in < 200ms
- ✅ Pagination works smoothly
- ✅ Rate limiting prevents spam

---

### 5.2 Accessibility

- [ ] Add ARIA labels to all interactive elements
  - [ ] Comment buttons: `aria-label="Add comment to [entity name]"`
  - [ ] Reply buttons: `aria-label="Reply to comment by [user name]"`
  - [ ] Close button: `aria-label="Close comment panel"`

- [ ] Ensure keyboard navigation works
  - [ ] Tab through comments
  - [ ] Enter to open/close panels
  - [ ] Escape to close panels

- [ ] Add focus indicators (already in design system)
  - [ ] Verify 3px focus ring on all buttons

- [ ] Test with screen reader (VoiceOver on Mac)
  - [ ] Ensure comment thread structure is understandable
  - [ ] Ensure mention autocomplete is accessible

**Acceptance Criteria**:
- ✅ All elements have ARIA labels
- ✅ Keyboard navigation works without mouse
- ✅ Focus indicators visible on all interactive elements
- ✅ Screen reader announces UI changes

---

### 5.3 Testing

- [ ] Write end-to-end tests
  - [ ] Test: User can add comment to concept
  - [ ] Test: User can reply to comment
  - [ ] Test: User can resolve comment thread
  - [ ] Test: User can edit own comment
  - [ ] Test: User can delete own comment
  - [ ] Test: @mention autocomplete works
  - [ ] Test: @mentioned user receives notification
  - [ ] Test: Real-time updates appear for other users

- [ ] Write integration tests
  - [ ] Test: EntityCommentService with real database
  - [ ] Test: OntologyHub with real SignalR connection
  - [ ] Test: Permission checks prevent unauthorized operations

- [ ] Load testing
  - [ ] Simulate 10 concurrent users adding comments
  - [ ] Measure SignalR message latency
  - [ ] Verify database performance under load

**Files to Create**:
- `/Eidos.Tests/Integration/EntityCommentIntegrationTests.cs`
- `/Eidos.Tests/E2E/CommentWorkflowTests.cs`

**Acceptance Criteria**:
- ✅ All unit tests pass (minimum 30 total)
- ✅ All integration tests pass
- ✅ All E2E tests pass
- ✅ Load tests show acceptable performance (< 100ms for SignalR broadcasts)

---

### 5.4 Documentation

- [ ] Update CLAUDE.md with comment feature description
  - [ ] Add to "Core Features" section
  - [ ] Describe SignalR integration
  - [ ] Mention @mention support

- [ ] Create user-facing documentation
  - [ ] How to add comments
  - [ ] How to @mention users
  - [ ] How to resolve discussions

- [ ] Create developer documentation
  - [ ] How to extend comment types (for future)
  - [ ] How to add custom mention triggers
  - [ ] Database schema explanation

- [ ] Add XML documentation comments to all public methods
  - [ ] Service interfaces
  - [ ] Hub methods

**Files to Modify**:
- `/CLAUDE.md`
- `/docs/user-guide.md` (if exists)
- `/docs/development-ledger/` (add entry for this feature)

**Acceptance Criteria**:
- ✅ CLAUDE.md updated with feature description
- ✅ User documentation created
- ✅ All public methods have XML comments

---

## Deployment Checklist

- [ ] Run all migrations in production
- [ ] Verify SignalR endpoints are accessible
- [ ] Monitor error logs for first week
- [ ] Track adoption metrics:
  - [ ] % of ontologies with comments
  - [ ] Average comments per ontology
  - [ ] @mention usage frequency
- [ ] Collect user feedback via in-app survey

---

## Success Metrics

Track these after 2 weeks of production use:

| Metric | Target |
|--------|--------|
| Adoption Rate | 30% of active ontologies have comments |
| Average Comments | 5 comments per ontology |
| @Mention Usage | 20% of comments have @mentions |
| Reply Rate | 40% of comments receive replies |
| Resolution Rate | 60% of threads get resolved |
| Performance | < 200ms comment load time |
| SignalR Latency | < 100ms real-time updates |

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Comment spam | Rate limiting (10 comments/min), permission checks |
| XSS attacks via Markdown | Sanitize HTML output, use trusted Markdown parser |
| Database performance | Denormalized counts, pagination, indexes |
| SignalR message volume | Ontology-scoped groups, debouncing |
| Mention privacy leaks | Only allow mentions for users with ontology access |

---

**Next Steps**:
1. Review this checklist with team
2. Assign tasks to developers
3. Start with Phase 1 (Database & Core Services)
4. Daily standup to track progress
5. Demo after each phase

**Estimated Timeline**:
- Phase 1: 5 days
- Phase 2: 3 days
- Phase 3: 7 days
- Phase 4: 3 days
- Phase 5: 4 days
- **Total**: 22 days (~4 weeks)
