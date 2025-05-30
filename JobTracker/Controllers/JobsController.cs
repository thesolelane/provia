using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<JobsController> _logger;

        public JobsController(JobTrackerContext context, ILogger<JobsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Jobs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetJobs()
        {
            try
            {
                // Add retry logic for database connection issues
                var jobs = new List<Job>();
                var retryCount = 0;
                const int maxRetries = 3;
                
                while (retryCount < maxRetries)
                {
                    try
                    {
                        jobs = await _context.Jobs.ToListAsync();
                        break;
                    }
                    catch (Exception dbEx) when (retryCount < maxRetries - 1)
                    {
                        _logger.LogWarning($"Database retry {retryCount + 1}/{maxRetries}: {dbEx.Message}");
                        retryCount++;
                        await Task.Delay(1000 * retryCount); // Progressive delay
                        
                        // Try to recreate the context if needed
                        try
                        {
                            await _context.Database.EnsureCreatedAsync();
                        }
                        catch (Exception ensureEx)
                        {
                            _logger.LogWarning($"Database ensure failed: {ensureEx.Message}");
                        }
                    }
                }
                
                // If no jobs exist, create sample jobs
                if (jobs.Count == 0)
                {
                    var sampleJobs = new List<Job>
                    {
                        new Job
                        {
                            Name = "Kitchen Renovation",
                            Description = "Complete kitchen remodel with new cabinets and appliances",
                            Location = "117 Marshall St, Fitchburg, MA",
                            JobNumber = "2025-01-15-MAR117-001",
                            StartDate = DateTime.UtcNow.AddDays(-15),
                            TargetCompletionDate = DateTime.UtcNow.AddDays(45),
                            Status = "In Progress",
                            Budget = 85000.00m,
                            ActualCost = 32000.00m,
                            ClientName = "Sarah Johnson",
                            ClientEmail = "sarah.johnson@email.com",
                            ClientPhone = "(978) 555-0123",
                            CompanyId = 1,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new Job
                        {
                            Name = "Bathroom Addition",
                            Description = "New master bathroom construction",
                            Location = "123 Main Street, Boston, MA",
                            JobNumber = "2025-01-20-MAI123-002",
                            StartDate = DateTime.UtcNow.AddDays(-10),
                            TargetCompletionDate = DateTime.UtcNow.AddDays(30),
                            Status = "In Progress",
                            Budget = 45000.00m,
                            ActualCost = 18000.00m,
                            ClientName = "Michael Chen",
                            ClientEmail = "m.chen@email.com",
                            ClientPhone = "(617) 555-0456",
                            CompanyId = 1,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new Job
                        {
                            Name = "Basement Finishing",
                            Description = "Finish basement with family room and office space",
                            Location = "40 Warnock St, Lowell, MA",
                            JobNumber = "2025-01-25-WAR040-003",
                            StartDate = DateTime.UtcNow.AddDays(-5),
                            TargetCompletionDate = DateTime.UtcNow.AddDays(60),
                            Status = "In Progress",
                            Budget = 65000.00m,
                            ActualCost = 12000.00m,
                            ClientName = "David Rodriguez",
                            ClientEmail = "d.rodriguez@email.com",
                            ClientPhone = "(978) 555-0789",
                            CompanyId = 1,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new Job
                        {
                            Name = "Home Addition",
                            Description = "Two-story addition with bedrooms and updated electrical",
                            Location = "61 Beach St, Haverhill, MA",
                            JobNumber = "2025-02-01-BEA061-004",
                            StartDate = DateTime.UtcNow.AddDays(-2),
                            TargetCompletionDate = DateTime.UtcNow.AddDays(90),
                            Status = "In Progress",
                            Budget = 125000.00m,
                            ActualCost = 8000.00m,
                            ClientName = "Lisa Thompson",
                            ClientEmail = "lisa.thompson@email.com",
                            ClientPhone = "(978) 555-0234",
                            CompanyId = 1,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };
                    
                    _context.Jobs.AddRange(sampleJobs);
                    await _context.SaveChangesAsync();
                    
                    // Add sample job sections for the first job
                    var firstJobId = sampleJobs[0].Id;
                    var sections = new List<JobSection>
                    {
                        new JobSection
                        {
                            JobId = firstJobId,
                            SectionType = 0, // Demolition
                            Description = "Remove existing kitchen cabinets and flooring",
                            Status = 2, // Completed
                            StartDate = DateTime.UtcNow.AddDays(-25),
                            CompletionDate = DateTime.UtcNow.AddDays(-15),
                            IsSubcontracted = false,
                            MaterialsOrdered = true,
                            MaterialsDelivered = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new JobSection
                        {
                            JobId = firstJobId,
                            SectionType = 3, // Electrical
                            Description = "Upgrade electrical panel and add new lighting fixtures",
                            Status = 1, // In Progress
                            StartDate = DateTime.UtcNow.AddDays(-10),
                            IsSubcontracted = true,
                            SubcontractorId = null,
                            ContractReference = "EL-2023-456",
                            MaterialsOrdered = true,
                            MaterialsDelivered = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new JobSection
                        {
                            JobId = firstJobId,
                            SectionType = 4, // Plumbing
                            Description = "Install new plumbing for kitchen sink and dishwasher",
                            Status = 0, // Not Started
                            IsSubcontracted = true,
                            MaterialsOrdered = true,
                            MaterialsDelivered = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };
                    
                    _context.JobSections.AddRange(sections);
                    await _context.SaveChangesAsync();
                    
                    // Refresh jobs list
                    jobs = await _context.Jobs.ToListAsync();
                }
                
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs");
                return StatusCode(500, "An error occurred while retrieving jobs");
            }
        }

        // GET: api/Jobs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Job>> GetJob(int id)
        {
            try
            {
                // Load job without complex joins first to avoid column issues
                var job = await _context.Jobs
                    .FirstOrDefaultAsync(j => j.Id == id);

                if (job == null)
                {
                    return NotFound();
                }

                // Load sections separately
                job.Sections = await _context.JobSections
                    .Where(s => s.JobId == id)
                    .ToListAsync();

                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving job with ID {id}");
                return StatusCode(500, $"An error occurred while retrieving job with ID {id}");
            }
        }

        // POST: api/Jobs
        [HttpPost]
        public async Task<ActionResult<Job>> CreateJob(Job job)
        {
            try
            {
                job.CreatedAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;
                
                // Set UTC dates
                job.StartDate = DateTime.SpecifyKind(job.StartDate, DateTimeKind.Utc);
                
                if (job.TargetCompletionDate.HasValue)
                {
                    job.TargetCompletionDate = DateTime.SpecifyKind(job.TargetCompletionDate.Value, DateTimeKind.Utc);
                }
                
                if (job.ActualCompletionDate.HasValue)
                {
                    job.ActualCompletionDate = DateTime.SpecifyKind(job.ActualCompletionDate.Value, DateTimeKind.Utc);
                }
                
                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job");
                return StatusCode(500, "An error occurred while creating the job");
            }
        }

        // PUT: api/Jobs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJob(int id, Job job)
        {
            if (id != job.Id)
            {
                return BadRequest("ID in the URL does not match the ID in the request body");
            }

            try
            {
                job.UpdatedAt = DateTime.UtcNow;
                
                _context.Entry(job).State = EntityState.Modified;
                // Don't modify the CreatedAt field
                _context.Entry(job).Property(x => x.CreatedAt).IsModified = false;
                
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobExists(id))
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
                _logger.LogError(ex, $"Error updating job with ID {id}");
                return StatusCode(500, $"An error occurred while updating job with ID {id}");
            }

            return NoContent();
        }

        // DELETE: api/Jobs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(id);
                if (job == null)
                {
                    return NotFound();
                }

                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting job with ID {id}");
                return StatusCode(500, $"An error occurred while deleting job with ID {id}");
            }
        }

        private bool JobExists(int id)
        {
            return _context.Jobs.Any(e => e.Id == id);
        }

        // POST: api/Jobs/{id}/update-coordinates
        [HttpPost("{id}/update-coordinates")]
        public async Task<IActionResult> UpdateJobCoordinates(int id, [FromBody] UpdateCoordinatesRequest request)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(id);
                if (job == null)
                {
                    return NotFound();
                }

                // Update coordinates
                job.Latitude = request.Latitude;
                job.Longitude = request.Longitude;
                job.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Job site coordinates updated for {job.Name} at {job.Location}: {request.Latitude}, {request.Longitude} by admin");

                return Ok(new { 
                    success = true, 
                    message = "Job site coordinates updated successfully",
                    latitude = request.Latitude,
                    longitude = request.Longitude
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job coordinates for job {JobId}", id);
                return StatusCode(500, new { success = false, message = "Failed to update coordinates" });
            }
        }
    }

    public class UpdateCoordinatesRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}