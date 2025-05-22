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
                    .Include(js => js.Subcontractor)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job sections");
                return StatusCode(500, "Internal server error occurred while retrieving job sections.");
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
                    .Include(js => js.Subcontractor)
                    .FirstOrDefaultAsync(js => js.SectionId == id);

                if (jobSection == null)
                {
                    return NotFound($"Job section with ID {id} not found.");
                }

                return jobSection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job section with ID {JobSectionId}", id);
                return StatusCode(500, $"Internal server error occurred while retrieving job section with ID {id}.");
            }
        }

        // GET: api/JobSections/job/5
        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<IEnumerable<JobSection>>> GetJobSectionsByJobId(int jobId)
        {
            try
            {
                return await _context.JobSections
                    .Include(js => js.Subcontractor)
                    .Where(js => js.JobId == jobId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job sections for job ID {JobId}", jobId);
                return StatusCode(500, $"Internal server error occurred while retrieving job sections for job ID {jobId}.");
            }
        }

        // PUT: api/JobSections/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJobSection(int id, JobSection jobSection)
        {
            try
            {
                if (id != jobSection.SectionId)
                {
                    return BadRequest("Job section ID mismatch.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.Entry(jobSection).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobSectionExists(id))
                    {
                        return NotFound($"Job section with ID {id} not found.");
                    }
                    else
                    {
                        throw;
                    }
                }

                // Update job status based on sections
                await UpdateJobStatus(jobSection.JobId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job section with ID {JobSectionId}", id);
                return StatusCode(500, $"Internal server error occurred while updating job section with ID {id}.");
            }
        }

        // POST: api/JobSections
        [HttpPost]
        public async Task<ActionResult<JobSection>> CreateJobSection(JobSection jobSection)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.JobSections.Add(jobSection);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetJobSection), new { id = jobSection.SectionId }, jobSection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new job section");
                return StatusCode(500, "Internal server error occurred while creating a new job section.");
            }
        }

        // PATCH: api/JobSections/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateSectionStatus(int id, [FromBody] SectionStatus status)
        {
            try
            {
                var jobSection = await _context.JobSections.FindAsync(id);
                if (jobSection == null)
                {
                    return NotFound($"Job section with ID {id} not found.");
                }

                jobSection.Status = status;
                
                // If status is Completed, set actual completion date
                if (status == SectionStatus.Completed && !jobSection.ActualCompletionDate.HasValue)
                {
                    jobSection.ActualCompletionDate = DateTime.UtcNow;
                }

                // If status is Failed, clear actual completion date
                if (status == SectionStatus.Failed)
                {
                    jobSection.ActualCompletionDate = null;
                }

                await _context.SaveChangesAsync();

                // Update job status based on sections
                await UpdateJobStatus(jobSection.JobId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for job section with ID {JobSectionId}", id);
                return StatusCode(500, $"Internal server error occurred while updating status for job section with ID {id}.");
            }
        }

        // PATCH: api/JobSections/5/subcontractor
        [HttpPatch("{id}/subcontractor")]
        public async Task<IActionResult> AssignSubcontractor(int id, [FromBody] int? subcontractorId)
        {
            try
            {
                var jobSection = await _context.JobSections.FindAsync(id);
                if (jobSection == null)
                {
                    return NotFound($"Job section with ID {id} not found.");
                }

                if (subcontractorId.HasValue)
                {
                    var subcontractor = await _context.Subcontractors.FindAsync(subcontractorId.Value);
                    if (subcontractor == null)
                    {
                        return NotFound($"Subcontractor with ID {subcontractorId.Value} not found.");
                    }

                    jobSection.SubcontractorId = subcontractorId;
                    jobSection.IsSubcontracted = true;
                }
                else
                {
                    jobSection.SubcontractorId = null;
                    jobSection.IsSubcontracted = false;
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning subcontractor to job section with ID {JobSectionId}", id);
                return StatusCode(500, $"Internal server error occurred while assigning subcontractor to job section with ID {id}.");
            }
        }

        // GET: api/JobSections/inspections
        [HttpGet("inspections")]
        public async Task<ActionResult<IEnumerable<JobSection>>> GetUpcomingInspections()
        {
            try
            {
                var today = DateTime.Today;
                var next14Days = today.AddDays(14);

                return await _context.JobSections
                    .Include(js => js.Job)
                    .Include(js => js.Subcontractor)
                    .Where(js => js.InspectionDate.HasValue && 
                                js.InspectionDate.Value >= today && 
                                js.InspectionDate.Value <= next14Days)
                    .OrderBy(js => js.InspectionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming inspections");
                return StatusCode(500, "Internal server error occurred while retrieving upcoming inspections.");
            }
        }

        // Private helper method to update job status based on sections
        private async Task UpdateJobStatus(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            var sections = await _context.JobSections.Where(js => js.JobId == jobId).ToListAsync();

            if (job != null)
            {
                if (sections.All(s => s.Status == SectionStatus.Completed))
                {
                    job.Status = JobStatus.Completed;
                    job.ActualCompletionDate = DateTime.UtcNow;
                }
                else if (sections.Any(s => s.Status == SectionStatus.OnHold))
                {
                    job.Status = JobStatus.OnHold;
                }
                else if (sections.Any(s => s.Status != SectionStatus.NotStarted))
                {
                    job.Status = JobStatus.InProgress;
                }

                await _context.SaveChangesAsync();
            }
        }

        private bool JobSectionExists(int id)
        {
            return _context.JobSections.Any(e => e.SectionId == id);
        }
    }
}
