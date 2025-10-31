# Eidos Ontology Builder - Claude Code Context

## Project Overview

**Eidos** is a modern web-based ontology builder that enables users to create, manage, and visualize knowledge ontologies collaboratively. Built with Blazor Server and .NET 9, it provides an intuitive interface for semantic modeling and knowledge organization.

**Live Site**: https://eidosonto.com

## Technology Stack

### Backend
- **.NET 9.0** - Modern C# web framework
- **Blazor Server** - Interactive web UI framework with SignalR
- **Entity Framework Core 9.0** - ORM for database operations
- **SQLite** (Development) / **Azure SQL** (Production) - Data persistence
- **ASP.NET Core Identity** - Authentication and user management
- **SignalR** - Real-time collaboration and presence tracking

### Frontend
- **Bootstrap 5** - Responsive UI components
- **Cytoscape.js** - Interactive graph visualization
- **JavaScript Interop** - Blazor/JS integration
- **SCSS** - Styling

### Infrastructure
- **Azure App Service** - Hosting
- **Application Insights** - Monitoring and telemetry
- **GitHub Actions** - CI/CD
- **Redis** (Optional) - Distributed caching and SignalR backplane

## Project Structure

```
eidos/
├── Components/              # Blazor components
│   ├── Layout/             # Layout components (NavMenu, MainLayout)
│   ├── Ontology/           # Ontology-specific components
│   ├── Pages/              # Page components (routable)
│   ├── Settings/           # Settings dialog components
│   └── Shared/             # Shared UI components
├── Data/                   # Database context and repositories
│   └── Repositories/       # Repository pattern implementations
├── Endpoints/              # Minimal API endpoints
├── Hubs/                   # SignalR hubs for real-time features
├── Migrations/             # EF Core migrations
├── Middleware/             # Custom middleware
├── Models/                 # Domain models and DTOs
│   └── Enums/             # Enum definitions
├── Pages/                  # Razor Pages (Account management)
│   └── Account/           # Login/Register/Logout
├── Services/              # Business logic services
│   └── Interfaces/        # Service interfaces
├── wwwroot/               # Static assets
│   ├── css/              # Stylesheets
│   └── js/               # JavaScript files
├── Eidos.Tests/           # Test project (157+ tests, 100% passing)
├── DEVELOPMENT_LEDGER.md  # Development history and changes
└── ontology.db            # SQLite database (development)
```

## Core Features

### 1. Ontology Management
- Create, edit, delete ontologies
- Fork/clone existing ontologies
- Import from TTL/RDF/OWL formats
- Export to TTL format
- Template system for quick starts
- Tagging and categorization

### 2. Concept & Relationship Management
- Add concepts with properties (name, description, URI, category)
- Define relationships between concepts (is-a, part-of, related-to, custom)
- Individual instances of concepts
- Hierarchical concept organization
- Bulk concept entry with "Save & Add Another"

### 3. Multiple View Modes
- **Graph View**: Interactive network visualization with Cytoscape.js
  - Drag nodes to rearrange
  - Zoom and pan
  - Click to view details
  - Show/hide individuals
  - Color-coded concepts
- **List View**: Tabular view with search and filtering
- **Hierarchy View**: Tree structure of parent-child relationships
- **TTL View**: Raw Turtle format for semantic web compliance

### 4. Real-Time Collaboration
- **SignalR-based presence tracking**
  - See who's viewing the ontology
  - User avatars with initials and colors
  - View tracking (which tab each user is on)
  - 30-second heartbeat for active status
- **Concurrent editing**
  - Multiple users can edit simultaneously
  - Real-time updates broadcast to all clients

### 5. Collaboration Board (Latest Feature)
- **Find Collaborators**
  - Browse active collaboration opportunities
  - Filter by domain and skill level
  - Search by keywords
  - Apply to projects
- **Post Projects**
  - Create collaboration posts for ontologies
  - Specify requirements and commitment level
  - Manage responses (accept/decline)
- **Automatic Permission Management**
  - Auto-creates collaboration groups when posting
  - Grants edit permissions to accepted collaborators
  - Sets ontology visibility to "Group" mode
  - Seamless onboarding workflow

### 6. Permission System
- **Three Visibility Levels**:
  - **Private**: Only owner and invited users
  - **Group**: Shared with specific user groups
  - **Public**: Anyone can view (optional edit)
- **Permission Levels**:
  - View: Read-only access
  - ViewAndAdd: Can add concepts
  - ViewAddEdit: Can add and edit concepts
  - FullAccess: Admin permissions
- **Group-Based Access Control**
  - Create and manage user groups
  - Grant group permissions to ontologies
  - Automatic group management via Collaboration Board

### 7. Version Control & Activity Tracking
- Track all changes (create, update, delete)
- Version history with before/after snapshots
- User attribution for all changes
- Activity timeline and statistics
- Contributor breakdown

### 8. User Management
- **Authentication**: OAuth (Google, Microsoft, GitHub, Entra ID)
- **Role-Based Access Control**: Admin, PowerUser, User, Guest
- **User Preferences**: Theme, default view, notifications
- **Profile Management**: Display name, photo (from OAuth)

### 9. Dark Mode & Themes
- Light and dark mode support
- System theme detection
- Persisted user preference
- Color-blind friendly palette
- Smooth transitions

### 10. Mobile Responsive
- Touch-friendly graph editor
- Responsive layouts for all views
- Collapsible sidebars
- Mobile-optimized controls
- Gesture support (pinch-zoom, pan)

## Key Services

### Business Logic Services
- **OntologyService**: CRUD operations for ontologies
- **ConceptService**: Concept management with command pattern
- **RelationshipService**: Relationship operations
- **CollaborationBoardService**: Collaboration post and response management
- **OntologyPermissionService**: Comprehensive permission checking
- **UserGroupService**: Group and membership management
- **UserPreferenceService**: User settings with caching
- **ActivityTrackingService**: Version control and history

### Infrastructure Services
- **IPresenceService**: Real-time user presence tracking
  - InMemoryPresenceService (default, single-server)
  - RedisPresenceService (optional, distributed)
- **ToastService**: User notifications
- **DatabaseSeeder**: Initial data population
- **UserManagementService**: Admin user operations

## Database Schema

### Core Tables
- **Ontologies**: Ontology metadata (Name, Description, Visibility, ConceptCount, RelationshipCount)
- **Concepts**: Concepts with properties (Name, Description, URI, Category)
- **Relationships**: Links between concepts (FromConceptId, ToConceptId, RelationshipType)
- **Individuals**: Instances of concepts
- **IndividualRelationships**: Relationships between individuals

### Collaboration Tables
- **CollaborationPosts**: Project collaboration posts
- **CollaborationResponses**: Applicant responses to posts
- **UserGroups**: User groups for permission management
- **UserGroupMembers**: Group membership (with admin flag)
- **OntologyGroupPermissions**: Group access to ontologies

### User & Activity Tables
- **AspNetUsers**: User accounts (via Identity)
- **AspNetRoles**: User roles
- **OntologyActivity**: Change tracking and version history
- **UserShareAccesses**: Direct user sharing (deprecated, use groups)
- **UserPreferences**: User settings
- **OntologyTags**: Ontology categorization

## Development Workflow

### Getting Started
```bash
# Restore dependencies
dotnet restore

# Run migrations
dotnet ef database update

# Run the application
dotnet run

# Run tests
dotnet test
```

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName

# Remove last migration
dotnet ef migrations remove --force

# Update database
dotnet ef database update

# View migration history
sqlite3 ontology.db "SELECT * FROM __EFMigrationsHistory;"
```

### Development Tools

#### User Switching (Development Only)
- **DevSwitchUser.razor** (`/dev/switch-user`): Switch between test users
- **DevSwitchUserEndpoint**: API for user switching
- **DevelopmentAuthMiddleware**: Auto-login in development

Test accounts:
- `dev@localhost.local` - Primary dev account
- `test@test.com` - Test user
- `collab@test.com` - Collaborator user

#### Configuration
Development settings in `appsettings.Development.json`:
```json
{
  "Development": {
    "EnableAutoLogin": true,
    "AutoLoginEmail": "dev@localhost.local",
    "AutoLoginName": "Dev User"
  }
}
```

## Important Patterns & Conventions

### Command Pattern for Undo/Redo
- All concept/relationship operations use command pattern
- Enables undo/redo functionality
- Maintains concept/relationship counts accurately

### Repository Pattern
- All data access through repositories
- Consistent error handling and logging
- Use `AsNoTracking()` for read operations
- Use `Include()` for eager loading to avoid N+1 queries

### Permission Checking
Always use `OntologyPermissionService` for access control:
```csharp
// Check if user can view
bool canView = await _permissionService.CanViewAsync(ontologyId, userId);

// Check if user can edit
bool canEdit = await _permissionService.CanEditAsync(ontologyId, userId);

// Check if user can manage
bool canManage = await _permissionService.CanManageAsync(ontologyId, userId);
```

### Logging
Use structured logging with placeholders:
```csharp
_logger.LogInformation("User {UserId} created ontology {OntologyId}", userId, ontologyId);
_logger.LogError(ex, "Failed to load ontology {OntologyId} for user {UserId}", ontologyId, userId);
```

### Error Handling
- Always wrap critical operations in try-catch
- Log errors with context
- Provide user-friendly error messages
- Use ToastService for user notifications

## Recent Major Features

### October 31, 2025 - Collaboration Board & Automated Group Management
- Full collaboration discovery system
- Automatic group creation for projects
- Seamless permission grant workflow
- Enhanced permissions tab to show groups
- Development user switching tools

### October 29, 2025 - Individual Visualization
- Show individuals in graph view
- Diamond-shaped nodes for instances
- Toggle to show/hide individuals

### October 28, 2025 - Real-Time Presence
- Google Docs-style user presence
- Color-coded avatars
- View tracking
- Multi-provider auth support

### October 26, 2025 - Group Management & Permissions
- Group-based permission system
- Permissions tab in settings
- OAuth improvements
- 32 new comprehensive tests

## Testing

### Test Coverage
- **157+ tests** (100% passing)
- Component tests with bUnit
- Service integration tests
- Repository tests with in-memory database
- End-to-end workflow tests

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~OntologyPermissionServiceTests"

# Run with verbose output
dotnet test --verbosity normal
```

## Security Considerations

### Authentication
- OAuth only (no password auth to reduce attack surface)
- Multiple providers supported
- Secure token handling via ASP.NET Core Identity

### Authorization
- Permission checks at multiple layers (Hub, Service, UI)
- Owner, group, and share-based access control
- Development endpoints restricted to Development environment

### Data Protection
- HTTPS only (TLS 1.2+)
- HttpOnly cookies
- CSRF protection
- SQL injection prevention via parameterized queries

## Performance Optimizations

### Query Optimization
- Fixed N+1 queries in multiple services
- Use `Include()` for related entities
- Use `AsNoTracking()` for read-only operations
- Projections to reduce data transfer

### Caching
- User preferences cached (5-minute sliding expiration)
- In-memory presence tracking (single-server)
- Optional Redis for distributed scenarios

### Database
- 7 performance indexes on foreign keys
- Denormalized counts (ConceptCount, RelationshipCount)
- Efficient recursive CTEs for hierarchies

## Known Issues & Limitations

### Current Limitations
1. Single-server SignalR (no Redis backplane in development)
2. In-memory presence tracking (not distributed)
3. TTL import limited to basic RDF/OWL constructs
4. Undo/redo limited to concept and relationship operations

### Future Improvements
- Email notifications for collaboration
- In-app notifications
- Collaboration analytics
- Project completion workflow
- Group chat/messaging
- Permission level adjustments post-acceptance
- Fine-grained permissions (per-concept, per-relationship)

## Deployment

### Production Configuration
- Azure App Service (Windows or Linux)
- Azure SQL Database (or managed SQL Server)
- Application Insights for monitoring
- GitHub Actions for CI/CD
- Environment variables for secrets

### Environment Variables
- `ConnectionStrings__DefaultConnection`
- `Authentication__Google__ClientId/ClientSecret`
- `Authentication__Microsoft__ClientId/ClientSecret`
- `Authentication__GitHub__ClientId/ClientSecret`
- `ApplicationInsights__InstrumentationKey`

## Documentation

- **DEVELOPMENT_LEDGER.md**: Development history and technical details
- **User Guide** (`/user-guide`): End-user documentation
- **Release Notes** (`/release-notes`): Feature announcements
- **API Documentation**: XML comments throughout codebase

## Contact & Support

- **GitHub Issues**: Report bugs and feature requests
- **Live Site**: https://eidosonto.com
- **Admin Email**: hoffchops@outlook.com

## License

[Specify license if applicable]

---

**Last Updated**: October 31, 2025
**Current Version**: See Release Notes for latest version
**Build Status**: ✅ Passing (0 errors, 20 warnings)
**Test Status**: ✅ 157+ tests passing
