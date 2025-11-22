# Dev User Switching Enhancement

**Date**: November 2, 2025
**Feature**: Development User Switching Improvements
**Status**: In Progress

## Overview

Enhancement to the existing dev user switching functionality to make it more accessible and functional in development mode.

## Problem Statement

The current dev user switching feature has several issues:

1. **Endpoint Not Registered**: The `MapDevSwitchUserEndpoint` method is defined but never called in `Program.cs`, making the endpoint inaccessible
2. **Broken Page Links**: The `/dev/switch-user` page uses `<a href>` links that navigate to `/dev/api/switch-user/{email}` causing 404 errors instead of properly calling the API endpoint
3. **Poor Accessibility**: The switch user page is only accessible via direct URL navigation (`/dev/switch-user`)
4. **No Login Page Integration**: Developers must manually navigate to switch users, which isn't discoverable

## Requirements

### Must Have

1. Register the `DevSwitchUserEndpoint` in `Program.cs` (only in Development environment)
2. Fix the `/dev/switch-user` page to properly call the API endpoint
3. Add "Switch User" link to the TopBar user dropdown menu (only visible in Development)
4. Ensure all functionality is restricted to Development environment only

### Nice to Have

1. Add dev user switching widget to the Login page (only in Development)
2. Show current user prominently on the switch user page
3. Add visual indicators that these are dev-only features

## Technical Approach

### 1. Register Endpoint in Program.cs

Add conditional endpoint registration:

```csharp
// Development-only endpoints
if (app.Environment.IsDevelopment())
{
    app.MapDevSwitchUserEndpoint();
}
```

### 2. Fix DevSwitchUser.razor Page

Replace `<a href>` links with proper form posts or JavaScript that calls the endpoint and handles the redirect:

**Option A**: Use forms with POST

```razor
<form method="post" action="/dev/api/switch-user/dev@localhost.local">
    <button type="submit" class="btn btn-lg btn-primary">...</button>
</form>
```

**Option B**: Use NavigationManager to navigate (simpler, works with GET)

```csharp
@inject NavigationManager Navigation

private void SwitchToUser(string email)
{
    Navigation.NavigateTo($"dev/api/switch-user/{email}", forceLoad: true);
}
```

We'll use Option B since the endpoint is already set up as a GET endpoint.

### 3. Add to TopBar User Dropdown

Inject `IWebHostEnvironment` and conditionally show the link:

```razor
@inject IWebHostEnvironment Environment

<!-- In user dropdown menu -->
@if (Environment.IsDevelopment())
{
    <li><hr class="dropdown-divider"></li>
    <li>
        <a class="dropdown-item text-warning" href="/dev/switch-user">
            <i class="bi bi-bug-fill"></i> Switch User (Dev)
        </a>
    </li>
}
```

### 4. Add to Login Page (Optional)

Add a dev-only card below the main login card when in Development mode.

## Files to Modify

1. **Program.cs** - Register the endpoint
2. **Components/Pages/DevSwitchUser.razor** - Fix button functionality
3. **Components/Layout/TopBar.razor** - Add dropdown menu item
4. **Pages/Account/Login.cshtml** (Optional) - Add dev user switching widget

## Security Considerations

- All dev switching functionality is gated by `IWebHostEnvironment.IsDevelopment()`
- Endpoint includes environment check and returns 403 if not in Development
- No security risk as this is development-only and already properly gated

## Testing Plan

1. Verify endpoint is registered and accessible in Development
2. Test each button on `/dev/switch-user` page switches users correctly
3. Verify "Switch User" link appears in TopBar dropdown in Development only
4. Verify "Switch User" link does NOT appear in Production
5. Test that endpoint returns 403 in non-Development environments
6. Verify user switching redirects to home page after successful switch
7. Verify current user is updated after switch

## Implementation Steps

1. Register endpoint in Program.cs
2. Update DevSwitchUser.razor to use NavigationManager
3. Update TopBar.razor to add dev menu item
4. Test all functionality
5. (Optional) Add to Login page
6. Document changes in DEVELOPMENT_LEDGER.md

## Related Files

- `/Endpoints/DevSwitchUserEndpoint.cs` - Existing endpoint definition
- `/Components/Pages/DevSwitchUser.razor` - User switching UI
- `/Components/Layout/TopBar.razor` - Main navigation bar
- `/Pages/Account/Login.cshtml` - Login page
- `/Program.cs` - Application startup and configuration

## Notes

- This feature follows the existing pattern of development-only tools
- The endpoint uses cookie-based authentication which is already implemented
- The feature improves developer experience without affecting production
