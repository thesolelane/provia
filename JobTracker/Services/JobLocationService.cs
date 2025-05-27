using JobTracker.Models;
using JobTracker.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobTracker.Services
{
    public class JobLocationService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<JobLocationService> _logger;
        private readonly HttpClient _httpClient;

        public JobLocationService(JobTrackerContext context, ILogger<JobLocationService> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<LocationResult> CaptureJobLocationAsync(int jobId, string address)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(jobId);
                if (job == null)
                {
                    return new LocationResult { Success = false, Message = "Job not found" };
                }

                // For now, store the address and mark for manual GPS entry
                job.Address = address;
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new LocationResult 
                { 
                    Success = true, 
                    Message = "Job address saved. GPS coordinates will be captured when workers arrive on site.",
                    RequiresManualGPS = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing job location for job {JobId}", jobId);
                return new LocationResult { Success = false, Message = "Failed to save job location" };
            }
        }

        public async Task<LocationResult> UpdateJobGPSAsync(int jobId, double latitude, double longitude)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(jobId);
                if (job == null)
                {
                    return new LocationResult { Success = false, Message = "Job not found" };
                }

                job.Latitude = latitude;
                job.Longitude = longitude;
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new LocationResult 
                { 
                    Success = true, 
                    Message = "Job GPS coordinates updated successfully" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job GPS for job {JobId}", jobId);
                return new LocationResult { Success = false, Message = "Failed to update GPS coordinates" };
            }
        }

        public async Task<List<Job>> GetJobsNeedingGPSAsync()
        {
            return await _context.Jobs
                .Where(j => j.Latitude == null || j.Longitude == null)
                .Where(j => j.Status != JobStatus.Completed && j.Status != JobStatus.Cancelled)
                .ToListAsync();
        }
    }

    public class LocationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool RequiresManualGPS { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}