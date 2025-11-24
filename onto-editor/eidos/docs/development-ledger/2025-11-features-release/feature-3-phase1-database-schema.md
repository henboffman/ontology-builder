# Feature 3 - Phase 1: Database Schema for Concept Properties

**Implemented**: November 4, 2025
**Status**: ✅ Complete
**Time**: 1 hour

---

## Summary

Created database schema to support OWL property definitions at the concept (class) level. This enables proper ontology structure where properties are defined with their domain and range, aligning with OWL standards.

---

## Problem Statement

### Current State

Eidos currently only supports properties at the individual (instance) level via the `IndividualProperty` model. This is incomplete for proper OWL ontologies, which require:

1. **Property definitions at class level** - Define what properties a class can have
2. **Domain specification** - Which class the property belongs to
3. **Range specification** - What type of values (data) or classes (objects) the property relates to
4. **Property characteristics** - Whether property is required, functional, etc.

### Why This Matters

In OWL and formal ontologies:
- Properties are first-class entities defined at the ontology level
- Classes (concepts) declare which properties they use via domain/range
- This enables reasoning, validation, and semantic interoperability
- Standard tools like Protégé expect this structure

**Example**:
```turtle
# Class-level property definition (what we're adding)
:hasAge rdf:type owl:DatatypeProperty ;
        rdfs:domain :Person ;
        rdfs:range xsd:integer ;
        rdf:type owl:FunctionalProperty .

# Instance-level usage (what we already have)
:JohnDoe rdf:type :Person ;
         :hasAge 25 .
```

---

## Solution Architecture

### New Models

#### 1. PropertyType Enum

**File**: `/Models/Enums/PropertyType.cs`

```csharp
public enum PropertyType
{
    /// <summary>
    /// Datatype Property - relates individuals to literal values
    /// Example: Person → age → "25" (integer)
    /// </summary>
    DataProperty,

    /// <summary>
    /// Object Property - relates individuals to other individuals
    /// Example: Person → knows → Person
    /// </summary>
    ObjectProperty
}
```

**Rationale**: OWL distinguishes between these two fundamental property types. DataProperty for literals (strings, numbers, dates), ObjectProperty for relationships between instances.

#### 2. ConceptProperty Model

**File**: `/Models/ConceptProperty.cs`

```csharp
public class ConceptProperty
{
    public int Id { get; set; }

    // Domain: The concept this property is defined for
    public int ConceptId { get; set; }
    [JsonIgnore]
    public Concept Concept { get; set; } = null!;

    // Property metadata
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public PropertyType PropertyType { get; set; }

    // For DataProperty: XSD datatype (e.g., "string", "integer", "date")
    [StringLength(50)]
    public string? DataType { get; set; }

    // For ObjectProperty: Range concept (target class)
    public int? RangeConceptId { get; set; }
    [JsonIgnore]
    public Concept? RangeConcept { get; set; }

    // OWL property characteristics
    public bool IsRequired { get; set; }        // Minimum cardinality >= 1
    public bool IsFunctional { get; set; }      // Maximum cardinality = 1

    public string? Description { get; set; }

    // URI for RDF export
    [StringLength(500)]
    public string? Uri { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

**Key Design Decisions**:

1. **ConceptId as Domain**: Links property to the concept it's defined for
2. **Discriminated Type**: PropertyType enum determines if DataType or RangeConceptId is used
3. **RangeConcept Foreign Key**: Allows ObjectProperty to reference target concept
4. **IsFunctional Flag**: Maps to OWL functional property (single-valued)
5. **IsRequired Flag**: Maps to OWL cardinality restrictions
6. **Uri Field**: Allows custom URIs or auto-generation for RDF export

---

## Database Changes

### Table: ConceptProperties

```sql
CREATE TABLE ConceptProperties (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ConceptId INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    PropertyType INT NOT NULL,
    DataType NVARCHAR(50) NULL,
    RangeConceptId INT NULL,
    IsRequired BIT NOT NULL DEFAULT 0,
    IsFunctional BIT NOT NULL DEFAULT 0,
    Description NVARCHAR(MAX) NULL,
    Uri NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_ConceptProperties_Concepts FOREIGN KEY (ConceptId) REFERENCES Concepts (Id) ON DELETE CASCADE,
    CONSTRAINT FK_ConceptProperties_RangeConcept FOREIGN KEY (RangeConceptId) REFERENCES Concepts (Id) ON DELETE NO ACTION
);
```

### Indexes

```sql
-- Performance index for querying properties by concept
CREATE INDEX IX_ConceptProperty_ConceptId ON ConceptProperties (ConceptId);

-- Index for ObjectProperty range lookups
CREATE INDEX IX_ConceptProperties_RangeConceptId ON ConceptProperties (RangeConceptId);
```

**Rationale**:
- **ConceptId index**: Efficiently load all properties for a concept
- **RangeConceptId index**: Find all properties that target a specific concept
- **CASCADE on ConceptId**: Delete properties when concept is deleted
- **RESTRICT on RangeConceptId**: Prevent deleting concept that's used as range

---

## Code Changes

### 1. Updated OntologyModels.cs

**File**: `/Models/OntologyModels.cs` (line 214)

Added navigation property to Concept class:

```csharp
// Navigation properties
public ICollection<Property> Properties { get; set; } = new List<Property>();
public ICollection<ConceptProperty> ConceptProperties { get; set; } = new List<ConceptProperty>(); // OWL property definitions
public ICollection<Relationship> RelationshipsAsSource { get; set; } = new List<Relationship>();
```

**Why**: Enables loading concept with its property definitions via EF Core Include().

### 2. Updated OntologyDbContext.cs

**File**: `/Data/OntologyDbContext.cs`

**Added DbSet** (line 20):
```csharp
public DbSet<ConceptProperty> ConceptProperties { get; set; } // OWL property definitions
```

**Added EF Configuration** (lines 139-156):
```csharp
// Configure ConceptProperty - Concept (domain)
modelBuilder.Entity<ConceptProperty>()
    .HasOne(p => p.Concept)
    .WithMany(c => c.ConceptProperties)
    .HasForeignKey(p => p.ConceptId)
    .OnDelete(DeleteBehavior.Cascade);

// Configure ConceptProperty - RangeConcept (for ObjectProperty type)
modelBuilder.Entity<ConceptProperty>()
    .HasOne(p => p.RangeConcept)
    .WithMany()
    .HasForeignKey(p => p.RangeConceptId)
    .OnDelete(DeleteBehavior.Restrict);

// PERFORMANCE: Add index on ConceptId for efficient property queries
modelBuilder.Entity<ConceptProperty>()
    .HasIndex(p => p.ConceptId)
    .HasDatabaseName("IX_ConceptProperty_ConceptId");
```

**Why**:
- Explicit configuration of relationships for clarity
- DeleteBehavior.Restrict prevents cascading deletes that would break integrity
- Index configuration for query performance

---

## Migration

### Created Migration

**File**: `Migrations/20251105014621_AddConceptPropertyDefinitions.cs`

Generated via:
```bash
dotnet ef migrations add AddConceptPropertyDefinitions
```

### Applied to Database

Applied manually via SQL due to migration history sync issues:

```bash
sqlite3 ontology.db < schema.sql
sqlite3 ontology.db "INSERT INTO __EFMigrationsHistory ..."
```

**Note**: Production deployment will use standard EF Core migration workflow.

---

## Validation

### Build Status
✅ **Build succeeded** - 0 errors, 0 warnings

### Database Schema Verification
```bash
sqlite3 ontology.db "PRAGMA table_info(ConceptProperties);"
```
✅ All columns present with correct types

### Migration History
```bash
sqlite3 ontology.db "SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20251105014621_AddConceptPropertyDefinitions';"
```
✅ Migration recorded in history

---

## Examples

### Example 1: DataProperty

**Scenario**: Person class has an "age" property (integer)

```csharp
var ageProperty = new ConceptProperty
{
    ConceptId = personConcept.Id,
    Name = "age",
    PropertyType = PropertyType.DataProperty,
    DataType = "integer",
    IsRequired = false,
    IsFunctional = true,  // A person has exactly one age
    Description = "The age of the person in years"
};
```

**Exports to**:
```turtle
:age rdf:type owl:DatatypeProperty ;
     rdfs:domain :Person ;
     rdfs:range xsd:integer ;
     rdf:type owl:FunctionalProperty .
```

### Example 2: ObjectProperty

**Scenario**: Person class has a "knows" relationship to other Persons

```csharp
var knowsProperty = new ConceptProperty
{
    ConceptId = personConcept.Id,
    Name = "knows",
    PropertyType = PropertyType.ObjectProperty,
    RangeConceptId = personConcept.Id,  // Points to Person concept
    IsRequired = false,
    IsFunctional = false,  // Can know multiple people
    Description = "A person that this person is acquainted with"
};
```

**Exports to**:
```turtle
:knows rdf:type owl:ObjectProperty ;
       rdfs:domain :Person ;
       rdfs:range :Person .
```

---

## Backwards Compatibility

### No Breaking Changes
- ✅ Existing `Property` model unchanged
- ✅ Existing `IndividualProperty` model unchanged
- ✅ All existing functionality preserved
- ✅ No data migration required

### Additive Only
- New table `ConceptProperties` is independent
- Existing TTL export continues to work
- New property definitions enhance exports but don't break them

---

## Next Steps

### Phase 2: Service Layer (Next)
- Create `IConceptPropertyService` interface
- Implement `ConceptPropertyService` with CRUD operations
- Add validation logic (DataProperty xor ObjectProperty)
- Add business rules (prevent circular dependencies)

### Phase 3: UI Components
- Create property editor component
- Integrate into concept editor dialog
- Property list display and management

### Phase 4: TTL Export Enhancement
- Update `TtlExportService` to export property definitions
- Include domain and range declarations
- Test with OWL validators (Protégé)

---

## Technical Decisions

### Why Not Reuse Existing Property Model?

**Rejected Approach**: Extend existing `Property` model to support both instance-level and class-level properties.

**Reason**:
- Different semantics: Properties are property *definitions*, IndividualProperty are property *values*
- Different relationships: ConceptProperty belongs to Concept, IndividualProperty belongs to Individual
- Clearer separation of concerns
- Follows OWL distinction between TBox (terminology/classes) and ABox (assertions/individuals)

### Why Discriminated Union (PropertyType Enum)?

**Alternative**: Separate tables for DataProperty and ObjectProperty

**Chosen Approach**: Single table with discriminator

**Reason**:
- Simpler queries (one table to join)
- Most validation is shared
- Easy to add property characteristics that apply to both types
- Standard pattern in ORM frameworks

### Why Restrict Delete on RangeConceptId?

**Reason**: Deleting a concept that's used as a range for ObjectProperty would leave dangling references. Force user to either:
1. Delete or update the property definition first, or
2. Handle the cascade explicitly

This prevents data integrity issues.

---

## Files Created

1. `/Models/Enums/PropertyType.cs` - Property type enum
2. `/Models/ConceptProperty.cs` - Property definition model
3. `/Migrations/20251105014621_AddConceptPropertyDefinitions.cs` - EF migration

## Files Modified

1. `/Models/OntologyModels.cs` - Added ConceptProperties navigation property
2. `/Data/OntologyDbContext.cs` - Added DbSet and EF configuration

---

## Testing Notes

### Manual Testing (Completed)
- ✅ Database table creation
- ✅ Foreign key constraints
- ✅ Index creation
- ✅ Build compilation

### Automated Testing (Phase 6)
- Unit tests for service layer
- Integration tests for database operations
- Component tests for UI
- End-to-end workflow tests

---

## References

- [OWL 2 Web Ontology Language Primer](https://www.w3.org/TR/owl2-primer/)
- [OWL Properties](https://www.w3.org/TR/owl-guide/#Properties)
- [RDF Schema](https://www.w3.org/TR/rdf-schema/)

---

**Build Status**: ✅ Passing (0 errors, 0 warnings)
**Database Status**: ✅ Schema applied
**Next Phase**: Phase 2 - Service Layer Implementation

**Last Updated**: November 4, 2025
