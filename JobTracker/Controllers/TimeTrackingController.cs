using Microsoft.AspNetCore.Mvc;
using JobTracker.Services;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;
using JobTracker.Models;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeTrackingController : ControllerBase
    {
        private readonly GeoFencingService _geoFencingService;
        private readonly JobTrackerContext _context;
        private readonly ILogger<TimeTrackingController> _logger;

        public TimeTrackingController(GeoFencingService geoFencingService, JobTrackerContext context, ILogger<TimeTrackingController> logger)
        {
            _geoFencingService = geoFencingService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("clock-in/initiate")]
        public async Task<IActionResult> InitiateClockIn([FromBody] InitiateClockInRequest request)
        {
            try
            {
                var result = await _geoFencingService.InitiateClockIn(request.UserId, request.Latitude, request.Longitude);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating clock-in for user {UserId}", request.UserId);
                return StatusCode(500, new { message = "Clock-in failed" });
            }
        }

        [HttpPost("clock-in/finalize")]
        public async Task<IActionResult> FinalizeClockIn([FromBody] FinalizeClockInRequest request)
        {
            try
            {
                var result = await _geoFencingService.FinalizeClockIn(request.PendingClockInId, request.Latitude, request.Longitude);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing clock-in {PendingClockInId}", request.PendingClockInId);
                return StatusCode(500, new { message = "Clock-in finalization failed" });
            }
        }

        [HttpPost("clock-out")]
        public async Task<IActionResult> ClockOut([FromBody] ClockOutRequest request)
        {
            try
            {
                var result = await _geoFencingService.ClockOut(request.UserId, request.Latitude, request.Longitude);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clocking out user {UserId}", request.UserId);
                return StatusCode(500, new { message = "Clock-out failed" });
            }
        }

        [HttpPost("clock-in")]
        public async Task<IActionResult> ClockIn([FromBody] SimpleClockRequest request)
        {
            try
            {
                // Get user ID from token (simplified for testing)
                var userId = 3; // Mike Johnson's ID for testing
                
                var result = await _geoFencingService.InitiateClockIn(userId, request.Latitude, request.Longitude);
                
                if (result.Success)
                {
                    return Ok(new { success = true, message = "Clocked in successfully" });
                }
                
                return Ok(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock-in");
                return Ok(new { success = false, message = "Clock-in failed" });
            }
        }

        [HttpPost("clock-out")]
        public async Task<IActionResult> ClockOut([FromBody] SimpleClockRequest request)
        {
            try
            {
                // Get user ID from token (simplified for testing)
                var userId = 3; // Mike Johnson's ID for testing
                
                var result = await _geoFencingService.ClockOut(userId, request.Latitude, request.Longitude);
                
                if (result.Success)
                {
                    return Ok(new { success = true, message = "Clocked out successfully" });
                }
                
                return Ok(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock-out");
                return Ok(new { success = false, message = "Clock-out failed" });
            }
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentTimeEntry()
        {
            try
            {
                // Get user ID from token (simplified for testing)
                var userId = 3; // Mike Johnson's ID for testing
                
                var activeTimeEntry = await _context.TimeEntries
                    .Include(t => t.Job)
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.IsActive);

                if (activeTimeEntry != null)
                {
                    return Ok(new
                    {
                        clockInTime = activeTimeEntry.ClockInTime,
                        clockOutTime = (DateTime?)null,
                        jobName = activeTimeEntry.Job?.Name,
                        jobLocation = activeTimeEntry.Job?.Location
                    });
                }
                
                return Ok(new { });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current time entry");
                return Ok(new { });
            }
        }

        [HttpGet("status/{userId}")]
        public async Task<IActionResult> GetClockStatus(int userId)
        {
            try
            {
                var activeTimeEntry = await _context.TimeEntries
                    .Include(t => t.Job)
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.IsActive);

                var pendingClockIn = await _context.PendingClockIns
                    .Include(p => p.Job)
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive && p.ExpiresAt > DateTime.UtcNow);

                return Ok(new
                {
                    IsClockedIn = activeTimeEntry != null,
                    HasPendingClockIn = pendingClockIn != null,
                    ActiveTimeEntry = activeTimeEntry != null ? new
                    {
                        activeTimeEntry.Id,
                        activeTimeEntry.ClockInTime,
                        Job = new
                        {
                            activeTimeEntry.Job.JobNumber,
                            activeTimeEntry.Job.Name,
                            activeTimeEntry.Job.Location
                        }
                    } : null,
                    PendingClockIn = pendingClockIn != null ? new
                    {
                        pendingClockIn.Id,
                        pendingClockIn.InitiatedAt,
                        pendingClockIn.ExpiresAt,
                        Job = new
                        {
                            pendingClockIn.Job.JobNumber,
                            pendingClockIn.Job.Name,
                            pendingClockIn.Job.Location
                        }
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clock status for user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to get clock status" });
            }
        }

        [HttpGet("entries/{userId}")]
        public async Task<IActionResult> GetTimeEntries(int userId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var query = _context.TimeEntries
                    .Include(t => t.Job)
                    .Where(t => t.UserId == userId);

                if (startDate.HasValue)
                    query = query.Where(t => t.ClockInTime >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(t => t.ClockInTime <= endDate.Value);

                var entries = await query
                    .OrderByDescending(t => t.ClockInTime)
                    .Select(t => new
                    {
                        t.Id,
                        t.ClockInTime,
                        t.ClockOutTime,
                        t.TotalHours,
                        t.LocationVerified,
                        Job = new
                        {
                            t.Job.JobNumber,
                            t.Job.Name,
                            t.Job.Location
                        }
                    })
                    .ToListAsync();

                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time entries for user {UserId}", userId);
                return StatusCode(500, new { message = "Failed to get time entries" });
            }
        }
    }

    public class InitiateClockInRequest
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class FinalizeClockInRequest
    {
        public int PendingClockInId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ClockOutRequest
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class SimpleClockRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}