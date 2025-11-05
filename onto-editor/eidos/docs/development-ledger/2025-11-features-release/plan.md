# Feature Release Plan - November 2025

**Created**: November 5, 2025
**Status**: Planning
**Target Release**: November 2025

---

## Overview

This release focuses on three key improvements to enhance user experience and ontological correctness:

1. **Persistent Color Selection** - Preserve user's color choice across consecutive concept creation
2. **View State Preservation** - Maintain selected tab when deleting relationships
3. **Concept Property Definitions** - Add proper OWL property definitions to concepts (classes)

---

## Feature 1: Persistent Color Selection

### User Problem
When creating multiple related concepts, users must repeatedly select the same color from the color picker. This is tedious and slows down bulk concept creation workflows.

### Current Behavior
- User opens "Add Concept" dialog
- Default color is loaded from user preferences
- User selects a color for the concept
- After saving, if user adds another concept, color resets to default
- User must reselect their desired color

### Desired Behavior
- When user selects a color, it becomes the "session color"
- Next concept dialog opens with the last-used color pre-selected
- Session color persists until:
  - User changes to a different color
  - User closes browser tab
  - User navigates away from ontology view

### Technical Approach
- Store `lastUsedConceptColor` in component state
- Update it when user changes color in concept editor
- Use `lastUsedConceptColor` ?? `userPreferences.DefaultConceptColor` when opening new concept dialog
- Reset to null on component disposal or navigation

### Files to Modify
- `OntologyView.razor` - Add `lastUsedConceptColor` state variable
- `ShowAddConceptDialog()` - Use last color instead of always loading from preferences
- `OnConceptCategoryChanged()` - Update to set lastUsedConceptColor when auto-applying color

### Testing
- ✅ Create concept with custom color
- ✅ Open new concept dialog, verify color is preserved
- ✅ Change color, create another concept, verify new color is used
- ✅ Verify preference default is used on first concept in session

---

## Feature 2: View State Preservation on Relationship Delete

### User Problem
When viewing relationships in the relationships tab and deleting one, the interface resets to the concepts tab. This is disorienting and requires extra clicks to return to relationships view.

### Current Behavior
- User switches to "Relationships" tab
- User deletes a relationship
- `LoadOntology()` is called to refresh data
- View resets to default (Concepts tab in List view)
- User must click back to Relationships tab

### Desired Behavior
- User remains on Relationships tab after deletion
- List updates to show relationship was removed
- Smooth, non-disruptive experience

### Technical Approach
Current issue is likely in `LoadOntology()` or the delete handler resetting view state. Need to:
- Investigate `DeleteRelationship` method
- Check if `LoadOntology()` resets `currentView` or similar state
- Preserve view state across reload
- Consider if we need full reload or can just update relationships collection

### Files to Investigate
- `OntologyView.razor` - Find `DeleteRelationship` method
- `LoadOntology()` method - Check for view state resets
- View state management - Ensure currentView is preserved

### Testing
- ✅ Switch to Relationships tab
- ✅ Delete a relationship
- ✅ Verify still on Relationships tab after deletion
- ✅ Verify relationship is removed from list
- ✅ Repeat test in different view modes (List, Graph if applicable)

---

## Feature 3: Concept Property Definitions

### Background: OWL Property Model

In OWL (Web Ontology Language), there are two fundamental types of properties:

1. **Datatype Properties** - Relate individuals to literal values (strings, numbers, dates)
   - Example: Person → age → "25" (integer)
   - Example: Person → name → "John" (string)

2. **Object Properties** - Relate individuals to other individuals
   - Example: Person → knows → Person
   - Example: Book → hasAuthor → Person

**Critical Distinction**: Properties are defined at the **class level** (domain and range), but **asserted at the individual level**. This is a core OWL principle.

- **Domain**: The class that the property can be applied to
- **Range**: The type of value (for datatype properties) or class (for object properties) that the property relates to

### Current State

Eidos currently only supports properties on **individuals** (instances):
- User creates a Concept (class): "Person"
- User creates an Individual (instance): "John"
- User adds properties to John: age=25, name="John"

This is correct for **instance-level** properties, but Eidos is missing **class-level property definitions**.

### The Problem

According to OWL best practices:
1. Properties should be defined with their domain and range
2. These definitions belong at the **ontology/class level**, not just on instances
3. Without property definitions, we cannot:
   - Export valid OWL ontologies with property restrictions
   - Use reasoners to infer types and validate data
   - Properly express class characteristics

### What Needs to Be Added

We should add the ability to define properties at the **concept level**:

**Example Scenario**:
- User creates Concept "Person"
- User defines properties for Person class:
  - Datatype Property: `age` (domain: Person, range: integer)
  - Datatype Property: `name` (domain: Person, range: string)
  - Object Property: `knows` (domain: Person, range: Person)
- When creating Individual "John" of type "Person":
  - Property suggestions come from Person's defined properties
  - Validation ensures properties match their range types

### Technical Approach

#### 1. Database Schema Changes

Add new table: `ConceptProperties`

```sql
CREATE TABLE ConceptProperties (
    Id INT PRIMARY KEY,
    ConceptId INT NOT NULL,  -- FK to Concepts
    Name VARCHAR(200) NOT NULL,
    PropertyType VARCHAR(50) NOT NULL,  -- "DataProperty" or "ObjectProperty"
    DataType VARCHAR(50),  -- For DataProperty: "string", "integer", "boolean", "date", etc.
    RangeConceptId INT,  -- For ObjectProperty: FK to Concepts
    IsRequired BOOL DEFAULT FALSE,
    IsFunctional BOOL DEFAULT FALSE,  -- Can have only one value
    Description TEXT,
    Uri VARCHAR(500),  -- For RDF export
    CreatedAt DATETIME,
    FOREIGN KEY (ConceptId) REFERENCES Concepts(Id) ON DELETE CASCADE,
    FOREIGN KEY (RangeConceptId) REFERENCES Concepts(Id) ON DELETE SET NULL
);
```

#### 2. Model Classes

```csharp
public class ConceptProperty
{
    public int Id { get; set; }
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public PropertyType PropertyType { get; set; }  // Enum: DataProperty, ObjectProperty

    // For DataProperty
    public string? DataType { get; set; }  // "string", "integer", "decimal", "boolean", "date"

    // For ObjectProperty
    public int? RangeConceptId { get; set; }
    public Concept? RangeConcept { get; set; }

    public bool IsRequired { get; set; }
    public bool IsFunctional { get; set; }
    public string? Description { get; set; }
    public string? Uri { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum PropertyType
{
    DataProperty,
    ObjectProperty
}
```

#### 3. UI Components

**ConceptPropertyEditor.razor** - New component for managing concept properties:
- List existing properties
- Add new property (choose between DataProperty or ObjectProperty)
- Edit property details
- Delete property

Add to Concept Editor dialog:
- New expandable section: "Property Definitions"
- Shows list of defined properties
- Button to add new property
- Shows property type, name, range/datatype

#### 4. TTL Export Enhancement

Update `TtlExportService` to include property definitions:

```turtle
# Current export (simplified)
:Person rdf:type owl:Class .

# Enhanced export
:Person rdf:type owl:Class .

:age rdf:type owl:DatatypeProperty ;
     rdfs:domain :Person ;
     rdfs:range xsd:integer .

:name rdf:type owl:DatatypeProperty ;
      rdfs:domain :Person ;
      rdfs:range xsd:string .

:knows rdf:type owl:ObjectProperty ;
       rdfs:domain :Person ;
       rdfs:range :Person .
```

#### 5. Individual Editor Enhancement

When creating an individual, suggest properties from the concept's property definitions:
- Show dropdown of defined properties
- Pre-fill datatype based on property definition
- For object properties, allow selecting another individual of the correct type
- Validate property values match their defined ranges

### Implementation Phases

**Phase 1: Foundation** (2-3 hours)
- Create database migration for ConceptProperties table
- Add ConceptProperty model class
- Add PropertyType enum
- Update Concept model to include Properties navigation property

**Phase 2: Service Layer** (2-3 hours)
- Create IConceptPropertyService
- Implement CRUD operations for concept properties
- Add validation (e.g., ObjectProperty must have RangeConceptId)
- Update OntologyService to load properties with concepts

**Phase 3: UI Components** (3-4 hours)
- Create ConceptPropertyEditor.razor component
- Add property list display
- Create add/edit property dialog
- Integrate into ConceptEditor

**Phase 4: Export Enhancement** (2 hours)
- Update TtlExportService to export property definitions
- Test with existing concepts and individuals
- Verify exports are valid OWL/Turtle

**Phase 5: Individual Editor Integration** (2 hours)
- Update IndividualEditor to suggest properties from concept
- Add property validation based on definitions
- Test creating individuals with defined properties

**Phase 6: Testing & Validation** (2 hours)
- Unit tests for ConceptPropertyService
- Component tests for ConceptPropertyEditor
- Integration tests for TTL export
- Manual testing of full workflow
- Validate exported TTL with OWL validator

**Total Estimated Time**: 13-16 hours

### Files to Create/Modify

**New Files**:
- `Models/ConceptProperty.cs`
- `Models/Enums/PropertyType.cs`
- `Services/Interfaces/IConceptPropertyService.cs`
- `Services/ConceptPropertyService.cs`
- `Components/Ontology/ConceptPropertyEditor.razor`
- `Migrations/[timestamp]_AddConceptProperties.cs`

**Modified Files**:
- `Models/Concept.cs` - Add Properties navigation property
- `Data/ApplicationDbContext.cs` - Add ConceptProperties DbSet
- `Services/TtlExportService.cs` - Export property definitions
- `Components/Ontology/ConceptEditor.razor` - Integrate property editor
- `Components/Ontology/IndividualEditor.razor` - Suggest properties

### Validation & Testing

After implementation, test that exports are valid:
1. Create concept with properties
2. Create individual with property values
3. Export to TTL
4. Validate with online OWL validator: http://mowl-power.cs.man.ac.uk:8080/converter/
5. Import into Protégé to verify structure
6. Test round-trip: export → import → verify no data loss

### Open Questions

1. Should we support property characteristics (transitive, symmetric, etc.)?
   - **Recommendation**: Start simple, add later if needed
2. Should properties be ontology-wide or concept-specific?
   - **Recommendation**: Make them concept-specific (domain), but support reuse
3. Should we support property hierarchies (subPropertyOf)?
   - **Recommendation**: Not in MVP, add if users request
4. What about annotation properties?
   - **Recommendation**: Future enhancement

---

## Implementation Order

1. **Feature 1 (Color Persistence)** - Quick win, 30-60 min
2. **Feature 2 (View Preservation)** - Bug fix, 30-60 min
3. **Feature 3 (Concept Properties)** - Major feature, 13-16 hours

**Rationale**: Deliver quick improvements first, then invest in the larger architectural enhancement.

---

## Success Criteria

### Feature 1
- ✅ Color selection persists across consecutive concept creation
- ✅ Session color resets on navigation/refresh
- ✅ Default preference is used on first concept

### Feature 2
- ✅ Deleting relationship maintains current view
- ✅ No unexpected navigation after delete
- ✅ UI updates smoothly without full page reload

### Feature 3
- ✅ Can define datatype properties on concepts
- ✅ Can define object properties on concepts
- ✅ Properties export correctly in TTL format
- ✅ Individual editor suggests defined properties
- ✅ All existing functionality continues to work
- ✅ Exports validate as correct OWL

---

## Risk Assessment

### Feature 1 - LOW RISK
- Simple state management change
- No database changes
- Easy to test and verify

### Feature 2 - LOW RISK
- UI state preservation
- No data changes
- Minimal code changes

### Feature 3 - MEDIUM RISK
- Database schema changes (migration required)
- New service layer
- TTL export changes (critical for data integrity)
- Requires thorough testing

**Mitigation**:
- Write comprehensive unit tests
- Test TTL export with validators
- Manual testing in Protégé
- Consider feature flag for gradual rollout

---

## Documentation Updates

After implementation:
- Update user guide with concept properties section
- Add examples of property definitions
- Document export format changes
- Add to release notes

---

**Next Steps**: Begin with Feature 1 implementation.
