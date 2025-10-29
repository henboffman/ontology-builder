# Changelog

All notable changes to the Eidos Ontology Builder project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Real-Time Presence Tracking (2025-10-28)

#### Added
- **Presence Indicators**: Google Docs / Figma-style real-time collaboration awareness
  - Avatar indicators showing who's currently viewing the ontology
  - Color-coded user avatars with initials
  - User count display ("X online")
  - Hover tooltips with display names
  - "Just you" message when alone
- **View Tracking**: See which tab other users are viewing
  - Shows current view for each user (Graph, List, Hierarchy, etc.)
  - View icons on user avatars
  - View labels under avatars
  - Real-time view change notifications
- **Colored View Indicators**: User-colored dots on tab buttons
  - Shows up to 3 colored dots per tab matching user colors
  - "+N" badge for additional viewers
  - Enhanced tooltips listing viewer names
  - Visual consistency between avatars and tab indicators
- **Multi-Provider Display Names**: Compatible with multiple auth providers
  - Supports Entra ID (Azure AD) display names
  - Supports Google, GitHub, Microsoft OAuth
  - Priority fallback chain for name resolution:
    1. "name" claim (OAuth providers, Entra ID)
    2. ClaimTypes.Name (standard .NET)
    3. "preferred_username" (Entra ID/Azure AD)
    4. GivenName + Surname (constructed)
    5. Email username (fallback)
- **Heartbeat Mechanism**: Keep presence active
  - 30-second timer for continuous presence updates
  - LastSeenAt timestamp tracking
  - Automatic cleanup on disconnect
- **Permission-Based Access**: Security-first presence
  - Permission check before joining ontology
  - Unauthorized attempts logged for audit
  - Only authorized users see presence data

#### Technical Details
- Created `PresenceInfo` model with user metadata
- Extended `OntologyHub` SignalR hub with presence tracking:
  - `JoinOntology()` with permission validation
  - `UpdateCurrentView()` with input validation (max 50 chars)
  - `Heartbeat()` for presence maintenance
  - `OnDisconnectedAsync()` for cleanup
- Created `PresenceIndicator.razor` component with animations
- Enhanced `ViewModeSelector.razor` with colored viewer indicators
- Updated `OntologyView.razor` with presence integration
- Extended `wwwroot/js/ontologyHub.js` with presence events
- Static `ConcurrentDictionary` for thread-safe presence storage
- Hash-based color assignment for consistent user colors
- CSS animations for smooth presence updates
- Dark mode support for all presence UI

#### Security
- Input validation on view names (prevent arbitrary strings)
- Authorization checks on hub join
- Audit logging for unauthorized access attempts
- No PII leakage (email only to authorized users)
- XSS protection via Blazor auto-escaping
- CSRF protection via SignalR built-in mechanisms

#### Testing
- Comprehensive unit tests: `OntologyHubPresenceTests.cs` (14 tests)
  - Permission validation tests
  - Presence info creation tests
  - View update tests
  - Input validation tests
  - Display name resolution tests (multi-provider)
  - Color consistency tests
  - Guest user tests
  - Disconnect cleanup tests

#### Performance
- Memory: ~200 bytes per user, ~200 KB for 1000 concurrent users
- Network: ~100 bytes per view change, ~50 bytes per heartbeat
- Scalability: Supports ~10,000 concurrent users per server
- For larger scale: Migrate to Redis backplane

---

### Version Control & Activity Tracking (2025-10-26)

#### Added
- **Version History Panel**: Complete version control interface
  - Timeline view showing all changes chronologically
  - Activity filtering by entity type (concepts, relationships, properties)
  - Activity filtering by activity type (create, update, delete, etc.)
  - Statistics panel showing version count, contributors, and activity breakdown
  - Expandable activity details with before/after snapshots
  - Load more pagination for large histories
  - Mobile-responsive design with dark mode support
- **Activity Recording Service**: Automatic tracking of all changes
  - Records create, update, and delete operations for concepts and relationships
  - Captures before/after snapshots in JSON format
  - Auto-incrementing version numbers
  - User attribution (email or guest identifier)
  - Timestamp tracking for all activities
- **Version Comparison**: Compare any two versions
  - Side-by-side diff view
  - Highlights added, modified, and removed fields
  - Summary of differences
- **Revert Capability**: Foundation for reverting to previous versions
  - UI confirmation dialogs
  - Service layer support (implementation pending)
- **History View Mode**: New "History" button in view mode selector
  - Integrated into ontology editor
  - Real-time activity updates

#### Technical Details
- Created `IOntologyActivityService` interface and implementation
- Created `OntologyActivity` model with snapshot storage
- Created DTOs: `OntologyActivityDto`, `VersionComparisonDto`, `VersionHistoryStatsDto`
- Integrated activity tracking into `ConceptService` and `RelationshipService`
- Added `VersionHistoryPanel.razor` component with responsive CSS
- Database migration: `AddOntologyActivityTracking`

---

### Collaborator Tracking System (2025-10-26)

#### Added
- **Collaborator Panel**: View all users and guests with access to ontologies
  - Lists authenticated users and guest sessions
  - Shows permission levels (View, View & Add, Can Edit, Full Access)
  - Displays access statistics (first accessed, last accessed, access count)
  - Guest user identification with badges
  - Expandable details for each collaborator
- **Edit Statistics**: Per-user breakdown of contributions
  - Total edits count
  - Concepts: created, updated, deleted
  - Relationships: created, updated, deleted
  - Properties: created, updated, deleted
- **Activity Timeline**: Recent activity for each collaborator
  - Chronological list of recent changes
  - Activity descriptions with timestamps
  - Entity names and types
  - Configurable activity limit
- **Collaborator Service**: New service for managing collaboration data
  - `GetCollaboratorsAsync()`: Retrieve all collaborators with stats
  - `GetUserActivityAsync()`: Get detailed user activity history
  - Combines user access data with activity tracking
  - Supports both authenticated users and guest sessions
- **Collaborators View Mode**: New "Collaborators" button in view mode selector

#### Technical Details
- Created `IOntologyShareService` methods for collaborator tracking
- Created DTOs: `CollaboratorInfo`, `CollaboratorActivity`, `CollaboratorEditStats`
- Created `CollaboratorPanel.razor` component with responsive CSS
- Database tables: `OntologyShares`, `GuestSessions`, `UserShareAccesses`
- Integration with existing share and activity tracking systems

---

### Testing Infrastructure (2025-10-25)

#### Added
- **Component Tests**: Comprehensive bUnit tests
  - `CollaboratorPanelTests.cs`: 9 tests for collaborator UI
  - Tests for loading states, empty states, data display, permissions, etc.
  - Mock service integration
- **Service Integration Tests**: Database-backed tests
  - `OntologyShareServiceTests.cs`: 8 tests for collaborator service
  - Tests for authenticated users, guests, activity history, permissions
  - In-memory database for isolation
- **Test Documentation**: Updated test README
  - Total: 162 tests (100% passing)
  - Breakdown by category (repository, service, workflow, component)
  - Test coverage statistics

---

### Dark Mode & Theme Support (2025-10-25)

#### Added
- **User Theme Preferences**: Light/dark mode toggle
  - Persisted to database per user
  - Defaults to light mode for new users
  - Theme selector in user interface
- **Dark Mode Styling**: Complete dark mode support
  - All components styled for both themes
  - Automatic detection of system preference
  - Smooth theme transitions
  - Color-blind friendly palette

#### Changed
- Updated `UserPreferences` model to include theme setting
- Migration: `AddThemeToUserPreferences`
- CSS updates across all components for theme support

---

### Mobile Responsive Design (2025-10-24)

#### Added
- **Responsive Graph View**: Touch-friendly ontology editor
  - Mobile-optimized controls
  - Touch gestures for zoom and pan
  - Responsive button sizing
  - Collapsible sidebars on small screens
- **Responsive Components**: All views work on mobile
  - List view with stacked cards
  - Hierarchy view with collapsible trees
  - Forms with mobile-friendly inputs
  - Navigation adapted for small screens
- **Viewport Meta Tags**: Proper mobile viewport configuration
- **Media Queries**: Breakpoints for tablet and mobile
  - Small: < 768px
  - Medium: 768px - 1024px
  - Large: > 1024px

---

### Azure Deployment & Infrastructure (2025-10-24)

#### Added
- **Azure App Service**: Production deployment
  - App Name: `eidos`
  - Region: Canada Central
  - SKU: Premium V2 (P1v2)
  - HTTPS-only with TLS 1.2 minimum
  - Custom domain: https://eidosonto.com
- **Azure SQL Database**: Production database
  - Server: `eidos-canada-central`
  - Database: `eidos-p1`
  - Tier: GeneralPurpose
  - Managed Identity authentication
  - Firewall configured for secure access
- **Azure Key Vault**: Secrets management
  - Vault name: `eidos`
  - Stores connection strings
  - OAuth client secrets (GitHub, Google, Microsoft)
  - Managed Identity access
- **Application Insights**: Monitoring and telemetry
  - Resource: `eidos-insights`
  - Daily cap: 300 MB/day (~9 GB/month)
  - Cost control: ~$10/month estimated
  - Performance tracking
  - Exception logging
  - User analytics
- **GitHub Actions CI/CD**: Automated deployment
  - Workflow: `.github/workflows/azure-deploy.yml`
  - Triggers on push to `main` branch
  - Build, test, and deploy pipeline
  - Automatic rollback on failure
- **OAuth Configuration**: External authentication
  - GitHub OAuth (configured)
  - Google OAuth (configured)
  - Microsoft OAuth (configured)
  - Secrets stored in Key Vault

#### Technical Details
- Docker container deployment
- Environment variables via App Settings
- Managed Identity for database access
- Publish profile for deployments
- Health check endpoints

---

### Core Features (Initial Release)

#### Added
- **Ontology Management**
  - Create, read, update, delete ontologies
  - Personal ontologies per user
  - Shared ontologies with permission levels
  - Ontology metadata (name, description, tags)
- **Concept Management**
  - Create concepts with names and definitions
  - Simple explanations for accessibility
  - Examples to illustrate concepts
  - Custom colors for visual organization
  - Categories for grouping
  - Position tracking for graph layout
- **Relationship Management**
  - Create relationships between concepts
  - Common relationship types (is-a, part-of, has-a, etc.)
  - Custom relationship types
  - Bidirectional relationship support
  - Relationship descriptions
- **Properties System**
  - Add properties to concepts
  - Property types (string, number, boolean, date)
  - Property constraints and validation
- **Individuals/Instances**
  - Create instances of concepts
  - Property values for individuals
  - Instance management
- **Restrictions**
  - Concept-level restrictions
  - Property cardinality constraints
  - Value restrictions
- **Graph Visualization**
  - Interactive node-based graph (Cytoscape.js)
  - Drag and drop positioning
  - Zoom and pan
  - Auto-layout algorithms
  - Color-coded nodes
  - Relationship arrows with labels
- **List View**
  - Tabular display of concepts
  - Sortable columns
  - Search and filter
  - Quick edit capabilities
- **Hierarchy View**
  - Tree structure visualization
  - Expandable/collapsible nodes
  - Parent-child relationships
  - Depth indicators
- **Instances View**
  - Manage individuals/instances
  - Property value editing
  - Instance-to-concept linking
- **TTL Export/Import**
  - Export to Turtle (TTL) format
  - Import from TTL files
  - RDF compatibility
  - Ontology interchange
- **Templates System**
  - Predefined concept templates
  - Custom template creation
  - Template-based concept creation
  - Template management
- **Notes System**
  - Add notes to ontologies
  - Rich text editing
  - Markdown support
- **Linked Ontologies**
  - Import concepts from other ontologies
  - Link to external ontologies
  - Track ontology dependencies
- **Fork/Clone**
  - Fork ontologies to create variants
  - Clone ontologies for experimentation
  - Provenance tracking
  - Lineage visualization
- **Undo/Redo**
  - Command pattern implementation
  - Undo recent changes (Ctrl+Z)
  - Redo undone changes (Ctrl+Y)
  - Operation history
- **Real-time Collaboration**
  - SignalR hub for live updates
  - Multi-user editing
  - Change broadcasting
  - Conflict prevention
- **User Management**
  - ASP.NET Core Identity
  - OAuth authentication
  - User profiles
  - Email verification
- **User Preferences**
  - Default concept colors
  - Auto-color by category
  - Text size scaling
  - Theme preferences
- **Help System**
  - Interactive framework guide
  - Contextual help
  - Examples and best practices
- **Feature Toggles**
  - Admin-controlled feature flags
  - Gradual feature rollout
  - A/B testing support

---

## [0.1.0] - 2025-10-20

### Initial Release
- Basic ontology editor
- Concept and relationship management
- Graph visualization
- User authentication
- Database persistence

---

## Technical Stack

### Frontend
- Blazor Server (.NET 9)
- Bootstrap 5
- Cytoscape.js (graph visualization)
- SignalR (real-time updates)

### Backend
- ASP.NET Core 9.0
- Entity Framework Core 9.0
- SQL Server / Azure SQL Database

### Infrastructure
- Azure App Service (Linux containers)
- Azure SQL Database
- Azure Key Vault
- Azure Application Insights
- GitHub Actions (CI/CD)

### Testing
- xUnit (test framework)
- bUnit (Blazor component testing)
- Moq (mocking)
- EF Core In-Memory (integration tests)

---

## Contributors

- Benjamin Hoffman (Product Owner)
- Claude (AI Development Assistant)

---

## Links

- **Live Application**: https://eidosonto.com
- **Repository**: [GitHub](https://github.com/YOUR_USERNAME/ontology-builder)
- **Documentation**: See README.md
- **Deployment Guide**: See DEPLOYMENT.md
