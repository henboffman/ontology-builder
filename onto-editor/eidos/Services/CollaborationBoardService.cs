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
    private readonly UserGroupService _userGroupService;
    private readonly ILogger<CollaborationBoardService> _logger;

    public CollaborationBoardService(
        ICollaborationPostRepository postRepository,
        IDbContextFactory<OntologyDbContext> contextFactory,
        UserGroupService userGroupService,
        ILogger<CollaborationBoardService> logger)
    {
        _postRepository = postRepository;
        _contextFactory = contextFactory;
        _userGroupService = userGroupService;
        _logger = logger;
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
        UserGroup? group = null;

        try
        {
            _logger.LogInformation("Creating collaboration post '{Title}' for user {UserId} with ontology {OntologyId}",
                post.Title, post.UserId, post.OntologyId);

            post.CreatedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;
            post.IsActive = true;
            post.ViewCount = 0;
            post.ResponseCount = 0;

            // Create a user group for this collaboration project
            var groupName = $"Collaboration: {post.Title}";
            var groupDescription = $"Collaboration group for project '{post.Title}'";

            group = await _userGroupService.CreateGroupAsync(
                groupName,
                groupDescription,
                post.UserId,
                "#3b82f6" // Blue color for collaboration groups
            );

            _logger.LogInformation("Created collaboration group {GroupId} '{GroupName}'", group.Id, groupName);

            // Add the post creator as a group admin
            await _userGroupService.AddUserToGroupAsync(
                group.Id,
                post.UserId,
                post.UserId,
                isGroupAdmin: true
            );

            _logger.LogInformation("Added user {UserId} as admin to group {GroupId}", post.UserId, group.Id);

            // If there's an associated ontology, grant the group edit permission and update visibility
            if (post.OntologyId.HasValue)
            {
                _logger.LogInformation("Granting group {GroupId} edit permission to ontology {OntologyId}",
                    group.Id, post.OntologyId.Value);

                // Grant group permission
                await _userGroupService.GrantGroupPermissionAsync(
                    post.OntologyId.Value,
                    group.Id,
                    PermissionLevels.Edit,
                    post.UserId
                );

                // Update ontology visibility to Group so permissions apply
                using var context = await _contextFactory.CreateDbContextAsync();
                var ontology = await context.Ontologies.FindAsync(post.OntologyId.Value);
                if (ontology != null)
                {
                    ontology.Visibility = OntologyVisibility.Group;
                    ontology.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Updated ontology {OntologyId} visibility to Group and granted edit permission to group {GroupId}",
                        post.OntologyId.Value, group.Id);
                }
                else
                {
                    _logger.LogWarning("Ontology {OntologyId} not found when trying to update visibility",
                        post.OntologyId.Value);
                }
            }

            // Link the group to the post
            post.CollaborationProjectGroupId = group.Id;

            var createdPost = await _postRepository.AddAsync(post);

            _logger.LogInformation("Successfully created collaboration post {PostId} with group {GroupId}",
                createdPost.Id, group.Id);

            return createdPost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collaboration post '{Title}' for user {UserId}. Group {GroupId} may need cleanup.",
                post.Title, post.UserId, group?.Id);

            // Note: Group cleanup could be added here if needed, but leaving orphaned groups
            // for investigation is preferred over automatic deletion in case of partial failures
            throw;
        }
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
        var response = await context.CollaborationResponses
            .Include(r => r.CollaborationPost)
            .FirstOrDefaultAsync(r => r.Id == responseId);

        if (response == null)
            return false;

        var oldStatus = response.Status;
        response.Status = newStatus;

        // If response is being accepted, add the user to the collaboration group
        if (newStatus == "Accepted" && oldStatus != "Accepted" && response.CollaborationPost.CollaborationProjectGroupId.HasValue)
        {
            var groupId = response.CollaborationPost.CollaborationProjectGroupId.Value;

            try
            {
                // Add user to the collaboration group
                await _userGroupService.AddUserToGroupAsync(
                    groupId,
                    response.UserId,
                    response.CollaborationPost.UserId, // Added by the post creator
                    isGroupAdmin: false
                );

                _logger.LogInformation("Added user {UserId} to collaboration group {GroupId} for post {PostId}",
                    response.UserId, groupId, response.CollaborationPostId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add user {UserId} to group {GroupId}", response.UserId, groupId);
                // Don't fail the status update if adding to group fails
            }
        }
        // If response is being declined/removed after being accepted, remove from group
        else if (oldStatus == "Accepted" && newStatus != "Accepted" && response.CollaborationPost.CollaborationProjectGroupId.HasValue)
        {
            var groupId = response.CollaborationPost.CollaborationProjectGroupId.Value;

            try
            {
                await _userGroupService.RemoveUserFromGroupAsync(groupId, response.UserId);

                _logger.LogInformation("Removed user {UserId} from collaboration group {GroupId}",
                    response.UserId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove user {UserId} from group {GroupId}", response.UserId, groupId);
                // Don't fail the status update if removing from group fails
            }
        }

        await context.SaveChangesAsync();
        return true;
    }
}
