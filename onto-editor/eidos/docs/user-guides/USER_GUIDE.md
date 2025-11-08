# Ontology Builder - User Guide

Complete guide to using the Ontology Builder application.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Interface Overview](#interface-overview)
3. [Creating Your First Ontology](#creating-your-first-ontology)
4. [Working with Concepts](#working-with-concepts)
5. [Managing Relationships](#managing-relationships)
6. [View Modes](#view-modes)
7. [Global Search](#global-search)
8. [Importing and Exporting](#importing-and-exporting)
9. [Keyboard Shortcuts](#keyboard-shortcuts)
10. [Tips and Best Practices](#tips-and-best-practices)
11. [Troubleshooting](#troubleshooting)

---

## Getting Started

### Launching the Application

1. Open your terminal
2. Navigate to the onto-editor directory
3. Run `dotnet run` or `dotnet watch run`
4. Open your web browser to `http://localhost:5000`

### First Steps

When you first open the application, you'll see the home page with an option to create a new ontology.

---

## Interface Overview

### Main Layout

The application interface consists of three main areas:

1. **Sidebar** (Left): Shows recent ontologies for quick access
2. **Main Content Area** (Center): Displays your ontology in various view modes
3. **Action Buttons** (Top): Context-sensitive buttons for adding concepts, relationships, etc.

### Sidebar

- **Home Icon**: Click to return to the home page
- **Recent Ontologies**: List of your 10 most recently updated ontologies
  - Shows concept count
  - Shows relationship count
  - Shows last update time

---

## Creating Your First Ontology

### Step 1: Create the Ontology

1. Click **"Create New Ontology"** on the home page
2. Enter a name for your ontology (e.g., "My First Ontology")
3. Click **"Create"**

### Step 2: Configure Settings (Optional)

Click the settings icon (⚙️) or press `Ctrl+,` to configure:

- **Namespace**: URI for your ontology (e.g., `http://example.org/ontology#`)
- **Author**: Your name or organization
- **License**: License information (e.g., "CC BY 4.0")
- **Tags**: Comma-separated tags for categorization
- **Notes**: Additional information about your ontology

---

## Working with Concepts

### What is a Concept?

A concept represents a class, entity, or idea in your ontology. Examples: "Person", "Organization", "Event".

### Adding a New Concept

**Method 1: Using the Button**
1. Click **"Add Concept"** button
2. Fill in the form
3. Click **"Save"**

**Method 2: Using Keyboard**
- Press `Ctrl+K` to open the concept form

### Concept Form Fields

- **Name** (Required): The concept name (e.g., "Person")
- **Category**: Grouping category (e.g., "Core", "Extended")
- **Type**: Specific type within the category
- **Description**: Detailed explanation of the concept
- **Simple Explanation**: Brief, plain-language description
- **Examples**: Real-world examples
- **Color**: Visual color for graph view (click the color picker)

### Using Templates

Templates help you quickly create concepts with predefined categories and types:

1. Switch to **Templates** view (`Alt+P`)
2. Click **"Add Template"**
3. Define your template:
   - Category (e.g., "Agent")
   - Type (e.g., "Person")
   - Description
   - Examples
   - Color
4. When creating concepts, select from your templates in the dropdown

### Editing Concepts

1. Find the concept in List or Graph view
2. Click the **pencil icon** (✏️)
3. Modify the fields
4. Click **"Save"**

### Duplicating Concepts

To create a similar concept:
1. Click the **copy icon** (📋) next to any concept
2. Modify the duplicated concept as needed
3. Save

### Deleting Concepts

1. Click the **trash icon** (🗑️) next to the concept
2. Confirm the deletion in the dialog
3. Note: This will also delete related relationships

---

## Managing Relationships

### What is a Relationship?

Relationships define connections between concepts. Examples: "subClassOf", "hasProperty", "relatedTo".

### Adding a Relationship

**Method 1: Using the Button**
1. Click **"Add Relationship"**
2. Select source concept
3. Select target concept
4. Choose or enter relationship type
5. Optional: Add a custom label
6. Click **"Save"**

**Method 2: Using Keyboard**
- Press `Ctrl+R` to open the relationship form

### Common Relationship Types

- **subClassOf**: Indicates inheritance (e.g., "Dog" subClassOf "Animal")
- **hasProperty**: Indicates possession
- **partOf**: Indicates composition
- **relatedTo**: General association
- **equivalentTo**: Indicates equivalence

### Editing Relationships

1. Find the relationship in List view
2. Click the **pencil icon** (✏️)
3. Modify the fields
4. Click **"Save"**

### Duplicating Relationships

1. Click the **copy icon** (📋) next to any relationship
2. Modify if needed
3. Save

### Deleting Relationships

1. Click the **trash icon** (🗑️)
2. Confirm the deletion

---

## View Modes

The application offers five view modes. Switch between them using the buttons or keyboard shortcuts.

### Graph View (`Alt+G`)

**Interactive visual representation of your ontology.**

Features:
- **Force-directed layout**: Nodes repel each other for clarity
- **Drag and drop**: Click and drag nodes to reposition
- **Zoom**: Use mouse wheel to zoom in/out
- **Pan**: Click and drag the background to move around
- **Color-coded**: Nodes use the colors you assigned to concepts
- **Labeled edges**: Relationships show their type

Tips:
- Drag nodes to organize them logically
- Zoom out to see the big picture
- Zoom in to focus on specific areas

### List View (`Alt+L`)

**Tabular view with search and sort capabilities.**

Features:
- **Search bar**: Filter concepts by name, description, examples, or category
- **Sort options**:
  - By Name (alphabetical)
  - By Category
  - By Created Date
- **Quick actions**: Edit, duplicate, or delete from the list
- **Relationship badges**: See connections at a glance

Search Tips:
- Press `Ctrl+F` to focus the search box
- Search is case-insensitive
- Matches against all concept fields
- Results update as you type

### TTL View (`Alt+T`)

**View and export your ontology in Turtle (TTL) format.**

Features:
- **Standard RDF format**: Compatible with other ontology tools
- **Copy button**: Copy TTL to clipboard
- **Pretty-printed**: Human-readable formatting
- **Includes all concepts and relationships**

Use Cases:
- Export for use in other tools (Protégé, TopBraid, etc.)
- Share with collaborators
- Version control
- Documentation

### Notes View (`Alt+N`)

**Add and edit markdown notes about your ontology.**

Features:
- **Markdown editor**: Supports standard markdown syntax
- **Large text area**: Plenty of space for detailed notes
- **Auto-save**: Changes save automatically

What to Include:
- Purpose and scope of the ontology
- Design decisions and rationale
- Usage instructions
- Change log
- Known limitations
- Future plans

### Templates View (`Alt+P`)

**Manage custom concept templates.**

Features:
- **Template library**: See all your custom templates
- **Add new templates**: Create reusable concept patterns
- **Edit templates**: Modify existing templates
- **Delete templates**: Remove unused templates
- **Color visualization**: See template colors at a glance

---

## Global Search

**Quickly find anything in your ontology with the Global Search feature.**

### Opening Global Search

Press **`Cmd+Shift+Space`** (Mac) or **`Ctrl+Shift+Space`** (Windows/Linux) from any view to open the search dialog.

### What Can You Search?

Global Search searches across **all entities** in your ontology:

- **Concepts**: Name, definition, explanation, examples, category
- **Relationships**: Relationship type, description
- **Individuals**: Name, label, description

### Using Global Search

1. **Open the search**: Press `Cmd+Shift+Space` (or `Ctrl+Shift+Space`)
2. **Start typing**: Results appear instantly as you type
3. **Navigate results**: Use `↑` and `↓` arrow keys to move between results
4. **Select a result**: Press `Enter` or click to jump to the item
5. **Close the search**: Press `Esc` or click outside the dialog

### Features

- **Real-time search**: Results update as you type (with 300ms debounce)
- **Grouped results**: Results are organized by type (Concepts, Relationships, Individuals)
- **Visual icons**: Each result type has a distinct icon
- **Context preview**: See definitions and descriptions in the results
- **Match indicators**: Shows which field matched your search
- **Keyboard navigation**: Full keyboard support for quick access
- **Spotlight-style UI**: Clean, modern interface inspired by macOS Spotlight

### Search Tips

- **Be specific**: More specific search terms yield better results
- **Partial matches work**: Type "auth" to find "Authentication"
- **Case-insensitive**: Search is not case-sensitive
- **Use keyboard**: Navigate with arrow keys for faster searching
- **Quick access**: The search dialog automatically regains focus after closing, so you can immediately search again

### After Selecting a Result

When you select a search result:
- The view switches to **List View** automatically
- The selected item is highlighted
- A confirmation toast appears showing what you navigated to

### Example Workflow

1. Press `Cmd+Shift+Space`
2. Type "user"
3. See all concepts, relationships, and individuals containing "user"
4. Arrow down to "User Authentication" concept
5. Press `Enter`
6. View switches to List View with "User Authentication" selected

---

## Importing and Exporting

### Importing TTL Files

1. Click **"Import TTL"** or press `Ctrl+I`
2. Paste your TTL content into the text area
3. Click **"Import"**
4. The system will parse and import concepts and relationships

Supported Features:
- RDF classes (become Concepts)
- Object properties (become Relationships)
- Labels and comments
- Standard TTL/Turtle syntax

### Exporting

The Ontology Builder supports exporting your ontology in multiple formats for different use cases.

**Using the Export Dialog (Recommended):**

1. Click the **"Export"** button in the ontology header
2. Select your desired export format:
   - **TTL (Turtle)** - Standard RDF format for ontology tools
   - **JSON** - Structured data for APIs and web applications
   - **CSV - Concepts Only** - Spreadsheet with just concepts
   - **CSV - Relationships Only** - Spreadsheet with just relationships
   - **CSV - Full Ontology** - Complete export including metadata
3. Review the preview of the exported content
4. Click **"Export & Copy to Clipboard"**
5. Paste into your destination application or save to a file

**Export Format Details:**

| Format | Best For | File Extension |
|--------|----------|----------------|
| TTL (Turtle) | Protégé, TopBraid, other ontology tools | `.ttl` |
| JSON | API integration, web applications, data processing | `.json` |
| CSV - Concepts | Excel analysis, concept documentation | `.csv` |
| CSV - Relationships | Relationship analysis, documentation | `.csv` |
| CSV - Full | Complete backup, external processing | `.csv` |

**Alternative: TTL View Export:**
1. Switch to TTL view (`Alt+T`)
2. Click **"Copy TTL"** or **"Download"**
3. Choose your RDF format (Turtle, RDF/XML, N-Triples, etc.)

**Tips:**
- Use **JSON** for integrating with other applications or APIs
- Use **CSV** formats for analysis in spreadsheet applications
- Use **TTL** for standard ontology interchange and tool compatibility
- All exports include complete ontology metadata when available

---

## Keyboard Shortcuts

Press `?` (question mark) anytime to see the keyboard shortcuts dialog.

### General

| Shortcut | Action |
|----------|--------|
| `?` | Show keyboard shortcuts |
| `Esc` | Close dialogs |

### Navigation

| Shortcut | Action |
|----------|--------|
| `Alt+G` | Switch to Graph view |
| `Alt+L` | Switch to List view |
| `Alt+T` | Switch to TTL view |
| `Alt+N` | Switch to Notes view |
| `Alt+P` | Switch to Templates view |

### Editing

| Shortcut | Action |
|----------|--------|
| `Ctrl+K` | Add new concept |
| `Ctrl+R` | Add new relationship |
| `Ctrl+I` | Import TTL |
| `Ctrl+,` | Open ontology settings |

### Search

| Shortcut | Action |
|----------|--------|
| `Cmd+Shift+Space` (Mac)<br>`Ctrl+Shift+Space` (Windows/Linux) | Open Global Search |
| `↑` / `↓` | Navigate search results |
| `Enter` | Select search result |
| `Esc` | Close search dialog |
| `Ctrl+F` | Focus search (in List view) |

### Tips for Power Users

- Learn the view mode shortcuts first (`Alt+G`, `Alt+L`, etc.) for quick navigation
- Use `Ctrl+K` and `Ctrl+R` to quickly add concepts and relationships without reaching for the mouse
- Press `?` if you forget a shortcut

---

## Tips and Best Practices

### Ontology Design

1. **Start Simple**: Begin with core concepts, add detail later
2. **Use Clear Names**: Choose descriptive, unambiguous concept names
3. **Document as You Go**: Add descriptions and examples to concepts
4. **Logical Grouping**: Use categories to organize related concepts
5. **Consistent Naming**: Establish naming conventions (e.g., PascalCase for concepts)

### Concept Organization

1. **Color Coding**: Use colors to group related concepts visually
2. **Categories**: Group concepts into logical categories
3. **Examples**: Always provide examples to clarify meaning
4. **Simple Explanations**: Write brief explanations for non-experts

### Relationship Best Practices

1. **Standard Types**: Use standard relationship types when possible (subClassOf, partOf, etc.)
2. **Custom Labels**: Add labels to clarify non-standard relationships
3. **Direction Matters**: Remember the direction: Source → Type → Target
4. **Avoid Redundancy**: Don't create duplicate relationships

### Working Efficiently

1. **Use Templates**: Create templates for frequently-used concept patterns
2. **Keyboard Shortcuts**: Learn keyboard shortcuts for common actions
3. **Search**: Use search in List view to quickly find concepts
4. **Duplicate**: Use duplicate feature to create similar concepts
5. **Save Regularly**: The app auto-saves, but export important work

### Collaboration

1. **Export TTL**: Share TTL files with collaborators
2. **Use Notes**: Document decisions and rationale
3. **Version Control**: Keep TTL exports in version control (Git)
4. **Consistent Standards**: Agree on naming conventions and relationship types

---

## Troubleshooting

### Common Issues

**Problem: Graph view is empty**
- **Solution**: Make sure you've added concepts to your ontology
- Check that concepts have been saved

**Problem: Can't see relationships in graph**
- **Solution**: Ensure you have at least 2 concepts and have created relationships
- Check that both source and target concepts exist

**Problem: Search isn't working**
- **Solution**: Make sure you're in List view (`Alt+L`)
- Click the search box or press `Ctrl+F`
- Clear any filters by clicking the X button

**Problem: TTL import fails**
- **Solution**: Verify your TTL syntax is valid
- Check for missing prefixes or malformed triples
- Try importing a smaller file first

**Problem: Keyboard shortcuts not working**
- **Solution**: Make sure you're not in an input field (press `Esc` first)
- Refresh the page to reload JavaScript
- Check browser console for errors

### Performance Tips

**For Large Ontologies (100+ concepts):**

1. **Use List View**: List view performs better than graph for large datasets
2. **Search Instead of Scrolling**: Use search to find specific concepts
3. **Export Regularly**: Keep TTL backups of your work
4. **Consider Splitting**: Very large ontologies might benefit from being split into modules

### Getting Help

**If you encounter issues:**

1. Check this user guide
2. Press `?` to see keyboard shortcuts
3. Check the browser console for error messages (F12)
4. Export your TTL before experimenting with fixes
5. Restart the application (`Ctrl+C` and `dotnet run`)

### Database Issues

**If data seems corrupted:**

1. Export your ontology to TTL first (if possible)
2. Stop the application
3. Back up `ontology.db`
4. Delete `ontology.db`, `ontology.db-shm`, and `ontology.db-wal`
5. Restart the application
6. Re-import your TTL file

---

## Advanced Features

### Custom Concept Templates

Templates allow you to define reusable concept patterns:

**Creating Effective Templates:**
1. Identify commonly-used concept patterns in your domain
2. Create templates with appropriate:
   - Category and Type
   - Standard descriptions
   - Example patterns
   - Distinctive colors
3. Use templates when creating new concepts

**Example Templates:**
- **Agent/Person**: For human entities
- **Agent/Organization**: For organizational entities
- **Event/Meeting**: For meeting events
- **Resource/Document**: For document resources

### Ontology Metadata

Use the settings dialog (`Ctrl+,`) to add:

- **Namespace**: Makes your ontology globally unique
- **Author**: Attribution and contact information
- **License**: Legal framework for usage
- **Tags**: For categorization and discovery
- **Notes**: Additional context and documentation

### Working with Multiple Ontologies

- **Sidebar Navigation**: Use the sidebar to quickly switch between ontologies
- **Recent List**: Access your 10 most recently edited ontologies
- **Naming**: Use descriptive names to distinguish ontologies
- **Export/Import**: Move concepts between ontologies via TTL

---

## Glossary

- **Concept**: A class or entity in your ontology (e.g., "Person", "Event")
- **Relationship**: A connection between two concepts (e.g., "subClassOf")
- **TTL**: Turtle format, a human-readable RDF serialization
- **Ontology**: A formal representation of knowledge with concepts and relationships
- **Template**: A predefined pattern for creating concepts
- **Namespace**: A URI that makes your ontology globally unique
- **RDF**: Resource Description Framework, a standard for knowledge representation

---

## Additional Resources

### Learning More About Ontologies

- [W3C OWL Specification](https://www.w3.org/OWL/)
- [RDF Primer](https://www.w3.org/TR/rdf11-primer/)
- [Turtle Syntax Specification](https://www.w3.org/TR/turtle/)

### Complementary Tools

- **Protégé**: Full-featured ontology editor
- **TopBraid Composer**: Enterprise ontology management
- **WebVOWL**: Ontology visualization
- **SPARQL**: Query language for RDF data

---

## Feedback and Contributions

We welcome feedback and contributions!

- Report issues on GitHub
- Suggest features
- Contribute code
- Improve documentation

---

**Version**: 1.0
**Last Updated**: 2025-10-22
**Built with**: .NET 9 and Blazor Server
