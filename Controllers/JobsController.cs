using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JobTrackerApp.Data;
using JobTrackerApp.Models;
using JobTrackerApp.Services.Microsoft;
using JobTrackerApp.Services.Google;
using JobTrackerApp.Services.CardShark;

namespace JobTrackerApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobsController> _logger;
        private readonly OfficeIntegrationService _officeService;
        private readonly GoogleIntegrationService _googleService;
        private readonly CardSharkIntegrationService _cardSharkService;

        public JobsController(
            ApplicationDbContext context,
            ILogger<JobsController> logger,
            OfficeIntegrationService officeService,
            GoogleIntegrationService googleService,
            CardSharkIntegrationService cardSharkService)
        {
            _context = context;
            _logger = logger;
            _officeService = officeService;
            _googleService = googleService;
            _cardSharkService = cardSharkService;
        }

        // GET: api/Jobs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetJobs([FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            try
            {
                IQueryable<Job> query = _context.Jobs
                    .Include(j => j.ProjectManager);

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(j => 
                        j.Name.ToLower().Contains(search) ||
                        j.JobNumber.ToLower().Contains(search) ||
                        j.Location.ToLower().Contains(search) ||
                        j.ClientName.ToLower().Contains(search));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(j => j.Status == status);
                }

                // Order by start date descending
                query = query.OrderByDescending(j => j.StartDate);

                var jobs = await query.ToListAsync();
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
                var job = await _context.Jobs
                    .Include(j => j.ProjectManager)
                    .Include(j => j.Sections)
                    .Include(j => j.Assignments)
                        .ThenInclude(a => a.Employee)
                    .FirstOrDefaultAsync(j => j.Id == id);

                if (job == null)
                {
                    return NotFound();
                }

                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job with ID {JobId}", id);
                return StatusCode(500, "An error occurred while retrieving the job");
            }
        }

        // POST: api/Jobs
        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<Job>> CreateJob(Job job)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Set metadata
                job.CreatedAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;
                job.CreatedBy = User.Identity?.Name ?? "System";
                job.UpdatedBy = User.Identity?.Name ?? "System";

                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();

                // Create initial job sections
                foreach (SectionType sectionType in Enum.GetValues(typeof(SectionType)))
                {
                    var section = new JobSection
                    {
                        JobId = job.Id,
                        SectionType = sectionType,
                        Status = SectionStatus.NotStarted,
                        CreatedBy = User.Identity?.Name ?? "System",
                        UpdatedBy = User.Identity?.Name ?? "System"
                    };
                    _context.JobSections.Add(section);
                }
                await _context.SaveChangesAsync();

                // Sync with CardShark (if needed)
                try
                {
                    await _cardSharkService.PushJobUpdate(job);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request
                    _logger.LogWarning(ex, "Failed to sync new job with CardShark");
                }

                _logger.LogInformation("Created new job: {JobName} ({JobId})", job.Name, job.Id);
                
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
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> UpdateJob(int id, Job job)
        {
            try
            {
                if (id != job.Id)
                {
                    return BadRequest("Job ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if job exists
                var existingJob = await _context.Jobs.FindAsync(id);
                if (existingJob == null)
                {
                    return NotFound();
                }

                // Update metadata
                job.CreatedAt = existingJob.CreatedAt;
                job.CreatedBy = existingJob.CreatedBy;
                job.UpdatedAt = DateTime.UtcNow;
                job.UpdatedBy = User.Identity?.Name ?? "System";

                _context.Entry(existingJob).State = EntityState.Detached;
                _context.Entry(job).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                // Sync with CardShark (if needed)
                try
                {
                    await _cardSharkService.PushJobUpdate(job);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request
                    _logger.LogWarning(ex, "Failed to sync updated job with CardShark");
                }

                _logger.LogInformation("Updated job: {JobName} ({JobId})", job.Name, job.Id);
                
                return NoContent();
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
                _logger.LogError(ex, "Error updating job with ID {JobId}", id);
                return StatusCode(500, "An error occurred while updating the job");
            }
        }

        // DELETE: api/Jobs/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
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

                _logger.LogInformation("Deleted job: {JobName} ({JobId})", job.Name, job.Id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job with ID {JobId}", id);
                return StatusCode(500, "An error occurred while deleting the job");
            }
        }

        // GET: api/Jobs/5/export-excel
        [HttpGet("{id}/export-excel")]
        public async Task<IActionResult> ExportJobToExcel(int id)
        {
            try
            {
                var job = await _context.Jobs
                    .Include(j => j.ProjectManager)
                    .Include(j => j.Sections)
                    .Include(j => j.Assignments)
                        .ThenInclude(a => a.Employee)
                    .FirstOrDefaultAsync(j => j.Id == id);

                if (job == null)
                {
                    return NotFound();
                }

                // Generate Excel report
                var excelData = await _officeService.GenerateExcelReport(new List<Job> { job });
                
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Job_{job.JobNumber}_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting job to Excel with ID {JobId}", id);
                return StatusCode(500, "An error occurred while exporting the job to Excel");
            }
        }

        // GET: api/Jobs/5/export-word
        [HttpGet("{id}/export-word")]
        public async Task<IActionResult> ExportJobToWord(int id)
        {
            try
            {
                var job = await _context.Jobs
                    .Include(j => j.ProjectManager)
                    .Include(j => j.Sections)
                    .Include(j => j.Assignments)
                        .ThenInclude(a => a.Employee)
                    .FirstOrDefaultAsync(j => j.Id == id);

                if (job == null)
                {
                    return NotFound();
                }

                // Generate Word document
                var wordData = await _officeService.GenerateWordDocument(job);
                
                return File(wordData, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Job_{job.JobNumber}_{DateTime.Now:yyyyMMdd}.docx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting job to Word with ID {JobId}", id);
                return StatusCode(500, "An error occurred while exporting the job to Word");
            }
        }

        // POST: api/Jobs/5/create-google-doc
        [HttpPost("{id}/create-google-doc")]
        public async Task<IActionResult> CreateGoogleDoc(int id)
        {
            try
            {
                var job = await _context.Jobs
                    .Include(j => j.ProjectManager)
                    .Include(j => j.Sections)
                    .Include(j => j.Assignments)
                        .ThenInclude(a => a.Employee)
                    .FirstOrDefaultAsync(j => j.Id == id);

                if (job == null)
                {
                    return NotFound();
                }

                // Create Google Doc
                string docId = await _googleService.CreateGoogleDocument(job);
                
                return Ok(new { documentId = docId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Google Doc for job with ID {JobId}", id);
                return StatusCode(500, "An error occurred while creating the Google Doc");
            }
        }

        private bool JobExists(int id)
        {
            return _context.Jobs.Any(e => e.Id == id);
        }
    }
}
