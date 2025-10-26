using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Eidos.Middleware;

/// <summary>
/// Telemetry processor that enriches Application Insights data with additional context
/// Adds user information, ontology IDs, and custom properties to all telemetry
/// </summary>
public class EnrichmentTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EnrichmentTelemetryProcessor(ITelemetryProcessor next, IHttpContextAccessor httpContextAccessor)
    {
        _next = next;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Process(ITelemetry item)
    {
        var context = _httpContextAccessor.HttpContext;

        if (context != null && item is ISupportProperties propertiesItem)
        {
            // Add user information if authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userName = context.User.Identity.Name ?? "Unknown";
                propertiesItem.Properties["User"] = userName;

                // Add user ID if available
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    propertiesItem.Properties["UserId"] = userId;
                }
            }
            else
            {
                propertiesItem.Properties["User"] = "Anonymous";
            }

            // Add request path for easier filtering
            if (context.Request != null)
            {
                propertiesItem.Properties["Path"] = context.Request.Path;
                propertiesItem.Properties["Method"] = context.Request.Method;

                // Extract ontology ID from path if present (e.g., /ontology/123)
                var pathSegments = context.Request.Path.Value?.Split('/');
                if (pathSegments?.Length > 2 && pathSegments[1] == "ontology" && int.TryParse(pathSegments[2], out var ontologyId))
                {
                    propertiesItem.Properties["OntologyId"] = ontologyId.ToString();
                }
            }

            // Add environment
            propertiesItem.Properties["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            // Add timestamp for easier querying
            propertiesItem.Properties["ProcessedAt"] = DateTime.UtcNow.ToString("o");
        }

        // Call the next processor in the chain
        _next.Process(item);
    }
}
