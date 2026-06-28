using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JobTrackerApp.Services;
using JobTrackerApp.Data;
using JobTrackerApp.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTrackerApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly ActiveDirectoryService _adService;
        private readonly ApplicationDbContext _context;

        public AuthController(
            ILogger<AuthController> logger,
            ActiveDirectoryService adService,
            ApplicationDbContext context)
        {
            _logger = logger;
            _adService = adService;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest("Username and password are required");
                }

                // Validate credentials against Active Directory
                bool isValid = _adService.ValidateCredentials(request.Username, request.Password);
                if (!isValid)
                {
                    _logger.LogWarning("Failed login attempt for user {Username}", request.Username);
                    return Unauthorized("Invalid credentials");
                }

                // Get user details from AD
                var adUser = _adService.GetUserDetails(request.Username);
                if (adUser == null)
                {
                    _logger.LogWarning("User {Username} authenticated but details could not be retrieved", request.Username);
                    return Unauthorized("User details could not be retrieved");
                }

                // Check if user exists in our database
                var dbUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == adUser.Username || u.Email == adUser.Email);

                if (dbUser == null)
                {
                    // Create new user in our database
                    dbUser = new User
                    {
                        Username = adUser.Username,
                        Email = adUser.Email,
                        ActiveDirectoryId = adUser.ActiveDirectoryId,
                        Role = adUser.Role,
                        IsActive = true,
                        LastLogin = DateTime.UtcNow,
                        CreatedBy = "System",
                        UpdatedBy = "System"
                    };

                    _context.Users.Add(dbUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created new user {Username} from Active Directory", dbUser.Username);
                }
                else
                {
                    // Update existing user
                    dbUser.ActiveDirectoryId = adUser.ActiveDirectoryId;
                    dbUser.Role = adUser.Role;
                    dbUser.LastLogin = DateTime.UtcNow;
                    dbUser.UpdatedAt = DateTime.UtcNow;
                    dbUser.UpdatedBy = "System";

                    _context.Users.Update(dbUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated user {Username} from Active Directory", dbUser.Username);
                }

                // Generate JWT token
                var token = _adService.GenerateJwtToken(dbUser);

                // Return user info and token
                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        Id = dbUser.Id,
                        Username = dbUser.Username,
                        Email = dbUser.Email,
                        Role = dbUser.Role,
                        EmployeeId = dbUser.EmployeeId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, "An error occurred during login");
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Get user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid token");
                }

                // Get user from database
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Return user info
                return Ok(new
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    EmployeeId = user.EmployeeId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user");
                return StatusCode(500, "An error occurred retrieving user information");
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Client should delete the token
            return Ok(new { message = "Logged out successfully" });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
