using Microsoft.AspNetCore.Identity;
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

    public DatabaseSeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<DatabaseSeeder> logger,
        IConfiguration configuration)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Seeds roles and admin user
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
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
}
