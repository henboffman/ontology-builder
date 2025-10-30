using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Eidos.Models;

namespace Eidos.Middleware;

/// <summary>
/// Development-only middleware that automatically logs in a test user
/// This should NEVER be enabled in production
/// </summary>
public class DevelopmentAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevelopmentAuthMiddleware> _logger;

    public DevelopmentAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<DevelopmentAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        // Only run in Development environment
        var enableAutoLogin = _configuration.GetValue<bool>("Development:EnableAutoLogin");

        if (enableAutoLogin && !context.User.Identity?.IsAuthenticated == true)
        {
            var email = _configuration["Development:AutoLoginEmail"] ?? "dev@localhost.local";
            var name = _configuration["Development:AutoLoginName"] ?? "Dev User";

            // Ensure the user exists in the database
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create development user: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    await _next(context);
                    return;
                }

                _logger.LogInformation("✓ Created development user: {Email}", email);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("name", name)
            };

            var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await context.SignInAsync(
                IdentityConstants.ApplicationScheme,
                claimsPrincipal);

            _logger.LogInformation("🔓 Development auto-login enabled for user: {Email}", email);
        }

        await _next(context);
    }
}
