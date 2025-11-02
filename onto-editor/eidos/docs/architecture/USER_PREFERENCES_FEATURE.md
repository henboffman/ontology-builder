# User Preferences Feature

**Implemented:** 2025-10-25

## Overview

Added a comprehensive user preferences system that allows users to customize default colors for concepts and relationships, as well as graph display settings. These preferences are automatically applied when creating new ontology elements.

---

## Features Implemented

### 1. User Preferences Model

**File:** `Models/UserPreferences.cs`

A new database table that stores per-user customization settings:

**Concept Color Defaults:**
- Entity (default: `#4A90E2` - Blue)
- Process (default: `#E67E22` - Orange)
- Quality (default: `#6BCF7F` - Green)
- Role (default: `#9B59B6` - Purple)
- Function (default: `#E74C3C` - Red)
- Information (default: `#3498DB` - Light Blue)
- Event (default: `#F39C12` - Amber)
- Default/Uncategorized (default: `#95A5A6` - Gray)

**Relationship Color Defaults:**
- Is-A / Subclass (default: `#2C3E50` - Dark Blue)
- Part-Of (default: `#16A085` - Teal)
- Has-Part (default: `#27AE60` - Green)
- Related-To (default: `#7F8C8D` - Gray)
- Default/Other (default: `#34495E` - Dark Gray)

**Graph Display Settings:**
- Default Node Size (20-80px, default: 40)
- Default Edge Thickness (1-5px, default: 2)
- Text Size Scale (50-150%, default: 100)
- Show Edge Labels (default: true)
- Auto-Color By Category (default: true)

**Helper Methods:**
- `GetColorForCategory(category)` - Returns appropriate color for a concept category
- `GetColorForRelationshipType(type)` - Returns appropriate color for a relationship type

---

### 2. Database Migration

**Migration:** `20251025170550_AddUserPreferences`

- Created `UserPreferences` table
- One-to-one relationship with `AspNetUsers` (ApplicationUser)
- Unique index on `UserId`
- Cascade delete when user is deleted

---

### 3. Service Layer

**Files:**
- `Services/Interfaces/IUserPreferencesService.cs`
- `Services/UserPreferencesService.cs`

**Methods:**
- `GetPreferencesAsync(userId)` - Get user preferences (auto-creates if none exist)
- `GetCurrentUserPreferencesAsync()` - Get preferences for authenticated user
- `UpdatePreferencesAsync(preferences)` - Save preference changes
- `ResetToDefaultsAsync(userId)` - Reset all preferences to defaults

**Features:**
- Automatic creation of default preferences on first access
- Full CRUD operations for all preference fields
- Logging of preference changes

---

### 4. User Interface

**File:** `Components/Settings/PreferencesSettings.razor`

A new "Preferences" tab in the Settings page (`/settings`) with:

**Concept Color Configuration:**
- Color pickers for each concept category
- Visual preview badges showing current colors
- Descriptive help text for each category

**Relationship Color Configuration:**
- Color pickers for each relationship type
- Visual preview badges
- Help text explaining each type

**Graph Display Settings:**
- Slider for node size (20-80px)
- Slider for edge thickness (1-5px)
- Slider for text size scale (50-150%)
- Checkbox for showing edge labels
- Checkbox for auto-coloring by category

**Actions:**
- **Save Preferences** - Saves all changes
- **Reset to Defaults** - Restores factory default colors (with confirmation)

**File:** `Components/Pages/Settings.razor`

Updated to include the new Preferences tab:
- Added "Preferences" tab with palette icon
- Integrated PreferencesSettings component

---

### 5. Integration with Concept Creation

**File:** `Components/Pages/OntologyView.razor`

**Changes:**

1. **ShowAddConceptDialog Method (lines 548-573)**
   - Now async method
   - Loads user preferences
   - Applies default concept color from preferences
   - Falls back to random color if preferences fail to load

2. **OnConceptCategoryChanged Method (lines 575-593)**
   - New method to handle category selection
   - Automatically applies category-specific color when `AutoColorByCategory` is enabled
   - Updates concept color based on user's preferred color scheme

3. **Dependency Injection**
   - Added `@inject IUserPreferencesService PreferencesService`

**User Experience:**
- When user clicks "Add Concept", the new concept gets the default color from their preferences
- When user selects a category (Entity, Process, Quality, etc.), the color automatically updates to match their preferred color for that category (if auto-color is enabled)
- User can still manually override colors using the color picker

---

### 6. Integration with Relationship Creation

**File:** `Components/Pages/OntologyView.razor`

**ShowAddRelationshipDialog Method (lines 595-619)**
- Prepared for future relationship color support
- Includes commented code showing how to apply default relationship colors
- Currently relationships don't have a Color property in the model
- To enable: Add `Color` property to `Relationship` model and uncomment the code

---

### 7. Integration with Template Service

**File:** `Services/OntologyTemplateService.cs`

**Changes:**

1. **Constructor** - Now injects `IUserPreferencesService`
2. **CreateFromBFOTemplateAsync** - Loads user preferences and applies category-specific colors to all BFO template concepts
3. **AddProvOConceptsAsync** - Applies user's preferred colors to PROV-O concepts

**User Experience:**
- When creating a new ontology from the BFO template, all concepts use the user's preferred colors
- Entity concepts get the user's Entity color
- Process concepts get the user's Process color
- Quality concepts get the user's Quality color
- Event concepts get the user's Event color
- Role concepts get the user's Role color

---

### 8. Real-Time Text Size Adjustment

**File:** `Components/Pages/GraphVisualization.razor`

**Features:**

1. **Floating Control Panel** - Overlay in top-right corner of graph with text size slider (50-150%)
2. **OnInitializedAsync** - Loads text size preference when component is created
3. **OnTextSizeChanged** - Saves text size to database immediately when slider changes
4. **Proportional Scaling** - Text scales independently from node size

**User Experience:**
- Text size slider appears directly on the graph view
- Changes apply immediately without page refresh
- Setting persists across browser sessions and devices
- Setting persists when switching between tabs (Graph ↔ List)
- Works in combination with node size (node size sets base, text scale multiplies)

**Technical Details:**
- JavaScript applies text scale multiplier to calculated font sizes
- Component loads preference on initialization, preventing reset on tab switches
- Database stores value as integer (50-150)
- JavaScript receives value as decimal (0.5-1.5)

---

## Usage Guide

### For Users

1. **Access Preferences**
   - Navigate to Settings (`/settings`)
   - Click the "Preferences" tab

2. **Customize Concept Colors**
   - Choose colors for each concept category
   - Colors will apply to new concepts of that category

3. **Customize Relationship Colors**
   - Choose colors for each relationship type
   - (Will apply when relationship colors are added to the model)

4. **Adjust Graph Settings**
   - Use sliders to set default node size, edge thickness, and text size
   - Toggle edge labels on/off
   - Enable/disable auto-coloring by category

5. **Adjust Text Size While Viewing Graph**
   - Use the floating slider in the top-right corner of any graph
   - Changes save automatically and persist across sessions

6. **Save or Reset**
   - Click "Save Preferences" to persist changes
   - Click "Reset to Defaults" to restore factory colors

### For Developers

**Accessing User Preferences in Code:**
```csharp
// Inject the service
@inject IUserPreferencesService PreferencesService

// Get current user's preferences
var prefs = await PreferencesService.GetCurrentUserPreferencesAsync();

// Get color for a category
string color = prefs.GetColorForCategory("Entity"); // Returns "#4A90E2"

// Get color for a relationship type
string color = prefs.GetColorForRelationshipType("is-a"); // Returns "#2C3E50"

// Update preferences
prefs.EntityColor = "#FF0000";
await PreferencesService.UpdatePreferencesAsync(prefs);
```

---

## Database Schema

```sql
CREATE TABLE [UserPreferences] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,

    -- Concept Colors
    [EntityColor] nvarchar(50) NOT NULL,
    [ProcessColor] nvarchar(50) NOT NULL,
    [QualityColor] nvarchar(50) NOT NULL,
    [RoleColor] nvarchar(50) NOT NULL,
    [FunctionColor] nvarchar(50) NOT NULL,
    [InformationColor] nvarchar(50) NOT NULL,
    [EventColor] nvarchar(50) NOT NULL,
    [DefaultConceptColor] nvarchar(50) NOT NULL,

    -- Relationship Colors
    [IsARelationshipColor] nvarchar(50) NOT NULL,
    [PartOfRelationshipColor] nvarchar(50) NOT NULL,
    [HasPartRelationshipColor] nvarchar(50) NOT NULL,
    [RelatedToRelationshipColor] nvarchar(50) NOT NULL,
    [DefaultRelationshipColor] nvarchar(50) NOT NULL,

    -- Graph Settings
    [DefaultNodeSize] int NOT NULL,
    [DefaultEdgeThickness] int NOT NULL,
    [TextSizeScale] int NOT NULL,
    [ShowEdgeLabels] bit NOT NULL,
    [AutoColorByCategory] bit NOT NULL,

    -- Metadata
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,

    CONSTRAINT [PK_UserPreferences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserPreferences_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_UserPreferences_UserId] ON [UserPreferences] ([UserId]);
```

---

## Benefits

1. **Personalization** - Each user can customize their ontology editing experience
2. **Consistency** - Colors are consistent across ontologies for the same user
3. **Productivity** - Automatic color assignment saves time when creating concepts
4. **Visual Organization** - Category-based coloring helps distinguish concept types at a glance
5. **Flexibility** - Users can override automatic colors manually when needed

---

## Future Enhancements

1. **Relationship Colors in Graph**
   - Add `Color` property to `Relationship` model
   - Implement color visualization in graph view
   - Uncomment relationship color code in `ShowAddRelationshipDialog`

2. **Import/Export Preferences**
   - Allow users to export their color scheme
   - Import color schemes from other users or templates

3. **Preset Color Schemes**
   - Provide built-in color palettes (e.g., "Colorblind-Friendly", "High Contrast", "Pastel")
   - One-click application of preset schemes

4. **Per-Ontology Overrides**
   - Allow overriding user defaults on a per-ontology basis
   - Useful for ontologies with specific color requirements

5. **Additional Customization**
   - Font preferences
   - Layout preferences
   - Default concept templates
   - Keyboard shortcuts

---

## Testing Checklist

- [x] Database migration applied successfully
- [x] UserPreferences table created with TextSizeScale
- [x] Service methods work correctly
- [x] UI loads preferences on Settings page
- [x] Color pickers update preferences
- [x] Save button persists changes
- [x] Reset button restores defaults
- [x] New concepts get default colors from preferences
- [x] Template ontologies (BFO, PROV-O) use user's preferred colors
- [x] Category change updates color (if auto-color enabled)
- [x] Graph display settings apply to graph visualization
- [x] Edge thickness changes reflected in graph
- [x] Edge label visibility toggles correctly
- [x] Node size changes reflected in graph
- [x] Text size slider appears on graph
- [x] Text size changes apply immediately
- [x] Text size persists when switching tabs
- [x] Text size persists across browser sessions
- [x] Multiple users have independent preferences
- [x] Preferences survive app restart

---

## Files Modified/Created

**Models:**
- ✅ `Models/UserPreferences.cs` (new)
- ✅ `Models/ApplicationUser.cs` (added Preferences navigation property)

**Data:**
- ✅ `Data/OntologyDbContext.cs` (added DbSet and configuration)

**Services:**
- ✅ `Services/Interfaces/IUserPreferencesService.cs` (new)
- ✅ `Services/UserPreferencesService.cs` (new)
- ✅ `Services/OntologyTemplateService.cs` (updated to use user preferences for template colors)
- ✅ `Program.cs` (registered service)

**Components:**
- ✅ `Components/Settings/PreferencesSettings.razor` (new - includes text size slider)
- ✅ `Components/Pages/Settings.razor` (added Preferences tab)
- ✅ `Components/Pages/OntologyView.razor` (integrated with concept creation and logging)
- ✅ `Components/Pages/GraphVisualization.razor` (added floating text size control, preference loading on init)

**JavaScript:**
- ✅ `wwwroot/js/graphVisualization.js` (updated to accept display options and scale text)

**Migrations:**
- ✅ `Migrations/20251025170550_AddUserPreferences.cs` (initial UserPreferences table)
- ✅ `Migrations/20251025205852_AddTextSizeScaleToUserPreferences.cs` (added TextSizeScale column)
- ✅ `Migrations/20251025210505_UpdateTextSizeScaleDefault.cs` (fixed existing records to have default 100)

---

## Author

Implemented by Claude Code Assistant on 2025-10-25

## Related Documentation

- [SECURITY_AUDIT.md](SECURITY_AUDIT.md) - Security analysis and recommendations
- [AZURE_SECURITY_AUDIT.md](AZURE_SECURITY_AUDIT.md) - Azure deployment security audit
- [SECURITY_FIXES_IMPLEMENTED.md](SECURITY_FIXES_IMPLEMENTED.md) - Real-time collaboration security fixes
