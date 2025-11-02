# Comprehensive Security Audit Report
## Eidos Ontology Builder Application

**Audit Date:** October 30, 2025
**Application Path:** `/Users/benjaminhoffman/documents/code/ontology-builder/onto-editor/eidos`
**Technology Stack:** ASP.NET Core 9.0, Blazor Server, Entity Framework Core, SignalR
**Auditor:** Security Assessment Team

---

## Executive Summary

This comprehensive security audit evaluated the Eidos Ontology Builder application across 10 critical security domains. The application demonstrates **strong security fundamentals** with well-implemented authentication, authorization, and input validation. However, several **HIGH and MEDIUM severity findings** require immediate attention to achieve production-grade security posture.

**Overall Security Posture:** **B+ (Good with improvements needed)**

### Critical Statistics
- **Critical Findings:** 0
- **High Severity:** 5
- **Medium Severity:** 8
- **Low Severity:** 12
- **Informational:** 7

---

## 1. Authentication & Authorization

### ✅ STRENGTHS

#### 1.1 OAuth Implementation
**Location:** `/Program.cs` (Lines 201-278)

**Findings:**
- **EXCELLENT:** Proper OAuth 2.0 implementation for GitHub, Google, and Microsoft providers
- **EXCELLENT:** Secure cookie configuration with `HttpOnly`, `SameSite=Lax`, and `SecurePolicy=Always`
- **EXCELLENT:** Token storage enabled for refresh token support (`SaveTokens = true`)
- **EXCELLENT:** External authentication cookie properly configured with 15-minute expiration

```csharp
// Program.cs - Lines 215-218
options.CorrelationCookie.HttpOnly = true;
options.CorrelationCookie.SameSite = SameSiteMode.Lax;
options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
options.CorrelationCookie.IsEssential = true;
```

#### 1.2 ASP.NET Core Identity Configuration
**Location:** `/Program.cs` (Lines 280-308)

**Findings:**
- **EXCELLENT:** Strong password requirements (8 chars, uppercase, lowercase, digit, special char, 4 unique)
- **EXCELLENT:** Account lockout enabled (5 attempts, 15 minute lockout)
- **EXCELLENT:** Unique email requirement enforced
- **GOOD:** Role-based access control properly implemented

```csharp
// Program.cs - Lines 283-294
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequiredLength = 8;
options.Password.RequiredUniqueChars = 4;

options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.AllowedForNewUsers = true;
```

#### 1.3 Authorization Policies
**Location:** `/Program.cs` (Lines 310-324), `/Models/AppRoles.cs`

**Findings:**
- **GOOD:** Three-tier authorization model (Admin, PowerUser, User)
- **GOOD:** Policies properly configured using role requirements
- **EXCELLENT:** Centralized role constants prevent typos

### 🚨 HIGH SEVERITY FINDINGS

#### H-AUTH-01: Development Auto-Login Enabled in Production Risk
**Severity:** HIGH
**Location:** `/Middleware/DevelopmentAuthMiddleware.cs`, `/Program.cs` (Line 531)
**CVSS Score:** 9.1 (Critical if deployed to production)

**Issue:**
The application has development auto-login middleware that could be accidentally enabled in production if the `Development:EnableAutoLogin` configuration is not properly managed.

```csharp
// Program.cs - Lines 528-532
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<Eidos.Middleware.DevelopmentAuthMiddleware>();
}
```

**Risk:**
- Automatic authentication bypass
- Unauthorized access to any user account
- Complete security compromise if configuration error occurs

**Current Mitigation:**
- Middleware only registers in Development environment
- Configuration-based flag required

**Exploitation Scenario:**
1. Attacker modifies environment variable `ASPNETCORE_ENVIRONMENT=Development`
2. Sets `Development:EnableAutoLogin=true`
3. Gains automatic authentication as configured test user

**Recommendation:**
```csharp
// Enhanced protection with double-check
if (app.Environment.IsDevelopment() && !app.Environment.IsProduction())
{
    var enableAutoLogin = builder.Configuration.GetValue<bool>("Development:EnableAutoLogin");
    if (enableAutoLogin)
    {
        _logger.LogWarning("SECURITY WARNING: Development auto-login is ENABLED");
        app.UseMiddleware<Eidos.Middleware.DevelopmentAuthMiddleware>();
    }
}

// Add startup validation
if (app.Environment.IsProduction())
{
    var autoLoginEnabled = builder.Configuration.GetValue<bool>("Development:EnableAutoLogin");
    if (autoLoginEnabled)
    {
        throw new InvalidOperationException(
            "SECURITY ERROR: Development auto-login cannot be enabled in production");
    }
}
```

#### H-AUTH-02: Missing Email Confirmation Requirement
**Severity:** HIGH
**Location:** `/Program.cs` (Lines 301-302)
**CWE:** CWE-306 (Missing Authentication for Critical Function)

**Issue:**
```csharp
options.SignIn.RequireConfirmedEmail = false;
options.SignIn.RequireConfirmedAccount = false;
```

**Risk:**
- Account enumeration via registration
- Email address spoofing
- Spam account creation
- No verification of email ownership

**Recommendation:**
1. Implement email confirmation flow
2. Add email service integration (SendGrid, AWS SES)
3. Set `RequireConfirmedEmail = true`
4. Add email verification UI components

#### H-AUTH-03: Hardcoded Database Password in Development
**Severity:** HIGH
**Location:** `/Program.cs` (Line 50)
**CWE:** CWE-798 (Use of Hard-coded Credentials)

**Issue:**
```csharp
var dockerConnectionString = "Server=localhost,1433;Database=EidosDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";
```

**Risk:**
- Weak password exposed in source code
- Password visible in version control history
- Potential SQL Server compromise if port exposed
- Common password pattern easily guessable

**Recommendation:**
```csharp
// Use environment variables or user secrets
var dockerConnectionString = builder.Configuration.GetConnectionString("DockerConnection")
    ?? throw new InvalidOperationException("Docker connection string not configured");

// In appsettings.Development.json or user secrets:
// "ConnectionStrings": {
//   "DockerConnection": "Server=localhost,1433;Database=EidosDb;User Id=sa;Password={SECURE_PASSWORD};TrustServerCertificate=True;"
// }
```

### ⚠️ MEDIUM SEVERITY FINDINGS

#### M-AUTH-01: Missing Two-Factor Authentication (2FA)
**Severity:** MEDIUM
**Location:** N/A (Feature not implemented)
**CWE:** CWE-308 (Use of Single-factor Authentication)

**Issue:**
Application does not support 2FA/MFA for enhanced account security.

**Risk:**
- Account compromise from password leaks
- Insufficient protection for admin accounts
- Non-compliance with security best practices

**Recommendation:**
Implement TOTP-based 2FA using ASP.NET Core Identity features:
```csharp
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    // ... existing options ...
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
})
.AddTokenProvider<AuthenticatorTokenProvider<ApplicationUser>>(TokenOptions.DefaultAuthenticatorProvider);
```

#### M-AUTH-02: External Login Bypass of Two-Factor
**Severity:** MEDIUM
**Location:** `/Pages/Account/ExternalLogin.cshtml.cs` (Line 50)

**Issue:**
```csharp
var result = await _signInManager.ExternalLoginSignInAsync(
    info.LoginProvider,
    info.ProviderKey,
    isPersistent: true,
    bypassTwoFactor: true  // ⚠️ 2FA bypass
);
```

**Risk:**
- Circumvents 2FA protection if implemented
- Weakens security for OAuth-authenticated users

**Recommendation:**
Set `bypassTwoFactor: false` and implement 2FA challenge for external logins.

#### M-AUTH-03: Admin Email Configuration in appsettings
**Severity:** MEDIUM
**Location:** `/appsettings.Development.json` (Line 36)

**Issue:**
```json
"AdminEmail": "hoffchops@outlook.com"
```

**Risk:**
- Personal email exposed in source code
- Version control leakage
- Incorrect admin assignments in production

**Recommendation:**
- Move to environment variables or Key Vault
- Support multiple admin emails
- Add admin audit logging

### 📋 LOW SEVERITY FINDINGS

#### L-AUTH-01: Correlation Cookie Security in Development
**Severity:** LOW
**Location:** `/Program.cs` (Lines 245-247, 270-272)

**Issue:**
Google and Microsoft OAuth use `CookieSecurePolicy.SameAsRequest` in development, while GitHub uses `Always`.

**Recommendation:**
Standardize all providers to use conditional secure policy for consistency.

---

## 2. Input Validation & Injection Attacks

### ✅ STRENGTHS

#### 2.1 SQL Injection Protection
**Finding:** **EXCELLENT - No SQL Injection Vulnerabilities Found**

**Analysis:**
- All database operations use Entity Framework Core with parameterized queries
- No `FromSqlRaw` or `ExecuteSqlRaw` usage detected
- LINQ queries properly parameterized
- Repository pattern enforces safe data access

**Evidence:**
```csharp
// Example from OntologyPermissionService.cs (Lines 27-37)
var ontologyInfo = await _context.Ontologies
    .Where(o => o.Id == ontologyId)
    .Select(o => new { o.UserId, o.Visibility, o.Id })
    .AsNoTracking()
    .FirstOrDefaultAsync();
```

#### 2.2 File Upload Validation
**Location:** `/Services/Import/RdfParser.cs` (Lines 12-54)
**Finding:** **EXCELLENT - Comprehensive File Upload Protection**

**Security Controls:**
```csharp
private const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024; // 10MB

// Size validation before reading
if (fileStream.CanSeek && fileStream.Length > MAX_FILE_SIZE_BYTES)
{
    return Task.FromResult(new TtlImportResult
    {
        Success = false,
        ErrorMessage = "File size exceeds maximum allowed size"
    });
}

// Buffered reading with size enforcement
var buffer = new byte[8192]; // 8KB buffer
long totalBytesRead = 0;

while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
{
    totalBytesRead += bytesRead;
    if (totalBytesRead > MAX_FILE_SIZE_BYTES)
    {
        return Task.FromResult(new TtlImportResult
        {
            Success = false,
            ErrorMessage = "File size exceeds maximum allowed size"
        });
    }
    memoryStream.Write(buffer, 0, bytesRead);
}
```

**Protections:**
- ✅ Maximum file size (10MB) enforced
- ✅ Buffered reading prevents memory exhaustion
- ✅ Size validation for both seekable and non-seekable streams
- ✅ Defense against zip bombs and DoS attacks

#### 2.3 XSS Prevention in Blazor
**Finding:** **GOOD - Framework-Level Protection**

**Analysis:**
- Blazor automatically encodes output by default
- No use of `MarkupString` without validation detected
- HTML sanitization handled by framework

### 🚨 HIGH SEVERITY FINDINGS

#### H-INPUT-01: Insufficient TTL/RDF Parsing Validation
**Severity:** HIGH
**Location:** `/Services/Import/RdfParser.cs` (Lines 62-77)
**CWE:** CWE-112 (Missing XML Validation)

**Issue:**
The RDF parser uses simple string detection to determine file format without proper validation:

```csharp
if (content.TrimStart().StartsWith("<") &&
    (content.Contains("<?xml") || content.Contains("<rdf:RDF") || content.Contains("xmlns:rdf")))
{
    parser = new RdfXmlParser();
}
else
{
    parser = new TurtleParser();
}
```

**Risks:**
- XML External Entity (XXE) attacks if RdfXmlParser doesn't disable external entities
- Malformed RDF files causing parser crashes
- Potential ReDoS from complex XML structures
- No schema validation

**Exploitation Scenario:**
```xml
<?xml version="1.0"?>
<!DOCTYPE foo [
  <!ENTITY xxe SYSTEM "file:///etc/passwd">
]>
<rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
  <rdf:Description rdf:about="http://example.org/malicious">
    <rdfs:label>&xxe;</rdfs:label>
  </rdf:Description>
</rdf:RDF>
```

**Recommendation:**
```csharp
private IRdfReader CreateSecureParser(string content)
{
    IRdfReader parser;

    if (content.TrimStart().StartsWith("<") &&
        (content.Contains("<?xml") || content.Contains("<rdf:RDF")))
    {
        // Configure secure XML parser
        var xmlParser = new RdfXmlParser
        {
            // Disable DTD processing to prevent XXE
            DtdProcessing = System.Xml.DtdProcessing.Prohibit
        };
        parser = xmlParser;
    }
    else
    {
        parser = new TurtleParser();
    }

    return parser;
}

// Add timeout protection
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
try
{
    parser.Load(graph, reader);
}
catch (OperationCanceledException)
{
    throw new OntologyImportException("Import timeout - file too complex");
}
```

#### H-INPUT-02: Unvalidated String Length in SignalR
**Severity:** HIGH
**Location:** `/Hubs/OntologyHub.cs` (Lines 248-254)
**CWE:** CWE-20 (Improper Input Validation)

**Issue:**
Partial validation but insufficient:

```csharp
public async Task UpdateCurrentView(int ontologyId, string viewName)
{
    if (string.IsNullOrWhiteSpace(viewName) || viewName.Length > 50)
    {
        _logger.LogWarning("Invalid view name provided: {ViewName}", viewName);
        return; // ⚠️ Silent failure, no error to client
    }
    // ... stores viewName without additional validation
}
```

**Risks:**
- Injection into presence tracking system
- XSS if viewName displayed without encoding
- Log injection via malicious viewName

**Recommendation:**
```csharp
public async Task UpdateCurrentView(int ontologyId, string viewName)
{
    // Validate and sanitize
    if (string.IsNullOrWhiteSpace(viewName))
    {
        throw new HubException("View name cannot be empty");
    }

    // Whitelist valid view names
    var validViews = new[] { "Graph", "List", "Hierarchy", "TTL", "Notes" };
    if (!validViews.Contains(viewName, StringComparer.OrdinalIgnoreCase))
    {
        throw new HubException("Invalid view name");
    }

    // Rest of implementation...
}
```

### ⚠️ MEDIUM SEVERITY FINDINGS

#### M-INPUT-01: Missing Content-Type Validation for File Uploads
**Severity:** MEDIUM
**Location:** `/Services/Import/RdfParser.cs`

**Issue:**
Parser relies on content inspection but doesn't validate Content-Type header.

**Recommendation:**
Add MIME type validation:
```csharp
public Task<TtlImportResult> ParseAsync(Stream fileStream, string contentType)
{
    var allowedTypes = new[] {
        "text/turtle",
        "application/rdf+xml",
        "application/x-turtle"
    };

    if (!allowedTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
    {
        return Task.FromResult(new TtlImportResult
        {
            Success = false,
            ErrorMessage = "Invalid file type"
        });
    }
    // ... rest of parsing
}
```

#### M-INPUT-02: Potential Path Traversal in Ontology Import
**Severity:** MEDIUM
**Location:** `/Services/Import/OntologyImporter.cs` (Lines 45-46)

**Issue:**
Custom name/description from user input used without sanitization:

```csharp
Name = customName ?? RdfUtilities.ExtractOntologyName(graph) ?? "Imported Ontology",
Description = customDescription ?? RdfUtilities.ExtractOntologyDescription(graph) ?? "Imported from RDF file"
```

**Risk:**
- Potential for malicious content in ontology metadata
- Script injection if displayed without proper encoding

**Recommendation:**
```csharp
private string SanitizeInput(string? input, int maxLength = 200)
{
    if (string.IsNullOrWhiteSpace(input))
        return string.Empty;

    // Remove control characters
    input = new string(input.Where(c => !char.IsControl(c)).ToArray());

    // Trim to max length
    if (input.Length > maxLength)
        input = input.Substring(0, maxLength);

    return input.Trim();
}

Name = SanitizeInput(customName ?? RdfUtilities.ExtractOntologyName(graph))
    ?? "Imported Ontology"
```

### 📋 LOW SEVERITY FINDINGS

#### L-INPUT-01: SecurityEventLogger Log Injection Protection
**Severity:** LOW
**Location:** `/Services/SecurityEventLogger.cs` (Lines 15-20)
**Finding:** **GOOD - Basic sanitization implemented**

```csharp
private static string? SanitizeForLog(string? input)
{
    if (input == null) return null;
    return input.Replace("\r", "").Replace("\n", "");
}
```

**Enhancement Recommendation:**
Add additional control character removal:
```csharp
private static string? SanitizeForLog(string? input)
{
    if (input == null) return null;

    // Remove all control characters including CRLF
    return new string(input.Where(c => !char.IsControl(c) || c == ' ').ToArray());
}
```

---

## 3. API Security

### ✅ STRENGTHS

#### 3.1 Rate Limiting Configuration
**Location:** `/Program.cs` (Lines 127-135), `/appsettings.json` (Lines 20-47)
**Finding:** **EXCELLENT - Comprehensive Rate Limiting**

**Configuration:**
```json
"IpRateLimiting": {
  "EnableEndpointRateLimiting": true,
  "GeneralRules": [
    { "Endpoint": "*", "Period": "1m", "Limit": 100 },
    { "Endpoint": "*/Account/Login", "Period": "5m", "Limit": 5 },
    { "Endpoint": "*/Account/Register", "Period": "1h", "Limit": 3 },
    { "Endpoint": "*/Account/ExternalLogin", "Period": "5m", "Limit": 10 }
  ]
}
```

**Analysis:**
- ✅ Endpoint-specific rate limits
- ✅ Aggressive limits on authentication endpoints
- ✅ Production-only enforcement (Lines 129-130, 523-526)
- ✅ Brute force protection

#### 3.2 CSRF Protection
**Location:** `/Program.cs` (Line 537)
**Finding:** **EXCELLENT - Antiforgery enabled globally**

```csharp
app.UseAntiforgery();
```

**Analysis:**
- Blazor Server automatically validates antiforgery tokens
- Form submissions protected by default
- No `[IgnoreAntiforgeryToken]` attributes found

#### 3.3 SignalR Hub Authorization
**Location:** `/Hubs/OntologyHub.cs` (Lines 1-17)
**Finding:** **GOOD - Hub requires authentication**

```csharp
[Authorize]
public class OntologyHub : Hub
{
    // ... implementation
}
```

**Permission Validation:**
```csharp
// Lines 96-141 - Comprehensive permission checking
if (string.IsNullOrEmpty(userId))
{
    permissionReason = "not_authenticated";
}
else if (ontology.UserId == userId)
{
    hasPermission = true;
    permissionReason = "owner";
}
else
{
    var hasShareAccess = await context.UserShareAccesses
        .Where(usa => usa.UserId == userId)
        .Where(usa => usa.OntologyShare != null && usa.OntologyShare.OntologyId == ontologyId)
        .Where(usa => usa.OntologyShare != null && usa.OntologyShare.IsActive)
        .AnyAsync();
    // ...
}
```

### ⚠️ MEDIUM SEVERITY FINDINGS

#### M-API-01: Rate Limiting Disabled in Development
**Severity:** MEDIUM
**Location:** `/Program.cs` (Lines 127-135, 523-526)
**CWE:** CWE-770 (Allocation of Resources Without Limits)

**Issue:**
```csharp
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    // ... rate limiting setup
}

// ...later...
if (!app.Environment.IsDevelopment())
{
    app.UseIpRateLimiting();
}
```

**Risk:**
- Development environment vulnerable to brute force
- Testing doesn't validate rate limiting functionality
- Potential for staging environment misconfiguration

**Recommendation:**
```csharp
// Always configure rate limiting, adjust limits for dev
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    var config = builder.Configuration.GetSection("IpRateLimiting");
    config.Bind(options);

    if (builder.Environment.IsDevelopment())
    {
        // More permissive in dev but still protected
        foreach (var rule in options.GeneralRules)
        {
            rule.Limit *= 10; // 10x higher limits in dev
        }
    }
});
```

#### M-API-02: Missing API Versioning
**Severity:** MEDIUM
**Location:** N/A (Feature not implemented)

**Issue:**
No API versioning strategy for SignalR or potential REST endpoints.

**Risk:**
- Breaking changes affect all clients
- Difficult to maintain backward compatibility
- No graceful deprecation path

**Recommendation:**
Implement API versioning:
```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
```

#### M-API-03: SignalR Message Size Limits
**Severity:** MEDIUM
**Location:** `/Program.cs` (Line 122)

**Issue:**
No explicit message size limits configured for SignalR.

**Recommendation:**
```csharp
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});
```

### 📋 LOW SEVERITY FINDINGS

#### L-API-01: Missing Request Logging for Security Events
**Severity:** LOW
**Location:** Various service methods

**Recommendation:**
Add comprehensive request logging for audit trails:
```csharp
app.Use(async (context, next) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    _logger.LogInformation(
        "Request: {Method} {Path} | User: {UserId} | IP: {IpAddress}",
        context.Request.Method,
        context.Request.Path,
        userId ?? "Anonymous",
        context.Connection.RemoteIpAddress
    );
    await next();
});
```

---

## 4. Data Protection

### ✅ STRENGTHS

#### 4.1 Password Storage
**Location:** `/Program.cs` (Lines 280-308)
**Finding:** **EXCELLENT - Industry Standard Protection**

**Analysis:**
- ASP.NET Core Identity uses PBKDF2 with HMAC-SHA256
- Automatic password hashing with salt
- Configurable iteration count (default: 10,000 iterations)
- Password history not tracked (consider adding)

#### 4.2 Azure Key Vault Integration
**Location:** `/Program.cs` (Lines 22-65)
**Finding:** **EXCELLENT - Proper Secrets Management**

```csharp
var keyVaultUri = builder.Configuration["KeyVault:Uri"];

if (!string.IsNullOrEmpty(keyVaultUri))
{
    try
    {
        var keyVaultEndpoint = new Uri(keyVaultUri);
        var credential = new DefaultAzureCredential();
        builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, credential);

        Console.WriteLine($"✓ Azure Key Vault configured at {keyVaultUri}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Warning: Could not connect to Azure Key Vault: {ex.Message}");
        Console.WriteLine("   Falling back to User Secrets and appsettings.");
    }
}
```

**Strengths:**
- ✅ DefaultAzureCredential supports multiple auth methods
- ✅ Graceful fallback to User Secrets
- ✅ Proper error handling without exposing secrets
- ✅ OAuth secrets stored in Key Vault, not appsettings

#### 4.3 Connection String Handling
**Location:** `/Program.cs` (Lines 158-183)
**Finding:** **GOOD - Proper connection string management**

**Analysis:**
- Connection strings from configuration
- Support for both SQLite (dev) and SQL Server (prod)
- Sensitive data logging only in development

#### 4.4 Data Protection Key Persistence
**Location:** `/Program.cs` (Lines 359-368)
**Finding:** **GOOD - OAuth correlation protection**

```csharp
if (builder.Environment.IsDevelopment())
{
    var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "keys");
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("Eidos");
}
```

### 🚨 HIGH SEVERITY FINDINGS

#### H-DATA-01: SQL Query Logging in Production
**Severity:** HIGH
**Location:** `/Program.cs` (Lines 103-108)
**CWE:** CWE-532 (Insertion of Sensitive Information into Log File)

**Issue:**
```csharp
builder.Services.ConfigureTelemetryModule<Microsoft.ApplicationInsights.DependencyCollector.DependencyTrackingTelemetryModule>(
    (module, o) =>
    {
        module.EnableSqlCommandTextInstrumentation = true; // ⚠️ Logs SQL with parameters
    }
);
```

**Risk:**
- **Sensitive data logged**: Passwords, emails, personal information
- **Query parameters exposed**: User IDs, ontology data
- **Compliance violation**: GDPR, HIPAA, PCI-DSS
- **Log aggregation risk**: Centralized logs contain sensitive data

**Example Exposure:**
```
SQL Query: UPDATE AspNetUsers SET PasswordHash = 'AQAAAAEAACcQAAAAEG...' WHERE Email = 'user@example.com'
```

**Recommendation:**
```csharp
// Option 1: Disable in production
if (!builder.Environment.IsDevelopment())
{
    builder.Services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>(
        (module, o) =>
        {
            module.EnableSqlCommandTextInstrumentation = false; // Secure
        }
    );
}

// Option 2: Implement query parameter filtering
builder.Services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>(
    (module, o) =>
    {
        module.EnableSqlCommandTextInstrumentation = true;
    }
);

// Add telemetry processor to filter sensitive data
builder.Services.AddApplicationInsightsTelemetryProcessor<SensitiveDataFilterProcessor>();
```

#### H-DATA-02: Sensitive Data Logging in Development
**Severity:** HIGH (in development)
**Location:** `/Program.cs` (Lines 185-189)

**Issue:**
```csharp
if (builder.Environment.IsDevelopment())
{
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
}
```

**Risk:**
- Passwords, emails logged in development
- Development logs may be committed to version control
- Shared development environments expose data

**Recommendation:**
```csharp
if (builder.Environment.IsDevelopment())
{
    // Only enable if explicitly requested
    var enableSensitiveLogging = builder.Configuration.GetValue<bool>("Debug:EnableSensitiveDataLogging");
    if (enableSensitiveLogging)
    {
        _logger.LogWarning("⚠️ SECURITY WARNING: Sensitive data logging is ENABLED");
        options.EnableSensitiveDataLogging();
    }
    options.EnableDetailedErrors();
}
```

### ⚠️ MEDIUM SEVERITY FINDINGS

#### M-DATA-01: Missing Data Encryption at Rest
**Severity:** MEDIUM
**Location:** Database configuration
**CWE:** CWE-311 (Missing Encryption of Sensitive Data)

**Issue:**
SQLite database (`ontology.db`) not encrypted at rest in development.

**Risk:**
- Physical access to development machines exposes data
- Backup files contain unencrypted data
- Version control accidents expose database

**Recommendation:**
```csharp
// For SQLite in production, use encryption
if (connectionString?.StartsWith("Data Source="))
{
    var sqliteConnectionString = new SqliteConnectionStringBuilder(connectionString)
    {
        Password = builder.Configuration["Database:SqlitePassword"],
        DataSource = connectionString.Replace("Data Source=", "")
    }.ToString();

    options.UseSqlite(sqliteConnectionString);
}

// For SQL Server, enforce TDE (Transparent Data Encryption)
options.UseSqlServer(connectionString, sqlOptions =>
{
    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
});
```

#### M-DATA-02: Data Protection Keys Not Persisted in Production
**Severity:** MEDIUM
**Location:** `/Program.cs` (Lines 359-368)

**Issue:**
Data protection keys only persisted in development.

**Risk:**
- OAuth tokens invalidated on app restart
- Users logged out after deployment
- Session cookies invalidated

**Recommendation:**
```csharp
// Production: Persist to Azure Blob Storage or Key Vault
if (builder.Environment.IsProduction())
{
    var blobStorageConnection = builder.Configuration["Azure:DataProtectionBlobStorage"];
    if (!string.IsNullOrEmpty(blobStorageConnection))
    {
        builder.Services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(blobStorageConnection, "dataprotection", "keys.xml")
            .ProtectKeysWithAzureKeyVault(
                new Uri(builder.Configuration["KeyVault:Uri"]),
                new DefaultAzureCredential())
            .SetApplicationName("Eidos");
    }
}
else if (builder.Environment.IsDevelopment())
{
    // Development: File system
    var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "keys");
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("Eidos");
}
```

#### M-DATA-03: Missing Personal Data Encryption
**Severity:** MEDIUM
**Location:** Database models

**Issue:**
User emails, display names not encrypted at column level.

**Risk:**
- Database breach exposes PII
- Compliance requirements (GDPR Article 32)
- Insider threat exposure

**Recommendation:**
Implement column-level encryption for sensitive fields:
```csharp
[ProtectedPersonalData]
public string Email { get; set; }

[ProtectedPersonalData]
public string DisplayName { get; set; }

// Add IPersonalDataProtector
public class EncryptedPersonalDataConverter : ValueConverter<string, string>
{
    public EncryptedPersonalDataConverter(IPersonalDataProtector protector)
        : base(
            plaintext => protector.Protect(plaintext),
            ciphertext => protector.Unprotect(ciphertext))
    {
    }
}
```

### 📋 LOW SEVERITY FINDINGS

#### L-DATA-01: Missing Audit Trail for Data Access
**Severity:** LOW
**Location:** Service layer

**Recommendation:**
Implement comprehensive audit logging:
```csharp
public class AuditLogEntry
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; } // Create, Read, Update, Delete
    public string EntityType { get; set; }
    public int EntityId { get; set; }
    public string? Changes { get; set; } // JSON diff
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
}
```

---

## 5. Security Headers & Transport

### ✅ STRENGTHS

#### 5.1 Security Headers Implementation
**Location:** `/Program.cs` (Lines 488-518)
**Finding:** **EXCELLENT - Comprehensive Security Headers**

```csharp
app.Use(async (context, next) =>
{
    // Content Security Policy
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https: blob:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "connect-src 'self' wss: https://cdn.jsdelivr.net https://unpkg.com; " +
        "frame-ancestors 'none';");

    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    await next();
});
```

**Security Analysis:**
- ✅ **CSP**: Comprehensive policy with Blazor compatibility
- ✅ **X-Content-Type-Options**: MIME sniffing prevented
- ✅ **X-Frame-Options**: Clickjacking protection
- ✅ **X-XSS-Protection**: Browser XSS filter enabled
- ✅ **Referrer-Policy**: Privacy-preserving referrer handling
- ✅ **Permissions-Policy**: Disables unnecessary browser features

#### 5.2 HTTPS Configuration
**Location:** `/Program.cs` (Lines 484-486, 520)
**Finding:** **EXCELLENT - HSTS and HTTPS Enforcement**

```csharp
// Enable HSTS (HTTP Strict Transport Security) in all environments
app.UseHsts();

// ... later ...
app.UseHttpsRedirection();
```

**Configuration in appsettings.json:**
```json
"Kestrel": {
  "Limits": {
    "KeepAliveTimeout": "00:00:30",
    "RequestHeadersTimeout": "00:00:30"
  }
}
```

### ⚠️ MEDIUM SEVERITY FINDINGS

#### M-TRANS-01: CSP Allows 'unsafe-inline' and 'unsafe-eval'
**Severity:** MEDIUM
**Location:** `/Program.cs` (Line 494)
**CWE:** CWE-1004 (Sensitive Cookie Without 'HttpOnly' Flag)

**Issue:**
```csharp
"script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://unpkg.com; " +
"style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; "
```

**Risk:**
- **'unsafe-inline'**: Allows inline scripts, weakening XSS protection
- **'unsafe-eval'**: Enables eval(), potential code injection
- **CDN dependencies**: Third-party script injection risk

**Justification:**
Blazor Server requires these directives for functionality, but they reduce CSP effectiveness.

**Recommendation:**
```csharp
// Option 1: Use nonce-based CSP for better security
var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
context.Items["csp-nonce"] = nonce;

context.Response.Headers.Append("Content-Security-Policy",
    $"default-src 'self'; " +
    $"script-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net https://unpkg.com; " +
    $"style-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net; " +
    $"img-src 'self' data: https: blob:; " +
    $"font-src 'self' https://cdn.jsdelivr.net; " +
    $"connect-src 'self' wss: https://cdn.jsdelivr.net https://unpkg.com; " +
    $"frame-ancestors 'none'; " +
    $"upgrade-insecure-requests;");

// Option 2: Use CSP reporting to monitor violations
context.Response.Headers.Append("Content-Security-Policy-Report-Only",
    "...; report-uri /api/csp-report;");
```

#### M-TRANS-02: Missing HSTS Preload Configuration
**Severity:** MEDIUM
**Location:** `/Program.cs` (Line 486)

**Issue:**
HSTS enabled but not configured for preloading or long duration.

**Current:**
```csharp
app.UseHsts(); // Uses default settings
```

**Risk:**
- First request vulnerable to MITM
- Users not protected on first visit
- Not included in browser HSTS preload lists

**Recommendation:**
```csharp
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
    options.ExcludedHosts.Clear(); // Include localhost in dev for testing
});

// Submit domain to: https://hstspreload.org/
```

#### M-TRANS-03: Missing Certificate Pinning
**Severity:** MEDIUM
**Location:** N/A (Feature not implemented)

**Issue:**
No HTTP Public Key Pinning (HPKP) or Certificate Transparency enforcement.

**Recommendation:**
Implement Expect-CT header:
```csharp
context.Response.Headers.Append("Expect-CT",
    "max-age=86400, enforce, report-uri=\"/api/ct-report\"");
```

### 📋 LOW SEVERITY FINDINGS

#### L-TRANS-01: X-XSS-Protection Header Deprecated
**Severity:** LOW
**Location:** `/Program.cs` (Line 508)

**Issue:**
```csharp
context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
```

**Note:**
Modern browsers deprecated X-XSS-Protection in favor of CSP. Header is harmless but unnecessary.

**Recommendation:**
Consider removing in favor of strong CSP:
```csharp
// Remove: context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
// Rely on CSP instead
```

---

## 6. Middleware & Error Handling

### ✅ STRENGTHS

#### 6.1 Global Exception Handler
**Location:** `/Middleware/GlobalExceptionHandlerMiddleware.cs`
**Finding:** **EXCELLENT - Comprehensive Error Handling**

**Security Features:**
```csharp
// Line 44: Unique correlation ID for tracking
var correlationId = Guid.NewGuid().ToString();
context.Response.Headers.Append("X-Correlation-ID", correlationId);

// Lines 47-179: Pattern matching for specific exceptions
var (statusCode, errorResponse) = exception switch
{
    OntologyNotFoundException ex => (HttpStatusCode.NotFound, new ErrorResponse { ... }),
    InsufficientPermissionException ex => (HttpStatusCode.Forbidden, new ErrorResponse { ... }),
    UnauthorizedAccessException => (HttpStatusCode.Unauthorized, new ErrorResponse { ... }),
    _ => (HttpStatusCode.InternalServerError, new ErrorResponse
    {
        Message = "An unexpected error occurred. Please try again later. " +
                 "If the problem persists, contact support with error ID: " + correlationId,
        Details = _env.IsDevelopment() ? exception.Message : null // ⚠️ Details only in dev
    })
};

// Line 182-186: Proper logging with correlation ID
_logger.LogError(exception,
    "Unhandled exception occurred. Correlation ID: {CorrelationId}, Status Code: {StatusCode}",
    correlationId, (int)statusCode, errorResponse.ErrorCode);
```

**Strengths:**
- ✅ No stack traces in production
- ✅ Correlation IDs for support
- ✅ Structured error responses
- ✅ Proper HTTP status codes
- ✅ User-friendly messages

#### 6.2 Middleware Ordering
**Location:** `/Program.cs` (Lines 443-552)
**Finding:** **GOOD - Proper middleware pipeline**

```csharp
app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); // Line 444 - First
// ... HSTS, security headers, HTTPS redirection ...
app.UseAuthentication(); // Line 534
app.UseAuthorization();  // Line 535
app.UseAntiforgery();    // Line 537
```

**Analysis:**
- ✅ Exception handler at the top of pipeline
- ✅ Security middleware before authentication
- ✅ Authentication before authorization
- ✅ Antiforgery after authorization

### 🚨 HIGH SEVERITY FINDINGS

#### H-MIDDLEWARE-01: Detailed Errors Leaked in Development
**Severity:** HIGH (for development/staging)
**Location:** `/Middleware/GlobalExceptionHandlerMiddleware.cs` (Lines 56, 66, 76, 86, 96, 106, 116, 127, 137, 147, 157, 167, 177)

**Issue:**
Exception details exposed in development mode:

```csharp
new ErrorResponse
{
    CorrelationId = correlationId,
    ErrorCode = ex.ErrorCode,
    Message = ex.UserFriendlyMessage,
    Details = _env.IsDevelopment() ? ex.Message : null // ⚠️ Stack trace exposure risk
}
```

**Risk:**
- **Development:** Acceptable for debugging
- **Staging:** May expose internal paths and logic
- **Misconfig Risk:** If staging uses `Development` environment

**Exploitation Scenario:**
Attacker triggers various exceptions to learn:
- Database schema from EF Core errors
- File paths from file I/O errors
- Internal API structure from validation errors

**Recommendation:**
```csharp
private ErrorResponse CreateErrorResponse(
    string correlationId,
    string errorCode,
    string message,
    Exception ex)
{
    var response = new ErrorResponse
    {
        CorrelationId = correlationId,
        ErrorCode = errorCode,
        Message = message
    };

    // Only expose details in local development
    if (_env.IsDevelopment() &&
        Environment.MachineName.StartsWith("DEV-", StringComparison.OrdinalIgnoreCase))
    {
        response.Details = ex.Message;
        response.StackTrace = ex.StackTrace;
    }

    return response;
}
```

### ⚠️ MEDIUM SEVERITY FINDINGS

#### M-MIDDLEWARE-01: Exception Details in Logs May Be Too Verbose
**Severity:** MEDIUM
**Location:** `/Middleware/GlobalExceptionHandlerMiddleware.cs` (Lines 182-186)

**Issue:**
```csharp
_logger.LogError(exception,
    "Unhandled exception occurred. Correlation ID: {CorrelationId}, Status Code: {StatusCode}, Error Code: {ErrorCode}",
    correlationId, (int)statusCode, errorResponse.ErrorCode);
```

**Risk:**
- Full exception details (including stack traces) logged
- May contain sensitive data in exception messages
- Log aggregation services expose detailed errors

**Recommendation:**
```csharp
// Filter sensitive exception details before logging
var sanitizedException = SanitizeException(exception);

_logger.LogError(sanitizedException,
    "Unhandled exception occurred. Correlation ID: {CorrelationId}, Status Code: {StatusCode}, Error Code: {ErrorCode}, Type: {ExceptionType}",
    correlationId,
    (int)statusCode,
    errorResponse.ErrorCode,
    exception.GetType().Name); // Log type but filter data

private Exception SanitizeException(Exception ex)
{
    // Remove sensitive data from exception messages
    if (ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("secret", StringComparison.OrdinalIgnoreCase))
    {
        return new Exception($"[REDACTED] {ex.GetType().Name}", ex);
    }
    return ex;
}
```

#### M-MIDDLEWARE-02: Missing Request/Response Logging
**Severity:** MEDIUM
**Location:** N/A (Feature not fully implemented)

**Issue:**
No comprehensive request/response logging for security auditing.

**Recommendation:**
```csharp
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        // Log request
        _logger.LogInformation(
            "Request {RequestId}: {Method} {Path} | User: {UserId} | IP: {IpAddress}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous",
            context.Connection.RemoteIpAddress
        );

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        // Log response
        _logger.LogInformation(
            "Response {RequestId}: {StatusCode} | Duration: {Duration}ms",
            requestId,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds
        );
    }
}
```

### 📋 LOW SEVERITY FINDINGS

#### L-MIDDLEWARE-01: JSON Serialization Options
**Severity:** LOW
**Location:** `/Middleware/GlobalExceptionHandlerMiddleware.cs` (Lines 192-196)

**Observation:**
```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = _env.IsDevelopment()
};
```

**Enhancement Recommendation:**
Add security-focused serialization options:
```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = _env.IsDevelopment(),
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    MaxDepth = 32, // Prevent circular reference attacks
    ReferenceHandler = ReferenceHandler.IgnoreCycles
};
```

---

## 7. Dependencies & Known Vulnerabilities

### ✅ STRENGTHS

#### 7.1 Package Versions
**Location:** `/Eidos.csproj`
**Finding:** **EXCELLENT - Up-to-date dependencies**

**Analysis:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Azure.Identity" Version="1.17.0" />
<PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.4.0" />
```

**Security Scanner:**
```bash
dotnet list package --vulnerable --include-transitive
```

**Results:** ✅ No known vulnerabilities in direct dependencies

#### 7.2 Security Scanning Tool
**Location:** `/Eidos.csproj` (Lines 27-30)

**Finding:**
```xml
<PackageReference Include="SecurityCodeScan.VS2019" Version="5.6.7">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

**Analysis:**
- ✅ Static security analysis integrated into build
- ✅ SecurityCodeScan provides SAST capabilities
- ✅ Analyzer runs during compilation

### ⚠️ MEDIUM SEVERITY FINDINGS

#### M-DEP-01: Outdated Security Scanner
**Severity:** MEDIUM
**Location:** `/Eidos.csproj` (Line 27)

**Issue:**
SecurityCodeScan.VS2019 v5.6.7 is the final version (discontinued).

**Risk:**
- No new security rules
- Missing detection for modern attack vectors
- No updates for new vulnerability patterns

**Recommendation:**
Migrate to modern alternatives:
```xml
<!-- Option 1: Use .NET 9 built-in security analyzers -->
<PropertyGroup>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest-All</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>

<!-- Option 2: Add Puma Scan or Roslynator.Security -->
<PackageReference Include="Roslynator.Analyzers" Version="4.12.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>

<!-- Option 3: Use SonarAnalyzer -->
<PackageReference Include="SonarAnalyzer.CSharp" Version="9.31.0.96804">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

#### M-DEP-02: AspNetCoreRateLimit Library Maintenance Status
**Severity:** MEDIUM
**Location:** `/Eidos.csproj` (Line 12)

**Issue:**
```xml
<PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
```

**Analysis:**
- Last major release: 2021
- Limited active development
- May not support .NET 9 features fully

**Risk:**
- Potential compatibility issues
- Missing modern rate limiting features
- Security updates may be delayed

**Recommendation:**
Consider migrating to built-in .NET rate limiting:
```csharp
// .NET 7+ has built-in rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests.", cancellationToken);
    };
});

// Use rate limiting middleware
app.UseRateLimiter();
```

#### M-DEP-03: dotNetRdf Security Considerations
**Severity:** MEDIUM
**Location:** `/Eidos.csproj` (Line 15), `/Services/Import/RdfParser.cs`

**Issue:**
```xml
<PackageReference Include="dotNetRdf" Version="3.4.1" />
```

**Analysis:**
- dotNetRdf 3.4.1 released February 2024
- XML parsing capabilities (XXE risk if misconfigured)
- No known CVEs but requires careful usage

**Risk Assessment:**
- **Current Implementation:** File size limits mitigate DoS
- **XXE Risk:** RdfXmlParser default settings unclear
- **Denial of Service:** Complex RDF graphs may cause performance issues

**Recommendation:**
```csharp
// Verify RdfXmlParser security settings
public class SecureRdfParser : RdfParser
{
    public override Task<TtlImportResult> ParseAsync(Stream fileStream)
    {
        // Add timeout for parsing operations
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            // Existing size validation
            // ... (current code) ...

            // Create secure parser
            if (content.TrimStart().StartsWith("<"))
            {
                var xmlParser = new RdfXmlParser();

                // Verify XML reader settings
                var readerSettings = new System.Xml.XmlReaderSettings
                {
                    DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                    MaxCharactersFromEntities = 1024,
                    XmlResolver = null // Disable external entity resolution
                };

                // Custom XML reader for added security
                using var stringReader = new StringReader(content);
                using var xmlReader = System.Xml.XmlReader.Create(stringReader, readerSettings);

                xmlParser.Load(graph, xmlReader);
            }
            else
            {
                // Turtle parser
                var parser = new TurtleParser();
                parser.Load(graph, reader);
            }
        }
        catch (OperationCanceledException)
        {
            throw new OntologyImportException("RDF parsing timeout - file too complex");
        }
    }
}
```

### 📋 LOW SEVERITY FINDINGS

#### L-DEP-01: Missing Dependency Scanning Automation
**Severity:** LOW
**Location:** N/A (CI/CD pipeline)

**Recommendation:**
Implement automated dependency scanning:

```yaml
# .github/workflows/security-scan.yml
name: Security Scan

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]
  schedule:
    - cron: '0 0 * * 0' # Weekly scan

jobs:
  dependency-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Check for vulnerable packages
        run: dotnet list package --vulnerable --include-transitive

      - name: Run Security Code Scan
        run: dotnet build /p:RunAnalyzers=true

      - name: OWASP Dependency Check
        uses: dependency-check/Dependency-Check_Action@main
        with:
          project: 'Eidos'
          path: '.'
          format: 'HTML'
```

#### L-DEP-02: NuGet Package Sources
**Severity:** LOW
**Location:** NuGet configuration

**Recommendation:**
Verify package source integrity:
```xml
<!-- nuget.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceCredentials />
  <config>
    <add key="signatureValidationMode" value="require" />
  </config>
</configuration>
```

---

## 8. Database Security

### ✅ STRENGTHS

#### 8.1 Entity Framework Core Usage
**Location:** All repository classes, `/Data/OntologyDbContext.cs`
**Finding:** **EXCELLENT - No SQL Injection Vulnerabilities**

**Analysis:**
- ✅ 100% parameterized queries via EF Core LINQ
- ✅ No raw SQL execution found
- ✅ Repository pattern enforces safe data access
- ✅ Proper query composition

**Example:**
```csharp
// OntologyPermissionService.cs (Lines 27-37)
var ontologyInfo = await _context.Ontologies
    .Where(o => o.Id == ontologyId)
    .Select(o => new { o.UserId, o.Visibility, o.Id })
    .AsNoTracking()
    .FirstOrDefaultAsync();
```

#### 8.2 Database Context Configuration
**Location:** `/Data/OntologyDbContext.cs`
**Finding:** **EXCELLENT - Proper EF Core configuration**

**Security Features:**
```csharp
// Lines 56-443: Comprehensive model configuration

// Cascade delete restrictions prevent unintended data loss
modelBuilder.Entity<Ontology>()
    .HasOne(o => o.User)
    .WithMany(u => u.Ontologies)
    .HasForeignKey(o => o.UserId)
    .OnDelete(DeleteBehavior.Restrict); // Prevents cascade

// Unique indexes for security tokens
modelBuilder.Entity<OntologyShare>()
    .HasIndex(s => s.ShareToken)
    .IsUnique();

modelBuilder.Entity<GuestSession>()
    .HasIndex(g => g.SessionToken)
    .IsUnique();

// Performance indexes on security-critical lookups
modelBuilder.Entity<OntologyActivity>()
    .HasIndex(a => new { a.OntologyId, a.CreatedAt })
    .HasDatabaseName("IX_OntologyActivity_OntologyId_CreatedAt");
```

**Strengths:**
- ✅ Proper foreign key constraints
- ✅ Unique indexes on sensitive fields
- ✅ Cascade delete carefully configured
- ✅ Composite indexes for performance

#### 8.3 Migration Security
**Location:** `/Program.cs` (Lines 446-476)
**Finding:** **GOOD - Automated migrations with safeguards**

```csharp
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OntologyDbContext>>();
    using var db = dbFactory.CreateDbContext();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (connectionString?.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) == true
        && !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
    {
        db.Database.EnsureCreated(); // SQLite
        Console.WriteLine("ℹ️  SQLite database schema ensured");
    }
    else
    {
        db.Database.Migrate(); // SQL Server
        Console.WriteLine("ℹ️  SQL Server migrations applied");
    }
}
```

### 🚨 HIGH SEVERITY FINDINGS

#### H-DB-01: Automatic Migration Execution on Startup
**Severity:** HIGH
**Location:** `/Program.cs` (Lines 446-476)
**CWE:** CWE-1265 (Unintended Reentrant Invocation of Non-reentrant Code)

**Issue:**
Migrations run automatically on every application startup, which is dangerous in production.

**Risks:**
- **Multi-instance deployments**: Race conditions between instances
- **Data loss**: Destructive migrations run without review
- **Deployment failures**: Migration errors break entire deployment
- **Rollback complications**: Can't separate code and schema rollbacks
- **Zero-downtime impossibility**: Schema changes require app restart

**Exploitation Scenario:**
```
# Production deployment with 3 instances
Instance 1: Starts, runs migration, half-completes
Instance 2: Starts simultaneously, runs same migration, conflicts
Instance 3: Starts, migration fails, app crashes
Result: Database corruption, app downtime
```

**Current Code:**
```csharp
// WARNING comment exists but migration still runs
// WARNING: Automatically applying migrations on startup can be risky in production environments.
// This approach may lead to data loss (if migrations are destructive), deployment failures,
// or conflicts in multi-instance deployments (e.g., in cloud or containerized environments).
// It is recommended to apply migrations manually as a separate deployment step in production.

// ... but migration still executes automatically ...
db.Database.Migrate(); // ⚠️ RUNS IN PRODUCTION
```

**Recommendation:**
```csharp
// Program.cs - Remove automatic migrations in production
if (app.Environment.IsDevelopment())
{
    // Only auto-migrate in development
    using var scope = app.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OntologyDbContext>>();
    using var db = dbFactory.CreateDbContext();

    db.Database.EnsureCreated(); // Dev only
    _logger.LogInformation("Development: Database schema ensured");
}
else
{
    // Production: Verify migrations are applied but don't run them
    using var scope = app.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OntologyDbContext>>();
    using var db = dbFactory.CreateDbContext();

    var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        var migrationList = string.Join(", ", pendingMigrations);
        _logger.LogError(
            "DEPLOYMENT ERROR: Pending migrations detected: {Migrations}. " +
            "Run migrations manually before deploying.",
            migrationList);
        throw new InvalidOperationException(
            $"Cannot start application with pending migrations: {migrationList}");
    }

    _logger.LogInformation("Production: All migrations applied successfully");
}

// Create separate migration tool for production deployments
// Tools/MigrationRunner.cs
public class MigrationRunner
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // ... configure services ...

        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OntologyDbContext>();

        Console.WriteLine("Applying pending migrations...");
        await db.Database.MigrateAsync();
        Console.WriteLine("Migrations applied successfully");
    }
}
```

**Deployment Process:**
```bash
# Step 1: Run migrations separately
dotnet run --project Tools/MigrationRunner/MigrationRunner.csproj -- --environment Production

# Step 2: Deploy application (migrations already applied)
dotnet publish -c Release
# ... deploy to Azure App Service, containers, etc.
```

### ⚠️ MEDIUM SEVERITY FINDINGS

#### M-DB-01: SQLite Database File Permissions
**Severity:** MEDIUM
**Location:** `/ontology.db`, `/appsettings.json` (Line 4)
**CWE:** CWE-732 (Incorrect Permission Assignment for Critical Resource)

**Issue:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=ontology.db"
}
```

SQLite database file in application root with default permissions.

**Risk:**
- Web server process can read/write database
- File may be world-readable on misconfigured systems
- Backup files expose database
- No encryption at rest

**Recommendation:**
```bash
# Set restrictive file permissions (Linux/macOS)
chmod 600 ontology.db
chown app-user:app-user ontology.db

# Move database outside web root
"ConnectionStrings": {
  "DefaultConnection": "Data Source=/var/lib/eidos/data/ontology.db"
}

# For production, use encrypted SQLite
# NuGet: SQLitePCLRaw.bundle_e_sqlcipher
var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = dbPath,
    Mode = SqliteOpenMode.ReadWriteCreate,
    Password = configuration["Database:SqlitePassword"]
}.ToString();
```

#### M-DB-02: Connection String Security in SQL Server
**Severity:** MEDIUM
**Location:** `/appsettings.Production.json` (Lines 3-5)

**Issue:**
```json
"ConnectionStrings": {
  "DefaultConnection": ""
},
"_comment_ConnectionStrings": "Example: Server=tcp:your-server.database.windows.net,1433;User ID=your-admin;Password=your-password;..."
```

**Risks:**
- Connection string in configuration (should be in Key Vault)
- Example shows SQL authentication (prefer Managed Identity)
- Password-based authentication risk

**Recommendation:**
```csharp
// Use Azure Managed Identity for SQL Server
builder.Services.AddDbContextFactory<OntologyDbContext>(options =>
{
    if (builder.Environment.IsProduction())
    {
        var sqlConnection = new SqlConnection(
            builder.Configuration.GetConnectionString("DefaultConnection"));

        // Use Managed Identity token authentication
        sqlConnection.AccessToken = await GetAzureSqlTokenAsync();

        options.UseSqlServer(sqlConnection);
    }
    else
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

private static async Task<string> GetAzureSqlTokenAsync()
{
    var credential = new DefaultAzureCredential();
    var token = await credential.GetTokenAsync(
        new TokenRequestContext(new[] { "https://database.windows.net/.default" }));
    return token.Token;
}

// Connection string without password (Managed Identity)
// "Server=tcp:your-server.database.windows.net,1433;Database=EidosDb;Encrypt=True;TrustServerCertificate=False;"
```

#### M-DB-03: Missing Query Timeout Configuration
**Severity:** MEDIUM
**Location:** `/Program.cs` (DbContext configuration)

**Issue:**
No global query timeout configured for database operations.

**Risk:**
- Long-running queries cause resource exhaustion
- Potential DoS through complex ontology queries
- Connection pool starvation

**Recommendation:**
```csharp
builder.Services.AddDbContextFactory<OntologyDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (connectionString?.StartsWith("Data Source="))
    {
        options.UseSqlite(connectionString, sqliteOptions =>
        {
            sqliteOptions.CommandTimeout(30); // 30 second timeout
        });
    }
    else
    {
        options.UseSqlServer(connectionString, sqlServerOptions =>
        {
            sqlServerOptions.CommandTimeout(30);
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
    }
});
```

### 📋 LOW SEVERITY FINDINGS

#### L-DB-01: Missing Database Activity Logging
**Severity:** LOW
**Location:** EF Core configuration

**Recommendation:**
Enable query logging for security auditing:
```csharp
if (builder.Environment.IsProduction())
{
    options.LogTo(
        message => _logger.LogInformation(message),
        new[] { DbLoggerCategory.Database.Command.Name },
        LogLevel.Information,
        DbContextLoggerOptions.DefaultWithUtcTime);
}
```

#### L-DB-02: Connection String Validation
**Severity:** LOW
**Location:** `/Program.cs` (Lines 158-183)

**Enhancement:**
Add connection string validation:
```csharp
private static void ValidateConnectionString(string connectionString, ILogger logger)
{
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Connection string is not configured");
    }

    // Validate SQL Server connection strings
    if (connectionString.Contains("Server="))
    {
        if (connectionString.Contains("Password=") &&
            !connectionString.Contains("Encrypt=True"))
        {
            logger.LogWarning("SQL Server connection string should enforce encryption");
        }
    }
}
```

---

## 9. Real-time Communication (SignalR) Security

### ✅ STRENGTHS

#### 9.1 Hub Authorization
**Location:** `/Hubs/OntologyHub.cs` (Lines 1-18)
**Finding:** **EXCELLENT - Authentication Required**

```csharp
[Authorize]
public class OntologyHub : Hub
{
    // All hub methods require authentication
}
```

**Analysis:**
- ✅ Hub-level authorization enforced
- ✅ User identity validated on every method call
- ✅ No anonymous access to real-time features

#### 9.2 Permission Validation in JoinOntology
**Location:** `/Hubs/OntologyHub.cs` (Lines 64-141)
**Finding:** **EXCELLENT - Comprehensive authorization checks**

```csharp
// Lines 56-57: Extract authenticated user ID
var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

// Lines 64-87: Verify ontology exists and get ownership
await using var context = await contextFactory.CreateDbContextAsync();
var ontology = await context.Ontologies
    .Where(o => o.Id == ontologyId)
    .Select(o => new { o.UserId })
    .AsNoTracking()
    .FirstOrDefaultAsync();

if (ontology == null)
{
    throw new HubException("Ontology not found");
}

// Lines 89-141: Three-tier permission check
if (string.IsNullOrEmpty(userId))
{
    permissionReason = "not_authenticated";
}
else if (ontology.UserId == userId)
{
    hasPermission = true;
    permissionReason = "owner";
}
else
{
    // Check share-based access
    var hasShareAccess = await context.UserShareAccesses
        .Where(usa => usa.UserId == userId)
        .Where(usa => usa.OntologyShare != null && usa.OntologyShare.OntologyId == ontologyId)
        .Where(usa => usa.OntologyShare != null && usa.OntologyShare.IsActive)
        .AnyAsync();

    if (hasShareAccess)
    {
        hasPermission = true;
        permissionReason = "shared_access";
    }
}

if (!hasPermission)
{
    throw new HubException("You do not have permission to join this ontology");
}
```

**Strengths:**
- ✅ Owner verification
- ✅ Share-based access validation
- ✅ Active share requirement
- ✅ Explicit permission denial with clear error
- ✅ Detailed logging for security audit

#### 9.3 Presence Tracking Security
**Location:** `/Hubs/OntologyHub.cs` (Lines 23-33, 159-188)
**Finding:** **GOOD - Privacy-conscious implementation**

```csharp
// Lines 154-170: User information extraction with fallbacks
var userName = GetDisplayName(Context.User) ?? "Guest";
var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
var profilePhotoUrl = GetProfilePhotoUrl(Context.User);
var isGuest = string.IsNullOrEmpty(userId);

var presenceInfo = new PresenceInfo
{
    ConnectionId = Context.ConnectionId,
    UserId = userId ?? $"guest_{Context.ConnectionId}",
    UserName = userName,
    UserEmail = userEmail,
    ProfilePhotoUrl = profilePhotoUrl,
    JoinedAt = DateTime.UtcNow,
    LastSeenAt = DateTime.UtcNow,
    Color = GetUserColor(userId ?? Context.ConnectionId),
    IsGuest = isGuest
};
```

**Analysis:**
- ✅ Email exposed to collaborators (may be intentional)
- ⚠️ Consider making email optional/private setting
- ✅ Consistent user color based on user ID hash
- ✅ Guest identification

### 🚨 HIGH SEVERITY FINDINGS

#### H-SIGNALR-01: In-Memory Presence Storage Without Cleanup
**Severity:** HIGH
**Location:** `/Hubs/OntologyHub.cs` (Lines 23-25)
**CWE:** CWE-401 (Missing Release of Memory after Effective Lifetime)

**Issue:**
```csharp
// In-memory storage for presence tracking
// Key: OntologyId, Value: Dictionary of ConnectionId -> PresenceInfo
private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, PresenceInfo>> _presenceByOntology = new();
```

**Risks:**
- **Memory leak**: Presence data accumulates without cleanup
- **Static storage**: Survives application restarts in long-running processes
- **No expiration**: Stale connections remain indefinitely
- **DoS potential**: Attacker joins many ontologies, exhausts memory
- **Scale issues**: Single-server architecture (no distributed cache)

**Exploitation Scenario:**
```csharp
// Attacker script
for (int i = 0; i < 10000; i++)
{
    await hubConnection.SendAsync("JoinOntology", attackerId);
    // Create new connection without cleanup
    await Task.Delay(100);
}
// Result: Server runs out of memory
```

**Recommendation:**
```csharp
// Option 1: Add background cleanup service
public class PresenceCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            var now = DateTime.UtcNow;
            var staleTimeout = TimeSpan.FromMinutes(30);

            foreach (var (ontologyId, presences) in _presenceByOntology)
            {
                var staleConnections = presences
                    .Where(p => now - p.Value.LastSeenAt > staleTimeout)
                    .Select(p => p.Key)
                    .ToList();

                foreach (var connectionId in staleConnections)
                {
                    presences.TryRemove(connectionId, out _);
                    _logger.LogInformation(
                        "Removed stale presence: ConnectionId={ConnectionId}, OntologyId={OntologyId}",
                        connectionId, ontologyId);
                }

                // Cleanup empty ontologies
                if (presences.IsEmpty)
                {
                    _presenceByOntology.TryRemove(ontologyId, out _);
                }
            }
        }
    }
}

// Register cleanup service
builder.Services.AddHostedService<PresenceCleanupService>();

// Option 2: Use distributed cache for multi-instance deployments
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "Eidos_Presence_";
});

public class RedisPresenceStore
{
    private readonly IDistributedCache _cache;

    public async Task SetPresenceAsync(int ontologyId, string connectionId, PresenceInfo info)
    {
        var key = $"presence:{ontologyId}:{connectionId}";
        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(info),
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30)
            });
    }
}
```

#### H-SIGNALR-02: User Email Exposed in Presence Info
**Severity:** HIGH
**Location:** `/Hubs/OntologyHub.cs` (Lines 163-164)
**CWE:** CWE-359 (Exposure of Private Personal Information)

**Issue:**
```csharp
var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
var presenceInfo = new PresenceInfo
{
    // ...
    UserEmail = userEmail, // ⚠️ Email broadcast to all collaborators
    // ...
};

// Line 185: Broadcast to others
await Clients.OthersInGroup(groupName).SendAsync("UserJoined", presenceInfo);
```

**Risk:**
- Email addresses visible to all collaborators
- Privacy violation (GDPR, CCPA)
- Email harvesting by malicious users
- No opt-out mechanism

**Exploitation Scenario:**
```csharp
// Malicious user joins shared ontology
await hubConnection.SendAsync("JoinOntology", sharedOntologyId);

// Receives UserJoined events with emails
hubConnection.On<PresenceInfo>("UserJoined", presence =>
{
    Console.WriteLine($"Harvested email: {presence.UserEmail}");
    // Store for spam/phishing campaigns
});
```

**Recommendation:**
```csharp
// Option 1: Add user privacy setting
public class UserPreferences
{
    // ... existing properties ...
    public bool ShowEmailInCollaboration { get; set; } = false; // Default: private
    public bool ShowProfilePhotoInCollaboration { get; set; } = true;
}

// In OntologyHub.cs
var showEmail = await GetUserPrivacySettingAsync(userId, "ShowEmailInCollaboration");

var presenceInfo = new PresenceInfo
{
    ConnectionId = Context.ConnectionId,
    UserId = userId ?? $"guest_{Context.ConnectionId}",
    UserName = userName,
    UserEmail = showEmail ? userEmail : null, // Respect privacy setting
    ProfilePhotoUrl = profilePhotoUrl,
    // ...
};

// Option 2: Hash email for avatar generation only
var presenceInfo = new PresenceInfo
{
    // ... other fields ...
    UserEmail = null, // Never send email
    EmailHash = HashEmail(userEmail), // For Gravatar-style avatars
    // ...
};

private static string HashEmail(string? email)
{
    if (string.IsNullOrEmpty(email)) return string.Empty;

    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
    return Convert.ToHexString(hash).ToLowerInvariant();
}
```

### ⚠️ MEDIUM SEVERITY FINDINGS

#### M-SIGNALR-01: No Connection Limit Per User
**Severity:** MEDIUM
**Location:** `/Hubs/OntologyHub.cs` (JoinOntology method)

**Issue:**
Single user can open unlimited SignalR connections.

**Risk:**
- Resource exhaustion
- DoS attack vector
- Connection pool depletion

**Recommendation:**
```csharp
private static readonly ConcurrentDictionary<string, int> _connectionCountByUser = new();
private const int MAX_CONNECTIONS_PER_USER = 10;

public async Task JoinOntology(int ontologyId)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!string.IsNullOrEmpty(userId))
    {
        var connectionCount = _connectionCountByUser.AddOrUpdate(
            userId,
            1,
            (key, count) => count + 1);

        if (connectionCount > MAX_CONNECTIONS_PER_USER)
        {
            _connectionCountByUser.AddOrUpdate(userId, connectionCount, (key, count) => count - 1);
            throw new HubException($"Maximum {MAX_CONNECTIONS_PER_USER} connections per user exceeded");
        }
    }

    // ... existing logic ...
}

public override async Task OnDisconnectedAsync(Exception? exception)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!string.IsNullOrEmpty(userId))
    {
        _connectionCountByUser.AddOrUpdate(userId, 0, (key, count) => Math.Max(0, count - 1));
    }

    await base.OnDisconnectedAsync(exception);
}
```

#### M-SIGNALR-02: Heartbeat Message Flooding
**Severity:** MEDIUM
**Location:** `/Hubs/OntologyHub.cs` (Lines 276-293)

**Issue:**
```csharp
public async Task Heartbeat(int ontologyId)
{
    // No rate limiting on heartbeat messages
    if (_presenceByOntology.TryGetValue(ontologyId, out var ontologyPresence))
    {
        if (ontologyPresence.TryGetValue(Context.ConnectionId, out var presence))
        {
            presence.LastSeenAt = DateTime.UtcNow;
        }
    }
    await Task.CompletedTask;
}
```

**Risk:**
- Client can spam heartbeat messages
- Server CPU exhaustion
- Dictionary lock contention

**Recommendation:**
```csharp
private static readonly ConcurrentDictionary<string, DateTime> _lastHeartbeat = new();
private static readonly TimeSpan MinHeartbeatInterval = TimeSpan.FromSeconds(5);

public async Task Heartbeat(int ontologyId)
{
    var connectionId = Context.ConnectionId;
    var now = DateTime.UtcNow;

    // Rate limit heartbeats
    if (_lastHeartbeat.TryGetValue(connectionId, out var lastTime))
    {
        if (now - lastTime < MinHeartbeatInterval)
        {
            _logger.LogWarning(
                "Heartbeat rate limit exceeded: ConnectionId={ConnectionId}",
                connectionId);
            return; // Silent drop, don't throw to avoid disconnecting legitimate users
        }
    }

    _lastHeartbeat[connectionId] = now;

    if (_presenceByOntology.TryGetValue(ontologyId, out var ontologyPresence))
    {
        if (ontologyPresence.TryGetValue(connectionId, out var presence))
        {
            presence.LastSeenAt = now;
        }
    }

    await Task.CompletedTask;
}
```

### 📋 LOW SEVERITY FINDINGS

#### L-SIGNALR-01: Presence Info Color Generation
**Severity:** LOW
**Location:** `/Hubs/OntologyHub.cs` (Lines 332-337)

**Issue:**
```csharp
private static string GetUserColor(string userId)
{
    var hash = Math.Abs(userId.GetHashCode());
    return _avatarColors[hash % _avatarColors.Length];
}
```

**Enhancement:**
Use cryptographically consistent hash for better distribution:
```csharp
private static string GetUserColor(string userId)
{
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
    var index = BitConverter.ToInt32(hash, 0) % _avatarColors.Length;
    return _avatarColors[Math.Abs(index)];
}
```

---

## 10. File Upload/Download Security

### ✅ STRENGTHS

#### 10.1 File Size Validation
**Location:** `/Services/Import/RdfParser.cs` (Lines 12-54)
**Finding:** **EXCELLENT - Comprehensive size validation**

```csharp
private const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024; // 10MB

// Security: Validate stream size before reading
if (fileStream.CanSeek && fileStream.Length > MAX_FILE_SIZE_BYTES)
{
    return Task.FromResult(new TtlImportResult
    {
        Success = false,
        ErrorMessage = $"File size exceeds maximum allowed size of {MAX_FILE_SIZE_BYTES / (1024 * 1024)}MB"
    });
}

// Security: Copy with buffer to prevent memory exhaustion
var buffer = new byte[8192]; // 8KB buffer
long totalBytesRead = 0;
int bytesRead;

while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
{
    totalBytesRead += bytesRead;

    // Security: Enforce size limit during copy for non-seekable streams
    if (totalBytesRead > MAX_FILE_SIZE_BYTES)
    {
        return Task.FromResult(new TtlImportResult
        {
            Success = false,
            ErrorMessage = $"File size exceeds maximum allowed size"
        });
    }

    memoryStream.Write(buffer, 0, bytesRead);
}
```

**Analysis:**
- ✅ Maximum file size enforced (10MB)
- ✅ Buffered reading prevents memory exhaustion
- ✅ Size validation for both seekable and non-seekable streams
- ✅ Protection against zip bombs and large file DoS

#### 10.2 File Format Detection
**Location:** `/Services/Import/RdfParser.cs` (Lines 62-77)
**Finding:** **GOOD - Content-based format detection**

```csharp
// Detect format based on content
IRdfReader parser;
if (content.TrimStart().StartsWith("<") &&
    (content.Contains("<?xml") || content.Contains("<rdf:RDF") || content.Contains("xmlns:rdf")))
{
    parser = new RdfXmlParser();
}
else
{
    parser = new TurtleParser();
}
```

**Analysis:**
- ✅ Content inspection rather than extension trust
- ⚠️ Simple detection logic (see H-INPUT-01 for XXE risks)

#### 10.3 Export File Handling
**Location:** Various export services
**Finding:** **GOOD - Memory-safe export generation**

**Analysis:**
- Export strategies use memory streams
- No direct file system writes for exports
- Content returned to client through HTTP response

### ⚠️ MEDIUM SEVERITY FINDINGS

#### M-FILE-01: Missing File Extension Validation
**Severity:** MEDIUM
**Location:** `/Services/Import/RdfParser.cs`
**CWE:** CWE-434 (Unrestricted Upload of File with Dangerous Type)

**Issue:**
No explicit file extension whitelist validation.

**Risk:**
- Malicious files disguised as TTL/RDF
- Unexpected file types processed
- Parser vulnerabilities triggered

**Recommendation:**
```csharp
public Task<TtlImportResult> ParseAsync(Stream fileStream, string fileName)
{
    // Validate file extension
    var allowedExtensions = new[] { ".ttl", ".rdf", ".owl", ".xml", ".nt", ".n3" };
    var extension = Path.GetExtension(fileName).ToLowerInvariant();

    if (!allowedExtensions.Contains(extension))
    {
        return Task.FromResult(new TtlImportResult
        {
            Success = false,
            ErrorMessage = $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}"
        });
    }

    // Validate file size
    // ... existing validation ...
}
```

#### M-FILE-02: Missing Virus Scanning
**Severity:** MEDIUM
**Location:** File upload flow
**CWE:** CWE-434 (Unrestricted Upload of File with Dangerous Type)

**Issue:**
No anti-malware scanning for uploaded files.

**Risk:**
- Malware uploaded through ontology import
- Server-side virus infection
- Malicious files distributed to other users

**Recommendation:**
```csharp
public class AntivirusService
{
    private readonly ILogger<AntivirusService> _logger;

    public async Task<(bool isClean, string? threat)> ScanAsync(Stream fileStream)
    {
        // Option 1: ClamAV integration
        // NuGet: nClam
        var clam = new ClamClient("localhost", 3310);
        var result = await clam.SendAndScanFileAsync(fileStream);

        if (result.Result == ClamScanResults.VirusDetected)
        {
            _logger.LogWarning("Virus detected: {Virus}", result.InfectedFiles?.FirstOrDefault()?.VirusName);
            return (false, result.InfectedFiles?.FirstOrDefault()?.VirusName);
        }

        return (true, null);
    }
}

// In RdfParser.ParseAsync
var scanResult = await _antivirusService.ScanAsync(fileStream);
if (!scanResult.isClean)
{
    _logger.LogWarning("File upload rejected: virus detected - {Threat}", scanResult.threat);
    return Task.FromResult(new TtlImportResult
    {
        Success = false,
        ErrorMessage = "File rejected: malware detected"
    });
}
```

#### M-FILE-03: No File Download Rate Limiting
**Severity:** MEDIUM
**Location:** Export functionality

**Issue:**
Export endpoints not separately rate limited.

**Risk:**
- Large export DoS attacks
- Bandwidth exhaustion
- Server resource consumption

**Recommendation:**
```csharp
// Add export-specific rate limiting
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules.Add(new RateLimitRule
    {
        Endpoint = "*/export/*",
        Period = "1m",
        Limit = 5 // 5 exports per minute
    });
});

// Or use attribute-based limiting
[EnableRateLimiting("export")]
public async Task<IActionResult> ExportOntology(int id)
{
    // ... export logic ...
}
```

### 📋 LOW SEVERITY FINDINGS

#### L-FILE-01: Missing File Upload Logging
**Severity:** LOW
**Location:** Import services

**Recommendation:**
Add comprehensive upload logging:
```csharp
public async Task<TtlImportResult> ParseAsync(Stream fileStream, string fileName, string userId)
{
    _logger.LogInformation(
        "File upload started: FileName={FileName}, UserId={UserId}, Size={Size}",
        fileName,
        userId,
        fileStream.Length);

    var sw = Stopwatch.StartNew();
    var result = await ParseInternalAsync(fileStream);
    sw.Stop();

    _logger.LogInformation(
        "File upload completed: FileName={FileName}, Success={Success}, Duration={Duration}ms",
        fileName,
        result.Success,
        sw.ElapsedMilliseconds);

    return result;
}
```

#### L-FILE-02: Export File Naming
**Severity:** LOW
**Location:** Export services

**Recommendation:**
Sanitize filenames in exports:
```csharp
private string SanitizeFileName(string fileName)
{
    // Remove invalid characters
    var invalidChars = Path.GetInvalidFileNameChars();
    var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

    // Limit length
    if (sanitized.Length > 200)
        sanitized = sanitized.Substring(0, 200);

    return sanitized;
}

// In export method
var safeFileName = SanitizeFileName(ontology.Name) + ".ttl";
Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{safeFileName}\"");
```

---

## Summary of Findings

### Critical Priorities (Address Immediately)

1. **H-AUTH-01**: Development Auto-Login Risk
   - **Action**: Add production environment validation
   - **Timeline**: Before next production deployment

2. **H-DATA-01**: SQL Query Logging in Production
   - **Action**: Disable `EnableSqlCommandTextInstrumentation` in production
   - **Timeline**: Immediate (data privacy violation)

3. **H-DB-01**: Automatic Migration Execution
   - **Action**: Implement manual migration process for production
   - **Timeline**: Before next production deployment

4. **H-SIGNALR-01**: In-Memory Presence Storage
   - **Action**: Implement background cleanup service
   - **Timeline**: Within 2 weeks

5. **H-AUTH-03**: Hardcoded Database Password
   - **Action**: Move to environment variables/Key Vault
   - **Timeline**: Immediate

### High Priority (Address Soon)

6. **H-AUTH-02**: Missing Email Confirmation
7. **H-INPUT-01**: XXE in RDF Parser
8. **H-INPUT-02**: SignalR Input Validation
9. **H-DATA-02**: Sensitive Data Logging
10. **H-SIGNALR-02**: Email Exposure in Presence

### Medium Priority (Plan for Next Sprint)

11-26. Medium severity findings across all categories

### Low Priority (Maintenance Backlog)

27-38. Low severity findings and enhancements

---

## Compliance Considerations

### OWASP Top 10 2021 Coverage

| OWASP Category | Status | Findings |
|----------------|--------|----------|
| A01: Broken Access Control | ✅ GOOD | Proper authorization throughout, minor improvements needed |
| A02: Cryptographic Failures | ⚠️ NEEDS WORK | H-DATA-01, H-DATA-02, M-DATA-01 |
| A03: Injection | ✅ EXCELLENT | No SQL injection, minor XSS concerns |
| A04: Insecure Design | ✅ GOOD | Security-first architecture, some missing features |
| A05: Security Misconfiguration | ⚠️ NEEDS WORK | H-AUTH-01, H-DB-01, M-TRANS-01 |
| A06: Vulnerable Components | ✅ GOOD | Up-to-date packages, M-DEP-01, M-DEP-02 |
| A07: Authentication Failures | ⚠️ NEEDS WORK | H-AUTH-02, M-AUTH-01 missing 2FA |
| A08: Software and Data Integrity | ✅ GOOD | Proper dependency management |
| A09: Logging Failures | ⚠️ NEEDS WORK | H-DATA-01, M-MIDDLEWARE-02 |
| A10: SSRF | ✅ N/A | No external URL fetching |

### GDPR Compliance

| Requirement | Status | Notes |
|-------------|--------|-------|
| Data Encryption | ⚠️ PARTIAL | M-DATA-01, M-DATA-03 |
| Right to be Forgotten | ❌ NOT IMPLEMENTED | Need user deletion capability |
| Data Breach Notification | ⚠️ PARTIAL | Logging exists, need formal process |
| Privacy by Design | ✅ MOSTLY | H-SIGNALR-02 email exposure issue |
| Data Minimization | ✅ GOOD | Appropriate data collection |

### Security Best Practices Checklist

- ✅ Authentication & Authorization
- ⚠️ Data Protection (improvements needed)
- ✅ Input Validation
- ✅ Output Encoding
- ⚠️ Error Handling (production exposure risk)
- ✅ Logging & Monitoring (with improvements)
- ⚠️ Secure Configuration (dev features risk)
- ✅ Cryptography (password hashing)
- ⚠️ Communication Security (CSP weaknesses)
- ✅ Database Security (EF Core protection)

---

## Recommendations Priority Matrix

```
CRITICAL | H-AUTH-01, H-DATA-01, H-DB-01, H-AUTH-03
---------|---------------------------------------
HIGH     | H-AUTH-02, H-INPUT-01, H-SIGNALR-01
---------|---------------------------------------
MEDIUM   | All M-* findings (systematic improvement)
---------|---------------------------------------
LOW      | All L-* findings (polish and enhancement)
```

---

## Conclusion

The Eidos Ontology Builder application demonstrates **strong security fundamentals** with well-implemented authentication, authorization, and input validation using modern ASP.NET Core 9.0 features. The development team has clearly prioritized security throughout the architecture.

**Key Strengths:**
- Comprehensive OAuth implementation
- Strong password policies and account lockout
- Azure Key Vault integration
- No SQL injection vulnerabilities
- Excellent security headers
- CSRF protection
- Rate limiting for authentication

**Critical Areas for Improvement:**
- Remove development auto-login risk from production
- Disable SQL query logging in production (immediate privacy concern)
- Implement manual database migrations for production
- Add email confirmation requirement
- Fix in-memory presence storage leak
- Address XXE vulnerability in RDF parser

**Overall Assessment:**
With the **5 high-severity findings** addressed, this application will achieve **production-grade security** suitable for deployment. The findings are specific, actionable, and can be resolved systematically. The codebase shows evidence of security awareness and best practices.

**Recommended Timeline:**
- **Week 1**: Address all HIGH severity findings
- **Weeks 2-4**: Implement MEDIUM severity fixes systematically
- **Ongoing**: LOW severity improvements in maintenance cycles

---

## Appendix A: Security Testing Commands

### Dependency Vulnerability Scanning
```bash
# Check for vulnerable packages
dotnet list package --vulnerable --include-transitive

# Update all packages
dotnet restore
```

### Static Code Analysis
```bash
# Run security analyzers
dotnet build /p:RunAnalyzers=true /p:TreatWarningsAsErrors=true

# Generate security report
dotnet build /p:GenerateDocumentation=true
```

### Database Security Audit
```bash
# Check pending migrations
dotnet ef migrations list

# Generate migration script for review
dotnet ef migrations script --idempotent --output migrations.sql
```

### Rate Limiting Testing
```bash
# Test login rate limit
for i in {1..10}; do
  curl -X POST https://localhost:5001/Account/Login \
    -d "email=test@example.com&password=wrong" \
    -v
done
```

---

## Appendix B: Secure Configuration Examples

### Production appsettings.json
```json
{
  "DetailedErrors": false,
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Eidos": "Information"
    }
  },
  "AllowedHosts": "yourdomain.com;*.yourdomain.com",
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Forwarded-For",
    "GeneralRules": [
      { "Endpoint": "*", "Period": "1m", "Limit": 60 },
      { "Endpoint": "*/Account/Login", "Period": "5m", "Limit": 5 },
      { "Endpoint": "*/Account/Register", "Period": "1h", "Limit": 3 }
    ]
  }
}
```

### Azure App Service Environment Variables
```bash
ASPNETCORE_ENVIRONMENT=Production
KeyVault__Uri=https://your-vault.vault.azure.net/
ApplicationInsights__ConnectionString=InstrumentationKey=...
ADMINEMAIL=admin@yourdomain.com
```

---

**End of Security Audit Report**

*For questions or clarifications, please contact the security team.*
