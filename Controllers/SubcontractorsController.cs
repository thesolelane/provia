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
    public class SubcontractorsController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<SubcontractorsController> _logger;

        public SubcontractorsController(JobTrackerContext context, ILogger<SubcontractorsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Subcontractors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Subcontractor>>> GetSubcontractors()
        {
            try
            {
                return await _context.Subcontractors.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcontractors");
                return StatusCode(500, "Internal server error occurred while retrieving subcontractors.");
            }
        }

        // GET: api/Subcontractors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Subcontractor>> GetSubcontractor(int id)
        {
            try
            {
                var subcontractor = await _context.Subcontractors
                    .Include(s => s.JobSections)
                    .ThenInclude(js => js.Job)
                    .FirstOrDefaultAsync(s => s.SubcontractorId == id);

                if (subcontractor == null)
                {
                    return NotFound($"Subcontractor with ID {id} not found.");
                }

                return subcontractor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcontractor with ID {SubcontractorId}", id);
                return StatusCode(500, $"Internal server error occurred while retrieving subcontractor with ID {id}.");
            }
        }

        // POST: api/Subcontractors
        [HttpPost]
        public async Task<ActionResult<Subcontractor>> CreateSubcontractor(Subcontractor subcontractor)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.Subcontractors.Add(subcontractor);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSubcontractor), new { id = subcontractor.SubcontractorId }, subcontractor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new subcontractor");
                return StatusCode(500, "Internal server error occurred while creating a new subcontractor.");
            }
        }

        // PUT: api/Subcontractors/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubcontractor(int id, Subcontractor subcontractor)
        {
            try
            {
                if (id != subcontractor.SubcontractorId)
                {
                    return BadRequest("Subcontractor ID mismatch.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.Entry(subcontractor).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubcontractorExists(id))
                    {
                        return NotFound($"Subcontractor with ID {id} not found.");
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
                _logger.LogError(ex, "Error updating subcontractor with ID {SubcontractorId}", id);
                return StatusCode(500, $"Internal server error occurred while updating subcontractor with ID {id}.");
            }
        }

        // DELETE: api/Subcontractors/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubcontractor(int id)
        {
            try
            {
                var subcontractor = await _context.Subcontractors.FindAsync(id);
                if (subcontractor == null)
                {
                    return NotFound($"Subcontractor with ID {id} not found.");
                }

                // Check if subcontractor is assigned to any job sections
                var assignedSections = await _context.JobSections
                    .Where(js => js.SubcontractorId == id)
                    .ToListAsync();

                if (assignedSections.Any())
                {
                    return BadRequest("Cannot delete subcontractor as they are assigned to one or more job sections.");
                }

                _context.Subcontractors.Remove(subcontractor);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subcontractor with ID {SubcontractorId}", id);
                return StatusCode(500, $"Internal server error occurred while deleting subcontractor with ID {id}.");
            }
        }

        // GET: api/Subcontractors/search?query=keyword
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Subcontractor>>> SearchSubcontractors(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return await GetSubcontractors();
                }

                return await _context.Subcontractors
                    .Where(s => s.CompanyName.Contains(query) || 
                                s.ContactName.Contains(query) || 
                                s.Email.Contains(query) || 
                                s.Phone.Contains(query) ||
                                s.Specialty.Contains(query))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching subcontractors with query {Query}", query);
                return StatusCode(500, "Internal server error occurred while searching subcontractors.");
            }
        }

        // GET: api/Subcontractors/specialty?specialty=Plumbing
        [HttpGet("specialty")]
        public async Task<ActionResult<IEnumerable<Subcontractor>>> GetSubcontractorsBySpecialty(string specialty)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(specialty))
                {
                    return await GetSubcontractors();
                }

                return await _context.Subcontractors
                    .Where(s => s.Specialty.Contains(specialty))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcontractors by specialty {Specialty}", specialty);
                return StatusCode(500, "Internal server error occurred while retrieving subcontractors by specialty.");
            }
        }

        // GET: api/Subcontractors/5/jobs
        [HttpGet("{id}/jobs")]
        public async Task<ActionResult<IEnumerable<Job>>> GetSubcontractorJobs(int id)
        {
            try
            {
                if (!SubcontractorExists(id))
                {
                    return NotFound($"Subcontractor with ID {id} not found.");
                }

                var jobIds = await _context.JobSections
                    .Where(js => js.SubcontractorId == id)
                    .Select(js => js.JobId)
                    .Distinct()
                    .ToListAsync();

                var jobs = await _context.Jobs
                    .Where(j => jobIds.Contains(j.JobId))
                    .ToListAsync();

                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs for subcontractor with ID {SubcontractorId}", id);
                return StatusCode(500, $"Internal server error occurred while retrieving jobs for subcontractor with ID {id}.");
            }
        }

        private bool SubcontractorExists(int id)
        {
            return _context.Subcontractors.Any(e => e.SubcontractorId == id);
        }
    }
}
