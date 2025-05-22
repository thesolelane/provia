using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class UsersController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly AuthService _authService;

        public UsersController(
            JobTrackerContext context, 
            ILogger<UsersController> logger,
            AuthService authService)
        {
            _context = context;
            _logger = logger;
            _authService = authService;
        }

        // GET: api/Users
        [HttpGet]
        [Authorize(Roles = "Administrator,ProjectManager")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            try
            {
                return await _context.Users.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, "Internal server error occurred while retrieving users.");
            }
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found.");
                }

                // Only administrators, project managers, or the user themselves can view user details
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Administrator") || User.IsInRole("ProjectManager");

                if (!isAdmin && currentUserId != id)
                {
                    return Forbid();
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
                return StatusCode(500, $"Internal server error occurred while retrieving user with ID {id}.");
            }
        }

        // POST: api/Users
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user already exists
                var existingUser = await _context.Users.FindAsync(user.UserId);
                if (existingUser != null)
                {
                    return Conflict($"User with ID {user.UserId} already exists.");
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new user");
                return StatusCode(500, "Internal server error occurred while creating a new user.");
            }
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> UpdateUser(string id, User user)
        {
            try
            {
                if (id != user.UserId)
                {
                    return BadRequest("User ID mismatch.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.Entry(user).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(id))
                    {
                        return NotFound($"User with ID {id} not found.");
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", id);
                return StatusCode(500, $"Internal server error occurred while updating user with ID {id}.");
            }
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found.");
                }

                // Instead of deleting, mark as inactive
                user.IsActive = false;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user with ID {UserId}", id);
                return StatusCode(500, $"Internal server error occurred while deactivating user with ID {id}.");
            }
        }

        // GET: api/Users/current
        [HttpGet("current")]
        public async Task<ActionResult<User>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated or identity not available.");
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    // If user doesn't exist in our database, but they're authenticated,
                    // we'll create an entry for them using AD information
                    user = await _authService.CreateUserFromActiveDirectory(userId);
                    if (user == null)
                    {
                        return NotFound("User not found in Active Directory.");
                    }
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user");
                return StatusCode(500, "Internal server error occurred while retrieving current user.");
            }
        }

        // GET: api/Users/skills?skill=Plumbing
        [HttpGet("skills")]
        [Authorize(Roles = "Administrator,ProjectManager,Supervisor")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersBySkill(string skill)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(skill))
                {
                    return await _context.Users.Where(u => u.IsActive).ToListAsync();
                }

                return await _context.Users
                    .Where(u => u.IsActive && u.Skills.Contains(skill))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by skill {Skill}", skill);
                return StatusCode(500, "Internal server error occurred while retrieving users by skill.");
            }
        }

        // GET: api/Users/5/timesheet
        [HttpGet("{id}/timesheet")]
        public async Task<ActionResult<IEnumerable<TimeEntry>>> GetUserTimesheet(string id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // Check if user exists
                if (!UserExists(id))
                {
                    return NotFound($"User with ID {id} not found.");
                }

                // Only administrators, project managers, or the user themselves can view timesheets
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Administrator") || User.IsInRole("ProjectManager");

                if (!isAdmin && currentUserId != id)
                {
                    return Forbid();
                }

                var query = _context.TimeEntries
                    .Include(t => t.Job)
                    .Include(t => t.JobSection)
                    .Where(t => t.UserId == id);

                if (startDate.HasValue)
                {
                    query = query.Where(t => t.ClockInTime >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(t => t.ClockInTime <= endDate.Value);
                }

                return await query.OrderByDescending(t => t.ClockInTime).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving timesheet for user with ID {UserId}", id);
                return StatusCode(500, $"Internal server error occurred while retrieving timesheet for user with ID {id}.");
            }
        }

        private bool UserExists(string id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
