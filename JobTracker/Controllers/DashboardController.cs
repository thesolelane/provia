using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Services;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Controllers
{
    [Controller]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(JobTrackerContext context, ITenantContext tenantContext, ILogger<DashboardController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        /// <summary>
        /// Route user to role-specific dashboard
        /// </summary>
        [HttpGet("/dashboard")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.Identity?.Name;

                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Dashboard access attempted with no email in claims");
                    return Redirect("/login.html");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail && u.CompanyId == companyId);

                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userEmail}");
                    return Redirect("/login.html");
                }

                // Set tenant context for subsequent requests
                _tenantContext.SetCurrentCompanyId(companyId);
                _tenantContext.UserId = user.Id;

                // Route based on role
                return user.Role switch
                {
                    1510 => Redirect("/admin-dashboard.html"), // Admin
                    1520 => Redirect("/foreman-dashboard.html"), // Foreman
                    1530 => Redirect("/supervisor-dashboard.html"), // Supervisor
                    2001 => Redirect("/field-operator-dashboard.html"), // Field Operator
                    2010 => Redirect("/contractor-dashboard.html"), // Sub-Contractor
                    _ => Redirect("/index.html")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error routing to dashboard");
                return Redirect("/login.html");
            }
        }

        /// <summary>
        /// Get current user info (for client-side role display)
        /// </summary>
        [HttpGet("api/user/current")]
        public async Task<ActionResult<object>> GetCurrentUser()
        {
            try
            {
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var companyId = _tenantContext.GetCurrentCompanyId();

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail && u.CompanyId == companyId);

                if (user == null)
                    return NotFound();

                return Ok(new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    role = user.Role,
                    roleDisplay = user.GetRoleDisplayName(),
                    companyId = user.CompanyId,
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "Error retrieving user info" });
            }
        }
    }
}
