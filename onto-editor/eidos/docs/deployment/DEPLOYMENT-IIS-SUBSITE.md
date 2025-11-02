# IIS Subsite Deployment Guide for Eidos

This guide explains how to deploy the Eidos Ontology Builder as an IIS subsite (e.g., `serveralias.com/eidos`) while maintaining support for root-level deployment.

## Overview

Eidos has been configured to support flexible deployment scenarios:
- **Root deployment**: `https://yourserver.com/` (PathBase = "")
- **Subsite deployment**: `https://yourserver.com/eidos/` (PathBase = "/eidos")

All navigation, routing, and static file references automatically adapt based on the PathBase configuration.

## Configuration Changes Made

### 1. PathBase Configuration (`appsettings.json`)
```json
{
  "PathBase": "",
  // ... other settings
}
```
- Set to `""` (empty string) for root deployment
- Set to `"/eidos"` for subsite deployment at `/eidos`
- Set to `"/myapp"` for subsite deployment at `/myapp`

### 2. Middleware Setup (`Program.cs:396-407`)
```csharp
var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
    Console.WriteLine($"✓ PathBase configured: {pathBase}");
}
```

### 3. Dynamic Base Href (`Components/App.razor:9`)
```razor
<base href="@(string.IsNullOrEmpty(Configuration["PathBase"]) ? "/" : Configuration["PathBase"] + "/")" />
```

### 4. Navigation Updates
All navigation links have been converted to relative paths:
- `Navigation.NavigateTo("/ontology/5")` → `Navigation.NavigateTo("ontology/5")`
- `<a href="/settings">` → `<NavLink href="settings">`

## IIS Deployment Instructions

### Prerequisites
- IIS 10.0 or later
- .NET 9.0 Hosting Bundle installed
- ASP.NET Core Module V2

### Step 1: Publish the Application
```bash
cd /path/to/eidos
dotnet publish -c Release -o ./publish
```

### Step 2: Configure PathBase

#### For Root Deployment (e.g., `https://yourserver.com/`)
Edit `appsettings.json` or `appsettings.Production.json`:
```json
{
  "PathBase": ""
}
```

Or set as environment variable in IIS:
- Name: `PathBase`
- Value: (leave empty)

#### For Subsite Deployment (e.g., `https://yourserver.com/eidos/`)
Edit `appsettings.json` or `appsettings.Production.json`:
```json
{
  "PathBase": "/eidos"
}
```

Or set as environment variable in IIS:
1. Open IIS Manager
2. Select your application
3. Open "Configuration Editor"
4. Section: `system.webServer/aspNetCore`
5. Add environment variable:
   - Name: `PathBase`
   - Value: `/eidos`

### Step 3: Create IIS Application

#### Option A: Root Website
1. Open IIS Manager
2. Right-click "Sites" → "Add Website"
3. Site name: `Eidos`
4. Physical path: `C:\inetpub\eidos` (copy publish folder here)
5. Binding: HTTP/HTTPS on port 80/443
6. Leave PathBase empty

#### Option B: Virtual Application (Subsite)
1. Open IIS Manager
2. Expand existing website
3. Right-click website → "Add Application"
4. Alias: `eidos` (this becomes the URL path)
5. Physical path: `C:\inetpub\eidos` (copy publish folder here)
6. Application pool: Create new or use existing .NET pool
7. Set PathBase to `/eidos` in appsettings.json

### Step 4: Verify web.config
Ensure `web.config` exists in the publish folder:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\Eidos.dll"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

### Step 5: Set Application Pool Settings
1. Open IIS Manager → Application Pools
2. Select your application pool
3. Right-click → "Advanced Settings"
4. Set:
   - .NET CLR Version: `No Managed Code`
   - Start Mode: `AlwaysRunning` (optional, for better performance)

### Step 6: Configure Permissions
Grant IIS_IUSRS read access to the application folder:
```powershell
icacls "C:\inetpub\eidos" /grant "IIS_IUSRS:(OI)(CI)R" /T
```

### Step 7: Test the Deployment

#### For Root Deployment:
- Navigate to: `https://yourserver.com/`
- Click "Settings" → Should go to: `https://yourserver.com/settings`
- Verify all navigation works

#### For Subsite Deployment:
- Navigate to: `https://yourserver.com/eidos/`
- Click "Settings" → Should go to: `https://yourserver.com/eidos/settings`
- Verify all navigation works
- Check that static files load (CSS, JS, images)

## Testing Checklist

### Navigation Tests
- [ ] Home page loads
- [ ] Dashboard loads
- [ ] Ontology view loads and displays correctly
- [ ] Settings page accessible
- [ ] Admin page accessible (if admin user)
- [ ] Account pages (Login, Register, Logout) work
- [ ] Back buttons work correctly
- [ ] Breadcrumbs work correctly

### Static File Tests
- [ ] CSS loads (check browser developer tools)
- [ ] JavaScript loads (check console for errors)
- [ ] Favicon displays
- [ ] Images load correctly

### Functionality Tests
- [ ] Create new ontology
- [ ] Add concepts and relationships
- [ ] Graph visualization renders
- [ ] Import/Export TTL works
- [ ] OAuth authentication works (if configured)

## Troubleshooting

### Issue: "Failed to load resource" errors for CSS/JS
**Cause:** Base href not configured correctly or static files not being served

**Solution:**
1. Verify `<base href>` in browser source view
2. Check IIS URL Rewrite rules aren't interfering
3. Ensure `app.UseStaticFiles()` is in Program.cs

### Issue: Navigation redirects to wrong URLs
**Cause:** PathBase mismatch or absolute paths still in code

**Solution:**
1. Verify PathBase setting matches IIS virtual application alias
2. Check browser console for navigation errors
3. Ensure all NavigateTo() calls use relative paths (no leading `/`)

### Issue: OAuth callback fails
**Cause:** OAuth provider redirect URIs not updated

**Solution:**
Update redirect URIs in OAuth provider settings:
- Root: `https://yourserver.com/signin-google`
- Subsite: `https://yourserver.com/eidos/signin-google`

### Issue: 500 Internal Server Error
**Cause:** Multiple possible causes

**Solution:**
1. Enable detailed errors in web.config:
   ```xml
   <aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
   ```
2. Check logs folder for error details
3. Verify .NET Hosting Bundle installed
4. Check Application Pool settings

## Environment Variables vs appsettings.json

You can set PathBase in multiple ways (in order of precedence):

1. **Environment Variable** (highest priority)
   - IIS Configuration Editor
   - web.config `<environmentVariable>` section

2. **appsettings.Production.json**
   ```json
   {
     "PathBase": "/eidos"
   }
   ```

3. **appsettings.json** (lowest priority)

**Recommendation:** Use environment variables in IIS for easier configuration without modifying files.

## Multiple Deployment Scenarios

You can deploy the same published application to multiple locations:

```
Production:
  https://production.com/           (PathBase: "")

Staging:
  https://staging.com/eidos/        (PathBase: "/eidos")

Development:
  https://dev.com/apps/eidos/       (PathBase: "/apps/eidos")
```

Each deployment only needs its own PathBase configuration—no code changes required.

## Performance Considerations

### Static File Caching
IIS automatically caches static files. For production, consider:
1. Enable HTTP/2 in IIS
2. Enable static file compression
3. Set appropriate cache headers

### Health Checks
Eidos includes health checks at `/health`. Configure IIS monitoring:
1. IIS URL Rewrite for health endpoint
2. Application Initialization for faster startup
3. Recycle settings based on memory/CPU

## Security Considerations

1. **HTTPS Only**: Configure HTTPS binding and redirect HTTP to HTTPS
2. **Connection Strings**: Store in environment variables or Azure Key Vault
3. **Authentication**: Update OAuth redirect URIs for subsite paths
4. **CORS**: If needed, configure for subsite deployment

## Support

For issues specific to IIS subsite deployment:
1. Check IIS logs: `C:\inetpub\logs\LogFiles`
2. Check application logs: `.\logs\stdout*.log`
3. Enable detailed errors for debugging
4. Verify PathBase configuration matches IIS alias exactly

---

**Last Updated:** 2025-10-28
**Configuration Version:** Eidos 1.0 - IIS Subsite Support
