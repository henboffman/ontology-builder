namespace Eidos.Models
{
    /// <summary>
    /// Represents a user's presence in an ontology editing session
    /// </summary>
    public class PresenceInfo
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
        public string Color { get; set; } = string.Empty; // For avatar/cursor color
        public bool IsGuest { get; set; } = false;
        public string? CurrentView { get; set; } // Which view they're on (Graph, List, etc.)
    }
}
