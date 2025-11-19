# Shared With Me - Implementation Guide

## Overview

This document provides technical implementation details for the "Shared with Me" feature, including database schema, service layer, API endpoints, and integration points.

---

## Database Schema

### SharedOntologyUserState Table

Stores user-specific state for shared ontologies (pin/hide/dismiss).

```sql
CREATE TABLE SharedOntologyUserStates (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL,
    OntologyId INT NOT NULL,
    IsPinned BIT NOT NULL DEFAULT 0,
    IsHidden BIT NOT NULL DEFAULT 0,
    IsDismissed BIT NOT NULL DEFAULT 0,
    PinnedAt DATETIME2 NULL,
    HiddenAt DATETIME2 NULL,
    DismissedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_SharedOntologyUserStates_Users
        FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SharedOntologyUserStates_Ontologies
        FOREIGN KEY (OntologyId) REFERENCES Ontologies(Id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IX_SharedOntologyUserState_UserId ON SharedOntologyUserStates(UserId);
CREATE INDEX IX_SharedOntologyUserState_OntologyId ON SharedOntologyUserStates(OntologyId);
CREATE UNIQUE INDEX IX_SharedOntologyUserState_UserId_OntologyId
    ON SharedOntologyUserStates(UserId, OntologyId);
CREATE INDEX IX_SharedOntologyUserState_UserId_IsPinned
    ON SharedOntologyUserStates(UserId, IsPinned);
```

### OntologyGroupPermission Changes

Added `LastAccessedAt` field to track group-based ontology access.

```sql
ALTER TABLE OntologyGroupPermissions
ADD LastAccessedAt DATETIME2 NULL;
```

---

## Data Models

### SharedOntologyUserState.cs

**Location:** `Models/SharedOntologyUserState.cs`

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace Eidos.Models
{
    /// <summary>
    /// User-specific state for shared ontologies (pin/hide/dismiss)
    /// Separate from access permissions to allow state persistence across access changes
    /// </summary>
    public class SharedOntologyUserState
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public int OntologyId { get; set; }
        public Ontology Ontology { get; set; } = null!;

        /// <summary>
        /// User has pinned this ontology for quick access
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// User has hidden this ontology from default view
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// User has dismissed this ontology (soft delete)
        /// </summary>
        public bool IsDismissed { get; set; }

        public DateTime? PinnedAt { get; set; }
        public DateTime? HiddenAt { get; set; }
        public DateTime? DismissedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

### SharedOntologyInfo.cs (DTO)

**Location:** `Models/DTOs/SharedOntologyInfo.cs`

```csharp
namespace Eidos.Models.DTOs
{
    /// <summary>
    /// Complete information about a shared ontology for display
    /// Combines data from UserShareAccess, OntologyGroupPermission, and SharedOntologyUserState
    /// </summary>
    public class SharedOntologyInfo
    {
        // Ontology identity
        public int OntologyId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        // Owner information
        public string OwnerId { get; set; } = null!;
        public string OwnerName { get; set; } = null!;
        public string? OwnerPhotoUrl { get; set; }

        // Access metadata
        public SharedAccessType AccessType { get; set; }
        public PermissionLevel PermissionLevel { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? LastViewedAt { get; set; }
        public int ViewCount { get; set; }

        // Ontology metadata
        public int ConceptCount { get; set; }
        public int RelationshipCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // User state
        public bool IsPinned { get; set; }
        public bool IsHidden { get; set; }
        public DateTime? PinnedAt { get; set; }

        // Collaboration metadata (if from group)
        public string? GroupName { get; set; }
        public int? GroupMemberCount { get; set; }
    }

    /// <summary>
    /// How the user gained access to this ontology
    /// </summary>
    public enum SharedAccessType
    {
        ShareLink,  // Accessed via share link
        Group,      // Accessed via group membership
        Both        // User has both types of access (deduplicated)
    }

    /// <summary>
    /// Sorting options for shared ontologies
    /// </summary>
    public enum SharedOntologySortBy
    {
        LastAccessed,   // Most recent first (default)
        LastViewed,     // Based on OntologyViewHistory
        Name,           // Alphabetical A-Z
        ConceptCount,   // Largest ontologies first
        PinnedFirst     // Pinned at top, then by LastAccessed
    }

    /// <summary>
    /// Filter options for shared ontology queries
    /// </summary>
    public class SharedOntologyFilter
    {
        public bool IncludeHidden { get; set; } = false;
        public bool PinnedOnly { get; set; } = false;
        public SharedAccessType? AccessTypeFilter { get; set; } = null;
        public int DaysBack { get; set; } = 90;
        public string? SearchTerm { get; set; }
        public SharedOntologySortBy SortBy { get; set; } = SharedOntologySortBy.LastAccessed;

        /// <summary>
        /// Generate cache key hash for this filter
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                IncludeHidden,
                PinnedOnly,
                AccessTypeFilter,
                DaysBack,
                SearchTerm,
                SortBy
            );
        }
    }

    /// <summary>
    /// Paginated result set for shared ontologies
    /// </summary>
    public class SharedOntologyResult
    {
        public List<SharedOntologyInfo> Ontologies { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
```

---

## Service Layer

### SharedOntologyService.cs

**Location:** `Services/SharedOntologyService.cs`

**Key Methods:**

#### GetSharedOntologiesAsync

Main query method that retrieves and filters shared ontologies.

```csharp
public async Task<SharedOntologyResult> GetSharedOntologiesAsync(
    string userId,
    SharedOntologyFilter filter,
    int page = 1,
    int pageSize = 24)
```

**Algorithm:**

1. Check cache for existing results
2. Query share link ontologies from `UserShareAccesses`
3. Query group-based ontologies from `UserGroupMembers` → `OntologyGroupPermissions`
4. Materialize both queries separately (type compatibility)
5. Convert group permission strings to enums
6. Combine results in memory
7. Deduplicate by `OntologyId` (take highest permission)
8. Load user state from `SharedOntologyUserStates`
9. Apply filters (hidden, pinned, search, days back)
10. Apply sorting
11. Paginate results
12. Cache and return

**Complexity:** O(n log n) where n = number of shared ontologies (dominated by sorting)

#### State Management Methods

```csharp
// Pin/unpin ontology
Task PinOntologyAsync(string userId, int ontologyId)
Task UnpinOntologyAsync(string userId, int ontologyId)

// Hide/unhide ontology
Task HideOntologyAsync(string userId, int ontologyId)
Task UnhideOntologyAsync(string userId, int ontologyId)

// Dismiss ontology (soft delete from shared list)
Task DismissOntologyAsync(string userId, int ontologyId)
```

**Pattern:** All state methods follow this flow:
1. Get or create `SharedOntologyUserState`
2. Update state flags and timestamps
3. Save changes
4. Invalidate cache

#### Access Tracking Methods

```csharp
// Update last accessed time for share link
Task UpdateShareLinkAccessAsync(string userId, int ontologyId)

// Update last accessed time for group
Task UpdateGroupAccessAsync(string userId, int ontologyId)
```

These methods update `LastAccessedAt` timestamps for "recently accessed" filtering.

#### Helper Methods

```csharp
// Convert string permission to enum (safe defaults)
private static PermissionLevel ParsePermissionLevel(string permissionLevelString)
{
    return permissionLevelString?.ToLower() switch
    {
        "view" => PermissionLevel.View,
        "viewandadd" => PermissionLevel.ViewAndAdd,
        "viewaddedit" => PermissionLevel.ViewAddEdit,
        "fullaccess" => PermissionLevel.FullAccess,
        _ => PermissionLevel.View // Safe default
    };
}

// Get or create user state record
private async Task<SharedOntologyUserState> GetOrCreateUserStateAsync(...)
```

---

## API Endpoints

### Endpoint Registration

**Location:** `Endpoints/SharedOntologyEndpoints.cs`

**Registration in Program.cs:**
```csharp
app.MapSharedOntologyEndpoints();
```

### Endpoint Definitions

**Base Path:** `/api/shared-ontologies`

**Authentication:** All endpoints require authentication (`.RequireAuthorization()`)

#### 1. Get Shared Ontologies

```
POST /api/shared-ontologies
Content-Type: application/json
```

**Request Body:**
```json
{
  "includeHidden": false,
  "pinnedOnly": false,
  "accessTypeFilter": null,  // null | "ShareLink" | "Group" | "Both"
  "daysBack": 90,
  "searchTerm": "",
  "sortBy": "LastAccessed",  // "LastAccessed" | "LastViewed" | "Name" | "ConceptCount" | "PinnedFirst"
  "page": 1,
  "pageSize": 24
}
```

**Response:**
```json
{
  "ontologies": [
    {
      "ontologyId": 123,
      "name": "Medical Terminology",
      "description": "Healthcare ontology",
      "ownerId": "user-abc",
      "ownerName": "Dr. Smith",
      "ownerPhotoUrl": null,
      "accessType": "Group",
      "permissionLevel": "ViewAndAdd",
      "lastAccessedAt": "2025-11-15T10:30:00Z",
      "lastViewedAt": "2025-11-15T10:30:00Z",
      "viewCount": 5,
      "conceptCount": 150,
      "relationshipCount": 200,
      "createdAt": "2025-10-01T00:00:00Z",
      "updatedAt": "2025-11-10T00:00:00Z",
      "isPinned": true,
      "isHidden": false,
      "pinnedAt": "2025-11-12T14:00:00Z",
      "groupName": "Healthcare Team",
      "groupMemberCount": 12
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 24,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

#### 2. Pin Ontology

```
POST /api/shared-ontologies/{ontologyId}/pin
```

**Response:**
```json
{
  "message": "Ontology pinned successfully"
}
```

#### 3. Unpin Ontology

```
DELETE /api/shared-ontologies/{ontologyId}/pin
```

**Response:**
```json
{
  "message": "Ontology unpinned successfully"
}
```

#### 4. Hide Ontology

```
POST /api/shared-ontologies/{ontologyId}/hide
```

**Response:**
```json
{
  "message": "Ontology hidden successfully"
}
```

#### 5. Unhide Ontology

```
DELETE /api/shared-ontologies/{ontologyId}/hide
```

**Response:**
```json
{
  "message": "Ontology unhidden successfully"
}
```

#### 6. Dismiss Ontology

```
POST /api/shared-ontologies/{ontologyId}/dismiss
```

**Response:**
```json
{
  "message": "Ontology dismissed successfully"
}
```

#### 7. Update Share Link Access Time

```
POST /api/shared-ontologies/{ontologyId}/access/share-link
```

**Response:**
```json
{
  "message": "Access time updated"
}
```

**Usage:** Call this when user accesses an ontology via share link to update `UserShareAccess.LastAccessedAt`.

#### 8. Update Group Access Time

```
POST /api/shared-ontologies/{ontologyId}/access/group
```

**Response:**
```json
{
  "message": "Access time updated"
}
```

**Usage:** Call this when user accesses an ontology via group membership to update `OntologyGroupPermission.LastAccessedAt`.

---

## Integration Points

### 1. Dashboard Component

**File:** `Components/Pages/Dashboard.razor` (to be modified)

**Integration:**
```razor
@inject SharedOntologyService SharedOntologyService

<div class="shared-ontologies-section">
    <h2>Shared with Me</h2>
    <SharedOntologyList UserId="@userId" />
</div>
```

### 2. Ontology View Component

**File:** `Components/Pages/OntologyView.razor` (to be modified)

**Integration:** Call access tracking endpoint when user opens an ontology:

```csharp
protected override async Task OnInitializedAsync()
{
    // ... existing code

    // Track access
    if (isShareLinkAccess)
    {
        await Http.PostAsync($"/api/shared-ontologies/{ontologyId}/access/share-link", null);
    }
    else if (isGroupAccess)
    {
        await Http.PostAsync($"/api/shared-ontologies/{ontologyId}/access/group", null);
    }
}
```

### 3. Service Registration

**File:** `Program.cs`

```csharp
// Line ~480
builder.Services.AddScoped<SharedOntologyService>();

// Line ~618
app.MapSharedOntologyEndpoints();
```

---

## Caching Strategy

### Cache Configuration

**Provider:** `IMemoryCache` (in-memory, single-server)

**TTL:** 5 minutes sliding expiration

**Key Format:** `SharedOntologies_{userId}_{filterHashCode}`

### Cache Invalidation

Invalidated on user state changes:
- Pin/unpin
- Hide/unhide
- Dismiss

**NOT** invalidated on:
- Access time updates (minor data, next cache refresh will show)
- Other users' actions (cache is per-user)

### Future: Distributed Caching

For multi-server deployments, consider:
- Redis cache
- Sticky sessions for Blazor SignalR
- Cache key partitioning by user region

---

## Performance Considerations

### Query Optimization

1. **Indexes:** All foreign keys and common query paths indexed
2. **Materialization:** Queries materialized before complex joins
3. **Deduplication:** In-memory (post-query) to avoid complex SQL
4. **Pagination:** Applied in-memory after deduplication

### Expected Query Performance

- Share link query: ~10ms (index on UserId, OntologyShare.IsActive)
- Group query: ~15ms (indexes on UserId, UserGroupId, OntologyId)
- Deduplication: ~1ms (in-memory hash set)
- Total: **~30-50ms** for typical user (10-20 shared ontologies)

### Scaling Limits

- **Current design:** Efficient up to ~1000 shared ontologies per user
- **Bottleneck:** In-memory deduplication and sorting
- **Mitigation:** Server-side caching reduces query frequency

---

## Error Handling

### Service Layer

All service methods follow this pattern:

```csharp
try
{
    // ... operation
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Database error...");
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error...");
    throw;
}
```

### Endpoint Layer

```csharp
try
{
    var result = await service.GetSharedOntologiesAsync(...);
    return Results.Ok(result);
}
catch (Exception ex)
{
    logger.LogError(ex, "Error retrieving shared ontologies");
    return Results.Problem("An error occurred while retrieving shared ontologies.");
}
```

### Frontend (Pending Implementation)

Frontend components should:
- Display user-friendly error messages
- Retry transient failures (network issues)
- Log errors for debugging

---

## Testing Strategy

### Unit Tests

**Service Tests:**
- GetSharedOntologiesAsync with various filters
- Pin/unpin/hide/unhide/dismiss operations
- ParsePermissionLevel edge cases
- Cache invalidation

**Endpoint Tests:**
- Authentication required
- Request validation
- Response format

### Integration Tests

- End-to-end query with real database
- Share link + group access deduplication
- State persistence across sessions

### Performance Tests

- 100+ shared ontologies per user
- Concurrent requests (cache contention)
- Memory usage (cache growth)

---

## Migration Path

### Running the Migration

```bash
# Apply migration
dotnet ef database update

# Verify tables created
dotnet ef database update --verbose
```

### Rollback Plan

```bash
# Revert migration
dotnet ef database update <PreviousMigration>

# Or remove migration
dotnet ef migrations remove
```

### Data Migration

No existing data migration required (new feature).

---

## Monitoring & Observability

### Logging

All service methods log:
- Method entry (Debug level)
- Errors (Error level)
- Performance warnings (Warning level if query > 100ms)

### Metrics to Monitor

- Cache hit rate (target: >80%)
- Query duration (target: <50ms p95)
- API endpoint latency (target: <100ms p95)
- Error rate (target: <0.1%)

### Application Insights

If enabled, tracks:
- API endpoint calls
- Service method execution time
- Exception details
- Custom events (pin/hide/dismiss actions)

---

## Security Considerations

### Authorization

- All endpoints require authentication (`.RequireAuthorization()`)
- User can only access their own shared ontologies
- UserId extracted from authenticated `ClaimsPrincipal`

### Input Validation

- OntologyId validated (must be positive integer)
- Page/PageSize validated (reasonable limits)
- SearchTerm sanitized (SQL injection prevention via EF parameterization)

### Data Exposure

- Only returns ontologies user has legitimate access to
- Owner information limited to public profile data
- No sensitive permission details exposed

---

**Document Version**: 1.0
**Last Updated**: November 19, 2025
**Status**: Backend Complete, Frontend Pending
