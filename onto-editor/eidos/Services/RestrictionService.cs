using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Eidos.Services;

/// <summary>
/// Service for managing concept restrictions (OWL-style constraints)
/// Handles CRUD operations and validation logic for property restrictions
/// </summary>
public class RestrictionService : IRestrictionService
{
    private readonly IRestrictionRepository _restrictionRepository;
    private readonly IConceptRepository _conceptRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IUserService _userService;
    private readonly IOntologyShareService _shareService;

    public RestrictionService(
        IRestrictionRepository restrictionRepository,
        IConceptRepository conceptRepository,
        IOntologyRepository ontologyRepository,
        IUserService userService,
        IOntologyShareService shareService)
    {
        _restrictionRepository = restrictionRepository;
        _conceptRepository = conceptRepository;
        _ontologyRepository = ontologyRepository;
        _userService = userService;
        _shareService = shareService;
    }

    public async Task<ConceptRestriction> CreateAsync(ConceptRestriction restriction)
    {
        // Get concept to check permissions
        var concept = await _conceptRepository.GetByIdAsync(restriction.ConceptId);
        if (concept == null)
        {
            throw new InvalidOperationException($"Concept with ID {restriction.ConceptId} not found");
        }

        // Verify user has permission to add restrictions
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            concept.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to add restrictions to ontology {concept.OntologyId}");
        }

        restriction.CreatedAt = DateTime.UtcNow;
        restriction.UpdatedAt = DateTime.UtcNow;
        var created = await _restrictionRepository.AddAsync(restriction);
        await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);

        return created;
    }

    public async Task<ConceptRestriction> UpdateAsync(ConceptRestriction restriction)
    {
        // Get concept to check permissions
        var concept = await _conceptRepository.GetByIdAsync(restriction.ConceptId);
        if (concept == null)
        {
            throw new InvalidOperationException($"Concept with ID {restriction.ConceptId} not found");
        }

        // Verify user has permission to edit restrictions
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            concept.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to edit restrictions in ontology {concept.OntologyId}");
        }

        restriction.UpdatedAt = DateTime.UtcNow;
        await _restrictionRepository.UpdateAsync(restriction);
        await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);

        return restriction;
    }

    public async Task DeleteAsync(int id)
    {
        var restriction = await _restrictionRepository.GetByIdAsync(id);
        if (restriction == null)
        {
            throw new InvalidOperationException($"Restriction with ID {id} not found");
        }

        var concept = await _conceptRepository.GetByIdAsync(restriction.ConceptId);
        if (concept == null)
        {
            throw new InvalidOperationException($"Concept with ID {restriction.ConceptId} not found");
        }

        // Verify user has permission to delete restrictions
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            concept.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.FullAccess);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to delete restrictions from ontology {concept.OntologyId}");
        }

        await _restrictionRepository.DeleteAsync(id);
        await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);
    }

    public async Task<ConceptRestriction?> GetByIdAsync(int id)
    {
        return await _restrictionRepository.GetByIdAsync(id);
    }

    public async Task<ConceptRestriction?> GetWithDetailsAsync(int id)
    {
        return await _restrictionRepository.GetWithDetailsAsync(id);
    }

    public async Task<IEnumerable<ConceptRestriction>> GetByConceptIdAsync(int conceptId)
    {
        return await _restrictionRepository.GetByConceptIdAsync(conceptId);
    }

    public async Task<IEnumerable<ConceptRestriction>> GetByOntologyIdAsync(int ontologyId)
    {
        return await _restrictionRepository.GetByOntologyIdAsync(ontologyId);
    }

    public async Task DeleteByConceptIdAsync(int conceptId)
    {
        var concept = await _conceptRepository.GetByIdAsync(conceptId);
        if (concept == null)
        {
            throw new InvalidOperationException($"Concept with ID {conceptId} not found");
        }

        // Verify user has permission
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            concept.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.FullAccess);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to delete restrictions from ontology {concept.OntologyId}");
        }

        await _restrictionRepository.DeleteByConceptIdAsync(conceptId);
        await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidatePropertyAsync(int conceptId, string propertyName, string? value)
    {
        var restrictions = await _restrictionRepository.GetByConceptIdAsync(conceptId);
        var propertyRestrictions = restrictions.Where(r => r.PropertyName == propertyName).ToList();

        if (!propertyRestrictions.Any())
        {
            return (true, null);
        }

        foreach (var restriction in propertyRestrictions)
        {
            var validationResult = ValidateAgainstRestriction(restriction, value);
            if (!validationResult.IsValid)
            {
                return validationResult;
            }
        }

        return (true, null);
    }

    private (bool IsValid, string? ErrorMessage) ValidateAgainstRestriction(ConceptRestriction restriction, string? value)
    {
        switch (restriction.RestrictionType)
        {
            case RestrictionTypes.Required:
                if (string.IsNullOrWhiteSpace(value))
                {
                    return (false, $"Property '{restriction.PropertyName}' is required");
                }
                break;

            case RestrictionTypes.ValueType:
                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(restriction.ValueType))
                {
                    if (!ValidateDataType(value, restriction.ValueType))
                    {
                        return (false, $"Property '{restriction.PropertyName}' must be of type {restriction.ValueType}");
                    }
                }
                break;

            case RestrictionTypes.Range:
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (!ValidateRange(value, restriction.MinValue, restriction.MaxValue))
                    {
                        return (false, $"Property '{restriction.PropertyName}' must be between {restriction.MinValue} and {restriction.MaxValue}");
                    }
                }
                break;

            case RestrictionTypes.Enumeration:
                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(restriction.AllowedValues))
                {
                    var allowedValues = restriction.AllowedValues.Split(',').Select(v => v.Trim()).ToList();
                    if (!allowedValues.Contains(value))
                    {
                        return (false, $"Property '{restriction.PropertyName}' must be one of: {restriction.AllowedValues}");
                    }
                }
                break;

            case RestrictionTypes.Pattern:
                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(restriction.Pattern))
                {
                    if (!Regex.IsMatch(value, restriction.Pattern))
                    {
                        return (false, $"Property '{restriction.PropertyName}' does not match the required pattern");
                    }
                }
                break;
        }

        return (true, null);
    }

    private bool ValidateDataType(string value, string dataType)
    {
        return dataType.ToLower() switch
        {
            "string" => true, // Any string is valid
            "integer" or "int" => int.TryParse(value, out _),
            "decimal" or "number" => decimal.TryParse(value, out _),
            "boolean" or "bool" => bool.TryParse(value, out _),
            "date" => DateTime.TryParse(value, out _),
            "uri" or "url" => Uri.TryCreate(value, UriKind.Absolute, out _),
            _ => true // Unknown types pass validation
        };
    }

    private bool ValidateRange(string value, string? minValue, string? maxValue)
    {
        // Try to parse as decimal for numeric comparison
        if (decimal.TryParse(value, out var numericValue))
        {
            if (!string.IsNullOrWhiteSpace(minValue) && decimal.TryParse(minValue, out var min))
            {
                if (numericValue < min) return false;
            }
            if (!string.IsNullOrWhiteSpace(maxValue) && decimal.TryParse(maxValue, out var max))
            {
                if (numericValue > max) return false;
            }
            return true;
        }

        // Try to parse as date for date comparison
        if (DateTime.TryParse(value, out var dateValue))
        {
            if (!string.IsNullOrWhiteSpace(minValue) && DateTime.TryParse(minValue, out var minDate))
            {
                if (dateValue < minDate) return false;
            }
            if (!string.IsNullOrWhiteSpace(maxValue) && DateTime.TryParse(maxValue, out var maxDate))
            {
                if (dateValue > maxDate) return false;
            }
            return true;
        }

        // If we can't parse, assume valid
        return true;
    }
}
