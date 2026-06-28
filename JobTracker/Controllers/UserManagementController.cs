using Microsoft.AspNetCore.Mvc;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserManagementController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly IUserDeactivationService _deactivationService;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            JobTrackerContext context, 
            IUserDeactivationService deactivationService,
            ILogger<UserManagementController> logger)
        {
            _context = context;
            _deactivationService = deactivationService;
            _logger = logger;
        }

        [HttpPost("{userId}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int userId, [FromBody] DeactivationRequest request)
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
                    if (parts.Length >= 3 && int.TryParse(parts[0], out int currentUserId) && int.TryParse(parts[2], out int userRole))
                    {
                        if (userRole != 1510) // Only Master Admin can deactivate users
                        {
                            return Forbid("Only administrators can deactivate users");
                        }

                        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId && u.IsActive);
                        if (currentUser == null)
                        {
                            return Unauthorized(new { message = "Invalid token" });
                        }

                        // Don't allow deactivating yourself
                        if (userId == currentUserId)
                        {
                            return BadRequest(new { message = "Cannot deactivate your own account" });
                        }

                        // Find user to deactivate
                        var userToDeactivate = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                        if (userToDeactivate == null)
                        {
                            return NotFound(new { message = "User not found or already deactivated" });
                        }

                        // Use the deactivation service to properly archive credentials
                        var success = await _deactivationService.DeactivateUserAsync(
                            userId, 
                            currentUserId, 
                            request.Reason, 
                            request.Notes
                        );

                        if (success)
                        {
                            return Ok(new { message = "User deactivated successfully and credentials archived" });
                        }
                        else
                        {
                            return StatusCode(500, new { message = "Failed to deactivate user" });
                        }
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
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while deactivating the user" });
            }
        }

        [HttpGet("deactivated")]
        public async Task<IActionResult> GetDeactivatedUsers()
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
                    if (parts.Length >= 3 && int.TryParse(parts[0], out int currentUserId) && int.TryParse(parts[2], out int userRole))
                    {
                        if (userRole != 1510) // Only Master Admin can view deactivated users archive
                        {
                            return Forbid("Only administrators can view deactivated users");
                        }

                        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId && u.IsActive);
                        if (currentUser == null)
                        {
                            return Unauthorized(new { message = "Invalid token" });
                        }

                        // Get deactivated users for the company
                        var deactivatedUsers = await _deactivationService.GetDeactivatedUsersAsync(currentUser.CompanyId);
                        
                        var result = deactivatedUsers.Select(du => new
                        {
                            id = du.Id,
                            originalUserId = du.OriginalUserId,
                            firstName = du.FirstName,
                            lastName = du.LastName,
                            email = du.Email,
                            phoneNumber = du.PhoneNumber,
                            role = du.Role,
                            userCode = du.UserCode,
                            shortenedUserId = du.ShortenedUserId,
                            deactivatedAt = du.DeactivatedAt,
                            deactivatedBy = du.DeactivatedBy?.GetDisplayName(),
                            deactivationReason = du.DeactivationReason,
                            deactivationNotes = du.DeactivationNotes,
                            canBeReactivated = du.CanBeReactivated
                        });

                        return Ok(result);
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
                _logger.LogError(ex, "Error retrieving deactivated users");
                return StatusCode(500, new { message = "An error occurred while retrieving deactivated users" });
            }
        }
    }

    public class DeactivationRequest
    {
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }
}