using Eidos.Models;

namespace Eidos.Data.Repositories;

public interface ICollaborationPostRepository : IRepository<CollaborationPost>
{
    /// <summary>
    /// Get all active collaboration posts with user details
    /// </summary>
    Task<IEnumerable<CollaborationPost>> GetActivePostsAsync();

    /// <summary>
    /// Get posts with filtering
    /// </summary>
    Task<IEnumerable<CollaborationPost>> SearchPostsAsync(string? searchTerm = null, string? domain = null, string? skillLevel = null, bool activeOnly = true);

    /// <summary>
    /// Get posts by a specific user
    /// </summary>
    Task<IEnumerable<CollaborationPost>> GetPostsByUserAsync(string userId);

    /// <summary>
    /// Get a post with all related data (user, ontology, responses)
    /// </summary>
    Task<CollaborationPost?> GetPostWithDetailsAsync(int id);

    /// <summary>
    /// Increment view count for a post
    /// </summary>
    Task IncrementViewCountAsync(int id);

    /// <summary>
    /// Update response count for a post
    /// </summary>
    Task UpdateResponseCountAsync(int postId);
}
