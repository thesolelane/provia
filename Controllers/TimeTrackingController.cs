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

namespace JobTracker.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TimeTrackingController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<TimeTrackingController> _logger;
        private readonly CardSharkService _cardSharkService;

        public TimeTrackingController(
            JobTrackerContext context, 
            ILogger<TimeTrackingController> logger,
            CardSharkService cardSharkService)
        {
            _context = context;
            _logger = logger;
            _cardSharkService = cardSharkService;
        }

        // GET: api/TimeTracking
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeEntry>>> GetTimeEntries()
        {
            try
            {
                return await _context.TimeEntries
                    .Include(t => t.User)
                    .Include(t => t.Job)
                    .Include(t => t.JobSection)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entries");
                return StatusCode(500, "Internal server error occurred while retrieving time entries.");
            }
        }

        // GET: api/TimeTracking/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TimeEntry>> GetTimeEntry(int id)
        {
            try
            {
                var timeEntry = await _context.TimeEntries
                    .Include(t => t.User)
                    .Include(t => t.Job)
                    .Include(t => t.JobSection)
                    .FirstOrDefaultAsync(t => t.TimeEntryId == id);

                if (timeEntry == null)
                {
                    return NotFound($"Time entry with ID {id} not found.");
                }

                return timeEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entry with ID {TimeEntryId}", id);
                return StatusCode(500, $"Internal server error occurred while retrieving time entry with ID {id}.");
            }
        }

        // POST: api/TimeTracking/clockin
        [HttpPost("clockin")]
        public async Task<ActionResult<TimeEntry>> ClockIn([FromBody] TimeEntry timeEntry)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user is already clocked in
                var existingOpenEntry = await _context.TimeEntries
                    .Where(t => t.UserId == timeEntry.UserId && t.ClockOutTime == null)
                    .FirstOrDefaultAsync();

                if (existingOpenEntry != null)
                {
                    return BadRequest("User is already clocked in. Please clock out first.");
                }

                // Set clock in time to current time if not provided
                if (timeEntry.ClockInTime == default)
                {
                    timeEntry.ClockInTime = DateTime.UtcNow;
                }

                _context.TimeEntries.Add(timeEntry);
                await _context.SaveChangesAsync();

                // Sync with CardShark if enabled
                try
                {
                    await _cardSharkService.SyncClockIn(timeEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync clock-in with CardShark. Time entry ID: {TimeEntryId}", timeEntry.TimeEntryId);
                    // Continue execution even if CardShark sync fails
                }

                return CreatedAtAction(nameof(GetTimeEntry), new { id = timeEntry.TimeEntryId }, timeEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing clock in");
                return StatusCode(500, "Internal server error occurred while processing clock in.");
            }
        }

        // POST: api/TimeTracking/clockout/5
        [HttpPost("clockout/{id}")]
        public async Task<ActionResult<TimeEntry>> ClockOut(int id, [FromBody] TimeEntry timeEntryUpdate)
        {
            try
            {
                var timeEntry = await _context.TimeEntries.FindAsync(id);
                if (timeEntry == null)
                {
                    return NotFound($"Time entry with ID {id} not found.");
                }

                if (timeEntry.ClockOutTime != null)
                {
                    return BadRequest("This time entry has already been clocked out.");
                }

                // Set clock out time to current time if not provided
                if (timeEntryUpdate.ClockOutTime == null)
                {
                    timeEntry.ClockOutTime = DateTime.UtcNow;
                }
                else
                {
                    timeEntry.ClockOutTime = timeEntryUpdate.ClockOutTime;
                }

                // Update notes if provided
                if (!string.IsNullOrWhiteSpace(timeEntryUpdate.Notes))
                {
                    timeEntry.Notes = timeEntryUpdate.Notes;
                }

                // Calculate total hours
                if (timeEntry.ClockOutTime.HasValue)
                {
                    TimeSpan duration = timeEntry.ClockOutTime.Value - timeEntry.ClockInTime;
                    timeEntry.TotalHours = (decimal)duration.TotalHours;
                }

                await _context.SaveChangesAsync();

                // Sync with CardShark if enabled
                try
                {
                    await _cardSharkService.SyncClockOut(timeEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync clock-out with CardShark. Time entry ID: {TimeEntryId}", timeEntry.TimeEntryId);
                    // Continue execution even if CardShark sync fails
                }

                return Ok(timeEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing clock out for time entry with ID {TimeEntryId}", id);
                return StatusCode(500, $"Internal server error occurred while processing clock out for time entry with ID {id}.");
            }
        }

        // GET: api/TimeTracking/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<TimeEntry>>> GetUserTimeEntries(string userId)
        {
            try
            {
                return await _context.TimeEntries
                    .Include(t => t.Job)
                    .Include(t => t.JobSection)
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.ClockInTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entries for user with ID {UserId}", userId);
                return StatusCode(500, $"Internal server error occurred while retrieving time entries for user with ID {userId}.");
            }
        }

        // GET: api/TimeTracking/job/{jobId}
        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<IEnumerable<TimeEntry>>> GetJobTimeEntries(int jobId)
        {
            try
            {
                return await _context.TimeEntries
                    .Include(t => t.User)
                    .Include(t => t.JobSection)
                    .Where(t => t.JobId == jobId)
                    .OrderByDescending(t => t.ClockInTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entries for job with ID {JobId}", jobId);
                return StatusCode(500, $"Internal server error occurred while retrieving time entries for job with ID {jobId}.");
            }
        }

        // GET: api/TimeTracking/section/{sectionId}
        [HttpGet("section/{sectionId}")]
        public async Task<ActionResult<IEnumerable<TimeEntry>>> GetSectionTimeEntries(int sectionId)
        {
            try
            {
                return await _context.TimeEntries
                    .Include(t => t.User)
                    .Include(t => t.Job)
                    .Where(t => t.JobSectionId == sectionId)
                    .OrderByDescending(t => t.ClockInTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entries for section with ID {SectionId}", sectionId);
                return StatusCode(500, $"Internal server error occurred while retrieving time entries for section with ID {sectionId}.");
            }
        }

        // GET: api/TimeTracking/report
        [HttpGet("report")]
        public async Task<ActionResult<object>> GetTimeReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string userId, [FromQuery] int? jobId)
        {
            try
            {
                var query = _context.TimeEntries
                    .Include(t => t.User)
                    .Include(t => t.Job)
                    .Include(t => t.JobSection)
                    .Where(t => t.ClockOutTime != null);  // Only include completed time entries

                // Apply filters
                if (startDate.HasValue)
                {
                    query = query.Where(t => t.ClockInTime >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(t => t.ClockInTime <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(t => t.UserId == userId);
                }

                if (jobId.HasValue)
                {
                    query = query.Where(t => t.JobId == jobId.Value);
                }

                var timeEntries = await query.ToListAsync();

                // Calculate totals
                var totalHours = timeEntries.Sum(t => t.TotalHours ?? 0);
                var userTotals = timeEntries
                    .GroupBy(t => new { t.UserId, UserName = t.User?.UserName })
                    .Select(g => new
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.UserName,
                        TotalHours = g.Sum(t => t.TotalHours ?? 0)
                    });

                var jobTotals = timeEntries
                    .GroupBy(t => new { t.JobId, JobName = t.Job?.JobName })
                    .Select(g => new
                    {
                        JobId = g.Key.JobId,
                        JobName = g.Key.JobName,
                        TotalHours = g.Sum(t => t.TotalHours ?? 0)
                    });

                var result = new
                {
                    TimeEntries = timeEntries,
                    TotalHours = totalHours,
                    UserTotals = userTotals,
                    JobTotals = jobTotals
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating time report");
                return StatusCode(500, "Internal server error occurred while generating time report.");
            }
        }

        // PUT: api/TimeTracking/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTimeEntry(int id, TimeEntry timeEntry)
        {
            try
            {
                if (id != timeEntry.TimeEntryId)
                {
                    return BadRequest("Time entry ID mismatch.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Recalculate total hours if both clock in and clock out times are provided
                if (timeEntry.ClockInTime != default && timeEntry.ClockOutTime.HasValue)
                {
                    TimeSpan duration = timeEntry.ClockOutTime.Value - timeEntry.ClockInTime;
                    timeEntry.TotalHours = (decimal)duration.TotalHours;
                }

                _context.Entry(timeEntry).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeEntryExists(id))
                    {
                        return NotFound($"Time entry with ID {id} not found.");
                    }
                    else
                    {
                        throw;
                    }
                }

                // Sync with CardShark if enabled
                try
                {
                    await _cardSharkService.SyncTimeEntryUpdate(timeEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync time entry update with CardShark. Time entry ID: {TimeEntryId}", timeEntry.TimeEntryId);
                    // Continue execution even if CardShark sync fails
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time entry with ID {TimeEntryId}", id);
                return StatusCode(500, $"Internal server error occurred while updating time entry with ID {id}.");
            }
        }

        // DELETE: api/TimeTracking/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTimeEntry(int id)
        {
            try
            {
                var timeEntry = await _context.TimeEntries.FindAsync(id);
                if (timeEntry == null)
                {
                    return NotFound($"Time entry with ID {id} not found.");
                }

                _context.TimeEntries.Remove(timeEntry);
                await _context.SaveChangesAsync();

                // Sync with CardShark if enabled
                try
                {
                    await _cardSharkService.SyncTimeEntryDeletion(timeEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync time entry deletion with CardShark. Time entry ID: {TimeEntryId}", timeEntry.TimeEntryId);
                    // Continue execution even if CardShark sync fails
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time entry with ID {TimeEntryId}", id);
                return StatusCode(500, $"Internal server error occurred while deleting time entry with ID {id}.");
            }
        }

        private bool TimeEntryExists(int id)
        {
            return _context.TimeEntries.Any(e => e.TimeEntryId == id);
        }
    }
}
