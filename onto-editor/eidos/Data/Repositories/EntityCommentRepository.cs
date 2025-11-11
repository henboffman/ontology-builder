using Microsoft.EntityFrameworkCore;
using Eidos.Models;

namespace Eidos.Data.Repositories;

public class EntityCommentRepository : BaseRepository<EntityComment>, IEntityCommentRepository
{
    public EntityCommentRepository(IDbContextFactory<OntologyDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<IEnumerable<EntityComment>> GetByEntityAsync(int ontologyId, string entityType, int entityId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EntityComments
            .Include(c => c.User)
            .Include(c => c.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .Include(c => c.Replies)
                .ThenInclude(r => r.User)
            .Where(c => c.OntologyId == ontologyId &&
                       c.EntityType == entityType &&
                       c.EntityId == entityId)
            .OrderBy(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<EntityComment>> GetTopLevelCommentsByEntityAsync(int ontologyId, string entityType, int entityId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EntityComments
            .Include(c => c.User)
            .Include(c => c.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .Include(c => c.Replies)
                .ThenInclude(r => r.User)
            .Where(c => c.OntologyId == ontologyId &&
                       c.EntityType == entityType &&
                       c.EntityId == entityId &&
                       c.ParentCommentId == null)
            .OrderBy(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<EntityComment?> GetWithRepliesAsync(int commentId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EntityComments
            .Include(c => c.User)
            .Include(c => c.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .Include(c => c.Replies)
                .ThenInclude(r => r.User)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Mentions)
                    .ThenInclude(m => m.MentionedUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == commentId);
    }

    public async Task<IEnumerable<EntityComment>> GetByUserIdAsync(string userId, int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EntityComments
            .Include(c => c.User)
            .Where(c => c.UserId == userId && c.OntologyId == ontologyId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<EntityComment>> GetUnresolvedThreadsByOntologyAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EntityComments
            .Include(c => c.User)
            .Include(c => c.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .Where(c => c.OntologyId == ontologyId &&
                       c.ParentCommentId == null &&
                       !c.IsResolved)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<CommentMention> AddMentionAsync(CommentMention mention)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.CommentMentions.Add(mention);
        await context.SaveChangesAsync();
        return mention;
    }

    public async Task<IEnumerable<CommentMention>> GetUnreadMentionsAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CommentMentions
            .Include(m => m.Comment)
                .ThenInclude(c => c.User)
            .Include(m => m.Comment)
                .ThenInclude(c => c.Ontology)
            .Where(m => m.MentionedUserId == userId && !m.HasViewed)
            .OrderByDescending(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task MarkMentionAsViewedAsync(int mentionId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var mention = await context.CommentMentions.FindAsync(mentionId);
        if (mention != null)
        {
            mention.HasViewed = true;
            mention.ViewedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<EntityCommentCount> GetOrCreateCommentCountAsync(int ontologyId, string entityType, int entityId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var count = await context.EntityCommentCounts
            .FirstOrDefaultAsync(c => c.OntologyId == ontologyId &&
                                     c.EntityType == entityType &&
                                     c.EntityId == entityId);

        if (count == null)
        {
            count = new EntityCommentCount
            {
                OntologyId = ontologyId,
                EntityType = entityType,
                EntityId = entityId,
                TotalComments = 0,
                UnresolvedThreads = 0
            };
            context.EntityCommentCounts.Add(count);
            await context.SaveChangesAsync();
        }

        return count;
    }

    public async Task IncrementCommentCountAsync(int ontologyId, string entityType, int entityId, bool isTopLevel)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var count = await GetOrCreateCommentCountAsync(ontologyId, entityType, entityId);

        var trackedCount = context.EntityCommentCounts.Find(count.Id);
        if (trackedCount != null)
        {
            trackedCount.TotalComments++;
            if (isTopLevel)
            {
                trackedCount.UnresolvedThreads++;
            }
            await context.SaveChangesAsync();
        }
    }

    public async Task DecrementCommentCountAsync(int ontologyId, string entityType, int entityId, bool isTopLevel)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var count = await context.EntityCommentCounts
            .FirstOrDefaultAsync(c => c.OntologyId == ontologyId &&
                                     c.EntityType == entityType &&
                                     c.EntityId == entityId);

        if (count != null)
        {
            count.TotalComments = Math.Max(0, count.TotalComments - 1);
            if (isTopLevel)
            {
                count.UnresolvedThreads = Math.Max(0, count.UnresolvedThreads - 1);
            }
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateUnresolvedThreadCountAsync(int ontologyId, string entityType, int entityId, bool isResolved)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var count = await context.EntityCommentCounts
            .FirstOrDefaultAsync(c => c.OntologyId == ontologyId &&
                                     c.EntityType == entityType &&
                                     c.EntityId == entityId);

        if (count != null)
        {
            if (isResolved)
            {
                count.UnresolvedThreads = Math.Max(0, count.UnresolvedThreads - 1);
            }
            else
            {
                count.UnresolvedThreads++;
            }
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<EntityCommentCount>> GetCommentCountsAsync(int ontologyId, string entityType, IEnumerable<int> entityIds)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var entityIdList = entityIds.ToList();
        return await context.EntityCommentCounts
            .Where(c => c.OntologyId == ontologyId &&
                       c.EntityType == entityType &&
                       entityIdList.Contains(c.EntityId))
            .AsNoTracking()
            .ToListAsync();
    }
}
