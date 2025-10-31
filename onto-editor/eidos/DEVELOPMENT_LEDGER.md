# Eidos Development Ledger

This document tracks major development milestones, features, and changes to the Eidos Ontology Builder application.

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
