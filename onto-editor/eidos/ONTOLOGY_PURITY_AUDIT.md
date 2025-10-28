# Ontology Purity Audit - RDF/TTL Standards Compliance

**Date:** October 28, 2025
**Purpose:** Comprehensive review of Eidos' RDF/OWL/TTL handling to ensure standards compliance and ontological purity

---

## Executive Summary

### Overall Assessment: **B+ (Good with Improvements Needed)**

Eidos demonstrates **strong fundamentals** in RDF/OWL handling with proper use of standard vocabularies and the dotNetRDF library. However, there are several **critical deviations** from pure ontological modeling that should be addressed for true semantic web compliance.

---

## ✅ STRENGTHS - What We're Doing Right

### 1. **Correct Use of Standard Vocabularies** ✓

**Evidence:** TtlExportService.cs lines 67-81
```csharp
graph.NamespaceMap.AddNamespace("rdf", UriFactory.Create(OntologyNamespaces.RdfSyntax));
graph.NamespaceMap.AddNamespace("rdfs", UriFactory.Create(OntologyNamespaces.RdfSchema));
graph.NamespaceMap.AddNamespace("owl", UriFactory.Create(OntologyNamespaces.Owl));
graph.NamespaceMap.AddNamespace("dc", UriFactory.Create(OntologyNamespaces.DublinCoreElements));
```

**Analysis:**
- ✓ Uses official W3C namespace URIs
- ✓ Properly imports Dublin Core for metadata
- ✓ Conditional BFO and PROV-O namespace inclusion
- ✓ Centralizes namespaces in `OntologyNamespaces.cs` for maintainability

### 2. **Proper OWL Class Declarations** ✓

**Evidence:** TtlExportService.cs lines 139-150
```csharp
var owlClass = graph.CreateUriNode("owl:Class");
var conceptNode = graph.CreateUriNode(UriFactory.Create(conceptUri));
graph.Assert(conceptNode, rdfType, owlClass);
```

**Analysis:**
- ✓ Concepts are exported as `owl:Class` (correct for classes)
- ✓ Uses `rdf:type` predicate (proper typing)
- ✓ Creates proper URI nodes (not literals)

### 3. **Standards-Compliant Subclass Relationships** ✓

**Evidence:** TtlExportService.cs lines 193-197
```csharp
if (relationship.RelationType.ToLower() == "is-a")
{
    graph.Assert(sourceNode, rdfsSubClassOf, targetNode);
}
```

**Analysis:**
- ✓ Uses `rdfs:subClassOf` for hierarchical relationships
- ✓ This is the **correct** OWL/RDFS predicate for class hierarchies
- ✓ Properly distinguishes "is-a" from other relationships

### 4. **OWL Restrictions Support** ✓

**Evidence:** TtlExportService.cs lines 216-304
```csharp
var owlRestriction = graph.CreateUriNode("owl:Restriction");
var owlOnProperty = graph.CreateUriNode("owl:onProperty");
var owlAllValuesFrom = graph.CreateUriNode("owl:allValuesFrom");
var owlMinCardinality = graph.CreateUriNode("owl:minCardinality");
```

**Analysis:**
- ✓ Implements OWL restrictions using anonymous nodes (blank nodes)
- ✓ Supports cardinality constraints (`owl:minCardinality`, `owl:maxCardinality`, `owl:cardinality`)
- ✓ Supports value type restrictions (`owl:allValuesFrom`)
- ✓ Uses typed literals with XSD data types (lines 255-256)
- ✓ This is **advanced OWL DL** modeling - very good!

### 5. **Named Individuals (ABox Assertions)** ✓

**Evidence:** TtlExportService.cs lines 306-395
```csharp
var owlNamedIndividual = graph.CreateUriNode("owl:NamedIndividual");
graph.Assert(individualNode, rdfType, owlNamedIndividual);
graph.Assert(individualNode, rdfType, conceptNode);
```

**Analysis:**
- ✓ Properly declares individuals as `owl:NamedIndividual`
- ✓ Asserts class membership with `rdf:type`
- ✓ Supports data properties with typed literals
- ✓ This provides **both TBox (schema) and ABox (data)** - complete OWL ontology!

### 6. **XSD Datatype Support** ✓

**Evidence:** TtlExportService.cs lines 406-418
```csharp
private string GetXsdDataType(string dataType)
{
    return dataType.ToLower() switch
    {
        "integer" or "int" => OntologyNamespaces.Xsd.Integer,
        "decimal" or "number" => OntologyNamespaces.Xsd.Decimal,
        "boolean" or "bool" => OntologyNamespaces.Xsd.Boolean,
        "date" => OntologyNamespaces.Xsd.Date,
        "datetime" => OntologyNamespaces.Xsd.DateTime,
        ...
    };
}
```

**Analysis:**
- ✓ Properly maps to XSD types (e.g., `xsd:integer`, `xsd:boolean`)
- ✓ Uses official W3C XML Schema namespace
- ✓ Supports common data types correctly

### 7. **Correct RDF Import Using dotNetRDF** ✓

**Evidence:** Uses VDS.RDF library throughout
- ✓ Industry-standard .NET RDF library
- ✓ Supports multiple formats (Turtle, RDF/XML, N-Triples, JSON-LD)
- ✓ Proper graph-based API usage

---

## ❌ CRITICAL ISSUES - Deviations from Pure Ontology Standards

### 1. **Non-Standard Custom Properties** ⚠️ **HIGH PRIORITY**

**Problem:** TtlExportService.cs lines 159-181
```csharp
// Simple explanation
if (!string.IsNullOrWhiteSpace(concept.SimpleExplanation))
{
    var explanationPredicate = graph.CreateUriNode(UriFactory.Create(baseUri + "simpleExplanation"));
    var explanationLiteral = graph.CreateLiteralNode(concept.SimpleExplanation);
    graph.Assert(conceptNode, explanationPredicate, explanationLiteral);
}

// Examples
if (!string.IsNullOrWhiteSpace(concept.Examples))
{
    var examplesPredicate = graph.CreateUriNode(UriFactory.Create(baseUri + "examples"));
    ...
}

// Category
if (!string.IsNullOrWhiteSpace(concept.Category))
{
    var categoryPredicate = graph.CreateUriNode(UriFactory.Create(baseUri + "category"));
    ...
}
```

**Issues:**
1. **`simpleExplanation`** - Custom property not defined in any standard vocabulary
2. **`examples`** - Custom property not defined in any standard vocabulary
3. **`category`** - Custom property not defined in any standard vocabulary

**Why This Matters:**
- These properties won't be understood by other ontology tools
- Breaks interoperability with semantic web applications
- Not following Linked Data principles

**Recommended Fix:**
```turtle
# INSTEAD OF:
:MyConcept :simpleExplanation "This is a friendly explanation" .
:MyConcept :examples "Dog, Cat, Whale" .
:MyConcept :category "Biology" .

# USE STANDARD VOCABULARIES:
:MyConcept skos:definition "This is a friendly explanation" .
:MyConcept skos:example "Dog", "Cat", "Whale" .
:MyConcept dcterms:subject "Biology" .

# OR if you must create custom properties, declare them properly:
:simpleExplanation a owl:AnnotationProperty ;
    rdfs:label "Simple Explanation" ;
    rdfs:comment "A user-friendly explanation of a concept" ;
    rdfs:subPropertyOf skos:definition .
```

**Severity:** HIGH - Breaks interoperability

---

### 2. **Missing Property Declarations** ⚠️ **HIGH PRIORITY**

**Problem:** TtlExportService.cs lines 199-204
```csharp
// Create custom property for other relationship types
var propertyUri = CreatePropertyUri(baseUri, relationship.RelationType);
var propertyNode = graph.CreateUriNode(UriFactory.Create(propertyUri));
graph.Assert(sourceNode, propertyNode, targetNode);
```

**Issues:**
- Properties are **used but never declared**
- No `rdf:type owl:ObjectProperty` assertion
- No `rdfs:domain` or `rdfs:range` declarations
- No `rdfs:label` or `rdfs:comment` for documentation

**What Should Happen:**
```turtle
# CURRENT OUTPUT:
:Dog :hasPart :Tail .

# WHAT WE NEED:
:hasPart a owl:ObjectProperty ;
    rdfs:label "has part" ;
    rdfs:comment "Indicates a part-whole relationship" ;
    rdfs:domain :Organism ;
    rdfs:range :BodyPart .

:Dog :hasPart :Tail .
```

**Severity:** HIGH - Creates invalid/incomplete OWL ontologies

---

### 3. **Visual Metadata Pollution** ⚠️ **MEDIUM PRIORITY**

**Problem:** OntologyModels.cs lines 189-191
```csharp
// Visual positioning for graph display
public double? PositionX { get; set; }
public double? PositionY { get; set; }
```

**Issues:**
- **Graph positions are NOT exported** (correctly) but exist in data model
- This is presentation logic mixed with ontology data
- **Good news:** Not polluting RDF output, but still conceptually wrong

**Best Practice:**
- ✓ Keep visual data separate from ontological data
- ✓ Store in a separate `GraphLayout` table
- ✓ Never export to RDF (currently doing this correctly)

**Severity:** MEDIUM - Architectural concern, not affecting RDF output

---

### 4. **"Color" as Ontological Property** ⚠️ **MEDIUM PRIORITY**

**Problem:** OntologyModels.cs line 195
```csharp
public string? Color { get; set; } // For visual distinction
```

**Issues:**
- Color is **presentation data**, not ontological data
- However, it's NOT being exported to RDF (good!)
- Should be in a separate UI preferences table

**Severity:** MEDIUM - Not affecting RDF output, but poor separation of concerns

---

### 5. **Ambiguous Relationship Semantics** ⚠️ **MEDIUM PRIORITY**

**Problem:** Users can create custom relationship types like "related-to", "depends-on", "part-of" without proper OWL property definitions

**Issues:**
- No distinction between `owl:ObjectProperty`, `owl:DatatypeProperty`, or `owl:AnnotationProperty`
- No transitivity, symmetry, or inverse property declarations
- "part-of" should ideally use proper mereology (BFO or RO parthood relations)

**Example of Better Approach:**
```turtle
# Instead of custom "part-of":
:hasProperPart a owl:ObjectProperty, owl:TransitiveProperty ;
    rdfs:subPropertyOf BFO:0000050 ;  # BFO "part of"
    rdfs:label "has proper part" ;
    rdfs:domain :Whole ;
    rdfs:range :Part .
```

**Severity:** MEDIUM - Affects reasoning capability

---

### 6. **No Ontology Metadata Declaration** ⚠️ **MEDIUM PRIORITY**

**Problem:** TtlExportService.cs lines 93-132

**Issues:**
- Declares ontology as `owl:Ontology` ✓
- Has Dublin Core metadata ✓
- **MISSING:**
  - `owl:imports` declarations for imported ontologies (BFO, PROV-O)
  - `owl:versionIRI` for version tracking
  - `dcterms:created` and `dcterms:modified` with proper date formatting
  - `dcterms:license` should use URI, not literal

**What's Needed:**
```turtle
<http://example.org/myontology> a owl:Ontology ;
    owl:imports <http://purl.obolibrary.org/obo/bfo.owl> ;
    owl:versionIRI <http://example.org/myontology/1.0> ;
    dcterms:created "2025-10-28"^^xsd:date ;
    dcterms:modified "2025-10-28"^^xsd:date ;
    dcterms:license <http://creativecommons.org/licenses/by/4.0/> .
```

**Severity:** MEDIUM - Reduces ontology reusability

---

### 7. **URI Scheme Inconsistency** ⚠️ **LOW PRIORITY**

**Problem:** TtlExportService.cs lines 420-439

**Issues:**
- Uses `example.org` as default (acceptable for testing, not production)
- Concept URIs: `baseUri + localName` (hash vs. slash URIs not distinguished)
- Individual URIs: `baseUri + "individual/" + name` (good!)
- Property URIs: `baseUri + sanitizedName` (inconsistent with individuals)

**Best Practice:**
```turtle
# Hash URIs (for small ontologies):
<http://example.org/myonto#Dog>
<http://example.org/myonto#hasPart>

# Slash URIs (for large ontologies):
<http://example.org/myonto/Dog>
<http://example.org/myonto/hasPart>

# W3C Recommendation: Pick ONE and be consistent
```

**Severity:** LOW - Both approaches are valid, but consistency is better

---

## 📊 Standards Compliance Scorecard

| Category | Score | Notes |
|----------|-------|-------|
| **RDF Syntax** | 95% | Excellent use of dotNetRDF, proper triples |
| **OWL DL Compliance** | 75% | Good class/individual modeling, missing property declarations |
| **RDFS Vocabulary** | 90% | Proper use of subClassOf, label, comment |
| **Standard Vocabularies** | 60% | Uses Dublin Core, but invents custom properties |
| **Interoperability** | 65% | Will import to Protégé but many properties won't be understood |
| **Reasoning Support** | 70% | Has restrictions, but missing property characteristics |
| **Linked Data Principles** | 60% | Uses URIs, but not dereferenceable; no owl:imports |

**Overall: B+ (83%)**

---

## 🔧 RECOMMENDED FIXES (Priority Order)

### Priority 1: Declare All Properties

**File:** `TtlExportService.cs`
**Add Method:**
```csharp
private void ExportPropertyDeclarations(IGraph graph, Ontology ontology, string baseUri)
{
    var owlObjectProperty = graph.CreateUriNode("owl:ObjectProperty");
    var owlDatatypeProperty = graph.CreateUriNode("owl:DatatypeProperty");
    var rdfType = graph.CreateUriNode("rdf:type");
    var rdfsLabel = graph.CreateUriNode("rdfs:label");
    var rdfsComment = graph.CreateUriNode("rdfs:comment");
    var rdfsDomain = graph.CreateUriNode("rdfs:domain");
    var rdfsRange = graph.CreateUriNode("rdfs:range");

    // Get all unique relationship types
    var relationshipTypes = ontology.Relationships
        .Select(r => r.RelationType)
        .Distinct()
        .Where(t => t.ToLower() != "is-a"); // Skip subClassOf

    foreach (var relType in relationshipTypes)
    {
        var propertyUri = CreatePropertyUri(baseUri, relType);
        var propertyNode = graph.CreateUriNode(UriFactory.Create(propertyUri));

        // Declare as ObjectProperty
        graph.Assert(propertyNode, rdfType, owlObjectProperty);

        // Add label
        var label = relType.Replace("_", " ").Replace("-", " ");
        graph.Assert(propertyNode, rdfsLabel, graph.CreateLiteralNode(label));

        // Add comment if available (from Relationship.Description)
        var firstRelationship = ontology.Relationships.FirstOrDefault(r => r.RelationType == relType);
        if (firstRelationship != null && !string.IsNullOrWhiteSpace(firstRelationship.Description))
        {
            graph.Assert(propertyNode, rdfsComment, graph.CreateLiteralNode(firstRelationship.Description));
        }
    }
}
```

**Call from BuildRdfGraph() after exporting concepts:**
```csharp
// Export property declarations
ExportPropertyDeclarations(graph, ontology, baseUri);

// Export relationships (now properties are declared)
foreach (var relationship in ontology.Relationships)
{
    ...
}
```

---

### Priority 2: Use Standard Vocabularies

**Replace custom properties with SKOS/Dublin Core:**

```csharp
// BEFORE:
var explanationPredicate = graph.CreateUriNode(UriFactory.Create(baseUri + "simpleExplanation"));

// AFTER:
graph.NamespaceMap.AddNamespace("skos", UriFactory.Create(OntologyNamespaces.Skos));
var explanationPredicate = graph.CreateUriNode("skos:definition");
```

**Map Eidos properties to standard vocabularies:**
- `SimpleExplanation` → `skos:definition`
- `Definition` → `skos:scopeNote` (or keep as `rdfs:comment`)
- `Examples` → `skos:example` (use multiple assertions for multiple examples)
- `Category` → `dcterms:subject` or `skos:broader` (if category is another concept)

---

### Priority 3: Add owl:imports for Imported Ontologies

**File:** `TtlExportService.cs` lines 93-132

```csharp
// AFTER declaring ontologyNode as owl:Ontology:

if (ontology.UsesBFO)
{
    var owlImports = graph.CreateUriNode("owl:imports");
    var bfoImport = graph.CreateUriNode(UriFactory.Create("http://purl.obolibrary.org/obo/bfo.owl"));
    graph.Assert(ontologyNode, owlImports, bfoImport);
}

if (ontology.UsesProvO)
{
    var owlImports = graph.CreateUriNode("owl:imports");
    var provImport = graph.CreateUriNode(UriFactory.Create("http://www.w3.org/ns/prov-o"));
    graph.Assert(ontologyNode, owlImports, provImport);
}
```

---

### Priority 4: Add Property Characteristics

**Enhance property declarations with OWL characteristics:**

```csharp
// For transitive properties like "part-of":
var owlTransitiveProperty = graph.CreateUriNode("owl:TransitiveProperty");
graph.Assert(propertyNode, rdfType, owlTransitiveProperty);

// For symmetric properties like "related-to":
var owlSymmetricProperty = graph.CreateUriNode("owl:SymmetricProperty");
graph.Assert(propertyNode, rdfType, owlSymmetricProperty);

// For functional properties (single-valued):
var owlFunctionalProperty = graph.CreateUriNode("owl:FunctionalProperty");
graph.Assert(propertyNode, rdfType, owlFunctionalProperty);
```

**Add to Relationship model:**
```csharp
public class Relationship
{
    ...
    public bool IsTransitive { get; set; }
    public bool IsSymmetric { get; set; }
    public bool IsFunctional { get; set; }
    public string? InversePropertyName { get; set; }
}
```

---

### Priority 5: Improve URI Strategy

**Update `OntologyNamespaces.cs`:**

```csharp
public enum UriStyle
{
    Hash,   // http://example.org/myonto#Dog
    Slash   // http://example.org/myonto/Dog
}

public static string CreateConceptUri(string baseUri, string conceptName, UriStyle style)
{
    var normalizedName = SanitizeName(conceptName);

    return style switch
    {
        UriStyle.Hash => baseUri.TrimEnd('/') + "#" + normalizedName,
        UriStyle.Slash => baseUri.TrimEnd('/') + "/" + normalizedName,
        _ => baseUri + normalizedName
    };
}
```

---

## 🎯 VALIDATION RECOMMENDATIONS

### 1. Automated Validation

**Install OWL validator:**
```bash
dotnet add package OWLDotNetApi
```

**Add validation method:**
```csharp
public ValidationResult ValidateOntology(IGraph graph)
{
    // Check for:
    // - Undeclared properties
    // - Classes without labels
    // - Invalid URIs
    // - Circular dependencies
    // - Cardinality violations
}
```

### 2. Test with Standard Tools

**Export test ontology and validate with:**
- **Protégé** - Industry standard OWL editor
- **ROBOT** - OWL tool for ontology development
- **RDF Validator** - https://www.w3.org/RDF/Validator/
- **OWL Validator** - http://mowl-power.cs.man.ac.uk:8080/validator/

### 3. Reasoning Tests

**Test with reasoners:**
- **HermiT** - OWL DL reasoner
- **Pellet** - Complete OWL DL reasoner
- Check if inferred axioms are correct

---

## 📚 RESOURCES FOR IMPROVEMENT

1. **OWL 2 Web Ontology Language Primer**
   https://www.w3.org/TR/owl2-primer/

2. **Linked Data Best Practices**
   https://www.w3.org/TR/ld-bp/

3. **SKOS Simple Knowledge Organization System**
   https://www.w3.org/TR/skos-reference/

4. **Dublin Core Metadata Terms**
   https://www.dublincore.org/specifications/dublin-core/dcmi-terms/

5. **BFO 2.0 Specification**
   https://basic-formal-ontology.org/

---

## 🏆 CONCLUSION

**Eidos has a solid foundation** for RDF/OWL ontology management. The use of `dotNetRDF`, proper OWL class declarations, subclass relationships, and OWL restrictions demonstrates strong understanding of semantic web standards.

**The main gaps are:**
1. Missing property declarations (easy to fix)
2. Overuse of custom properties instead of standard vocabularies (medium effort)
3. Missing OWL ontology imports (easy to fix)

**With the recommended fixes, Eidos can achieve:**
- ✓ Full OWL DL compliance
- ✓ Compatibility with Protégé, TopBraid, and other ontology tools
- ✓ Support for automated reasoning
- ✓ True Linked Data interoperability

**Estimated effort to reach A+ grade: 1-2 weeks of focused development**

---

**Auditor's Recommendation:**
Proceed with Priority 1-3 fixes immediately. These are high-impact, relatively low-effort improvements that will significantly enhance ontology quality and interoperability.
