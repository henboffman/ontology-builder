using Microsoft.EntityFrameworkCore;
using Eidos.Models;

namespace Eidos.Data.Repositories;

public class RestrictionRepository : BaseRepository<ConceptRestriction>, IRestrictionRepository
{
    public RestrictionRepository(IDbContextFactory<OntologyDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<IEnumerable<ConceptRestriction>> GetByConceptIdAsync(int conceptId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ConceptRestrictions
            .Where(r => r.ConceptId == conceptId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ConceptRestriction?> GetWithDetailsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ConceptRestrictions
            .Include(r => r.Concept)
            .Include(r => r.AllowedConcept)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<ConceptRestriction>> GetByOntologyIdAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ConceptRestrictions
            .Include(r => r.Concept)
            .Where(r => r.Concept.OntologyId == ontologyId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task DeleteByConceptIdAsync(int conceptId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var restrictions = await context.ConceptRestrictions
            .Where(r => r.ConceptId == conceptId)
            .ToListAsync();

        if (restrictions.Any())
        {
            context.ConceptRestrictions.RemoveRange(restrictions);
            await context.SaveChangesAsync();
        }
    }

    public override async Task<ConceptRestriction> AddAsync(ConceptRestriction restriction)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        restriction.CreatedAt = DateTime.UtcNow;
        restriction.UpdatedAt = DateTime.UtcNow;
        context.ConceptRestrictions.Add(restriction);
        await context.SaveChangesAsync();
        return restriction;
    }

    public override async Task UpdateAsync(ConceptRestriction restriction)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        restriction.UpdatedAt = DateTime.UtcNow;
        context.ConceptRestrictions.Update(restriction);
        await context.SaveChangesAsync();
    }

    public override async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var restriction = await context.ConceptRestrictions.FindAsync(id);
        if (restriction != null)
        {
            context.ConceptRestrictions.Remove(restriction);
            await context.SaveChangesAsync();
        }
    }
}
