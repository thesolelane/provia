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
                return await _context.Jobs.ToListAsync();
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
                var job = await _context.Jobs
                    .Include(j => j.Sections)
                    .FirstOrDefaultAsync(j => j.Id == id);

                if (job == null)
                {
                    return NotFound();
                }

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