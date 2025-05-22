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
    public class BuildingCodeController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<BuildingCodeController> _logger;

        public BuildingCodeController(JobTrackerContext context, ILogger<BuildingCodeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/BuildingCode
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BuildingCode>>> GetBuildingCodes()
        {
            try
            {
                return await _context.BuildingCodes.Where(bc => bc.IsActive).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building codes");
                return StatusCode(500, "Internal server error occurred while retrieving building codes.");
            }
        }

        // GET: api/BuildingCode/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BuildingCode>> GetBuildingCode(int id)
        {
            try
            {
                var buildingCode = await _context.BuildingCodes.FindAsync(id);

                if (buildingCode == null || !buildingCode.IsActive)
                {
                    return NotFound($"Building code with ID {id} not found.");
                }

                return buildingCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building code with ID {BuildingCodeId}", id);
                return StatusCode(500, $"Internal server error occurred while retrieving building code with ID {id}.");
            }
        }

        // GET: api/BuildingCode/section/{sectionType}
        [HttpGet("section/{sectionType}")]
        public async Task<ActionResult<IEnumerable<BuildingCode>>> GetBuildingCodesBySection(SectionType sectionType)
        {
            try
            {
                return await _context.BuildingCodes
                    .Where(bc => bc.RelatedSection == sectionType && bc.IsActive)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building codes for section type {SectionType}", sectionType);
                return StatusCode(500, $"Internal server error occurred while retrieving building codes for section type {sectionType}.");
            }
        }

        // GET: api/BuildingCode/search?query=keyword
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BuildingCode>>> SearchBuildingCodes(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return await _context.BuildingCodes.Where(bc => bc.IsActive).Take(50).ToListAsync();
                }

                return await _context.BuildingCodes
                    .Where(bc => bc.IsActive && (
                        bc.CodeNumber.Contains(query) ||
                        bc.Title.Contains(query) ||
                        bc.Description.Contains(query) ||
                        bc.Category.Contains(query) ||
                        bc.Subcategory.Contains(query) ||
                        bc.FullText.Contains(query)))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching building codes with query {Query}", query);
                return StatusCode(500, "Internal server error occurred while searching building codes.");
            }
        }

        // POST: api/BuildingCode
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<BuildingCode>> CreateBuildingCode(BuildingCode buildingCode)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.BuildingCodes.Add(buildingCode);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBuildingCode), new { id = buildingCode.CodeId }, buildingCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new building code");
                return StatusCode(500, "Internal server error occurred while creating a new building code.");
            }
        }

        // PUT: api/BuildingCode/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> UpdateBuildingCode(int id, BuildingCode buildingCode)
        {
            try
            {
                if (id != buildingCode.CodeId)
                {
                    return BadRequest("Building code ID mismatch.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _context.Entry(buildingCode).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BuildingCodeExists(id))
                    {
                        return NotFound($"Building code with ID {id} not found.");
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
                _logger.LogError(ex, "Error updating building code with ID {BuildingCodeId}", id);
                return StatusCode(500, $"Internal server error occurred while updating building code with ID {id}.");
            }
        }

        // DELETE: api/BuildingCode/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteBuildingCode(int id)
        {
            try
            {
                var buildingCode = await _context.BuildingCodes.FindAsync(id);
                if (buildingCode == null)
                {
                    return NotFound($"Building code with ID {id} not found.");
                }

                // Soft delete - mark as inactive
                buildingCode.IsActive = false;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting building code with ID {BuildingCodeId}", id);
                return StatusCode(500, $"Internal server error occurred while deleting building code with ID {id}.");
            }
        }

        // GET: api/BuildingCode/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            try
            {
                var categories = await _context.BuildingCodes
                    .Where(bc => bc.IsActive && !string.IsNullOrEmpty(bc.Category))
                    .Select(bc => bc.Category)
                    .Distinct()
                    .ToListAsync();

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building code categories");
                return StatusCode(500, "Internal server error occurred while retrieving building code categories.");
            }
        }

        // GET: api/BuildingCode/subcategories/{category}
        [HttpGet("subcategories/{category}")]
        public async Task<ActionResult<IEnumerable<string>>> GetSubcategories(string category)
        {
            try
            {
                var subcategories = await _context.BuildingCodes
                    .Where(bc => bc.IsActive && bc.Category == category && !string.IsNullOrEmpty(bc.Subcategory))
                    .Select(bc => bc.Subcategory)
                    .Distinct()
                    .ToListAsync();

                return subcategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcategories for category {Category}", category);
                return StatusCode(500, $"Internal server error occurred while retrieving subcategories for category {category}.");
            }
        }

        private bool BuildingCodeExists(int id)
        {
            return _context.BuildingCodes.Any(e => e.CodeId == id);
        }
    }
}
