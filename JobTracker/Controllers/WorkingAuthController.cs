using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BCrypt.Net;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkingAuthController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<WorkingAuthController> _logger;
        private readonly JobTracker.Services.ITenantContext _tenantContext;

        public WorkingAuthController(JobTrackerContext context, ILogger<WorkingAuthController> logger, JobTracker.Services.ITenantContext tenantContext)
        {
            _context = context;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Identifier) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email/username and password are required" });
                }

                // Retry logic for database connection issues
                User user = null;
                int retries = 3;
                while (retries > 0)
                {
                    try
                    {
                        // Find user by email or username
                        user = await _context.Users
                            .Include(u => u.Company)
                            .FirstOrDefaultAsync(u => 
                                u.Email == request.Identifier || 
                                u.Username == request.Identifier);
                        break;
                    }
                    catch (Exception dbEx) when (retries > 1)
                    {
                        _logger.LogWarning($"Database connection retry {4 - retries}/3: {dbEx.Message}");
                        retries--;
                        await Task.Delay(1000); // Wait 1 second before retry
                        
                        // Ensure fresh context
                        await _context.Database.EnsureCreatedAsync();
                    }
                }

                if (user == null)
                {
                    _logger.LogWarning($"Login attempt failed: User not found for identifier {request.Identifier}");
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning($"Login attempt failed: User {user.Id} is inactive");
                    return Unauthorized(new { message = "Account is inactive" });
                }

                // SECURITY: Use BCrypt for password verification
                if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Login attempt failed: Invalid password for user {user.Id}");
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                // Update last login using correct property
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // SECURITY: Generate JWT token with 1-hour expiration using environment variable
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "PROVIA-Production-SecureKey-MinimumLength-32Chars";
                var key = Encoding.ASCII.GetBytes(jwtSecret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("id", user.Id.ToString()),
                        new Claim("email", user.Email ?? ""),
                        new Claim("role", user.Role.ToString()),
                        new Claim("companyId", user.CompanyId.ToString())
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                var token = tokenHandler.WriteToken(securityToken);

                _logger.LogInformation($"User {user.Id} logged in successfully");

                return Ok(new
                {
                    token = token,
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
                _logger.LogError(ex, $"Error during login for identifier {request.Identifier}");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpGet("company-users/{companyId}")]
        public async Task<IActionResult> GetCompanyUsers(int companyId)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.CompanyId == companyId && u.IsActive)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.PhoneNumber,
                        u.Role,
                        u.IsActive,
                        u.UserCode
                    })
                    .OrderBy(u => u.FirstName)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company users");
                return StatusCode(500, new { message = "Error fetching users" });
            }
        }

        [HttpPost("phone-login")]
        public async Task<IActionResult> PhoneLogin([FromBody] PhoneLoginRequestModel request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Pin))
                {
                    return BadRequest(new { message = "Phone number and PIN are required" });
                }

                // Find user by phone number
                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

                if (user == null)
                {
                    _logger.LogWarning($"Phone login attempt failed: User not found for phone {request.PhoneNumber}");
                    return Unauthorized(new { message = "Invalid phone number or PIN" });
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning($"Phone login attempt failed: User {user.Id} is inactive");
                    return Unauthorized(new { message = "Account is inactive" });
                }

                // Simple PIN check for demo
                if (user.PinHash != request.Pin)
                {
                    _logger.LogWarning($"Phone login attempt failed: Invalid PIN for user {user.Id}");
                    return Unauthorized(new { message = "Invalid phone number or PIN" });
                }

                // Update last login using correct property
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate simple token
                var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user.Id}:{DateTime.UtcNow.Ticks}:{user.Role}"));

                _logger.LogInformation($"User {user.Id} logged in successfully via phone");

                return Ok(new
                {
                    token = token,
                    user = new
                    {
                        id = user.Id,
                        phoneNumber = user.PhoneNumber,
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
                _logger.LogError(ex, $"Error during phone login for phone {request.PhoneNumber}");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("validate-token")]
        public async Task<IActionResult> ValidateToken([FromBody] TokenValidationRequestModel request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return Ok(new { valid = false, message = "Token is required" });
                }

                try
                {
                    var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(request.Token));
                    var parts = tokenData.Split(':');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int userId))
                    {
                        var user = await _context.Users
                            .Include(u => u.Company)
                            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                        if (user != null)
                        {
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
                    }
                }
                catch
                {
                    // Token parsing failed
                }

                return Ok(new { valid = false, message = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token validation");
                return Ok(new { valid = false, message = "Token validation error" });
            }
        }

        [HttpGet("user")]
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
                
                try
                {
                    var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                    var parts = tokenData.Split(':');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int userId))
                    {
                        var user = await _context.Users
                            .Include(u => u.Company)
                            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                        if (user != null)
                        {
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
                                lastLogin = user.LastLoginAt
                            });
                        }
                    }
                }
                catch
                {
                    // Token parsing failed
                }

                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "An error occurred while getting user information" });
            }
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestModel request)
        {
            try
            {
                // Get current user from token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // Validate current user is admin
                try
                {
                    var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                    var parts = tokenData.Split(':');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out int userId) && int.TryParse(parts[2], out int userRole))
                    {
                        if (userRole != UserRoles.Admin) // 1510 is Admin
                        {
                            return Forbid("Only administrators can create users");
                        }

                        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                        if (currentUser == null)
                        {
                            return Unauthorized(new { message = "Invalid token" });
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
                            CompanyId = currentUser.CompanyId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = currentUser.Id,
                            LanguagePreference = request.LanguagePreference ?? "en"
                        };

                        // SECURITY: Hash password using BCrypt
                        if (!string.IsNullOrEmpty(request.Password))
                        {
                            newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                        }

                        if (!string.IsNullOrEmpty(request.Pin))
                        {
                            newUser.PinHash = request.Pin;
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
                }
                catch
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "An error occurred while creating the user" });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("send-verification-sms")]
        public async Task<IActionResult> SendVerificationSms([FromBody] SendPhoneVerificationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PhoneNumber))
                {
                    return BadRequest(new { message = "Phone number is required" });
                }

                // Generate verification code
                var verificationCode = new Random().Next(100000, 999999).ToString();

                // Try to send SMS via Twilio
                var messagingService = HttpContext.RequestServices.GetService<JobTracker.Services.IMessagingService>();

                if (messagingService != null)
                {
                    try
                    {
                        var success = await messagingService.SendVerificationSmsAsync(request.PhoneNumber, verificationCode);
                        
                        if (success)
                        {
                            return Ok(new { 
                                message = $"Verification SMS sent to {request.PhoneNumber}",
                                details = "Message sent via Twilio",
                                success = true
                            });
                        }
                        else
                        {
                            return Ok(new { 
                                message = "SMS service not configured properly",
                                verificationCode = verificationCode,
                                details = "Check Twilio credentials",
                                success = false
                            });
                        }
                    }
                    catch (Exception smsEx)
                    {
                        _logger.LogError(smsEx, "Error sending verification SMS");
                        return Ok(new { 
                            message = "SMS sending failed",
                            verificationCode = verificationCode,
                            details = smsEx.Message,
                            success = false
                        });
                    }
                }
                else
                {
                    return Ok(new { 
                        message = "SMS service not available",
                        verificationCode = verificationCode,
                        details = "Messaging service not initialized",
                        success = false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in phone verification process");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    details = ex.Message,
                    success = false
                });
            }
        }

        [HttpGet("/api/auth/users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // Validate current user is admin
                try
                {
                    var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                    var parts = tokenData.Split(':');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out int userId) && int.TryParse(parts[2], out int userRole))
                    {
                        if (userRole != 1510) // Only Admin can view all users
                        {
                            return Forbid("Only administrators can view all users");
                        }

                        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                        if (currentUser == null)
                        {
                            return Unauthorized(new { message = "Invalid token" });
                        }

                        // Get all users for current company only - TENANT ISOLATION
                        var users = await _context.Users
                            .Where(u => u.IsActive && u.CompanyId == currentUser.CompanyId)
                            .OrderBy(u => u.Role)
                            .ThenBy(u => u.FirstName)
                            .Select(u => new
                            {
                                id = u.Id,
                                firstName = u.FirstName,
                                lastName = u.LastName,
                                email = u.Email,
                                role = u.Role,
                                phoneNumber = u.PhoneNumber,
                                isActive = u.IsActive,
                                createdAt = u.CreatedAt,
                                lastLoginAt = u.LastLoginAt
                            })
                            .ToListAsync();

                        return Ok(users);
                    }
                }
                catch
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return StatusCode(500, new { message = "An error occurred while fetching users" });
            }
        }

        [HttpGet("/api/users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // Validate current user is admin
                try
                {
                    var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                    var parts = tokenData.Split(':');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out int userId) && int.TryParse(parts[2], out int userRole))
                    {
                        if (userRole != 1510) // Only Admin can view user details
                        {
                            return Forbid("Only administrators can view user details");
                        }

                        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                        if (currentUser == null)
                        {
                            return Unauthorized(new { message = "Invalid token" });
                        }

                        // Find requested user
                        var requestedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                        if (requestedUser == null)
                        {
                            return NotFound(new { message = "User not found" });
                        }

                        return Ok(new
                        {
                            id = requestedUser.Id,
                            firstName = requestedUser.FirstName,
                            lastName = requestedUser.LastName,
                            email = requestedUser.Email,
                            phoneNumber = requestedUser.PhoneNumber,
                            username = requestedUser.Username,
                            role = requestedUser.Role,
                            isActive = requestedUser.IsActive,
                            createdAt = requestedUser.CreatedAt,
                            lastLoginAt = requestedUser.LastLoginAt
                        });
                    }
                }
                catch
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user");
                return StatusCode(500, new { message = "An error occurred while fetching the user" });
            }
        }

        [HttpDelete("/api/users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // Validate current user is admin
                try
                {
                    var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                    var parts = tokenData.Split(':');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out int userId) && int.TryParse(parts[2], out int userRole))
                    {
                        if (userRole != 1510) // Only Admin can delete users
                        {
                            return Forbid("Only administrators can delete users");
                        }

                        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                        if (currentUser == null)
                        {
                            return Unauthorized(new { message = "Invalid token" });
                        }

                        // Find user to delete
                        var userToDelete = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                        if (userToDelete == null)
                        {
                            return NotFound(new { message = "User not found" });
                        }

                        // Don't allow deleting yourself
                        if (userToDelete.Id == currentUser.Id)
                        {
                            return BadRequest(new { message = "Cannot delete your own account" });
                        }

                        _context.Users.Remove(userToDelete);
                        await _context.SaveChangesAsync();

                        return Ok(new { message = "User deleted successfully" });
                    }
                }
                catch
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, new { message = "An error occurred while deleting the user" });
            }
        }

        [HttpPost("/api/users/{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // Validate current user is admin
                try
                {
                    var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                    var parts = tokenData.Split(':');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out int userId) && int.TryParse(parts[2], out int userRole))
                    {
                        if (userRole != 1510) // Only Admin can deactivate users
                        {
                            return Forbid("Only administrators can deactivate users");
                        }

                        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                        if (currentUser == null)
                        {
                            return Unauthorized(new { message = "Invalid token" });
                        }

                        // Find user to deactivate
                        var userToDeactivate = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                        if (userToDeactivate == null)
                        {
                            return NotFound(new { message = "User not found" });
                        }

                        // Don't allow deactivating yourself
                        if (userToDeactivate.Id == currentUser.Id)
                        {
                            return BadRequest(new { message = "Cannot deactivate your own account" });
                        }

                        userToDeactivate.IsActive = false;
                        userToDeactivate.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        return Ok(new { message = "User deactivated successfully" });
                    }
                }
                catch
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user");
                return StatusCode(500, new { message = "An error occurred while deactivating the user" });
            }
        }

        [HttpPut("/api/users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // Validate current user is admin
                try
                {
                    var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                    var parts = tokenData.Split(':');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out int userId) && int.TryParse(parts[2], out int userRole))
                    {
                        if (userRole != 1510) // Only Admin can update users
                        {
                            return Forbid("Only administrators can update users");
                        }

                        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                        if (currentUser == null)
                        {
                            return Unauthorized(new { message = "Invalid token" });
                        }

                        // Find user to update
                        var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                        if (userToUpdate == null)
                        {
                            return NotFound(new { message = "User not found" });
                        }

                        // Update user properties
                        userToUpdate.FirstName = request.FirstName?.Trim();
                        userToUpdate.LastName = request.LastName?.Trim();
                        userToUpdate.Email = request.Email?.Trim();
                        userToUpdate.PhoneNumber = request.PhoneNumber?.Trim();
                        userToUpdate.Username = request.Username?.Trim();
                        userToUpdate.Role = request.Role;
                        userToUpdate.UpdatedAt = DateTime.UtcNow;

                        // Validate email uniqueness if changed
                        if (!string.IsNullOrEmpty(request.Email))
                        {
                            var existingUser = await _context.Users
                                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id);
                            if (existingUser != null)
                            {
                                return BadRequest(new { message = "Email address is already in use" });
                            }
                        }

                        // Validate phone number uniqueness if changed
                        if (!string.IsNullOrEmpty(request.PhoneNumber))
                        {
                            var existingUser = await _context.Users
                                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != id);
                            if (existingUser != null)
                            {
                                return BadRequest(new { message = "Phone number is already in use" });
                            }
                        }

                        // Validate username uniqueness if changed
                        if (!string.IsNullOrEmpty(request.Username))
                        {
                            var existingUser = await _context.Users
                                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Id != id);
                            if (existingUser != null)
                            {
                                return BadRequest(new { message = "Username is already in use" });
                            }
                        }

                        await _context.SaveChangesAsync();

                        return Ok(new { message = "User updated successfully" });
                    }
                }
                catch
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, new { message = "An error occurred while updating the user" });
            }
        }

        [HttpPost("users/{id}/verify-phone")]
        public async Task<IActionResult> VerifyPhoneNumber(int id, [FromBody] VerifyPhoneRequest request)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user session" });
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (currentUser == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            if (currentUser.Role != 1510)
            {
                return Forbid("Only admins can verify phone numbers");
            }

            try
            {
                var userToVerify = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
                if (userToVerify == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Generate verification code
                var verificationCode = new Random().Next(100000, 999999).ToString();
                userToVerify.PhoneVerificationCode = verificationCode;
                userToVerify.PhoneVerificationExpiry = DateTime.UtcNow.AddMinutes(10);

                await _context.SaveChangesAsync();

                // Try to send SMS via Twilio
                var messagingService = HttpContext.RequestServices.GetService<JobTracker.Services.IMessagingService>();

                if (messagingService != null)
                {
                    try
                    {
                        var employeeName = $"{userToVerify.FirstName} {userToVerify.LastName}";
                        var success = await messagingService.SendVerificationSmsAsync(request.PhoneNumber, verificationCode);
                        
                        if (success)
                        {
                            return Ok(new { 
                                message = $"Verification SMS sent to {request.PhoneNumber}",
                                details = "Message sent via Twilio"
                            });
                        }
                        else
                        {
                            return Ok(new { 
                                message = $"SMS service not configured - verification code generated",
                                verificationCode = verificationCode,
                                details = "Check Twilio credentials"
                            });
                        }
                    }
                    catch (Exception smsEx)
                    {
                        _logger.LogError(smsEx, "Error sending verification SMS");
                        return Ok(new { 
                            message = $"Verification code generated but SMS failed",
                            verificationCode = verificationCode,
                            details = "Check Twilio configuration"
                        });
                    }
                }
                else
                {
                    var message = $"PROVIA verification code: {verificationCode}. Expires in 10 minutes.";
                    return Ok(new { 
                        message = $"Test SMS ready for {request.PhoneNumber}",
                        verificationCode = verificationCode,
                        testMessage = message,
                        details = "SMS service not available"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in phone verification process");
                return StatusCode(500, new { message = "Error processing verification request", error = ex.Message });
            }
        }

        [HttpPost("users/{id}/verify-email")]
        public async Task<IActionResult> VerifyEmail(int id, [FromBody] VerifyEmailRequest request)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user session" });
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (currentUser == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            if (currentUser.Role != 1510)
            {
                return Forbid("Only admins can verify emails");
            }

            try
            {
                var userToVerify = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
                if (userToVerify == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Generate verification code
                var verificationCode = new Random().Next(100000, 999999).ToString();
                userToVerify.EmailVerificationCode = verificationCode;
                userToVerify.EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(15);

                await _context.SaveChangesAsync();

                // Try to get email service
                var emailService = HttpContext.RequestServices.GetService<JobTracker.Services.IEmailService>();

                if (emailService != null)
                {
                    try
                    {
                        var subject = "PROVIA Email Verification";
                        var employeeName = $"{userToVerify.FirstName} {userToVerify.LastName}";
                        var emailBody = $@"
                            <html>
                            <body>
                                <h2>PROVIA Email Verification</h2>
                                <p>Hi {employeeName},</p>
                                <p>Your email verification code is: <strong>{verificationCode}</strong></p>
                                <p>This code expires in 15 minutes.</p>
                                <p>If you didn't request this verification, please contact your administrator.</p>
                                <br>
                                <p>Best regards,<br>PROVIA System</p>
                            </body>
                            </html>";
                        
                        var success = await emailService.SendEmailAsync(request.Email, subject, emailBody);
                        
                        if (success)
                        {
                            return Ok(new { 
                                message = $"Verification email sent to {request.Email}",
                                details = "Email sent via SendGrid"
                            });
                        }
                        else
                        {
                            return Ok(new { 
                                message = $"Email service not configured - verification code generated",
                                verificationCode = verificationCode,
                                testSubject = subject,
                                testMessage = emailBody,
                                details = "SendGrid credentials may need configuration"
                            });
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error sending verification email");
                        return Ok(new { 
                            message = $"Verification code generated but email failed",
                            verificationCode = verificationCode,
                            details = "Check SendGrid credentials and configuration"
                        });
                    }
                }
                else
                {
                    var subject = "PROVIA Email Verification";
                    var emailMessage = $"Your email verification code is: {verificationCode}. This code expires in 15 minutes.";
                    return Ok(new { 
                        message = $"Test email ready for {request.Email}",
                        verificationCode = verificationCode,
                        testSubject = subject,
                        testMessage = emailMessage,
                        details = "Email service not initialized"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email verification process");
                return StatusCode(500, new { message = "Error processing verification request", error = ex.Message });
            }
        }
    }

    public class LoginRequestModel
    {
        public string Identifier { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class PhoneLoginRequestModel
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }

    public class TokenValidationRequestModel
    {
        public string Token { get; set; } = string.Empty;
    }

    public class CreateUserRequestModel
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

    public class UpdateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Username { get; set; }
        public int Role { get; set; }
    }

    public class VerifyPhoneRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class SendPhoneVerificationRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class VerifyEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}