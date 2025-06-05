using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.AspNetCore.Authorization;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class WorkingAuthController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<WorkingAuthController> _logger;

        public WorkingAuthController(JobTrackerContext context, ILogger<WorkingAuthController> logger)
        {
            _context = context;
            _logger = logger;
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

                // Find user by email or username
                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => 
                        u.Email == request.Identifier || 
                        u.Username == request.Identifier);

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

                // Simple password check for demo
                if (user.PasswordHash != request.Password)
                {
                    _logger.LogWarning($"Login attempt failed: Invalid password for user {user.Id}");
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                // Update last login using correct property
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate simple token
                var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user.Id}:{DateTime.UtcNow.Ticks}:{user.Role}"));

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

                        // Set password or PIN hash (simple for demo)
                        if (!string.IsNullOrEmpty(request.Password))
                        {
                            newUser.PasswordHash = request.Password;
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
        public int Role { get; set; }
    }
}