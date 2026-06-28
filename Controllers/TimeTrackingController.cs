using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JobTrackerApp.Data;
using JobTrackerApp.Models;
using JobTrackerApp.Services.CardShark;

namespace JobTrackerApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TimeTrackingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TimeTrackingController> _logger;
        private readonly CardSharkIntegrationService _cardSharkService;

        public TimeTrackingController(
            ApplicationDbContext context,
            ILogger<TimeTrackingController> logger,
            CardSharkIntegrationService cardSharkService)
        {
            _context = context;
            _logger = logger;
            _cardSharkService = cardSharkService;
        }

        // GET: api/TimeTracking
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeEntry>>> GetTimeEntries(
            [FromQuery] int? employeeId = null,
            [FromQuery] int? jobId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                IQueryable<TimeEntry> query = _context.TimeEntries
                    .Include(t => t.Employee)
                    .Include(t => t.Job);

                // Apply filters
                if (employeeId.HasValue)
                {
                    query = query.Where(t => t.EmployeeId == employeeId.Value);
                }

                if (jobId.HasValue)
                {
                    query = query.Where(t => t.JobId == jobId.Value);
                }

                if (startDate.HasValue)
                {
                    DateTime start = startDate.Value.Date;
                    query = query.Where(t => t.ClockInTime >= start);
                }

                if (endDate.HasValue)
                {
                    DateTime end = endDate.Value.Date.AddDays(1).AddSeconds(-1); // End of day
                    query = query.Where(t => t.ClockInTime <= end);
                }

                // Order by most recent first
                query = query.OrderByDescending(t => t.ClockInTime);

                var timeEntries = await query.ToListAsync();
                return Ok(timeEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entries");
                return StatusCode(500, "An error occurred while retrieving time entries");
            }
        }

        // GET: api/TimeTracking/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TimeEntry>> GetTimeEntry(int id)
        {
            try
            {
                var timeEntry = await _context.TimeEntries
                    .Include(t => t.Employee)
                    .Include(t => t.Job)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (timeEntry == null)
                {
                    return NotFound();
                }

                return Ok(timeEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entry with ID {TimeEntryId}", id);
                return StatusCode(500, "An error occurred while retrieving the time entry");
            }
        }

        // POST: api/TimeTracking/clock-in
        [HttpPost("clock-in")]
        public async Task<ActionResult<TimeEntry>> ClockIn(ClockInRequest request)
        {
            try
            {
                // Validate employee
                var employee = await _context.Employees.FindAsync(request.EmployeeId);
                if (employee == null)
                {
                    return BadRequest("Invalid employee ID");
                }

                // Check if employee is already clocked in
                var openEntry = await _context.TimeEntries
                    .FirstOrDefaultAsync(t => t.EmployeeId == request.EmployeeId && t.ClockOutTime == null);

                if (openEntry != null)
                {
                    return BadRequest("Employee is already clocked in");
                }

                // Validate job if provided
                if (request.JobId.HasValue)
                {
                    var job = await _context.Jobs.FindAsync(request.JobId.Value);
                    if (job == null)
                    {
                        return BadRequest("Invalid job ID");
                    }
                }

                // Create new time entry
                var timeEntry = new TimeEntry
                {
                    EmployeeId = request.EmployeeId,
                    JobId = request.JobId,
                    ClockInTime = request.ClockInTime ?? DateTime.UtcNow,
                    Location = request.Location,
                    Notes = request.Notes,
                    Status = "Open",
                    CreatedBy = User.Identity?.Name ?? "System",
                    UpdatedBy = User.Identity?.Name ?? "System"
                };

                _context.TimeEntries.Add(timeEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee {EmployeeId} clocked in at {ClockInTime}", 
                    timeEntry.EmployeeId, timeEntry.ClockInTime);

                return CreatedAtAction(nameof(GetTimeEntry), new { id = timeEntry.Id }, timeEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock in");
                return StatusCode(500, "An error occurred during clock in");
            }
        }

        // POST: api/TimeTracking/clock-out
        [HttpPost("clock-out")]
        public async Task<ActionResult<TimeEntry>> ClockOut(ClockOutRequest request)
        {
            try
            {
                // Find the open time entry
                var timeEntry = await _context.TimeEntries
                    .FirstOrDefaultAsync(t => t.EmployeeId == request.EmployeeId && t.ClockOutTime == null);

                if (timeEntry == null)
                {
                    return BadRequest("No open time entry found for this employee");
                }

                // Update time entry
                timeEntry.ClockOutTime = request.ClockOutTime ?? DateTime.UtcNow;
                timeEntry.JobId = request.JobId ?? timeEntry.JobId; // Allow updating job ID on clock out
                timeEntry.Notes = !string.IsNullOrEmpty(request.Notes) ? request.Notes : timeEntry.Notes;
                timeEntry.Status = "Completed";
                timeEntry.UpdatedAt = DateTime.UtcNow;
                timeEntry.UpdatedBy = User.Identity?.Name ?? "System";

                // Calculate total hours
                timeEntry.CalculateTotalHours();

                _context.Entry(timeEntry).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee {EmployeeId} clocked out at {ClockOutTime}, total hours: {TotalHours}", 
                    timeEntry.EmployeeId, timeEntry.ClockOutTime, timeEntry.TotalHours);

                // Sync with CardShark
                try
                {
                    await _cardSharkService.SyncTimeEntries(new List<TimeEntry> { timeEntry });
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request
                    _logger.LogWarning(ex, "Failed to sync time entry with CardShark");
                }

                return Ok(timeEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock out");
                return StatusCode(500, "An error occurred during clock out");
            }
        }

        // POST: api/TimeTracking
        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<TimeEntry>> CreateTimeEntry(TimeEntry timeEntry)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate employee
                var employee = await _context.Employees.FindAsync(timeEntry.EmployeeId);
                if (employee == null)
                {
                    return BadRequest("Invalid employee ID");
                }

                // Validate job if provided
                if (timeEntry.JobId.HasValue)
                {
                    var job = await _context.Jobs.FindAsync(timeEntry.JobId.Value);
                    if (job == null)
                    {
                        return BadRequest("Invalid job ID");
                    }
                }

                // Set as manual entry
                timeEntry.IsManualEntry = true;
                
                // Set metadata
                timeEntry.CreatedAt = DateTime.UtcNow;
                timeEntry.UpdatedAt = DateTime.UtcNow;
                timeEntry.CreatedBy = User.Identity?.Name ?? "System";
                timeEntry.UpdatedBy = User.Identity?.Name ?? "System";

                // Calculate total hours if both times are provided
                if (timeEntry.ClockInTime != null && timeEntry.ClockOutTime != null)
                {
                    timeEntry.CalculateTotalHours();
                    timeEntry.Status = "Completed";
                }
                else
                {
                    timeEntry.Status = "Open";
                }

                _context.TimeEntries.Add(timeEntry);
                await _context.SaveChangesAsync();

                // Sync with CardShark if entry is complete
                if (timeEntry.ClockOutTime.HasValue)
                {
                    try
                    {
                        await _cardSharkService.SyncTimeEntries(new List<TimeEntry> { timeEntry });
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the request
                        _logger.LogWarning(ex, "Failed to sync time entry with CardShark");
                    }
                }

                _logger.LogInformation("Created manual time entry for employee {EmployeeId}", timeEntry.EmployeeId);
                
                return CreatedAtAction(nameof(GetTimeEntry), new { id = timeEntry.Id }, timeEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating time entry");
                return StatusCode(500, "An error occurred while creating the time entry");
            }
        }

        // PUT: api/TimeTracking/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> UpdateTimeEntry(int id, TimeEntry timeEntry)
        {
            try
            {
                if (id != timeEntry.Id)
                {
                    return BadRequest("Time entry ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if time entry exists
                var existingEntry = await _context.TimeEntries.FindAsync(id);
                if (existingEntry == null)
                {
                    return NotFound();
                }

                // Update metadata
                timeEntry.CreatedAt = existingEntry.CreatedAt;
                timeEntry.CreatedBy = existingEntry.CreatedBy;
                timeEntry.UpdatedAt = DateTime.UtcNow;
                timeEntry.UpdatedBy = User.Identity?.Name ?? "System";
                timeEntry.IsManualEntry = true; // Mark as edited

                // Calculate total hours if both times are provided
                if (timeEntry.ClockInTime != null && timeEntry.ClockOutTime != null)
                {
                    timeEntry.CalculateTotalHours();
                    timeEntry.Status = "Completed";
                }
                else
                {
                    timeEntry.Status = "Open";
                }

                _context.Entry(existingEntry).State = EntityState.Detached;
                _context.Entry(timeEntry).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                // Sync with CardShark if entry is complete
                if (timeEntry.ClockOutTime.HasValue)
                {
                    try
                    {
                        await _cardSharkService.SyncTimeEntries(new List<TimeEntry> { timeEntry });
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the request
                        _logger.LogWarning(ex, "Failed to sync updated time entry with CardShark");
                    }
                }

                _logger.LogInformation("Updated time entry: {TimeEntryId}", timeEntry.Id);
                
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TimeEntryExists(id))
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
                _logger.LogError(ex, "Error updating time entry with ID {TimeEntryId}", id);
                return StatusCode(500, "An error occurred while updating the time entry");
            }
        }

        // DELETE: api/TimeTracking/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTimeEntry(int id)
        {
            try
            {
                var timeEntry = await _context.TimeEntries.FindAsync(id);
                if (timeEntry == null)
                {
                    return NotFound();
                }

                _context.TimeEntries.Remove(timeEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted time entry: {TimeEntryId}", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time entry with ID {TimeEntryId}", id);
                return StatusCode(500, "An error occurred while deleting the time entry");
            }
        }

        // GET: api/TimeTracking/employee/5/current
        [HttpGet("employee/{id}/current")]
        public async Task<ActionResult<TimeEntry>> GetCurrentTimeEntry(int id)
        {
            try
            {
                var timeEntry = await _context.TimeEntries
                    .Include(t => t.Job)
                    .FirstOrDefaultAsync(t => t.EmployeeId == id && t.ClockOutTime == null);

                if (timeEntry == null)
                {
                    return NotFound("No active time entry found");
                }

                return Ok(timeEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current time entry for employee {EmployeeId}", id);
                return StatusCode(500, "An error occurred while retrieving the current time entry");
            }
        }

        // GET: api/TimeTracking/summary
        [HttpGet("summary")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<IEnumerable<TimeEntrySummary>>> GetTimeSummary(
            [FromQuery] int? employeeId = null,
            [FromQuery] int? jobId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                IQueryable<TimeEntry> query = _context.TimeEntries
                    .Where(t => t.ClockOutTime != null); // Only completed entries

                // Apply filters
                if (employeeId.HasValue)
                {
                    query = query.Where(t => t.EmployeeId == employeeId.Value);
                }

                if (jobId.HasValue)
                {
                    query = query.Where(t => t.JobId == jobId.Value);
                }

                if (startDate.HasValue)
                {
                    DateTime start = startDate.Value.Date;
                    query = query.Where(t => t.ClockInTime >= start);
                }

                if (endDate.HasValue)
                {
                    DateTime end = endDate.Value.Date.AddDays(1).AddSeconds(-1); // End of day
                    query = query.Where(t => t.ClockInTime <= end);
                }

                // Group by employee and job
                var summaries = await query
                    .GroupBy(t => new { t.EmployeeId, t.JobId })
                    .Select(g => new TimeEntrySummary
                    {
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeName = g.First().Employee.FirstName + " " + g.First().Employee.LastName,
                        JobId = g.Key.JobId,
                        JobName = g.Key.JobId.HasValue ? g.First().Job.Name : "No Job",
                        TotalHours = g.Sum(t => t.TotalHours ?? 0),
                        EntryCount = g.Count(),
                        FirstEntry = g.Min(t => t.ClockInTime),
                        LastEntry = g.Max(t => t.ClockOutTime)
                    })
                    .ToListAsync();

                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time summary");
                return StatusCode(500, "An error occurred while retrieving time summary");
            }
        }

        private bool TimeEntryExists(int id)
        {
            return _context.TimeEntries.Any(e => e.Id == id);
        }
    }

    public class ClockInRequest
    {
        public int EmployeeId { get; set; }
        public int? JobId { get; set; }
        public DateTime? ClockInTime { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
    }

    public class ClockOutRequest
    {
        public int EmployeeId { get; set; }
        public int? JobId { get; set; }
        public DateTime? ClockOutTime { get; set; }
        public string? Notes { get; set; }
    }

    public class TimeEntrySummary
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int? JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public int EntryCount { get; set; }
        public DateTime FirstEntry { get; set; }
        public DateTime? LastEntry { get; set; }
    }
}
