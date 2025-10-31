# Eidos Development Guidelines

## Code Style & Standards

### C# Conventions

#### Naming
- **Classes/Interfaces**: PascalCase (e.g., `OntologyService`, `IOntologyService`)
- **Methods**: PascalCase (e.g., `GetOntologyAsync`, `CanViewAsync`)
- **Properties**: PascalCase (e.g., `OntologyId`, `UserName`)
- **Private Fields**: camelCase with underscore prefix (e.g., `_logger`, `_context`)
- **Parameters**: camelCase (e.g., `ontologyId`, `userId`)
- **Local Variables**: camelCase (e.g., `ontology`, `permissions`)

#### Async/Await
- All I/O operations must be async
- Suffix async methods with `Async` (e.g., `GetOntologyAsync`)
- Use `Task<T>` for async methods that return values
- Use `Task` for async methods that don't return values
- Always await async calls, never use `.Result` or `.Wait()`

```csharp
// ✅ Good
public async Task<Ontology> GetOntologyAsync(int id)
{
    return await _context.Ontologies.FindAsync(id);
}

// ❌ Bad
public Ontology GetOntology(int id)
{
    return _context.Ontologies.Find(id); // Synchronous
}
```

### Dependency Injection

#### Registration Lifetimes
- **Scoped**: Services that should live for the duration of a request (most services)
  - OntologyService, ConceptService, repositories
- **Singleton**: Stateless services, shared state, or expensive to create
  - IPresenceService, caching services
- **Transient**: Lightweight services, created each time requested
  - Rarely used in this project

```csharp
// In Program.cs
builder.Services.AddScoped<IOntologyService, OntologyService>();
builder.Services.AddSingleton<IPresenceService, InMemoryPresenceService>();
```

#### Constructor Injection
```csharp
public class OntologyService : IOntologyService
{
    private readonly IOntologyRepository _repository;
    private readonly ILogger<OntologyService> _logger;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;

    public OntologyService(
        IOntologyRepository repository,
        ILogger<OntologyService> logger,
        IDbContextFactory<OntologyDbContext> contextFactory)
    {
        _repository = repository;
        _logger = logger;
        _contextFactory = contextFactory;
    }
}
```

### Error Handling

#### Always Use Try-Catch for Critical Operations
```csharp
public async Task<CollaborationPost> CreatePostAsync(CollaborationPost post)
{
    try
    {
        _logger.LogInformation("Creating collaboration post '{Title}'", post.Title);

        // Implementation here

        _logger.LogInformation("Successfully created post {PostId}", post.Id);
        return post;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating collaboration post '{Title}'", post.Title);
        throw; // Re-throw to let caller handle
    }
}
```

#### User-Friendly Error Messages
```csharp
try
{
    await SaveChanges();
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Database error saving ontology {OntologyId}", ontologyId);
    ToastService.ShowError("Failed to save changes. Please try again.");
}
```

### Logging Best Practices

#### Use Structured Logging
```csharp
// ✅ Good - Structured with placeholders
_logger.LogInformation("User {UserId} created ontology {OntologyId}", userId, ontologyId);

// ❌ Bad - String interpolation
_logger.LogInformation($"User {userId} created ontology {ontologyId}");
```

#### Log Levels
- **LogTrace**: Very detailed, usually only for debugging
- **LogDebug**: Detailed information for debugging
- **LogInformation**: General information about application flow
- **LogWarning**: Unexpected but recoverable issues
- **LogError**: Errors that prevent operation completion
- **LogCritical**: Catastrophic failures

```csharp
_logger.LogInformation("Starting collaboration post creation");
_logger.LogWarning("Ontology {OntologyId} not found, skipping visibility update", ontologyId);
_logger.LogError(ex, "Failed to create collaboration group for post {PostId}", postId);
```

### Permission Checking

#### Always Use OntologyPermissionService
```csharp
// ✅ Good - Use permission service
var canEdit = await _permissionService.CanEditAsync(ontologyId, userId);
if (!canEdit)
{
    throw new UnauthorizedAccessException("User does not have edit permission");
}

// ❌ Bad - Direct ownership check only
if (ontology.UserId != userId)
{
    throw new UnauthorizedAccessException(); // Ignores group permissions!
}
```

#### Permission Check Locations
1. **UI Components**: Check before showing edit buttons
2. **Services**: Validate before performing operations
3. **SignalR Hubs**: Verify before allowing real-time actions
4. **API Endpoints**: Guard all public endpoints

### Database Operations

#### Use Repository Pattern
```csharp
// ✅ Good - Use repository
var ontology = await _ontologyRepository.GetByIdAsync(id);

// ❌ Bad - Direct DbContext access in service
var ontology = await _context.Ontologies.FindAsync(id);
```

#### Optimize Queries

##### Avoid N+1 Queries
```csharp
// ✅ Good - Eager load with Include
var ontologies = await _context.Ontologies
    .Include(o => o.User)
    .Include(o => o.Tags)
    .ToListAsync();

// ❌ Bad - N+1 query
var ontologies = await _context.Ontologies.ToListAsync();
// Later: var userName = ontology.User.Name; // Separate query per ontology!
```

##### Use AsNoTracking for Read-Only Operations
```csharp
// ✅ Good - Read-only, no tracking overhead
var ontologies = await _context.Ontologies
    .AsNoTracking()
    .Where(o => o.Visibility == "public")
    .ToListAsync();

// ❌ Bad - Tracks entities unnecessarily
var ontologies = await _context.Ontologies
    .Where(o => o.Visibility == "public")
    .ToListAsync();
```

##### Use Projections to Reduce Data Transfer
```csharp
// ✅ Good - Only select needed fields
var summaries = await _context.Ontologies
    .Select(o => new OntologySummary
    {
        Id = o.Id,
        Name = o.Name,
        ConceptCount = o.ConceptCount
    })
    .ToListAsync();

// ❌ Bad - Load entire entities
var ontologies = await _context.Ontologies.ToListAsync();
var summaries = ontologies.Select(o => new OntologySummary { ... });
```

### Blazor Component Guidelines

#### Use RenderMode
```razor
@page "/collaboration"
@rendermode InteractiveServer
```

#### Inject Services at Top
```razor
@inject IOntologyService OntologyService
@inject ILogger<CollaborationBoard> Logger
@inject ToastService ToastService
```

#### Handle Loading States
```razor
@if (isLoading)
{
    <div class="spinner-border" role="status">
        <span class="visually-hidden">Loading...</span>
    </div>
}
else if (posts == null || !posts.Any())
{
    <div class="alert alert-info">No collaboration posts found.</div>
}
else
{
    @foreach (var post in posts)
    {
        <!-- Display posts -->
    }
}
```

#### Use OnInitializedAsync for Data Loading
```csharp
@code {
    private List<CollaborationPost>? posts;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            isLoading = true;
            posts = await CollaborationService.GetActivePostsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading collaboration posts");
            ToastService.ShowError("Failed to load posts");
        }
        finally
        {
            isLoading = false;
        }
    }
}
```

### SignalR Hub Guidelines

#### Permission Validation
```csharp
public async Task JoinOntology(int ontologyId)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    // Always validate permissions
    var hasPermission = await _permissionService.CanViewAsync(ontologyId, userId);
    if (!hasPermission)
    {
        _logger.LogWarning("Permission denied for user {UserId} to join ontology {OntologyId}",
            userId, ontologyId);
        throw new HubException("You do not have permission to join this ontology");
    }

    // Continue with join logic...
}
```

#### Broadcast to Others
```csharp
// Notify other users in the group
await Clients.OthersInGroup(groupName).SendAsync("UserJoined", presenceInfo);

// Send data only to caller
await Clients.Caller.SendAsync("PresenceList", currentUsers);

// Send to all users in group (including caller)
await Clients.Group(groupName).SendAsync("ConceptAdded", concept);
```

### Testing Guidelines

#### Test Structure
```csharp
public class OntologyServiceTests
{
    [Fact]
    public async Task CreateOntologyAsync_ValidInput_ReturnsOntology()
    {
        // Arrange
        var options = CreateInMemoryDbOptions();
        using var context = new OntologyDbContext(options);
        var service = new OntologyService(repository, logger, contextFactory);

        var ontology = new Ontology { Name = "Test", UserId = "user123" };

        // Act
        var result = await service.CreateOntologyAsync(ontology, "user123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateOntologyAsync_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateOntologyAsync(null!, "user123")
        );
    }
}
```

#### Use In-Memory Database for Repository Tests
```csharp
private DbContextOptions<OntologyDbContext> CreateInMemoryDbOptions()
{
    return new DbContextOptionsBuilder<OntologyDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
}
```

### Documentation

#### XML Documentation for Public APIs
```csharp
/// <summary>
/// Creates a new collaboration post with automatic group creation and permission grant.
/// </summary>
/// <param name="post">The collaboration post to create.</param>
/// <returns>The created collaboration post with group linked.</returns>
/// <exception cref="ArgumentNullException">Thrown when post is null.</exception>
/// <exception cref="InvalidOperationException">Thrown when group creation fails.</exception>
public async Task<CollaborationPost> CreatePostAsync(CollaborationPost post)
{
    // Implementation
}
```

#### Inline Comments for Complex Logic
```csharp
// Map database permission strings to PermissionLevel enum
// Database stores: "view", "edit", "admin"
// Enum values: View, ViewAddEdit, FullAccess
PermissionLevel = p.PermissionLevel.ToLowerInvariant() switch
{
    "view" => PermissionLevel.View,
    "edit" => PermissionLevel.ViewAddEdit,
    "admin" => PermissionLevel.FullAccess,
    _ => PermissionLevel.View
}
```

### Security Best Practices

#### Never Trust Client Input
```csharp
// ✅ Good - Validate and sanitize
if (string.IsNullOrWhiteSpace(ontology.Name) || ontology.Name.Length > 200)
{
    throw new ArgumentException("Invalid ontology name");
}

// ❌ Bad - Accept any input
_context.Ontologies.Add(ontology);
```

#### Check Environment for Sensitive Operations
```csharp
// Development-only endpoints
if (!_environment.IsDevelopment())
{
    _logger.LogWarning("Attempted to access dev endpoint in {Environment}",
        _environment.EnvironmentName);
    return Results.StatusCode(403);
}
```

#### Use HttpOnly Cookies
```csharp
httpContext.Response.Cookies.Append("session", value, new CookieOptions
{
    HttpOnly = true,  // Prevents JavaScript access
    Secure = true,    // HTTPS only
    SameSite = SameSiteMode.Lax
});
```

### Performance Considerations

#### Use DbContextFactory for Scoped Contexts
```csharp
// ✅ Good - Create scoped context
using var context = await _contextFactory.CreateDbContextAsync();
var ontology = await context.Ontologies.FindAsync(id);

// ❌ Bad - Long-lived context in singleton
_context.Ontologies.Find(id); // Context injected as singleton - memory leak!
```

#### Cache Frequently Accessed Data
```csharp
// Cache user preferences with sliding expiration
var cacheKey = $"user_prefs_{userId}";
if (!_cache.TryGetValue(cacheKey, out UserPreferences? prefs))
{
    prefs = await _context.UserPreferences.FindAsync(userId);
    _cache.Set(cacheKey, prefs, new MemoryCacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(5)
    });
}
```

### Git Commit Messages

#### Format
```
<type>: <subject>

<body>

🤖 Generated with Claude Code

Co-Authored-By: Claude <noreply@anthropic.com>
```

#### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

#### Example
```
feat: Add collaboration board with automatic group management

- Created CollaborationBoardService with automatic group creation
- Updated OntologyHub to recognize group permissions
- Added PermissionsSettingsTab group display
- Enhanced user switching for development testing

🤖 Generated with Claude Code

Co-Authored-By: Claude <noreply@anthropic.com>
```

## Code Review Checklist

Before submitting changes, verify:

- [ ] All new services registered in Program.cs
- [ ] Permission checks added where needed
- [ ] Comprehensive logging with structured placeholders
- [ ] Error handling with try-catch blocks
- [ ] User-friendly error messages via ToastService
- [ ] XML documentation for public APIs
- [ ] Tests added for new functionality
- [ ] Database migrations created if schema changed
- [ ] DEVELOPMENT_LEDGER.md updated
- [ ] Release notes updated if user-facing
- [ ] User guide updated if user-facing
- [ ] Build succeeds with no new errors
- [ ] All tests pass (dotnet test)
- [ ] No N+1 queries introduced
- [ ] AsNoTracking used for read-only queries

## Common Pitfalls to Avoid

1. **Forgetting to await async calls** - Use async/await consistently
2. **Direct DbContext access in services** - Use repositories
3. **N+1 query problems** - Use Include() for related entities
4. **Missing permission checks** - Always validate access
5. **Not handling errors** - Wrap critical operations in try-catch
6. **Poor logging** - Use structured logging with placeholders
7. **Tracking entities unnecessarily** - Use AsNoTracking() for reads
8. **Singleton services with DbContext** - Use DbContextFactory
9. **Missing null checks** - Validate inputs and handle nulls
10. **Exposing sensitive info in logs** - Don't log passwords, tokens, etc.

## Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [SignalR Documentation](https://docs.microsoft.com/aspnet/core/signalr)
- [Cytoscape.js](https://js.cytoscape.org/)
