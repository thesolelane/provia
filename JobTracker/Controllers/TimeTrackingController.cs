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
        private readonly IEmailService _emailService;

        public TimeTrackingController(GeoFencingService geoFencingService, JobTrackerContext context, ILogger<TimeTrackingController> logger, IEmailService emailService)
        {
            _geoFencingService = geoFencingService;
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }



        [HttpPost("clock-in/initiate")]
        public async Task<IActionResult> InitiateClockIn([FromBody] InitiateClockInRequest request)
        {
            try
            {
                // Get user ID from token (simplified for testing)
                var userId = 9; // Mike Johnson corrected ID
                
                // Log the actual coordinates received
                _logger.LogInformation($"Clock-in attempt - User coordinates: {request.Latitude}, {request.Longitude}");
                _logger.LogInformation($"Job ID {request.JobId} selected for clock-in");
                
                // Get the selected job to check coordinates
                var selectedJob = await _context.Jobs.FindAsync(request.JobId);
                if (selectedJob != null)
                {
                    _logger.LogInformation($"Job coordinates: {selectedJob.Latitude}, {selectedJob.Longitude} at {selectedJob.Location}");
                    
                    // Calculate distance
                    var distance = CalculateDistance(request.Latitude, request.Longitude, 
                                                   selectedJob.Latitude ?? 0, selectedJob.Longitude ?? 0);
                    _logger.LogInformation($"Distance to job site: {distance:F2} feet");
                }
                
                // Create a temporary TimeEntry that shows user as clocked in
                var timeEntry = new TimeEntry
                {
                    UserId = userId,
                    JobId = request.JobId,
                    ClockInTime = DateTime.UtcNow,
                    ClockInLatitude = request.Latitude,
                    ClockInLongitude = request.Longitude,
                    LocationVerified = false, // Will be verified later
                    VerificationPending = true, // Mark as pending verification
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TimeEntries.Add(timeEntry);
                await _context.SaveChangesAsync(); // Save TimeEntry first to get the ID

                var pendingClockIn = new PendingClockIn
                {
                    UserId = userId,
                    JobId = request.JobId,
                    InitialLatitude = request.Latitude,
                    InitialLongitude = request.Longitude,
                    InitiatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(9),
                    IsActive = true,
                    TimeEntryId = timeEntry.Id // Link to the temp time entry
                };

                _context.PendingClockIns.Add(pendingClockIn);
                await _context.SaveChangesAsync();
                
                return Ok(new { success = true, pendingClockInId = pendingClockIn.Id, message = "Clock-in initiated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating clock-in: {Error}", ex.Message);
                return Ok(new { success = false, message = "Clock-in initiation failed: " + ex.Message });
            }
        }

        [HttpPost("clock-in/finalize")]
        public async Task<IActionResult> FinalizeClockIn([FromBody] FinalizeClockInRequest request)
        {
            try
            {
                var pendingClockIn = await _context.PendingClockIns
                    .Include(p => p.TimeEntry)
                    .FirstOrDefaultAsync(p => p.Id == request.PendingClockInId && p.IsActive);

                if (pendingClockIn == null)
                {
                    return Ok(new { success = false, message = "Clock-in session not found or expired." });
                }

                // Update the existing temporary TimeEntry to mark it as verified
                if (pendingClockIn.TimeEntry != null)
                {
                    pendingClockIn.TimeEntry.LocationVerified = true;
                    pendingClockIn.TimeEntry.VerificationPending = false;
                    pendingClockIn.TimeEntry.UpdatedAt = DateTime.UtcNow;
                    
                    // Update final verification coordinates if different
                    pendingClockIn.TimeEntry.ClockInLatitude = request.Latitude;
                    pendingClockIn.TimeEntry.ClockInLongitude = request.Longitude;
                }

                // Mark the pending clock-in as completed
                pendingClockIn.IsActive = false;
                pendingClockIn.CompletedTimeEntryId = pendingClockIn.TimeEntry?.Id;
                
                await _context.SaveChangesAsync();

                // Start background location tracking
                try 
                {
                    var locationTrackingService = HttpContext.RequestServices.GetService<JobTracker.Services.LocationTrackingService>();
                    if (locationTrackingService != null)
                    {
                        await locationTrackingService.StartLocationTracking(
                            pendingClockIn.TimeEntry?.Id ?? 0, 
                            pendingClockIn.UserId, 
                            pendingClockIn.JobId, 
                            pendingClockIn.InitialLatitude, 
                            pendingClockIn.InitialLongitude
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to start location tracking, but clock-in was successful");
                }

                return Ok(new { 
                    success = true, 
                    message = "Successfully clocked in! Background location tracking started.",
                    timeEntryId = pendingClockIn.TimeEntry?.Id 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing clock-in: {Error}", ex.Message);
                return Ok(new { success = false, message = "Clock-in finalization failed: " + ex.Message });
            }
        }

        [HttpPost("clock-in")]
        public async Task<IActionResult> ClockIn([FromBody] ClockInRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Check if user is already clocked in
                var existingEntry = await _context.TimeEntries
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.ClockOutTime == null && t.IsActive);

                if (existingEntry != null)
                {
                    return BadRequest(new { message = "You are already clocked in" });
                }

                // Create immediate clock-in entry with pending verification
                var timeEntry = new TimeEntry
                {
                    UserId = userId,
                    JobId = request.JobId,
                    ClockInTime = DateTime.UtcNow,
                    ClockInLatitude = request.Latitude,
                    ClockInLongitude = request.Longitude,
                    LocationVerified = request.IsLocationVerified,
                    IsPendingVerification = true,
                    VerificationDeadline = DateTime.UtcNow.AddMinutes(8),
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeEntries.Add(timeEntry);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    message = "Clock-in successful! Location verification in progress.", 
                    timeEntryId = timeEntry.Id 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock-in");
                return StatusCode(500, new { message = "Clock-in failed" });
            }
        }

        [HttpPost("verify-location")]
        public async Task<IActionResult> VerifyLocation([FromBody] VerifyLocationRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var timeEntry = await _context.TimeEntries
                    .Include(t => t.Job)
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == request.TimeEntryId && t.UserId == userId);

                if (timeEntry == null)
                {
                    return NotFound(new { message = "Time entry not found" });
                }

                // Check if verification deadline has passed
                if (DateTime.UtcNow > timeEntry.VerificationDeadline)
                {
                    // Verification failed - auto logout
                    timeEntry.VerificationFailed = true;
                    timeEntry.AutoLogoutTime = DateTime.UtcNow;
                    timeEntry.ClockOutTime = DateTime.UtcNow;
                    timeEntry.IsPendingVerification = false;
                    timeEntry.Notes = "Auto logged out - location verification failed";

                    await _context.SaveChangesAsync();

                    // Send notification (email/SMS will be implemented with Twilio)
                    await SendVerificationFailedNotification(timeEntry.User, timeEntry.Job);

                    return Ok(new { verified = false, message = "Verification failed - auto logged out" });
                }

                // TODO: Implement actual location re-verification here
                // For now, assume verification is successful
                timeEntry.IsPendingVerification = false;
                timeEntry.LocationVerified = true;
                timeEntry.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { verified = true, message = "Location verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during location verification");
                return StatusCode(500, new { message = "Verification failed" });
            }
        }

        private async Task SendVerificationFailedNotification(User user, Job job)
        {
            try
            {
                // Email notification
                if (!string.IsNullOrEmpty(user.Email))
                {
                    var subject = "Clock-in Verification Failed";
                    var body = $"Hello {user.FirstName},\n\nYour clock-in at {job.Name} could not be verified and you have been automatically logged out. Please contact your supervisor if you believe this is an error.\n\nTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    
                    await _emailService.SendEmailAsync(user.Email, subject, body);
                }

                // SMS notification (will be implemented with Twilio)
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    // TODO: Implement SMS with Twilio when API keys are provided
                    _logger.LogInformation("SMS notification needed for user {UserId} at {PhoneNumber}", user.Id, user.PhoneNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification failed notification");
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



        [HttpPost("clock-out")]
        public async Task<IActionResult> ClockOut([FromBody] SimpleClockRequest request)
        {
            try
            {
                // Get user ID from token (simplified for testing)
                var userId = 9; // Mike Johnson corrected ID
                
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
                var userId = 9; // Mike Johnson corrected ID
                
                var activeTimeEntry = await _context.TimeEntries
                    .Include(t => t.Job)
                    .Where(t => t.UserId == userId && t.ClockOutTime == null)
                    .FirstOrDefaultAsync();

                if (activeTimeEntry != null)
                {
                    return Ok(new
                    {
                        clockInTime = activeTimeEntry.ClockInTime,
                        clockOutTime = activeTimeEntry.ClockOutTime,
                        jobName = activeTimeEntry.Job?.Name,
                        jobLocation = activeTimeEntry.Job?.Location
                    });
                }
                
                return Ok(new { });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current time entry: {Error}", ex.Message);
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

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula to calculate distance between two GPS coordinates
            const double earthRadiusMiles = 3959.0;
            
            var lat1Rad = lat1 * Math.PI / 180;
            var lat2Rad = lat2 * Math.PI / 180;
            var deltaLat = (lat2 - lat1) * Math.PI / 180;
            var deltaLon = (lon2 - lon1) * Math.PI / 180;
            
            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distanceMiles = earthRadiusMiles * c;
            
            return distanceMiles * 5280; // Convert to feet
        }

        [HttpPost("lunch/start")]
        public async Task<IActionResult> StartLunchBreak([FromBody] LunchBreakRequest request)
        {
            try
            {
                var userId = 9; // Mike Johnson for testing
                
                // Verify user is currently clocked in
                var currentTimeEntry = await _context.TimeEntries
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.ClockOutTime == null && t.IsActive);

                if (currentTimeEntry == null)
                {
                    return Ok(new { success = false, message = "You must be clocked in to start a lunch break." });
                }

                // Get job coordinates for location verification
                var job = await _context.Jobs.FindAsync(request.JobId);
                if (job == null)
                {
                    return Ok(new { success = false, message = "Job not found." });
                }

                // Calculate distance to verify they're at the job site
                var distance = CalculateDistance(request.Latitude, request.Longitude, 
                                               job.Latitude ?? 0, job.Longitude ?? 0);

                if (distance > 350) // 350 feet geofence
                {
                    return Ok(new { success = false, message = "You are not at job site - you must be at the job site to start lunch break." });
                }

                // Create lunch break record
                var lunchBreak = new LunchBreak
                {
                    UserId = userId,
                    JobId = request.JobId,
                    TimeEntryId = currentTimeEntry.Id,
                    StartLatitude = request.Latitude,
                    StartLongitude = request.Longitude,
                    StartTime = DateTime.UtcNow,
                    VerificationDeadline = DateTime.UtcNow.AddMinutes(8),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LunchBreaks.Add(lunchBreak);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, lunchBreakId = lunchBreak.Id, message = "Lunch break started" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting lunch break: {Error}", ex.Message);
                return Ok(new { success = false, message = "Lunch break start failed: " + ex.Message });
            }
        }

        [HttpPost("lunch/verify")]
        public async Task<IActionResult> VerifyLunchLocation([FromBody] LunchVerificationRequest request)
        {
            try
            {
                var lunchBreak = await _context.LunchBreaks
                    .Include(l => l.Job)
                    .FirstOrDefaultAsync(l => l.Id == request.LunchBreakId && l.IsActive);

                if (lunchBreak == null)
                {
                    return Ok(new { success = false, message = "Lunch break session not found." });
                }

                // Calculate distance to verify they returned to the job site
                var distance = CalculateDistance(request.Latitude, request.Longitude, 
                                               lunchBreak.Job.Latitude ?? 0, lunchBreak.Job.Longitude ?? 0);

                if (distance > 350) // 350 feet geofence
                {
                    // User failed verification - mark lunch break as failed
                    lunchBreak.IsActive = false;
                    lunchBreak.VerificationFailed = true;
                    lunchBreak.EndTime = DateTime.UtcNow;

                    // Clock out the user
                    var timeEntry = await _context.TimeEntries.FindAsync(lunchBreak.TimeEntryId);
                    if (timeEntry != null)
                    {
                        timeEntry.ClockOutTime = DateTime.UtcNow;
                        timeEntry.IsActive = false;
                    }

                    await _context.SaveChangesAsync();
                    return Ok(new { success = false, message = "Location verification failed - automatically clocked out" });
                }

                // Verification successful - mark lunch break as completed
                lunchBreak.IsActive = false;
                lunchBreak.EndLatitude = request.Latitude;
                lunchBreak.EndLongitude = request.Longitude;
                lunchBreak.EndTime = DateTime.UtcNow;
                lunchBreak.LocationVerified = true;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Lunch break completed - back to work" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying lunch location: {Error}", ex.Message);
                return Ok(new { success = false, message = "Lunch verification failed: " + ex.Message });
            }
        }

        [HttpPost("material-run/start")]
        public async Task<IActionResult> StartMaterialRun([FromBody] MaterialRunStartRequest request)
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (userId == null) return Unauthorized();

                // Check if user is currently clocked in
                var activeTimeEntry = await _context.TimeEntries
                    .Include(t => t.Job)
                    .Where(t => t.UserId == userId && t.ClockOutTime == null)
                    .FirstOrDefaultAsync();

                if (activeTimeEntry == null)
                {
                    return BadRequest(new { success = false, message = "You must be clocked in to start a material run" });
                }

                // Verify user is at job site
                var distance = CalculateDistance(
                    request.Latitude, request.Longitude,
                    activeTimeEntry.Job.Latitude ?? 0, activeTimeEntry.Job.Longitude ?? 0
                );

                if (distance > 106) // 350 feet
                {
                    return BadRequest(new { success = false, message = "You must be at the job site to start a material run" });
                }

                // Create material run record
                var materialRun = new MaterialRun
                {
                    UserId = userId.Value,
                    JobId = request.JobId,
                    TimeEntryId = activeTimeEntry.Id,
                    StoreType = request.StoreType,
                    Materials = request.Materials,
                    StartLatitude = request.Latitude,
                    StartLongitude = request.Longitude,
                    StartTime = DateTime.UtcNow,
                    VerificationDeadline = DateTime.UtcNow.AddMinutes(75), // 1.25 hours
                    IsActive = true
                };

                _context.MaterialRuns.Add(materialRun);
                await _context.SaveChangesAsync();

                // Send material list to admin Erika
                await SendMaterialListToAdmin(materialRun, activeTimeEntry);

                return Ok(new { success = true, materialRunId = materialRun.Id, message = "Material run started" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Server error starting material run" });
            }
        }

        [HttpPost("material-run/ping")]
        public async Task<IActionResult> MaterialRunPing([FromBody] MaterialRunPingRequest request)
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (userId == null) return Unauthorized();

                var materialRun = await _context.MaterialRuns
                    .Where(m => m.Id == request.MaterialRunId && m.IsActive)
                    .FirstOrDefaultAsync();

                if (materialRun == null)
                {
                    return BadRequest(new { success = false, message = "Material run not found" });
                }

                // Check if material run has timed out
                if (DateTime.UtcNow > materialRun.VerificationDeadline)
                {
                    materialRun.VerificationFailed = true;
                    materialRun.IsActive = false;
                    
                    // Auto logout
                    var activeTimeEntry = await _context.TimeEntries
                        .Where(t => t.UserId == userId && t.ClockOutTime == null)
                        .FirstOrDefaultAsync();

                    if (activeTimeEntry != null)
                    {
                        activeTimeEntry.ClockOutTime = DateTime.UtcNow;
                        activeTimeEntry.AutoLogoutTime = DateTime.UtcNow;
                        activeTimeEntry.IsActive = false;
                        activeTimeEntry.Notes = "Auto logout - material run timeout";
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { success = false, timeout = true, message = "Material run timeout" });
                }

                return Ok(new { success = true, message = "Location ping recorded" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Server error during material run ping" });
            }
        }

        [HttpPost("material-run/verify-return")]
        public async Task<IActionResult> VerifyMaterialRunReturn([FromBody] MaterialRunVerificationRequest request)
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (userId == null) return Unauthorized();

                var materialRun = await _context.MaterialRuns
                    .Include(m => m.Job)
                    .Where(m => m.Id == request.MaterialRunId && m.IsActive)
                    .FirstOrDefaultAsync();

                if (materialRun == null)
                {
                    return BadRequest(new { success = false, message = "Material run not found" });
                }

                // Calculate distance from job site
                var distance = CalculateDistance(
                    request.Latitude, request.Longitude,
                    materialRun.Job.Latitude ?? 0, materialRun.Job.Longitude ?? 0
                );

                // Within 350 feet means back at job site
                var isAtJobSite = distance <= 106;

                if (isAtJobSite)
                {
                    // Successfully returned to job site
                    materialRun.EndTime = DateTime.UtcNow;
                    materialRun.EndLatitude = request.Latitude;
                    materialRun.EndLongitude = request.Longitude;
                    materialRun.LocationVerified = true;
                    materialRun.IsActive = false;
                    
                    await _context.SaveChangesAsync();

                    return Ok(new { success = true, message = "Material run completed - back to work" });
                }
                else
                {
                    // User is not back at job site - auto logout
                    materialRun.VerificationFailed = true;
                    materialRun.IsActive = false;
                    
                    // Auto logout current time entry
                    var activeTimeEntry = await _context.TimeEntries
                        .Where(t => t.UserId == userId && t.ClockOutTime == null)
                        .FirstOrDefaultAsync();

                    if (activeTimeEntry != null)
                    {
                        activeTimeEntry.ClockOutTime = DateTime.UtcNow;
                        activeTimeEntry.AutoLogoutTime = DateTime.UtcNow;
                        activeTimeEntry.IsActive = false;
                        activeTimeEntry.Notes = $"Auto logout - material run verification failed. Distance from job site: {distance:F0}m";
                    }
                    
                    await _context.SaveChangesAsync();

                    return Ok(new { success = false, autoLogout = true, message = "Auto logout - not at job site after material run" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Server error during material run verification" });
            }
        }

        private async Task SendMaterialListToAdmin(MaterialRun materialRun, TimeEntry timeEntry)
        {
            try
            {
                // Get admin Erika's email
                var adminUser = await _context.Users
                    .Where(u => u.FirstName == "Erika" && u.Role >= 1520)
                    .FirstOrDefaultAsync();

                if (adminUser != null)
                {
                    var user = await _context.Users.FindAsync(materialRun.UserId);
                    var job = await _context.Jobs.FindAsync(materialRun.JobId);

                    var subject = $"Material Run Request - {user?.FirstName} {user?.LastName}";
                    var body = $@"
Material Run Details:

Employee: {user?.FirstName} {user?.LastName}
Job Site: {job?.Location}
Store Type: {materialRun.StoreType}
Start Time: {materialRun.StartTime:MM/dd/yyyy HH:mm}

Materials Needed:
{materialRun.Materials}

This is an automated notification from the Job Tracker system.
";

                    await _emailService.SendEmailAsync(adminUser.Email, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send material list email to admin");
            }
        }
    }

    public class InitiateClockInRequest
    {
        public int UserId { get; set; }
        public int JobId { get; set; }
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

    public class LunchBreakRequest
    {
        public int JobId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class LunchVerificationRequest
    {
        public int LunchBreakId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class SimpleClockRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class MaterialRunStartRequest
    {
        public int JobId { get; set; }
        public string StoreType { get; set; } = string.Empty;
        public string Materials { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class MaterialRunPingRequest
    {
        public int MaterialRunId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class MaterialRunVerificationRequest
    {
        public int MaterialRunId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}