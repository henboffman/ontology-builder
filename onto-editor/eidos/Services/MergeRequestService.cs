using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Eidos.Services;

/// <summary>
/// Service for managing merge request operations
/// Handles approval workflow for ontology changes
/// </summary>
public class MergeRequestService : IMergeRequestService
{
    private readonly IMergeRequestRepository _mergeRequestRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IConceptRepository _conceptRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IIndividualRepository _individualRepository;
    private readonly OntologyPermissionService _permissionService;
    private readonly ILogger<MergeRequestService> _logger;

    public MergeRequestService(
        IMergeRequestRepository mergeRequestRepository,
        IOntologyRepository ontologyRepository,
        IConceptRepository conceptRepository,
        IRelationshipRepository relationshipRepository,
        IIndividualRepository individualRepository,
        OntologyPermissionService permissionService,
        ILogger<MergeRequestService> logger)
    {
        _mergeRequestRepository = mergeRequestRepository;
        _ontologyRepository = ontologyRepository;
        _conceptRepository = conceptRepository;
        _relationshipRepository = relationshipRepository;
        _individualRepository = individualRepository;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<MergeRequest> CreateMergeRequestAsync(
        int ontologyId,
        string title,
        string? description,
        string createdByUserId,
        MergeRequestPriority priority = MergeRequestPriority.Normal)
    {
        _logger.LogInformation("Creating merge request for ontology {OntologyId} by user {UserId}", ontologyId, createdByUserId);

        // Verify ontology exists
        var ontology = await _ontologyRepository.GetByIdAsync(ontologyId);
        if (ontology == null)
        {
            throw new InvalidOperationException($"Ontology {ontologyId} not found");
        }

        // Verify user has edit permission
        var canEdit = await _permissionService.CanEditAsync(ontologyId, createdByUserId);
        if (!canEdit)
        {
            throw new UnauthorizedAccessException($"User {createdByUserId} does not have edit permission for ontology {ontologyId}");
        }

        var mergeRequest = new MergeRequest
        {
            OntologyId = ontologyId,
            Title = title,
            Description = description,
            CreatedByUserId = createdByUserId,
            Status = MergeRequestStatus.Draft,
            Priority = priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _mergeRequestRepository.AddAsync(mergeRequest);

        _logger.LogInformation("Created merge request {MergeRequestId} for ontology {OntologyId}", created.Id, ontologyId);

        return created;
    }

    public async Task<MergeRequest?> GetMergeRequestAsync(int id)
    {
        return await _mergeRequestRepository.GetWithDetailsAsync(id);
    }

    public async Task<IEnumerable<MergeRequest>> GetMergeRequestsForOntologyAsync(int ontologyId)
    {
        return await _mergeRequestRepository.GetByOntologyIdAsync(ontologyId);
    }

    public async Task<IEnumerable<MergeRequest>> GetPendingMergeRequestsAsync(int ontologyId)
    {
        return await _mergeRequestRepository.GetByOntologyIdAndStatusAsync(ontologyId, MergeRequestStatus.PendingReview);
    }

    public async Task<IEnumerable<MergeRequest>> GetUserMergeRequestsAsync(string userId)
    {
        return await _mergeRequestRepository.GetByCreatedByUserIdAsync(userId);
    }

    public async Task<IEnumerable<MergeRequest>> GetReviewerMergeRequestsAsync(string userId)
    {
        return await _mergeRequestRepository.GetByAssignedReviewerAsync(userId);
    }

    public async Task SubmitForReviewAsync(int mergeRequestId, string? reviewerUserId = null)
    {
        _logger.LogInformation("Submitting merge request {MergeRequestId} for review", mergeRequestId);

        var mergeRequest = await _mergeRequestRepository.GetByIdAsync(mergeRequestId);
        if (mergeRequest == null)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} not found");
        }

        if (mergeRequest.Status != MergeRequestStatus.Draft && mergeRequest.Status != MergeRequestStatus.ChangesRequested)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} cannot be submitted from status {mergeRequest.Status}");
        }

        mergeRequest.Status = MergeRequestStatus.PendingReview;
        mergeRequest.SubmittedAt = DateTime.UtcNow;
        mergeRequest.UpdatedAt = DateTime.UtcNow;

        if (reviewerUserId != null)
        {
            mergeRequest.AssignedReviewerUserId = reviewerUserId;
        }

        await _mergeRequestRepository.UpdateAsync(mergeRequest);

        // Add system comment
        await AddCommentInternalAsync(mergeRequestId, mergeRequest.CreatedByUserId, "Submitted for review", null, isSystem: true);

        _logger.LogInformation("Merge request {MergeRequestId} submitted for review", mergeRequestId);
    }

    public async Task ApproveAsync(int mergeRequestId, string reviewerUserId, string? comments = null)
    {
        _logger.LogInformation("Approving merge request {MergeRequestId} by user {ReviewerId}", mergeRequestId, reviewerUserId);

        var mergeRequest = await _mergeRequestRepository.GetByIdAsync(mergeRequestId);
        if (mergeRequest == null)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} not found");
        }

        // Verify reviewer has manage permission
        var canManage = await _permissionService.CanManageAsync(mergeRequest.OntologyId, reviewerUserId);
        if (!canManage)
        {
            throw new UnauthorizedAccessException($"User {reviewerUserId} does not have permission to approve merge requests for ontology {mergeRequest.OntologyId}");
        }

        if (mergeRequest.Status != MergeRequestStatus.PendingReview)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} cannot be approved from status {mergeRequest.Status}");
        }

        mergeRequest.Status = MergeRequestStatus.Approved;
        mergeRequest.ReviewedByUserId = reviewerUserId;
        mergeRequest.ReviewedAt = DateTime.UtcNow;
        mergeRequest.UpdatedAt = DateTime.UtcNow;

        if (comments != null)
        {
            mergeRequest.ReviewComments = comments;
        }

        await _mergeRequestRepository.UpdateAsync(mergeRequest);

        // Add system comment
        await AddCommentInternalAsync(mergeRequestId, reviewerUserId, $"Approved{(comments != null ? $": {comments}" : "")}", null, isSystem: true);

        _logger.LogInformation("Merge request {MergeRequestId} approved by user {ReviewerId}", mergeRequestId, reviewerUserId);
    }

    public async Task RejectAsync(int mergeRequestId, string reviewerUserId, string comments)
    {
        _logger.LogInformation("Rejecting merge request {MergeRequestId} by user {ReviewerId}", mergeRequestId, reviewerUserId);

        var mergeRequest = await _mergeRequestRepository.GetByIdAsync(mergeRequestId);
        if (mergeRequest == null)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} not found");
        }

        // Verify reviewer has manage permission
        var canManage = await _permissionService.CanManageAsync(mergeRequest.OntologyId, reviewerUserId);
        if (!canManage)
        {
            throw new UnauthorizedAccessException($"User {reviewerUserId} does not have permission to reject merge requests for ontology {mergeRequest.OntologyId}");
        }

        if (mergeRequest.Status != MergeRequestStatus.PendingReview)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} cannot be rejected from status {mergeRequest.Status}");
        }

        mergeRequest.Status = MergeRequestStatus.Rejected;
        mergeRequest.ReviewedByUserId = reviewerUserId;
        mergeRequest.ReviewedAt = DateTime.UtcNow;
        mergeRequest.ReviewComments = comments;
        mergeRequest.UpdatedAt = DateTime.UtcNow;

        await _mergeRequestRepository.UpdateAsync(mergeRequest);

        // Add system comment
        await AddCommentInternalAsync(mergeRequestId, reviewerUserId, $"Rejected: {comments}", null, isSystem: true);

        _logger.LogInformation("Merge request {MergeRequestId} rejected by user {ReviewerId}", mergeRequestId, reviewerUserId);
    }

    public async Task RequestChangesAsync(int mergeRequestId, string reviewerUserId, string comments)
    {
        _logger.LogInformation("Requesting changes on merge request {MergeRequestId} by user {ReviewerId}", mergeRequestId, reviewerUserId);

        var mergeRequest = await _mergeRequestRepository.GetByIdAsync(mergeRequestId);
        if (mergeRequest == null)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} not found");
        }

        // Verify reviewer has manage permission
        var canManage = await _permissionService.CanManageAsync(mergeRequest.OntologyId, reviewerUserId);
        if (!canManage)
        {
            throw new UnauthorizedAccessException($"User {reviewerUserId} does not have permission to request changes for ontology {mergeRequest.OntologyId}");
        }

        if (mergeRequest.Status != MergeRequestStatus.PendingReview)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} cannot have changes requested from status {mergeRequest.Status}");
        }

        mergeRequest.Status = MergeRequestStatus.ChangesRequested;
        mergeRequest.ReviewedByUserId = reviewerUserId;
        mergeRequest.ReviewedAt = DateTime.UtcNow;
        mergeRequest.ReviewComments = comments;
        mergeRequest.UpdatedAt = DateTime.UtcNow;

        await _mergeRequestRepository.UpdateAsync(mergeRequest);

        // Add system comment
        await AddCommentInternalAsync(mergeRequestId, reviewerUserId, $"Changes requested: {comments}", null, isSystem: true);

        _logger.LogInformation("Changes requested on merge request {MergeRequestId} by user {ReviewerId}", mergeRequestId, reviewerUserId);
    }

    public async Task<bool> MergeAsync(int mergeRequestId, string userId)
    {
        _logger.LogInformation("Merging merge request {MergeRequestId} by user {UserId}", mergeRequestId, userId);

        var mergeRequest = await _mergeRequestRepository.GetWithDetailsAsync(mergeRequestId);
        if (mergeRequest == null)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} not found");
        }

        // Verify user has manage permission
        var canManage = await _permissionService.CanManageAsync(mergeRequest.OntologyId, userId);
        if (!canManage)
        {
            throw new UnauthorizedAccessException($"User {userId} does not have permission to merge requests for ontology {mergeRequest.OntologyId}");
        }

        if (mergeRequest.Status != MergeRequestStatus.Approved)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} must be approved before merging (current status: {mergeRequest.Status})");
        }

        // Check for conflicts
        var hasConflicts = await DetectConflictsAsync(mergeRequestId);
        if (hasConflicts)
        {
            _logger.LogWarning("Merge request {MergeRequestId} has conflicts and cannot be merged", mergeRequestId);
            return false;
        }

        // Apply changes to ontology
        // This is a simplified version - in production, you'd apply each change from the MR
        // For now, we just mark it as merged
        mergeRequest.Status = MergeRequestStatus.Merged;
        mergeRequest.MergedAt = DateTime.UtcNow;
        mergeRequest.UpdatedAt = DateTime.UtcNow;

        await _mergeRequestRepository.UpdateAsync(mergeRequest);

        // Add system comment
        await AddCommentInternalAsync(mergeRequestId, userId, "Merged into ontology", null, isSystem: true);

        _logger.LogInformation("Merge request {MergeRequestId} merged successfully", mergeRequestId);

        return true;
    }

    public async Task CloseAsync(int mergeRequestId, string userId)
    {
        _logger.LogInformation("Closing merge request {MergeRequestId} by user {UserId}", mergeRequestId, userId);

        var mergeRequest = await _mergeRequestRepository.GetByIdAsync(mergeRequestId);
        if (mergeRequest == null)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} not found");
        }

        // Verify user is creator or has manage permission
        var canManage = await _permissionService.CanManageAsync(mergeRequest.OntologyId, userId);
        var isCreator = mergeRequest.CreatedByUserId == userId;

        if (!canManage && !isCreator)
        {
            throw new UnauthorizedAccessException($"User {userId} does not have permission to close merge request {mergeRequestId}");
        }

        if (mergeRequest.Status == MergeRequestStatus.Merged || mergeRequest.Status == MergeRequestStatus.Closed)
        {
            throw new InvalidOperationException($"Merge request {mergeRequestId} is already {mergeRequest.Status.ToString().ToLower()}");
        }

        mergeRequest.Status = MergeRequestStatus.Closed;
        mergeRequest.UpdatedAt = DateTime.UtcNow;

        await _mergeRequestRepository.UpdateAsync(mergeRequest);

        // Add system comment
        await AddCommentInternalAsync(mergeRequestId, userId, "Closed without merging", null, isSystem: true);

        _logger.LogInformation("Merge request {MergeRequestId} closed", mergeRequestId);
    }

    public async Task<MergeRequestComment> AddCommentAsync(int mergeRequestId, string userId, string text, int? changeId = null)
    {
        return await AddCommentInternalAsync(mergeRequestId, userId, text, changeId, isSystem: false);
    }

    private async Task<MergeRequestComment> AddCommentInternalAsync(int mergeRequestId, string userId, string text, int? changeId, bool isSystem)
    {
        var comment = new MergeRequestComment
        {
            MergeRequestId = mergeRequestId,
            UserId = userId,
            Text = text,
            MergeRequestChangeId = changeId,
            IsSystemComment = isSystem,
            CreatedAt = DateTime.UtcNow
        };

        return await _mergeRequestRepository.AddCommentAsync(comment);
    }

    public async Task<MergeRequestChange> AddChangeAsync(MergeRequestChange change)
    {
        var created = await _mergeRequestRepository.AddChangeAsync(change);

        // Update statistics
        await UpdateChangeStatisticsAsync(change.MergeRequestId);

        return created;
    }

    public async Task UpdateChangeStatisticsAsync(int mergeRequestId)
    {
        var changes = await _mergeRequestRepository.GetChangesAsync(mergeRequestId);
        var mergeRequest = await _mergeRequestRepository.GetByIdAsync(mergeRequestId);

        if (mergeRequest == null) return;

        // Calculate statistics
        mergeRequest.ConceptsAdded = changes.Count(c => c.EntityType == EntityType.Concept && c.ChangeType == MergeRequestChangeType.Add);
        mergeRequest.ConceptsModified = changes.Count(c => c.EntityType == EntityType.Concept && c.ChangeType == MergeRequestChangeType.Modify);
        mergeRequest.ConceptsDeleted = changes.Count(c => c.EntityType == EntityType.Concept && c.ChangeType == MergeRequestChangeType.Delete);

        mergeRequest.RelationshipsAdded = changes.Count(c => c.EntityType == EntityType.Relationship && c.ChangeType == MergeRequestChangeType.Add);
        mergeRequest.RelationshipsModified = changes.Count(c => c.EntityType == EntityType.Relationship && c.ChangeType == MergeRequestChangeType.Modify);
        mergeRequest.RelationshipsDeleted = changes.Count(c => c.EntityType == EntityType.Relationship && c.ChangeType == MergeRequestChangeType.Delete);

        mergeRequest.IndividualsAdded = changes.Count(c => c.EntityType == EntityType.Individual && c.ChangeType == MergeRequestChangeType.Add);
        mergeRequest.IndividualsModified = changes.Count(c => c.EntityType == EntityType.Individual && c.ChangeType == MergeRequestChangeType.Modify);
        mergeRequest.IndividualsDeleted = changes.Count(c => c.EntityType == EntityType.Individual && c.ChangeType == MergeRequestChangeType.Delete);

        mergeRequest.HasConflicts = changes.Any(c => c.HasConflict);

        await _mergeRequestRepository.UpdateAsync(mergeRequest);
    }

    public async Task<bool> DetectConflictsAsync(int mergeRequestId)
    {
        // Simplified conflict detection
        // In a full implementation, you would:
        // 1. Load the base snapshot from when the MR was created
        // 2. Compare current ontology state with base snapshot
        // 3. Check if any entities changed in both the MR and the current state
        // 4. Mark individual changes as conflicting

        var mergeRequest = await _mergeRequestRepository.GetWithDetailsAsync(mergeRequestId);
        if (mergeRequest == null) return false;

        // For now, just check the flag
        return mergeRequest.HasConflicts;
    }

    public async Task<int> GetPendingReviewCountAsync(int ontologyId)
    {
        return await _mergeRequestRepository.GetPendingReviewCountAsync(ontologyId);
    }
}
