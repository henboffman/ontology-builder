using Eidos.Data;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eidos.Services;

/// <summary>
/// Service for managing concept property definitions (OWL properties at class level).
/// Handles CRUD operations and validation for DataProperty and ObjectProperty definitions.
/// </summary>
public class ConceptPropertyService : IConceptPropertyService
{
    private readonly OntologyDbContext _context;
    private readonly ILogger<ConceptPropertyService> _logger;

    public ConceptPropertyService(
        OntologyDbContext context,
        ILogger<ConceptPropertyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ICollection<ConceptProperty>> GetPropertiesByConceptIdAsync(int conceptId)
    {
        try
        {
            var properties = await _context.ConceptProperties
                .AsNoTracking()
                .Include(p => p.RangeConcept)
                .Where(p => p.ConceptId == conceptId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} properties for concept {ConceptId}", properties.Count, conceptId);
            return properties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve properties for concept {ConceptId}", conceptId);
            throw;
        }
    }

    public async Task<ConceptProperty?> GetByIdAsync(int id)
    {
        try
        {
            var property = await _context.ConceptProperties
                .AsNoTracking()
                .Include(p => p.Concept)
                .Include(p => p.RangeConcept)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property != null)
            {
                _logger.LogInformation("Retrieved property {PropertyId} ({PropertyName})", id, property.Name);
            }

            return property;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve property {PropertyId}", id);
            throw;
        }
    }

    public async Task<ConceptProperty> CreateAsync(ConceptProperty conceptProperty)
    {
        try
        {
            // Validate before creating
            await ValidateAsync(conceptProperty);

            // Set timestamps
            conceptProperty.CreatedAt = DateTime.UtcNow;
            conceptProperty.UpdatedAt = DateTime.UtcNow;

            // Generate URI if not provided
            if (string.IsNullOrWhiteSpace(conceptProperty.Uri))
            {
                var concept = await _context.Concepts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == conceptProperty.ConceptId);

                if (concept != null)
                {
                    var ontology = await _context.Ontologies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == concept.OntologyId);

                    if (ontology != null && !string.IsNullOrWhiteSpace(ontology.Namespace))
                    {
                        conceptProperty.Uri = $"{ontology.Namespace.TrimEnd('#', '/')}/{conceptProperty.Name}";
                    }
                }
            }

            _context.ConceptProperties.Add(conceptProperty);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created property {PropertyName} for concept {ConceptId} (Type: {PropertyType})",
                conceptProperty.Name, conceptProperty.ConceptId, conceptProperty.PropertyType);

            return conceptProperty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create property {PropertyName} for concept {ConceptId}",
                conceptProperty.Name, conceptProperty.ConceptId);
            throw;
        }
    }

    public async Task<ConceptProperty> UpdateAsync(ConceptProperty conceptProperty)
    {
        try
        {
            var existing = await _context.ConceptProperties
                .FirstOrDefaultAsync(p => p.Id == conceptProperty.Id);

            if (existing == null)
            {
                throw new ArgumentException($"Property with ID {conceptProperty.Id} not found");
            }

            // Validate before updating
            await ValidateAsync(conceptProperty);

            // Update fields
            existing.Name = conceptProperty.Name;
            existing.PropertyType = conceptProperty.PropertyType;
            existing.DataType = conceptProperty.DataType;
            existing.RangeConceptId = conceptProperty.RangeConceptId;
            existing.IsRequired = conceptProperty.IsRequired;
            existing.IsFunctional = conceptProperty.IsFunctional;
            existing.Description = conceptProperty.Description;
            existing.Uri = conceptProperty.Uri;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated property {PropertyId} ({PropertyName}) for concept {ConceptId}",
                conceptProperty.Id, conceptProperty.Name, conceptProperty.ConceptId);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update property {PropertyId}", conceptProperty.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var property = await _context.ConceptProperties
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                throw new ArgumentException($"Property with ID {id} not found");
            }

            _context.ConceptProperties.Remove(property);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Deleted property {PropertyId} ({PropertyName}) from concept {ConceptId}",
                id, property.Name, property.ConceptId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete property {PropertyId}", id);
            throw;
        }
    }

    public async Task<bool> PropertyNameExistsAsync(int conceptId, string propertyName, int? excludePropertyId = null)
    {
        try
        {
            var query = _context.ConceptProperties
                .AsNoTracking()
                .Where(p => p.ConceptId == conceptId && p.Name == propertyName);

            if (excludePropertyId.HasValue)
            {
                query = query.Where(p => p.Id != excludePropertyId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check property name existence for concept {ConceptId}", conceptId);
            throw;
        }
    }

    public async Task ValidateAsync(ConceptProperty conceptProperty)
    {
        // Validate required fields
        if (conceptProperty.ConceptId <= 0)
        {
            throw new ArgumentException("ConceptId is required");
        }

        if (string.IsNullOrWhiteSpace(conceptProperty.Name))
        {
            throw new ArgumentException("Property name is required");
        }

        // Validate property name format (no spaces, valid identifier)
        if (!IsValidPropertyName(conceptProperty.Name))
        {
            throw new ArgumentException(
                "Property name must be a valid identifier (letters, numbers, underscores, no spaces)");
        }

        // Check for duplicate property names within the same concept
        var nameExists = await PropertyNameExistsAsync(
            conceptProperty.ConceptId,
            conceptProperty.Name,
            conceptProperty.Id > 0 ? conceptProperty.Id : null);

        if (nameExists)
        {
            throw new ArgumentException(
                $"A property named '{conceptProperty.Name}' already exists for this concept");
        }

        // Validate concept exists
        var conceptExists = await _context.Concepts
            .AsNoTracking()
            .AnyAsync(c => c.Id == conceptProperty.ConceptId);

        if (!conceptExists)
        {
            throw new ArgumentException($"Concept with ID {conceptProperty.ConceptId} not found");
        }

        // Type-specific validation
        switch (conceptProperty.PropertyType)
        {
            case PropertyType.DataProperty:
                // DataProperty must have DataType, not RangeConceptId
                if (string.IsNullOrWhiteSpace(conceptProperty.DataType))
                {
                    throw new ArgumentException("DataType is required for DataProperty");
                }

                if (!IsValidDataType(conceptProperty.DataType))
                {
                    throw new ArgumentException(
                        $"Invalid DataType '{conceptProperty.DataType}'. Valid types: string, integer, decimal, boolean, date, dateTime");
                }

                if (conceptProperty.RangeConceptId.HasValue)
                {
                    throw new ArgumentException(
                        "DataProperty cannot have RangeConceptId. Use DataType instead.");
                }
                break;

            case PropertyType.ObjectProperty:
                // ObjectProperty must have RangeConceptId, not DataType
                if (!conceptProperty.RangeConceptId.HasValue)
                {
                    throw new ArgumentException("RangeConceptId is required for ObjectProperty");
                }

                if (!string.IsNullOrWhiteSpace(conceptProperty.DataType))
                {
                    throw new ArgumentException(
                        "ObjectProperty cannot have DataType. Use RangeConceptId instead.");
                }

                // Validate range concept exists
                var rangeConceptExists = await _context.Concepts
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == conceptProperty.RangeConceptId.Value);

                if (!rangeConceptExists)
                {
                    throw new ArgumentException(
                        $"Range concept with ID {conceptProperty.RangeConceptId.Value} not found");
                }
                break;

            default:
                throw new ArgumentException($"Invalid PropertyType: {conceptProperty.PropertyType}");
        }

        _logger.LogDebug("Validated property {PropertyName} for concept {ConceptId}",
            conceptProperty.Name, conceptProperty.ConceptId);
    }

    /// <summary>
    /// Validates property name format (letters, numbers, underscores, must start with letter)
    /// </summary>
    private static bool IsValidPropertyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Must start with a letter
        if (!char.IsLetter(name[0]))
            return false;

        // Only letters, numbers, and underscores allowed
        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    /// <summary>
    /// Validates XSD datatype string
    /// </summary>
    private static bool IsValidDataType(string dataType)
    {
        var validTypes = new[]
        {
            "string", "integer", "decimal", "boolean", "date", "dateTime",
            "float", "double", "long", "int", "short", "byte",
            "anyURI", "base64Binary", "hexBinary"
        };

        return validTypes.Contains(dataType, StringComparer.OrdinalIgnoreCase);
    }
}
