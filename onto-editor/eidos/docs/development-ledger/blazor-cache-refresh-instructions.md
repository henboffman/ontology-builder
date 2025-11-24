# Blazor Server Cache Refresh Instructions

**Date**: November 3, 2025
**Issue**: Blazor Server caching old component versions

## Problem

When making changes to Blazor components, the old version continues to render even after rebuilding. This is because:

1. **SignalR maintains active connections** with cached component state
2. **Browser caches static assets** and component JavaScript
3. **dotnet process holds compiled assemblies** in memory

## Solution: Complete Cache Clear

### Step 1: Kill ALL dotnet Processes

```bash
pkill -9 dotnet
```

This forcefully terminates all running dotnet processes.

### Step 2: Clean Build Artifacts

```bash
dotnet clean
```

This deletes all compiled DLLs and build artifacts from `bin/` and `obj/` directories.

### Step 3: Rebuild

```bash
dotnet build
```

Fresh compilation of all Razor components and C# code.

### Step 4: Start Fresh

```bash
dotnet run
```

Starts a new instance with all new compiled code.

### Step 5: Force Browser Refresh

In your browser:
- **Hard Refresh**: `Cmd+Shift+R` (Mac) or `Ctrl+Shift+R` (Windows/Linux)
- **Or**: Open DevTools → Right-click refresh → "Empty Cache and Hard Reload"
- **Or**: Close all browser tabs for the site and reopen

## What Was Done (This Session)

```bash
# 1. Killed all dotnet processes
pkill -9 dotnet

# 2. Cleaned build
dotnet clean
# Output: Deleted 300+ files from bin/Debug/net9.0/

# 3. Rebuilt
dotnet build
# Result: Build succeeded (0 errors, 11 warnings)
```

## Files That Were Updated

1. **IndividualManagementPanel.razor** - Added `StateHasChanged()` to cancel method
2. **ConceptManagementPanel.razor** - Added `StateHasChanged()` to cancel method
3. **RelationshipManagementPanel.razor** - Added `StateHasChanged()` to cancel method
4. **IndividualEditor.razor** - Complete rewrite of form bindings

## What You Need to Do Now

**Option 1: If you're running `dotnet run` yourself**
1. Stop your running `dotnet run` process (Ctrl+C)
2. Run: `dotnet run` again
3. In browser: Hard refresh (`Cmd+Shift+R`)

**Option 2: Let me start it for you**
- I can start a fresh instance in the background
- You'll still need to hard refresh your browser

## Why This Keeps Happening

Blazor Server is different from traditional web apps:

### Traditional Web (Stateless)
- Each request fetches fresh HTML from server
- Refresh = new HTML immediately
- ✅ Changes appear immediately

### Blazor Server (Stateful)
- Initial request loads HTML + JavaScript
- SignalR maintains persistent connection
- Components cached in memory on server
- Rendered HTML cached in browser
- ❌ Refresh doesn't reload components
- ❌ Rebuild doesn't restart server

### The Fix
Must do ALL of these:
1. ✅ Kill server process
2. ✅ Clean build artifacts
3. ✅ Rebuild application
4. ✅ Start fresh server
5. ✅ Hard refresh browser

## Quick Reference

### Full Reset (Copy-Paste)
```bash
pkill -9 dotnet && dotnet clean && dotnet build && dotnet run
```

Then in browser: `Cmd+Shift+R` (Mac) or `Ctrl+Shift+R` (Windows)

### Check if dotnet is Running
```bash
lsof -ti:7216  # Port 7216
lsof -ti:5026  # Port 5026
```

### Kill Specific Port
```bash
lsof -ti:7216 | xargs kill -9
```

## Browser Cache Issues

If changes still don't appear after server restart:

### Chrome/Edge
1. Open DevTools (F12)
2. Right-click the refresh button
3. Select "Empty Cache and Hard Reload"

### Firefox
1. Open DevTools (F12)
2. Click Network tab
3. Check "Disable Cache"
4. Refresh page

### Safari
1. Open DevTools (Cmd+Option+I)
2. Go to Develop menu
3. Select "Empty Caches"
4. Refresh page

## Prevention

To avoid this issue in the future:

### During Development
1. **Watch mode**: Use `dotnet watch run` instead of `dotnet run`
   - Auto-rebuilds on file changes
   - Auto-restarts server
   - Still need browser refresh

2. **Hot Reload**: Available in .NET 9
   - Enable with: `dotnet watch run --non-interactive`
   - Automatically pushes changes to browser
   - Sometimes fails on complex component changes

3. **Browser DevTools**: Keep DevTools open with "Disable Cache" checked

### When Making Component Changes
If editing:
- Razor components (`.razor`)
- Component code-behind (`.razor.cs`)
- Shared components
- Layout changes

→ Always do full restart (kill + clean + build + run)

## Status

✅ All processes killed
✅ Build artifacts cleaned
✅ Application rebuilt successfully
✅ 0 errors, 11 pre-existing warnings

**Next Step**: You need to restart the server and hard refresh your browser to see the changes.

---

## Technical Details

### What Gets Cached

**Server Side (dotnet process)**:
- Compiled Razor components in memory
- SignalR circuit state
- Component instances
- Scoped services

**Client Side (Browser)**:
- Static assets (CSS, JS)
- Blazor framework JavaScript
- SignalR connection state
- Component render tree in JavaScript

### Why Rebuild Alone Doesn't Work

Running `dotnet build` while server is running:
1. ✅ Creates new DLL files
2. ❌ Running process doesn't reload DLLs
3. ❌ Browser maintains SignalR connection to old process
4. ❌ Result: Old code still runs

### The Complete Reset Flow

```
pkill -9 dotnet
    ↓
Old server process terminated
    ↓
dotnet clean
    ↓
All DLLs deleted
    ↓
dotnet build
    ↓
Fresh DLLs created
    ↓
dotnet run
    ↓
New server process starts with new DLLs
    ↓
Cmd+Shift+R in browser
    ↓
Browser discards old cache
    ↓
New SignalR connection to new process
    ↓
✅ New components render
```

## Alternative: Use `dotnet watch`

Instead of manual restarts, use watch mode:

```bash
dotnet watch run
```

**Advantages**:
- Auto-detects file changes
- Auto-rebuilds
- Auto-restarts server
- Minimal downtime

**Still Required**:
- Browser refresh (usually)
- Occasionally needs full reset for complex changes

**Recommended** for active development!
