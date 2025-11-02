# Eidos Architecture Documentation

## System Architecture Overview

Eidos follows a **layered architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│                     Presentation Layer                    │
│  (Blazor Components, Razor Pages, SignalR Client JS)     │
└─────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────┐
│                    Application Layer                      │
│         (Services, SignalR Hubs, API Endpoints)          │
└─────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────┐
│                      Data Access Layer                    │
│           (Repositories, DbContext, Models)               │
└─────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────┐
│                       Database Layer                      │
│              (SQLite/Azure SQL, Redis Cache)             │
└─────────────────────────────────────────────────────────┘
```

## Technology Stack Deep Dive

### Frontend Architecture

#### Blazor Server
- **Interactive Mode**: Server-side rendering with SignalR for interactivity
- **Render Modes**: `@rendermode InteractiveServer` for dynamic components
- **State Management**: Component state + cascading parameters
- **Real-time Updates**: SignalR connection for bidirectional communication

#### Component Hierarchy
```
App.razor (Root)
├── Routes.razor (Routing)
├── MainLayout.razor
│   ├── NavMenu.razor
│   └── Page Content
│       ├── Home.razor
│       ├── OntologyView.razor
│       │   ├── Graph View (Cytoscape.js)
│       │   ├── List View
│       │   ├── Hierarchy View
│       │   └── Settings Dialog
│       │       ├── General Settings Tab
│       │       └── Permissions Settings Tab
│       ├── CollaborationBoard.razor
│       └── MyCollaborationPosts.razor
└── Toast Container (Global)
```

### Backend Architecture

#### Service Layer Pattern

**Service Responsibilities**:
1. Business logic implementation
2. Transaction management
3. Permission validation
4. Logging and error handling
5. Coordination between repositories

**Key Services**:

```csharp
// Ontology Management
IOntologyService
- CreateOntologyAsync()
- UpdateOntologyAsync()
- DeleteOntologyAsync()
- CloneOntologyAsync()
- ImportFromTtlAsync()
- ExportToTtlAsync()

// Concept & Relationship Management
IConceptService (with Command Pattern)
- AddConceptAsync() -> Returns ICommand
- UpdateConceptAsync() -> Returns ICommand
- DeleteConceptAsync() -> Returns ICommand
- Undo() / Redo()

// Collaboration & Permissions
ICollaborationBoardService
- CreatePostAsync() -> Auto-creates group + grants permissions
- UpdateResponseStatusAsync() -> Auto-manages group membership

IOntologyPermissionService
- CanViewAsync()
- CanEditAsync()
- CanManageAsync()
- GrantGroupAccessAsync()

// User & Group Management
IUserGroupService
- CreateGroupAsync()
- AddUserToGroupAsync()
- RemoveUserFromGroupAsync()
- GrantGroupPermissionAsync()
```

#### Repository Pattern

**Purpose**: Abstract data access logic from business logic

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

// Specialized repositories extend base repository
public interface IOntologyRepository : IRepository<Ontology>
{
    Task<IEnumerable<Ontology>> GetByUserIdAsync(string userId);
    Task<IEnumerable<Ontology>> GetPublicOntologiesAsync();
    Task<Ontology?> GetWithDetailsAsync(int id);
}
```

**Benefits**:
- Testability (easy to mock)
- Consistency in data access
- Single responsibility
- Query optimization in one place

### Real-Time Collaboration Architecture

#### SignalR Hub Design

```
                    ┌─────────────────┐
                    │  OntologyHub    │
                    │   (Server)      │
                    └────────┬────────┘
                             │
            ┌────────────────┼────────────────┐
            │                │                │
      ┌─────▼─────┐    ┌────▼────┐    ┌─────▼─────┐
      │  Client 1  │    │ Client 2│    │  Client 3 │
      │  (Browser) │    │(Browser)│    │ (Browser) │
      └────────────┘    └─────────┘    └───────────┘
```

**Hub Methods**:
- `JoinOntology(ontologyId)` - User joins collaboration session
  - Validates permissions via OntologyPermissionService
  - Adds to SignalR group
  - Tracks presence
  - Broadcasts join event

- `LeaveOntology(ontologyId)` - User leaves session
  - Removes from group
  - Cleans up presence
  - Broadcasts leave event

- `UpdateCurrentView(ontologyId, viewName)` - Track view changes
  - Updates presence info
  - Broadcasts to others

- `Heartbeat(ontologyId)` - Keep-alive (30s interval)
  - Updates LastSeenAt timestamp
  - Detects disconnections

**Presence Tracking**:
```csharp
public interface IPresenceService
{
    Task AddOrUpdatePresenceAsync(int ontologyId, PresenceInfo info);
    Task RemovePresenceAsync(int ontologyId, string connectionId);
    Task<List<PresenceInfo>> GetPresenceListAsync(int ontologyId);
    Task UpdateLastSeenAsync(int ontologyId, string connectionId);
    Task UpdateCurrentViewAsync(int ontologyId, string connectionId, string viewName);
}

// Implementations:
// - InMemoryPresenceService (single-server)
// - RedisPresenceService (distributed - optional)
```

### Data Access Architecture

#### DbContext Design

```csharp
public class OntologyDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Ontology> Ontologies { get; set; }
    public DbSet<Concept> Concepts { get; set; }
    public DbSet<Relationship> Relationships { get; set; }
    public DbSet<Individual> Individuals { get; set; }
    public DbSet<IndividualRelationship> IndividualRelationships { get; set; }
    public DbSet<CollaborationPost> CollaborationPosts { get; set; }
    public DbSet<CollaborationResponse> CollaborationResponses { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<UserGroupMember> UserGroupMembers { get; set; }
    public DbSet<OntologyGroupPermission> OntologyGroupPermissions { get; set; }
    public DbSet<OntologyActivity> OntologyActivities { get; set; }
    public DbSet<UserPreference> UserPreferences { get; set; }
    public DbSet<OntologyTag> OntologyTags { get; set; }
}
```

#### DbContext Factory Pattern

```csharp
// Service registration
builder.Services.AddDbContextFactory<OntologyDbContext>(options =>
    options.UseSqlite(connectionString)
);

// Usage in services (creates scoped context per operation)
public class OntologyService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;

    public async Task<Ontology> GetOntologyAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Ontologies.FindAsync(id);
    }
}
```

**Why DbContextFactory?**
- Singleton services can create scoped contexts
- Better memory management
- Thread-safe
- Prevents context lifetime issues

### Permission System Architecture

#### Multi-Level Permission Hierarchy

```
┌─────────────────────────────────────────────────┐
│               Permission Check Flow              │
└─────────────────────────────────────────────────┘
                        │
                        ↓
            ┌───────────────────────┐
            │  Is User the Owner?   │
            └───────────┬───────────┘
                        │
                  Yes ──┘   No
                  │          │
                  ↓          ↓
            ┌─────────┐  ┌───────────────────────┐
            │ GRANTED │  │ Is Visibility Public? │
            └─────────┘  └─────────┬─────────────┘
                                   │
                            Yes ───┤─── No
                             │     │
                             ↓     ↓
                     ┌─────────┐  ┌────────────────────────┐
                     │ GRANTED │  │ Check Group Membership │
                     └─────────┘  └──────────┬─────────────┘
                                             │
                                      Found ─┤─── Not Found
                                       │     │
                                       ↓     ↓
                                 ┌─────────┐ ┌────────┐
                                 │ GRANTED │ │ DENIED │
                                 └─────────┘ └────────┘
```

#### Permission Service Design

```csharp
public class OntologyPermissionService
{
    // Core permission checks
    public async Task<bool> CanViewAsync(int ontologyId, string? userId)
    {
        // 1. Check if owner
        // 2. Check if public visibility
        // 3. Check group permissions
        // 4. Check direct share access
    }

    public async Task<bool> CanEditAsync(int ontologyId, string? userId)
    {
        // Same hierarchy + check permission level >= Edit
    }

    public async Task<bool> CanManageAsync(int ontologyId, string? userId)
    {
        // Owner only or group admin
    }

    // Group permission management
    public async Task GrantGroupAccessAsync(int ontologyId, int groupId,
        string permissionLevel, string grantedBy)
    {
        // Creates OntologyGroupPermission record
        // Logs grant action
    }

    public async Task<List<OntologyGroupPermission>>
        GetGroupPermissionsAsync(int ontologyId)
    {
        // Returns all groups with access + member counts
    }
}
```

### Collaboration Workflow Architecture

#### Automatic Group Management Flow

```
User Creates Collaboration Post
            │
            ↓
    ┌───────────────────────┐
    │ CollaborationBoard    │
    │    Service            │
    └───────────┬───────────┘
                │
                ↓
    1. Create UserGroup
       "Collaboration: {Title}"
                │
                ↓
    2. Add Creator as Group Admin
                │
                ↓
    3. Grant Group Edit Permission
       to Ontology
                │
                ↓
    4. Set Ontology Visibility
       to "Group"
                │
                ↓
    5. Link Group to Post
       (CollaborationProjectGroupId)
                │
                ↓
    Post Created ✅


User Applies to Project
            │
            ↓
    CollaborationResponse Created
    (Status: Pending)


Post Owner Accepts Response
            │
            ↓
    ┌───────────────────────┐
    │ UpdateResponseStatus  │
    │    Async              │
    └───────────┬───────────┘
                │
                ↓
    1. Change Status to "Accepted"
                │
                ↓
    2. Add User to Group
       (UserGroupMembers)
                │
                ↓
    User Gains Edit Access ✅


User Accesses Ontology
            │
            ↓
    ┌───────────────────────┐
    │  OntologyHub          │
    │  JoinOntology()       │
    └───────────┬───────────┘
                │
                ↓
    Permission Check via
    OntologyPermissionService
                │
                ↓
    Finds User in Group
    with Edit Permission
                │
                ↓
    Access Granted ✅
```

## Design Patterns Used

### 1. Repository Pattern
**Purpose**: Abstract data access
**Location**: `Data/Repositories/`
**Example**: `OntologyRepository`, `ConceptRepository`

### 2. Command Pattern
**Purpose**: Undo/redo functionality
**Location**: `Services/Commands/`
**Example**: `AddConceptCommand`, `DeleteRelationshipCommand`

### 3. Factory Pattern
**Purpose**: Create DbContext instances
**Location**: Built-in `IDbContextFactory<T>`
**Usage**: All services use factory instead of direct injection

### 4. Service Layer Pattern
**Purpose**: Business logic encapsulation
**Location**: `Services/`
**Example**: `OntologyService`, `CollaborationBoardService`

### 5. Dependency Injection
**Purpose**: Loose coupling, testability
**Location**: `Program.cs` registration
**Usage**: Constructor injection throughout

### 6. Observer Pattern
**Purpose**: Real-time updates
**Implementation**: SignalR (pub/sub model)
**Usage**: Presence tracking, concurrent editing

## Data Flow Examples

### Example 1: Creating a Concept

```
User clicks "Add Concept" in UI
            │
            ↓
    ConceptDialog.razor
    - User fills form
    - Clicks Save
            │
            ↓
    ConceptService.AddConceptAsync()
    - Creates AddConceptCommand
    - Validates input
    - Logs action
            │
            ↓
    ConceptRepository.AddAsync()
    - Adds to DbContext
    - Saves changes
    - Returns concept
            │
            ↓
    ActivityTrackingService
    - Records activity
    - Captures before/after
            │
            ↓
    ConceptService
    - Increments ConceptCount
    - Returns concept
            │
            ↓
    UI Component
    - Shows success toast
    - Updates graph view
    - Refreshes concept list
```

### Example 2: Real-Time Presence Update

```
User opens ontology
            │
            ↓
    OntologyView.razor
    - OnInitializedAsync()
    - Creates SignalR connection
            │
            ↓
    JS: ontologyHub.start()
            │
            ↓
    JS: hub.invoke("JoinOntology", ontologyId)
            │
            ↓
    OntologyHub.JoinOntology()
    - Validates permission
    - Adds to SignalR group
    - Creates PresenceInfo
            │
            ↓
    PresenceService.AddOrUpdatePresenceAsync()
    - Stores in memory/Redis
            │
            ↓
    SignalR: Clients.OthersInGroup().SendAsync("UserJoined")
            │
            ↓
    Other Connected Browsers
    - Receive "UserJoined" event
    - Update user avatars
    - Show new user in presence list
```

## Scalability Considerations

### Current Architecture (Single Server)
- **SignalR**: In-process (no backplane)
- **Presence**: In-memory dictionary
- **Cache**: In-memory cache
- **Database**: Single SQLite/SQL Server

**Suitable for**: Small to medium deployments (< 100 concurrent users)

### Scaling to Multiple Servers

**Required Changes**:

1. **SignalR Backplane**:
```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(connectionString, options => {
        options.Configuration.ChannelPrefix = "eidos-signalr";
    });
```

2. **Distributed Presence**:
```csharp
builder.Services.AddSingleton<IPresenceService, RedisPresenceService>();
```

3. **Distributed Cache**:
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "eidos-cache:";
});
```

4. **Session Management**:
```csharp
builder.Services.AddStackExchangeRedisCache(options => { ... });
builder.Services.AddSession();
```

## Security Architecture

### Authentication Flow

```
User visits /Account/Login
            │
            ↓
    User clicks OAuth provider
    (Google, Microsoft, GitHub)
            │
            ↓
    Redirect to provider
    (/Account/ExternalLogin)
            │
            ↓
    Provider authenticates user
            │
            ↓
    Redirect back with token
    (/Account/ExternalLogin-callback)
            │
            ↓
    LoginModel.OnPostExternalLogin()
    - Validates token
    - Creates/updates user
    - Signs in with cookie
            │
            ↓
    Redirect to /
    User is authenticated ✅
```

### Authorization Layers

1. **Route Protection**: `[Authorize]` attribute
2. **Hub Methods**: Permission check in each method
3. **Service Layer**: Permission validation before operations
4. **UI Layer**: Conditional rendering based on permissions

## Performance Optimizations

### Query Optimization Techniques

1. **Eager Loading**: `Include()` for related entities
2. **No Tracking**: `AsNoTracking()` for read-only
3. **Projections**: `Select()` for DTOs
4. **Indexing**: Foreign key columns indexed
5. **Denormalization**: ConceptCount, RelationshipCount cached

### Caching Strategy

```
┌────────────────────────────────────────┐
│        User Preferences Cache           │
│  (5 min sliding expiration)            │
└────────────────────────────────────────┘
            Reduces DB reads by 90%

┌────────────────────────────────────────┐
│        Presence Tracking Cache          │
│  (In-memory or Redis)                  │
└────────────────────────────────────────┘
            Fast real-time updates

┌────────────────────────────────────────┐
│        Public Ontologies Cache          │
│  (Could be added - 1 min expiration)   │
└────────────────────────────────────────┘
            Future optimization
```

## Testing Architecture

### Test Pyramid

```
                   ┌────────┐
                   │  E2E   │  (Future)
                   └────────┘
                ┌──────────────┐
                │  Integration │  (Service + Repository)
                └──────────────┘
            ┌────────────────────────┐
            │       Unit Tests       │  (Logic, Utilities)
            └────────────────────────┘
```

**Current Coverage**:
- **Unit Tests**: LoginModel OAuth detection
- **Integration Tests**: OntologyPermissionService (20+ tests)
- **Component Tests**: (Planned with bUnit)

### Test Infrastructure

```csharp
// In-memory database for repository tests
var options = new DbContextOptionsBuilder<OntologyDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;

// Mock services
var mockLogger = new Mock<ILogger<OntologyService>>();
var mockToastService = new Mock<ToastService>();

// Test fixtures for shared setup
public class OntologyServiceTestFixture : IDisposable
{
    public OntologyDbContext CreateContext() { ... }
    public void Dispose() { ... }
}
```

## Deployment Architecture

### Production Environment

```
┌─────────────────────────────────────────────────┐
│              Azure App Service                   │
│          (Linux or Windows Container)            │
│                                                  │
│  ┌───────────────────────────────────────────┐  │
│  │           Eidos Application               │  │
│  │  (Blazor Server + SignalR + ASP.NET)      │  │
│  └───────────────────────────────────────────┘  │
└──────────────┬──────────────┬───────────────────┘
               │              │
    ┌──────────┴─────┐   ┌────┴────────────┐
    │  Azure SQL DB  │   │ App Insights    │
    │  (Production)  │   │  (Monitoring)   │
    └────────────────┘   └─────────────────┘
```

### CI/CD Pipeline (GitHub Actions)

```
Push to main branch
        │
        ↓
GitHub Actions Workflow
        │
        ├─→ Restore dependencies
        ├─→ Build solution
        ├─→ Run tests
        ├─→ Publish artifacts
        └─→ Deploy to Azure App Service
                │
                ↓
        Production Site Updated
```

## Future Architecture Considerations

### Planned Enhancements

1. **Microservices Migration** (if needed at scale)
   - Separate collaboration service
   - Separate permission service
   - API Gateway pattern

2. **CQRS Pattern** (for complex queries)
   - Separate read/write models
   - Event sourcing for version control

3. **Message Queue** (for async operations)
   - Email notifications via queue
   - Bulk operations async processing

4. **CDN Integration**
   - Static assets via CDN
   - Graph visualization caching

5. **GraphQL API** (alternative to REST)
   - More flexible queries
   - Reduce over-fetching

---

**Last Updated**: October 31, 2025
