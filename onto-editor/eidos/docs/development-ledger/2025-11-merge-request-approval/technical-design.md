# Merge Request / Approval Workflow - Technical Architecture

**Date**: November 8, 2025
**Feature**: Technical Design for Merge Request System
**Status**: Planning Phase

---

## Architecture Overview

The Merge Request system integrates with existing Eidos architecture patterns:
- **Repository Pattern** for data access
- **Service Layer** for business logic
- **Command Pattern** for undoable operations
- **SignalR** for real-time notifications
- **Activity Tracking** for audit trail

```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor Components                        │
│  OntologyView │ MergeRequestList │ MergeRequestDetail       │
└────────┬──────────────────┬───────────────────┬────────────┘
         │                  │                   │
         ▼                  ▼                   ▼
┌─────────────────────────────────────────────────────────────┐
│                      SignalR Hub                             │
│              MergeRequestHub (Real-time updates)            │
└────────┬────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│                      Service Layer                           │
│  MergeRequestService │ ChangeDetectionService │             │
│  OntologyActivityService │ OntologyPermissionService        │
└────────┬──────────────────────────┬────────────────────────┘
         │                          │
         ▼                          ▼
┌─────────────────────────┐  ┌──────────────────────────────┐
│    Repository Layer     │  │     Command Pattern          │
│  MergeRequestRepository │  │  ApplyMergeRequestCommand    │
│  MergeRequestChange     │  │  (Atomic application)        │
│  Repository             │  │                              │
└────────┬────────────────┘  └──────────────┬───────────────┘
         │                                  │
         ▼                                  ▼
┌─────────────────────────────────────────────────────────────┐
│                   Entity Framework Core                      │
│                   OntologyDbContext                          │
└────────┬────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│                   Database (SQLite/Azure SQL)                │
│  MergeRequests │ MergeRequestChanges │ MergeRequestComments  │
└─────────────────────────────────────────────────────────────┘
```

---

## Database Schema

### Table: MergeRequests

Primary entity for merge requests.

```sql
CREATE TABLE MergeRequests (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OntologyId INT NOT NULL,

    -- Submitter information
    SubmitterId NVARCHAR(450) NULL, -- FK to AspNetUsers, null if guest
    SubmitterName NVARCHAR(200) NOT NULL, -- Denormalized for history
    SubmitterEmail NVARCHAR(200) NULL,

    -- MR metadata
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL, -- Markdown supported
    Status NVARCHAR(20) NOT NULL, -- Draft, Pending, Approved, Rejected, Cancelled, Stale

    -- Version tracking
    BaseVersionNumber INT NOT NULL, -- OntologyActivity version when MR created
    CurrentVersionNumber INT NULL, -- Updated if ontology changes (stale detection)

    -- Change summary (denormalized for performance)
    ConceptsAdded INT NOT NULL DEFAULT 0,
    ConceptsModified INT NOT NULL DEFAULT 0,
    ConceptsDeleted INT NOT NULL DEFAULT 0,
    RelationshipsAdded INT NOT NULL DEFAULT 0,
    RelationshipsModified INT NOT NULL DEFAULT 0,
    RelationshipsDeleted INT NOT NULL DEFAULT 0,
    PropertiesChanged INT NOT NULL DEFAULT 0,
    TotalChanges INT NOT NULL DEFAULT 0,

    -- Reviewer information
    AssignedReviewerId NVARCHAR(450) NULL, -- FK to AspNetUsers
    ReviewedById NVARCHAR(450) NULL, -- Who approved/rejected
    ReviewedAt DATETIME2 NULL,
    ReviewComment NVARCHAR(MAX) NULL, -- Approval comment or rejection reason

    -- Conflict tracking
    HasConflicts BIT NOT NULL DEFAULT 0,
    ConflictDetails NVARCHAR(MAX) NULL, -- JSON describing conflicts

    -- Timestamps
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    SubmittedAt DATETIME2 NULL, -- When status changed from Draft to Pending

    -- Audit trail
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,

    -- Foreign keys
    CONSTRAINT FK_MergeRequests_Ontology FOREIGN KEY (OntologyId)
        REFERENCES Ontologies(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MergeRequests_Submitter FOREIGN KEY (SubmitterId)
        REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
    CONSTRAINT FK_MergeRequests_AssignedReviewer FOREIGN KEY (AssignedReviewerId)
        REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
    CONSTRAINT FK_MergeRequests_ReviewedBy FOREIGN KEY (ReviewedById)
        REFERENCES AspNetUsers(Id) ON DELETE SET NULL,

    -- Indexes
    INDEX IX_MergeRequests_OntologyId (OntologyId),
    INDEX IX_MergeRequests_Status (Status),
    INDEX IX_MergeRequests_SubmitterId (SubmitterId),
    INDEX IX_MergeRequests_CreatedAt (CreatedAt DESC),
    INDEX IX_MergeRequests_AssignedReviewerId (AssignedReviewerId)
);
```

---

### Table: MergeRequestChanges

Individual changes within a merge request.

```sql
CREATE TABLE MergeRequestChanges (
    Id INT PRIMARY KEY IDENTITY(1,1),
    MergeRequestId INT NOT NULL,

    -- Change metadata
    ChangeType NVARCHAR(20) NOT NULL, -- Create, Update, Delete
    EntityType NVARCHAR(50) NOT NULL, -- Concept, Relationship, Property, Individual
    EntityId INT NULL, -- ID of entity being modified (null for creates)
    EntityName NVARCHAR(500) NULL, -- Name for display

    -- Change payload
    BeforeSnapshot NVARCHAR(MAX) NULL, -- JSON of entity before change (null for creates)
    AfterSnapshot NVARCHAR(MAX) NOT NULL, -- JSON of entity after change (null for deletes)

    -- Field-level diff (for efficient conflict detection)
    FieldChanges NVARCHAR(MAX) NULL, -- JSON: [{"field": "name", "before": "Person", "after": "Human"}]

    -- Sequence for ordering
    SequenceNumber INT NOT NULL, -- Order in which changes should be applied

    -- Conflict tracking
    HasConflict BIT NOT NULL DEFAULT 0,
    ConflictWith NVARCHAR(500) NULL, -- Description of conflicting change

    -- Foreign key
    CONSTRAINT FK_MergeRequestChanges_MergeRequest FOREIGN KEY (MergeRequestId)
        REFERENCES MergeRequests(Id) ON DELETE CASCADE,

    -- Indexes
    INDEX IX_MergeRequestChanges_MergeRequestId (MergeRequestId),
    INDEX IX_MergeRequestChanges_EntityType_EntityId (EntityType, EntityId),
    INDEX IX_MergeRequestChanges_SequenceNumber (SequenceNumber)
);
```

---

### Table: MergeRequestComments

Discussion threads on merge requests.

```sql
CREATE TABLE MergeRequestComments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    MergeRequestId INT NOT NULL,

    -- Author information
    AuthorId NVARCHAR(450) NULL, -- FK to AspNetUsers
    AuthorName NVARCHAR(200) NOT NULL,

    -- Comment content
    CommentText NVARCHAR(MAX) NOT NULL, -- Markdown supported

    -- Metadata
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL, -- If edited
    IsEdited BIT NOT NULL DEFAULT 0,
    IsDeleted BIT NOT NULL DEFAULT 0, -- Soft delete

    -- Threading (future enhancement)
    ParentCommentId INT NULL, -- For nested replies

    -- Foreign keys
    CONSTRAINT FK_MergeRequestComments_MergeRequest FOREIGN KEY (MergeRequestId)
        REFERENCES MergeRequests(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MergeRequestComments_Author FOREIGN KEY (AuthorId)
        REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
    CONSTRAINT FK_MergeRequestComments_Parent FOREIGN KEY (ParentCommentId)
        REFERENCES MergeRequestComments(Id) ON DELETE NO ACTION,

    -- Indexes
    INDEX IX_MergeRequestComments_MergeRequestId (MergeRequestId),
    INDEX IX_MergeRequestComments_CreatedAt (CreatedAt)
);
```

---

### Table: OntologySettings (Extension)

Add approval mode setting to existing Ontologies table or new settings table.

**Option A: Extend Ontologies table (simpler)**
```sql
ALTER TABLE Ontologies
ADD RequiresApproval BIT NOT NULL DEFAULT 0;

ALTER TABLE Ontologies
ADD ApprovalModeEnabledAt DATETIME2 NULL;

ALTER TABLE Ontologies
ADD ApprovalModeEnabledBy NVARCHAR(450) NULL;
```

**Option B: New OntologySettings table (more flexible)**
```sql
CREATE TABLE OntologySettings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OntologyId INT NOT NULL UNIQUE,

    -- Approval mode settings
    RequiresApproval BIT NOT NULL DEFAULT 0,
    ApprovalModeEnabledAt DATETIME2 NULL,
    ApprovalModeEnabledBy NVARCHAR(450) NULL,

    -- Future settings can be added here
    -- AllowPublicMergeRequests BIT,
    -- AutoApproveThreshold INT,
    -- etc.

    -- Timestamps
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Foreign keys
    CONSTRAINT FK_OntologySettings_Ontology FOREIGN KEY (OntologyId)
        REFERENCES Ontologies(Id) ON DELETE CASCADE,

    INDEX IX_OntologySettings_OntologyId (OntologyId)
);
```

**Recommendation**: Option A (extend Ontologies table) for MVP simplicity. Option B for long-term flexibility.

---

## Domain Models

### MergeRequest.cs

```csharp
namespace Eidos.Models;

public class MergeRequest
{
    public int Id { get; set; }
    public int OntologyId { get; set; }
    public Ontology Ontology { get; set; } = null!;

    // Submitter
    public string? SubmitterId { get; set; }
    public ApplicationUser? Submitter { get; set; }
    public string SubmitterName { get; set; } = string.Empty;
    public string? SubmitterEmail { get; set; }

    // Metadata
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = MergeRequestStatus.Draft;

    // Version tracking
    public int BaseVersionNumber { get; set; }
    public int? CurrentVersionNumber { get; set; }

    // Change summary
    public int ConceptsAdded { get; set; }
    public int ConceptsModified { get; set; }
    public int ConceptsDeleted { get; set; }
    public int RelationshipsAdded { get; set; }
    public int RelationshipsModified { get; set; }
    public int RelationshipsDeleted { get; set; }
    public int PropertiesChanged { get; set; }
    public int TotalChanges { get; set; }

    // Review
    public string? AssignedReviewerId { get; set; }
    public ApplicationUser? AssignedReviewer { get; set; }
    public string? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }

    // Conflicts
    public bool HasConflicts { get; set; }
    public string? ConflictDetails { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    // Audit
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Navigation properties
    public ICollection<MergeRequestChange> Changes { get; set; } = new List<MergeRequestChange>();
    public ICollection<MergeRequestComment> Comments { get; set; } = new List<MergeRequestComment>();

    // Computed properties
    public bool IsStale => CurrentVersionNumber.HasValue && CurrentVersionNumber > BaseVersionNumber;
    public bool IsPending => Status == MergeRequestStatus.Pending;
    public bool IsApproved => Status == MergeRequestStatus.Approved;
    public bool IsRejected => Status == MergeRequestStatus.Rejected;
    public bool IsDraft => Status == MergeRequestStatus.Draft;
    public bool CanBeReviewed => (Status == MergeRequestStatus.Pending || IsStale) && !HasConflicts;
}

public static class MergeRequestStatus
{
    public const string Draft = "Draft";
    public const string Pending = "Pending";
    public const string Stale = "Stale";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
}
```

---

### MergeRequestChange.cs

```csharp
namespace Eidos.Models;

public class MergeRequestChange
{
    public int Id { get; set; }
    public int MergeRequestId { get; set; }
    public MergeRequest MergeRequest { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string ChangeType { get; set; } = string.Empty; // Create, Update, Delete

    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty; // Concept, Relationship, etc.

    public int? EntityId { get; set; }

    [StringLength(500)]
    public string? EntityName { get; set; }

    // Snapshots (JSON serialized)
    public string? BeforeSnapshot { get; set; }

    [Required]
    public string AfterSnapshot { get; set; } = string.Empty;

    // Field-level changes (JSON)
    public string? FieldChanges { get; set; }

    public int SequenceNumber { get; set; }

    // Conflicts
    public bool HasConflict { get; set; }
    public string? ConflictWith { get; set; }
}

public static class MergeRequestChangeType
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
}
```

---

### MergeRequestComment.cs

```csharp
namespace Eidos.Models;

public class MergeRequestComment
{
    public int Id { get; set; }
    public int MergeRequestId { get; set; }
    public MergeRequest MergeRequest { get; set; } = null!;

    public string? AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }

    [Required]
    [StringLength(200)]
    public string AuthorName { get; set; } = string.Empty;

    [Required]
    public string CommentText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }

    // Threading
    public int? ParentCommentId { get; set; }
    public MergeRequestComment? ParentComment { get; set; }
    public ICollection<MergeRequestComment> Replies { get; set; } = new List<MergeRequestComment>();
}
```

---

## DTOs (Data Transfer Objects)

### MergeRequestDto.cs

```csharp
namespace Eidos.Models.DTOs;

public class MergeRequestDto
{
    public int Id { get; set; }
    public int OntologyId { get; set; }
    public string OntologyName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;

    // Submitter info
    public string SubmitterName { get; set; } = string.Empty;
    public string? SubmitterEmail { get; set; }
    public string? SubmitterId { get; set; }

    // Change summary
    public int ConceptsAdded { get; set; }
    public int ConceptsModified { get; set; }
    public int ConceptsDeleted { get; set; }
    public int RelationshipsAdded { get; set; }
    public int RelationshipsModified { get; set; }
    public int RelationshipsDeleted { get; set; }
    public int PropertiesChanged { get; set; }
    public int TotalChanges { get; set; }

    // Review info
    public string? AssignedReviewerName { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    // Flags
    public bool HasConflicts { get; set; }
    public bool IsStale { get; set; }
    public bool CanReview { get; set; } // Based on user permissions
    public bool CanCancel { get; set; } // Based on user permissions

    // Summary text (computed)
    public string ChangeSummary => BuildChangeSummary();

    private string BuildChangeSummary()
    {
        var parts = new List<string>();
        if (ConceptsAdded > 0) parts.Add($"+{ConceptsAdded} concepts");
        if (ConceptsModified > 0) parts.Add($"~{ConceptsModified} concepts");
        if (ConceptsDeleted > 0) parts.Add($"-{ConceptsDeleted} concepts");
        if (RelationshipsAdded > 0) parts.Add($"+{RelationshipsAdded} relationships");
        if (RelationshipsModified > 0) parts.Add($"~{RelationshipsModified} relationships");
        if (RelationshipsDeleted > 0) parts.Add($"-{RelationshipsDeleted} relationships");
        if (PropertiesChanged > 0) parts.Add($"{PropertiesChanged} properties changed");

        return parts.Any() ? string.Join(", ", parts) : "No changes";
    }
}
```

---

### MergeRequestChangeDto.cs

```csharp
namespace Eidos.Models.DTOs;

public class MergeRequestChangeDto
{
    public int Id { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? EntityName { get; set; }

    // Parsed snapshots
    public object? Before { get; set; }
    public object? After { get; set; }

    // Field-level changes
    public List<FieldChange>? FieldChanges { get; set; }

    public int SequenceNumber { get; set; }
    public bool HasConflict { get; set; }
    public string? ConflictWith { get; set; }
}

public class FieldChange
{
    public string FieldName { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public bool IsConflict { get; set; }
}
```

---

### CreateMergeRequestDto.cs

```csharp
namespace Eidos.Models.DTOs;

public class CreateMergeRequestDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool SubmitForReview { get; set; } = false; // false = save as draft

    // Changes are captured from command stack, not passed in DTO
}
```

---

### ApprovalDecisionDto.cs

```csharp
namespace Eidos.Models.DTOs;

public class ApprovalDecisionDto
{
    [Required]
    public int MergeRequestId { get; set; }

    [Required]
    public bool Approved { get; set; } // true = approve, false = reject

    [StringLength(500)]
    public string? Comment { get; set; } // Optional for approval, required for rejection
}
```

---

## Service Layer

### IMergeRequestService.cs

```csharp
namespace Eidos.Services.Interfaces;

public interface IMergeRequestService
{
    // CRUD operations
    Task<MergeRequestDto> CreateMergeRequestAsync(
        int ontologyId,
        CreateMergeRequestDto dto,
        string userId,
        string userName);

    Task<MergeRequestDto> UpdateMergeRequestAsync(
        int mergeRequestId,
        CreateMergeRequestDto dto,
        string userId);

    Task<MergeRequestDto> GetMergeRequestAsync(int mergeRequestId, string userId);

    Task<List<MergeRequestDto>> GetMergeRequestsAsync(
        int ontologyId,
        string? status = null,
        string? userId = null,
        int skip = 0,
        int take = 25);

    // Workflow operations
    Task<MergeRequestDto> SubmitForReviewAsync(int mergeRequestId, string userId);

    Task<MergeRequestDto> ApproveMergeRequestAsync(
        int mergeRequestId,
        string reviewerId,
        string? comment = null);

    Task<MergeRequestDto> RejectMergeRequestAsync(
        int mergeRequestId,
        string reviewerId,
        string reason);

    Task<MergeRequestDto> CancelMergeRequestAsync(int mergeRequestId, string userId);

    Task<List<MergeRequestDto>> BulkApproveAsync(
        List<int> mergeRequestIds,
        string reviewerId,
        string? comment = null);

    Task<List<MergeRequestDto>> BulkRejectAsync(
        List<int> mergeRequestIds,
        string reviewerId,
        string reason);

    // Conflict detection
    Task<bool> DetectConflictsAsync(int mergeRequestId);

    Task<List<MergeRequestChangeDto>> GetConflictsAsync(int mergeRequestId);

    // Change tracking
    Task<List<MergeRequestChangeDto>> GetChangesAsync(int mergeRequestId);

    Task<int> GetPendingCountAsync(int ontologyId);

    // Comments
    Task<MergeRequestComment> AddCommentAsync(
        int mergeRequestId,
        string userId,
        string userName,
        string commentText);

    Task<List<MergeRequestComment>> GetCommentsAsync(int mergeRequestId);

    // Permissions
    Task<bool> CanUserReviewAsync(int mergeRequestId, string userId);

    Task<bool> CanUserCancelAsync(int mergeRequestId, string userId);
}
```

---

### MergeRequestService.cs (Implementation Outline)

```csharp
namespace Eidos.Services;

public class MergeRequestService : IMergeRequestService
{
    private readonly IMergeRequestRepository _mergeRequestRepository;
    private readonly IChangeDetectionService _changeDetectionService;
    private readonly IOntologyActivityService _activityService;
    private readonly IOntologyPermissionService _permissionService;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly ILogger<MergeRequestService> _logger;

    public MergeRequestService(
        IMergeRequestRepository mergeRequestRepository,
        IChangeDetectionService changeDetectionService,
        IOntologyActivityService activityService,
        IOntologyPermissionService permissionService,
        IDbContextFactory<OntologyDbContext> contextFactory,
        ILogger<MergeRequestService> logger)
    {
        _mergeRequestRepository = mergeRequestRepository;
        _changeDetectionService = changeDetectionService;
        _activityService = activityService;
        _permissionService = permissionService;
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<MergeRequestDto> CreateMergeRequestAsync(
        int ontologyId,
        CreateMergeRequestDto dto,
        string userId,
        string userName)
    {
        // 1. Validate user has Edit permission
        var canEdit = await _permissionService.CanEditAsync(ontologyId, userId);
        if (!canEdit)
            throw new UnauthorizedAccessException("User does not have edit permission.");

        // 2. Get current ontology version
        var currentVersion = await _activityService.GetCurrentVersionNumberAsync(ontologyId);

        // 3. Capture changes from command invoker (to be implemented)
        var changes = await _changeDetectionService.CaptureChangesAsync(ontologyId, userId);

        if (!changes.Any())
            throw new InvalidOperationException("No changes to include in merge request.");

        // 4. Create MergeRequest entity
        var mergeRequest = new MergeRequest
        {
            OntologyId = ontologyId,
            SubmitterId = userId,
            SubmitterName = userName,
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.SubmitForReview ? MergeRequestStatus.Pending : MergeRequestStatus.Draft,
            BaseVersionNumber = currentVersion,
            CurrentVersionNumber = currentVersion,
            SubmittedAt = dto.SubmitForReview ? DateTime.UtcNow : null,
            // Change counts populated by CalculateChangeSummary
        };

        // 5. Add changes
        foreach (var change in changes)
        {
            mergeRequest.Changes.Add(change);
        }

        // 6. Calculate change summary
        CalculateChangeSummary(mergeRequest);

        // 7. Save to database
        await _mergeRequestRepository.AddAsync(mergeRequest);

        // 8. Record activity
        await _activityService.RecordActivityAsync(new OntologyActivity
        {
            OntologyId = ontologyId,
            UserId = userId,
            ActorName = userName,
            ActivityType = "merge_request_created",
            EntityType = "merge_request",
            EntityId = mergeRequest.Id,
            EntityName = mergeRequest.Title,
            Description = $"Created merge request: {mergeRequest.Title}",
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation(
            "User {UserId} created merge request {MergeRequestId} for ontology {OntologyId}",
            userId, mergeRequest.Id, ontologyId);

        return MapToDto(mergeRequest);
    }

    public async Task<MergeRequestDto> ApproveMergeRequestAsync(
        int mergeRequestId,
        string reviewerId,
        string? comment = null)
    {
        // 1. Load merge request with changes
        var mergeRequest = await _mergeRequestRepository.GetWithChangesAsync(mergeRequestId);
        if (mergeRequest == null)
            throw new NotFoundException($"Merge request {mergeRequestId} not found.");

        // 2. Validate reviewer permissions
        var canManage = await _permissionService.CanManageAsync(
            mergeRequest.OntologyId,
            reviewerId);
        if (!canManage)
            throw new UnauthorizedAccessException("User does not have permission to approve.");

        // 3. Prevent self-approval
        if (mergeRequest.SubmitterId == reviewerId)
            throw new InvalidOperationException("Cannot approve your own merge request.");

        // 4. Check status
        if (mergeRequest.Status != MergeRequestStatus.Pending &&
            mergeRequest.Status != MergeRequestStatus.Stale)
            throw new InvalidOperationException($"Cannot approve merge request with status {mergeRequest.Status}.");

        // 5. Detect conflicts
        var hasConflicts = await DetectConflictsAsync(mergeRequestId);
        if (hasConflicts)
            throw new InvalidOperationException("Cannot approve merge request with conflicts.");

        // 6. Apply changes atomically using transaction
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Apply each change in sequence
            foreach (var change in mergeRequest.Changes.OrderBy(c => c.SequenceNumber))
            {
                await ApplyChangeAsync(change, context);
            }

            // Update merge request status
            mergeRequest.Status = MergeRequestStatus.Approved;
            mergeRequest.ReviewedById = reviewerId;
            mergeRequest.ReviewedAt = DateTime.UtcNow;
            mergeRequest.ReviewComment = comment;
            mergeRequest.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // Record approval activity
            await _activityService.RecordActivityAsync(new OntologyActivity
            {
                OntologyId = mergeRequest.OntologyId,
                UserId = reviewerId,
                ActivityType = "merge_request_approved",
                EntityType = "merge_request",
                EntityId = mergeRequestId,
                EntityName = mergeRequest.Title,
                Description = $"Approved merge request: {mergeRequest.Title}",
                Notes = comment,
                CreatedAt = DateTime.UtcNow
            });

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Reviewer {ReviewerId} approved merge request {MergeRequestId}",
                reviewerId, mergeRequestId);

            return MapToDto(mergeRequest);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "Failed to approve merge request {MergeRequestId}",
                mergeRequestId);
            throw;
        }
    }

    private async Task ApplyChangeAsync(
        MergeRequestChange change,
        OntologyDbContext context)
    {
        // Deserialize the AfterSnapshot and apply based on EntityType
        switch (change.EntityType)
        {
            case EntityTypes.Concept:
                await ApplyConceptChangeAsync(change, context);
                break;
            case EntityTypes.Relationship:
                await ApplyRelationshipChangeAsync(change, context);
                break;
            case EntityTypes.Property:
                await ApplyPropertyChangeAsync(change, context);
                break;
            default:
                throw new NotImplementedException(
                    $"Change type {change.EntityType} not implemented.");
        }
    }

    private async Task ApplyConceptChangeAsync(
        MergeRequestChange change,
        OntologyDbContext context)
    {
        var conceptData = JsonSerializer.Deserialize<Concept>(change.AfterSnapshot);
        if (conceptData == null) return;

        switch (change.ChangeType)
        {
            case MergeRequestChangeType.Create:
                context.Concepts.Add(conceptData);
                break;

            case MergeRequestChangeType.Update:
                var existing = await context.Concepts.FindAsync(change.EntityId);
                if (existing != null)
                {
                    // Update fields
                    existing.Name = conceptData.Name;
                    existing.Definition = conceptData.Definition;
                    existing.SimpleExplanation = conceptData.SimpleExplanation;
                    existing.Examples = conceptData.Examples;
                    existing.Category = conceptData.Category;
                    existing.Color = conceptData.Color;
                    // ... other fields
                    context.Concepts.Update(existing);
                }
                break;

            case MergeRequestChangeType.Delete:
                var toDelete = await context.Concepts.FindAsync(change.EntityId);
                if (toDelete != null)
                {
                    context.Concepts.Remove(toDelete);
                }
                break;
        }

        await context.SaveChangesAsync();
    }

    // Similar methods for Relationship, Property, etc.

    private void CalculateChangeSummary(MergeRequest mergeRequest)
    {
        mergeRequest.ConceptsAdded = mergeRequest.Changes
            .Count(c => c.EntityType == EntityTypes.Concept &&
                       c.ChangeType == MergeRequestChangeType.Create);

        mergeRequest.ConceptsModified = mergeRequest.Changes
            .Count(c => c.EntityType == EntityTypes.Concept &&
                       c.ChangeType == MergeRequestChangeType.Update);

        mergeRequest.ConceptsDeleted = mergeRequest.Changes
            .Count(c => c.EntityType == EntityTypes.Concept &&
                       c.ChangeType == MergeRequestChangeType.Delete);

        // Similar for relationships and properties

        mergeRequest.TotalChanges = mergeRequest.Changes.Count;
    }

    private MergeRequestDto MapToDto(MergeRequest mergeRequest)
    {
        return new MergeRequestDto
        {
            Id = mergeRequest.Id,
            OntologyId = mergeRequest.OntologyId,
            Title = mergeRequest.Title,
            Description = mergeRequest.Description,
            Status = mergeRequest.Status,
            SubmitterName = mergeRequest.SubmitterName,
            SubmitterEmail = mergeRequest.SubmitterEmail,
            SubmitterId = mergeRequest.SubmitterId,
            ConceptsAdded = mergeRequest.ConceptsAdded,
            ConceptsModified = mergeRequest.ConceptsModified,
            ConceptsDeleted = mergeRequest.ConceptsDeleted,
            RelationshipsAdded = mergeRequest.RelationshipsAdded,
            RelationshipsModified = mergeRequest.RelationshipsModified,
            RelationshipsDeleted = mergeRequest.RelationshipsDeleted,
            PropertiesChanged = mergeRequest.PropertiesChanged,
            TotalChanges = mergeRequest.TotalChanges,
            AssignedReviewerName = mergeRequest.AssignedReviewer?.Email,
            ReviewedByName = mergeRequest.ReviewedBy?.Email,
            ReviewedAt = mergeRequest.ReviewedAt,
            ReviewComment = mergeRequest.ReviewComment,
            CreatedAt = mergeRequest.CreatedAt,
            SubmittedAt = mergeRequest.SubmittedAt,
            HasConflicts = mergeRequest.HasConflicts,
            IsStale = mergeRequest.IsStale
        };
    }
}
```

---

### IChangeDetectionService.cs

```csharp
namespace Eidos.Services.Interfaces;

/// <summary>
/// Detects and captures changes made to an ontology
/// </summary>
public interface IChangeDetectionService
{
    /// <summary>
    /// Capture all uncommitted changes from command invoker
    /// </summary>
    Task<List<MergeRequestChange>> CaptureChangesAsync(int ontologyId, string userId);

    /// <summary>
    /// Detect conflicts between merge request and current ontology state
    /// </summary>
    Task<List<Conflict>> DetectConflictsAsync(MergeRequest mergeRequest);

    /// <summary>
    /// Generate diff between two entity snapshots
    /// </summary>
    List<FieldChange> GenerateDiff(string beforeJson, string afterJson);
}

public class Conflict
{
    public int ChangeId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public object? MergeRequestValue { get; set; }
    public object? CurrentValue { get; set; }
    public string ConflictType { get; set; } = string.Empty; // "Modified", "Deleted", "Structural"
}
```

---

### ChangeDetectionService.cs (Implementation Outline)

```csharp
namespace Eidos.Services;

public class ChangeDetectionService : IChangeDetectionService
{
    private readonly CommandInvoker _commandInvoker;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly ILogger<ChangeDetectionService> _logger;

    public async Task<List<MergeRequestChange>> CaptureChangesAsync(
        int ontologyId,
        string userId)
    {
        // This is a key integration point with the existing command pattern
        // The CommandInvoker maintains an undo stack of executed commands
        // We need to serialize these into MergeRequestChanges

        var changes = new List<MergeRequestChange>();
        int sequenceNumber = 1;

        // Get commands from invoker (this will require extending CommandInvoker)
        // to expose a method like GetExecutedCommands()
        // var commands = _commandInvoker.GetExecutedCommands(ontologyId);

        // For now, pseudocode:
        // foreach (var command in commands)
        // {
        //     var change = new MergeRequestChange
        //     {
        //         ChangeType = DetermineChangeType(command),
        //         EntityType = command.EntityType,
        //         EntityId = command.EntityId,
        //         BeforeSnapshot = command.BeforeSnapshot, // Commands must capture this
        //         AfterSnapshot = command.AfterSnapshot,
        //         SequenceNumber = sequenceNumber++
        //     };
        //
        //     changes.Add(change);
        // }

        return changes;
    }

    public async Task<List<Conflict>> DetectConflictsAsync(MergeRequest mergeRequest)
    {
        var conflicts = new List<Conflict>();

        await using var context = await _contextFactory.CreateDbContextAsync();

        foreach (var change in mergeRequest.Changes)
        {
            if (change.ChangeType == MergeRequestChangeType.Create)
            {
                // Check if entity was created elsewhere
                // (by name, if ID doesn't exist yet)
                continue;
            }

            if (change.EntityType == EntityTypes.Concept)
            {
                var current = await context.Concepts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == change.EntityId);

                if (current == null && change.ChangeType != MergeRequestChangeType.Delete)
                {
                    // Entity was deleted
                    conflicts.Add(new Conflict
                    {
                        ChangeId = change.Id,
                        EntityType = change.EntityType,
                        EntityId = change.EntityId,
                        ConflictType = "Deleted",
                        FieldName = "*"
                    });
                    continue;
                }

                if (current != null)
                {
                    // Deserialize MR's before snapshot
                    var mrBefore = JsonSerializer.Deserialize<Concept>(change.BeforeSnapshot);

                    // Compare field by field
                    if (mrBefore.Name != current.Name)
                    {
                        conflicts.Add(new Conflict
                        {
                            ChangeId = change.Id,
                            EntityType = change.EntityType,
                            EntityId = change.EntityId,
                            FieldName = "Name",
                            MergeRequestValue = mrBefore.Name,
                            CurrentValue = current.Name,
                            ConflictType = "Modified"
                        });
                    }

                    // Repeat for other fields...
                }
            }

            // Similar logic for Relationship, Property, etc.
        }

        return conflicts;
    }

    public List<FieldChange> GenerateDiff(string beforeJson, string afterJson)
    {
        var fieldChanges = new List<FieldChange>();

        var before = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(beforeJson);
        var after = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(afterJson);

        if (before == null || after == null)
            return fieldChanges;

        // Find all changed fields
        foreach (var key in after.Keys)
        {
            if (!before.ContainsKey(key))
            {
                // New field
                fieldChanges.Add(new FieldChange
                {
                    FieldName = key,
                    OldValue = null,
                    NewValue = after[key].ToString()
                });
            }
            else if (!JsonElement.DeepEquals(before[key], after[key]))
            {
                // Modified field
                fieldChanges.Add(new FieldChange
                {
                    FieldName = key,
                    OldValue = before[key].ToString(),
                    NewValue = after[key].ToString()
                });
            }
        }

        // Find deleted fields
        foreach (var key in before.Keys)
        {
            if (!after.ContainsKey(key))
            {
                fieldChanges.Add(new FieldChange
                {
                    FieldName = key,
                    OldValue = before[key].ToString(),
                    NewValue = null
                });
            }
        }

        return fieldChanges;
    }
}
```

---

## Repository Layer

### IMergeRequestRepository.cs

```csharp
namespace Eidos.Data.Repositories;

public interface IMergeRequestRepository
{
    Task<MergeRequest> AddAsync(MergeRequest mergeRequest);
    Task<MergeRequest?> GetByIdAsync(int id);
    Task<MergeRequest?> GetWithChangesAsync(int id);
    Task<MergeRequest?> GetWithCommentsAsync(int id);
    Task<List<MergeRequest>> GetByOntologyAsync(int ontologyId);
    Task<List<MergeRequest>> GetByStatusAsync(int ontologyId, string status);
    Task<List<MergeRequest>> GetBySubmitterAsync(string submitterId);
    Task UpdateAsync(MergeRequest mergeRequest);
    Task DeleteAsync(int id);
    Task<int> GetPendingCountAsync(int ontologyId);
}
```

---

## State Management Integration

### OntologyViewState Extension

Extend existing `OntologyViewState.cs` to support approval mode:

```csharp
public class OntologyViewState
{
    // Existing properties...

    // Approval mode
    public bool RequiresApproval { get; private set; }
    public bool CanCreateMergeRequest => CanEdit && RequiresApproval;
    public bool CanDirectEdit => CanEdit && !RequiresApproval;
    public int PendingMergeRequestCount { get; private set; }

    public void SetApprovalMode(bool requiresApproval, int pendingCount = 0)
    {
        RequiresApproval = requiresApproval;
        PendingMergeRequestCount = pendingCount;
        StateChanged?.Invoke();
    }
}
```

---

## SignalR Hub

### MergeRequestHub.cs

```csharp
namespace Eidos.Hubs;

public class MergeRequestHub : Hub
{
    private readonly IMergeRequestService _mergeRequestService;
    private readonly IOntologyPermissionService _permissionService;

    public MergeRequestHub(
        IMergeRequestService mergeRequestService,
        IOntologyPermissionService permissionService)
    {
        _mergeRequestService = mergeRequestService;
        _permissionService = permissionService;
    }

    public async Task JoinOntologyGroup(int ontologyId)
    {
        // Join SignalR group for this ontology
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ontology-{ontologyId}");
    }

    public async Task LeaveOntologyGroup(int ontologyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ontology-{ontologyId}");
    }

    // Called by service layer to broadcast events
    public async Task NotifyMergeRequestCreated(int ontologyId, MergeRequestDto mergeRequest)
    {
        await Clients.Group($"ontology-{ontologyId}")
            .SendAsync("MergeRequestCreated", mergeRequest);
    }

    public async Task NotifyMergeRequestApproved(int ontologyId, MergeRequestDto mergeRequest)
    {
        await Clients.Group($"ontology-{ontologyId}")
            .SendAsync("MergeRequestApproved", mergeRequest);
    }

    public async Task NotifyMergeRequestRejected(int ontologyId, MergeRequestDto mergeRequest)
    {
        await Clients.Group($"ontology-{ontologyId}")
            .SendAsync("MergeRequestRejected", mergeRequest);
    }

    public async Task NotifyCommentAdded(int mergeRequestId, MergeRequestComment comment)
    {
        await Clients.Group($"merge-request-{mergeRequestId}")
            .SendAsync("CommentAdded", comment);
    }
}
```

---

## Integration Points

### 1. Command Pattern Integration

**Challenge**: Capture changes from CommandInvoker for MR creation.

**Solution**:
- Extend `ICommand` interface to include `GetSnapshot()` method
- Store before/after snapshots in commands
- Add `CommandInvoker.GetExecutedCommands()` method to retrieve command history
- Serialize commands to MergeRequestChanges

**Modified CommandInvoker.cs**:
```csharp
public class CommandInvoker
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    // NEW: Get commands executed since last clear
    public List<ICommand> GetExecutedCommands(int ontologyId)
    {
        return _undoStack
            .Where(c => c.OntologyId == ontologyId)
            .Reverse()
            .ToList();
    }

    // NEW: Clear command history (called after MR submission)
    public void ClearForOntology(int ontologyId)
    {
        var toRemove = _undoStack
            .Where(c => c.OntologyId == ontologyId)
            .ToList();

        foreach (var cmd in toRemove)
        {
            _undoStack.Pop(); // This is simplified; real implementation needs careful stack management
        }

        _redoStack.Clear();
    }
}
```

---

### 2. Version History Integration

**Integration**: Use existing `OntologyActivityService` for audit trail.

**MR-specific activities**:
- `merge_request_created`
- `merge_request_submitted`
- `merge_request_approved`
- `merge_request_rejected`
- `merge_request_cancelled`

**Activity tracking**:
```csharp
await _activityService.RecordActivityAsync(new OntologyActivity
{
    OntologyId = ontologyId,
    UserId = userId,
    ActorName = userName,
    ActivityType = "merge_request_approved",
    EntityType = "merge_request",
    EntityId = mergeRequestId,
    EntityName = mergeRequest.Title,
    Description = $"Approved {mergeRequest.TotalChanges} changes",
    Notes = reviewComment,
    VersionNumber = await _activityService.GetCurrentVersionNumberAsync(ontologyId) + 1
});
```

---

### 3. Permission System Integration

**Use existing `OntologyPermissionService`**:

```csharp
// Check if user can create MR
var canEdit = await _permissionService.CanEditAsync(ontologyId, userId);
if (!canEdit && !isApprovalMode)
    throw new UnauthorizedAccessException();

// Check if user can review MR
var canManage = await _permissionService.CanManageAsync(ontologyId, userId);
if (!canManage)
    throw new UnauthorizedAccessException();
```

**New permission check**:
```csharp
public async Task<bool> CanReviewMergeRequestAsync(int mergeRequestId, string userId)
{
    var mr = await _mergeRequestRepository.GetByIdAsync(mergeRequestId);
    if (mr == null) return false;

    // Cannot review own MR
    if (mr.SubmitterId == userId) return false;

    // Must have FullAccess
    return await _permissionService.CanManageAsync(mr.OntologyId, userId);
}
```

---

### 4. Notification System

**Create new `INotificationService`**:

```csharp
public interface INotificationService
{
    Task NotifyMergeRequestCreatedAsync(MergeRequest mergeRequest);
    Task NotifyMergeRequestApprovedAsync(MergeRequest mergeRequest);
    Task NotifyMergeRequestRejectedAsync(MergeRequest mergeRequest);
    Task NotifyCommentAddedAsync(MergeRequestComment comment);
}
```

**Implementation using existing ToastService + SignalR**:
```csharp
public class NotificationService : INotificationService
{
    private readonly IHubContext<MergeRequestHub> _hubContext;
    private readonly IEmailService _emailService; // Future

    public async Task NotifyMergeRequestCreatedAsync(MergeRequest mergeRequest)
    {
        // In-app notification via SignalR
        await _hubContext.Clients.Group($"ontology-{mergeRequest.OntologyId}")
            .SendAsync("MergeRequestCreated", MapToDto(mergeRequest));

        // Future: Email notification to reviewers
        // await _emailService.SendMergeRequestNotificationAsync(...);
    }
}
```

---

## Performance Considerations

### 1. Database Indexes

Critical indexes for query performance:

```sql
-- Merge request queries by status
CREATE INDEX IX_MergeRequests_OntologyId_Status
ON MergeRequests(OntologyId, Status)
INCLUDE (CreatedAt, SubmitterId);

-- Change queries
CREATE INDEX IX_MergeRequestChanges_MergeRequestId_SequenceNumber
ON MergeRequestChanges(MergeRequestId, SequenceNumber);

-- Pending count queries
CREATE INDEX IX_MergeRequests_OntologyId_Status_Count
ON MergeRequests(OntologyId, Status)
WHERE Status = 'Pending';
```

---

### 2. Caching Strategy

**Cache pending MR count**:
```csharp
private readonly IMemoryCache _cache;

public async Task<int> GetPendingCountAsync(int ontologyId)
{
    var cacheKey = $"mr-pending-count-{ontologyId}";

    if (_cache.TryGetValue(cacheKey, out int count))
        return count;

    count = await _mergeRequestRepository.GetPendingCountAsync(ontologyId);

    _cache.Set(cacheKey, count, TimeSpan.FromMinutes(5));

    return count;
}
```

**Invalidate cache on MR state change**:
```csharp
private void InvalidatePendingCount(int ontologyId)
{
    _cache.Remove($"mr-pending-count-{ontologyId}");
}
```

---

### 3. Pagination

All list queries must support pagination:

```csharp
public async Task<PagedResult<MergeRequestDto>> GetMergeRequestsAsync(
    int ontologyId,
    string? status = null,
    int skip = 0,
    int take = 25)
{
    var query = _context.MergeRequests
        .Where(mr => mr.OntologyId == ontologyId)
        .AsNoTracking();

    if (!string.IsNullOrEmpty(status))
        query = query.Where(mr => mr.Status == status);

    var total = await query.CountAsync();

    var items = await query
        .OrderByDescending(mr => mr.CreatedAt)
        .Skip(skip)
        .Take(take)
        .Select(mr => MapToDto(mr))
        .ToListAsync();

    return new PagedResult<MergeRequestDto>
    {
        Items = items,
        TotalCount = total,
        Skip = skip,
        Take = take
    };
}
```

---

### 4. Lazy Loading Changes

Don't load all changes by default:

```csharp
// Lightweight list view
var mergeRequests = await _context.MergeRequests
    .Where(mr => mr.OntologyId == ontologyId)
    .Select(mr => new MergeRequestDto
    {
        Id = mr.Id,
        Title = mr.Title,
        Status = mr.Status,
        TotalChanges = mr.TotalChanges,
        // Don't include Changes navigation property
    })
    .ToListAsync();

// Full detail view loads changes
var mergeRequest = await _context.MergeRequests
    .Include(mr => mr.Changes.OrderBy(c => c.SequenceNumber))
    .Include(mr => mr.Comments.OrderBy(c => c.CreatedAt))
    .FirstOrDefaultAsync(mr => mr.Id == mergeRequestId);
```

---

## Security Considerations

### 1. Authorization Checks

**Every operation must check permissions**:

```csharp
public async Task<MergeRequestDto> ApproveMergeRequestAsync(...)
{
    // 1. Load MR
    var mr = await _repository.GetByIdAsync(mergeRequestId);
    if (mr == null) throw new NotFoundException();

    // 2. Check permissions
    var canManage = await _permissionService.CanManageAsync(mr.OntologyId, reviewerId);
    if (!canManage) throw new UnauthorizedAccessException();

    // 3. Prevent self-approval
    if (mr.SubmitterId == reviewerId)
        throw new InvalidOperationException("Cannot approve own MR");

    // 4. Proceed with approval
    // ...
}
```

---

### 2. SQL Injection Prevention

Use parameterized queries (handled by EF Core):

```csharp
// GOOD: Parameterized
var mergeRequests = await _context.MergeRequests
    .Where(mr => mr.Title.Contains(searchTerm))
    .ToListAsync();

// BAD: String concatenation (never do this)
// var sql = $"SELECT * FROM MergeRequests WHERE Title LIKE '%{searchTerm}%'";
```

---

### 3. XSS Prevention in Markdown

Sanitize markdown before rendering:

```csharp
public class MarkdownSanitizer
{
    public static string Sanitize(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .DisableHtml() // Prevent raw HTML injection
            .UseAdvancedExtensions()
            .Build();

        return Markdown.ToHtml(markdown, pipeline);
    }
}
```

---

### 4. Audit Trail

Every MR action must be logged:

```csharp
private async Task LogMergeRequestAction(
    string action,
    MergeRequest mergeRequest,
    string userId)
{
    await _activityService.RecordActivityAsync(new OntologyActivity
    {
        OntologyId = mergeRequest.OntologyId,
        UserId = userId,
        ActivityType = action,
        EntityType = "merge_request",
        EntityId = mergeRequest.Id,
        EntityName = mergeRequest.Title,
        Description = $"{action}: {mergeRequest.Title}",
        IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString()
    });
}
```

---

## Error Handling

### Exception Types

```csharp
public class MergeRequestException : Exception
{
    public MergeRequestException(string message) : base(message) { }
}

public class MergeRequestConflictException : MergeRequestException
{
    public List<Conflict> Conflicts { get; }

    public MergeRequestConflictException(List<Conflict> conflicts)
        : base($"Merge request has {conflicts.Count} conflicts.")
    {
        Conflicts = conflicts;
    }
}

public class MergeRequestNotFoundException : MergeRequestException
{
    public int MergeRequestId { get; }

    public MergeRequestNotFoundException(int mergeRequestId)
        : base($"Merge request {mergeRequestId} not found.")
    {
        MergeRequestId = mergeRequestId;
    }
}
```

---

### Global Error Handling

```csharp
public class MergeRequestErrorHandler
{
    public static async Task<IResult> HandleExceptionAsync(Exception ex, ILogger logger)
    {
        logger.LogError(ex, "Merge request operation failed");

        return ex switch
        {
            MergeRequestNotFoundException notFound =>
                Results.NotFound(new { error = notFound.Message }),

            MergeRequestConflictException conflict =>
                Results.Conflict(new { error = conflict.Message, conflicts = conflict.Conflicts }),

            UnauthorizedAccessException unauthorized =>
                Results.Forbid(),

            ValidationException validation =>
                Results.BadRequest(new { error = validation.Message }),

            _ => Results.Problem("An unexpected error occurred.")
        };
    }
}
```

---

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public async Task CreateMergeRequest_WithValidChanges_ShouldSucceed()
{
    // Arrange
    var service = CreateService();
    var dto = new CreateMergeRequestDto
    {
        Title = "Add product concepts",
        Description = "Adding 3 new product types",
        SubmitForReview = true
    };

    // Act
    var result = await service.CreateMergeRequestAsync(1, dto, "user1", "User One");

    // Assert
    Assert.NotNull(result);
    Assert.Equal(MergeRequestStatus.Pending, result.Status);
    Assert.True(result.TotalChanges > 0);
}

[Fact]
public async Task ApproveMergeRequest_WhenConflictsExist_ShouldThrow()
{
    // Arrange
    var service = CreateService();
    // Create MR with conflicts

    // Act & Assert
    await Assert.ThrowsAsync<MergeRequestConflictException>(
        () => service.ApproveMergeRequestAsync(1, "reviewer1"));
}

[Fact]
public async Task ApproveMergeRequest_SelfApproval_ShouldThrow()
{
    // Arrange
    var service = CreateService();
    // Create MR by user1

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.ApproveMergeRequestAsync(1, "user1"));
}
```

---

### Integration Tests

```csharp
[Fact]
public async Task MergeRequestWorkflow_EndToEnd_ShouldSucceed()
{
    // Arrange
    using var context = CreateInMemoryContext();
    var service = CreateService(context);

    // 1. Create ontology with approval mode
    var ontology = CreateOntology(requiresApproval: true);

    // 2. User makes changes and creates MR
    var mrDto = await service.CreateMergeRequestAsync(
        ontology.Id,
        new CreateMergeRequestDto { Title = "Test MR" },
        "user1",
        "User One");

    Assert.Equal(MergeRequestStatus.Pending, mrDto.Status);

    // 3. Reviewer approves
    var approved = await service.ApproveMergeRequestAsync(mrDto.Id, "admin1");

    Assert.Equal(MergeRequestStatus.Approved, approved.Status);

    // 4. Verify changes applied to ontology
    var concepts = await context.Concepts
        .Where(c => c.OntologyId == ontology.Id)
        .ToListAsync();

    Assert.NotEmpty(concepts);
}
```

---

## Migration Strategy

### Phase 1: Database Schema

```bash
dotnet ef migrations add AddMergeRequestTables
dotnet ef database update
```

### Phase 2: Backwards Compatibility

Ensure existing functionality works:
- Ontologies without RequiresApproval flag work as before
- Command pattern undo/redo unchanged
- Activity tracking continues to work

### Phase 3: Data Migration (if needed)

No existing data needs migration. New tables start empty.

---

## Monitoring & Observability

### Key Metrics

```csharp
// Track MR operations
_logger.LogInformation(
    "MR {MergeRequestId} created for ontology {OntologyId} by {UserId}. Changes: {ChangeCount}",
    mergeRequest.Id, ontologyId, userId, changeCount);

// Track approvals
_logger.LogInformation(
    "MR {MergeRequestId} approved by {ReviewerId}. Duration: {Duration}ms",
    mergeRequestId, reviewerId, duration);

// Track conflicts
_logger.LogWarning(
    "MR {MergeRequestId} has {ConflictCount} conflicts detected",
    mergeRequestId, conflicts.Count);
```

### Application Insights Metrics

```csharp
private readonly TelemetryClient _telemetry;

_telemetry.TrackMetric("MergeRequest.Created", 1);
_telemetry.TrackMetric("MergeRequest.ApprovalTime", approvalTime.TotalHours);
_telemetry.TrackMetric("MergeRequest.ChangeCount", changeCount);
```

---

## Related Documents

- [Business Requirements](./requirements.md)
- [Implementation Plan](./implementation-plan.md)
- [UI Mockups](./ui-mockups.md)
- [Data Model](./data-model.md)

---

**Last Updated**: November 8, 2025
**Status**: Draft for Review
**Next Step**: Implementation Plan
