using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Eidos.Components;
using Eidos.Components.Account;
using Eidos.Constants;
using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services;
using Eidos.Services.Export;
using Eidos.Services.Import;
using Eidos.Services.Interfaces;
using Eidos.Middleware;
using AspNetCoreRateLimit;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Azure Key Vault Configuration
// Always attempt to load from Key Vault if URI is configured
// DefaultAzureCredential handles authentication in both dev and prod:
// - Development: Uses Azure CLI credentials (requires 'az login')
// - Production: Uses Managed Identity
var keyVaultUri = builder.Configuration["KeyVault:Uri"];

if (!string.IsNullOrEmpty(keyVaultUri))
{
    try
    {
        var keyVaultEndpoint = new Uri(keyVaultUri);

        // Use DefaultAzureCredential which supports:
        // - Managed Identity (in Azure)
        // - Azure CLI (for local development - run 'az login')
        // - Visual Studio credentials
        // - Environment variables
        var credential = new DefaultAzureCredential();

        builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, credential);

        Console.WriteLine($"✓ Azure Key Vault configured at {keyVaultUri}");

        // In development, override the connection string to use Docker SQL Server
        // Key Vault provides OAuth secrets, but we use local database
        if (builder.Environment.IsDevelopment())
        {
            var dockerConnectionString = "Server=localhost,1433;Database=EidosDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";
            builder.Configuration["ConnectionStrings:DefaultConnection"] = dockerConnectionString;
            Console.WriteLine("ℹ️  Development mode: Using Docker SQL Server (localhost:1433)");
            Console.WriteLine("   OAuth secrets loaded from Key Vault");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Warning: Could not connect to Azure Key Vault: {ex.Message}");
        Console.WriteLine("   Falling back to User Secrets and appsettings. Run 'az login' to authenticate.");
    }
}
else
{
    Console.WriteLine("ℹ️  Azure Key Vault not configured. Using User Secrets and appsettings.");
}

// Configure Kestrel for faster shutdown in development
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    });

    // Reduce shutdown timeout in development
    builder.Host.ConfigureHostOptions(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromSeconds(3);
    });
}

// Configure Application Insights (production only)
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];

        // Disable adaptive sampling to capture all telemetry details
        options.EnableAdaptiveSampling = false; // Capture 100% of telemetry for debugging
        options.EnablePerformanceCounterCollectionModule = true;
        options.EnableDependencyTrackingTelemetryModule = true;
        options.EnableRequestTrackingTelemetryModule = true;
        options.EnableEventCounterCollectionModule = true;
        options.EnableQuickPulseMetricStream = true; // Live Metrics

        // Enable detailed dependency tracking (SQL queries, HTTP calls, etc.)
        options.DependencyCollectionOptions.EnableLegacyCorrelationHeadersInjection = true;
    });

    // Configure detailed SQL tracking
    builder.Services.ConfigureTelemetryModule<Microsoft.ApplicationInsights.DependencyCollector.DependencyTrackingTelemetryModule>(
        (module, o) =>
        {
            module.EnableSqlCommandTextInstrumentation = true; // Track actual SQL queries
        }
    );

    // Add telemetry processor for enriching data
    builder.Services.AddApplicationInsightsTelemetryProcessor<EnrichmentTelemetryProcessor>();

    Console.WriteLine("✓ Application Insights configured with detailed tracking and optimized sampling");
}

// Add services to the container.
builder.Services.AddRazorPages(); // For authentication pages
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR for real-time collaborative editing
builder.Services.AddSignalR();

// Add HTTP context accessor for authentication
builder.Services.AddHttpContextAccessor();

// Configure rate limiting (only in production)
builder.Services.AddMemoryCache();
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
}

// Add authentication services
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Configure circuit options for large operations
builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
    // In development, disconnect circuits faster for quicker shutdown
    options.DisconnectedCircuitRetentionPeriod = builder.Environment.IsDevelopment()
        ? TimeSpan.FromSeconds(10)  // Fast shutdown in dev
        : TimeSpan.FromMinutes(5);   // Retain longer in production
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(5);
    options.MaxBufferedUnacknowledgedRenderBatches = 20;
});

// Configure database with DbContextFactory for Blazor Server concurrency
// Use SQL Server for both development (via Docker) and production (Azure SQL Database)
builder.Services.AddDbContextFactory<OntologyDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Add authentication services (required by Identity)
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});

authBuilder.AddIdentityCookies();

// Add GitHub OAuth provider (required)
authBuilder.AddGitHub(options =>
{
    options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? throw new InvalidOperationException("GitHub ClientId not configured in Key Vault or User Secrets");
    options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? throw new InvalidOperationException("GitHub ClientSecret not configured in Key Vault or User Secrets");
    options.CallbackPath = "/signin-github";
    options.Scope.Add("user:email");
    options.SaveTokens = true;

    // Configure correlation cookie to prevent correlation errors
    options.CorrelationCookie.HttpOnly = true;
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always; // Always require HTTPS
    options.CorrelationCookie.IsEssential = true;

    // Assign roles after successful authentication
    options.Events.OnCreatingTicket = async context => await ProgramHelpers.HandleOAuthTicketCreationAsync(context, "GitHub");
});

// Add Google OAuth provider (optional - only if configured)
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;

        // Configure correlation cookie to prevent correlation errors
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.CorrelationCookie.IsEssential = true;

        // Assign roles after successful authentication
        options.Events.OnCreatingTicket = async context => await ProgramHelpers.HandleOAuthTicketCreationAsync(context, "Google");
    });
}

// Add Microsoft OAuth provider (optional - only if configured)
var microsoftClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
var microsoftClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = microsoftClientId;
        options.ClientSecret = microsoftClientSecret;
        options.CallbackPath = "/signin-microsoft";
        options.SaveTokens = true;

        // Configure correlation cookie to prevent correlation errors
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.CorrelationCookie.IsEssential = true;

        // Assign roles after successful authentication
        options.Events.OnCreatingTicket = async context => await ProgramHelpers.HandleOAuthTicketCreationAsync(context, "Microsoft");
    });
}

// Configure ASP.NET Core Identity with strong security settings
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    // Password settings - enforce strong passwords
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;

    // Lockout settings - prevent brute force attacks
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(AppConstants.Security.LockoutDurationMinutes);
    options.Lockout.MaxFailedAccessAttempts = AppConstants.Security.MaxLoginAttempts;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // SignIn settings
    options.SignIn.RequireConfirmedEmail = false; // Set to true in production with email service
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddRoles<IdentityRole>() // Add role support
    .AddEntityFrameworkStores<OntologyDbContext>()
    .AddSignInManager()
    .AddRoleManager<RoleManager<IdentityRole>>() // Add role manager
    .AddDefaultTokenProviders();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    // Admin-only policy
    options.AddPolicy(AppPolicies.RequireAdmin, policy =>
        policy.RequireRole(AppRoles.Admin));

    // Power user policy (Admin or PowerUser)
    options.AddPolicy(AppPolicies.RequirePowerUser, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.PowerUser));

    // Authenticated user policy
    options.AddPolicy(AppPolicies.RequireAuthenticatedUser, policy =>
        policy.RequireAuthenticatedUser());
});

// Register DatabaseSeeder service
builder.Services.AddScoped<DatabaseSeeder>();

// Register User Management service
builder.Services.AddScoped<UserManagementService>();

// Register Ontology Permission service
builder.Services.AddScoped<OntologyPermissionService>();

// Configure cookie settings for secure authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true; // Prevent XSS attacks
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Always require HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax; // Changed from Strict to Lax for better compatibility
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Configure external authentication cookie (for OAuth providers)
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Always require HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax; // Critical for OAuth callbacks
    options.Cookie.IsEssential = true; // Required for GDPR compliance
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15); // Give enough time for OAuth flow
    options.SlidingExpiration = false;
});

// Configure Data Protection for development
// This prevents correlation errors when the app restarts during OAuth flow
if (builder.Environment.IsDevelopment())
{
    var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "keys");
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("Eidos");
}

// Add HTTP client for downloading ontologies
builder.Services.AddHttpClient();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OntologyDbContext>("database");

// Register Repositories (Data Layer)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOntologyRepository, OntologyRepository>();
builder.Services.AddScoped<IConceptRepository, ConceptRepository>();
builder.Services.AddScoped<IRelationshipRepository, RelationshipRepository>();
builder.Services.AddScoped<IIndividualRepository, IndividualRepository>();
builder.Services.AddScoped<IRestrictionRepository, RestrictionRepository>();

// Register Export Strategies (Strategy Pattern)
builder.Services.AddScoped<IExportStrategy, JsonExportStrategy>();
builder.Services.AddScoped<IExportStrategy, CsvExportStrategy>();
builder.Services.AddScoped<IExportStrategy>(sp => new TtlExportStrategy(Eidos.Services.Export.RdfFormat.Turtle));
builder.Services.AddScoped<IOntologyExporter, OntologyExporter>();

// Register Command Pattern (for undo/redo)
builder.Services.AddScoped<Eidos.Services.Commands.ICommandFactory, Eidos.Services.Commands.CommandFactory>();
builder.Services.AddScoped<Eidos.Services.Commands.CommandInvoker>();

// Register Focused Services (Single Responsibility Principle)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<IFeatureToggleService, FeatureToggleService>();
builder.Services.AddScoped<IConceptService, ConceptService>();
builder.Services.AddScoped<IRelationshipService, RelationshipService>();
builder.Services.AddScoped<IOntologyShareService, OntologyShareService>();
builder.Services.AddScoped<IOntologyActivityService, OntologyActivityService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IIndividualService, IndividualService>();
builder.Services.AddScoped<IRestrictionService, RestrictionService>();
builder.Services.AddScoped<IRelationshipSuggestionService, RelationshipSuggestionService>();

// Register Import Services (Single Responsibility Principle)
builder.Services.AddScoped<IRdfParser, RdfParser>();
builder.Services.AddScoped<IOntologyImporter, OntologyImporter>();

// Register Application Services (with interfaces for DI)
builder.Services.AddScoped<TutorialService>();
builder.Services.AddScoped<IOntologyService, OntologyService>(); // Now a facade that delegates to focused services
builder.Services.AddScoped<OntologyTemplateService>();
builder.Services.AddScoped<OntologyDownloadService>();
builder.Services.AddScoped<ITtlImportService, TtlImportService>();
builder.Services.AddScoped<ITtlExportService, TtlExportService>();

// Register Security Services
builder.Services.AddScoped<SecurityEventLogger>();

// Register UI Services (scoped per circuit to prevent cross-client issues)
builder.Services.AddScoped<ConfirmService>();
builder.Services.AddScoped<ToastService>();

// Keep legacy services for backward compatibility (will be removed in Phase 2)
builder.Services.AddScoped<JsonExportService>();
builder.Services.AddScoped<CsvExportService>();

var app = builder.Build();

// Global exception handling middleware - must be early in pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Apply pending migrations automatically on startup.
// WARNING: Automatically applying migrations on startup can be risky in production environments.
// This approach may lead to data loss (if migrations are destructive), deployment failures,
// or conflicts in multi-instance deployments (e.g., in cloud or containerized environments).
// It is recommended to apply migrations manually as a separate deployment step in production.
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OntologyDbContext>>();
    using var db = dbFactory.CreateDbContext();

    // Use migrations instead of EnsureCreated for proper schema management
    db.Database.Migrate();

    // Seed roles and admin users
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Enable HSTS (HTTP Strict Transport Security) in all environments
// This forces browsers to only use HTTPS
app.UseHsts();

// Add security headers middleware
app.Use(async (context, next) =>
{
    // Content Security Policy - allows Blazor to work while still providing protection
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https: blob:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "connect-src 'self' wss: https://cdn.jsdelivr.net https://unpkg.com; " +
        "frame-ancestors 'none';");

    // Prevent MIME type sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // Prevent clickjacking
    context.Response.Headers.Append("X-Frame-Options", "DENY");

    // Enable browser XSS protection
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

    // Control referrer information
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Permissions Policy (formerly Feature Policy)
    context.Response.Headers.Append("Permissions-Policy",
        "geolocation=(), microphone=(), camera=()");

    await next();
});

app.UseHttpsRedirection();

// Enable rate limiting (only in production)
if (!app.Environment.IsDevelopment())
{
    app.UseIpRateLimiting();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages(); // Map Razor Pages for authentication

// Map additional Identity endpoints
app.MapAdditionalIdentityEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub for real-time collaborative editing
app.MapHub<Eidos.Hubs.OntologyHub>("/ontologyhub");

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.Run();

/// <summary>
/// Helper methods for Program.cs
/// </summary>
static partial class ProgramHelpers
{
    /// <summary>
    /// Handles OAuth ticket creation for all OAuth providers.
    /// Assigns default roles to users after successful authentication.
    /// </summary>
    /// <param name="context">The OAuth creating ticket context</param>
    /// <param name="providerName">Name of the OAuth provider (GitHub, Google, Microsoft)</param>
    public static async Task HandleOAuthTicketCreationAsync(
        Microsoft.AspNetCore.Authentication.OAuth.OAuthCreatingTicketContext context,
        string providerName)
    {
        try
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Eidos.Models.ApplicationUser>>();
            var seeder = context.HttpContext.RequestServices.GetRequiredService<Eidos.Services.DatabaseSeeder>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();

            // Get the user email from claims
            var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                // Find existing user (they may not exist yet on first login)
                var user = await userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    await seeder.AssignDefaultRolesOnLoginAsync(user);
                    logger.LogInformation("{Provider} OAuth: Roles assigned for user: {Email}", providerName, email);
                }
                else
                {
                    logger.LogInformation("{Provider} OAuth: User not found during OnCreatingTicket, will be created by framework: {Email}",
                        providerName, email);
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't break the login flow
            var logger = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
            logger.LogError(ex, "Error in {Provider} OAuth OnCreatingTicket event", providerName);
        }
    }
}
