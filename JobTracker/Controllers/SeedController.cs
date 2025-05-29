using Microsoft.AspNetCore.Mvc;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<SeedController> _logger;

        public SeedController(JobTrackerContext context, ILogger<SeedController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Seed/JobSections
        [HttpGet("JobSections")]
        public async Task<IActionResult> SeedJobSections()
        {
            try
            {
                var job = await _context.Jobs.FirstOrDefaultAsync();
                
                if (job == null)
                {
                    return NotFound("No jobs found in the database");
                }

                // Check if we already have more than 5 sections
                var existingSections = await _context.JobSections.CountAsync(j => j.JobId == job.Id);
                if (existingSections > 5)
                {
                    return Ok($"Job already has {existingSections} sections, no need to add more.");
                }
                
                // Create additional job sections with different statuses and types
                var newSections = new List<JobSection>
                {
                    new JobSection
                    {
                        JobId = job.Id,
                        SectionType = 5, // HVAC 
                        Description = "Install central air conditioning system",
                        Status = 1, // In Progress
                        StartDate = DateTime.UtcNow.AddDays(5),
                        CompletionDate = null,
                        IsSubcontracted = true,
                        ContractReference = "HVAC-2023-789",
                        MaterialsOrdered = true,
                        MaterialsDelivered = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new JobSection
                    {
                        JobId = job.Id,
                        SectionType = 8, // Flooring
                        Description = "Install hardwood floors in kitchen and living room",
                        Status = 0, // Not Started
                        StartDate = DateTime.UtcNow.AddDays(15),
                        CompletionDate = null,
                        IsSubcontracted = false,
                        MaterialsOrdered = true,
                        MaterialsDelivered = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new JobSection
                    {
                        JobId = job.Id,
                        SectionType = 9, // Cabinetry (using Finishing type)
                        Description = "Install custom kitchen cabinets",
                        Status = 0, // Not Started
                        StartDate = DateTime.UtcNow.AddDays(22),
                        CompletionDate = null,
                        IsSubcontracted = false,
                        MaterialsOrdered = true,
                        MaterialsDelivered = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new JobSection
                    {
                        JobId = job.Id,
                        SectionType = 9, // Countertops (using Finishing type)
                        Description = "Install granite countertops in kitchen",
                        Status = 0, // Not Started
                        StartDate = null,
                        CompletionDate = null,
                        IsSubcontracted = true,
                        ContractReference = "STONE-2023-321",
                        MaterialsOrdered = false,
                        MaterialsDelivered = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new JobSection
                    {
                        JobId = job.Id,
                        SectionType = 7, // Painting
                        Description = "Paint kitchen and bathrooms",
                        Status = 0, // Not Started
                        StartDate = null,
                        CompletionDate = null,
                        IsSubcontracted = false,
                        MaterialsOrdered = false,
                        MaterialsDelivered = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new JobSection
                    {
                        JobId = job.Id,
                        SectionType = 9, // Fixtures (using Finishing type)
                        Description = "Install new light fixtures and bathroom fixtures",
                        Status = 0, // Not Started
                        StartDate = null,
                        CompletionDate = null,
                        IsSubcontracted = false,
                        MaterialsOrdered = true,
                        MaterialsDelivered = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new JobSection
                    {
                        JobId = job.Id,
                        SectionType = 9, // Appliances (using Finishing type)
                        Description = "Install new kitchen appliances",
                        Status = 0, // Not Started
                        StartDate = null,
                        CompletionDate = null,
                        IsSubcontracted = false,
                        MaterialsOrdered = true,
                        MaterialsDelivered = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new JobSection
                    {
                        JobId = job.Id,
                        SectionType = 10, // Final Inspection
                        Description = "Final inspection and client walkthrough",
                        Status = 0, // Not Started
                        StartDate = null,
                        CompletionDate = null,
                        IsSubcontracted = false,
                        MaterialsOrdered = false,
                        MaterialsDelivered = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                _context.JobSections.AddRange(newSections);
                await _context.SaveChangesAsync();

                return Ok($"Added {newSections.Count} job sections to job {job.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding job sections");
                return StatusCode(500, $"An error occurred while seeding job sections: {ex.Message}");
            }
        }
    }
}