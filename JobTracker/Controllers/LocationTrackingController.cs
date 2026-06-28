using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobTracker.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LocationTrackingController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<LocationTrackingController> _logger;
        private readonly LocationTrackingService _locationTrackingService;

        public LocationTrackingController(
            JobTrackerContext context,
            ILogger<LocationTrackingController> logger,
            LocationTrackingService locationTrackingService)
        {
            _context = context;
            _logger = logger;
            _locationTrackingService = locationTrackingService;
        }

        [HttpPost("ping-location")]
        public async Task<IActionResult> PingLocation([FromBody] LocationPingRequest request)
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var parts = token.Split('-');
                var userId = int.Parse(parts[0]);

                var success = await _locationTrackingService.RecordLocationPing(
                    userId, 
                    request.Latitude, 
                    request.Longitude, 
                    request.Accuracy
                );

                if (success)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Location recorded successfully",
                        nextCheckIn = DateTime.UtcNow.AddMinutes(20)
                    });
                }

                return BadRequest(new { success = false, message = "Failed to record location" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pinging location");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateLocationStatus([FromBody] UpdateStatusRequest request)
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var parts = token.Split('-');
                var userId = int.Parse(parts[0]);

                var success = await _locationTrackingService.UpdateLocationStatus(
                    userId, 
                    request.Status, 
                    request.NewJobId
                );

                if (success)
                {
                    string message = request.Status switch
                    {
                        LocationTrackerStatus.LunchBreak => "Lunch break started. Location will be checked in 1 hour.",
                        LocationTrackerStatus.MaterialRun => "Material run started. Location will be checked every 15 minutes.",
                        LocationTrackerStatus.ClockedIn => "Status updated to clocked in. Regular 20-minute location checks resumed.",
                        _ => "Status updated successfully"
                    };

                    return Ok(new { 
                        success = true, 
                        message = message,
                        newStatus = request.Status.ToString()
                    });
                }

                return BadRequest(new { success = false, message = "Failed to update status" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location status");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("current-status")]
        public async Task<IActionResult> GetCurrentLocationStatus()
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var parts = token.Split('-');
                var userId = int.Parse(parts[0]);

                var tracker = await _context.LocationTrackers
                    .Include(lt => lt.Job)
                    .Include(lt => lt.User)
                    .FirstOrDefaultAsync(lt => lt.UserId == userId && lt.IsActive);

                if (tracker == null)
                {
                    return Ok(new { 
                        success = true, 
                        status = "ClockedOut",
                        message = "No active location tracking"
                    });
                }

                return Ok(new
                {
                    success = true,
                    status = tracker.Status.ToString(),
                    jobName = tracker.Job.Name,
                    nextLocationCheck = tracker.NextLocationCheckAt,
                    lastUpdate = tracker.LastLocationUpdate,
                    consecutiveViolations = tracker.ConsecutiveViolations,
                    isActive = tracker.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location status");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("respond-verification")]
        public async Task<IActionResult> RespondToVerification([FromBody] VerificationResponseRequest request)
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var parts = token.Split('-');
                var userId = int.Parse(parts[0]);

                var verificationRequest = await _context.LocationVerificationRequests
                    .Include(vr => vr.LocationTracker)
                    .ThenInclude(lt => lt.Job)
                    .FirstOrDefaultAsync(vr => vr.Id == request.VerificationRequestId && vr.UserId == userId);

                if (verificationRequest == null)
                {
                    return NotFound(new { success = false, message = "Verification request not found" });
                }

                if (verificationRequest.Status != VerificationStatus.Pending)
                {
                    return BadRequest(new { success = false, message = "Verification request already processed" });
                }

                // Check if response is within time limit
                if (DateTime.UtcNow > verificationRequest.ExpectedResponseTime)
                {
                    verificationRequest.Status = VerificationStatus.Expired;
                    await _context.SaveChangesAsync();
                    return BadRequest(new { success = false, message = "Verification request expired" });
                }

                // Calculate distance from required location
                var job = verificationRequest.LocationTracker.Job;
                var distance = CalculateDistance(
                    request.Latitude, request.Longitude,
                    job.Latitude ?? 0, job.Longitude ?? 0
                );

                var allowedDistance = CalculateAllowedDistance(request.Accuracy);
                var isWithinGeofence = distance <= allowedDistance;

                // Update verification request
                verificationRequest.RespondedAt = DateTime.UtcNow;
                verificationRequest.ResponseLatitude = request.Latitude;
                verificationRequest.ResponseLongitude = request.Longitude;
                verificationRequest.ResponseAccuracy = request.Accuracy;
                verificationRequest.DistanceFromRequired = distance;
                verificationRequest.IsWithinGeofence = isWithinGeofence;
                verificationRequest.Status = isWithinGeofence ? VerificationStatus.Completed : VerificationStatus.Failed;

                await _context.SaveChangesAsync();

                if (isWithinGeofence)
                {
                    // Update location tracker
                    var tracker = verificationRequest.LocationTracker;
                    tracker.CurrentLatitude = request.Latitude;
                    tracker.CurrentLongitude = request.Longitude;
                    tracker.LastLocationUpdate = DateTime.UtcNow;
                    tracker.ConsecutiveViolations = 0;

                    // Set next check time based on status
                    switch (tracker.Status)
                    {
                        case LocationTrackerStatus.LunchBreak:
                            tracker.NextLocationCheckAt = DateTime.UtcNow.AddHours(1);
                            break;
                        case LocationTrackerStatus.MaterialRun:
                            tracker.NextLocationCheckAt = DateTime.UtcNow.AddMinutes(15);
                            break;
                        default:
                            tracker.NextLocationCheckAt = DateTime.UtcNow.AddMinutes(20);
                            break;
                    }

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = "Location verified successfully",
                        status = tracker.Status.ToString(),
                        nextCheck = tracker.NextLocationCheckAt
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Location verification failed. You are {Math.Round(distance)} feet from the job site (allowed: {allowedDistance} feet)",
                        distance = Math.Round(distance),
                        allowedDistance = allowedDistance
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to verification");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("pending-verifications")]
        public async Task<IActionResult> GetPendingVerifications()
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var parts = token.Split('-');
                var userId = int.Parse(parts[0]);

                var pendingVerifications = await _context.LocationVerificationRequests
                    .Include(vr => vr.LocationTracker)
                    .ThenInclude(lt => lt.Job)
                    .Where(vr => vr.UserId == userId && vr.Status == VerificationStatus.Pending)
                    .OrderBy(vr => vr.ExpectedResponseTime)
                    .ToListAsync();

                var result = pendingVerifications.Select(vr => new
                {
                    id = vr.Id,
                    requestedAt = vr.RequestedAt,
                    expectedResponseTime = vr.ExpectedResponseTime,
                    timeRemaining = (vr.ExpectedResponseTime - DateTime.UtcNow).TotalMinutes,
                    jobName = vr.LocationTracker.Job.Name,
                    requiredStatus = vr.RequiredStatus.ToString()
                });

                return Ok(new { success = true, verifications = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending verifications");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000; // Earth's radius in meters
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLng = (lng2 - lng1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distanceMeters = R * c;
            return distanceMeters * 3.28084; // Convert to feet
        }

        private double CalculateAllowedDistance(double accuracyMeters)
        {
            var accuracyFeet = accuracyMeters * 3.28084;
            var baseDistance = 350.0; // Base geofence radius
            
            // Expand geofence for poor GPS accuracy
            if (accuracyFeet > 50)
            {
                return Math.Min(baseDistance + (accuracyFeet * 2), 1000); // Max 1000 feet
            }
            
            return baseDistance;
        }
    }

    public class LocationPingRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
    }

    public class UpdateStatusRequest
    {
        public LocationTrackerStatus Status { get; set; }
        public int? NewJobId { get; set; }
    }

    public class VerificationResponseRequest
    {
        public int VerificationRequestId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
    }
}