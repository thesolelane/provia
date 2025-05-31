using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;
using Microsoft.AspNetCore.Authorization;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
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
                    .Where(s => s.JobId == jobId && !s.IsDeleted)
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
        public async Task<ActionResult<JobSection>> CreateJobSection(CreateJobSectionRequest request)
        {
            try
            {
                // Validate that the job exists
                var job = await _context.Jobs.FindAsync(request.JobId);
                if (job == null)
                {
                    return NotFound($"Job with ID {request.JobId} not found");
                }

                // Create new JobSection from request
                var jobSection = new JobSection
                {
                    JobId = request.JobId,
                    SectionType = request.SectionType,
                    Status = request.Status,
                    IsSubcontracted = request.IsSubcontracted,
                    Notes = request.Notes ?? string.Empty,
                    Description = request.Description ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

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

        [HttpPost("{id}/schedule-inspection")]
        public async Task<IActionResult> ScheduleInspection(int id, [FromBody] ScheduleInspectionRequest request)
        {
            try
            {
                var section = await _context.JobSections.FindAsync(id);
                if (section == null)
                {
                    return NotFound("Section not found");
                }

                // Update inspection schedule based on type
                switch (request.InspectionType.ToLower())
                {
                    case "electrical":
                    case "electrical rough-in":
                        section.ElectricalInspectionDate = request.InspectionDate;
                        section.ElectricalInspectionRequired = true;
                        break;
                    case "plumbing":
                    case "plumbing rough-in":
                        section.PlumbingInspectionDate = request.InspectionDate;
                        section.PlumbingInspectionRequired = true;
                        break;
                    case "framing":
                    case "framing rough-in":
                        section.BuildingInspectionDate = request.InspectionDate;
                        section.BuildingInspectionRequired = true;
                        break;
                    default:
                        section.InspectionDate = request.InspectionDate;
                        break;
                }

                if (!string.IsNullOrEmpty(request.Notes))
                {
                    section.InspectionNotes = request.Notes;
                }

                section.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Inspection scheduled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error scheduling inspection: " + ex.Message);
            }
        }

        [HttpPost("{id}/record-inspection")]
        public async Task<IActionResult> RecordInspectionResult(int id, [FromBody] InspectionResultRequest request)
        {
            try
            {
                var section = await _context.JobSections.FindAsync(id);
                if (section == null)
                {
                    return NotFound("Section not found");
                }

                // Record inspection result based on type
                switch (request.InspectionType.ToLower())
                {
                    case "electrical":
                    case "electrical rough-in":
                        section.ElectricalInspectionCompleted = true;
                        section.ElectricalInspectionPassed = request.Passed;
                        section.ElectricalInspectionDate = request.InspectionDate ?? DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(request.Notes))
                            section.ElectricalInspectionNotes = request.Notes;
                        break;
                    case "plumbing":
                    case "plumbing rough-in":
                        section.PlumbingInspectionCompleted = true;
                        section.PlumbingInspectionPassed = request.Passed;
                        section.PlumbingInspectionDate = request.InspectionDate ?? DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(request.Notes))
                            section.PlumbingInspectionNotes = request.Notes;
                        break;
                    case "framing":
                    case "framing rough-in":
                        section.BuildingInspectionCompleted = true;
                        section.BuildingInspectionPassed = request.Passed;
                        section.BuildingInspectionDate = request.InspectionDate ?? DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(request.Notes))
                            section.BuildingInspectionNotes = request.Notes;
                        break;
                }

                section.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Inspection result recorded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error recording inspection: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJobSection(int id)
        {
            try
            {
                var section = await _context.JobSections.Include(s => s.Job).FirstOrDefaultAsync(s => s.Id == id);
                if (section == null)
                {
                    return NotFound();
                }

                // Soft delete: mark as deleted and archive with abbreviated job number
                section.IsDeleted = true;
                section.DeletedAt = DateTime.UtcNow;
                section.DeletedBy = "System"; // In real app, this would be the current user
                section.DeletionReason = "Section removed by user";
                
                // Create abbreviated job number (last 5 digits only) for archive
                var originalJobNumber = section.Job?.JobNumber ?? "UNKNOWN";
                var abbreviatedJobNumber = originalJobNumber.Length > 5 
                    ? originalJobNumber.Substring(originalJobNumber.Length - 5) 
                    : originalJobNumber;
                
                // Archive the section data with abbreviated reference
                section.Notes = $"[ARCHIVED from {originalJobNumber} -> {abbreviatedJobNumber}] {section.Notes ?? ""}";
                
                // Update the section instead of hard deleting
                _context.JobSections.Update(section);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Section archived successfully", 
                    archivedAs = abbreviatedJobNumber,
                    originalJobNumber = originalJobNumber
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error archiving section: {ex.Message}");
            }
        }
    }
}