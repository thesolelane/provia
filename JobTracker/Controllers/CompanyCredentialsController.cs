using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/company/credentials")]
    [Authorize]
    public class CompanyCredentialsController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<CompanyCredentialsController> _logger;
        private readonly IWebHostEnvironment _env;

        public CompanyCredentialsController(
            JobTrackerContext context,
            ITenantContext tenantContext,
            ILogger<CompanyCredentialsController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllCredentials()
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var credentials = await _context.CompanyCredentials
                    .Where(c => c.CompanyId == companyId)
                    .OrderBy(c => c.CredentialType)
                    .Select(c => new
                    {
                        c.Id,
                        c.CredentialType,
                        c.LicenseNumber,
                        c.HolderName,
                        c.IssueDate,
                        c.ExpirationDate,
                        c.InsuranceProvider,
                        c.PolicyNumber,
                        c.CoverageAmount,
                        c.DocumentPath,
                        c.OriginalFileName,
                        c.Status,
                        c.Notes,
                        c.UploadedAt,
                        isExpired = c.ExpirationDate.HasValue && c.ExpirationDate.Value < DateTime.UtcNow,
                        isExpiringSoon = c.ExpirationDate.HasValue && c.ExpirationDate.Value > DateTime.UtcNow && c.ExpirationDate.Value < DateTime.UtcNow.AddDays(30)
                    })
                    .ToListAsync();

                return Ok(new { count = credentials.Count, credentials });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company credentials");
                return StatusCode(500, new { message = "Error retrieving credentials" });
            }
        }

        [HttpGet("{type}")]
        public async Task<ActionResult> GetCredentialByType(string type)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var credential = await _context.CompanyCredentials
                    .Where(c => c.CompanyId == companyId && c.CredentialType == type.ToUpper())
                    .OrderByDescending(c => c.UploadedAt)
                    .FirstOrDefaultAsync();

                if (credential == null)
                    return NotFound(new { message = $"No {type} credential found" });

                return Ok(credential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential");
                return StatusCode(500, new { message = "Error retrieving credential" });
            }
        }

        [HttpPost("upload")]
        public async Task<ActionResult> UploadCredential([FromForm] CredentialUploadDto dto)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                if (user.Role != RoleCodes.Admin && user.Role != RoleCodes.MasterAdmin)
                    return Forbid("Only admins can upload company credentials");

                var companyId = _tenantContext.GetCurrentCompanyId();

                string? documentPath = null;
                string? originalFileName = null;
                long? fileSize = null;

                if (dto.Document != null && dto.Document.Length > 0)
                {
                    var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "credentials", companyId.ToString());
                    Directory.CreateDirectory(uploadsPath);

                    var timestamp = DateTime.UtcNow.Ticks;
                    var safeFileName = $"{dto.CredentialType}_{timestamp}{Path.GetExtension(dto.Document.FileName)}";
                    var filePath = Path.Combine(uploadsPath, safeFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.Document.CopyToAsync(stream);
                    }

                    documentPath = $"/uploads/credentials/{companyId}/{safeFileName}";
                    originalFileName = dto.Document.FileName;
                    fileSize = dto.Document.Length;
                }

                var credential = new CompanyCredential
                {
                    CompanyId = companyId,
                    CredentialType = dto.CredentialType.ToUpper(),
                    LicenseNumber = dto.LicenseNumber,
                    HolderName = dto.HolderName,
                    IssueDate = dto.IssueDate,
                    ExpirationDate = dto.ExpirationDate,
                    InsuranceProvider = dto.InsuranceProvider,
                    PolicyNumber = dto.PolicyNumber,
                    CoverageAmount = dto.CoverageAmount,
                    DocumentPath = documentPath,
                    OriginalFileName = originalFileName,
                    FileSize = fileSize,
                    Status = "ACTIVE",
                    Notes = dto.Notes,
                    UploadedAt = DateTime.UtcNow,
                    UploadedByUserId = user.Id,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CompanyCredentials.Add(credential);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Credential {dto.CredentialType} uploaded for company {companyId}");

                return Ok(new { message = "Credential uploaded successfully", credentialId = credential.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading credential");
                return StatusCode(500, new { message = "Error uploading credential" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCredential(int id, [FromBody] CredentialUpdateDto dto)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                if (user.Role != RoleCodes.Admin && user.Role != RoleCodes.MasterAdmin)
                    return Forbid("Only admins can update credentials");

                var companyId = _tenantContext.GetCurrentCompanyId();
                var credential = await _context.CompanyCredentials
                    .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

                if (credential == null)
                    return NotFound(new { message = "Credential not found" });

                if (!string.IsNullOrEmpty(dto.LicenseNumber))
                    credential.LicenseNumber = dto.LicenseNumber;
                if (!string.IsNullOrEmpty(dto.HolderName))
                    credential.HolderName = dto.HolderName;
                if (dto.IssueDate.HasValue)
                    credential.IssueDate = dto.IssueDate;
                if (dto.ExpirationDate.HasValue)
                    credential.ExpirationDate = dto.ExpirationDate;
                if (!string.IsNullOrEmpty(dto.InsuranceProvider))
                    credential.InsuranceProvider = dto.InsuranceProvider;
                if (!string.IsNullOrEmpty(dto.PolicyNumber))
                    credential.PolicyNumber = dto.PolicyNumber;
                if (dto.CoverageAmount.HasValue)
                    credential.CoverageAmount = dto.CoverageAmount;
                if (!string.IsNullOrEmpty(dto.Status))
                    credential.Status = dto.Status;
                if (dto.Notes != null)
                    credential.Notes = dto.Notes;

                credential.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Credential updated successfully", credential });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating credential");
                return StatusCode(500, new { message = "Error updating credential" });
            }
        }

        [HttpGet("status")]
        public async Task<ActionResult> GetCredentialStatus()
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var credentials = await _context.CompanyCredentials
                    .Where(c => c.CompanyId == companyId)
                    .ToListAsync();

                var status = CredentialTypes.All.Select(type => {
                    var cred = credentials.FirstOrDefault(c => c.CredentialType == type);
                    return new
                    {
                        type,
                        typeName = type switch
                        {
                            "CSL_LICENSE" => "Construction Supervisor License",
                            "HIC_LICENSE" => "Home Improvement Contractor License",
                            "GENERAL_LIABILITY" => "General Liability Insurance",
                            "WORKERS_COMP" => "Workers Compensation Insurance",
                            _ => type
                        },
                        hasDocument = cred != null,
                        licenseNumber = cred?.LicenseNumber,
                        expirationDate = cred?.ExpirationDate,
                        isExpired = cred?.IsExpired ?? false,
                        isExpiringSoon = cred?.IsExpiringSoon ?? false,
                        status = cred?.Status ?? "MISSING"
                    };
                });

                var expiring = credentials.Count(c => c.IsExpiringSoon);
                var expired = credentials.Count(c => c.IsExpired);
                var missing = CredentialTypes.All.Length - credentials.Select(c => c.CredentialType).Distinct().Count();

                return Ok(new
                {
                    summary = new { total = CredentialTypes.All.Length, uploaded = credentials.Count, expiring, expired, missing },
                    credentials = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credential status");
                return StatusCode(500, new { message = "Error retrieving status" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCredential(int id)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null || (user.Role != RoleCodes.Admin && user.Role != RoleCodes.MasterAdmin))
                    return Forbid("Only admins can delete credentials");

                var companyId = _tenantContext.GetCurrentCompanyId();
                var credential = await _context.CompanyCredentials
                    .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

                if (credential == null)
                    return NotFound(new { message = "Credential not found" });

                _context.CompanyCredentials.Remove(credential);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Credential deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting credential");
                return StatusCode(500, new { message = "Error deleting credential" });
            }
        }
    }

    public class CredentialUploadDto
    {
        public string CredentialType { get; set; } = string.Empty;
        public string? LicenseNumber { get; set; }
        public string? HolderName { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? PolicyNumber { get; set; }
        public decimal? CoverageAmount { get; set; }
        public string? Notes { get; set; }
        public IFormFile? Document { get; set; }
    }

    public class CredentialUpdateDto
    {
        public string? LicenseNumber { get; set; }
        public string? HolderName { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? PolicyNumber { get; set; }
        public decimal? CoverageAmount { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }
}
