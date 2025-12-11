using System.Diagnostics;

namespace LMSPlatform.Services.Middleware;

/// <summary>
/// Middleware for logging security-related events
/// </summary>
public class SecurityLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityLoggingMiddleware> _logger;

    public SecurityLoggingMiddleware(RequestDelegate next, ILogger<SecurityLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        // Log request start
        LogRequestStart(context, requestId);

        try
        {
            await _next(context);
            
            stopwatch.Stop();
            
            // Log security events based on response
            LogSecurityEvents(context, requestId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogSecurityException(context, requestId, ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void LogRequestStart(HttpContext context, string requestId)
    {
        var user = context.User.Identity?.Name ?? "Anonymous";
        var path = context.Request.Path;
        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Log sensitive endpoint access
        if (IsSensitiveEndpoint(path))
        {
            _logger.LogInformation(
                "[{RequestId}] Security: Sensitive endpoint access - User: {User}, Path: {Path}, Method: {Method}, IP: {IP}",
                requestId, user, path, method, ip);
        }
    }

    private void LogSecurityEvents(HttpContext context, string requestId, long elapsedMs)
    {
        var statusCode = context.Response.StatusCode;
        var user = context.User.Identity?.Name ?? "Anonymous";
        var path = context.Request.Path;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Log authentication failures
        if (statusCode == 401)
        {
            _logger.LogWarning(
                "[{RequestId}] Security: Authentication failed - Path: {Path}, IP: {IP}, Duration: {Duration}ms",
                requestId, path, ip, elapsedMs);
        }

        // Log authorization failures
        if (statusCode == 403)
        {
            _logger.LogWarning(
                "[{RequestId}] Security: Authorization denied - User: {User}, Path: {Path}, IP: {IP}, Duration: {Duration}ms",
                requestId, user, path, ip, elapsedMs);
        }

        // Log successful admin operations
        if (IsAdminEndpoint(path) && statusCode >= 200 && statusCode < 300)
        {
            _logger.LogInformation(
                "[{RequestId}] Security: Admin operation completed - User: {User}, Path: {Path}, Duration: {Duration}ms",
                requestId, user, path, elapsedMs);
        }

        // Log successful login/logout
        if (IsAuthEndpoint(path) && statusCode >= 200 && statusCode < 300)
        {
            _logger.LogInformation(
                "[{RequestId}] Security: Auth operation - User: {User}, Path: {Path}, IP: {IP}",
                requestId, user, path, ip);
        }
    }

    private void LogSecurityException(HttpContext context, string requestId, Exception ex, long elapsedMs)
    {
        var user = context.User.Identity?.Name ?? "Anonymous";
        var path = context.Request.Path;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        _logger.LogError(ex,
            "[{RequestId}] Security: Exception occurred - User: {User}, Path: {Path}, IP: {IP}, Duration: {Duration}ms",
            requestId, user, path, ip, elapsedMs);
    }

    private static bool IsSensitiveEndpoint(string path)
    {
        var sensitivePaths = new[]
        {
            "/Admin",
            "/Payment",
            "/Identity/Account",
            "/api/payment",
            "/api/admin"
        };

        return sensitivePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAdminEndpoint(string path)
    {
        return path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAuthEndpoint(string path)
    {
        var authPaths = new[]
        {
            "/Identity/Account/Login",
            "/Identity/Account/Logout",
            "/Identity/Account/Register"
        };

        return authPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension methods for security logging middleware
/// </summary>
public static class SecurityLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityLoggingMiddleware>();
    }
}
