using Eidos.Models.DTOs;
using Eidos.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eidos.Endpoints
{
    /// <summary>
    /// API endpoints for user search functionality
    /// Used by UserPicker component for access management
    /// Security: All endpoints require authentication
    /// </summary>
    public static class UserSearchEndpoints
    {
        public static void MapUserSearchEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/users")
                .RequireAuthorization();

            // Search users
            group.MapGet("/search", SearchUsers)
                .WithName("SearchUsers")
                .WithDescription("Search for users by name or email (prioritizes past collaborators)");
        }

        /// <summary>
        /// Search users by query string
        /// Returns up to 10 results, prioritizing users the current user has worked with
        /// </summary>
        private static async Task<IResult> SearchUsers(
            [FromQuery] string query,
            ClaimsPrincipal user,
            [FromServices] IUserService userService)
        {
            // Security: Require authentication
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                return Results.Unauthorized();
            }

            // Validate query
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Results.Ok(new List<UserSearchResult>());
            }

            // Get current user ID for prioritization
            var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Search users
            var results = await userService.SearchUsersAsync(query, currentUserId, limit: 10);

            return Results.Ok(results);
        }
    }
}
