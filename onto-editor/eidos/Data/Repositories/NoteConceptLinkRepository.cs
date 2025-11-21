using Eidos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository for NoteConceptLink data access operations
/// Manages automatic detection and tracking of concept mentions in notes
/// Uses DbContextFactory to ensure each operation gets its own DbContext instance
/// </summary>
public class NoteConceptLinkRepository
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly ILogger<NoteConceptLinkRepository> _logger;

    public NoteConceptLinkRepository(
        IDbContextFactory<OntologyDbContext> contextFactory,
        ILogger<NoteConceptLinkRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Get all concept links for a specific note
    /// </summary>
    public async Task<List<NoteConceptLink>> GetByNoteIdAsync(int noteId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.NoteConceptLinks
                .Where(ncl => ncl.NoteId == noteId)
                .Include(ncl => ncl.Concept)
                    .ThenInclude(c => c.RelationshipsAsSource)
                        .ThenInclude(r => r.TargetConcept)
                .Include(ncl => ncl.Concept)
                    .ThenInclude(c => c.RelationshipsAsTarget)
                        .ThenInclude(r => r.SourceConcept)
                .OrderByDescending(ncl => ncl.TotalMentions)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting concept links for note {NoteId}", noteId);
            throw;
        }
    }

    /// <summary>
    /// Get all notes that mention a specific concept (backlinks)
    /// </summary>
    public async Task<List<NoteConceptLink>> GetByConceptIdAsync(int conceptId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.NoteConceptLinks
                .Where(ncl => ncl.ConceptId == conceptId)
                .Include(ncl => ncl.Note)
                .OrderByDescending(ncl => ncl.UpdatedAt)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting note mentions for concept {ConceptId}", conceptId);
            throw;
        }
    }

    /// <summary>
    /// Get a specific link between a note and a concept
    /// </summary>
    public async Task<NoteConceptLink?> GetLinkAsync(int noteId, int conceptId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.NoteConceptLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(ncl => ncl.NoteId == noteId && ncl.ConceptId == conceptId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting link between note {NoteId} and concept {ConceptId}", noteId, conceptId);
            throw;
        }
    }

    /// <summary>
    /// Create a new concept link
    /// </summary>
    public async Task<NoteConceptLink> CreateAsync(NoteConceptLink link)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            link.CreatedAt = DateTime.UtcNow;
            link.UpdatedAt = DateTime.UtcNow;

            context.NoteConceptLinks.Add(link);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created concept link between note {NoteId} and concept {ConceptId} ({Mentions} mentions)",
                link.NoteId, link.ConceptId, link.TotalMentions);

            return link;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating concept link for note {NoteId} and concept {ConceptId}",
                link.NoteId, link.ConceptId);
            throw;
        }
    }

    /// <summary>
    /// Update an existing concept link (e.g., mention count changed)
    /// </summary>
    public async Task UpdateAsync(NoteConceptLink link)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            link.UpdatedAt = DateTime.UtcNow;

            context.NoteConceptLinks.Update(link);
            await context.SaveChangesAsync();

            _logger.LogInformation("Updated concept link {LinkId} ({Mentions} mentions)",
                link.Id, link.TotalMentions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating concept link {LinkId}", link.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete a specific concept link
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var link = await context.NoteConceptLinks.FindAsync(id);
            if (link != null)
            {
                context.NoteConceptLinks.Remove(link);
                await context.SaveChangesAsync();

                _logger.LogInformation("Deleted concept link {LinkId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting concept link {LinkId}", id);
            throw;
        }
    }

    /// <summary>
    /// Delete all concept links for a specific note
    /// Used when re-scanning a note or when a note is deleted
    /// </summary>
    public async Task DeleteByNoteIdAsync(int noteId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Note: Using load-remove-save pattern for compatibility with in-memory database tests
            var links = await context.NoteConceptLinks
                .Where(ncl => ncl.NoteId == noteId)
                .ToListAsync();

            if (links.Any())
            {
                context.NoteConceptLinks.RemoveRange(links);
                await context.SaveChangesAsync();
            }

            _logger.LogInformation("Deleted all concept links for note {NoteId}", noteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting concept links for note {NoteId}", noteId);
            throw;
        }
    }

    /// <summary>
    /// Delete all concept links for a specific concept
    /// Used when a concept is deleted or renamed
    /// </summary>
    public async Task DeleteByConceptIdAsync(int conceptId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Note: Using load-remove-save pattern for compatibility with in-memory database tests
            var links = await context.NoteConceptLinks
                .Where(ncl => ncl.ConceptId == conceptId)
                .ToListAsync();

            if (links.Any())
            {
                context.NoteConceptLinks.RemoveRange(links);
                await context.SaveChangesAsync();
            }

            _logger.LogInformation("Deleted all concept links for concept {ConceptId}", conceptId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting concept links for concept {ConceptId}", conceptId);
            throw;
        }
    }

    /// <summary>
    /// Batch update or create links for a note
    /// Used by ConceptDetectionService after scanning note content
    /// </summary>
    public async Task UpsertLinksAsync(int noteId, List<NoteConceptLink> newLinks)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Remove existing links for this note
            // Note: Using load-remove-save pattern for compatibility with in-memory database tests
            var existingLinks = await context.NoteConceptLinks
                .Where(ncl => ncl.NoteId == noteId)
                .ToListAsync();

            if (existingLinks.Any())
            {
                context.NoteConceptLinks.RemoveRange(existingLinks);
            }

            // Add new links
            if (newLinks.Any())
            {
                foreach (var link in newLinks)
                {
                    link.CreatedAt = DateTime.UtcNow;
                    link.UpdatedAt = DateTime.UtcNow;
                }

                context.NoteConceptLinks.AddRange(newLinks);
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("Upserted {Count} concept links for note {NoteId}", newLinks.Count, noteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting concept links for note {NoteId}", noteId);
            throw;
        }
    }

    /// <summary>
    /// Get count of notes that mention a specific concept
    /// </summary>
    public async Task<int> GetMentionCountAsync(int conceptId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.NoteConceptLinks
                .Where(ncl => ncl.ConceptId == conceptId)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mention count for concept {ConceptId}", conceptId);
            throw;
        }
    }

    /// <summary>
    /// Get all concept links in a workspace (via notes)
    /// Useful for building note network visualization
    /// </summary>
    public async Task<List<NoteConceptLink>> GetByWorkspaceIdAsync(int workspaceId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.NoteConceptLinks
                .Include(ncl => ncl.Note)
                .Include(ncl => ncl.Concept)
                .Where(ncl => ncl.Note.WorkspaceId == workspaceId)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting concept links for workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }
}
