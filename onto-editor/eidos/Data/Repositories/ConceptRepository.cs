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

        // Use EF.Functions.Like for case-insensitive search that can leverage indexes
        // Pattern: %query% for "contains" behavior
        var searchPattern = $"%{query}%";

        return await context.Concepts
            .Where(c =>
                EF.Functions.Like(c.Name, searchPattern) ||
                (c.Definition != null && EF.Functions.Like(c.Definition, searchPattern)) ||
                (c.SimpleExplanation != null && EF.Functions.Like(c.SimpleExplanation, searchPattern)) ||
                (c.Category != null && EF.Functions.Like(c.Category, searchPattern)))
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

    public override async Task UpdateAsync(Concept concept)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Detach navigation properties to avoid tracking conflicts
        var entry = context.Entry(concept);
        entry.State = EntityState.Modified;

        // Explicitly set which scalar properties to update (exclude navigation properties)
        entry.Property(c => c.OntologyId).IsModified = true;
        entry.Property(c => c.Name).IsModified = true;
        entry.Property(c => c.Definition).IsModified = true;
        entry.Property(c => c.SimpleExplanation).IsModified = true;
        entry.Property(c => c.Examples).IsModified = true;
        entry.Property(c => c.PositionX).IsModified = true;
        entry.Property(c => c.PositionY).IsModified = true;
        entry.Property(c => c.Category).IsModified = true;
        entry.Property(c => c.Color).IsModified = true;
        entry.Property(c => c.SourceOntology).IsModified = true;

        // Don't track or update navigation properties
        entry.Reference(c => c.Ontology).IsModified = false;
        entry.Collection(c => c.Properties).IsModified = false;
        entry.Collection(c => c.ConceptProperties).IsModified = false;
        entry.Collection(c => c.RelationshipsAsSource).IsModified = false;
        entry.Collection(c => c.RelationshipsAsTarget).IsModified = false;
        entry.Collection(c => c.Restrictions).IsModified = false;

        await context.SaveChangesAsync();
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
