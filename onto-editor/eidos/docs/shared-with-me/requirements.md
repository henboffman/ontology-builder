# Requirements Document: "Shared with Me" Ontologies Dashboard Feature

**Feature Name:** Shared with Me Dashboard Section
**Version:** 1.0
**Date:** November 18, 2025
**Author:** Requirements Analysis
**Status:** Planning - Phase 1 (MVP)

---

## Table of Contents

1. [Business Requirements](#1-business-requirements)
2. [Functional Requirements](#2-functional-requirements)
3. [User Experience Requirements](#3-user-experience-requirements)
4. [Data Requirements](#4-data-requirements)
5. [Technical Requirements](#5-technical-requirements)
6. [Security & Privacy Requirements](#6-security--privacy-requirements)
7. [Performance Requirements](#7-performance-requirements)
8. [UI Component Requirements](#8-ui-component-requirements)
9. [Integration Requirements](#9-integration-requirements)
10. [Acceptance Criteria](#10-acceptance-criteria)
11. [Edge Cases & Error Handling](#11-edge-cases--error-handling)
12. [Testing Requirements](#12-testing-requirements)
13. [Documentation Requirements](#13-documentation-requirements)
14. [Implementation Phases](#14-implementation-phases)

---

## 1. Business Requirements

### 1.1 Problem Statement

Users currently have difficulty discovering and accessing ontologies that have been shared with them through share links or group permissions. While the sharing mechanism works technically, there is no dedicated UI to help users:

- Find ontologies that others have shared with them
- Revisit ontologies they've previously accessed via share link
- Distinguish between their own ontologies and those shared by others
- Manage their list of shared ontologies (pin favorites, hide unwanted)

This creates a poor user experience where users must rely on:
- Saved browser bookmarks for share links
- Manual notes of which ontologies were shared
- Searching through their entire ontology list to find shared items

### 1.2 User Value Proposition

**For Ontology Consumers (Users receiving shares):**
- **Discoverability:** Easily find all ontologies shared with you in one place
- **Organization:** Pin important shared ontologies, hide ones you don't need
- **Context:** See who shared each ontology and what permission level you have
- **Efficiency:** Quick access to recently accessed shared work without hunting for links

**For Ontology Owners (Users sharing):**
- **Increased engagement:** Recipients are more likely to access and contribute to shared ontologies
- **Better collaboration:** Easier for team members to find and work on shared projects
- **Visibility:** Know that your shared ontologies are discoverable by collaborators

**For Platform:**
- **Increased usage:** Users engage more with shared content when it's easy to find
- **Better retention:** Users see value in collaboration features
- **Network effects:** Easier sharing encourages more collaboration

### 1.3 Success Metrics

**Primary Metrics:**
- **Adoption rate:** % of users with shared ontologies who use the "Shared with Me" section (Target: 70%+)
- **Return visits:** % of users who access the same shared ontology more than once (Target: 50%+)
- **Time to access:** Average time from share link click to returning to ontology later (Target: reduce by 60%)

**Secondary Metrics:**
- **Pin usage:** % of shared ontologies that get pinned (indicates value)
- **Hide usage:** % of shared ontologies that get hidden (indicates quality/relevance)
- **Permission level awareness:** User survey responses indicating understanding of their access level

**Engagement Metrics:**
- **Click-through rate:** % of users who click through to view shared ontologies from the section
- **Daily active users:** Increase in users accessing shared ontologies
- **Collaboration completion:** % of collaboration board posts that result in actual collaborative work

### 1.4 Business Goals

1. **Improve Collaboration:** Make it easier for users to work together on ontologies
2. **Increase Platform Value:** Demonstrate that Eidos is a collaborative platform, not just a personal tool
3. **Reduce Support Burden:** Fewer user questions about "how do I find that ontology someone shared?"
4. **Enable Team Workflows:** Support organizations using Eidos for team ontology development
5. **Prepare for Future Features:** Foundation for notifications, activity feeds, and real-time collaboration features

---

## 2. Functional Requirements

### 2.1 Core Display Functionality

**FR-001: Display Shared Ontologies List**
- **Priority:** P0 (Critical)
- **Description:** Display a dedicated "Shared with Me" section on the dashboard showing all ontologies shared with the current user
- **Data Sources:**
  - Direct share link access: Ontologies accessed via `UserShareAccess` table
  - Group-based access: Ontologies accessible via `OntologyGroupPermission` where user is a member
- **Display Rules:**
  - Only show ontologies accessed in the last 90 days
  - Exclude ontologies owned by the current user (even if they're in a group that has access)
  - Sort by last accessed date (most recent first) by default
  - Show maximum 100 ontologies (paginated)

**FR-002: Ontology Card Information**
- **Priority:** P0 (Critical)
- **Description:** Each ontology card must display:
  - Ontology name
  - Description (truncated if > 100 characters)
  - Visual "Shared" badge (blue badge with share icon)
  - Owner information (who owns the ontology)
  - Permission level indicator (View, View & Add, View & Edit, Manage)
  - Concept count and relationship count
  - Last accessed date
  - Optional: Tags/folders from owner's organization

**FR-003: Click to Open Ontology**
- **Priority:** P0 (Critical)
- **Description:** Clicking on an ontology card navigates to the ontology detail page
- **Behavior:**
  - Update `LastAccessedAt` timestamp on click
  - Respect permission level (read-only view if "View", full editor if "Edit")
  - Track access count for analytics

### 2.2 Pin/Favorite Functionality

**FR-004: Pin Shared Ontologies**
- **Priority:** P1 (High)
- **Description:** Users can pin/favorite shared ontologies to keep them at the top of the list
- **Implementation:**
  - Pin icon/button on each ontology card
  - Toggle on/off (star icon filled vs outline)
  - Pinned ontologies appear first in the list (above unpinned)
  - Pinned state persists across sessions
  - Visual indicator distinguishes pinned from unpinned

**FR-005: Pin Management**
- **Priority:** P1 (High)
- **Description:**
  - No limit on number of pinned ontologies
  - Pinned ontologies still respect the 90-day access filter
  - Pinned ontologies maintain sort order within pinned section (by last accessed)

### 2.3 Hide/Dismiss Functionality

**FR-006: Hide Shared Ontologies**
- **Priority:** P1 (High)
- **Description:** Users can hide/dismiss shared ontologies they no longer want to see
- **Implementation:**
  - "Hide" or "Dismiss" action in card menu (three-dot menu)
  - Confirmation dialog: "Hide this ontology from your Shared with Me list?"
  - Hidden ontologies are removed from the list immediately
  - Hidden state persists across sessions
  - User can still access via direct URL or share link if they have the link

**FR-007: Unhide Functionality**
- **Priority:** P2 (Medium)
- **Description:** Provide a way for users to view and unhide previously hidden ontologies
- **Implementation:**
  - "Show Hidden" toggle or link at bottom of "Shared with Me" section
  - Clicking shows a list of hidden ontologies with "Unhide" action
  - Unhiding restores ontology to main "Shared with Me" list

### 2.4 Owner Information Display

**FR-008: Show Ontology Owner**
- **Priority:** P1 (High)
- **Description:** Display who owns each shared ontology
- **Implementation:**
  - Show owner's name (from `ApplicationUser` display name)
  - Show owner's avatar/initials if available (from OAuth provider photo)
  - Format: "Shared by [Owner Name]" or "Owner: [Owner Name]"
  - If owner is known collaborator, highlight relationship (e.g., "Your Collaborator")

**FR-009: Sharing Method Context**
- **Priority:** P2 (Medium - Phase 2)**
- **Description:** Show HOW the ontology was shared with the user
- **Options:**
  - "Shared via link" (direct share link access)
  - "Shared via group: [Group Name]" (group permission)
  - "Multiple sources" (both share link AND group access)
- **Benefits:** Helps users understand their access method and context

### 2.5 Permission Level Filtering

**FR-010: Filter by Permission Level**
- **Priority:** P1 (High)
- **Description:** Filter shared ontologies by the user's permission level
- **Filter Options:**
  - "All Permissions" (default)
  - "View Only" (PermissionLevel.View)
  - "Can Add" (PermissionLevel.ViewAndAdd)
  - "Can Edit" (PermissionLevel.ViewAddEdit)
  - "Full Access" (PermissionLevel.FullAccess)
- **Implementation:**
  - Dropdown filter in dashboard controls section
  - Filter applies on top of other filters (search, sort)
  - Show count of ontologies per permission level

**FR-011: Permission Level Indicator**
- **Priority:** P0 (Critical)
- **Description:** Visual indicator of user's permission level on each card
- **Display:**
  - Badge color-coded by permission level:
    - View: Gray/secondary
    - View & Add: Blue/info
    - View & Edit: Green/success
    - Full Access: Purple/primary
  - Tooltip on hover explaining what the permission allows

### 2.6 Sorting and Pagination

**FR-012: Sort Options**
- **Priority:** P0 (Critical)
- **Description:** Users can sort shared ontologies
- **Sort Options:**
  - Last Accessed (most recent first) - **DEFAULT**
  - Last Accessed (oldest first)
  - Name (A-Z)
  - Name (Z-A)
  - Permission Level (highest to lowest)
  - Owner Name (A-Z)
- **Behavior:**
  - Pinned ontologies always appear first, then sorted within each section
  - Sort selection persists during session (not across sessions)

**FR-013: Pagination**
- **Priority:** P1 (High)
- **Description:** Handle large lists of shared ontologies gracefully
- **Implementation:**
  - Show 24 ontologies per page (4 rows of 6 in grid view)
  - "Load More" button for additional pages (infinite scroll alternative)
  - Show count: "Showing 24 of 87 shared ontologies"
  - If ≤ 24 ontologies, no pagination needed

### 2.7 Search Functionality

**FR-014: Search Shared Ontologies**
- **Priority:** P1 (High)
- **Description:** Users can search within their shared ontologies
- **Search Scope:**
  - Ontology name
  - Ontology description
  - Owner name
  - Tags (if shown)
- **Behavior:**
  - Real-time filtering as user types
  - Case-insensitive search
  - Highlight search terms in results (optional enhancement)
  - Clear button to reset search

---

## 3. User Experience Requirements

### 3.1 Dashboard Layout and Placement

**UX-001: Section Placement**
- **Location:** Below "My Ontologies" section on the main dashboard (Home.razor)
- **Rationale:**
  - User's own ontologies are primary content, shared content is secondary
  - Maintains consistent left-to-right, top-to-bottom information hierarchy
  - Allows progressive disclosure (user sees own work first)

**UX-002: Section Header**
- **Title:** "Shared with Me" with share icon (bi-share)
- **Subtitle:** "Ontologies others have shared with you"
- **Right-aligned actions:**
  - Filter dropdown (permission level)
  - View toggle (grid/list)
  - Optional: Expand/collapse section

**UX-003: Section Visibility**
- **Show When:** User has at least one shared ontology (accessed in last 90 days)
- **Hide When:** User has zero shared ontologies
- **Empty State:** Show helpful message with call-to-action (see UX-006)

**UX-004: Collapsible Section (Optional - Phase 2)**
- **Behavior:** Section can be collapsed to save vertical space
- **Default:** Expanded if user has shared ontologies
- **Persistence:** Remember expanded/collapsed state in user preferences

### 3.2 Visual Indicators

**UX-005: "Shared" Badge**
- **Design:**
  - Blue badge (Bootstrap `bg-primary` or `bg-info`)
  - Icon: `bi-share` (Bootstrap Icons)
  - Text: "Shared"
  - Placement: Top-right of ontology card, next to Public/Private badges
- **Tooltip:** "This ontology is shared with you by [Owner Name]"

**UX-006: Permission Badge**
- **Design:**
  - Color-coded by permission level:
    - View: `badge bg-secondary` (gray)
    - View & Add: `badge bg-info` (blue)
    - View & Edit: `badge bg-success` (green)
    - Full Access: `badge bg-primary` (purple/primary)
  - Icon: Lock icon variants (locked, unlocked, key, etc.)
  - Text: "View", "Can Add", "Can Edit", "Manage"
  - Placement: Below "Shared" badge or in card footer

**UX-007: Pin Indicator**
- **Design:**
  - Star icon: `bi-star-fill` (filled/pinned) or `bi-star` (outline/unpinned)
  - Color: Gold/yellow when pinned (`text-warning`)
  - Placement: Top-left of card or next to title
  - Hover: Scale up slightly, show tooltip
- **Tooltip:** "Click to pin/unpin this ontology"

**UX-008: Owner Information**
- **Design:**
  - Avatar or initials circle (similar to presence indicators)
  - Owner name in small/muted text
  - Placement: Card footer or below description
  - Format: "Owner: [Name]" or "By [Name]"
- **Interaction:** Clicking owner name could show owner's profile (future enhancement)

### 3.3 Interaction Patterns

**UX-009: Card Click Behavior**
- **Primary Action:** Click anywhere on card opens ontology
- **Exceptions:** Clicking pin, hide menu, or tag buttons does NOT open ontology
- **Hover:** Card elevation increases (existing behavior)
- **Feedback:** Brief loading indicator during navigation

**UX-010: Pin/Unpin Interaction**
- **Action:** Click star icon to toggle pin state
- **Feedback:**
  - Icon changes immediately (optimistic UI)
  - Optional: Toast notification "Pinned" or "Unpinned"
  - Card animates to new position in list (smooth transition)
- **Error Handling:** If pin fails, revert icon state and show error

**UX-011: Hide/Dismiss Interaction**
- **Trigger:** Three-dot menu (kebab menu) on card → "Hide" option
- **Confirmation:** Modal dialog or inline confirmation:
  - "Hide this ontology from your Shared with Me list?"
  - "You can unhide it later by clicking 'Show Hidden' at the bottom of this section."
  - Buttons: "Cancel", "Hide"
- **Feedback:**
  - Card fades out and slides away (animation)
  - Toast notification: "Ontology hidden. [Undo]"
  - Optional: Undo button in toast (5-second window)

**UX-012: Filter and Sort Interaction**
- **Filters Apply Immediately:** No "Apply" button needed
- **Visual Feedback:**
  - Show spinner/skeleton during filter application
  - Update result count: "Showing 12 of 87 shared ontologies"
  - If no results, show "No matching ontologies" message

### 3.4 Empty States

**UX-013: No Shared Ontologies**
- **Condition:** User has never accessed a shared ontology
- **Display:**
  - Icon: Large share icon (bi-share) in muted color
  - Heading: "No shared ontologies yet"
  - Subtext: "When others share ontologies with you via link or group, they'll appear here."
  - Call-to-Action:
    - "Browse Public Ontologies" button → public ontologies section
    - "Join the Collaboration Board" button → collaboration page

**UX-014: All Shared Ontologies Hidden**
- **Condition:** User has hidden all their shared ontologies
- **Display:**
  - Icon: Eye-slash icon (bi-eye-slash)
  - Heading: "All shared ontologies are hidden"
  - Subtext: "Click 'Show Hidden' to view and unhide ontologies."
  - Call-to-Action: "Show Hidden" button (toggles hidden list)

**UX-015: No Results from Filter/Search**
- **Condition:** Filters/search return no results
- **Display:**
  - Icon: Magnifying glass with X (bi-search) + (bi-x)
  - Heading: "No matching ontologies"
  - Subtext: "Try adjusting your filters or search terms."
  - Call-to-Action: "Clear Filters" button

**UX-016: Access Expired/Revoked**
- **Condition:** User previously had access, but share was revoked or expired
- **Display:**
  - Card shows with muted/grayed out appearance
  - Badge: "Access Revoked" or "Link Expired" (red badge)
  - Tooltip: "You no longer have access to this ontology."
  - Interaction: Clicking shows modal explaining loss of access
  - Optional: "Request Access" button to message owner

### 3.5 Loading States

**UX-017: Initial Load**
- **Display:**
  - Skeleton cards (shimmer effect) in grid layout
  - 6 skeleton cards visible (one row)
  - Section header visible with spinner

**UX-018: Filter/Sort Application**
- **Display:**
  - Existing cards fade slightly (opacity: 0.6)
  - Small spinner in section header or filter dropdown
  - Duration: < 500ms for good perceived performance

**UX-019: Pagination Load More**
- **Display:**
  - "Load More" button changes to spinner
  - New cards fade in smoothly as they're added
  - Scroll position maintained

### 3.6 Error States

**UX-020: Failed to Load**
- **Display:**
  - Alert box (Bootstrap `alert-danger`)
  - Icon: Exclamation triangle (bi-exclamation-triangle)
  - Message: "Failed to load shared ontologies. Please try again."
  - Action: "Retry" button to reload section

**UX-021: Failed to Pin/Unpin**
- **Display:**
  - Toast notification (error): "Failed to pin ontology. Please try again."
  - Icon reverts to previous state (optimistic UI rollback)

**UX-022: Failed to Hide**
- **Display:**
  - Toast notification (error): "Failed to hide ontology. Please try again."
  - Card remains visible in list

### 3.7 Mobile Responsiveness

**UX-023: Mobile Layout (< 768px)**
- **Grid View:** Switch to single column layout (1 card per row)
- **Card Size:** Full-width cards with optimized vertical spacing
- **Controls:** Stack filters and sort dropdown vertically
- **Pin/Hide:** Touch-friendly tap targets (minimum 44x44px)
- **Menu:** Three-dot menu accessible with thumb on right side

**UX-024: Tablet Layout (768px - 1024px)**
- **Grid View:** 2 columns of cards
- **Controls:** Horizontal layout maintained
- **Sidebar:** Folder sidebar collapses or moves to hamburger menu

**UX-025: Touch Interactions**
- **Long-press:** Alternative to three-dot menu (long-press card for actions)
- **Swipe:** Optional swipe-to-hide gesture (Phase 2)
- **Pinch-zoom:** Disabled on cards (not applicable)

---

## 4. Data Requirements

### 4.1 Existing Data Sources

**Data Source 1: Direct Share Link Access**
- **Table:** `UserShareAccess`
- **Key Fields:**
  - `UserId` (current user)
  - `OntologyShareId` (links to share)
  - `FirstAccessedAt` (when first accessed)
  - `LastAccessedAt` (when last accessed)
  - `AccessCount` (number of accesses)
- **Join Path:**
  ```
  UserShareAccess → OntologyShare → Ontology
  ```
- **Filter:**
  - `UserId = currentUserId`
  - `LastAccessedAt >= DateTime.UtcNow.AddDays(-90)`
  - `OntologyShare.IsActive = true`

**Data Source 2: Group-Based Access**
- **Tables:** `OntologyGroupPermission`, `UserGroupMember`
- **Key Fields:**
  - `UserGroupMember.UserId` (current user)
  - `UserGroupMember.UserGroupId` (group)
  - `OntologyGroupPermission.OntologyId` (ontology)
  - `OntologyGroupPermission.PermissionLevel` (permission)
- **Join Path:**
  ```
  UserGroupMember → UserGroup → OntologyGroupPermission → Ontology
  ```
- **Filter:**
  - `UserGroupMember.UserId = currentUserId`
  - `Ontology.UserId != currentUserId` (exclude own ontologies)
  - Need to track LastAccessedAt (see New Data Requirements)

### 4.2 New Data Requirements

**NEW TABLE: `SharedOntologyUserState`**
- **Purpose:** Track user-specific state for shared ontologies (pin, hide, last accessed for group shares)
- **Schema:**
  ```csharp
  public class SharedOntologyUserState
  {
      public int Id { get; set; }

      // Composite key: UserId + OntologyId
      public string UserId { get; set; } = string.Empty;
      public ApplicationUser User { get; set; } = null!;

      public int OntologyId { get; set; }
      public Ontology Ontology { get; set; } = null!;

      // State flags
      public bool IsPinned { get; set; } = false;
      public bool IsHidden { get; set; } = false;

      // Access tracking for group-shared ontologies
      // (Share link access already tracked in UserShareAccess)
      public DateTime? FirstAccessedAt { get; set; }
      public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
      public int AccessCount { get; set; } = 0;

      // Metadata
      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
      public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

      // Index on composite key for performance
      // Index on (UserId, LastAccessedAt) for filtering
      // Index on (UserId, IsPinned) for pinned items query
  }
  ```

**Rationale for New Table:**
- **Separate Concerns:** Pin/hide state is user preference, not access tracking
- **Support Group Shares:** Group-based access doesn't have `UserShareAccess` record, so we need to track last access somewhere
- **Efficient Querying:** Indexed table allows fast filtering by user, pinned state, and last accessed date
- **Future Extensibility:** Can add more user preferences (custom sort order, notes, custom tags)

**Alternative Considered: Add Fields to UserShareAccess**
- **Rejected Because:**
  - Only works for share link access, not group shares
  - Mixing access tracking with user preferences violates single responsibility
  - Would require duplicate records for users in both share link AND group

### 4.3 Data Retention Policy

**DR-001: 90-Day Access Window**
- **Rule:** Only show shared ontologies accessed in the last 90 days
- **Implementation:**
  - Filter query: `LastAccessedAt >= DateTime.UtcNow.AddDays(-90)`
  - Applies to both share link access and group access
  - Pinned items still respect 90-day filter (if no access in 90 days, they disappear even if pinned)

**DR-002: Data Cleanup (Optional - Phase 2)**
- **Background Job:**
  - Run weekly to clean up old `SharedOntologyUserState` records
  - Delete records where `LastAccessedAt` > 1 year old AND `IsPinned = false`
  - Keeps database size manageable

**DR-003: Hidden Items Retention**
- **Rule:** Hidden items never auto-delete (user explicitly hid them)
- **Rationale:** User intent to hide should persist even if they don't access the ontology again

### 4.4 Data Consistency

**DC-001: Update Last Accessed on View**
- **Trigger:** User clicks on shared ontology card
- **Action:**
  - Update `SharedOntologyUserState.LastAccessedAt` to `DateTime.UtcNow`
  - Increment `SharedOntologyUserState.AccessCount`
  - If share link access, also update `UserShareAccess.LastAccessedAt` and `AccessCount`

**DC-002: Create State Record on First Access**
- **Trigger:** User accesses shared ontology for first time (via share link OR group)
- **Action:**
  - If `SharedOntologyUserState` doesn't exist, create it with:
    - `FirstAccessedAt = DateTime.UtcNow`
    - `LastAccessedAt = DateTime.UtcNow`
    - `AccessCount = 1`
    - `IsPinned = false`
    - `IsHidden = false`

**DC-003: Handle Deleted Ontologies**
- **Behavior:** If ontology is deleted, cascade delete `SharedOntologyUserState` records
- **Database:** Foreign key constraint with `ON DELETE CASCADE`

**DC-004: Handle Revoked Access**
- **Share Link Revoked:**
  - `OntologyShare.IsActive = false`
  - Ontology should not appear in "Shared with Me" list
  - `SharedOntologyUserState` record persists (for historical tracking)
- **Group Membership Revoked:**
  - User removed from `UserGroupMembers`
  - Ontology should not appear in "Shared with Me" list
  - `SharedOntologyUserState` record persists

---

## 5. Technical Requirements

### 5.1 Backend Services

**SERVICE-001: SharedOntologyService (New)**
- **Purpose:** Manage shared ontology operations (pin, hide, access tracking)
- **Location:** `/Services/SharedOntologyService.cs`
- **Interface:** `/Services/Interfaces/ISharedOntologyService.cs`
- **Key Methods:**
  ```csharp
  // Get shared ontologies for user
  Task<List<SharedOntologyDto>> GetSharedOntologiesAsync(string userId, SharedOntologyFilter filter);

  // Pin/unpin
  Task PinOntologyAsync(int ontologyId, string userId);
  Task UnpinOntologyAsync(int ontologyId, string userId);

  // Hide/unhide
  Task HideOntologyAsync(int ontologyId, string userId);
  Task UnhideOntologyAsync(int ontologyId, string userId);
  Task<List<SharedOntologyDto>> GetHiddenOntologiesAsync(string userId);

  // Access tracking
  Task TrackAccessAsync(int ontologyId, string userId);
  Task<SharedOntologyUserState> GetOrCreateStateAsync(int ontologyId, string userId);

  // Statistics
  Task<SharedOntologyStats> GetStatsAsync(string userId);
  ```

**SERVICE-002: SharedOntologyDto (New)**
- **Purpose:** Data transfer object for shared ontology display
- **Location:** `/Models/DTOs/SharedOntologyDto.cs`
- **Properties:**
  ```csharp
  public class SharedOntologyDto
  {
      // Ontology info
      public int Id { get; set; }
      public string Name { get; set; }
      public string Description { get; set; }
      public int ConceptCount { get; set; }
      public int RelationshipCount { get; set; }
      public List<OntologyTag> Tags { get; set; }

      // Owner info
      public string OwnerId { get; set; }
      public string OwnerName { get; set; }
      public string? OwnerPhotoUrl { get; set; }

      // Access info
      public PermissionLevel PermissionLevel { get; set; }
      public string PermissionLevelDisplay { get; set; } // "View", "Can Edit", etc.
      public DateTime LastAccessedAt { get; set; }
      public DateTime? FirstAccessedAt { get; set; }
      public int AccessCount { get; set; }

      // Sharing method
      public bool IsSharedViaLink { get; set; }
      public bool IsSharedViaGroup { get; set; }
      public string? GroupName { get; set; }

      // User state
      public bool IsPinned { get; set; }
      public bool IsHidden { get; set; }
  }
  ```

**SERVICE-003: SharedOntologyFilter (New)**
- **Purpose:** Filter criteria for shared ontologies query
- **Properties:**
  ```csharp
  public class SharedOntologyFilter
  {
      public string? SearchQuery { get; set; }
      public PermissionLevel? PermissionLevel { get; set; }
      public string SortBy { get; set; } = "last-accessed-desc"; // or "name-asc", etc.
      public bool IncludeHidden { get; set; } = false;
      public int PageSize { get; set; } = 24;
      public int PageNumber { get; set; } = 1;
  }
  ```

### 5.2 Repository Methods

**REPO-001: SharedOntologyUserStateRepository (New)**
- **Purpose:** Data access for `SharedOntologyUserState` table
- **Location:** `/Data/Repositories/SharedOntologyUserStateRepository.cs`
- **Key Methods:**
  ```csharp
  Task<SharedOntologyUserState?> GetByUserAndOntologyAsync(string userId, int ontologyId);
  Task<List<SharedOntologyUserState>> GetByUserAsync(string userId);
  Task<List<SharedOntologyUserState>> GetPinnedByUserAsync(string userId);
  Task<List<SharedOntologyUserState>> GetHiddenByUserAsync(string userId);
  Task UpsertAsync(SharedOntologyUserState state);
  ```

**REPO-002: Extended OntologyPermissionService**
- **Purpose:** Add method to get shared ontologies with user state
- **New Method:**
  ```csharp
  Task<List<SharedOntologyDto>> GetSharedOntologiesAsync(
      string userId,
      SharedOntologyFilter filter);
  ```
- **Implementation:**
  - Query both `UserShareAccess` and group-based access
  - Join with `SharedOntologyUserState` for pin/hide state
  - Join with `ApplicationUser` for owner info
  - Apply filters (search, permission, hidden)
  - Apply sorting
  - Apply pagination

### 5.3 API Endpoints (Optional - if using Minimal APIs)

**API-001: Get Shared Ontologies**
- **Endpoint:** `GET /api/shared-ontologies`
- **Auth:** Required
- **Query Params:**
  - `search` (string, optional)
  - `permissionLevel` (enum, optional)
  - `sortBy` (string, optional)
  - `includeHidden` (bool, optional)
  - `pageSize` (int, optional)
  - `pageNumber` (int, optional)
- **Response:** `List<SharedOntologyDto>`

**API-002: Pin/Unpin Ontology**
- **Endpoint:** `POST /api/shared-ontologies/{ontologyId}/pin`
- **Auth:** Required
- **Body:** `{ "isPinned": true/false }`
- **Response:** `200 OK` or `400 Bad Request`

**API-003: Hide/Unhide Ontology**
- **Endpoint:** `POST /api/shared-ontologies/{ontologyId}/hide`
- **Auth:** Required
- **Body:** `{ "isHidden": true/false }`
- **Response:** `200 OK` or `400 Bad Request`

**API-004: Get Hidden Ontologies**
- **Endpoint:** `GET /api/shared-ontologies/hidden`
- **Auth:** Required
- **Response:** `List<SharedOntologyDto>`

**Alternative: SignalR Hub Methods (if using existing OntologyHub)**
- **Pro:** Real-time updates when access is granted/revoked
- **Con:** More complex, may be overkill for MVP

### 5.4 Caching Strategy

**CACHE-001: Shared Ontologies List**
- **Cache Key:** `shared-ontologies:{userId}`
- **TTL:** 5 minutes (300 seconds)
- **Invalidation:**
  - User pins/unpins an ontology
  - User hides/unhides an ontology
  - User accesses an ontology (update last accessed)
  - New share link accessed
  - Group membership changes
- **Implementation:** In-memory cache (MemoryCache) or Redis if distributed

**CACHE-002: User State Lookup**
- **Cache Key:** `shared-ontology-state:{userId}:{ontologyId}`
- **TTL:** 10 minutes (600 seconds)
- **Invalidation:** Pin, unpin, hide, unhide, access tracking

**CACHE-003: Permission Level Cache**
- **Leverage Existing:** `OntologyPermissionService` already caches permission checks
- **No additional caching needed**

### 5.5 Database Migration

**MIGRATION-001: Create SharedOntologyUserState Table**
- **File:** `Migrations/YYYYMMDDHHMMSS_AddSharedOntologyUserState.cs`
- **Up:**
  ```csharp
  migrationBuilder.CreateTable(
      name: "SharedOntologyUserStates",
      columns: table => new
      {
          Id = table.Column<int>(nullable: false)
              .Annotation("SqlServer:Identity", "1, 1"),
          UserId = table.Column<string>(nullable: false),
          OntologyId = table.Column<int>(nullable: false),
          IsPinned = table.Column<bool>(nullable: false, defaultValue: false),
          IsHidden = table.Column<bool>(nullable: false, defaultValue: false),
          FirstAccessedAt = table.Column<DateTime>(nullable: true),
          LastAccessedAt = table.Column<DateTime>(nullable: false),
          AccessCount = table.Column<int>(nullable: false, defaultValue: 0),
          CreatedAt = table.Column<DateTime>(nullable: false),
          UpdatedAt = table.Column<DateTime>(nullable: false)
      },
      constraints: table =>
      {
          table.PrimaryKey("PK_SharedOntologyUserStates", x => x.Id);
          table.ForeignKey(
              name: "FK_SharedOntologyUserStates_AspNetUsers_UserId",
              column: x => x.UserId,
              principalTable: "AspNetUsers",
              principalColumn: "Id",
              onDelete: ReferentialAction.Cascade);
          table.ForeignKey(
              name: "FK_SharedOntologyUserStates_Ontologies_OntologyId",
              column: x => x.OntologyId,
              principalTable: "Ontologies",
              principalColumn: "Id",
              onDelete: ReferentialAction.Cascade);
      });

  migrationBuilder.CreateIndex(
      name: "IX_SharedOntologyUserStates_UserId_OntologyId",
      table: "SharedOntologyUserStates",
      columns: new[] { "UserId", "OntologyId" },
      unique: true);

  migrationBuilder.CreateIndex(
      name: "IX_SharedOntologyUserStates_UserId_LastAccessedAt",
      table: "SharedOntologyUserStates",
      columns: new[] { "UserId", "LastAccessedAt" });

  migrationBuilder.CreateIndex(
      name: "IX_SharedOntologyUserStates_UserId_IsPinned",
      table: "SharedOntologyUserStates",
      columns: new[] { "UserId", "IsPinned" });
  ```

**MIGRATION-002: Add Index for Performance**
- **Existing Tables:** Add index to `UserShareAccess.LastAccessedAt` if not already indexed
- **Rationale:** Frequently queried for 90-day filter

---

## 6. Security & Privacy Requirements

### 6.1 Permission Verification

**SEC-001: Verify Access on Display**
- **Requirement:** Before showing an ontology in "Shared with Me", verify user still has access
- **Implementation:**
  - Check `OntologyShare.IsActive = true` for share link access
  - Check user still in `UserGroupMembers` for group access
  - Check `OntologyGroupPermission` still exists
- **Fail-Safe:** If access check fails, don't display ontology (or show "Access Revoked" state)

**SEC-002: Verify Access on Click**
- **Requirement:** When user clicks to open ontology, re-verify access at server side
- **Implementation:**
  - Use existing `OntologyPermissionService.CanViewAsync()` in Ontology detail page
  - If access denied, show error page or redirect to dashboard with error message
- **Security Note:** Never trust client-side permission display; always verify server-side

**SEC-003: Prevent Unauthorized Pin/Hide**
- **Requirement:** User can only pin/hide ontologies they have access to
- **Implementation:**
  - Verify `CanViewAsync()` before allowing pin/hide operation
  - Return 403 Forbidden if user doesn't have access
- **Edge Case:** If access revoked while user viewing list, pin/hide should fail gracefully

### 6.2 Access Control Rules

**SEC-004: Hide Owner's Own Ontologies**
- **Requirement:** "Shared with Me" should NOT include ontologies owned by the user
- **Rationale:** User's own ontologies appear in "My Ontologies" section
- **Implementation:**
  - Filter: `Ontology.UserId != currentUserId`
  - Applies even if user is in a group that has access to their own ontology

**SEC-005: Private Ontology Visibility**
- **Requirement:** Private ontologies should only appear if explicitly shared
- **Rules:**
  - Show if: User has `UserShareAccess` record AND `OntologyShare.IsActive = true`
  - Show if: User in group with `OntologyGroupPermission` AND `Visibility = 'Group'`
  - Do NOT show if: `Visibility = 'Private'` and no explicit access record
- **Fail-Safe:** Double-check visibility in permission service

**SEC-006: Public Ontology Handling**
- **Requirement:** Public ontologies should only appear if user has accessed them
- **Rationale:** Don't clutter "Shared with Me" with ALL public ontologies
- **Implementation:**
  - Show if: User has `SharedOntologyUserState` record (accessed via link or group)
  - Do NOT show: All public ontologies by default

### 6.3 Information Disclosure

**SEC-007: Owner Information Display**
- **Allowed:**
  - Owner display name (from `ApplicationUser.Name` or OAuth profile)
  - Owner initials/avatar (from OAuth photo URL)
- **Not Allowed:**
  - Owner email address (privacy concern)
  - Owner phone number or other PII
  - Owner's other ontologies (unless already shared)

**SEC-008: Group Name Display**
- **Allowed:**
  - Show group name if user is a member of the group
- **Not Allowed:**
  - Show group name if user only has share link access (group is irrelevant)
  - Show group member list (privacy concern - use group management page)

**SEC-009: Access Statistics**
- **Allowed:**
  - User's own access count and last accessed date
- **Not Allowed:**
  - Other users' access counts (privacy concern)
  - Total share link clicks (owner can see this, but not other users)

### 6.4 Privacy of Sharing Metadata

**SEC-010: Hidden State Privacy**
- **Requirement:** User's hidden state is private (other users can't see what user has hidden)
- **Implementation:** `SharedOntologyUserState.IsHidden` only accessible to user who owns the record
- **Exception:** Ontology owner CANNOT see who has hidden their ontology

**SEC-011: Pinned State Privacy**
- **Requirement:** User's pinned state is private
- **Implementation:** `SharedOntologyUserState.IsPinned` only accessible to user who owns the record

**SEC-012: Access Tracking Privacy**
- **Requirement:** User's access history (last accessed, access count) is private
- **Implementation:** Only accessible to the user themselves
- **Exception:** Ontology owner CAN see aggregate access stats on share links (existing feature)

---

## 7. Performance Requirements

### 7.1 Page Load Time Targets

**PERF-001: Initial Dashboard Load**
- **Target:** < 1 second to first paint (FCP)
- **Measurement:** Time from navigation to rendering first ontology card
- **Optimizations:**
  - Server-side query optimization (see PERF-004)
  - Lazy-load "Shared with Me" section (render after "My Ontologies")
  - Use skeleton loaders during data fetch

**PERF-002: Shared Ontologies Section Render**
- **Target:** < 500ms to render 24 ontology cards
- **Measurement:** Time from data received to all cards visible
- **Optimizations:**
  - Virtualization for > 100 items (Phase 2)
  - Efficient rendering (avoid unnecessary re-renders)

**PERF-003: Filter/Sort Application**
- **Target:** < 300ms to apply filter and re-render
- **Measurement:** Time from filter change to updated results
- **Optimizations:**
  - Client-side filtering for small lists (< 50 items)
  - Debounce search input (300ms delay)
  - Server-side filtering for large lists (> 50 items)

### 7.2 Query Performance Targets

**PERF-004: Shared Ontologies Query**
- **Target:** < 200ms database query time
- **Measurement:** Time to execute `GetSharedOntologiesAsync()` query
- **Query Complexity:**
  - Join `UserShareAccess` + `OntologyShare` + `Ontology` + `ApplicationUser`
  - Join `UserGroupMember` + `OntologyGroupPermission` + `Ontology` + `ApplicationUser`
  - Join `SharedOntologyUserState` for pin/hide
  - Filter by 90-day window, hidden status, search query
  - Sort and paginate
- **Optimizations:**
  - Indexes on `UserShareAccess.LastAccessedAt`
  - Indexes on composite keys (see Data Requirements)
  - Use `AsNoTracking()` for read-only queries
  - Limit to 24 results per page
  - Consider split queries if cartesian explosion occurs

**PERF-005: Pin/Unpin Action**
- **Target:** < 100ms roundtrip time
- **Measurement:** Time from click to UI update
- **Optimizations:**
  - Optimistic UI update (change icon immediately)
  - Async server update in background
  - Rollback on failure

**PERF-006: Hide Action**
- **Target:** < 150ms roundtrip time
- **Measurement:** Time from confirmation to card removal
- **Optimizations:**
  - Optimistic UI update (fade card immediately)
  - Async server update in background
  - Rollback on failure

### 7.3 Caching Performance

**PERF-007: Cache Hit Rate**
- **Target:** > 80% cache hit rate for shared ontologies list
- **Measurement:** `CacheHits / (CacheHits + CacheMisses)`
- **Strategy:**
  - Cache shared ontologies list for 5 minutes
  - Invalidate on pin/unpin/hide actions
  - Separate cache per user (avoid cross-contamination)

**PERF-008: Cache Memory Usage**
- **Target:** < 10 MB per user for cached data
- **Measurement:** Size of cached `List<SharedOntologyDto>`
- **Optimization:**
  - Limit cached list to 100 items max
  - Use sliding expiration (5 minutes) to evict old entries

### 7.4 Pagination Thresholds

**PERF-009: Pagination Trigger**
- **Rule:** Paginate if > 24 shared ontologies
- **Page Size:** 24 items per page (4 rows x 6 columns in grid view)
- **Loading:** "Load More" button (not infinite scroll for MVP)
- **Rationale:** 24 items is manageable for DOM and user scanning

**PERF-010: Maximum Items Displayed**
- **Limit:** 100 shared ontologies total
- **Rationale:** Based on volume expectations (typical user: 5-20, max: 100)
- **Fallback:** If > 100, show oldest 100 by last accessed (encourage cleanup)

---

## 8. UI Component Requirements

### 8.1 Component Hierarchy

**COMP-001: SharedWithMeSection Component**
- **File:** `/Components/Shared/SharedWithMeSection.razor`
- **Purpose:** Top-level container for "Shared with Me" section
- **Props:**
  - `@inject ISharedOntologyService SharedOntologyService`
  - `@inject AuthenticationStateProvider AuthenticationStateProvider`
  - `@inject NavigationManager Navigation`
- **State:**
  - `List<SharedOntologyDto> sharedOntologies`
  - `SharedOntologyFilter filter`
  - `bool isLoading`
  - `bool showHidden`
  - `string errorMessage`
- **Child Components:**
  - `SharedOntologyCard` (repeated for each ontology)
  - `SharedOntologyFilters` (search, permission filter, sort)
  - `SharedOntologyEmptyState` (when no ontologies)
  - `SharedOntologyPagination` (if > 24 items)

**COMP-002: SharedOntologyCard Component**
- **File:** `/Components/Shared/SharedOntologyCard.razor`
- **Purpose:** Individual ontology card with pin/hide actions
- **Props:**
  - `SharedOntologyDto Ontology`
  - `EventCallback<int> OnPin`
  - `EventCallback<int> OnUnpin`
  - `EventCallback<int> OnHide`
  - `EventCallback<int> OnClick`
- **State:**
  - `bool showMenu` (three-dot menu)
  - `bool isAnimating` (for pin/hide animations)
- **Child Components:**
  - `OwnerAvatar` (owner info display)
  - `PermissionBadge` (permission level indicator)

**COMP-003: SharedOntologyFilters Component**
- **File:** `/Components/Shared/SharedOntologyFilters.razor`
- **Purpose:** Filter and sort controls
- **Props:**
  - `SharedOntologyFilter Filter` (two-way binding)
  - `EventCallback OnFilterChanged`
- **Elements:**
  - Search input (with debounce)
  - Permission level dropdown
  - Sort dropdown
  - Clear filters button

**COMP-004: SharedOntologyEmptyState Component**
- **File:** `/Components/Shared/SharedOntologyEmptyState.razor`
- **Purpose:** Show helpful message when no shared ontologies
- **Props:**
  - `string EmptyStateType` ("none", "all-hidden", "no-results")
- **Conditional Rendering:** Different message/CTA per type

### 8.2 Reusable Components

**COMP-005: OwnerAvatar Component**
- **File:** `/Components/Shared/OwnerAvatar.razor` (may already exist)
- **Purpose:** Display owner avatar/initials
- **Props:**
  - `string OwnerId`
  - `string OwnerName`
  - `string? OwnerPhotoUrl`
  - `string Size` ("sm", "md", "lg")
- **Rendering:**
  - If `OwnerPhotoUrl` exists, show image
  - Else, show initials in colored circle (like presence indicators)

**COMP-006: PermissionBadge Component**
- **File:** `/Components/Shared/PermissionBadge.razor`
- **Purpose:** Display permission level badge
- **Props:**
  - `PermissionLevel PermissionLevel`
  - `string Size` ("sm", "md")
- **Rendering:**
  - Badge with icon and text
  - Color-coded by permission level
  - Tooltip explaining permission

### 8.3 Bootstrap/Styling Approach

**STYLE-001: Bootstrap Classes**
- **Section Container:** `container-fluid` or `container`
- **Card Grid:** `row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4` (same as My Ontologies)
- **Card:** `card h-100 ontology-card` (reuse existing styles)
- **Badges:** `badge bg-primary`, `badge bg-secondary`, etc.
- **Buttons:** `btn btn-sm btn-outline-primary`, etc.

**STYLE-002: Custom CSS**
- **File:** `/wwwroot/css/shared-ontologies.css` (optional, or add to `site.css`)
- **Styles:**
  - `.shared-badge` - Shared badge styling
  - `.permission-badge-view`, `.permission-badge-edit`, etc. - Permission badge variants
  - `.pin-icon` - Pin icon hover/active states
  - `.card-fade-out` - Animation for hiding card
  - `.card-slide-up` - Animation for pinning card

**STYLE-003: Icons**
- **Icon Library:** Bootstrap Icons (existing)
- **Icons Needed:**
  - `bi-share` - Shared badge
  - `bi-star`, `bi-star-fill` - Pin/unpin
  - `bi-eye-slash` - Hide
  - `bi-eye` - Unhide/show
  - `bi-lock`, `bi-unlock`, `bi-key` - Permission indicators
  - `bi-three-dots-vertical` - Kebab menu

### 8.4 Accessibility

**A11Y-001: Semantic HTML**
- Use `<section>` for "Shared with Me" section
- Use `<h2>` for section heading
- Use `<button>` for interactive elements (not `<div>` with click handlers)
- Use `<ul>` and `<li>` for card grid (or `role="list"`)

**A11Y-002: ARIA Labels**
- Pin button: `aria-label="Pin ontology"` or `aria-label="Unpin ontology"`
- Hide button: `aria-label="Hide ontology from Shared with Me"`
- Filter dropdown: `aria-label="Filter by permission level"`
- Search input: `aria-label="Search shared ontologies"`

**A11Y-003: Keyboard Navigation**
- All interactive elements (cards, buttons, dropdowns) focusable with Tab
- Pin icon toggleable with Space or Enter
- Card opens ontology with Space or Enter
- Escape closes three-dot menu

**A11Y-004: Screen Reader Support**
- Announce filter changes: "Showing 12 results for 'medical'"
- Announce pin state: "Ontology pinned" (via live region)
- Announce hide action: "Ontology hidden. Undo" (via toast notification)
- Badge tooltips readable by screen readers

**A11Y-005: Color Contrast**
- All badges meet WCAG AA contrast ratio (4.5:1 for normal text)
- Permission badges use icon + text (not color alone)
- Pin icon uses shape (star) not just color

---

## 9. Integration Requirements

### 9.1 Integration with Existing Dashboard

**INT-001: Home.razor Modification**
- **Location:** Insert `<SharedWithMeSection />` component below "My Ontologies" section
- **Placement:** After line ~780 (end of "My Ontologies" grid/list view)
- **Conditional Rendering:**
  ```razor
  @if (isAuthenticated)
  {
      <SharedWithMeSection />
  }
  ```
- **Rationale:** Only show for logged-in users (unauthenticated users can't have shared ontologies)

**INT-002: Dashboard Controls Consistency**
- **Requirement:** "Shared with Me" controls match "My Ontologies" controls
- **Shared Controls:**
  - Search input (same styling)
  - Sort dropdown (same options where applicable)
  - View toggle (grid/list) - shared with "My Ontologies" or separate?
- **Decision Point:** Should view mode (grid/list) be shared across sections or independent?
  - **Recommendation:** Independent (user might prefer grid for own ontologies, list for shared)

**INT-003: Folder Sidebar Integration**
- **Current State:** Folder sidebar shows "My Ontologies", "Shared with Me", "Public Ontologies"
- **Requirement:** Update folder sidebar to reflect shared ontology count
- **Implementation:**
  - `sharedWithMeCount` already calculated in `Home.razor` (line 1098)
  - Ensure count is accurate (only non-hidden, accessed in 90 days)
  - Update count when user pins/hides ontologies
- **Click Behavior:** Clicking "Shared with Me" in sidebar scrolls to section or filters view

### 9.2 Integration with OntologyPermissionService

**INT-004: Permission Checking**
- **Requirement:** Use existing `OntologyPermissionService` for all access checks
- **Methods Used:**
  - `CanViewAsync(ontologyId, userId)` - Verify user can see ontology
  - `CanEditAsync(ontologyId, userId)` - Determine permission level
  - `GetPermissionLevelAsync(ontologyId, userId)` - Display permission badge
- **Integration Point:** `SharedOntologyService.GetSharedOntologiesAsync()` calls permission service

**INT-005: Permission Level Mapping**
- **Requirement:** Map permission enums to display strings
- **Mapping:**
  - `PermissionLevel.View` → "View"
  - `PermissionLevel.ViewAndAdd` → "Can Add"
  - `PermissionLevel.ViewAddEdit` → "Can Edit"
  - `PermissionLevel.FullAccess` → "Manage"
- **Implementation:** Static helper method or extension method

### 9.3 Integration with Share Link System

**INT-006: Track Share Link Access**
- **Current Behavior:** `UserShareAccess` created when user first accesses share link
- **Requirement:** Also create/update `SharedOntologyUserState` on share link access
- **Integration Point:** `OntologyShareService` or middleware that handles share link access
- **Action:**
  1. User clicks share link → navigates to `/ontology/{id}?shareToken={token}`
  2. Server validates token, creates `UserShareAccess` record (existing)
  3. **NEW:** Server also creates/updates `SharedOntologyUserState` record
  4. User sees ontology detail page

**INT-007: Share Link Revocation**
- **Current Behavior:** Setting `OntologyShare.IsActive = false` revokes access
- **Requirement:** Revoked share links should not appear in "Shared with Me"
- **Implementation:** Filter query checks `IsActive = true`
- **User Experience:** If user tries to access revoked share link, show error page
- **No Changes Needed:** Existing permission checks already handle this

### 9.4 Integration with Group Permission System

**INT-008: Track Group Access**
- **Current Behavior:** User gains access when added to `UserGroupMember` with `OntologyGroupPermission`
- **Requirement:** Create/update `SharedOntologyUserState` when user first accesses ontology via group
- **Integration Point:** When user navigates to `/ontology/{id}` and has group access
- **Action:**
  1. User clicks on ontology (from search, public list, or direct URL)
  2. Server checks permission via `OntologyPermissionService.CanViewAsync()`
  3. **NEW:** If user has group access, create/update `SharedOntologyUserState` record
  4. User sees ontology detail page

**INT-009: Group Membership Revocation**
- **Current Behavior:** Removing user from `UserGroupMembers` revokes access
- **Requirement:** Revoked group access should not appear in "Shared with Me"
- **Implementation:**
  - Filter query joins `UserGroupMembers` to ensure user still in group
  - If user removed from group, ontology disappears from "Shared with Me"
- **User Experience:** If user tries to access ontology after removal, show error page
- **No Changes Needed:** Existing permission checks already handle this

### 9.5 Integration with Ontology Detail Page

**INT-010: Update Last Accessed Timestamp**
- **Requirement:** When user clicks "View & Edit" button on shared ontology card, update `LastAccessedAt`
- **Integration Point:** `OntologyService` or middleware on ontology detail page load
- **Implementation:**
  ```csharp
  // In OnInitializedAsync() of Ontology.razor or OntologyHub
  if (IsSharedOntology(ontologyId, userId))
  {
      await SharedOntologyService.TrackAccessAsync(ontologyId, userId);
  }
  ```

**INT-011: Breadcrumb/Navigation Context**
- **Requirement:** When user navigates from "Shared with Me" to ontology, show context
- **Optional Enhancement (Phase 2):**
  - Breadcrumb: "Home > Shared with Me > [Ontology Name]"
  - Back button: "Back to Shared with Me" instead of generic "Back"
- **Implementation:** Use query parameter `?from=shared` to track navigation source

---

## 10. Acceptance Criteria

### 10.1 Core Display - Acceptance Criteria

**AC-001: Display Shared Ontologies**
- **Given:** User has accessed 3 ontologies via share link and 2 via group in the last 90 days
- **When:** User navigates to the dashboard
- **Then:**
  - ✅ "Shared with Me" section is visible below "My Ontologies"
  - ✅ Section displays 5 ontology cards
  - ✅ Each card shows ontology name, description, owner name, shared badge
  - ✅ Cards are sorted by last accessed (most recent first)

**AC-002: Exclude User's Own Ontologies**
- **Given:** User owns an ontology and is also in a group that has access to it
- **When:** User views "Shared with Me" section
- **Then:**
  - ✅ User's own ontology does NOT appear in "Shared with Me"
  - ✅ Only ontologies owned by others appear

**AC-003: 90-Day Filter**
- **Given:** User accessed ontology A 30 days ago and ontology B 100 days ago
- **When:** User views "Shared with Me" section
- **Then:**
  - ✅ Ontology A appears in the list
  - ✅ Ontology B does NOT appear (outside 90-day window)

**AC-004: Empty State**
- **Given:** User has never accessed a shared ontology
- **When:** User views the dashboard
- **Then:**
  - ✅ "Shared with Me" section is NOT visible (or shows empty state message)
  - ✅ Empty state shows helpful message and CTA

### 10.2 Pin Functionality - Acceptance Criteria

**AC-005: Pin Ontology**
- **Given:** User has 5 shared ontologies in the list
- **When:** User clicks the star icon on ontology A
- **Then:**
  - ✅ Star icon changes to filled/gold (visual feedback)
  - ✅ Ontology A moves to the top of the list
  - ✅ Pin state persists after page refresh
  - ✅ Other users do NOT see this user's pin state

**AC-006: Unpin Ontology**
- **Given:** User has pinned ontology A
- **When:** User clicks the filled star icon on ontology A
- **Then:**
  - ✅ Star icon changes to outline (visual feedback)
  - ✅ Ontology A moves back to its sorted position (by last accessed)
  - ✅ Unpin state persists after page refresh

**AC-007: Multiple Pinned Ontologies**
- **Given:** User has pinned ontologies A, B, and C
- **When:** User views "Shared with Me" section
- **Then:**
  - ✅ Pinned ontologies appear at the top
  - ✅ Within pinned section, ontologies are sorted by last accessed
  - ✅ Unpinned ontologies appear below pinned ontologies

### 10.3 Hide Functionality - Acceptance Criteria

**AC-008: Hide Ontology**
- **Given:** User has 5 shared ontologies in the list
- **When:** User clicks three-dot menu → "Hide" → confirms
- **Then:**
  - ✅ Ontology fades out and disappears from list
  - ✅ Toast notification shows "Ontology hidden. [Undo]"
  - ✅ Hidden state persists after page refresh
  - ✅ Hidden ontology does NOT appear in list on subsequent visits

**AC-009: Unhide Ontology**
- **Given:** User has hidden ontology A
- **When:** User clicks "Show Hidden" → clicks "Unhide" on ontology A
- **Then:**
  - ✅ Ontology A reappears in main "Shared with Me" list
  - ✅ Ontology A is sorted by last accessed (not pinned)
  - ✅ Unhide state persists after page refresh

**AC-010: Undo Hide (Optional - Phase 2)**
- **Given:** User just hid ontology A (toast notification visible)
- **When:** User clicks "Undo" in toast notification (within 5 seconds)
- **Then:**
  - ✅ Ontology A reappears in list immediately
  - ✅ Hide action is reversed (server-side)

### 10.4 Permission Display - Acceptance Criteria

**AC-011: Permission Badge Display**
- **Given:** User has View, Can Edit, and Manage access to 3 different ontologies
- **When:** User views "Shared with Me" section
- **Then:**
  - ✅ Each ontology shows correct permission badge (View, Can Edit, Manage)
  - ✅ Badges are color-coded (gray for View, green for Edit, purple for Manage)
  - ✅ Hovering badge shows tooltip explaining permission

**AC-012: Permission Filter**
- **Given:** User has 10 shared ontologies with mixed permission levels
- **When:** User selects "Can Edit" from permission filter dropdown
- **Then:**
  - ✅ Only ontologies with "Can Edit" or "Manage" permission are shown
  - ✅ Count updates: "Showing 4 of 10 shared ontologies"
  - ✅ Filter applies on top of search and sort

### 10.5 Owner Information - Acceptance Criteria

**AC-013: Owner Display**
- **Given:** Ontology is owned by user "Jane Doe" with OAuth photo
- **When:** User views ontology card
- **Then:**
  - ✅ Card shows "Owner: Jane Doe" or "Shared by Jane Doe"
  - ✅ Card shows Jane's avatar/photo (or initials if no photo)
  - ✅ Owner's email is NOT shown (privacy)

**AC-014: Sharing Method Display (Phase 2)**
- **Given:** User accessed ontology via share link
- **When:** User views ontology card
- **Then:**
  - ✅ Card shows "Shared via link" indicator
- **Given:** User accessed ontology via group "Medical Team"
- **Then:**
  - ✅ Card shows "Shared via group: Medical Team" indicator

### 10.6 Sorting and Filtering - Acceptance Criteria

**AC-015: Sort by Last Accessed**
- **Given:** User has 5 shared ontologies with different last accessed dates
- **When:** User selects "Last Accessed (most recent first)" from sort dropdown
- **Then:**
  - ✅ Ontologies are sorted with most recently accessed first
  - ✅ Pinned ontologies remain at top, then sorted

**AC-016: Sort by Name**
- **Given:** User has ontologies named "Zebra", "Apple", "Medical"
- **When:** User selects "Name (A-Z)" from sort dropdown
- **Then:**
  - ✅ Ontologies are sorted alphabetically: Apple, Medical, Zebra
  - ✅ Pinned ontologies remain at top, then sorted

**AC-017: Search**
- **Given:** User has 10 shared ontologies
- **When:** User types "medical" in search box
- **Then:**
  - ✅ Only ontologies with "medical" in name/description/owner name are shown
  - ✅ Search is case-insensitive
  - ✅ Count updates: "Showing 2 of 10 shared ontologies"
  - ✅ Clearing search restores full list

### 10.7 Click to Open - Acceptance Criteria

**AC-018: Navigate to Ontology**
- **Given:** User clicks on a shared ontology card
- **When:** Navigation completes
- **Then:**
  - ✅ User is taken to `/ontology/{id}` page
  - ✅ Ontology detail page shows correct permission level (read-only if View, editable if Edit)
  - ✅ Last accessed timestamp is updated in database
  - ✅ Access count is incremented

**AC-019: Revoked Access Handling**
- **Given:** User had access to ontology A, but share link was revoked
- **When:** User tries to click on ontology A card
- **Then:**
  - ✅ User sees error message: "Access to this ontology has been revoked"
  - ✅ User is NOT able to view ontology content
  - ✅ Card shows "Access Revoked" badge (or is removed from list)

### 10.8 Performance - Acceptance Criteria

**AC-020: Page Load Performance**
- **Given:** User has 24 shared ontologies
- **When:** User navigates to dashboard
- **Then:**
  - ✅ "Shared with Me" section loads in < 1 second
  - ✅ All 24 cards render in < 500ms after data received
  - ✅ No janky scrolling or layout shifts

**AC-021: Filter Performance**
- **Given:** User has 50 shared ontologies
- **When:** User types in search box or changes filter
- **Then:**
  - ✅ Results update in < 300ms
  - ✅ No noticeable lag or freezing

### 10.9 Mobile Responsiveness - Acceptance Criteria

**AC-022: Mobile Layout**
- **Given:** User views dashboard on mobile (< 768px width)
- **When:** User scrolls to "Shared with Me" section
- **Then:**
  - ✅ Cards stack in single column (full width)
  - ✅ Filters/sort controls stack vertically
  - ✅ Pin/hide buttons are touch-friendly (≥ 44x44px)
  - ✅ All functionality works on touch devices

---

## 11. Edge Cases & Error Handling

### 11.1 No Shared Ontologies

**EDGE-001: User Has Never Accessed Shared Ontology**
- **Scenario:** New user or user who has never clicked a share link
- **Behavior:**
  - "Shared with Me" section is not rendered (or shows empty state)
  - Dashboard shows "My Ontologies" section only
  - Optional: Promotional message encouraging collaboration

**EDGE-002: All Shared Ontologies Outside 90-Day Window**
- **Scenario:** User accessed shared ontologies 6 months ago, hasn't accessed since
- **Behavior:**
  - "Shared with Me" section is not rendered (or shows empty state)
  - Message: "No recently accessed shared ontologies. Shared ontologies you haven't accessed in 90 days are hidden."

**EDGE-003: All Shared Ontologies Hidden**
- **Scenario:** User has hidden all their shared ontologies
- **Behavior:**
  - Section shows empty state: "All shared ontologies are hidden"
  - "Show Hidden" button is prominent
  - Clicking shows list of hidden ontologies with "Unhide" action

### 11.2 Many Shared Ontologies

**EDGE-004: User Has 100+ Shared Ontologies**
- **Scenario:** Power user in large organization with many group shares
- **Behavior:**
  - Show first 24 ontologies (1 page)
  - "Load More" button to load next 24
  - After 100 ontologies, show warning: "Showing 100 most recent. Use search/filters to find older ontologies."
  - Encourage user to hide unwanted ontologies

**EDGE-005: Pagination Edge Cases**
- **Scenario:** User has 25 ontologies (just over 1 page)
- **Behavior:**
  - First page shows 24 ontologies
  - "Load More" button shows "1 more ontology"
  - Clicking loads 1 ontology
  - Button disappears

### 11.3 Revoked Shares

**EDGE-006: Share Link Deactivated While Viewing List**
- **Scenario:** User is viewing "Shared with Me" list, admin deactivates share link
- **Behavior:**
  - Ontology remains in list until page refresh (cached)
  - On refresh, ontology disappears from list
  - If user tries to click, access denied error
  - Optional: Real-time removal via SignalR (Phase 2)

**EDGE-007: User Removed from Group While Viewing List**
- **Scenario:** User is viewing list, admin removes user from group
- **Behavior:**
  - Same as EDGE-006
  - Ontology remains until refresh, then disappears
  - Access denied on click

**EDGE-008: Share Link Expired**
- **Scenario:** Share link has `ExpiresAt` date in the past
- **Behavior:**
  - Ontology does NOT appear in "Shared with Me" (filtered out)
  - If user tries to access via saved URL, show "Link Expired" error
  - Badge on card: "Expired" (red badge) - if we choose to show expired links (Phase 2 decision)

### 11.4 Deleted Ontologies

**EDGE-009: Ontology Deleted by Owner**
- **Scenario:** Owner deletes ontology that was shared with user
- **Behavior:**
  - `SharedOntologyUserState` record is cascade deleted (FK constraint)
  - Ontology disappears from "Shared with Me" list
  - No error needed (seamless removal)

**EDGE-010: User Tries to Access Deleted Ontology**
- **Scenario:** User has old bookmark to deleted ontology
- **Behavior:**
  - 404 Not Found page
  - Message: "This ontology has been deleted."
  - Link back to dashboard

### 11.5 Changed Permissions

**EDGE-011: Permission Downgraded from Edit to View**
- **Scenario:** Owner changes share link from "Can Edit" to "View Only"
- **Behavior:**
  - Permission badge updates to "View" on next load
  - User can still access ontology, but in read-only mode
  - Optional: Notification to user about permission change (Phase 2)

**EDGE-012: Permission Upgraded from View to Edit**
- **Scenario:** Owner changes share link from "View" to "Can Edit"
- **Behavior:**
  - Permission badge updates to "Can Edit" on next load
  - User sees edit buttons in ontology detail page
  - Optional: Notification to user about permission change (Phase 2)

### 11.6 Network Errors

**EDGE-013: Failed to Load Shared Ontologies**
- **Scenario:** Database query fails or network timeout
- **Behavior:**
  - Show error alert: "Failed to load shared ontologies. Please try again."
  - "Retry" button to re-attempt query
  - Log error for debugging
  - Don't show empty state (confusing)

**EDGE-014: Failed to Pin/Unpin**
- **Scenario:** Server error or network failure during pin action
- **Behavior:**
  - Optimistic UI reverts (star icon changes back)
  - Toast error: "Failed to pin ontology. Please try again."
  - User can retry action
  - Log error for debugging

**EDGE-015: Failed to Hide**
- **Scenario:** Server error during hide action
- **Behavior:**
  - Card animation reverses (fades back in)
  - Toast error: "Failed to hide ontology. Please try again."
  - User can retry action

### 11.7 Concurrent Access

**EDGE-016: Multiple Browser Tabs**
- **Scenario:** User has dashboard open in 2 tabs, pins ontology in tab 1
- **Behavior:**
  - Tab 1: Immediate UI update (optimistic)
  - Tab 2: No change (not real-time for MVP)
  - Tab 2 refreshes: Pin state updates
  - Phase 2: SignalR real-time sync

**EDGE-017: Multiple Users Accessing Same Ontology**
- **Scenario:** User A and User B both have access to ontology X
- **Behavior:**
  - User A pins ontology X → only affects User A's view
  - User B does NOT see User A's pin state
  - Each user has independent state (`SharedOntologyUserState` per user)

### 11.8 Browser/Client Issues

**EDGE-018: JavaScript Disabled**
- **Scenario:** User has JavaScript disabled (Blazor Server requires JS)
- **Behavior:**
  - Blazor won't load (existing issue, not specific to this feature)
  - Show message: "This application requires JavaScript to function."

**EDGE-019: Slow Network Connection**
- **Scenario:** User on slow 3G connection
- **Behavior:**
  - Show loading skeleton during data fetch
  - Cards fade in as data arrives
  - No error unless timeout (> 30 seconds)
  - Optimistic UI for pin/hide helps perceived performance

**EDGE-020: Browser Cache Stale**
- **Scenario:** User has old cached data
- **Behavior:**
  - Server-side rendering ensures fresh data on page load
  - Cache invalidation on pin/hide/access actions
  - Users won't see stale data (Blazor Server advantage)

---

## 12. Testing Requirements

### 12.1 Unit Tests

**TEST-001: SharedOntologyService Tests**
- **Test File:** `Eidos.Tests/Services/SharedOntologyServiceTests.cs`
- **Test Cases:**
  - ✅ `GetSharedOntologiesAsync_ReturnsDirectShareLinkOntologies`
  - ✅ `GetSharedOntologiesAsync_ReturnsGroupSharedOntologies`
  - ✅ `GetSharedOntologiesAsync_ExcludesOwnedOntologies`
  - ✅ `GetSharedOntologiesAsync_Applies90DayFilter`
  - ✅ `GetSharedOntologiesAsync_ExcludesHiddenOntologies`
  - ✅ `GetSharedOntologiesAsync_AppliesSearchFilter`
  - ✅ `GetSharedOntologiesAsync_AppliesPermissionFilter`
  - ✅ `GetSharedOntologiesAsync_SortsByLastAccessed`
  - ✅ `GetSharedOntologiesAsync_SortsByName`
  - ✅ `GetSharedOntologiesAsync_PaginatesResults`
  - ✅ `PinOntologyAsync_CreatesStateRecord`
  - ✅ `PinOntologyAsync_UpdatesExistingRecord`
  - ✅ `UnpinOntologyAsync_UpdatesStateRecord`
  - ✅ `HideOntologyAsync_SetsHiddenFlag`
  - ✅ `UnhideOntologyAsync_ClearsHiddenFlag`
  - ✅ `TrackAccessAsync_UpdatesLastAccessedAt`
  - ✅ `TrackAccessAsync_IncrementsAccessCount`
  - ✅ `GetOrCreateStateAsync_CreatesNewRecord`
  - ✅ `GetOrCreateStateAsync_ReturnsExistingRecord`

**TEST-002: SharedOntologyUserStateRepository Tests**
- **Test File:** `Eidos.Tests/Repositories/SharedOntologyUserStateRepositoryTests.cs`
- **Test Cases:**
  - ✅ `GetByUserAndOntologyAsync_ReturnsRecord`
  - ✅ `GetByUserAndOntologyAsync_ReturnsNullIfNotFound`
  - ✅ `GetByUserAsync_ReturnsAllUserRecords`
  - ✅ `GetPinnedByUserAsync_ReturnsOnlyPinned`
  - ✅ `GetHiddenByUserAsync_ReturnsOnlyHidden`
  - ✅ `UpsertAsync_CreatesNewRecord`
  - ✅ `UpsertAsync_UpdatesExistingRecord`

**TEST-003: OntologyPermissionService Integration Tests**
- **Test File:** `Eidos.Tests/Services/OntologyPermissionServiceTests.cs` (add to existing)
- **Test Cases:**
  - ✅ `GetSharedOntologiesAsync_CombinesShareLinkAndGroupAccess`
  - ✅ `GetSharedOntologiesAsync_RespectsPermissionChecks`
  - ✅ `GetSharedOntologiesAsync_HandlesRevokedAccess`

### 12.2 Integration Tests

**TEST-004: End-to-End Shared Ontology Workflow**
- **Test File:** `Eidos.Tests/Integration/SharedOntologyWorkflowTests.cs`
- **Test Scenario:**
  1. Create test user A and user B
  2. User A creates ontology
  3. User A shares ontology with user B via share link
  4. User B accesses share link
  5. Verify ontology appears in user B's "Shared with Me"
  6. User B pins ontology
  7. Verify pin state persists
  8. User B hides ontology
  9. Verify ontology disappears from list
  10. User B unhides ontology
  11. Verify ontology reappears

**TEST-005: Group Permission Workflow**
- **Test File:** `Eidos.Tests/Integration/GroupPermissionWorkflowTests.cs`
- **Test Scenario:**
  1. Create test user A and user B
  2. User A creates ontology
  3. User A creates group and adds user B
  4. User A grants group permission to ontology
  5. User B accesses ontology
  6. Verify ontology appears in user B's "Shared with Me"
  7. User A removes user B from group
  8. Verify ontology disappears from user B's "Shared with Me"

**TEST-006: Permission Change Workflow**
- **Test File:** `Eidos.Tests/Integration/PermissionChangeWorkflowTests.cs`
- **Test Scenario:**
  1. Share ontology with "View" permission
  2. Verify permission badge shows "View"
  3. Upgrade permission to "Can Edit"
  4. Verify permission badge updates to "Can Edit"
  5. Downgrade back to "View"
  6. Verify permission badge reverts to "View"

### 12.3 UI Component Tests (bUnit)

**TEST-007: SharedWithMeSection Component Tests**
- **Test File:** `Eidos.Tests/Components/SharedWithMeSectionTests.cs`
- **Test Cases:**
  - ✅ `RendersOntologyCards_WhenDataLoaded`
  - ✅ `ShowsEmptyState_WhenNoSharedOntologies`
  - ✅ `AppliesSearchFilter_OnInputChange`
  - ✅ `AppliesPermissionFilter_OnDropdownChange`
  - ✅ `SortsOntologies_OnSortChange`
  - ✅ `LoadsMoreOntologies_OnLoadMoreClick`
  - ✅ `ShowsLoadingState_WhileFetching`
  - ✅ `ShowsErrorState_OnFetchFailure`

**TEST-008: SharedOntologyCard Component Tests**
- **Test File:** `Eidos.Tests/Components/SharedOntologyCardTests.cs`
- **Test Cases:**
  - ✅ `DisplaysOntologyInfo_Correctly`
  - ✅ `DisplaysSharedBadge`
  - ✅ `DisplaysPermissionBadge_WithCorrectColor`
  - ✅ `DisplaysOwnerInfo_WithAvatar`
  - ✅ `ShowsPinIcon_Unpinned_ByDefault`
  - ✅ `ShowsPinIcon_Filled_WhenPinned`
  - ✅ `InvokesOnPin_WhenStarClicked`
  - ✅ `InvokesOnClick_WhenCardClicked`
  - ✅ `ShowsMenu_OnThreeDotsClick`
  - ✅ `InvokesOnHide_WhenHideMenuItemClicked`

**TEST-009: PermissionBadge Component Tests**
- **Test File:** `Eidos.Tests/Components/PermissionBadgeTests.cs`
- **Test Cases:**
  - ✅ `RendersBadge_View_AsGray`
  - ✅ `RendersBadge_CanEdit_AsGreen`
  - ✅ `RendersBadge_Manage_AsPurple`
  - ✅ `ShowsTooltip_OnHover`

### 12.4 Performance Tests

**TEST-010: Query Performance Tests**
- **Test File:** `Eidos.Tests/Performance/SharedOntologyQueryPerformanceTests.cs`
- **Test Cases:**
  - ✅ `GetSharedOntologiesAsync_CompletesWith50Items_UnderTargetTime` (< 200ms)
  - ✅ `GetSharedOntologiesAsync_CompletesWith100Items_UnderTargetTime` (< 300ms)
  - ✅ `GetSharedOntologiesAsync_WithComplexFilters_UnderTargetTime` (< 300ms)
  - ✅ `PinOntologyAsync_CompletesUnderTargetTime` (< 100ms)

**TEST-011: Load Tests (Optional - Manual or Automated)**
- **Tool:** k6, JMeter, or Artillery
- **Scenarios:**
  - 100 concurrent users loading "Shared with Me" section
  - Measure: Response time, throughput, error rate
  - Target: 95th percentile < 1 second

### 12.5 Accessibility Tests

**TEST-012: Automated Accessibility Tests**
- **Tool:** axe-core or pa11y
- **Test Cases:**
  - ✅ No color contrast violations (WCAG AA)
  - ✅ All interactive elements keyboard accessible
  - ✅ All images/icons have alt text or aria-labels
  - ✅ Form controls have associated labels
  - ✅ Headings in logical order (h2 for section, h3 for cards)

**TEST-013: Manual Accessibility Tests**
- **Test Cases:**
  - ✅ Tab through all interactive elements in logical order
  - ✅ Pin/unpin works with keyboard (Space or Enter)
  - ✅ Screen reader announces "Shared with Me" section
  - ✅ Screen reader announces pin state changes
  - ✅ High contrast mode works correctly

### 12.6 Browser Compatibility Tests

**TEST-014: Cross-Browser Testing**
- **Browsers:**
  - Chrome (latest)
  - Firefox (latest)
  - Safari (latest)
  - Edge (latest)
- **Test Cases:**
  - ✅ Layout renders correctly
  - ✅ Pin/hide animations work smoothly
  - ✅ Filters apply correctly
  - ✅ No console errors

**TEST-015: Mobile Browser Testing**
- **Devices:**
  - iOS Safari (iPhone)
  - Chrome Mobile (Android)
- **Test Cases:**
  - ✅ Responsive layout (single column)
  - ✅ Touch targets ≥ 44x44px
  - ✅ Scroll performance smooth
  - ✅ All functionality works

### 12.7 Security Tests

**TEST-016: Authorization Tests**
- **Test File:** `Eidos.Tests/Security/SharedOntologyAuthorizationTests.cs`
- **Test Cases:**
  - ✅ `UserCannotPinOntology_WithoutAccess`
  - ✅ `UserCannotHideOntology_WithoutAccess`
  - ✅ `UserCannotSeeOtherUsersHiddenState`
  - ✅ `UserCannotSeeOtherUsersPinnedState`
  - ✅ `RevokedAccessDoesNotAppearInList`
  - ✅ `PrivateOntologiesRequireExplicitShare`

---

## 13. Documentation Requirements

### 13.1 User-Facing Documentation Updates

**DOC-001: User Guide - New Section**
- **File:** `/Components/Pages/UserGuide.razor` or separate markdown
- **Section:** "Working with Shared Ontologies"
- **Topics:**
  - What is "Shared with Me"?
  - How to find shared ontologies
  - Understanding permission levels (View, Can Edit, Manage)
  - Pinning important ontologies
  - Hiding unwanted ontologies
  - Unhiding previously hidden ontologies
  - How sharing works (link vs group)
- **Screenshots:**
  - Annotated screenshot of "Shared with Me" section
  - Example of pinned ontology
  - Permission badge explanation

**DOC-002: Features Page Update**
- **File:** `/Components/Pages/Features.razor`
- **Update:** Add "Shared with Me Dashboard" to collaboration features section
- **Content:**
  - Brief description (2-3 sentences)
  - Key benefits (discoverability, organization)
  - Link to user guide for details

**DOC-003: Release Notes Entry**
- **File:** `/Components/Shared/ReleaseNotes.razor`
- **Version:** Next release (e.g., v1.15.0)
- **Entry:**
  ```markdown
  ## Shared with Me Dashboard (November 2025)

  ### New Feature: Shared with Me
  - **Dashboard Section:** Dedicated section on the home page showing all ontologies shared with you
  - **Pin/Favorite:** Pin important shared ontologies to keep them at the top
  - **Hide/Dismiss:** Hide unwanted ontologies from your list
  - **Permission Indicators:** Clear badges showing your access level (View, Can Edit, Manage)
  - **Owner Information:** See who shared each ontology with you
  - **Smart Filtering:** Filter by permission level, search by name, sort by recency
  - **90-Day Window:** Only shows ontologies accessed in the last 90 days (keeps list clean)

  ### Benefits
  - Find shared ontologies easily without hunting for bookmarks
  - Quickly revisit collaborative work
  - Understand your access level at a glance
  - Organize shared ontologies the way you want
  ```

### 13.2 Developer Documentation

**DOC-004: Technical Documentation**
- **File:** `DEVELOPMENT_LEDGER.md`
- **Section:** New entry at top
- **Content:**
  - Feature overview
  - Architecture decisions
  - New services and repositories
  - Database schema changes
  - Integration points
  - Performance considerations
  - Future enhancements

**DOC-005: CLAUDE.md Update**
- **File:** `CLAUDE.md`
- **Sections to Update:**
  - "Core Features" - Add "Shared with Me Dashboard"
  - "Project Structure" - Add new services/components
  - "Database Schema" - Add `SharedOntologyUserState` table
  - "Recent Major Features" - Add entry with date and description

**DOC-006: API Documentation (if applicable)**
- **File:** XML comments in service classes
- **Requirements:**
  - All public methods have `<summary>` tags
  - All parameters have `<param>` tags
  - All return values have `<returns>` tags
  - All exceptions have `<exception>` tags
- **Example:**
  ```csharp
  /// <summary>
  /// Gets all ontologies shared with the specified user.
  /// </summary>
  /// <param name="userId">The ID of the user</param>
  /// <param name="filter">Filter criteria for shared ontologies</param>
  /// <returns>List of shared ontologies with user state and permission info</returns>
  /// <exception cref="ArgumentNullException">If userId is null or empty</exception>
  public async Task<List<SharedOntologyDto>> GetSharedOntologiesAsync(
      string userId, SharedOntologyFilter filter)
  ```

### 13.3 Help System Updates

**DOC-007: In-App Help**
- **File:** `/Components/Pages/Help.razor` or tooltip system
- **Updates:**
  - Add "Shared with Me" section to help topics
  - Add tooltips for pin/hide buttons
  - Add permission badge explanations
  - Add contextual help for filters

**DOC-008: FAQ Updates**
- **File:** FAQ page or help docs
- **New FAQs:**
  - Q: Why don't I see a shared ontology in my list?
    - A: It may be outside the 90-day window, hidden, or access may have been revoked
  - Q: What does "Shared via link" vs "Shared via group" mean?
    - A: Explains the two sharing methods
  - Q: Can other users see my pinned/hidden state?
    - A: No, your preferences are private
  - Q: How do I get access to more shared ontologies?
    - A: Join collaboration board, ask colleagues to share, or browse public ontologies

### 13.4 Comment Preservation

**DOC-009: Inline Code Comments**
- **Requirement:** Preserve all existing inline comments in modified files
- **New Comments:** Add comments explaining complex logic
- **Example:**
  ```csharp
  // Check if user has access via both share link AND group
  // If both exist, prioritize group permission (usually higher access level)
  var sharePermission = GetShareLinkPermission(userId, ontologyId);
  var groupPermission = GetGroupPermission(userId, ontologyId);
  var finalPermission = (groupPermission ?? sharePermission) ?? PermissionLevel.View;
  ```

---

## 14. Implementation Phases

### 14.1 Phase 1: MVP (Core Features) - Priority P0

**Duration:** 2-3 weeks
**Goal:** Launch functional "Shared with Me" section with essential features

**Deliverables:**
1. **Database & Backend (Week 1)**
   - ✅ Create `SharedOntologyUserState` table and migration
   - ✅ Implement `SharedOntologyUserStateRepository`
   - ✅ Implement `SharedOntologyService` with core methods
   - ✅ Extend `OntologyPermissionService` with shared ontologies query
   - ✅ Add access tracking on ontology view (update last accessed)
   - ✅ Unit tests for service and repository

2. **UI Components (Week 2)**
   - ✅ Create `SharedWithMeSection` component
   - ✅ Create `SharedOntologyCard` component
   - ✅ Create `PermissionBadge` component
   - ✅ Reuse `OwnerAvatar` component (or create if needed)
   - ✅ Implement basic layout (grid view)
   - ✅ Implement shared badge display

3. **Features (Week 2)**
   - ✅ Display shared ontologies (share link + group)
   - ✅ 90-day filter
   - ✅ Exclude owned ontologies
   - ✅ Click to open ontology
   - ✅ Show owner information
   - ✅ Show permission level badge
   - ✅ Sort by last accessed (default)

4. **Pin/Hide Functionality (Week 3)**
   - ✅ Implement pin/unpin action
   - ✅ Implement hide/unhide action
   - ✅ Pin state persistence
   - ✅ Hidden state persistence
   - ✅ "Show Hidden" toggle
   - ✅ Optimistic UI updates

5. **Filtering & Search (Week 3)**
   - ✅ Permission level filter
   - ✅ Search by name/description/owner
   - ✅ Sort options (last accessed, name)
   - ✅ Clear filters button

6. **Testing & Polish (Week 3)**
   - ✅ Integration tests for core workflows
   - ✅ UI component tests (bUnit)
   - ✅ Manual testing on different browsers
   - ✅ Accessibility audit (basic)
   - ✅ Performance testing (query times)

7. **Documentation (Week 3)**
   - ✅ Update user guide
   - ✅ Update release notes
   - ✅ Update CLAUDE.md and DEVELOPMENT_LEDGER.md
   - ✅ Add inline code comments

**Success Criteria:**
- ✅ Users can see all shared ontologies in one place
- ✅ Users can pin/hide ontologies with persistence
- ✅ Users can filter and search shared ontologies
- ✅ Permission levels clearly displayed
- ✅ 90-day window enforced
- ✅ < 1 second page load time
- ✅ Zero P0 bugs

### 14.2 Phase 2: Enhanced Features - Priority P1/P2

**Duration:** 2-3 weeks (after Phase 1 launch)
**Goal:** Add polish, real-time updates, and advanced features

**Deliverables:**
1. **Sharing Method Context**
   - ✅ Show "Shared via link" vs "Shared via group: [name]"
   - ✅ Display sharing date (when first shared)
   - ✅ Multiple source indicator (both link and group)

2. **Undo Functionality**
   - ✅ Undo hide action (5-second toast window)
   - ✅ Undo pin action (optional)

3. **Real-Time Updates (SignalR)**
   - ✅ Real-time update when new ontology is shared
   - ✅ Real-time update when access is revoked
   - ✅ Real-time badge update when permission changes
   - ✅ Sync pin/hide state across multiple browser tabs

4. **Advanced Filtering**
   - ✅ Filter by sharing method (link vs group)
   - ✅ Filter by date range (custom 30/60/90 days)
   - ✅ Multi-select permission filter

5. **List View Support**
   - ✅ List view in addition to grid view
   - ✅ Toggle between grid and list
   - ✅ Persistent view preference

6. **Enhanced Empty States**
   - ✅ Different messages for different scenarios
   - ✅ Actionable CTAs (browse public, join collaboration)
   - ✅ Illustrations/icons for visual appeal

7. **Mobile Enhancements**
   - ✅ Swipe-to-hide gesture
   - ✅ Long-press for menu
   - ✅ Optimized touch targets

8. **Analytics & Insights**
   - ✅ Show access statistics (how many times accessed)
   - ✅ "Most accessed" sort option
   - ✅ "Recently shared" indicator (shared in last 7 days)

**Success Criteria:**
- ✅ Real-time updates working reliably
- ✅ Undo functionality intuitive and fast
- ✅ Mobile gestures feel natural
- ✅ List view performs as well as grid view

### 14.3 Phase 3: Future Enhancements - Priority P3

**Duration:** TBD (future roadmap)
**Goal:** Long-term vision for collaboration features

**Potential Deliverables:**
1. **Notification System**
   - Email notification when ontology is shared
   - In-app notification badge
   - Digest email (weekly summary of new shares)

2. **Request Access Feature**
   - Request elevated permissions (View → Edit)
   - Owner receives request and can approve/deny
   - Notification to requester on decision

3. **Collaboration Analytics**
   - Dashboard showing collaboration stats
   - Most active collaborators
   - Most shared ontologies
   - Engagement metrics

4. **Advanced Organization**
   - Custom folders/categories for shared ontologies
   - Tags on shared ontologies (user-specific)
   - Notes on shared ontologies (private to user)

5. **Bulk Actions**
   - Pin/unpin multiple ontologies at once
   - Hide multiple ontologies at once
   - Export list of shared ontologies (CSV)

6. **Smart Recommendations**
   - "You might be interested in" (based on access patterns)
   - "Similar shared ontologies" (based on tags/domain)

7. **Workspace Integration**
   - Show workspace notes for shared ontologies
   - Collaborative workspace features

**Decision Points:**
- Prioritize based on user feedback after Phase 1/2 launch
- Evaluate usage metrics to determine most valuable features
- Consider resource constraints and technical complexity

### 14.4 Migration & Deployment Considerations

**DEPLOY-001: Database Migration**
- **Pre-Deployment:**
  - Test migration on staging database
  - Verify indexes created successfully
  - Check for migration conflicts with other branches
- **Deployment:**
  - Apply migration during low-traffic window
  - Monitor migration progress
  - Verify no data loss or corruption
- **Rollback Plan:**
  - Have rollback migration ready
  - Backup database before migration
  - Document rollback steps

**DEPLOY-002: Zero-Downtime Deployment**
- **Strategy:** Blue-green or rolling deployment
- **Steps:**
  1. Deploy new code to staging servers
  2. Run smoke tests on staging
  3. Gradually shift traffic to new servers
  4. Monitor for errors/performance issues
  5. Complete cutover or rollback if issues

**DEPLOY-003: Feature Toggle**
- **Requirement:** Add feature toggle for "Shared with Me" section
- **Purpose:**
  - Enable gradual rollout (beta users first)
  - Quick disable if critical bug found
  - A/B testing potential
- **Implementation:**
  ```csharp
  @if (await FeatureToggleService.IsEnabledAsync("SharedWithMe"))
  {
      <SharedWithMeSection />
  }
  ```

**DEPLOY-004: Post-Deployment Monitoring**
- **Metrics to Monitor:**
  - Page load times (dashboard)
  - Query execution times (shared ontologies query)
  - Error rates (pin/hide actions)
  - User adoption rate (% of users viewing section)
  - Feature usage (pin/hide/filter usage)
- **Alerts:**
  - Alert if query time > 500ms (P1)
  - Alert if error rate > 5% (P0)
  - Alert if page load time > 3 seconds (P1)

**DEPLOY-005: Rollback Criteria**
- **Automatic Rollback If:**
  - Error rate > 10% (critical)
  - Page crashes or infinite loops
  - Database corruption detected
- **Manual Rollback If:**
  - Performance degradation > 50%
  - User complaints exceed threshold
  - Critical bug affecting data integrity

### 14.5 Dependencies & Prerequisites

**PREREQ-001: Code Dependencies**
- ✅ No blocking dependencies (feature is self-contained)
- ✅ Ensure `OntologyPermissionService` is in good state (already exists)
- ✅ Ensure `UserShareAccess` and `OntologyGroupPermission` tables exist (already exist)

**PREREQ-002: Infrastructure Dependencies**
- ✅ Database must support migrations (SQLite dev, Azure SQL prod)
- ✅ In-memory cache or Redis available for caching
- ✅ Blazor Server working correctly

**PREREQ-003: Design/UX Dependencies**
- ✅ Bootstrap Icons available (already in use)
- ✅ Bootstrap 5 CSS framework (already in use)
- ✅ No custom design assets needed (use existing patterns)

---

## 15. Appendix

### 15.1 Glossary

- **Shared Ontology:** An ontology that the current user did not create but has access to view/edit
- **Share Link:** A unique URL with token that grants access to an ontology
- **Group Share:** Access granted via membership in a UserGroup with OntologyGroupPermission
- **Pin:** User action to mark a shared ontology as important (appears at top of list)
- **Hide:** User action to remove a shared ontology from their "Shared with Me" list
- **Permission Level:** The level of access a user has to an ontology (View, ViewAndAdd, ViewAddEdit, FullAccess)
- **Last Accessed:** Timestamp of when user last viewed the ontology
- **90-Day Window:** Filter that only shows shared ontologies accessed in the last 90 days
- **Owner:** The user who created the ontology (shown on shared ontology cards)

### 15.2 Related Documents

- `CLAUDE.md` - Project overview and architecture
- `DEVELOPMENT_LEDGER.md` - Development history and decisions
- `DATA_ACCESS_OPTIMIZATION_REPORT.md` - Performance optimization reference
- User Guide - End-user documentation
- Release Notes - Feature announcements

### 15.3 Open Questions & Decisions Needed

**DECISION-001: View Mode Sharing**
- **Question:** Should "Shared with Me" section share the grid/list view toggle with "My Ontologies", or be independent?
- **Options:**
  - A: Shared toggle (one view mode for entire dashboard)
  - B: Independent toggle (users can view own ontologies in grid, shared in list)
- **Recommendation:** Independent (more flexibility, users may have different preferences for own vs shared)
- **Decision:** TBD

**DECISION-002: Expired Share Links**
- **Question:** Should expired share links appear in "Shared with Me" with an "Expired" badge, or be hidden entirely?
- **Options:**
  - A: Show with "Expired" badge (user can see historical access)
  - B: Hide entirely (cleaner list, expired = no longer accessible)
- **Recommendation:** Hide entirely for MVP (cleaner), add "Show Expired" toggle in Phase 2
- **Decision:** TBD

**DECISION-003: Pin Limit**
- **Question:** Should there be a limit on how many ontologies a user can pin?
- **Options:**
  - A: No limit (user can pin all if they want)
  - B: Limit to 10-20 pins (prevents abuse, encourages curation)
- **Recommendation:** No limit for MVP (trust users, monitor usage)
- **Decision:** TBD

**DECISION-004: Collaboration Board Integration**
- **Question:** Should ontologies from accepted collaboration board posts automatically appear in "Shared with Me"?
- **Options:**
  - A: Yes, auto-appear when user accepts collaboration
  - B: No, only appear after first access
- **Recommendation:** Auto-appear (helps users find their new collaborations)
- **Decision:** TBD (depends on collaboration board workflow)

### 15.4 Success Metrics Tracking

**Metrics Dashboard (Phase 2):**
- Total users with shared ontologies
- Average shared ontologies per user
- Pin usage rate (% of users who pin at least one)
- Hide usage rate (% of users who hide at least one)
- Return visit rate (% who access same shared ontology 2+ times)
- Permission distribution (View vs Edit vs Manage)
- Sharing method breakdown (link vs group)

**A/B Testing Opportunities:**
- Pin icon placement (top-left vs top-right)
- Default sort order (last accessed vs name)
- Empty state messaging
- Filter placement (top vs sidebar)

---

## Document Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-18 | Requirements Analysis | Initial comprehensive requirements document |

---

**End of Requirements Document**

This comprehensive requirements document provides a complete specification for the "Shared with Me" ontologies dashboard feature. It covers all aspects from business justification to technical implementation, testing, and deployment considerations. The document is structured to support both high-level decision-making and detailed implementation planning.
