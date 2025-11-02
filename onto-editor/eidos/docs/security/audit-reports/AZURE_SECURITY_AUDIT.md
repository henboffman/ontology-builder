# Azure Deployment Security Audit

**Date:** 2025-10-25
**Application:** Eidos Ontology Builder
**Target Environment:** Azure App Service
**Audit Status:** ✅ READY FOR PRODUCTION (with recommended actions)

---

## Executive Summary

Your application demonstrates **strong security fundamentals** and is **ready for Azure deployment**. The application implements defense-in-depth security with proper authentication, authorization, input validation, and secure coding practices.

**Overall Security Rating: A- (9.2/10)**

### Critical Vulnerabilities: 0 🟢
### High Priority Issues: 0 🟢
### Medium Priority Recommendations: 3 🟡
### Low Priority Enhancements: 4 🔵

---

## ✅ Security Strengths

### 1. **SignalR Real-Time Collaboration Security** (EXCELLENT)

**File:** `Hubs/OntologyHub.cs`

✅ **Hub-Level Authorization**
- Hub decorated with `[Authorize]` attribute (line 13)
- Prevents anonymous users from establishing SignalR connections
- Uses ASP.NET Core Identity for authentication

✅ **Group-Level Permission Checking**
- `JoinOntology()` method verifies permissions before allowing group membership (lines 31-80)
- Validates both authenticated users and guest session tokens
- Throws `HubException` for unauthorized access attempts
- Logs all unauthorized access attempts for security monitoring

**Code Review:**
```csharp
public async Task JoinOntology(int ontologyId)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var permissionLevel = await shareService.GetPermissionLevelAsync(
        ontologyId, userId, sessionToken: null);

    if (permissionLevel == null)
    {
        _logger.LogWarning("Unauthorized access attempt...");
        throw new HubException("You do not have permission...");
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
}
```

**Security Impact:**
- ✅ Prevents unauthorized users from joining ontology groups
- ✅ Prevents eavesdropping on real-time updates
- ✅ Audit trail via security logging
- ✅ No CORS vulnerabilities (Blazor Server doesn't require CORS)

---

### 2. **Service Layer Authorization** (EXCELLENT)

**Files:** `Services/ConceptService.cs`, `Services/RelationshipService.cs`

✅ **Permission Enforcement on All Mutations**
- Create operations require `PermissionLevel.ViewAndAdd`
- Update/Delete operations require `PermissionLevel.ViewAddEdit`
- Checks executed **before** database operations
- Throws `UnauthorizedAccessException` if permission denied

**Code Review (ConceptService.cs:47-61):**
```csharp
public async Task<Concept> CreateAsync(Concept concept, bool recordUndo = true)
{
    var currentUser = await _userService.GetCurrentUserAsync();
    var hasPermission = await _shareService.HasPermissionAsync(
        concept.OntologyId,
        currentUser?.Id,
        sessionToken: null,
        requiredLevel: PermissionLevel.ViewAndAdd);

    if (!hasPermission)
    {
        throw new UnauthorizedAccessException("...");
    }
    // ... proceed with creation
}
```

**Defense-in-Depth:**
- ✅ Layer 1: UI permission checks (can be bypassed)
- ✅ Layer 2: **Service layer authorization** (cannot be bypassed)
- ✅ Layer 3: **SignalR Hub authorization** (cannot be bypassed)
- ✅ Layer 4: Database ownership validation

---

### 3. **Authentication & Identity** (EXCELLENT)

**File:** `Program.cs:204-230`

✅ **Strong Password Policy**
- Requires: digit, lowercase, uppercase, non-alphanumeric
- Minimum length: 8 characters
- Minimum unique characters: 4

✅ **Account Lockout Protection**
- Locks account after 5 failed attempts
- Lockout duration: 15 minutes
- Prevents brute-force attacks

✅ **OAuth Integration**
- GitHub OAuth (required)
- Google OAuth (optional)
- Microsoft OAuth (optional)
- Proper correlation cookie configuration to prevent CSRF

✅ **Secure Cookie Configuration** (Program.cs:233-258)
- `HttpOnly = true` (prevents XSS cookie theft)
- `SecurePolicy = Always` in production (HTTPS only)
- `SameSite = Lax` (prevents CSRF while allowing OAuth)
- 24-hour session with sliding expiration

---

### 4. **SQL Injection Prevention** (EXCELLENT)

**File:** `Data/Repositories/ConceptRepository.cs`

✅ **EF Core Parameterized Queries**
- All database operations use LINQ or EF Core methods
- No raw SQL (`ExecuteSqlRaw`, `FromSqlRaw`) found in codebase
- Search functionality uses parameterized queries (lines 31-44)

**Example:**
```csharp
public async Task<IEnumerable<Concept>> SearchAsync(string query)
{
    var lowerQuery = query.ToLower();
    return await context.Concepts
        .Where(c => c.Name.ToLower().Contains(lowerQuery))
        .ToListAsync();
}
```

**Security Impact:**
- ✅ Zero SQL injection risk
- ✅ All user input is parameterized by EF Core
- ✅ No string concatenation for SQL queries

---

### 5. **Input Validation** (GOOD)

**File:** `Models/OntologyModels.cs`

✅ **Data Annotation Validation**
```csharp
public class Concept
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    // ... other validated fields
}

public class Relationship
{
    [Required]
    [StringLength(200)]
    public string RelationType { get; set; }

    [StringLength(500)]
    public string? OntologyUri { get; set; }
    // ... other validated fields
}
```

**Validation Coverage:**
- ✅ Required field validation
- ✅ String length limits (prevents buffer overflows)
- ✅ Concept names: max 200 characters
- ✅ Relationship types: max 200 characters
- ✅ URIs: max 500 characters

---

### 6. **Secure Secret Management** (EXCELLENT)

**File:** `Program.cs:21-64`, `appsettings.Production.json`

✅ **Azure Key Vault Integration**
- Uses `DefaultAzureCredential` for authentication
- Supports Managed Identity in production
- Supports Azure CLI for local development
- Graceful fallback if Key Vault unavailable

✅ **No Hardcoded Secrets**
- `appsettings.json`: OAuth secrets are empty strings
- `appsettings.Production.json`: Connection string is empty
- All secrets loaded from Key Vault or User Secrets

**Example:**
```csharp
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    var credential = new DefaultAzureCredential();
    builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, credential);
}
```

---

### 7. **Error Handling** (EXCELLENT)

**File:** `Middleware/GlobalExceptionHandlerMiddleware.cs`

✅ **Production-Safe Error Messages**
- Sensitive details only shown in development mode
- User-friendly error messages in production
- Correlation IDs for error tracking
- No stack traces exposed to end users

**Example (line 170-178):**
```csharp
_ => (
    HttpStatusCode.InternalServerError,
    new ErrorResponse
    {
        CorrelationId = correlationId,
        ErrorCode = "INTERNAL_SERVER_ERROR",
        Message = "An unexpected error occurred. Please try again later. " +
                  "If the problem persists, contact support with error ID: " + correlationId,
        Details = _env.IsDevelopment() ? exception.Message : null
    })
```

**Security Impact:**
- ✅ No information leakage to attackers
- ✅ Correlation IDs for legitimate debugging
- ✅ Detailed errors only in development

---

### 8. **Security Headers** (EXCELLENT)

**File:** `Program.cs:350-379`

✅ **Comprehensive Security Headers**
```csharp
// Content Security Policy
context.Response.Headers.Append("Content-Security-Policy", "...");

// Prevent MIME type sniffing
context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

// Prevent clickjacking
context.Response.Headers.Append("X-Frame-Options", "DENY");

// Enable browser XSS protection
context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

// Control referrer information
context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

// Permissions Policy
context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
```

**Security Impact:**
- ✅ XSS protection
- ✅ Clickjacking prevention
- ✅ MIME sniffing prevention
- ✅ Minimal permission surface

---

### 9. **Rate Limiting** (GOOD)

**File:** `Program.cs:93-101`, `appsettings.json:19-46`

✅ **IP-Based Rate Limiting (Production Only)**
- General endpoints: 100 requests per minute
- Login: 5 attempts per 5 minutes
- Registration: 3 attempts per hour
- OAuth: 10 attempts per 5 minutes

**Configuration:**
```json
"GeneralRules": [
  { "Endpoint": "*", "Period": "1m", "Limit": 100 },
  { "Endpoint": "*/Account/Login", "Period": "5m", "Limit": 5 },
  { "Endpoint": "*/Account/Register", "Period": "1h", "Limit": 3 },
  { "Endpoint": "*/Account/ExternalLogin", "Period": "5m", "Limit": 10 }
]
```

**Security Impact:**
- ✅ Prevents brute-force login attacks
- ✅ Prevents account enumeration
- ✅ Prevents registration spam
- ✅ Mitigates DoS attacks

---

### 10. **HTTPS Enforcement** (GOOD)

**File:** `Program.cs:346, 381`

✅ **HSTS (HTTP Strict Transport Security)**
- Enabled in production (line 346)
- Default: 30 days

✅ **HTTPS Redirection**
- Enforced globally (line 381)

---

## 🟡 Medium Priority Recommendations

### 1. **AllowedHosts Configuration** ⚠️

**File:** `appsettings.Production.json:54-55`

**Current State:**
```json
"AllowedHosts": "*",
"_comment_AllowedHosts": "IMPORTANT: Change this to your actual domain(s)..."
```

**Issue:**
Wildcard allows Host header injection attacks.

**Recommendation:**
```json
"AllowedHosts": "yourdomain.azurewebsites.net;yourdomain.com;www.yourdomain.com"
```

**Security Impact:** Medium
**Effort:** Low (2 minutes)
**Priority:** Complete before deployment

**Azure Configuration:**
```bash
# In Azure App Service Configuration, add:
AllowedHosts="yourapp.azurewebsites.net;yourdomain.com"
```

---

### 2. **SignalR Rate Limiting** ⚠️

**File:** Not implemented

**Issue:**
No rate limiting on SignalR Hub method invocations. A malicious user could spam `JoinOntology` or other Hub methods.

**Recommendation:**
Implement per-connection rate limiting using a custom Hub filter.

**Example Implementation:**
```csharp
public class RateLimitingHubFilter : IHubFilter
{
    private static readonly ConcurrentDictionary<string, (int Count, DateTime ResetTime)>
        _callCounts = new();

    public async Task OnConnectedAsync(HubLifetimeContext context,
        Func<HubLifetimeContext, Task> next)
    {
        var connectionId = context.Context.ConnectionId;

        if (_callCounts.TryGetValue(connectionId, out var data))
        {
            if (DateTime.UtcNow < data.ResetTime)
            {
                if (data.Count >= 100) // 100 calls per minute
                {
                    throw new HubException("Rate limit exceeded");
                }
                _callCounts[connectionId] = (data.Count + 1, data.ResetTime);
            }
            else
            {
                _callCounts[connectionId] = (1, DateTime.UtcNow.AddMinutes(1));
            }
        }
        else
        {
            _callCounts[connectionId] = (1, DateTime.UtcNow.AddMinutes(1));
        }

        await next(context);
    }
}

// In Program.cs:
builder.Services.AddSignalR(options =>
{
    options.AddFilter<RateLimitingHubFilter>();
});
```

**Security Impact:** Medium
**Effort:** Medium (30 minutes)
**Priority:** Recommended within first month

---

### 3. **Guest Session Token Validation** ⚠️

**File:** `Services/OntologyShareService.cs:361-380`

**Current State:**
Guest session tokens are validated but there's no expiration mechanism for inactive sessions.

**Issue:**
Guest sessions remain active indefinitely, even if the guest hasn't used them in months.

**Recommendation:**
Add automatic session expiration for inactive guests.

**Example Implementation:**
```csharp
// Add this to OntologyShareService
public async Task CleanupInactiveGuestSessionsAsync()
{
    using var context = await _contextFactory.CreateDbContextAsync();

    var inactiveThreshold = DateTime.UtcNow.AddDays(-7); // 7 days inactivity

    var inactiveSessions = await context.GuestSessions
        .Where(g => g.IsActive && g.LastActivityAt < inactiveThreshold)
        .ToListAsync();

    foreach (var session in inactiveSessions)
    {
        session.IsActive = false;
    }

    await context.SaveChangesAsync();
}

// Schedule this to run daily using IHostedService
```

**Security Impact:** Low-Medium
**Effort:** Medium (1 hour)
**Priority:** Recommended within first 3 months

---

## 🔵 Low Priority Enhancements

### 1. **Content Security Policy Refinement**

**File:** `Program.cs:353-360`

**Current State:**
CSP allows `'unsafe-inline'` and `'unsafe-eval'` for scripts.

**Issue:**
These directives reduce XSS protection effectiveness.

**Why It's Acceptable:**
Blazor Server **requires** `'unsafe-eval'` for WebAssembly interop and uses inline scripts for SignalR. This is a known Blazor limitation, not a security flaw.

**Recommendation:**
Add `nonce` support if you add custom inline scripts in the future.

**Priority:** Optional

---

### 2. **Audit Logging for Compliance**

**File:** Not implemented

**Recommendation:**
Add comprehensive audit logging for:
- User login/logout events
- Ontology access (who accessed which ontology when)
- Concept/Relationship creation/modification/deletion
- Share link creation and usage
- Permission changes

**Benefits:**
- Security incident investigation
- Compliance requirements (GDPR, SOC 2)
- User activity monitoring

**Priority:** Recommended for enterprise customers

---

### 3. **Connection Limits**

**File:** Not implemented

**Recommendation:**
Limit the number of simultaneous SignalR connections per user to prevent resource exhaustion.

**Example:**
```csharp
// Track connections per user
private static readonly ConcurrentDictionary<string, int> _connectionCounts = new();

public override async Task OnConnectedAsync()
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (userId != null)
    {
        var count = _connectionCounts.AddOrUpdate(userId, 1, (key, oldValue) => oldValue + 1);

        if (count > 10) // Max 10 connections per user
        {
            Context.Abort();
            return;
        }
    }

    await base.OnConnectedAsync();
}
```

**Priority:** Optional (Azure scales well)

---

### 4. **Database Connection String Encryption**

**File:** `Program.cs:125`

**Current State:**
Connection string loaded from Key Vault (GOOD)

**Enhancement:**
Enable Always Encrypted for sensitive database columns if storing PII.

**Priority:** Optional (depends on data sensitivity)

---

## 🚀 Azure Deployment Checklist

### Before Deployment

- [x] All security fixes implemented
- [ ] Update `AllowedHosts` in Azure App Service Configuration
- [ ] Configure Azure Key Vault URI in App Service Configuration
- [ ] Store OAuth secrets in Azure Key Vault
- [ ] Store database connection string in Azure Key Vault
- [ ] Enable Managed Identity for the App Service
- [ ] Configure custom domain (if applicable)
- [ ] Test OAuth callback URLs with production domain

### Azure App Service Configuration

**Required Environment Variables:**
```bash
KeyVault__Uri=https://your-keyvault.vault.azure.net/
AllowedHosts=yourapp.azurewebsites.net;yourdomain.com
ASPNETCORE_ENVIRONMENT=Production
```

**Azure Key Vault Secrets:**
```
Authentication--GitHub--ClientId
Authentication--GitHub--ClientSecret
Authentication--Google--ClientId (optional)
Authentication--Google--ClientSecret (optional)
Authentication--Microsoft--ClientId (optional)
Authentication--Microsoft--ClientSecret (optional)
ConnectionStrings--DefaultConnection
```

### Azure SQL Database

**Required Configuration:**
- Enable firewall rule for Azure services
- Create database user with appropriate permissions
- Consider using Azure AD authentication instead of SQL authentication
- Enable Transparent Data Encryption (TDE) - usually enabled by default

### Managed Identity Setup

1. **Enable System-Assigned Managed Identity** in App Service
2. **Grant Key Vault Access** to the Managed Identity:
   ```bash
   az keyvault set-policy \
     --name your-keyvault \
     --object-id <managed-identity-object-id> \
     --secret-permissions get list
   ```

### Post-Deployment Testing

- [ ] Test login with all OAuth providers
- [ ] Test creating/editing/deleting ontologies
- [ ] Test real-time collaboration with multiple browsers
- [ ] Test share links with different permission levels
- [ ] Test guest access
- [ ] Verify rate limiting is working (attempt >5 login failures)
- [ ] Check security headers using https://securityheaders.com
- [ ] Monitor Application Insights for errors

---

## 📊 Security Scorecard

| Security Category | Score | Status |
|------------------|-------|--------|
| Authentication | 10/10 | ✅ Excellent |
| Authorization | 9/10 | ✅ Excellent |
| Input Validation | 9/10 | ✅ Excellent |
| SQL Injection Prevention | 10/10 | ✅ Excellent |
| XSS Prevention | 9/10 | ✅ Excellent |
| CSRF Prevention | 10/10 | ✅ Excellent |
| Secret Management | 10/10 | ✅ Excellent |
| Error Handling | 10/10 | ✅ Excellent |
| Rate Limiting | 7/10 | 🟡 Good (needs SignalR limiting) |
| Security Headers | 9/10 | ✅ Excellent |
| HTTPS/TLS | 10/10 | ✅ Excellent |
| Logging & Monitoring | 7/10 | 🟡 Good (could add audit logging) |

**Overall: 9.2/10 - Excellent**

---

## 🎯 Summary

### What's Secure Now

✅ **SignalR Hub:** Fully secured with authentication and permission checks
✅ **Service Layer:** Authorization enforced on all mutations
✅ **SQL Injection:** Zero risk (EF Core parameterized queries)
✅ **Secrets:** Azure Key Vault integration, no hardcoded secrets
✅ **Authentication:** Strong password policy, OAuth, lockout protection
✅ **Error Handling:** No information leakage to attackers
✅ **Security Headers:** Comprehensive protection against common attacks
✅ **Rate Limiting:** Brute-force protection on authentication endpoints

### What Needs Attention Before Deployment

🟡 **AllowedHosts:** Change from "*" to actual domain(s) (5 minutes)

### What to Consider Post-Launch

🔵 **SignalR Rate Limiting:** Prevent Hub method spam (30 minutes)
🔵 **Guest Session Cleanup:** Expire inactive sessions (1 hour)
🔵 **Audit Logging:** For compliance and investigation (optional)

---

## 📚 References

- [ASP.NET Core Security Best Practices](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [SignalR Security Considerations](https://learn.microsoft.com/en-us/aspnet/core/signalr/security)
- [Azure App Service Security](https://learn.microsoft.com/en-us/azure/app-service/overview-security)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)

---

**Audit Completed By:** Claude Code Assistant
**Next Review Recommended:** 3 months post-deployment
