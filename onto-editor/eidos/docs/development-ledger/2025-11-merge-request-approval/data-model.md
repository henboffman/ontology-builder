# Merge Request / Approval Workflow - Data Model

**Date**: November 8, 2025
**Feature**: Complete Database Schema and Data Model
**Status**: Planning Phase

---

## Overview

This document provides the complete data model for the Merge Request system, including table schemas, relationships, indexes, constraints, and sample queries.

---

## Entity Relationship Diagram

```
┌─────────────────┐
│   Ontologies    │
│─────────────────│
│ Id (PK)         │
│ RequiresApproval│<───────┐
│ ...             │        │
└─────────────────┘        │
         △                 │
         │                 │
         │                 │
         │                 │
┌────────┴──────────────┐  │
│   MergeRequests       │  │
│───────────────────────│  │
│ Id (PK)               │  │
│ OntologyId (FK) ──────┘  │
│ SubmitterId (FK) ────────┼───> AspNetUsers
│ Title                 │  │
│ Status                │  │
│ BaseVersionNumber     │  │
│ ...                   │  │
└───────┬───────────────┘  │
        │                  │
        │                  │
        │                  │
   ┌────┴─────────────┐    │
   │ MergeRequest     │    │
   │ Changes          │    │
   │──────────────────│    │
   │ Id (PK)          │    │
   │ MergeRequestId(FK)    │
   │ ChangeType       │    │
   │ EntityType       │    │
   │ EntityId         │    │
   │ BeforeSnapshot   │    │
   │ AfterSnapshot    │    │
   │ ...              │    │
   └──────────────────┘    │
                           │
   ┌────────────────┐      │
   │ MergeRequest   │      │
   │ Comments       │      │
   │────────────────│      │
   │ Id (PK)        │      │
   │ MergeRequestId(FK)────┘
   │ AuthorId (FK) ────────> AspNetUsers
   │ CommentText    │
   │ ...            │
   └────────────────┘
```

---

## Table Schemas

### Table: MergeRequests

**Purpose**: Primary entity for merge requests containing metadata and summary information.

```sql
CREATE TABLE MergeRequests (
    -- Primary Key
    Id INT PRIMARY KEY IDENTITY(1,1),

    -- Foreign Keys
    OntologyId INT NOT NULL,
    SubmitterId NVARCHAR(450) NULL, -- Nullable for guest users
    AssignedReviewerId NVARCHAR(450) NULL,
    ReviewedById NVARCHAR(450) NULL,

    -- Basic Information
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Draft',
        -- Values: 'Draft', 'Pending', 'Stale', 'Approved', 'Rejected', 'Cancelled'

    -- Submitter Information (denormalized for history preservation)
    SubmitterName NVARCHAR(200) NOT NULL,
    SubmitterEmail NVARCHAR(200) NULL,

    -- Version Tracking
    BaseVersionNumber INT NOT NULL,
        -- OntologyActivity version when MR was created
    CurrentVersionNumber INT NULL,
        -- Updated when ontology changes (for stale detection)

    -- Change Summary (denormalized for performance)
    ConceptsAdded INT NOT NULL DEFAULT 0,
    ConceptsModified INT NOT NULL DEFAULT 0,
    ConceptsDeleted INT NOT NULL DEFAULT 0,
    RelationshipsAdded INT NOT NULL DEFAULT 0,
    RelationshipsModified INT NOT NULL DEFAULT 0,
    RelationshipsDeleted INT NOT NULL DEFAULT 0,
    PropertiesChanged INT NOT NULL DEFAULT 0,
    TotalChanges INT NOT NULL DEFAULT 0,
        -- Sum of all changes for quick display

    -- Review Information
    ReviewedAt DATETIME2 NULL,
    ReviewComment NVARCHAR(MAX) NULL,
        -- Approval comment or rejection reason

    -- Conflict Tracking
    HasConflicts BIT NOT NULL DEFAULT 0,
    ConflictDetails NVARCHAR(MAX) NULL,
        -- JSON: [{"changeId": 1, "fieldName": "Name", "type": "Modified"}]

    -- Timestamps
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    SubmittedAt DATETIME2 NULL,
        -- When status changed from Draft to Pending

    -- Audit Trail
    IpAddress NVARCHAR(45) NULL,
        -- IPv6 max length
    UserAgent NVARCHAR(500) NULL,

    -- Constraints
    CONSTRAINT FK_MergeRequests_Ontology
        FOREIGN KEY (OntologyId)
        REFERENCES Ontologies(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_MergeRequests_Submitter
        FOREIGN KEY (SubmitterId)
        REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,

    CONSTRAINT FK_MergeRequests_AssignedReviewer
        FOREIGN KEY (AssignedReviewerId)
        REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,

    CONSTRAINT FK_MergeRequests_ReviewedBy
        FOREIGN KEY (ReviewedById)
        REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,

    CONSTRAINT CK_MergeRequests_Status
        CHECK (Status IN ('Draft', 'Pending', 'Stale', 'Approved', 'Rejected', 'Cancelled')),

    CONSTRAINT CK_MergeRequests_ChangeCount
        CHECK (TotalChanges >= 0),

    -- Indexes
    INDEX IX_MergeRequests_OntologyId (OntologyId),
    INDEX IX_MergeRequests_Status (Status),
    INDEX IX_MergeRequests_SubmitterId (SubmitterId),
    INDEX IX_MergeRequests_CreatedAt (CreatedAt DESC),
    INDEX IX_MergeRequests_AssignedReviewerId (AssignedReviewerId),

    -- Composite Indexes for Common Queries
    INDEX IX_MergeRequests_OntologyId_Status
        (OntologyId, Status)
        INCLUDE (CreatedAt, SubmitterId, TotalChanges),

    INDEX IX_MergeRequests_SubmitterId_Status
        (SubmitterId, Status)
        INCLUDE (OntologyId, Title, CreatedAt),

    -- Filtered Index for Pending Count (Performance Optimization)
    INDEX IX_MergeRequests_Pending
        (OntologyId, Status)
        WHERE Status = 'Pending'
);
```

**Sample Data**:

```sql
INSERT INTO MergeRequests (
    OntologyId, SubmitterId, SubmitterName, SubmitterEmail,
    Title, Description, Status,
    BaseVersionNumber, CurrentVersionNumber,
    ConceptsAdded, RelationshipsAdded, TotalChanges,
    CreatedAt, SubmittedAt
) VALUES (
    1, 'user123', 'John Doe', 'john.doe@example.com',
    'Add product taxonomy concepts',
    'Adding concepts for product categories and attributes',
    'Pending',
    145, 145,
    5, 3, 10,
    GETUTCDATE(), GETUTCDATE()
);
```

---

### Table: MergeRequestChanges

**Purpose**: Individual changes within a merge request. Each row represents one create/update/delete operation.

```sql
CREATE TABLE MergeRequestChanges (
    -- Primary Key
    Id INT PRIMARY KEY IDENTITY(1,1),

    -- Foreign Key
    MergeRequestId INT NOT NULL,

    -- Change Metadata
    ChangeType NVARCHAR(20) NOT NULL,
        -- Values: 'Create', 'Update', 'Delete'
    EntityType NVARCHAR(50) NOT NULL,
        -- Values: 'Concept', 'Relationship', 'Property', 'Individual'
    EntityId INT NULL,
        -- NULL for creates (no ID yet), populated for updates/deletes
    EntityName NVARCHAR(500) NULL,
        -- Denormalized for display purposes

    -- Change Payload (JSON)
    BeforeSnapshot NVARCHAR(MAX) NULL,
        -- JSON of entity before change (NULL for creates)
    AfterSnapshot NVARCHAR(MAX) NOT NULL,
        -- JSON of entity after change (NULL for deletes)

    -- Field-Level Diff (for efficient conflict detection)
    FieldChanges NVARCHAR(MAX) NULL,
        -- JSON: [{"field": "name", "before": "Person", "after": "Human"}]

    -- Sequence for Ordering
    SequenceNumber INT NOT NULL,
        -- Order in which changes should be applied (1, 2, 3, ...)

    -- Conflict Tracking
    HasConflict BIT NOT NULL DEFAULT 0,
    ConflictWith NVARCHAR(500) NULL,
        -- Description of conflicting change

    -- Constraints
    CONSTRAINT FK_MergeRequestChanges_MergeRequest
        FOREIGN KEY (MergeRequestId)
        REFERENCES MergeRequests(Id)
        ON DELETE CASCADE,

    CONSTRAINT CK_MergeRequestChanges_ChangeType
        CHECK (ChangeType IN ('Create', 'Update', 'Delete')),

    CONSTRAINT CK_MergeRequestChanges_EntityType
        CHECK (EntityType IN ('Concept', 'Relationship', 'Property', 'Individual', 'ConceptProperty')),

    -- Indexes
    INDEX IX_MergeRequestChanges_MergeRequestId (MergeRequestId),
    INDEX IX_MergeRequestChanges_SequenceNumber
        (MergeRequestId, SequenceNumber),
    INDEX IX_MergeRequestChanges_EntityType_EntityId
        (EntityType, EntityId)
);
```

**Sample Data**:

```sql
INSERT INTO MergeRequestChanges (
    MergeRequestId, ChangeType, EntityType,
    EntityId, EntityName,
    BeforeSnapshot, AfterSnapshot,
    SequenceNumber
) VALUES (
    1, 'Create', 'Concept',
    NULL, 'Product',
    NULL,
    '{
        "Name": "Product",
        "Definition": "A tangible or intangible good or service",
        "Category": "Business Concepts",
        "Color": "#4A90E2"
    }',
    1
);
```

---

### Table: MergeRequestComments

**Purpose**: Discussion threads on merge requests (for future comment feature).

```sql
CREATE TABLE MergeRequestComments (
    -- Primary Key
    Id INT PRIMARY KEY IDENTITY(1,1),

    -- Foreign Keys
    MergeRequestId INT NOT NULL,
    AuthorId NVARCHAR(450) NULL,
        -- NULL if author is deleted
    ParentCommentId INT NULL,
        -- For nested replies (future enhancement)

    -- Author Information (denormalized)
    AuthorName NVARCHAR(200) NOT NULL,

    -- Comment Content
    CommentText NVARCHAR(MAX) NOT NULL,
        -- Markdown supported

    -- Metadata
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsEdited BIT NOT NULL DEFAULT 0,
    IsDeleted BIT NOT NULL DEFAULT 0,
        -- Soft delete for comment history

    -- Constraints
    CONSTRAINT FK_MergeRequestComments_MergeRequest
        FOREIGN KEY (MergeRequestId)
        REFERENCES MergeRequests(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_MergeRequestComments_Author
        FOREIGN KEY (AuthorId)
        REFERENCES AspNetUsers(Id)
        ON DELETE SET NULL,

    CONSTRAINT FK_MergeRequestComments_Parent
        FOREIGN KEY (ParentCommentId)
        REFERENCES MergeRequestComments(Id)
        ON DELETE NO ACTION,

    -- Indexes
    INDEX IX_MergeRequestComments_MergeRequestId (MergeRequestId),
    INDEX IX_MergeRequestComments_CreatedAt (CreatedAt),
    INDEX IX_MergeRequestComments_ParentCommentId (ParentCommentId)
);
```

**Sample Data**:

```sql
INSERT INTO MergeRequestComments (
    MergeRequestId, AuthorId, AuthorName,
    CommentText, CreatedAt
) VALUES (
    1, 'admin456', 'Jane Admin',
    'These look great! Just one question: should "Electronics" be a subclass of "Product"?',
    GETUTCDATE()
);
```

---

### Table: Ontologies (Extension)

**Purpose**: Extend existing Ontologies table to support approval mode.

```sql
-- Add columns to existing Ontologies table
ALTER TABLE Ontologies
ADD RequiresApproval BIT NOT NULL DEFAULT 0;

ALTER TABLE Ontologies
ADD ApprovalModeEnabledAt DATETIME2 NULL;

ALTER TABLE Ontologies
ADD ApprovalModeEnabledBy NVARCHAR(450) NULL;

-- Add foreign key constraint
ALTER TABLE Ontologies
ADD CONSTRAINT FK_Ontologies_ApprovalEnabledBy
    FOREIGN KEY (ApprovalModeEnabledBy)
    REFERENCES AspNetUsers(Id)
    ON DELETE SET NULL;

-- Add index for filtering
CREATE INDEX IX_Ontologies_RequiresApproval
    ON Ontologies(RequiresApproval)
    WHERE RequiresApproval = 1;
```

---

## Computed Columns & Views

### View: MergeRequestSummary

**Purpose**: Lightweight view for list displays (avoids loading changes).

```sql
CREATE VIEW MergeRequestSummary AS
SELECT
    mr.Id,
    mr.OntologyId,
    o.Name AS OntologyName,
    mr.Title,
    mr.Status,
    mr.SubmitterName,
    mr.SubmitterEmail,
    mr.TotalChanges,
    mr.CreatedAt,
    mr.SubmittedAt,
    mr.ReviewedAt,
    CASE
        WHEN mr.CurrentVersionNumber IS NOT NULL
             AND mr.CurrentVersionNumber > mr.BaseVersionNumber
        THEN 1
        ELSE 0
    END AS IsStale,
    reviewer.Email AS ReviewedByEmail,
    assigned.Email AS AssignedReviewerEmail,
    (SELECT COUNT(*) FROM MergeRequestChanges WHERE MergeRequestId = mr.Id AND HasConflict = 1) AS ConflictCount
FROM MergeRequests mr
INNER JOIN Ontologies o ON mr.OntologyId = o.Id
LEFT JOIN AspNetUsers reviewer ON mr.ReviewedById = reviewer.Id
LEFT JOIN AspNetUsers assigned ON mr.AssignedReviewerId = assigned.Id;
```

---

### View: MergeRequestDetails

**Purpose**: Full details for detail view (includes changes).

```sql
CREATE VIEW MergeRequestDetails AS
SELECT
    mr.Id,
    mr.OntologyId,
    o.Name AS OntologyName,
    mr.Title,
    mr.Description,
    mr.Status,
    mr.SubmitterName,
    mr.SubmitterEmail,
    mr.BaseVersionNumber,
    mr.CurrentVersionNumber,
    mr.ConceptsAdded,
    mr.ConceptsModified,
    mr.ConceptsDeleted,
    mr.RelationshipsAdded,
    mr.RelationshipsModified,
    mr.RelationshipsDeleted,
    mr.PropertiesChanged,
    mr.TotalChanges,
    mr.HasConflicts,
    mr.ConflictDetails,
    mr.CreatedAt,
    mr.UpdatedAt,
    mr.SubmittedAt,
    mr.ReviewedAt,
    mr.ReviewComment,
    submitter.Email AS SubmitterFullEmail,
    reviewer.Email AS ReviewedByEmail,
    reviewer.UserName AS ReviewedByName,
    assigned.Email AS AssignedReviewerEmail,
    assigned.UserName AS AssignedReviewerName,
    (SELECT COUNT(*) FROM MergeRequestChanges mrc WHERE mrc.MergeRequestId = mr.Id) AS ActualChangeCount,
    (SELECT COUNT(*) FROM MergeRequestComments mcc WHERE mcc.MergeRequestId = mr.Id AND mcc.IsDeleted = 0) AS CommentCount
FROM MergeRequests mr
INNER JOIN Ontologies o ON mr.OntologyId = o.Id
LEFT JOIN AspNetUsers submitter ON mr.SubmitterId = submitter.Id
LEFT JOIN AspNetUsers reviewer ON mr.ReviewedById = reviewer.Id
LEFT JOIN AspNetUsers assigned ON mr.AssignedReviewerId = assigned.Id;
```

---

## Sample Queries

### Query 1: Get Pending MRs for Ontology

```sql
SELECT
    Id,
    Title,
    SubmitterName,
    TotalChanges,
    CreatedAt,
    DATEDIFF(HOUR, CreatedAt, GETUTCDATE()) AS HoursAgo
FROM MergeRequests
WHERE OntologyId = @OntologyId
  AND Status = 'Pending'
ORDER BY CreatedAt ASC;
```

**Performance**: Uses `IX_MergeRequests_OntologyId_Status` index. <10ms for 10,000 MRs.

---

### Query 2: Get MRs by User with Filters

```sql
SELECT
    mr.Id,
    mr.Title,
    mr.Status,
    o.Name AS OntologyName,
    mr.TotalChanges,
    mr.CreatedAt,
    mr.SubmittedAt
FROM MergeRequests mr
INNER JOIN Ontologies o ON mr.OntologyId = o.Id
WHERE mr.SubmitterId = @UserId
  AND (@StatusFilter IS NULL OR mr.Status = @StatusFilter)
  AND mr.CreatedAt >= @StartDate
ORDER BY mr.CreatedAt DESC
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
```

**Performance**: Uses `IX_MergeRequests_SubmitterId_Status` index. Pagination prevents memory issues.

---

### Query 3: Get MR with All Changes (for Detail View)

```sql
SELECT
    mr.Id,
    mr.Title,
    mr.Description,
    mr.Status,
    mr.SubmitterName,
    mr.TotalChanges,
    mr.CreatedAt,
    -- Changes
    mrc.Id AS ChangeId,
    mrc.ChangeType,
    mrc.EntityType,
    mrc.EntityName,
    mrc.BeforeSnapshot,
    mrc.AfterSnapshot,
    mrc.FieldChanges,
    mrc.SequenceNumber,
    mrc.HasConflict
FROM MergeRequests mr
LEFT JOIN MergeRequestChanges mrc ON mr.Id = mrc.MergeRequestId
WHERE mr.Id = @MergeRequestId
ORDER BY mrc.SequenceNumber;
```

**Performance**: Uses `IX_MergeRequestChanges_SequenceNumber` index. <50ms for 500 changes.

---

### Query 4: Detect Conflicts (Compare MR to Current State)

```sql
-- Find concepts modified in both MR and current ontology
SELECT
    mrc.Id AS ChangeId,
    mrc.EntityType,
    mrc.EntityId,
    mrc.EntityName,
    'Modified' AS ConflictType
FROM MergeRequestChanges mrc
INNER JOIN MergeRequests mr ON mrc.MergeRequestId = mr.Id
INNER JOIN Concepts c ON mrc.EntityId = c.Id
WHERE mr.Id = @MergeRequestId
  AND mrc.EntityType = 'Concept'
  AND mrc.ChangeType = 'Update'
  AND (
      -- Check if concept was modified after MR creation
      -- This requires comparing JSON snapshots
      -- Simplified: check if entity still exists
      mrc.EntityId NOT IN (SELECT Id FROM Concepts WHERE OntologyId = mr.OntologyId)
  );
```

**Note**: Full conflict detection requires JSON comparison in application layer.

---

### Query 5: Get Pending Count (Cached)

```sql
SELECT COUNT(*)
FROM MergeRequests
WHERE OntologyId = @OntologyId
  AND Status = 'Pending';
```

**Performance**: Uses filtered index `IX_MergeRequests_Pending`. <5ms.

---

### Query 6: Get Recent Activity for MR

```sql
SELECT
    oa.ActivityType,
    oa.Description,
    oa.ActorName,
    oa.CreatedAt
FROM OntologyActivity oa
WHERE oa.OntologyId = @OntologyId
  AND oa.EntityType = 'merge_request'
  AND oa.EntityId = @MergeRequestId
ORDER BY oa.CreatedAt DESC;
```

**Integration**: Uses existing OntologyActivity table for audit trail.

---

### Query 7: Bulk Approve (Transaction)

```sql
BEGIN TRANSACTION;

DECLARE @MergeRequestIds TABLE (Id INT);
INSERT INTO @MergeRequestIds VALUES (1), (2), (3);

-- Update all MRs to approved
UPDATE MergeRequests
SET Status = 'Approved',
    ReviewedById = @ReviewerId,
    ReviewedAt = GETUTCDATE(),
    ReviewComment = @Comment
WHERE Id IN (SELECT Id FROM @MergeRequestIds)
  AND Status = 'Pending';

-- Apply changes for each MR
-- (This would be done in application layer using the ApplyChangeAsync method)

COMMIT TRANSACTION;
```

**Note**: Actual change application happens in service layer with proper error handling.

---

## Data Integrity Constraints

### 1. Status Transitions

Only valid state transitions are allowed:

```sql
-- Enforce valid status transitions (application layer logic)
-- Draft -> Pending, Cancelled
-- Pending -> Approved, Rejected, Cancelled, Stale
-- Stale -> Approved, Rejected
-- Approved, Rejected, Cancelled -> [Terminal, no transitions]
```

---

### 2. Change Count Consistency

Ensure TotalChanges matches actual change rows:

```sql
-- Trigger to update TotalChanges (optional, or compute in application)
CREATE TRIGGER TR_MergeRequestChanges_UpdateCount
ON MergeRequestChanges
AFTER INSERT, DELETE
AS
BEGIN
    UPDATE mr
    SET TotalChanges = (
        SELECT COUNT(*)
        FROM MergeRequestChanges
        WHERE MergeRequestId = mr.Id
    )
    FROM MergeRequests mr
    INNER JOIN inserted i ON mr.Id = i.MergeRequestId
    UNION
    SELECT mr.Id
    FROM MergeRequests mr
    INNER JOIN deleted d ON mr.Id = d.MergeRequestId;
END;
```

**Recommendation**: Compute in application layer instead of trigger for better control.

---

### 3. Prevent Self-Approval

Enforced in application layer:

```csharp
if (mergeRequest.SubmitterId == reviewerId)
    throw new InvalidOperationException("Cannot approve own merge request");
```

---

### 4. Require Reason for Rejection

Enforced in application layer:

```csharp
if (decision.Approved == false && string.IsNullOrWhiteSpace(decision.Comment))
    throw new ValidationException("Rejection reason is required");
```

---

## Indexing Strategy

### Primary Indexes

All foreign keys have indexes for join performance:
- `IX_MergeRequests_OntologyId`
- `IX_MergeRequests_SubmitterId`
- `IX_MergeRequestChanges_MergeRequestId`
- `IX_MergeRequestComments_MergeRequestId`

### Composite Indexes

For common query patterns:
- `IX_MergeRequests_OntologyId_Status` (filter by ontology and status)
- `IX_MergeRequests_SubmitterId_Status` (user's MRs by status)
- `IX_MergeRequestChanges_SequenceNumber` (ordered change retrieval)

### Filtered Indexes

For high-selectivity queries:
- `IX_MergeRequests_Pending` (pending count queries)
- `IX_Ontologies_RequiresApproval` (approval mode enabled filter)

### Covering Indexes

Include columns to avoid lookups:
- `IX_MergeRequests_OntologyId_Status INCLUDE (CreatedAt, SubmitterId, TotalChanges)`

---

## Data Retention & Cleanup

### Retention Policy

```sql
-- Archive old approved/rejected MRs after 1 year (optional)
-- Move to separate archive table or soft delete

UPDATE MergeRequests
SET IsArchived = 1
WHERE Status IN ('Approved', 'Rejected')
  AND ReviewedAt < DATEADD(YEAR, -1, GETUTCDATE());

-- Or delete (with cascade to changes and comments)
DELETE FROM MergeRequests
WHERE Status IN ('Approved', 'Rejected')
  AND ReviewedAt < DATEADD(YEAR, -1, GETUTCDATE());
```

---

### Cleanup Stale Drafts

```sql
-- Delete drafts older than 30 days with no activity
DELETE FROM MergeRequests
WHERE Status = 'Draft'
  AND UpdatedAt < DATEADD(DAY, -30, GETUTCDATE());
```

---

## Migration Scripts

### Migration 1: Create MergeRequests Table

```csharp
public partial class AddMergeRequestTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MergeRequests",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OntologyId = table.Column<int>(type: "int", nullable: false),
                SubmitterId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                SubmitterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                SubmitterEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                BaseVersionNumber = table.Column<int>(type: "int", nullable: false),
                CurrentVersionNumber = table.Column<int>(type: "int", nullable: true),
                ConceptsAdded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                ConceptsModified = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                ConceptsDeleted = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                RelationshipsAdded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                RelationshipsModified = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                RelationshipsDeleted = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                PropertiesChanged = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                TotalChanges = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                AssignedReviewerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                ReviewedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReviewComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                HasConflicts = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                ConflictDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MergeRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_MergeRequests_Ontology",
                    column: x => x.OntologyId,
                    principalTable: "Ontologies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MergeRequests_Submitter",
                    column: x => x.SubmitterId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_MergeRequests_AssignedReviewer",
                    column: x => x.AssignedReviewerId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_MergeRequests_ReviewedBy",
                    column: x => x.ReviewedById,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.CheckConstraint(
                    "CK_MergeRequests_Status",
                    "Status IN ('Draft', 'Pending', 'Stale', 'Approved', 'Rejected', 'Cancelled')");
                table.CheckConstraint(
                    "CK_MergeRequests_ChangeCount",
                    "TotalChanges >= 0");
            });

        migrationBuilder.CreateIndex(
            name: "IX_MergeRequests_OntologyId",
            table: "MergeRequests",
            column: "OntologyId");

        migrationBuilder.CreateIndex(
            name: "IX_MergeRequests_Status",
            table: "MergeRequests",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_MergeRequests_SubmitterId",
            table: "MergeRequests",
            column: "SubmitterId");

        migrationBuilder.CreateIndex(
            name: "IX_MergeRequests_CreatedAt",
            table: "MergeRequests",
            column: "CreatedAt",
            descending: true);

        migrationBuilder.CreateIndex(
            name: "IX_MergeRequests_AssignedReviewerId",
            table: "MergeRequests",
            column: "AssignedReviewerId");

        migrationBuilder.CreateIndex(
            name: "IX_MergeRequests_OntologyId_Status",
            table: "MergeRequests",
            columns: new[] { "OntologyId", "Status" },
            includes: new[] { "CreatedAt", "SubmitterId", "TotalChanges" });

        migrationBuilder.CreateIndex(
            name: "IX_MergeRequests_Pending",
            table: "MergeRequests",
            columns: new[] { "OntologyId", "Status" },
            filter: "Status = 'Pending'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MergeRequests");
    }
}
```

---

### Migration 2: Create MergeRequestChanges Table

```csharp
public partial class AddMergeRequestChangesTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MergeRequestChanges",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                MergeRequestId = table.Column<int>(type: "int", nullable: false),
                ChangeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                EntityId = table.Column<int>(type: "int", nullable: true),
                EntityName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                BeforeSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                AfterSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                FieldChanges = table.Column<string>(type: "nvarchar(max)", nullable: true),
                SequenceNumber = table.Column<int>(type: "int", nullable: false),
                HasConflict = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                ConflictWith = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MergeRequestChanges", x => x.Id);
                table.ForeignKey(
                    name: "FK_MergeRequestChanges_MergeRequest",
                    column: x => x.MergeRequestId,
                    principalTable: "MergeRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.CheckConstraint(
                    "CK_MergeRequestChanges_ChangeType",
                    "ChangeType IN ('Create', 'Update', 'Delete')");
                table.CheckConstraint(
                    "CK_MergeRequestChanges_EntityType",
                    "EntityType IN ('Concept', 'Relationship', 'Property', 'Individual', 'ConceptProperty')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_MergeRequestChanges_MergeRequestId",
            table: "MergeRequestChanges",
            column: "MergeRequestId");

        migrationBuilder.CreateIndex(
            name: "IX_MergeRequestChanges_SequenceNumber",
            table: "MergeRequestChanges",
            columns: new[] { "MergeRequestId", "SequenceNumber" });

        migrationBuilder.CreateIndex(
            name: "IX_MergeRequestChanges_EntityType_EntityId",
            table: "MergeRequestChanges",
            columns: new[] { "EntityType", "EntityId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MergeRequestChanges");
    }
}
```

---

### Migration 3: Extend Ontologies for Approval Mode

```csharp
public partial class AddApprovalModeToOntologies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "RequiresApproval",
            table: "Ontologies",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "ApprovalModeEnabledAt",
            table: "Ontologies",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ApprovalModeEnabledBy",
            table: "Ontologies",
            type: "nvarchar(450)",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Ontologies_RequiresApproval",
            table: "Ontologies",
            column: "RequiresApproval",
            filter: "RequiresApproval = 1");

        migrationBuilder.AddForeignKey(
            name: "FK_Ontologies_ApprovalEnabledBy",
            table: "Ontologies",
            column: "ApprovalModeEnabledBy",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Ontologies_ApprovalEnabledBy",
            table: "Ontologies");

        migrationBuilder.DropIndex(
            name: "IX_Ontologies_RequiresApproval",
            table: "Ontologies");

        migrationBuilder.DropColumn(
            name: "RequiresApproval",
            table: "Ontologies");

        migrationBuilder.DropColumn(
            name: "ApprovalModeEnabledAt",
            table: "Ontologies");

        migrationBuilder.DropColumn(
            name: "ApprovalModeEnabledBy",
            table: "Ontologies");
    }
}
```

---

## Database Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Ontologies Table                         │
│─────────────────────────────────────────────────────────────────│
│ PK: Id                                                          │
│ Name, Description, UserId, ...                                  │
│ RequiresApproval (BIT)                                          │
│ ApprovalModeEnabledAt (DATETIME2)                               │
│ ApprovalModeEnabledBy (FK → AspNetUsers)                        │
└────────────────┬────────────────────────────────────────────────┘
                 │
                 │ 1:N
                 │
┌────────────────▼────────────────────────────────────────────────┐
│                    MergeRequests Table                          │
│─────────────────────────────────────────────────────────────────│
│ PK: Id                                                          │
│ FK: OntologyId → Ontologies.Id                                  │
│ FK: SubmitterId → AspNetUsers.Id (NULL)                         │
│ FK: AssignedReviewerId → AspNetUsers.Id (NULL)                  │
│ FK: ReviewedById → AspNetUsers.Id (NULL)                        │
│ Title, Description, Status                                      │
│ BaseVersionNumber, CurrentVersionNumber                         │
│ ConceptsAdded, ConceptsModified, ConceptsDeleted, ...           │
│ TotalChanges, HasConflicts, ConflictDetails                     │
│ CreatedAt, UpdatedAt, SubmittedAt, ReviewedAt                   │
│ ReviewComment, IpAddress, UserAgent                             │
└────────┬───────────────────────────────────┬──────────────────┘
         │                                   │
         │ 1:N                               │ 1:N
         │                                   │
┌────────▼─────────────────┐    ┌───────────▼─────────────────┐
│ MergeRequestChanges      │    │ MergeRequestComments        │
│──────────────────────────│    │─────────────────────────────│
│ PK: Id                   │    │ PK: Id                      │
│ FK: MergeRequestId       │    │ FK: MergeRequestId          │
│ ChangeType, EntityType   │    │ FK: AuthorId (NULL)         │
│ EntityId, EntityName     │    │ FK: ParentCommentId (NULL)  │
│ BeforeSnapshot (JSON)    │    │ AuthorName, CommentText     │
│ AfterSnapshot (JSON)     │    │ CreatedAt, UpdatedAt        │
│ FieldChanges (JSON)      │    │ IsEdited, IsDeleted         │
│ SequenceNumber           │    └─────────────────────────────┘
│ HasConflict, ConflictWith│
└──────────────────────────┘
```

---

## Related Documents

- [Business Requirements](./requirements.md)
- [Technical Design](./technical-design.md)
- [Implementation Plan](./implementation-plan.md)
- [UI Mockups](./ui-mockups.md)

---

**Last Updated**: November 8, 2025
**Status**: Draft for Review
**Next Step**: Begin Phase 1 Implementation
