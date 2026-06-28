using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.AspNetCore.Authorization;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(JobTrackerContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.IsActive)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "Error retrieving users" });
            }
        }

        [HttpGet("company/{companyId}")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetUsersByCompany(int companyId)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.CompanyId == companyId && u.IsActive)
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

                var stats = new
                {
                    total = users.Count,
                    admins = users.Count(u => u.role == 1510),
                    supervisors = users.Count(u => u.role == 1520),
                    fieldOperators = users.Count(u => u.role == 2001)
                };

                return Ok(new { users, stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for company {CompanyId}", companyId);
                return StatusCode(500, new { message = "Error retrieving users" });
            }
        }

        [HttpGet("field-operators")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<object>>> GetFieldOperators()
        {
            try
            {
                var fieldOperators = await _context.Users
                    .Where(u => u.Role == 2001 && u.IsActive) // Field Operator role code
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.FirstName + " " + u.LastName,
                        role = "FieldOperator",
                        email = u.Email,
                        phone = u.PhoneNumber
                    })
                    .ToListAsync();

                return Ok(fieldOperators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving field operators");
                return StatusCode(500, "Error retrieving field operators");
            }
        }


    }
}