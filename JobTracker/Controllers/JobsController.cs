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
                var jobs = await _context.Jobs.ToListAsync();
                
                // If no jobs exist, create a sample job
                if (jobs.Count == 0)
                {
                    var sampleJob = new Job
                    {
                        Name = "Sample Renovation Project",
                        Description = "Kitchen and bathroom renovation for a residential property",
                        Location = "123 Main Street, Boston, MA",
                        JobNumber = "REN-2023-001",
                        StartDate = DateTime.UtcNow.AddDays(-30),
                        TargetCompletionDate = DateTime.UtcNow.AddDays(60),
                        Status = "In Progress",
                        ClientName = "John Smith",
                        ClientEmail = "john.smith@example.com",
                        ClientPhone = "(555) 123-4567",
                        Budget = 75000.00m,
                        ActualCost = 25000.00m,
                        Notes = "Client has requested high-end fixtures for all bathrooms",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    _context.Jobs.Add(sampleJob);
                    await _context.SaveChangesAsync();
                    
                    // Add sample job sections
                    var sections = new List<JobSection>
                    {
                        new JobSection
                        {
                            JobId = sampleJob.Id,
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
                            JobId = sampleJob.Id,
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
                            JobId = sampleJob.Id,
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
                if (job.StartDate.HasValue)
                {
                    job.StartDate = DateTime.SpecifyKind(job.StartDate.Value, DateTimeKind.Utc);
                }
                
                if (job.EndDate.HasValue)
                {
                    job.EndDate = DateTime.SpecifyKind(job.EndDate.Value, DateTimeKind.Utc);
                }
                
                if (job.CompletionDate.HasValue)
                {
                    job.CompletionDate = DateTime.SpecifyKind(job.CompletionDate.Value, DateTimeKind.Utc);
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
    }
}