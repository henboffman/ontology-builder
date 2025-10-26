using System.Net;
using System.Text.Json;
using Eidos.Exceptions;

namespace Eidos.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions,
/// logs them with correlation IDs, and returns user-friendly error responses
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Generate a unique correlation ID for this error
        var correlationId = Guid.NewGuid().ToString();

        // Add correlation ID to response headers so it's accessible in the browser
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        var (statusCode, errorResponse) = exception switch
        {
            OntologyNotFoundException ex => (
                HttpStatusCode.NotFound,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = ex.ErrorCode,
                    Message = ex.UserFriendlyMessage,
                    Details = _env.IsDevelopment() ? ex.Message : null
                }),

            ConceptNotFoundException ex => (
                HttpStatusCode.NotFound,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = ex.ErrorCode,
                    Message = ex.UserFriendlyMessage,
                    Details = _env.IsDevelopment() ? ex.Message : null
                }),

            RelationshipNotFoundException ex => (
                HttpStatusCode.NotFound,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = ex.ErrorCode,
                    Message = ex.UserFriendlyMessage,
                    Details = _env.IsDevelopment() ? ex.Message : null
                }),

            ShareLinkNotFoundException ex => (
                HttpStatusCode.NotFound,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = ex.ErrorCode,
                    Message = ex.UserFriendlyMessage,
                    Details = _env.IsDevelopment() ? ex.Message : null
                }),

            ShareLinkExpiredException ex => (
                HttpStatusCode.Gone,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = ex.ErrorCode,
                    Message = ex.UserFriendlyMessage,
                    Details = _env.IsDevelopment() ? ex.Message : null
                }),

            ShareLinkDeactivatedException ex => (
                HttpStatusCode.Gone,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = ex.ErrorCode,
                    Message = ex.UserFriendlyMessage,
                    Details = _env.IsDevelopment() ? ex.Message : null
                }),

            InsufficientPermissionException ex => (
                HttpStatusCode.Forbidden,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = ex.ErrorCode,
                    Message = ex.UserFriendlyMessage,
                    Details = _env.IsDevelopment() ? ex.Message : null
                }),

            OntologyValidationException ex => (
                HttpStatusCode.BadRequest,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = ex.ErrorCode,
                    Message = ex.UserFriendlyMessage,
                    Details = _env.IsDevelopment() ? ex.Message : null,
                    ValidationErrors = ex.ValidationErrors
                }),

            OntologyImportException ex => (
                HttpStatusCode.BadRequest,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = ex.ErrorCode,
                    Message = ex.UserFriendlyMessage,
                    Details = _env.IsDevelopment() ? ex.Message : null
                }),

            ArgumentNullException ex => (
                HttpStatusCode.BadRequest,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = "INVALID_ARGUMENT",
                    Message = "A required parameter was not provided.",
                    Details = _env.IsDevelopment() ? ex.Message : null
                }),

            ArgumentException ex => (
                HttpStatusCode.BadRequest,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = "INVALID_ARGUMENT",
                    Message = "One or more arguments are invalid.",
                    Details = _env.IsDevelopment() ? ex.Message : null
                }),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = "UNAUTHORIZED",
                    Message = "You must be logged in to perform this action.",
                    Details = _env.IsDevelopment() ? exception.Message : null
                }),

            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse
                {
                    CorrelationId = correlationId,
                    ErrorCode = "INTERNAL_SERVER_ERROR",
                    Message = "An unexpected error occurred. Please try again later. If the problem persists, contact support with error ID: " + correlationId,
                    Details = _env.IsDevelopment() ? exception.Message : null
                })
        };

        // Log the full exception with correlation ID and stack trace
        _logger.LogError(exception,
            "Unhandled exception occurred. Correlation ID: {CorrelationId}, Status Code: {StatusCode}, Error Code: {ErrorCode}",
            correlationId,
            (int)statusCode,
            errorResponse.ErrorCode);

        // Write the response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _env.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }

    /// <summary>
    /// Standard error response model
    /// </summary>
    private class ErrorResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public Dictionary<string, string[]>? ValidationErrors { get; set; }
    }
}
