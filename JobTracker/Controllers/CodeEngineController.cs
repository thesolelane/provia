using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Services;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobTracker.Controllers
{
    /// <summary>
    /// Code Engine Controller - Manages scope selections, code rules, and department views
    /// Supports the Job Options Wizard and auto-generation of permits/inspections
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CodeEngineController : ControllerBase
    {
        private readonly ICodeEngineService _codeEngineService;
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<CodeEngineController> _logger;

        public CodeEngineController(
            ICodeEngineService codeEngineService,
            JobTrackerContext context,
            ITenantContext tenantContext,
            ILogger<CodeEngineController> logger)
        {
            _codeEngineService = codeEngineService;
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        /// <summary>
        /// Get all scope categories with items for Job Options Wizard
        /// </summary>
        [HttpGet("scope-categories")]
        public async Task<ActionResult<List<ScopeCategoryDto>>> GetScopeCategories()
        {
            try
            {
                var categories = await _codeEngineService.GetAllScopeCategoriesAsync();
                return Ok(new { count = categories.Count, categories = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scope categories");
                return StatusCode(500, new { message = "Error retrieving scope categories" });
            }
        }

        /// <summary>
        /// Get code rules triggered by a specific scope item
        /// </summary>
        [HttpGet("code-rules/{scopeItemCode}")]
        public async Task<ActionResult<List<CodeRuleDto>>> GetCodeRulesForScope(string scopeItemCode)
        {
            try
            {
                var rules = await _codeEngineService.GetCodeRulesForScopeAsync(scopeItemCode);
                return Ok(new { count = rules.Count, rules = rules });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting code rules");
                return StatusCode(500, new { message = "Error retrieving code rules" });
            }
        }

        /// <summary>
        /// Save selected scopes for a job (from Job Options Wizard)
        /// Supervisor or Admin only
        /// </summary>
        [HttpPost("job/{jobId}/scopes")]
        public async Task<ActionResult> SaveJobScopes(int jobId, [FromBody] JobScopeSelectionDto request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                // Only Admin or Supervisor can save scopes
                if (user.Role != RoleCodes.Admin && user.Role != RoleCodes.Supervisor)
                    return Forbid("Only admins and supervisors can set job scopes");

                var companyId = _tenantContext.GetCurrentCompanyId();

                var success = await _codeEngineService.SaveJobScopesAsync(
                    jobId, 
                    companyId, 
                    request.SelectedScopeItemIds, 
                    user.Id);

                if (!success)
                    return BadRequest("Failed to save job scopes");

                // Auto-generate inspections from scopes
                var inspections = await _codeEngineService.GenerateInspectionsFromScopesAsync(jobId, companyId);

                // Get required permit types
                var permitTypes = await _codeEngineService.GetRequiredPermitTypesAsync(jobId);

                _logger.LogInformation($"Saved scopes for job {jobId}, generated {inspections.Count} inspections");

                return Ok(new 
                { 
                    message = "Job scopes saved successfully",
                    scopeCount = request.SelectedScopeItemIds.Count,
                    inspectionsGenerated = inspections.Count,
                    requiredPermits = permitTypes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving job scopes");
                return StatusCode(500, new { message = "Error saving job scopes" });
            }
        }

        /// <summary>
        /// Get scopes for a job
        /// </summary>
        [HttpGet("job/{jobId}/scopes")]
        public async Task<ActionResult> GetJobScopes(int jobId)
        {
            try
            {
                var scopes = await _context.JobScopes
                    .Where(js => js.JobId == jobId)
                    .Include(js => js.ScopeItem)
                    .ThenInclude(si => si!.ScopeCategory)
                    .ToListAsync();

                var result = scopes.Select(js => new
                {
                    id = js.Id,
                    scopeItemId = js.ScopeItemId,
                    itemCode = js.ScopeItem?.ItemCode,
                    itemName = js.ScopeItem?.ItemName,
                    categoryName = js.ScopeItem?.ScopeCategory?.CategoryName,
                    tradeType = js.ScopeItem?.TradeType,
                    requiresLicensedTrade = js.ScopeItem?.RequiresLicensedTrade,
                    department = js.ScopeItem?.Department,
                    status = js.Status,
                    assignedUserId = js.AssignedUserId,
                    isPermitHolder = js.IsPermitHolder,
                    notes = js.Notes
                });

                return Ok(new { count = scopes.Count, scopes = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job scopes");
                return StatusCode(500, new { message = "Error retrieving job scopes" });
            }
        }

        /// <summary>
        /// Get required permits for a job based on scopes
        /// </summary>
        [HttpGet("job/{jobId}/required-permits")]
        public async Task<ActionResult> GetRequiredPermits(int jobId)
        {
            try
            {
                var permitTypes = await _codeEngineService.GetRequiredPermitTypesAsync(jobId);
                return Ok(new { jobId = jobId, requiredPermits = permitTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting required permits");
                return StatusCode(500, new { message = "Error retrieving required permits" });
            }
        }

        /// <summary>
        /// Get department-specific view for a job
        /// Filters scopes, code references, and inspections by department
        /// </summary>
        [HttpGet("job/{jobId}/department/{department}")]
        public async Task<ActionResult> GetDepartmentView(int jobId, string department)
        {
            try
            {
                var view = await _codeEngineService.GetDepartmentViewAsync(jobId, department.ToUpper());
                return Ok(view);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department view");
                return StatusCode(500, new { message = "Error retrieving department view" });
            }
        }

        /// <summary>
        /// Assign a trade to a job scope
        /// </summary>
        [HttpPost("job/{jobId}/assign-trade")]
        public async Task<ActionResult> AssignTradeToJob(int jobId, [FromBody] dynamic request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                // Only Admin, Supervisor, or Foreman can assign trades
                if (user.Role != RoleCodes.Admin && user.Role != RoleCodes.Supervisor && user.Role != RoleCodes.Foreman)
                    return Forbid("Only admins, supervisors, and foremen can assign trades");

                var companyId = _tenantContext.GetCurrentCompanyId();

                var assignment = new TradeAssignment
                {
                    JobId = jobId,
                    UserId = (int)request.userId,
                    CompanyId = companyId,
                    AssignmentType = (string)request.assignmentType, // FOREMAN, SUBCONTRACTOR, FIELD_OPERATOR
                    TradeType = (string?)request.tradeType,
                    IsPermitHolder = (bool?)request.isPermitHolder ?? false,
                    Status = "ACTIVE",
                    AssignedAt = DateTime.UtcNow,
                    Notes = (string?)request.notes
                };

                _context.TradeAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Trade assigned to job {jobId}");
                return Ok(new { message = "Trade assigned successfully", assignmentId = assignment.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning trade");
                return StatusCode(500, new { message = "Error assigning trade" });
            }
        }

        /// <summary>
        /// Get trade assignments for a job
        /// </summary>
        [HttpGet("job/{jobId}/assignments")]
        public async Task<ActionResult> GetJobAssignments(int jobId)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                var assignments = await _context.TradeAssignments
                    .Where(ta => ta.JobId == jobId)
                    .Include(ta => ta.User)
                    .ToListAsync();

                // Filter based on role
                if (user.Role == RoleCodes.Subcontractor)
                {
                    // Subs only see their own assignment
                    assignments = assignments.Where(a => a.UserId == user.Id).ToList();
                }
                else if (user.Role == RoleCodes.FieldOperator)
                {
                    // Field ops only see their own assignment
                    assignments = assignments.Where(a => a.UserId == user.Id).ToList();
                }

                var result = assignments.Select(a => new
                {
                    id = a.Id,
                    userId = a.UserId,
                    userName = a.User?.GetDisplayName(),
                    assignmentType = a.AssignmentType,
                    tradeType = a.TradeType,
                    isPermitHolder = a.IsPermitHolder,
                    status = a.Status,
                    assignedAt = a.AssignedAt,
                    notes = a.Notes
                });

                return Ok(new { count = assignments.Count, assignments = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job assignments");
                return StatusCode(500, new { message = "Error retrieving assignments" });
            }
        }

        /// <summary>
        /// Seed Massachusetts codes (admin only, one-time setup)
        /// </summary>
        [HttpPost("seed-codes")]
        public async Task<ActionResult> SeedCodes()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null || user.Role != RoleCodes.Admin)
                    return Forbid("Only admins can seed codes");

                await _codeEngineService.SeedMassachusettsCodesAsync();
                await _codeEngineService.SeedScopeCategoriesAsync();

                return Ok(new { message = "Massachusetts codes and scope categories seeded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding codes");
                return StatusCode(500, new { message = "Error seeding codes" });
            }
        }

        /// <summary>
        /// Get role limits and permissions info
        /// </summary>
        [HttpGet("role-info")]
        public ActionResult GetRoleInfo()
        {
            return Ok(new
            {
                roles = new[]
                {
                    new { code = RoleCodes.Admin, name = "Admin", limit = RoleCodes.AdminLimit, canHoldPermit = false },
                    new { code = RoleCodes.Supervisor, name = "Supervisor", limit = RoleCodes.SupervisorLimit, canHoldPermit = false },
                    new { code = RoleCodes.Foreman, name = "Foreman", limit = RoleCodes.ForemanLimit, canHoldPermit = false },
                    new { code = RoleCodes.FieldOperator, name = "Field Operator", limit = RoleCodes.FieldOperatorLimit, canHoldPermit = false },
                    new { code = RoleCodes.Subcontractor, name = "Subcontractor", limit = RoleCodes.SubcontractorLimit, canHoldPermit = true }
                },
                departments = DepartmentCodes.AllDepartments,
                licensedTrades = TradeTypes.LicensedTrades,
                generalTrades = TradeTypes.GeneralTrades
            });
        }
    }
}
