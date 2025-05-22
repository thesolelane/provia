using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JobTrackerApp.Data;
using JobTrackerApp.Models;

namespace JobTrackerApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SubcontractorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubcontractorsController> _logger;

        public SubcontractorsController(
            ApplicationDbContext context,
            ILogger<SubcontractorsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Subcontractors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Subcontractor>>> GetSubcontractors(
            [FromQuery] bool? active = null,
            [FromQuery] string? search = null)
        {
            try
            {
                IQueryable<Subcontractor> query = _context.Subcontractors;

                // Filter by active status if provided
                if (active.HasValue)
                {
                    query = query.Where(s => s.IsActive == active.Value);
                }

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(s =>
                        s.CompanyName.ToLower().Contains(search) ||
                        s.ContactName.ToLower().Contains(search) ||
                        s.Email.ToLower().Contains(search) ||
                        s.LicenseNumber.ToLower().Contains(search));
                }

                // Order by company name
                query = query.OrderBy(s => s.CompanyName);

                var subcontractors = await query.ToListAsync();
                return Ok(subcontractors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcontractors");
                return StatusCode(500, "An error occurred while retrieving subcontractors");
            }
        }

        // GET: api/Subcontractors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Subcontractor>> GetSubcontractor(int id)
        {
            try
            {
                var subcontractor = await _context.Subcontractors
                    .Include(s => s.SectionSubcontractors)
                        .ThenInclude(ss => ss.Section)
                            .ThenInclude(s => s.Job)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (subcontractor == null)
                {
                    return NotFound();
                }

                return Ok(subcontractor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcontractor with ID {SubcontractorId}", id);
                return StatusCode(500, "An error occurred while retrieving the subcontractor");
            }
        }

        // POST: api/Subcontractors
        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<Subcontractor>> CreateSubcontractor(Subcontractor subcontractor)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Set metadata
                subcontractor.CreatedAt = DateTime.UtcNow;
                subcontractor.UpdatedAt = DateTime.UtcNow;
                subcontractor.CreatedBy = User.Identity?.Name ?? "System";
                subcontractor.UpdatedBy = User.Identity?.Name ?? "System";
                
                // Default to active
                subcontractor.IsActive = true;

                _context.Subcontractors.Add(subcontractor);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new subcontractor: {CompanyName} ({SubcontractorId})", 
                    subcontractor.CompanyName, subcontractor.Id);
                
                return CreatedAtAction(nameof(GetSubcontractor), new { id = subcontractor.Id }, subcontractor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subcontractor");
                return StatusCode(500, "An error occurred while creating the subcontractor");
            }
        }

        // PUT: api/Subcontractors/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> UpdateSubcontractor(int id, Subcontractor subcontractor)
        {
            try
            {
                if (id != subcontractor.Id)
                {
                    return BadRequest("Subcontractor ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if subcontractor exists
                var existingSubcontractor = await _context.Subcontractors.FindAsync(id);
                if (existingSubcontractor == null)
                {
                    return NotFound();
                }

                // Update metadata
                subcontractor.CreatedAt = existingSubcontractor.CreatedAt;
                subcontractor.CreatedBy = existingSubcontractor.CreatedBy;
                subcontractor.UpdatedAt = DateTime.UtcNow;
                subcontractor.UpdatedBy = User.Identity?.Name ?? "System";

                _context.Entry(existingSubcontractor).State = EntityState.Detached;
                _context.Entry(subcontractor).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated subcontractor: {CompanyName} ({SubcontractorId})", 
                    subcontractor.CompanyName, subcontractor.Id);
                
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubcontractorExists(id))
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
                _logger.LogError(ex, "Error updating subcontractor with ID {SubcontractorId}", id);
                return StatusCode(500, "An error occurred while updating the subcontractor");
            }
        }

        // DELETE: api/Subcontractors/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSubcontractor(int id)
        {
            try
            {
                var subcontractor = await _context.Subcontractors.FindAsync(id);
                if (subcontractor == null)
                {
                    return NotFound();
                }

                // Check if subcontractor is referenced by any job sections
                var hasReferences = await _context.JobSections
                    .AnyAsync(s => s.SubcontractorId == id);

                if (hasReferences)
                {
                    // Soft delete if referenced
                    subcontractor.IsActive = false;
                    subcontractor.UpdatedAt = DateTime.UtcNow;
                    subcontractor.UpdatedBy = User.Identity?.Name ?? "System";
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Soft-deleted subcontractor: {CompanyName} ({SubcontractorId})", 
                        subcontractor.CompanyName, subcontractor.Id);
                }
                else
                {
                    // Hard delete if not referenced
                    _context.Subcontractors.Remove(subcontractor);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Hard-deleted subcontractor: {CompanyName} ({SubcontractorId})", 
                        subcontractor.CompanyName, subcontractor.Id);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subcontractor with ID {SubcontractorId}", id);
                return StatusCode(500, "An error occurred while deleting the subcontractor");
            }
        }

        // GET: api/Subcontractors/5/sections
        [HttpGet("{id}/sections")]
        public async Task<ActionResult<IEnumerable<JobSection>>> GetSubcontractorSections(int id)
        {
            try
            {
                var subcontractor = await _context.Subcontractors
                    .Include(s => s.SectionSubcontractors)
                        .ThenInclude(ss => ss.Section)
                            .ThenInclude(s => s.Job)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (subcontractor == null)
                {
                    return NotFound();
                }

                var sections = subcontractor.SectionSubcontractors
                    .Select(ss => ss.Section)
                    .ToList();

                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sections for subcontractor {SubcontractorId}", id);
                return StatusCode(500, "An error occurred while retrieving sections for the subcontractor");
            }
        }

        // POST: api/Subcontractors/section-assignment
        [HttpPost("section-assignment")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<ActionResult<SectionSubcontractor>> CreateSectionAssignment(SectionSubcontractorRequest request)
        {
            try
            {
                // Validate section
                var section = await _context.JobSections.FindAsync(request.SectionId);
                if (section == null)
                {
                    return BadRequest("Invalid section ID");
                }

                // Validate subcontractor
                var subcontractor = await _context.Subcontractors.FindAsync(request.SubcontractorId);
                if (subcontractor == null)
                {
                    return BadRequest("Invalid subcontractor ID");
                }

                // Check if assignment already exists
                var existingAssignment = await _context.SectionSubcontractors
                    .FirstOrDefaultAsync(ss => ss.SectionId == request.SectionId && ss.SubcontractorId == request.SubcontractorId);

                if (existingAssignment != null)
                {
                    return BadRequest("This subcontractor is already assigned to this section");
                }

                // Create section-subcontractor relationship
                var sectionSubcontractor = new SectionSubcontractor
                {
                    SectionId = request.SectionId,
                    SubcontractorId = request.SubcontractorId,
                    ContractReference = request.ContractReference,
                    StartDate = request.StartDate ?? DateTime.UtcNow,
                    ExpectedCompletionDate = request.ExpectedCompletionDate,
                    ContractAmount = request.ContractAmount,
                    PaidAmount = 0, // Initialize to zero
                    Notes = request.Notes,
                    CreatedBy = User.Identity?.Name ?? "System",
                    UpdatedBy = User.Identity?.Name ?? "System"
                };

                // Update section to mark as subcontracted
                section.IsSubcontracted = true;
                section.SubcontractorId = request.SubcontractorId;
                section.UpdatedAt = DateTime.UtcNow;
                section.UpdatedBy = User.Identity?.Name ?? "System";

                _context.SectionSubcontractors.Add(sectionSubcontractor);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Assigned subcontractor {SubcontractorId} to section {SectionId}", 
                    request.SubcontractorId, request.SectionId);
                
                return Ok(sectionSubcontractor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating section-subcontractor assignment");
                return StatusCode(500, "An error occurred while creating the assignment");
            }
        }

        // PUT: api/Subcontractors/section-assignment/5
        [HttpPut("section-assignment/{id}")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> UpdateSectionAssignment(int id, SectionSubcontractorRequest request)
        {
            try
            {
                // Find existing assignment
                var sectionSubcontractor = await _context.SectionSubcontractors.FindAsync(id);
                if (sectionSubcontractor == null)
                {
                    return NotFound();
                }

                // Update fields
                sectionSubcontractor.ContractReference = request.ContractReference;
                sectionSubcontractor.StartDate = request.StartDate ?? sectionSubcontractor.StartDate;
                sectionSubcontractor.ExpectedCompletionDate = request.ExpectedCompletionDate;
                sectionSubcontractor.ActualCompletionDate = request.ActualCompletionDate;
                sectionSubcontractor.ContractAmount = request.ContractAmount;
                sectionSubcontractor.PaidAmount = request.PaidAmount ?? sectionSubcontractor.PaidAmount;
                sectionSubcontractor.Notes = request.Notes;
                sectionSubcontractor.UpdatedAt = DateTime.UtcNow;
                sectionSubcontractor.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated section-subcontractor assignment {AssignmentId}", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating section-subcontractor assignment {AssignmentId}", id);
                return StatusCode(500, "An error occurred while updating the assignment");
            }
        }

        // DELETE: api/Subcontractors/section-assignment/5
        [HttpDelete("section-assignment/{id}")]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> DeleteSectionAssignment(int id)
        {
            try
            {
                var sectionSubcontractor = await _context.SectionSubcontractors.FindAsync(id);
                if (sectionSubcontractor == null)
                {
                    return NotFound();
                }

                // Get the section to update its subcontracted status
                var section = await _context.JobSections.FindAsync(sectionSubcontractor.SectionId);
                
                _context.SectionSubcontractors.Remove(sectionSubcontractor);
                await _context.SaveChangesAsync();

                // If this was the only assignment for this section, update the section
                if (section != null)
                {
                    var hasOtherAssignments = await _context.SectionSubcontractors
                        .AnyAsync(ss => ss.SectionId == section.Id);

                    if (!hasOtherAssignments)
                    {
                        section.IsSubcontracted = false;
                        section.SubcontractorId = null;
                        section.UpdatedAt = DateTime.UtcNow;
                        section.UpdatedBy = User.Identity?.Name ?? "System";
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("Deleted section-subcontractor assignment {AssignmentId}", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting section-subcontractor assignment {AssignmentId}", id);
                return StatusCode(500, "An error occurred while deleting the assignment");
            }
        }

        private bool SubcontractorExists(int id)
        {
            return _context.Subcontractors.Any(e => e.Id == id);
        }
    }

    public class SectionSubcontractorRequest
    {
        public int SectionId { get; set; }
        public int SubcontractorId { get; set; }
        public string? ContractReference { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public decimal ContractAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public string? Notes { get; set; }
    }
}
