using System;
using Eidos.Models.Enums;

namespace Eidos.Models.DTOs
{
    /// <summary>
    /// Unified model representing a shared ontology with all metadata
    /// Combines share link and group access information
    /// </summary>
    public class SharedOntologyInfo
    {
        public int OntologyId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string OwnerId { get; set; } = null!;
        public string OwnerName { get; set; } = null!;
        public string? OwnerPhotoUrl { get; set; }

        // Access metadata
        public SharedAccessType AccessType { get; set; }
        public PermissionLevel PermissionLevel { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? LastViewedAt { get; set; }
        public int ViewCount { get; set; }

        // Ontology metadata
        public int ConceptCount { get; set; }
        public int RelationshipCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // User state
        public bool IsPinned { get; set; }
        public bool IsHidden { get; set; }
        public DateTime? PinnedAt { get; set; }

        // Collaboration metadata (if from group)
        public string? GroupName { get; set; }
        public int? GroupMemberCount { get; set; }
    }

    /// <summary>
    /// Type of access user has to the shared ontology
    /// </summary>
    public enum SharedAccessType
    {
        ShareLink,  // Shared via direct share link
        Group,      // Shared via group membership
        Both        // User has both share link AND group access (show highest permission)
    }

    /// <summary>
    /// Filter options for shared ontologies query
    /// </summary>
    public class SharedOntologyFilter
    {
        public bool IncludeHidden { get; set; } = false;
        public bool PinnedOnly { get; set; } = false;
        public SharedAccessType? AccessTypeFilter { get; set; } = null;
        public int DaysBack { get; set; } = 90; // Default 90 days
        public string? SearchTerm { get; set; }
        public SharedOntologySortBy SortBy { get; set; } = SharedOntologySortBy.LastAccessed;

        public override int GetHashCode()
        {
            return HashCode.Combine(IncludeHidden, PinnedOnly, AccessTypeFilter, DaysBack, SearchTerm, SortBy);
        }
    }

    /// <summary>
    /// Sorting options for shared ontologies
    /// </summary>
    public enum SharedOntologySortBy
    {
        LastAccessed,   // Most recent first (default)
        LastViewed,     // Based on OntologyViewHistory
        Name,           // Alphabetical
        ConceptCount,   // Largest first
        PinnedFirst     // Pinned at top, then by LastAccessed
    }

    /// <summary>
    /// Paginated result for shared ontologies query
    /// </summary>
    public class SharedOntologyResult
    {
        public List<SharedOntologyInfo> Ontologies { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
