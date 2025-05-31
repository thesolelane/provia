using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Services
{
    public class EnterpriseSecurityService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<EnterpriseSecurityService> _logger;

        public EnterpriseSecurityService(JobTrackerContext context, ILogger<EnterpriseSecurityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> ValidateUserAccess(int userId, string requiredRole = null)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null || !user.Company.IsActive)
                {
                    _logger.LogWarning("Access denied for inactive user {UserId}", userId);
                    return false;
                }

                if (!string.IsNullOrEmpty(requiredRole) && user.Role != requiredRole)
                {
                    _logger.LogWarning("Insufficient privileges for user {UserId}. Required: {RequiredRole}, Actual: {ActualRole}", 
                        userId, requiredRole, user.Role);
                    return false;
                }

                // Log successful access for audit trail
                _logger.LogInformation("User {UserId} ({Role}) accessed system at {Timestamp}", 
                    userId, user.Role, DateTime.UtcNow);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user access for {UserId}", userId);
                return false;
            }
        }

        public async Task LogSecurityEvent(string eventType, int? userId = null, string details = null)
        {
            try
            {
                var securityLog = new SecurityAuditLog
                {
                    EventType = eventType,
                    UserId = userId,
                    Details = details,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    Timestamp = DateTime.UtcNow
                };

                _context.SecurityAuditLogs.Add(securityLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
            }
        }

        private string GetClientIpAddress()
        {
            // Implementation to get client IP address
            return "127.0.0.1"; // Placeholder
        }

        private string GetUserAgent()
        {
            // Implementation to get user agent
            return "Unknown"; // Placeholder
        }
    }

    public class SecurityAuditLog
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class EnterpriseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly EnterpriseSecurityService _securityService;
        private readonly JobTrackerContext _context;

        public EnterpriseAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            EnterpriseSecurityService securityService,
            JobTrackerContext context)
            : base(options, logger, encoder, clock)
        {
            _securityService = securityService;
            _context = context;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return AuthenticateResult.NoResult();
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // For development/testing, accept specific test tokens
                if (token == "test-token")
                {
                    var testClaims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "9"),
                        new Claim(ClaimTypes.Role, "FieldOperator"),
                        new Claim(ClaimTypes.Name, "Test User")
                    };

                    var testIdentity = new ClaimsIdentity(testClaims, Scheme.Name);
                    var testPrincipal = new ClaimsPrincipal(testIdentity);

                    await _securityService.LogSecurityEvent("TEST_LOGIN", 9, "Test token authentication");

                    return AuthenticateResult.Success(new AuthenticationTicket(testPrincipal, Scheme.Name));
                }

                // For production, implement proper JWT validation here
                return AuthenticateResult.Fail("Invalid token");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Authentication error");
                return AuthenticateResult.Fail("Authentication failed");
            }
        }
    }
}