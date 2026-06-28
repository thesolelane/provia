using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JobTrackerApp.Models;
using JobTrackerApp.Services.BuildingCode;

namespace JobTrackerApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingCodeController : ControllerBase
    {
        private readonly BuildingCodeService _buildingCodeService;
        private readonly ILogger<BuildingCodeController> _logger;

        public BuildingCodeController(
            BuildingCodeService buildingCodeService,
            ILogger<BuildingCodeController> logger)
        {
            _buildingCodeService = buildingCodeService;
            _logger = logger;
        }

        // GET: api/BuildingCode
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BuildingCode>>> GetBuildingCodes()
        {
            try
            {
                var codes = await _buildingCodeService.GetAllCodes();
                return Ok(codes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building codes");
                return StatusCode(500, "An error occurred while retrieving building codes");
            }
        }

        // GET: api/BuildingCode/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BuildingCode>> GetBuildingCode(int id)
        {
            try
            {
                var code = await _buildingCodeService.GetCodeById(id);
                if (code == null)
                {
                    return NotFound();
                }
                return Ok(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building code with ID {CodeId}", id);
                return StatusCode(500, "An error occurred while retrieving the building code");
            }
        }

        // GET: api/BuildingCode/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BuildingCode>>> SearchBuildingCodes([FromQuery] string searchTerm)
        {
            try
            {
                var codes = await _buildingCodeService.SearchCodes(searchTerm);
                return Ok(codes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching building codes with term {SearchTerm}", searchTerm);
                return StatusCode(500, "An error occurred while searching building codes");
            }
        }

        // GET: api/BuildingCode/section/{sectionType}
        [HttpGet("section/{sectionType}")]
        public async Task<ActionResult<IEnumerable<BuildingCode>>> GetCodesBySection(SectionType sectionType)
        {
            try
            {
                var codes = await _buildingCodeService.GetCodesBySection(sectionType);
                return Ok(codes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building codes for section type {SectionType}", sectionType);
                return StatusCode(500, "An error occurred while retrieving building codes for the section");
            }
        }

        // GET: api/BuildingCode/category/{category}
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<BuildingCode>>> GetCodesByCategory(string category)
        {
            try
            {
                var codes = await _buildingCodeService.GetCodesByCategory(category);
                return Ok(codes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving building codes for category {Category}", category);
                return StatusCode(500, "An error occurred while retrieving building codes for the category");
            }
        }

        // POST: api/BuildingCode
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BuildingCode>> CreateBuildingCode(BuildingCode code)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Set metadata
                code.CreatedAt = DateTime.UtcNow;
                code.UpdatedAt = DateTime.UtcNow;
                code.CreatedBy = User.Identity?.Name ?? "System";
                code.UpdatedBy = User.Identity?.Name ?? "System";
                code.IsActive = true;

                var createdCode = await _buildingCodeService.AddCode(code);

                _logger.LogInformation("Created new building code: {CodeNumber} - {Title}", code.CodeNumber, code.Title);
                
                return CreatedAtAction(nameof(GetBuildingCode), new { id = createdCode.Id }, createdCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating building code");
                return StatusCode(500, "An error occurred while creating the building code");
            }
        }

        // PUT: api/BuildingCode/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBuildingCode(int id, BuildingCode code)
        {
            try
            {
                if (id != code.Id)
                {
                    return BadRequest("Building code ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Update metadata
                code.UpdatedAt = DateTime.UtcNow;
                code.UpdatedBy = User.Identity?.Name ?? "System";

                var success = await _buildingCodeService.UpdateCode(code);
                if (!success)
                {
                    return NotFound();
                }

                _logger.LogInformation("Updated building code: {CodeNumber} - {Title}", code.CodeNumber, code.Title);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating building code with ID {CodeId}", id);
                return StatusCode(500, "An error occurred while updating the building code");
            }
        }

        // DELETE: api/BuildingCode/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBuildingCode(int id)
        {
            try
            {
                var success = await _buildingCodeService.DeleteCode(id);
                if (!success)
                {
                    return NotFound();
                }

                _logger.LogInformation("Deleted building code with ID {CodeId}", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting building code with ID {CodeId}", id);
                return StatusCode(500, "An error occurred while deleting the building code");
            }
        }
    }
}
