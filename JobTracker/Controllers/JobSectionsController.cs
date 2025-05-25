using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobSectionsController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly InspectionTrackingService _inspectionService;
        private readonly ILogger<JobSectionsController> _logger;

        public JobSectionsController(JobTrackerContext context, InspectionTrackingService inspectionService, ILogger<JobSectionsController> logger)
        {
            _context = context;
            _inspectionService = inspectionService;
            _logger = logger;
        }

        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<IEnumerable<JobSection>>> GetJobSections(int jobId)
        {
            try
            {
                var sections = await _context.JobSections
                    .Where(s => s.JobId == jobId)
                    .Include(s => s.Images)
                    .OrderBy(s => s.SectionType)
                    .ToListAsync();

                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job sections for job {JobId}", jobId);
                return StatusCode(500, "Error retrieving job sections");
            }
        }

        [HttpPost]
        public async Task<ActionResult<JobSection>> CreateJobSection(JobSection jobSection)
        {
            try
            {
                // Validate that the job exists
                var job = await _context.Jobs.FindAsync(jobSection.JobId);
                if (job == null)
                {
                    return NotFound($"Job with ID {jobSection.JobId} not found");
                }

                // Set timestamps
                jobSection.CreatedAt = DateTime.UtcNow;
                jobSection.UpdatedAt = DateTime.UtcNow;

                // Determine if inspection is required based on section type
                SetInspectionRequirements(jobSection);

                _context.JobSections.Add(jobSection);
                await _context.SaveChangesAsync();

                // If section requires inspection, create reminders when completed
                if (jobSection.Status == 3 && jobSection.RequiresInspection) // Status 3 = Completed
                {
                    await _inspectionService.CheckAndCreateInspectionReminders(jobSection.Id);
                }

                return CreatedAtAction(nameof(GetJobSection), new { id = jobSection.Id }, jobSection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job section");
                return StatusCode(500, "Error creating job section");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<JobSection>> GetJobSection(int id)
        {
            try
            {
                var section = await _context.JobSections
                    .Include(s => s.Images)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (section == null)
                {
                    return NotFound();
                }

                return Ok(section);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job section {SectionId}", id);
                return StatusCode(500, "Error retrieving job section");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJobSection(int id, JobSection jobSection)
        {
            if (id != jobSection.Id)
            {
                return BadRequest("Section ID mismatch");
            }

            try
            {
                var existingSection = await _context.JobSections.FindAsync(id);
                if (existingSection == null)
                {
                    return NotFound();
                }

                // Check if status changed to completed
                bool wasCompleted = existingSection.Status == 3;
                bool nowCompleted = jobSection.Status == 3;

                // Update properties
                existingSection.Status = jobSection.Status;
                existingSection.Description = jobSection.Description;
                existingSection.StartDate = jobSection.StartDate;
                existingSection.CompletionDate = jobSection.CompletionDate;
                existingSection.IsSubcontracted = jobSection.IsSubcontracted;
                existingSection.SubcontractorId = jobSection.SubcontractorId;
                existingSection.ContractReference = jobSection.ContractReference;
                existingSection.ResponsibleEmployeeId = jobSection.ResponsibleEmployeeId;
                existingSection.MaterialsOrdered = jobSection.MaterialsOrdered;
                existingSection.MaterialsDelivered = jobSection.MaterialsDelivered;
                existingSection.Notes = jobSection.Notes;
                existingSection.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Create inspection reminders if section was just completed
                if (!wasCompleted && nowCompleted && existingSection.RequiresInspection)
                {
                    await _inspectionService.CheckAndCreateInspectionReminders(existingSection.Id);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job section {SectionId}", id);
                return StatusCode(500, "Error updating job section");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJobSection(int id)
        {
            try
            {
                var section = await _context.JobSections.FindAsync(id);
                if (section == null)
                {
                    return NotFound();
                }

                _context.JobSections.Remove(section);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job section {SectionId}", id);
                return StatusCode(500, "Error deleting job section");
            }
        }

        private void SetInspectionRequirements(JobSection section)
        {
            // Set inspection requirements based on section type
            switch (section.SectionType)
            {
                case 4: // Framing
                    section.RequiresInspection = true;
                    section.BuildingInspectionRequired = true;
                    break;
                case 5: // Electrical
                    section.RequiresInspection = true;
                    section.ElectricalInspectionRequired = true;
                    break;
                case 6: // Plumbing
                    section.RequiresInspection = true;
                    section.PlumbingInspectionRequired = true;
                    break;
                case 8: // Insulation
                    section.RequiresInspection = true;
                    section.BuildingInspectionRequired = true;
                    break;
                default:
                    section.RequiresInspection = false;
                    break;
            }
        }
    }
}