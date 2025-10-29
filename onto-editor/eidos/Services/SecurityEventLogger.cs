using Microsoft.AspNetCore.Identity;

namespace Eidos.Services;

/// <summary>
/// Service for logging security-related events
/// </summary>
public class SecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Sanitizes strings for safe logging (removes line breaks).
    /// </summary>
    private static string SanitizeForLog(string input)
    {
        if (input == null) return null;
        return input.Replace("\r", "").Replace("\n", "");
    }

    public SecurityEventLogger(
        ILogger<SecurityEventLogger> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void LogLoginSuccess(string userId, string email)
    {
        var ipAddress = GetClientIpAddress();
        _logger.LogInformation(
            "User login successful. UserId: {UserId}, Email: {Email}, IP: {IpAddress}",
            userId, email, ipAddress);
    }

    public void LogLoginFailed(string email, string reason)
    {
        var ipAddress = GetClientIpAddress();
        _logger.LogWarning(
            "Login attempt failed. Email: {Email}, Reason: {Reason}, IP: {IpAddress}",
            email, reason, ipAddress);
    }

    public void LogAccountLockout(string userId, string email)
    {
        var ipAddress = GetClientIpAddress();
        _logger.LogWarning(
            "Account locked out due to failed login attempts. UserId: {UserId}, Email: {Email}, IP: {IpAddress}",
            userId, email, ipAddress);
    }

    public void LogRegistration(string userId, string email)
    {
        var ipAddress = GetClientIpAddress();
        _logger.LogInformation(
            "New user registered. UserId: {UserId}, Email: {Email}, IP: {IpAddress}",
            userId, email, ipAddress);
    }

    public void LogPasswordChange(string userId, string email)
    {
        var ipAddress = GetClientIpAddress();
        _logger.LogInformation(
            "Password changed. UserId: {UserId}, Email: {Email}, IP: {IpAddress}",
            userId, email, ipAddress);
    }

    public void LogPasswordReset(string email)
    {
        var ipAddress = GetClientIpAddress();
        _logger.LogInformation(
            "Password reset requested. Email: {Email}, IP: {IpAddress}",
            email, ipAddress);
    }

    public void LogExternalLoginSuccess(string provider, string userId, string email)
    {
        var ipAddress = GetClientIpAddress();
        _logger.LogInformation(
            "External login successful. Provider: {Provider}, UserId: {UserId}, Email: {Email}, IP: {IpAddress}",
            provider, userId, email, ipAddress);
    }

    public void LogExternalLoginFailed(string provider, string reason)
    {
        var ipAddress = GetClientIpAddress();
        _logger.LogWarning(
            "External login failed. Provider: {Provider}, Reason: {Reason}, IP: {IpAddress}",
            provider, reason, ipAddress);
    }

    public void LogAccountUnlink(string userId, string email, string provider)
    {
        var ipAddress = SanitizeForLog(GetClientIpAddress());
        _logger.LogInformation(
            "External account unlinked. Provider: {Provider}, UserId: {UserId}, Email: {Email}, IP: {IpAddress}",
            provider, userId, email, ipAddress);
    }

    public void LogRateLimitExceeded(string endpoint)
    {
        var ipAddress = SanitizeForLog(GetClientIpAddress());
        _logger.LogWarning(
            "Rate limit exceeded. Endpoint: {Endpoint}, IP: {IpAddress}",
            endpoint, ipAddress);
    }

    public void LogSuspiciousActivity(string activity, string details)
    {
        var ipAddress = SanitizeForLog(GetClientIpAddress());
        _logger.LogWarning(
            "Suspicious activity detected. Activity: {Activity}, Details: {Details}, IP: {IpAddress}",
            activity, details, ipAddress);
    }

    public void LogUnauthorizedAccess(string userId, string resource)
    {
        var ipAddress = SanitizeForLog(GetClientIpAddress());
        _logger.LogWarning(
            "Unauthorized access attempt. UserId: {UserId}, Resource: {Resource}, IP: {IpAddress}",
            userId, resource, ipAddress);
    }

    private string GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return "Unknown";

        // Check for forwarded IP (when behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
                return ips[0].Trim();
        }

        // Check for real IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
