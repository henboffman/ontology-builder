# Month 1: Smart Note-Concept Linking & Knowledge Graph Integration

## Business Requirements Document

---

## Document Purpose

This document describes the functional requirements for implementing Smart Note-Concept Linking in the Eidos Ontology Builder. It is written from the perspective of what users need to accomplish, not how the system should be built technically.

**Target Audience:** Development team, QA testers, product managers  
**Timeline:** Month 1 (4 weeks)  
**Version:** 1.0

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Business Problem & Opportunity](#business-problem--opportunity)
3. [Feature 1: Automatic Concept Detection](#feature-1-automatic-concept-detection)
4. [Feature 2: Backlinks Panel](#feature-2-backlinks-panel)
5. [Feature 3: Note Network Visualization](#feature-3-note-network-visualization)
6. [Feature 4: Quick Navigation](#feature-4-quick-navigation)
7. [Feature 5: Smart Note Templates](#feature-5-smart-note-templates)
8. [User Workflows](#user-workflows)
9. [Success Criteria](#success-criteria)
10. [Out of Scope](#out-of-scope)

---

## Executive Summary

### The Big Picture

Currently, Eidos has two powerful but separate systems:

1. **Ontology Management** - Users create concepts and define relationships between them
2. **Workspace/Notes** - Users write markdown notes with tags and metadata

These systems don't talk to each other. When users write notes about concepts in their ontology, the system doesn't know they're related. Users have to remember connections manually.

### What We're Building

We're building an intelligent linking system that automatically:

- Detects when notes mention concepts from the ontology
- Shows users which notes reference each concept
- Visualizes how notes are connected through shared concepts
- Enables seamless navigation between notes and ontology

### User Value

- **Discover Hidden Connections** - See relationships between ideas you didn't know existed
- **Never Lose Context** - Always know what notes relate to the concept you're studying
- **Navigate Intuitively** - Jump from concept to related notes to related concepts with one click
- **Build Knowledge Naturally** - The system connects your ideas as you write, no manual linking required

---

## Business Problem & Opportunity

### Current Pain Points

1. **Disconnected Information**
   - A researcher writes notes about "Mitochondria" in the workspace
   - They have a "Mitochondria" concept in their ontology
   - The system doesn't connect these, so the note is lost when viewing the concept

2. **Manual Context Switching**
   - User is viewing a concept in the graph
   - They want to see their notes about this concept
   - They must leave the graph, go to workspace, search for notes, remember to search for related concepts too

3. **Lost Insights**
   - User has 5 notes that all mention "Cellular Respiration"
   - These notes probably contain related ideas
   - User never discovers these connections because they're not visible

4. **Duplicate Work**
   - User writes notes about a concept
   - Months later, they can't find those notes
   - They write new notes about the same concept
   - Knowledge is fragmented across multiple notes

### Opportunity

By connecting notes to concepts automatically, we transform Eidos from two separate tools into one integrated knowledge management system. Users can think and write naturally, and the system reveals the structure of their knowledge.

---

## Feature 1: Automatic Concept Detection

### What It Does

When a user writes in a note, the system automatically detects mentions of concepts from their ontology and creates invisible links. The user doesn't need to do anything special—just write naturally.

### Expected Behavior

#### Detection Rules

**When the system should create a link:**

- The note contains the exact name of a concept (case-insensitive)
- Example: Note contains "mitochondria" and there's a concept named "Mitochondria" → Link created

**What counts as a match:**

- Exact matches: "Cell Membrane" matches concept "Cell Membrane"
- Case-insensitive: "cell membrane" matches concept "Cell Membrane"
- Whole word matches: "Cell" should NOT match inside "Cellular" (unless "Cellular" is also a concept)
- Plural forms: This is nice-to-have but not required for v1
- Acronyms: If a concept has "DNA" as an alias, "DNA" in notes should match

**What should NOT be linked:**

- Partial word matches: "Cell" appearing inside "Excellent" should not match
- Common words: If someone creates a concept called "The" or "And", be smart about not linking every occurrence
- Words inside code blocks (markdown code blocks should be excluded)
- Words inside URLs

#### Visual Indicators in Notes

When editing a note, users should see which words are linked to concepts:

**Requirement 1.1: Highlight Concept Mentions**

- Text that matches a concept should be visually distinguished (different color, underline, or subtle background)
- The highlighting should be non-intrusive—users should be able to read normally
- Hovering over highlighted text should show a tooltip with:
  - Concept name
  - First 100 characters of concept description
  - "Click to view concept" message

**Requirement 1.2: Click to Navigate**

- Clicking a highlighted concept mention should:
  - Option A: Open the concept in a side panel (user stays in note)
  - Option B: Navigate to the concept in the ontology view (user leaves note)
  - Recommendation: Implement Option A with a "Open in ontology" button in the side panel

**Requirement 1.3: Manual Link Control**

- Users should be able to disable auto-linking for a specific note
  - Add a toggle in the note editor: "Auto-detect concepts"
  - When disabled, no automatic links are created
  - Existing links remain but no new ones are added
- Users should be able to manually create a link even if auto-detection missed it
  - Select text → Right-click → "Link to concept" → Choose concept from list
- Users should be able to remove a specific link
  - Hover over linked text → Small X icon appears → Click to unlink
  - This should only remove this specific link, not disable all auto-linking

#### When Detection Happens

**Requirement 1.4: Automatic Analysis Timing**

- When a note is created: Analyze immediately
- When a note is saved/updated: Re-analyze the entire note
- When a concept is renamed: Re-analyze all notes in that ontology
- When a concept is deleted: Remove all links to that concept
- Background processing is acceptable if immediate processing would slow down the UI

**Requirement 1.5: Performance Requirements**

- Analysis should complete within 2 seconds for notes up to 10,000 words
- If analysis takes longer, show a loading indicator and allow the user to continue editing
- The note editor should never feel slow or laggy due to concept detection

#### Edge Cases

**Multiple mentions of the same concept:**

- If a note mentions "Mitochondria" 5 times, all 5 should be highlighted
- But the system should store this as one link with multiple positions (not 5 separate links)
- Backlink count should show "1 note mentions this" not "5 mentions"

**Overlapping concept names:**

- If concepts are "Cell" and "Cell Membrane", and note says "Cell Membrane":
  - Should link to "Cell Membrane" (longer match wins)
  - Should NOT also link "Cell" separately

**Concepts with special characters:**

- Concept named "α-Helix" should match "α-Helix" in notes
- Concept named "Parent→Child" should match "Parent→Child" exactly

**Notes spanning multiple concepts:**

- A note can link to unlimited concepts
- All should be detected and linked
- Show count: "This note references 12 concepts"

---

## Feature 2: Backlinks Panel

### What It Does

When viewing a concept in the ontology, users can see all notes that mention that concept. This answers the question: "What have I written about this concept?"

### Expected Behavior

#### Location in UI

**Requirement 2.1: Backlinks Panel in Concept View**

When a user is viewing a concept (whether in graph view, list view, or concept details), they should see a "Backlinks" section that shows:

- Total count: "Referenced in 7 notes"
- List of notes that mention this concept
- Preview of each note showing where the concept is mentioned

**Where to display:**

- Option A: Add a new tab in concept details called "Backlinks" (alongside existing tabs)
- Option B: Add a collapsible section in the concept details panel
- Option C: Add a sidebar that can be toggled on/off
- Recommendation: Option A (tab) is cleanest and most discoverable

#### Backlinks List Display

**Requirement 2.2: Note Preview Cards**

Each note in the backlinks list should show:

- **Note title** (linked—clicking opens the note)
- **Date modified** (relative format: "2 hours ago", "3 days ago")
- **Context snippet** showing where the concept is mentioned:
  - Show 100 characters before and after the concept mention
  - Highlight the concept name in the snippet
  - If the concept is mentioned multiple times, show the first occurrence
- **Number of times mentioned** in that note (if more than once): "Mentioned 3 times"
- **Tags** from the note (as badges/chips)

**Visual design:**

- Each note should be a card or list item
- Cards should have hover effects to indicate they're clickable
- Most recent notes should appear first (sorted by note modified date)

**Requirement 2.3: Empty State**

When a concept has no backlinks:

- Show a friendly message: "No notes reference this concept yet"
- Show a suggestion: "Create a note about [Concept Name]" with a button that:
  - Creates a new note
  - Pre-fills title with concept name
  - Automatically adds concept name to note content
  - Opens the note for editing

#### Interaction Behaviors

**Requirement 2.4: Clicking a Note**

When user clicks a note in the backlinks list:

- Open the note in edit mode
- Scroll to the position where the concept is mentioned
- Briefly highlight the concept mention (flash effect)
- Keep the concept panel visible if possible (side-by-side view ideal)

**Requirement 2.5: Filtering and Sorting**

Users should be able to:

- **Sort by:**
  - Most recent (default)
  - Oldest first
  - Most mentions (notes that mention the concept most frequently)
  - Note title (alphabetical)
- **Filter by:**
  - Tags (if concept is mentioned in notes with specific tags)
  - Date range (show only notes from last week/month/year)

**Requirement 2.6: Bulk Actions**

If many notes reference a concept, users should be able to:

- "Open all in workspace" - Opens all backlinked notes as tabs
- "Export all" - Downloads all backlinked notes as a ZIP
- This is lower priority—can be deferred if time is short

#### Performance Requirements

**Requirement 2.7: Loading and Display**

- Backlinks should load within 1 second for concepts with <100 backlinks
- For concepts with 100+ backlinks, show first 20 and paginate or lazy-load
- Show count immediately: "Referenced in 147 notes" with spinner for details
- Never block the UI while loading backlinks

#### Integration with Existing Features

**Requirement 2.8: Concept Card/Badge Display**

In the ontology graph view and list view, concepts should show their backlink count:

- Display as a small badge/icon with number: "📝 7"
- Hovering should show tooltip: "Referenced in 7 notes"
- This helps users quickly see which concepts have associated notes

---

## Feature 3: Note Network Visualization

### What It Does

Shows users a visual graph of their notes and how they connect through shared concepts. If two notes both mention "Mitochondria", they're connected in the graph. This reveals the hidden structure of the user's knowledge.

### Expected Behavior

#### Access Point

**Requirement 3.1: New "Note Graph" View**

Add a new view mode in the Workspace area:

- Current workspace has: List view of notes (existing)
- Add: Graph view of notes (new)
- Switch between views with a toggle or tab
- Location: Top of workspace, next to existing workspace controls

**Alternative access:**

- Could be accessed from workspace toolbar: "Visualize Notes" button
- Could be a separate page: "Knowledge Graph" in main navigation

#### Graph Visualization

**Requirement 3.2: What the Graph Shows**

**Nodes:**

- Each note is represented as a node
- Node appearance:
  - **Label:** Note title
  - **Size:** Proportional to how many concepts the note references
    - Note with 1 concept = small node
    - Note with 20 concepts = large node
  - **Color:** Based on primary tag (if note has tags)
  - **Shape:** Rounded rectangle or circle
  - **Icon:** Small document icon

**Edges (connections between notes):**

- Two notes are connected if they reference the same concept(s)
- Edge appearance:
  - **Thickness:** Thicker if notes share more concepts
    - Share 1 concept = thin line
    - Share 5 concepts = thick line
  - **Label:** Shows shared concept count: "3 shared concepts"
  - **Color:** Subtle gray, lighter than nodes

**Concept indicators:**

- Optionally, show concepts as nodes too (different shape/color)
- This creates a bipartite graph: Notes connect to Concepts, Concepts connect to other Notes
- Recommendation: Start without concept nodes (simpler), add later if users request it

**Requirement 3.3: Layout and Organization**

- Use force-directed layout (like existing ontology graph)
- Densely connected notes should cluster together
- Isolated notes (no shared concepts) should appear on periphery
- Layout should be deterministic—same notes should appear in same positions between sessions
- Users can drag nodes to reposition them
- Positions should persist (save to database)

**Requirement 3.4: Interactive Features**

**Hovering over a node:**

- Highlight the note node
- Dim all other nodes
- Highlight connected notes (notes that share concepts)
- Show tooltip with:
  - Note title
  - Number of concepts referenced
  - List of top 3 concepts referenced
  - Created/modified dates

**Clicking a node:**

- Opens the note in edit mode (could be side panel or navigation)
- Keep graph visible if possible
- Highlight path from previous node to clicked node

**Clicking an edge:**

- Shows a popup listing the shared concepts
- Each concept is clickable → Opens concept in ontology view
- Example popup: "These notes share 3 concepts: Mitochondria, ATP, Cellular Respiration"

**Right-clicking a node:**

- Context menu with options:
  - "Open note"
  - "View in workspace"
  - "Show connected concepts" (opens a concept graph filtered to concepts this note mentions)
  - "Delete note"

**Requirement 3.5: Filtering and Search**

Users should be able to filter the graph:

- **Search bar:** Type to find notes by title
  - Matching notes are highlighted
  - Non-matching notes fade out
- **Tag filter:** Show only notes with specific tags
  - Multi-select tags
  - "Show notes tagged: Research, TODO"
- **Date filter:** Show notes from specific time period
  - "Last week", "Last month", "Last year", custom range
- **Concept filter:** Show only notes that reference specific concepts
  - Search and select concepts
  - "Show notes about: Mitochondria, Chloroplast"
  - This creates a focused subgraph

**Requirement 3.6: Zoom and Pan**

- Users can zoom in/out (mouse wheel or pinch gesture)
- Users can pan by clicking and dragging background
- "Fit to screen" button centers and zooms to show all notes
- "Reset zoom" button returns to default zoom level
- Zoom and pan state should persist

#### Information Displays

**Requirement 3.7: Statistics Panel**

Show a small info panel (corner or sidebar) with:

- Total note count in workspace
- Total notes visible in current view (after filters)
- Number of connected note clusters
- Most-referenced concepts (top 5)
- Density score: "Your notes reference an average of X concepts each"

**Requirement 3.8: Empty States**

When workspace has no notes:

- Show message: "No notes yet. Create your first note to see your knowledge graph."
- Show "Create Note" button

When notes have no concept links:

- Show message: "Your notes don't reference any concepts yet. Try writing about concepts from your ontology."
- Show tip: "Tip: Write naturally—the system will detect concept mentions automatically."

#### Performance Requirements

**Requirement 3.9: Loading and Rendering**

- Graph should load within 2 seconds for 500 notes
- For 500-1000 notes, show loading spinner with progress: "Loading notes... 342/876"
- For 1000+ notes, consider:
  - Virtualization (only render visible nodes)
  - Clustering (group distant notes into meta-nodes)
  - Pagination (show most relevant notes first)
- User should be able to interact while graph is still loading (don't block UI)

**Requirement 3.10: Smooth Animations**

- Node movements should be smooth (60fps)
- Transitioning between filters should animate (nodes fade in/out)
- Don't animate if it would slow performance below 30fps

---

## Feature 4: Quick Navigation

### What It Does

Provides seamless, fast navigation between notes, concepts, and the ontology. Users should never feel "stuck" in one view—they should always have a clear path to related information.

### Expected Behavior

#### Navigation from Note to Concept

**Requirement 4.1: Concept Links in Note Editor**

When editing a note:

- Detected concepts are highlighted (from Feature 1)
- Clicking a highlighted concept should:
  - **Option A:** Open concept in a side panel (slide out from right)
    - Panel shows concept details (name, description, properties)
    - Panel has "Open in ontology graph" button
    - Panel can be closed or minimized
    - User can continue editing note while panel is open
  - **Option B:** Navigate to concept in ontology view
    - User leaves note editor
    - Browser back button returns to note
  - **Recommendation:** Implement Option A for better flow

**Requirement 4.2: "Related Concepts" Section in Note**

At the bottom or side of note editor, show:

- "Related Concepts" section
- List all concepts mentioned in this note
- Each concept shows:
  - Concept name
  - Concept type/category (if applicable)
  - Number of times mentioned
  - Small preview icon or color indicator
- Clicking concept opens it (side panel or navigation)

#### Navigation from Concept to Note

**Requirement 4.3: Backlinks as Navigation (from Feature 2)**

In concept view, backlinks panel (Feature 2) serves as navigation:

- Each backlink is clickable
- Clicking opens that note
- If opening in same view, user can use back button to return
- If opening in side panel, concept view remains visible

**Requirement 4.4: "View in Note Graph" Button**

In concept view, add button:

- Label: "View notes about this concept"
- Action: Opens note graph (Feature 3) with filter applied
- Shows only notes that mention this specific concept
- Concept node could be highlighted or centered

#### Navigation Between Notes

**Requirement 4.5: "See Related Notes" Feature**

When viewing a note, show:

- "Related Notes" section (separate from "Related Concepts")
- Lists notes that share concepts with current note
- Sorted by relevance:
  - Notes sharing more concepts appear first
  - More recent notes appear before older notes
- Each related note shows:
  - Note title
  - Shared concept count: "Shares 3 concepts"
  - List of shared concepts (first 3)
  - Last modified date
- Clicking opens that note

**Requirement 4.6: Navigation History**

Users should be able to navigate backward and forward through their exploration:

- Browser back/forward buttons should work correctly
- Keyboard shortcuts: Alt+Left (back), Alt+Right (forward)
- Show breadcrumb trail at top:
  - "Ontology > Mitochondria > Note: Cell Biology Basics > ATP"
  - Each breadcrumb is clickable

#### Global Navigation Features

**Requirement 4.7: Enhanced Spotlight Search**

Enhance existing Cmd+K spotlight search to include:

- Search across concepts AND notes simultaneously
- Results grouped:
  - "Concepts" section
  - "Notes" section
- Show result type icon (concept vs note)
- Show context: Where search term appears
- Keyboard navigation: Arrow keys to move, Enter to open
- Typing narrows results in real-time

**Requirement 4.8: "Jump to Related" Command**

Add keyboard shortcut (e.g., Cmd+J):

- Opens quick-pick menu
- Shows items related to current context:
  - If viewing concept: Shows backlinked notes
  - If viewing note: Shows related concepts and notes
- Type to filter
- Enter to navigate
- This is like Cmd+K but context-aware

#### Visual Navigation Aids

**Requirement 4.9: Navigation Context Indicators**

When users navigate from one place to another, show where they came from:

- Banner at top: "Viewing this concept from note: [Note Title]"
- "Return to note" button in banner
- Banner auto-dismisses after 10 seconds or user closes it

**Requirement 4.10: Mini-Map or Overview**

Optional (nice-to-have):

- Small overview showing user's current position in knowledge graph
- "You are here" indicator
- Clicking different areas navigates there
- Could be toggled on/off

---

## Feature 5: Smart Note Templates

### What It Does

When creating a note about a specific concept, the system can pre-populate the note with relevant structure and content based on the concept type.

### Expected Behavior

#### Template Triggers

**Requirement 5.1: "Create Note About Concept" Action**

Add action in concept view:

- Button: "Create note about this concept"
- Location: In concept details panel or right-click menu
- Action: Creates new note with smart template applied

**Requirement 5.2: Template Selection**

When user clicks "Create note about concept":

- System determines concept type (if concept has a type/category)
- Selects appropriate template automatically
- User can override: "Use different template" dropdown
- Templates include:
  - "General concept note" (default)
  - "Research note"
  - "Meeting note"
  - "Literature review"
  - Custom templates user has created

#### Template Content

**Requirement 5.3: Auto-Populated Fields**

New note should be pre-filled with:

- **Title:** Concept name
  - Example: If concept is "Mitochondria", title is "Notes on Mitochondria"
  - User can edit immediately
- **First line:** Concept name as header
  - "# Mitochondria"
- **Concept description:** Copied from concept as starting point
  - Prefixed with: "## Definition"
  - User can modify or delete
- **Automatic link:** Note explicitly mentions the concept
  - This triggers auto-linking (Feature 1)
- **Tags:** Automatically add relevant tags
  - Add tag matching concept name
  - Add tag matching concept type/category
  - Example: Concept "Mitochondria" of type "Cell Organelle"
    - Tags: "Mitochondria", "Cell Organelle"

**Requirement 5.4: Template Structure Sections**

Include helpful section headers:

- "## Definition" (with concept description)
- "## Key Points"
- "## Related Concepts"
- "## Questions"
- "## References"

User can:

- Delete any section
- Add more sections
- Reorder sections
- These are just starting points

**Requirement 5.5: Related Concepts Section**

Auto-populate "## Related Concepts" with:

- List of concepts directly related to this concept in ontology
- Format: "- [[Concept Name]] - relationship type"
- Example:

  ```
  ## Related Concepts
  - [[ATP]] - produces
  - [[Cellular Respiration]] - part of
  - [[Cell Organelle]] - is a
  ```

- Each [[Concept Name]] should be detected and linked (Feature 1)

#### Template Management

**Requirement 5.6: Custom Templates**

Users should be able to:

- Create their own note templates
- Associate templates with concept types
- Example: "When creating note about a 'Disease' concept, use my 'Disease Research Template'"
- Templates stored per workspace or per user
- Templates can include:
  - Fixed text
  - Variables: {{concept_name}}, {{concept_description}}, {{related_concepts}}
  - Section structures

**Requirement 5.7: Template Library**

Provide built-in templates:

- **Research Note:** Includes hypothesis, methodology, findings sections
- **Meeting Note:** Includes date, attendees, decisions, action items
- **Literature Review:** Includes citation, summary, key findings, critique
- **Concept Exploration:** Includes definition, examples, counterexamples, questions
- **Quick Note:** Minimal template, just title and concept mention

#### User Experience

**Requirement 5.8: Template Preview**

Before creating note with template:

- Show preview of what note will look like
- "Create with this template" button
- "Choose different template" button
- "Create blank note" option

**Requirement 5.9: Template Learning**

Optional (nice-to-have):

- System learns user's note-taking patterns
- Suggests templates based on:
  - Which templates user uses most
  - What sections user typically adds
  - How user structures notes about similar concepts
- "We noticed you usually add a 'Questions' section. Include it automatically?" prompt

---

## User Workflows

### Workflow 1: Researcher Building Knowledge

**Scenario:** Dr. Sarah is researching cellular biology and building an ontology of cell components.

**Steps:**

1. Sarah creates a concept "Mitochondria" in her ontology
2. She adds properties: description, relationships to "ATP" and "Cell Organelle"
3. Later, she's reading a paper and wants to take notes
4. She creates a note: "Understanding Cellular Energy Production"
5. In the note, she writes: "Mitochondria are the powerhouses of the cell. They produce ATP through cellular respiration..."
6. **System detects "Mitochondria", "ATP", and "cellular respiration" are concepts**
7. **These words are subtly highlighted in her note**
8. Sarah hovers over "Mitochondria" and sees a tooltip with the concept info
9. She clicks it and a side panel opens showing the full concept details
10. In the side panel, she sees "Referenced in 3 notes" (her backlinks)
11. She clicks one of those backlinks and navigates to an older note
12. **She discovers she wrote about mitochondria 2 months ago and forgot!**
13. She copies useful information from the old note to the new note
14. Both notes now link to "Mitochondria" concept

**Key Moments:**

- Auto-detection saved her from manually creating links
- Backlinks helped her discover forgotten knowledge
- Side panel kept her in flow (didn't navigate away)

### Workflow 2: Team Collaboration

**Scenario:** A team of 5 researchers sharing an ontology about medical conditions.

**Steps:**

1. Alice creates a concept "Type 2 Diabetes"
2. Bob writes a note about recent research on diabetes treatment
3. **System automatically links Bob's note to Alice's concept**
4. Alice views the "Type 2 Diabetes" concept
5. She sees in the Backlinks panel: "Referenced in 4 notes"
6. One of those notes is Bob's (she didn't know he wrote it)
7. She reads Bob's note and learns about new treatment approaches
8. She creates her own note: "Treatment Protocol Update"
9. Her note mentions "Type 2 Diabetes" and "Insulin Resistance" (another concept)
10. Carol views the note graph and sees a cluster forming
11. **She discovers Alice and Bob's notes are connected through shared concepts**
12. The team uses this to identify collaboration opportunities

**Key Moments:**

- Automatic linking enabled discovery across team members
- Backlinks made work visible to others
- Note graph revealed team knowledge structure

### Workflow 3: Student Learning

**Scenario:** Marcus is a medical student studying for exams using Eidos.

**Steps:**

1. Marcus imports a pre-built medical ontology with 500+ concepts
2. He starts creating study notes for each system (cardiovascular, respiratory, etc.)
3. As he writes, concepts are auto-linked
4. He writes: "The heart pumps blood through the circulatory system"
5. **"Heart", "blood", and "circulatory system" are all linked**
6. Before his exam, he uses the note graph to review
7. He filters to show only notes tagged "High Priority"
8. **He sees which concepts he's written most about (big nodes) and which he's neglected (small nodes)**
9. He notices "Kidney Function" has no connected notes
10. He clicks "Create note about this concept" from the kidney concept
11. A template note is created with sections: Definition, Function, Related Organs, Clinical Significance
12. He fills it in, and now his note graph is more complete

**Key Moments:**

- Auto-linking made study notes automatically structured
- Note graph revealed knowledge gaps visually
- Smart templates guided him in creating comprehensive notes

### Workflow 4: Writer Organizing Novel Ideas

**Scenario:** Jennifer is writing a fantasy novel and using Eidos to track characters, locations, and plot points.

**Steps:**

1. Jennifer has concepts for characters: "Elara" (protagonist), "Mordain" (antagonist)
2. She has concepts for locations: "Crystal Caverns", "Royal Palace"
3. She writes scene notes in her workspace
4. Scene 1 note: "Elara discovers the Crystal Caverns beneath the Royal Palace..."
5. **System links to all three concepts automatically**
6. Later, she's writing Scene 15 and can't remember if Elara has been to the palace before
7. She views "Royal Palace" concept
8. Backlinks show: "Referenced in Scene 1, Scene 3, Scene 7"
9. She quickly reviews those scenes
10. She uses the note graph to visualize which scenes share characters/locations
11. **She discovers Scenes 3, 7, and 11 all involve Elara and Mordain but she never wrote their confrontation!**
12. She creates a new scene filling that plot gap

**Key Moments:**

- Backlinks served as continuity checker
- Note graph revealed plot structure and gaps
- Creative work benefits from knowledge management tools

---

## Success Criteria

### Functional Success Metrics

**Must Have (MVP):**

- ✅ Concepts are detected in notes with >90% accuracy
- ✅ Users can see backlinks for any concept
- ✅ Users can click from note to concept and back
- ✅ Note graph displays and is interactive
- ✅ All features work on desktop browsers (Chrome, Firefox, Safari, Edge)
- ✅ No performance degradation in existing features

**Should Have:**

- ✅ Concept detection happens within 2 seconds
- ✅ Backlinks load within 1 second
- ✅ Note graph loads within 3 seconds for 500 notes
- ✅ Mobile responsive (works on tablets and phones)
- ✅ Keyboard shortcuts work for navigation

**Nice to Have:**

- ✅ Custom templates can be created by users
- ✅ Note graph supports 1000+ notes without lag
- ✅ Automatic template suggestions based on learning
- ✅ Export note graph as image

### User Experience Success Metrics

**Qualitative:**

- Users report "discovering connections I didn't know existed"
- Users feel navigation between notes and concepts is seamless
- Users don't need documentation to understand the features
- Users describe the system as "intelligent" or "helpful"

**Quantitative:**

- 80% of active users use backlinks feature within first week
- 60% of users view note graph at least once per session
- Average time to navigate from concept to related note: <5 seconds
- Users create 30% more notes after feature launch (indicates more engagement)

### Technical Success Metrics

**Performance:**

- Concept detection: <2 seconds for 10,000 word notes
- Backlinks query: <1 second for concepts with <100 backlinks
- Note graph render: <3 seconds for 500 notes
- Page load time: No increase in existing page load times
- Database queries: <5 additional queries per page load

**Reliability:**

- Zero false positives in concept detection (no incorrect links)
- <1% false negatives (missing real concept mentions is acceptable if rare)
- 99.9% uptime for linking service
- Zero data loss (links never corrupted or lost)

**Scalability:**

- System handles workspaces with 10,000 notes
- System handles ontologies with 5,000 concepts
- Concept detection works with concepts in any language (UTF-8 support)
- Note graph remains usable with 1000+ notes (even if slower)

---

## Out of Scope

### Features NOT Included in Month 1

These are valuable but deferred to later:

**Advanced NLP Features:**

- ❌ Semantic matching (detecting synonyms or related terms)
- ❌ Fuzzy matching (typo tolerance)
- ❌ Context-aware linking (same word means different things in different contexts)
- ❌ Automatic relationship extraction (detecting "causes", "prevents", etc.)
- ❌ Sentiment analysis of notes
- ❌ Automatic summarization

**AI/ML Features:**

- ❌ Concept suggestion (suggesting new concepts based on note content)
- ❌ Note clustering (grouping similar notes automatically)
- ❌ Anomaly detection (finding notes that don't fit patterns)
- ❌ Predictive linking (suggesting which concept user might want to link next)

**Advanced Visualization:**

- ❌ 3D graph visualization
- ❌ Timeline view of note creation
- ❌ Heatmap of concept usage
- ❌ Animated graph showing knowledge growth over time

**Collaboration Features:**

- ❌ Real-time collaborative graph editing
- ❌ Comments on links
- ❌ Link voting/quality scoring
- ❌ Suggested links that team members can approve

**Import/Export:**

- ❌ Import notes with links from Obsidian
- ❌ Import from Roam Research
- ❌ Export note graph to Neo4j
- ❌ Export to knowledge graph formats (RDF with note links)

**Advanced Templates:**

- ❌ Conditional template logic (if concept has property X, include section Y)
- ❌ Template marketplace (share templates with other users)
- ❌ Multi-step template wizards

---

## Acceptance Criteria

### Definition of Done

A feature is "done" when:

1. **Implemented:**
   - Code is written and peer-reviewed
   - Unit tests pass (>80% coverage)
   - Integration tests pass
   - No critical or high-priority bugs

2. **Tested:**
   - QA has tested happy paths
   - QA has tested edge cases
   - Performance meets requirements
   - Works on all supported browsers
   - Works on mobile (responsive)

3. **Documented:**
   - User-facing documentation exists
   - Tooltips and help text in UI
   - Release notes written
   - API documentation (if applicable)

4. **Deployed:**
   - Feature flag enabled
   - Deployed to production
   - Monitoring in place
   - Rollback plan exists

### Feature-Specific Acceptance Criteria

**Feature 1: Automatic Concept Detection**

- [ ] Concept names are detected case-insensitively
- [ ] Whole word matching works correctly
- [ ] No false positives in 100 test notes
- [ ] Detected concepts are visually highlighted
- [ ] Clicking highlighted text opens concept details
- [ ] Auto-linking can be disabled per note
- [ ] Performance: <2 seconds for 10,000 word notes

**Feature 2: Backlinks Panel**

- [ ] Backlinks panel appears in concept view
- [ ] Shows correct count of referencing notes
- [ ] Each backlink shows note title, date, preview
- [ ] Clicking backlink opens note
- [ ] Empty state appears when no backlinks exist
- [ ] Sorting and filtering work correctly
- [ ] Performance: <1 second to load 100 backlinks

**Feature 3: Note Network Visualization**

- [ ] Note graph displays all notes as nodes
- [ ] Edges show shared concepts correctly
- [ ] Force-directed layout works smoothly
- [ ] Hovering highlights connected notes
- [ ] Clicking node opens note
- [ ] Filtering by tag/date/concept works
- [ ] Search highlights matching notes
- [ ] Performance: <3 seconds for 500 notes
- [ ] Zoom and pan work smoothly

**Feature 4: Quick Navigation**

- [ ] Clicking concept in note opens concept panel
- [ ] Clicking backlink opens note
- [ ] Related notes section shows correct notes
- [ ] Breadcrumb trail shows navigation path
- [ ] Browser back button works correctly
- [ ] Keyboard shortcuts work (Cmd+K, Cmd+J)
- [ ] Navigation never results in dead ends

**Feature 5: Smart Note Templates**

- [ ] "Create note about concept" button exists
- [ ] Template is pre-filled with concept info
- [ ] Related concepts are auto-populated
- [ ] User can choose different template
- [ ] Custom templates can be created
- [ ] Template preview shows before creation
- [ ] All templates work with auto-linking

---

## Dependencies and Assumptions

### Dependencies

**Technical Dependencies:**

- Existing note editor must support highlighting text dynamically
- Existing graph visualization (Cytoscape.js) must support note graphs
- Database must support efficient full-text search
- SignalR must support real-time link updates (if multiple users)

**Feature Dependencies:**

- Requires existing Notes/Workspace system
- Requires existing Ontology/Concepts system
- Requires existing user authentication
- Requires existing permission system

### Assumptions

**User Behavior Assumptions:**

- Users write notes in natural language (not structured data)
- Users use consistent terminology (concept names match note language)
- Users want connections revealed automatically
- Users won't create thousands of single-character concept names
- Users understand graph visualizations (or can learn quickly)

**Technical Assumptions:**

- UTF-8 encoding for all text (supports all languages)
- Notes are stored as markdown text in database
- Concepts have unique names within an ontology
- Database can handle full-text search efficiently
- Browser supports modern JavaScript (ES6+)

**Business Assumptions:**

- Feature will increase user engagement
- Feature differentiates Eidos from competitors
- Users won't abuse auto-linking (e.g., creating spammy concepts)
- Performance requirements are achievable with current infrastructure

---

## Risk Analysis

### High-Priority Risks

**Risk 1: Performance Degradation**

- **Likelihood:** Medium
- **Impact:** High
- **Description:** Concept detection on every note save could slow down the editor
- **Mitigation:**
  - Implement debouncing (only analyze after user stops typing for 2 seconds)
  - Use background processing for large notes
  - Cache concept names in memory
  - Optimize database queries with proper indexes

**Risk 2: False Positive Links**

- **Likelihood:** Medium
- **Impact:** Medium
- **Description:** System links common words that happen to be concept names
- **Mitigation:**
  - Implement minimum word length requirement (3 characters)
  - Use whole-word matching only
  - Allow users to disable specific links
  - Don't link words inside code blocks or URLs
  - Provide "Report false link" feedback mechanism

**Risk 3: Overwhelming Backlinks**

- **Likelihood:** Low
- **Impact:** Medium
- **Description:** Popular concepts might have 1000+ backlinks, unusable
- **Mitigation:**
  - Paginate backlinks (show 20 at a time)
  - Provide strong filtering and sorting
  - Show summary statistics first
  - Allow "export all" for bulk access

**Risk 4: Graph Visualization Performance**

- **Likelihood:** Medium
- **Impact:** Medium
- **Description:** Note graph with 1000+ notes might be slow or crash browser
- **Mitigation:**
  - Implement lazy loading (render only visible nodes)
  - Use clustering for distant nodes
  - Provide warning: "Large graph, may be slow"
  - Add pagination or filtering by default for large graphs

### Medium-Priority Risks

**Risk 5: User Confusion**

- **Likelihood:** Medium
- **Impact:** Low
- **Description:** Users don't understand auto-linking or how to use backlinks
- **Mitigation:**
  - Provide onboarding tutorial
  - Add contextual help tooltips
  - Create video walkthroughs
  - Show examples in documentation

**Risk 6: Database Growth**

- **Likelihood:** High
- **Impact:** Low
- **Description:** Link tables grow large, increasing storage costs
- **Mitigation:**
  - Archive old links (soft delete)
  - Implement data retention policies
  - Monitor database size
  - Optimize indexes

---

## Testing Strategy

### Test Types

**Unit Tests:**

- Test concept detection algorithm with various inputs
- Test link creation and deletion
- Test graph data structure building
- Test navigation path generation
- Target: >80% code coverage

**Integration Tests:**

- Test note save → concept detection → link creation flow
- Test concept rename → reanalyze notes flow
- Test backlinks display with real database
- Test note graph with sample data
- Test navigation between all features

**UI/Component Tests:**

- Test note editor with concept highlighting
- Test backlinks panel rendering
- Test note graph interaction (hover, click, drag)
- Test template application
- Test responsive design on different screen sizes

**Performance Tests:**

- Concept detection with 10,000 word notes
- Backlinks loading with 500 linked notes
- Note graph rendering with 1000 nodes
- Concurrent users analyzing notes simultaneously
- Database query performance under load

**End-to-End Tests:**

- Complete user workflows (from scenarios above)
- Test all navigation paths
- Test error handling and recovery
- Test with different browsers and devices

### Test Data Requirements

**Sample Ontologies:**

- Small (10 concepts) for quick tests
- Medium (100 concepts) for typical use
- Large (1000 concepts) for stress testing
- Multi-language (concepts in different languages)

**Sample Notes:**

- Empty notes
- Short notes (< 100 words)
- Medium notes (500 words)
- Long notes (10,000 words)
- Notes with many concepts (20+)
- Notes with no concepts
- Notes with special characters
- Notes in different languages

**Sample Users:**

- Single user workspace
- Multi-user workspace (collaboration)
- User with permissions restrictions
- Guest/read-only user

---

## Documentation Requirements

### User Documentation

**Feature Guides:**

- "Understanding Auto-Linking"
- "Using Backlinks to Discover Knowledge"
- "Navigating the Note Graph"
- "Creating Smart Templates"
- "Best Practices for Concept Naming"

**Quick Start:**

- 5-minute video: "How Smart Linking Works"
- Interactive tutorial (first-time user experience)
- Cheat sheet: Common workflows

**FAQ:**

- "Why isn't my concept being detected?"
- "How do I disable auto-linking?"
- "Can I create custom templates?"
- "How are notes connected in the graph?"

### Developer Documentation

**Architecture:**

- Concept detection algorithm explanation
- Database schema for linking
- Graph data structure
- API endpoints (if any)

**Integration Guide:**

- How to extend concept detection
- How to add new template types
- How to customize graph layout

---

## Support and Maintenance

### Monitoring

**Key Metrics to Track:**

- Concept detection success rate
- Average detection time
- Backlinks query performance
- Note graph load time
- Error rates in linking service
- User adoption rates (% using features)

**Alerts:**

- Detection taking >5 seconds (performance issue)
- High error rate in link creation (data issue)
- Graph not loading (service down)
- Database connection errors

### Maintenance Tasks

**Daily:**

- Monitor performance metrics
- Review error logs
- Check user feedback

**Weekly:**

- Analyze false positive/negative reports
- Review slow queries
- Update concept mention cache

**Monthly:**

- Archive old links (if needed)
- Optimize database indexes
- Review and improve detection algorithm
- Update documentation based on user questions

---

## Timeline and Milestones

### Week 1: Foundation

- Database schema design and migration
- Concept detection algorithm (core logic)
- Basic linking repository
- Unit tests for detection

**Milestone:** Concept detection working in isolation

### Week 2: Note Integration

- Integrate detection into note editor
- Visual highlighting of concepts
- Link creation on note save
- Click to view concept (side panel)

**Milestone:** Users can see linked concepts in notes

### Week 3: Backlinks and Navigation

- Backlinks panel in concept view
- Related notes section in note editor
- Quick navigation features
- Search enhancements

**Milestone:** Bidirectional navigation working

### Week 4: Visualization and Templates

- Note graph view (basic version)
- Graph interactions (hover, click, zoom)
- Smart templates for note creation
- Filtering and sorting
- Final testing and bug fixes

**Milestone:** All features complete and tested

---

## Questions for Stakeholders

Before starting implementation, clarify:

1. **Performance Trade-offs:**
   - Should concept detection be synchronous (immediate but slower) or asynchronous (delayed but faster)?
   - What's the acceptable delay for backlinks loading?

2. **UI/UX Decisions:**
   - Should clicking a concept in a note open a side panel or navigate away?
   - Should note graph be a separate view or integrated into workspace?
   - What's the default sorting for backlinks (chronological or relevance)?

3. **Scope Decisions:**
   - Is custom template creation in scope for v1 or defer to v2?
   - Are we supporting multi-language concept detection in v1?
   - Do we need real-time collaboration for linking (if multiple users editing)?

4. **Business Rules:**
   - Should auto-linking work for guest/read-only users?
   - Are there limits on links per note (performance reasons)?
   - Should deleted concepts remove their links or preserve them?

5. **Success Metrics:**
   - What's the minimum adoption rate to consider this successful?
   - How will we measure "discovery of hidden connections"?
   - What's our rollback plan if performance is poor?

---

## Conclusion

This document describes a comprehensive smart linking system that will transform Eidos from a dual-purpose tool (ontology + notes) into a unified knowledge management platform. By automatically detecting connections and visualizing relationships, we empower users to think and write naturally while the system reveals the structure of their knowledge.

The features are designed to be intuitive, performant, and valuable across multiple use cases: academic research, creative writing, team collaboration, and personal knowledge management.

Success depends on: accurate detection, seamless navigation, performant visualization, and thoughtful UX that doesn't interrupt the user's flow.

**Next Steps:**

1. Stakeholder review and sign-off
2. Technical design document (how to implement)
3. Sprint planning (breaking into tasks)
4. Development kickoff

---

**Document Version:** 1.0  
**Last Updated:** November 19, 2025  
**Author:** Product Requirements Team  
**Review Status:** Draft - Pending Approval
