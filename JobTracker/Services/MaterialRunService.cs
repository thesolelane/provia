using JobTracker.Models;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Services
{
    public class MaterialRunService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<MaterialRunService> _logger;
        
        // Headquarters location: 37 Duck Mill Rd Fitchburg MA 01420
        private const double HQ_LATITUDE = 42.5617;
        private const double HQ_LONGITUDE = -71.8028;

        public MaterialRunService(JobTrackerContext context, ILogger<MaterialRunService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MaterialRunResult> StartMaterialRun(int userId, int jobId, int materialStoreId, string purpose, double currentLatitude, double currentLongitude)
        {
            try
            {
                // Verify user is assigned to the job
                var userAssignment = await _context.UserJobAssignments
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.JobId == jobId && ua.IsActive);

                if (userAssignment == null)
                {
                    return new MaterialRunResult 
                    { 
                        Success = false, 
                        Message = "You are not assigned to this job" 
                    };
                }

                // Get job and store information
                var job = await _context.Jobs.FindAsync(jobId);
                var store = await _context.MaterialStores.FindAsync(materialStoreId);

                if (job == null || store == null)
                {
                    return new MaterialRunResult 
                    { 
                        Success = false, 
                        Message = "Job or store not found" 
                    };
                }

                // Create new material run
                var materialRun = new MaterialRun
                {
                    UserId = userId,
                    JobId = jobId,
                    MaterialStoreId = materialStoreId,
                    Purpose = purpose,
                    DepartureTime = DateTime.UtcNow,
                    DepartureLatitude = currentLatitude,
                    DepartureLongitude = currentLongitude,
                    LocationVerified = IsAtJobSite(currentLatitude, currentLongitude, job),
                    CreatedAt = DateTime.UtcNow
                };

                _context.MaterialRuns.Add(materialRun);
                await _context.SaveChangesAsync();

                return new MaterialRunResult 
                { 
                    Success = true, 
                    Message = $"Material run started to {store.StoreName}",
                    MaterialRunId = materialRun.Id,
                    EstimatedTravelTime = CalculateTravelTime(currentLatitude, currentLongitude, store.Latitude, store.Longitude)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting material run for user {UserId}", userId);
                return new MaterialRunResult 
                { 
                    Success = false, 
                    Message = "Failed to start material run" 
                };
            }
        }

        public async Task<MaterialRunResult> ArriveAtStore(int materialRunId, double currentLatitude, double currentLongitude)
        {
            try
            {
                var materialRun = await _context.MaterialRuns
                    .Include(mr => mr.MaterialStore)
                    .FirstOrDefaultAsync(mr => mr.Id == materialRunId);

                if (materialRun == null)
                {
                    return new MaterialRunResult 
                    { 
                        Success = false, 
                        Message = "Material run not found" 
                    };
                }

                // Verify location is near the store (within 500 feet)
                var distanceToStore = CalculateDistanceInFeet(
                    currentLatitude, currentLongitude,
                    materialRun.MaterialStore.Latitude, materialRun.MaterialStore.Longitude);

                if (distanceToStore > 500)
                {
                    return new MaterialRunResult 
                    { 
                        Success = false, 
                        Message = $"You must be within 500 feet of {materialRun.MaterialStore.StoreName} to check in" 
                    };
                }

                materialRun.ArrivalAtStoreTime = DateTime.UtcNow;
                materialRun.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new MaterialRunResult 
                { 
                    Success = true, 
                    Message = $"Arrived at {materialRun.MaterialStore.StoreName}" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording store arrival for material run {MaterialRunId}", materialRunId);
                return new MaterialRunResult 
                { 
                    Success = false, 
                    Message = "Failed to record store arrival" 
                };
            }
        }

        public async Task<MaterialRunResult> DepartFromStore(int materialRunId, double currentLatitude, double currentLongitude)
        {
            try
            {
                var materialRun = await _context.MaterialRuns
                    .Include(mr => mr.MaterialStore)
                    .FirstOrDefaultAsync(mr => mr.Id == materialRunId);

                if (materialRun == null)
                {
                    return new MaterialRunResult 
                    { 
                        Success = false, 
                        Message = "Material run not found" 
                    };
                }

                materialRun.DepartureFromStoreTime = DateTime.UtcNow;
                materialRun.UpdatedAt = DateTime.UtcNow;

                // Calculate store time
                if (materialRun.ArrivalAtStoreTime.HasValue)
                {
                    var storeTime = materialRun.DepartureFromStoreTime.Value - materialRun.ArrivalAtStoreTime.Value;
                    materialRun.StoreTimeHours = (decimal)storeTime.TotalHours;
                }

                await _context.SaveChangesAsync();

                return new MaterialRunResult 
                { 
                    Success = true, 
                    Message = $"Departed from {materialRun.MaterialStore.StoreName}" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording store departure for material run {MaterialRunId}", materialRunId);
                return new MaterialRunResult 
                { 
                    Success = false, 
                    Message = "Failed to record store departure" 
                };
            }
        }

        public async Task<MaterialRunResult> CompleteMaterialRun(int materialRunId, double currentLatitude, double currentLongitude, string notes = "")
        {
            try
            {
                var materialRun = await _context.MaterialRuns
                    .Include(mr => mr.Job)
                    .Include(mr => mr.MaterialStore)
                    .FirstOrDefaultAsync(mr => mr.Id == materialRunId);

                if (materialRun == null)
                {
                    return new MaterialRunResult 
                    { 
                        Success = false, 
                        Message = "Material run not found" 
                    };
                }

                materialRun.ReturnTime = DateTime.UtcNow;
                materialRun.ReturnLatitude = currentLatitude;
                materialRun.ReturnLongitude = currentLongitude;
                materialRun.IsCompleted = true;
                materialRun.Notes = notes;
                materialRun.UpdatedAt = DateTime.UtcNow;

                // Calculate total times
                if (materialRun.DepartureTime.HasValue)
                {
                    var totalTime = materialRun.ReturnTime.Value - materialRun.DepartureTime.Value;
                    materialRun.TotalTimeHours = (decimal)totalTime.TotalHours;

                    // Calculate travel time (total time minus store time)
                    var travelTime = totalTime;
                    if (materialRun.StoreTimeHours.HasValue)
                    {
                        travelTime = totalTime - TimeSpan.FromHours((double)materialRun.StoreTimeHours.Value);
                    }
                    materialRun.TravelTimeHours = (decimal)travelTime.TotalHours;
                }

                // Verify return location
                materialRun.LocationVerified = IsAtJobSite(currentLatitude, currentLongitude, materialRun.Job);

                await _context.SaveChangesAsync();

                return new MaterialRunResult 
                { 
                    Success = true, 
                    Message = "Material run completed successfully",
                    TotalHours = (double)materialRun.TotalTimeHours,
                    TravelHours = (double)materialRun.TravelTimeHours,
                    LocationVerified = materialRun.LocationVerified
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing material run {MaterialRunId}", materialRunId);
                return new MaterialRunResult 
                { 
                    Success = false, 
                    Message = "Failed to complete material run" 
                };
            }
        }

        public async Task<List<MaterialRun>> GetActiveMaterialRuns(int userId)
        {
            return await _context.MaterialRuns
                .Include(mr => mr.Job)
                .Include(mr => mr.MaterialStore)
                .Where(mr => mr.UserId == userId && !mr.IsCompleted)
                .OrderBy(mr => mr.DepartureTime)
                .ToListAsync();
        }

        private bool IsAtJobSite(double currentLat, double currentLon, Job job)
        {
            if (!job.Latitude.HasValue || !job.Longitude.HasValue)
                return false;

            var distance = CalculateDistanceInFeet(currentLat, currentLon, job.Latitude.Value, job.Longitude.Value);
            return distance <= 200; // Same 200-foot radius as clock-in
        }

        private double CalculateDistanceInFeet(double lat1, double lon1, double lat2, double lon2)
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

        private double CalculateTravelTime(double fromLat, double fromLon, double toLat, double toLon)
        {
            var distanceMiles = CalculateDistanceInFeet(fromLat, fromLon, toLat, toLon) / 5280.0;
            // Estimate 30 mph average speed for local travel
            return (distanceMiles / 30.0) * 60; // Return minutes
        }
    }

    public class MaterialRunResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? MaterialRunId { get; set; }
        public double? EstimatedTravelTime { get; set; } // in minutes
        public double? TotalHours { get; set; }
        public double? TravelHours { get; set; }
        public bool LocationVerified { get; set; }
    }
}