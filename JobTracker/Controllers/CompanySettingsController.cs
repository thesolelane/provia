using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Services;
using System.ComponentModel.DataAnnotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/company/settings")]
    [Authorize]
    public class CompanySettingsController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<CompanySettingsController> _logger;
        private readonly IWebHostEnvironment _env;

        public CompanySettingsController(
            JobTrackerContext context,
            ITenantContext tenantContext,
            ILogger<CompanySettingsController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<CompanySettingsResponse>> GetCompanySettings()
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var company = await _context.Companies.FindAsync(companyId);

                if (company == null)
                    return NotFound(new { message = "Company not found" });

                return Ok(new CompanySettingsResponse
                {
                    CompanyId = company.Id,
                    CompanyName = company.CompanyName,
                    Description = company.Description,
                    ContactEmail = company.ContactEmail,
                    ContactPhone = company.ContactPhone,
                    Address = company.Address,
                    City = company.City,
                    State = company.State,
                    ZipCode = company.ZipCode,
                    LogoUrl = company.LogoUrl,
                    PrimaryColor = company.PrimaryColor,
                    SecondaryColor = company.SecondaryColor,
                    AccentColor = company.AccentColor,
                    Tagline = company.Tagline,
                    WebsiteUrl = company.WebsiteUrl,
                    SubscriptionType = company.SubscriptionType.ToString(),
                    MaxUsers = company.MaxUsers,
                    MaxJobs = company.MaxJobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company settings");
                return StatusCode(500, new { message = "Error fetching settings" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCompanySettings([FromBody] UpdateCompanySettingsRequest request)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail && u.CompanyId == companyId);
                if (user == null || user.Role != 1510) // Admin only
                    return Forbid();

                // Cross-tenant security check
                if (user.CompanyId != companyId)
                {
                    _logger.LogWarning($"Cross-tenant access attempt: User {userEmail} tried to update company {companyId}");
                    return Forbid();
                }

                // Validate color formats
                var colorPattern = new System.Text.RegularExpressions.Regex(@"^#[0-9A-Fa-f]{6}$");
                if (!string.IsNullOrWhiteSpace(request.PrimaryColor) && !colorPattern.IsMatch(request.PrimaryColor))
                    return BadRequest(new { message = "Primary color must be a valid hex color (e.g., #FF9500)" });
                if (!string.IsNullOrWhiteSpace(request.SecondaryColor) && !colorPattern.IsMatch(request.SecondaryColor))
                    return BadRequest(new { message = "Secondary color must be a valid hex color (e.g., #2F5A7E)" });
                if (!string.IsNullOrWhiteSpace(request.AccentColor) && !colorPattern.IsMatch(request.AccentColor))
                    return BadRequest(new { message = "Accent color must be a valid hex color (e.g., #764ba2)" });

                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                    return NotFound(new { message = "Company not found" });

                if (!string.IsNullOrWhiteSpace(request.CompanyName))
                    company.CompanyName = request.CompanyName;
                
                if (request.Description != null)
                    company.Description = request.Description;
                
                if (!string.IsNullOrWhiteSpace(request.ContactEmail))
                    company.ContactEmail = request.ContactEmail;
                
                if (request.ContactPhone != null)
                    company.ContactPhone = request.ContactPhone;
                
                if (request.Address != null)
                    company.Address = request.Address;
                
                if (request.City != null)
                    company.City = request.City;
                
                if (request.State != null)
                    company.State = request.State;
                
                if (request.ZipCode != null)
                    company.ZipCode = request.ZipCode;

                if (!string.IsNullOrWhiteSpace(request.PrimaryColor))
                    company.PrimaryColor = request.PrimaryColor;
                
                if (!string.IsNullOrWhiteSpace(request.SecondaryColor))
                    company.SecondaryColor = request.SecondaryColor;
                
                if (request.AccentColor != null)
                    company.AccentColor = request.AccentColor;
                
                if (request.Tagline != null)
                    company.Tagline = request.Tagline;
                
                if (request.WebsiteUrl != null)
                    company.WebsiteUrl = request.WebsiteUrl;

                company.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Company settings updated for Company {companyId} by {userEmail}");

                return Ok(new { message = "Settings updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company settings");
                return StatusCode(500, new { message = "Error updating settings" });
            }
        }

        // Logo standard size: 300x100 pixels (3:1 aspect ratio) - industry standard for enterprise headers
        private const int LogoMaxWidth = 300;
        private const int LogoMaxHeight = 100;

        [HttpPost("logo")]
        public async Task<IActionResult> UploadLogo(IFormFile file)
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail && u.CompanyId == companyId);
                if (user == null || user.Role != 1510)
                    return Forbid();

                // Cross-tenant security check
                if (user.CompanyId != companyId)
                {
                    _logger.LogWarning($"Cross-tenant logo upload attempt: User {userEmail} tried to update company {companyId}");
                    return Forbid();
                }

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file uploaded" });

                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/svg+xml" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                    return BadRequest(new { message = "Invalid file type. Allowed: JPEG, PNG, GIF, SVG" });

                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(new { message = "File too large. Maximum 5MB" });

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "logos");
                Directory.CreateDirectory(uploadsDir);

                // Always save as PNG for consistency
                var fileName = $"company_{companyId}_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Handle SVG separately (can't be resized)
                if (file.ContentType.ToLower() == "image/svg+xml")
                {
                    var svgFileName = $"company_{companyId}_{DateTime.UtcNow:yyyyMMddHHmmss}.svg";
                    var svgFilePath = Path.Combine(uploadsDir, svgFileName);
                    
                    using (var stream = new FileStream(svgFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    
                    var company2 = await _context.Companies.FindAsync(companyId);
                    if (company2 == null) return NotFound();
                    
                    // Delete old logo
                    if (!string.IsNullOrEmpty(company2.LogoUrl))
                    {
                        var oldPath = Path.Combine(_env.WebRootPath, company2.LogoUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }
                    
                    company2.LogoUrl = $"/uploads/logos/{svgFileName}";
                    company2.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { 
                        message = "SVG logo uploaded successfully",
                        logoUrl = company2.LogoUrl,
                        note = "SVG files maintain their original dimensions"
                    });
                }

                // Process raster images (JPEG, PNG, GIF) - resize to standard dimensions
                using (var inputStream = file.OpenReadStream())
                using (var image = await Image.LoadAsync(inputStream))
                {
                    // Calculate new dimensions maintaining aspect ratio within max bounds
                    var ratioX = (double)LogoMaxWidth / image.Width;
                    var ratioY = (double)LogoMaxHeight / image.Height;
                    var ratio = Math.Min(ratioX, ratioY);
                    
                    var newWidth = (int)(image.Width * ratio);
                    var newHeight = (int)(image.Height * ratio);
                    
                    // Resize the image
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                    
                    // Save as PNG
                    await image.SaveAsPngAsync(filePath);
                }

                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(company.LogoUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, company.LogoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                company.LogoUrl = $"/uploads/logos/{fileName}";
                company.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Logo uploaded and resized to {LogoMaxWidth}x{LogoMaxHeight} for Company {companyId} by {userEmail}");

                return Ok(new { 
                    message = "Logo uploaded and standardized successfully",
                    logoUrl = company.LogoUrl,
                    dimensions = $"Max {LogoMaxWidth}x{LogoMaxHeight}px (3:1 ratio)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading logo");
                return StatusCode(500, new { message = "Error uploading logo" });
            }
        }

        [HttpDelete("logo")]
        public async Task<IActionResult> DeleteLogo()
        {
            try
            {
                var companyId = _tenantContext.GetCurrentCompanyId();
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail && u.CompanyId == companyId);
                if (user == null || user.Role != 1510)
                    return Forbid();

                // Cross-tenant security check
                if (user.CompanyId != companyId)
                {
                    _logger.LogWarning($"Cross-tenant logo delete attempt: User {userEmail} tried to update company {companyId}");
                    return Forbid();
                }

                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(company.LogoUrl))
                {
                    var filePath = Path.Combine(_env.WebRootPath, company.LogoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    company.LogoUrl = null;
                    company.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Logo removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting logo");
                return StatusCode(500, new { message = "Error deleting logo" });
            }
        }
    }

    public class CompanySettingsResponse
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? LogoUrl { get; set; }
        public string PrimaryColor { get; set; } = "#FF9500";
        public string SecondaryColor { get; set; } = "#2F5A7E";
        public string? AccentColor { get; set; }
        public string? Tagline { get; set; }
        public string? WebsiteUrl { get; set; }
        public string SubscriptionType { get; set; } = "Trial";
        public int MaxUsers { get; set; }
        public int MaxJobs { get; set; }
    }

    public class UpdateCompanySettingsRequest
    {
        [StringLength(200)]
        public string? CompanyName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [EmailAddress]
        [StringLength(200)]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(10)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        [StringLength(7)]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Invalid color format. Use hex like #FF9500")]
        public string? PrimaryColor { get; set; }

        [StringLength(7)]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Invalid color format")]
        public string? SecondaryColor { get; set; }

        [StringLength(7)]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Invalid color format")]
        public string? AccentColor { get; set; }

        [StringLength(200)]
        public string? Tagline { get; set; }

        [StringLength(500)]
        [Url]
        public string? WebsiteUrl { get; set; }
    }
}
