using Microsoft.EntityFrameworkCore;

namespace Eidos.Data.Repositories;

/// <summary>
/// Base repository implementation providing common CRUD operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IDbContextFactory<OntologyDbContext> _contextFactory;

    protected BaseRepository(IDbContextFactory<OntologyDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        // Use AsNoTracking for read-only queries to reduce memory usage by 10-20%
        // FindAsync doesn't support AsNoTracking, so we use FirstOrDefaultAsync instead
        return await context.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<T>().AsNoTracking().ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Set<T>().FindAsync(id);
        if (entity != null)
        {
            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        // Use AnyAsync instead of FindAsync - more efficient as it doesn't materialize the entity
        return await context.Set<T>()
            .AnyAsync(e => EF.Property<int>(e, "Id") == id);
    }

    public virtual async Task<int> SaveChangesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SaveChangesAsync();
    }
}
