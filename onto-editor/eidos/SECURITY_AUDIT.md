# Security Audit Report - Real-Time Collaboration

## Executive Summary

This audit was performed on the SignalR-based real-time collaboration system. While the application has a **permission system in place for the UI**, there are **CRITICAL server-side vulnerabilities** that must be addressed before production deployment.

---

## ✅ Code Cleanup Status

### Remaining Debug Code
**Location:** `wwwroot/js/ontologyHub.js` (lines 23, 29, 35, 40, 45, 49, 67)

**Status:** Low priority - these `console.log()` statements are useful for debugging and don't expose sensitive data.

**Recommendation:**
- **Keep for development** - helpful for debugging connection issues
- **Optional for production** - can be removed or wrapped in `if (DEBUG_MODE)` checks

```javascript
// Current logging (safe to keep):
console.log("SignalR connected");
console.log("Joined ontology group:", ontologyId);
console.log("ConceptChanged event received:", changeEvent);
```

---

## 🔴 CRITICAL Security Issues

### 1. **No Authorization in SignalR Hub Methods**

**Severity:** 🔴 CRITICAL
**Location:** `Hubs/OntologyHub.cs:24-36` (JoinOntology method)

**Issue:**
```csharp
public async Task JoinOntology(int ontologyId)
{
    var groupName = GetOntologyGroupName(ontologyId);
    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    // ❌ NO PERMISSION CHECK!
}
```

**Vulnerability:**
- Any authenticated user can join ANY ontology group by calling `JoinOntology(X)` with any ontology ID
- Once in the group, they receive real-time updates containing full concept data
- This bypasses the UI permission checks completely

**Attack Vector:**
```javascript
// Malicious user opens browser console and executes:
connection.invoke("JoinOntology", 999); // Join someone else's private ontology
// Now receives all real-time updates for ontology 999
```

**Fix Required:**
```csharp
public async Task JoinOntology(int ontologyId)
{
    // Get current user ID from Context
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    // Verify user has permission to access this ontology
    var permissionLevel = await _shareService.GetPermissionLevelAsync(
        ontologyId,
        userId,
        sessionToken: null);

    if (permissionLevel == null)
    {
        _logger.LogWarning(
            "User {UserId} attempted to join ontology {OntologyId} without permission",
            userId,
            ontologyId);
        throw new HubException("You do not have permission to access this ontology");
    }

    var groupName = GetOntologyGroupName(ontologyId);
    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

    _logger.LogInformation(
        "User {UserId} joined ontology {OntologyId} with {PermissionLevel}",
        userId,
        ontologyId,
        permissionLevel);
}
```

---

### 2. **No Permission Validation in Service Layer**

**Severity:** 🔴 CRITICAL
**Location:** `Services/ConceptService.cs:39-56`, `Services/RelationshipService.cs`

**Issue:**
```csharp
public async Task<Concept> CreateAsync(Concept concept, bool recordUndo = true)
{
    // ❌ NO CHECK if current user has permission to modify this ontology!
    await _conceptRepository.AddAsync(concept);
    await BroadcastConceptChange(concept.OntologyId, ChangeType.Added, concept);
    return concept;
}
```

**Vulnerability:**
- Services don't verify the current user has permission to create/update/delete
- Permission checks only exist in the UI layer (OntologyView.razor)
- A malicious user can bypass UI and call service methods directly via API endpoints

**Fix Required:**
Add an authorization service and inject current user context:

```csharp
public async Task<Concept> CreateAsync(Concept concept, bool recordUndo = true)
{
    // Verify user has permission to add concepts
    var hasPermission = await _shareService.HasPermissionAsync(
        concept.OntologyId,
        _userService.GetCurrentUserId(),
        PermissionLevel.ViewAndAdd);

    if (!hasPermission)
    {
        throw new UnauthorizedAccessException(
            "You do not have permission to add concepts to this ontology");
    }

    // ... rest of method
}
```

---

### 3. **Data Leakage via Broadcasts**

**Severity:** 🟠 HIGH
**Location:** `Services/ConceptService.cs:119-131`

**Issue:**
```csharp
private async Task BroadcastConceptChange(
    int ontologyId,
    ChangeType changeType,
    Concept? concept,
    int? deletedConceptId = null)
{
    var groupName = $"ontology_{ontologyId}";
    var changeEvent = new ConceptChangedEvent
    {
        Concept = concept, // ❌ Sends FULL concept data to all group members
    };

    await _hubContext.Clients.Group(groupName).SendAsync("ConceptChanged", changeEvent);
}
```

**Vulnerability:**
- Full concept data (including private notes, examples, definitions) is sent to ALL clients in the group
- If Issue #1 is exploited, unauthorized users receive complete sensitive data

**Current Mitigation:**
- Issue #1 makes this more severe, but if #1 is fixed, only authorized users receive the data

**Best Practice Recommendation:**
- Consider sending minimal data (just the concept ID and change type)
- Let clients reload data from server (which enforces permissions)

**Alternative Fix:**
```csharp
var changeEvent = new ConceptChangedEvent
{
    ChangeType = changeType,
    OntologyId = ontologyId,
    ConceptId = concept?.Id ?? deletedConceptId,
    // Don't send full concept data - clients will reload
};
```

However, your current implementation of reloading on the client side already does this:
```csharp
// In OntologyView.razor:1454
var freshOntology = await OntologyService.GetOntologyAsync(Id);
```

So this is actually **already secure** as long as `GetOntologyAsync` enforces permissions (needs verification).

---

## 🟡 MEDIUM Security Issues

### 4. **No Session Token Support in SignalR Hub**

**Severity:** 🟡 MEDIUM
**Location:** `Hubs/OntologyHub.cs`

**Issue:**
- Guest users with session tokens can't be validated in the Hub
- Only authenticated users can use SignalR currently
- Guest users viewing shared ontologies won't get real-time updates

**Fix Required:**
Support session tokens in SignalR connection:

```csharp
public async Task JoinOntology(int ontologyId, string? guestSessionToken = null)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    var permissionLevel = await _shareService.GetPermissionLevelAsync(
        ontologyId,
        userId,
        guestSessionToken);

    // ... rest of method
}
```

---

### 5. **No Rate Limiting**

**Severity:** 🟡 MEDIUM
**Location:** SignalR Hub and Services

**Issue:**
- No rate limiting on SignalR method calls
- Malicious user could spam `JoinOntology()` or trigger excessive broadcasts

**Fix Required:**
Implement rate limiting using middleware or a rate limiting library.

---

## ✅ Good Security Practices Already In Place

### 1. **Hub-Level Authorization**
```csharp
[Authorize]  // ✅ Good - requires authentication
public class OntologyHub : Hub
```

### 2. **Permission Enum System**
```csharp
public enum PermissionLevel
{
    View = 1,
    ViewAndAdd = 2,
    ViewAddEdit = 3,
    FullAccess = 4
}
```

### 3. **UI-Level Permission Checks**
All UI operations check `CanAdd()`, `CanEdit()`, `CanFullAccess()` before executing.

### 4. **Cryptographically Secure Share Tokens**
```csharp
// In OntologyShareService.cs:
var tokenBytes = new byte[32];
RandomNumberGenerator.Fill(tokenBytes);
var shareToken = Convert.ToBase64String(tokenBytes);
```

### 5. **Owner Permission Model**
Owners always have full access regardless of share links.

---

## 📋 SignalR Best Practices Review

### ✅ What You're Doing Right

1. **Using Groups for Multi-Tenant Isolation**
   ```csharp
   private static string GetOntologyGroupName(int ontologyId) => $"ontology_{ontologyId}";
   ```

2. **Automatic Reconnection**
   ```javascript
   new signalR.HubConnectionBuilder()
       .withAutomaticReconnect() // ✅ Good
   ```

3. **Logging Connection Events**
   ```csharp
   _logger.LogInformation("User {ConnectionId} joined ontology {OntologyId}", ...);
   ```

4. **Clean Disconnection Handling**
   ```javascript
   public async ValueTask DisposeAsync()
   {
       await JS.InvokeVoidAsync("ontologyHub.disconnect", ontology.Id);
   }
   ```

### ❌ What Needs Improvement

1. **Add Authorization to All Hub Methods** (Critical #1 above)
2. **Implement Server-Side Permission Validation** (Critical #2 above)
3. **Add Connection Tracking** - Track which users are viewing which ontologies
4. **Add Error Handling in Hub Methods** - Currently no try/catch blocks
5. **Implement Connection Limits** - Prevent one user from opening too many connections

---

## 🔒 Recommended Implementation Priority

### Phase 1: Critical Fixes (DO BEFORE PRODUCTION)

1. **Add authorization to `JoinOntology()`** - Without this, any user can spy on any ontology
2. **Add permission checks to all service methods** - Prevents API bypass
3. **Verify `GetOntologyAsync` enforces permissions** - Currently unclear if it does

### Phase 2: High Priority

4. **Test guest session token flow with SignalR**
5. **Add comprehensive error handling to Hub methods**
6. **Add logging for permission denial attempts**

### Phase 3: Medium Priority

7. **Implement rate limiting**
8. **Add connection count limits per user**
9. **Consider minimal broadcast payloads** (optional - current reload pattern is secure)

---

## 🧪 Security Testing Recommendations

### Test 1: Unauthorized Group Join
```javascript
// In browser console, try to join another user's ontology:
connection.invoke("JoinOntology", 999);
// Expected: Should throw "Permission denied" error
// Current: Succeeds (VULNERABILITY)
```

### Test 2: Direct Service Call Bypass
Try calling ConceptService.CreateAsync directly via API without UI permission checks.

### Test 3: Guest Session Token
Test if guest users with valid session tokens can receive real-time updates.

---

## 📝 Code Changes Needed

### File 1: `Hubs/OntologyHub.cs`

**Add dependency injection:**
```csharp
private readonly IOntologyShareService _shareService;

public OntologyHub(
    ILogger<OntologyHub> logger,
    IOntologyShareService shareService)
{
    _logger = logger;
    _shareService = shareService;
}
```

**Add authorization to JoinOntology:**
```csharp
public async Task JoinOntology(int ontologyId, string? guestSessionToken = null)
{
    try
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var permissionLevel = await _shareService.GetPermissionLevelAsync(
            ontologyId,
            userId,
            guestSessionToken);

        if (permissionLevel == null)
        {
            throw new HubException("You do not have permission to access this ontology");
        }

        var groupName = GetOntologyGroupName(ontologyId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} joined ontology {OntologyId} with permission {PermissionLevel}",
            userId ?? "Guest",
            ontologyId,
            permissionLevel);

        await Clients.OthersInGroup(groupName).SendAsync("UserJoined", Context.ConnectionId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error joining ontology {OntologyId}", ontologyId);
        throw;
    }
}
```

### File 2: `Services/ConceptService.cs`

**Add authorization to Create/Update/Delete:**
```csharp
public async Task<Concept> CreateAsync(Concept concept, bool recordUndo = true)
{
    var currentUserId = _userService.GetCurrentUserId();

    var hasPermission = await _shareService.HasPermissionAsync(
        concept.OntologyId,
        currentUserId,
        sessionToken: null,
        requiredLevel: PermissionLevel.ViewAndAdd);

    if (!hasPermission)
    {
        throw new UnauthorizedAccessException(
            $"User {currentUserId} does not have permission to add concepts");
    }

    // ... rest of method unchanged
}
```

---

## Summary

**Good News:** You have a well-designed permission system and UI-level security.

**Bad News:** The server-side enforcement is missing, creating critical vulnerabilities.

**Action Required:** Implement server-side authorization checks in the SignalR Hub and service layer before deploying to production.

**Estimated Effort:** 4-6 hours to implement all critical fixes and test thoroughly.
