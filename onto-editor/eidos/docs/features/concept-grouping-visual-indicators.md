# Concept Grouping Visual Indicators & Smart Positioning

**Last Updated**: November 15, 2024
**Feature Status**: ✅ Implemented
**Related Files**:
- `wwwroot/js/conceptGrouping.js`
- `wwwroot/css/concept-grouping.css`
- `Services/UserPreferencesService.cs`
- `Models/UserPreferences.cs`

## Overview

This feature provides visual feedback for collapsed concept groups through animated floating indicators and intelligent node positioning when expanding groups to prevent overlaps.

## Components

### 1. Floating Orbital Indicators

When concepts are grouped and collapsed, small animated circles appear around the parent node to indicate that child nodes are hidden.

#### Visual Design

**Appearance:**
- Small circles (10px diameter) with 2px white borders
- Color-matched to the parent node's background color
- Multi-layer glow effect using box-shadow
- Positioned in a circle around the node (15px outside the border)

**Animation:**
- Pulsing scale animation (0.9 → 1.3 scale over 1.5 seconds)
- Staggered timing: each circle starts 0.15s after the previous one
- Creates a "breathing" or "orbiting satellites" effect
- Opacity pulses from 0.8 to 1.0

**Distribution:**
- Up to 5 indicators shown (even if more children exist)
- Evenly distributed around the full 360° circle
- Start position: top (12 o'clock) and proceed clockwise

#### Implementation Details

**JavaScript (`conceptGrouping.js`):**

```javascript
// Key function: addFloatingIndicators(cy, node, childCount)
// Called from: applyStackedEffect() after a group is collapsed

function addFloatingIndicators(cy, node, childCount) {
    // Creates indicator container as absolute positioned div
    // Appends to cy.container() with high z-index (9999)

    // For each indicator circle:
    // 1. Calculate position using trigonometry (angle-based)
    // 2. Apply node's background color dynamically
    // 3. Set up staggered animation delays
    // 4. Register pan/zoom/position event handlers for repositioning
}

function updateIndicatorPositions(cy, node, container, count) {
    // Recalculates positions on graph pan/zoom/position changes
    // Uses node.renderedPosition() for screen coordinates
    // Orbit radius: nodeWidth / 2 + 15px
    // Full circle distribution: 360° / count
}
```

**CSS (`concept-grouping.css`):**

```css
.group-indicator-circle {
    position: absolute;
    width: 10px;
    height: 10px;
    animation: floatIndicator 1.5s ease-in-out infinite;
    /* Border and box-shadow set dynamically via JS */
}

@keyframes floatIndicator {
    0%, 100% { transform: scale(0.9); opacity: 0.8; }
    50% { transform: scale(1.3); opacity: 1; }
}
```

#### Lifecycle Management

**Creation:**
- Indicators created when `applyStackedEffect()` is called
- Triggered by group collapse or initial graph load

**Updates:**
- Position updates on: pan, zoom, and node position changes
- Event handlers stored in node data: `node.data('indicatorUpdateHandler')`

**Cleanup:**
- Removed when group is expanded (`expandConceptGroup`)
- Removed when groups are refreshed (`updateConceptGroups`)
- Event handlers cleaned up to prevent memory leaks
- Query: `document.querySelectorAll('.group-indicator-container[data-node-id="..."]')`

### 2. Smart Node Positioning on Expand

When a collapsed group is expanded, child nodes are intelligently positioned to avoid overlaps with existing nodes.

#### Algorithm Overview

**Goal:** Position child nodes around the parent while avoiding collisions with other visible nodes.

**Parameters:**
- `spawnRadius`: 150px - Distance from parent to spawn children
- `minNodeDistance`: 80px - Minimum acceptable distance from other nodes
- `angleTests`: 16 positions tested per node (every 22.5°)

#### Collision Avoidance Algorithm

**Step-by-step Process:**

1. **Initial Setup**
   ```javascript
   const parentPos = parentNode.position();
   const otherNodes = cy.nodes().filter(/* visible, not parent, not children */);
   ```

2. **For Each Child Node:**

   a. **Test 16 Candidate Positions**
      - Positions arranged in a circle at `spawnRadius` from parent
      - Angles: 0°, 22.5°, 45°, 67.5°, ... 337.5°

   b. **Score Each Position**
      ```javascript
      for each test position:
          score = 0
          for each other node:
              distance = euclidean_distance(test_pos, other_pos)

              if distance < minNodeDistance:
                  score -= 1000  // Heavy penalty for overlap
              else:
                  score += distance  // Reward for being far away
      ```

   c. **Select Best Position**
      - Choose position with highest score
      - Higher score = more space around the node

   d. **Animate to Position**
      ```javascript
      childNode.animate({
          position: { x: bestPos.x, y: bestPos.y }
      }, {
          duration: 400,
          easing: 'ease-out'
      });
      ```

#### Implementation Locations

**Primary Implementation:** `expandConceptGroup()` function (lines 822-903)
- Called directly when user clicks expand button
- Handles initial positioning logic

**Secondary Implementation:** `updateConceptGroups()` function (lines 992-1071)
- Called after server-side group state changes
- Ensures positioning survives state refreshes
- Uses edge connections to identify parent nodes

**Why Two Implementations?**
The flow when expanding a group is:
1. User clicks expand button
2. `expandConceptGroup()` is called (tries to position nodes)
3. Server updates group state
4. `updateConceptGroups()` is called (would override positions)
5. Without positioning in step 4, nodes revert to original positions

By implementing in both places, we ensure the smart positioning is preserved.

#### Scoring Examples

**Good Position (High Score):**
```
Position: (1021.9, 282.8)
Score: 2453.6

Distance to nearest nodes:
- Node A: 156px → +156 points
- Node B: 234px → +234 points
- Node C: 189px → +189 points
Total: ~2453 points (far from everything)
```

**Bad Position (Low Score):**
```
Position: (894.5, 332.8)
Score: -987.2

Distance to nearest nodes:
- Node A: 45px → -1000 points (collision!)
- Node B: 67px → -1000 points (collision!)
- Node C: 234px → +234 points
Total: ~-987 points (too close to nodes)
```

### 3. User Preference: Grouping Radius

Users can adjust the grouping radius (drag distance) in Settings > Preferences.

#### Database Schema

**Table:** `UserPreferences`

**Column:** `GroupingRadius`
- Type: `INTEGER`
- Default: `100`
- Range: `25-200` pixels
- Description: Distance nodes must be within to group when dragging

#### Service Layer

**File:** `Services/UserPreferencesService.cs`

**Key Methods:**

```csharp
public async Task<UserPreferences> UpdatePreferencesAsync(UserPreferences preferences)
{
    // Line 130: Updates GroupingRadius along with other preferences
    existing.GroupingRadius = preferences.GroupingRadius;

    await context.SaveChangesAsync();

    // Invalidate cache
    _cache.Remove($"{CACHE_KEY_PREFIX}{preferences.UserId}");
}

public async Task<UserPreferences> ResetToDefaultsAsync(string userId)
{
    // Line 186: Resets GroupingRadius to default (100px)
    preferences.GroupingRadius = defaults.GroupingRadius;
}
```

**Bug Fix (2024-11-15):**
- Previously, `GroupingRadius` was not being saved to the database
- The property existed in the model and database, but was omitted from the update logic
- Fixed by adding the property to both update methods

#### UI Component

**File:** `Components/Settings/PreferencesSettings.razor`

**Control:**
```razor
<input type="range" class="form-range"
       min="25" max="200" step="5"
       value="@preferences.GroupingRadius"
       @oninput="@((e) => UpdateGroupingRadiusPreview(e))" />
```

**Features:**
- Live preview showing visual radius indicator
- SVG visualization of the grouping radius
- Real-time feedback as user adjusts slider

## Performance Considerations

### Indicator Rendering

**Optimization Strategies:**
1. **Limited Count**: Maximum 5 indicators per group (prevents clutter)
2. **CSS Animations**: Uses CSS transforms (GPU-accelerated)
3. **Event Throttling**: Position updates throttled on pan/zoom
4. **Cleanup**: Indicators removed immediately when no longer needed

**Memory Management:**
- Event handlers stored and properly cleaned up
- No memory leaks from orphaned listeners
- Document-level query cleanup on group expansion

### Positioning Algorithm

**Complexity Analysis:**
- Time: O(n × m × k) where:
  - n = number of child nodes being positioned
  - m = number of other visible nodes
  - k = number of test angles (16)
- Typical case: 2-5 children × 10-50 nodes × 16 angles = ~160-4000 iterations
- Performance: < 10ms on modern hardware

**Optimization Opportunities:**
- Could use spatial indexing (quadtree) for large graphs (100+ nodes)
- Could reduce test angles for graphs with < 10 nodes
- Current implementation performs well for typical use cases (< 50 nodes)

## Browser Compatibility

### CSS Features Used
- CSS transforms (scale)
- CSS animations (@keyframes)
- CSS custom properties (for dark mode)
- Multiple box-shadows

**Support:** All modern browsers (Chrome 90+, Firefox 88+, Safari 14+, Edge 90+)

### JavaScript Features Used
- ES6 arrow functions
- Template literals
- querySelector/querySelectorAll
- Math functions (cos, sin, sqrt, pow)

**Support:** All modern browsers

## Testing Recommendations

### Manual Testing Scenarios

1. **Basic Grouping Flow**
   - Create a group with 2-3 nodes
   - Verify floating indicators appear
   - Verify indicators match node color
   - Verify pulsing animation is smooth
   - Expand group and verify indicators disappear
   - Verify child nodes spread out at 150px radius

2. **Dense Graph Scenario**
   - Create graph with 10+ nodes in close proximity
   - Group 3-4 nodes in the center
   - Expand group
   - Verify children find "gaps" between existing nodes
   - Check console for positioning scores

3. **User Preferences**
   - Adjust grouping radius slider (Settings > Preferences)
   - Save preferences
   - Navigate away and return
   - Verify value persists
   - Try different radius values (25px, 100px, 200px)

4. **Edge Cases**
   - Group with 1 child (should show 1 indicator)
   - Group with 10 children (should show 5 indicators max)
   - Expand group at graph edge (nodes should still find positions)
   - Pan/zoom graph while indicators are visible

### Automated Testing Opportunities

**Unit Tests (Future):**
```csharp
// Services/UserPreferencesServiceTests.cs
[Fact]
public async Task UpdatePreferences_ShouldPersistGroupingRadius()
{
    // Arrange
    var preferences = new UserPreferences { GroupingRadius = 150 };

    // Act
    await _service.UpdatePreferencesAsync(preferences);
    var retrieved = await _service.GetPreferencesAsync(userId);

    // Assert
    Assert.Equal(150, retrieved.GroupingRadius);
}
```

**Integration Tests (Future):**
```javascript
// JavaScript tests for positioning algorithm
describe('Smart Node Positioning', () => {
    it('should avoid placing nodes within minNodeDistance', () => {
        // Setup graph with existing nodes
        // Position new child node
        // Assert distance > minNodeDistance for all nodes
    });

    it('should prefer positions with higher scores', () => {
        // Test that algorithm chooses position farthest from others
    });
});
```

## Troubleshooting

### Indicators Not Appearing

**Symptoms:** No floating circles around grouped nodes

**Possible Causes:**
1. Container z-index conflict
2. Indicators created but positioned off-screen
3. CSS not loaded

**Debug Steps:**
```javascript
// Check console for creation logs
// Should see: "🎨 Adding floating indicators for node concept-X with N children"

// Inspect DOM
document.querySelectorAll('.group-indicator-container')
// Should return NodeList with containers

// Check computed styles
const circle = document.querySelector('.group-indicator-circle');
window.getComputedStyle(circle).display; // Should be "block"
window.getComputedStyle(circle).visibility; // Should be "visible"
```

### Indicators Not Removed

**Symptoms:** Circles remain after expanding group

**Check Console:**
```
// Should see cleanup logs when expanding:
"🗑️ Removing indicator container for concept-X"
"🗑️ Removed update handler for concept-X"
```

**Manual Cleanup:**
```javascript
// If indicators are stuck, manually remove:
document.querySelectorAll('.group-indicator-container').forEach(el => el.remove());
```

### Poor Node Positioning

**Symptoms:** Nodes cluster together or overlap after expansion

**Debug Console Output:**
```
// Check positioning scores in console:
"📍 Positioning child 245 at (1021.9, 282.8) with score 2453.6"

// Low scores (< 0) indicate overlaps
// High scores (> 1000) indicate good spacing
```

**Adjustment Options:**
```javascript
// In conceptGrouping.js, adjust parameters:
const spawnRadius = 150;      // Increase for more spread
const minNodeDistance = 80;   // Increase to avoid closer nodes
const angleTests = 16;        // Increase for more thorough search
```

### GroupingRadius Not Persisting

**Symptoms:** Slider value resets after navigation

**Verify Database:**
```sql
SELECT Id, UserId, GroupingRadius
FROM UserPreferences
WHERE UserId = 'your-user-id';
```

**Check Update Method:**
- Ensure `UserPreferencesService.UpdatePreferencesAsync` includes:
  ```csharp
  existing.GroupingRadius = preferences.GroupingRadius;
  ```

## Future Enhancements

### Potential Improvements

1. **Adaptive Spawn Radius**
   - Automatically adjust `spawnRadius` based on graph density
   - Increase radius for dense graphs, decrease for sparse graphs

2. **Force-Directed Layout Integration**
   - After positioning, apply gentle force-directed adjustment
   - Minimize edge crossings and improve overall layout

3. **Indicator Customization**
   - User preference for indicator style (circles, dots, stars)
   - Option to show count badge on parent node instead of circles

4. **Animation Enhancements**
   - Rotate indicators around the node (true orbital motion)
   - Particle effect when expanding (circles fly to child positions)

5. **Performance Optimization**
   - Use spatial indexing for position scoring (O(log n) lookups)
   - Web Worker for position calculations on large graphs

6. **Accessibility**
   - ARIA labels for grouped nodes
   - Keyboard navigation for expand/collapse
   - Screen reader announcements for group state changes

## Version History

### v1.0 (2024-11-15)
- ✅ Initial implementation of floating indicators
- ✅ Smart collision-avoidance positioning
- ✅ User preference for grouping radius
- ✅ Fix: GroupingRadius persistence bug
- ✅ Indicators match parent node color
- ✅ Cleanup on expand to prevent visual artifacts

## Related Documentation

- [Concept Grouping Architecture](../architecture/concept-grouping.md)
- [User Preferences System](../architecture/user-preferences.md)
- [Graph Visualization](../features/graph-visualization.md)

## References

- Cytoscape.js Documentation: https://js.cytoscape.org/
- Force-Directed Layouts: https://en.wikipedia.org/wiki/Force-directed_graph_drawing
- CSS Transforms: https://developer.mozilla.org/en-US/docs/Web/CSS/transform
