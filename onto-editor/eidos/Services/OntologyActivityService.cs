using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Eidos.Data;
using Eidos.Models;
using Eidos.Models.DTOs;
using Eidos.Services.Interfaces;
using System.Text.Json;

namespace Eidos.Services;

public class OntologyActivityService : IOntologyActivityService
{
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly ILogger<OntologyActivityService> _logger;

    public OntologyActivityService(
        IDbContextFactory<OntologyDbContext> contextFactory,
        ILogger<OntologyActivityService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<OntologyActivityDto>> GetActivityHistoryAsync(int ontologyId, int skip = 0, int take = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var activities = await context.OntologyActivities
            .Where(a => a.OntologyId == ontologyId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(a => new OntologyActivityDto
            {
                Id = a.Id,
                VersionNumber = a.VersionNumber,
                ActivityType = a.ActivityType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EntityName = a.EntityName,
                Description = a.Description,
                ActorName = a.ActorName,
                UserId = a.UserId,
                IsGuestUser = a.UserId == null,
                CreatedAt = a.CreatedAt,
                BeforeSnapshot = a.BeforeSnapshot,
                AfterSnapshot = a.AfterSnapshot,
                Notes = a.Notes
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} activities for ontology {OntologyId}", activities.Count, ontologyId);
        return activities;
    }

    public async Task<List<OntologyActivityDto>> GetActivityByEntityTypeAsync(int ontologyId, string entityType, int skip = 0, int take = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var activities = await context.OntologyActivities
            .Where(a => a.OntologyId == ontologyId && a.EntityType == entityType)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(a => new OntologyActivityDto
            {
                Id = a.Id,
                VersionNumber = a.VersionNumber,
                ActivityType = a.ActivityType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EntityName = a.EntityName,
                Description = a.Description,
                ActorName = a.ActorName,
                UserId = a.UserId,
                IsGuestUser = a.UserId == null,
                CreatedAt = a.CreatedAt,
                BeforeSnapshot = a.BeforeSnapshot,
                AfterSnapshot = a.AfterSnapshot,
                Notes = a.Notes
            })
            .ToListAsync();

        return activities;
    }

    public async Task<List<OntologyActivityDto>> GetActivityByUserAsync(int ontologyId, string userId, int skip = 0, int take = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var activities = await context.OntologyActivities
            .Where(a => a.OntologyId == ontologyId && a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(a => new OntologyActivityDto
            {
                Id = a.Id,
                VersionNumber = a.VersionNumber,
                ActivityType = a.ActivityType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EntityName = a.EntityName,
                Description = a.Description,
                ActorName = a.ActorName,
                UserId = a.UserId,
                IsGuestUser = a.UserId == null,
                CreatedAt = a.CreatedAt,
                BeforeSnapshot = a.BeforeSnapshot,
                AfterSnapshot = a.AfterSnapshot,
                Notes = a.Notes
            })
            .ToListAsync();

        return activities;
    }

    public async Task<OntologyActivity?> GetVersionAsync(int ontologyId, int versionNumber)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.OntologyActivities
            .FirstOrDefaultAsync(a => a.OntologyId == ontologyId && a.VersionNumber == versionNumber);
    }

    public async Task<List<OntologyActivityDto>> GetEntityHistoryAsync(int ontologyId, string entityType, int entityId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var activities = await context.OntologyActivities
            .Where(a => a.OntologyId == ontologyId &&
                       a.EntityType == entityType &&
                       a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new OntologyActivityDto
            {
                Id = a.Id,
                VersionNumber = a.VersionNumber,
                ActivityType = a.ActivityType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                EntityName = a.EntityName,
                Description = a.Description,
                ActorName = a.ActorName,
                UserId = a.UserId,
                IsGuestUser = a.UserId == null,
                CreatedAt = a.CreatedAt,
                BeforeSnapshot = a.BeforeSnapshot,
                AfterSnapshot = a.AfterSnapshot,
                Notes = a.Notes
            })
            .ToListAsync();

        return activities;
    }

    public async Task<VersionComparisonDto> CompareVersionsAsync(int activityId1, int activityId2)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var activity1 = await context.OntologyActivities.FindAsync(activityId1);
        var activity2 = await context.OntologyActivities.FindAsync(activityId2);

        if (activity1 == null || activity2 == null)
        {
            throw new ArgumentException("One or both activities not found");
        }

        var comparison = new VersionComparisonDto
        {
            Version1 = MapToDto(activity1),
            Version2 = MapToDto(activity2),
            Differences = new List<VersionDifference>()
        };

        // Compare snapshots if available
        if (!string.IsNullOrEmpty(activity1.AfterSnapshot) && !string.IsNullOrEmpty(activity2.AfterSnapshot))
        {
            comparison.Differences = CompareSnapshots(activity1.AfterSnapshot, activity2.AfterSnapshot);
        }

        comparison.Summary = $"Comparing version {activity1.VersionNumber} to version {activity2.VersionNumber}. {comparison.Differences.Count} difference(s) found.";

        return comparison;
    }

    public async Task RevertToVersionAsync(int ontologyId, int versionNumber, string userId, string actorName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var targetVersion = await context.OntologyActivities
            .FirstOrDefaultAsync(a => a.OntologyId == ontologyId && a.VersionNumber == versionNumber);

        if (targetVersion == null)
        {
            throw new ArgumentException($"Version {versionNumber} not found for ontology {ontologyId}");
        }

        _logger.LogWarning("Reverting ontology {OntologyId} to version {VersionNumber} by user {UserId}",
            ontologyId, versionNumber, userId);

        // Get current version number
        var currentVersionNumber = await GetCurrentVersionNumberAsync(ontologyId);

        // Create a new activity record for the revert operation
        var revertActivity = new OntologyActivity
        {
            OntologyId = ontologyId,
            UserId = userId,
            ActorName = actorName,
            ActivityType = "revert",
            EntityType = targetVersion.EntityType,
            EntityId = targetVersion.EntityId,
            EntityName = targetVersion.EntityName,
            Description = $"Reverted to version {versionNumber}",
            BeforeSnapshot = null, // Current state would need to be captured by calling service
            AfterSnapshot = targetVersion.AfterSnapshot,
            CreatedAt = DateTime.UtcNow,
            VersionNumber = currentVersionNumber + 1,
            Notes = $"Reverted from version {currentVersionNumber} to version {versionNumber}"
        };

        context.OntologyActivities.Add(revertActivity);
        await context.SaveChangesAsync();

        _logger.LogInformation("Created revert activity for ontology {OntologyId}, new version {VersionNumber}",
            ontologyId, revertActivity.VersionNumber);

        // Note: The actual reversion of ontology data (concepts, relationships, etc.)
        // should be handled by the calling service that has access to those repositories
    }

    public async Task<VersionHistoryStatsDto> GetVersionStatsAsync(int ontologyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var activities = await context.OntologyActivities
            .Where(a => a.OntologyId == ontologyId)
            .ToListAsync();

        if (!activities.Any())
        {
            return new VersionHistoryStatsDto
            {
                TotalVersions = 0,
                CurrentVersion = 0,
                FirstActivityDate = DateTime.MinValue,
                LastActivityDate = DateTime.MinValue,
                TotalContributors = 0
            };
        }

        var stats = new VersionHistoryStatsDto
        {
            TotalVersions = activities.Count,
            CurrentVersion = activities.Max(a => a.VersionNumber ?? 0),
            FirstActivityDate = activities.Min(a => a.CreatedAt),
            LastActivityDate = activities.Max(a => a.CreatedAt),
            TotalContributors = activities.Where(a => a.UserId != null).Select(a => a.UserId).Distinct().Count(),
            ActivityTypeBreakdown = activities
                .GroupBy(a => a.ActivityType)
                .ToDictionary(g => g.Key, g => g.Count()),
            EntityTypeBreakdown = activities
                .GroupBy(a => a.EntityType)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }

    public async Task RecordActivityAsync(OntologyActivity activity)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Auto-assign version number if not set
        if (activity.VersionNumber == null)
        {
            activity.VersionNumber = await GetCurrentVersionNumberAsync(activity.OntologyId) + 1;
        }

        context.OntologyActivities.Add(activity);
        await context.SaveChangesAsync();

        _logger.LogInformation("Recorded activity {ActivityType} for ontology {OntologyId}, version {VersionNumber}",
            activity.ActivityType, activity.OntologyId, activity.VersionNumber);
    }

    public async Task<int> GetCurrentVersionNumberAsync(int ontologyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var maxVersion = await context.OntologyActivities
            .Where(a => a.OntologyId == ontologyId)
            .MaxAsync(a => (int?)a.VersionNumber);

        return maxVersion ?? 0;
    }

    // Helper methods

    private OntologyActivityDto MapToDto(OntologyActivity activity)
    {
        return new OntologyActivityDto
        {
            Id = activity.Id,
            VersionNumber = activity.VersionNumber,
            ActivityType = activity.ActivityType,
            EntityType = activity.EntityType,
            EntityId = activity.EntityId,
            EntityName = activity.EntityName,
            Description = activity.Description,
            ActorName = activity.ActorName,
            UserId = activity.UserId,
            IsGuestUser = activity.UserId == null,
            CreatedAt = activity.CreatedAt,
            BeforeSnapshot = activity.BeforeSnapshot,
            AfterSnapshot = activity.AfterSnapshot,
            Notes = activity.Notes
        };
    }

    private List<VersionDifference> CompareSnapshots(string snapshot1Json, string snapshot2Json)
    {
        var differences = new List<VersionDifference>();

        try
        {
            var snapshot1 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(snapshot1Json);
            var snapshot2 = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(snapshot2Json);

            if (snapshot1 == null || snapshot2 == null)
            {
                return differences;
            }

            // Find fields in snapshot1 that are different or missing in snapshot2
            foreach (var field in snapshot1)
            {
                if (!snapshot2.ContainsKey(field.Key))
                {
                    differences.Add(new VersionDifference
                    {
                        FieldName = field.Key,
                        OldValue = field.Value.ToString(),
                        NewValue = null,
                        Type = DifferenceType.Removed
                    });
                }
                else if (!JsonElement.DeepEquals(field.Value, snapshot2[field.Key]))
                {
                    differences.Add(new VersionDifference
                    {
                        FieldName = field.Key,
                        OldValue = field.Value.ToString(),
                        NewValue = snapshot2[field.Key].ToString(),
                        Type = DifferenceType.Modified
                    });
                }
            }

            // Find fields in snapshot2 that are not in snapshot1 (additions)
            foreach (var field in snapshot2)
            {
                if (!snapshot1.ContainsKey(field.Key))
                {
                    differences.Add(new VersionDifference
                    {
                        FieldName = field.Key,
                        OldValue = null,
                        NewValue = field.Value.ToString(),
                        Type = DifferenceType.Added
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing snapshots");
        }

        return differences;
    }
}
