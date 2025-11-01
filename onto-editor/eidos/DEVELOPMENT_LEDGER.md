# Eidos Development Ledger

This document tracks major development milestones, features, and changes to the Eidos Ontology Builder application.

---

## 2025-11-01 - Bulk Creation Feature with Spreadsheet-Like Interface

**Update 2**: Added direct Excel paste support into grid and improved button visibility

### Features Added

#### 📊 Bulk Creation Dialog
A comprehensive bulk creation system that allows users to create multiple concepts or relationships at once using a spreadsheet-like interface.

**Key Capabilities:**
- **Two Creation Modes**:
  - **Concepts Only**: Single-column entry for quickly adding multiple concept names
  - **Relationships**: Create relationship types or full triples (Subject | Relationship | Object)

- **Relationship Formats**:
  - **Simple Mode**: Just relationship type names (e.g., manages, employs, owns)
  - **Full Triple Mode**: Complete relationship definitions with auto-concept creation

- **Spreadsheet-Like Grid**:
  - Editable table interface for relationship triples
  - Tab to move between cells, Enter for new row
  - Add/remove rows dynamically
  - Real-time validation with error highlighting

- **Paste Support**:
  - **Direct Grid Paste**: Copy rows from Excel and paste directly into the grid (NEW in Update 2)
  - Automatic tab-separated parsing (from Excel/Sheets)
  - Parse pipe-delimited format (Subject | Relationship | Object)
  - Bulk import from text area
  - Toast notifications confirm successful paste

- **Auto-Concept Creation**:
  - When creating relationship triples, missing concepts are automatically created
  - Preview shows which new concepts will be created
  - Concepts created first, then relationships

- **Multi-Step Workflow**:
  1. **Mode Selection**: Choose concepts vs relationships
  2. **Data Entry**: Textarea or grid input with paste support
  3. **Preview**: Review items and new concepts before creation
  4. **Processing**: Progress bar with real-time updates
  5. **Results**: Success count, error summary, and option to create more

- **User Experience Enhancements**:
  - Pro tips with keyboard shortcuts
  - Line/row counters
  - Valid item counts
  - Progress tracking (percentage and current/total)
  - Error collection and display
  - Success toast notifications

#### 🎨 UI Components

**BulkCreateDialog.razor** (`Components/Ontology/BulkCreateDialog.razor`):
- Modal dialog with 5-step wizard interface
- Responsive card-based mode selection
- Textarea for simple bulk entry
- Editable grid for relationship triples
- Preview table with validation
- Animated progress indicator
- Success/error summary

**OntologyView.razor** (Modified):
- Added "Bulk Create" button to action panel
- Button positioned after "Add Concept" and "Add Relationship"
- **Warning color styling** (yellow/orange) for high visibility (Update 2)
- Permission-aware (disabled if user can't add)

### Files Modified

#### UI Components
- `Components/Ontology/BulkCreateDialog.razor` (NEW - 850+ lines)
  - Complete wizard-based bulk creation interface
  - Handles both concepts and relationships
  - Multi-format support (text, grid, paste)
  - **Direct grid paste handler** with clipboard API (Update 2 - line 601-681)
  - Auto-parse tab-separated and pipe-delimited data

- `Components/Pages/OntologyView.razor` (Modified)
  - Line 613-618: Added "Bulk Create" button
  - Line 1003: Added `showBulkCreate` flag
  - Line 1287-1300: Added `ShowBulkCreateDialog()` and `OnBulkCreateComplete()` methods
  - Line 187-194: Added BulkCreateDialog component instance

#### Documentation
- `DEVELOPMENT_LEDGER.md` (This entry)

### Technical Details

#### Direct Excel Paste (Update 2)

**Feature**: Users can now copy data from Excel/Google Sheets and paste directly into the grid without using the textarea.

**Implementation**:
```csharp
private async Task HandleGridPaste(ClipboardEventArgs e)
{
    // Get clipboard via JS interop
    var clipboardText = await JSRuntime.InvokeAsync<string>("navigator.clipboard.readText");

    // Parse tab-separated (Excel) or pipe-delimited data
    foreach (var line in lines)
    {
        if (line.Contains('\t'))
            parts = line.Split('\t');  // Excel format
        else if (line.Contains('|'))
            parts = line.Split('|');   // Pipe format

        // Create RelationshipTriple from parsed parts
        relationshipTriples.Add(new RelationshipTriple {
            Subject = parts[0],
            Relationship = parts[1],
            Object = parts[2]
        });
    }
}
```

**User Experience**:
1. Copy rows from Excel (Subject, Relationship, Object columns)
2. Click on the grid area (it has `tabindex="0"` for focus)
3. Press Ctrl+V (or Cmd+V on Mac)
4. Grid instantly populates with parsed data
5. Toast notification shows "Pasted X rows from clipboard"
6. Empty rows added at end for manual additions

**Button Visibility Enhancement**:
- Changed from `btn-outline-primary` (subtle blue outline)
- To `btn-warning text-dark` (yellow/orange with dark text)
- Much more prominent in the action panel
- Stands out between green "Add Concept" and blue "Add Relationship"

#### Bulk Creation Workflow

**Concepts Mode:**
1. User enters concept names (one per line) in textarea
2. Preview shows all concepts to be created
3. Click "Create All" to add concepts via `ConceptService.CreateConceptAsync()`
4. Progress updates in real-time
5. Success summary shows created count

**Relationships - Simple Mode:**
1. User enters relationship type names (one per line)
2. Preview shows all types
3. Types are validated and made available for use
4. Useful for pre-defining common relationship vocabularies

**Relationships - Full Triple Mode:**
1. User enters triples in grid or pastes from spreadsheet
2. System parses format: `Subject | Relationship | Object` or tab-separated
3. Validates all fields are present
4. Identifies concepts that don't exist in ontology
5. Preview shows:
   - All triples to be created
   - New concepts that will be auto-created
6. Creation process:
   - First: Create missing concepts via `ConceptService.CreateConceptAsync()`
   - Reload concepts to get new IDs
   - Second: Create relationships via `RelationshipService.CreateRelationshipAsync()`
   - Lookup concept IDs using case-insensitive dictionary
7. Progress tracking for both concepts and relationships
8. Error collection with detailed messages

#### Paste Parsing Logic
Supports multiple formats:
- **Pipe-delimited**: `Dog | is-a | Mammal`
- **Tab-separated**: `Dog\tis-a\tMammal` (from Excel/Sheets)
- **Single column**: Just relationship types (Simple mode)

Parser splits by delimiter, trims whitespace, validates field count.

#### Grid Interaction
- Each row is a `RelationshipTriple` object with Subject, Relationship, Object properties
- Keyboard shortcuts:
  - **Enter**: Move to next row (auto-add if on last row)
  - **Tab**: Move to next cell
- Validation marks rows as error if any field is missing
- Error messages displayed inline beneath invalid rows

#### Permission Checks
Bulk create respects ontology permissions:
- Button disabled if `!CanAdd()` returns false
- Uses existing permission checking infrastructure
- Same permissions as individual concept/relationship creation

### Design Patterns & Code Style

**Patterns Used:**
- **Wizard/Stepper Pattern**: Multi-step modal with clear progression
- **Repository Pattern**: Uses `IConceptService` and `IRelationshipService` (existing)
- **Error Collection**: Captures errors without stopping batch process
- **Progress Reporting**: Real-time progress updates with percentage calculation
- **Auto-Complete**: Creates dependent entities (concepts) before main entities (relationships)

**Code Style Consistency:**
- Bootstrap 5 classes for styling (consistent with app)
- Icon usage from Bootstrap Icons (`bi-table`, `bi-plus-circle`, etc.)
- Small button sizes (`btn-sm`) for compact UI
- Toast notifications for user feedback
- `@code` block organization following existing Eidos conventions
- Logging pattern would follow existing `Logger.LogInformation()` style (not yet implemented)

**Error Handling:**
- Try-catch around individual item creation to prevent one error from stopping batch
- Error messages collected in list
- Display up to 5 errors in summary, indicate if more exist
- Continue processing remaining items after error

### User Benefits

1. **Efficiency**: Create dozens of concepts/relationships in seconds instead of one-by-one
2. **Spreadsheet Familiarity**: Users can prepare data in Excel/Sheets and paste
3. **Reduced Friction**: Auto-create concepts eliminates need to create them separately first
4. **Visibility**: Preview step prevents accidental bulk creation
5. **Error Recovery**: Partial success still creates valid items, shows errors for manual fix
6. **Flexibility**: Multiple input methods (textarea, grid, paste) suit different workflows

### Known Limitations & Future Enhancements

**Current Limitations:**
- Simple relationship mode doesn't actually store types as reusable templates (would need new table)
- No import/export of bulk data to CSV
- Grid doesn't have advanced Excel features (copy/paste within grid, drag-fill, etc.)
- No undo for bulk operations (would need batch command pattern)

**Potential Future Enhancements:**
- Import from CSV/Excel files directly
- Export validation errors to file
- Template saving for common bulk patterns
- Bulk edit (update existing items)
- Bulk delete with multi-select
- Drag-and-drop file upload
- Relationship type storage and autocomplete
- Individual property bulk setting (category, color for concepts)
- Relationship property bulk setting (custom labels, bidirectionality)

### Testing Recommendations

**Manual Testing Scenarios:**
1. **Concepts - Simple Entry**:
   - Enter 5-10 concept names
   - Verify preview shows all
   - Verify all created successfully

2. **Concepts - Paste from Spreadsheet**:
   - Copy column of names from Excel
   - Paste into textarea
   - Verify parsing works correctly

3. **Relationships - Simple Mode**:
   - Enter relationship types like "manages", "employs"
   - Verify they appear in relationship editor dropdown

4. **Relationships - Full Triples (All Concepts Exist)**:
   - Create concepts: Dog, Cat, Mammal
   - Bulk create: `Dog | is-a | Mammal`, `Cat | is-a | Mammal`
   - Verify both relationships created

5. **Relationships - Full Triples (Auto-Create Concepts)**:
   - Bulk create: `Apple | is-a | Fruit`, `Banana | is-a | Fruit`
   - Verify "Fruit" preview shows as new concept
   - Verify all 3 items created (1 concept, 2 relationships)

6. **Error Handling**:
   - Try creating duplicate concepts
   - Try creating self-referencing relationships
   - Verify errors shown but other items created

7. **Permission Checks**:
   - Open ontology with view-only access
   - Verify Bulk Create button is disabled

8. **Progress Tracking**:
   - Create 20+ items
   - Verify progress bar animates smoothly
   - Verify counts update correctly

### Performance Considerations

- Creates items sequentially (not parallel) to avoid race conditions
- Uses small delay (`await Task.Delay(10)`) to allow UI updates
- For very large batches (100+), consider:
  - Batch chunking
  - Background job processing
  - WebSocket/SignalR progress updates
- Current implementation suitable for typical usage (5-50 items)

---

## 2025-10-31 - Collaboration Board & Automated Group Management

### Features Added

#### 🤝 Collaboration Board System
- **CollaborationBoardService**: Comprehensive service for managing collaboration posts and responses
  - `GetActivePostsAsync()`: Get all active collaboration posts
  - `SearchPostsAsync()`: Search and filter posts by domain, skill level, keywords
  - `GetPostDetailsAsync()`: Get detailed post information with view tracking
  - `GetMyPostsAsync()`: Get user's own collaboration posts
  - `CreatePostAsync()`: Create post with automatic group creation and permission grant
  - `UpdatePostAsync()`: Update post details
  - `DeletePostAsync()`: Delete collaboration post
  - `TogglePostActiveStatusAsync()`: Pause/resume recruiting
  - `AddResponseAsync()`: Apply to collaboration projects
  - `GetPostResponsesAsync()`: Get all responses for a post
  - `UpdateResponseStatusAsync()`: Accept/reject responses with automatic group membership

#### 🔑 Automated Permission Workflow
- **Automatic Group Creation**: When creating a collaboration post:
  - Creates user group named "Collaboration: [Project Title]"
  - Adds post creator as group admin
  - Links collaboration post to the group via `CollaborationProjectGroupId`
  - Sets ontology visibility to "Group" if ontology is attached
  - Grants group "Edit" permission on the ontology

- **Seamless Collaborator Onboarding**: When accepting a collaboration response:
  - Automatically adds user to the collaboration project group
  - User immediately gains edit access to the ontology
  - No manual permission configuration required
  - When declining/removing, automatically removes user from group

#### 🎨 UI Components
- **CollaborationBoard.razor** (`/collaboration`): Browse and search active projects
  - Filter by domain and skill level
  - Search functionality
  - View detailed project cards
  - Apply to projects

- **MyCollaborationPosts.razor** (`/collaboration/my-posts`): Manage your posts
  - View all your collaboration posts
  - Toggle active status
  - Manage responses (accept/reject)
  - View response details

- **CollaborationPostDetail.razor**: Detailed post view with response management
  - Full post information
  - Applicant list with experience and motivation
  - Accept/decline buttons for post owners

#### 🔧 Permission System Enhancements
- **PermissionsSettingsTab.razor**: Implemented `LoadGroupAccess()` method
  - Displays collaboration groups that have access to ontology
  - Shows group names, member counts, and permission levels
  - Real-time group permission visibility
  - Fixed permission level enum mapping (view, edit, admin → View, ViewAddEdit, FullAccess)

- **OntologyHub.cs**: Updated SignalR permission checks
  - Changed from old `UserShareAccesses` check to `OntologyPermissionService.CanViewAsync()`
  - Now properly recognizes group-based permissions
  - Enables real-time collaboration for group members

#### 🧪 Development Tools
- **DevSwitchUser.razor** (`/dev/switch-user`): Multi-user testing page (development only)
  - Quick links to switch between test users
  - Interactive server mode for seamless user switching

- **DevSwitchUserEndpoint.cs**: API endpoint for user switching
  - Handles authentication outside Blazor response pipeline
  - Signs out current user and signs in as selected user
  - Sets cookie to prevent auto-login middleware from overriding
  - Development-only with environment check

- **DevelopmentAuthMiddleware.cs**: Enhanced auto-login middleware
  - Checks for "manual-user-switch" cookie
  - Skips auto-login when user has manually switched accounts
  - Preserves manual user switches across page refreshes

### Database Changes
- Uses existing `CollaborationPosts` table with `CollaborationProjectGroupId` column
- Uses existing `UserGroups`, `UserGroupMembers`, `OntologyGroupPermissions` tables
- No new migrations required

### Files Modified

#### Services
- `/Services/CollaborationBoardService.cs` - Added automatic group creation and permission grant logic
- `/Services/OntologyPermissionService.cs` - Used for permission checks in hub

#### UI Components
- `/Components/Settings/PermissionsSettingsTab.razor` - Implemented group access display
- `/Components/Pages/CollaborationBoard.razor` - Main collaboration discovery page
- `/Components/Pages/MyCollaborationPosts.razor` - User's posts management page
- `/Components/Pages/DevSwitchUser.razor` - Development user switcher (NEW)

#### SignalR
- `/Hubs/OntologyHub.cs` - Updated permission checks to use OntologyPermissionService

#### Endpoints
- `/Endpoints/DevSwitchUserEndpoint.cs` - User switching API (NEW)

#### Middleware
- `/Middleware/DevelopmentAuthMiddleware.cs` - Added manual switch cookie check

#### Documentation
- `/Components/Shared/ReleaseNotes.razor` - Documented collaboration features
- `/DEVELOPMENT_LEDGER.md` - This entry

### Technical Details

#### Collaboration Lifecycle
1. **Post Creation**:
   - User creates collaboration post for their ontology
   - System creates `UserGroup` named "Collaboration: [Title]"
   - Creator added as group admin
   - Group granted "Edit" permission to ontology
   - Ontology visibility changed to "Group"

2. **Response Submission**:
   - Other users apply to join the project
   - Response created with "Pending" status

3. **Response Acceptance**:
   - Post owner accepts response
   - Status changed to "Accepted"
   - User automatically added to collaboration group
   - User immediately gains edit access to ontology

4. **Collaboration**:
   - Collaborators can view and edit ontology
   - Real-time presence tracking via SignalR
   - Permission checks validate group membership
   - Visible in Permissions tab

#### Permission Level Mapping
Database stores permission levels as strings, but UI uses enum:
- `"view"` → `PermissionLevel.View`
- `"edit"` → `PermissionLevel.ViewAddEdit`
- `"admin"` → `PermissionLevel.FullAccess`

Fixed in `PermissionsSettingsTab.razor` line 178-183

#### Development Testing Workflow
1. Navigate to `/dev/switch-user`
2. Click user to switch to (e.g., test@test.com, collab@test.com)
3. Cookie prevents auto-login from overriding
4. Test collaboration workflow from different user perspectives

### Bug Fixes
- Fixed permission denied errors when group members tried to access ontologies
- Fixed "No groups have been granted access yet" message when groups existed
- Fixed enum mapping errors in PermissionsSettingsTab
- Fixed `OntologyId.HasValue` error (changed to `OntologyId > 0`)

### Performance Considerations
- Group creation and permission grant happen in single transaction
- Efficient permission checks via `OntologyPermissionService`
- Real-time updates via SignalR for collaborators
- Cookie-based state for user switching (minimal overhead)

### Security Enhancements
- Development user switcher only works in Development environment
- Permission checks enforce at multiple layers (Hub, Service, UI)
- Group membership validated before granting ontology access
- Automatic permission management reduces manual configuration errors

### Future Improvements
- [ ] Email notifications when responses are accepted/declined
- [ ] In-app notifications for collaboration updates
- [ ] Collaboration analytics (response rates, active projects)
- [ ] Project completion/archival workflow
- [ ] Collaboration activity feed
- [ ] Group chat/messaging for collaborators
- [ ] Permission level adjustments after acceptance

---

## 2025-10-26 - Group Management & Permission System

### Features Added

#### 🔐 Ontology Permission System
- **OntologyPermissionService**: Comprehensive service for managing ontology access control
  - `CanViewAsync()`: Check if user can view an ontology
  - `CanEditAsync()`: Check if user can edit an ontology
  - `CanManageAsync()`: Check if user can manage (admin) an ontology
  - `GetAccessibleOntologiesAsync()`: Get all ontologies user has access to
  - `GrantGroupAccessAsync()`: Grant group access with specific permission level
  - `RevokeGroupAccessAsync()`: Revoke group access
  - `GetGroupPermissionsAsync()`: Get all group permissions for an ontology
  - `UpdateVisibilityAsync()`: Update ontology visibility settings

#### 👥 Group Management UI
- **Permissions Tab in OntologySettingsDialog**: New tabbed interface for managing ontology settings
  - **General Tab**: Existing ontology metadata (name, description, author, version, etc.)
  - **Permissions Tab**: New permission management interface
    - Visibility controls (Private, Group, Public)
    - Allow public edit toggle for public ontologies
    - Group permission management:
      - Add groups with permission levels (View, Edit, Admin)
      - Change permission levels for existing groups
      - Remove group access
      - View member counts and grant history
      - Real-time updates

#### 🛡️ Role-Based Access Control Integration
- Integrated `OntologyPermissionService` into `Home.razor`:
  - Filters ontology list to show only accessible ontologies
- Integrated permission checks into `OntologyView.razor`:
  - Checks view permission before loading ontology
  - Checks edit permission and combines with share permissions
  - Prioritizes most restrictive permission level
  - Shows appropriate error messages for unauthorized access

#### 🔧 OAuth Login Improvements
- **Conditional OAuth Buttons**: Login page now only shows OAuth buttons for configured providers
  - `LoginModel.IsGoogleConfigured`: Detects if Google OAuth is configured
  - `LoginModel.IsMicrosoftConfigured`: Detects if Microsoft OAuth is configured
  - `LoginModel.IsGitHubConfigured`: Detects if GitHub OAuth is configured
  - Prevents users from clicking unconfigured OAuth providers
- **OAuth Error Handling**: Added comprehensive try-catch blocks to all OAuth event handlers
  - GitHub OAuth `OnCreatingTicket` event
  - Google OAuth `OnCreatingTicket` event
  - Microsoft OAuth `OnCreatingTicket` event
  - Prevents login failures from breaking the authentication flow
  - Logs errors for debugging without disrupting user experience

### Database Changes
- No new migrations required (uses existing `OntologyGroupPermissions`, `UserGroups`, `UserGroupMembers` tables)
- Leverages existing `Visibility` and `AllowPublicEdit` columns on `Ontologies` table

### Files Modified

#### Services
- `/Services/OntologyPermissionService.cs` - Core permission management logic

#### UI Components
- `/Components/Ontology/OntologySettingsDialog.razor` - Added Permissions tab with group management
- `/Components/Pages/OntologyView.razor` - Integrated permission checks
- `/Components/Pages/Home.razor` - Filtered ontology list by permissions

#### Authentication
- `/Pages/Account/Login.cshtml` - Conditional OAuth button rendering
- `/Pages/Account/Login.cshtml.cs` - OAuth provider configuration detection
- `/Program.cs` - OAuth event handler error handling

### Tests Added
- `/Eidos.Tests/Integration/Services/OntologyPermissionServiceTests.cs` - 20+ comprehensive tests covering:
  - View permission checks (owner, public, private, group)
  - Edit permission checks (owner, public edit, group edit/view)
  - Manage permission checks (owner, group admin)
  - Group permission management (grant, update, revoke)
  - Visibility updates
  - Accessible ontologies query

- `/Eidos.Tests/Unit/Pages/LoginModelTests.cs` - 12 tests covering:
  - OAuth provider configuration detection (Google, Microsoft, GitHub)
  - Multiple provider scenarios
  - Register mode detection
  - URL toggling

### Technical Details

#### Permission Hierarchy
1. **Owner**: Full access (view, edit, manage)
2. **Group Admin**: Full access except changing ownership
3. **Group Edit**: Can view and edit
4. **Group View**: Can only view
5. **Public Edit**: Anyone can edit public ontologies with flag enabled
6. **Public View**: Anyone can view public ontologies

#### Visibility Levels
- **Private**: Only owner can access
- **Group**: Only owner and specified groups can access
- **Public**: Anyone can view, optionally edit

### Performance Considerations
- Permission checks use EF Core's `Include()` for efficient loading
- Group permissions cached in memory during component lifetime
- Real-time updates use `InvokeAsync()` for reactive UI

### Security Enhancements
- OAuth login errors no longer expose sensitive error details to users
- Permission checks enforce at both UI and service layers
- Group membership validated before granting access

---

## Previous Entries

### 2025-10-25 - Admin Dashboard & User Management
- Added role-based access control (RBAC)
- Created Admin dashboard at `/admin`
- Implemented UserManagementService
- Added user roles: Admin, PowerUser, User, Guest
- Created user groups and group membership management

### 2025-10-24 - Database Optimization
- Downgraded Azure SQL to GP_S_Gen5_1 (80% cost reduction)
- Fixed N+1 queries in ConceptService
- Optimized batch operations in OntologyService
- Added UserPreferences caching

### 2025-10-23 - Activity Tracking
- Added OntologyActivity tracking system
- Implemented version control foundation
- Added collaborator visibility
- Created activity timeline

### 2025-10-22 - Initial Deployment
- Deployed to Azure App Service
- Configured Application Insights
- Set up CI/CD with GitHub Actions
- Implemented health checks

---

## Testing

To run the new tests:

```bash
# Run all tests
dotnet test

# Run only permission tests
dotnet test --filter "FullyQualifiedName~OntologyPermissionServiceTests"

# Run only login tests
dotnet test --filter "FullyQualifiedName~LoginModelTests"
```

---

## Next Steps / Roadmap

### Immediate
- [ ] Deploy group management features to production
- [ ] Update user documentation with group management workflow
- [ ] Monitor OAuth error logs

### Short Term
- [ ] Add group creation UI for regular users
- [ ] Implement group invitation system
- [ ] Add email notifications for group access grants

### Long Term
- [ ] Implement fine-grained permissions (per-concept, per-relationship)
- [ ] Add permission inheritance for ontology hierarchies
- [ ] Create permission audit log
