# Eidos Development Ledger

This document tracks major development milestones, features, and changes to the Eidos Ontology Builder application.

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
