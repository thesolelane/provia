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
        private readonly ISubContractorService _subContractorService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            JobTrackerContext context, 
            ITenantContext tenantContext, 
            ISubContractorService subContractorService,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _subContractorService = subContractorService;
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

                // Special handling for Sub-Contractors: they work for multiple companies
                if (user.Role == 2010)
                {
                    // Sub-contractors see multi-company portal
                    return Redirect("/contractor-dashboard.html");
                }

                // Route based on role
                return user.Role switch
                {
                    1510 => Redirect("/admin-dashboard.html"), // Admin
                    1520 => Redirect("/foreman-dashboard.html"), // Foreman
                    1530 => Redirect("/supervisor-dashboard.html"), // Supervisor
                    2001 => Redirect("/field-operator-dashboard.html"), // Field Operator
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

        /// <summary>
        /// Get all companies a sub-contractor works for
        /// </summary>
        [HttpGet("api/contractor/companies")]
        public async Task<ActionResult<object>> GetContractorCompanies()
        {
            try
            {
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null || user.Role != 2010)
                    return Forbid();

                var companies = await _subContractorService.GetCompaniesForSubContractorAsync(user.Id);

                return Ok(new
                {
                    subContractorId = user.Id,
                    companyCount = companies.Count,
                    companies = companies.Select(sc => new
                    {
                        companyId = sc.CompanyId,
                        companyName = sc.Company.CompanyName,
                        status = sc.Status,
                        isVerified = sc.IsVerified,
                        specializations = sc.Specializations,
                        billingRate = sc.BillingRate,
                        addedAt = sc.AddedAt,
                        lastActivityAt = sc.LastActivityAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contractor companies");
                return StatusCode(500, new { message = "Error retrieving companies" });
            }
        }

        /// <summary>
        /// Get available job bids for a sub-contractor
        /// </summary>
        [HttpGet("api/contractor/available-bids")]
        public async Task<ActionResult<object>> GetAvailableBids()
        {
            try
            {
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null || user.Role != 2010)
                    return Forbid();

                var jobs = await _subContractorService.GetAvailableBidsForSubContractorAsync(user.Id);

                return Ok(new
                {
                    availableJobCount = jobs.Count,
                    jobs = jobs.Select(j => new
                    {
                        jobId = j.Id,
                        jobName = j.Name,
                        companyId = j.CompanyId,
                        status = j.Status,
                        estimatedValue = j.Budget,
                        createdAt = j.CreatedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available bids");
                return StatusCode(500, new { message = "Error retrieving bids" });
            }
        }
    }
}
