using Eidos.Models;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository interface for User entity
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Get a user by username
    /// </summary>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// Check if a username exists
    /// </summary>
    Task<bool> UsernameExistsAsync(string username);
}
