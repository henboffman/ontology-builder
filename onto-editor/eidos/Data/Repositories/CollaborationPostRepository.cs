using Eidos.Models;
using Microsoft.EntityFrameworkCore;

namespace Eidos.Data.Repositories;

public class CollaborationPostRepository : BaseRepository<CollaborationPost>, ICollaborationPostRepository
{
    public CollaborationPostRepository(IDbContextFactory<OntologyDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<IEnumerable<CollaborationPost>> GetActivePostsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CollaborationPosts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Ontology)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.LastBumpedAt ?? p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<CollaborationPost>> SearchPostsAsync(
        string? searchTerm = null,
        string? domain = null,
        string? skillLevel = null,
        bool activeOnly = true)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.CollaborationPosts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Ontology)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearch = searchTerm.ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(lowerSearch) ||
                p.Description.ToLower().Contains(lowerSearch) ||
                (p.Tags != null && p.Tags.ToLower().Contains(lowerSearch)) ||
                (p.Domain != null && p.Domain.ToLower().Contains(lowerSearch)));
        }

        if (!string.IsNullOrWhiteSpace(domain))
        {
            query = query.Where(p => p.Domain == domain);
        }

        if (!string.IsNullOrWhiteSpace(skillLevel))
        {
            query = query.Where(p => p.SkillLevel == skillLevel);
        }

        return await query
            .OrderByDescending(p => p.LastBumpedAt ?? p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<CollaborationPost>> GetPostsByUserAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CollaborationPosts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Ontology)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<CollaborationPost?> GetPostWithDetailsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CollaborationPosts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Ontology)
            .Include(p => p.Responses)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task IncrementViewCountAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var post = await context.CollaborationPosts.FindAsync(id);
        if (post != null)
        {
            post.ViewCount++;
            post.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateResponseCountAsync(int postId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var post = await context.CollaborationPosts.FindAsync(postId);
        if (post != null)
        {
            post.ResponseCount = await context.CollaborationResponses
                .CountAsync(r => r.CollaborationPostId == postId);
            post.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
}
