using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using System.Security.Cryptography;
using System.Text;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly JobTracker.Services.IEmailService _emailService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(JobTrackerContext context, JobTracker.Services.IEmailService emailService, ILogger<UsersController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetCompanyUsers(int companyId)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.CompanyId == companyId && u.IsActive)
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.LastName)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.PhoneNumber,
                        Role = ((int)u.Role).ToString(),
                        u.LanguagePreference,
                        u.IsEmailVerified,
                        u.IsPhoneVerified,
                        u.LastLoginAt,
                        u.CreatedAt
                    })
                    .ToListAsync();

                var roleStats = await _context.Users
                    .Where(u => u.CompanyId == companyId && u.IsActive)
                    .GroupBy(u => u.Role)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToListAsync();

                return Ok(new
                {
                    users,
                    stats = new
                    {
                        totalUsers = users.Count,
                        masterAdmins = roleStats.FirstOrDefault(r => r.Role == UserRole.MasterAdmin)?.Count ?? 0,
                        admins = roleStats.FirstOrDefault(r => r.Role == UserRole.Admin)?.Count ?? 0,
                        regularUsers = roleStats.FirstOrDefault(r => r.Role == UserRole.RegularUser)?.Count ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users for company {CompanyId}", companyId);
                return StatusCode(500, new { message = "Failed to retrieve users" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Validate company exists
                var company = await _context.Companies.FindAsync(request.CompanyId);
                if (company == null || !company.IsActive)
                {
                    return BadRequest(new { message = "Invalid company" });
                }

                // Check role limits
                var currentUsers = await _context.Users
                    .Where(u => u.CompanyId == request.CompanyId && u.IsActive)
                    .ToListAsync();

                var masterAdminCount = currentUsers.Count(u => u.Role == UserRole.MasterAdmin);
                var adminCount = currentUsers.Count(u => u.Role == UserRole.Admin);

                if ((int)request.Role == 2 && masterAdminCount >= 3)  // MasterAdmin = 2
                {
                    return BadRequest(new { message = "Maximum 3 Master Admins allowed per company" });
                }

                if ((int)request.Role == 1 && adminCount >= 10)  // Admin = 1
                {
                    return BadRequest(new { message = "Maximum 10 Admins allowed per company" });
                }

                if (currentUsers.Count >= company.MaxUsers)
                {
                    return BadRequest(new { message = $"Maximum {company.MaxUsers} users allowed for your subscription" });
                }

                // Check for duplicate email/phone
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email && u.IsActive);
                    if (emailExists)
                    {
                        return BadRequest(new { message = "Email address already exists" });
                    }
                }

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.IsActive);
                    if (phoneExists)
                    {
                        return BadRequest(new { message = "Phone number already exists" });
                    }
                }

                // Generate verification code and temporary password
                var verificationCode = GenerateVerificationCode();
                var temporaryPassword = GenerateTemporaryPassword();

                // Create user
                var user = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = request.Role,
                    LanguagePreference = request.LanguagePreference ?? "en",
                    PasswordHash = HashPassword(temporaryPassword),
                    CompanyId = request.CompanyId,
                    IsActive = true,
                    IsEmailVerified = false, // Require verification for all users
                    IsPhoneVerified = false,
                    EmailVerificationCode = verificationCode,
                    EmailVerificationExpiry = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Send verification email automatically
                string emailStatus = "Email service not configured";
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        var emailSent = await _emailService.SendVerificationEmailAsync(
                            user.Email,
                            user.FirstName,
                            verificationCode,
                            temporaryPassword
                        );
                        emailStatus = emailSent ? "Verification email sent" : "Email service needs configuration";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not send verification email to {Email}", user.Email);
                        emailStatus = "Email service needs setup";
                    }
                }

                return Ok(new
                {
                    message = "User created successfully",
                    userId = user.Id,
                    loginMethod = user.Role == UserRole.RegularUser ? "phone" : "email",
                    emailStatus,
                    verificationRequired = true,
                    temporaryPassword = emailStatus.Contains("not configured") ? temporaryPassword : "Check email",
                    verificationCode = emailStatus.Contains("not configured") ? verificationCode : "Check email"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "Failed to create user" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null || !user.IsActive)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Update fields
                user.FirstName = request.FirstName ?? user.FirstName;
                user.LastName = request.LastName ?? user.LastName;
                user.Email = request.Email ?? user.Email;
                user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
                user.LanguagePreference = request.LanguagePreference ?? user.LanguagePreference;
                
                if (!string.IsNullOrEmpty(request.Password))
                {
                    user.PasswordHash = HashPassword(request.Password);
                }

                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { message = "Failed to update user" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Soft delete
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                return StatusCode(500, new { message = "Failed to deactivate user" });
            }
        }

        private static string GenerateVerificationCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return Math.Abs(BitConverter.ToInt32(bytes, 0)).ToString("D8")[..6];
        }

        private static string GenerateTemporaryPassword()
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
            using var rng = RandomNumberGenerator.Create();
            var result = new char[12];
            var bytes = new byte[4];

            for (int i = 0; i < 12; i++)
            {
                rng.GetBytes(bytes);
                result[i] = chars[Math.Abs(BitConverter.ToInt32(bytes, 0)) % chars.Length];
            }

            return new string(result);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "JobTracker_User_Salt"));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public class CreateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public string? LanguagePreference { get; set; }
        public string? Password { get; set; }
        public int CompanyId { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LanguagePreference { get; set; }
        public string? Password { get; set; }
    }
}