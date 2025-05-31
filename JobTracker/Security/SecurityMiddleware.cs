using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Security
{
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityMiddleware> _logger;

        public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, JobTrackerContext dbContext)
        {
            // Log all requests for audit trail
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var path = context.Request.Path.ToString();

            _logger.LogInformation("Request: {Method} {Path} from {IP} - {UserAgent}", 
                context.Request.Method, path, ipAddress, userAgent);

            // SQL Injection Protection
            if (ContainsSqlInjectionAttempt(context.Request))
            {
                _logger.LogWarning("SQL injection attempt detected from {IP} on path {Path}", ipAddress, path);
                await LogSecurityEvent(dbContext, "SQL_INJECTION_ATTEMPT", null, $"Path: {path}", ipAddress);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request");
                return;
            }

            // XSS Protection
            if (ContainsXssAttempt(context.Request))
            {
                _logger.LogWarning("XSS attempt detected from {IP} on path {Path}", ipAddress, path);
                await LogSecurityEvent(dbContext, "XSS_ATTEMPT", null, $"Path: {path}", ipAddress);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request");
                return;
            }

            // Rate limiting for API endpoints
            if (await IsRateLimited(context, dbContext))
            {
                _logger.LogWarning("Rate limit exceeded from {IP}", ipAddress);
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Rate limit exceeded");
                return;
            }

            await _next(context);
        }

        private bool ContainsSqlInjectionAttempt(HttpRequest request)
        {
            var sqlPatterns = new[]
            {
                @"(\bunion\b.*\bselect\b)|(\bselect\b.*\bunion\b)",
                @"(\bdrop\b.*\btable\b)|(\btable\b.*\bdrop\b)",
                @"(\binsert\b.*\binto\b)|(\binto\b.*\binsert\b)",
                @"(\bdelete\b.*\bfrom\b)|(\bfrom\b.*\bdelete\b)",
                @"(\bupdate\b.*\bset\b)|(\bset\b.*\bupdate\b)",
                @"(\bexec\b.*\bxp_)|(\bxp_.*\bexec\b)",
                @"(\bsp_.*\bexec\b)|(\bexec\b.*\bsp_)",
                @"(\bor\b.*\b1\s*=\s*1\b)|(\b1\s*=\s*1\b.*\bor\b)",
                @"(\band\b.*\b1\s*=\s*1\b)|(\b1\s*=\s*1\b.*\band\b)",
                @"(\bor\b.*\b'\s*=\s*'\b)|(\b'\s*=\s*'\b.*\bor\b)"
            };

            var content = GetRequestContent(request);
            foreach (var pattern in sqlPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
            return false;
        }

        private bool ContainsXssAttempt(HttpRequest request)
        {
            var xssPatterns = new[]
            {
                @"<\s*script[^>]*>",
                @"<\s*/\s*script\s*>",
                @"javascript\s*:",
                @"on\w+\s*=",
                @"<\s*iframe[^>]*>",
                @"<\s*object[^>]*>",
                @"<\s*embed[^>]*>",
                @"<\s*link[^>]*>",
                @"<\s*meta[^>]*>",
                @"expression\s*\(",
                @"vbscript\s*:",
                @"<\s*img[^>]*onerror",
                @"<\s*svg[^>]*onload"
            };

            var content = GetRequestContent(request);
            foreach (var pattern in xssPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
            return false;
        }

        private string GetRequestContent(HttpRequest request)
        {
            var content = request.QueryString.ToString();
            
            if (request.HasFormContentType && request.Form != null)
            {
                foreach (var item in request.Form)
                {
                    content += " " + item.Key + "=" + item.Value;
                }
            }

            foreach (var header in request.Headers)
            {
                content += " " + header.Key + "=" + header.Value;
            }

            return content.ToLower();
        }

        private async Task<bool> IsRateLimited(HttpContext context, JobTrackerContext dbContext)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress)) return false;

            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-1);

            try
            {
                var requestCount = await dbContext.SecurityAuditLogs
                    .Where(log => log.IpAddress == ipAddress && 
                                 log.Timestamp >= windowStart && 
                                 log.EventType == "REQUEST")
                    .CountAsync();

                // Allow 60 requests per minute per IP
                if (requestCount >= 60)
                {
                    await LogSecurityEvent(dbContext, "RATE_LIMIT_EXCEEDED", null, 
                        $"IP: {ipAddress}, Requests: {requestCount}", ipAddress);
                    return true;
                }

                // Log this request
                await LogSecurityEvent(dbContext, "REQUEST", null, 
                    $"Path: {context.Request.Path}", ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for IP {IP}", ipAddress);
            }

            return false;
        }

        private async Task LogSecurityEvent(JobTrackerContext context, string eventType, 
            int? userId, string? details, string? ipAddress)
        {
            try
            {
                var auditLog = new SecurityAuditLog
                {
                    EventType = eventType,
                    UserId = userId,
                    Details = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                context.SecurityAuditLogs.Add(auditLog);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
            }
        }
    }
}