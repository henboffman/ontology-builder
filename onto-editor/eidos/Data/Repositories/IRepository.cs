namespace Eidos.Data.Repositories;

/// <summary>
/// Generic repository interface for data access operations
/// Follows the Repository Pattern for separation of concerns
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Get an entity by its ID
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Get all entities
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Add a new entity
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Update an existing entity
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Delete an entity by ID
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Check if an entity exists
    /// </summary>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Save all pending changes
    /// </summary>
    Task<int> SaveChangesAsync();
}
