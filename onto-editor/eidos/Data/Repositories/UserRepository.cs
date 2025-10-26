using Microsoft.EntityFrameworkCore;
using Eidos.Models;

namespace Eidos.Data.Repositories;

/// <summary>
/// Repository for User entity
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(IDbContextFactory<OntologyDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.LegacyUsers
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.LegacyUsers
            .AnyAsync(u => u.Username == username);
    }
}
