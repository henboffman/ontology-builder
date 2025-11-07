using Microsoft.EntityFrameworkCore;
using Eidos.Models;
using Eidos.Models.Enums;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository implementation for OntologyLink operations
/// Provides data access for both external (URI-based) and internal (virtualized) ontology links
/// </summary>
public class OntologyLinkRepository : BaseRepository<OntologyLink>, IOntologyLinkRepository
{
    public OntologyLinkRepository(IDbContextFactory<OntologyDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OntologyLink>> GetByOntologyIdAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OntologyLinks
            .Where(l => l.OntologyId == ontologyId)
            .Include(l => l.LinkedOntology) // Eager load for internal links
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<OntologyLink?> GetWithRelatedAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OntologyLinks
            .Include(l => l.Ontology)
            .Include(l => l.LinkedOntology)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OntologyLink>> GetInternalLinksByOntologyIdAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OntologyLinks
            .Where(l => l.OntologyId == ontologyId && l.LinkType == LinkType.Internal)
            .Include(l => l.LinkedOntology)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OntologyLink>> GetExternalLinksByOntologyIdAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OntologyLinks
            .Where(l => l.OntologyId == ontologyId && l.LinkType == LinkType.External)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetDependentOntologyIdsAsync(int targetOntologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OntologyLinks
            .AsNoTracking()
            .Where(l => l.LinkType == LinkType.Internal && l.LinkedOntologyId == targetOntologyId)
            .Select(l => l.OntologyId)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> LinkExistsAsync(int parentOntologyId, int linkedOntologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OntologyLinks
            .AnyAsync(l =>
                l.OntologyId == parentOntologyId &&
                l.LinkType == LinkType.Internal &&
                l.LinkedOntologyId == linkedOntologyId);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OntologyLink>> GetLinksNeedingSyncAsync(int? ontologyId = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.OntologyLinks
            .Where(l => l.LinkType == LinkType.Internal && l.UpdateAvailable);

        if (ontologyId.HasValue)
        {
            query = query.Where(l => l.OntologyId == ontologyId.Value);
        }

        return await query
            .Include(l => l.LinkedOntology)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc/>
    public override async Task<OntologyLink> AddAsync(OntologyLink link)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        link.UpdatedAt = DateTime.UtcNow;

        // Set LastSyncedAt for internal links
        if (link.LinkType == LinkType.Internal)
        {
            link.LastSyncedAt = DateTime.UtcNow;
        }

        context.OntologyLinks.Add(link);
        await context.SaveChangesAsync();
        return link;
    }

    /// <inheritdoc/>
    public override async Task UpdateAsync(OntologyLink link)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        link.UpdatedAt = DateTime.UtcNow;
        context.OntologyLinks.Update(link);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var link = await context.OntologyLinks
            .FirstOrDefaultAsync(l => l.Id == id);

        if (link != null)
        {
            context.OntologyLinks.Remove(link);
            await context.SaveChangesAsync();
        }
    }
}
