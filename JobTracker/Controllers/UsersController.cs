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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.FirstName + " " + u.LastName,
                        role = u.Role == 1510 ? "MasterAdmin" : 
                               u.Role == 1520 ? "Admin" : 
                               u.Role == 2001 ? "FieldOperator" : "Unknown",
                        email = u.Email,
                        phone = u.PhoneNumber
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, "Error retrieving users");
            }
        }
    }
}