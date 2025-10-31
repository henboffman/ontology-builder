using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Eidos.Models;

namespace Eidos.Endpoints;

/// <summary>
/// Development-only endpoint for switching between test users.
/// This endpoint is only available when running in Development environment.
/// </summary>
public static class DevSwitchUserEndpoint
{
    public static void MapDevSwitchUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dev/api/switch-user/{email}", async (
            [FromRoute] string email,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] IWebHostEnvironment environment,
            [FromServices] ILogger<Program> logger,
            HttpContext httpContext) =>
        {
            // SECURITY: Only allow in development environment
            if (!environment.IsDevelopment())
            {
                logger.LogWarning("Attempted to access dev switch-user endpoint in {Environment} environment",
                    environment.EnvironmentName);
                return Results.StatusCode(403);
            }

            try
            {
                logger.LogInformation("Attempting to switch to user: {Email}", email);

                // Find the user
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    logger.LogWarning("User not found for switch: {Email}", email);
                    return Results.NotFound($"User {email} not found");
                }

                // Sign out current user
                await signInManager.SignOutAsync();

                // Sign in as the new user
                await signInManager.SignInAsync(user, isPersistent: false);

                // Set a cookie to indicate manual user switch (prevents auto-login override)
                httpContext.Response.Cookies.Append("manual-user-switch", "true", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromHours(24)
                });

                logger.LogInformation("Successfully switched to user: {Email} (UserId: {UserId})",
                    email, user.Id);

                // Redirect to home
                return Results.Redirect("/");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error switching to user: {Email}", email);
                return Results.Problem($"Error switching user: {ex.Message}");
            }
        });
    }
}
