using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobSectionsController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<JobSectionsController> _logger;

        public JobSectionsController(JobTrackerContext context, ILogger<JobSectionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/JobSections
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JobSection>>> GetJobSections()
        {
            try
            {
                return await _context.JobSections
                    .Include(js => js.Job)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job sections");
                return StatusCode(500, "An error occurred while retrieving job sections");
            }
        }

        // GET: api/JobSections/5
        [HttpGet("{id}")]
        public async Task<ActionResult<JobSection>> GetJobSection(int id)
        {
            try
            {
                var jobSection = await _context.JobSections
                    .Include(js => js.Job)
                    .FirstOrDefaultAsync(js => js.Id == id);

                if (jobSection == null)
                {
                    return NotFound();
                }

                return jobSection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving job section with ID {id}");
                return StatusCode(500, $"An error occurred while retrieving job section with ID {id}");
            }
        }

        // GET: api/JobSections/job/5 or api/JobSections/ByJob/5
        [HttpGet("job/{jobId}")]
        [HttpGet("ByJob/{jobId}")]
        public async Task<ActionResult<IEnumerable<JobSection>>> GetJobSectionsByJob(int jobId)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(jobId);
                if (job == null)
                {
                    return NotFound($"Job with ID {jobId} not found");
                }

                return await _context.JobSections
                    .Where(js => js.JobId == jobId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving job sections for job ID {jobId}");
                return StatusCode(500, $"An error occurred while retrieving job sections for job ID {jobId}");
            }
        }

        // POST: api/JobSections
        [HttpPost]
        public async Task<ActionResult<JobSection>> CreateJobSection(JobSection jobSection)
        {
            try
            {
                // Validate that the job exists
                var jobExists = await _context.Jobs.AnyAsync(j => j.Id == jobSection.JobId);
                if (!jobExists)
                {
                    return BadRequest($"Job with ID {jobSection.JobId} does not exist");
                }

                jobSection.CreatedAt = DateTime.UtcNow;
                jobSection.UpdatedAt = DateTime.UtcNow;
                
                _context.JobSections.Add(jobSection);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetJobSection), new { id = jobSection.Id }, jobSection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job section");
                return StatusCode(500, "An error occurred while creating the job section");
            }
        }

        // PUT: api/JobSections/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJobSection(int id, JobSection jobSection)
        {
            if (id != jobSection.Id)
            {
                return BadRequest("ID in the URL does not match the ID in the request body");
            }

            try
            {
                // Validate that the job exists
                var jobExists = await _context.Jobs.AnyAsync(j => j.Id == jobSection.JobId);
                if (!jobExists)
                {
                    return BadRequest($"Job with ID {jobSection.JobId} does not exist");
                }

                jobSection.UpdatedAt = DateTime.UtcNow;
                
                _context.Entry(jobSection).State = EntityState.Modified;
                // Don't modify the CreatedAt field
                _context.Entry(jobSection).Property(x => x.CreatedAt).IsModified = false;
                
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobSectionExists(id))
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
                _logger.LogError(ex, $"Error updating job section with ID {id}");
                return StatusCode(500, $"An error occurred while updating job section with ID {id}");
            }

            return NoContent();
        }

        // PUT: api/JobSections/5/UpdateStatus/2
        [HttpPut("{id}/UpdateStatus/{status}")]
        public async Task<IActionResult> UpdateJobSectionStatus(int id, int status)
        {
            try
            {
                // Validate status is in valid range (1-8)
                if (status < 1 || status > 8)
                {
                    return BadRequest("Status must be between 1 and 8");
                }

                var jobSection = await _context.JobSections.FindAsync(id);
                if (jobSection == null)
                {
                    return NotFound();
                }

                jobSection.Status = status;
                jobSection.UpdatedAt = DateTime.UtcNow;
                
                // If status is Completed (3), set the completion date
                if (status == 3 && jobSection.CompletionDate == null)
                {
                    jobSection.CompletionDate = DateTime.UtcNow;
                }
                
                // If status is In Progress (2) and there's no start date, set it
                if (status == 2 && jobSection.StartDate == null)
                {
                    jobSection.StartDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating status for job section with ID {id}");
                return StatusCode(500, $"An error occurred while updating the status for job section with ID {id}");
            }
        }

        // DELETE: api/JobSections/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJobSection(int id)
        {
            try
            {
                var jobSection = await _context.JobSections.FindAsync(id);
                if (jobSection == null)
                {
                    return NotFound();
                }

                _context.JobSections.Remove(jobSection);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting job section with ID {id}");
                return StatusCode(500, $"An error occurred while deleting job section with ID {id}");
            }
        }

        private bool JobSectionExists(int id)
        {
            return _context.JobSections.Any(e => e.Id == id);
        }
    }
}