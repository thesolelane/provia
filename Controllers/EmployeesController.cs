using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JobTrackerApp.Data;
using JobTrackerApp.Models;

namespace JobTrackerApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(ApplicationDbContext context, ILogger<EmployeesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Employees
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees([FromQuery] bool? active = null, [FromQuery] string? search = null)
        {
            try
            {
                IQueryable<Employee> query = _context.Employees;

                // Filter by active status if provided
                if (active.HasValue)
                {
                    query = query.Where(e => e.IsActive == active.Value);
                }

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(e =>
                        e.FirstName.ToLower().Contains(search) ||
                        e.LastName.ToLower().Contains(search) ||
                        e.Email.ToLower().Contains(search) ||
                        e.EmployeeNumber.ToLower().Contains(search));
                }

                // Order by name
                query = query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName);

                var employees = await query.ToListAsync();
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees");
                return StatusCode(500, "An error occurred while retrieving employees");
            }
        }

        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.JobAssignments)
                        .ThenInclude(a => a.Job)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                {
                    return NotFound();
                }

                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee with ID {EmployeeId}", id);
                return StatusCode(500, "An error occurred while retrieving the employee");
            }
        }

        // POST: api/Employees
        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Set metadata
                employee.CreatedAt = DateTime.UtcNow;
                employee.UpdatedAt = DateTime.UtcNow;
                employee.CreatedBy = User.Identity?.Name ?? "System";
                employee.UpdatedBy = User.Identity?.Name ?? "System";

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new employee: {EmployeeName} ({EmployeeId})", employee.FullName, employee.Id);
                
                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                return StatusCode(500, "An error occurred while creating the employee");
            }
        }

        // PUT: api/Employees/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee employee)
        {
            try
            {
                if (id != employee.Id)
                {
                    return BadRequest("Employee ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if employee exists
                var existingEmployee = await _context.Employees.FindAsync(id);
                if (existingEmployee == null)
                {
                    return NotFound();
                }

                // Update metadata
                employee.CreatedAt = existingEmployee.CreatedAt;
                employee.CreatedBy = existingEmployee.CreatedBy;
                employee.UpdatedAt = DateTime.UtcNow;
                employee.UpdatedBy = User.Identity?.Name ?? "System";

                _context.Entry(existingEmployee).State = EntityState.Detached;
                _context.Entry(employee).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated employee: {EmployeeName} ({EmployeeId})", employee.FullName, employee.Id);
                
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID {EmployeeId}", id);
                return StatusCode(500, "An error occurred while updating the employee");
            }
        }

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound();
                }

                // Soft delete by setting IsActive to false
                employee.IsActive = false;
                employee.UpdatedAt = DateTime.UtcNow;
                employee.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft-deleted employee: {EmployeeName} ({EmployeeId})", employee.FullName, employee.Id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee with ID {EmployeeId}", id);
                return StatusCode(500, "An error occurred while deleting the employee");
            }
        }

        // GET: api/Employees/5/job-assignments
        [HttpGet("{id}/job-assignments")]
        public async Task<ActionResult<IEnumerable<JobAssignment>>> GetEmployeeAssignments(int id, [FromQuery] DateTime? date = null)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound();
                }

                IQueryable<JobAssignment> query = _context.JobAssignments
                    .Include(a => a.Job)
                    .Where(a => a.EmployeeId == id && a.IsActive);

                // Filter by date if provided
                if (date.HasValue)
                {
                    DateTime dateOnly = date.Value.Date;
                    query = query.Where(a => a.AssignmentDate.Date == dateOnly);
                }
                else
                {
                    // Default to current date
                    DateTime today = DateTime.Today;
                    query = query.Where(a => a.AssignmentDate.Date == today);
                }

                var assignments = await query.ToListAsync();
                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job assignments for employee {EmployeeId}", id);
                return StatusCode(500, "An error occurred while retrieving job assignments");
            }
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }
}
