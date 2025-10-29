using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Microsoft.EntityFrameworkCore;

namespace Eidos.Services;

public interface ICollaborationBoardService
{
    Task<IEnumerable<CollaborationPost>> GetActivePostsAsync();
    Task<IEnumerable<CollaborationPost>> SearchPostsAsync(string? searchTerm = null, string? domain = null, string? skillLevel = null);
    Task<CollaborationPost?> GetPostDetailsAsync(int id, bool incrementView = false);
    Task<IEnumerable<CollaborationPost>> GetMyPostsAsync(string userId);
    Task<CollaborationPost> CreatePostAsync(CollaborationPost post);
    Task<CollaborationPost> UpdatePostAsync(CollaborationPost post);
    Task DeletePostAsync(int id);
    Task<bool> TogglePostActiveStatusAsync(int id);
    Task<CollaborationResponse> AddResponseAsync(CollaborationResponse response);
    Task<IEnumerable<CollaborationResponse>> GetPostResponsesAsync(int postId);
    Task<bool> UpdateResponseStatusAsync(int responseId, string newStatus);
}

public class CollaborationBoardService : ICollaborationBoardService
{
    private readonly ICollaborationPostRepository _postRepository;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;

    public CollaborationBoardService(
        ICollaborationPostRepository postRepository,
        IDbContextFactory<OntologyDbContext> contextFactory)
    {
        _postRepository = postRepository;
        _contextFactory = contextFactory;
    }

    public async Task<IEnumerable<CollaborationPost>> GetActivePostsAsync()
    {
        return await _postRepository.GetActivePostsAsync();
    }

    public async Task<IEnumerable<CollaborationPost>> SearchPostsAsync(
        string? searchTerm = null,
        string? domain = null,
        string? skillLevel = null)
    {
        return await _postRepository.SearchPostsAsync(searchTerm, domain, skillLevel);
    }

    public async Task<CollaborationPost?> GetPostDetailsAsync(int id, bool incrementView = false)
    {
        if (incrementView)
        {
            await _postRepository.IncrementViewCountAsync(id);
        }

        return await _postRepository.GetPostWithDetailsAsync(id);
    }

    public async Task<IEnumerable<CollaborationPost>> GetMyPostsAsync(string userId)
    {
        return await _postRepository.GetPostsByUserAsync(userId);
    }

    public async Task<CollaborationPost> CreatePostAsync(CollaborationPost post)
    {
        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        post.IsActive = true;
        post.ViewCount = 0;
        post.ResponseCount = 0;

        return await _postRepository.AddAsync(post);
    }

    public async Task<CollaborationPost> UpdatePostAsync(CollaborationPost post)
    {
        post.UpdatedAt = DateTime.UtcNow;
        await _postRepository.UpdateAsync(post);
        return post;
    }

    public async Task DeletePostAsync(int id)
    {
        await _postRepository.DeleteAsync(id);
    }

    public async Task<bool> TogglePostActiveStatusAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var post = await context.CollaborationPosts.FindAsync(id);

        if (post == null)
            return false;

        post.IsActive = !post.IsActive;
        post.UpdatedAt = DateTime.UtcNow;

        if (post.IsActive)
        {
            post.LastBumpedAt = DateTime.UtcNow; // Bump to top when reactivating
        }

        await context.SaveChangesAsync();
        return post.IsActive;
    }

    public async Task<CollaborationResponse> AddResponseAsync(CollaborationResponse response)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        response.CreatedAt = DateTime.UtcNow;
        response.Status = "Pending";

        context.CollaborationResponses.Add(response);
        await context.SaveChangesAsync();

        // Update response count on the post
        await _postRepository.UpdateResponseCountAsync(response.CollaborationPostId);

        return response;
    }

    public async Task<IEnumerable<CollaborationResponse>> GetPostResponsesAsync(int postId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CollaborationResponses
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.CollaborationPostId == postId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateResponseStatusAsync(int responseId, string newStatus)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var response = await context.CollaborationResponses.FindAsync(responseId);

        if (response == null)
            return false;

        response.Status = newStatus;
        await context.SaveChangesAsync();
        return true;
    }
}
