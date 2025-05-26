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
    public class AuthController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(JobTrackerContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                User? user = null;

                // Login with email (Admin users)
                if (!string.IsNullOrEmpty(request.Email))
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
                    
                    if (user == null || user.Role == UserRole.RegularUser)
                    {
                        return Unauthorized(new { message = "Invalid email credentials or insufficient permissions" });
                    }
                }
                // Login with phone number (Regular users)
                else if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.IsActive);
                    
                    if (user == null)
                    {
                        return Unauthorized(new { message = "Invalid phone number credentials" });
                    }
                }
                else
                {
                    return BadRequest(new { message = "Email or phone number is required" });
                }

                // Verify password (simplified - in production use proper hashing)
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Invalid password" });
                }

                // Update language preference if provided
                if (!string.IsNullOrEmpty(request.LanguagePreference))
                {
                    user.LanguagePreference = request.LanguagePreference;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                var response = new
                {
                    userId = user.Id,
                    name = user.GetDisplayName(),
                    role = user.Role.ToString(),
                    roleDisplay = user.GetRoleDisplayName(),
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    language = user.LanguagePreference,
                    permissions = GetUserPermissions(user.Role)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return StatusCode(500, new { message = "Login failed" });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Validate that either email or phone is provided
                if (string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.PhoneNumber))
                {
                    return BadRequest(new { message = "Either email or phone number is required" });
                }

                // Admin users must have email, regular users must have phone
                if (request.RequestedRole != UserRole.RegularUser && string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { message = "Admin users must provide an email address" });
                }

                if (request.RequestedRole == UserRole.RegularUser && string.IsNullOrEmpty(request.PhoneNumber))
                {
                    return BadRequest(new { message = "Regular users must provide a phone number" });
                }

                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => 
                        (u.Email == request.Email && !string.IsNullOrEmpty(request.Email)) ||
                        (u.PhoneNumber == request.PhoneNumber && !string.IsNullOrEmpty(request.PhoneNumber)));

                if (existingUser != null)
                {
                    return BadRequest(new { message = "User already exists with this email or phone number" });
                }

                // Check role limits
                var roleValidation = await ValidateRoleLimits(request.RequestedRole);
                if (!roleValidation.IsValid)
                {
                    return BadRequest(new { message = roleValidation.Message });
                }

                var user = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = request.RequestedRole,
                    LanguagePreference = request.LanguagePreference,
                    PasswordHash = HashPassword(request.Password),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "User registered successfully",
                    userId = user.Id,
                    role = user.GetRoleDisplayName()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error");
                return StatusCode(500, new { message = "Registration failed" });
            }
        }

        [HttpGet("role-limits")]
        public async Task<IActionResult> GetRoleLimits()
        {
            try
            {
                var masterAdminCount = await _context.Users.CountAsync(u => u.Role == UserRole.MasterAdmin && u.IsActive);
                var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive);

                return Ok(new
                {
                    masterAdmins = new { current = masterAdminCount, max = 3 },
                    admins = new { current = adminCount, max = 10 },
                    regularUsers = new { current = -1, max = -1 } // No limit
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role limits");
                return StatusCode(500, new { message = "Failed to get role limits" });
            }
        }

        private async Task<(bool IsValid, string Message)> ValidateRoleLimits(UserRole requestedRole)
        {
            switch (requestedRole)
            {
                case UserRole.MasterAdmin:
                    var masterAdminCount = await _context.Users.CountAsync(u => u.Role == UserRole.MasterAdmin && u.IsActive);
                    if (masterAdminCount >= 3)
                    {
                        return (false, "Maximum of 3 Master Admins allowed");
                    }
                    break;

                case UserRole.Admin:
                    var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive);
                    if (adminCount >= 10)
                    {
                        return (false, "Maximum of 10 Admins allowed (excluding Master Admins)");
                    }
                    break;

                case UserRole.RegularUser:
                    // No limit for regular users
                    break;
            }

            return (true, string.Empty);
        }

        private static string[] GetUserPermissions(UserRole role)
        {
            return role switch
            {
                UserRole.MasterAdmin => new[] { "manage_users", "create_jobs", "manage_sections", "view_all", "system_admin" },
                UserRole.Admin => new[] { "create_jobs", "manage_sections", "view_all" },
                UserRole.RegularUser => new[] { "view_assigned", "update_progress" },
                _ => Array.Empty<string>()
            };
        }

        private static string HashPassword(string password)
        {
            // Simplified hashing - in production use BCrypt or similar
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "JobTracker_Salt"));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string? hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;
            return HashPassword(password) == hash;
        }
    }
}