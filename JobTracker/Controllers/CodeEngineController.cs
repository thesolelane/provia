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
                    new { code = RoleCodes.MasterAdmin, name = "Master Admin", description = "General Contractor / Account Owner (individual or company)", limit = RoleCodes.MasterAdminLimit, canHoldPermit = false, isLicenseHolder = true },
                    new { code = RoleCodes.Supervisor, name = "Supervisor", description = "Project managers, create jobs, approve scopes", limit = RoleCodes.SupervisorLimit, canHoldPermit = false, isLicenseHolder = false },
                    new { code = RoleCodes.Foreman, name = "Foreman", description = "On-site leads, assign daily tasks", limit = RoleCodes.ForemanLimit, canHoldPermit = false, isLicenseHolder = false },
                    new { code = RoleCodes.FieldOperator, name = "Field Operator", description = "In-house crews under GC permit", limit = RoleCodes.FieldOperatorLimit, canHoldPermit = false, isLicenseHolder = false },
                    new { code = RoleCodes.Subcontractor, name = "Subcontractor", description = "Licensed trades with permit authority", limit = RoleCodes.SubcontractorLimit, canHoldPermit = true, isLicenseHolder = false }
                },
                departments = DepartmentCodes.AllDepartments,
                licensedTrades = TradeTypes.LicensedTrades,
                generalTrades = TradeTypes.GeneralTrades
            });
        }

        // ==================== PERMIT MANAGEMENT ====================

        /// <summary>
        /// Get all permits for a job
        /// </summary>
        [HttpGet("job/{jobId}/permits")]
        public async Task<ActionResult> GetJobPermits(int jobId)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var permits = await _context.JobPermits
                    .Where(p => p.JobId == jobId && p.CompanyId == companyId)
                    .OrderBy(p => p.PermitType)
                    .Select(p => new
                    {
                        p.Id,
                        p.JobId,
                        p.PermitType,
                        p.Status,
                        p.PermitNumber,
                        p.IssuingAuthority,
                        p.ApplicationDate,
                        p.SubmittedDate,
                        p.ApprovedDate,
                        p.ExpirationDate,
                        p.ApplicationFee,
                        p.FeePaid,
                        p.HasPlotPlan,
                        p.HasConstructionDrawings,
                        p.HasContractorLicense,
                        p.HasOwnerAuthorization,
                        p.Notes,
                        p.DenialReason,
                        p.CodeReference,
                        p.AssignedUserId,
                        p.CreatedAt,
                        p.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new { count = permits.Count, permits });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job permits");
                return StatusCode(500, new { message = "Error retrieving permits" });
            }
        }

        /// <summary>
        /// Initialize permits for a job based on required permit types
        /// Creates permit records if they don't exist
        /// </summary>
        [HttpPost("job/{jobId}/permits/initialize")]
        public async Task<ActionResult> InitializeJobPermits(int jobId)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                
                // Get required permit types from scopes
                var requiredPermitTypes = await _codeEngineService.GetRequiredPermitTypesAsync(jobId);
                
                // Get existing permits
                var existingPermits = await _context.JobPermits
                    .Where(p => p.JobId == jobId && p.CompanyId == companyId)
                    .Select(p => p.PermitType)
                    .ToListAsync();

                var newPermits = new List<JobPermit>();
                foreach (var permitType in requiredPermitTypes)
                {
                    if (!existingPermits.Contains(permitType))
                    {
                        var codeRef = permitType switch
                        {
                            "BUILDING" => "780 CMR",
                            "ELECTRICAL" => "527 CMR",
                            "PLUMBING" => "248 CMR",
                            "GAS" => "248 CMR",
                            "FIRE" => "527 CMR 12.00",
                            _ => null
                        };

                        newPermits.Add(new JobPermit
                        {
                            JobId = jobId,
                            CompanyId = companyId,
                            PermitType = permitType,
                            Status = "PENDING",
                            CodeReference = codeRef,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (newPermits.Count > 0)
                {
                    await _context.JobPermits.AddRangeAsync(newPermits);
                    await _context.SaveChangesAsync();
                }

                // Return all permits
                var permits = await _context.JobPermits
                    .Where(p => p.JobId == jobId && p.CompanyId == companyId)
                    .OrderBy(p => p.PermitType)
                    .ToListAsync();

                return Ok(new 
                { 
                    message = $"Initialized {newPermits.Count} new permits",
                    count = permits.Count, 
                    permits 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing job permits");
                return StatusCode(500, new { message = "Error initializing permits" });
            }
        }

        /// <summary>
        /// Update a permit
        /// </summary>
        [HttpPut("permits/{id}")]
        public async Task<ActionResult> UpdatePermit(int id, [FromBody] UpdatePermitDto dto)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var permit = await _context.JobPermits
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

                if (permit == null)
                    return NotFound(new { message = "Permit not found" });

                // Update fields
                if (!string.IsNullOrEmpty(dto.Status))
                    permit.Status = dto.Status;
                if (dto.PermitNumber != null)
                    permit.PermitNumber = dto.PermitNumber;
                if (dto.IssuingAuthority != null)
                    permit.IssuingAuthority = dto.IssuingAuthority;
                if (dto.ApplicationDate.HasValue)
                    permit.ApplicationDate = dto.ApplicationDate;
                if (dto.SubmittedDate.HasValue)
                    permit.SubmittedDate = dto.SubmittedDate;
                if (dto.ApprovedDate.HasValue)
                    permit.ApprovedDate = dto.ApprovedDate;
                if (dto.ExpirationDate.HasValue)
                    permit.ExpirationDate = dto.ExpirationDate;
                if (dto.ApplicationFee.HasValue)
                    permit.ApplicationFee = dto.ApplicationFee;
                if (dto.FeePaid.HasValue)
                    permit.FeePaid = dto.FeePaid.Value;
                if (dto.HasPlotPlan.HasValue)
                    permit.HasPlotPlan = dto.HasPlotPlan.Value;
                if (dto.HasConstructionDrawings.HasValue)
                    permit.HasConstructionDrawings = dto.HasConstructionDrawings.Value;
                if (dto.HasContractorLicense.HasValue)
                    permit.HasContractorLicense = dto.HasContractorLicense.Value;
                if (dto.HasOwnerAuthorization.HasValue)
                    permit.HasOwnerAuthorization = dto.HasOwnerAuthorization.Value;
                if (dto.Notes != null)
                    permit.Notes = dto.Notes;
                if (dto.DenialReason != null)
                    permit.DenialReason = dto.DenialReason;
                if (dto.AssignedUserId.HasValue)
                    permit.AssignedUserId = dto.AssignedUserId;

                permit.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Permit updated successfully", permit });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permit");
                return StatusCode(500, new { message = "Error updating permit" });
            }
        }

        /// <summary>
        /// Get a single permit
        /// </summary>
        [HttpGet("permits/{id}")]
        public async Task<ActionResult> GetPermit(int id)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var permit = await _context.JobPermits
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

                if (permit == null)
                    return NotFound(new { message = "Permit not found" });

                return Ok(permit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permit");
                return StatusCode(500, new { message = "Error retrieving permit" });
            }
        }

        /// <summary>
        /// Get subcontractors available for permit assignment
        /// Filters by trade type based on permit type
        /// </summary>
        [HttpGet("subcontractors")]
        public async Task<ActionResult> GetSubcontractorsForPermits([FromQuery] string? permitType = null)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();

                // Get subcontractors linked to this company
                var subcontractors = await _context.SubContractorCompanies
                    .Where(sc => sc.CompanyId == companyId && sc.Status == "ACTIVE")
                    .Include(sc => sc.SubContractorUser)
                    .Select(sc => new
                    {
                        userId = sc.SubContractorUserId,
                        name = sc.SubContractorUser.FirstName + " " + sc.SubContractorUser.LastName,
                        email = sc.SubContractorUser.Email,
                        phone = sc.SubContractorUser.PhoneNumber,
                        specializations = sc.Specializations,
                        isVerified = sc.IsVerified
                    })
                    .ToListAsync();

                // Also get users with Subcontractor role in this company
                var subUsers = await _context.Users
                    .Where(u => u.CompanyId == companyId && u.Role == RoleCodes.Subcontractor && u.IsActive)
                    .Select(u => new
                    {
                        userId = u.Id,
                        name = u.FirstName + " " + u.LastName,
                        email = u.Email,
                        phone = u.PhoneNumber,
                        specializations = (string?)null,
                        isVerified = true
                    })
                    .ToListAsync();

                // Combine and deduplicate
                var allSubs = subcontractors
                    .Concat(subUsers)
                    .GroupBy(s => s.userId)
                    .Select(g => g.First())
                    .OrderBy(s => s.name)
                    .ToList();

                return Ok(new { count = allSubs.Count, subcontractors = allSubs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subcontractors");
                return StatusCode(500, new { message = "Error retrieving subcontractors" });
            }
        }

        /// <summary>
        /// Get permit holder types - explains who pulls each permit type
        /// </summary>
        [HttpGet("permit-holders")]
        public ActionResult GetPermitHolderInfo()
        {
            return Ok(new
            {
                permitTypes = new[]
                {
                    new { 
                        type = "BUILDING", 
                        pulledBy = "GC", 
                        description = "Pulled by General Contractor (license holder)",
                        requiredLicense = "Construction Supervisor License"
                    },
                    new { 
                        type = "ELECTRICAL", 
                        pulledBy = "SUBCONTRACTOR", 
                        description = "Pulled by Licensed Electrician",
                        requiredLicense = "Electrician License (527 CMR)"
                    },
                    new { 
                        type = "PLUMBING", 
                        pulledBy = "SUBCONTRACTOR", 
                        description = "Pulled by Licensed Plumber",
                        requiredLicense = "Plumber License (248 CMR)"
                    },
                    new { 
                        type = "GAS", 
                        pulledBy = "SUBCONTRACTOR", 
                        description = "Pulled by Licensed Gas Fitter (separate from plumbing)",
                        requiredLicense = "Gas Fitter License (248 CMR)"
                    },
                    new { 
                        type = "HVAC", 
                        pulledBy = "SUBCONTRACTOR", 
                        description = "Pulled by Licensed HVAC Installer",
                        requiredLicense = "Refrigeration Technician License"
                    },
                    new { 
                        type = "SHEET_METAL", 
                        pulledBy = "SUBCONTRACTOR", 
                        description = "Pulled by Licensed Sheet Metal Worker (separate from HVAC)",
                        requiredLicense = "Sheet Metal License"
                    },
                    new { 
                        type = "OIL_BURNER", 
                        pulledBy = "SUBCONTRACTOR", 
                        description = "Pulled by Licensed Oil Burner Technician",
                        requiredLicense = "Oil Burner Technician License"
                    },
                    new { 
                        type = "FIRE", 
                        pulledBy = "SUBCONTRACTOR", 
                        description = "Pulled by Licensed Fire Protection Contractor",
                        requiredLicense = "Fire Protection License"
                    }
                }
            });
        }

        // ==================== PROPERTY DATA & PDF GENERATION ====================

        /// <summary>
        /// Fetch property data from MassGIS for a job
        /// </summary>
        [HttpPost("job/{jobId}/property-data/fetch")]
        public async Task<ActionResult> FetchPropertyData(int jobId, [FromServices] IPermitDocumentService permitDocService)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var propertyProfile = await permitDocService.FetchAndSavePropertyDataAsync(jobId, companyId);

                if (propertyProfile == null)
                {
                    return Ok(new { 
                        success = false, 
                        message = "Could not fetch property data. Please check the job address and try again." 
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Property data fetched from MassGIS",
                    propertyProfile = new
                    {
                        propertyProfile.ParcelId,
                        propertyProfile.MapLot,
                        propertyProfile.OwnerName,
                        propertyProfile.OwnerAddress,
                        propertyProfile.PropertyAddress,
                        propertyProfile.City,
                        propertyProfile.ZoningCode,
                        propertyProfile.LotAreaSqFt,
                        propertyProfile.LandValue,
                        propertyProfile.BuildingValue,
                        propertyProfile.TotalAssessedValue,
                        propertyProfile.UseCode,
                        propertyProfile.UseDescription,
                        propertyProfile.YearBuilt,
                        propertyProfile.FiscalYear,
                        propertyProfile.GisFetchedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching property data");
                return StatusCode(500, new { message = "Error fetching property data" });
            }
        }

        /// <summary>
        /// Get property profile for a job
        /// </summary>
        [HttpGet("job/{jobId}/property-data")]
        public async Task<ActionResult> GetPropertyData(int jobId)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var propertyProfile = await _context.PropertyProfiles
                    .FirstOrDefaultAsync(p => p.JobId == jobId && p.CompanyId == companyId);

                if (propertyProfile == null)
                {
                    return Ok(new { exists = false, message = "No property data on file. Click 'Fetch from GIS' to retrieve." });
                }

                return Ok(new
                {
                    exists = true,
                    propertyProfile = new
                    {
                        propertyProfile.ParcelId,
                        propertyProfile.MapLot,
                        propertyProfile.OwnerName,
                        propertyProfile.OwnerAddress,
                        propertyProfile.PropertyAddress,
                        propertyProfile.City,
                        propertyProfile.ZoningCode,
                        propertyProfile.LotAreaSqFt,
                        propertyProfile.LandValue,
                        propertyProfile.BuildingValue,
                        propertyProfile.TotalAssessedValue,
                        propertyProfile.UseCode,
                        propertyProfile.UseDescription,
                        propertyProfile.YearBuilt,
                        propertyProfile.FiscalYear,
                        propertyProfile.GisFetchedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property data");
                return StatusCode(500, new { message = "Error retrieving property data" });
            }
        }

        /// <summary>
        /// Upload a document as a permit form template (saves copy, does not modify original)
        /// </summary>
        [HttpPost("permit-templates/upload")]
        public async Task<ActionResult> UploadPermitTemplate(
            [FromForm] IFormFile file,
            [FromForm] string templateName,
            [FromForm] string permitType,
            [FromForm] string? description,
            [FromForm] string? version)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file uploaded" });

                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { message = "Only PDF, DOCX, and XLSX files are allowed" });

                var templateDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "permits");
                if (!Directory.Exists(templateDir))
                    Directory.CreateDirectory(templateDir);

                var uniqueFileName = $"{permitType}_{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(templateDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var storagePath = $"/templates/permits/{uniqueFileName}";
                var template = new PermitFormTemplate
                {
                    TemplateName = templateName,
                    PermitType = permitType,
                    Description = description,
                    Version = version ?? "1.0",
                    StoragePath = storagePath,
                    FilePath = storagePath,
                    OriginalFileName = file.FileName,
                    FileSize = file.Length,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PermitFormTemplates.Add(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Uploaded permit template: {TemplateName} ({FileName})", templateName, uniqueFileName);

                return Ok(new 
                { 
                    message = "Template uploaded successfully",
                    template = new
                    {
                        template.Id,
                        template.TemplateName,
                        template.PermitType,
                        template.FilePath,
                        template.OriginalFileName,
                        template.Version,
                        template.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading permit template");
                return StatusCode(500, new { message = "Error uploading template" });
            }
        }

        /// <summary>
        /// Get available permit form templates
        /// </summary>
        [HttpGet("permit-templates")]
        public async Task<ActionResult> GetPermitTemplates([FromQuery] string? permitType, [FromServices] IPermitDocumentService permitDocService)
        {
            try
            {
                var templates = await permitDocService.GetActiveTemplatesAsync(permitType);
                return Ok(new { count = templates.Count, templates });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permit templates");
                return StatusCode(500, new { message = "Error retrieving templates" });
            }
        }

        /// <summary>
        /// Get form data preview for a permit (what will be filled in the PDF)
        /// </summary>
        [HttpGet("permits/{permitId}/form-data")]
        public async Task<ActionResult> GetPermitFormData(int permitId, [FromServices] IPermitDocumentService permitDocService)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var formData = await permitDocService.GetPermitFormDataAsync(permitId, companyId);
                return Ok(new { fieldCount = formData.Count, fields = formData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permit form data");
                return StatusCode(500, new { message = "Error retrieving form data" });
            }
        }

        /// <summary>
        /// Get generated documents for a permit
        /// </summary>
        [HttpGet("permits/{permitId}/documents")]
        public async Task<ActionResult> GetPermitDocuments(int permitId)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var documents = await _context.PermitDocuments
                    .Include(d => d.Template)
                    .Where(d => d.JobPermitId == permitId && d.CompanyId == companyId)
                    .OrderByDescending(d => d.GeneratedAt)
                    .Select(d => new
                    {
                        d.Id,
                        d.TemplateId,
                        templateName = d.Template != null ? d.Template.TemplateName : "Unknown",
                        d.StoragePath,
                        d.Status,
                        d.GeneratedAt
                    })
                    .ToListAsync();

                return Ok(new { count = documents.Count, documents });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permit documents");
                return StatusCode(500, new { message = "Error retrieving documents" });
            }
        }
    }

    /// <summary>
    /// DTO for updating a permit
    /// </summary>
    public class UpdatePermitDto
    {
        public string? Status { get; set; }
        public string? PermitNumber { get; set; }
        public string? IssuingAuthority { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public decimal? ApplicationFee { get; set; }
        public bool? FeePaid { get; set; }
        public bool? HasPlotPlan { get; set; }
        public bool? HasConstructionDrawings { get; set; }
        public bool? HasContractorLicense { get; set; }
        public bool? HasOwnerAuthorization { get; set; }
        public string? Notes { get; set; }
        public string? DenialReason { get; set; }
        public int? AssignedUserId { get; set; }
    }
}
