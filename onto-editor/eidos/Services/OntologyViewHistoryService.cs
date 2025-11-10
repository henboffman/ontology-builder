using Eidos.Data;
using Eidos.Models;
using Eidos.Models.DTOs;
using Eidos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Eidos.Services;

/// <summary>
/// Service for tracking when users view ontologies and providing "What's New" functionality
/// </summary>
public class OntologyViewHistoryService : IOntologyViewHistoryService
{
    private readonly OntologyDbContext _context;
    private readonly ILogger<OntologyViewHistoryService> _logger;

    public OntologyViewHistoryService(
        OntologyDbContext context,
        ILogger<OntologyViewHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecordViewAsync(int ontologyId, string userId, string sessionId)
    {
        try
        {
            var viewHistory = await _context.OntologyViewHistories
                .FirstOrDefaultAsync(v => v.OntologyId == ontologyId && v.UserId == userId);

            if (viewHistory == null)
            {
                // First time user views this ontology
                viewHistory = new OntologyViewHistory
                {
                    OntologyId = ontologyId,
                    UserId = userId,
                    LastViewedAt = DateTime.UtcNow,
                    CurrentSessionId = sessionId,
                    ViewCount = 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.OntologyViewHistories.Add(viewHistory);
                _logger.LogInformation("[ViewHistory] First visit - created new record with session {SessionId}", sessionId);
            }
            else
            {
                // Update existing view history
                var previousSessionId = viewHistory.CurrentSessionId;
                viewHistory.LastViewedAt = DateTime.UtcNow;
                viewHistory.CurrentSessionId = sessionId;
                viewHistory.ViewCount++;
                _logger.LogInformation("[ViewHistory] Updated session from {PreviousSessionId} to {CurrentSessionId}",
                    previousSessionId, sessionId);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record view for ontology {OntologyId} by user {UserId}",
                ontologyId, userId);
            // Don't throw - tracking failures shouldn't break the user experience
        }
    }

    public async Task<WhatsNewDto?> GetWhatsNewAsync(int ontologyId, string userId, string sessionId)
    {
        try
        {
            _logger.LogInformation("[WhatsNew] GetWhatsNewAsync called for ontology {OntologyId}, user {UserId}, session {SessionId}",
                ontologyId, userId, sessionId);

            var viewHistory = await _context.OntologyViewHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.OntologyId == ontologyId && v.UserId == userId);

            if (viewHistory == null)
            {
                // Brand new user - don't show anything on first visit
                _logger.LogInformation("[WhatsNew] No history at all - first visit");
                return null;
            }

            // Determine the checkpoint based on session tracking
            DateTime checkpointTime;
            string? checkpointSession = null;

            // If user has dismissed changes, use that session as checkpoint
            if (!string.IsNullOrEmpty(viewHistory.LastDismissedSessionId))
            {
                checkpointSession = viewHistory.LastDismissedSessionId;
                checkpointTime = viewHistory.LastDismissedAt ?? viewHistory.LastViewedAt;
                _logger.LogInformation("[WhatsNew] Using LastDismissedSessionId: {SessionId}, time: {Time}",
                    checkpointSession, checkpointTime);
            }
            // Otherwise, if this is a new session (different from current), use their previous view
            else if (!string.IsNullOrEmpty(viewHistory.CurrentSessionId) && viewHistory.CurrentSessionId != sessionId)
            {
                // New session - show changes since last view
                checkpointTime = viewHistory.LastViewedAt;
                _logger.LogInformation("[WhatsNew] New session detected. Previous: {PreviousSession}, Current: {CurrentSession}. Using LastViewedAt: {Time}",
                    viewHistory.CurrentSessionId, sessionId, checkpointTime);
            }
            else
            {
                // Same session or no previous session - don't show changes
                _logger.LogInformation("[WhatsNew] Same session ({SessionId}) - no changes to show", sessionId);
                return null;
            }

            // Get all activities since checkpoint, excluding the current user's own changes
            var activitiesSinceLastView = await _context.OntologyActivities
                .AsNoTracking()
                .Where(a => a.OntologyId == ontologyId
                    && a.CreatedAt > checkpointTime
                    && a.UserId != userId) // Exclude user's own changes
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("[WhatsNew] Found {Count} activities since checkpoint", activitiesSinceLastView.Count);

            if (!activitiesSinceLastView.Any())
            {
                // No changes since last visit
                _logger.LogInformation("[WhatsNew] No changes from other users since last session");
                return null;
            }

            // Count changes by type
            var conceptsAdded = activitiesSinceLastView.Count(a =>
                a.EntityType == EntityTypes.Concept && a.ActivityType == ActivityTypes.Create);
            var conceptsModified = activitiesSinceLastView.Count(a =>
                a.EntityType == EntityTypes.Concept && a.ActivityType == ActivityTypes.Update);
            var conceptsDeleted = activitiesSinceLastView.Count(a =>
                a.EntityType == EntityTypes.Concept && a.ActivityType == ActivityTypes.Delete);

            var relationshipsAdded = activitiesSinceLastView.Count(a =>
                a.EntityType == EntityTypes.Relationship && a.ActivityType == ActivityTypes.Create);
            var relationshipsModified = activitiesSinceLastView.Count(a =>
                a.EntityType == EntityTypes.Relationship && a.ActivityType == ActivityTypes.Update);
            var relationshipsDeleted = activitiesSinceLastView.Count(a =>
                a.EntityType == EntityTypes.Relationship && a.ActivityType == ActivityTypes.Delete);

            // Get unique contributors
            var contributors = activitiesSinceLastView
                .Where(a => !string.IsNullOrEmpty(a.ActorName))
                .Select(a => a.ActorName)
                .Distinct()
                .ToList();

            // Convert to DTOs (take top 20 most recent)
            var recentActivities = activitiesSinceLastView
                .Take(20)
                .Select(a => new OntologyActivityDto
                {
                    Id = a.Id,
                    ActorName = a.ActorName,
                    ActivityType = a.ActivityType,
                    EntityType = a.EntityType,
                    EntityName = a.EntityName ?? string.Empty,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                })
                .ToList();

            var dto = new WhatsNewDto
            {
                LastViewedAt = viewHistory.LastViewedAt,
                TotalChanges = activitiesSinceLastView.Count,
                ConceptsAdded = conceptsAdded,
                ConceptsModified = conceptsModified,
                ConceptsDeleted = conceptsDeleted,
                RelationshipsAdded = relationshipsAdded,
                RelationshipsModified = relationshipsModified,
                RelationshipsDeleted = relationshipsDeleted,
                RecentActivities = recentActivities,
                ContributorNames = contributors,
                HasDismissed = viewHistory.LastDismissedAt.HasValue
                    && viewHistory.LastDismissedAt >= activitiesSinceLastView.Min(a => a.CreatedAt)
            };

            _logger.LogInformation("[WhatsNew] Returning DTO with {TotalChanges} changes, {ConceptsAdded} concepts added, {RelationshipsAdded} relationships added",
                dto.TotalChanges, dto.ConceptsAdded, dto.RelationshipsAdded);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get what's new for ontology {OntologyId} and user {UserId}",
                ontologyId, userId);
            return null;
        }
    }

    public async Task DismissWhatsNewAsync(int ontologyId, string userId, string sessionId)
    {
        try
        {
            var viewHistory = await _context.OntologyViewHistories
                .FirstOrDefaultAsync(v => v.OntologyId == ontologyId && v.UserId == userId);

            if (viewHistory != null)
            {
                viewHistory.LastDismissedAt = DateTime.UtcNow;
                viewHistory.LastDismissedSessionId = sessionId;
                _logger.LogInformation("[WhatsNew] Dismissed changes for session {SessionId}", sessionId);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss what's new for ontology {OntologyId} by user {UserId}",
                ontologyId, userId);
            // Don't throw - this is a non-critical operation
        }
    }

    public async Task<OntologyViewHistory?> GetViewHistoryAsync(int ontologyId, string userId)
    {
        try
        {
            return await _context.OntologyViewHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.OntologyId == ontologyId && v.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get view history for ontology {OntologyId} and user {UserId}",
                ontologyId, userId);
            return null;
        }
    }
}
