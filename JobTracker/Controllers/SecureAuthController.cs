using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/secureauth")]
    public class SecureAuthController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<SecureAuthController> _logger;

        public SecureAuthController(
            JobTrackerContext context, 
            IAuthenticationService authService,
            ILogger<SecureAuthController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SecureLoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Identifier) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email/username and password are required" });
                }

                var result = await _authService.AuthenticateAsync(request.Identifier, request.Password);

                if (!result.Success)
                {
                    return Unauthorized(new { message = result.ErrorMessage });
                }

                return Ok(new
                {
                    token = result.Token,
                    user = new
                    {
                        id = result.User!.Id,
                        email = result.User.Email,
                        firstName = result.User.FirstName,
                        lastName = result.User.LastName,
                        role = result.User.Role,
                        companyId = result.User.CompanyId,
                        companyName = result.User.Company?.CompanyName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for identifier {request.Identifier}");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("phone-login")]
        public async Task<IActionResult> PhoneLogin([FromBody] SecurePhoneLoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Pin))
                {
                    return BadRequest(new { message = "Phone number and PIN are required" });
                }

                var result = await _authService.AuthenticatePhoneAsync(request.PhoneNumber, request.Pin);

                if (!result.Success)
                {
                    return Unauthorized(new { message = result.ErrorMessage });
                }

                return Ok(new
                {
                    token = result.Token,
                    user = new
                    {
                        id = result.User!.Id,
                        phoneNumber = result.User.PhoneNumber,
                        firstName = result.User.FirstName,
                        lastName = result.User.LastName,
                        role = result.User.Role,
                        companyId = result.User.CompanyId,
                        companyName = result.User.Company?.CompanyName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during phone login for phone {request.PhoneNumber}");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("validate-token")]
        public async Task<IActionResult> ValidateToken([FromBody] SecureTokenValidationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new { message = "Token is required" });
                }

                var user = await _authService.GetUserByTokenAsync(request.Token);

                if (user == null)
                {
                    return Ok(new { valid = false, message = "Invalid token" });
                }

                return Ok(new
                {
                    valid = true,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        role = user.Role,
                        companyId = user.CompanyId,
                        companyName = user.Company?.CompanyName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token validation");
                return Ok(new { valid = false, message = "Token validation error" });
            }
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var user = await _authService.GetUserByTokenAsync(token);

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                return Ok(new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = user.Role,
                    companyId = user.CompanyId,
                    companyName = user.Company?.CompanyName,
                    isActive = user.IsActive,
                    lastLogin = user.LastLogin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "An error occurred while getting user information" });
            }
        }

        [HttpPost("create-user")]
        [Authorize]
        public async Task<IActionResult> CreateUser([FromBody] SecureCreateUserRequest request)
        {
            try
            {
                // Check if current user has admin permissions
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var currentUser = await _authService.GetUserByTokenAsync(token);

                if (currentUser == null || currentUser.Role != UserRoles.Admin)
                {
                    return Forbid("Only administrators can create users");
                }

                // Validate request
                if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
                {
                    return BadRequest(new { message = "First name and last name are required" });
                }

                // Check for duplicate email or phone
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                    if (existingUser != null)
                    {
                        return BadRequest(new { message = "Email already exists" });
                    }
                }

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
                    if (existingUser != null)
                    {
                        return BadRequest(new { message = "Phone number already exists" });
                    }
                }

                // Create new user
                var newUser = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = request.Role,
                    CompanyId = currentUser.CompanyId, // Same company as admin
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = currentUser.Id,
                    LanguagePreference = request.LanguagePreference ?? "en"
                };

                // Set password or PIN hash
                if (!string.IsNullOrEmpty(request.Password))
                {
                    newUser.PasswordHash = AuthenticationService.HashPassword(request.Password);
                }

                if (!string.IsNullOrEmpty(request.Pin))
                {
                    newUser.PinHash = AuthenticationService.HashPin(request.Pin);
                }

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "User created successfully",
                    user = new
                    {
                        id = newUser.Id,
                        firstName = newUser.FirstName,
                        lastName = newUser.LastName,
                        email = newUser.Email,
                        phoneNumber = newUser.PhoneNumber,
                        role = newUser.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "An error occurred while creating the user" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // Token-based logout - client should discard the token
            return Ok(new { message = "Logged out successfully" });
        }
    }

    public class SecureLoginRequest
    {
        public string Identifier { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SecurePhoneLoginRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }

    public class SecureTokenValidationRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class SecureCreateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public string? Pin { get; set; }
        public int Role { get; set; }
        public string? LanguagePreference { get; set; }
    }
}