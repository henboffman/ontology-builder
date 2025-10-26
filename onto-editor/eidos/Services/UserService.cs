using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;
using Eidos.Services.Interfaces;

namespace Eidos.Services;

/// <summary>
/// Service for managing users and current user context
/// Integrated with ASP.NET Core Identity for authentication
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly IServiceProvider _serviceProvider;

    public UserService(
        UserManager<ApplicationUser> userManager,
        AuthenticationStateProvider authStateProvider,
        IDbContextFactory<OntologyDbContext> contextFactory,
        IServiceProvider serviceProvider)
    {
        _userManager = userManager;
        _authStateProvider = authStateProvider;
        _contextFactory = contextFactory;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Get the currently authenticated user
    /// Returns ApplicationUser (not legacy User)
    /// </summary>
    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            // Create a new scope to ensure UserManager gets a fresh DbContext
            // This prevents concurrency issues with Blazor Server
            using var scope = _serviceProvider.CreateScope();
            var scopedUserManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await scopedUserManager.GetUserAsync(user);
        }

        return null;
    }

    /// <summary>
    /// Check if the current user is authenticated
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User?.Identity?.IsAuthenticated == true;
    }

    /// <summary>
    /// Get user by ID (ApplicationUser ID which is a string)
    /// </summary>
    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    public async Task<ApplicationUser?> GetUserByUsernameAsync(string username)
    {
        return await _userManager.FindByNameAsync(username);
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    /// <summary>
    /// Get all users (for admin purposes - returns ApplicationUsers)
    /// </summary>
    public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
    {
        return await _userManager.Users.ToListAsync();
    }

    /// <summary>
    /// Legacy method - kept for backward compatibility with old User selector
    /// This will be removed once UI is updated
    /// </summary>
    [Obsolete("Use Identity registration instead")]
    public async Task<User> CreateUserAsync(User user)
    {
        // This is for the old User table - keep for backward compatibility during migration
        using var context = await _contextFactory.CreateDbContextAsync();
        if (await context.LegacyUsers.AnyAsync(u => u.Username == user.Username))
        {
            throw new InvalidOperationException($"Username '{user.Username}' already exists");
        }

        user.CreatedAt = DateTime.UtcNow;
        context.LegacyUsers.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Legacy method - kept for backward compatibility
    /// This will be removed once UI is updated to use Identity
    /// </summary>
    [Obsolete("This is a legacy method for backward compatibility")]
    public async Task SetCurrentUserAsync(int userId)
    {
        // This method no longer makes sense with Identity
        // User switching is handled by authentication
        // Kept as no-op for backward compatibility
        await Task.CompletedTask;
    }
}
