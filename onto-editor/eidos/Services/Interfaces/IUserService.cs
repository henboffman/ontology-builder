using Eidos.Models;

namespace Eidos.Services.Interfaces;

/// <summary>
/// Service for managing users and current user context
/// Integrated with ASP.NET Core Identity
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get the currently authenticated user (ApplicationUser from Identity)
    /// </summary>
    Task<ApplicationUser?> GetCurrentUserAsync();

    /// <summary>
    /// Check if the current user is authenticated
    /// </summary>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// Get a user by ID (Identity user ID is a string)
    /// </summary>
    Task<ApplicationUser?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Get a user by username
    /// </summary>
    Task<ApplicationUser?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Get a user by email
    /// </summary>
    Task<ApplicationUser?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Get all users (ApplicationUsers from Identity)
    /// </summary>
    Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();

    /// <summary>
    /// Legacy: Create a new user in the old Users table (for backward compatibility)
    /// </summary>
    [Obsolete("Use Identity registration instead")]
    Task<User> CreateUserAsync(User user);

    /// <summary>
    /// Legacy: Set the current user (for backward compatibility)
    /// </summary>
    [Obsolete("User switching is handled by Identity authentication")]
    Task SetCurrentUserAsync(int userId);
}
