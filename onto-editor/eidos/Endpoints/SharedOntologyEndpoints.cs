using Eidos.Models.DTOs;
using Eidos.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eidos.Endpoints
{
    /// <summary>
    /// Minimal API endpoints for shared ontology management
    /// Security: All endpoints require authentication
    /// </summary>
    public static class SharedOntologyEndpoints
    {
        public static void MapSharedOntologyEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/shared-ontologies")
                .RequireAuthorization();

            // Get shared ontologies for current user
            group.MapPost("/", GetSharedOntologies)
                .WithName("GetSharedOntologies")
                .WithDescription("Get all ontologies shared with the current user with filtering and pagination");

            // Pin an ontology
            group.MapPost("/{ontologyId}/pin", PinOntology)
                .WithName("PinSharedOntology")
                .WithDescription("Pin a shared ontology for quick access");

            // Unpin an ontology
            group.MapDelete("/{ontologyId}/pin", UnpinOntology)
                .WithName("UnpinSharedOntology")
                .WithDescription("Unpin a shared ontology");

            // Hide an ontology
            group.MapPost("/{ontologyId}/hide", HideOntology)
                .WithName("HideSharedOntology")
                .WithDescription("Hide a shared ontology from the list");

            // Unhide an ontology
            group.MapDelete("/{ontologyId}/hide", UnhideOntology)
                .WithName("UnhideSharedOntology")
                .WithDescription("Unhide a shared ontology");

            // Dismiss an ontology
            group.MapPost("/{ontologyId}/dismiss", DismissOntology)
                .WithName("DismissSharedOntology")
                .WithDescription("Dismiss a shared ontology (soft delete from shared list)");

            // Update access time for share link
            group.MapPost("/{ontologyId}/access/share-link", UpdateShareLinkAccess)
                .WithName("UpdateShareLinkAccess")
                .WithDescription("Update last accessed time for share link access");

            // Update access time for group
            group.MapPost("/{ontologyId}/access/group", UpdateGroupAccess)
                .WithName("UpdateGroupAccess")
                .WithDescription("Update last accessed time for group access");

            // Reset all user state (unhide all, unpin all)
            group.MapPost("/reset-all", ResetAllUserState)
                .WithName("ResetAllSharedOntologyState")
                .WithDescription("Reset all user state (unhide all hidden ontologies)");
        }

        /// <summary>
        /// Get shared ontologies for current user with filtering and pagination
        /// POST /api/shared-ontologies
        /// </summary>
        private static async Task<IResult> GetSharedOntologies(
            [FromBody] SharedOntologyFilterRequest request,
            ClaimsPrincipal user,
            SharedOntologyService sharedOntologyService,
            ILogger<SharedOntologyService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var filter = new SharedOntologyFilter
                {
                    IncludeHidden = request.IncludeHidden,
                    PinnedOnly = request.PinnedOnly,
                    AccessTypeFilter = request.AccessTypeFilter,
                    DaysBack = request.DaysBack,
                    SearchTerm = request.SearchTerm,
                    SortBy = request.SortBy
                };

                var result = await sharedOntologyService.GetSharedOntologiesAsync(
                    userId,
                    filter,
                    request.Page,
                    request.PageSize);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving shared ontologies for user");
                return Results.Problem("An error occurred while retrieving shared ontologies.");
            }
        }

        /// <summary>
        /// Pin a shared ontology
        /// POST /api/shared-ontologies/{ontologyId}/pin
        /// </summary>
        private static async Task<IResult> PinOntology(
            int ontologyId,
            ClaimsPrincipal user,
            SharedOntologyService sharedOntologyService,
            ILogger<SharedOntologyService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                await sharedOntologyService.PinOntologyAsync(userId, ontologyId);

                return Results.Ok(new { message = "Ontology pinned successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error pinning ontology {OntologyId}", ontologyId);
                return Results.Problem("An error occurred while pinning the ontology.");
            }
        }

        /// <summary>
        /// Unpin a shared ontology
        /// DELETE /api/shared-ontologies/{ontologyId}/pin
        /// </summary>
        private static async Task<IResult> UnpinOntology(
            int ontologyId,
            ClaimsPrincipal user,
            SharedOntologyService sharedOntologyService,
            ILogger<SharedOntologyService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                await sharedOntologyService.UnpinOntologyAsync(userId, ontologyId);

                return Results.Ok(new { message = "Ontology unpinned successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error unpinning ontology {OntologyId}", ontologyId);
                return Results.Problem("An error occurred while unpinning the ontology.");
            }
        }

        /// <summary>
        /// Hide a shared ontology
        /// POST /api/shared-ontologies/{ontologyId}/hide
        /// </summary>
        private static async Task<IResult> HideOntology(
            int ontologyId,
            ClaimsPrincipal user,
            SharedOntologyService sharedOntologyService,
            ILogger<SharedOntologyService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                await sharedOntologyService.HideOntologyAsync(userId, ontologyId);

                return Results.Ok(new { message = "Ontology hidden successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error hiding ontology {OntologyId}", ontologyId);
                return Results.Problem("An error occurred while hiding the ontology.");
            }
        }

        /// <summary>
        /// Unhide a shared ontology
        /// DELETE /api/shared-ontologies/{ontologyId}/hide
        /// </summary>
        private static async Task<IResult> UnhideOntology(
            int ontologyId,
            ClaimsPrincipal user,
            SharedOntologyService sharedOntologyService,
            ILogger<SharedOntologyService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                await sharedOntologyService.UnhideOntologyAsync(userId, ontologyId);

                return Results.Ok(new { message = "Ontology unhidden successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error unhiding ontology {OntologyId}", ontologyId);
                return Results.Problem("An error occurred while unhiding the ontology.");
            }
        }

        /// <summary>
        /// Dismiss a shared ontology
        /// POST /api/shared-ontologies/{ontologyId}/dismiss
        /// </summary>
        private static async Task<IResult> DismissOntology(
            int ontologyId,
            ClaimsPrincipal user,
            SharedOntologyService sharedOntologyService,
            ILogger<SharedOntologyService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                await sharedOntologyService.DismissOntologyAsync(userId, ontologyId);

                return Results.Ok(new { message = "Ontology dismissed successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error dismissing ontology {OntologyId}", ontologyId);
                return Results.Problem("An error occurred while dismissing the ontology.");
            }
        }

        /// <summary>
        /// Update last accessed time for share link access
        /// POST /api/shared-ontologies/{ontologyId}/access/share-link
        /// </summary>
        private static async Task<IResult> UpdateShareLinkAccess(
            int ontologyId,
            ClaimsPrincipal user,
            SharedOntologyService sharedOntologyService,
            ILogger<SharedOntologyService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                await sharedOntologyService.UpdateShareLinkAccessAsync(userId, ontologyId);

                return Results.Ok(new { message = "Access time updated" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating share link access for ontology {OntologyId}", ontologyId);
                return Results.Problem("An error occurred while updating access time.");
            }
        }

        /// <summary>
        /// Update last accessed time for group access
        /// POST /api/shared-ontologies/{ontologyId}/access/group
        /// </summary>
        private static async Task<IResult> UpdateGroupAccess(
            int ontologyId,
            ClaimsPrincipal user,
            SharedOntologyService sharedOntologyService,
            ILogger<SharedOntologyService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                await sharedOntologyService.UpdateGroupAccessAsync(userId, ontologyId);

                return Results.Ok(new { message = "Access time updated" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating group access for ontology {OntologyId}", ontologyId);
                return Results.Problem("An error occurred while updating access time.");
            }
        }

        /// <summary>
        /// Reset all user state (unhide all hidden ontologies)
        /// POST /api/shared-ontologies/reset-all
        /// </summary>
        private static async Task<IResult> ResetAllUserState(
            ClaimsPrincipal user,
            SharedOntologyService sharedOntologyService,
            ILogger<SharedOntologyService> logger)
        {
            try
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                await sharedOntologyService.ResetAllUserStateAsync(userId);

                return Results.Ok(new { message = "All hidden ontologies have been unhidden" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resetting user state");
                return Results.Problem("An error occurred while resetting user state.");
            }
        }
    }

    /// <summary>
    /// Request model for filtering shared ontologies
    /// </summary>
    public class SharedOntologyFilterRequest
    {
        public bool IncludeHidden { get; set; } = false;
        public bool PinnedOnly { get; set; } = false;
        public SharedAccessType? AccessTypeFilter { get; set; } = null;
        public int DaysBack { get; set; } = 90;
        public string? SearchTerm { get; set; }
        public SharedOntologySortBy SortBy { get; set; } = SharedOntologySortBy.LastAccessed;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 24;
    }
}
