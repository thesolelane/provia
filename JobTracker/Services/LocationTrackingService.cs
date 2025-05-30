using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobTracker.Services
{
    public class LocationTrackingService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<LocationTrackingService> _logger;
        private readonly EmailService _emailService;
        private readonly SMSService _smsService;
        private readonly Timer _locationCheckTimer;

        public LocationTrackingService(
            JobTrackerContext context, 
            ILogger<LocationTrackingService> logger,
            EmailService emailService,
            SMSService smsService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _smsService = smsService;
            
            // Start location verification timer (runs every 5 minutes to check due verifications)
            _locationCheckTimer = new Timer(ProcessLocationVerifications, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        public async Task<bool> StartLocationTracking(int timeEntryId, int userId, int jobId, double latitude, double longitude)
        {
            try
            {
                var locationTracker = new LocationTracker
                {
                    TimeEntryId = timeEntryId,
                    UserId = userId,
                    JobId = jobId,
                    InitialLatitude = latitude,
                    InitialLongitude = longitude,
                    CurrentLatitude = latitude,
                    CurrentLongitude = longitude,
                    Status = LocationTrackerStatus.ClockedIn,
                    NextLocationCheckAt = DateTime.UtcNow.AddMinutes(20),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.LocationTrackers.Add(locationTracker);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Started location tracking for User {userId} at Job {jobId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start location tracking for User {userId}");
                return false;
            }
        }

        public async Task<bool> UpdateLocationStatus(int userId, LocationTrackerStatus status, int? newJobId = null)
        {
            try
            {
                var tracker = await _context.LocationTrackers
                    .FirstOrDefaultAsync(lt => lt.UserId == userId && lt.IsActive);

                if (tracker == null) return false;

                tracker.Status = status;
                tracker.StatusChangedAt = DateTime.UtcNow;

                // Set next check time based on status
                switch (status)
                {
                    case LocationTrackerStatus.LunchBreak:
                        tracker.NextLocationCheckAt = DateTime.UtcNow.AddHours(1);
                        break;
                    case LocationTrackerStatus.MaterialRun:
                        tracker.NextLocationCheckAt = DateTime.UtcNow.AddMinutes(15);
                        tracker.MaterialRunStartedAt = DateTime.UtcNow;
                        if (newJobId.HasValue) tracker.JobId = newJobId.Value;
                        break;
                    case LocationTrackerStatus.ClockedIn:
                        tracker.NextLocationCheckAt = DateTime.UtcNow.AddMinutes(20);
                        break;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update location status for User {userId}");
                return false;
            }
        }

        public async Task<bool> RecordLocationPing(int userId, double latitude, double longitude, double accuracy)
        {
            try
            {
                var tracker = await _context.LocationTrackers
                    .Include(lt => lt.Job)
                    .Include(lt => lt.User)
                    .FirstOrDefaultAsync(lt => lt.UserId == userId && lt.IsActive);

                if (tracker == null) return false;

                // Calculate distance from job site
                var distance = CalculateDistance(
                    latitude, longitude,
                    tracker.Job.Latitude ?? 0, tracker.Job.Longitude ?? 0
                );

                // Create location ping record
                var locationPing = new LocationPing
                {
                    LocationTrackerId = tracker.Id,
                    Latitude = latitude,
                    Longitude = longitude,
                    Accuracy = accuracy,
                    DistanceFromJobSite = distance,
                    PingTime = DateTime.UtcNow,
                    Status = tracker.Status
                };

                _context.LocationPings.Add(locationPing);

                // Update tracker current location
                tracker.CurrentLatitude = latitude;
                tracker.CurrentLongitude = longitude;
                tracker.LastLocationUpdate = DateTime.UtcNow;

                // Check if location is within allowed distance
                var allowedDistance = CalculateAllowedDistance(accuracy);
                var isWithinGeofence = distance <= allowedDistance;

                if (!isWithinGeofence)
                {
                    await HandleLocationViolation(tracker, distance, allowedDistance);
                }
                else
                {
                    // Reset violation count if back in range
                    tracker.ConsecutiveViolations = 0;
                    
                    // Set next check time based on current status
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
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to record location ping for User {userId}");
                return false;
            }
        }

        private async Task HandleLocationViolation(LocationTracker tracker, double distance, double allowedDistance)
        {
            tracker.ConsecutiveViolations++;
            tracker.LastViolationAt = DateTime.UtcNow;

            // Auto-logout after first violation for security
            if (tracker.ConsecutiveViolations >= 1)
            {
                await AutoLogoutUser(tracker);
                await SendLocationViolationNotifications(tracker, distance, allowedDistance);
            }
        }

        private async Task AutoLogoutUser(LocationTracker tracker)
        {
            try
            {
                // Find active time entry and clock out
                var timeEntry = await _context.TimeEntries
                    .FirstOrDefaultAsync(te => te.Id == tracker.TimeEntryId && te.ClockOutTime == null);

                if (timeEntry != null)
                {
                    timeEntry.ClockOutTime = DateTime.UtcNow;
                    timeEntry.AutoLogoutTime = DateTime.UtcNow;
                    timeEntry.Notes = $"Auto-logout: Location violation. Distance: {Math.Round(tracker.DistanceFromJobSite ?? 0)} feet from job site.";
                    
                    // Calculate total hours
                    var totalHours = (timeEntry.ClockOutTime.Value - timeEntry.ClockInTime).TotalHours;
                    timeEntry.TotalHours = Math.Round(totalHours, 2);
                }

                // Deactivate location tracker
                tracker.IsActive = false;
                tracker.AutoLoggedOutAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogWarning($"Auto-logged out User {tracker.UserId} due to location violation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to auto-logout User {tracker.UserId}");
            }
        }

        private async Task SendLocationViolationNotifications(LocationTracker tracker, double distance, double allowedDistance)
        {
            try
            {
                var user = await _context.Users.FindAsync(tracker.UserId);
                var job = await _context.Jobs.FindAsync(tracker.JobId);
                var company = await _context.Companies.FindAsync(user?.CompanyId);

                if (user == null || job == null) return;

                var message = $"Location Alert: You have been automatically clocked out. " +
                             $"You were {Math.Round(distance)} feet from {job.Name} (allowed: {allowedDistance} feet). " +
                             $"To clock back in, return to the job site or select a different location.";

                // Send SMS if phone number available
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    await _smsService.SendSMSAsync(user.PhoneNumber, message);
                }

                // Send email
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendLocationViolationEmailAsync(
                        user.Email, 
                        user.FirstName, 
                        job.Name, 
                        Math.Round(distance), 
                        allowedDistance
                    );
                }

                // Notify admins
                var admins = await _context.Users
                    .Where(u => u.CompanyId == user.CompanyId && 
                               (u.Role == UserRoles.Admin || u.Role == UserRoles.MasterAdmin))
                    .ToListAsync();

                foreach (var admin in admins)
                {
                    if (!string.IsNullOrEmpty(admin.Email))
                    {
                        await _emailService.SendAdminLocationAlertAsync(
                            admin.Email,
                            admin.FirstName,
                            $"{user.FirstName} {user.LastName}",
                            job.Name,
                            Math.Round(distance),
                            allowedDistance
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send location violation notifications");
            }
        }

        private async void ProcessLocationVerifications(object state)
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Find trackers that need location verification
                var trackersNeedingCheck = await _context.LocationTrackers
                    .Include(lt => lt.User)
                    .Include(lt => lt.Job)
                    .Where(lt => lt.IsActive && lt.NextLocationCheckAt <= now)
                    .ToListAsync();

                foreach (var tracker in trackersNeedingCheck)
                {
                    await RequestLocationFromUser(tracker);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing location verifications");
            }
        }

        private async Task RequestLocationFromUser(LocationTracker tracker)
        {
            try
            {
                // Check for material run timeout (1.25 hours)
                if (tracker.Status == LocationTrackerStatus.MaterialRun && 
                    tracker.MaterialRunStartedAt.HasValue &&
                    DateTime.UtcNow - tracker.MaterialRunStartedAt.Value > TimeSpan.FromMinutes(75))
                {
                    await SendMaterialRunTimeoutNotification(tracker);
                    return;
                }

                // Create location verification request
                var verificationRequest = new LocationVerificationRequest
                {
                    LocationTrackerId = tracker.Id,
                    UserId = tracker.UserId,
                    RequestedAt = DateTime.UtcNow,
                    ExpectedResponseTime = DateTime.UtcNow.AddMinutes(8),
                    Status = VerificationStatus.Pending,
                    RequiredStatus = tracker.Status
                };

                _context.LocationVerificationRequests.Add(verificationRequest);
                
                // Update next check time
                tracker.NextLocationCheckAt = DateTime.UtcNow.AddMinutes(8);
                
                await _context.SaveChangesAsync();

                // Send notification to user
                await SendLocationVerificationRequest(tracker, verificationRequest.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to request location from User {tracker.UserId}");
            }
        }

        private async Task SendLocationVerificationRequest(LocationTracker tracker, int verificationRequestId)
        {
            try
            {
                var user = await _context.Users.FindAsync(tracker.UserId);
                if (user == null) return;

                var message = $"Location Check Required: Please verify your location within 8 minutes. " +
                             $"Open the app and confirm your current location to stay clocked in.";

                // Send push notification through app (you can implement WebSocket or SignalR)
                // For now, send SMS if available
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    await _smsService.SendSMSAsync(user.PhoneNumber, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send location verification request");
            }
        }

        private async Task SendMaterialRunTimeoutNotification(LocationTracker tracker)
        {
            try
            {
                var user = await _context.Users.FindAsync(tracker.UserId);
                if (user == null) return;

                var message = $"Material Run Timeout: Please confirm your location immediately. " +
                             $"You have been on a material run for over 1.25 hours. " +
                             $"Confirm your location to continue or you will be automatically clocked out.";

                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    await _smsService.SendSMSAsync(user.PhoneNumber, message);
                }

                // Set urgent verification request
                tracker.NextLocationCheckAt = DateTime.UtcNow.AddMinutes(5);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send material run timeout notification");
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

        public void Dispose()
        {
            _locationCheckTimer?.Dispose();
        }
    }


}