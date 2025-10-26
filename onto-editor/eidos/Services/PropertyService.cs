using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Services.Interfaces;

namespace Eidos.Services;

/// <summary>
/// Service for managing concept properties
/// </summary>
public class PropertyService : IPropertyService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly IOntologyRepository _ontologyRepository;

    public PropertyService(
        IDbContextFactory<OntologyDbContext> contextFactory,
        IOntologyRepository ontologyRepository)
    {
        _contextFactory = contextFactory;
        _ontologyRepository = ontologyRepository;
    }

    public async Task<Property> CreateAsync(Property property)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Properties.Add(property);

        var concept = await context.Concepts.FindAsync(property.ConceptId);
        if (concept != null)
        {
            await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);
        }

        await context.SaveChangesAsync();
        return property;
    }

    public async Task<Property> UpdateAsync(Property property)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Properties.Update(property);

        var concept = await context.Concepts.FindAsync(property.ConceptId);
        if (concept != null)
        {
            await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);
        }

        await context.SaveChangesAsync();
        return property;
    }

    public async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var property = await context.Properties.FindAsync(id);
        if (property != null)
        {
            var concept = await context.Concepts.FindAsync(property.ConceptId);
            context.Properties.Remove(property);

            if (concept != null)
            {
                await _ontologyRepository.UpdateTimestampAsync(concept.OntologyId);
            }

            await context.SaveChangesAsync();
        }
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
