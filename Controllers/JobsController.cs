using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;

namespace JobTracker.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
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
                return await _context.Jobs
                    .Include(j => j.Sections)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs");
                return StatusCode(500, "Internal server error occurred while retrieving jobs.");
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
                    .ThenInclude(s => s.Subcontractor)
                    .FirstOrDefaultAsync(j => j.JobId == id);

                if (job == null)
                {
                    return NotFound($"Job with ID {id} not found.");
                }

                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job with ID {JobId}", id);
                return StatusCode(500, $"Internal server error occurred while retrieving job with ID {id}.");
            }
        }

        // POST: api/Jobs
        [HttpPost]
        public async Task<ActionResult<Job>> CreateJob(Job job)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();

                // Automatically create the 12 job sections for the new job
                var sectionTypes = Enum.GetValues(typeof(SectionType));
                foreach (SectionType sectionType in sectionTypes)
                {
                    var section = new JobSection
                    {
                        JobId = job.JobId,
                        Type = sectionType,
                        Status = SectionStatus.NotStarted,
                        IsSubcontracted = false
                    };
                    _context.JobSections.Add(section);
                }
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetJob), new { id = job.JobId }, job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new job");
                return StatusCode(500, "Internal server error occurred while creating a new job.");
            }
        }

        // PUT: api/Jobs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJob(int id, Job job)
        {
            try
            {
                if (id != job.JobId)
                {
                    return BadRequest("Job ID mismatch.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.Entry(job).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobExists(id))
                    {
                        return NotFound($"Job with ID {id} not found.");
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job with ID {JobId}", id);
                return StatusCode(500, $"Internal server error occurred while updating job with ID {id}.");
            }
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
                    return NotFound($"Job with ID {id} not found.");
                }

                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job with ID {JobId}", id);
                return StatusCode(500, $"Internal server error occurred while deleting job with ID {id}.");
            }
        }

        // GET: api/Jobs/search?query=keyword
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Job>>> SearchJobs(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return await GetJobs();
                }

                var jobs = await _context.Jobs
                    .Include(j => j.Sections)
                    .Where(j => j.JobName.Contains(query) || 
                                j.Description.Contains(query) || 
                                j.Location.Contains(query) || 
                                j.ClientName.Contains(query) ||
                                j.ProjectManager.Contains(query))
                    .ToListAsync();

                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching jobs with query {Query}", query);
                return StatusCode(500, "Internal server error occurred while searching jobs.");
            }
        }

        // GET: api/Jobs/filter?status=InProgress
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<Job>>> FilterJobs([FromQuery] JobStatus? status, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var query = _context.Jobs.Include(j => j.Sections).AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(j => j.Status == status.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(j => j.StartDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(j => j.ExpectedCompletionDate <= endDate.Value || j.ActualCompletionDate <= endDate.Value);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering jobs");
                return StatusCode(500, "Internal server error occurred while filtering jobs.");
            }
        }

        // GET: api/Jobs/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetDashboardData()
        {
            try
            {
                var allJobs = await _context.Jobs.ToListAsync();
                var allSections = await _context.JobSections.ToListAsync();

                var result = new
                {
                    TotalJobs = allJobs.Count,
                    ActiveJobs = allJobs.Count(j => j.Status == JobStatus.InProgress),
                    CompletedJobs = allJobs.Count(j => j.Status == JobStatus.Completed),
                    OnHoldJobs = allJobs.Count(j => j.Status == JobStatus.OnHold),
                    SectionStatus = new
                    {
                        NotStarted = allSections.Count(s => s.Status == SectionStatus.NotStarted),
                        InProgress = allSections.Count(s => s.Status == SectionStatus.InProgress),
                        Completed = allSections.Count(s => s.Status == SectionStatus.Completed),
                        Failed = allSections.Count(s => s.Status == SectionStatus.Failed),
                        OnHold = allSections.Count(s => s.Status == SectionStatus.OnHold),
                        WaitingForInspection = allSections.Count(s => s.Status == SectionStatus.WaitingForInspection),
                        WaitingForMaterials = allSections.Count(s => s.Status == SectionStatus.WaitingForMaterials)
                    },
                    RecentJobs = await _context.Jobs
                        .OrderByDescending(j => j.CreatedDate)
                        .Take(5)
                        .ToListAsync(),
                    UpcomingInspections = await _context.JobSections
                        .Where(s => s.InspectionDate.HasValue && s.InspectionDate.Value >= DateTime.Today && s.InspectionDate.Value <= DateTime.Today.AddDays(7))
                        .Include(s => s.Job)
                        .OrderBy(s => s.InspectionDate)
                        .Take(5)
                        .ToListAsync()
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return StatusCode(500, "Internal server error occurred while retrieving dashboard data.");
            }
        }

        private bool JobExists(int id)
        {
            return _context.Jobs.Any(e => e.JobId == id);
        }
    }
}
