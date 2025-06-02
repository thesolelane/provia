using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using System.Security.Claims;

namespace JobTracker.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserManagementController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly UserCodeService _userCodeService;

        public UserManagementController(JobTrackerContext context, UserCodeService userCodeService)
        {
            _context = context;
            _userCodeService = userCodeService;
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var currentUserIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return Unauthorized("Invalid user token");
            }

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || !UserRoles.IsAdmin(currentUser.Role))
            {
                return StatusCode(403, "Only admins can create users");
            }

            // Validate role assignment permissions
            if (request.Role == UserRoles.Admin && !UserRoles.IsHighestAdmin(currentUser.Role))
            {
                return StatusCode(403, "Only Admins can create Admin accounts");
            }

            try
            {
                // Generate user code
                var userCode = await _userCodeService.GenerateUserCodeAsync(currentUser.CompanyId, request.Role);

                var newUser = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = request.Role,
                    CompanyId = currentUser.CompanyId,
                    UserCode = userCode,
                    IsActive = true,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.TemporaryPassword),
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = currentUserId,
                    LocationTrackingConsent = request.Role == UserRoles.FieldOperator,
                    LanguagePreference = request.LanguagePreference ?? "en"
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    UserId = newUser.Id,
                    UserCode = newUser.UserCode,
                    Message = "User created successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating user: {ex.Message}");
            }
        }

        [HttpPost("batch-create-users")]
        public async Task<IActionResult> BatchCreateUsers([FromBody] BatchCreateUsersRequest request)
        {
            var currentUserIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return Unauthorized("Invalid user token");
            }

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || !UserRoles.IsAdmin(currentUser.Role))
            {
                return StatusCode(403, "Only admins can create users");
            }

            var results = new List<object>();
            var errors = new List<string>();

            foreach (var userRequest in request.Users)
            {
                try
                {
                    // Validate role assignment permissions
                    if (userRequest.Role == UserRoles.Admin && !UserRoles.IsHighestAdmin(currentUser.Role))
                    {
                        errors.Add($"{userRequest.FirstName} {userRequest.LastName}: Only Admins can create Admin accounts");
                        continue;
                    }

                    // Check if email already exists
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == userRequest.Email && u.CompanyId == currentUser.CompanyId);
                    
                    if (existingUser != null)
                    {
                        errors.Add($"{userRequest.FirstName} {userRequest.LastName}: Email already exists");
                        continue;
                    }

                    // Generate user code
                    var userCode = await _userCodeService.GenerateUserCodeAsync(currentUser.CompanyId, userRequest.Role);

                    var newUser = new User
                    {
                        FirstName = userRequest.FirstName,
                        LastName = userRequest.LastName,
                        Email = userRequest.Email,
                        PhoneNumber = userRequest.PhoneNumber,
                        Role = userRequest.Role,
                        CompanyId = currentUser.CompanyId,
                        UserCode = userCode,
                        IsActive = true,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(userRequest.TemporaryPassword ?? "TempPass123!"),
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = currentUserId,
                        LocationTrackingConsent = userRequest.Role == UserRoles.FieldOperator,
                        LanguagePreference = userRequest.LanguagePreference ?? "en"
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    results.Add(new {
                        UserId = newUser.Id,
                        UserCode = newUser.UserCode,
                        Name = $"{newUser.FirstName} {newUser.LastName}",
                        Email = newUser.Email,
                        Role = UserRoles.GetRoleName(newUser.Role)
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"{userRequest.FirstName} {userRequest.LastName}: {ex.Message}");
                }
            }

            return Ok(new { 
                CreatedUsers = results,
                Errors = errors,
                TotalProcessed = request.Users.Count,
                SuccessfullyCreated = results.Count
            });
        }

        [HttpGet("available-roles")]
        public IActionResult GetAvailableRoles()
        {
            var currentUserIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return Unauthorized("Invalid user token");
            }

            var currentUser = _context.Users.Find(currentUserId);
            if (currentUser == null || !UserRoles.IsAdmin(currentUser.Role))
            {
                return StatusCode(403, "Only admins can access role information");
            }

            var availableRoles = new List<object>();

            // Field Operator - all admins can create
            availableRoles.Add(new { Code = UserRoles.FieldOperator, Name = "Field Operator" });
            
            // Supervisor - all admins can create
            availableRoles.Add(new { Code = UserRoles.Supervisor, Name = "Supervisor" });

            // Admin - only highest admins can create
            if (UserRoles.IsHighestAdmin(currentUser.Role))
            {
                availableRoles.Add(new { Code = UserRoles.Admin, Name = "Admin" });
            }

            return Ok(availableRoles);
        }

        [HttpPost("assign-user-codes")]
        public async Task<IActionResult> AssignUserCodes()
        {
            var currentUserIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return Unauthorized("Invalid user token");
            }

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || !UserRoles.IsHighestAdmin(currentUser.Role))
            {
                return StatusCode(403, "Only Admins can assign user codes");
            }

            try
            {
                await _userCodeService.AssignUserCodesAsync();
                return Ok(new { Message = "User codes assigned successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error assigning user codes: {ex.Message}");
            }
        }
    }

    public class CreateUserRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public int Role { get; set; }
        public string TemporaryPassword { get; set; } = "";
        public string? LanguagePreference { get; set; }
    }

    public class BatchCreateUsersRequest
    {
        public List<CreateUserRequest> Users { get; set; } = new();
    }
}