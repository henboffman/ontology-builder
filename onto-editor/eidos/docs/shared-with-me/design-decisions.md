# Shared With Me - Design Decisions

## Overview

This document captures key architectural and design decisions made during the development of the "Shared with Me" feature, along with the rationale and alternatives considered.

---

## Decision 1: Separate User State Table

**Decision:** Create a dedicated `SharedOntologyUserState` table to store user-specific state (pin/hide/dismiss) separate from access permissions.

**Rationale:**
- User state (pinning, hiding) is independent of how they gained access
- Same ontology might be accessible via multiple paths (share link + group)
- State should persist even if one access method is removed
- Cleaner separation of concerns

**Alternatives Considered:**
1. **Store state in UserShareAccess table** - Rejected because it only covers share link access, not group access
2. **Store state in OntologyGroupPermission** - Rejected because users can access via multiple groups
3. **Store state in user preferences JSON** - Rejected due to querying complexity and performance

**Implementation:**
```csharp
public class SharedOntologyUserState
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int OntologyId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsHidden { get; set; }
    public bool IsDismissed { get; set; }
    // ... timestamps
}
```

**Indexes:**
- `(UserId, OntologyId)` - Unique constraint
- `(UserId)` - User queries
- `(UserId, IsPinned)` - Pinned queries

---

## Decision 2: Unified Query with Separate Materialization

**Decision:** Query share link and group access separately, materialize both, then combine in memory.

**Rationale:**
- Share links use `PermissionLevel` enum, groups use string permission levels
- Different DateTime nullability (`LastAccessedAt` on groups is nullable)
- LINQ cannot Union incompatible anonymous types
- Materializing early avoids complex database query translation

**Alternatives Considered:**
1. **Single database query with UNION** - Rejected due to type incompatibility
2. **Cast everything to strings in SQL** - Rejected due to performance and type safety
3. **Separate endpoints for each access type** - Rejected as it complicates UI

**Implementation Pattern:**
```csharp
// Query share links
var shareLinkResults = await context.UserShareAccesses
    .Select(usa => new { ... })
    .ToListAsync();

// Query group access
var groupResults = await context.UserGroupMembers
    .Select(ogp => new { ... })
    .ToListAsync();

// Convert and combine in memory
var allResults = shareLinkResults
    .Concat(groupResultsConverted)
    .ToList();
```

---

## Decision 3: Add LastAccessedAt to OntologyGroupPermission

**Decision:** Extend the existing `OntologyGroupPermission` table with a `LastAccessedAt` field.

**Rationale:**
- Matches the pattern used in `UserShareAccess.LastAccessedAt`
- Enables "recently accessed" filtering for group-shared ontologies
- Minimal schema change (single nullable column)
- Consistent with existing access tracking

**Alternatives Considered:**
1. **Separate access tracking table** - Rejected as over-engineered for this use case
2. **Use OntologyActivity table** - Rejected because it tracks modifications, not views
3. **No group access tracking** - Rejected because it creates inconsistent UX

**Migration:**
```csharp
migrationBuilder.AddColumn<DateTime>(
    name: "LastAccessedAt",
    table: "OntologyGroupPermissions",
    type: "datetime2",
    nullable: true);
```

---

## Decision 4: Service-Level Caching with 5-Minute TTL

**Decision:** Implement IMemoryCache in SharedOntologyService with 5-minute sliding expiration.

**Rationale:**
- Shared ontologies list changes infrequently
- Query joins 4+ tables (expensive operation)
- Cache key based on `(userId, filterHashCode)` for granular invalidation
- Sliding expiration keeps frequently accessed data hot

**Cache Invalidation:**
- Pin/Unpin actions invalidate user's cache
- Hide/Unhide actions invalidate user's cache
- Dismiss actions invalidate user's cache

**Alternatives Considered:**
1. **No caching** - Rejected due to performance concerns
2. **10+ minute TTL** - Rejected because state changes need to be visible quickly
3. **Redis distributed cache** - Deferred for future optimization (single-server sufficient for MVP)

**Implementation:**
```csharp
private const string CACHE_KEY_PREFIX = "SharedOntologies_";
private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

var cacheKey = $"{CACHE_KEY_PREFIX}{userId}_{filter.GetHashCode()}";
if (_cache.TryGetValue(cacheKey, out SharedOntologyResult? cached))
{
    return cached;
}
// ... query and cache
```

---

## Decision 5: Deduplication with "Both" Access Type

**Decision:** When a user has both share link AND group access to the same ontology, deduplicate and mark as `AccessType.Both`, taking the highest permission level.

**Rationale:**
- Users should see each ontology exactly once in the list
- Showing both access types provides transparency
- Highest permission level gives users maximum capability
- Simplifies UI (no duplicate cards)

**Permission Hierarchy:**
```
FullAccess > ViewAddEdit > ViewAndAdd > View
```

**Implementation:**
```csharp
var deduplicatedResults = allResults
    .GroupBy(r => r.OntologyId)
    .Select(g =>
    {
        if (g.Count() > 1)
        {
            var highest = g.OrderByDescending(x => x.PermissionLevel).First();
            highest.AccessType = SharedAccessType.Both;
            return highest;
        }
        return g.First();
    })
    .ToList();
```

---

## Decision 6: Minimal API Endpoints with Scoped Service Injection

**Decision:** Use ASP.NET Core Minimal APIs with dependency injection for SharedOntologyService.

**Rationale:**
- Consistent with existing Eidos endpoint patterns (AttachmentEndpoints, etc.)
- Clean separation from Blazor components
- Easy to test independently
- Supports future API versioning

**Authentication:**
- All endpoints require authentication via `.RequireAuthorization()`
- User identity extracted from `ClaimsPrincipal`

**Alternatives Considered:**
1. **Blazor component code-behind** - Rejected due to tight coupling
2. **Traditional MVC controllers** - Rejected as unnecessarily heavyweight
3. **Direct DbContext injection in components** - Rejected due to service layer pattern

**Endpoint Pattern:**
```csharp
var group = app.MapGroup("/api/shared-ontologies")
    .RequireAuthorization();

group.MapPost("/", GetSharedOntologies);
group.MapPost("/{ontologyId}/pin", PinOntology);
```

---

## Decision 7: Default 90-Day Lookback Window

**Decision:** Default to showing shared ontologies accessed in the last 90 days, with user override capability.

**Rationale:**
- 90 days balances recency with utility
- Most users will have 5-20 shared ontologies (per requirements)
- Prevents overwhelming UI with stale shares
- User can extend lookback if needed

**Expected Distribution:**
- 80% of accesses in last 30 days
- 15% in 30-90 day range
- 5% older than 90 days

**User Controls:**
```csharp
public class SharedOntologyFilter
{
    public int DaysBack { get; set; } = 90; // User can override
    // ...
}
```

---

## Decision 8: Pin/Hide/Dismiss Semantics

**Decision:** Implement three distinct user actions with specific behaviors:

**Pin:**
- Keeps ontology at top of list
- Persists across sessions
- User can have unlimited pins

**Hide:**
- Removes from default view
- Reversible (can unhide)
- Available in "Show Hidden" filter

**Dismiss:**
- Soft delete from shared list
- User explicitly doesn't want to see it
- Not reversible via UI (requires direct database access)

**Rationale:**
- Provides flexibility for different user preferences
- Common pattern in email/notification UIs
- "Dismiss" is stronger than "hide" for unwanted shares

**Alternatives Considered:**
1. **Only Pin and Archive** - Rejected because "archive" semantics are unclear
2. **Only Hide with "never show again" checkbox** - Rejected due to UI complexity
3. **Star/Favorite instead of Pin** - Rejected to match platform conventions (pin = top positioning)

---

## Decision 9: Pagination with 24 Items Per Page

**Decision:** Default page size of 24 items with user override capability.

**Rationale:**
- 24 divides evenly into 2, 3, 4, 6 column layouts
- Typical screen shows 6-12 items, so 2-4 pages = 1-2 scrolls
- Balances performance with UX (not too many API calls)
- Matches common design system patterns

**Expected Layout:**
- Desktop: 4 columns × 6 rows = 24 items
- Tablet: 3 columns × 8 rows = 24 items
- Mobile: 2 columns × 12 rows = 24 items

---

## Decision 10: Permission Level Conversion Helper

**Decision:** Create `ParsePermissionLevel()` helper method to convert string permissions to enum.

**Rationale:**
- OntologyShare uses `PermissionLevel` enum
- OntologyGroupPermission uses string (legacy design)
- Conversion logic centralized in one place
- Safe defaults prevent errors

**Implementation:**
```csharp
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
```

**Future:** Consider migrating OntologyGroupPermission to use enum for type safety.

---

## Future Considerations

### Not Implemented (Deferred)

1. **Collaborative Filtering** - "Users who viewed this also viewed..." (deferred until more data)
2. **Activity Notifications** - Alert when shared ontology is updated (deferred for notifications feature)
3. **Bulk Actions** - Select multiple ontologies to pin/hide (deferred for UX simplicity)
4. **Custom Sort Orders** - User-defined sort preferences (deferred for MVP)
5. **Tags on Shared Ontologies** - User-specific tagging (deferred to avoid scope creep)

### Scalability Considerations

- Current design supports up to ~1000 shared ontologies per user before pagination becomes critical
- Indexes optimized for common query patterns
- Caching reduces database load
- Future: Consider materialized views for complex aggregations

---

**Document Version**: 1.0
**Last Updated**: November 19, 2025
**Authors**: Development Team with AI Assistance (Claude Code)
