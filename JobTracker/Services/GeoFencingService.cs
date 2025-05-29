using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;

namespace JobTracker.Services
{
    public class GeoFencingService
    {
        private readonly JobTrackerContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<GeoFencingService> _logger;
        private const double ALLOWED_DISTANCE_FEET = 200.0;

        public GeoFencingService(JobTrackerContext context, IEmailService emailService, ILogger<GeoFencingService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ClockInResult> InitiateClockIn(int userId, double latitude, double longitude)
        {
            try
            {
                // Get user's assigned jobs
                var userJobs = await GetUserActiveJobs(userId);
                
                if (!userJobs.Any())
                {
                    return new ClockInResult 
                    { 
                        Success = false, 
                        Message = "No active jobs assigned. Please contact your supervisor." 
                    };
                }

                // Check if user is within range of any job site
                var nearbyJob = await FindNearbyJobSite(userJobs, latitude, longitude);
                
                if (nearbyJob == null)
                {
                    return new ClockInResult 
                    { 
                        Success = false, 
                        Message = "You must be within 200 feet of a job site to clock in." 
                    };
                }

                // Create pending clock-in record
                var pendingClockIn = new PendingClockIn
                {
                    UserId = userId,
                    JobId = nearbyJob.Id,
                    InitialLatitude = latitude,
                    InitialLongitude = longitude,
                    InitiatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(9),
                    IsActive = true
                };

                _context.PendingClockIns.Add(pendingClockIn);
                await _context.SaveChangesAsync();

                return new ClockInResult 
                { 
                    Success = true, 
                    Message = "Clock-in initiated. Please stay at the job site for verification.",
                    PendingClockInId = pendingClockIn.Id,
                    JobSite = nearbyJob.Location
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating clock-in for user {UserId}", userId);
                return new ClockInResult 
                { 
                    Success = false, 
                    Message = "Clock-in failed. Please try again." 
                };
            }
        }

        public async Task<ClockInResult> FinalizeClockIn(int pendingClockInId, double latitude, double longitude)
        {
            try
            {
                var pendingClockIn = await _context.PendingClockIns
                    .Include(p => p.User)
                    .Include(p => p.Job)
                    .FirstOrDefaultAsync(p => p.Id == pendingClockInId && p.IsActive);

                if (pendingClockIn == null)
                {
                    return new ClockInResult 
                    { 
                        Success = false, 
                        Message = "Clock-in session not found or expired." 
                    };
                }

                if (pendingClockIn.ExpiresAt < DateTime.UtcNow)
                {
                    pendingClockIn.IsActive = false;
                    await _context.SaveChangesAsync();
                    
                    return new ClockInResult 
                    { 
                        Success = false, 
                        Message = "Clock-in verification expired. Please start over." 
                    };
                }

                // Calculate distance between initial and final GPS positions
                var distance = CalculateDistance(
                    pendingClockIn.InitialLatitude, 
                    pendingClockIn.InitialLongitude,
                    latitude, 
                    longitude);

                if (distance > ALLOWED_DISTANCE_FEET)
                {
                    // Send email notification
                    await SendLocationVerificationFailureEmail(pendingClockIn.User, pendingClockIn.Job, distance);
                    
                    pendingClockIn.IsActive = false;
                    await _context.SaveChangesAsync();

                    return new ClockInResult 
                    { 
                        Success = false, 
                        Message = $"Location verification failed. You moved {distance:F0} feet from initial position." 
                    };
                }

                // Create successful time entry
                var timeEntry = new TimeEntry
                {
                    UserId = pendingClockIn.UserId,
                    JobId = pendingClockIn.JobId,
                    ClockInTime = pendingClockIn.InitiatedAt,
                    ClockInLatitude = pendingClockIn.InitialLatitude,
                    ClockInLongitude = pendingClockIn.InitialLongitude,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeEntries.Add(timeEntry);
                pendingClockIn.IsActive = false;
                pendingClockIn.CompletedTimeEntryId = timeEntry.Id;
                
                await _context.SaveChangesAsync();

                return new ClockInResult 
                { 
                    Success = true, 
                    Message = "Successfully clocked in!",
                    TimeEntryId = timeEntry.Id 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing clock-in {PendingClockInId}", pendingClockInId);
                return new ClockInResult 
                { 
                    Success = false, 
                    Message = "Clock-in finalization failed. Please try again." 
                };
            }
        }

        public async Task<ClockOutResult> ClockOut(int userId, double latitude, double longitude)
        {
            try
            {
                var activeTimeEntry = await _context.TimeEntries
                    .Include(t => t.Job)
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.IsActive);

                if (activeTimeEntry == null)
                {
                    return new ClockOutResult 
                    { 
                        Success = false, 
                        Message = "No active clock-in found." 
                    };
                }

                // Verify user is still at job site for clock-out
                var distance = CalculateDistance(
                    activeTimeEntry.ClockInLatitude ?? 0,
                    activeTimeEntry.ClockInLongitude ?? 0,
                    latitude,
                    longitude);

                activeTimeEntry.ClockOutTime = DateTime.UtcNow;
                activeTimeEntry.ClockOutLatitude = latitude;
                activeTimeEntry.ClockOutLongitude = longitude;
                activeTimeEntry.IsActive = false;
                activeTimeEntry.LocationVerified = distance <= ALLOWED_DISTANCE_FEET;

                var totalHours = (activeTimeEntry.ClockOutTime.Value - activeTimeEntry.ClockInTime).TotalHours;
                activeTimeEntry.TotalHours = (decimal)totalHours;

                await _context.SaveChangesAsync();

                return new ClockOutResult 
                { 
                    Success = true, 
                    Message = "Successfully clocked out!",
                    TotalHours = totalHours,
                    LocationVerified = activeTimeEntry.LocationVerified 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clocking out user {UserId}", userId);
                return new ClockOutResult 
                { 
                    Success = false, 
                    Message = "Clock-out failed. Please try again." 
                };
            }
        }

        private async Task<List<Job>> GetUserActiveJobs(int userId)
        {
            return await _context.UserJobAssignments
                .Where(a => a.UserId == userId && a.IsActive)
                .Include(a => a.Job)
                .Select(a => a.Job)
                .Where(j => j.Status != "Completed" && j.Status != "Cancelled")
                .ToListAsync();
        }

        private async Task<Job?> FindNearbyJobSite(List<Job> jobs, double userLat, double userLon)
        {
            foreach (var job in jobs)
            {
                if (job.Latitude.HasValue && job.Longitude.HasValue)
                {
                    var distance = CalculateDistance(userLat, userLon, job.Latitude.Value, job.Longitude.Value);
                    if (distance <= ALLOWED_DISTANCE_FEET)
                    {
                        return job;
                    }
                }
            }
            return null;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusMiles = 3959;
            const double feetPerMile = 5280;

            var lat1Rad = DegreesToRadians(lat1);
            var lat2Rad = DegreesToRadians(lat2);
            var deltaLatRad = DegreesToRadians(lat2 - lat1);
            var deltaLonRad = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distanceMiles = earthRadiusMiles * c;

            return distanceMiles * feetPerMile;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        private async Task SendLocationVerificationFailureEmail(User user, Job job, double distance)
        {
            try
            {
                var subject = "Clock-in Location Verification Failed";
                var body = $@"
                    <h3>Clock-in Verification Failed</h3>
                    <p>Dear {user.FirstName} {user.LastName},</p>
                    <p>Your clock-in attempt for job <strong>{job.JobNumber}</strong> was not successful due to location verification failure.</p>
                    <p><strong>Details:</strong></p>
                    <ul>
                        <li>Job Site: {job.Location}</li>
                        <li>Distance moved during verification: {distance:F0} feet</li>
                        <li>Maximum allowed distance: {ALLOWED_DISTANCE_FEET} feet</li>
                        <li>Time: {DateTime.Now:MMM dd, yyyy h:mm tt}</li>
                    </ul>
                    <p>You are <strong>NOT</strong> clocked in for this job. Please ensure you remain at the job site during the verification process and try again.</p>
                    <p>If you believe this is an error, please contact your supervisor immediately.</p>
                ";

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send location verification failure email to {Email}", user.Email);
            }
        }
    }

    public class ClockInResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? PendingClockInId { get; set; }
        public int? TimeEntryId { get; set; }
        public string? JobSite { get; set; }
    }

    public class ClockOutResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double TotalHours { get; set; }
        public bool LocationVerified { get; set; }
    }
}