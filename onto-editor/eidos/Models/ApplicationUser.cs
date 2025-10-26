using Microsoft.AspNetCore.Identity;

namespace Eidos.Models
{
    /// <summary>
    /// Application user extending ASP.NET Core Identity with custom properties
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Display name shown in the UI
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// When the user account was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last time the user logged in
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Ontologies owned by this user
        /// </summary>
        public ICollection<Ontology> Ontologies { get; set; } = new List<Ontology>();

        /// <summary>
        /// User preferences for customization (one-to-one relationship)
        /// </summary>
        public UserPreferences? Preferences { get; set; }

        // Note: Email, UserName, PasswordHash, SecurityStamp, etc. are inherited from IdentityUser
        // These are handled securely by ASP.NET Core Identity
    }
}
