using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services.Interfaces;

namespace Eidos.Services;

/// <summary>
/// Service for managing concept properties
/// </summary>
public class PropertyService : IPropertyService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IUserService _userService;
    private readonly IOntologyShareService _shareService;

    public PropertyService(
        IDbContextFactory<OntologyDbContext> contextFactory,
        IOntologyRepository ontologyRepository,
        IUserService userService,
        IOntologyShareService shareService)
    {
        _contextFactory = contextFactory;
        _ontologyRepository = ontologyRepository;
        _userService = userService;
        _shareService = shareService;
    }

    public async Task<Property> CreateAsync(Property property)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var concept = await context.Concepts.FindAsync(property.ConceptId);
        if (concept == null)
        {
            throw new InvalidOperationException($"Concept with ID {property.ConceptId} not found");
        }

        // Verify user has permission to add properties (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            concept.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAndAdd);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to add properties to ontology {concept.OntologyId}");
        }

        context.Properties.Add(property);
        await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);
        await context.SaveChangesAsync();
        return property;
    }

    public async Task<Property> UpdateAsync(Property property)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var concept = await context.Concepts.FindAsync(property.ConceptId);
        if (concept == null)
        {
            throw new InvalidOperationException($"Concept with ID {property.ConceptId} not found");
        }

        // Verify user has permission to edit properties (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            concept.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to edit properties in ontology {concept.OntologyId}");
        }

        context.Properties.Update(property);
        await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);
        await context.SaveChangesAsync();
        return property;
    }

    public async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var property = await context.Properties.FindAsync(id);
        if (property == null)
        {
            return; // Property doesn't exist, nothing to delete
        }

        var concept = await context.Concepts.FindAsync(property.ConceptId);
        if (concept == null)
        {
            throw new InvalidOperationException($"Concept with ID {property.ConceptId} not found");
        }

        // Verify user has permission to delete properties (defense in depth)
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            concept.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to delete properties from ontology {concept.OntologyId}");
        }

        context.Properties.Remove(property);
        await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Property>> GetByConceptIdAsync(int conceptId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Properties
            .Where(p => p.ConceptId == conceptId)
            .AsNoTracking()
            .ToListAsync();
    }
}
