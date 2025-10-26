using Microsoft.EntityFrameworkCore;
using Eidos.Models;

namespace Eidos.Data.Repositories;

public class IndividualRepository : BaseRepository<Individual>, IIndividualRepository
{
    public IndividualRepository(IDbContextFactory<OntologyDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<Individual?> GetWithPropertiesAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Individuals
            .Include(i => i.Properties)
            .Include(i => i.Concept)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Individual?> GetWithDetailsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Individuals
            .Include(i => i.Properties)
            .Include(i => i.Concept)
            .Include(i => i.RelationshipsAsSource)
                .ThenInclude(r => r.TargetIndividual)
            .Include(i => i.RelationshipsAsTarget)
                .ThenInclude(r => r.SourceIndividual)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Individual>> GetByOntologyIdAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Individuals
            .Include(i => i.Concept)
            .Include(i => i.Properties)
            .Where(i => i.OntologyId == ontologyId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Individual>> GetByConceptIdAsync(int conceptId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Individuals
            .Include(i => i.Properties)
            .Where(i => i.ConceptId == conceptId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Individual>> SearchAsync(string query, int? ontologyId = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var lowerQuery = query.ToLower();

        var queryable = context.Individuals
            .Include(i => i.Concept)
            .Where(i =>
                i.Name.ToLower().Contains(lowerQuery) ||
                (i.Description != null && i.Description.ToLower().Contains(lowerQuery)) ||
                (i.Label != null && i.Label.ToLower().Contains(lowerQuery)));

        if (ontologyId.HasValue)
        {
            queryable = queryable.Where(i => i.OntologyId == ontologyId.Value);
        }

        return await queryable
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetCountByConceptIdAsync(int conceptId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Individuals
            .Where(i => i.ConceptId == conceptId)
            .CountAsync();
    }

    public override async Task<Individual> AddAsync(Individual individual)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        individual.CreatedAt = DateTime.UtcNow;
        individual.UpdatedAt = DateTime.UtcNow;
        context.Individuals.Add(individual);
        await context.SaveChangesAsync();
        return individual;
    }

    public override async Task UpdateAsync(Individual individual)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        individual.UpdatedAt = DateTime.UtcNow;
        context.Individuals.Update(individual);
        await context.SaveChangesAsync();
    }

    public override async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var individual = await context.Individuals
            .Include(i => i.Properties)
            .Include(i => i.RelationshipsAsSource)
            .Include(i => i.RelationshipsAsTarget)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (individual != null)
        {
            context.Individuals.Remove(individual);
            await context.SaveChangesAsync();
        }
    }
}
