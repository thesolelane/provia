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
        private readonly IConfiguration _configuration;

        public AuthController(JobTrackerContext context, ILogger<AuthController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("detect-company")]
        public async Task<IActionResult> DetectCompany([FromBody] DetectCompanyRequest request)
        {
            try
            {
                // Find user by email to determine their company
                var user = await _context.Users
                    .Include(u => u.Company)
                    .Where(u => u.Email == request.Email && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user?.Company != null)
                {
                    return Ok(new
                    {
                        company = new
                        {
                            user.Company.CompanyName,
                            user.Company.AccountNumber,
                            user.Company.Description
                        }
                    });
                }

                return NotFound(new { message = "No account found with this email address" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting company for email {Email}", request.Email);
                return StatusCode(500, new { message = "Error detecting company" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Find user by email
                var user = await _context.Users
                    .Include(u => u.Company)
                    .Where(u => u.Email == request.Email && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return BadRequest(new { message = "Invalid email or password" });
                }

                // Verify password
                if (!VerifyPassword(request.Password, user.PasswordHash ?? ""))
                {
                    return BadRequest(new { message = "Invalid email or password" });
                }

                // Check if company is active
                if (!user.Company.IsActive)
                {
                    return BadRequest(new { message = "Company account is inactive" });
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate session token
                var token = GenerateSessionToken(user);

                return Ok(new
                {
                    token,
                    user = new
                    {
                        user.Id,
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        Role = user.Role.ToString(),
                        user.LanguagePreference,
                        user.IsEmailVerified,
                        user.IsPhoneVerified
                    },
                    company = new
                    {
                        user.Company.Id,
                        user.Company.CompanyName,
                        user.Company.AccountNumber,
                        user.Company.Description,
                        user.Company.SubscriptionType
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email login for {Email}", request.Email);
                return StatusCode(500, new { message = "Login error occurred" });
            }
        }

        [HttpPost("login-phone")]
        public async Task<IActionResult> LoginPhone([FromBody] PhoneLoginRequest request)
        {
            try
            {
                // Find user by phone number
                var user = await _context.Users
                    .Include(u => u.Company)
                    .Where(u => u.PhoneNumber == request.PhoneNumber && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return BadRequest(new { message = "Invalid phone number or PIN" });
                }

                // Verify PIN (stored as password hash)
                if (!VerifyPassword(request.Pin, user.PasswordHash ?? ""))
                {
                    return BadRequest(new { message = "Invalid phone number or PIN" });
                }

                // Check if company is active
                if (!user.Company.IsActive)
                {
                    return BadRequest(new { message = "Company account is inactive" });
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate session token
                var token = GenerateSessionToken(user);

                return Ok(new
                {
                    token,
                    user = new
                    {
                        user.Id,
                        user.FirstName,
                        user.LastName,
                        user.PhoneNumber,
                        Role = user.Role.ToString(),
                        user.LanguagePreference,
                        user.IsEmailVerified,
                        user.IsPhoneVerified
                    },
                    company = new
                    {
                        user.Company.Id,
                        user.Company.CompanyName,
                        user.Company.AccountNumber,
                        user.Company.Description,
                        user.Company.SubscriptionType
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during phone login for {PhoneNumber}", request.PhoneNumber);
                return StatusCode(500, new { message = "Login error occurred" });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Since we're using JWT tokens, logout is handled client-side by removing the token
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("verify-token")]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenRequest request)
        {
            try
            {
                // Simple token validation for now
                var tokenBytes = Convert.FromBase64String(request.Token);
                var tokenPayload = Encoding.UTF8.GetString(tokenBytes);
                var parts = tokenPayload.Split(':');
                
                if (parts.Length < 4)
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var userId = int.Parse(parts[0]);

                // Get current user info
                var user = await _context.Users
                    .Include(u => u.Company)
                    .Where(u => u.Id == userId && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Unauthorized(new { message = "User not found or inactive" });
                }

                return Ok(new
                {
                    valid = true,
                    user = new
                    {
                        user.Id,
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        user.PhoneNumber,
                        Role = user.Role.ToString(),
                        user.LanguagePreference,
                        user.IsEmailVerified,
                        user.IsPhoneVerified
                    },
                    company = new
                    {
                        user.Company.Id,
                        user.Company.CompanyName,
                        user.Company.AccountNumber,
                        user.Company.Description,
                        user.Company.SubscriptionType
                    }
                });
            }
            catch
            {
                return Unauthorized(new { message = "Invalid token" });
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "JobTracker_User_Salt"));
            var computedHash = Convert.ToBase64String(hashedBytes);
            return computedHash == hash;
        }

        private string GenerateSessionToken(User user)
        {
            // Simple session token for now - can be upgraded to JWT later
            var payload = $"{user.Id}:{user.CompanyId}:{user.Role}:{DateTime.UtcNow.Ticks}";
            var bytes = Encoding.UTF8.GetBytes(payload);
            return Convert.ToBase64String(bytes);
        }
    }

    public class DetectCompanyRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class PhoneLoginRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }

    public class VerifyTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}