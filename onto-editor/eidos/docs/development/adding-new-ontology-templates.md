# Adding New Ontology Templates

This guide explains how to add new ontology templates to Eidos, making them available in the "Base Framework" dropdown when creating a new ontology.

## Overview

Eidos supports loading standard ontologies from local RDF/TTL files as templates when creating new ontologies. This allows users to start with established vocabularies like BFO, SKOS, Dublin Core, etc.

## Architecture

The ontology template system consists of three main components:

1. **OntologyDownloadService** - Manages ontology metadata and file paths
2. **Home.razor** - UI for selecting templates during ontology creation
3. **RDF/TTL Files** - The actual ontology files stored in `wwwroot/`

## Step-by-Step Guide

### 1. Add Your Ontology File

First, add your RDF or TTL file to the appropriate folder:

- **TTL files**: `wwwroot/ttl/`
- **RDF files**: `wwwroot/rdf/`

**Example:**
```bash
# Copy your file to the project
cp my-ontology.rdf wwwroot/rdf/
```

### 2. Register in OntologyDownloadService

Add your ontology to the `OntologySources` dictionary in `Services/OntologyDownloadService.cs`.

**Location:** `Services/OntologyDownloadService.cs` (lines 14-64)

**Code to add:**
```csharp
["your-key"] = new OntologySource(
    "wwwroot/rdf/your-file.rdf",  // File path relative to project root
    "Your Ontology Name",          // Display name
    "PREFIX",                      // Preferred prefix (e.g., "dc", "foaf")
    "http://your.ontology/uri/"    // Ontology namespace URI
)
```

**Example (Dublin Core Terms):**
```csharp
["dcterms"] = new OntologySource(
    "wwwroot/rdf/dublin_core_terms.rdf",
    "Dublin Core Metadata Terms",
    "dcterms",
    "http://purl.org/dc/terms/"
)
```

### 3. Add to UI Dropdown

Update the template selection dropdown in `Components/Pages/Home.razor`.

**Location:** `Components/Pages/Home.razor` (around line 177)

**Add your option:**
```html
<option value="your-key">Your Ontology Name - Brief description</option>
```

**Example:**
```html
<option value="dcterms">Dublin Core Metadata Terms - Metadata vocabulary</option>
```

### 4. Add Description Text

Add a helpful description that appears when the user selects your template.

**Location:** `Components/Pages/Home.razor` (around line 188)

**Add your description block:**
```html
else if (selectedTemplate == "your-key")
{
    <span>Brief description of what this ontology provides</span>
}
```

**Example:**
```html
else if (selectedTemplate == "dcterms")
{
    <span>Dublin Core: Standard metadata properties like title, creator, subject, description</span>
}
```

### 5. Add to Import Logic

Include your template key in the import processing logic.

**Location:** `Components/Pages/Home.razor` (around line 692)

**Add to condition:**
```csharp
if (!string.IsNullOrEmpty(selectedTemplate) &&
    (selectedTemplate == "ro" || selectedTemplate == "owl" || selectedTemplate == "skos" ||
     selectedTemplate == "foaf" || selectedTemplate == "schema" || selectedTemplate == "your-key"))
```

**Example:**
```csharp
if (!string.IsNullOrEmpty(selectedTemplate) &&
    (selectedTemplate == "ro" || selectedTemplate == "owl" || selectedTemplate == "skos" ||
     selectedTemplate == "foaf" || selectedTemplate == "schema" || selectedTemplate == "dcterms"))
```

## Complete Example: Adding Dublin Core Terms

Here's a complete walkthrough of adding Dublin Core Terms:

### 1. File Added
```
wwwroot/rdf/dublin_core_terms.rdf
```

### 2. OntologyDownloadService.cs
```csharp
["dcterms"] = new OntologySource(
    "wwwroot/rdf/dublin_core_terms.rdf",
    "Dublin Core Metadata Terms",
    "dcterms",
    "http://purl.org/dc/terms/"
)
```

### 3. Home.razor - Dropdown Option
```html
<option value="dcterms">Dublin Core Metadata Terms - Metadata vocabulary</option>
```

### 4. Home.razor - Description
```html
else if (selectedTemplate == "dcterms")
{
    <span>Dublin Core: Standard metadata properties like title, creator, subject, description</span>
}
```

### 5. Home.razor - Import Logic
```csharp
selectedTemplate == "dcterms"
```

## File Format Support

Eidos supports both RDF/XML and Turtle (TTL) formats. The parser automatically detects the format.

### Supported Patterns

The RDF parser looks for:

- **OWL/RDFS Classes**: `owl:Class`, `rdfs:Class`
- **SKOS Concepts**: `skos:Concept`
- **Relationships**: `rdfs:subClassOf`, `skos:broader`, `skos:narrower`
- **Object Properties**: `owl:ObjectProperty`

## Testing Your Addition

1. **Restart the application** to reload the service with your changes
2. Navigate to the home page
3. Click "Create New Ontology"
4. Open the "Base Framework" dropdown
5. Your ontology should appear in the list
6. Select it and verify the description appears below
7. Create the ontology and verify concepts are imported correctly

## Troubleshooting

### Template Not Appearing in Dropdown

- Check that you added the `<option>` in `Home.razor`
- Verify the `value` attribute matches your key in `OntologySources`
- Ensure the application has been rebuilt

### File Not Found Error

- Verify the file path in `OntologySource` is correct
- Check that the file exists in `wwwroot/ttl/` or `wwwroot/rdf/`
- Path should be relative to project root, not `wwwroot/`

### No Concepts Imported

- Check the RDF file format is valid (use an online validator)
- Verify the file contains class/concept definitions
- Check the console for parser output/errors
- Ensure your ontology uses supported patterns (OWL, RDFS, or SKOS)

### Import Processing Not Triggered

- Verify you added your template key to the import logic condition in `Home.razor`
- Check that the condition includes your template key

## Best Practices

1. **Use descriptive keys** - Keep them short but meaningful (e.g., "dcterms", "foaf", "schema")
2. **Provide helpful descriptions** - Explain what the ontology offers in practical terms
3. **Use standard namespaces** - Follow established URI patterns for well-known ontologies
4. **Test thoroughly** - Import a test ontology and verify all concepts/relationships appear
5. **Document sources** - Add comments indicating where the ontology file came from

## Related Files

- `Services/OntologyDownloadService.cs` - Template metadata registry
- `Services/Import/RdfParser.cs` - RDF/TTL file parser
- `Services/Import/OntologyImporter.cs` - Import logic
- `Components/Pages/Home.razor` - Create ontology UI

## See Also

- [RDF Parser Documentation](./rdf-parser.md) (if exists)
- [Import System Architecture](./import-architecture.md) (if exists)
