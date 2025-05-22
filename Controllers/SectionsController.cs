using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JobTrackerApp.Data;
using JobTrackerApp.Models;
using JobTrackerApp.Services.Google;
using JobTrackerApp.Services.AI;

namespace JobTrackerApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SectionsController> _logger;
        private readonly GoogleIntegrationService _googleService;
        private readonly AIAssistantService _aiService;

        public SectionsController(
            ApplicationDbContext context,
            ILogger<SectionsController> logger,
            GoogleIntegrationService googleService,
            AIAssistantService aiService)
        {
            _context = context;
            _logger = logger;
            _googleService = googleService;
            _aiService = aiService;
        }

        // GET: api/Sections
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JobSection>>> GetSections([FromQuery] int? jobId = null)
        {
            try
            {
                IQueryable<JobSection> query = _context.JobSections
                    .Include(s => s.Job)
                    .Include(s => s.ResponsibleEmployee)
                    .Include(s => s.Subcontractor);

                if (jobId.HasValue)
                {
                    query = query.Where(s => s.JobId == jobId.Value);
                }

                var sections = await query.ToListAsync();
                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job sections");
                return StatusCode(500, "An error occurred while retrieving job sections");
            }
        }

        // GET: api/Sections/5
        [HttpGet("{id}")]
        public async Task<ActionResult<JobSection>> GetSection(int id)
        {
            try
            {
                var section = await _context.JobSections
                    .Include(s => s.Job)
                    .Include(s => s.ResponsibleEmployee)
                    .Include(s => s.Subcontractor)
                    .Include(s => s.SectionSubcontractors)
                        .ThenInclude(ss => ss.Subcontractor)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (section == null)
                {
                    return NotFound();
                }

                return Ok(section);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job section with ID {SectionId}", id);
                return StatusCode(500, "An error occurred while retrieving the job section");
            }
        }

        // PUT: api/Sections/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ProjectManager,Employee")]
        public async Task<IActionResult> UpdateSection(int id, JobSection section)
        {
            try
            {
                if (id != section.Id)
                {
                    return BadRequest("Section ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if section exists
                var existingSection = await _context.JobSections.FindAsync(id);
                if (existingSection == null)
                {
                    return NotFound();
                }

                // Update metadata
                section.CreatedAt = existingSection.CreatedAt;
                section.CreatedBy = existingSection.CreatedBy;
                section.UpdatedAt = DateTime.UtcNow;
                section.UpdatedBy = User.Identity?.Name ?? "System";

                _context.Entry(existingSection).State = EntityState.Detached;
                _context.Entry(section).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                // If inspection date changed, update Google Calendar
                if (section.InspectionDate.HasValue && 
                    (!existingSection.InspectionDate.HasValue || 
                     section.InspectionDate.Value != existingSection.InspectionDate.Value))
                {
                    try
                    {
                        var job = await _context.Jobs.FindAsync(section.JobId);
                        if (job != null)
                        {
                            await _googleService.CreateCalendarEvent(job, section);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the request
                        _logger.LogWarning(ex, "Failed to update Google Calendar for section {SectionId}", id);
                    }
                }

                _logger.LogInformation("Updated job section: {SectionType} for job {JobId}", section.SectionType, section.JobId);
                
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SectionExists(id))
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
                _logger.LogError(ex, "Error updating job section with ID {SectionId}", id);
                return StatusCode(500, "An error occurred while updating the job section");
            }
        }

        // POST: api/Sections/{id}/toggle-subcontractor
        [HttpPost("{id}/toggle-subcontractor")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> ToggleSubcontractor(int id, [FromBody] SubcontractorToggleRequest request)
        {
            try
            {
                var section = await _context.JobSections.FindAsync(id);
                if (section == null)
                {
                    return NotFound();
                }

                section.IsSubcontracted = request.IsSubcontracted;
                
                if (request.IsSubcontracted && request.SubcontractorId.HasValue)
                {
                    section.SubcontractorId = request.SubcontractorId.Value;
                    
                    // Create section-subcontractor relation if it doesn't exist
                    var sectionSubcontractor = await _context.SectionSubcontractors
                        .FirstOrDefaultAsync(ss => ss.SectionId == id && ss.SubcontractorId == request.SubcontractorId.Value);
                    
                    if (sectionSubcontractor == null)
                    {
                        sectionSubcontractor = new SectionSubcontractor
                        {
                            SectionId = id,
                            SubcontractorId = request.SubcontractorId.Value,
                            StartDate = DateTime.UtcNow,
                            ContractReference = request.ContractReference,
                            CreatedBy = User.Identity?.Name ?? "System",
                            UpdatedBy = User.Identity?.Name ?? "System"
                        };
                        
                        _context.SectionSubcontractors.Add(sectionSubcontractor);
                    }
                }
                else
                {
                    section.SubcontractorId = null;
                }

                section.UpdatedAt = DateTime.UtcNow;
                section.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated job section subcontractor status: {SectionType} for job {JobId}, IsSubcontracted: {IsSubcontracted}", 
                    section.SectionType, section.JobId, section.IsSubcontracted);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling subcontractor for section {SectionId}", id);
                return StatusCode(500, "An error occurred while toggling the subcontractor");
            }
        }

        // POST: api/Sections/{id}/schedule-inspection
        [HttpPost("{id}/schedule-inspection")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> ScheduleInspection(int id, [FromBody] InspectionScheduleRequest request)
        {
            try
            {
                var section = await _context.JobSections
                    .Include(s => s.Job)
                    .FirstOrDefaultAsync(s => s.Id == id);
                
                if (section == null)
                {
                    return NotFound();
                }

                section.InspectionDate = request.InspectionDate;
                section.Status = SectionStatus.NeedsInspection;
                section.UpdatedAt = DateTime.UtcNow;
                section.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                // Create Google Calendar event
                try
                {
                    string eventId = await _googleService.CreateCalendarEvent(section.Job, section);
                    return Ok(new { eventId });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create Google Calendar event for section {SectionId}", id);
                    return Ok(new { message = "Inspection scheduled, but calendar event creation failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling inspection for section {SectionId}", id);
                return StatusCode(500, "An error occurred while scheduling the inspection");
            }
        }

        // POST: api/Sections/{id}/record-inspection
        [HttpPost("{id}/record-inspection")]
        [Authorize(Roles = "Admin,ProjectManager,Employee")]
        public async Task<IActionResult> RecordInspection(int id, [FromBody] InspectionResultRequest request)
        {
            try
            {
                var section = await _context.JobSections.FindAsync(id);
                if (section == null)
                {
                    return NotFound();
                }

                section.InspectionResult = request.Result;
                section.InspectionNotes = request.Notes;
                
                // Update status based on result
                section.Status = request.Result.ToLower() == "pass" 
                    ? SectionStatus.PassedInspection 
                    : SectionStatus.FailedInspection;
                
                // If failed, set reinspection date
                if (request.Result.ToLower() == "fail" && request.ReinspectionDate.HasValue)
                {
                    section.ReinspectionDate = request.ReinspectionDate;
                }

                section.UpdatedAt = DateTime.UtcNow;
                section.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Recorded inspection result for job section: {SectionType} for job {JobId}, Result: {Result}", 
                    section.SectionType, section.JobId, request.Result);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording inspection result for section {SectionId}", id);
                return StatusCode(500, "An error occurred while recording the inspection result");
            }
        }

        // GET: api/Sections/{id}/ai-guidance
        [HttpGet("{id}/ai-guidance")]
        public async Task<IActionResult> GetAIGuidance(int id)
        {
            try
            {
                var section = await _context.JobSections.FindAsync(id);
                if (section == null)
                {
                    return NotFound();
                }

                string guidance = await _aiService.GetJobSectionGuidance(section);
                
                return Ok(new { guidance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI guidance for section {SectionId}", id);
                return StatusCode(500, "An error occurred while getting AI guidance");
            }
        }

        private bool SectionExists(int id)
        {
            return _context.JobSections.Any(e => e.Id == id);
        }
    }

    public class SubcontractorToggleRequest
    {
        public bool IsSubcontracted { get; set; }
        public int? SubcontractorId { get; set; }
        public string? ContractReference { get; set; }
    }

    public class InspectionScheduleRequest
    {
        public DateTime InspectionDate { get; set; }
    }

    public class InspectionResultRequest
    {
        public string Result { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime? ReinspectionDate { get; set; }
    }
}
