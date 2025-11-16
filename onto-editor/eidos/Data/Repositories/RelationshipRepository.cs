using Microsoft.EntityFrameworkCore;
using Eidos.Models;

namespace Eidos.Data.Repositories;

public class RelationshipRepository : BaseRepository<Relationship>, IRelationshipRepository
{
    public RelationshipRepository(IDbContextFactory<OntologyDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<IEnumerable<Relationship>> GetByOntologyIdAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Relationships
            .Where(r => r.OntologyId == ontologyId)
            .Include(r => r.SourceConcept)
            .Include(r => r.TargetConcept)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Relationship>> GetByConceptIdAsync(int conceptId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Relationships
            .Where(r => r.SourceConceptId == conceptId || r.TargetConceptId == conceptId)
            .Include(r => r.SourceConcept)
            .Include(r => r.TargetConcept)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int sourceId, int targetId, string relationType)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Relationships
            .AsNoTracking()
            .AnyAsync(r => r.SourceConceptId == sourceId
                && r.TargetConceptId == targetId
                && r.RelationType == relationType);
    }

    public async Task<Relationship?> GetWithConceptsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Relationships
            .Include(r => r.SourceConcept)
            .Include(r => r.TargetConcept)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public override async Task<Relationship> AddAsync(Relationship relationship)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        relationship.CreatedAt = DateTime.UtcNow;
        context.Relationships.Add(relationship);
        await context.SaveChangesAsync();
        return relationship;
    }

    public override async Task UpdateAsync(Relationship relationship)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Detach navigation properties to avoid tracking conflicts
        // The Update method will only update the relationship's own properties
        var entry = context.Entry(relationship);
        entry.State = EntityState.Modified;

        // Explicitly set which properties to update (exclude navigation properties)
        entry.Property(r => r.SourceConceptId).IsModified = true;
        entry.Property(r => r.TargetConceptId).IsModified = true;
        entry.Property(r => r.RelationType).IsModified = true;
        entry.Property(r => r.Label).IsModified = true;
        entry.Property(r => r.Description).IsModified = true;

        // Don't track or update navigation properties
        entry.Reference(r => r.SourceConcept).IsModified = false;
        entry.Reference(r => r.TargetConcept).IsModified = false;
        entry.Reference(r => r.Ontology).IsModified = false;

        await context.SaveChangesAsync();
    }
}
