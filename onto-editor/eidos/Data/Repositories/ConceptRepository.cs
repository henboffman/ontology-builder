using Microsoft.EntityFrameworkCore;
using Eidos.Models;

namespace Eidos.Data.Repositories;

public class ConceptRepository : BaseRepository<Concept>, IConceptRepository
{
    public ConceptRepository(IDbContextFactory<OntologyDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<Concept?> GetWithPropertiesAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Concepts
            .Include(c => c.Properties)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Concept>> GetByOntologyIdAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Concepts
            .Where(c => c.OntologyId == ontologyId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Concept>> SearchAsync(string query)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var lowerQuery = query.ToLower();

        return await context.Concepts
            .Where(c =>
                c.Name.ToLower().Contains(lowerQuery) ||
                (c.Definition != null && c.Definition.ToLower().Contains(lowerQuery)) ||
                (c.SimpleExplanation != null && c.SimpleExplanation.ToLower().Contains(lowerQuery)) ||
                (c.Category != null && c.Category.ToLower().Contains(lowerQuery)))
            .AsNoTracking()
            .ToListAsync();
    }

    public override async Task<Concept> AddAsync(Concept concept)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        concept.CreatedAt = DateTime.UtcNow;
        context.Concepts.Add(concept);
        await context.SaveChangesAsync();
        return concept;
    }

    public override async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var concept = await context.Concepts
            .Include(c => c.Properties)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (concept != null)
        {
            context.Concepts.Remove(concept);
            await context.SaveChangesAsync();
        }
    }
}
