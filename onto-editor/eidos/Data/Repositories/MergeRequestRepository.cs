using Microsoft.EntityFrameworkCore;
using Eidos.Models;
using Eidos.Models.Enums;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository for merge request operations with EF Core
/// </summary>
public class MergeRequestRepository : BaseRepository<MergeRequest>, IMergeRequestRepository
{
    public MergeRequestRepository(IDbContextFactory<OntologyDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<MergeRequest?> GetWithDetailsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MergeRequests
            .Include(mr => mr.Changes)
            .Include(mr => mr.Comments)
                .ThenInclude(c => c.User)
            .Include(mr => mr.CreatedByUser)
            .Include(mr => mr.AssignedReviewer)
            .Include(mr => mr.ReviewedBy)
            .Include(mr => mr.Ontology)
            .AsNoTracking()
            .FirstOrDefaultAsync(mr => mr.Id == id);
    }

    public async Task<IEnumerable<MergeRequest>> GetByOntologyIdAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MergeRequests
            .Include(mr => mr.CreatedByUser)
            .Include(mr => mr.AssignedReviewer)
            .Include(mr => mr.Changes)
            .Where(mr => mr.OntologyId == ontologyId)
            .OrderByDescending(mr => mr.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<MergeRequest>> GetByOntologyIdAndStatusAsync(int ontologyId, MergeRequestStatus status)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MergeRequests
            .Include(mr => mr.CreatedByUser)
            .Include(mr => mr.AssignedReviewer)
            .Include(mr => mr.Changes)
            .Where(mr => mr.OntologyId == ontologyId && mr.Status == status)
            .OrderByDescending(mr => mr.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<MergeRequest>> GetByCreatedByUserIdAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MergeRequests
            .Include(mr => mr.Ontology)
            .Include(mr => mr.AssignedReviewer)
            .Include(mr => mr.Changes)
            .Where(mr => mr.CreatedByUserId == userId)
            .OrderByDescending(mr => mr.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<MergeRequest>> GetByAssignedReviewerAsync(string userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MergeRequests
            .Include(mr => mr.Ontology)
            .Include(mr => mr.CreatedByUser)
            .Include(mr => mr.Changes)
            .Where(mr => mr.AssignedReviewerUserId == userId)
            .OrderByDescending(mr => mr.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetPendingReviewCountAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MergeRequests
            .Where(mr => mr.OntologyId == ontologyId && mr.Status == MergeRequestStatus.PendingReview)
            .CountAsync();
    }

    public async Task<MergeRequestChange> AddChangeAsync(MergeRequestChange change)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        change.CreatedAt = DateTime.UtcNow;
        context.MergeRequestChanges.Add(change);
        await context.SaveChangesAsync();
        return change;
    }

    public async Task<MergeRequestComment> AddCommentAsync(MergeRequestComment comment)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        comment.CreatedAt = DateTime.UtcNow;
        context.MergeRequestComments.Add(comment);
        await context.SaveChangesAsync();
        return comment;
    }

    public async Task<IEnumerable<MergeRequestChange>> GetChangesAsync(int mergeRequestId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MergeRequestChanges
            .Where(c => c.MergeRequestId == mergeRequestId)
            .OrderBy(c => c.OrderIndex)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<MergeRequestComment>> GetCommentsAsync(int mergeRequestId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MergeRequestComments
            .Include(c => c.User)
            .Where(c => c.MergeRequestId == mergeRequestId)
            .OrderBy(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(int mergeRequestId, MergeRequestStatus status)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var mergeRequest = await context.MergeRequests.FindAsync(mergeRequestId);
        if (mergeRequest != null)
        {
            mergeRequest.Status = status;
            mergeRequest.UpdatedAt = DateTime.UtcNow;

            // Update review tracking fields based on status
            if (status == MergeRequestStatus.PendingReview && mergeRequest.SubmittedAt == null)
            {
                mergeRequest.SubmittedAt = DateTime.UtcNow;
            }
            else if (status == MergeRequestStatus.Approved || status == MergeRequestStatus.Rejected || status == MergeRequestStatus.ChangesRequested)
            {
                mergeRequest.ReviewedAt = DateTime.UtcNow;
            }
            else if (status == MergeRequestStatus.Merged)
            {
                mergeRequest.MergedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
    }

    public override async Task<MergeRequest> AddAsync(MergeRequest mergeRequest)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        mergeRequest.CreatedAt = DateTime.UtcNow;
        mergeRequest.UpdatedAt = DateTime.UtcNow;
        context.MergeRequests.Add(mergeRequest);
        await context.SaveChangesAsync();
        return mergeRequest;
    }

    public override async Task<MergeRequest> UpdateAsync(MergeRequest mergeRequest)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        mergeRequest.UpdatedAt = DateTime.UtcNow;
        context.MergeRequests.Update(mergeRequest);
        await context.SaveChangesAsync();
        return mergeRequest;
    }
}
