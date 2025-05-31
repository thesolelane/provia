using Microsoft.AspNetCore.Authorization;
using System.Text;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Security
{
    public static class SecurityConfiguration
    {
        public static void ConfigureSecurityServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Authorization Policies for role-based access control
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "MasterAdmin"));
                options.AddPolicy("MasterAdminOnly", policy => policy.RequireRole("MasterAdmin"));
                options.AddPolicy("FieldOperatorAccess", policy => policy.RequireRole("FieldOperator", "Admin", "MasterAdmin"));
            });

            // Security Services
            services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        }
    }

    public interface ISecurityAuditService
    {
        Task LogSecurityEventAsync(string eventType, int? userId, string? details, string? ipAddress);
        Task<bool> ValidateUserAccessAsync(int userId, string? requiredRole = null);
    }

    public class SecurityAuditService : ISecurityAuditService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<SecurityAuditService> _logger;

        public SecurityAuditService(JobTrackerContext context, ILogger<SecurityAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogSecurityEventAsync(string eventType, int? userId, string? details, string? ipAddress)
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

                _context.SecurityAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Security event logged: {EventType} for user {UserId}", eventType, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
            }
        }

        public async Task<bool> ValidateUserAccessAsync(int userId, string? requiredRole = null)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null || !user.Company.IsActive)
                {
                    await LogSecurityEventAsync("ACCESS_DENIED", userId, "Inactive user or company", null);
                    return false;
                }

                if (!string.IsNullOrEmpty(requiredRole) && user.Role != requiredRole)
                {
                    await LogSecurityEventAsync("INSUFFICIENT_PRIVILEGES", userId, 
                        $"Required: {requiredRole}, Actual: {user.Role}", null);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user access for {UserId}", userId);
                return false;
            }
        }
    }

    public class SecurityAuditLog
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}