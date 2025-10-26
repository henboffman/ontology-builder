namespace Eidos.Models.DTOs;

/// <summary>
/// DTO for displaying activity in the version history UI
/// </summary>
public class OntologyActivityDto
{
    public int Id { get; set; }
    public int? VersionNumber { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public bool IsGuestUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? BeforeSnapshot { get; set; }
    public string? AfterSnapshot { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for comparing two versions
/// </summary>
public class VersionComparisonDto
{
    public OntologyActivityDto Version1 { get; set; } = null!;
    public OntologyActivityDto Version2 { get; set; } = null!;
    public List<VersionDifference> Differences { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Represents a single difference between versions
/// </summary>
public class VersionDifference
{
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DifferenceType Type { get; set; }
}

public enum DifferenceType
{
    Added,
    Removed,
    Modified
}

/// <summary>
/// Statistics about version history
/// </summary>
public class VersionHistoryStatsDto
{
    public int TotalVersions { get; set; }
    public int CurrentVersion { get; set; }
    public DateTime FirstActivityDate { get; set; }
    public DateTime LastActivityDate { get; set; }
    public int TotalContributors { get; set; }
    public Dictionary<string, int> ActivityTypeBreakdown { get; set; } = new();
    public Dictionary<string, int> EntityTypeBreakdown { get; set; } = new();
}

/// <summary>
/// Filter options for version history queries
/// </summary>
public class VersionHistoryFilter
{
    public string? EntityType { get; set; }
    public string? UserId { get; set; }
    public string? ActivityType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}
