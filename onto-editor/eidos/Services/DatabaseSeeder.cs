using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;

namespace Eidos.Services;

/// <summary>
/// Seeds initial data into the database including roles and admin user
/// </summary>
public class DatabaseSeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly IConfiguration _configuration;
    private readonly OntologyDbContext _context;

    public DatabaseSeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<DatabaseSeeder> logger,
        IConfiguration configuration,
        OntologyDbContext context)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
        _context = context;
    }

    /// <summary>
    /// Seeds roles and admin user
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
        await SeedDevelopmentUsersAsync();
    }

    /// <summary>
    /// Creates all application roles if they don't exist
    /// </summary>
    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("Seeding roles...");

        foreach (var roleName in AppRoles.AllRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    /// <summary>
    /// Assigns admin role to configured admin users
    /// </summary>
    private async Task SeedAdminUserAsync()
    {
        _logger.LogInformation("Seeding admin users...");

        // Get admin email from configuration (appsettings.json or environment variable)
        var adminEmail = _configuration["AdminEmail"];

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            _logger.LogWarning("No AdminEmail configured. Skipping admin user seeding. " +
                "Set AdminEmail in appsettings.json or ADMINEMAIL environment variable.");
            return;
        }

        var adminEmails = adminEmail.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var email in adminEmails)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("Admin user with email {Email} not found. " +
                    "They will be assigned admin role when they first sign in.", email);
                continue;
            }

            // Check if user is already an admin
            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                _logger.LogInformation("User {Email} is already an admin", email);
                continue;
            }

            // Assign admin role
            var result = await _userManager.AddToRoleAsync(user, AppRoles.Admin);
            if (result.Succeeded)
            {
                _logger.LogInformation("Assigned Admin role to user: {Email}", email);
            }
            else
            {
                _logger.LogError("Failed to assign Admin role to {Email}: {Errors}",
                    email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Also assign User role if they don't have it (all admins are also users)
            if (!await _userManager.IsInRoleAsync(user, AppRoles.User))
            {
                await _userManager.AddToRoleAsync(user, AppRoles.User);
            }
        }
    }

    /// <summary>
    /// Handles first-time login role assignment for configured admins
    /// Called after external authentication
    /// </summary>
    public async Task AssignDefaultRolesOnLoginAsync(ApplicationUser user)
    {
        if (user == null) return;

        var adminEmail = _configuration["AdminEmail"];
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var adminEmails = adminEmail.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Check if this user should be an admin
            if (adminEmails.Contains(user.Email, StringComparer.OrdinalIgnoreCase))
            {
                if (!await _userManager.IsInRoleAsync(user, AppRoles.Admin))
                {
                    await _userManager.AddToRoleAsync(user, AppRoles.Admin);
                    _logger.LogInformation("Assigned Admin role to first-time admin user: {Email}", user.Email);
                }
            }
        }

        // Assign default User role to all authenticated users if they don't have any roles
        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Count == 0)
        {
            await _userManager.AddToRoleAsync(user, AppRoles.User);
            _logger.LogInformation("Assigned default User role to: {Email}", user.Email);
        }
    }

    /// <summary>
    /// Seeds development test users for local development
    /// </summary>
    private async Task SeedDevelopmentUsersAsync()
    {
        _logger.LogInformation("Seeding development users...");

        // Dev user (admin)
        var devEmail = "dev@localhost.local";
        var devUser = await _userManager.FindByEmailAsync(devEmail);
        if (devUser == null)
        {
            devUser = new ApplicationUser
            {
                Id = "cb7c6b4d-af5d-4ff5-88d0-ba9fc88239fa",
                UserName = devEmail,
                Email = devEmail,
                EmailConfirmed = true,
                DisplayName = "Dev User",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(devUser);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(devUser, AppRoles.Admin);
                await _userManager.AddToRoleAsync(devUser, AppRoles.User);

                // Add preferences
                if (!await _context.UserPreferences.AnyAsync(p => p.UserId == devUser.Id))
                {
                    _context.UserPreferences.Add(new UserPreferences
                    {
                        UserId = devUser.Id,
                        Theme = "dark",
                        GroupingRadius = 100,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created dev user: {Email}", devEmail);
            }
        }

        // Test user
        var testEmail = "test@test.com";
        var testUser = await _userManager.FindByEmailAsync(testEmail);
        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                Id = "test-user-id-123",
                UserName = testEmail,
                Email = testEmail,
                EmailConfirmed = true,
                DisplayName = "Test User",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(testUser);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(testUser, AppRoles.User);

                // Add preferences
                if (!await _context.UserPreferences.AnyAsync(p => p.UserId == testUser.Id))
                {
                    _context.UserPreferences.Add(new UserPreferences
                    {
                        UserId = testUser.Id,
                        Theme = "light",
                        GroupingRadius = 100,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created test user: {Email}", testEmail);
            }
        }

        // Collab user
        var collabEmail = "collab@test.com";
        var collabUser = await _userManager.FindByEmailAsync(collabEmail);
        if (collabUser == null)
        {
            collabUser = new ApplicationUser
            {
                Id = "collab-user-id-456",
                UserName = collabEmail,
                Email = collabEmail,
                EmailConfirmed = true,
                DisplayName = "Collaborator User",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(collabUser);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(collabUser, AppRoles.User);

                // Add preferences
                if (!await _context.UserPreferences.AnyAsync(p => p.UserId == collabUser.Id))
                {
                    _context.UserPreferences.Add(new UserPreferences
                    {
                        UserId = collabUser.Id,
                        Theme = "light",
                        GroupingRadius = 100,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created collab user: {Email}", collabEmail);
            }
        }

        _logger.LogInformation("Development users seeding complete");
    }
}
